using System;
using System.IO;
using MonoTorrent.BEncoding;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    public class FastResume
    {
        #region Static

        private static readonly BEncodedString BitfieldKey = (BEncodedString) "bitfield";
        private static readonly BEncodedString BitfieldLengthKey = (BEncodedString) "bitfield_length";
        private static readonly BEncodedString InfoHashKey = (BEncodedString) "infohash";
        private static readonly BEncodedString VersionKey = (BEncodedString) "version";

        #endregion

        #region Internals

        private BitField bitfield;
        private InfoHash infoHash;

        public BitField Bitfield
        {
            get { return bitfield; }
        }

        public InfoHash Infohash
        {
            get { return infoHash; }
        }

        #endregion

        #region Constructor

        public FastResume()
        {
        }

        public FastResume(InfoHash infoHash, BitField bitfield)
        {
            if (infoHash == null)
                throw new ArgumentNullException("infoHash");
            if (bitfield == null)
                throw new ArgumentNullException("bitfield");

            this.infoHash = infoHash;
            this.bitfield = bitfield;
        }

        public FastResume(BEncodedDictionary dict)
        {
            CheckContent(dict, VersionKey, (BEncodedNumber) 1);
            CheckContent(dict, InfoHashKey);
            CheckContent(dict, BitfieldKey);
            CheckContent(dict, BitfieldLengthKey);

            infoHash = new InfoHash(((BEncodedString) dict[InfoHashKey]).TextBytes);
            bitfield = new BitField((int) ((BEncodedNumber) dict[BitfieldLengthKey]).Number);
            byte[] data = ((BEncodedString) dict[BitfieldKey]).TextBytes;
            bitfield.FromArray(data, 0, data.Length);
        }

        #endregion

        #region Members

        private void CheckContent(BEncodedDictionary dict, BEncodedString key, BEncodedNumber value)
        {
            CheckContent(dict, key);
            if (!dict[key].Equals(value))
                throw new TorrentException(
                    $"Invalid FastResume data. The value of '{key}' was '{dict[key]}' instead of '{value}'");
        }

        private void CheckContent(BEncodedDictionary dict, BEncodedString key)
        {
            if (!dict.ContainsKey(key))
                throw new TorrentException($"Invalid FastResume data. Key '{key}' was not present");
        }

        public BEncodedDictionary Encode()
        {
            BEncodedDictionary dict = new BEncodedDictionary();
            dict.Add(VersionKey, (BEncodedNumber) 1);
            dict.Add(InfoHashKey, new BEncodedString(infoHash.Hash));
            dict.Add(BitfieldKey, new BEncodedString(bitfield.ToByteArray()));
            dict.Add(BitfieldLengthKey, (BEncodedNumber) bitfield.Length);
            return dict;
        }

        public void Encode(Stream s)
        {
            byte[] data = Encode().Encode();
            s.Write(data, 0, data.Length);
        }

        #endregion
    }
}