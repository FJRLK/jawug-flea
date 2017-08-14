using System;
using System.Net;
using MonoTorrent.BEncoding;

namespace MonoTorrent.Tracker.Listeners
{
    public class ManualListener : ListenerBase
    {
        #region Internals

        private bool running;

        public override bool Running
        {
            get { return running; }
        }

        #endregion

        #region Members

        public override void Start()
        {
            running = true;
        }

        public override void Stop()
        {
            running = false;
        }

        public BEncodedValue Handle(string rawUrl, IPAddress remoteAddress)
        {
            if (rawUrl == null)
                throw new ArgumentNullException("rawUrl");
            if (remoteAddress == null)
                throw new ArgumentOutOfRangeException("remoteAddress");

            rawUrl = rawUrl.Substring(rawUrl.LastIndexOf('/'));
            bool isScrape = rawUrl.StartsWith("/scrape", StringComparison.OrdinalIgnoreCase);
            return Handle(rawUrl, remoteAddress, isScrape);
        }

        #endregion
    }
}