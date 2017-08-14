using System.Collections.Generic;

namespace MonoTorrent.Common
{
    interface ICache<T>
    {
        #region Internals

        int Count { get; }

        #endregion

        #region Members

        T Dequeue();
        void Enqueue(T instance);

        #endregion
    }

    class Cache<T> : ICache<T>
        where T : class, ICacheable, new()
    {
        #region Internals

        bool autoCreate;
        Queue<T> cache;

        public int Count
        {
            get { return cache.Count; }
        }

        #endregion

        #region Constructor

        public Cache()
            : this(false)
        {
        }

        public Cache(bool autoCreate)
        {
            this.autoCreate = autoCreate;
            cache = new Queue<T>();
        }

        #endregion

        #region Members

        public T Dequeue()
        {
            if (cache.Count > 0)
                return cache.Dequeue();
            return autoCreate ? new T() : null;
        }

        public void Enqueue(T instance)
        {
            instance.Initialise();
            cache.Enqueue(instance);
        }

        public ICache<T> Synchronize()
        {
            return new SynchronizedCache<T>(this);
        }

        #endregion
    }

    class SynchronizedCache<T> : ICache<T>
    {
        #region Internals

        ICache<T> cache;

        public int Count
        {
            get { return cache.Count; }
        }

        #endregion

        #region Constructor

        public SynchronizedCache(ICache<T> cache)
        {
            Check.Cache(cache);
            this.cache = cache;
        }

        #endregion

        #region Members

        public T Dequeue()
        {
            lock (cache)
                return cache.Dequeue();
        }

        public void Enqueue(T instance)
        {
            lock (cache)
                cache.Enqueue(instance);
        }

        #endregion
    }
}