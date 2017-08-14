using System;
using System.Collections.Generic;
using System.Threading;
using Mono.Ssdp.Internal;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    public delegate void MainLoopResult(object result);

    public delegate object MainLoopJob();

    public delegate void MainLoopTask();

    public delegate bool TimeoutTask();

    public class MainLoop
    {
        #region Types

        private class DelegateTask : ICacheable
        {
            #region Internals

            private ManualResetEvent handle;
            private bool isBlocking;
            private MainLoopJob job;
            private object jobResult;
            private Exception storedException;
            private MainLoopTask task;
            private TimeoutTask timeout;
            private bool timeoutResult;

            public bool IsBlocking
            {
                get { return isBlocking; }
                set { isBlocking = value; }
            }

            public MainLoopJob Job
            {
                get { return job; }
                set { job = value; }
            }

            public object JobResult
            {
                get { return jobResult; }
            }

            public Exception StoredException
            {
                get { return storedException; }
                set { storedException = value; }
            }

            public MainLoopTask Task
            {
                get { return task; }
                set { task = value; }
            }

            public TimeoutTask Timeout
            {
                get { return timeout; }
                set { timeout = value; }
            }

            public bool TimeoutResult
            {
                get { return timeoutResult; }
            }

            public ManualResetEvent WaitHandle
            {
                get { return handle; }
            }

            #endregion

            #region Constructor

            public DelegateTask()
            {
                handle = new ManualResetEvent(false);
            }

            #endregion

            #region Members

            public void Initialise()
            {
                isBlocking = false;
                job = null;
                jobResult = null;
                storedException = null;
                task = null;
                timeout = null;
                timeoutResult = false;
            }

            public void Execute()
            {
                try
                {
                    if (job != null)
                        jobResult = job();
                    else if (task != null)
                        task();
                    else if (timeout != null)
                        timeoutResult = timeout();
                }
                catch (Exception ex)
                {
                    storedException = ex;

                    // FIXME: I assume this case can't happen. The only user interaction
                    // with the mainloop is with blocking tasks. Internally it's a big bug
                    // if i allow an exception to propagate to the mainloop.
                    if (!IsBlocking)
                        throw;
                }
                finally
                {
                    handle.Set();
                }
            }

            #endregion
        }

        #endregion

        #region Internals

        ICache<DelegateTask> cache = new Cache<DelegateTask>(true).Synchronize();

        TimeoutDispatcher dispatcher = new TimeoutDispatcher();
        AutoResetEvent handle = new AutoResetEvent(false);
        Queue<DelegateTask> tasks = new Queue<DelegateTask>();
        internal Thread thread;

        #endregion

        #region Constructor

        public MainLoop(string name)
        {
            thread = new Thread(Loop);
            thread.IsBackground = true;
            thread.Start();
        }

        #endregion

        #region Members

        void Loop()
        {
            while (true)
            {
                DelegateTask task = null;

                lock (tasks)
                {
                    if (tasks.Count > 0)
                        task = tasks.Dequeue();
                }

                if (task == null)
                {
                    handle.WaitOne();
                }
                else
                {
                    bool reuse = !task.IsBlocking;
                    task.Execute();
                    if (reuse)
                        cache.Enqueue(task);
                }
            }
        }

        private void Queue(DelegateTask task)
        {
            Queue(task, Priority.Normal);
        }

        private void Queue(DelegateTask task, Priority priority)
        {
            lock (tasks)
            {
                tasks.Enqueue(task);
                handle.Set();
            }
        }

        public void Queue(MainLoopTask task)
        {
            DelegateTask dTask = cache.Dequeue();
            dTask.Task = task;
            Queue(dTask);
        }

        public void QueueWait(MainLoopTask task)
        {
            DelegateTask dTask = cache.Dequeue();
            dTask.Task = task;
            try
            {
                QueueWait(dTask);
            }
            finally
            {
                cache.Enqueue(dTask);
            }
        }

        public object QueueWait(MainLoopJob task)
        {
            DelegateTask dTask = cache.Dequeue();
            dTask.Job = task;

            try
            {
                QueueWait(dTask);
                return dTask.JobResult;
            }
            finally
            {
                cache.Enqueue(dTask);
            }
        }

        private void QueueWait(DelegateTask t)
        {
            t.WaitHandle.Reset();
            t.IsBlocking = true;
            if (Thread.CurrentThread == thread)
                t.Execute();
            else
                Queue(t, Priority.Highest);

            t.WaitHandle.WaitOne();

            if (t.StoredException != null)
                throw new TorrentException("Exception in mainloop", t.StoredException);
        }

        public uint QueueTimeout(TimeSpan span, TimeoutTask task)
        {
            DelegateTask dTask = cache.Dequeue();
            dTask.Timeout = task;

            return dispatcher.Add(span, delegate
            {
                QueueWait(dTask);
                return dTask.TimeoutResult;
            });
        }

        public AsyncCallback Wrap(AsyncCallback callback)
        {
            return delegate(IAsyncResult result) { Queue(delegate { callback(result); }); };
        }

        #endregion
    }
}