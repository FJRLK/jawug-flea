using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    internal static partial class PeerIO
    {
        #region Types

        class SendMessageState : ICacheable
        {
            #region Internals

            public byte[] Buffer { get; private set; }

            public AsyncIOCallback Callback { get; private set; }

            public object State { get; set; }

            #endregion

            #region Members

            public void Initialise()
            {
                Initialise(null, null, null);
            }

            public SendMessageState Initialise(byte[] buffer, AsyncIOCallback callback, object state)
            {
                Buffer = buffer;
                Callback = callback;
                State = state;
                return this;
            }

            #endregion
        }

        #endregion
    }
}