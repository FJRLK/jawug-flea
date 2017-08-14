namespace MonoTorrent.Client.Messages.UdpTracker
{
    class ConnectResponseMessage : UdpTrackerMessage
    {
        #region Internals

        long connectionId;

        public override int ByteLength
        {
            get { return 8 + 4 + 4; }
        }

        public long ConnectionId
        {
            get { return connectionId; }
        }

        #endregion

        #region Constructor

        public ConnectResponseMessage()
            : this(0, 0)
        {
        }

        public ConnectResponseMessage(int transactionId, long connectionId)
            : base(0, transactionId)
        {
            this.connectionId = connectionId;
        }

        #endregion

        #region Members

        public override void Decode(byte[] buffer, int offset, int length)
        {
            if (Action != ReadInt(buffer, ref offset))
                ThrowInvalidActionException();
            TransactionId = ReadInt(buffer, ref offset);
            connectionId = ReadLong(buffer, ref offset);
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, Action);
            written += Write(buffer, written, TransactionId);
            written += Write(buffer, written, ConnectionId);

            return ByteLength;
        }

        #endregion
    }
}