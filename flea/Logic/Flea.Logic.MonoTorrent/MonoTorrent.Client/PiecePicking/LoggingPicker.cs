using System;
using System.Collections.Generic;
using MonoTorrent.Client.Messages;
using MonoTorrent.Client.Messages.Standard;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    class LoggingPicker : PiecePicker
    {
        #region Types

        class Request : IComparable<Request>
        {
            #region Internals

            public int PieceIndex;
            public PeerId RequestedOff;
            public int RequestLength;
            public int StartOffset;
            public bool Verified;

            #endregion

            #region Members

            public int CompareTo(Request other)
            {
                int difference;
                if ((difference = PieceIndex.CompareTo(other.PieceIndex)) != 0)
                    return difference;
                if ((difference = StartOffset.CompareTo(other.StartOffset)) != 0)
                    return difference;
                return RequestLength.CompareTo(other.RequestLength);
            }

            #endregion
        }

        #endregion

        #region Internals

        SortList<Request> requests = new SortList<Request>();

        #endregion

        #region Constructor

        public LoggingPicker(PiecePicker picker)
            : base(picker)
        {
        }

        #endregion

        #region Members

        public override RequestMessage ContinueExistingRequest(PeerId peer)
        {
            RequestMessage m = base.ContinueExistingRequest(peer);
            if (m != null)
                HandleRequest(peer, m);
            return m;
        }

        public override MessageBundle PickPiece(PeerId id, BitField peerBitfield, List<PeerId> otherPeers, int count,
            int startIndex, int endIndex)
        {
            MessageBundle bundle = base.PickPiece(id, peerBitfield, otherPeers, count, startIndex, endIndex);
            if (bundle != null)
            {
                foreach (RequestMessage m in bundle.Messages)
                {
                    HandleRequest(id, m);
                }
            }

            return bundle;
        }

        private void HandleRequest(PeerId id, RequestMessage m)
        {
            Request r = new Request();
            r.PieceIndex = m.PieceIndex;
            r.RequestedOff = id;
            r.RequestLength = m.RequestLength;
            r.StartOffset = m.StartOffset;
            List<Request> current = requests.FindAll(delegate(Request req) { return req.CompareTo(r) == 0; });
            if (current.Count > 0)
            {
                foreach (Request request in current)
                {
                    if (request.Verified)
                    {
                        if (id.TorrentManager.Bitfield[request.PieceIndex])
                        {
                            Logger.Log(null, "Double request: {0}", m);
                            Logger.Log(null, "From: {0} and {1}", id.PeerID, r.RequestedOff.PeerID);
                        }
                        else
                        {
                            // The piece failed a hashcheck, so ignore it this time
                            requests.Remove(request);
                        }
                    }
                }
            }
            requests.Add(r);
        }

        public override bool ValidatePiece(PeerId peer, int pieceIndex, int startOffset, int length, out Piece piece)
        {
            bool validatedOk = base.ValidatePiece(peer, pieceIndex, startOffset, length, out piece);

            List<Request> list = requests.FindAll(delegate(Request r)
            {
                return r.PieceIndex == pieceIndex &&
                       r.RequestLength == length &&
                       r.StartOffset == startOffset;
            });

            if (list.Count == 0)
            {
                Logger.Log(null, "Piece was not requested from anyone: {1}-{2}", peer.PeerID, pieceIndex, startOffset);
            }
            else if (list.Count == 1)
            {
                if (list[0].Verified)
                {
                    if (validatedOk)
                        Logger.Log(null, "The piece should not have validated");
                    Logger.Log(null, "Piece already verified: Orig: {0} Current: {3} <> {1}-{2}",
                        list[0].RequestedOff.PeerID, pieceIndex, startOffset, peer.PeerID);
                }
            }
            else
            {
                List<Request> alreadyVerified = list.FindAll(delegate(Request r) { return r.Verified; });
                if (alreadyVerified.Count > 0)
                {
                    if (validatedOk)
                        Logger.Log(null, "The piece should not have validated 2");
                    Logger.Log(null, "Piece has already been verified {0} times", alreadyVerified.Count);
                }
            }

            foreach (Request request in list)
            {
                if (request.RequestedOff == peer)
                    if (!request.Verified)
                    {
                        if (!validatedOk)
                            Logger.Log(null, "The piece should have validated");
                        request.Verified = true;
                    }
                    else
                    {
                        if (validatedOk)
                            Logger.Log(null, "The piece should not have validated 3");
                        Logger.Log(null, "This peer has already sent and verified this piece. {0} <> {1}-{2}",
                            peer.PeerID, pieceIndex, startOffset);
                    }
            }

            return validatedOk;
        }

        #endregion
    }
}