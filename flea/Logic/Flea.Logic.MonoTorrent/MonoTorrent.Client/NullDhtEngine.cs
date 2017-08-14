using System;
using MonoTorrent.BEncoding;

namespace MonoTorrent.Client
{
    class NullDhtEngine : IDhtEngine
    {
        #region Types

        public event EventHandler<PeersFoundEventArgs> PeersFound;
        public event EventHandler StateChanged;

        #endregion

        #region Internals

        public bool Disposed
        {
            get { return false; }
        }

        public DhtState State
        {
            get { return DhtState.NotReady; }
        }

        #endregion

        #region Members

        public void Add(BEncodedList nodes)
        {
        }

        public void Announce(InfoHash infohash, int port)
        {
        }

        public void GetPeers(InfoHash infohash)
        {
        }

        public byte[] SaveNodes()
        {
            return new byte[0];
        }

        public void Start()
        {
        }

        public void Start(byte[] initialNodes)
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }

        #endregion
    }
}