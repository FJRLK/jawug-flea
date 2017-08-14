using System;
using System.Collections.Generic;

namespace MonoTorrent.Client.Messages
{
    public class MessageBundle : PeerMessage
    {
        #region Internals

        private List<PeerMessage> messages;

        public override int ByteLength
        {
            get
            {
                int total = 0;
                for (int i = 0; i < messages.Count; i++)
                    total += messages[i].ByteLength;
                return total;
            }
        }

        public List<PeerMessage> Messages
        {
            get { return messages; }
        }

        #endregion

        #region Constructor

        public MessageBundle()
        {
            messages = new List<PeerMessage>();
        }

        public MessageBundle(PeerMessage message)
            : this()
        {
            messages.Add(message);
        }

        #endregion

        #region Members

        public override void Decode(byte[] buffer, int offset, int length)
        {
            throw new InvalidOperationException();
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            for (int i = 0; i < messages.Count; i++)
                written += messages[i].Encode(buffer, written);

            return CheckWritten(written - offset);
        }

        #endregion
    }
}