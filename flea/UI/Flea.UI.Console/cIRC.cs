using System.Configuration;
using Flea.Logic.MainUnit;
using Flea.Logic.Utils;

namespace Flea.UI.FleaMain
{
    // ReSharper disable once InconsistentNaming
    public static class CIRC
    {
        #region Members

        /// <summary>
        ///     Defines the entry point of the application.
        /// </summary>
        private static void Main()
        {
            // dummy to force DLL load
            IRCAppender appender = new IRCAppender();


            // real start
            string ircServerList = ConfigurationManager.AppSettings["IrcServers"];
            int ircPort = int.Parse(ConfigurationManager.AppSettings["IrcPort"]);
            string ircUser = ConfigurationManager.AppSettings["IrcUser"];
            string ircChan = ConfigurationManager.AppSettings["IrcChan"];
            string ircElectionChan = "#botelection";
            string elect = ConfigurationManager.AppSettings["IrcElectionChan"];
            if (elect != null) ircElectionChan = elect;
            string[] ircServerListArray = ircServerList.Split(' ');

            FleaController myFleaController = new FleaController(ircServerListArray, ircPort, ircUser, ircChan,
                ircElectionChan);
            myFleaController.StartFlea();
        } /* Main */

        #endregion
    }
}