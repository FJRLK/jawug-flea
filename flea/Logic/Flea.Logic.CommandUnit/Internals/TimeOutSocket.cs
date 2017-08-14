using System;
using System.Net.Sockets;
using System.Threading;

namespace Flea.Logic.CommandUnit.Internals
{
    public class TimeOutSocket
    {
        #region Internals

        private readonly ManualResetEvent _TimeoutObject = new ManualResetEvent(false);
        private bool _IsConnectionSuccessful;
        private Exception _Socketexception;

        #endregion

        #region Members

        /// <summary>
        ///     Connects the specified hostname.
        /// </summary>
        /// <param name="Hostname">The hostname.</param>
        /// <param name="Port">The port.</param>
        /// <param name="timeoutMSec">The timeout m sec.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Failed.</exception>
        /// <exception cref="System.TimeoutException">TimeOut Exception</exception>
        public TcpClient Connect(string Hostname, int Port, int timeoutMSec)
        {
            _TimeoutObject.Reset();
            _Socketexception = null;

            TcpClient tcpclient = new TcpClient();
            tcpclient.BeginConnect(Hostname, Port, CallBackMethod, tcpclient);

            if (_TimeoutObject.WaitOne(timeoutMSec, false))
            {
                if (_IsConnectionSuccessful)
                {
                    return tcpclient;
                }
                if (_Socketexception != null)
                    throw _Socketexception;
                throw new Exception("Failed.");
            }
            tcpclient.Close();
            throw new TimeoutException("TimeOut Exception");
        }

        /// <summary>
        ///     Calls the back method.
        /// </summary>
        /// <param name="asyncresult">The asyncresult.</param>
        private void CallBackMethod(IAsyncResult asyncresult)
        {
            try
            {
                _IsConnectionSuccessful = false;
                TcpClient tcpclient = asyncresult.AsyncState as TcpClient;

                if (tcpclient != null && tcpclient.Client != null)
                {
                    tcpclient.EndConnect(asyncresult);
                    _IsConnectionSuccessful = true;
                }
            }
            catch (Exception ex)
            {
                _IsConnectionSuccessful = false;
                _Socketexception = ex;
            }
            finally
            {
                _TimeoutObject.Set();
            }
        }

        #endregion
    }
}