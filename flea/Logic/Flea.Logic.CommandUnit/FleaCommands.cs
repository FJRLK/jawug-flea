using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using Flea.Data.WebserviceCommunicator;
using Flea.Data.WebserviceCommunicator.Responses;
using Flea.Logic.CommandUnit.Internals;
using Flea.Logic.IrcConnectionUnit;
using ICSharpCode.SharpZipLib.Zip;
using JHSoftware;
using log4net;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Wintellect.PowerCollections;

namespace Flea.Logic.CommandUnit
{
    public class FleaCommands
    {
        #region Types

        private delegate IPHostEntry GetHostEntryHandler(string ip);

        #endregion

        #region Static

        private static int _dnsTimeout = 300;
        private static readonly List<string> DnsServers = new List<string>();
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Internals

        private readonly Dictionary<string, string> _locationIdToCoOrds = new Dictionary<string, string>();
        private readonly List<string> _excusesList = new List<string>();
        private readonly List<string> _highSiteThreadTextResponses = new List<string>();
        private readonly Dictionary<string, string> _phoneNumberList = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _reverseDnsCache = new Dictionary<string, string>();
        private readonly List<KeyValuePair<string, string>> _tellCommands = new List<KeyValuePair<string, string>>();
        private readonly Dictionary<string, string> _usernameToGreet = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _locationNameToLocationId = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _wordsList = new Dictionary<string, string>();
        private bool _checkFreqMode;
        private Thread _freCheckThread;
        private Thread _highSiteCheckThread;
        private DateTime _lastgreetDateStamp = DateTime.Now;
        public bool masterSleep;

        /// <summary>
        ///     Gets the announce message.
        /// </summary>
        /// <value>
        ///     The announce message.
        /// </value>
        public string AnnounceMessage { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="FleaCommands" /> class.
        /// </summary>
        /// <param name="checkFreqMode">if set to <c>true</c> [check freq mode].</param>
        /// <param name="masterSleep">if set to <c>true</c> [master sleep].</param>
        public FleaCommands(bool checkFreqMode, bool masterSleep)
        {
            _checkFreqMode = checkFreqMode;
            this.masterSleep = masterSleep;
            InitDns();
            RefreshDb();
        }

        #endregion

        #region Members

        /// <summary>
        ///     Activates the sleep mode.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IrcMessage> ActivateSleepMode()
        {
            List<string> responseText = new List<string>();
            masterSleep = true;
            responseText.Add("Zzzzzz");
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Actuallies the ping ip.
        /// </summary>
        /// <param name="thisHighSite">The this high site.</param>
        /// <returns></returns>
        private string ActuallyPingIp(string thisHighSite)
        {
            try
            {
                Ping ping = new Ping();
                string ipDest = GetIpAddress(thisHighSite, _dnsTimeout);
                PingReply pingreply = ping.Send(ipDest, 5000);
                string pingStatus = "No response";
                if (pingreply != null)
                {
                    pingStatus = pingreply.Status == IPStatus.Success
                        ? "Ok!"
                        : pingreply.Status.ToString();
                }
                return pingStatus;
            }
            catch (Exception ee9)
            {
                return ("Error:" + ee9.Message);
            }
        }

        /// <summary>
        ///     Adds the announce command.
        /// </summary>
        /// <param name="mess">The mess.</param>
        private void AddAnnounceCommand(string mess)
        {
            FlushAnnounceCommandToDisk(mess);
            AnnounceMessage = mess;
        }

        /// <summary>
        ///     Adds the greet command.
        /// </summary>
        /// <param name="nick">The nick.</param>
        /// <param name="mess">The mess.</param>
        private void AddGreetCommand(string nick, string mess)
        {
            if (_usernameToGreet.ContainsKey(nick))
                _usernameToGreet[nick] = mess;
            else
                _usernameToGreet.Add(nick, mess);

            FlushGreetCommandsToDisk();
        }

        /// <summary>
        ///     Adds my SQL forward DNS record.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="name">The name.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        private bool AddMySqlForwardDnsRecord(string ip, string name, MySqlConnection connection, string mode)
        {
            try
            {
                // Check Existing A Record
                MySqlCommand selectCommand =
                    new MySqlCommand(
                        "select `name` from records where lower(`name`)=@name",
                        connection);
                selectCommand.Parameters.AddWithValue("@name", name.ToLower());
                MySqlDataReader myReader = selectCommand.ExecuteReader();
                bool safeToAdd = true;
                while (myReader.Read())
                {
                    safeToAdd = false;
                }
                myReader.Close();

                if (safeToAdd)
                {
                    string recordType = "A";
                    if (mode == "ipv6") recordType = "AAAA";
                    // ADD THE A RECORD
                    MySqlCommand insertCommand =
                        new MySqlCommand(
                            "INSERT INTO records(domain_id, `name`, `type`, content, ttl, prio) VALUES(1, @name, @rdtype, @ip, 14400, 0)",
                            connection);
                    insertCommand.Parameters.AddWithValue("@ip", ip.ToLower());
                    insertCommand.Parameters.AddWithValue("@name", name.ToLower());
                    insertCommand.Parameters.AddWithValue("@rdtype", recordType);
                    insertCommand.Parameters.AddWithValue("@name_start", name.Split('.')[0].ToLower());
                    insertCommand.ExecuteNonQuery();

                    IncrementSoaSerial(connection);

                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Adds my SQL reverse DNS record.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="name">The name.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        private bool AddMySqlReverseDnsRecord(string ip, string name, MySqlConnection connection, string mode)
        {
            if (mode == "ipv6") return false;

            // Check Existing PTR Record
            MySqlCommand selectCommand =
                new MySqlCommand(
                    "select name from records where name=lower(@name)",
                    connection);
            selectCommand.Parameters.AddWithValue("@name", ip.ToLower() + ".in-addr.arpa");
            MySqlDataReader myReader = selectCommand.ExecuteReader();
            bool safeToAdd = true;
            while (myReader.Read())
            {
                safeToAdd = false;
            }
            myReader.Close();

            if (safeToAdd)
            {
                // ADD THE PTR RECORD
                MySqlCommand insertCommand =
                    new MySqlCommand(
                        "INSERT INTO records(domain_id, `name`, `type`, content, ttl, prio) VALUES(2, @ip, 'PTR', @name, 14400, 0)",
                        connection);
                insertCommand.Parameters.AddWithValue("@ip", ip.ToLower() + ".in-addr.arpa");
                insertCommand.Parameters.AddWithValue("@name", name.ToLower());
                insertCommand.ExecuteNonQuery();

                IncrementSoaSerial(connection);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Adds the phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number.</param>
        /// <param name="nick">The nick.</param>
        public void AddPhoneNumber(string phoneNumber, string nick)
        {
            if (_phoneNumberList.ContainsKey(nick.ToLowerInvariant()))
                _phoneNumberList[nick.ToLowerInvariant()] = phoneNumber;
            else
                _phoneNumberList.Add(nick.ToLowerInvariant(), phoneNumber);

            FlushPhoneNumbersToDisk();
        }

        /// <summary>
        ///     Adds the tell command.
        /// </summary>
        /// <param name="tellCommand">The tell command.</param>
        private void AddTellCommand(KeyValuePair<string, string> tellCommand)
        {
            _tellCommands.Add(tellCommand);

            FlushTellCommandsToDisk();
        }

        /// <summary>
        ///     Adds the word command.
        /// </summary>
        /// <param name="word">The word.</param>
        /// <param name="mess">The mess.</param>
        private void AddWordCommand(string word, string mess)
        {
            word = word.ToLower();

            if (_wordsList.ContainsKey(word))
                _wordsList.Remove(word);

            _wordsList.Add(word, mess);

            FlushWordsListToDisk();
        }

        /// <summary>
        ///     Calculates the ip range.
        /// </summary>
        /// <param name="ipAddressRange">The ip address range.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> CalcIpRange(string ipAddressRange)
        {
            List<IrcMessage> responseText = new List<IrcMessage>();
            try
            {
                IPAddress ip = IPAddress.Parse(ipAddressRange.Split('/')[0]);
                int bits = int.Parse(ipAddressRange.Split('/')[1]);

                uint mask = ~(uint.MaxValue >> bits);

                // Convert the IP address to bytes.
                byte[] thisIpBytes = ip.GetAddressBytes();

                // BitConverter gives bytes in opposite order to GetAddressBytes().
                byte[] thisMaskBytes = BitConverter.GetBytes(mask);
                Array.Reverse(thisMaskBytes);

                byte[] thisStartIpBytes = new byte[thisIpBytes.Length];
                byte[] thisEndIpBytes = new byte[thisIpBytes.Length];

                // Calculate the bytes of the start and end IP addresses.
                for (int i = 0; i < thisIpBytes.Length; i++)
                {
                    thisStartIpBytes[i] = (byte)(thisIpBytes[i] & thisMaskBytes[i]);
                    thisEndIpBytes[i] = (byte)(thisIpBytes[i] | ~thisMaskBytes[i]);
                }

                // Convert the bytes to IP addresses.
                IPAddress thisStartIp = new IPAddress(thisStartIpBytes);
                IPAddress thisEndIp = new IPAddress(thisEndIpBytes);
                IPAddress hostMin = IncrementIpAddress(thisStartIp);
                IPAddress hostMax = DecrementIpAddress(thisEndIp);
                responseText.Add(new IrcMessage(
                    $"Network: {thisStartIp} Hostmin: {hostMin} HostMax: {hostMax} Broadcast: {thisEndIp} Addresses: {(Math.Pow(2, 32 - bits) - 2)}"));
            }
            catch (Exception ee)
            {
                responseText.Add(new IrcMessage("Failed: " + ee.Message));
                MakeExcuse();
            }
            return responseText;
        }

        /// <summary>
        ///     Calculates the membership status.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <returns></returns>
        public static IEnumerable<IrcMessage> CalcMembershipStatus(string memberName)
        {
            List<string> responseText = new List<string>();
            //Initialize mysql connection

            WebClient myWebClient = new WebClient();
            string responseData =
                myWebClient.DownloadString($"http://mercenary.ninja/jawug/memdb/member.php?name={memberName}");
            JArray tokenList = JArray.Parse(responseData);

            if (tokenList.Count == 0)
            {
                responseData =
                    myWebClient.DownloadString($"http://mercenary.ninja/jawug/memdb/member.php?name={memberName}%25");
                tokenList = JArray.Parse(responseData);
            }

            foreach (JToken token in tokenList)
            {
                if (token.SelectToken("name") != null)
                {
                    string paidString = (string)token.SelectToken("memdate");
                    if (paidString == "0000-00-00" || paidString == "null" || string.IsNullOrEmpty(paidString))
                        paidString = " has no payment on record";
                    else paidString = " paid up until " + paidString;
                    responseText.Add(token.SelectToken("name") + paidString);
                }
            }

            if (tokenList.Count == 0) responseText.Add("User " + memberName + " not found.. ");

            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Checks the freq thread.
        /// </summary>
        private void CheckFreqThread()
        {
            List<string> responseText = new List<string>();

            while (_checkFreqMode)
            {
                try
                {
                    string connectionString = ConfigurationManager.AppSettings["DnsConnectionMySql"];
                    MySqlConnection connection = new MySqlConnection(connectionString);
                    connection.Open();
                    MySqlCommand command =
                        new MySqlCommand("Select * from wugcentral.dnstable_Jawug where name like @searchterm",
                            connection);
                    command.Parameters.AddWithValue("searchterm", "lo0.%");
                    MySqlDataReader myReader = command.ExecuteReader();
                    int matches = 0;
                    while (myReader.Read())
                    {
                        string ipDest = GetIpAddress(myReader["rdata"].ToString(), _dnsTimeout);

                        matches++;

                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        // ReSharper disable once HeuristicUnreachableCode
                        if (!_checkFreqMode) break;

                        // Telnet to remote RB
                        TelnetConnection thisTelnetConnection = new TelnetConnection(ipDest, 23);
                        if (LoginToRb("XXXXXXXXXXXXXXXXXX", "XXXXXXXXXXXXXXX", thisTelnetConnection))
                        {
                            string result = RunCommandOnRb(thisTelnetConnection,
                                ":for i from=1 to=[:len [/int wireless find disabled=no]] do={ :put (\"SSID \".[/int wireless get ($i-1) ssid].\"@\".[/int wireless get ($i-1) frequency]); }");
                            string[] resultList = result.Split('\n');
                            int counter = 0;
                            foreach (string s in resultList)
                            {
                                counter++;
                                if (counter > 1)
                                {
                                    string cleanS = s.Replace("\r", " ")
                                        .Replace("\0", " ")
                                        .Replace(Convert.ToChar(27), ' ');
                                    if (cleanS.Contains("@") && cleanS.Contains("SSID"))
                                    {
                                        try
                                        {
                                            string isOkay = "OK";
                                            string thisFreq = cleanS.Split('@')[1];
                                            int thisItheFreq = Convert.ToInt32(thisFreq);
                                            if (thisItheFreq < 2412) isOkay = "ILLEGAL!!";
                                            if ((thisItheFreq > 2484) && (thisItheFreq < 5180)) isOkay = "ILLEGAL!!";
                                            if ((thisItheFreq > 5320) && (thisItheFreq < 5500)) isOkay = "ILLEGAL!!";
                                            if ((thisItheFreq > 5700) && (thisItheFreq < 5750)) isOkay = "ILLEGAL!!";
                                            if (thisItheFreq > 5850) isOkay = "ILLEGAL!!";

                                            if (isOkay != "OK")
                                                responseText.Add(cleanS.Split('@')[0] + " is running on " + thisFreq +
                                                                 "Mhz " + isOkay);
                                        }
                                        catch (Exception e)
                                        {
                                            responseText.Add("CheckFreq: Failure:" + e.Message);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (matches == 0)
                        responseText.Add("Sorry couldn't find any lo0's in the DNS db.. ");

                    myReader.Close();
                    connection.Close();
                }
                catch (Exception ee2)
                {
                    responseText.Add("Error: " + ee2.Message);
                }

                for (int i = 0; i < 17280; i++)
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (_checkFreqMode)
                        Thread.Sleep(5000);
            }
        }

        /// <summary>
        ///     Checks the radius servers.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IrcMessage> CheckRadiusServers()
        {
            List<string> responseText = new List<string>();

            try
            {
                WebClient myWebClient = new WebClient { Credentials = new NetworkCredential("admin", "admin") };
                string website = myWebClient.DownloadString("http://172.16.106.171:8020/scoreboards");

                Regex myRegexName = new Regex("name=(\\w)*\"", RegexOptions.IgnoreCase);
                Regex myRegexStatus = new Regex("Authentication</TD><TD>([a-z]|[A-Z]|[0-9]|\\:|\\ |\\,)*",
                    RegexOptions.IgnoreCase);
                Regex myRegexIp = new Regex("172.(16|31).\\d*.\\d*", RegexOptions.IgnoreCase);
                Regex myRegexIp2 = new Regex("^172.(16|31).\\d*.\\d*:", RegexOptions.IgnoreCase | RegexOptions.Multiline);

                MatchCollection thisMcNames = myRegexName.Matches(website);
                MatchCollection thisMcStatus = myRegexStatus.Matches(website);
                MatchCollection thisMcip = myRegexIp.Matches(website);

                Dictionary<string, List<string>> replicationErrors = new Dictionary<string, List<string>>();

                // Russian Raduis check
                string website2 = myWebClient.DownloadString("http://172.16.55.5/ros/radcheckslave.php");
                string[] list = website2.Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries);
                string lastIp = "";
                foreach (string thisT in list)
                {
                    if (thisT != "<b>MySQL slave errors</b>")
                    {
                        MatchCollection thisIpm = myRegexIp2.Matches(thisT);
                        foreach (Match m in thisIpm)
                        {
                            lastIp = m.Value.Replace(":", "");
                        }
                        if (!replicationErrors.ContainsKey(lastIp)) replicationErrors.Add(lastIp, new List<string>());
                        replicationErrors[lastIp].Add(thisT);
                    }
                }


                int i = 0;
                foreach (Match m in thisMcNames)
                {
                    string name = m.Value.Replace("name=", "").Replace("\"", "");
                    string status = thisMcStatus[i].Value.Replace("Authentication</TD><TD>", "");
                    string theIp = thisMcip[i].Value;
                    responseText.Add("Radius Server " + name + "(" + theIp + ") has status of " + status);
                    if (replicationErrors.ContainsKey(theIp))
                    {
                        responseText.AddRange(replicationErrors[theIp].Select(s => "........ Replication " + s));
                    }
                    i++;
                }
            }
            catch (Exception ee2)
            {
                responseText.Add("Radius Server Check Failed: " + ee2.Message);
                MakeExcuse();
            }
            return responseText.ToPublicIrcMessageList();
        }


        /// <summary>
        ///     Checks the TCP port.
        /// </summary>
        /// <param name="checkDest">The check dest.</param>
        /// <param name="checkPort">The check port.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> CheckTcpPort(string checkDest, string checkPort)
        {
            List<string> responseText = new List<string>();
            try
            {
                TcpClient c = new TcpClient(checkDest, Convert.ToInt32(checkPort));
                c.Close();
                responseText.Add("Port open");
            }
            catch (Exception e)
            {
                responseText.Add("Port probably closed:" + e.Message);
                MakeExcuse();
            }
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Converts the int32 to IPV4 address.
        /// </summary>
        /// <param name="ipInteger">The ip integer.</param>
        /// <returns></returns>
        private static IPAddress ConvertInt32ToIPv4Address(int ipInteger)
        {
            byte[] ipBytes = new byte[4];

            ipBytes[0] = (byte)((ipInteger >> 24) & 0xFF);
            ipBytes[1] = (byte)((ipInteger >> 16) & 0xFF);
            ipBytes[2] = (byte)((ipInteger >> 8) & 0xFF);
            ipBytes[3] = (byte)(ipInteger & 0xFF);

            return new IPAddress(ipBytes);
        }

        /// <summary>
        ///     Converts the IPV4 address to int32.
        /// </summary>
        /// <param name="ipAddr">The ip addr.</param>
        /// <returns></returns>
        private static int ConvertIPv4AddressToInt32(IPAddress ipAddr)
        {
            byte[] ipBytes = ipAddr.GetAddressBytes();

            int ipInteger = (ipBytes[0] << 24) + (ipBytes[1] << 16) + (ipBytes[2] << 8) + ipBytes[3];

            return ipInteger;
        }

        /// <summary>
        ///     Converts the kml to XML.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="outputFile">The output file.</param>
        private static void ConvertKmlToXml(string inputFile, string outputFile)
        {
            using (StreamWriter sw = new StreamWriter(outputFile))
            {
                using (StreamReader sr = new StreamReader(inputFile))
                {
                    string line;
                    int thisLine = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        thisLine++;
                        if (line.Contains("&l.")) line = line.Replace("&l.", "_AMP_");
                        sw.WriteLine(thisLine == 2 ? "<kml>" : line.Replace("& ", "&amp; "));
                    }
                }
            }
        }

        /// <summary>
        ///     Converts to co ordinates.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        /// <returns></returns>
        private Pair<string, string> ConvertToCoOrdinates(string locationId)
        {
            Pair<string, string> thisRetVal = new Pair<string, string>
            {
                First = locationId,
                Second = _locationIdToCoOrds[locationId].Replace(" ", "")
            };
            return thisRetVal;
        }

        /// <summary>
        ///     Creates the los2 link URL.
        /// </summary>
        /// <param name="searchLocationName1">The search location name1.</param>
        /// <param name="searchLocationName2">The search location name2.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> CreateLos2LinkUrl(string searchLocationName1, string searchLocationName2)
        {
            List<string> responseText = new List<string>();

            Pair<string, string> locId1 = GetLocationId(searchLocationName1);
            Pair<string, string> locId2 = GetLocationId(searchLocationName2);

            if (_locationIdToCoOrds.Count < 5) RefreshDb();

            Pair<string, string> locationId1 = ConvertToCoOrdinates(locId1.Second);
            Pair<string, string> locationId2 = ConvertToCoOrdinates(locId2.Second);
            string url =
                $"http://www.heywhatsthat.com/bin/profile.cgi?src=profiler&axes=1&curvature=0&metric=1&pt0={locationId1.Second},ff0000&pt1={locationId2.Second},00c000  {locId1.First} to {locId2.First}";
            responseText.Add(url);

            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Creates the los analysis link.
        /// </summary>
        /// <param name="search1">The search1.</param>
        /// <param name="search2">The search2.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> CreateLosAnalysisLink(string search1, string search2)
        {
            List<string> responseText = new List<string>();

            Pair<string, string> locationId1 = GetLocationId(search1);
            Pair<string, string> locationId2 = GetLocationId(search2);
            string responseString = "Could not find: ";
            if (locationId1.Second == "")
            {
                responseString += search1 + "  ";
                responseText.Add(responseString);
                return responseText.ToPublicIrcMessageList();
            }
            if (locationId2.Second == "")
            {
                responseString += search2;
                responseText.Add(responseString);
                return responseText.ToPublicIrcMessageList();
            }


            Pair<string, string> coOrd1 = ConvertToCoOrdinates(locationId1.Second);
            Pair<string, string> coOrd2 = ConvertToCoOrdinates(locationId2.Second);

            if (locationId1.Second != "" && locationId2.Second != "")
            {
                string url =
                    string.Format(
                        "http://flea.wolfen.za.net/FleaAnalyse.aspx?Name1={2}&Name2={3}&Id1={0}&Id2={1}&Lat1={4}&Lat2={5}&Lon1={6}&Lon2={7}",
                        UrlEncode(locationId1.Second.Trim()), UrlEncode(locationId2.Second.Trim()),
                        UrlEncode(locationId1.First.Trim()), UrlEncode(locationId2.First.Trim()),
                        UrlEncode(coOrd1.Second.Split(',')[0]), UrlEncode(coOrd2.Second.Split(',')[0]),
                        UrlEncode(coOrd1.Second.Split(',')[1]), UrlEncode(coOrd2.Second.Split(',')[1]));
                url = IrcEscape(url);
                responseText.Add(url);
            }
            else
                responseText.Add(responseString);

            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        /// Does additional escaping to deal with irc weirdness
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        private string IrcEscape(string url)
        {
            return url.Replace("(","%28").Replace(")", "%29");
        }

        /// <summary>
        ///     Creates the map URL.
        /// </summary>
        /// <param name="search1">The search1.</param>
        /// <param name="search2">The search2.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> CreateMapUrl(string search1, string search2)
        {
            List<IrcMessage> responseText = new List<IrcMessage>();

            Pair<string, string> locationId1 = GetLocationId(search1);
            Pair<string, string> locationId2 = GetLocationId(search2);

            if (String.IsNullOrEmpty(locationId1.Second)) responseText.Add(new IrcMessage($"Could not find a location for the search text {search1}"));;
            if (String.IsNullOrEmpty(locationId2.Second) && locationId1.First != locationId2.First)
                responseText.Add(new IrcMessage($"Could not find a location for the search text {search2}")); ;
            if (responseText.Any())
                return responseText;

            // saftey check
            if (_locationIdToCoOrds.Count < 5) responseText.AddRange(RefreshDb());

            try
            {
                Pair<string, string> coOrd1 = ConvertToCoOrdinates(locationId1.Second);

                try
                {
                    Pair<string, string> coOrd2 = ConvertToCoOrdinates(locationId2.Second);

                    if (locationId1.Second != "" && locationId2.Second != "")
                    {
                        string url =
                            string.Format(
                                "http://flea.wolfen.za.net/FleaMap.aspx?Lat1={2}&Lat2={3}&Lon1={4}&Lon2={5}&Name1={0}&Name2={1}",
                                UrlEncode(locationId1.First), UrlEncode(locationId2.First),
                                coOrd1.Second.Split(',')[0],
                                coOrd2.Second.Split(',')[0], coOrd1.Second.Split(',')[1],
                                coOrd2.Second.Split(',')[1]);
                        responseText.Add(new IrcMessage(IrcEscape(url)));
                    }
                    else
                        responseText.Add(new IrcMessage("Failed :("));
                }
                catch (KeyNotFoundException ee3)
                {
                    responseText.Add(new IrcMessage(
                        $"Could not convert {search2} ({locationId2.Second}) into co-ordinates: {ee3.Message}"));
                }
            }
            catch (KeyNotFoundException ee3)
            {
                responseText.Add(new IrcMessage(
                    $"Could not convert {search1} ({locationId1.Second}) into co-ordinates: {ee3.Message}"));
                MakeExcuse();
            }

            return responseText;
        }

        /// <summary>
        ///     Decrements the ip address.
        /// </summary>
        /// <param name="ipAddr">The ip addr.</param>
        /// <returns></returns>
        private IPAddress DecrementIpAddress(IPAddress ipAddr)
        {
            int ipInteger = ConvertIPv4AddressToInt32(ipAddr);

            ipInteger--;

            return ConvertInt32ToIPv4Address(ipInteger);
        }

        /// <summary>
        ///     Deletes the announce command.
        /// </summary>
        private void DelAnnounceCommand()
        {
            FlushAnnounceCommandToDisk(null);
            AnnounceMessage = "!IGNORE!";
        }

        /// <summary>
        ///     Deletes the greet command.
        /// </summary>
        /// <param name="nick">The nick.</param>
        private void DelGreetCommand(string nick)
        {
            if (_usernameToGreet.ContainsKey(nick))
                _usernameToGreet.Remove(nick);

            FlushGreetCommandsToDisk();
        }

        /// <summary>
        ///     Deletes the phone number.
        /// </summary>
        /// <param name="nick">The nick.</param>
        private void DelPhoneNumber(string nick)
        {
            if (_phoneNumberList.ContainsKey(nick.ToLowerInvariant()))
                _phoneNumberList.Remove(nick.ToLowerInvariant());

            FlushPhoneNumbersToDisk();
        }

        /// <summary>
        ///     Deletes the word command.
        /// </summary>
        /// <param name="word">The word.</param>
        private void DelWordCommand(string word)
        {
            word = word.ToLower();

            _wordsList.Remove(word);

            FlushWordsListToDisk();
        }

        /// <summary>
        ///     Disables the sleep mode.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IrcMessage> DisableSleepMode()
        {
            List<string> responseText = new List<string>();

            masterSleep = false;
            responseText.Add("Morning folks..");
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Displats the nat rule syntax.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IrcMessage> DisplatNatRuleSyntax()
        {
            List<string> responseText = new List<string>
            {
                "In Winbox - go to IP - Firewall - NAT - Then Press the + ",
                "       chain=srcnat ",
                "       dst-address=172.16.0.0/12 ",
                "And in the action tab set the action to masquerade "
            };

            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Display8s the ball message.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IrcMessage> Display8BallMessage()
        {
            List<string> responseText = new List<string>();

            try
            {
                string[] wordlist =
                {
                    "It is certain", "It is decidedly so", "Without a doubt", "Yes – definitely",
                    "You may rely on it",
                    "As I see it, yes", "Most likely", "Outlook good", "Signs point to yes", "Yes",
                    "Reply hazy, try again", "Ask again later", "Better not tell you now", "Cannot predict now",
                    "Concentrate and ask again", "Don't count on it", "My reply is no", "My sources say no",
                    "Outlook not so good", "Very doubtful"
                };

                Random myRandom = new Random();
                string thisRespText = $"The magic 8ball says: {wordlist[myRandom.Next(20)]}";
                responseText.Add(thisRespText);
            }
            catch (Exception ee)
            {
                responseText.Add("Failed: " + ee.Message);
                responseText.Add(MakeExcuse());
            }
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Displays the GSG link.
        /// </summary>
        /// <returns></returns>
        public string DisplayGsgLink()
        {
            return "http://www.jawug.org.za/?page_id=89";
        }

        /// <summary>
        ///     Displays the help text.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> DisplayHelpText()
        {
            List<string> returnText = new List<string>
            {
                "My commands are: (Type them with no space between the ! and the word) ",
                " ! 8BALL - Returns word list ",
                " ! ANNOUNCE msg - Tells all joining users msg on join ",
                " ! ANA place1 place2 - Returns LOS analysis url ",
                " ! CHECKFREQ - Checks all lo0 ip's for illegal freq ",
                " ! CHECKPORT ip port - Checks if a specified port is open (it's ip<space>port)",
                " ! DNS ip - Resolve the IP ",
                " ! DNSFIND search - Find a DNS entry in the DB ",
                " ! GOOGLE search - return google i'm feeling lucky url",
                " ! GSG - Returns getting started guide url ",
                " ! GREET text - Flea says text each time your nick joins (text=remove to remove)",
                " ! HELP - Shows this list ",
                " ! INVITE #channel - Invite the bot to join a channel ",
                " ! IPCALC ip/mask - Shows the Network, Broadcast, Min and Max ips for the range ",
                " ! KML - Returns KML url ",
                " ! LEAVE - MAke the bot leave the current chanel ",
                " ! LOC search - Find Locations like the search ",
                " ! LOS place1 place2 - Returns LOS url ",
                " ! LOS2 place1 place2 - Returns alternate LOS url ",
                " ! MAINTENANCE message - Shuts down flea for maintenance ",
                " ! MAKEWORD word message - Make flea say message when !word is typed  (text=remove to remove) ",
                " ! MAKEPM word message - Make flea pm message when !word <nick> is typed  (text=remove to remove) ",
                " ! MAP place1 place2 - Draws a google map from place1 to place2 ",
                " ! MAPTRACE dns/ip - Returns TraceRoute on a map ",
                " ! MEMBERS - Returns member list of all members (paid up) ",
                " ! MEMSTATUS member - Returns member status of member (paid up) ",
                " ! NEAR place1 km - Returns NEAR url. The optional km is a number of km radius to display ",
                " ! NUMBER nick cellnumber - Teaches flea the number of the nick, used for sms. Make the number 'remove' to remove ",
                " ! PASS place1 - Returns password (to authorised users) for jawugadmin on place1 ",
                " ! PING dns/ip [count] - Ping the IP or DNS entry, count is optional (up to 4) ",
                " ! RADIUS - Check the status of the radius servers ",
                " ! RBCP ip/dns - Returns rbcp url for routerboard ",
                " ! REFRESHDB - Reload co-ordinates from KML ",
                " ! REGISTER ip name - Register the ip address in dns ",
                " ! ROUTE - Prints the route command for noobs ",
                " ! RTRACE sourcedns/ip targetdns/ip - Returns TraceRoute from A to B ",
                " ! SERVICES - Prints the services url ",
                " ! SMS nick message - Sends an SMS to nick (use ! NUMBER to register the number)",
                " ! TESTKIT - Returns testkit url ",
                " ! TIME - Returns Current Date n Time ",
                " ! TRACE dns/ip - Returns TraceRoute ",
                " ! SLEEP - Put the bot to sleep, will ignore all commands ",
                " ! UNREGISTER ip name - Remove the dns fwd and reverse record (name is optional, if omitted, removes all) ",
                " ! UPTIME - Says how long the bot is connected ",
                " ! VERSION - return's flea version",
                " ! WAKE - Wake the bot up, will respond to commands ",
                " ! WEATHER - Return a bunch of weather URL's",
                " ! WGET url - Download a html file ",
                " ! WORDS - List all known response words ",
                " <bot nick> TELL <othernick> <message> - Leave a message for <othernick> ",
                masterSleep
                    ? "NOTE: I'm currently ignoring all commands."
                    : "NOTE: I'm currently responding to commands."
            };
            return returnText;
        }

        /// <summary>
        ///     Displays the KML link.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IrcMessage> DisplayKmlLink()
        {
            List<IrcMessage> responseText = new List<IrcMessage>
            {
                new IrcMessage("New: http://rbcp.jawug.org.za/kml_map.php"),
                new IrcMessage("Old: http://www.wug.za.net/newkmz.php?wug=1&download=Download+KMZ")
            };
            return responseText;
        }

        /// <summary>
        ///     Displays the member list.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IrcMessage> DisplayMemberList()
        {
            List<IrcMessage> responseText = new List<IrcMessage>();

            try
            {
                //Initialize mysql connection
                WebClient myWebClient = new WebClient();
                string responseData =
                    myWebClient.DownloadString(string.Format("http://mercenary.ninja/jawug/memdb/member.php?name=%25"));

                JArray tokenList = JArray.Parse(responseData);
                JArray lapsedMembers = new JArray();
                int i = 0;
                foreach (JToken token in tokenList)
                {
                    if (token?.SelectToken("name") == null) continue;
                    if (token.SelectToken("memdate") == null) continue;
                    string paidString = (string)token.SelectToken("memdate");
                    if (paidString != "0000-00-00" && paidString != "null" && !string.IsNullOrEmpty(paidString))
                    {
                        DateTime paidUntil;
                        if (DateTime.TryParse(paidString, out paidUntil))
                        {
                            if (paidUntil > DateTime.Now)
                            {
                                paidString = " paid up until " + paidString;
                                responseText.Add(
                                    new IrcMessage($"{token.SelectToken("name")}{paidString}", false));
                                i++;
                            }
                            else
                            {
                                if (paidUntil > DateTime.Now.AddMonths(-6))
                                    lapsedMembers.Add(token);
                            }
                        }
                        else
                        {
                            responseText.Add(
                                new IrcMessage(
                                    $"Could not understand paid until value: {token.SelectToken("name")} - {token.SelectToken("memdate")}"));
                        }
                    }
                }

                responseText.Add(new IrcMessage($"Total Members: {i}"));
                responseText.Add(new IrcMessage("Recently Lapsed Members follow (6 months)", false));

                foreach (JToken token in lapsedMembers)
                {
                    responseText.Add(
                        new IrcMessage(
                            $"{token.SelectToken("name")} paid up until {token.SelectToken("memdate")}", false));
                }
            }
            catch (Exception ee)
            {
                if (ee.InnerException != null)
                {
                    responseText.Add(
                        new IrcMessage($"Failed: {ee.Message} because {ee.InnerException.Message}"));
                }
                else
                {
                    responseText.Add(new IrcMessage($"Failed: {ee.Message}"));
                }
                MakeExcuse();
            }
            return responseText;
        }

        /// <summary>
        ///     Displays the services link.
        /// </summary>
        /// <returns></returns>
        public string DisplayServicesLink()
        {
            return "http://172.16.106.172/mwiki/index.php/JAWUG_Services";
        }

        /// <summary>
        ///     Displays the test kit link.
        /// </summary>
        /// <returns></returns>
        public string DisplayTestKitLink()
        {
            return "http://172.16.106.172/mwiki/index.php/Testkit";
        }

        /// <summary>
        ///     Displays the time.
        /// </summary>
        /// <returns></returns>
        public string DisplayTime()
        {
            return
                $"Time now is {DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}"
                ;
        }

        /// <summary>
        ///     Displays the word list.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IrcMessage> DisplayWordList()
        {
            return
                _wordsList.Keys.Select(
                    thisWord => $"! {thisWord.ToLower()} - {_wordsList[thisWord]}")
                    .ToList()
                    .ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Does the map trace.
        /// </summary>
        /// <param name="traceDest">The trace dest.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">null</exception>
        public IEnumerable<IrcMessage> DoMapTrace(string traceDest)
        {
            List<string> responseText = new List<string>();
            string ipDest = GetIpAddress(traceDest, _dnsTimeout);

            if ((ipDest.Split('.').Length < 4) && (ipDest.Split(':').Length < 2))
                throw new Exception($"DNS lookup failed for {traceDest}", null);

            Pair<List<IPAddress>, List<string>> ipList = Internals.TraceRoute.GetTraceRoute(ipDest);
            List<IPAddress> prevHops = new List<IPAddress>();
            int hop = 0;
            Guid myGuid = Guid.NewGuid();
            int registeredNodes = 0;

            foreach (IPAddress thisIp in ipList.First)
            {
                hop++;
                if (Equals(thisIp, IPAddress.None)) continue;
                string thisTxtLoop = "";
                if (prevHops.Contains(thisIp))
                    thisTxtLoop = "** LOOP!! **";


                string thisLo0Ip = GetLo0Ip(thisIp.ToString());
                if (thisLo0Ip != null)
                {
                    string mapLocation = LookupMap(thisLo0Ip);
                    if (!string.IsNullOrEmpty(mapLocation))
                    {
                        Pair<string, string> locationId = GetLocationId(mapLocation);
                        Pair<string, string> mapCoOrds = ConvertToCoOrdinates(locationId.Second);
                        const string makeUrl =
                            "http://flea.wolfen.za.net/FleaMapTrace.aspx?Id={3}&Hop={2}&Name={0}&Location={1}";
                        string thisUrl = string.Format(makeUrl, UrlEncode(mapLocation), mapCoOrds.Second, hop,
                            myGuid);
                        WebClient myWebClient = new WebClient();
                        myWebClient.DownloadString(thisUrl);
                        registeredNodes++;
                    }
                }

                prevHops.Add(thisIp);

                if (thisTxtLoop != "") break;
            }
            if (registeredNodes > 0)
                responseText.Add("Trace ready: http://flea.wolfen.za.net/FleaMapTrace.aspx?DisplayId=" + myGuid);
            else
                responseText.Add("Could not geocode any nodes.. sorry!");
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Does the near analysis.
        /// </summary>
        /// <param name="locationId1">The location id1.</param>
        /// <param name="searchTerm1">The search term1.</param>
        /// <param name="radius">The radius.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Could not find:  + searchTerm1</exception>
        public IEnumerable<IrcMessage> DoNearAnalysis(string locationId1, string searchTerm1, int radius)
        {
            List<string> responseText = new List<string>();


            // saftey check
            if (locationId1 == "") throw new Exception("Could not find: " + searchTerm1);
            if (_locationIdToCoOrds.Count < 5) RefreshDb();
            Pair<string, string> locCoOrds2 = ConvertToCoOrdinates(locationId1);
            string url = $"http://www.wug.za.net/near.php?locationid={locationId1}";
            if (radius != 0)
            {
                url += "&rad=" + radius;
            }
            responseText.Add(IrcEscape(url));

            // now to make the nearmap


            // A. Get all the location ID and names
            WebClient myWebClient = new WebClient();
            myWebClient.Headers.Add(HttpRequestHeader.Cookie,
                "PHPSESSID=ocnb3sr9nrt9kl0e0ie8r0t7f5; phpbb2mysql_data=a:2:{s:11:\"autologinid\";s:0:\"\";s:6:\"userid\";s:4:\"5687\";}; phpbb2mysql_sid=229ec2a97a77e694644ca906d7d9d0f3");
            myWebClient.Headers.Add(HttpRequestHeader.Referer, "http://www.wug.za.net/los.php");
            string myString = myWebClient.DownloadString(url);

            Regex myRegex = new Regex("<h2>(?!(Nodes within))(.)*?&tl=[0-9]+&prev=", RegexOptions.IgnoreCase);
            Match thisMatch = myRegex.Match(myString, 0);
            List<string> allLocations = new List<string>();
            while (thisMatch.Value != "")
            {
                string locationId =
                    thisMatch.Value.Replace("&tl=", ",")
                        .Replace("&prev=", "")
                        .Replace("</h2><IMG SRC=\"newlos2.php?fl=", ",")
                        .Replace("<h2>", "");
                allLocations.Add(locationId);
                thisMatch = thisMatch.NextMatch();
            }

            //B. Find the co-ordinates of all the locations
            List<Pair<string, string>> allCoords = new List<Pair<string, string>>();
            foreach (string thisT in allLocations)
            {
                try
                {
                    string locId = thisT.Split(',')[2];
                    Pair<string, string> locCoOrds = ConvertToCoOrdinates(locId);
                    locCoOrds.First = thisT;
                    allCoords.Add(locCoOrds);
                }
                catch (Exception ee2)
                {
                    responseText.Add($"Error failed to convert {thisT} to coordinates.. {ee2.Message} ");
                    MakeExcuse();
                }
            }

            // Get the centre location's coOrdinates etc
            string l2 = locCoOrds2.Second;
            string l21 = l2.Split(',')[0];
            string l22 = l2.Split(',')[1];
            float thisXx = Convert.ToSingle(l21, CultureInfo.InvariantCulture.NumberFormat);
            float thisYy = Convert.ToSingle(l22, CultureInfo.InvariantCulture.NumberFormat);

            // Sort all the coOrdinates by distance to centre
            allCoords.Sort(delegate (Pair<string, string> a, Pair<string, string> b)
            {
                string a2 = a.Second;
                string b2 = b.Second;
                string a21 = a2.Split(',')[0];
                string a22 = a2.Split(',')[1];
                string b21 = b2.Split(',')[0];
                string b22 = b2.Split(',')[1];
                float thisAx = Convert.ToSingle(a21, CultureInfo.InvariantCulture.NumberFormat);
                float thisAy = Convert.ToSingle(a22, CultureInfo.InvariantCulture.NumberFormat);
                float thisBx = Convert.ToSingle(b21, CultureInfo.InvariantCulture.NumberFormat);
                float thisBy = Convert.ToSingle(b22, CultureInfo.InvariantCulture.NumberFormat);

                double distToA = Math.Sqrt(Math.Pow(thisXx - thisAx, 2) + Math.Pow(thisYy - thisAy, 2));
                double distToB = Math.Sqrt(Math.Pow(thisXx - thisBx, 2) + Math.Pow(thisYy - thisBy, 2));
                return (distToA.CompareTo(distToB));
            });

            // Upload the coOrds
            int i2 = 0;
            allCoords.ForEach(delegate (Pair<string, string> thisCoord)
            {
                string locName = thisCoord.First.Split(',')[0];
                string locId = thisCoord.First.Split(',')[2];
                i2++;
                bool sendIt = false;
                if (locName.Contains("(") && locName.Contains(")")) // All Highsites
                {
                    sendIt = true;
                }
                else if (i2 < 30) // Top 15 nodes
                {
                    sendIt = true;
                }

                if (sendIt)
                {
                    const string makeUrl = "http://flea.wolfen.za.net/FleaNear.aspx?Node={0}&Data={1}";
                    string theUrl = string.Format(makeUrl, locationId1,
                        locId + "," + UrlEncode(locName) + "," + thisCoord.Second);
                    myWebClient.DownloadString(theUrl);
                }
            });

            // Make final URL
            const string urlFormat =
                "http://flea.wolfen.za.net/FleaNear.aspx?Node={0}&Lat1={1}&Lon1={2}&Name1={3}";
            string urlFormated = string.Format(urlFormat, locationId1, locCoOrds2.Second.Split(',')[0],
                locCoOrds2.Second.Split(',')[1], searchTerm1);
            responseText.Add(IrcEscape(urlFormated));

            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Executes the remote trace.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="traceDest">The trace dest.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        ///     null
        ///     or
        ///     null
        /// </exception>
        public IEnumerable<IrcMessage> ExecuteRemoteTrace(string traceSource, string traceDest)
        {
            List<string> responseText = new List<string>();
            string ipSource = GetIpAddress(traceSource, _dnsTimeout);
            string ipDest = GetIpAddress(traceDest, _dnsTimeout);

            if ((ipSource.Split('.').Length < 4) && (ipSource.Split(':').Length < 2))
                throw new Exception($"DNS lookup failed for {traceSource}", null);

            if ((ipDest.Split('.').Length < 4) && (ipDest.Split(':').Length < 2))
                throw new Exception($"DNS lookup failed for {traceDest}", null);

            responseText.Add($"Running Trace from {ipSource} to {ipDest}");

            // Telnet to remote RB
            TelnetConnection thisTelnetConnection = new TelnetConnection(ipSource, 23);
            if (LoginToRb("XXXXXXXXXXXXXXX", "XXXXXXXXXXXXXXXX", thisTelnetConnection))
            {
                // Run Trace
                string result = RunCommandOnRb(thisTelnetConnection, "/tool trace use-dns=yes address=" + ipDest);
                string[] resultList = result.Split('\n');
                responseText.AddRange(
                    resultList.Where(s => !s.Contains("/tool") && !s.Contains("[") && s.Trim().Length > 1));
                responseText.Add("Trace Completed.");
            }
            else
                responseText.Add("Remote trace failed. Couldn't log in .. :('");

            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Extracts the document KML.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="outputFile">The output file.</param>
        private static void ExtractDocKml(string inputFile, string outputFile)
        {
            FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.ReadWrite);

            fs.Seek(5, SeekOrigin.Begin);
            fs.WriteByte(0);
            fs.WriteByte(1);
            fs.Seek(0, SeekOrigin.Begin);

            ZipFile zf = new ZipFile(fs);
            ZipEntry ze = zf.GetEntry("doc.kml");
            ze.ForceZip64();
            Stream s = zf.GetInputStream(ze);
            //s.Length
            FileStream thisNewFile = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            byte[] buffer = new byte[10000];
            int size = 1;
            while (size > 0)
            {
                size = s.Read(buffer, 0, buffer.Length);
                thisNewFile.Write(buffer, 0, size);
            }
            fs.Close();
            zf.Close();
            s.Close();
            thisNewFile.Close();
        }

        /// <summary>
        ///     Fetches the jawug admin password.
        /// </summary>
        /// <param name="search1">The search1.</param>
        /// <returns></returns>
        public static IEnumerable<IrcMessage> FetchJawugAdminPassword(string search1)
        {
            List<IrcMessage> responseText = new List<IrcMessage>();
            //Initialize mysql connection
            string connectionString = ConfigurationManager.AppSettings["PassDBConnectionMySql"];
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            MySqlCommand myCommand =
                new MySqlCommand(
                    "select * from JawugAdminPass where ip like @sitename or identity like @sitename ",
                    connection);
            myCommand.Parameters.AddWithValue("sitename", "%" + search1 + "%");

            MySqlDataReader myReader = myCommand.ExecuteReader();
            string mainChannelMessage = "Password for ";
            while (myReader.Read())
            {
                responseText.Add(new IrcMessage(
                    $"{myReader["ip"]} ({myReader["identity"]})    Password: \u0002{myReader["currentpass"]}\u000F    Previous: {myReader["prevpass"]}   Updated: {myReader["dateupdate"]}",
                    false)
                    );
                mainChannelMessage += myReader["ip"] + " (" + myReader["identity"] + ") ";
            }
            responseText.Add(new IrcMessage(mainChannelMessage + " in PM"));
            connection.Close();

            return responseText;
        }

        /// <summary>
        ///     Flushes the announce command to disk.
        /// </summary>
        /// <param name="theMessage">The message.</param>
        private void FlushAnnounceCommandToDisk(string theMessage)
        {
            // flush announce commands file to disk
            StreamWriter fs = new StreamWriter("announcecommands.txt", false);

            fs.WriteLine(theMessage ?? "!IGNORE!");

            fs.Flush();
            fs.Close();
            Log.InfoFormat("Announce command saved: {0}", theMessage);
        }

        /// <summary>
        ///     Flushes the greet commands to disk.
        /// </summary>
        private void FlushGreetCommandsToDisk()
        {
            // flush greet commands file to disk
            StreamWriter fs = new StreamWriter("greetcommands.txt", false);
            foreach (string thisKey in _usernameToGreet.Keys)
            {
                fs.WriteLine(thisKey + "[SPLITTER]" + _usernameToGreet[thisKey]);
            }
            fs.Flush();
            fs.Close();
            Log.InfoFormat("Greet commands saved: {0}", _tellCommands.Count);
        }

        /// <summary>
        ///     Flushes the phone numbers to disk.
        /// </summary>
        private void FlushPhoneNumbersToDisk()
        {
            // flush greet commands file to disk
            StreamWriter fs = new StreamWriter("numbers.txt", false);
            foreach (string thisKey in _phoneNumberList.Keys)
            {
                fs.WriteLine(thisKey.ToLowerInvariant() + "[SPLITTER]" + _phoneNumberList[thisKey]);
            }
            fs.Flush();
            fs.Close();
            Log.InfoFormat("Phone Numbers saved: {0}", _phoneNumberList.Count);
        }

        /// <summary>
        ///     Flushes the tell commands to disk.
        /// </summary>
        private void FlushTellCommandsToDisk()
        {
            // flush tell commands file to disk
            StreamWriter fs = new StreamWriter("tellcommands.txt", false);
            foreach (KeyValuePair<string, string> tc in _tellCommands.Distinct())
            {
                fs.WriteLine(tc.Key + "[SPLITTER]" + tc.Value);
            }
            fs.Flush();
            fs.Close();
            Log.InfoFormat("Tell commands saved: {0}", _tellCommands.Count);
        }

        /// <summary>
        ///     Flushes the words list to disk.
        /// </summary>
        private void FlushWordsListToDisk()
        {
            // flush words commands file to disk
            StreamWriter fs = new StreamWriter("wordcommands.txt", false);
            foreach (string thisKey in _wordsList.Keys)
            {
                fs.WriteLine(thisKey + "[SPLITTER]" + _wordsList[thisKey]);
            }
            fs.Flush();
            fs.Close();
            Log.InfoFormat("word commands saved: {0}", _wordsList.Count);
        }

        /// <summary>
        ///     Generates the RBCP URL.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> GenerateRbcpUrl(string traceSource)
        {
            List<string> responseText = new List<string>();
            string ipSource = GetIpAddress(traceSource, _dnsTimeout);

            HttpWebResponse thisWResp;
            try
            {
                HttpWebRequest request =
                    (HttpWebRequest)
                        WebRequest.Create("http://172.16.81.2/rbcp/rb_unit_overview.php?ip=" + ipSource);
                request.AllowAutoRedirect = false;

                thisWResp = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException we)
            {
                thisWResp = ((HttpWebResponse)we.Response);
            }

            if (thisWResp == null)
            {
                responseText.Add("Communication with RBCP failed..");
                MakeExcuse();
            }
            else if (thisWResp.StatusCode == HttpStatusCode.NotFound)
                responseText.Add("The IP (" + ipSource + ") is not in the RBCP DB..");
            else
                responseText.Add("http://rbcp.services.jawug/rbcp/rb_unit_overview.php?ip=" + ipSource);

            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Gets all ip address.
        /// </summary>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="timeoutMs">The timeout ms.</param>
        /// <returns></returns>
        private string GetAllIpAddress(string hostName, int timeoutMs)
        {
            // Map a domain name to a set of IP addresses
            try
            {
                string responseValue = "";

                IPHostEntry nameToIpAddress = Dns.GetHostEntry(hostName);
                for (int i = 0; i < nameToIpAddress.AddressList.Length; i++)
                {
                    if (i > 0) responseValue += ", ";
                    responseValue += nameToIpAddress.AddressList[i].ToString();
                }
                if (responseValue == hostName)
                    return GetAlternateAllIpAddress(hostName, timeoutMs);
                return responseValue;
            }
            catch (Exception)
            {
                return GetAlternateAllIpAddress(hostName, timeoutMs);
            }
        }

        /// <summary>
        ///     Gets the alternate all ip address.
        /// </summary>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="timeoutinms">The timeoutinms.</param>
        /// <returns></returns>
        private string GetAlternateAllIpAddress(string hostName, int timeoutinms)
        {
            try
            {
                DnsClient.RequestOptions ops = MakeDnsOptions(DnsServers[0], timeoutinms);
                IPAddress[] addlist = DnsClient.LookupHost(hostName, DnsClient.IPVersion.IPv4, ops);
                string returnVal = "";
                if (addlist.Length > 0)
                {
                    for (int i = 0; i < addlist.Length; i++)
                    {
                        if (i > 0) returnVal += ", ";
                        returnVal += addlist[0].ToString();
                    }
                    return returnVal;
                }
                return hostName;
            }
            catch (Exception ee)
            {
                string thisTheMessage = ee.Message;
                if (thisTheMessage.Contains("a definitive")) thisTheMessage = "Lookup failed";
                return $"Error:{thisTheMessage}  returned from server:{DnsServers[0]}";
            }
        }

        /// <summary>
        ///     Gets the alternate ip address.
        /// </summary>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="timeoutinms">The timeoutinms.</param>
        /// <returns></returns>
        private static string GetAlternateIpAddress(string hostName, int timeoutinms)
        {
            try
            {
                DnsClient.RequestOptions opts = MakeDnsOptions(DnsServers[0], timeoutinms);
                IPAddress[] addlist = DnsClient.LookupHost(hostName, DnsClient.IPVersion.IPv4, opts);
                if (addlist.Length > 0)
                {
                    return addlist[0].ToString();
                }
                return hostName;
            }
            catch
            {
                if (!hostName.EndsWith(".jawug"))
                {
                    string tryWithJawug = GetAlternateIpAddress(hostName + ".jawug", _dnsTimeout);
                    if (tryWithJawug != hostName + ".jawug") return tryWithJawug;
                }
                return hostName;
            }
        }

        /// <summary>
        ///     Gets the alternate reverse DNS with server.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="timeoutinms">The timeoutinms.</param>
        /// <returns></returns>
        private Pair<string, string> GetAlternateReverseDnsWithServer(string ip, int timeoutinms)
        {
            string thisTheMessage = "No Dns Servers";

            foreach (string thisServer in DnsServers)
            {
                try
                {
                    DnsClient.RequestOptions ops = MakeDnsOptions(thisServer, timeoutinms);
                    string[] responseValues = DnsClient.LookupReverse(IPAddress.Parse(ip), ops);
                    if (responseValues.Length > 0)
                    {
                        _reverseDnsCache.Add(ip.ToLower(), responseValues[0]);
                        return new Pair<string, string>(thisServer, responseValues[0]);
                    }
                    thisTheMessage = "Error: Not found";
                }
                catch (Exception ee)
                {
                    thisTheMessage = ee.Message;
                    if (thisTheMessage.Contains("a definitive")) thisTheMessage = "Lookup failed (No result)";
                    if (thisTheMessage.Contains("NXDomain")) thisTheMessage = "Lookup failed (NXDomain)";
                }
            }

            return new Pair<string, string>("localhost", "Error:" + thisTheMessage);
        }

        /// <summary>
        ///     Gets the flea internet ip.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IrcMessage> GetFleaInternetIp()
        {
            List<string> responseText = new List<string>();

            try
            {
                string contents =
                    new WebClient().DownloadString("http://www.networksecuritytoolkit.org/nst/tools/ip.php");
                contents = Regex.Replace(contents, "<.*?>", string.Empty);
                contents = contents.Replace("Current IP Check", "");
                responseText.Add(contents);
            }
            catch (Exception ee)
            {
                responseText.Add("Failed to get ip .. " + ee.Message);
                MakeExcuse();
            }
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Gets the ip address.
        /// </summary>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="timeoutinms">The timeoutinms.</param>
        /// <returns></returns>
        private string GetIpAddress(string hostName, int timeoutinms)
        {
            // Map a domain name to a set of IP addresses
            string thisRetVal;
            try
            {
                IPHostEntry nameToIpAddress = Dns.GetHostEntry(hostName);

                thisRetVal = nameToIpAddress.AddressList.Length > 0
                    ? nameToIpAddress.AddressList[0].ToString()
                    : GetAlternateIpAddress(hostName, timeoutinms);
            }
            catch
            {
                thisRetVal = GetAlternateIpAddress(hostName, timeoutinms);
            }


            if (thisRetVal.StartsWith("0.0.0.")) thisRetVal = thisRetVal.Replace("0.0.0.", "172.16.250.");
            if (thisRetVal.StartsWith(hostName.Split('.')[0] + ".0.0."))
                thisRetVal = "172.16." + thisRetVal.Replace(".0.0.", ".");
            if (hostName.Split('.').Length > 2)
                if (
                    thisRetVal.StartsWith(hostName.Split('.')[0] + "." + hostName.Split('.')[1] + ".0." +
                                          hostName.Split('.')[2]))
                    thisRetVal = "172." + hostName;
            //if (retVal.StartsWith("0.")) retVal = retVal.Replace("0.", "172.");

            return thisRetVal;
        }

        /// <summary>
        ///     Gets the lo0 ip.
        /// </summary>
        /// <param name="anyip">The anyip.</param>
        /// <returns></returns>
        private string GetLo0Ip(string anyip)
        {
            string connectionString = ConfigurationManager.AppSettings["FleaConnectionMySql"];
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            MySqlCommand selectCommand = new MySqlCommand("select lo0Ip from highsiteip where ip = @ip", connection);
            selectCommand.Parameters.AddWithValue("@ip", anyip);
            object objLo0Ip = selectCommand.ExecuteScalar();
            connection.Close();
            if (objLo0Ip == null) return null;
            return objLo0Ip.ToString();
        }

        /// <summary>
        ///     Gets the location identifier.
        /// </summary>
        /// <param name="locationSearch">The location search.</param>
        /// <param name="refreshFirst">if set to <c>true</c> [refresh first].</param>
        /// <returns></returns>
        public Pair<string, string> GetLocationId(string locationSearch, bool refreshFirst = false)
        {
            if (refreshFirst) RefreshDb();

            // Safety check.. 
            if (_locationNameToLocationId.Count < 5) RefreshDb();
            if (locationSearch.Contains("%")) locationSearch = locationSearch.Replace("%", "");

            // first search for exact match
            Dictionary<string, string>.Enumerator myEnum1 = _locationNameToLocationId.GetEnumerator();
            while (myEnum1.MoveNext())
            {
                KeyValuePair<string, string> thisVal = myEnum1.Current;
                if (myEnum1.Current.Key.ToLower().Trim().Equals(locationSearch.ToLower()))
                {
                    Pair<string, string> thisRetVal = new Pair<string, string>
                    {
                        First = thisVal.Key,
                        Second = thisVal.Value
                    };
                    Log.Info($"Converted location search text: {locationSearch} into coords {thisRetVal}");
                    return thisRetVal;
                }
            }

            // second search for partial match
            Dictionary<string, string>.Enumerator myEnum = _locationNameToLocationId.GetEnumerator();
            while (myEnum.MoveNext())
            {
                KeyValuePair<string, string> thisVal = myEnum.Current;
                if (myEnum.Current.Key.ToLower().Contains(locationSearch.ToLower()))
                {
                    Pair<string, string> thisRetVal = new Pair<string, string>
                    {
                        First = thisVal.Key,
                        Second = thisVal.Value
                    };
                    Log.Info($"Converted location search text: {locationSearch} into coords {thisRetVal}");
                    return thisRetVal;
                }
            }

            // Try refreshing the DB
            if (!refreshFirst) return GetLocationId(locationSearch, true);

            // if you get here - nothing found - return "null"
            Pair<string, string> thisRetVal2 = new Pair<string, string> { First = locationSearch, Second = "" };
            Log.Info($"Converted location search text: {locationSearch} into coords {thisRetVal2} (NOT FOUND)");
            return thisRetVal2;
        }

        /// <summary>
        ///     Gets the reverse DNS.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="timeoutinms">The timeoutinms.</param>
        /// <returns></returns>
        private string GetReverseDns(string ip, int timeoutinms)
        {
            return GetReverseDnsWithServer(ip, timeoutinms).Second;
        }

        /// <summary>
        ///     Gets the reverse DNS with server.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="timeoutMs">The timeout ms.</param>
        /// <returns></returns>
        private Pair<string, string> GetReverseDnsWithServer(string ip, int timeoutMs)
        {
            try
            {
                if (_reverseDnsCache.ContainsKey(ip.ToLower()))
                {
                    return new Pair<string, string>("Cache", _reverseDnsCache[ip.ToLower()]);
                }

                GetHostEntryHandler callback = Dns.GetHostEntry;
                IAsyncResult result = callback.BeginInvoke(ip, null, null);
                if (result.AsyncWaitHandle.WaitOne(timeoutMs, false))
                {
                    string hostname = callback.EndInvoke(result).HostName;
                    if (hostname == ip) return GetAlternateReverseDnsWithServer(ip, timeoutMs);
                    _reverseDnsCache.Add(ip.ToLower(), hostname);
                    return new Pair<string, string>("localhost", hostname);
                }
                return GetAlternateReverseDnsWithServer(ip, timeoutMs);
            }
            catch (Exception)
            {
                return GetAlternateReverseDnsWithServer(ip, timeoutMs);
            }
        }


        /// <summary>
        ///     Increments the ip address.
        /// </summary>
        /// <param name="ipAddr">The ip addr.</param>
        /// <returns></returns>
        private static IPAddress IncrementIpAddress(IPAddress ipAddr)
        {
            int ipInteger = ConvertIPv4AddressToInt32(ipAddr);

            ipInteger++;

            return ConvertInt32ToIPv4Address(ipInteger);
        }

        /// <summary>
        ///     Increments the soa serial.
        /// </summary>
        /// <param name="connection">The connection.</param>
        private static void IncrementSoaSerial(MySqlConnection connection)
        {
            // UPDATE THE SOA RECORD
            MySqlCommand updateCommand =
                new MySqlCommand(
                    "update records set content = replace(content, REPLACE(SUBSTRING(SUBSTRING_INDEX(content, ' ', 3), LENGTH(SUBSTRING_INDEX(content, ' ', 2)) + 1),' ', ''), REPLACE(SUBSTRING(SUBSTRING_INDEX(content, ' ', 3), LENGTH(SUBSTRING_INDEX(content, ' ', 2)) + 1),' ', '')+1) where type = 'SOA'",
                    connection);
            //UpdateCommand.Parameters.Add("@ItemNumber", MySqlDbType.Int16, 4, "ItemNumber");
            updateCommand.ExecuteNonQuery();
        }

        /// <summary>
        ///     Initializes the DNS.
        /// </summary>
        private static void InitDns()
        {
            // Read DNS from Config File
            DnsServers.Clear();
            string dnsServersConfig = ConfigurationManager.AppSettings["DnsServers"];

            string[] allDnsServers = dnsServersConfig.Split(' ');
            foreach (string thisServer in allDnsServers)
            {
                DnsServers.Add(thisServer);
            }

            _dnsTimeout = Convert.ToInt32(ConfigurationManager.AppSettings["DnsTimeout"]);
        }


        /// <summary>
        ///     Loads the coords from file.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        private void LoadCoordsFromFile(string inputFile)
        {
            using (StreamReader sr = new StreamReader(inputFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] thisNode = line.Split(new[] { "/*888*/" }, StringSplitOptions.RemoveEmptyEntries);
                    if (thisNode.Length == 3)
                    {
                        string[] locsplit = thisNode[2].Split(new[] { '&', ':' });
                        if (locsplit.Length > 2)
                        {
                            string[] coordssplit = thisNode[1].Replace(",0", "").Split(',');
                            _locationIdToCoOrds.Add(locsplit[1].Trim(), coordssplit[1] + "," + coordssplit[0]);
                            string thisTheName = thisNode[0];
                            if (!_locationNameToLocationId.ContainsKey(thisTheName))
                                _locationNameToLocationId.Add(thisTheName, locsplit[1].Trim());

                            //Logger.WriteLine(string.Format("{2} ({0}) = {1}", locsplit[1].Trim(), thisNode[1], thisNode[0]));
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Logins to rb.
        /// </summary>
        /// <param name="wuguser">The wuguser.</param>
        /// <param name="wugpass">The wugpass.</param>
        /// <param name="telnetConnection">The telnet connection.</param>
        /// <returns></returns>
        private static bool LoginToRb(string wuguser, string wugpass, TelnetConnection telnetConnection)
        {
            string input = "";
            DateTime startTime = DateTime.Now;
            while (telnetConnection.IsConnected)
            {
                string thisbit = telnetConnection.Read();
                if (thisbit.Length > 0)
                {
                    input += thisbit;
                    //Logger.Write(thisbit);

                    if (input.EndsWith("Login: "))
                    {
                        //Logger.WriteLine("\r\n*LOGIN DETECTED*");
                        telnetConnection.WriteLine(wuguser);
                    }

                    if (input.EndsWith("Password: "))
                    {
                        //Logger.WriteLine("\r\n*PASSWORD DETECTED*");
                        telnetConnection.WriteLine(wugpass);
                    }

                    if (input.EndsWith("> "))
                    {
                        //Logger.WriteLine("\r\n*PROMPT DETECTED*");
                        return true;
                    }

                    if (input.Contains("Too many failures"))
                    {
                        return false;
                    }
                }
                // timeout
                if ((DateTime.Now - startTime).TotalSeconds > 30) return false;
            }
            return false;
        }

        /// <summary>
        ///     Lookups the map.
        /// </summary>
        /// <param name="lo0Ip">The lo0 ip.</param>
        /// <returns></returns>
        private string LookupMap(string lo0Ip)
        {
            string connectionString = ConfigurationManager.AppSettings["FleaConnectionMySql"];
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            MySqlCommand selectCommand = new MySqlCommand("select plotted_as from highsite where ip = @ip", connection);
            selectCommand.Parameters.AddWithValue("@ip", lo0Ip);
            object thisObjLocation = selectCommand.ExecuteScalar();
            connection.Close();
            if (thisObjLocation == null) return null;
            return thisObjLocation.ToString();
        }

        /// <summary>
        ///     Maintains the word command.
        /// </summary>
        /// <param name="mess">The mess.</param>
        /// <param name="word">The word.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> MaintainWordCommand(string mess, string word)
        {
            List<string> responseText = new List<string>();
            if (mess.ToLower().Contains("remove"))
            {
                DelWordCommand(word);
                responseText.Add("word for " + word + " removed");
            }
            else
            {
                AddWordCommand(word, mess);
                responseText.Add("cool, i'll respond to !" + word + " with " + mess);
            }
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Makes the action.
        /// </summary>
        /// <param name="actionText">The action text.</param>
        /// <returns></returns>
        public static IEnumerable<IrcMessage> MakeAction(string actionText)
        {
            List<string> responseText = new List<string> { actionText };
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Makes the DNS entry.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="ip">The ip.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        ///     Name not valid ( + name + )
        ///     or
        ///     Name not valid ( + name + )
        ///     or
        ///     Name not valid ( + name + ) Syntax is toplace.here.jawug
        ///     or
        ///     IP not valid ( + ip + )
        ///     or
        ///     IP not valid ( + ip + )
        /// </exception>
        public IEnumerable<IrcMessage> MakeDnsEntry(string name, string ip)
        {
            List<string> responseText = new List<string>();

            if (name.Contains("_")) name = name.Replace("_", ".");
            if (!name.EndsWith(".jawug")) name += ".jawug";
            if (name.Contains("..")) throw new Exception("Name not valid (" + name + ")");
            if (name.StartsWith(".")) throw new Exception("Name not valid (" + name + ")");
            if (name.Split('.').Length < 3)
                throw new Exception("Name not valid (" + name + ") Syntax is toplace.here.jawug ");

            string mode = "ipv4";
            if (ip.StartsWith("2001:43f8:360")) mode = "ipv6";

            if (mode == "ipv4")
            {
                if ((!ip.StartsWith("172.16.") && (!ip.StartsWith("172.31."))) || (ip.Split('.').Length != 4))
                    throw new Exception("IP not valid (" + ip + ")");
            }
            else
            {
                if (!ip.StartsWith("2001:43f8:360"))
                    throw new Exception("IP not valid (" + ip + ")");
            }

            string responseString = "Registering " + mode + " " + ip + " to " + name;
            responseText.Add(responseString);

            //RegisterRecordInBind(SourceChannel, ip, name);

            //Initialize mysql connection
            string connectionString = ConfigurationManager.AppSettings["DnsConnectionMySql"];
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();
            bool fwdAdded = AddMySqlForwardDnsRecord(ip, name, connection, mode);
            bool revAdded = false;
            if (mode != "ipv6") revAdded = AddMySqlReverseDnsRecord(ReverseIp(ip), name, connection, mode);
            connection.Close();

            string result = "Finished:";
            if (fwdAdded) result += " Forward Added.";
            else result += " Forward NOT added, already exists... ";

            if (revAdded) result += " Reverse Added.";
            else if (mode == "ipv4") result += " Already had reverse: " + GetReverseDns(ip, _dnsTimeout);
            else result += " ipv6 reverse disabled, pending Xarion creating reverse zone";
            responseText.Add(result);

            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Makes the DNS options.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="timeoutinms">The timeoutinms.</param>
        /// <returns></returns>
        private static DnsClient.RequestOptions MakeDnsOptions(string ip, int timeoutinms)
        {
            DnsClient.RequestOptions ops = new DnsClient.RequestOptions
            {
                DnsServers = new[] { IPAddress.Parse(ip) },
                RetryCount = 2,
                RequestRecursion = true,
                TimeOut = new TimeSpan(0, 0, 0, 0, timeoutinms)
            };
            return ops;
        }

        /// <summary>
        ///     Makes the excuse.
        /// </summary>
        /// <returns></returns>
        public string MakeExcuse()
        {
            Random myRandom = new Random(DateTime.Now.Millisecond);
            int chosenExcuse = myRandom.Next(_excusesList.Count);
            return $"It must be because {_excusesList[chosenExcuse]}";
        }

        /// <summary>
        ///     Makes the google lucky URL.
        /// </summary>
        /// <param name="search">The search.</param>
        /// <returns></returns>
        public string MakeGoogleLuckyUrl(string search)
        {
            search = IrcEscape(UrlEncode(search));
            return $"http://www.google.com/search?q={search}&btnI";
        }

        /// <summary>
        ///     Makes the los link.
        /// </summary>
        /// <param name="search1">The search1.</param>
        /// <param name="search2">The search2.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> MakeLosLink(string search1, string search2)
        {
            List<string> responseText = new List<string>();

            Pair<string, string> locationId1 = GetLocationId(search1);
            Pair<string, string> locationId2 = GetLocationId(search2);
            string responseString = "Could not find: ";
            if (locationId1.Second == "")
            {
                responseString += search1 + "  ";
            }
            if (locationId2.Second == "")
            {
                responseString += search2;
            }

            if (locationId1.Second != "" && locationId2.Second != "")
            {
                string url =
                    $"http://www.wug.za.net/newlos2.php?fl={locationId1.Second}&tl={locationId2.Second}    {locationId1.First} to {locationId2.First}";
                responseText.Add(url);
            }
            else
                responseText.Add(responseString);

            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Pings the specified trace dest.
        /// </summary>
        /// <param name="traceDest">The trace dest.</param>
        /// <param name="traceCount">The trace count.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> Ping(string traceDest, short traceCount)
        {
            List<string> responseText = new List<string>();
            // make counting vars
            int success = 0;
            float totalTime = 0;
            float minTime = float.MaxValue;
            float maxTime = float.MinValue;

            for (int i = 0; i < traceCount; i++)
            {
                Ping ping = new Ping();
                string ipDest = GetIpAddress(traceDest, _dnsTimeout);


                PingReply pingreply = ping.Send(ipDest, 5000);

                if (pingreply != null && pingreply.Status == IPStatus.Success)
                {
                    responseText.Add(
                        $"Time: {pingreply.RoundtripTime}ms IP:{GetIpAddress(traceDest, _dnsTimeout)} ({GetReverseDns(pingreply.Address.ToString(), _dnsTimeout)})");
                    totalTime += pingreply.RoundtripTime;
                    success++;
                    if (pingreply.RoundtripTime > maxTime) maxTime = pingreply.RoundtripTime;
                    if (pingreply.RoundtripTime < minTime) minTime = pingreply.RoundtripTime;
                }
                else
                {
                    if (pingreply != null && pingreply.Address != null)
                        responseText.Add(
                            $"Time: {pingreply.RoundtripTime}ms  Status: {pingreply.Status} IP:{GetIpAddress(traceDest, _dnsTimeout)} (Failed at {GetReverseDns(pingreply.Address.ToString(), _dnsTimeout)})");
                    else if (pingreply != null)
                        responseText.Add(
                            $"Time: {pingreply.RoundtripTime}ms  Status: {pingreply.Status}   IP:{GetIpAddress(traceDest, _dnsTimeout)} ({GetReverseDns(ipDest, _dnsTimeout)})");
                }
            }

            if ((traceCount <= 1) || (success <= 0)) return responseText.ToPublicIrcMessageList();

            float avgTime = totalTime / success;
            responseText.Add(
                $"Minimum = {minTime} ms, Maximum = {maxTime} ms, Average = {avgTime} ms");
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Posts the weather links.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IrcMessage> PostWeatherLinks()
        {
            List<string> responseText = new List<string>
            {
                "Google says : http://goo.gl/V4bAC",
                "WUnderground says : http://goo.gl/GqPqJ",
                "Bryanston weather station : http://goo.gl/OoqCg",
                "Weather SA says : http://goo.gl/C1TC5"
            };

            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Processes the greet commands.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> ProcessGreetCommands(string userName)
        {
            List<string> responseText = new List<string>();

            if ((DateTime.Now - _lastgreetDateStamp).TotalSeconds > 60)
            {
                _lastgreetDateStamp = DateTime.Now;
                if (_usernameToGreet.ContainsKey(userName))
                    responseText.Add(userName + ", " + _usernameToGreet[userName]);
            }

            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Processes the tell commands.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="sourceType">Type of the source.</param>
        /// <param name="sourceChannel">The source channel.</param>
        /// <param name="ircObject">The irc object.</param>
        public void ProcessTellCommands(string userName, string sourceType, string sourceChannel, IrcConnection ircObject)
        {
            if (ircObject == null) throw new ArgumentNullException(nameof(ircObject));
            if (sourceType != "PRIVMSG") return;

            // get any tells
            List<KeyValuePair<string, string>> tells =
                _tellCommands.Where(
                    thisTell => string.Equals(userName, thisTell.Key, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

            if (!tells.Any()) return;
            ircObject.PostToChannel(new IrcMessage($"{userName}, check PM for message(s)"), sourceChannel);
            foreach (KeyValuePair<string, string> thisTell in tells)
            {
                ircObject.SendPrivateMessageToUser(thisTell.Value, userName);
                _tellCommands.Remove(thisTell);
            }
            FlushTellCommandsToDisk();
        }

        /// <summary>
        ///     Processes the word commands.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> ProcessWordCommands(string ircCommand)
        {
            List<string> responseText = new List<string>();

            foreach (string thisWord in _wordsList.Keys)
            {
                if (!ircCommand.ToLower().Contains("!" + thisWord)) continue;
                string[] ircWords = ircCommand.Split(' ');
                responseText.AddRange(from t in ircWords
                                      where t.ToLower().Replace(":", "") == ("!" + thisWord)
                                      select _wordsList[thisWord]);
            }

            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Reads the announce command from disk.
        /// </summary>
        private void ReadAnnounceCommandFromDisk()
        {
            // flush tell commands file to disk
            AnnounceMessage = "!IGNORE!";
            if (File.Exists("announcecommands.txt"))
            {
                StreamReader fs = new StreamReader("announcecommands.txt");
                while (!fs.EndOfStream)
                {
                    string thisAnnounce = fs.ReadLine();
                    AnnounceMessage = thisAnnounce;
                }
                Log.InfoFormat("Announce commands loaded: {0}", 1);
                fs.Close();
            }
        }

        /// <summary>
        ///     Reads the configuration from disk.
        /// </summary>
        public void ReadConfigFromDisk()
        {
            // Read config From Disk
            SafeRunCommand(ReadTellCommandsFromDisk);
            SafeRunCommand(ReadTellCommandsFromDisk);
            SafeRunCommand(ReadGreetCommandsFromDisk);
            SafeRunCommand(ReadAnnounceCommandFromDisk);
            SafeRunCommand(ReadWordCommandsFromDisk);
            SafeRunCommand(ReadExcusesFromDisk);
            SafeRunCommand(ReadPhoneNumbersFromDisk);
        }

        /// <summary>
        ///     Reads the excuses from disk.
        /// </summary>
        private void ReadExcusesFromDisk()
        {
            // flush tell commands file to disk
            try
            {
                StreamReader fs = new StreamReader("excuses.txt");
                while (!fs.EndOfStream)
                {
                    string thisWordCommand = fs.ReadLine();
                    _excusesList.Add(thisWordCommand);
                }
                Log.InfoFormat("Excuses loaded: {0}", _excusesList.Count);
                fs.Close();
            }
            catch (Exception ee)
            {
                Log.ErrorFormat("Failed to load Excuses: " + ee.Message, ee);
            }
        }

        /// <summary>
        ///     Reads the greet commands from disk.
        /// </summary>
        private void ReadGreetCommandsFromDisk()
        {
            // flush tell commands file to disk
            try
            {
                StreamReader fs = new StreamReader("greetcommands.txt");
                while (!fs.EndOfStream)
                {
                    string thisTell = fs.ReadLine();
                    string[] splitterval = { "[SPLITTER]" };
                    Debug.Assert(thisTell != null, "thisTell != null");
                    _usernameToGreet.Add(thisTell.Split(splitterval, StringSplitOptions.None)[0],
                        thisTell.Split(splitterval, StringSplitOptions.None)[1]);
                }
                Log.InfoFormat("Greet commands loaded: {0}", _usernameToGreet.Count);
                fs.Close();
            }
            catch (Exception ee)
            {
                Log.ErrorFormat("Failed to load Greet Commands: " + ee.Message, ee);
            }
        }

        /// <summary>
        ///     Reads the phone numbers from disk.
        /// </summary>
        private void ReadPhoneNumbersFromDisk()
        {
            // flush tell commands file to disk
            try
            {
                StreamReader fs = new StreamReader("numbers.txt");
                while (!fs.EndOfStream)
                {
                    string thisNumber = fs.ReadLine();
                    string[] splitterval = { "[SPLITTER]" };
                    Debug.Assert(thisNumber != null, "thisNumber != null");
                    _phoneNumberList.Add(thisNumber.Split(splitterval, StringSplitOptions.None)[0].ToLowerInvariant(),
                        thisNumber.Split(splitterval, StringSplitOptions.None)[1]);
                }
                Log.InfoFormat("Phone Numbers loaded: {0}", _wordsList.Count);
                fs.Close();
            }
            catch (Exception ee)
            {
                Log.ErrorFormat("Failed to load Phone Numbers: " + ee.Message, ee);
            }
        }

        /// <summary>
        ///     Reads the tell commands from disk.
        /// </summary>
        private void ReadTellCommandsFromDisk()
        {
            // flush tell commands file to disk
            StreamReader fs = new StreamReader("tellcommands.txt");
            while (!fs.EndOfStream)
            {
                string thisTell = fs.ReadLine();
                string[] splitterval = { "[SPLITTER]" };
                Debug.Assert(thisTell != null, "thisTell != null");
                KeyValuePair<string, string> tc =
                    new KeyValuePair<string, string>(
                        thisTell.Split(splitterval, StringSplitOptions.None)[0].ToLowerInvariant(),
                        thisTell.Split(splitterval, StringSplitOptions.None)[1]);
                _tellCommands.Add(tc);
            }
            Log.InfoFormat("Tell commands loaded: {0}", _tellCommands.Count);
            fs.Close();
        }

        /// <summary>
        ///     Reads the word commands from disk.
        /// </summary>
        private void ReadWordCommandsFromDisk()
        {
            // flush tell commands file to disk
            try
            {
                StreamReader fs = new StreamReader("wordcommands.txt");
                while (!fs.EndOfStream)
                {
                    string thisWordCommand = fs.ReadLine();
                    string[] splitterval = { "[SPLITTER]" };
                    Debug.Assert(thisWordCommand != null, "thisWordCommand != null");
                    _wordsList.Add(thisWordCommand.Split(splitterval, StringSplitOptions.None)[0],
                        thisWordCommand.Split(splitterval, StringSplitOptions.None)[1]);
                }
                Log.InfoFormat("Word commands loaded: {0}", _wordsList.Count);
                fs.Close();
            }
            catch (Exception ee)
            {
                Log.ErrorFormat("Failed to load Word Commands: " + ee.Message, ee);
            }
        }

        /// <summary>
        ///     Refreshes the database.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IrcMessage> RefreshDb()
        {
            List<string> responseText = new List<string>();

            try
            {
                WebClient myWebClient = new WebClient();
                Log.InfoFormat("Downloading KMZ files..");
                myWebClient.DownloadFile("http://www.wug.za.net/newkmz.php?wug=1&download=Download+KMZ", "kmz_1.zip");
                myWebClient.DownloadFile("http://www.wug.za.net/newkmz.php?wug=15&download=Download+KMZ", "kmz_2.zip");

                Log.InfoFormat("Unzipping KML files...");
                ExtractDocKml("kmz_1.zip", "doc1.kml");
                ExtractDocKml("kmz_2.zip", "doc2.kml");

                Log.InfoFormat("Converting KML files to XML files ...");
                ConvertKmlToXml("doc1.kml", "doc1.xml");
                ConvertKmlToXml("doc2.kml", "doc2.xml");

                Log.InfoFormat("Converting XML files to co-ordinates ...");
                RunXslTransform("doc1.xml", "CoOrd.xslt", "result1.dat");
                RunXslTransform("doc2.xml", "CoOrd.xslt", "result2.dat");

                Log.InfoFormat("Clearing internal cache..");
                _locationIdToCoOrds.Clear();
                _locationNameToLocationId.Clear();

                Log.InfoFormat("Loading co-ordinates from file ...");
                LoadCoordsFromFile("result1.dat");
                LoadCoordsFromFile("result2.dat");
                Log.InfoFormat("{0} co-ordinates loaded..", _locationIdToCoOrds.Count);
                Log.InfoFormat("{0} username to id's loaded..", _locationNameToLocationId.Count);

                responseText.Add("Co-Ordinates (re)loaded");
            }
            catch (Exception ee2)
            {
                Log.Error("Failure during Co-Ordinate DB refresh: " + ee2.Message, ee2);
                responseText.Add("Failed to fetch and process KML: " + ee2.Message + " " + ee2.StackTrace);
            }
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Removes the DNS entry.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="ip">The ip.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        ///     Name not valid ( + name + )
        ///     or
        ///     Name not valid ( + name + )
        ///     or
        ///     IP not valid ( + ip + )
        ///     or
        ///     IP not valid ( + ip + )
        /// </exception>
        public IEnumerable<IrcMessage> RemoveDnsEntry(string name, string ip)
        {
            List<string> responseText = new List<string>();
            if (name.Contains("_")) name = name.Replace("_", ".");
            if (!name.EndsWith(".jawug")) name += ".jawug";
            if (name.Contains("..")) throw new Exception("Name not valid (" + name + ")");
            if (name.StartsWith(".")) throw new Exception("Name not valid (" + name + ")");

            string mode = "ipv4";
            if (ip.StartsWith("2001:43f8:360")) mode = "ipv6";

            if (mode == "ipv4")
            {
                if ((!ip.StartsWith("172.16.") && (!ip.StartsWith("172.31."))) || (ip.Split('.').Length != 4))
                    throw new Exception("IP not valid (" + ip + ")");
            }
            else
            {
                if (!ip.StartsWith("2001:43f8:360"))
                    throw new Exception("IP not valid (" + ip + ")");
            }


            //Initialize mysql connection
            string connectionString = ConfigurationManager.AppSettings["DnsConnectionMySql"];
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            responseText.AddRange(RemoveMySqlForwardDnsRecords(ip, name, connection));
            if (mode == "ipv4") responseText.AddRange(RemoveMySqlReverseDnsRecords(ReverseIp(ip), name, connection));

            connection.Close();

            responseText.Add("Finished.");
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Removes my SQL forward DNS records.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="name">The name.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        private IEnumerable<string> RemoveMySqlForwardDnsRecords(string ip, string name, MySqlConnection connection)
        {
            List<string> responseText = new List<string>();

            // Find Fwd Matching Records  //  and dnsname = @name
            MySqlCommand selectCommand = new MySqlCommand("select * from records where lower(content) like @ip ",
                connection);
            selectCommand.Parameters.AddWithValue("@ip", ip.ToLower() + "%");
            selectCommand.Parameters.AddWithValue("@name", name.ToLower());
            MySqlDataReader wolfReader = selectCommand.ExecuteReader();
            List<MySqlCommand> delList = new List<MySqlCommand>();
            while (wolfReader.Read())
            {
                string result = wolfReader.GetString("name").ToLower() + " - " +
                                wolfReader.GetString("content").ToLower();
                if (wolfReader.GetString("name").ToLower().StartsWith(name.ToLower()) || name.ToLower() == "all.jawug")
                {
                    result += " removing..";
                    // DO THE ACTUAL REMOVE
                    MySqlCommand deleteCommand =
                        new MySqlCommand("delete from records where lower(name)=@name and lower(content)=@ip",
                            connection);
                    deleteCommand.Parameters.AddWithValue("@name", wolfReader.GetString("name").ToLower());
                    deleteCommand.Parameters.AddWithValue("@ip", wolfReader.GetString("content").ToLower());
                    delList.Add(deleteCommand);
                }
                else result += " ignoring..";
                responseText.Add("Dns Record " + result);
            }
            wolfReader.Close();
            foreach (MySqlCommand comm in delList)
            {
                comm.ExecuteNonQuery();
            }
            return responseText;
        }

        /// <summary>
        ///     Removes my SQL reverse DNS records.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="name">The name.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        private IEnumerable<string> RemoveMySqlReverseDnsRecords(string ip, string name, MySqlConnection connection)
        {
            List<string> responseText = new List<string>();

            // Find Rev Matching Records  //  and dnsname = @name
            MySqlCommand selectCommand = new MySqlCommand(
                "select * from records where lower(name) like @ip ", connection);
            selectCommand.Parameters.AddWithValue("@ip", ip.ToLower() + "%");
            selectCommand.Parameters.AddWithValue("@name", name);
            MySqlDataReader wolfReader = selectCommand.ExecuteReader();
            List<MySqlCommand> delList = new List<MySqlCommand>();
            while (wolfReader.Read())
            {
                string result = wolfReader.GetString("name").ToLower() + " - " +
                                wolfReader.GetString("content").ToLower();
                if (wolfReader.GetString("content").ToLower().StartsWith(name.ToLower()) ||
                    name.ToLower() == "all.jawug")
                {
                    result += " removing..";
                    // DO THE ACTUAL REMOVE
                    MySqlCommand deleteCommand =
                        new MySqlCommand("delete from records where lower(name)=@name and lower(content)=@ip",
                            connection);
                    deleteCommand.Parameters.AddWithValue("@name", wolfReader.GetString("name").ToLower());
                    deleteCommand.Parameters.AddWithValue("@ip", wolfReader.GetString("content").ToLower());
                    delList.Add(deleteCommand);
                }
                else result += " ignoring..";
                responseText.Add("Dns Record " + result);
            }
            wolfReader.Close();
            foreach (MySqlCommand comm in delList)
            {
                comm.ExecuteNonQuery();
            }

            return responseText;
        }

        /// <summary>
        ///     Resolves the DNS.
        /// </summary>
        /// <param name="traceDest">The trace dest.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> ResolveDns(string traceDest)
        {
            List<string> responseText = new List<string>();
            DateTime dnsStartTime = DateTime.Now;
            Pair<string, string> reverseDns = GetReverseDnsWithServer(traceDest, _dnsTimeout * 10);
            DateTime reverseTime = DateTime.Now;

            string ipDns = GetAllIpAddress(traceDest, _dnsTimeout * 10);
            if (!reverseDns.Second.StartsWith("Error"))
                responseText.Add(
                    $"Reverse DNS: {reverseDns.Second} for {traceDest} (Request took: {Convert.ToInt32((reverseTime - dnsStartTime).TotalMilliseconds)}ms from {reverseDns.First})");
            if (!ipDns.StartsWith("Error"))
                responseText.Add(
                    "All IP(s): " + ipDns + " for " + traceDest + " (Request took: " +
                    Convert.ToInt32((DateTime.Now - reverseTime).TotalMilliseconds) + "ms)");
            if ((reverseDns.Second.StartsWith("Error")) && (ipDns.StartsWith("Error")))
                responseText.Add("Error: " + ipDns);
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Returns the version.
        /// </summary>
        /// <returns></returns>
        public string ReturnVersion()
        {
            AssemblyFileVersionAttribute attribute = (AssemblyFileVersionAttribute)Assembly
                .GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true)
                .Single();
            return $"The system is currently running version {attribute.Version}";
        }

        /// <summary>
        ///     Reverses the ip.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <returns></returns>
        private static string ReverseIp(string ip)
        {
            string[] iparray = ip.Split('.');
            return iparray[3] + "." + iparray[2] + '.' + iparray[1] + '.' + iparray[0];
        }

        /// <summary>
        ///     Runs the command on rb.
        /// </summary>
        /// <param name="telnetConnection">The telnet connection.</param>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        private static string RunCommandOnRb(TelnetConnection telnetConnection, string command)
        {
            telnetConnection.WriteLine(command + "\r\n");

            string input = "";
            while (telnetConnection.IsConnected)
            {
                string thisbit = telnetConnection.Read();
                if (thisbit.Length > 0)
                {
                    input += thisbit;
                    //Logger.Write(thisbit);

                    if (input.EndsWith("> "))
                    {
                        //Logger.WriteLine("\r\n*PROMPT DETECTED*");
                        return input;
                    }
                }
            }
            return input;
        }

        /// <summary>
        ///     Runs the DNS search on database.
        /// </summary>
        /// <param name="searchterm">The searchterm.</param>
        /// <returns></returns>
        private List<Pair<string, string>> RunDnsSearchOnDb(string searchterm)
        {
            string connectionString = ConfigurationManager.AppSettings["DnsConnectionMySql"];
            MySqlConnection connection = new MySqlConnection(connectionString);

            connection.Open();
            MySqlCommand command =
                new MySqlCommand("Select * from wugcentral.dnstable_Jawug where name like @searchterm",
                    connection);
            command.Parameters.AddWithValue("searchterm", "%" + searchterm + "%");
            MySqlDataReader myReader = command.ExecuteReader();
            List<Pair<string, string>> resultList = new List<Pair<string, string>>();
            while (myReader.Read())
            {
                resultList.Add(new Pair<string, string>(myReader["name"].ToString(), myReader["rdata"].ToString()));
            }
            myReader.Close();
            connection.Close();
            return resultList;
        }

        /// <summary>
        ///     Runs the XSL transform.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="transformFile">The transform file.</param>
        /// <param name="outputFile">The output file.</param>
        private static void RunXslTransform(string inputFile, string transformFile, string outputFile)
        {
            XPathDocument myXPathDoc = new XPathDocument(inputFile);
            XslCompiledTransform myXslTrans = new XslCompiledTransform();
            myXslTrans.Load(transformFile);
            XmlTextWriter myWriter = new XmlTextWriter(outputFile, null);
            myXslTrans.Transform(myXPathDoc, null, myWriter);
            myWriter.Close();
        }

        /// <summary>
        ///     Runs a command safely (ignoring errors and logging them)
        /// </summary>
        /// <param name="runDelegate">The run delegate.</param>
        private static void SafeRunCommand(Action runDelegate)
        {
            try
            {
                runDelegate.DynamicInvoke();
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Error:" + e.Message, e);
            }
        }

        /// <summary>
        ///     Saves the announce message.
        /// </summary>
        /// <param name="mess">The mess.</param>
        /// <param name="nick">The nick.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> SaveAnnounceMessage(string mess, string nick)
        {
            List<string> responseText = new List<string>();
            if (mess.ToLower().Contains("remove"))
            {
                DelAnnounceCommand();
                responseText.Add("announce for " + nick + " removed");
            }
            else
            {
                AddAnnounceCommand(mess);
                responseText.Add("cool, i'll announce to everyone: " + mess);
            }
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Saves the delayed message.
        /// </summary>
        /// <param name="nick">The nick.</param>
        /// <param name="mess">The mess.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> SaveDelayedMessage(string nick, string mess)
        {
            List<string> responseText = new List<string>();
            KeyValuePair<string, string> tellCommand = new KeyValuePair<string, string>(nick, mess);
            AddTellCommand(tellCommand);
            responseText.Add("cool, i'll tell " + tellCommand.Key + " as soon as I see him..");
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Saves the greeting message.
        /// </summary>
        /// <param name="mess">The mess.</param>
        /// <param name="nick">The nick.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> SaveGreetingMessage(string mess, string nick)
        {
            List<string> responseText = new List<string>();
            if (mess.ToLower().Contains("remove"))
            {
                DelGreetCommand(nick);
                responseText.Add("greeting for " + nick + " removed");
            }
            else
            {
                AddGreetCommand(nick, mess);
                responseText.Add("cool, i'll greet " + nick + " with " + mess);
            }
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Saves the phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number.</param>
        /// <param name="nick">The nick.</param>
        /// <returns></returns>
        public List<IrcMessage> SavePhoneNumber(string phoneNumber, string nick)
        {
            List<string> responseText = new List<string>();
            if (phoneNumber.ToLower().Contains("remove"))
            {
                DelPhoneNumber(nick);
                responseText.Add("phone number for " + nick + " removed");
            }
            else
            {
                AddPhoneNumber(phoneNumber, nick);
                responseText.Add("cool, i'll sms " + nick + " with " + phoneNumber);
            }
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Searches for DNS entry.
        /// </summary>
        /// <param name="searchterm">The searchterm.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> SearchForDnsEntry(string searchterm)
        {
            List<string> responseText = new List<string>();
            List<Pair<string, string>> resultList = RunDnsSearchOnDb(searchterm);

            resultList.ForEach(delegate (Pair<string, string> thisIp)
            {
                Ping ping = new Ping();
                PingReply pingreply = ping.Send(thisIp.Second, 5000);
                if (pingreply != null)
                {
                    string pingStatus = pingreply.Status == IPStatus.Success
                        ? "Ok!"
                        : pingreply.Status.ToString();

                    responseText.Add(
                        $"Found match: {thisIp.First} - {thisIp.Second} (Ping: {pingStatus})");
                }
            });

            int matches = resultList.Count;

            if (matches == 0)
                responseText.Add("Sorry couldn't find " + searchterm + " in the DNS db.. ");

            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Searches for location.
        /// </summary>
        /// <param name="checkDest">The check dest.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> SearchForLocation(string checkDest)
        {
            List<string> responseText = new List<string>();
            try
            {
                RefreshDb();
                Dictionary<string, string>.Enumerator myenum = _locationNameToLocationId.GetEnumerator();
                int found = 0;
                string response = "";
                while (myenum.MoveNext() && found < 10)
                {
                    if (myenum.Current.Key.ToLower().Contains(checkDest.ToLower()))
                    {
                        found++;
                        if (found > 1) response += ", ";
                        response += myenum.Current.Key;
                    }
                }
                if (response.Length > 0)
                    responseText.Add("Found nodes: " + response);
                else
                    responseText.Add("Not found :(");
            }
            catch (Exception e)
            {
                responseText.Add("Error:" + e.Message);
                MakeExcuse();
            }
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Sends the SMS.
        /// </summary>
        /// <param name="messageText">The mess.</param>
        /// <param name="username">The nick.</param>
        /// <returns></returns>
        public string SendSms(string messageText, string username)
        {
            if (_phoneNumberList.ContainsKey(username.ToLowerInvariant()))
            {
                WebClient myWebClient = new WebClient();
                string url =
                    $"http://www.mymobileapi.com/api5/http5.aspx?Type=sendparam&username=flea&password=XXXXXXXXXXXXXXX&numto={_phoneNumberList[username.ToLowerInvariant()]}&data1={HttpUtility.UrlEncode(messageText)}&return_credits=1";
                string response = myWebClient.DownloadString(url);
                return "Message Sent: " + response;
            }
            return "I don't have a number registered for " + username;
        }

        /// <summary>
        ///     Sets the frequency checking mode.
        /// </summary>
        /// <returns></returns>
        public string SetFrequencyCheckingMode()
        {
            string responseText;

            _checkFreqMode = !_checkFreqMode;
            if (_checkFreqMode)
            {
                responseText = "Frequency Checking Mode is now ON";
                _freCheckThread = new Thread(CheckFreqThread);
                _freCheckThread.Start();
            }
            else
            {
                responseText = "Frequency Checking Mode is now OFF";
                _freCheckThread?.Join();
            }
            return responseText;
        }

        /// <summary>
        ///     Shutdowns the system.
        /// </summary>
        /// <param name="dieMessage">The die message.</param>
        /// <returns></returns>
        public List<IrcMessage> ShutdownSystem(string dieMessage)
        {
            List<string> responseText = new List<string>();
            masterSleep = true;
            responseText.Add($"Flea going down for maintenance: {dieMessage}");
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Tests my SQL links.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IrcMessage> TestMySqlLinks()
        {
            List<IrcMessage> responseText = new List<IrcMessage>
            {
                new IrcMessage("Testing link to FleaDB on Wolfen's MySQL:")
            };

            string connectionString1 = ConfigurationManager.AppSettings["FleaConnectionMySql"];
            responseText.AddRange(TestMySqlServer(connectionString1));

            responseText.Add(new IrcMessage("Testing link to DNSDB on Xarion's MySQL:"));
            string connectionString2 = ConfigurationManager.AppSettings["DnsConnectionMySql"];
            responseText.AddRange(TestMySqlServer(connectionString2));

            responseText.Add(new IrcMessage("Testing link to MemberDB on Xarion's MySQL:"));
            string connectionString3 = ConfigurationManager.AppSettings["WebsiteConnectionMySql"];
            responseText.AddRange(TestMySqlServer(connectionString3));

            responseText.Add(new IrcMessage("Testing link to PassDB on Russian's MySQL:"));
            string connectionString4 = ConfigurationManager.AppSettings["PassDBConnectionMySql"];
            responseText.AddRange(TestMySqlServer(connectionString4));

            return responseText;
        }

        /// <summary>
        ///     Tests my SQL server.
        /// </summary>
        /// <param name="connectionString1">The connection string1.</param>
        /// <returns></returns>
        private List<IrcMessage> TestMySqlServer(string connectionString1)
        {
            List<IrcMessage> responseTextBit = new List<IrcMessage>();
            MySqlConnection connection = new MySqlConnection(connectionString1);
            try
            {
                connection.Open();
                try
                {
                    MySqlCommand myCommand = new MySqlCommand("Select 1 as A", connection);
                    int a = Convert.ToInt32(myCommand.ExecuteScalar());
                    responseTextBit.Add(new IrcMessage($"Query Result: {a}. The link is Ok!"));
                }
                catch (Exception ee4)
                {
                    responseTextBit.Add(new IrcMessage("Unable to run query. Error was: " + ee4.Message));

                    MySqlConnectionStringBuilder connectionString = new MySqlConnectionStringBuilder(connectionString1);
                    responseTextBit.Add(new IrcMessage("Ping: " + ActuallyPingIp(connectionString.Server)));
                    responseTextBit.AddRange(CheckTcpPort(connectionString.Server, "3306"));
                }
            }
            catch (Exception ee3)
            {
                responseTextBit.Add(new IrcMessage("Unable to connect. Error was: " + ee3.Message));

                MySqlConnectionStringBuilder connectionString = new MySqlConnectionStringBuilder(connectionString1);
                responseTextBit.Add(new IrcMessage("Ping: " + ActuallyPingIp(connectionString.Server)));
                responseTextBit.AddRange(CheckTcpPort(connectionString.Server, "3306"));
            }
            connection.Close();
            return responseTextBit;
        }

        /// <summary>
        ///     Tests the web link.
        /// </summary>
        /// <param name="checkDest">The check dest.</param>
        /// <returns></returns>
        public IEnumerable<IrcMessage> TestWebLink(string checkDest)
        {
            List<string> responseText = new List<string>();
            try
            {
                if (!checkDest.ToLower().StartsWith("http://")) checkDest = "http://" + checkDest;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(checkDest);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                responseText.Add(
                    $"Response was {response.StatusCode} ({response.StatusDescription}). Content Length was {response.ContentLength}.");
            }
            catch (Exception e)
            {
                responseText.Add("Error:" + e.Message);
                MakeExcuse();
            }
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Traces the route.
        /// </summary>
        /// <param name="traceDest">The trace dest.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">null</exception>
        public IEnumerable<IrcMessage> TraceRoute(string traceDest)
        {
            List<string> responseText = new List<string>();
            string ipDest = GetIpAddress(traceDest, _dnsTimeout);

            if ((ipDest.Split('.').Length < 4) && (ipDest.Split(':').Length < 2))
                throw new Exception($"DNS lookup failed for {traceDest}", null);

            Pair<List<IPAddress>, List<string>> ipList = Internals.TraceRoute.GetTraceRoute(ipDest);
            List<IPAddress> prevHops = new List<IPAddress>();
            int hop = 0;
            foreach (IPAddress thisIp in ipList.First)
            {
                hop++;
                if (!Equals(thisIp, IPAddress.None))
                {
                    string thisTxtWords = GetReverseDns(thisIp.ToString(), _dnsTimeout);
                    string thisTxtLoop = "";
                    if (prevHops.Contains(thisIp))
                        thisTxtLoop = "** LOOP!! **";

                    string thisStatusWord = ipList.Second[hop - 1].Split('|')[0];
                    string thisTimeWord = "";
                    if (ipList.Second[hop - 1].Contains("|"))
                        thisTimeWord = ipList.Second[hop - 1].Split('|')[1];

                    if (thisTxtWords.StartsWith("Error"))
                        thisTxtWords = "ReverseDNS Fail";

                    responseText.Add(
                        string.Format("Hop: {0} \t{5} \tIp: {1}  ({2}) {3} {4}", hop, thisIp, thisTxtWords,
                            thisStatusWord, thisTxtLoop, thisTimeWord));

                    prevHops.Add(thisIp);

                    if (thisTxtLoop != "") break;
                }
                else
                    responseText.Add("Hop: " + hop + " Failed: " + ipList.Second[hop - 1]);
            }

            responseText.Add("Trace Completed.");
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     URLs the encode.
        /// </summary>
        /// <param name="encodeme">The encodeme.</param>
        /// <returns></returns>
        private static string UrlEncode(string encodeme)
        {
            return HttpUtility.UrlEncode(encodeme);
        }

        /// <summary>
        ///     Displays the member2 list.
        /// </summary>
        /// <returns></returns>
        public async Task<MemberCheckResponse> GetWugMSMemberList()
        {
            using (WugmsCommunicator service = new WugmsCommunicator())
            {
                return await service.GetMemberlist();
            }
        }

        /// <summary>
        /// Gets the length of the tell queue.
        /// </summary>
        /// <returns></returns>
        public TellMessageStatus GetTellQueueLength()
        {
            int users = _tellCommands.Select(x => x.Key.ToLowerInvariant()).Distinct().Count();
            int messages = _tellCommands.Count();
            return new TellMessageStatus() { UserCount = users, MessageCount = messages };
        }

        #endregion
    }
}