using MonoTorrent.Client.Messages.Standard;

namespace MonoTorrent.Client.Connections
{
    public partial class HttpConnection
    {
        #region Types

        private class HttpRequestData
        {
            #region Internals

            public RequestMessage Request;
            public bool SentHeader;
            public bool SentLength;
            public int TotalReceived;
            public int TotalToReceive;

            public bool Complete
            {
                get { return TotalToReceive == TotalReceived; }
            }

            #endregion

            #region Constructor

            public HttpRequestData(RequestMessage request)
            {
                Request = request;
                PieceMessage m = new PieceMessage(request.PieceIndex, request.StartOffset, request.RequestLength);
                TotalToReceive = m.ByteLength;
            }

            #endregion
        }

        #endregion
    }
}