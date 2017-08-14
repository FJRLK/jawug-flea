namespace MonoTorrent.Tracker
{
    public class TimedOutEventArgs : PeerEventArgs
    {
        #region Constructor

        public TimedOutEventArgs(Peer peer, SimpleTorrentManager manager)
            : base(peer, manager)
        {
        }

        #endregion
    }
}