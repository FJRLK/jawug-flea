using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TorrentMakerUnit
{
    public class FileSystemEnumerable : IEnumerable<FileSystemInfo>
    {
        #region Internals

        private readonly SearchOption _Option;
        private readonly IList<string> _Patterns;
        private readonly DirectoryInfo _Root;

        #endregion

        #region Constructor

        public FileSystemEnumerable(DirectoryInfo root, string pattern, SearchOption option)
        {
            _Root = root;
            _Patterns = new List<string> {pattern};
            _Option = option;
        }

        public FileSystemEnumerable(DirectoryInfo root, IList<string> patterns, SearchOption option)
        {
            _Root = root;
            _Patterns = patterns;
            _Option = option;
        }

        #endregion

        #region Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<FileSystemInfo> GetEnumerator()
        {
            if (_Root == null || !_Root.Exists) yield break;

            IEnumerable<FileSystemInfo> matches = new List<FileSystemInfo>();
            try
            {
                foreach (string pattern in _Patterns)
                {
                    matches = matches.Concat(_Root.EnumerateDirectories(pattern, SearchOption.TopDirectoryOnly))
                        .Concat(_Root.EnumerateFiles(pattern, SearchOption.TopDirectoryOnly));
                }
            }
            catch (UnauthorizedAccessException)
            {
                yield break;
            }
            catch (PathTooLongException ptle)
            {
                yield break;
            }

            foreach (FileSystemInfo file in matches)
            {
                yield return file;
            }

            if (_Option == SearchOption.AllDirectories)
            {
                foreach (DirectoryInfo dir in _Root.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    FileSystemEnumerable fileSystemInfos = new FileSystemEnumerable(dir, _Patterns, _Option);
                    foreach (FileSystemInfo match in fileSystemInfos)
                    {
                        yield return match;
                    }
                }
            }
        }

        #endregion
    }
}