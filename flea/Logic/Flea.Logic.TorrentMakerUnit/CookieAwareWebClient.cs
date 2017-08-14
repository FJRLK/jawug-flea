using System;
using System.Net;

namespace TorrentMakerUnit
{
    class CookieAwareWebClient : WebClient
    {
        #region Internals

        private CookieContainer cc = new CookieContainer();
        private string lastPage;

        #endregion

        #region Members

        public void SetCookieContainer(CookieContainer newCookieBox)
        {
            cc = newCookieBox;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest R = base.GetWebRequest(address);
            if (R is HttpWebRequest)
            {
                HttpWebRequest WR = (HttpWebRequest) R;
                WR.CookieContainer = cc;
                if (lastPage != null)
                {
                    WR.Referer = lastPage;
                }
            }
            lastPage = address.ToString();
            return R;
        }

        #endregion
    }
}