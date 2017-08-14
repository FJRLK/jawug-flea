using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    public class PeerConnectionFailedEventArgs : TorrentEventArgs
    {
        #region Internals

        private Direction connectionDirection;
        private string message;
        private Peer peer;

        /// <summary>
        ///     Direction of event (if our connection failed to them or their connection failed to us)
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

        /// <summary>
        ///     Peer from which this event happened
        /// </summary>
        public Peer Peer
        {
            get { return peer; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Create new instance of PeerConnectionFailedEventArgs for peer from given torrent.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="peer"></param>
        /// <param name="direction">Which direction the connection attempt was</param>
        /// <param name="message">Message associated with the failure</param>
        public PeerConnectionFailedEventArgs(TorrentManager manager, Peer peer, Direction direction, string message)
            : base(manager)
        {
            this.peer = peer;
            connectionDirection = direction;
            this.message = message;
        }

        #endregion
    }
}