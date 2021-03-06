using System;
using System.Reflection;

namespace MonoTorrent.Common
{
    public static class VersionInfo
    {
        #region Static

        /// <summary>
        ///     Protocol string for version 1.0 of Bittorrent Protocol
        /// </summary>
        public static readonly string ProtocolStringV100 = "BitTorrent protocol";

        /// <summary>
        ///     The current version of the client
        /// </summary>
        public static readonly string ClientVersion = CreateClientVersion();

        public static readonly string DhtClientVersion = "MO06";


        internal static Version Version;

        #endregion

        #region Members

        static string CreateClientVersion()
        {
            AssemblyInformationalVersionAttribute versionAttr;
            Assembly assembly = Assembly.GetExecutingAssembly();
            versionAttr =
                (AssemblyInformationalVersionAttribute)
                    assembly.GetCustomAttributes(typeof (AssemblyInformationalVersionAttribute), false)[0];
            Version = new Version(versionAttr.InformationalVersion);

            // 'MO' for MonoTorrent then four digit version number
            string version =
                $"{Math.Max(Version.Major, 0)}{Math.Max(Version.Minor, 0)}{Math.Max(Version.Build, 0)}{Math.Max(Version.Revision, 0)}";
            if (version.Length > 4)
                version = version.Substring(0, 4);
            else
                version = version.PadRight(4, '0');
            return $"-MO{version}-";
        }

        #endregion
    }
}