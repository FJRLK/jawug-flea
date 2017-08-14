using System.Text;

namespace MonoTorrent.Client.Messages.FastPeer
{
    public class AllowedFastMessage : PeerMessage, IFastPeerMessage
    {
        #region Static

        internal static readonly byte MessageId = 0x11;

        #endregion

        #region Internals

        private readonly int messageLength = 5;
        private int pieceIndex;

        public override int ByteLength
        {
            get { return messageLength + 4; }
        }

        public int PieceIndex
        {
            get { return pieceIndex; }
        }

        #endregion

        #region Constructor

        internal AllowedFastMessage()
        {
        }

        internal AllowedFastMessage(int pieceIndex)
        {
            this.pieceIndex = pieceIndex;
        }

        #endregion

        #region Members

        public override int Encode(byte[] buffer, int offset)
        {
            if (!ClientEngine.SupportsFastPeer)
                throw new ProtocolException("Message encoding not supported");

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

            pieceIndex = ReadInt(buffer, offset);
        }

        public override bool Equals(object obj)
        {
            AllowedFastMessage msg = obj as AllowedFastMessage;
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
            sb.Append("AllowedFast");
            sb.Append(" Index: ");
            sb.Append(pieceIndex);
            return sb.ToString();
        }

        #endregion
    }
}