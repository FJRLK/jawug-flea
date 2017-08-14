using System;
using System.Net;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    public interface IListener
    {
        #region Types

        event EventHandler<EventArgs> StatusChanged;

        #endregion

        #region Internals

        IPEndPoint Endpoint { get; }
        ListenerStatus Status { get; }

        #endregion

        #region Members

        void ChangeEndpoint(IPEndPoint port);
        void Start();
        void Stop();

        #endregion
    }
}