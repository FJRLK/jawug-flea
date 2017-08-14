using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;

namespace Flea.Logic.Utils
{
    public class IRCAppender : AppenderSkeleton
    {
        private static IIrcConnection IRCConnectionObject;
        private bool loggingInProgress;
        public static bool LoggingEnabled { get; set; } = false;

        protected override void Append(LoggingEvent loggingEvent)
        {
            // prevent recursion
            if (!loggingInProgress && LoggingEnabled)
            {
                loggingInProgress = true;
                IRCConnectionObject?.SendLogMessage(loggingEvent.RenderedMessage);
                loggingInProgress = false;
            }
        }

        public static void SetIRCLink(IIrcConnection ircObject)
        {
            IRCConnectionObject = ircObject;
        }
    }

    public interface IIrcConnection
    {
        void SendLogMessage(string messageToSend);
    }
}
