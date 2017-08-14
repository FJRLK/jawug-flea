using MonoTorrent.BEncoding;
using MonoTorrent.Common;

namespace MonoTorrent
{
    public class TorrentEditor : EditableTorrent
    {
        #region Internals

        public new bool CanEditSecureMetadata
        {
            get { return base.CanEditSecureMetadata; }
            set { base.CanEditSecureMetadata = value; }
        }

        #endregion

        #region Constructor

        public TorrentEditor(Torrent torrent)
            : base(torrent)
        {
        }

        public TorrentEditor(BEncodedDictionary metadata)
            : base(metadata)
        {
        }

        #endregion
    }
}