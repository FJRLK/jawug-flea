using System;

namespace MonoTorrent.Client
{
    public class AttemptConnectionEventArgs : EventArgs
    {
        #region Internals

        private bool banPeer;
        private Peer peer;

        public bool BanPeer
        {
            get { return banPeer; }
            set { banPeer = value; }
        }

        public Peer Peer
        {
            get { return peer; }
        }

        #endregion

        #region Constructor

        public AttemptConnectionEventArgs(Peer peer)
        {
            this.peer = peer;
        }

        #endregion
    }
}