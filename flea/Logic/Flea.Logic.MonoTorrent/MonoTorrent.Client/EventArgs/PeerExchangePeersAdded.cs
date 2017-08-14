using System;

namespace MonoTorrent.Client
{
    public class PeerExchangePeersAdded : PeersAddedEventArgs
    {
        #region Internals

        private PeerId id;

        public PeerId Id
        {
            get { return id; }
        }

        #endregion

        #region Constructor

        public PeerExchangePeersAdded(TorrentManager manager, int count, int total, PeerId id)
            : base(manager, count, total)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            this.id = id;
        }

        #endregion
    }
}