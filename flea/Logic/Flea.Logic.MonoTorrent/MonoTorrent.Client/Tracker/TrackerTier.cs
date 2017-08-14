using System;
using System.Collections;
using System.Collections.Generic;

namespace MonoTorrent.Client.Tracker
{
    public class TrackerTier : IEnumerable<Tracker>
    {
        #region Internals

        private bool sendingStartedEvent;
        private bool sentStartedEvent;
        private List<Tracker> trackers;

        internal bool SendingStartedEvent
        {
            get { return sendingStartedEvent; }
            set { sendingStartedEvent = value; }
        }

        internal bool SentStartedEvent
        {
            get { return sentStartedEvent; }
            set { sentStartedEvent = value; }
        }

        internal List<Tracker> Trackers
        {
            get { return trackers; }
        }

        #endregion

        #region Constructor

        internal TrackerTier(IEnumerable<string> trackerUrls)
        {
            Uri result;
            List<Tracker> trackerList = new List<Tracker>();

            foreach (string trackerUrl in trackerUrls)
            {
                // FIXME: Debug spew?
                if (!Uri.TryCreate(trackerUrl, UriKind.Absolute, out result))
                {
                    Logger.Log(null, "TrackerTier - Invalid tracker Url specified: {0}", trackerUrl);
                    continue;
                }

                Tracker tracker = TrackerFactory.Create(result);
                if (tracker != null)
                {
                    trackerList.Add(tracker);
                }
                else
                {
                    Console.Error.WriteLine("Unsupported protocol {0}", result); // FIXME: Debug spew?
                }
            }

            trackers = trackerList;
        }

        #endregion

        #region Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<Tracker> GetEnumerator()
        {
            return trackers.GetEnumerator();
        }

        internal int IndexOf(Tracker tracker)
        {
            return trackers.IndexOf(tracker);
        }

        public List<Tracker> GetTrackers()
        {
            return new List<Tracker>(trackers);
        }

        #endregion
    }
}