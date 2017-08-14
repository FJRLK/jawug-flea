namespace MonoTorrent.Client.Messages.Standard
{
    public class UnchokeMessage : PeerMessage
    {
        #region Static

        private const int messageLength = 1;
        internal static readonly byte MessageId = 1;

        #endregion

        #region Internals

        public override int ByteLength
        {
            get { return (messageLength + 4); }
        }

        #endregion

        #region Constructor

        public UnchokeMessage()
        {
        }

        #endregion

        #region Members

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, messageLength);
            written += Write(buffer, written, MessageId);

            return CheckWritten(written - offset);
        }

        public override void Decode(byte[] buffer, int offset, int length)
        {
            // No decoding needed
        }

        public override string ToString()
        {
            return "UnChokeMessage";
        }

        public override bool Equals(object obj)
        {
            return (obj is UnchokeMessage);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        #endregion
    }
}