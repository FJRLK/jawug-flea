using System;

namespace Flea.Data.DataObjects.WugMs
{
    public class WugMsMember
    {
        #region Internals

        public string account_status { get; set; }
        public DateTime? last_payment { get; set; }
        public string member { get; set; }
        public string payment_type { get; set; }

        #endregion
    }
}