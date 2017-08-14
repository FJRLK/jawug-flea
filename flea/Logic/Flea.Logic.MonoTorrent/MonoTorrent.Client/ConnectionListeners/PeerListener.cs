using System;
using System.Net;
using MonoTorrent.Client.Connections;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    public abstract class PeerListener : Listener
    {
        #region Types

        public event EventHandler<NewConnectionEventArgs> ConnectionReceived;

        #endregion

        #region Constructor

        protected PeerListener(IPEndPoint endpoint)
            : base(endpoint)
        {
        }

        #endregion

        #region Members

        protected virtual void RaiseConnectionReceived(Peer peer, IConnection connection, TorrentManager manager)
        {
            if (ConnectionReceived != null)
                Toolbox.RaiseAsyncEvent<NewConnectionEventArgs>(ConnectionReceived, this,
                    new NewConnectionEventArgs(peer, connection, manager));
        }

        #endregion
    }
}