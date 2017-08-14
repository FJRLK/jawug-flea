using System;
using System.Net;
using System.Reflection;
using log4net;
using MonoTorrent.BEncoding;

namespace MonoTorrent.Tracker.Listeners
{
    public class HttpListener : ListenerBase
    {
        #region Static

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Internals

        private System.Net.HttpListener listener;

        private string prefix;

        /// <summary>
        ///     True if the listener is waiting for incoming connections
        /// </summary>
        public override bool Running
        {
            get { return listener != null; }
        }

        #endregion

        #region Constructor

        public HttpListener(IPAddress address, int port)
            : this($"http://{address}:{port}/announce/")
        {
        }

        public HttpListener(IPEndPoint endpoint)
            : this(endpoint.Address, endpoint.Port)
        {
        }

        public HttpListener(string httpPrefix)
        {
            if (string.IsNullOrEmpty(httpPrefix))
                throw new ArgumentNullException("httpPrefix");

            prefix = httpPrefix;
        }

        #endregion

        #region Members

        /// <summary>
        ///     Starts listening for incoming connections
        /// </summary>
        public override void Start()
        {
            if (Running)
                return;

            listener = new System.Net.HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
            listener.BeginGetContext(EndGetRequest, listener);
        }

        /// <summary>
        ///     Stops listening for incoming connections
        /// </summary>
        public override void Stop()
        {
            if (!Running)
                return;

            IDisposable d = (IDisposable) listener;
            listener = null;
            d.Dispose();
        }

        private void EndGetRequest(IAsyncResult result)
        {
            HttpListenerContext context = null;
            System.Net.HttpListener listener = (System.Net.HttpListener) result.AsyncState;

            try
            {
                context = listener.EndGetContext(result);
                using (context.Response)
                    HandleRequest(context);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Exception in listener: {0}{1}", Environment.NewLine, ex);
            }
            finally
            {
                try
                {
                    if (listener.IsListening)
                        listener.BeginGetContext(EndGetRequest, listener);
                }
                catch
                {
                    Stop();
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            bool isScrape = context.Request.RawUrl.StartsWith("/scrape", StringComparison.OrdinalIgnoreCase);

            BEncodedValue responseData = Handle(context.Request.RawUrl, context.Request.RemoteEndPoint.Address, isScrape);

            byte[] response = responseData.Encode();
            context.Response.ContentType = "text/plain";
            context.Response.StatusCode = 200;
            context.Response.ContentLength64 = response.LongLength;
            context.Response.OutputStream.Write(response, 0, response.Length);
        }

        #endregion
    }
}