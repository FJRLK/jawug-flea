namespace MonoTorrent.Client.Messages.Standard
{
    /// <summary>
    ///     Represents a "NotInterested" message
    /// </summary>
    public class NotInterestedMessage : PeerMessage
    {
        #region Static

        private const int messageLength = 1;
        internal static readonly byte MessageId = 3;

        #endregion

        #region Internals

        /// <summary>
        ///     Returns the length of the message in bytes
        /// </summary>
        public override int ByteLength
        {
            get { return (messageLength + 4); }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Creates a new NotInterestedMessage
        /// </summary>
        public NotInterestedMessage()
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

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "NotInterestedMessage";
        }

        public override bool Equals(object obj)
        {
            return (obj is NotInterestedMessage);
        }

        public override int GetHashCode()
        {
            return (ToString().GetHashCode());
        }

        #endregion
    }
}