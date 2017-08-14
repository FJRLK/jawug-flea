namespace MonoTorrent.Client.Messages
{
    interface IMessage
    {
        #region Internals

        int ByteLength { get; }

        #endregion

        #region Members

        byte[] Encode();
        int Encode(byte[] buffer, int offset);

        void Decode(byte[] buffer, int offset, int length);

        #endregion
    }
}