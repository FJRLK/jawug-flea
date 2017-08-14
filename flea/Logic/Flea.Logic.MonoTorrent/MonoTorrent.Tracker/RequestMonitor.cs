using MonoTorrent.Common;

namespace MonoTorrent.Tracker
{
    public class RequestMonitor
    {
        #region Internals

        private SpeedMonitor announces;
        private SpeedMonitor scrapes;

        public int AnnounceRate
        {
            get { return announces.Rate; }
        }

        public int ScrapeRate
        {
            get { return scrapes.Rate; }
        }

        public int TotalAnnounces
        {
            get { return (int) announces.Total; }
        }

        public int TotalScrapes
        {
            get { return (int) scrapes.Total; }
        }

        #endregion

        #region Constructor

        public RequestMonitor()
        {
            announces = new SpeedMonitor();
            scrapes = new SpeedMonitor();
        }

        #endregion

        #region Members

        internal void AnnounceReceived()
        {
            lock (announces)
                announces.AddDelta(1);
        }

        internal void ScrapeReceived()
        {
            lock (scrapes)
                scrapes.AddDelta(1);
        }

        internal void Tick()
        {
            lock (announces)
                announces.Tick();
            lock (scrapes)
                scrapes.Tick();
        }

        #endregion
    }
}