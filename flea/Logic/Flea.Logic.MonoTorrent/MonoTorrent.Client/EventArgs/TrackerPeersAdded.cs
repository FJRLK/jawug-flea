using System;

namespace MonoTorrent.Client
{
    public class TrackerPeersAdded : PeersAddedEventArgs
    {
        #region Internals

        Tracker.Tracker tracker;

        public Tracker.Tracker Tracker
        {
            get { return tracker; }
        }

        #endregion

        #region Constructor

        public TrackerPeersAdded(TorrentManager manager, int peersAdded, int total, Tracker.Tracker tracker)
            : base(manager, peersAdded, total)
        {
            if (tracker == null)
                throw new ArgumentNullException("tracker");

            this.tracker = tracker;
        }

        #endregion
    }
}