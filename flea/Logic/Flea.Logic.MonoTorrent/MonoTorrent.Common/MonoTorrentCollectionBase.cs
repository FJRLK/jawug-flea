using System;
using System.Collections.Generic;

namespace MonoTorrent.Common
{
    public class MonoTorrentCollection<T> : List<T>, ICloneable
    {
        #region Constructor

        public MonoTorrentCollection()
            : base()
        {
        }

        public MonoTorrentCollection(IEnumerable<T> collection)
            : base(collection)
        {
        }

        public MonoTorrentCollection(int capacity)
            : base(capacity)
        {
        }

        #endregion

        #region Members

        object ICloneable.Clone()
        {
            return Clone();
        }

        public MonoTorrentCollection<T> Clone()
        {
            return new MonoTorrentCollection<T>(this);
        }

        public T Dequeue()
        {
            T result = this[0];
            RemoveAt(0);
            return result;
        }

        #endregion
    }
}