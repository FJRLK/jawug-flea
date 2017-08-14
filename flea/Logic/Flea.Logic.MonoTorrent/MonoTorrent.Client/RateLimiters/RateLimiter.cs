using System.Threading;

namespace MonoTorrent.Client
{
    class RateLimiter : IRateLimiter
    {
        #region Internals

        int chunks;
        int savedError;
        bool unlimited;

        public bool Unlimited
        {
            get { return unlimited; }
        }

        #endregion

        #region Constructor

        public RateLimiter()
        {
            UpdateChunks(0, 0);
        }

        #endregion

        #region Members

        public bool TryProcess(int amount)
        {
            if (Unlimited)
                return true;

            int c;
            do
            {
                c = chunks;
                if (c < amount)
                    return false;
            } while (Interlocked.CompareExchange(ref chunks, c - amount, c) != c);
            return true;
        }

        public void UpdateChunks(int maxRate, int actualRate)
        {
            unlimited = maxRate == 0;
            if (unlimited)
                return;

            // From experimentation, i found that increasing by 5% gives more accuate rate limiting
            // for peer communications. For disk access and whatnot, a 5% overshoot is fine.
            maxRate = (int) (maxRate*1.05);
            int errorRateDown = maxRate - actualRate;
            int delta = (int) (0.4*errorRateDown + 0.6*savedError);
            savedError = errorRateDown;


            int increaseAmount = (int) ((maxRate + delta)/ConnectionManager.ChunkLength);
            Interlocked.Add(ref chunks, increaseAmount);
            if (chunks > (maxRate*1.2/ConnectionManager.ChunkLength))
                Interlocked.Exchange(ref chunks, (int) (maxRate*1.2/ConnectionManager.ChunkLength));

            if (chunks < (maxRate/ConnectionManager.ChunkLength/2))
                Interlocked.Exchange(ref chunks, (maxRate/ConnectionManager.ChunkLength/2));

            if (maxRate == 0)
                chunks = 0;
        }

        #endregion
    }
}