namespace MonoTorrent.Client
{
    interface IUnchoker
    {
        #region Members

        void Choke(PeerId id);
        void UnchokeReview();
        void Unchoke(PeerId id);

        #endregion
    }
}