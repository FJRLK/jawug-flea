using System;
using System.Collections;

namespace MonoTorrent.Client
{
    public class Piece : IComparable<Piece>
    {
        #region Static

        internal const int BlockSize = (1 << 14); // 16kB

        #endregion

        #region Internals

        private Block[] blocks;
        private int index;
        private int totalReceived;
        private int totalRequested;
        private int totalWritten;

        public bool AllBlocksReceived
        {
            get { return totalReceived == BlockCount; }
        }

        public bool AllBlocksRequested
        {
            get { return totalRequested == BlockCount; }
        }

        public bool AllBlocksWritten
        {
            get { return totalWritten == BlockCount; }
        }

        public int BlockCount
        {
            get { return blocks.Length; }
        }

        internal Block[] Blocks
        {
            get { return blocks; }
        }

        public int Index
        {
            get { return index; }
        }

        public Block this[int index]
        {
            get { return blocks[index]; }
        }

        public bool NoBlocksRequested
        {
            get { return totalRequested == 0; }
        }

        public int TotalReceived
        {
            get { return totalReceived; }
            internal set { totalReceived = value; }
        }

        public int TotalRequested
        {
            get { return totalRequested; }
            internal set { totalRequested = value; }
        }

        public int TotalWritten
        {
            get { return totalWritten; }
            internal set { totalWritten = value; }
        }

        #endregion

        #region Constructor

        internal Piece(int pieceIndex, int pieceLength, long torrentSize)
        {
            index = pieceIndex;

            // Request last piece. Special logic needed
            if ((torrentSize - (long) pieceIndex*pieceLength) < pieceLength)
                LastPiece(pieceIndex, pieceLength, torrentSize);

            else
            {
                int numberOfPieces = (int) Math.Ceiling(((double) pieceLength/BlockSize));

                blocks = new Block[numberOfPieces];

                for (int i = 0; i < numberOfPieces; i++)
                    blocks[i] = new Block(this, i*BlockSize, BlockSize);

                if ((pieceLength%BlockSize) != 0) // I don't think this would ever happen. But just in case
                    blocks[blocks.Length - 1] = new Block(this, blocks[blocks.Length - 1].StartOffset,
                        pieceLength - blocks[blocks.Length - 1].StartOffset);
            }
        }

        #endregion

        #region Members

        public int CompareTo(Piece other)
        {
            return other == null ? 1 : Index.CompareTo(other.Index);
        }

        private void LastPiece(int pieceIndex, int pieceLength, long torrentSize)
        {
            int bytesRemaining = (int) (torrentSize - ((long) pieceIndex*pieceLength));
            int numberOfBlocks = bytesRemaining/BlockSize;
            if (bytesRemaining%BlockSize != 0)
                numberOfBlocks++;

            blocks = new Block[numberOfBlocks];

            int i = 0;
            while (bytesRemaining - BlockSize > 0)
            {
                blocks[i] = new Block(this, i*BlockSize, BlockSize);
                bytesRemaining -= BlockSize;
                i++;
            }

            blocks[i] = new Block(this, i*BlockSize, bytesRemaining);
        }

        public override bool Equals(object obj)
        {
            Piece p = obj as Piece;
            return (p == null) ? false : index.Equals(p.index);
        }

        public IEnumerator GetEnumerator()
        {
            return blocks.GetEnumerator();
        }

        public override int GetHashCode()
        {
            return index;
        }

        #endregion
    }
}