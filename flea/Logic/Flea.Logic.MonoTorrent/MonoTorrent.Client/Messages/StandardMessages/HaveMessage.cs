using System.Text;

namespace MonoTorrent.Client.Messages.Standard
{
    /// <summary>
    ///     Represents a "Have" message
    /// </summary>
    public class HaveMessage : PeerMessage
    {
        #region Static

        private const int messageLength = 5;
        internal static readonly byte MessageId = 4;

        #endregion

        #region Internals

        private int pieceIndex;

        /// <summary>
        ///     Returns the length of the message in bytes
        /// </summary>
        public override int ByteLength
        {
            get { return (messageLength + 4); }
        }

        /// <summary>
        ///     The index of the piece that you "have"
        /// </summary>
        public int PieceIndex
        {
            get { return pieceIndex; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Creates a new HaveMessage
        /// </summary>
        public HaveMessage()
        {
        }


        /// <summary>
        ///     Creates a new HaveMessage
        /// </summary>
        /// <param name="pieceIndex">The index of the piece that you "have"</param>
        public HaveMessage(int pieceIndex)
        {
            this.pieceIndex = pieceIndex;
        }

        #endregion

        #region Members

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, messageLength);
            written += Write(buffer, written, MessageId);
            written += Write(buffer, written, pieceIndex);

            return CheckWritten(written - offset);
        }

        public override void Decode(byte[] buffer, int offset, int length)
        {
            pieceIndex = ReadInt(buffer, offset);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("HaveMessage ");
            sb.Append(" Index ");
            sb.Append(pieceIndex);
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            HaveMessage msg = obj as HaveMessage;

            if (msg == null)
                return false;

            return (pieceIndex == msg.pieceIndex);
        }

        public override int GetHashCode()
        {
            return pieceIndex.GetHashCode();
        }

        #endregion
    }
}