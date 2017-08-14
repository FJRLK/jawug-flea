using MonoTorrent.Common;

namespace MonoTorrent.Client.Tracker
{
    /// <summary>
    ///     Provides the data needed to handle a TrackerUpdate event
    /// </summary>
    public class TrackerStateChangedEventArgs : TorrentEventArgs
    {
        #region Internals

        private TrackerState newState;
        private TrackerState oldState;
        private Tracker tracker;


        public TrackerState NewState
        {
            get { return newState; }
        }


        public TrackerState OldState
        {
            get { return oldState; }
        }

        /// <summary>
        ///     The current status of the tracker update
        /// </summary>
        public Tracker Tracker
        {
            get { return tracker; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Creates a new TrackerUpdateEventArgs
        /// </summary>
        /// <param name="state">The current state of the update</param>
        /// <param name="response">The response of the tracker (if any)</param>
        public TrackerStateChangedEventArgs(TorrentManager manager, Tracker tracker, TrackerState oldState,
            TrackerState newState)
            : base(manager)
        {
            this.tracker = tracker;
            this.oldState = oldState;
            this.newState = newState;
        }

        #endregion
    }
}