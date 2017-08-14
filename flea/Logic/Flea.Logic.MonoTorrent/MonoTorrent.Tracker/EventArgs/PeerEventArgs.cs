using System;

namespace MonoTorrent.Tracker
{
    public abstract class PeerEventArgs : EventArgs
    {
        #region Internals

        private Peer peer;
        private SimpleTorrentManager torrent;

        public Peer Peer
        {
            get { return peer; }
        }

        public SimpleTorrentManager Torrent
        {
            get { return torrent; }
        }

        #endregion

        #region Constructor

        protected PeerEventArgs(Peer peer, SimpleTorrentManager torrent)
        {
            this.peer = peer;
            this.torrent = torrent;
        }

        #endregion
    }
}