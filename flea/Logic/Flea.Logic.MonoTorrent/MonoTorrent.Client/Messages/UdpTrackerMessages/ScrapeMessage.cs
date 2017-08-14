using System.Collections.Generic;

namespace MonoTorrent.Client.Messages.UdpTracker
{
    public class ScrapeMessage : UdpTrackerMessage
    {
        #region Internals

        long connectionId;
        List<byte[]> infohashes;

        public override int ByteLength
        {
            get { return 8 + 4 + 4 + infohashes.Count*20; }
        }

        public List<byte[]> InfoHashes
        {
            get { return infohashes; }
        }

        #endregion

        #region Constructor

        public ScrapeMessage()
            : this(0, 0, new List<byte[]>())
        {
        }

        public ScrapeMessage(int transactionId, long connectionId, List<byte[]> infohashes)
            : base(2, transactionId)
        {
            this.connectionId = connectionId;
            this.infohashes = infohashes;
        }

        #endregion

        #region Members

        public override void Decode(byte[] buffer, int offset, int length)
        {
            connectionId = ReadLong(buffer, ref offset);
            if (Action != ReadInt(buffer, ref offset))
                throw new MessageException("Udp message decoded incorrectly");
            TransactionId = ReadInt(buffer, ref offset);
            while (offset <= (length - 20))
                infohashes.Add(ReadBytes(buffer, ref offset, 20));
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, connectionId);
            written += Write(buffer, written, Action);
            written += Write(buffer, written, TransactionId);
            for (int i = 0; i < infohashes.Count; i++)
                written += Write(buffer, written, infohashes[i]);

            return written - offset;
        }

        #endregion
    }
}