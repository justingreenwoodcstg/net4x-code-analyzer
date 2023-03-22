using CSTG.CodeAnalyzer.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSTG.CodeAnalyzer
{
    public class AssemblyUtil
    {
        public static void EnrichHarvestedDataWithAssemblies(DirectoryInfo rootDir, HarvestedData data)
        {
            var allDllFiles = Utility.RecursiveFileSearch(rootDir, extensions: new string[] { ".dll" }, skipDirs: new string[] { ".git" });

            foreach (var p in data.Projects)
            {
                var projectFolderDllFiles = allDllFiles.Where(f => f.FullName.StartsWith(p.File.Directory.FullName)).ToList();

                foreach (var assRef in p.AssemblyReferences.Where(x => !string.IsNullOrWhiteSpace(x.HintPath)))
                {
                    bool foundUsingHint = true;
                    var fi = new FileInfo(Path.Combine(p.File.Directory.FullName, assRef.HintPath));
                    System.Reflection.Assembly matchingAssembly = null;
                    if (fi.Exists) try { matchingAssembly = System.Reflection.Assembly.LoadFile(fi.FullName); } catch { }

                    if (!fi.Exists)
                    {
                        foundUsingHint = false;

                        var dllName = assRef.Name.EndsWith(".dll") ? assRef.Name : $"{assRef.Name}.dll";

                        //TODO: collect all of the matches, and then try to get the one with the closest version?
                        var assemblies = projectFolderDllFiles
                            .Where(x => x.Name.Equals(dllName, StringComparison.InvariantCultureIgnoreCase))
                            .Select(x =>
                            {
                                try { return System.Reflection.Assembly.LoadFile(x.FullName); } catch { return null; }
                            }).Where(x => x != null).ToList();
                        matchingAssembly = assemblies.Where(x => assRef.Version == null || x.GetName().Version == assRef.Version).FirstOrDefault();
                        var lastOptionMatch = assemblies.FirstOrDefault();
                        if (matchingAssembly == null)
                        {
                            assemblies = allDllFiles
                                .Where(x => x.Name.Equals(dllName, StringComparison.InvariantCultureIgnoreCase))
                                .Select(x =>
                                {
                                    try { return System.Reflection.Assembly.LoadFile(x.FullName); } catch { return null; }
                                }).Where(x => x != null).ToList();
                            matchingAssembly = assemblies.Where(x => assRef.Version == null || x.GetName().Version == assRef.Version).FirstOrDefault();
                            if (lastOptionMatch == null) lastOptionMatch = assemblies.FirstOrDefault();
                        }
                        matchingAssembly = matchingAssembly ?? lastOptionMatch;
                        if (matchingAssembly == null)
                        {
                            fi = projectFolderDllFiles.FirstOrDefault(x => x.Name.Equals(dllName, StringComparison.InvariantCultureIgnoreCase))
                                ?? allDllFiles.FirstOrDefault(x => x.Name.Equals(dllName, StringComparison.InvariantCultureIgnoreCase));
                        }
                        else
                        {
                            fi = new FileInfo(matchingAssembly.Location);
                        }
                    }

                    if (fi?.Exists ?? false)
                    {
                        assRef.FileLocation = new AssemblyReferenceFile
                        {
                            File = fi,
                            Version = matchingAssembly?.GetName().Version,
                            HintIsValid = foundUsingHint
                        };

                        if (assRef.FileLocation.InPackages)
                        {
                            // try to find the package!
                            var dir = assRef.FileLocation.File.Directory;
                            while (dir?.Parent != null && dir.Parent.Name != "packages") { dir = dir.Parent; }
                            if (dir == null)
                            {
                                
                            } 
                            else
                            {
                                var packageName = dir.Name;
                                var matchingPackage = p.NugetPackages.Where(x => packageName.StartsWith(x.Id)).FirstOrDefault();
                                if (matchingPackage != null)
                                {
                                    assRef.PackageId = matchingPackage.Id;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
