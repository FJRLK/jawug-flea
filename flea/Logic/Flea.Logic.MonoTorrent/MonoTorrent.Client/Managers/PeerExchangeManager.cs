using System;
using System.Collections.Generic;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Client.Messages.Libtorrent;

namespace MonoTorrent.Client
{
    /// <summary>
    ///     This class is used to send each minute a peer excahnge message to peer who have enable this protocol
    /// </summary>
    public class PeerExchangeManager : IDisposable
    {
        #region Static

        private const int MAX_PEERS = 50;

        #endregion

        #region Internals

        private List<Peer> addedPeers;
        private bool disposed = false;
        private List<Peer> droppedPeers;

        private PeerId id;

        #endregion

        #region Constructor

        internal PeerExchangeManager(PeerId id)
        {
            this.id = id;

            addedPeers = new List<Peer>();
            droppedPeers = new List<Peer>();
            id.TorrentManager.OnPeerFound += new EventHandler<PeerAddedEventArgs>(OnAdd);
            Start();
        }

        #endregion

        #region Members

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            id.TorrentManager.OnPeerFound -= new EventHandler<PeerAddedEventArgs>(OnAdd);
        }

        internal void OnAdd(object source, PeerAddedEventArgs e)
        {
            addedPeers.Add(e.Peer);
        }

        internal void Start()
        {
            ClientEngine.MainLoop.QueueTimeout(TimeSpan.FromMinutes(1), delegate
            {
                if (!disposed)
                    OnTick();
                return !disposed;
            });
        }

        internal void OnTick()
        {
            if (!id.TorrentManager.Settings.EnablePeerExchange)
                return;

            int len = (addedPeers.Count <= MAX_PEERS) ? addedPeers.Count : MAX_PEERS;
            byte[] added = new byte[len*6];
            byte[] addedDotF = new byte[len];
            for (int i = 0; i < len; i++)
            {
                addedPeers[i].CompactPeer(added, i*6);
                if ((addedPeers[i].Encryption & (EncryptionTypes.RC4Full | EncryptionTypes.RC4Header)) !=
                    EncryptionTypes.None)
                {
                    addedDotF[i] = 0x01;
                }
                else
                {
                    addedDotF[i] = 0x00;
                }

                addedDotF[i] |= (byte) (addedPeers[i].IsSeeder ? 0x02 : 0x00);
            }
            addedPeers.RemoveRange(0, len);

            len = Math.Min(MAX_PEERS - len, droppedPeers.Count);

            byte[] dropped = new byte[len*6];
            for (int i = 0; i < len; i++)
                droppedPeers[i].CompactPeer(dropped, i*6);

            droppedPeers.RemoveRange(0, len);
            id.Enqueue(new PeerExchangeMessage(id, added, addedDotF, dropped));
        }

        #endregion

        // TODO onDropped!
    }
}