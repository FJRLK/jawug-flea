using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Flea.Data.DataObjects.WugMs;
using Flea.Data.WebserviceCommunicator.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Flea.Data.WebserviceCommunicator
{
    public class WugmsCommunicator : IDisposable
    {
        #region Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        ///     Jsons the error handler.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="args">The <see cref="Newtonsoft.Json.Serialization.ErrorEventArgs" /> instance containing the event data.</param>
        private void JsonErrorHandler(object obj, ErrorEventArgs args)
        {
            ErrorContext context = args.ErrorContext;
            if (context.Member.ToString() == "last_payment")
                context.Handled = true;
        }

        public async Task<MemberCheckResponse> GetMemberlist()
        {
            // http://wugms.bwcsystem.net:20080/flea/members.php
            using (HttpClient httpClient = new HttpClient())
            {
                string json = await httpClient.GetStringAsync("http://wugms.bwcsystem.net:20080/flea/members.php");
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    Error = JsonErrorHandler
                };

                WugMsResponse response = JsonConvert.DeserializeObject<WugMsResponse>(json, settings);

                MemberCheckResponse mems = new MemberCheckResponse
                {
                    TotalPaidUpMembers = response.data.Count(x => x.account_status == "No payment required"),
                    PaidUpMembers = response.data.Where(x => x.account_status == "No payment required").ToList(),
                    LapsedMembers = response.data.Where(x => x.account_status != "No payment required").ToList()
                };

                return mems;
            }
        }

        #endregion
    }
}