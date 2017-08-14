namespace MonoTorrent.Client
{
    /// <summary>
    ///     Provides the data needed to handle a PeersAdded event
    /// </summary>
    public abstract class PeersAddedEventArgs : TorrentEventArgs
    {
        #region Internals

        private int count;
        private int total;

        public int ExistingPeers
        {
            get { return total - NewPeers; }
        }

        public int NewPeers
        {
            get { return count; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Creates a new PeersAddedEventArgs
        /// </summary>
        /// <param name="peersAdded">The number of peers just added</param>
        protected PeersAddedEventArgs(TorrentManager manager, int peersAdded, int total)
            : base(manager)
        {
            count = peersAdded;
            this.total = total;
        }

        #endregion
    }
}