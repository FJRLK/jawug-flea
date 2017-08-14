using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    class StoppedMode : Mode
    {
        #region Internals

        public override bool CanHashCheck
        {
            get { return true; }
        }

        public override TorrentState State
        {
            get { return TorrentState.Stopped; }
        }

        #endregion

        #region Constructor

        public StoppedMode(TorrentManager manager)
            : base(manager)
        {
            CanAcceptConnections = false;
        }

        #endregion

        #region Members

        public override void HandlePeerConnected(PeerId id, Direction direction)
        {
            id.CloseConnection();
        }


        public override void Tick(int counter)
        {
            // When stopped, do nothing
        }

        #endregion
    }
}