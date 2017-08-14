using System.Collections.Generic;
using Flea.Data.DataObjects.WugMs;

namespace Flea.Data.WebserviceCommunicator.Responses
{
    public class MemberCheckResponse
    {
        #region Internals

        public List<WugMsMember> LapsedMembers { get; set; }
        public List<WugMsMember> PaidUpMembers { get; set; }
        public int TotalPaidUpMembers { get; set; }

        #endregion
    }
}