using MonoTorrent.Client.Messages;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    /// <summary>
    ///     Provides the data needed to handle a PeerMessage event
    /// </summary>
    public class PeerMessageEventArgs : TorrentEventArgs
    {
        #region Internals

        private Direction direction;
        private PeerId id;
        private PeerMessage message;

        /// <summary>
        ///     The direction of the message (outgoing/incoming)
        /// </summary>
        public Direction Direction
        {
            get { return direction; }
        }

        public PeerId ID
        {
            get { return id; }
        }

        /// <summary>
        ///     The Peer message that was just sent/Received
        /// </summary>
        public PeerMessage Message
        {
            get { return message; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Creates a new PeerMessageEventArgs
        /// </summary>
        /// <param name="message">The peer message involved</param>
        /// <param name="direction">The direction of the message</param>
        internal PeerMessageEventArgs(TorrentManager manager, PeerMessage message, Direction direction, PeerId id)
            : base(manager)
        {
            this.direction = direction;
            this.id = id;
            this.message = message;
        }

        #endregion
    }
}