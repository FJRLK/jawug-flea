using System;

namespace MonoTorrent.Tracker
{
    public class TrackerException : Exception
    {
        #region Constructor

        public TrackerException()
            : base()
        {
        }

        public TrackerException(string message)
            : base(message)
        {
        }

        #endregion
    }
}