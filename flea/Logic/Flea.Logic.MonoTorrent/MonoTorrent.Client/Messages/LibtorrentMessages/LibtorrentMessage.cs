using System;
using System.Collections.Generic;

namespace MonoTorrent.Client.Messages.Libtorrent
{
    public abstract class ExtensionMessage : PeerMessage
    {
        #region Static

        private static Dictionary<byte, CreateMessage> messageDict;
        internal static readonly byte MessageId = 20;
        private static byte nextId;

        internal static readonly List<ExtensionSupport> SupportedMessages = new List<ExtensionSupport>();

        #endregion

        #region Internals

        private byte extensionId;

        public byte ExtensionId
        {
            get { return extensionId; }
            protected set { extensionId = value; }
        }

        #endregion

        #region Constructor

        static ExtensionMessage()
        {
            messageDict = new Dictionary<byte, CreateMessage>();

            Register(nextId++, delegate { return new ExtendedHandshakeMessage(); });

            Register(nextId, delegate { return new LTChat(); });
            SupportedMessages.Add(new ExtensionSupport("LT_chat", nextId++));

            Register(nextId, delegate { return new LTMetadata(); });
            SupportedMessages.Add(new ExtensionSupport("ut_metadata", nextId++));

            Register(nextId, delegate { return new PeerExchangeMessage(); });
            SupportedMessages.Add(new ExtensionSupport("ut_pex", nextId++));
        }

        public ExtensionMessage(byte messageId)
        {
            extensionId = messageId;
        }

        #endregion

        #region Members

        public static void Register(byte identifier, CreateMessage creator)
        {
            if (creator == null)
                throw new ArgumentNullException("creator");

            lock (messageDict)
                messageDict.Add(identifier, creator);
        }

        protected static ExtensionSupport CreateSupport(string name)
        {
            return SupportedMessages.Find(delegate(ExtensionSupport s) { return s.Name == name; });
        }

        public new static PeerMessage DecodeMessage(byte[] buffer, int offset, int count, TorrentManager manager)
        {
            CreateMessage creator;
            PeerMessage message;

            if (!ClientEngine.SupportsExtended)
                throw new MessageException("Extension messages are not supported");

            if (!messageDict.TryGetValue(buffer[offset], out creator))
                throw new ProtocolException("Unknown extension message received");

            message = creator(manager);
            message.Decode(buffer, offset + 1, count - 1);
            return message;
        }

        #endregion
    }
}