using System;

namespace MonoTorrent.TorrentWatcher
{
    /// <summary>
    ///     Provides the data needed to handle a TorrentWatcher event
    /// </summary>
    public class TorrentWatcherEventArgs : EventArgs
    {
        #region Internals

        private string torrentPath;

        /// <summary>
        ///     The path of the torrent
        /// </summary>
        public string TorrentPath
        {
            get { return torrentPath; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Creates a new TorrentWatcherEventArgs
        /// </summary>
        /// <param name="torrent">The torrent which is affected</param>
        public TorrentWatcherEventArgs(string torrentPath)
        {
            this.torrentPath = torrentPath;
        }

        #endregion
    }
}