using System;
using System.Collections.Generic;

namespace MonoTorrent.Tracker
{
    public class ScrapeEventArgs : EventArgs
    {
        #region Internals

        private List<SimpleTorrentManager> torrents;

        public List<SimpleTorrentManager> Torrents
        {
            get { return torrents; }
        }

        #endregion

        #region Constructor

        public ScrapeEventArgs(List<SimpleTorrentManager> torrents)
        {
            this.torrents = torrents;
        }

        #endregion
    }
}