using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using CSTG.CodeAnalyzer.Model;
using System.Xml;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;

namespace CSTG.CodeAnalyzer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var untouchedDir = new DirectoryInfo(@"C:\projects\state-of-indiana\untouched");
            var workingDirectories = new DirectoryInfo[] {
                new DirectoryInfo(@"C:\git-repos\IN_AdminApp_DOR"),
                new DirectoryInfo(@"C:\git-repos\IN_AdminApp_DWD"),
                new DirectoryInfo(@"C:\git-repos\IN_AdminApp_IPLA"),
                new DirectoryInfo(@"C:\git-repos\IN_AdminApp_SOS"),
                new DirectoryInfo(@"C:\git-repos\IN_BSD"),
                new DirectoryInfo(@"C:\git-repos\IN_INBIZ"),
                new DirectoryInfo(@"C:\git-repos\IN_WebService_BSDService")
            };

            // files to look at in the future - .sln (build/deploy profiles) .sql (ddl files) *.cs app.config web.config packages.config
            HarvestedData data = null;

            if (File.Exists("ProjectsAndSolutions.json"))
            {
                try
                {
                    data = JsonConvert.DeserializeObject<HarvestedData>(File.ReadAllText("ProjectsAndSolutions.json"));
                } catch { }
            }

            if (data == null)
            {
                // deal with project files!
                var projectFiles = new List<ProjectFile>();
                var matchingFiles = Utility.RecursiveFileSearch(untouchedDir, extensions: new string[] { ".csproj" }, skipDirs: new string[] { "bin", "obj", "packages", "Libraries", ".git" });
                foreach (var projFile in matchingFiles)
                {
                    var projectFile = ProjectFileUtil.Read(projFile);

                    foreach (var nugetPackage in projectFile.NugetPackages)
                    {
                        NugetHelper.LookUpPackage(nugetPackage).Wait();
                    }
                    projectFiles.Add(projectFile);
                }

                // deal with project files!
                var solutionFiles = new List<SolutionFile>();
                matchingFiles = Utility.RecursiveFileSearch(untouchedDir, extensions: new string[] { ".sln" }, skipDirs: new string[] { "bin", "obj", "packages", "Libraries", ".git" });
                foreach (var solFile in matchingFiles)
                {
                    var solutionfile = SolutionFileUtil.Read(solFile);

                    foreach (var pref in solutionfile.Projects)
                    {
                        var match = projectFiles.FirstOrDefault(xx => xx.ProjectId == pref.ProjectId) ?? projectFiles.FirstOrDefault(xx => xx.File == pref.ProjectFile);
                        if (match != null)
                        {
                            if (match.ProjectId != pref.ProjectId)
                            {
                                Debugger.Break();
                            }
                            if (!match.SolutionFiles.Contains(solutionfile.File)) match.SolutionFiles.Add(solutionfile.File);
                            if (pref.ProjectFile == null) pref.ProjectFile = match.File;
                        }
                        else
                        {

                        }
                    }
                    solutionFiles.Add(solutionfile);

                }

                data = new HarvestedData
                {
                    Solutions = solutionFiles,
                    Projects = projectFiles
                };

                File.WriteAllText("ProjectsAndSolutions.json", JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
            }

            var allDllFiles = Utility.RecursiveFileSearch(untouchedDir, extensions: new string[] { ".dll" }, skipDirs: new string[] { ".git" });

            foreach (var p in data.Projects)
            {
                var projectFolderDllFiles = allDllFiles.Where(f => f.FullName.StartsWith(p.File.Directory.FullName)).ToList();

                foreach (var assRef in p.AssemblyReferences.Where(x=>!string.IsNullOrWhiteSpace(x.HintPath)))
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
                        if (matchingAssembly == null) { 
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
                    }
                }
            }
            File.WriteAllText("ProjectsAndSolutions.json", JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
        }

    }
}