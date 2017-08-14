using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MonoTorrent.BEncoding
{
    /// <summary>
    ///     Class representing a BEncoded list
    /// </summary>
    public class BEncodedList : BEncodedValue, IList<BEncodedValue>
    {
        #region Internals

        private List<BEncodedValue> list;

        public int Count
        {
            get { return list.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public BEncodedValue this[int index]
        {
            get { return list[index]; }
            set { list[index] = value; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Create a new BEncoded List with default capacity
        /// </summary>
        public BEncodedList()
            : this(new List<BEncodedValue>())
        {
        }

        /// <summary>
        ///     Create a new BEncoded List with the supplied capacity
        /// </summary>
        /// <param name="capacity">The initial capacity</param>
        public BEncodedList(int capacity)
            : this(new List<BEncodedValue>(capacity))
        {
        }

        public BEncodedList(IEnumerable<BEncodedValue> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            this.list = new List<BEncodedValue>(list);
        }

        private BEncodedList(List<BEncodedValue> value)
        {
            list = value;
        }

        #endregion

        #region Members

        public void Add(BEncodedValue item)
        {
            list.Add(item);
        }

        public void Clear()
        {
            list.Clear();
        }

        public bool Contains(BEncodedValue item)
        {
            return list.Contains(item);
        }

        public void CopyTo(BEncodedValue[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public bool Remove(BEncodedValue item)
        {
            return list.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<BEncodedValue> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public int IndexOf(BEncodedValue item)
        {
            return list.IndexOf(item);
        }

        public void Insert(int index, BEncodedValue item)
        {
            list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }


        /// <summary>
        ///     Encodes the list to a byte[]
        /// </summary>
        /// <param name="buffer">The buffer to encode the list to</param>
        /// <param name="offset">The offset to start writing the data at</param>
        /// <returns></returns>
        public override int Encode(byte[] buffer, int offset)
        {
            int written = 0;
            buffer[offset] = (byte) 'l';
            written++;
            for (int i = 0; i < list.Count; i++)
                written += list[i].Encode(buffer, offset + written);
            buffer[offset + written] = (byte) 'e';
            written++;
            return written;
        }

        /// <summary>
        ///     Decodes a BEncodedList from the given StreamReader
        /// </summary>
        /// <param name="reader"></param>
        internal override void DecodeInternal(RawReader reader)
        {
            if (reader.ReadByte() != 'l') // Remove the leading 'l'
                throw new BEncodingException("Invalid data found. Aborting");

            while ((reader.PeekByte() != -1) && (reader.PeekByte() != 'e'))
                list.Add(Decode(reader));

            if (reader.ReadByte() != 'e') // Remove the trailing 'e'
                throw new BEncodingException("Invalid data found. Aborting");
        }

        /// <summary>
        ///     Returns the size of the list in bytes
        /// </summary>
        /// <returns></returns>
        public override int LengthInBytes()
        {
            int length = 0;

            length += 1; // Lists start with 'l'
            for (int i = 0; i < list.Count; i++)
                length += list[i].LengthInBytes();

            length += 1; // Lists end with 'e'
            return length;
        }

        public override bool Equals(object obj)
        {
            BEncodedList other = obj as BEncodedList;

            if (other == null)
                return false;

            for (int i = 0; i < list.Count; i++)
                if (!list[i].Equals(other.list[i]))
                    return false;

            return true;
        }


        public override int GetHashCode()
        {
            int result = 0;
            for (int i = 0; i < list.Count; i++)
                result ^= list[i].GetHashCode();

            return result;
        }


        public override string ToString()
        {
            return Encoding.UTF8.GetString(Encode());
        }

        public void AddRange(IEnumerable<BEncodedValue> collection)
        {
            list.AddRange(collection);
        }

        #endregion
    }
}