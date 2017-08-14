namespace MonoTorrent.Client.Tracker
{
    public class ScrapeResponseEventArgs : TrackerResponseEventArgs
    {
        #region Constructor

        public ScrapeResponseEventArgs(Tracker tracker, object state, bool successful)
            : base(tracker, state, successful)
        {
        }

        #endregion
    }
}