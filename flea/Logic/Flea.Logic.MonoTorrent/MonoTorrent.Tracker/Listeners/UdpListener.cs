﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Client.Messages.UdpTracker;

namespace MonoTorrent.Tracker.Listeners
{
    public class UdpListener : ListenerBase
    {
        #region Internals

        private Dictionary<IPAddress, long> connectionIDs;
        private long curConnectionID;
        private IPEndPoint endpoint;

        private UdpClient listener;
        //TODO system to clear old connectionID...
        public override bool Running
        {
            get { return listener != null; }
        }

        #endregion

        #region Constructor

        public UdpListener(int port)
            : this(new IPEndPoint(IPAddress.Any, port))
        {
        }

        public UdpListener(IPEndPoint endpoint)
        {
            this.endpoint = endpoint;
            connectionIDs = new Dictionary<IPAddress, long>();
        }

        #endregion

        #region Members

        /// <summary>
        ///     Starts listening for incoming connections
        /// </summary>
        public override void Start()
        {
            if (Running)
                return;

            //TODO test if it is better to use socket directly
            //Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            listener = new UdpClient(endpoint.Port);
            listener.BeginReceive(new AsyncCallback(ReceiveData), listener);
        }

        /// <summary>
        ///     Stops listening for incoming connections
        /// </summary>
        public override void Stop()
        {
            if (!Running)
                return;
            UdpClient listener = this.listener;
            this.listener = null;
            listener.Close();
        }

        private void ReceiveData(IAsyncResult ar)
        {
            try
            {
                UdpClient listener = (UdpClient) ar.AsyncState;
                byte[] data = listener.EndReceive(ar, ref endpoint);
                if (data.Length < 16)
                    return; //bad request

                UdpTrackerMessage request = UdpTrackerMessage.DecodeMessage(data, 0, data.Length, MessageType.Request);

                switch (request.Action)
                {
                    case 0:
                        ReceiveConnect((ConnectMessage) request);
                        break;
                    case 1:
                        ReceiveAnnounce((AnnounceMessage) request);
                        break;
                    case 2:
                        ReceiveScrape((ScrapeMessage) request);
                        break;
                    case 3:
                        ReceiveError((ErrorMessage) request);
                        break;
                    default:
                        throw new ProtocolException($"Invalid udp message received: {request.Action}");
                }
            }
            catch (Exception e)
            {
                Logger.Log(null, e.ToString());
            }
            finally
            {
                if (Running)
                    listener.BeginReceive(new AsyncCallback(ReceiveData), listener);
            }
        }

        protected virtual void ReceiveConnect(ConnectMessage connectMessage)
        {
            UdpTrackerMessage m = new ConnectResponseMessage(connectMessage.TransactionId, CreateConnectionID());
            byte[] data = m.Encode();
            listener.Send(data, data.Length, endpoint);
        }

        //TODO is endpoint.Address.Address enough and do we really need this complex system for connection ID
        //advantage: this system know if we have ever connect before announce scrape request...
        private long CreateConnectionID()
        {
            curConnectionID++;
            if (!connectionIDs.ContainsKey(endpoint.Address))
                connectionIDs.Add(endpoint.Address, curConnectionID);
            return curConnectionID;
        }

        //QUICKHACK: format bencoded val and get it back wereas must refactor tracker system to have more generic object...
        protected virtual void ReceiveAnnounce(AnnounceMessage announceMessage)
        {
            UdpTrackerMessage m;
            BEncodedDictionary dict = Handle(getCollection(announceMessage), endpoint.Address, false);
            if (dict.ContainsKey(RequestParameters.FailureKey))
            {
                m = new ErrorMessage(announceMessage.TransactionId, dict[RequestParameters.FailureKey].ToString());
            }
            else
            {
                TimeSpan interval = TimeSpan.Zero;
                int leechers = 0;
                int seeders = 0;
                List<Client.Peer> peers = new List<Client.Peer>();
                foreach (KeyValuePair<BEncodedString, BEncodedValue> keypair in dict)
                {
                    switch (keypair.Key.Text)
                    {
                        case ("complete"):
                            seeders = Convert.ToInt32(keypair.Value.ToString()); //same as seeder?
                            break;

                        case ("incomplete"):
                            leechers = Convert.ToInt32(keypair.Value.ToString()); //same as leecher?
                            break;

                        case ("interval"):
                            interval = TimeSpan.FromSeconds(int.Parse(keypair.Value.ToString()));
                            break;

                        case ("peers"):
                            if (keypair.Value is BEncodedList) // Non-compact response
                                peers.AddRange(Client.Peer.Decode((BEncodedList) keypair.Value));
                            else if (keypair.Value is BEncodedString) // Compact response
                                peers.AddRange(Client.Peer.Decode((BEncodedString) keypair.Value));
                            break;

                        default:
                            break;
                    }
                }
                m = new AnnounceResponseMessage(announceMessage.TransactionId, interval, leechers, seeders, peers);
            }
            byte[] data = m.Encode();
            listener.Send(data, data.Length, endpoint);
        }

        private NameValueCollection getCollection(AnnounceMessage announceMessage)
        {
            NameValueCollection res = new NameValueCollection();
            res.Add("info_hash", announceMessage.Infohash.UrlEncode());
            res.Add("peer_id", announceMessage.PeerId);
            res.Add("port", announceMessage.Port.ToString());
            res.Add("uploaded", announceMessage.Uploaded.ToString());
            res.Add("downloaded", announceMessage.Downloaded.ToString());
            res.Add("left", announceMessage.Left.ToString());
            res.Add("compact", "1"); //hardcode
            res.Add("numwant", announceMessage.NumWanted.ToString());
            res.Add("ip", announceMessage.Ip.ToString());
            res.Add("key", announceMessage.Key.ToString());
            res.Add("event", announceMessage.TorrentEvent.ToString().ToLower());
            return res;
        }

        protected virtual void ReceiveScrape(ScrapeMessage scrapeMessage)
        {
            BEncodedDictionary val = Handle(getCollection(scrapeMessage), endpoint.Address, true);

            UdpTrackerMessage m;
            byte[] data;
            if (val.ContainsKey(RequestParameters.FailureKey))
            {
                m = new ErrorMessage(scrapeMessage.TransactionId, val[RequestParameters.FailureKey].ToString());
            }
            else
            {
                List<ScrapeDetails> scrapes = new List<ScrapeDetails>();

                foreach (KeyValuePair<BEncodedString, BEncodedValue> keypair in val)
                {
                    BEncodedDictionary dict = (BEncodedDictionary) keypair.Value;
                    int seeds = 0;
                    int leeches = 0;
                    int complete = 0;
                    foreach (KeyValuePair<BEncodedString, BEncodedValue> keypair2 in dict)
                    {
                        switch (keypair2.Key.Text)
                        {
                            case "complete": //The current number of connected seeds
                                seeds = Convert.ToInt32(keypair2.Value.ToString());
                                break;
                            case "downloaded": //The total number of completed downloads
                                complete = Convert.ToInt32(keypair2.Value.ToString());
                                break;
                            case "incomplete":
                                leeches = Convert.ToInt32(keypair2.Value.ToString());
                                break;
                        }
                    }
                    ScrapeDetails sd = new ScrapeDetails(seeds, leeches, complete);
                    scrapes.Add(sd);
                    if (scrapes.Count == 74) //protocole do not support to send more than 74 scrape at once...
                    {
                        m = new ScrapeResponseMessage(scrapeMessage.TransactionId, scrapes);
                        data = m.Encode();
                        listener.Send(data, data.Length, endpoint);
                        scrapes.Clear();
                    }
                }
                m = new ScrapeResponseMessage(scrapeMessage.TransactionId, scrapes);
            }
            data = m.Encode();
            listener.Send(data, data.Length, endpoint);
        }

        private NameValueCollection getCollection(ScrapeMessage scrapeMessage)
        {
            NameValueCollection res = new NameValueCollection();
            if (scrapeMessage.InfoHashes.Count == 0)
                return res; //no infohash????
            //TODO more than one infohash : paid attention to order in response!!!
            InfoHash hash = new InfoHash(scrapeMessage.InfoHashes[0]);
            res.Add("info_hash", hash.UrlEncode());
            return res;
        }

        protected virtual void ReceiveError(ErrorMessage errorMessage)
        {
            throw new ProtocolException($"ErrorMessage from :{endpoint.Address}");
        }

        #endregion
    }
}