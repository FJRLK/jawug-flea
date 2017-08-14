namespace MonoTorrent.Client
{
    public class DhtPeersAdded : PeersAddedEventArgs
    {
        #region Constructor

        public DhtPeersAdded(TorrentManager manager, int peersAdded, int total)
            : base(manager, peersAdded, total)
        {
        }

        #endregion
    }
}