using System.Text;
using MonoTorrent.Client.Messages.Standard;

namespace MonoTorrent.Client.Messages.FastPeer
{
    public class RejectRequestMessage : PeerMessage, IFastPeerMessage
    {
        #region Static

        internal static readonly byte MessageId = 0x10;

        #endregion

        #region Internals

        public readonly int messageLength = 13;
        private int pieceIndex;
        private int requestLength;
        private int startOffset;

        public override int ByteLength
        {
            get { return messageLength + 4; }
        }

        /// <summary>
        ///     The index of the piece
        /// </summary>
        public int PieceIndex
        {
            get { return pieceIndex; }
        }

        /// <summary>
        ///     The length of the block of data
        /// </summary>
        public int RequestLength
        {
            get { return requestLength; }
        }

        /// <summary>
        ///     The offset in bytes of the block of data
        /// </summary>
        public int StartOffset
        {
            get { return startOffset; }
        }

        #endregion

        #region Constructor

        public RejectRequestMessage()
        {
        }


        public RejectRequestMessage(PieceMessage message)
            : this(message.PieceIndex, message.StartOffset, message.RequestLength)
        {
        }

        public RejectRequestMessage(RequestMessage message)
            : this(message.PieceIndex, message.StartOffset, message.RequestLength)
        {
        }

        public RejectRequestMessage(int pieceIndex, int startOffset, int requestLength)
        {
            this.pieceIndex = pieceIndex;
            this.startOffset = startOffset;
            this.requestLength = requestLength;
        }

        #endregion

        #region Members

        public override int Encode(byte[] buffer, int offset)
        {
            if (!ClientEngine.SupportsFastPeer)
                throw new ProtocolException("Message encoding not supported");

            int written = offset;

            written += Write(buffer, written, messageLength);
            written += Write(buffer, written, MessageId);
            written += Write(buffer, written, pieceIndex);
            written += Write(buffer, written, startOffset);
            written += Write(buffer, written, requestLength);

            return CheckWritten(written - offset);
        }


        public override void Decode(byte[] buffer, int offset, int length)
        {
            if (!ClientEngine.SupportsFastPeer)
                throw new ProtocolException("Message decoding not supported");

            pieceIndex = ReadInt(buffer, ref offset);
            startOffset = ReadInt(buffer, ref offset);
            requestLength = ReadInt(buffer, ref offset);
        }

        public override bool Equals(object obj)
        {
            RejectRequestMessage msg = obj as RejectRequestMessage;
            if (msg == null)
                return false;

            return (pieceIndex == msg.pieceIndex
                    && startOffset == msg.startOffset
                    && requestLength == msg.requestLength);
        }


        public override int GetHashCode()
        {
            return (pieceIndex.GetHashCode()
                    ^ requestLength.GetHashCode()
                    ^ startOffset.GetHashCode());
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(24);
            sb.Append("Reject Request");
            sb.Append(" Index: ");
            sb.Append(pieceIndex);
            sb.Append(" Offset: ");
            sb.Append(startOffset);
            sb.Append(" Length ");
            sb.Append(requestLength);
            return sb.ToString();
        }

        #endregion
    }
}