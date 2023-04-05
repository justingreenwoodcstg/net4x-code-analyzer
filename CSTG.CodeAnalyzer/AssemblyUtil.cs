using CSTG.CodeAnalyzer.Model;
using Newtonsoft.Json;
using NuGet.ContentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CSTG.CodeAnalyzer
{
    public class AssemblyUtil
    {
        public static void TypeCheck(HarvestedData data)
        {
            var startupProjects = data.Projects.Where(x => x.IsWebProject).ToList();
            foreach (var startup in startupProjects)
            {
                var asses = new List<Assembly>();
                var binFolder = Path.Combine(startup.File.Directory.FullName, $".\\bin\\debug\\");
                binFolder = Directory.Exists(binFolder) ? binFolder : Path.Combine(startup.File.Directory.FullName, $".\\bin\\");

                var domain = AppDomain.CreateDomain("test", null, binFolder, binFolder, true);
                foreach (var projRef in startup.ProjectReferences)
                {
                    var proj = data.Projects.FirstOrDefault(x => x.ProjectId == projRef.ProjectId);
                    if (proj != null)
                    {
                        var assemblyName = proj.AssemblyName;
                        var assemblyFile = new FileInfo(Path.Combine(binFolder, $"{assemblyName}.dll"));
                        if (assemblyFile.Exists)
                        {
                            System.Reflection.Assembly matchingAssembly = null;
                            try
                            {
                                matchingAssembly = System.Reflection.Assembly.LoadFile(assemblyFile.FullName);
                                //matchingAssembly = domain.Load(matchingAssembly.GetName());
                                matchingAssembly.GetReferencedAssemblies();
                                asses.Add(matchingAssembly);
                            }
                            catch { }
                        }
                    }
                    //if (matchingAssembly != null)
                    //{
                    //    Console.WriteLine($"{matchingAssembly.GetName().Name} ASSEMBLY FOUND!!!");
                    //    var isError = false;
                    //    try
                    //    {
                    //        foreach (var type in matchingAssembly.DefinedTypes)
                    //        {
                    //            Console.WriteLine($" --->DEF {type.Name}");
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        isError = true;
                    //        Console.WriteLine($"--X-X-X--DEF ERROR - {ex}");
                    //    }
                    //    if (isError)
                    //    {
                    //        try
                    //        {
                    //            matchingAssembly = System.Reflection.Assembly.LoadFile(assemblyFile.FullName);
                    //            foreach (var type in matchingAssembly.ExportedTypes)
                    //            {
                    //                Console.WriteLine($" --->EXP {type.Name}");
                    //            }
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            Console.WriteLine($"--X-X-X--EXP ERROR - {ex}");
                    //        }
                    //    }

                    //}
                }


                foreach (var matchingAssembly in asses)
                {
                    Console.WriteLine($"START: {matchingAssembly.GetName().Name}");
                    try
                    {
                        var types = matchingAssembly.GetTypes();
                        try
                        {
                            foreach (var type in types)
                            {
                                Console.WriteLine($" ---> {type.Name}");
                            }
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            //isError = true;
                            Console.WriteLine($"--X-X-X--1 {ex}");
                            foreach (Exception exSub in ex.LoaderExceptions)
                            {
                                Console.WriteLine(exSub.Message);
                            }

                        }
                    }
                    catch (ReflectionTypeLoadException ex1)
                    {
                        Console.WriteLine($"--X-X-X--2 {ex1}");
                        foreach (Exception exSub in ex1.LoaderExceptions)
                        {
                            Console.WriteLine(exSub.Message);
                        }
                    }
                    Console.WriteLine($"END: {matchingAssembly.GetName().Name}");
                    System.Console.ReadKey();
                }
            }
        }
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
                            if (dir != null)
                            {
                                var packageDirName = dir.Name;
                                var matchingPackage = p.NugetPackages.Where(x => packageDirName.StartsWith(x.Id, StringComparison.InvariantCultureIgnoreCase)).OrderByDescending(x=>x.Id.Length).FirstOrDefault();
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
