using System;
using System.Collections.Generic;
using MonoTorrent.Client.Messages;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    public class RandomisedPicker : PiecePicker
    {
        #region Internals

        Random random = new Random();

        #endregion

        #region Constructor

        public RandomisedPicker(PiecePicker picker)
            : base(picker)
        {
        }

        #endregion

        #region Members

        public override MessageBundle PickPiece(PeerId id, BitField peerBitfield, List<PeerId> otherPeers, int count,
            int startIndex, int endIndex)
        {
            if (peerBitfield.AllFalse)
                return null;

            if (count > 1)
                return base.PickPiece(id, peerBitfield, otherPeers, count, startIndex, endIndex);

            int midpoint = random.Next(startIndex, endIndex);
            return base.PickPiece(id, peerBitfield, otherPeers, count, midpoint, endIndex) ??
                   base.PickPiece(id, peerBitfield, otherPeers, count, startIndex, midpoint);
        }

        #endregion
    }
}