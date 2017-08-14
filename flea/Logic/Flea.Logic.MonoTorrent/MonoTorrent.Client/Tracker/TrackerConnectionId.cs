using System.Threading;
using MonoTorrent.Common;

namespace MonoTorrent.Client.Tracker
{
    internal class TrackerConnectionID
    {
        #region Internals

        TorrentEvent torrentEvent;
        Tracker tracker;
        bool trySubsequent;
        ManualResetEvent waitHandle;

        public TorrentEvent TorrentEvent
        {
            get { return torrentEvent; }
        }

        public Tracker Tracker
        {
            get { return tracker; }
        }

        internal bool TrySubsequent
        {
            get { return trySubsequent; }
        }

        public ManualResetEvent WaitHandle
        {
            get { return waitHandle; }
        }

        #endregion

        #region Constructor

        public TrackerConnectionID(Tracker tracker, bool trySubsequent, TorrentEvent torrentEvent,
            ManualResetEvent waitHandle)
        {
            this.tracker = tracker;
            this.trySubsequent = trySubsequent;
            this.torrentEvent = torrentEvent;
            this.waitHandle = waitHandle;
        }

        #endregion
    }
}