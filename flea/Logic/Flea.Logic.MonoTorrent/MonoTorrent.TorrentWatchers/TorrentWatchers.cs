using MonoTorrent.Common;

namespace MonoTorrent.TorrentWatcher
{
    /// <summary>
    ///     Main controller class for ITorrentWatcher
    /// </summary>
    public class TorrentWatchers : MonoTorrentCollection<ITorrentWatcher>
    {
        #region Constructor

        /// <summary>
        /// </summary>
        /// <param name="settings"></param>
        public TorrentWatchers()
        {
        }

        #endregion

        #region Members

        /// <summary>
        /// </summary>
        public void StartAll()
        {
            for (int i = 0; i < Count; i++)
                this[i].Start();
        }


        /// <summary>
        /// </summary>
        public void StopAll()
        {
            for (int i = 0; i < Count; i++)
                this[i].Stop();
        }


        /// <summary>
        /// </summary>
        public void ForceScanAll()
        {
            for (int i = 0; i < Count; i++)
                this[i].ForceScan();
        }

        #endregion
    }
}