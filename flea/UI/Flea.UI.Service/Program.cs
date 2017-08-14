using System.ServiceProcess;

namespace FleaService
{
    static class Program
    {
        #region Members

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] servicesToRun =
            {
                new FleaServiceController()
            };
            ServiceBase.Run(servicesToRun);
        }

        #endregion
    }
}