using System;
using System.Net;

namespace MonoTorrent.Client.Connections
{
    public interface IConnection : IDisposable
    {
        #region Internals

        byte[] AddressBytes { get; }

        bool CanReconnect { get; }

        bool Connected { get; }

        EndPoint EndPoint { get; }

        bool IsIncoming { get; }

        Uri Uri { get; }

        #endregion

        #region Members

        IAsyncResult BeginConnect(AsyncCallback callback, object state);
        void EndConnect(IAsyncResult result);

        IAsyncResult BeginReceive(byte[] buffer, int offset, int count, AsyncCallback callback, object state);
        int EndReceive(IAsyncResult result);

        IAsyncResult BeginSend(byte[] buffer, int offset, int count, AsyncCallback callback, object state);
        int EndSend(IAsyncResult result);

        #endregion
    }
}