using MonoTorrent.Client.Connections;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    static partial class NetworkIO
    {
        #region Types

        class AsyncConnectState : ICacheable
        {
            #region Internals

            public AsyncIOCallback Callback { get; private set; }

            public IConnection Connection { get; private set; }

            public object State { get; private set; }

            #endregion

            #region Members

            public void Initialise()
            {
                Initialise(null, null, null);
            }

            public AsyncConnectState Initialise(IConnection connection, AsyncIOCallback callback, object state)
            {
                Connection = connection;
                Callback = callback;
                State = state;
                return this;
            }

            #endregion
        }

        #endregion
    }
}