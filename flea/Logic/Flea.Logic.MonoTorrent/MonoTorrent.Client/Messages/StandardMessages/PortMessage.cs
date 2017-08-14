using System.Text;

namespace MonoTorrent.Client.Messages.Standard
{
    /// <summary>
    ///     Represents a "Port" message
    /// </summary>
    public class PortMessage : PeerMessage
    {
        #region Static

        private const int messageLength = 3;
        internal static readonly byte MessageId = 9;

        #endregion

        #region Internals

        private ushort port;

        public override int ByteLength
        {
            get { return (messageLength + 4); }
        }

        public int Port
        {
            get { return port; }
        }

        #endregion

        #region Constructor

        public PortMessage()
        {
        }

        public PortMessage(ushort port)
        {
            this.port = port;
        }

        #endregion

        #region Members

        public override void Decode(byte[] buffer, int offset, int length)
        {
            port = (ushort) ReadShort(buffer, ref offset);
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, messageLength);
            written += Write(buffer, written, MessageId);
            written += Write(buffer, written, port);

            return CheckWritten(written - offset);
        }

        public override bool Equals(object obj)
        {
            PortMessage msg = obj as PortMessage;
            return (msg == null) ? false : (port == msg.port);
        }

        public override int GetHashCode()
        {
            return port.GetHashCode();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("PortMessage ");
            sb.Append(" Port ");
            sb.Append(port);
            return sb.ToString();
        }

        #endregion
    }
}