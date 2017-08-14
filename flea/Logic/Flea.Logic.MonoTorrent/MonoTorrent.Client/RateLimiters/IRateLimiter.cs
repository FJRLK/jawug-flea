namespace MonoTorrent.Client
{
    interface IRateLimiter
    {
        #region Internals

        bool Unlimited { get; }

        #endregion

        #region Members

        bool TryProcess(int amount);
        void UpdateChunks(int maxRate, int actualRate);

        #endregion
    }
}