using System;
using System.Net;
using MonoTorrent.Client.Connections;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Client.Messages;
using MonoTorrent.Client.Messages.Standard;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    internal static partial class PeerIO
    {
        #region Static

        const int MaxMessageLength = Piece.BlockSize*4;
        static ICache<ReceiveMessageState> receiveCache = new Cache<ReceiveMessageState>(true).Synchronize();
        static ICache<SendMessageState> sendCache = new Cache<SendMessageState>(true).Synchronize();
        static AsyncIOCallback MessageLengthReceivedCallback = MessageLengthReceived;
        static AsyncIOCallback EndSendCallback = EndSend;
        static AsyncIOCallback MessageBodyReceivedCallback = MessageBodyReceived;
        static AsyncIOCallback HandshakeReceivedCallback = HandshakeReceived;

        #endregion

        #region Members

        public static void EnqueueSendMessage(IConnection connection, IEncryption encryptor, PeerMessage message,
            IRateLimiter rateLimiter, ConnectionMonitor peerMonitor, ConnectionMonitor managerMonitor,
            AsyncIOCallback callback, object state)
        {
            int count = message.ByteLength;
            byte[] buffer = ClientEngine.BufferManager.GetBuffer(count);
            message.Encode(buffer, 0);
            encryptor.Encrypt(buffer, 0, count);

            SendMessageState data = sendCache.Dequeue().Initialise(buffer, callback, state);
            NetworkIO.EnqueueSend(connection, buffer, 0, count, rateLimiter, peerMonitor, managerMonitor,
                EndSendCallback, data);
        }

        static void EndSend(bool successful, int count, object state)
        {
            SendMessageState data = (SendMessageState) state;
            ClientEngine.BufferManager.FreeBuffer(data.Buffer);
            data.Callback(successful, count, data.State);
            sendCache.Enqueue(data);
        }

        public static void EnqueueReceiveHandshake(IConnection connection, IEncryption decryptor,
            AsyncMessageReceivedCallback callback, object state)
        {
            byte[] buffer = ClientEngine.BufferManager.GetBuffer(HandshakeMessage.HandshakeLength);
            ReceiveMessageState data = receiveCache.Dequeue()
                .Initialise(connection, decryptor, null, null, null, buffer, callback, state);
            NetworkIO.EnqueueReceive(connection, buffer, 0, HandshakeMessage.HandshakeLength, null, null, null,
                HandshakeReceivedCallback, data);
        }

        static void HandshakeReceived(bool successful, int transferred, object state)
        {
            ReceiveMessageState data = (ReceiveMessageState) state;
            PeerMessage message = null;

            if (successful)
            {
                data.Decryptor.Decrypt(data.Buffer, 0, transferred);
                message = new HandshakeMessage();
                message.Decode(data.Buffer, 0, transferred);
            }

            data.Callback(successful, message, data.State);
            ClientEngine.BufferManager.FreeBuffer(data.Buffer);
            receiveCache.Enqueue(data);
        }

        public static void EnqueueReceiveMessage(IConnection connection, IEncryption decryptor, IRateLimiter rateLimiter,
            ConnectionMonitor monitor, TorrentManager manager, AsyncMessageReceivedCallback callback, object state)
        {
            // FIXME: Hardcoded number
            int count = 4;
            byte[] buffer = ClientEngine.BufferManager.GetBuffer(count);
            ReceiveMessageState data = receiveCache.Dequeue()
                .Initialise(connection, decryptor, rateLimiter, monitor, manager, buffer, callback, state);
            NetworkIO.EnqueueReceive(connection, buffer, 0, count, rateLimiter, monitor, data.ManagerMonitor,
                MessageLengthReceivedCallback, data);
        }

        static void MessageLengthReceived(bool successful, int transferred, object state)
        {
            ReceiveMessageState data = (ReceiveMessageState) state;
            int messageLength = -1;

            if (successful)
            {
                data.Decryptor.Decrypt(data.Buffer, 0, transferred);
                messageLength = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(data.Buffer, 0));
            }

            if (!successful || messageLength < 0 || messageLength > MaxMessageLength)
            {
                ClientEngine.BufferManager.FreeBuffer(data.Buffer);
                data.Callback(false, null, data.State);
                receiveCache.Enqueue(data);
                return;
            }

            if (messageLength == 0)
            {
                ClientEngine.BufferManager.FreeBuffer(data.Buffer);
                data.Callback(true, new KeepAliveMessage(), data.State);
                receiveCache.Enqueue(data);
                return;
            }

            byte[] buffer = ClientEngine.BufferManager.GetBuffer(messageLength + transferred);
            Buffer.BlockCopy(data.Buffer, 0, buffer, 0, transferred);
            ClientEngine.BufferManager.FreeBuffer(data.Buffer);
            data.Buffer = buffer;

            NetworkIO.EnqueueReceive(data.Connection, buffer, transferred, messageLength, data.RateLimiter,
                data.PeerMonitor,
                data.ManagerMonitor, MessageBodyReceivedCallback, data);
        }

        static void MessageBodyReceived(bool successful, int transferred, object state)
        {
            ReceiveMessageState data = (ReceiveMessageState) state;
            if (!successful)
            {
                ClientEngine.BufferManager.FreeBuffer(data.Buffer);
                data.Callback(false, null, data.State);
                receiveCache.Enqueue(data);
                return;
            }

            data.Decryptor.Decrypt(data.Buffer, 4, transferred);
            PeerMessage message = PeerMessage.DecodeMessage(data.Buffer, 0, transferred + 4, data.Manager);
            ClientEngine.BufferManager.FreeBuffer(data.Buffer);
            data.Callback(true, message, data.State);
            receiveCache.Enqueue(data);
        }

        #endregion
    }
}