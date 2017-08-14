using System;

namespace MonoTorrent.Common
{
    public class SpeedMonitor
    {
        #region Static

        private const int DefaultAveragePeriod = 12;

        #endregion

        #region Internals

        private DateTime lastUpdated;
        private int speed;
        private int[] speeds;
        private int speedsIndex;
        private long tempRecvCount;

        private long total;


        public int Rate
        {
            get { return speed; }
        }

        public long Total
        {
            get { return total; }
        }

        #endregion

        #region Constructor

        public SpeedMonitor()
            : this(DefaultAveragePeriod)
        {
        }

        public SpeedMonitor(int averagingPeriod)
        {
            if (averagingPeriod < 0)
                throw new ArgumentOutOfRangeException("averagingPeriod");

            lastUpdated = DateTime.UtcNow;
            speeds = new int[Math.Max(1, averagingPeriod)];
            speedsIndex = -speeds.Length;
        }

        #endregion

        #region Members

        public void AddDelta(int speed)
        {
            total += speed;
            tempRecvCount += speed;
        }

        public void AddDelta(long speed)
        {
            total += speed;
            tempRecvCount += speed;
        }

        public void Reset()
        {
            total = 0;
            speed = 0;
            tempRecvCount = 0;
            lastUpdated = DateTime.UtcNow;
            speedsIndex = -speeds.Length;
        }

        private void TimePeriodPassed(int difference)
        {
            int currSpeed = (int) (tempRecvCount*1000/difference);
            tempRecvCount = 0;

            int speedsCount;
            if (speedsIndex < 0)
            {
                //speeds array hasn't been filled yet

                int idx = speeds.Length + speedsIndex;

                speeds[idx] = currSpeed;
                speedsCount = idx + 1;

                speedsIndex++;
            }
            else
            {
                //speeds array is full, keep wrapping around overwriting the oldest value
                speeds[speedsIndex] = currSpeed;
                speedsCount = speeds.Length;

                speedsIndex = (speedsIndex + 1)%speeds.Length;
            }

            int total = speeds[0];
            for (int i = 1; i < speedsCount; i++)
                total += speeds[i];

            speed = total/speedsCount;
        }


        public void Tick()
        {
            DateTime old = lastUpdated;
            lastUpdated = DateTime.UtcNow;
            int difference = (int) (lastUpdated - old).TotalMilliseconds;

            if (difference > 800)
                TimePeriodPassed(difference);
        }

        // Used purely for unit testing purposes.
        internal void Tick(int difference)
        {
            lastUpdated = DateTime.UtcNow;
            TimePeriodPassed(difference);
        }

        #endregion
    }
}