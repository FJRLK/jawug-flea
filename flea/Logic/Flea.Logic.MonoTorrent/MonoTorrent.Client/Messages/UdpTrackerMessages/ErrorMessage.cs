using System.Text;

namespace MonoTorrent.Client.Messages.UdpTracker
{
    public class ErrorMessage : UdpTrackerMessage
    {
        #region Internals

        string errorMessage;

        public override int ByteLength
        {
            get { return 4 + 4 + Encoding.ASCII.GetByteCount(errorMessage); }
        }

        public string Error
        {
            get { return errorMessage; }
        }

        #endregion

        #region Constructor

        public ErrorMessage()
            : this(0, "")
        {
        }

        public ErrorMessage(int transactionId, string error)
            : base(3, transactionId)
        {
            errorMessage = error;
        }

        #endregion

        #region Members

        public override void Decode(byte[] buffer, int offset, int length)
        {
            if (Action != ReadInt(buffer, ref offset))
                ThrowInvalidActionException();
            TransactionId = ReadInt(buffer, ref offset);
            errorMessage = ReadString(buffer, ref offset, length - offset);
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, Action);
            written += Write(buffer, written, TransactionId);
            written += WriteAscii(buffer, written, errorMessage);

            return written - offset;
            ;
        }

        #endregion
    }
}