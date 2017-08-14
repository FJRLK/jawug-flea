using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MonoTorrent.Common
{
    /// <summary>
    ///     This class is for represting the Peer's bitfield
    /// </summary>
    public class BitField : ICloneable, IEnumerable<bool>
    {
        #region Internals

        private int[] array;
        private int length;
        private int trueCount;

        internal bool AllFalse
        {
            get { return trueCount == 0; }
        }

        internal bool AllTrue
        {
            get { return trueCount == length; }
        }

        public bool this[int index]
        {
            get { return Get(index); }
            internal set { Set(index, value); }
        }

        public int Length
        {
            get { return length; }
        }

        public int LengthInBytes
        {
            get { return (length + 7)/8; } //8 bits in a byte.
        }

        public double PercentComplete
        {
            get { return (double) trueCount/length*100.0; }
        }

        public int TrueCount
        {
            get { return trueCount; }
        }

        #endregion

        #region Constructor

        public BitField(byte[] array, int length)
            : this(length)
        {
            FromArray(array, 0, array.Length);
        }

        public BitField(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");

            this.length = length;
            array = new int[(length + 31)/32];
        }

        public BitField(bool[] array)
        {
            length = array.Length;
            this.array = new int[(array.Length + 31)/32];
            for (int i = 0; i < array.Length; i++)
                Set(i, array[i]);
        }

        #endregion

        #region Members

        object ICloneable.Clone()
        {
            return Clone();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<bool> GetEnumerator()
        {
            for (int i = 0; i < length; i++)
                yield return Get(i);
        }

        public BitField Clone()
        {
            BitField b = new BitField(length);
            Buffer.BlockCopy(array, 0, b.array, 0, array.Length*4);
            b.trueCount = trueCount;
            return b;
        }

        public BitField From(BitField value)
        {
            Check(value);
            Buffer.BlockCopy(value.array, 0, array, 0, array.Length*4);
            trueCount = value.trueCount;
            return this;
        }

        public BitField Not()
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = ~array[i];

            trueCount = length - trueCount;
            return this;
        }

        public BitField And(BitField value)
        {
            Check(value);

            for (int i = 0; i < array.Length; i++)
                array[i] &= value.array[i];

            Validate();
            return this;
        }

        internal BitField NAnd(BitField value)
        {
            Check(value);

            for (int i = 0; i < array.Length; i++)
                array[i] &= ~value.array[i];

            Validate();
            return this;
        }

        public BitField Or(BitField value)
        {
            Check(value);

            for (int i = 0; i < array.Length; i++)
                array[i] |= value.array[i];

            Validate();
            return this;
        }

        public BitField Xor(BitField value)
        {
            Check(value);

            for (int i = 0; i < array.Length; i++)
                array[i] ^= value.array[i];

            Validate();
            return this;
        }

        public override bool Equals(object obj)
        {
            BitField bf = obj as BitField;

            if (bf == null || array.Length != bf.array.Length || TrueCount != bf.TrueCount)
                return false;

            for (int i = 0; i < array.Length; i++)
                if (array[i] != bf.array[i])
                    return false;

            return true;
        }

        public int FirstTrue()
        {
            return FirstTrue(0, length);
        }

        public int FirstTrue(int startIndex, int endIndex)
        {
            int start;
            int end;

            // If the number of pieces is an exact multiple of 32, we need to decrement by 1 so we don't overrun the array
            // For the case when endIndex == 0, we need to ensure we don't go negative
            int loopEnd = Math.Min((endIndex/32), array.Length - 1);
            for (int i = (startIndex/32); i <= loopEnd; i++)
            {
                if (array[i] == 0) // This one has no true values
                    continue;

                start = i*32;
                end = start + 32;
                start = (start < startIndex) ? startIndex : start;
                end = (end > length) ? length : end;
                end = (end > endIndex) ? endIndex : end;
                if (end == Length && end > 0)
                    end--;

                for (int j = start; j <= end; j++)
                    if (Get(j)) // This piece is true
                        return j;
            }

            return -1; // Nothing is true
        }

        public int FirstFalse()
        {
            return FirstFalse(0, Length);
        }

        public int FirstFalse(int startIndex, int endIndex)
        {
            int start;
            int end;

            // If the number of pieces is an exact multiple of 32, we need to decrement by 1 so we don't overrun the array
            // For the case when endIndex == 0, we need to ensure we don't go negative
            int loopEnd = Math.Min((endIndex/32), array.Length - 1);
            for (int i = (startIndex/32); i <= loopEnd; i++)
            {
                if (array[i] == ~0) // This one has no false values
                    continue;

                start = i*32;
                end = start + 32;
                start = (start < startIndex) ? startIndex : start;
                end = (end > length) ? length : end;
                end = (end > endIndex) ? endIndex : end;
                if (end == Length && end > 0)
                    end--;

                for (int j = start; j <= end; j++)
                    if (!Get(j)) // This piece is true
                        return j;
            }

            return -1; // Nothing is true
        }

        internal void FromArray(byte[] buffer, int offset, int length)
        {
            int end = Length/32;
            for (int i = 0; i < end; i++)
                array[i] = (buffer[offset++] << 24) |
                           (buffer[offset++] << 16) |
                           (buffer[offset++] << 8) |
                           (buffer[offset++] << 0);

            int shift = 24;
            for (int i = end*32; i < Length; i += 8)
            {
                array[array.Length - 1] |= buffer[offset++] << shift;
                shift -= 8;
            }
            Validate();
        }

        bool Get(int index)
        {
            if (index < 0 || index >= length)
                throw new ArgumentOutOfRangeException("index");

            return (array[index >> 5] & (1 << (31 - (index & 31)))) != 0;
        }

        public override int GetHashCode()
        {
            int count = 0;
            for (int i = 0; i < array.Length; i++)
                count += array[i];

            return count;
        }

        public BitField Set(int index, bool value)
        {
            if (index < 0 || index >= length)
                throw new ArgumentOutOfRangeException("index");

            if (value)
            {
                if ((array[index >> 5] & (1 << (31 - (index & 31)))) == 0) // If it's not already true
                    trueCount++; // Increase true count
                array[index >> 5] |= (1 << (31 - index & 31));
            }
            else
            {
                if ((array[index >> 5] & (1 << (31 - (index & 31)))) != 0) // If it's not already false
                    trueCount--; // Decrease true count
                array[index >> 5] &= ~(1 << (31 - (index & 31)));
            }

            return this;
        }

        internal BitField SetTrue(params int[] indices)
        {
            foreach (int index in indices)
                Set(index, true);
            return this;
        }

        internal BitField SetFalse(params int[] indices)
        {
            foreach (int index in indices)
                Set(index, false);
            return this;
        }

        internal BitField SetAll(bool value)
        {
            if (value)
            {
                for (int i = 0; i < array.Length; i++)
                    array[i] = ~0;
                Validate();
            }

            else
            {
                for (int i = 0; i < array.Length; i++)
                    array[i] = 0;
                trueCount = 0;
            }

            return this;
        }

        internal byte[] ToByteArray()
        {
            byte[] data = new byte[LengthInBytes];
            ToByteArray(data, 0);
            return data;
        }

        internal void ToByteArray(byte[] buffer, int offset)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            ZeroUnusedBits();
            int end = Length/32;
            for (int i = 0; i < end; i++)
            {
                buffer[offset++] = (byte) (array[i] >> 24);
                buffer[offset++] = (byte) (array[i] >> 16);
                buffer[offset++] = (byte) (array[i] >> 8);
                buffer[offset++] = (byte) (array[i] >> 0);
            }

            int shift = 24;
            for (int i = end*32; i < Length; i += 8)
            {
                buffer[offset++] = (byte) (array[array.Length - 1] >> shift);
                shift -= 8;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(array.Length*16);
            for (int i = 0; i < Length; i++)
            {
                sb.Append(Get(i) ? 'T' : 'F');
                sb.Append(' ');
            }

            return sb.ToString(0, sb.Length - 1);
        }

        void Validate()
        {
            ZeroUnusedBits();

            // Update the population count
            uint count = 0;
            for (int i = 0; i < array.Length; i++)
            {
                uint v = (uint) array[i];
                v = v - ((v >> 1) & 0x55555555);
                v = (v & 0x33333333) + ((v >> 2) & 0x33333333);
                count += (((v + (v >> 4) & 0xF0F0F0F)*0x1010101)) >> 24;
            }
            trueCount = (int) count;
        }

        void ZeroUnusedBits()
        {
            if (array.Length == 0)
                return;

            // Zero the unused bits
            int shift = 32 - length%32;
            if (shift != 0)
                array[array.Length - 1] &= (-1 << shift);
        }

        void Check(BitField value)
        {
            MonoTorrent.Check.Value(value);
            if (length != value.length)
                throw new ArgumentException("BitFields are of different lengths", "value");
        }

        #endregion
    }
}