using System;
using System.Collections.Generic;
using MonoTorrent.Client.Messages.Standard;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    class ChokeData
    {
        #region Internals

        public BitField CurrentPieces;
        public DateTime LastChoked;
        public DateTime LastUnchoked;
        public PeerId Peer;
        public int SharedPieces;
        public int TotalPieces;

        public double ShareRatio
        {
            get { return (SharedPieces + 1.0)/(TotalPieces + 1.0); }
        }

        #endregion

        #region Constructor

        public ChokeData(PeerId peer)
        {
            LastChoked = DateTime.Now;
            Peer = peer;
            CurrentPieces = new BitField(peer.BitField.Length);
        }

        #endregion
    }

    class SeededPiece
    {
        #region Internals

        public int BlocksSent;
        public int Index;
        public PeerId Peer;
        public DateTime SeededAt;
        public int TotalBlocks;

        #endregion

        #region Constructor

        public SeededPiece(PeerId peer, int index, int totalBlocks)
        {
            Index = index;
            Peer = peer;
            SeededAt = DateTime.Now;
            TotalBlocks = totalBlocks;
        }

        #endregion
    }

    class InitialSeedUnchoker : Unchoker
    {
        #region Internals

        List<SeededPiece> advertisedPieces;
        BitField bitfield;
        TorrentManager manager;
        List<ChokeData> peers;
        BitField temp;

        public bool Complete
        {
            get { return bitfield.AllTrue; }
        }

        public int MaxAdvertised
        {
            get { return 4; }
        }

        internal int PeerCount
        {
            get { return peers.Count; }
        }

        bool PendingUnchoke
        {
            get { return peers.Exists(delegate(ChokeData d) { return d.Peer.AmChoking && d.Peer.IsInterested; }); }
        }

        #endregion

        #region Constructor

        public InitialSeedUnchoker(TorrentManager manager)
        {
            advertisedPieces = new List<SeededPiece>();
            bitfield = new BitField(manager.Bitfield.Length);
            this.manager = manager;
            peers = new List<ChokeData>();
            temp = new BitField(bitfield.Length);
        }

        #endregion

        #region Members

        public override void Choke(PeerId id)
        {
            base.Choke(id);

            advertisedPieces.RemoveAll(delegate(SeededPiece p) { return p.Peer == id; });

            // Place the peer at the end of the list so the rest of the peers
            // will get an opportunity to unchoke before this peer gets tried again
            ChokeData data = peers.Find(delegate(ChokeData d) { return d.Peer == id; });
            peers.Remove(data);
            peers.Add(data);
        }

        public void PeerConnected(PeerId id)
        {
            peers.Add(new ChokeData(id));
        }

        public void PeerDisconnected(PeerId id)
        {
            peers.RemoveAll(delegate(ChokeData d) { return d.Peer == id; });
            advertisedPieces.RemoveAll(delegate(SeededPiece piece) { return piece.Peer == id; });
        }

        public void ReceivedHave(PeerId peer, int pieceIndex)
        {
            bitfield[pieceIndex] = true;

            // If a peer reports they have a piece that *isn't* the peer
            // we uploaded it to, then the peer we uploaded to has shared it
            foreach (ChokeData data in peers)
            {
                if (data.CurrentPieces[pieceIndex] && data.Peer != peer)
                {
                    data.CurrentPieces[pieceIndex] = false;
                    data.SharedPieces++;
                    // Give him another piece if no-one else is waiting.
                    TryAdvertisePiece(data);
                    break;
                }
            }

            foreach (SeededPiece piece in advertisedPieces)
            {
                if (piece.Index == pieceIndex)
                {
                    advertisedPieces.Remove(piece);
                    return;
                }
            }
        }

        public void ReceivedNotInterested(PeerId id)
        {
            advertisedPieces.RemoveAll(delegate(SeededPiece piece) { return piece.Peer == id; });
        }

        public void SentBlock(PeerId peer, int pieceIndex)
        {
            SeededPiece piece =
                advertisedPieces.Find(delegate(SeededPiece p) { return p.Peer == peer && p.Index == pieceIndex; });
            if (piece == null)
                return;

            piece.SeededAt = DateTime.Now;
            piece.BlocksSent++;
            if (piece.TotalBlocks == piece.BlocksSent)
                advertisedPieces.Remove(piece);
        }

        void TryAdvertisePiece(ChokeData data)
        {
            // If we are seeding to this peer and we have a peer waiting to unchoke
            // don't advertise more data
            if (!data.Peer.AmChoking && PendingUnchoke)
                return;

            int advertised = advertisedPieces.FindAll(delegate(SeededPiece p) { return p.Peer == data.Peer; }).Count;
            int max = MaxAdvertised;
            if (manager.UploadingTo < manager.Settings.UploadSlots)
                max = MaxAdvertised;
            else if (data.ShareRatio < 0.25)
                max = 1;
            else if (data.ShareRatio < 0.35)
                max = 2;
            else if (data.ShareRatio < 0.50)
                max = 3;
            else
                max = MaxAdvertised;

            if (advertised >= max)
                return;

            // List of pieces *not* in the swarm
            temp.From(bitfield).Not();

            // List of pieces that he wants that aren't in the swarm
            temp.NAnd(data.Peer.BitField);

            // Ignore all the pieces we've already started sharing
            foreach (SeededPiece p in advertisedPieces)
                temp[p.Index] = false;

            int index = 0;
            while (advertised < max)
            {
                // Get the index of the first piece we can send him
                index = temp.FirstTrue(index, temp.Length);
                // Looks like he's not interested in us...
                if (index == -1)
                    return;

                advertised++;
                data.TotalPieces++;
                data.CurrentPieces[index] = true;
                advertisedPieces.Add(new SeededPiece(data.Peer, index,
                    data.Peer.TorrentManager.Torrent.PieceLength/Piece.BlockSize));
                data.Peer.Enqueue(new HaveMessage(index));
                index++;
            }
        }

        void TryChoke(ChokeData data)
        {
            // Already choked
            if (data.Peer.AmChoking)
                return;

            if (!data.Peer.IsInterested)
            {
                // Choke him if he's not interested
                Choke(data.Peer);
            }
            else if (!advertisedPieces.Exists(delegate(SeededPiece p) { return p.Peer == data.Peer; }))
            {
                // If we have no free slots and peers are waiting, choke after 30 seconds.
                // FIXME: Choke as soon as the next piece completes *or* a larger time limit *and*
                // at least one piece has uploaded.
                data.LastChoked = DateTime.Now;
                Choke(data.Peer);
            }
        }

        void TryUnchoke(ChokeData data)
        {
            // Already unchoked
            if (!data.Peer.AmChoking)
                return;

            // Don't unchoke if he's not interested
            if (!data.Peer.IsInterested)
                return;

            // Don't unchoke if we are have maxed our slots
            if (manager.UploadingTo >= manager.Settings.UploadSlots)
                return;

            data.LastUnchoked = DateTime.Now;
            Unchoke(data.Peer);
        }

        public override void UnchokeReview()
        {
            if (PendingUnchoke)
            {
                List<ChokeData> dupePeers = new List<ChokeData>(peers);
                foreach (ChokeData data in dupePeers)
                    TryChoke(data);

                dupePeers = new List<ChokeData>(peers);
                // See if there's anyone interesting to unchoke
                foreach (ChokeData data in dupePeers)
                    TryUnchoke(data);
            }

            // Make sure our list of pieces available in the swarm is up to date
            foreach (ChokeData data in peers)
                bitfield.Or(data.Peer.BitField);

            advertisedPieces.RemoveAll(delegate(SeededPiece p) { return bitfield[p.Index]; });

            // Send have messages to anyone that needs them
            foreach (ChokeData data in peers)
                TryAdvertisePiece(data);
        }

        #endregion
    }
}