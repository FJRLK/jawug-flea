using System;
using MonoTorrent.BEncoding;

namespace MonoTorrent
{
    public interface IDhtEngine : IDisposable
    {
        #region Types

        event EventHandler<PeersFoundEventArgs> PeersFound;
        event EventHandler StateChanged;

        #endregion

        #region Internals

        bool Disposed { get; }
        DhtState State { get; }

        #endregion

        #region Members

        byte[] SaveNodes();
        void Add(BEncodedList nodes);
        void Announce(InfoHash infohash, int port);
        void GetPeers(InfoHash infohash);
        void Start();
        void Start(byte[] initialNodes);
        void Stop();

        #endregion
    }
}