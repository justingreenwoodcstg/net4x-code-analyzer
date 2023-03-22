using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSTG.CodeAnalyzer.Model;

namespace CSTG.CodeAnalyzer
{
    public class Utility
    {
        public static List<FileInfo> RecursiveFileSearch(DirectoryInfo parent, string[] extensions, string[] skipDirs = null)
        {
            var matchingFiles = new List<FileInfo>();
            RecursiveFileSearch(parent, matchingFiles, extensions, skipDirs);
            return matchingFiles;
        }

        public static void RecursiveFileSearch(DirectoryInfo parent, List<FileInfo> matchingFiles, string[] extensions, string[] skipDirs = null)
        {
            foreach (var file in parent.GetFiles())
            {
                if (extensions.Any(x => file.Extension.Equals(x, StringComparison.InvariantCultureIgnoreCase)))
                {
                    // this is a match
                    matchingFiles.Add(file);
                }
            }
            foreach (var dir in parent.GetDirectories())
            {
                if (skipDirs != null && skipDirs.Any(x => dir.Name.Equals(x, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue;
                }
                else
                {
                    RecursiveFileSearch(dir, matchingFiles, extensions, skipDirs);
                }
            }
        }
    }
}
