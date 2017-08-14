using System.Collections.Generic;

namespace MonoTorrent.Common
{
    public interface ITorrentFileSource
    {
        #region Internals

        IEnumerable<FileMapping> Files { get; }
        string TorrentName { get; }

        #endregion
    }
}