using System;
using System.Text;

namespace MonoTorrent.Client.Messages.Standard
{
    public class PieceMessage : PeerMessage
    {
        #region Static

        private const int messageLength = 9;
        internal static readonly byte MessageId = 7;

        #endregion

        #region Internals

        internal byte[] Data;

        private int dataOffset;
        private int pieceIndex;
        private int requestLength;
        private int startOffset;

        internal int BlockIndex
        {
            get { return startOffset/Piece.BlockSize; }
        }

        public override int ByteLength
        {
            get { return (messageLength + requestLength + 4); }
        }

        internal int DataOffset
        {
            get { return dataOffset; }
        }

        public int PieceIndex
        {
            get { return pieceIndex; }
        }

        public int RequestLength
        {
            get { return requestLength; }
        }

        public int StartOffset
        {
            get { return startOffset; }
        }

        #endregion

        #region Constructor

        public PieceMessage()
        {
            Data = BufferManager.EmptyBuffer;
        }

        public PieceMessage(int pieceIndex, int startOffset, int blockLength)
        {
            this.pieceIndex = pieceIndex;
            this.startOffset = startOffset;
            requestLength = blockLength;
            Data = BufferManager.EmptyBuffer;
        }

        #endregion

        #region Members

        public override void Decode(byte[] buffer, int offset, int length)
        {
            pieceIndex = ReadInt(buffer, ref offset);
            startOffset = ReadInt(buffer, ref offset);
            requestLength = length - 8;

            dataOffset = offset;

            // This buffer will be freed after the PieceWriter has finished with it
            Data = BufferManager.EmptyBuffer;
            ClientEngine.BufferManager.GetBuffer(ref Data, requestLength);
            Buffer.BlockCopy(buffer, offset, Data, 0, requestLength);
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, messageLength + requestLength);
            written += Write(buffer, written, MessageId);
            written += Write(buffer, written, pieceIndex);
            written += Write(buffer, written, startOffset);
            written += Write(buffer, written, Data, 0, requestLength);

            return CheckWritten(written - offset);
        }

        public override bool Equals(object obj)
        {
            PieceMessage msg = obj as PieceMessage;
            return (msg == null)
                ? false
                : (pieceIndex == msg.pieceIndex
                   && startOffset == msg.startOffset
                   && requestLength == msg.requestLength);
        }

        public override int GetHashCode()
        {
            return (requestLength.GetHashCode()
                    ^ dataOffset.GetHashCode()
                    ^ pieceIndex.GetHashCode()
                    ^ startOffset.GetHashCode());
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("PieceMessage ");
            sb.Append(" Index ");
            sb.Append(pieceIndex);
            sb.Append(" Offset ");
            sb.Append(startOffset);
            sb.Append(" Length ");
            sb.Append(requestLength);
            return sb.ToString();
        }

        #endregion
    }
}