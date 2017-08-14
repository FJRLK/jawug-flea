namespace MonoTorrent.Client
{
    class DiskWriterLimiter : IRateLimiter
    {
        #region Internals

        DiskManager manager;

        public bool Unlimited
        {
            get { return manager.QueuedWrites < 20; }
        }

        #endregion

        #region Constructor

        public DiskWriterLimiter(DiskManager manager)
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
            // This is a simple on/off limiter which prevents
            // additional downloading if the diskwriter is backlogged
        }

        #endregion
    }
}