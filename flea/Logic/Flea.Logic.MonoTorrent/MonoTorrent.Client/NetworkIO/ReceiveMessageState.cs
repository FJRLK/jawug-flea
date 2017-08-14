using MonoTorrent.Client.Connections;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    internal static partial class PeerIO
    {
        #region Types

        class ReceiveMessageState : ICacheable
        {
            #region Internals

            public byte[] Buffer { get; set; }

            public AsyncMessageReceivedCallback Callback { get; private set; }

            public IConnection Connection { get; private set; }

            public IEncryption Decryptor { get; set; }

            public TorrentManager Manager { get; private set; }

            public ConnectionMonitor ManagerMonitor { get; set; }

            public ConnectionMonitor PeerMonitor { get; set; }

            public IRateLimiter RateLimiter { get; set; }

            public object State { get; private set; }

            #endregion

            #region Members

            public void Initialise()
            {
                Initialise(null, null, null, null, null, null, null, null);
            }

            public ReceiveMessageState Initialise(IConnection connection, IEncryption decryptor, IRateLimiter limiter,
                ConnectionMonitor peerMonitor, TorrentManager manager, byte[] buffer,
                AsyncMessageReceivedCallback callback, object state)
            {
                Connection = connection;
                Decryptor = decryptor;
                Manager = manager;
                Buffer = buffer;
                PeerMonitor = peerMonitor;
                RateLimiter = limiter;
                ManagerMonitor = manager == null ? null : manager.Monitor;
                Callback = callback;
                State = state;
                return this;
            }

            #endregion
        }

        #endregion
    }
}