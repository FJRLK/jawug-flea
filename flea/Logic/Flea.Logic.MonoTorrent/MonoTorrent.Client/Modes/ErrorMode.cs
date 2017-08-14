using System;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    // In the error mode, we just disable all connections
    // Usually we enter this because the HD is full
    public enum Reason
    {
        ReadFailure,
        WriteFailure
    }

    public class Error
    {
        #region Internals

        Exception exception;
        Reason reason;

        public Exception Exception
        {
            get { return exception; }
        }

        public Reason Reason
        {
            get { return reason; }
        }

        #endregion

        #region Constructor

        public Error(Reason reason, Exception exception)
        {
            this.reason = reason;
            this.exception = exception;
        }

        #endregion
    }

    class ErrorMode : Mode
    {
        #region Internals

        public override TorrentState State
        {
            get { return TorrentState.Error; }
        }

        #endregion

        #region Constructor

        public ErrorMode(TorrentManager manager)
            : base(manager)
        {
            CanAcceptConnections = false;
            CloseConnections();
        }

        #endregion

        #region Members

        public override void Tick(int counter)
        {
            Manager.Monitor.Reset();
            CloseConnections();
        }

        void CloseConnections()
        {
            foreach (PeerId peer in Manager.Peers.ConnectedPeers)
                peer.CloseConnection();
        }

        #endregion
    }
}