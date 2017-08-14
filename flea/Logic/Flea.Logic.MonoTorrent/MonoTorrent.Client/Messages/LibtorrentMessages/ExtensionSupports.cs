using System.Collections.Generic;
using MonoTorrent.Client.Messages.Libtorrent;

namespace MonoTorrent.Client
{
    public class ExtensionSupports : List<ExtensionSupport>
    {
        #region Constructor

        public ExtensionSupports()
        {
        }

        public ExtensionSupports(IEnumerable<ExtensionSupport> collection)
            : base(collection)
        {
        }

        #endregion

        #region Members

        public bool Supports(string name)
        {
            for (int i = 0; i < Count; i++)
                if (this[i].Name == name)
                    return true;
            return false;
        }

        internal byte MessageId(ExtensionSupport support)
        {
            for (int i = 0; i < Count; i++)
                if (this[i].Name == support.Name)
                    return this[i].MessageId;

            throw new MessageException($"{support.Name} is not supported by this peer");
        }

        #endregion
    }
}