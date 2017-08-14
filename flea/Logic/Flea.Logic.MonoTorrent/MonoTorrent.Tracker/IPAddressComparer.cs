namespace MonoTorrent.Tracker
{
    public interface IPeerComparer
    {
        #region Members

        object GetKey(AnnounceParameters parameters);

        #endregion
    }

    public class IPAddressComparer : IPeerComparer
    {
        #region Members

        public object GetKey(AnnounceParameters parameters)
        {
            return parameters.ClientAddress;
        }

        #endregion
    }
}