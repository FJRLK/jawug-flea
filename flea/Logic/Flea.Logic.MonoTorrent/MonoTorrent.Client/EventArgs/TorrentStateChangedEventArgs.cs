using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    /// <summary>
    ///     Provides the data needed to handle a TorrentStateChanged event
    /// </summary>
    public class TorrentStateChangedEventArgs : TorrentEventArgs
    {
        #region Internals

        private TorrentState newState;
        private TorrentState oldState;


        /// <summary>
        ///     The new state for the torrent
        /// </summary>
        public TorrentState NewState
        {
            get { return newState; }
        }

        /// <summary>
        ///     The old state for the torrent
        /// </summary>
        public TorrentState OldState
        {
            get { return oldState; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Creates a new TorrentStateChangedEventArgs
        /// </summary>
        /// <param name="oldState">The old state of the Torrent</param>
        /// <param name="newState">The new state of the Torrent</param>
        public TorrentStateChangedEventArgs(TorrentManager manager, TorrentState oldState, TorrentState newState)
            : base(manager)
        {
            this.oldState = oldState;
            this.newState = newState;
        }

        #endregion
    }
}