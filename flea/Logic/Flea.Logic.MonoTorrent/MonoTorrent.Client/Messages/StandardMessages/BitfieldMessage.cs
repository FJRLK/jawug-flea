using MonoTorrent.Common;

namespace MonoTorrent.Client.Messages.Standard
{
    /// <summary>
    /// </summary>
    public class BitfieldMessage : PeerMessage
    {
        #region Static

        internal static readonly byte MessageId = 5;

        #endregion

        #region Internals

        private BitField bitField;

        /// <summary>
        ///     The bitfield
        /// </summary>
        public BitField BitField
        {
            get { return bitField; }
        }

        /// <summary>
        ///     Returns the length of the message in bytes
        /// </summary>
        public override int ByteLength
        {
            get { return (bitField.LengthInBytes + 5); }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Creates a new BitfieldMessage
        /// </summary>
        /// <param name="length">The length of the bitfield</param>
        public BitfieldMessage(int length)
        {
            bitField = new BitField(length);
        }


        /// <summary>
        ///     Creates a new BitfieldMessage
        /// </summary>
        /// <param name="bitfield">The bitfield to use</param>
        public BitfieldMessage(BitField bitfield)
        {
            bitField = bitfield;
        }

        #endregion

        #region Members

        public override void Decode(byte[] buffer, int offset, int length)
        {
            bitField.FromArray(buffer, offset, length);
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, bitField.LengthInBytes + 1);
            written += Write(buffer, written, MessageId);
            bitField.ToByteArray(buffer, written);
            written += bitField.LengthInBytes;

            return CheckWritten(written - offset);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "BitfieldMessage";
        }

        public override bool Equals(object obj)
        {
            BitfieldMessage bf = obj as BitfieldMessage;
            if (bf == null)
                return false;

            return bitField.Equals(bf.bitField);
        }

        public override int GetHashCode()
        {
            return bitField.GetHashCode();
        }

        #endregion
    }
}