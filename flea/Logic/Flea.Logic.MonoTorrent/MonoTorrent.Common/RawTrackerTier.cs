using System.Collections;
using System.Collections.Generic;
using MonoTorrent.BEncoding;

namespace MonoTorrent
{
    public class RawTrackerTier : IList<string>
    {
        #region Internals

        public int Count
        {
            get { return Tier.Count; }
        }

        public bool IsReadOnly
        {
            get { return Tier.IsReadOnly; }
        }

        public string this[int index]
        {
            get { return ((BEncodedString) Tier[index]).Text; }
            set { Tier[index] = new BEncodedString(value); }
        }

        internal BEncodedList Tier { get; set; }

        #endregion

        #region Constructor

        public RawTrackerTier()
            : this(new BEncodedList())
        {
        }

        public RawTrackerTier(BEncodedList tier)
        {
            Tier = tier;
        }

        public RawTrackerTier(IEnumerable<string> announces)
            : this()
        {
            foreach (string v in announces)
                Add(v);
        }

        #endregion

        #region Members

        public void Add(string item)
        {
            Tier.Add((BEncodedString) item);
        }

        public void Clear()
        {
            Tier.Clear();
        }

        public bool Contains(string item)
        {
            return Tier.Contains((BEncodedString) item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            foreach (string s in this)
                array[arrayIndex ++] = s;
        }

        public bool Remove(string item)
        {
            return Tier.Remove((BEncodedString) item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<string> GetEnumerator()
        {
            foreach (BEncodedString v in Tier)
                yield return v.Text;
        }

        public int IndexOf(string item)
        {
            return Tier.IndexOf((BEncodedString) item);
        }

        public void Insert(int index, string item)
        {
            Tier.Insert(index, (BEncodedString) item);
        }

        public void RemoveAt(int index)
        {
            Tier.RemoveAt(index);
        }

        #endregion
    }
}