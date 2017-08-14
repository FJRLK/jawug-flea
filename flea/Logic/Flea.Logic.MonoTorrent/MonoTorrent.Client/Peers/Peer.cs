using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using MonoTorrent.BEncoding;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    public class Peer
    {
        #region Internals

        private int cleanedUpCount;
        private Uri connectionUri;
        private EncryptionTypes encryption;
        private int failedConnectionAttempts;
        private bool isSeeder;
        private DateTime lastConnectionAttempt;
        private int localPort;
        private string peerId;
        private int repeatedHashFails;
        private int totalHashFails;

        internal int CleanedUpCount
        {
            get { return cleanedUpCount; }
            set { cleanedUpCount = value; }
        }

        public Uri ConnectionUri
        {
            get { return connectionUri; }
        }

        public EncryptionTypes Encryption
        {
            get { return encryption; }
            set { encryption = value; }
        }

        internal int FailedConnectionAttempts
        {
            get { return failedConnectionAttempts; }
            set { failedConnectionAttempts = value; }
        }

        internal bool IsSeeder
        {
            get { return isSeeder; }
            set { isSeeder = value; }
        }

        internal DateTime LastConnectionAttempt
        {
            get { return lastConnectionAttempt; }
            set { lastConnectionAttempt = value; }
        }

        internal int LocalPort
        {
            get { return localPort; }
            set { localPort = value; }
        }

        internal string PeerId
        {
            get { return peerId; }
            set { peerId = value; }
        }

        internal int RepeatedHashFails
        {
            get { return repeatedHashFails; }
        }

        internal int TotalHashFails
        {
            get { return totalHashFails; }
        }

        #endregion

        #region Constructor

        public Peer(string peerId, Uri connectionUri)
            : this(peerId, connectionUri, EncryptionTypes.All)
        {
        }

        public Peer(string peerId, Uri connectionUri, EncryptionTypes encryption)
        {
            if (peerId == null)
                throw new ArgumentNullException("peerId");
            if (connectionUri == null)
                throw new ArgumentNullException("connectionUri");

            this.connectionUri = connectionUri;
            this.encryption = encryption;
            this.peerId = peerId;
        }

        #endregion

        #region Members

        public override bool Equals(object obj)
        {
            return Equals(obj as Peer);
        }

        public bool Equals(Peer other)
        {
            if (other == null)
                return false;

            // FIXME: Don't compare the port, just compare the IP
            if (string.IsNullOrEmpty(peerId) && string.IsNullOrEmpty(other.peerId))
                return connectionUri.Host.Equals(other.connectionUri.Host);

            return peerId == other.peerId;
        }

        public override int GetHashCode()
        {
            return connectionUri.Host.GetHashCode();
        }

        public override string ToString()
        {
            return connectionUri.ToString();
        }

        internal byte[] CompactPeer()
        {
            byte[] data = new byte[6];
            CompactPeer(data, 0);
            return data;
        }

        internal void CompactPeer(byte[] data, int offset)
        {
            Buffer.BlockCopy(IPAddress.Parse(connectionUri.Host).GetAddressBytes(), 0, data, offset, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(((short) connectionUri.Port))), 0, data,
                offset + 4, 2);
        }

        internal void HashedPiece(bool succeeded)
        {
            if (succeeded && repeatedHashFails > 0)
                repeatedHashFails--;

            if (!succeeded)
            {
                repeatedHashFails++;
                totalHashFails++;
            }
        }

        public static MonoTorrentCollection<Peer> Decode(BEncodedList peers)
        {
            MonoTorrentCollection<Peer> list = new MonoTorrentCollection<Peer>(peers.Count);
            foreach (BEncodedValue value in peers)
            {
                try
                {
                    if (value is BEncodedDictionary)
                        list.Add(DecodeFromDict((BEncodedDictionary) value));
                    else if (value is BEncodedString)
                        foreach (Peer p in Decode((BEncodedString) value))
                            list.Add(p);
                }
                catch
                {
                    // If something is invalid and throws an exception, ignore it
                    // and continue decoding the rest of the peers
                }
            }
            return list;
        }

        private static Peer DecodeFromDict(BEncodedDictionary dict)
        {
            string peerId;

            if (dict.ContainsKey("peer id"))
                peerId = dict["peer id"].ToString();
            else if (dict.ContainsKey("peer_id")) // HACK: Some trackers return "peer_id" instead of "peer id"
                peerId = dict["peer_id"].ToString();
            else
                peerId = string.Empty;

            Uri connectionUri = new Uri("tcp://" + dict["ip"].ToString() + ":" + dict["port"].ToString());
            return new Peer(peerId, connectionUri, EncryptionTypes.All);
        }

        public static MonoTorrentCollection<Peer> Decode(BEncodedString peers)
        {
            // "Compact Response" peers are encoded in network byte order. 
            // IP's are the first four bytes
            // Ports are the following 2 bytes
            byte[] byteOrderedData = peers.TextBytes;
            int i = 0;
            ushort port;
            StringBuilder sb = new StringBuilder(27);
            MonoTorrentCollection<Peer> list = new MonoTorrentCollection<Peer>((byteOrderedData.Length/6) + 1);
            while ((i + 5) < byteOrderedData.Length)
            {
                sb.Remove(0, sb.Length);

                sb.Append("tcp://");
                sb.Append(byteOrderedData[i++]);
                sb.Append('.');
                sb.Append(byteOrderedData[i++]);
                sb.Append('.');
                sb.Append(byteOrderedData[i++]);
                sb.Append('.');
                sb.Append(byteOrderedData[i++]);

                port = (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(byteOrderedData, i));
                i += 2;
                sb.Append(':');
                sb.Append(port);

                Uri uri = new Uri(sb.ToString());
                list.Add(new Peer("", uri, EncryptionTypes.All));
            }

            return list;
        }

        internal static BEncodedList Encode(IEnumerable<Peer> peers)
        {
            BEncodedList list = new BEncodedList();
            foreach (Peer p in peers)
                list.Add((BEncodedString) p.CompactPeer());
            return list;
        }

        #endregion
    }
}