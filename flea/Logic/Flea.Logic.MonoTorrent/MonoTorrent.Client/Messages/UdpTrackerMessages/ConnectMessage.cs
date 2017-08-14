using System;
using System.Net;

namespace MonoTorrent.Client.Messages.UdpTracker
{
    public class ConnectMessage : UdpTrackerMessage
    {
        #region Internals

        private long connectionId;

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

        public ConnectMessage()
            : base(0, DateTime.Now.GetHashCode())
        {
            connectionId = IPAddress.NetworkToHostOrder(0x41727101980); // Init connectionId as per spec
        }

        #endregion

        #region Members

        public override void Decode(byte[] buffer, int offset, int length)
        {
            connectionId = ReadLong(buffer, ref offset);
            if (Action != ReadInt(buffer, ref offset))
                ThrowInvalidActionException();
            TransactionId = ReadInt(buffer, ref offset);
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, connectionId);
            written += Write(buffer, written, Action);
            written += Write(buffer, written, TransactionId);

            return written - offset;
        }

        #endregion
    }
}