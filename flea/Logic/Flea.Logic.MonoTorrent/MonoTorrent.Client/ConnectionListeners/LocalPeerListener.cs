using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    class LocalPeerListener : Listener
    {
        #region Static

        const int MulticastPort = 6771;
        static readonly IPAddress multicastIpAddress = IPAddress.Parse("239.192.152.143");

        #endregion

        #region Internals

        private ClientEngine engine;
        private UdpClient udpClient;

        #endregion

        #region Constructor

        public LocalPeerListener(ClientEngine engine)
            : base(new IPEndPoint(IPAddress.Any, 6771))
        {
            this.engine = engine;
        }

        #endregion

        #region Members

        public override void Start()
        {
            if (Status == ListenerStatus.Listening)
                return;
            try
            {
                udpClient = new UdpClient(MulticastPort);
                udpClient.JoinMulticastGroup(multicastIpAddress);
                udpClient.BeginReceive(OnReceiveCallBack, udpClient);
                RaiseStatusChanged(ListenerStatus.Listening);
            }
            catch
            {
                RaiseStatusChanged(ListenerStatus.PortNotFree);
            }
        }

        public override void Stop()
        {
            if (Status == ListenerStatus.NotListening)
                return;

            RaiseStatusChanged(ListenerStatus.NotListening);
            UdpClient c = udpClient;
            udpClient = null;
            if (c != null)
                c.Close();
        }

        private void OnReceiveCallBack(IAsyncResult ar)
        {
            UdpClient u = (UdpClient) ar.AsyncState;
            IPEndPoint e = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                byte[] receiveBytes = u.EndReceive(ar, ref e);
                string receiveString = Encoding.ASCII.GetString(receiveBytes);

                Regex exp =
                    new Regex(
                        "BT-SEARCH \\* HTTP/1.1\\r\\nHost: 239.192.152.143:6771\\r\\nPort: (?<port>[^@]+)\\r\\nInfohash: (?<hash>[^@]+)\\r\\n\\r\\n\\r\\n");
                Match match = exp.Match(receiveString);

                if (!match.Success)
                    return;

                int portcheck = Convert.ToInt32(match.Groups["port"].Value);
                if (portcheck < 0 || portcheck > 65535)
                    return;

                TorrentManager manager = null;
                InfoHash matchHash = InfoHash.FromHex(match.Groups["hash"].Value);
                for (int i = 0; manager == null && i < engine.Torrents.Count; i ++)
                    if (engine.Torrents[i].InfoHash == matchHash)
                        manager = engine.Torrents[i];

                if (manager == null)
                    return;

                Uri uri = new Uri("tcp://" + e.Address.ToString() + ':' + match.Groups["port"].Value);
                Peer peer = new Peer("", uri, EncryptionTypes.All);

                // Add new peer to matched Torrent
                if (!manager.HasMetadata || !manager.Torrent.IsPrivate)
                {
                    ClientEngine.MainLoop.Queue(delegate
                    {
                        int count = manager.AddPeersCore(peer);
                        manager.RaisePeersFound(new LocalPeersAdded(manager, count, 1));
                    });
                }
            }
            catch
            {
                // Failed to receive data, ignore
            }
            finally
            {
                try
                {
                    u.BeginReceive(OnReceiveCallBack, ar.AsyncState);
                }
                catch
                {
                    // It's closed
                }
            }
        }

        #endregion
    }
}