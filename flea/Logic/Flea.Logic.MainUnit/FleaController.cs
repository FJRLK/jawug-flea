using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Flea.Logic.CommandUnit;
using Flea.Logic.IrcConnectionUnit;
using Flea.Logic.IrcTranslationUnit;
using Flea.Logic.Utils;
using log4net;

namespace Flea.Logic.MainUnit
{
    public class FleaController
    {
        #region Types

        /// <summary>
        ///     The Remote Trace Handler
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="dest">The dest.</param>
        /// <returns></returns>
        private delegate IEnumerable<IrcMessage> RTraceHandler(string source, string dest);

        #endregion

        #region Static

        private static int _currentIrcServerIndex = -1;
        private static bool _stayConnected = true;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Internals

        private readonly FleaCommands _fleaCommands;
        private readonly IrcConnection _ircObject;
        private readonly int _ircPort;
        private readonly string[] _ircServerListArray;
        private Thread _ircConnectionThread;


        /// <summary>
        ///     Gets the flea commands.
        /// </summary>
        /// <value>
        ///     The flea commands.
        /// </value>
        private FleaCommands FleaCommands
        {
            get { return _fleaCommands; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="FleaController" /> class.
        /// </summary>
        /// <param name="ircServerListArray">The irc server list array.</param>
        /// <param name="ircPort">The irc port.</param>
        /// <param name="ircUser">The irc user.</param>
        /// <param name="ircChan">The irc chan.</param>
        /// <param name="ircElectionChan">The irc election chan.</param>
        public FleaController(string[] ircServerListArray, int ircPort, string ircUser, string ircChan,
            string ircElectionChan)
        {
            _ircServerListArray = ircServerListArray;
            _ircPort = ircPort;
            _fleaCommands = new FleaCommands(false, false);
            _ircObject = new IrcConnection(ircUser, ircChan, ircElectionChan);

            InitEvents();

            _fleaCommands.ReadConfigFromDisk();

            IRCAppender.SetIRCLink(_ircObject);
        }

        #endregion

        #region Members

        /// <summary>
        ///     Connects to server once.
        /// </summary>
        /// <param name="ircPortToConnect">The irc port to connect.</param>
        /// <param name="ircServer">The irc server.</param>
        private void ConnectToServerOnce(int ircPortToConnect, string ircServer)
        {
            Log.InfoFormat("Connecting to {0}:{1}", ircServer, ircPortToConnect);
            try
            {
                _ircObject.ConnectOnce(ircServer, ircPortToConnect);
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Link failed: {0}", e.Message);
                Log.ErrorFormat("Link failed: {0}", e.StackTrace);

                Thread.Sleep(5000);
                if (e.Message == "SHUTDOWN REQUESTED")
                    _stayConnected = false;
            }
            Log.InfoFormat("Closing link to : {0}", ircServer);
        }

        /// <summary>
        ///     Initializes the events.
        /// </summary>
        private void InitEvents()
        {
            // Assign events
            _ircObject.EventReceiving += IrcCommandReceived;
            _ircObject.EventJoin += IrcJoin;
            _ircObject.EventPart += IrcPart;
            _ircObject.EventMode += IrcMode;
            _ircObject.EventNickChange += IrcNickChange;
            _ircObject.EventKick += IrcKick;
            _ircObject.EventQuit += IrcQuit;
        }

        /// <summary>
        /// Deals with a new IRC command recieved
        /// </summary>
        /// <param name="rawIrcCommand">The raw irc command.</param>
        private async void IrcCommandReceived(string rawIrcCommand)
        {
            IrcMessage receivedMessage = IrcMessageTranslator.ParseRawMessage(rawIrcCommand, _ircObject.CurrentIrcNick);

            // send all TELL messages to the user (if any)
            FleaCommands.ProcessTellCommands(receivedMessage.SourceUser, receivedMessage.SourceType, receivedMessage.SourceChannel, _ircObject);

            // Respond to server pings
            ProcessIrcPingCommand(receivedMessage.RawMessage, receivedMessage.SourceUser);

            // Respond to help command
            if (receivedMessage.ExclamationCommand == "!HELP")
            {
                _ircObject.MultiNoticeToUser(FleaCommands.DisplayHelpText(), receivedMessage.SourceUser);
            }

            // Respond to normal commands
            if (!FleaCommands.masterSleep)
            {
                switch (receivedMessage.ExclamationCommand)
                {
                    case "!WEATHER":
                        _ircObject.MultiPostToChannel(FleaCommands.PostWeatherLinks(), receivedMessage.SourceChannel);
                        break;
                    case "!GOOGLE":
                        _ircObject.MultiPostToChannel(CreateGoogleLink(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!TELLQUEUE":
                        _ircObject.PostToChannel(IrcMessageTranslator.Translate(FleaCommands.GetTellQueueLength()), receivedMessage.SourceChannel);
                        break;
                    case "!RBCP":
                        _ircObject.MultiPostToChannel(CreateRbcpLink(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!RADIUS":
                        _ircObject.MultiPostToChannel(FleaCommands.CheckRadiusServers(), receivedMessage.SourceChannel);
                        break;
                    case "!PASS":
                        IEnumerable<IrcMessage> rawResponse = GetJawugAdminPassword(receivedMessage.RawMessage, receivedMessage.SourceChannel);
                        SendResponseBack(rawResponse, receivedMessage.SourceChannel, receivedMessage.SourceUser);
                        break;
                    case "!MEMBERS":
                        IEnumerable<IrcMessage> rawResponse2 = FleaCommands.DisplayMemberList();
                        SendResponseBack(rawResponse2, receivedMessage.SourceChannel, receivedMessage.SourceUser);
                        break;
                    case "!MEMBERS2":
                        IEnumerable<IrcMessage> rawResponse9 = IrcMessageTranslator.Translate(await FleaCommands.GetWugMSMemberList());
                        SendResponseBack(rawResponse9, receivedMessage.SourceChannel, receivedMessage.SourceUser);
                        break;
                    case "!MEMSTATUS":

                        _ircObject.MultiPostToChannel(DisplayMembershipStatus(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!RENAME":
                        if ((receivedMessage.IsPrivateMessage) && (receivedMessage.RawMessage.Split(' ').Length >= 5))
                        {
                            string newNick = receivedMessage.RawMessage.Split(' ')[4];
                            _ircObject.CurrentIrcNick = newNick;
                        }
                        break;
                    case "!IPCALC":
                        _ircObject.MultiPostToChannel(CalculateIpRange(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!8BALL":
                        _ircObject.MultiPostToChannel(FleaCommands.Display8BallMessage(), receivedMessage.SourceChannel);
                        break;
                    case "!TIME":
                        _ircObject.PostToChannel(FleaCommands.DisplayTime(), receivedMessage.SourceChannel);
                        break;
                    case "!UPTIME":
                        _ircObject.MultiPostToChannel(DisplayUpTime(), receivedMessage.SourceChannel);
                        break;
                    case "!MAP":
                        _ircObject.MultiPostToChannel(DisplayMapUrl(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!ANA":
                        _ircObject.MultiPostToChannel(CreateLosAnalysisLink(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!LOS":
                        _ircObject.MultiPostToChannel(CreateLosLink(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!SERVICES":
                        _ircObject.PostToChannel(FleaCommands.DisplayServicesLink(), receivedMessage.SourceChannel);
                        break;
                    case "!NATRULE":
                        _ircObject.MultiPostToChannel(FleaCommands.DisplatNatRuleSyntax(), receivedMessage.SourceChannel);
                        break;
                    case "!CHECKFREQ":
                        _ircObject.PostToChannel(FleaCommands.SetFrequencyCheckingMode(), receivedMessage.SourceChannel);
                        break;
                    case "!DNSFIND":
                        _ircObject.MultiPostToChannel(RunDnsSearch(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!REGISTER":
                        _ircObject.MultiPostToChannel(RegisterDnsEntry(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!UNREGISTER":
                        _ircObject.MultiPostToChannel(RemoveDnsEntry(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!LOS2":
                        _ircObject.MultiPostToChannel(CreateLos2Link(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!NEAR":
                        _ircObject.MultiPostToChannel(CreateNearLink(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!RTRACE":
                        _ircObject.MultiPostToChannel(RunRemoteTrace(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!TRACE":
                        _ircObject.MultiPostToChannel(RunTrace(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!MAPTRACE":
                        _ircObject.MultiPostToChannel(RunMaptrace(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!WORDS":
                        _ircObject.SendMultiPrivateMessageToUser(FleaCommands.DisplayWordList(), receivedMessage.SourceUser);
                        break;
                    case "!PING":
                        _ircObject.MultiPostToChannel(RunPing(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!DNS":
                        _ircObject.MultiPostToChannel(ResolveDns(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!GREET":
                        _ircObject.MultiPostToChannel(SaveGreetMessage(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!NUMBER":
                        _ircObject.MultiPostToChannel(SavePhoneNumber(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!MAKEWORD":
                        _ircObject.MultiPostToChannel(SaveWordCommand(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!SMS":
                        _ircObject.MultiPostToChannel(SendSms(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!ANNOUNCE":
                        _ircObject.MultiPostToChannel(SaveAnnouncement(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!REFRESHDB":
                        _ircObject.MultiPostToChannel(FleaCommands.RefreshDb(), receivedMessage.SourceChannel);
                        break;
                    case "!WHATSMYIP":
                        _ircObject.MultiPostToChannel(FleaCommands.GetFleaInternetIp(), receivedMessage.SourceChannel);
                        break;
                    case "!KML":
                        _ircObject.MultiPostToChannel(FleaCommands.DisplayKmlLink(), receivedMessage.SourceChannel);
                        break;
                    case "!TESTKIT":
                        _ircObject.PostToChannel(FleaCommands.DisplayTestKitLink(), receivedMessage.SourceChannel);
                        break;
                    case "!GSG":
                        _ircObject.PostToChannel(new IrcMessage(FleaCommands.DisplayGsgLink()), receivedMessage.SourceChannel);
                        break;
                    case "!CHECKPORT":
                        _ircObject.MultiPostToChannel(CheckPortOpen(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!WGET":
                        _ircObject.MultiPostToChannel(TestHttpLink(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!EXCUSE":
                    case "!WHY":
                        _ircObject.PostToChannel(FleaCommands.MakeExcuse(), receivedMessage.SourceChannel);
                        break;
                    case "!LOC":
                        SearchForLocationByName(receivedMessage.RawMessage, receivedMessage.SourceChannel);
                        break;
                    case "!MYSQLTEST":
                        _ircObject.MultiPostToChannel(FleaCommands.TestMySqlLinks(), receivedMessage.SourceChannel);
                        break;
                    case "!ACTION":
                        _ircObject.MultiActionToChannel(MakeAction(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                        break;
                    case "!VERSION":
                        _ircObject.PostToChannel(FleaCommands.ReturnVersion(), receivedMessage.SourceChannel);
                        break;
                    case "!INVITE":
                        _ircObject.JoinChannel(receivedMessage.RawMessage);
                        break;
                    case "!LEAVE":
                        _ircObject.PartChannel(receivedMessage.RawMessage);
                        break;
                    case "!LOGON":
                        IRCAppender.LoggingEnabled = true;
                        _ircObject.PostToChannel("IRC based loggin activated, see #flea. ", receivedMessage.SourceChannel);
                        break;
                    case "!LOGOFF":
                        IRCAppender.LoggingEnabled = false;
                        _ircObject.PostToChannel("IRC based loggin disabled", receivedMessage.SourceChannel);
                        break;
                }
                // Process Word Commands  (Custom)
                _ircObject.MultiPostToChannel(FleaCommands.ProcessWordCommands(receivedMessage.RawMessage), receivedMessage.SourceChannel);
            }

            if (receivedMessage.ExclamationCommand == "!MAINTENANCE")
            {
                _ircObject.MultiPostToChannel(ShutDown(receivedMessage.RawMessage), receivedMessage.SourceChannel);
                _stayConnected = false;
                _ircObject.Disconnect();
            }

            if (receivedMessage.ExclamationCommand == "!SLEEP" && receivedMessage.IsPrivateMessage)
            {
                _ircObject.MultiPostToChannel(FleaCommands.ActivateSleepMode(), receivedMessage.SourceChannel);
            }

            if (receivedMessage.ExclamationCommand == "!WAKE" && receivedMessage.IsPrivateMessage)
            {
                _ircObject.MultiPostToChannel(FleaCommands.DisableSleepMode(), receivedMessage.SourceChannel);
            }

            if (receivedMessage.ExclamationCommand == "!TELL")
            {
                _ircObject.MultiPostToChannel(SaveTellMessage(receivedMessage.RawMessage), receivedMessage.SourceChannel);
            }

            if (!receivedMessage.ExclamationCommand.StartsWith("!") && receivedMessage.Url != null)
            {
                string title = GetPageTitle(receivedMessage.Url);
                if (!String.IsNullOrEmpty(title))
                    _ircObject.PostToChannel($"Url Title: {title}", receivedMessage.SourceChannel);
            }
        }

        /// <summary>
        /// Gets the page title.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        private string GetPageTitle(string url)
        {
            string title = null;
            try
            {
                HttpWebRequest request = (WebRequest.Create(url) as HttpWebRequest);
                HttpWebResponse response = request?.GetResponse() as HttpWebResponse;

                if (response != null)
                    using (Stream stream = response.GetResponseStream())
                    {
                        // compiled regex to check for <title></title> block
                        Regex titleCheck = new Regex(@"<title>\s*(.+?)\s*</title>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        int bytesToRead = 8092;
                        byte[] buffer = new byte[bytesToRead];
                        string contents = "";
                        int length = 0;
                        while (stream != null && ((length = stream.Read(buffer, 0, bytesToRead)) > 0 && contents.Length < 64000))
                        {
                            // convert the byte-array to a string and add it to the rest of the
                            // contents that have been downloaded so far
                            contents += Encoding.UTF8.GetString(buffer, 0, length);

                            Match m = titleCheck.Match(contents);
                            if (m.Success)
                            {
                                // we found a <title></title> match =]
                                title = m.Groups[1].Value.ToString();
                                break;
                            }
                            else if (contents.Contains("</head>"))
                            {
                                // reached end of head-block; no title found =[
                                break;
                            }
                        }
                    }
                return title;
            }
            catch (Exception e)
            {
                return null;
            }
        }


        private void SendResponseBack(IEnumerable<IrcMessage> rawResponse, string sourceChannel, string sourceUser)
        {
            IEnumerable<IrcMessage> pmList = rawResponse.Where(x => !x.IsPublic);
            IEnumerable<IrcMessage> publicList = rawResponse.Where(x => x.IsPublic);
            _ircObject.MultiPostToChannel(publicList, sourceChannel);
            _ircObject.SendMultiPrivateMessageToUser(pmList, sourceUser);
        }

        /// <summary>
        ///     Maintains the irc connection.
        /// </summary>
        private void MaintainIrcConnection()
        {
            while (_stayConnected)
            {
                _currentIrcServerIndex++;
                if (_currentIrcServerIndex == _ircServerListArray.Length) _currentIrcServerIndex = 0;
                string thisServer = _ircServerListArray[_currentIrcServerIndex];
                ConnectToServerOnce(_ircPort, thisServer);
                if (_stayConnected)
                {
                    Log.Info("Delaying reconnection attempt by 30 seconds...");
                    Thread.Sleep(30000);
                }
            }
        }

        /// <summary>
        ///     Makes the action.
        /// </summary>
        /// <param name="actionText">The action text.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> MakeAction(string actionText)
        {
            string text = actionText.RestOfString(4);
            if (text.Length > 3)
            {
                int spaceAt = text.IndexOf(" ", StringComparison.Ordinal);
                text = text.Insert(spaceAt, "s");
                return FleaCommands.MakeAction(text);
            }
            return new List<IrcMessage>();
        }

        /// <summary>
        ///     Searches for the location by name
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <param name="sourceChannel">The source channel.</param>
        private void SearchForLocationByName(string ircCommand, string sourceChannel)
        {
            string checkDest = ircCommand.Split(' ')[4];
            if (ircCommand.Split(' ').Length >= 5)
            {
                _ircObject.MultiPostToChannel(FleaCommands.SearchForLocation(checkDest), sourceChannel);
            }
            else
                _ircObject.PostToChannel(new IrcMessage("Usage: ! loc AAAAAAA "), sourceChannel);
        }

        /// <summary>
        ///     Sends the SMS.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> SendSms(string ircCommand)
        {
            try
            {
                string nick = ircCommand.Split(' ')[4];
                string mess = "";
                string[] thisMessA = ircCommand.Split(' ');
                for (int i = 5; i < thisMessA.Length; i++)
                {
                    mess += thisMessA[i] + " ";
                }

                return new List<IrcMessage> { new IrcMessage(FleaCommands.SendSms(mess, nick)) };
            }
            catch (Exception ee)
            {
                return MakeIrcErrorMessage(ee);
            }
        }

        /// <summary>
        ///     Starts the flea.
        /// </summary>
        public void StartFlea()
        {
            _ircConnectionThread = new Thread(MaintainIrcConnection);
            _ircConnectionThread.Start();
        }

        /// <summary>
        ///     Stops the flea.
        /// </summary>
        public void StopFlea()
        {
            _stayConnected = false;
            _ircObject.Disconnect();
            _ircConnectionThread.Join();
        }


        /// <summary>
        ///     Determines whether [is allowed channel] [the specified allowed channels].
        /// </summary>
        /// <param name="allowedChannels">The allowed channels.</param>
        /// <param name="sourceChannel">The source channel.</param>
        /// <returns></returns>
        private bool IsAllowedChannel(ICollection<string> allowedChannels, string sourceChannel)
        {
            if (allowedChannels.Contains(sourceChannel.ToLowerInvariant())) return true;
            _ircObject.PostToChannel(new IrcMessage("This command is restricted. Please use in the correct channel"),
                sourceChannel);
            return false;
        }


        /// <summary>
        ///     Processes the irc ping command.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <param name="sourceUser">The source user.</param>
        private void ProcessIrcPingCommand(string ircCommand, string sourceUser)
        {
            if (!ircCommand.Contains("(echo)")) return;
            string[] thisIrcCommandArray = ircCommand.Split(' ');
            if (thisIrcCommandArray.Length <= 4) return;
            string theChannel = thisIrcCommandArray[2];
            string theId = ircCommand.Split(' ')[1];
            if (theId != "401" && theId != "JOIN" && theId != "PART" && theId != "NICK" && theId != "KICK" &&
                theId != "QUIT" && theId != "931" && theId != "366" && theId != ":Closing" && theId != "353" &&
                theChannel != _ircObject.CurrentIrcNick)
            {
                int colonPos = ircCommand.IndexOf(':', 1);
                string said = ircCommand.Substring(colonPos + 1);
                _ircObject.PostToChannel(
                    new IrcMessage($"{sourceUser}:  {said.Replace("(echo)", "")}"), theChannel);
            }
        }


        /// <summary>
        ///     Ircs the join.
        /// </summary>
        /// <param name="ircChan">The irc chan.</param>
        /// <param name="ircUser">The irc user.</param>
        private void IrcJoin(string ircChan, string ircUser)
        {
            Log.InfoFormat("User {0} joined channel {1}", ircUser, ircChan);
            //_ircObject.RefreshNamesList(ircChan);

            _ircObject.MultiPostToChannel(FleaCommands.ProcessGreetCommands(ircUser), ircChan);
            ProcessAnnounceCommands(ircUser);
        } /* IrcJoin */


        /// <summary>
        ///     Ircs the part.
        /// </summary>
        /// <param name="ircChan">The irc chan.</param>
        /// <param name="ircUser">The irc user.</param>
        private void IrcPart(string ircChan, string ircUser)
        {
            Log.InfoFormat("User {0} parted channel {1}", ircUser, ircChan);
            //_ircObject.RefreshNamesList(ircChan);
        } /* IrcPart */

        /// <summary>
        ///     Ircs the mode.
        /// </summary>
        /// <param name="ircChan">The irc chan.</param>
        /// <param name="ircUser">The irc user.</param>
        /// <param name="userMode">The user mode.</param>
        private void IrcMode(string ircChan, string ircUser, string userMode)
        {
            if (ircUser != ircChan)
            {
                Log.InfoFormat("{0} sets mode {1} in channel {2}", ircUser, userMode, ircChan);
            }
        } /* IrcMode */

        /// <summary>
        ///     Ircs the nick change.
        /// </summary>
        /// <param name="userOldNick">The user old nick.</param>
        /// <param name="userNewNick">The user new nick.</param>
        /// <param name="ircChannel">The irc channel.</param>
        private void IrcNickChange(string userOldNick, string userNewNick, string ircChannel)
        {
            Log.InfoFormat("User {0} changed nick to {1}", userOldNick, userNewNick);
            //_ircObject.RefreshNamesList(ircChannel);
        } /* IrcNickChange */

        /// <summary>
        ///     Ircs the kick.
        /// </summary>
        /// <param name="ircChannel">The irc channel.</param>
        /// <param name="userKicker">The user kicker.</param>
        /// <param name="userKicked">The user kicked.</param>
        /// <param name="kickMessage">The kick message.</param>
        private void IrcKick(string ircChannel, string userKicker, string userKicked, string kickMessage)
        {
            Log.InfoFormat("{0} kicks {1} out {2} ({3})", userKicker, userKicked, ircChannel, kickMessage);
            //_ircObject.RefreshNamesList(ircChannel);
        } /* IrcKick */

        /// <summary>
        ///     Ircs the quit.
        /// </summary>
        /// <param name="userName">The user quit.</param>
        /// <param name="quitMessage">The quit message.</param>
        /// <param name="ircChannel">The irc channel.</param>
        private void IrcQuit(string userName, string quitMessage, string ircChannel)
        {
            Log.InfoFormat("{0} left {2} quit: {1}", userName, quitMessage, ircChannel);
            //_ircObject.RefreshNamesList(ircChannel);
        } /* IrcQuit */

        /// <summary>
        ///     Tests the HTTP link.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> TestHttpLink(string ircCommand)
        {
            if (ircCommand.Split(' ').Length >= 5)
            {
                string checkDest = ircCommand.Split(' ')[4];
                return FleaCommands.TestWebLink(checkDest);
            }

            return new List<IrcMessage> { new IrcMessage("Usage: ! checkport 1.2.3.4 80") };
        }

        /// <summary>
        ///     Checks the port open.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> CheckPortOpen(string ircCommand)
        {
            if (ircCommand.Split(' ').Length >= 6)
            {
                string checkDest = ircCommand.Split(' ')[4];
                string checkPort = ircCommand.Split(' ')[5];

                return FleaCommands.CheckTcpPort(checkDest, checkPort);
            }
            return new List<IrcMessage> { new IrcMessage("Usage: ! checkport 0.0.0.0 0") };
        }

        /// <summary>
        ///     Saves the announcement.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> SaveAnnouncement(string ircCommand)
        {
            try
            {
                string nick = ircCommand.Split(' ')[0].Split('!')[0].Replace("!", "").Replace("@", "").Replace(":", "");
                string mess = "";
                string[] thisMessA = ircCommand.Split(' ');
                for (int i = 4; i < thisMessA.Length; i++)
                {
                    mess += thisMessA[i] + " ";
                }

                return FleaCommands.SaveAnnounceMessage(mess, nick);
            }
            catch
            {
                List<IrcMessage> responseText = new List<IrcMessage> { new IrcMessage("Huh?") };
                return responseText;
            }
        }

        /// <summary>
        ///     Saves the word command.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> SaveWordCommand(string ircCommand)
        {
            try
            {
                string word = ircCommand.Split(' ')[4];
                string mess = "";
                string[] thisMessA = ircCommand.Split(' ');
                for (int i = 5; i < thisMessA.Length; i++)
                {
                    mess += thisMessA[i] + " ";
                }

                return FleaCommands.MaintainWordCommand(mess, word);
            }
            catch (Exception ee)
            {
                return MakeIrcErrorMessage(ee);
            }
        }

        /// <summary>
        ///     Saves the phone number.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> SavePhoneNumber(string ircCommand)
        {
            try
            {
                string nick = ircCommand.Split(' ')[4];
                string phoneNumber = ircCommand.Split(' ')[5];
                return FleaCommands.SavePhoneNumber(phoneNumber, nick);
            }
            catch
            {
                List<string> responseText = new List<string> { "Huh?" };
                FleaCommands.MakeExcuse();
                return responseText.ToPublicIrcMessageList();
            }
        }


        /// <summary>
        ///     Saves the greet message.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> SaveGreetMessage(string ircCommand)
        {
            try
            {
                string nick = ircCommand.Split(' ')[0].Split('!')[0].Replace("!", "").Replace("@", "").Replace(":", "");
                string mess = "";
                string[] thisMessA = ircCommand.Split(' ');
                for (int i = 4; i < thisMessA.Length; i++)
                {
                    mess += thisMessA[i] + " ";
                }

                return FleaCommands.SaveGreetingMessage(mess, nick);
            }
            catch
            {
                List<string> responseText = new List<string> { "Huh?" };
                FleaCommands.MakeExcuse();
                return responseText.ToPublicIrcMessageList();
            }
        }

        /// <summary>
        ///     Saves the tell message.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> SaveTellMessage(string ircCommand)
        {
            try
            {
                string sourceUser = ircCommand.Split(' ')[0].Split('!')[0].Replace(":", "");

                int first = ircCommand.IndexOf(':');
                int startAt = ircCommand.IndexOf(':', first + 1);
                // remove the 1st 2 words
                List<string> tellmessageArray = ircCommand.Substring(startAt + 1).Split(' ').ToList();
                tellmessageArray.RemoveAt(0);
                tellmessageArray.RemoveAt(0);
                string tellmessage = string.Join(" ", tellmessageArray);

                string mess = $"Message from {sourceUser}: {tellmessage} ({DateTime.Now:yyyy/MM/dd HH:mm})";
                string nick = ircCommand.Split(' ')[4];
                nick = nick.ToLowerInvariant();


                return FleaCommands.SaveDelayedMessage(nick, mess);
            }
            catch
            {
                List<string> responseText = new List<string> { "Huh?" };
                FleaCommands.MakeExcuse();
                return responseText.ToPublicIrcMessageList();
            }
        }

        /// <summary>
        ///     Resolves the DNS.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> ResolveDns(string ircCommand)
        {
            try
            {
                string traceDest = ircCommand.Split(' ')[4];
                return FleaCommands.ResolveDns(traceDest);
            }
            catch (Exception ee)
            {
                return MakeIrcErrorMessage(ee);
            }
        }

        /// <summary>
        ///     Runs the ping.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> RunPing(string ircCommand)
        {
            try
            {
                if (ircCommand.Split(' ').Length >= 5)
                {
                    string traceDest = ircCommand.Split(' ')[4];

                    // Get number of pings (default 1)
                    short traceCount = 1;
                    if (ircCommand.Split(' ').Length == 6)
                        traceCount = Convert.ToInt16(ircCommand.Split(' ')[5]);
                    if (traceCount > 5) traceCount = 5;


                    return FleaCommands.Ping(traceDest, traceCount);
                }
            }
            catch (Exception ee)
            {
                if (ee.InnerException != null)
                {
                    List<IrcMessage> responseText = new List<IrcMessage>
                    {
                        new IrcMessage("Ping Error:" + ee.InnerException.Message)
                    };
                    return responseText;
                }
                List<IrcMessage> responseText2 = new List<IrcMessage>
                {
                    new IrcMessage($"Ping Error:{ee.Message}")
                };
                return responseText2;
            }
            return null;
        }

        /// <summary>
        ///     Runs the maptrace.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> RunMaptrace(string ircCommand)
        {
            try
            {
                if (ircCommand.Split(' ').Length >= 5)
                {
                    string traceDest = ircCommand.Split(' ')[4];
                    return FleaCommands.DoMapTrace(traceDest);
                }
            }
            catch (Exception e32)
            {
                IEnumerable<IrcMessage> responseText = new List<IrcMessage>
                {
                    new IrcMessage($"MapTrace error - {e32.Message}")
                };
                FleaCommands.MakeExcuse();
                return responseText;
            }
            return null;
        }

        /// <summary>
        ///     Runs the trace.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> RunTrace(string ircCommand)
        {
            try
            {
                if (ircCommand.Split(' ').Length >= 5)
                {
                    string traceDest = ircCommand.Split(' ')[4];
                    return FleaCommands.TraceRoute(traceDest);
                }
            }
            catch (Exception e32)
            {
                List<IrcMessage> responseText = new List<IrcMessage>
                {
                    new IrcMessage($"Trace error - {e32.Message}")
                };
                FleaCommands.MakeExcuse();
                return responseText;
            }
            return null;
        }

        /// <summary>
        ///     Runs the remote trace.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> RunRemoteTrace(string ircCommand)
        {
            List<IrcMessage> responseText = new List<IrcMessage>();
            try
            {
                if (ircCommand.Split(' ').Length >= 6)
                {
                    string traceSource = ircCommand.Split(' ')[4];
                    string traceDest = ircCommand.Split(' ')[5];

                    RTraceHandler callback = FleaCommands.ExecuteRemoteTrace;
                    IAsyncResult result = callback.BeginInvoke(traceSource, traceDest, null, null);

                    if (result.AsyncWaitHandle.WaitOne(35000, false))
                    {
                        responseText.AddRange(callback.EndInvoke(result));
                        return responseText;
                    }
                    responseText.Add(new IrcMessage("Gave up on the RTrace, probably a timeout..."));
                    return responseText;
                }
            }
            catch (Exception e32)
            {
                return MakeIrcErrorMessage(e32);
            }
            return null;
        }

        /// <summary>
        ///     Creates the near link.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> CreateNearLink(string ircCommand)
        {
            try
            {
                // http://www.wug.za.net/near.php?locationid=22421
                if (ircCommand.Split(' ').Length >= 5)
                {
                    string searchTerm1 = ircCommand.Split(' ')[4];
                    string locationId1 = FleaCommands.GetLocationId(searchTerm1).Second;
                    int radius = 0;
                    if (ircCommand.Split(' ').Length > 5)
                        radius = Convert.ToInt32(ircCommand.Split(' ')[5]);

                    return FleaCommands.DoNearAnalysis(locationId1, searchTerm1, radius);
                }
                return
                    new List<IrcMessage>
                    {
                        new IrcMessage(
                            "Not enough parameters! It works like this !near location distance   for example: !near wolfen351 20")
                    };
            }
            catch (Exception ee2)
            {
                return MakeIrcErrorMessage(ee2);
            }
        }

        /// <summary>
        ///     Creates the los2 link.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> CreateLos2Link(string ircCommand)
        {
            try
            {
                if (ircCommand.Split(' ').Length >= 6)
                {
                    string searchLocationName1 = ircCommand.Split(' ')[4];
                    string searchLocationName2 = ircCommand.Split(' ')[5];

                    return FleaCommands.CreateLos2LinkUrl(searchLocationName1, searchLocationName2);
                }
            }
            catch (Exception ee2)
            {
                return MakeIrcErrorMessage(ee2);
            }
            return null;
        }

        /// <summary>
        ///     Removes the DNS entry.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> RemoveDnsEntry(string ircCommand)
        {
            try
            {
                if (ircCommand.Split(' ').Length >= 6)
                {
                    string ip = ircCommand.Split(' ')[4];
                    string name = ircCommand.Split(' ')[5];

                    return FleaCommands.RemoveDnsEntry(name, ip);
                }
            }
            catch (Exception ee2)
            {
                return MakeIrcErrorMessage(ee2);
            }
            return null;
        }

        /// <summary>
        ///     Registers the DNS entry.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> RegisterDnsEntry(string ircCommand)
        {
            try
            {
                if (ircCommand.Split(' ').Length >= 6)
                {
                    string ip = ircCommand.Split(' ')[4];
                    string name = ircCommand.Split(' ')[5];

                    return FleaCommands.MakeDnsEntry(name, ip);
                }
            }
            catch (Exception ee2)
            {
                return MakeIrcErrorMessage(ee2);
            }
            return null;
        }

        /// <summary>
        ///     Runs the DNS search.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> RunDnsSearch(string ircCommand)
        {
            try
            {
                if (ircCommand.Split(' ').Length >= 5)
                {
                    string searchterm = ircCommand.Split(' ')[4];
                    return FleaCommands.SearchForDnsEntry(searchterm);
                }
            }
            catch (Exception ee2)
            {
                return MakeIrcErrorMessage(ee2);
            }
            return null;
        }

        /// <summary>
        ///     Makes the irc error message.
        /// </summary>
        /// <param name="ee2">The ee2.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> MakeIrcErrorMessage(Exception ee2)
        {
            Log.ErrorFormat("Error occured: {0}", ee2);
            List<IrcMessage> responseText = new List<IrcMessage>
            {
                new IrcMessage($"Error: {ee2.Message}")
            };
            Exception innerException = ee2.InnerException;
            while (innerException != null)
            {
                responseText.Add(new IrcMessage($"because {innerException.Message}"));
                innerException = innerException.InnerException;
            }
            responseText.Add(new IrcMessage(FleaCommands.MakeExcuse()));
            return responseText;
        }

        /// <summary>
        ///     Creates the los link.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> CreateLosLink(string ircCommand)
        {
            try
            {
                // http://www.wug.za.net/ajax_loc.php?name=wolfen
                if (ircCommand.Split(' ').Length >= 6)
                {
                    string search1 = ircCommand.Split(' ')[4];
                    string search2 = ircCommand.Split(' ')[5];

                    return FleaCommands.MakeLosLink(search1, search2);
                }
            }
            catch (Exception eee)
            {
                MakeIrcErrorMessage(eee);
            }
            return null;
        }

        /// <summary>
        ///     Creates the los analysis link.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> CreateLosAnalysisLink(string ircCommand)
        {
            try
            {
                //http://localhost:40503/flea.wolfen.za.net/FleaAnalyse.aspx?Name1=A&Name2=B&Lat1=0&Lon1=1&Lat2=2&Lon2=3&Id1=1000&Id2=2000
                if (ircCommand.Split(' ').Length >= 6)
                {
                    string search1 = ircCommand.Split(' ')[4];
                    string search2 = ircCommand.Split(' ')[5];

                    return FleaCommands.CreateLosAnalysisLink(search1, search2);
                }
            }
            catch (Exception rr5)
            {
                MakeIrcErrorMessage(rr5);
            }
            return null;
        }

        /// <summary>
        ///     Displays the map URL.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> DisplayMapUrl(string ircCommand)
        {
            try
            {
                // http://www.wug.za.net/ajax_loc.php?name=wolfen
                if (ircCommand.Split(' ').Length >= 5)
                {
                    string search1 = ircCommand.Split(' ')[4];
                    string search2 = ircCommand.Split(' ')[4];
                    if (ircCommand.Split(' ').Length >= 6)
                        search2 = ircCommand.Split(' ')[5];
                    return FleaCommands.CreateMapUrl(search1, search2);
                }
            }
            catch (Exception ee2)
            {
                MakeIrcErrorMessage(ee2);
            }
            return null;
        }

        /// <summary>
        ///     Shuts down.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> ShutDown(string ircCommand)
        {
            string reason = ircCommand.Split(' ')[0] + ' ' + ircCommand.Split(' ')[1] + ' ' + ircCommand.Split(' ')[2] +
                            ' ' +
                            ircCommand.Split(' ')[3] + ' ';

            string dieMessage = ircCommand.Replace(reason, "");

            return FleaCommands.ShutdownSystem(dieMessage);
        }

        /// <summary>
        ///     Calculates the ip range.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> CalculateIpRange(string ircCommand)
        {
            if (ircCommand.Split(' ').Length >= 5)
            {
                string search1 = ircCommand.Split(' ')[4];
                return FleaCommands.CalcIpRange(search1);
            }
            return null;
        }

        /// <summary>
        ///     Displays the membership status.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> DisplayMembershipStatus(string ircCommand)
        {
            try
            {
                if (ircCommand.Split(' ').Length >= 5)
                {
                    string search1 = ircCommand.Split(' ')[4];
                    return FleaCommands.CalcMembershipStatus(search1);
                }
            }
            catch (Exception ee)
            {
                MakeIrcErrorMessage(ee);
            }
            return null;
        }

        /// <summary>
        ///     Displays up time.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IrcMessage> DisplayUpTime()
        {
            List<string> responseText = new List<string>();

            TimeSpan awakeTime = DateTime.Now - _ircObject.ConnectedTime;
            responseText.Add($"I've been has been online for {awakeTime}");
            return responseText.ToPublicIrcMessageList();
        }

        /// <summary>
        ///     Processes the announce commands.
        /// </summary>
        /// <param name="thisIrcUser">The this irc user.</param>
        private void ProcessAnnounceCommands(string thisIrcUser)
        {
            if (FleaCommands.AnnounceMessage != "!IGNORE!")
            {
                _ircObject.SendNoticeToUser("ANNOUNCE: " + FleaCommands.AnnounceMessage, thisIrcUser);
            }
        }


        /// <summary>
        ///     Gets the jawug admin password.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <param name="sourceChannel">The source channel.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> GetJawugAdminPassword(string ircCommand, string sourceChannel)
        {
            if (ircCommand.Split(' ').Length < 5) return null;
            try
            {
                if (sourceChannel != "#jawugadmin")
                {
                    List<string> responseText = new List<string> { "This command only works in #jawugadmin..." };
                    return responseText.ToPublicIrcMessageList();
                }
                string search1 = ircCommand.Split(' ')[4];
                return FleaCommands.FetchJawugAdminPassword(search1);
            }
            catch (Exception ee)
            {
                return MakeIrcErrorMessage(ee);
            }
        }

        /// <summary>
        ///     Creates the RBCP link.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> CreateRbcpLink(string ircCommand)
        {
            if (ircCommand.Split(' ').Length < 5) return null;
            try
            {
                string traceSource = ircCommand.Split(' ')[4];
                return FleaCommands.GenerateRbcpUrl(traceSource);
            }
            catch (Exception ee)
            {
                return MakeIrcErrorMessage(ee);
            }
        }

        /// <summary>
        ///     Creates the google link.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <returns></returns>
        private IEnumerable<IrcMessage> CreateGoogleLink(string ircCommand)
        {
            if (ircCommand.Split(' ').Length < 5) return null;
            try
            {
                string search = ircCommand.RestOfString(4);
                return new List<IrcMessage> { new IrcMessage(FleaCommands.MakeGoogleLuckyUrl(search)) };
            }
            catch (Exception ee)
            {
                return MakeIrcErrorMessage(ee);
            }
        }

        #endregion
    }
}