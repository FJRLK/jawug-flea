using System;
using System.Collections.Generic;
using System.Threading;

namespace Mono.Ssdp.Internal
{
    internal delegate bool TimeoutHandler(object state, ref TimeSpan interval);

    internal class TimeoutDispatcher : IDisposable
    {
        #region Types

        private struct TimeoutItem : IComparable<TimeoutItem>
        {
            public uint Id;
            public TimeSpan Timeout;
            public DateTime Trigger;
            public TimeoutHandler Handler;
            public object State;

            public int CompareTo(TimeoutItem item)
            {
                return Trigger.CompareTo(item.Trigger);
            }

            public override string ToString()
            {
                return $"{Id} ({Trigger})";
            }
        }

        #endregion

        #region Static

        private static uint timeout_ids = 1;

        #endregion

        #region Internals

        private bool disposed;

        private List<TimeoutItem> timeouts = new List<TimeoutItem>();
        private AutoResetEvent wait = new AutoResetEvent(false);

        #endregion

        #region Constructor

        public TimeoutDispatcher()
        {
            Thread t = new Thread(TimerThread);
            t.IsBackground = true;
            t.Start();
        }

        #endregion

        #region Members

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            Clear();
            wait.Close();
            disposed = true;
        }

        public uint Add(uint timeoutMs, TimeoutHandler handler)
        {
            return Add(timeoutMs, handler, null);
        }

        public uint Add(TimeSpan timeout, TimeoutHandler handler)
        {
            return Add(timeout, handler, null);
        }

        public uint Add(uint timeoutMs, TimeoutHandler handler, object state)
        {
            return Add(TimeSpan.FromMilliseconds(timeoutMs), handler, state);
        }

        public uint Add(TimeSpan timeout, TimeoutHandler handler, object state)
        {
            CheckDisposed();
            TimeoutItem item = new TimeoutItem();
            item.Id = timeout_ids++;
            item.Timeout = timeout;
            item.Trigger = DateTime.UtcNow + timeout;
            item.Handler = handler;
            item.State = state;

            Add(ref item);

            return item.Id;
        }

        private void Add(ref TimeoutItem item)
        {
            lock (timeouts)
            {
                int index = timeouts.BinarySearch(item);
                index = index >= 0 ? index : ~index;
                timeouts.Insert(index, item);

                if (index == 0)
                {
                    wait.Set();
                }
            }
        }

        public void Remove(uint id)
        {
            lock (timeouts)
            {
                CheckDisposed();
                // FIXME: Comparer for BinarySearch
                for (int i = 0; i < timeouts.Count; i++)
                {
                    if (timeouts[i].Id == id)
                    {
                        timeouts.RemoveAt(i);
                        if (i == 0)
                        {
                            wait.Set();
                        }
                        return;
                    }
                }
            }
        }

        private void Start()
        {
            wait.Reset();
        }

        private void TimerThread(object state)
        {
            bool hasItem;
            TimeoutItem item = default(TimeoutItem);

            while (true)
            {
                if (disposed)
                {
                    wait.Close();
                    return;
                }

                lock (timeouts)
                {
                    hasItem = timeouts.Count > 0;
                    if (hasItem)
                        item = timeouts[0];
                }

                TimeSpan interval = hasItem ? item.Trigger - DateTime.UtcNow : TimeSpan.FromMilliseconds(-1);
                if (hasItem && interval < TimeSpan.Zero)
                {
                    interval = TimeSpan.Zero;
                }

                if (!wait.WaitOne(interval, false) && hasItem)
                {
                    bool requeue = item.Handler(item.State, ref item.Timeout);
                    lock (timeouts)
                    {
                        Remove(item.Id);
                        if (requeue)
                        {
                            item.Trigger += item.Timeout;
                            Add(ref item);
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            lock (timeouts)
            {
                timeouts.Clear();
                wait.Set();
            }
        }

        private void CheckDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(ToString());
            }
        }

        #endregion
    }
}