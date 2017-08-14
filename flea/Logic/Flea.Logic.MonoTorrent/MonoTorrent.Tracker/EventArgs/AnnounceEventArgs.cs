namespace MonoTorrent.Tracker
{
    public class AnnounceEventArgs : PeerEventArgs
    {
        #region Constructor

        public AnnounceEventArgs(Peer peer, SimpleTorrentManager manager)
            : base(peer, manager)
        {
        }

        #endregion
    }
}