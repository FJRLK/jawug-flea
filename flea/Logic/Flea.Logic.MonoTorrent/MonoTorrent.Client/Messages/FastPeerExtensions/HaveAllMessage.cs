namespace MonoTorrent.Client.Messages.FastPeer
{
    public class HaveAllMessage : PeerMessage, IFastPeerMessage
    {
        #region Static

        internal static readonly byte MessageId = 0x0E;

        #endregion

        #region Internals

        private readonly int messageLength = 1;

        public override int ByteLength
        {
            get { return messageLength + 4; }
        }

        #endregion

        #region Constructor

        public HaveAllMessage()
        {
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

            return CheckWritten(written - offset);
        }

        public override void Decode(byte[] buffer, int offset, int length)
        {
            if (!ClientEngine.SupportsFastPeer)
                throw new ProtocolException("Message decoding not supported");
        }

        public override bool Equals(object obj)
        {
            return obj is HaveAllMessage;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return "HaveAllMessage";
        }

        #endregion
    }
}