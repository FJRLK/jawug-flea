using System;
using System.Threading;

namespace MonoTorrent.Common
{
    public class AsyncResult : IAsyncResult
    {
        #region Internals

        private object asyncState;
        private AsyncCallback callback;
        private bool completedSyncronously;
        private bool isCompleted;
        private Exception savedException;
        private ManualResetEvent waitHandle;

        public object AsyncState
        {
            get { return asyncState; }
        }

        WaitHandle IAsyncResult.AsyncWaitHandle
        {
            get { return waitHandle; }
        }

        protected internal ManualResetEvent AsyncWaitHandle
        {
            get { return waitHandle; }
        }

        internal AsyncCallback Callback
        {
            get { return callback; }
        }

        public bool CompletedSynchronously
        {
            get { return completedSyncronously; }
            protected internal set { completedSyncronously = value; }
        }

        public bool IsCompleted
        {
            get { return isCompleted; }
            protected internal set { isCompleted = value; }
        }

        protected internal Exception SavedException
        {
            get { return savedException; }
            set { savedException = value; }
        }

        #endregion

        #region Constructor

        public AsyncResult(AsyncCallback callback, object asyncState)
        {
            this.asyncState = asyncState;
            this.callback = callback;
            waitHandle = new ManualResetEvent(false);
        }

        #endregion

        #region Members

        protected internal void Complete()
        {
            Complete(savedException);
        }

        protected internal void Complete(Exception ex)
        {
            // Ensure we only complete once - Needed because in encryption there could be
            // both a pending send and pending receive so if there is an error, both will
            // attempt to complete the encryption handshake meaning this is called twice.
            if (isCompleted)
                return;

            savedException = ex;
            completedSyncronously = false;
            isCompleted = true;
            waitHandle.Set();

            if (callback != null)
                callback(this);
        }

        #endregion
    }
}