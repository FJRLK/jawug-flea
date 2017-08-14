using System.Text;
using MonoTorrent.Common;

namespace MonoTorrent.Client.Messages.Standard
{
    /// <summary>
    /// </summary>
    public class HandshakeMessage : PeerMessage
    {
        #region Static

        private const byte ExtendedMessagingFlag = 0x10;
        private const byte FastPeersFlag = 0x04;
        internal const int HandshakeLength = 68;
        private readonly static byte[] ZeroedBits = new byte[8];

        #endregion

        #region Internals

        private bool extended;
        internal InfoHash infoHash;
        private string peerId;
        private string protocolString;
        private int protocolStringLength;
        private bool supportsFastPeer;

        public override int ByteLength
        {
            get { return 68; }
        }


        /// <summary>
        ///     The infohash of the torrent
        /// </summary>
        public InfoHash InfoHash
        {
            get { return infoHash; }
        }


        /// <summary>
        ///     The ID of the peer
        /// </summary>
        public string PeerId
        {
            get { return peerId; }
        }


        /// <summary>
        ///     The protocol string to send
        /// </summary>
        public string ProtocolString
        {
            get { return protocolString; }
        }

        /// <summary>
        ///     The length of the protocol string
        /// </summary>
        public int ProtocolStringLength
        {
            get { return protocolStringLength; }
        }

        public bool SupportsExtendedMessaging
        {
            get { return extended; }
        }

        /// <summary>
        ///     True if the peer supports the Bittorrent FastPeerExtensions
        /// </summary>
        public bool SupportsFastPeer
        {
            get { return supportsFastPeer; }
        }

        #endregion

        #region Constructor

        public HandshakeMessage()
            : this(ClientEngine.SupportsFastPeer)
        {
        }

        /// <summary>
        ///     Creates a new HandshakeMessage
        /// </summary>
        public HandshakeMessage(bool enableFastPeer)
            : this(new InfoHash(new byte[20]), "", VersionInfo.ProtocolStringV100, enableFastPeer)
        {
        }

        public HandshakeMessage(InfoHash infoHash, string peerId, string protocolString)
            : this(infoHash, peerId, protocolString, ClientEngine.SupportsFastPeer, ClientEngine.SupportsExtended)
        {
        }

        public HandshakeMessage(InfoHash infoHash, string peerId, string protocolString, bool enableFastPeer)
            : this(infoHash, peerId, protocolString, enableFastPeer, ClientEngine.SupportsExtended)
        {
        }

        public HandshakeMessage(InfoHash infoHash, string peerId, string protocolString, bool enableFastPeer,
            bool enableExtended)
        {
            if (!ClientEngine.SupportsFastPeer && enableFastPeer)
                throw new ProtocolException("The engine does not support fast peer, but fast peer was requested");

            if (!ClientEngine.SupportsExtended && enableExtended)
                throw new ProtocolException("The engine does not support extended, but extended was requested");

            this.infoHash = infoHash;
            this.peerId = peerId;
            this.protocolString = protocolString;
            protocolStringLength = protocolString.Length;
            supportsFastPeer = enableFastPeer;
            extended = enableExtended;
        }

        #endregion

        #region Members

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, (byte) protocolString.Length);
            written += WriteAscii(buffer, written, protocolString);
            written += Write(buffer, written, ZeroedBits);

            if (SupportsExtendedMessaging)
                buffer[written - 3] |= ExtendedMessagingFlag;
            if (SupportsFastPeer)
                buffer[written - 1] |= FastPeersFlag;

            written += Write(buffer, written, infoHash.Hash);
            written += WriteAscii(buffer, written, peerId);

            return CheckWritten(written - offset);
        }

        public override void Decode(byte[] buffer, int offset, int length)
        {
            protocolStringLength = ReadByte(buffer, ref offset); // First byte is length

            // #warning Fix this hack - is there a better way of verifying the protocol string? Hack
            if (protocolStringLength != VersionInfo.ProtocolStringV100.Length)
                protocolStringLength = VersionInfo.ProtocolStringV100.Length;

            protocolString = ReadString(buffer, ref offset, protocolStringLength);
            CheckForSupports(buffer, ref offset);
            infoHash = new InfoHash(ReadBytes(buffer, ref offset, 20));
            peerId = ReadString(buffer, ref offset, 20);
        }

        private void CheckForSupports(byte[] buffer, ref int offset)
        {
            // Increment offset first so that the indices are consistent between Encoding and Decoding
            offset += 8;
            extended = (ExtendedMessagingFlag & buffer[offset - 3]) != 0;
            supportsFastPeer = (FastPeersFlag & buffer[offset - 1]) != 0;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("HandshakeMessage ");
            sb.Append(" PeerID ");
            sb.Append(peerId);
            sb.Append(" FastPeer ");
            sb.Append(supportsFastPeer);
            return sb.ToString();
        }


        public override bool Equals(object obj)
        {
            HandshakeMessage msg = obj as HandshakeMessage;

            if (msg == null)
                return false;

            if (infoHash != msg.infoHash)
                return false;

            return (peerId == msg.peerId
                    && protocolString == msg.protocolString
                    && supportsFastPeer == msg.supportsFastPeer);
        }


        public override int GetHashCode()
        {
            return (infoHash.GetHashCode() ^ peerId.GetHashCode() ^ protocolString.GetHashCode());
        }

        #endregion
    }
}