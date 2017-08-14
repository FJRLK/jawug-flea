using System;
using System.Runtime.Serialization;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    public class ProtocolException : TorrentException
    {
        #region Constructor

        public ProtocolException()
            : base()
        {
        }


        public ProtocolException(string message)
            : base(message)
        {
        }


        public ProtocolException(string message, Exception innerException)
            : base(message, innerException)
        {
        }


        public ProtocolException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}