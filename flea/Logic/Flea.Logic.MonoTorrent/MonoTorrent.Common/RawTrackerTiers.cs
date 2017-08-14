using System.Collections;
using System.Collections.Generic;
using MonoTorrent.BEncoding;

namespace MonoTorrent
{
    public class RawTrackerTiers : IList<RawTrackerTier>
    {
        #region Internals

        public int Count
        {
            get { return Tiers.Count; }
        }

        public bool IsReadOnly
        {
            get { return Tiers.IsReadOnly; }
        }

        public RawTrackerTier this[int index]
        {
            get { return new RawTrackerTier((BEncodedList) Tiers[index]); }
            set { Tiers[index] = value.Tier; }
        }

        BEncodedList Tiers { get; set; }

        #endregion

        #region Constructor

        public RawTrackerTiers()
            : this(new BEncodedList())
        {
        }

        public RawTrackerTiers(BEncodedList tiers)
        {
            Tiers = tiers;
        }

        #endregion

        #region Members

        public void Add(RawTrackerTier item)
        {
            Tiers.Add(item.Tier);
        }

        public void Clear()
        {
            Tiers.Clear();
        }

        public bool Contains(RawTrackerTier item)
        {
            return IndexOf(item) != -1;
        }

        public void CopyTo(RawTrackerTier[] array, int arrayIndex)
        {
            foreach (RawTrackerTier v in this)
                array[arrayIndex ++] = v;
        }

        public bool Remove(RawTrackerTier item)
        {
            int index = IndexOf(item);
            if (index != -1)
                RemoveAt(index);

            return index != -1;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<RawTrackerTier> GetEnumerator()
        {
            foreach (BEncodedValue v in Tiers)
                yield return new RawTrackerTier((BEncodedList) v);
        }

        public int IndexOf(RawTrackerTier item)
        {
            if (item != null)
            {
                for (int i = 0; i < Tiers.Count; i++)
                    if (item.Tier == Tiers[i])
                        return i;
            }
            return -1;
        }

        public void Insert(int index, RawTrackerTier item)
        {
            Tiers.Insert(index, item.Tier);
        }

        public void RemoveAt(int index)
        {
            Tiers.RemoveAt(index);
        }

        public void AddRange(IEnumerable<RawTrackerTier> tiers)
        {
            foreach (RawTrackerTier v in tiers)
                Add(v);
        }

        #endregion
    }
}