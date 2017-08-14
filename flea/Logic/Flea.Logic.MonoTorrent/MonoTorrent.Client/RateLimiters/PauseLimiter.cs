using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    class PauseLimiter : IRateLimiter
    {
        #region Internals

        TorrentManager manager;

        public bool Unlimited
        {
            get { return manager.State != TorrentState.Paused; }
        }

        #endregion

        #region Constructor

        public PauseLimiter(TorrentManager manager)
        {
            this.manager = manager;
        }

        #endregion

        #region Members

        public bool TryProcess(int amount)
        {
            return Unlimited;
        }

        public void UpdateChunks(int maxRate, int actualRate)
        {
            // This is a simple on/off limiter
        }

        #endregion
    }
}