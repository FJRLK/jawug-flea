using System;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Flea.Logic.CommandUnit.Properties;
using log4net;

namespace Flea.Logic.CommandUnit.Internals
{
    class TelnetConnection
    {
        #region Static

        private const int TimeOutMs = 100;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Internals

        readonly TcpClient _tcpSocket;

        /// <summary>
        ///     Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected
        {
            get { return _tcpSocket != null && _tcpSocket.Connected; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="TelnetConnection" /> class.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <param name="port">The port.</param>
        public TelnetConnection(string hostname, int port)
        {
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    TimeOutSocket tSocket = new TimeOutSocket();
                    _tcpSocket = tSocket.Connect(hostname, port, 2000 + i*1000);
                    Log.InfoFormat("Connection established to {0}:{1}", hostname, port);
                    break;
                }
                catch (Exception e5)
                {
                    //Logger.WriteLine("Connection failed to {0}:{1} on attempt {2}. {3}", Hostname, Port, i+1, e.Message);
                    if (i == 2) Log.ErrorFormat("Gave up on Telnet connection: {0}", e5.Message);
                }
            }
        }

        #endregion

        #region Members

        /// <summary>
        ///     Closes this instance.
        /// </summary>
        [UsedImplicitly]
        public void Close()
        {
            if (_tcpSocket != null)
                _tcpSocket.Close();
        }


        /// <summary>
        ///     Writes the line.
        /// </summary>
        /// <param name="cmd">The command.</param>
        public void WriteLine(string cmd)
        {
            Write(cmd + "\n");
        }

        /// <summary>
        ///     Writes the specified command.
        /// </summary>
        /// <param name="cmd">The command.</param>
        private void Write(string cmd)
        {
            if (!_tcpSocket.Connected) return;
            byte[] buf = Encoding.ASCII.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF"));
            _tcpSocket.GetStream().Write(buf, 0, buf.Length);
            _tcpSocket.GetStream().Flush();
        }

        /// <summary>
        ///     Reads this instance.
        /// </summary>
        /// <returns></returns>
        public string Read()
        {
            if (!_tcpSocket.Connected) return null;
            StringBuilder sb = new StringBuilder();
            DateTime startTime = DateTime.Now;
            TimeSpan elapsed;
            do
            {
                //Console.ForegroundColor = ConsoleColor.Green;
                //Logger.Write("*");
                //Console.ForegroundColor = ConsoleColor.Gray;

                bool gotData = ParseTelnet(sb);
                //Logger.Write("\b \b");
                if (gotData)
                {
                    startTime = DateTime.Now;
                    //Logger.Write(sb);
                }
                elapsed = DateTime.Now - startTime;
            } while
                (
                (!sb.ToString().TrimEnd().EndsWith(":"))
                && (elapsed.TotalMilliseconds < TimeOutMs)
                && (!sb.ToString().TrimEnd().EndsWith(">"))
                && (!sb.ToString().EndsWith("\n"))
                );

            string dataReceived = sb.ToString();
            string pattern = ((char) 27) + "\\[(\\w)*m";
            string dataResponse = Regex.Replace(dataReceived, pattern, "");
            return dataResponse;
        }

        /// <summary>
        ///     Parses the telnet.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <returns></returns>
        private bool ParseTelnet(StringBuilder sb)
        {
            bool gotData = false;
            while (_tcpSocket.Available > 0)
            {
                int input = _tcpSocket.GetStream().ReadByte();
                switch (input)
                {
                    case -1:
                        break;
                    case (int) TelnetVerbs.IAC:
                        // interpret as command
                        int inputverb = _tcpSocket.GetStream().ReadByte();
                        if (inputverb == -1) break;
                        switch (inputverb)
                        {
                            case (int) TelnetVerbs.IAC:
                                //literal IAC = 255 escaped, so append char 255 to string
                                sb.Append(inputverb);
                                break;
                            case (int) TelnetVerbs.DO:
                            case (int) TelnetVerbs.DONT:
                            case (int) TelnetVerbs.WILL:
                            case (int) TelnetVerbs.WONT:
                                // reply to all commands with "WONT", unless it is SGA (suppres go ahead)
                                int inputoption = _tcpSocket.GetStream().ReadByte();
                                if (inputoption == -1) break;
                                _tcpSocket.GetStream().WriteByte((byte) TelnetVerbs.IAC);
                                if (inputoption == (int) TelnetTcpOptions.SGA)
                                    _tcpSocket.GetStream()
                                        .WriteByte(inputverb == (int) TelnetVerbs.DO
                                            ? (byte) TelnetVerbs.WILL
                                            : (byte) TelnetVerbs.DO);
                                else
                                    _tcpSocket.GetStream()
                                        .WriteByte(inputverb == (int) TelnetVerbs.DO
                                            ? (byte) TelnetVerbs.WONT
                                            : (byte) TelnetVerbs.DONT);
                                _tcpSocket.GetStream().WriteByte((byte) inputoption);
                                break;
                        }
                        break;
                    default:
                        sb.Append((char) input);
                        gotData = true;
                        break;
                }
            }
            return gotData;
        }

        #endregion
    }
}