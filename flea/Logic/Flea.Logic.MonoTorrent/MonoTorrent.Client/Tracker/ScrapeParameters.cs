namespace MonoTorrent.Client.Tracker
{
    public class ScrapeParameters
    {
        #region Internals

        private InfoHash infoHash;


        public InfoHash InfoHash
        {
            get { return infoHash; }
        }

        #endregion

        #region Constructor

        public ScrapeParameters(InfoHash infoHash)
        {
            this.infoHash = infoHash;
        }

        #endregion
    }
}