using System.Collections.Generic;
using System.Linq;

namespace Flea.Logic.IrcConnectionUnit
{
    public static class StringListHelper
    {
        #region Members

        public static List<IrcMessage> ToPublicIrcMessageList(this List<string> messagesList)
        {
            return messagesList.Select(x => new IrcMessage(x)).ToList();
        }

        #endregion
    }
}