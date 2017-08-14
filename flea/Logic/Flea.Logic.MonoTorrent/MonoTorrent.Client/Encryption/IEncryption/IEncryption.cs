namespace MonoTorrent.Client.Encryption
{
    public interface IEncryption
    {
        #region Members

        void Decrypt(byte[] buffer);
        void Decrypt(byte[] buffer, int offset, int count);
        void Decrypt(byte[] src, int srcOffset, byte[] dest, int destOffset, int count);

        void Encrypt(byte[] buffer);
        void Encrypt(byte[] buffer, int offset, int count);
        void Encrypt(byte[] src, int srcOffset, byte[] dest, int destOffset, int count);

        #endregion
    }
}