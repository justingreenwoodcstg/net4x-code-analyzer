using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CSTG.CodeAnalyzer.Model;

namespace CSTG.CodeAnalyzer
{
    public class SolutionFileUtil
    {
        public static SolutionFile Read(FileInfo solFileInfo)
        {
            var solutionFile = new SolutionFile
            {
                File = solFileInfo
            };
            Console.WriteLine(solFileInfo.Name);
            var lines = File.ReadAllLines(solFileInfo.FullName);
            foreach (var line in lines.Where(l => l.StartsWith("Project(\"")))
            {
                var tokens = line.Split('=');
                if (tokens.Length == 2)
                {
                    tokens = tokens[1].Split(',').Select(x => x.Trim(' ', '"')).ToArray();
                    if (tokens.Length >= 3 && tokens[1].EndsWith("proj", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var pref = new ProjectFileReference
                        {
                            Name = tokens[0],
                            RelativePath = tokens[1],
                            ProjectId = new Guid(tokens[2]),
                        };
                        solutionFile.Projects.Add(pref);

                        var f = new FileInfo(Path.Combine(solFileInfo.Directory.FullName, tokens[1]));
                        if (f.Exists) pref.ProjectFile = f;
                    }
                }
            }

            return solutionFile;
        }
    }
}
