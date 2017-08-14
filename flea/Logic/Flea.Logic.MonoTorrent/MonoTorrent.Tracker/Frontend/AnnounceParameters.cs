using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using MonoTorrent.BEncoding;
using MonoTorrent.Common;

namespace MonoTorrent.Tracker
{
    public class AnnounceParameters : RequestParameters
    {
        #region Static

        // FIXME: Expose these as configurable options
        internal static readonly int DefaultWanted = 30;

        private static readonly string[] mandatoryFields =
        {
            "info_hash", "peer_id", "port", "uploaded", "downloaded", "left", "compact"
        };

        internal static readonly bool UseTrackerKey = false;

        #endregion

        #region Internals

        private IPEndPoint clientAddress;
        InfoHash infoHash;
        private bool isValid;

        public IPEndPoint ClientAddress
        {
            get { return clientAddress; }
        }

        public int Downloaded
        {
            get { return ParseInt("downloaded"); }
        }

        public TorrentEvent Event
        {
            get
            {
                string e = Parameters["event"];
                if (e != null)
                {
                    if (e.Equals("started"))
                        return TorrentEvent.Started;
                    if (e.Equals("stopped"))
                        return TorrentEvent.Stopped;
                    if (e.Equals("completed"))
                        return TorrentEvent.Completed;
                }

                return TorrentEvent.None;
            }
        }

        public bool HasRequestedCompact
        {
            get { return ParseInt("compact") == 1; }
        }

        public InfoHash InfoHash
        {
            get { return infoHash; }
        }

        public override bool IsValid
        {
            get { return isValid; }
        }

        public string Key
        {
            get { return Parameters["key"]; }
        }

        public int Left
        {
            get { return ParseInt("left"); }
        }

        public int NumberWanted
        {
            get
            {
                int val = ParseInt(Parameters["numwant"]);
                return val != 0 ? val : DefaultWanted;
            }
        }

        public string PeerId
        {
            get { return Parameters["peer_id"]; }
        }

        public int Port
        {
            get { return ParseInt("port"); }
        }

        public string TrackerId
        {
            get { return Parameters["trackerid"]; }
        }

        public long Uploaded
        {
            get { return ParseInt("uploaded"); }
        }

        #endregion

        #region Constructor

        public AnnounceParameters(NameValueCollection collection, IPAddress address)
            : base(collection, address)
        {
            CheckMandatoryFields();
            if (!isValid)
                return;

            /* If the user has supplied an IP address, we use that instead of
             * the IP address we read from the announce request connection. */
            IPAddress supplied;
            if (IPAddress.TryParse(Parameters["ip"] ?? "", out supplied) && !supplied.Equals(IPAddress.Any))
                clientAddress = new IPEndPoint(supplied, Port);
            else
                clientAddress = new IPEndPoint(address, Port);
        }

        #endregion

        #region Members

        private void CheckMandatoryFields()
        {
            isValid = false;

            List<string> keys = new List<string>(Parameters.AllKeys);
            foreach (string field in mandatoryFields)
            {
                if (keys.Contains(field))
                    continue;

                Response.Add(FailureKey,
                    (BEncodedString) ("mandatory announce parameter " + field + " in query missing"));
                return;
            }
            byte[] hash = UriHelper.UrlDecode(Parameters["info_hash"]);
            if (hash.Length != 20)
            {
                Response.Add(FailureKey,
                    (BEncodedString)
                        ($"infohash was {hash.Length} bytes long, it must be 20 bytes long."));
                return;
            }
            infoHash = new InfoHash(hash);
            isValid = true;
        }

        public override int GetHashCode()
        {
            return RemoteAddress.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            AnnounceParameters other = obj as AnnounceParameters;
            return other == null
                ? false
                : other.clientAddress.Equals(clientAddress)
                  && other.Port.Equals(Port);
        }

        private int ParseInt(string str)
        {
            int p;
            str = Parameters[str];
            if (!int.TryParse(str, out p))
                p = 0;
            return p;
        }

        #endregion
    }
}