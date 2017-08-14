using System;
using System.Collections.Specialized;
using System.Net;
using MonoTorrent.BEncoding;

namespace MonoTorrent.Tracker
{
    public abstract class RequestParameters : EventArgs
    {
        #region Static

        protected internal static readonly string FailureKey = "failure reason";
        protected internal static readonly string WarningKey = "warning message";

        #endregion

        #region Internals

        private NameValueCollection parameters;

        private IPAddress remoteAddress;
        private BEncodedDictionary response;

        public abstract bool IsValid { get; }

        public NameValueCollection Parameters
        {
            get { return parameters; }
        }

        public IPAddress RemoteAddress
        {
            get { return remoteAddress; }
            protected set { remoteAddress = value; }
        }

        public BEncodedDictionary Response
        {
            get { return response; }
        }

        #endregion

        #region Constructor

        protected RequestParameters(NameValueCollection parameters, IPAddress address)
        {
            this.parameters = parameters;
            remoteAddress = address;
            response = new BEncodedDictionary();
        }

        #endregion
    }
}