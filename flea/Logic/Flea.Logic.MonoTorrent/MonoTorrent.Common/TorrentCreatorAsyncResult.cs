using System;
using MonoTorrent.BEncoding;

namespace MonoTorrent.Common
{
    class TorrentCreatorAsyncResult : AsyncResult
    {
        #region Internals

        bool aborted;
        BEncodedDictionary dictionary;

        public bool Aborted
        {
            get { return aborted; }
            set { aborted = value; }
        }

        internal BEncodedDictionary Dictionary
        {
            get { return dictionary; }
            set { dictionary = value; }
        }

        #endregion

        #region Constructor

        public TorrentCreatorAsyncResult(AsyncCallback callback, object asyncState)
            : base(callback, asyncState)
        {
        }

        #endregion
    }
}