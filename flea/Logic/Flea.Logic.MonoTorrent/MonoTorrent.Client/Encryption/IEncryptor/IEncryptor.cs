using System;
using MonoTorrent.Client.Connections;

namespace MonoTorrent.Client.Encryption
{
    public interface IEncryptor
    {
        #region Internals

        IEncryption Decryptor { get; }

        IEncryption Encryptor { get; }

        byte[] InitialData { get; }

        #endregion

        #region Members

        void AddPayload(byte[] buffer);
        void AddPayload(byte[] buffer, int offset, int count);

        IAsyncResult BeginHandshake(IConnection socket, AsyncCallback callback, object state);

        IAsyncResult BeginHandshake(IConnection socket, byte[] initialBuffer, int offset, int count,
            AsyncCallback callback, object state);

        void EndHandshake(IAsyncResult result);

        #endregion
    }
}