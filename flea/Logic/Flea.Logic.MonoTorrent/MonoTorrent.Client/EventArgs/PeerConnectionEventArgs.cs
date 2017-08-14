using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    /// <summary>
    ///     Provides the data needed to handle a PeerConnection event
    /// </summary>
    public class PeerConnectionEventArgs : TorrentEventArgs
    {
        #region Internals

        private Direction connectionDirection;

        private string message;
        private PeerId peerConnectionId;


        /// <summary>
        ///     The peer event that just happened
        /// </summary>
        public Direction ConnectionDirection
        {
            get { return connectionDirection; }
        }

        /// <summary>
        ///     Any message that might be associated with this event
        /// </summary>
        public string Message
        {
            get { return message; }
        }

        public PeerId PeerID
        {
            get { return peerConnectionId; }
        }

        #endregion

        #region Constructor

        internal PeerConnectionEventArgs(TorrentManager manager, PeerId id, Direction direction)
            : this(manager, id, direction, "")
        {
        }


        internal PeerConnectionEventArgs(TorrentManager manager, PeerId id, Direction direction, string message)
            : base(manager)
        {
            peerConnectionId = id;
            connectionDirection = direction;
            this.message = message;
        }

        #endregion
    }
}