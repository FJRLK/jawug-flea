using System;
using MonoTorrent.Common;

namespace MonoTorrent.Client.Tracker
{
    public abstract class Tracker : ITracker
    {
        #region Types

        public event EventHandler BeforeAnnounce;
        public event EventHandler<AnnounceResponseEventArgs> AnnounceComplete;
        public event EventHandler BeforeScrape;
        public event EventHandler<ScrapeResponseEventArgs> ScrapeComplete;

        #endregion

        #region Internals

        bool canAnnounce;
        bool canScrape;
        int complete;
        int downloaded;
        string failureMessage;
        int incomplete;
        TimeSpan minUpdateInterval;
        TrackerState status;
        TimeSpan updateInterval;
        Uri uri;
        string warningMessage;

        public bool CanAnnounce
        {
            get { return canAnnounce; }
            protected set { canAnnounce = value; }
        }

        public bool CanScrape
        {
            get { return canScrape; }
            set { canScrape = value; }
        }

        public int Complete
        {
            get { return complete; }
            protected set { complete = value; }
        }

        public int Downloaded
        {
            get { return downloaded; }
            protected set { downloaded = value; }
        }

        public string FailureMessage
        {
            get { return failureMessage ?? ""; }
            protected set { failureMessage = value; }
        }

        public int Incomplete
        {
            get { return incomplete; }
            protected set { incomplete = value; }
        }

        public TimeSpan MinUpdateInterval
        {
            get { return minUpdateInterval; }
            protected set { minUpdateInterval = value; }
        }

        public TrackerState Status
        {
            get { return status; }
            protected set { status = value; }
        }

        public TimeSpan UpdateInterval
        {
            get { return updateInterval; }
            protected set { updateInterval = value; }
        }

        public Uri Uri
        {
            get { return uri; }
        }

        public string WarningMessage
        {
            get { return warningMessage ?? ""; }
            protected set { warningMessage = value; }
        }

        #endregion

        #region Constructor

        protected Tracker(Uri uri)
        {
            Check.Uri(uri);
            MinUpdateInterval = TimeSpan.FromMinutes(3);
            UpdateInterval = TimeSpan.FromMinutes(30);
            this.uri = uri;
        }

        #endregion

        #region Members

        public abstract void Announce(AnnounceParameters parameters, object state);
        public abstract void Scrape(ScrapeParameters parameters, object state);

        protected virtual void RaiseBeforeAnnounce()
        {
            EventHandler h = BeforeAnnounce;
            if (h != null)
                h(this, EventArgs.Empty);
        }

        protected virtual void RaiseAnnounceComplete(AnnounceResponseEventArgs e)
        {
            EventHandler<AnnounceResponseEventArgs> h = AnnounceComplete;
            if (h != null)
                h(this, e);
        }

        protected virtual void RaiseBeforeScrape()
        {
            EventHandler h = BeforeScrape;
            if (h != null)
                h(this, EventArgs.Empty);
        }

        protected virtual void RaiseScrapeComplete(ScrapeResponseEventArgs e)
        {
            EventHandler<ScrapeResponseEventArgs> h = ScrapeComplete;
            if (h != null)
                h(this, e);
        }

        #endregion
    }
}