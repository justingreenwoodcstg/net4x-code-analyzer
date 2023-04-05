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
using CSTG.CodeAnalyzer.Model.Reflection;

namespace CSTG.CodeAnalyzer
{
    internal class Program
    {
        const bool FORCE_RELOAD = false;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                //AnalyzeFolder("FIXED_IN_INBIZ", "in-inbiz-fixed", @"C:\git-repos\IN_INBIZ");
                //AnalyzeFolder("FIXED_IN_BSD", "in-bsd-fixed", @"C:\git-repos\IN_BSD");
                //AnalyzeFolder("IN_INBIZ", "in-inbiz-untouched", @"C:\projects\state-of-indiana\untouched\IN_INBIZ");
                //AnalyzeFolder("IN_BSD", "in-bsd-untouched", @"C:\projects\state-of-indiana\untouched\IN_BSD");
                //AnalyzeFolder("IN_WebService_BSDService", "in-bsdsvc-untouched", @"C:\projects\state-of-indiana\untouched\IN_WebService_BSDService");
                //AnalyzeFolder("IN_AdminApp_SOS", "in-admin-sos-untouched", @"C:\projects\state-of-indiana\untouched\IN_AdminApp_SOS");
                //AnalyzeFolder("IN_AdminApp_DOR", "in-admin-dor-untouched", @"C:\projects\state-of-indiana\untouched\IN_OtherAdminApps_DOD-IPLA-DWD\IN_AdminApp_DOR");
                //AnalyzeFolder("IN_AdminApp_DWD", "in-admin-dwd-untouched", @"C:\projects\state-of-indiana\untouched\IN_OtherAdminApps_DOD-IPLA-DWD\IN_AdminApp_DWD");
                //AnalyzeFolder("IN_AdminApp_IPLA", "in-admin-ipla-untouched", @"C:\projects\state-of-indiana\untouched\IN_OtherAdminApps_DOD-IPLA-DWD\IN_AdminApp_IPLA");
                //AnalyzeFolder("Everything", "everything-untouched", @"C:\projects\state-of-indiana\untouched");
            }
            //Console.ReadKey();
            var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            var files = currentDir.GetFiles("*.type-details.json");
            foreach (var file in files)
            {
                var title = file.Name.Replace(".type-details.json", "");
                var reportData = JsonConvert.DeserializeObject<ApplicationAssemblyInfo>(File.ReadAllText(file.FullName));
                reportData.ReportTitle = title;
                var reportHtml = ReflectionReportGenerator.GenerateHtml(reportData);
                File.WriteAllText(title + "-api-details.html", reportHtml);
            }
        }



        public static void AnalyzeFolder(string title, string filePrefix, string directory)
        { 
            var rootDirectory = new DirectoryInfo(directory);
            var jsonDataFileName = filePrefix + "-code-analysis.json";
            var htmlReportName = filePrefix + "-code-analysis.html";

            // files to look at in the future - .sln (build/deploy profiles) .sql (ddl files) *.cs app.config web.config packages.config
            HarvestedData data = null;

            if (File.Exists(jsonDataFileName) && !FORCE_RELOAD)
            {
                try
                {
                    data = JsonConvert.DeserializeObject<HarvestedData>(File.ReadAllText(jsonDataFileName));
                } catch { }
            }

            if (data == null)
            {
                // deal with project files!
                var projectFiles = new List<ProjectFile>();
                var matchingFiles = Utility.RecursiveFileSearch(rootDirectory, extensions: new string[] { ".csproj", ".sqlproj" }, skipDirs: new string[] { "bin", "obj", "packages", "Libraries", ".git" });
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
                matchingFiles = Utility.RecursiveFileSearch(rootDirectory, extensions: new string[] { ".sln" }, skipDirs: new string[] { "bin", "obj", "packages", "Libraries", ".git" });
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
                    ReportTitle = title,
                    Solutions = solutionFiles,
                    Projects = projectFiles
                };

                AssemblyUtil.EnrichHarvestedDataWithAssemblies(rootDirectory, data);


                foreach (var project in data.Projects)
                {
                    var configFiles = Utility.RecursiveFileSearch(project.File.Directory, extensions: new string[] { ".config" }, skipDirs: new string[] { "bin", "obj", "packages", "Libraries", ".git" });
                    foreach (var confFile in configFiles.Where(c => c.Name.Equals("app.config", StringComparison.InvariantCultureIgnoreCase) || c.Name.Equals("web.config", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        project.ConfigFiles.Add(ConfigFileUtil.Read(confFile));
                    }
                }

                File.WriteAllText(jsonDataFileName, JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
            }
            if (data.ReportTitle != title)
            {
                data.ReportTitle = title;
                File.WriteAllText(jsonDataFileName, JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
            }

            //foreach (var project in data.Projects)
            //{
            //    Console.WriteLine("=========================================================================");
            //    Console.WriteLine("== " + project.AssemblyName);
            //    Console.WriteLine("=========================================================================");
            //    foreach (var ar in project.AssemblyReferences.Where(x => !string.IsNullOrWhiteSpace(x.HintPath) && x.FileLocation == null))
            //    {
            //        Console.WriteLine("MISSING:" + ar.Name);
            //    }
            //    Console.WriteLine("-------------------------------------------------------------------------");
            //    foreach (var ar in project.AssemblyReferences.Where(x => !string.IsNullOrWhiteSpace(x.HintPath) && x.FileLocation != null 
            //        && (x.Name.ToLower().StartsWith("gcr") || x.Name.ToLower().StartsWith("pcc") 
            //            || (x.FileLocation.InPackages && !x.FileLocation.HintIsValid) || !x.FileLocation.InPackages)))
            //    {
            //        Console.WriteLine("CUSTOM:" + ar.Name);
            //    }
            //}


            SaveOutputFile(htmlReportName,
                null,
                HtmlReportGenerator.GenerateHtml(data));

            AssemblyUtil.TypeCheck(data);
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