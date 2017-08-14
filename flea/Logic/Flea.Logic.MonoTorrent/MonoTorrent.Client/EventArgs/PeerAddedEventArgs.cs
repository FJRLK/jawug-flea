namespace MonoTorrent.Client
{
    /// <summary>
    ///     Provides the data needed to handle a PeersAdded event
    /// </summary>
    public class PeerAddedEventArgs : TorrentEventArgs
    {
        #region Internals

        private Peer peer;

        /// <summary>
        ///     The number of peers that were added in the last update
        /// </summary>
        public Peer Peer
        {
            get { return peer; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Creates a new PeersAddedEventArgs
        /// </summary>
        /// <param name="peersAdded">The number of peers just added</param>
        public PeerAddedEventArgs(TorrentManager manager, Peer peerAdded)
            : base(manager)
        {
            peer = peerAdded;
        }

        #endregion
    }
}