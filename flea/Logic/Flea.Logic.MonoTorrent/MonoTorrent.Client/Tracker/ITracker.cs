using System;
using MonoTorrent.Common;

namespace MonoTorrent.Client.Tracker
{
    interface ITracker
    {
        #region Types

        event EventHandler BeforeAnnounce;
        event EventHandler<AnnounceResponseEventArgs> AnnounceComplete;
        event EventHandler BeforeScrape;
        event EventHandler<ScrapeResponseEventArgs> ScrapeComplete;

        #endregion

        #region Internals

        bool CanAnnounce { get; }
        bool CanScrape { get; }
        int Complete { get; }
        int Downloaded { get; }
        string FailureMessage { get; }
        int Incomplete { get; }
        TimeSpan MinUpdateInterval { get; }
        TrackerState Status { get; }
        TimeSpan UpdateInterval { get; }
        Uri Uri { get; }
        string WarningMessage { get; }

        #endregion

        #region Members

        void Announce(AnnounceParameters parameters, object state);
        void Scrape(ScrapeParameters parameters, object state);

        #endregion
    }
}