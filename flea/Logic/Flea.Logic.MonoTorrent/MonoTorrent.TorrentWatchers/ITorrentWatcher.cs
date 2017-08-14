using System;

namespace MonoTorrent.TorrentWatcher
{
    public interface ITorrentWatcher
    {
        #region Types

        event EventHandler<TorrentWatcherEventArgs> TorrentFound;
        event EventHandler<TorrentWatcherEventArgs> TorrentLost;

        #endregion

        #region Members

        void Start();
        void Stop();
        void ForceScan();

        #endregion
    }
}