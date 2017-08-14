using System.Text;

namespace MonoTorrent.Client.Messages.FastPeer
{
    // FIXME: The only use for a SuggestPiece message is for when i load a piece into a Disk Cache and want to make use for it
    public class SuggestPieceMessage : PeerMessage, IFastPeerMessage
    {
        #region Static

        internal static readonly byte MessageId = 0x0D;

        #endregion

        #region Internals

        private readonly int messageLength = 5;
        private int pieceIndex;

        public override int ByteLength
        {
            get { return messageLength + 4; }
        }

        /// <summary>
        ///     The index of the suggested piece to request
        /// </summary>
        public int PieceIndex
        {
            get { return pieceIndex; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Creates a new SuggestPiece message
        /// </summary>
        public SuggestPieceMessage()
        {
        }


        /// <summary>
        ///     Creates a new SuggestPiece message
        /// </summary>
        /// <param name="pieceIndex">The suggested piece to download</param>
        public SuggestPieceMessage(int pieceIndex)
        {
            this.pieceIndex = pieceIndex;
        }

        #endregion

        #region Members

        public override int Encode(byte[] buffer, int offset)
        {
            if (!ClientEngine.SupportsFastPeer)
                throw new ProtocolException("Message decoding not supported");

            int written = offset;

            written += Write(buffer, written, messageLength);
            written += Write(buffer, written, MessageId);
            written += Write(buffer, written, pieceIndex);

            return CheckWritten(written - offset);
        }

        public override void Decode(byte[] buffer, int offset, int length)
        {
            if (!ClientEngine.SupportsFastPeer)
                throw new ProtocolException("Message decoding not supported");

            pieceIndex = ReadInt(buffer, ref offset);
        }

        public override bool Equals(object obj)
        {
            SuggestPieceMessage msg = obj as SuggestPieceMessage;
            if (msg == null)
                return false;

            return pieceIndex == msg.pieceIndex;
        }

        public override int GetHashCode()
        {
            return pieceIndex.GetHashCode();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(24);
            sb.Append("Suggest Piece");
            sb.Append(" Index: ");
            sb.Append(pieceIndex);
            return sb.ToString();
        }

        #endregion
    }
}