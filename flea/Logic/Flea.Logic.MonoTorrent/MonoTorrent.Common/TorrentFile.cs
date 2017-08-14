using System;
using System.Text;

namespace MonoTorrent.Common
{
    /// <summary>
    ///     This is the base class for the files available to download from within a .torrent.
    ///     This should be inherited by both Client and Tracker "TorrentFile" classes
    /// </summary>
    public class TorrentFile : IEquatable<TorrentFile>
    {
        #region Internals

        private BitField bitfield;
        private byte[] ed2k;
        private int endPiece;
        private string fullPath;
        private long length;
        private byte[] md5;
        private string path;
        private Priority priority;
        private BitField selector;
        private byte[] sha1;
        private int startPiece;

        /// <summary>
        ///     The number of pieces which have been successfully downloaded which are from this file
        /// </summary>
        public BitField BitField
        {
            get { return bitfield; }
        }

        public long BytesDownloaded
        {
            get { return (long) (BitField.PercentComplete*Length/100.0); }
        }

        /// <summary>
        ///     The ED2K hash of the file
        /// </summary>
        public byte[] ED2K
        {
            get { return ed2k; }
        }

        /// <summary>
        ///     The index of the last piece of this file
        /// </summary>
        public int EndPieceIndex
        {
            get { return endPiece; }
        }

        public string FullPath
        {
            get { return fullPath; }
            internal set { fullPath = value; }
        }

        /// <summary>
        ///     The length of the file in bytes
        /// </summary>
        public long Length
        {
            get { return length; }
        }

        /// <summary>
        ///     The MD5 hash of the file
        /// </summary>
        public byte[] MD5
        {
            get { return md5; }
            internal set { md5 = value; }
        }

        /// <summary>
        ///     In the case of a single torrent file, this is the name of the file.
        ///     In the case of a multi-file torrent this is the relative path of the file
        ///     (including the filename) from the base directory
        /// </summary>
        public string Path
        {
            get { return path; }
        }

        /// <summary>
        ///     The priority of this torrent file
        /// </summary>
        public Priority Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        /// <summary>
        ///     The SHA1 hash of the file
        /// </summary>
        public byte[] SHA1
        {
            get { return sha1; }
        }

        /// <summary>
        ///     The index of the first piece of this file
        /// </summary>
        public int StartPieceIndex
        {
            get { return startPiece; }
        }

        #endregion

        #region Constructor

        public TorrentFile(string path, long length)
            : this(path, length, path)
        {
        }

        public TorrentFile(string path, long length, string fullPath)
            : this(path, length, fullPath, 0, 0)
        {
        }

        public TorrentFile(string path, long length, int startIndex, int endIndex)
            : this(path, length, path, startIndex, endIndex)
        {
        }

        public TorrentFile(string path, long length, string fullPath, int startIndex, int endIndex)
            : this(path, length, fullPath, startIndex, endIndex, null, null, null)
        {
        }

        public TorrentFile(string path, long length, string fullPath, int startIndex, int endIndex, byte[] md5,
            byte[] ed2k, byte[] sha1)
        {
            bitfield = new BitField(endIndex - startIndex + 1);
            this.ed2k = ed2k;
            endPiece = endIndex;
            this.fullPath = fullPath;
            this.length = length;
            this.md5 = md5;
            this.path = path;
            priority = Priority.Normal;
            this.sha1 = sha1;
            startPiece = startIndex;
        }

        #endregion

        #region Members

        public bool Equals(TorrentFile other)
        {
            return other == null ? false : path == other.path && length == other.length;
            ;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TorrentFile);
        }

        public override int GetHashCode()
        {
            return path.GetHashCode();
        }

        internal BitField GetSelector(int totalPieces)
        {
            if (selector != null)
                return selector;

            selector = new BitField(totalPieces);
            for (int i = StartPieceIndex; i <= EndPieceIndex; i++)
                selector[i] = true;
            return selector;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(32);
            sb.Append("File: ");
            sb.Append(path);
            sb.Append(" StartIndex: ");
            sb.Append(StartPieceIndex);
            sb.Append(" EndIndex: ");
            sb.Append(EndPieceIndex);
            return sb.ToString();
        }

        #endregion
    }
}