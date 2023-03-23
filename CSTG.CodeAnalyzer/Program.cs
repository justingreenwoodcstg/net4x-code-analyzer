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
using CSTG.CodeAnalyzer.Reports;

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

                AssemblyUtil.EnrichHarvestedDataWithAssemblies(untouchedDir, data);


                foreach (var project in data.Projects)
                {
                    var configFiles = Utility.RecursiveFileSearch(project.File.Directory, extensions: new string[] { ".config" }, skipDirs: new string[] { "bin", "obj", "packages", "Libraries", ".git" });
                    foreach (var confFile in configFiles.Where(c => c.Name.Equals("app.config", StringComparison.InvariantCultureIgnoreCase) || c.Name.Equals("web.config", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        project.ConfigFiles.Add(ConfigFileUtil.Read(confFile));
                    }
                }

                File.WriteAllText("ProjectsAndSolutions.json", JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
            }

            foreach (var project in data.Projects)
            {
                Console.WriteLine("=========================================================================");
                Console.WriteLine("== " + project.AssemblyName);
                Console.WriteLine("=========================================================================");
                foreach (var ar in project.AssemblyReferences.Where(x => !string.IsNullOrWhiteSpace(x.HintPath) && x.FileLocation == null))
                {
                    Console.WriteLine("MISSING:" + ar.Name);
                }
                Console.WriteLine("-------------------------------------------------------------------------");
                foreach (var ar in project.AssemblyReferences.Where(x => !string.IsNullOrWhiteSpace(x.HintPath) && x.FileLocation != null 
                    && (x.Name.ToLower().StartsWith("gcr") || x.Name.ToLower().StartsWith("pcc") 
                        || (x.FileLocation.InPackages && !x.FileLocation.HintIsValid) || !x.FileLocation.InPackages)))
                {
                    Console.WriteLine("CUSTOM:" + ar.Name);
                }
            }


            SaveOutputFile("IndianaSourceCodeAnalysisReport.html",
                null,
                HtmlReportGenerator.GenerateHtml(data));
            //Console.ReadLine();
        }

        public static void SaveOutputFile(string defaultFileName, string overrideFilePath, string fileData)
        {
            File.WriteAllText($"{Path.GetFileNameWithoutExtension(defaultFileName)}{Path.GetExtension(defaultFileName)}", fileData);
            // save the file to the bin directory
            //if (AppSettings.Instance.CopyAllFilesToBinDirectory)
            //{
            //    File.WriteAllText($"{Path.GetFileNameWithoutExtension(defaultFileName)}.{DateTime.Now.ToString("yyyyMMddHHmm")}{Path.GetExtension(defaultFileName)}", fileData);
            //}
            // if configured, save the file in the "generated" folder, or overriden output directory
            //if (AppSettings.Instance.CopyAllFilesToOutputDirectory)
            //{
            //    var fp = Path.Combine(AppContext.Instance.OutputDirectory.FullName, defaultFileName);
            //    if (File.Exists(fp)) File.Delete(fp);
            //    File.WriteAllText(fp, fileData);
            //}
            //if (!string.IsNullOrWhiteSpace(overrideFilePath))
            //{
            //    var fi = new FileInfo(overrideFilePath);
            //    if (fi.Directory.Exists)
            //    {
            //        if (fi.Exists) { fi.Delete(); fi.Refresh(); }
            //        File.WriteAllText(fi.FullName, fileData);
            //    }
            //}
        }
    }
}