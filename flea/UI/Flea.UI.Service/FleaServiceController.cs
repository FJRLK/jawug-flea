using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using Flea.Logic.MainUnit;

namespace FleaService
{
    public partial class FleaServiceController : ServiceBase
    {
        #region Internals

        readonly FleaController _MyFleaController;

        #endregion

        #region Constructor

        public FleaServiceController()
        {
            InitializeComponent();
            string ircServerList = ConfigurationManager.AppSettings["IrcServers"];
            int ircPort = int.Parse(ConfigurationManager.AppSettings["IrcPort"]);
            string ircUser = ConfigurationManager.AppSettings["IrcUser"];
            string ircChan = ConfigurationManager.AppSettings["IrcChan"];
            string[] ircServerListArray = ircServerList.Split(' ');
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            string ircElectionChan = "#botelection";
            string elect = ConfigurationManager.AppSettings["IrcElectionChan"];
            if (elect != null) ircElectionChan = elect;

            _MyFleaController = new FleaController(ircServerListArray, ircPort, ircUser, ircChan, ircElectionChan);
        }

        #endregion

        #region Members

        protected override void OnStart(string[] args)
        {
            _MyFleaController.StartFlea();
        }

        protected override void OnStop()
        {
            _MyFleaController.StopFlea();
        }

        #endregion
    }
}