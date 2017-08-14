using System;
using System.Net;
using System.Net.Sockets;

namespace MonoTorrent.Client.Connections
{
    public class IPV4Connection : IConnection
    {
        #region Internals

        private IPEndPoint endPoint;
        private bool isIncoming;
        private Socket socket;
        private Uri uri;

        public byte[] AddressBytes
        {
            get { return endPoint.Address.GetAddressBytes(); }
        }

        public bool CanReconnect
        {
            get { return !isIncoming; }
        }

        public bool Connected
        {
            get { return socket.Connected; }
        }

        EndPoint IConnection.EndPoint
        {
            get { return endPoint; }
        }

        public IPEndPoint EndPoint
        {
            get { return endPoint; }
        }

        public bool IsIncoming
        {
            get { return isIncoming; }
        }

        public Uri Uri
        {
            get { return uri; }
        }

        #endregion

        #region Constructor

        public IPV4Connection(Uri uri)
            : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port),
                false)
        {
            this.uri = uri;
        }

        public IPV4Connection(IPEndPoint endPoint)
            : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), endPoint, false)
        {
        }

        public IPV4Connection(Socket socket, bool isIncoming)
            : this(socket, (IPEndPoint) socket.RemoteEndPoint, isIncoming)
        {
        }


        private IPV4Connection(Socket socket, IPEndPoint endpoint, bool isIncoming)
        {
            this.socket = socket;
            endPoint = endpoint;
            this.isIncoming = isIncoming;
        }

        #endregion

        #region Members

        public IAsyncResult BeginConnect(AsyncCallback peerEndCreateConnection, object state)
        {
            return socket.BeginConnect(endPoint, peerEndCreateConnection, state);
        }

        public IAsyncResult BeginReceive(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object state)
        {
            return socket.BeginReceive(buffer, offset, count, SocketFlags.None, asyncCallback, state);
        }

        public IAsyncResult BeginSend(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object state)
        {
            return socket.BeginSend(buffer, offset, count, SocketFlags.None, asyncCallback, state);
        }

        public void EndConnect(IAsyncResult result)
        {
            socket.EndConnect(result);
        }

        public int EndReceive(IAsyncResult result)
        {
            return socket.EndReceive(result);
        }

        public int EndSend(IAsyncResult result)
        {
            return socket.EndSend(result);
        }

        public void Dispose()
        {
            ((IDisposable) socket).Dispose();
        }

        #endregion
    }
}