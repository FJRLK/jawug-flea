using System;
using System.Net;
using MonoTorrent.BEncoding;

namespace MonoTorrent.Tracker
{
    /// <summary>This class holds informations about Peers downloading Files</summary>
    public class Peer : IEquatable<Peer>
    {
        #region Internals

        private IPEndPoint clientAddress;
        private object dictionaryKey;
        private long downloaded;
        private int downloadSpeed;
        private DateTime lastAnnounceTime;
        private long left;
        private string peerId;
        private long uploaded;
        private int uploadSpeed;


        /// <summary>
        ///     The IPEndpoint at which the client is listening for connections at
        /// </summary>
        public IPEndPoint ClientAddress
        {
            get { return clientAddress; }
        }

        /// <summary>
        ///     A byte[] containing the peer's IPEndpoint in compact form
        /// </summary>
        internal byte[] CompactEntry
        {
            get { return GenerateCompactPeersEntry(); }
        }

        internal object DictionaryKey
        {
            get { return dictionaryKey; }
        }

        /// <summary>
        ///     The amount of data (in bytes) which the peer has downloaded this session
        /// </summary>
        public long Downloaded
        {
            get { return downloaded; }
        }

        /// <summary>
        ///     The estimated download speed of the peer in bytes/second
        /// </summary>
        public int DownloadSpeed
        {
            get { return downloadSpeed; }
        }

        /// <summary>
        ///     True if the peer has completed the torrent
        /// </summary>
        public bool HasCompleted
        {
            get { return Remaining == 0; }
        }

        /// <summary>
        ///     The time when the peer last announced at
        /// </summary>
        public DateTime LastAnnounceTime
        {
            get { return lastAnnounceTime; }
        }

        /// <summary>The peer entry in non compact format.</summary>
        internal BEncodedDictionary NonCompactEntry
        {
            get { return GeneratePeersEntry(); }
        }

        /// <summary>
        ///     The Id of the client software
        /// </summary>
        public string PeerId
        {
            get { return peerId; }
        }

        /// <summary>
        ///     The amount of data (in bytes) which the peer has to download to complete the torrent
        /// </summary>
        public long Remaining
        {
            get { return left; }
        }

        /// <summary>
        ///     The amount of data the peer has uploaded this session
        /// </summary>
        public long Uploaded
        {
            get { return uploaded; }
        }

        /// <summary>
        ///     The estimated upload speed of the peer in bytes/second
        /// </summary>
        public int UploadSpeed
        {
            get { return uploadSpeed; }
        }

        #endregion

        #region Constructor

        internal Peer(AnnounceParameters par, object dictionaryKey)
        {
            this.dictionaryKey = dictionaryKey;
            Update(par);
        }

        #endregion

        #region Members

        public bool Equals(Peer other)
        {
            if (other == null)
                return false;
            return dictionaryKey.Equals(other.dictionaryKey);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Peer);
        }

        public override int GetHashCode()
        {
            return dictionaryKey.GetHashCode();
        }

        internal void Update(AnnounceParameters parameters)
        {
            DateTime now = DateTime.Now;
            double elapsedTime = (now - lastAnnounceTime).TotalSeconds;
            if (elapsedTime < 1)
                elapsedTime = 1;

            clientAddress = parameters.ClientAddress;
            downloadSpeed = (int) ((parameters.Downloaded - downloaded)/elapsedTime);
            uploadSpeed = (int) ((parameters.Uploaded - uploaded)/elapsedTime);
            downloaded = parameters.Downloaded;
            uploaded = parameters.Uploaded;
            left = parameters.Left;
            peerId = parameters.PeerId;
            lastAnnounceTime = now;
        }


        private BEncodedDictionary GeneratePeersEntry()
        {
            BEncodedString encPeerId = new BEncodedString(PeerId);
            BEncodedString encAddress = new BEncodedString(ClientAddress.Address.ToString());
            BEncodedNumber encPort = new BEncodedNumber(ClientAddress.Port);

            BEncodedDictionary dictionary = new BEncodedDictionary();
            dictionary.Add(Tracker.PeerIdKey, encPeerId);
            dictionary.Add(Tracker.Ip, encAddress);
            dictionary.Add(Tracker.Port, encPort);
            return dictionary;
        }

        private byte[] GenerateCompactPeersEntry()
        {
            byte[] port = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) ClientAddress.Port));
            byte[] addr = ClientAddress.Address.GetAddressBytes();
            byte[] entry = new byte[addr.Length + port.Length];

            Array.Copy(addr, entry, addr.Length);
            Array.Copy(port, 0, entry, addr.Length, port.Length);
            return entry;
        }

        #endregion
    }
}