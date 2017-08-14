using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using Wintellect.PowerCollections;

namespace Flea.Logic.CommandUnit.Internals
{
    public static class TraceRoute
    {
        #region Static

        private const string Data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        #endregion

        #region Members

        /// <summary>
        ///     Gets the trace route.
        /// </summary>
        /// <param name="hostNameOrAddress">The host name or address.</param>
        /// <returns></returns>
        public static Pair<List<IPAddress>, List<string>> GetTraceRoute(string hostNameOrAddress)
        {
            return GetTraceRoute(hostNameOrAddress, 1, IPStatus.TtlExpired.ToString());
        }

        /// <summary>
        ///     Gets the trace route.
        /// </summary>
        /// <param name="hostNameOrAddress">The host name or address.</param>
        /// <param name="ttl">The TTL.</param>
        /// <param name="prevStatus">The previous status.</param>
        /// <returns></returns>
        private static Pair<List<IPAddress>, List<string>> GetTraceRoute(string hostNameOrAddress, int ttl,
            string prevStatus)
        {
            Ping pinger = new Ping();
            PingOptions pingerOptions = new PingOptions(ttl, true);
            const int timeout = 12000;
            byte[] buffer = Encoding.ASCII.GetBytes(Data);
            DateTime startTime = DateTime.Now;
            PingReply reply = pinger.Send(hostNameOrAddress, timeout, buffer, pingerOptions);
            DateTime endTime = DateTime.Now;
            TimeSpan elapsedTime = endTime - startTime;

            List<IPAddress> result = new List<IPAddress>();
            List<string> statuslist = new List<string>();
            if (reply == null) return new Pair<List<IPAddress>, List<string>>(result, statuslist);
            switch (reply.Status)
            {
                case IPStatus.Success:
                    result.Add(reply.Address);
                    statuslist.Add(reply.Status.ToString() + "|" + reply.RoundtripTime + "ms");
                    break;
                case IPStatus.TtlExpired:
                    result.Add(reply.Address);
                    statuslist.Add(".." + $"|{Math.Round(elapsedTime.TotalMilliseconds)}ms");
                    if (ttl < 30)
                    {
                        Pair<List<IPAddress>, List<string>> tempResult = GetTraceRoute(hostNameOrAddress, ttl + 1,
                            reply.Status.ToString());
                        result.AddRange(tempResult.First);
                        statuslist.AddRange(tempResult.Second);
                    }
                    break;
                default:
                    result.Add(IPAddress.None);
                    statuslist.Add(reply.Status.ToString());
                    if (prevStatus == IPStatus.TtlExpired.ToString())
                    {
                        if (ttl < 30)
                        {
                            // prev hop was ok.
                            Pair<List<IPAddress>, List<string>> tempResult = GetTraceRoute(hostNameOrAddress, ttl + 1,
                                reply.Status.ToString());
                            result.AddRange(tempResult.First);
                            statuslist.AddRange(tempResult.Second);
                        }
                    }
                    break;
            }
            return new Pair<List<IPAddress>, List<string>>(result, statuslist);
        }

        #endregion
    }
}