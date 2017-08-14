using System;
using System.Collections.Generic;

namespace MonoTorrent.Common
{
    public class UriQueryBuilder
    {
        #region Internals

        UriBuilder builder;
        Dictionary<string, string> queryParams;

        public string this[string key]
        {
            get { return queryParams[key]; }
            set { queryParams[key] = value; }
        }

        #endregion

        #region Constructor

        public UriQueryBuilder(string uri)
            : this(new Uri(uri))

        {
        }

        public UriQueryBuilder(Uri uri)
        {
            builder = new UriBuilder(uri);
            queryParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ParseParameters();
        }

        #endregion

        #region Members

        public UriQueryBuilder Add(string key, object value)
        {
            Check.Key(key);
            Check.Value(value);

            queryParams[key] = value.ToString();
            return this;
        }

        public bool Contains(string key)
        {
            return queryParams.ContainsKey(key);
        }

        void ParseParameters()
        {
            if (builder.Query.Length == 0 || !builder.Query.StartsWith("?"))
                return;

            string[] strs = builder.Query.Remove(0, 1).Split('&');
            for (int i = 0; i < strs.Length; i++)
            {
                string[] kv = strs[i].Split('=');
                if (kv.Length == 2)
                    queryParams.Add(kv[0].Trim(), kv[1].Trim());
            }
        }

        public override string ToString()
        {
            return ToUri().OriginalString;
        }

        public Uri ToUri()
        {
            string result = "";
            foreach (KeyValuePair<string, string> keypair in queryParams)
                result += keypair.Key + "=" + keypair.Value + "&";
            builder.Query = result.Length == 0 ? result : result.Remove(result.Length - 1);
            return builder.Uri;
        }

        #endregion
    }
}