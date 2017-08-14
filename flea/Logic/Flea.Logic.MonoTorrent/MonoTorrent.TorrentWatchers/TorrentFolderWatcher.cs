using System;
using System.IO;

namespace MonoTorrent.TorrentWatcher
{
    public class TorrentFolderWatcher : ITorrentWatcher
    {
        #region Types

        public event EventHandler<TorrentWatcherEventArgs> TorrentFound;
        public event EventHandler<TorrentWatcherEventArgs> TorrentLost;

        #endregion

        #region Internals

        private string torrentDirectory;

        private FileSystemWatcher watcher;
        private string watchFilter;

        #endregion

        #region Constructor

        public TorrentFolderWatcher(string torrentDirectory, string watchFilter)
        {
            if (torrentDirectory == null)
                throw new ArgumentNullException("torrentDirectory");

            if (watchFilter == null)
                throw new ArgumentNullException("watchFilter");

            if (!Directory.Exists(torrentDirectory))
                Directory.CreateDirectory(torrentDirectory);

            this.torrentDirectory = torrentDirectory;
            this.watchFilter = watchFilter;
        }

        public TorrentFolderWatcher(DirectoryInfo torrentDirectory)
            : this(torrentDirectory.FullName, "*.torrent")
        {
        }

        #endregion

        #region Members

        public void ForceScan()
        {
            foreach (string path in Directory.GetFiles(torrentDirectory, watchFilter))
                RaiseTorrentFound(path);
        }

        public void Start()
        {
            if (watcher == null)
            {
                watcher = new FileSystemWatcher(torrentDirectory);
                watcher.Filter = watchFilter;
                //this.watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Created += new FileSystemEventHandler(OnCreated);
                watcher.Deleted += new FileSystemEventHandler(OnDeleted);
            }
            watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            watcher.EnableRaisingEvents = false;
        }

        /// <summary>Gets called when a File with .torrent extension was added to the torrentDirectory</summary>
        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            RaiseTorrentFound(e.FullPath);
        }

        /// <summary>Gets called when a File with .torrent extension was deleted from the torrentDirectory</summary>
        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            RaiseTorrentLost(e.FullPath);
        }

        protected virtual void RaiseTorrentFound(string path)
        {
            if (TorrentFound != null)
                TorrentFound(this, new TorrentWatcherEventArgs(path));
        }

        protected virtual void RaiseTorrentLost(string path)
        {
            if (TorrentLost != null)
                TorrentLost(this, new TorrentWatcherEventArgs(path));
        }

        #endregion
    }
}