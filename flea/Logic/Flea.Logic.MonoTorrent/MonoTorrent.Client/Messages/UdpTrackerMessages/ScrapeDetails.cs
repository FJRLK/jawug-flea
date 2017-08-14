namespace MonoTorrent.Client.Messages.UdpTracker
{
    public class ScrapeDetails
    {
        #region Internals

        private int complete;
        private int leeches;
        private int seeds;

        public int Complete
        {
            get { return complete; }
        }

        public int Leeches
        {
            get { return leeches; }
        }

        public int Seeds
        {
            get { return seeds; }
        }

        #endregion

        #region Constructor

        public ScrapeDetails(int seeds, int leeches, int complete)
        {
            this.complete = complete;
            this.leeches = leeches;
            this.seeds = seeds;
        }

        #endregion
    }
}