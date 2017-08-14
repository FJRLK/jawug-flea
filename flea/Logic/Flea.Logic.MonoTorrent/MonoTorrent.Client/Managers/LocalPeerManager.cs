using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MonoTorrent.Client
{
    class LocalPeerManager : IDisposable
    {
        #region Static

        private const int port = 6771;

        #endregion

        #region Internals

        private IPEndPoint ep;

        private UdpClient socket;

        #endregion

        #region Constructor

        public LocalPeerManager()
        {
            socket = new UdpClient();
            ep = new IPEndPoint(IPAddress.Broadcast, port);
        }

        #endregion

        #region Members

        public void Dispose()
        {
            socket.Close();
        }

        public void Broadcast(TorrentManager manager)
        {
            if (manager.HasMetadata && manager.Torrent.IsPrivate)
                return;

            string message =
                $"BT-SEARCH * HTTP/1.1\r\nHost: 239.192.152.143:6771\r\nPort: {manager.Engine.Settings.ListenPort}\r\nInfohash: {manager.InfoHash.ToHex()}\r\n\r\n\r\n";
            byte[] data = Encoding.ASCII.GetBytes(message);
            try
            {
                socket.Send(data, data.Length, ep);
            }
            catch
            {
                // If data can't be sent, just ignore the error
            }
        }

        #endregion
    }
}