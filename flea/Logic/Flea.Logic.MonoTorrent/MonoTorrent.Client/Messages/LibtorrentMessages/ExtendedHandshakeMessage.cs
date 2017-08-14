using System.Collections.Generic;
using MonoTorrent.BEncoding;

namespace MonoTorrent.Client.Messages.Libtorrent
{
    public class ExtendedHandshakeMessage : ExtensionMessage
    {
        #region Static

        private static readonly BEncodedString MaxRequestKey = "reqq";
        private static readonly BEncodedString MetadataSizeKey = "metadata_size";
        private static readonly BEncodedString PortKey = "p";

        internal static readonly ExtensionSupport Support = new ExtensionSupport("LT_handshake", 0);
        private static readonly BEncodedString SupportsKey = "m";
        private static readonly BEncodedString VersionKey = "v";

        #endregion

        #region Internals

        private int localPort;
        private int maxRequests;
        private int metadataSize;
        private ExtensionSupports supports;
        private string version;

        public override int ByteLength
        {
            get
            {
                // FIXME Implement this properly

                // The length of the payload, 4 byte length prefix, 1 byte BT message id, 1 byte LT message id
                return Create().LengthInBytes() + 4 + 1 + 1;
            }
        }

        public int LocalPort
        {
            get { return localPort; }
        }

        public int MaxRequests
        {
            get { return maxRequests; }
        }

        public int MetadataSize
        {
            get { return metadataSize; }
        }

        public ExtensionSupports Supports
        {
            get { return supports; }
        }

        public string Version
        {
            get { return version ?? ""; }
        }

        #endregion

        #region Constructor

        public ExtendedHandshakeMessage()
            : base(Support.MessageId)
        {
            supports = new ExtensionSupports(SupportedMessages);
        }

        public ExtendedHandshakeMessage(int metadataSize)
            : this()
        {
            this.metadataSize = metadataSize;
        }

        #endregion

        #region Members

        public override void Decode(byte[] buffer, int offset, int length)
        {
            BEncodedValue val;
            BEncodedDictionary d = BEncodedValue.Decode<BEncodedDictionary>(buffer, offset, length, false);

            if (d.TryGetValue(MaxRequestKey, out val))
                maxRequests = (int) ((BEncodedNumber) val).Number;
            if (d.TryGetValue(VersionKey, out val))
                version = ((BEncodedString) val).Text;
            if (d.TryGetValue(PortKey, out val))
                localPort = (int) ((BEncodedNumber) val).Number;

            LoadSupports((BEncodedDictionary) d[SupportsKey]);

            if (d.TryGetValue(MetadataSizeKey, out val))
                metadataSize = (int) ((BEncodedNumber) val).Number;
        }

        private void LoadSupports(BEncodedDictionary supports)
        {
            ExtensionSupports list = new ExtensionSupports();
            foreach (KeyValuePair<BEncodedString, BEncodedValue> k in supports)
                list.Add(new ExtensionSupport(k.Key.Text, (byte) ((BEncodedNumber) k.Value).Number));

            this.supports = list;
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;
            BEncodedDictionary dict = Create();

            written += Write(buffer, written, dict.LengthInBytes() + 1 + 1);
            written += Write(buffer, written, MessageId);
            written += Write(buffer, written, Support.MessageId);
            written += dict.Encode(buffer, written);

            CheckWritten(written - offset);
            return written - offset;
        }

        private BEncodedDictionary Create()
        {
            if (!ClientEngine.SupportsExtended)
                throw new MessageException("Libtorrent extension messages not supported");

            BEncodedDictionary mainDict = new BEncodedDictionary();
            BEncodedDictionary supportsDict = new BEncodedDictionary();

            mainDict.Add(MaxRequestKey, (BEncodedNumber) maxRequests);
            mainDict.Add(VersionKey, (BEncodedString) Version);
            mainDict.Add(PortKey, (BEncodedNumber) localPort);

            SupportedMessages.ForEach(
                delegate(ExtensionSupport s) { supportsDict.Add(s.Name, (BEncodedNumber) s.MessageId); });
            mainDict.Add(SupportsKey, supportsDict);

            mainDict.Add(MetadataSizeKey, (BEncodedNumber) metadataSize);

            return mainDict;
        }

        #endregion
    }
}