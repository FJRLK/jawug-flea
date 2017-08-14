using System;
using System.Collections.Generic;
using MonoTorrent.Client.Connections;
using MonoTorrent.Client.Messages;
using MonoTorrent.Client.Messages.Standard;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    /// <summary>
    ///     Contains the logic for choosing what piece to download next
    /// </summary>
    public class PieceManager
    {
        #region Types

        public event EventHandler<BlockEventArgs> BlockReceived;
        public event EventHandler<BlockEventArgs> BlockRequested;
        public event EventHandler<BlockEventArgs> BlockRequestCancelled;

        #endregion

        #region Static

        // For every 10 kB/sec upload a peer has, we request one extra piece above the standard amount him
        internal const int BonusRequestPerKb = 10;
        internal const int MaxEndGameRequests = 2;
        internal const int NormalRequestAmount = 2;

        #endregion

        #region Internals

        PiecePicker picker;
        BitField unhashedPieces;

        internal PiecePicker Picker
        {
            get { return picker; }
        }

        internal BitField UnhashedPieces
        {
            get { return unhashedPieces; }
        }

        #endregion

        #region Constructor

        internal PieceManager()
        {
            picker = new NullPicker();
            unhashedPieces = new BitField(0);
        }

        #endregion

        #region Members

        internal void RaiseBlockReceived(BlockEventArgs args)
        {
            Toolbox.RaiseAsyncEvent<BlockEventArgs>(BlockReceived, args.TorrentManager, args);
        }

        internal void RaiseBlockRequested(BlockEventArgs args)
        {
            Toolbox.RaiseAsyncEvent<BlockEventArgs>(BlockRequested, args.TorrentManager, args);
        }

        internal void RaiseBlockRequestCancelled(BlockEventArgs args)
        {
            Toolbox.RaiseAsyncEvent<BlockEventArgs>(BlockRequestCancelled, args.TorrentManager, args);
        }

        public void PieceDataReceived(PeerId peer, PieceMessage message)
        {
            Piece piece;
            if (picker.ValidatePiece(peer, message.PieceIndex, message.StartOffset, message.RequestLength, out piece))
            {
                PeerId id = peer;
                TorrentManager manager = id.TorrentManager;
                Block block = piece.Blocks[message.StartOffset/Piece.BlockSize];
                long offset = (long) message.PieceIndex*id.TorrentManager.Torrent.PieceLength + message.StartOffset;

                id.LastBlockReceived = DateTime.Now;
                id.TorrentManager.PieceManager.RaiseBlockReceived(new BlockEventArgs(manager, block, piece, id));
                id.TorrentManager.Engine.DiskManager.QueueWrite(manager, offset, message.Data, message.RequestLength,
                    delegate
                    {
                        piece.Blocks[message.StartOffset/Piece.BlockSize].Written = true;
                        ClientEngine.BufferManager.FreeBuffer(ref message.Data);
                        // If we haven't written all the pieces to disk, there's no point in hash checking
                        if (!piece.AllBlocksWritten)
                            return;

                        // Hashcheck the piece as we now have all the blocks.
                        id.Engine.DiskManager.BeginGetHash(id.TorrentManager, piece.Index, delegate(object o)
                        {
                            byte[] hash = (byte[]) o;
                            bool result = hash == null
                                ? false
                                : id.TorrentManager.Torrent.Pieces.IsValid(hash, piece.Index);
                            id.TorrentManager.Bitfield[message.PieceIndex] = result;

                            ClientEngine.MainLoop.Queue(delegate
                            {
                                id.TorrentManager.PieceManager.UnhashedPieces[piece.Index] = false;

                                id.TorrentManager.HashedPiece(new PieceHashedEventArgs(id.TorrentManager, piece.Index,
                                    result));
                                List<PeerId> peers = new List<PeerId>(piece.Blocks.Length);
                                for (int i = 0; i < piece.Blocks.Length; i++)
                                    if (piece.Blocks[i].RequestedOff != null &&
                                        !peers.Contains(piece.Blocks[i].RequestedOff))
                                        peers.Add(piece.Blocks[i].RequestedOff);

                                for (int i = 0; i < peers.Count; i++)
                                {
                                    if (peers[i].Connection != null)
                                    {
                                        peers[i].Peer.HashedPiece(result);
                                        if (peers[i].Peer.TotalHashFails == 5)
                                            peers[i].ConnectionManager.CleanupSocket(id, "Too many hash fails");
                                    }
                                }

                                // If the piece was successfully hashed, enqueue a new "have" message to be sent out
                                if (result)
                                    id.TorrentManager.finishedPieces.Enqueue(piece.Index);
                            });
                        });
                    });

                if (piece.AllBlocksReceived)
                    unhashedPieces[message.PieceIndex] = true;
            }
            else
            {
            }
        }

        internal void AddPieceRequests(PeerId id)
        {
            PeerMessage msg = null;
            int maxRequests = id.MaxPendingRequests;

            if (id.AmRequestingPiecesCount >= maxRequests)
                return;

            int count = 1;
            if (id.Connection is HttpConnection)
            {
                // How many whole pieces fit into 2MB
                count = (2*1024*1024)/id.TorrentManager.Torrent.PieceLength;

                // Make sure we have at least one whole piece
                count = Math.Max(count, 1);

                count *= id.TorrentManager.Torrent.PieceLength/Piece.BlockSize;
            }

            if (!id.IsChoking || id.SupportsFastPeer)
            {
                while (id.AmRequestingPiecesCount < maxRequests)
                {
                    msg = Picker.ContinueExistingRequest(id);
                    if (msg != null)
                        id.Enqueue(msg);
                    else
                        break;
                }
            }

            if (!id.IsChoking || (id.SupportsFastPeer && id.IsAllowedFastPieces.Count > 0))
            {
                while (id.AmRequestingPiecesCount < maxRequests)
                {
                    msg = Picker.PickPiece(id, id.TorrentManager.Peers.ConnectedPeers, count);
                    if (msg != null)
                        id.Enqueue(msg);
                    else
                        break;
                }
            }
        }

        internal bool IsInteresting(PeerId id)
        {
            // If i have completed the torrent, then no-one is interesting
            if (id.TorrentManager.Complete)
                return false;

            // If the peer is a seeder, then he is definately interesting
            if ((id.Peer.IsSeeder = id.BitField.AllTrue))
                return true;

            // Otherwise we need to do a full check
            return Picker.IsInteresting(id.BitField);
        }

        internal void ChangePicker(PiecePicker picker, BitField bitfield, TorrentFile[] files)
        {
            if (unhashedPieces.Length != bitfield.Length)
                unhashedPieces = new BitField(bitfield.Length);

            picker = new IgnoringPicker(bitfield, picker);
            picker = new IgnoringPicker(unhashedPieces, picker);
            IEnumerable<Piece> pieces = Picker == null ? new List<Piece>() : Picker.ExportActiveRequests();
            picker.Initialise(bitfield, files, pieces);
            this.picker = picker;
        }

        internal void Reset()
        {
            unhashedPieces.SetAll(false);
            if (picker != null)
                picker.Reset();
        }

        internal int CurrentRequestCount()
        {
            return
                (int) ClientEngine.MainLoop.QueueWait((MainLoopJob) delegate { return Picker.CurrentRequestCount(); });
        }

        #endregion
    }
}