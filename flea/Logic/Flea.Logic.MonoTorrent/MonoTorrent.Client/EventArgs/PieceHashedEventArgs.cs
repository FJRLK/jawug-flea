namespace MonoTorrent.Client
{
    /// <summary>
    ///     Provides the data needed to handle a PieceHashed event
    /// </summary>
    public class PieceHashedEventArgs : TorrentEventArgs
    {
        #region Internals

        private bool hashPassed;
        private int pieceIndex;


        /// <summary>
        ///     The value of whether the piece passed or failed the hash check
        /// </summary>
        public bool HashPassed
        {
            get { return hashPassed; }
        }

        /// <summary>
        ///     The index of the piece that was just hashed
        /// </summary>
        public int PieceIndex
        {
            get { return pieceIndex; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Creates a new PieceHashedEventArgs
        /// </summary>
        /// <param name="pieceIndex">The index of the piece that was hashed</param>
        /// <param name="hashPassed">True if the piece passed the hashcheck, false otherwise</param>
        public PieceHashedEventArgs(TorrentManager manager, int pieceIndex, bool hashPassed)
            : base(manager)
        {
            this.pieceIndex = pieceIndex;
            this.hashPassed = hashPassed;
        }

        #endregion
    }
}