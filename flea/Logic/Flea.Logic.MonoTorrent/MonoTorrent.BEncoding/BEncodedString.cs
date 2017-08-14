using System;
using System.Text;
using MonoTorrent.Client.Messages;
using MonoTorrent.Common;

namespace MonoTorrent.BEncoding
{
    /// <summary>
    ///     Class representing a BEncoded string
    /// </summary>
    public class BEncodedString : BEncodedValue, IComparable<BEncodedString>
    {
        #region Internals

        private byte[] textBytes;

        public string Hex
        {
            get { return BitConverter.ToString(TextBytes); }
        }

        /// <summary>
        ///     The value of the BEncodedString
        /// </summary>
        public string Text
        {
            get { return Encoding.UTF8.GetString(textBytes); }
            set { textBytes = Encoding.UTF8.GetBytes(value); }
        }

        /// <summary>
        ///     The underlying byte[] associated with this BEncodedString
        /// </summary>
        public byte[] TextBytes
        {
            get { return textBytes; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Create a new BEncodedString using UTF8 encoding
        /// </summary>
        public BEncodedString()
            : this(new byte[0])
        {
        }

        /// <summary>
        ///     Create a new BEncodedString using UTF8 encoding
        /// </summary>
        /// <param name="value"></param>
        public BEncodedString(char[] value)
            : this(Encoding.UTF8.GetBytes(value))
        {
        }

        /// <summary>
        ///     Create a new BEncodedString using UTF8 encoding
        /// </summary>
        /// <param name="value">Initial value for the string</param>
        public BEncodedString(string value)
            : this(Encoding.UTF8.GetBytes(value))
        {
        }


        /// <summary>
        ///     Create a new BEncodedString using UTF8 encoding
        /// </summary>
        /// <param name="value"></param>
        public BEncodedString(byte[] value)
        {
            textBytes = value;
        }

        #endregion

        #region Members

        public int CompareTo(BEncodedString other)
        {
            if (other == null)
                return 1;

            int difference = 0;
            int length = textBytes.Length > other.textBytes.Length ? other.textBytes.Length : textBytes.Length;

            for (int i = 0; i < length; i++)
                if ((difference = textBytes[i].CompareTo(other.textBytes[i])) != 0)
                    return difference;

            if (textBytes.Length == other.textBytes.Length)
                return 0;

            return textBytes.Length > other.textBytes.Length ? 1 : -1;
        }


        public static implicit operator BEncodedString(string value)
        {
            return new BEncodedString(value);
        }

        public static implicit operator BEncodedString(char[] value)
        {
            return new BEncodedString(value);
        }

        public static implicit operator BEncodedString(byte[] value)
        {
            return new BEncodedString(value);
        }


        /// <summary>
        ///     Encodes the BEncodedString to a byte[] using the supplied Encoding
        /// </summary>
        /// <param name="buffer">The buffer to encode the string to</param>
        /// <param name="offset">The offset at which to save the data to</param>
        /// <param name="e">The encoding to use</param>
        /// <returns>The number of bytes encoded</returns>
        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;
            written += Message.WriteAscii(buffer, written, textBytes.Length.ToString());
            written += Message.WriteAscii(buffer, written, ":");
            written += Message.Write(buffer, written, textBytes);
            return written - offset;
        }


        /// <summary>
        ///     Decodes a BEncodedString from the supplied StreamReader
        /// </summary>
        /// <param name="reader">The StreamReader containing the BEncodedString</param>
        internal override void DecodeInternal(RawReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            int letterCount;
            string length = string.Empty;

            while ((reader.PeekByte() != -1) && (reader.PeekByte() != ':')) // read in how many characters
                length += (char) reader.ReadByte(); // the string is

            if (reader.ReadByte() != ':') // remove the ':'
                throw new BEncodingException("Invalid data found. Aborting");

            if (!int.TryParse(length, out letterCount))
                throw new BEncodingException(
                    $"Invalid BEncodedString. Length was '{length}' instead of a number");

            textBytes = new byte[letterCount];
            if (reader.Read(textBytes, 0, letterCount) != letterCount)
                throw new BEncodingException("Couldn't decode string");
        }

        public override int LengthInBytes()
        {
            // The length is equal to the length-prefix + ':' + length of data
            int prefix = 1; // Account for ':'

            // Count the number of characters needed for the length prefix
            for (int i = textBytes.Length; i != 0; i = i/10)
                prefix += 1;

            if (textBytes.Length == 0)
                prefix++;

            return prefix + textBytes.Length;
        }

        public int CompareTo(object other)
        {
            return CompareTo(other as BEncodedString);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            BEncodedString other;
            if (obj is string)
                other = new BEncodedString((string) obj);
            else if (obj is BEncodedString)
                other = (BEncodedString) obj;
            else
                return false;

            return Toolbox.ByteMatch(textBytes, other.textBytes);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < textBytes.Length; i++)
                hash += textBytes[i];

            return hash;
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(textBytes);
        }

        #endregion
    }
}