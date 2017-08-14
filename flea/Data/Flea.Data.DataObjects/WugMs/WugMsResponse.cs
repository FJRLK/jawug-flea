namespace Flea.Data.DataObjects.WugMs
{
    public class WugMsResponse
    {
        #region Internals

        public WugMsMember[] data { get; set; }
        public string message { get; set; }
        public int status { get; set; }

        #endregion
    }
}