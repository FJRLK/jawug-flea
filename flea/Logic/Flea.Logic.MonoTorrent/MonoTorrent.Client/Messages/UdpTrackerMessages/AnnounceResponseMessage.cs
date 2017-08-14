using System;
using System.Collections.Generic;
using System.Net;

namespace MonoTorrent.Client.Messages.UdpTracker
{
    class AnnounceResponseMessage : UdpTrackerMessage
    {
        #region Internals

        TimeSpan interval;
        int leechers;
        List<Peer> peers;
        int seeders;

        public override int ByteLength
        {
            get { return (4*5 + peers.Count*6); }
        }

        public TimeSpan Interval
        {
            get { return interval; }
        }

        public int Leechers
        {
            get { return leechers; }
        }

        public List<Peer> Peers
        {
            get { return peers; }
        }

        public int Seeders
        {
            get { return seeders; }
        }

        #endregion

        #region Constructor

        public AnnounceResponseMessage()
            : this(0, TimeSpan.Zero, 0, 0, new List<Peer>())
        {
        }

        public AnnounceResponseMessage(int transactionId, TimeSpan interval, int leechers, int seeders, List<Peer> peers)
            : base(1, transactionId)
        {
            this.interval = interval;
            this.leechers = leechers;
            this.seeders = seeders;
            this.peers = peers;
        }

        #endregion

        #region Members

        public override void Decode(byte[] buffer, int offset, int length)
        {
            if (Action != ReadInt(buffer, offset))
                ThrowInvalidActionException();
            TransactionId = ReadInt(buffer, offset + 4);
            interval = TimeSpan.FromSeconds(ReadInt(buffer, offset + 8));
            leechers = ReadInt(buffer, offset + 12);
            seeders = ReadInt(buffer, offset + 16);

            LoadPeerDetails(buffer, 20);
        }

        private void LoadPeerDetails(byte[] buffer, int offset)
        {
            while (offset <= (buffer.Length - 6))
            {
                int ip = IPAddress.NetworkToHostOrder(ReadInt(buffer, ref offset));
                ushort port = (ushort) ReadShort(buffer, ref offset);
                peers.Add(new Peer("", new Uri("tcp://" + new IPEndPoint(new IPAddress(ip), port).ToString())));
            }
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, Action);
            written += Write(buffer, written, TransactionId);
            written += Write(buffer, written, (int) interval.TotalSeconds);
            written += Write(buffer, written, leechers);
            written += Write(buffer, written, seeders);

            for (int i = 0; i < peers.Count; i++)
                Peers[i].CompactPeer(buffer, written + (i*6));

            return written - offset;
        }

        #endregion
    }
}