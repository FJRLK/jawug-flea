namespace MonoTorrent.Client
{
    class PeerEventArgs : TorrentEventArgs
    {
        #region Internals

        PeerId peer;

        public PeerId Peer
        {
            get { return peer; }
        }

        #endregion

        #region Constructor

        public PeerEventArgs(PeerId peer)
            : base(peer.TorrentManager)
        {
            this.peer = peer;
        }

        #endregion
    }
}