using System;
using System.IO;
using MonoTorrent.BEncoding;

namespace MonoTorrent.Client.Messages.Libtorrent
{
    internal class LTMetadata : ExtensionMessage
    {
        #region Types

        internal enum eMessageType
        {
            Request = 0,
            Data = 1,
            Reject = 2
        }

        #endregion

        #region Static

        internal static readonly int BlockSize = 16384; //16Kb
        private static readonly BEncodedString MessageTypeKey = "msg_type";
        private static readonly BEncodedString PieceKey = "piece";
        public static readonly ExtensionSupport Support = CreateSupport("ut_metadata");
        private static readonly BEncodedString TotalSizeKey = "total_size";

        #endregion

        #region Internals

        BEncodedDictionary dict;
        private eMessageType messageType;

        //this buffer contain all metadata when we send message 
        //and only a piece of metadata we receive message
        private byte[] metadata;
        private int piece;

        public override int ByteLength
        {
            // 4 byte length, 1 byte BT id, 1 byte LT id, 1 byte payload
            get
            {
                int length = 4 + 1 + 1 + dict.LengthInBytes();
                if (messageType == eMessageType.Data)
                    length += Math.Min(metadata.Length - piece*BlockSize, BlockSize);
                return length;
            }
        }

        internal eMessageType MetadataMessageType
        {
            get { return messageType; }
        }

        public byte[] MetadataPiece
        {
            get { return metadata; }
        }

        public int Piece
        {
            get { return piece; }
        }

        #endregion

        #region Constructor

        //only for register
        public LTMetadata()
            : base(Support.MessageId)
        {
        }

        public LTMetadata(PeerId id, eMessageType type, int piece)
            : this(id, type, piece, null)
        {
        }

        public LTMetadata(PeerId id, eMessageType type, int piece, byte[] metadata)
            : this(id.ExtensionSupports.MessageId(Support), type, piece, metadata)
        {
        }

        public LTMetadata(byte extensionId, eMessageType type, int piece, byte[] metadata)
            : this()
        {
            ExtensionId = extensionId;
            messageType = type;
            this.metadata = metadata;
            this.piece = piece;

            dict = new BEncodedDictionary();
            dict.Add(MessageTypeKey, (BEncodedNumber) (int) messageType);
            dict.Add(PieceKey, (BEncodedNumber) piece);

            if (messageType == eMessageType.Data)
            {
                Check.Metadata(metadata);
                dict.Add(TotalSizeKey, (BEncodedNumber) metadata.Length);
            }
        }

        #endregion

        #region Members

        public override void Decode(byte[] buffer, int offset, int length)
        {
            BEncodedValue val;
            using (RawReader reader = new RawReader(new MemoryStream(buffer, offset, length, false), false))
            {
                BEncodedDictionary d = BEncodedValue.Decode<BEncodedDictionary>(reader);
                int totalSize = 0;

                if (d.TryGetValue(MessageTypeKey, out val))
                    messageType = (eMessageType) ((BEncodedNumber) val).Number;
                if (d.TryGetValue(PieceKey, out val))
                    piece = (int) ((BEncodedNumber) val).Number;
                if (d.TryGetValue(TotalSizeKey, out val))
                {
                    totalSize = (int) ((BEncodedNumber) val).Number;
                    metadata = new byte[Math.Min(totalSize - piece*BlockSize, BlockSize)];
                    reader.Read(metadata, 0, metadata.Length);
                }
            }
        }

        public override int Encode(byte[] buffer, int offset)
        {
            if (!ClientEngine.SupportsFastPeer)
                throw new MessageException("Libtorrent extension messages not supported");

            int written = offset;

            written += Write(buffer, written, ByteLength - 4);
            written += Write(buffer, written, MessageId);
            written += Write(buffer, written, ExtensionId);
            written += dict.Encode(buffer, written);
            if (messageType == eMessageType.Data)
                written += Write(buffer, written, metadata, piece*BlockSize,
                    Math.Min(metadata.Length - piece*BlockSize, BlockSize));

            return CheckWritten(written - offset);
        }

        #endregion
    }
}