using System.ComponentModel;
using System.Configuration.Install;

namespace FleaService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        #region Constructor

        public ProjectInstaller()
        {
            InitializeComponent();
        }

        #endregion
    }
}