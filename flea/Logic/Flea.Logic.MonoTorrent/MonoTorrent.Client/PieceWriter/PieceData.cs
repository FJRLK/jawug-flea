using System.Collections.Generic;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    public partial class DiskManager
    {
        #region Types

        public class BufferedIO : ICacheable
        {
            #region Internals

            private int actualCount;
            internal byte[] buffer;
            private DiskIOCallback callback;
            private bool complete;
            private int count;
            private IList<TorrentFile> files;
            private TorrentManager manager;
            private long offset;
            private PeerId peerId;
            private int pieceLength;

            public int ActualCount
            {
                get { return actualCount; }
                set { actualCount = value; }
            }

            public int BlockIndex
            {
                get { return PieceOffset/Piece.BlockSize; }
            }

            public byte[] Buffer
            {
                get { return buffer; }
            }

            internal DiskIOCallback Callback
            {
                get { return callback; }
                set { callback = value; }
            }

            public bool Complete
            {
                get { return complete; }
                set { complete = value; }
            }

            public int Count
            {
                get { return count; }
                set { count = value; }
            }

            public IList<TorrentFile> Files
            {
                get { return files; }
            }

            internal PeerId Id
            {
                get { return peerId; }
                set { peerId = value; }
            }

            internal TorrentManager Manager
            {
                get { return manager; }
            }

            public long Offset
            {
                get { return offset; }
                set { offset = value; }
            }

            public int PieceIndex
            {
                get { return (int) (offset/pieceLength); }
            }

            public int PieceLength
            {
                get { return pieceLength; }
            }

            public int PieceOffset
            {
                get
                {
                    return (int) (offset%pieceLength);
                    ;
                }
            }

            #endregion

            #region Constructor

            public BufferedIO()
            {
            }

            #endregion

            #region Members

            public void Initialise()
            {
                Initialise(null, BufferManager.EmptyBuffer, 0, 0, 0, null);
            }

            public void Initialise(TorrentManager manager, byte[] buffer, long offset, int count, int pieceLength,
                IList<TorrentFile> files)
            {
                actualCount = 0;
                this.buffer = buffer;
                callback = null;
                complete = false;
                this.count = count;
                this.files = files;
                this.manager = manager;
                this.offset = offset;
                peerId = null;
                this.pieceLength = pieceLength;
            }

            public override string ToString()
            {
                return $"Piece: {PieceIndex} Block: {BlockIndex} Count: {count}";
            }

            #endregion
        }

        #endregion
    }
}