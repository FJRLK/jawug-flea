namespace MonoTorrent.Client
{
    public class LocalPeersAdded : PeersAddedEventArgs
    {
        #region Constructor

        public LocalPeersAdded(TorrentManager manager, int peersAdded, int total)
            : base(manager, peersAdded, total)
        {
        }

        #endregion
    }
}