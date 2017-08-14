using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Flea.Logic.Utils;
using log4net;

namespace Flea.Logic.IrcConnectionUnit
{
    public delegate void CommandReceived(string ircCommand);

    public delegate void TopicSet(string ircChannel, string ircTopic);

    public delegate void TopicOwner(string ircChannel, string ircUser, string topicDate);

    public delegate void NamesList(string userNames);

    public delegate void ServerMessage(string serverMessage);

    public delegate void Join(string ircChannel, string ircUser);

    public delegate void Part(string ircChannel, string ircUser);

    public delegate void Mode(string ircChannel, string ircUser, string userMode);

    public delegate void NickChange(string userOldNick, string userNewNick, string ircChannel);

    public delegate void Kick(string ircChannel, string userKicker, string userKicked, string kickMessage);

    public delegate void Quit(string userQuit, string quitMessage, string ircChannel);

    public class IrcConnection : IIrcConnection
    {
        #region Types

        public event CommandReceived EventReceiving;
        public event TopicSet EventTopicSet;
        public event TopicOwner EventTopicOwner;
        public event ServerMessage EventServerMessage;
        public event Join EventJoin;
        public event Part EventPart;
        public event Mode EventMode;
        public event NickChange EventNickChange;
        public event Kick EventKick;
        public event Quit EventQuit;

        #endregion

        #region Static

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Internals

        private int _ConnectPort;
        private string _ConnectServer;
        private string _CurrentIrcNick;
        private DateTime _LastPosted = DateTime.MinValue;
        private string _Lastwinner;


        private DateTime _LiveDate = DateTime.Now;
        private List<string> _UserList = new List<string>();

        /// <summary>
        ///     Gets the connected time.
        /// </summary>
        /// <value>
        ///     The connected time.
        /// </value>
        public DateTime ConnectedTime { get; private set; }

        private bool ConnectedToChannels { get; set; }

        /// <summary>
        ///     Gets or sets the current irc nick.
        /// </summary>
        /// <value>
        ///     The current irc nick.
        /// </value>
        public string CurrentIrcNick
        {
            get { return _CurrentIrcNick; }
            set
            {
                _CurrentIrcNick = value;
                ChangeNick(value);
            }
        }

        private bool ImAlive
        {
            get { return (_LiveDate > DateTime.Now.AddMinutes(-5)); }
            set { _LiveDate = value ? DateTime.Now : DateTime.Now.AddYears(-1); }
        }

        private TcpClient InternalIrcConnection { get; set; }
        private string IrcChannel { get; set; }
        private string IrcElectionChannel { get; set; }
        private int IrcPort { get; set; }
        private StreamReader IrcReader { get; set; }
        private string IrcRealName { get; set; }

        private string IrcServer { get; set; }
        private NetworkStream IrcStream { get; set; }
        private string IrcUser { get; set; }
        private StreamWriter IrcWriter { get; set; }
        private bool IsInvisble { get; set; }

        /// <summary>
        ///     Gets or sets the main irc nick.
        /// </summary>
        /// <value>
        ///     The main irc nick.
        /// </value>
        public string MainIrcNick { get; set; }

        #endregion

        #region Constructor

        /* IrcReader */

        public IrcConnection(string ircNick, string ircChannel, string ircElectionChan)
        {
            MainIrcNick = ircNick;
            IrcChannel = ircChannel;
            IrcUser = Environment.MachineName;
            IrcRealName = "I'm FLEA! (That's IRC speak for a lil helper bot)";
            IsInvisble = false;
            IrcElectionChannel = ircElectionChan;
            ConnectedToChannels = false;
        }

        #endregion

        #region Members

        public void SendLogMessage(string messageToSend)
        {
            if (ImAlive && ConnectedToChannels)
                PostToChannel(messageToSend, "#FLEA", true);
        }

        /* IRC */

        public void MultiNoticeToUser(IEnumerable<string> textToDisplay, string sourceUser)
        {
            foreach (string msg in textToDisplay)
            {
                SendNoticeToUser(msg, sourceUser);
            }
        }

        /// <summary>
        ///     Sends the multi private message to user.
        /// </summary>
        /// <param name="messagesToSend">The messages to send.</param>
        /// <param name="sourceUser">The source user.</param>
        public void SendMultiPrivateMessageToUser(IEnumerable<IrcMessage> messagesToSend, string sourceUser)
        {
            foreach (IrcMessage msg in messagesToSend)
            {
                SendPrivateMessageToUser(msg, sourceUser);
            }
        }

        /// <summary>
        ///     Sends the private message to user.
        /// </summary>
        /// <param name="messageToPost">The message to post.</param>
        /// <param name="targetUser">The user to post.</param>
        public void SendPrivateMessageToUser(IrcMessage messageToPost, string targetUser)
        {
            try
            {
                IrcWriter.WriteLine("PRIVMSG {0} :{1} ", targetUser, messageToPost);
                IrcWriter.Flush();
                Thread.Sleep(100);
            }
            catch (Exception)
            {
                Log.ErrorFormat("Could Not Notice To User:{0} {1}", targetUser, messageToPost);
            }
        }

        /// <summary>
        ///     Sends the private message to user.
        /// </summary>
        /// <param name="messageText">The message to post.</param>
        /// <param name="targetUser">The user to post.</param>
        public void SendPrivateMessageToUser(string messageText, string targetUser)
        {
            SendPrivateMessageToUser(new IrcMessage(messageText), targetUser);
        }

        /// <summary>
        ///     Sends the notice to user.
        /// </summary>
        /// <param name="messageToPost">The message to post.</param>
        /// <param name="userToPost">The user to post.</param>
        public void SendNoticeToUser(string messageToPost, string userToPost)
        {
            try
            {
                IrcWriter.WriteLine("NOTICE {0} :{1} ", userToPost, messageToPost);
                IrcWriter.Flush();
                Thread.Sleep(100);
            }
            catch (Exception)
            {
                Log.ErrorFormat("Could Not Notice To User:{0} {1}", userToPost, messageToPost);
            }
        }

        /// <summary>
        ///     Posts a message to channel.
        /// </summary>
        /// <param name="messageToPost">The message to post.</param>
        /// <param name="channelToPost">The channel to post. (Remember to include the #)</param>
        /// <param name="preventLogging"></param>
        public void PostToChannel(IrcMessage messageToPost, string channelToPost, bool preventLogging = false)
        {
            try
            {
                double diffTime = (DateTime.Now - _LastPosted).TotalMilliseconds;
                if ((diffTime < 500) && (channelToPost.Contains("#"))) Thread.Sleep(500);

                IrcWriter.WriteLine("PRIVMSG {0} :{1} ", channelToPost, messageToPost.MessageText);
                IrcWriter.Flush();

                Console.ForegroundColor = ConsoleColor.Green;
                if (!preventLogging) Log.DebugFormat("SEND:{0} {1}", channelToPost, messageToPost.MessageText);
                Console.ForegroundColor = ConsoleColor.Gray;
                _LastPosted = DateTime.Now;
            }
            catch (Exception)
            {
                if (!preventLogging)
                    Log.ErrorFormat("Could Not Post To Channel:{0} {1}", channelToPost, messageToPost.MessageText);
            }
        }

        /// <summary>
        ///     Posts to channel.
        /// </summary>
        /// <param name="messageToPost">The message to post.</param>
        /// <param name="channelToPost">The channel to post.</param>
        /// <param name="preventLogging">if set to <c>true</c> [prevent logging].</param>
        public void PostToChannel(string messageToPost, string channelToPost, bool preventLogging = false)
        {
            PostToChannel(new IrcMessage(messageToPost), channelToPost, preventLogging);
        }

        private void ActionToChannel(IrcMessage messageToPost, string channelToPost)
        {
            try
            {
                double diffTime = (DateTime.Now - _LastPosted).TotalMilliseconds;
                if ((diffTime < 500) && (channelToPost.Contains("#"))) Thread.Sleep(500);

                IrcWriter.WriteLine("PRIVMSG {0} :{2}ACTION {1}{2}", channelToPost, messageToPost.MessageText,
                    Convert.ToChar(1));
                IrcWriter.Flush();

                Console.ForegroundColor = ConsoleColor.Green;
                Log.DebugFormat("SEND:{0} {1}", channelToPost, messageToPost);
                Console.ForegroundColor = ConsoleColor.Gray;
                _LastPosted = DateTime.Now;
            }
            catch (Exception)
            {
                Log.ErrorFormat("Could Not Post To Channel:{0} {1}", channelToPost, messageToPost);
            }
        }

        /// <summary>
        ///     Multis the post to channel.
        /// </summary>
        /// <param name="textToDisplay">The text to display.</param>
        /// <param name="channelToPost">The channel to post.</param>
        public void MultiPostToChannel(IEnumerable<IrcMessage> textToDisplay, string channelToPost)
        {
            if (textToDisplay == null)
            {
                PostToChannel(new IrcMessage("Unable to comply, please check command syntax and try again.."),
                    channelToPost);
                return;
            }
            foreach (IrcMessage msg in textToDisplay)
            {
                PostToChannel(msg, channelToPost);
            }
        }

        /// <summary>
        ///     Multis the action to channel.
        /// </summary>
        /// <param name="textToDisplay">The text to display.</param>
        /// <param name="channelToAction">The source channel.</param>
        public void MultiActionToChannel(IEnumerable<IrcMessage> textToDisplay, string channelToAction)
        {
            if (textToDisplay == null)
            {
                PostToChannel(new IrcMessage("Unable to comply, please check command syntax and try again.."),
                    channelToAction);
                return;
            }
            foreach (IrcMessage msg in textToDisplay)
            {
                ActionToChannel(msg, channelToAction);
            }
        }


        /// <summary>
        ///     Connects once.
        /// </summary>
        /// <param name="connectServer">The connect server.</param>
        /// <param name="connectPort">The connect port.</param>
        public void ConnectOnce(string connectServer, int connectPort)
        {
            _ConnectServer = connectServer;
            _ConnectPort = connectPort;
            ConnectOnceAndProcessMessages();
        }

        public void Disconnect()
        {
            ImAlive = false;
        }

        private void ConnectOnceAndProcessMessages()
        {
            Log.Info("Connecting to IRC Server...");

            IrcServer = _ConnectServer;
            IrcPort = _ConnectPort;

            // ConnectOnce with the IRC server.
            InternalIrcConnection = new TcpClient(IrcServer, IrcPort);
            IrcStream = InternalIrcConnection.GetStream();
            IrcReader = new StreamReader(IrcStream);
            IrcWriter = new StreamWriter(IrcStream);

            // Authenticate our user
            string isInvisible = IsInvisble ? "8" : "0";
            IrcWriter.WriteLine("USER {0} {1} * :{2}", IrcUser, isInvisible, IrcRealName);
            IrcWriter.Flush();
            CurrentIrcNick = MainIrcNick + "_" + DateTime.Now.ToString("_yyyyMMddhhmm");
            IrcWriter.WriteLine("NICK {0}", CurrentIrcNick);
            IrcWriter.Flush();
            IrcWriter.WriteLine("MODE {0} +B", CurrentIrcNick);
            IrcWriter.Flush();
            DateTime startTime = DateTime.Now;
            DateTime lastElection = DateTime.MinValue;

            ImAlive = true;
            Log.Info("Connection Ready!");
            // Listen for commands
            while (ImAlive)
            {
                string ircCommand;
                IrcReader.BaseStream.ReadTimeout = 5000;
                try
                {
                    ircCommand = IrcReader.ReadLine();
                }
                catch
                {
                    ircCommand = null;
                }
                if (ircCommand != null)
                {
                    ProcessRawMessage(ircCommand);
                }
                if ((DateTime.Now - lastElection).TotalMinutes > 1)
                {
                    lastElection = DateTime.Now;
                    RefreshNamesList(IrcElectionChannel);
                }

                if (startTime >= DateTime.Now.AddSeconds(-15)) continue;
                startTime = RunInitialStartup(startTime);
            }
            IrcWriter.Close();
            IrcReader.Close();
            InternalIrcConnection.Close();
            Log.Info("Link Closed (on purpose).. ");
        }

        /// <summary>
        ///     Runs the Initial Startup
        /// </summary>
        /// <param name="startTime">The start time.</param>
        /// <returns>startup Time</returns>
        private DateTime RunInitialStartup(DateTime startTime)
        {
            // Only do the next stuff on startup
            ConnectToChannel(IrcElectionChannel);
            startTime = startTime.AddYears(1);
            _Lastwinner = Guid.NewGuid().ToString();
            return startTime;
        }

        /// <summary>
        ///     Processes the raw message.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        private void ProcessRawMessage(string ircCommand)
        {
            Log.DebugFormat("{0:hh:mm} {1}", DateTime.Now, ircCommand);
            ImAlive = true;

            EventReceiving?.Invoke(ircCommand);

            string[] commandParts = ircCommand.Split(' ');
            if (commandParts[0].Substring(0, 1) == ":")
            {
                commandParts[0] = commandParts[0].Remove(0, 1);
            }

            // Server message
            switch (commandParts[1])
            {
                case "332":
                    //IrcTopic(commandParts);
                    break;
                case "333":
                    //IrcTopicOwner(commandParts);
                    break;
                case "353":
                    ProcessNamesList(commandParts);
                    break;
                case "366": /*this.IrcEndNamesList(commandParts);*/
                    break;
                case "372": /*this.IrcMOTD(commandParts);*/
                    break;
                case "376": /*this.IrcEndMOTD(commandParts);*/
                    break;
                default:
                    IrcServerMessage(commandParts);
                    break;
            }

            if (commandParts[0] == "PING")
            {
                // Server PING, send PONG back
                IrcPing(commandParts);
            }
            else
            {
                // Normal message
                string commandAction = commandParts[1];
                switch (commandAction)
                {
                    case "JOIN":
                        IrcJoin(commandParts);
                        break;
                    case "PART":
                        IrcPart(commandParts);
                        break;
                    case "MODE":
                        IrcMode(commandParts);
                        break;
                    case "NICK":
                        IrcNickChange(commandParts);
                        break;
                    case "KICK":
                        IrcKick(commandParts);
                        break;
                    case "QUIT":
                        IrcQuit(commandParts);
                        break;
                }
            }
        }


        /// <summary>
        ///     Connects to channels.
        /// </summary>
        private void ConnectToChannels()
        {
            Debug.Assert(IrcChannel != null, "ircChannel != null");
            string[] channelList = IrcChannel.Split(' ');
            foreach (string thisChannel in channelList)
            {
                Log.InfoFormat("Joining channel:{0}", thisChannel.Split(':')[0]);
                IrcWriter.WriteLine("JOIN {0}", thisChannel.Replace(":", " "));
                IrcWriter.Flush();
                ConnectedTime = DateTime.Now;
            }
            IrcWriter.WriteLine("JOIN " + IrcElectionChannel);
            IrcWriter.Flush();

            ConnectedToChannels = true;

        }

        /// <summary>
        ///     Connects to channel.
        /// </summary>
        /// <param name="channelName">Name of the channel.</param>
        private void ConnectToChannel(string channelName)
        {
            Log.InfoFormat("Joining channel:{0}", channelName.Split(':')[0]);
            IrcWriter.WriteLine("JOIN {0}", channelName.Replace(":", " "));
            IrcWriter.Flush();
            ConnectedTime = DateTime.Now;
            IrcWriter.Flush();
        }


        /// <summary>
        ///     Leaves the channel.
        /// </summary>
        /// <param name="channelName">Name of the channel.</param>
        private void LeaveChannel(string channelName)
        {
            Log.InfoFormat("Leaving channel:{0}", channelName.Split(':')[0]);
            IrcWriter.WriteLine("PART {0}", channelName.Replace(":", " "));
            IrcWriter.Flush();
            ConnectedTime = DateTime.Now;
            IrcWriter.Flush();
        }

        /// <summary>
        ///     Leaves the channels.
        /// </summary>
        private void LeaveChannels()
        {
            Debug.Assert(IrcChannel != null, "ircChannel != null");
            string[] channelList = IrcChannel.Split(' ');
            foreach (string thisChannel in channelList)
            {
                Log.InfoFormat("Leaving channel:" + thisChannel.Split(':')[0]);
                IrcWriter.WriteLine("PART {0} {1}", thisChannel.Split(':')[0],
                    "It is time to leave this wretched place, and seek another, even more terrible!");
                IrcWriter.Flush();
                ConnectedTime = DateTime.Now;
            }
            IrcWriter.Flush();

            ConnectedToChannels = false;

        }

        /// <summary>
        ///     Changes the nick.
        /// </summary>
        /// <param name="newNick">The new nick.</param>
        private void ChangeNick(string newNick)
        {
            Debug.Assert(IrcChannel != null, "ircChannel != null");
            Log.Info("Renaming myself:" + newNick);
            IrcWriter.WriteLine("NICK {0}", newNick);
            IrcWriter.Flush();
            ConnectedTime = DateTime.Now;
        }

        /// <summary>
        ///     Processes the names list.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        private void ProcessNamesList(string[] ircCommand)
        {
            _UserList = new List<string>();
            for (int intI = 5; intI < ircCommand.Length; intI++)
            {
                _UserList.Add(ircCommand[intI].Replace("@", "").Replace(":", ""));
            }

            string sourceChannel = ircCommand[4];

            // Do Election Stuff
            if (sourceChannel == IrcElectionChannel) ElectNewBot(ircCommand, sourceChannel);
        }

        /// <summary>
        ///     Elects the new bot.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <param name="sourceChannel">The source channel.</param>
        private void ElectNewBot(string[] ircCommand, string sourceChannel)
        {
            List<string> allNicks = new List<string>();
            for (int i = 5; i < ircCommand.Length; i++)
            {
                if (ircCommand[i].Length > 1)
                    allNicks.Add(
                        ircCommand[i].Replace("~", "")
                            .Replace("@", "")
                            .Replace("&", "")
                            .Replace("%", "")
                            .Replace("+", "")
                            .Replace(":", ""));
            }
            allNicks.Sort();
            string winner = allNicks[0];
            if (_Lastwinner != winner)
            {
                PostToChannel(
                    new IrcMessage($"I had an election and I have decided that the winning bot is: {winner}"),
                    sourceChannel);
                if (winner == CurrentIrcNick)
                {
                    CurrentIrcNick = MainIrcNick;
                    ConnectToChannels();
                    PostToChannel(new IrcMessage("Connected to channels."), sourceChannel);
                }
                else
                {
                    LeaveChannels();
                    PostToChannel(new IrcMessage("Parted from channels."), sourceChannel);
                    CurrentIrcNick = $"{MainIrcNick}_{DateTime.Now.ToString("_yyyyMMddhhmm")}";
                }
                _Lastwinner = winner;
            }
        }

        /* ProcessNamesList */

        private void IrcServerMessage(string[] ircCommand)
        {
            string serverMessage = "";
            for (int intI = 1; intI < ircCommand.Length; intI++)
            {
                serverMessage += ircCommand[intI] + " ";
            }
            EventServerMessage?.Invoke(serverMessage.Trim());
        } /* IrcServerMessage */


        private void IrcPing(string[] ircCommand)
        {
            string pingHash = "";
            for (int intI = 1; intI < ircCommand.Length; intI++)
            {
                pingHash += ircCommand[intI] + " ";
            }
            pingHash = pingHash.Replace(":", "");
            IrcWriter.WriteLine("PONG " + pingHash);
            IrcWriter.Flush();
        } /* IrcPing */


        private void IrcJoin(IList<string> ircCommand)
        {
            string sourceIrcChannel = ircCommand[2];
            string sourceIrcUser = ircCommand[0].Split('!')[0];
            EventJoin?.Invoke(sourceIrcChannel.Remove(0, 1), sourceIrcUser);
        } /* IrcJoin */

        private void IrcPart(IList<string> ircCommand)
        {
            string sourceIrcChannel = ircCommand[2];
            string sourceIrcUser = ircCommand[0].Split('!')[0];
            EventPart?.Invoke(sourceIrcChannel, sourceIrcUser);
        } /* IrcPart */

        private void IrcMode(IList<string> ircCommand)
        {
            string msgIrcChannel = ircCommand[2];
            string msgIrcUser = ircCommand[0].Split('!')[0];
            string userMode = "";
            for (int intI = 3; intI < ircCommand.Count; intI++)
            {
                userMode += ircCommand[intI] + " ";
            }
            if (userMode.Substring(0, 1) == ":")
            {
                userMode = userMode.Remove(0, 1);
            }
            EventMode?.Invoke(msgIrcChannel, msgIrcUser, userMode.Trim());
        } /* IrcMode */

        private void IrcNickChange(IList<string> ircCommand)
        {
            string userOldNick = ircCommand[0].Split('!')[0];
            string userNewNick = ircCommand[2].Remove(0, 1);
            EventNickChange?.Invoke(userOldNick, userNewNick, ircCommand[2]);
        } /* IrcNickChange */

        private void IrcKick(string[] ircCommand)
        {
            string userKicker = ircCommand[0].Split('!')[0];
            string userKicked = ircCommand[3];
            string msgIrcChannel = ircCommand[2];
            string kickMessage = "";
            for (int intI = 4; intI < ircCommand.Length; intI++)
            {
                kickMessage += ircCommand[intI] + " ";
            }
            EventKick?.Invoke(msgIrcChannel, userKicker, userKicked, kickMessage.Remove(0, 1).Trim());
        } /* IrcKick */

        private void IrcQuit(string[] ircCommand)
        {
            string userQuit = ircCommand[0].Split('!')[0];
            string quitMessage = "";
            for (int intI = 2; intI < ircCommand.Length; intI++)
            {
                quitMessage += ircCommand[intI] + " ";
            }
            EventQuit?.Invoke(userQuit, quitMessage.Remove(0, 1).Trim(), ircCommand[2]);
        } /* IrcQuit */


        /// <summary>
        ///     Refreshes the names list.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns></returns>
        public void RefreshNamesList(string channel)
        {
            Log.Info("Getting names list..");
            string cmd = $"NAMES {channel}";
            IrcWriter.WriteLine(cmd);
            IrcWriter.Flush();
        }


        /// <summary>
        ///     Joins the channel.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        public void JoinChannel(string ircCommand)
        {
            if (ircCommand.Length > 4)
            {
                ConnectToChannel(ircCommand.Split(' ')[4]);
            }
        }

        /// <summary>
        ///     Parts the channel.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        public void PartChannel(string ircCommand)
        {
            if (ircCommand.Length > 2)
            {
                LeaveChannel(ircCommand.Split(' ')[2]);
            }
        }

        #endregion
    }

    /* IRC */
} /* System.Net */