using System;
using System.Net;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    public abstract class Listener : IListener
    {
        #region Types

        public event EventHandler<EventArgs> StatusChanged;

        #endregion

        #region Internals

        private IPEndPoint endpoint;
        private ListenerStatus status;

        public IPEndPoint Endpoint
        {
            get { return endpoint; }
        }

        public ListenerStatus Status
        {
            get { return status; }
        }

        #endregion

        #region Constructor

        protected Listener(IPEndPoint endpoint)
        {
            status = ListenerStatus.NotListening;
            this.endpoint = endpoint;
        }

        #endregion

        #region Members

        public void ChangeEndpoint(IPEndPoint endpoint)
        {
            this.endpoint = endpoint;
            if (Status == ListenerStatus.Listening)
            {
                Stop();
                Start();
            }
        }

        public abstract void Start();

        public abstract void Stop();

        protected virtual void RaiseStatusChanged(ListenerStatus status)
        {
            this.status = status;
            if (StatusChanged != null)
                Toolbox.RaiseAsyncEvent<EventArgs>(StatusChanged, this, EventArgs.Empty);
        }

        #endregion
    }
}