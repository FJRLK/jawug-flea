using System;
using System.Collections.Generic;

namespace MonoTorrent.Client.Connections
{
    public static class ConnectionFactory
    {
        #region Static

        private static object locker = new object();
        private static Dictionary<string, Type> trackerTypes = new Dictionary<string, Type>();

        #endregion

        #region Constructor

        static ConnectionFactory()
        {
            RegisterTypeForProtocol("tcp", typeof (IPV4Connection));
            RegisterTypeForProtocol("ipv6", typeof (IPV6Connection));
            RegisterTypeForProtocol("http", typeof (HttpConnection));
        }

        #endregion

        #region Members

        public static void RegisterTypeForProtocol(string protocol, Type connectionType)
        {
            if (string.IsNullOrEmpty(protocol))
                throw new ArgumentException("cannot be null or empty", "protocol");
            if (connectionType == null)
                throw new ArgumentNullException("connectionType");

            lock (locker)
                trackerTypes[protocol] = connectionType;
        }

        public static IConnection Create(Uri connectionUri)
        {
            if (connectionUri == null)
                throw new ArgumentNullException("connectionUrl");

            Type type;
            lock (locker)
                if (!trackerTypes.TryGetValue(connectionUri.Scheme, out type))
                    return null;

            try
            {
                return (IConnection) Activator.CreateInstance(type, connectionUri);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}