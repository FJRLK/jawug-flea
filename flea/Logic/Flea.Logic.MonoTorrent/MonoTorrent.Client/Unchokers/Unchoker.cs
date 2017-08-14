using System;
using MonoTorrent.Client.Messages.Standard;

namespace MonoTorrent.Client
{
    abstract class Unchoker : IUnchoker
    {
        #region Members

        public virtual void Choke(PeerId id)
        {
            id.AmChoking = true;
            id.TorrentManager.UploadingTo--;
            id.Enqueue(new ChokeMessage());
        }

        public virtual void Unchoke(PeerId id)
        {
            id.AmChoking = false;
            id.TorrentManager.UploadingTo++;
            id.Enqueue(new UnchokeMessage());
            id.LastUnchoked = DateTime.Now;
        }

        public abstract void UnchokeReview();

        #endregion
    }
}