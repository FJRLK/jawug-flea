using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;

namespace MonoTorrent.Tracker
{
    public class ScrapeParameters : RequestParameters
    {
        #region Internals

        private List<InfoHash> hashs;

        public int Count
        {
            get { return hashs.Count; }
        }

        public List<InfoHash> InfoHashes
        {
            get { return hashs; }
        }

        public override bool IsValid
        {
            get { return true; }
        }

        #endregion

        #region Constructor

        public ScrapeParameters(NameValueCollection collection, IPAddress address)
            : base(collection, address)
        {
            hashs = new List<InfoHash>();
            ParseHashes(Parameters["info_hash"]);
        }

        #endregion

        #region Members

        private void ParseHashes(string infoHash)
        {
            if (string.IsNullOrEmpty(infoHash))
                return;

            if (infoHash.IndexOf(',') > 0)
            {
                string[] stringHashs = infoHash.Split(',');
                for (int i = 0; i < stringHashs.Length; i++)
                    hashs.Add(InfoHash.UrlDecode(stringHashs[i]));
            }
            else
            {
                hashs.Add(InfoHash.UrlDecode(infoHash));
            }
        }

        public IEnumerator GetEnumerator()
        {
            return hashs.GetEnumerator();
        }

        #endregion
    }
}