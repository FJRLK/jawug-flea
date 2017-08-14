using System.Text;

namespace MonoTorrent.Client.Messages.Standard
{
    public class RequestMessage : PeerMessage
    {
        #region Static

        internal const int MaxSize = 65536 + 64;
        private const int messageLength = 13;
        internal const int MinSize = 4096;
        internal static readonly byte MessageId = 6;

        #endregion

        #region Internals

        private int pieceIndex;
        private int requestLength;
        private int startOffset;

        public override int ByteLength
        {
            get { return (messageLength + 4); }
        }

        public int PieceIndex
        {
            get { return pieceIndex; }
        }

        public int RequestLength
        {
            get { return requestLength; }
        }

        public int StartOffset
        {
            get { return startOffset; }
        }

        #endregion

        #region Constructor

        public RequestMessage()
        {
        }

        public RequestMessage(int pieceIndex, int startOffset, int requestLength)
        {
            this.pieceIndex = pieceIndex;
            this.startOffset = startOffset;
            this.requestLength = requestLength;
        }

        #endregion

        #region Members

        public override void Decode(byte[] buffer, int offset, int length)
        {
            pieceIndex = ReadInt(buffer, ref offset);
            startOffset = ReadInt(buffer, ref offset);
            requestLength = ReadInt(buffer, ref offset);
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, messageLength);
            written += Write(buffer, written, MessageId);
            written += Write(buffer, written, pieceIndex);
            written += Write(buffer, written, startOffset);
            written += Write(buffer, written, requestLength);

            return CheckWritten(written - offset);
        }

        public override bool Equals(object obj)
        {
            RequestMessage msg = obj as RequestMessage;
            return (msg == null)
                ? false
                : (pieceIndex == msg.pieceIndex
                   && startOffset == msg.startOffset
                   && requestLength == msg.requestLength);
        }

        public override int GetHashCode()
        {
            return (pieceIndex.GetHashCode() ^ requestLength.GetHashCode() ^ startOffset.GetHashCode());
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("RequestMessage ");
            sb.Append(" Index ");
            sb.Append(pieceIndex);
            sb.Append(" Offset ");
            sb.Append(startOffset);
            sb.Append(" Length ");
            sb.Append(requestLength);
            return sb.ToString();
        }

        #endregion
    }
}