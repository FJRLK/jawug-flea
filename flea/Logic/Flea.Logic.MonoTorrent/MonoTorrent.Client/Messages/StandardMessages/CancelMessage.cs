using System.Text;

namespace MonoTorrent.Client.Messages.Standard
{
    /// <summary>
    /// </summary>
    public class CancelMessage : PeerMessage
    {
        #region Static

        private const int messageLength = 13;
        internal static readonly byte MessageId = 8;

        #endregion

        #region Internals

        private int pieceIndex;
        private int requestLength;
        private int startOffset;

        /// <summary>
        ///     Returns the length of the message in bytes
        /// </summary>
        public override int ByteLength
        {
            get { return (messageLength + 4); }
        }

        /// <summary>
        ///     The index of the piece
        /// </summary>
        public int PieceIndex
        {
            get { return pieceIndex; }
        }


        /// <summary>
        ///     The length in bytes of the block of data
        /// </summary>
        public int RequestLength
        {
            get { return requestLength; }
        }


        /// <summary>
        ///     The offset in bytes of the block of data
        /// </summary>
        public int StartOffset
        {
            get { return startOffset; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Creates a new CancelMessage
        /// </summary>
        public CancelMessage()
        {
        }


        /// <summary>
        ///     Creates a new CancelMessage
        /// </summary>
        /// <param name="pieceIndex">The index of the piece to cancel</param>
        /// <param name="startOffset">The offset in bytes of the block of data to cancel</param>
        /// <param name="requestLength">The length in bytes of the block of data to cancel</param>
        public CancelMessage(int pieceIndex, int startOffset, int requestLength)
        {
            this.pieceIndex = pieceIndex;
            this.startOffset = startOffset;
            this.requestLength = requestLength;
        }

        #endregion

        #region Members

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, messageLength);
            written += Write(buffer, written, MessageId);
            written += Write(buffer, written, pieceIndex);
            written += Write(buffer, written, startOffset);
            written += Write(buffer, written, requestLength);

            return CheckWritten(written - offset);
        }

        public override void Decode(byte[] buffer, int offset, int length)
        {
            pieceIndex = ReadInt(buffer, ref offset);
            startOffset = ReadInt(buffer, ref offset);
            requestLength = ReadInt(buffer, ref offset);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("CancelMessage ");
            sb.Append(" Index ");
            sb.Append(pieceIndex);
            sb.Append(" Offset ");
            sb.Append(startOffset);
            sb.Append(" Length ");
            sb.Append(requestLength);
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            CancelMessage msg = obj as CancelMessage;

            if (msg == null)
                return false;

            return (pieceIndex == msg.pieceIndex
                    && startOffset == msg.startOffset
                    && requestLength == msg.requestLength);
        }

        public override int GetHashCode()
        {
            return (pieceIndex.GetHashCode()
                    ^ requestLength.GetHashCode()
                    ^ startOffset.GetHashCode());
        }

        #endregion
    }
}