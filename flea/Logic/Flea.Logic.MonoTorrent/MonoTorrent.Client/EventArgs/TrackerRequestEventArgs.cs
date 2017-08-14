using System;

namespace MonoTorrent.Client.Tracker
{
    public abstract class TrackerResponseEventArgs : EventArgs
    {
        #region Internals

        TrackerConnectionID id;
        private bool successful;
        private Tracker tracker;

        internal TrackerConnectionID Id
        {
            get { return id; }
        }

        public object State
        {
            get { return id; }
        }

        /// <summary>
        ///     True if the request completed successfully
        /// </summary>
        public bool Successful
        {
            get { return successful; }
            set { successful = value; }
        }

        /// <summary>
        ///     The tracker which the request was sent to
        /// </summary>
        public Tracker Tracker
        {
            get { return tracker; }
            protected set { tracker = value; }
        }

        #endregion

        #region Constructor

        protected TrackerResponseEventArgs(Tracker tracker, object state, bool successful)
        {
            if (tracker == null)
                throw new ArgumentNullException("tracker");
            if (!(state is TrackerConnectionID))
                throw new ArgumentException("The state object must be the same object as in the call to Announce",
                    "state");
            id = (TrackerConnectionID) state;
            this.successful = successful;
            this.tracker = tracker;
        }

        #endregion
    }
}