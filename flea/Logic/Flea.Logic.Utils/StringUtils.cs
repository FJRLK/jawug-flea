using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flea.Logic.Utils
{
    public static class StringUtils
    {
        /// <summary>
        ///     Rests the of string.
        /// </summary>
        /// <param name="sourceString">The source string.</param>
        /// <param name="startingWord">The starting word.</param>
        /// <returns></returns>
        public static string RestOfString(this string sourceString, int startingWord)
        {
            string[] wordList = sourceString.Split(' ');
            string search = "";
            for (int i = startingWord; i < wordList.Length; i++)
            {
                search += sourceString.Split(' ')[i] + " ";
            }
            return search;
        }
    }
}
