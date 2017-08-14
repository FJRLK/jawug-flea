using System.Collections.Generic;

namespace MonoTorrent.Client
{
    class RateLimiterGroup : IRateLimiter
    {
        #region Internals

        List<IRateLimiter> limiters;

        public bool Unlimited
        {
            get
            {
                for (int i = 0; i < limiters.Count; i++)
                    if (!limiters[i].Unlimited)
                        return false;
                return true;
            }
        }

        #endregion

        #region Constructor

        public RateLimiterGroup()
        {
            limiters = new List<IRateLimiter>();
        }

        #endregion

        #region Members

        public bool TryProcess(int amount)
        {
            for (int i = 0; i < limiters.Count; i++)
            {
                if (limiters[i].Unlimited)
                    continue;
                else if (!limiters[i].TryProcess(amount))
                    return false;
            }
            return true;
        }

        public void UpdateChunks(int maxRate, int actualRate)
        {
            for (int i = 0; i < limiters.Count; i++)
                limiters[i].UpdateChunks(maxRate, actualRate);
        }

        public void Add(IRateLimiter limiter)
        {
            Check.Limiter(limiter);
            limiters.Add(limiter);
        }

        public void Remove(IRateLimiter limiter)
        {
            Check.Limiter(limiter);
            limiters.Remove(limiter);
        }

        #endregion
    }
}