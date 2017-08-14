using System;

namespace MonoTorrent.Client
{
    public class TorrentEventArgs : EventArgs
    {
        #region Internals

        private TorrentManager torrentManager;


        public TorrentManager TorrentManager
        {
            get { return torrentManager; }
            protected set { torrentManager = value; }
        }

        #endregion

        #region Constructor

        public TorrentEventArgs(TorrentManager manager)
        {
            torrentManager = manager;
        }

        #endregion
    }
}