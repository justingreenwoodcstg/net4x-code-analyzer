using CSTG.CodeAnalyzer.Model;
using NuGet.ContentModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace CSTG.CodeAnalyzer.Reports
{
    public class HtmlReportGenerator
    {
        public static bool UseCheckConstraintsInsteadOfEnums { get; set; } = true;

        private const string FileHeader = @"<!DOCTYPE html>
<html lang=""en"">
	<head>
		<title>{title}</title>
{css}
	</head>
	<body>";

        private const string FileFooter = @"</body>
</html>";

        public static string GenerateHtml(HarvestedData data)
        {
            string s;
            var sb = new StringBuilder();

            sb.AppendLine(FileHeader
                .Replace("{title}", $"{data.ReportTitle} - Source Code Analysis - {DateTime.Now.ToString()}")
                .Replace("{css}", CssBlock));

            sb.AppendLine($"<h1>{data.ReportTitle} - Source Code Analysis</h1>");
            sb.AppendLine($"<p><i>Generated from the source code on {DateTime.Now.ToString()}.</i></p>");
            sb.AppendLine(GenerateTOC(data));

            var webProjects = data.Projects.Where(x => x.IsWebProject).OrderBy(p => p.AssemblyName);
            var exeProjects = data.Projects.Where(x => !x.IsWebProject && !x.IsTestProject && !x.IsDatabaseProject && !x.IsReportProject && x.ProjectType != "Library").OrderBy(p => p.AssemblyName);
            var databaseProjects = data.Projects.Where(x => x.IsDatabaseProject).OrderBy(p => p.AssemblyName);
            var reportProjects = data.Projects.Where(x => x.IsReportProject).OrderBy(p => p.AssemblyName);
            var testProjects = data.Projects.Where(x => x.IsTestProject).OrderBy(p => p.AssemblyName);
            var classLibraries = data.Projects.Where(x => x.ProjectType == "Library").OrderBy(p => p.AssemblyName);

            if (webProjects.Any())
            {
                sb.AppendLine("<h1>Web Applications</h1>");
                foreach (var proj in webProjects)
                {
                    sb.AppendLine(GenerateProjectReport(proj, data));
                }
            }
            if (exeProjects.Any())
            {
                sb.AppendLine("<h1>Executable Applications</h1>");
                foreach (var proj in exeProjects)
                {
                    sb.AppendLine(GenerateProjectReport(proj, data));
                }
            }
            if (databaseProjects.Any())
            {
                sb.AppendLine("<h1>Unit Test Projects</h1>");
                foreach (var proj in databaseProjects)
                {
                    sb.AppendLine(GenerateProjectReport(proj, data));
                }
            }
            if (reportProjects.Any())
            {
                sb.AppendLine("<h1>Report Projects</h1>");
                foreach (var proj in reportProjects)
                {
                    sb.AppendLine(GenerateProjectReport(proj, data));
                }
            }
            if (testProjects.Any())
            {
                sb.AppendLine("<h1>Unit Test Projects</h1>");
                foreach (var proj in testProjects)
                {
                    sb.AppendLine(GenerateProjectReport(proj, data));
                }
            }
            if (classLibraries.Any())
            {
                sb.AppendLine("<h1>Class Libraries</h1>");
                foreach (var proj in classLibraries)
                {
                    sb.AppendLine(GenerateProjectReport(proj, data));
                }
            }

            sb.AppendLine(GenerateNuGetPackagesReport(data));

            //sb.AppendLine("<h1>Custom Libraries</h1>");
            //sb.AppendLine(GenerateCustomLibrariesReport(data));

            sb.AppendLine("<h1>Assemblies</h1>");
            sb.AppendLine(GenerateCustomLibrariesReport("Custom Libraries", "custom-dlls", true, data));
            sb.AppendLine(GenerateCustomLibrariesReport("Third Party Libraries", "third-party-dlls", false, data));


            sb.AppendLine("<h1>File Statistics</h1>");
            sb.Append(GenerateAllFileStats(data));

            /*
            sb.AppendLine("<h1>Entity Relationship Diagram</h1>");
            if (File.Exists("Images/smt-erd.png"))
            {

                sb.AppendLine(
                    $"<section id=\"erd\">" +
                    $"<img style=\"width: 100%\" src=\"data:image/png;base64, {Convert.ToBase64String(File.ReadAllBytes("Images/smt-erd.png"))}\"/>" +
                    $"</section>");
            }
            sb.AppendLine("<h1>Tables</h1>");
            foreach (var tbl in tables)
            {
                sb.AppendLine(GenerateTableReport(tbl, dbInfo));
            }

            sb.AppendLine("<h1>Views</h1>");
            foreach (var vw in views)
            {
                sb.AppendLine(GenerateViewReport(vw, dbInfo));
            }
            sb.AppendLine("<h1>Enums (Check Constraints)</h1>");
            foreach (var enx in enums)
            {
                sb.AppendLine(GenerateEnumConstraintReport(enx, dbInfo));
            }
            */

            sb.AppendLine(FileFooter);

            return sb.ToString();
        }
        protected static string GenerateTOC(HarvestedData data)
        {
            var webProjects = data.Projects.Where(x => x.IsWebProject).OrderBy(p => p.AssemblyName);
            var exeProjects = data.Projects.Where(x => !x.IsWebProject && !x.IsTestProject && !x.IsDatabaseProject && !x.IsReportProject && x.ProjectType != "Library").OrderBy(p => p.AssemblyName);
            var databaseProjects = data.Projects.Where(x => x.IsDatabaseProject).OrderBy(p => p.AssemblyName);
            var reportProjects = data.Projects.Where(x => x.IsReportProject).OrderBy(p => p.AssemblyName);
            var testProjects = data.Projects.Where(x => x.IsTestProject).OrderBy(p => p.AssemblyName);
            var classLibraries = data.Projects.Where(x => x.ProjectType == "Library").OrderBy(p => p.AssemblyName);

            var sb = new StringBuilder();
            sb.AppendLine("<section class=\"toc\">")
                .AppendLine("\t<h2>Table of Contents</h2>")
                .AppendLine("\t<ul>");
            if (webProjects.Any())
            {
                sb.AppendLine("\t\t<li>Web Applications")
                    .AppendLine("\t\t\t<ul>");
                foreach (var proj in webProjects)
                {
                    var solX = proj.SolutionFiles.Count == 0 ? " <span class=\"tag\">Orphan</span>" : "";
                    sb.AppendLine($"\t\t\t\t<li><a href=\"#prj_{proj.AssemblyName}\">{proj.AssemblyName}  ({proj.FrameworkVersion} - {(proj.ProjectType)})</a>{solX}</li>");
                }
                sb.AppendLine("\t\t\t</ul></li>");
            }
            if (exeProjects.Any())
            {
                sb.AppendLine("\t\t<li>Executable Applications")
                    .AppendLine("\t\t\t<ul>");
                foreach (var proj in exeProjects)
                {
                    var solX = proj.SolutionFiles.Count == 0 ? " <span class=\"tag\">Orphan</span>" : "";
                    sb.AppendLine($"\t\t\t\t<li><a href=\"#prj_{proj.AssemblyName}\">{proj.AssemblyName}  ({proj.FrameworkVersion} - {(proj.ProjectType)})</a>{solX}</li>");
                }
                sb.AppendLine("\t\t\t</ul></li>");
            }
            if (databaseProjects.Any())
            {
                sb.AppendLine("\t\t<li>Database Projects")
                    .AppendLine("\t\t\t<ul>");
                foreach (var proj in databaseProjects)
                {
                    var solX = proj.SolutionFiles.Count == 0 ? " <span class=\"tag\">Orphan</span>" : "";
                    sb.AppendLine($"\t\t\t\t<li><a href=\"#prj_{proj.AssemblyName}\">{proj.AssemblyName}  ({proj.FrameworkVersion} - {(proj.ProjectType)})</a>{solX}</li>");
                }
                sb.AppendLine("\t\t\t</ul></li>");
            }
            if (reportProjects.Any())
            {
                sb.AppendLine("\t\t<li>Report Projects")
                    .AppendLine("\t\t\t<ul>");
                foreach (var proj in reportProjects)
                {
                    var solX = proj.SolutionFiles.Count == 0 ? " <span class=\"tag\">Orphan Project</span>" : "";
                    sb.AppendLine($"\t\t\t\t<li><a href=\"#prj_{proj.File.Name}\">{proj.File.Name}  ({(proj.ProjectType)})</a>{solX}</li>");
                }
                sb.AppendLine("\t\t\t</ul></li>");
            }
            if (testProjects.Any())
            {
                sb.AppendLine("\t\t<li>Unit Test Projects")
                    .AppendLine("\t\t\t<ul>");
                foreach (var proj in testProjects)
                {
                    var solX = proj.SolutionFiles.Count == 0 ? " <span class=\"tag\">Orphan</span>" : "";
                    sb.AppendLine($"\t\t\t\t<li><a href=\"#prj_{proj.AssemblyName}\">{proj.AssemblyName}  ({proj.FrameworkVersion} - {(proj.ProjectType)})</a>{solX}</li>");
                }
                sb.AppendLine("\t\t\t</ul></li>");
            }
            if (classLibraries.Any())
            {
                sb.AppendLine("\t\t<li>Class Libraries")
                .AppendLine("\t\t\t<ul>");
                foreach (var proj in data.Projects.Where(x => x.ProjectType == "Library").OrderBy(p => p.AssemblyName))
                {
                    var solX = proj.SolutionFiles.Count == 0 ? " <span class=\"tag\">Orphan</span>" : "";
                    sb.AppendLine($"\t\t\t\t<li><a href=\"#prj_{proj.AssemblyName}\">{proj.AssemblyName}  ({proj.FrameworkVersion} - {(proj.ProjectType)})</a>{solX}</li>");
                }
                sb.AppendLine("\t\t\t</ul></li>");
            }
            sb.AppendLine("\t\t<li><a href=\"#nuget_packages\">NuGet Packages</a></li>")
                .AppendLine("\t\t<li><a href=\"#custom-dlls\">Custom Assemblies</a></li>")
                .AppendLine("\t\t<li><a href=\"#third-party-dlls\">Third Party Assemblies</a></li>")
                .AppendLine("\t\t<li><a href=\"#file-stats\">File Statistics</a></li>");
            sb.AppendLine("\t</ul>")
                .AppendLine("</section>");
            sb.AppendLine("<div class=\"pagebreak\"></div>");
            return sb.ToString();
        }
        protected static string GenerateHierarchy(ProjectFile rootProject, HarvestedData data)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"\t<h3>Reference Hierarchy</h3>")
                .AppendLine($"\t<ul>");

            Action<ProjectFile, int> renderSome = null;
            renderSome = new Action<ProjectFile, int>((proj, depth) =>
            {
                if (depth > 10) return;
                sb.AppendLine($"\t\t<li>{proj.AssemblyName}");
                if (proj.ProjectReferences.Count > 0)
                {
                    sb.AppendLine($"\t\t<ul>");
                    foreach (var _childProjRef in proj.ProjectReferences)
                    {
                        var _childProj = data.Projects.Where(x => x.ProjectId == _childProjRef.ProjectId).FirstOrDefault() ??
                            data.Projects.Where(x => x.AssemblyName == _childProjRef.Name).FirstOrDefault();
                        if (_childProj != null)
                        {
                            renderSome(_childProj, depth+1);
                        }
                    }
                    sb.AppendLine($"\t\t</ul>");
                }
                sb.AppendLine($"\t\t</li>");
            });

            renderSome(rootProject, 0);
            sb.AppendLine($"\t</ul>");
            return sb.ToString();
        }

        protected static string GenerateAllFileStats(HarvestedData data)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<section id=\"file-stats\" class=\"table\">")
                .AppendLine($"\t<h2>Project Files</h2>");
            var assemblies = new Dictionary<string, List<(AssemblyReference, ProjectFile)>>();
            var all = new List<ProjectItemTypeInfo>();
            foreach (var project in data.Projects)
            {
                foreach (var ftype in project.IncludedFileTypes)
                {
                    var match = all.Where(x => x.Classification == ftype.Classification).FirstOrDefault();
                    if (match == null) all.Add(new ProjectItemTypeInfo
                    {
                        Classification = ftype.Classification,
                        Count = ftype.Count,
                        FileExtentions = ftype.FileExtentions.OrderBy(x => x).ToList(),
                        EmptyLines = ftype.EmptyLines,
                        Lines = ftype.Lines,
                        SizeInBytes = ftype.SizeInBytes,
                    });
                    else
                    {
                        if (ftype.Lines.HasValue) match.Lines = (match.Lines ?? 0) + ftype.Lines;
                        if (ftype.EmptyLines.HasValue) match.EmptyLines = (match.EmptyLines ?? 0) + ftype.EmptyLines;
                        match.SizeInBytes += ftype.SizeInBytes;
                        match.Count += ftype.Count;
                        var missingExts = ftype.FileExtentions.Where(x => !match.FileExtentions.Contains(x)).ToList();
                        if (missingExts.Count > 0) match.FileExtentions.AddRange(missingExts);
                    }
                }
            }
            sb.Append(GenerateFileStats(all));
            sb.AppendLine($"</section>");
            return sb.ToString();
        }

        protected static string GenerateFileStats(List<ProjectItemTypeInfo> fileTypeInfos)
        {
            var sb = new StringBuilder();

            if (fileTypeInfos.Count > 0)
            {
                sb
                    .AppendLine($"\t<h3>File Statistics</h3>")
                    .AppendLine($"\t<table>")
                    .AppendLine($"\t\t<thead>")
                    .AppendLine($"\t\t\t<tr>")
                    .AppendLine($"\t\t\t\t<th>Type</th><th>Extensions</th><th>Count</th><th># Lines</th><th>Size</th>")
                    .AppendLine($"\t\t\t</tr>")
                    .AppendLine($"\t\t</thead>")
                    .AppendLine($"\t\t<tbody>");
                foreach (var ftype in fileTypeInfos.OrderByDescending(x => x.Count))
                {
                    sb.AppendLine($"\t\t\t<tr>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{ftype.Classification.ToString()}</td>");
                    sb.AppendLine($"\t\t\t\t<td>{string.Join(" ", ftype.FileExtentions)}</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{ftype.Count} files</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{ftype.Lines}{(ftype.EmptyLines > 0 ? " (+" + ftype.EmptyLines + " empty)" : "")}</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{ftype.SizeInBytes} bytes</td>");
                    sb.AppendLine($"\t\t\t</tr>");
                }
                sb.AppendLine($"\t\t</tbody>")
                    .AppendLine($"\t</table>");
            }

            return sb.ToString();
        }
        protected static string GenerateProjectReport(ProjectFile proj, HarvestedData data)
        {
            var sb = new StringBuilder();

            string projName = proj.AssemblyName ?? proj.File.Name;

            var solX = proj.SolutionFiles.Count == 0 ? " <span class=\"tag\">Orphan</span>" : "";
            sb.AppendLine($"<section id=\"prj_{projName}\" class=\"table\">")
                .AppendLine($"\t<h2>{projName} ({proj.FrameworkVersion ?? "N/A"} - {(proj.ProjectType)}){solX}</h2>")
                .AppendLine($"\t<p>This project has {proj.ConfigFiles.Count} config file(s), {proj.NugetPackages.Count} nuget package registration(s), {proj.AssemblyReferences.Count} assembly reference(s) and {proj.ProjectReferences.Count} project reference(s).</p>");

            sb.Append(GenerateFileStats(proj.IncludedFileTypes));

            if (proj.MissingFiles.Count > 0)
            {
                sb.AppendLine($"\t<h3>Missing Files</h3><ul>");
                foreach (var mf in proj.MissingFiles)
                {
                    sb.AppendLine($"\t\t<li>{mf}</li>");
                }
                sb.AppendLine($"\t</ul>");
            }

            var connectionStrings = new List<NameValuePair>();
            var settings = new List<NameValuePair>();
            var endPoints = new List<NameValuePair>();
            foreach (var configFile in proj.ConfigFiles)
            {
                connectionStrings.AddRange(configFile.ConnectionStrings);
                settings.AddRange(configFile.Settings);
                endPoints.AddRange(configFile.ServiceModelEndpoints);
            }

            Action<List<NameValuePair>,string> renderDictionary = null;
            var breakIt = new Func<string, string>((s) => 
            {
                var _sb = new StringBuilder();
                int x = 0;
                for (var i = 0;i<s.Length; i++)
                {
                    if (char.IsWhiteSpace(s[i])) x = 0;
                    else x++;
                    if (x > 100) { _sb.AppendLine(); x = 0; }
                    _sb.Append(s[i]);
                }
                return _sb.ToString(); 
            });
            renderDictionary = new Action<List<NameValuePair>, string>((dict, name) =>
            {
                if (dict.Count > 0)
                {
                    sb
                        .AppendLine($"\t<h3>{name}</h3>")
                        .AppendLine($"\t<table>")
                        .AppendLine($"\t\t<thead>")
                        .AppendLine($"\t\t\t<tr>")
                        .AppendLine($"\t\t\t\t<th>Name</th><th>Value</th>")
                        .AppendLine($"\t\t\t</tr>")
                        .AppendLine($"\t\t</thead>")
                        .AppendLine($"\t\t<tbody>");
                    foreach (var cs in dict)
                    {
                        sb.AppendLine($"\t\t\t<tr>");
                        sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{HttpUtility.HtmlEncode(cs.Name)}</td>");
                        sb.AppendLine($"\t\t\t\t<td class=\"overflow-wrap\">{HttpUtility.HtmlEncode(breakIt(cs.Value))}</td>");
                        sb.AppendLine($"\t\t\t</tr>");
                    }
                    sb.AppendLine($"\t\t</tbody>")
                        .AppendLine($"\t</table>");
                }
            });
            renderDictionary(connectionStrings, "Connection Strings");
            renderDictionary(settings, "Settings");
            renderDictionary(endPoints, "Service Model Endpoints");

            if (proj.ProjectReferences.Count > 0)
            {
                sb
                    .AppendLine($"\t<h3>Project References</h3>")
                    .AppendLine($"\t<table>")
                    .AppendLine($"\t\t<thead>")
                    .AppendLine($"\t\t\t<tr>")
                    .AppendLine($"\t\t\t\t<th>File</th><th>ID</th><th>Assembly Name</th><th>Default Namespace</th>")
                    .AppendLine($"\t\t\t</tr>")
                    .AppendLine($"\t\t</thead>")
                    .AppendLine($"\t\t<tbody>");
                foreach (var projRef in proj.ProjectReferences)
                {
                    var referencedProj = data.Projects.Where(x => x.ProjectId == projRef.ProjectId).FirstOrDefault() ??
                        data.Projects.Where(x => x.AssemblyName == projRef.Name).FirstOrDefault();
                    if (referencedProj != null)
                    {
                        sb.AppendLine($"\t\t\t<tr>");
                        sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{referencedProj.File.Name}</td>");
                        sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{referencedProj.ProjectId}</td>");
                        sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{referencedProj.AssemblyName}</td>");
                        sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{referencedProj.NameSpace}</td>");
                        sb.AppendLine($"\t\t\t</tr>");
                    }
                }
                sb.AppendLine($"\t\t</tbody>")
                    .AppendLine($"\t</table>");
            }
            var custAsses = proj.AssemblyReferences.Where(assRef => string.IsNullOrWhiteSpace(assRef.PackageId) && !string.IsNullOrWhiteSpace(assRef.HintPath)).ToList();
            if (custAsses.Count > 0)
            {
                sb
                    .AppendLine($"\t<h3>Custom Assembly References (no code)</h3>")
                    .AppendLine($"\t<table>")
                    .AppendLine($"\t\t<thead>")
                    .AppendLine($"\t\t\t<tr>")
                    .AppendLine($"\t\t\t\t<th>Assembly</th><th>Version</th><th>Location Hint</th><th>Hint Valid?</th><th>Issues</th>")
                    .AppendLine($"\t\t\t</tr>")
                    .AppendLine($"\t\t</thead>")
                    .AppendLine($"\t\t<tbody>");
                foreach (var assRef in custAsses)
                {
                    var isCustomAssembly = assRef.Name.ToLower().StartsWith("pcc") || assRef.Name.ToLower().StartsWith("gcr");
                    var status = "&nbsp;";
                    var isInPackages = assRef.HintPath.Contains(@"\packages\");
                    var fileMissing = assRef.FileLocation == null;
                    if (isCustomAssembly)
                    {
                        if (isInPackages)
                        {
                            status = "<span class=\"tag\">Private Registry</span> ";
                            if (fileMissing) status += "<span class=\"tag error-tag\">Missing File</span> ";
                        }
                        else if (fileMissing) status = "<span class=\"tag error-tag\">Missing File</span> ";
                    }
                    else
                    {
                        if (fileMissing)
                        {
                            status = "<span class=\"tag error-tag\">Missing File</span> ";
                            if (isInPackages) status += "<span class=\"tag\">Invalid Registration</span> ";
                        }
                        else
                        {
                            if (isInPackages) status = "<span class=\"tag\">Invalid Registration</span> ";
                            else status = "<span class=\"tag\">Needs Registration</span>";
                        }
                    }
                    if ((assRef.FileLocation?.InOutputDirectory ?? false) || assRef.HintPath.Contains(@"\bin\Debug") || assRef.HintPath.Contains(@"\bin\Release"))
                    {
                        status += "<span class=\"tag\">BIN Reference</span>"; status = status.Trim();
                    }
                    sb.AppendLine($"\t\t\t<tr>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{assRef.Name}</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{assRef.VersionString ?? assRef.FileLocation?.VersionString}</td>");
                    sb.AppendLine($"\t\t\t\t<td>{assRef.HintPath}</td>");
                    sb.AppendLine($"\t\t\t\t<td>{((assRef.FileLocation?.HintIsValid ?? false) ? "Yes" : "No")}</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{status}</td>");
                    sb.AppendLine($"\t\t\t</tr>");
                }
                sb.AppendLine($"\t\t</tbody>")
                    .AppendLine($"\t</table>");
            }

            if (proj.NugetPackages.Count > 0)
            {
                sb
                    .AppendLine($"\t<h3>NuGet Packages</h3>")
                    .AppendLine($"\t<table>")
                    .AppendLine($"\t\t<thead>")
                    .AppendLine($"\t\t\t<tr>")
                    .AppendLine($"\t\t\t\t<th>Name</th><th>Version</th><th>Published</th><th>Latest Version</th><th>Latest Published</th>")
                    .AppendLine($"\t\t\t\t<th>Author</th><th>Description</th><th>DLLs Referenced In Project</th>")
                    .AppendLine($"\t\t\t</tr>")
                    .AppendLine($"\t\t</thead>")
                    .AppendLine($"\t\t<tbody>");
                foreach (var pkg in proj.NugetPackages)
                {
                    var inNuget = pkg.VersionDetails != null;
                    var assemblies = proj.AssemblyReferences.Where(x => x.PackageId == pkg.Id).ToList();

                    sb.AppendLine($"\t\t\t<tr>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{(inNuget ? "" : "<span style=\"error-text\">⛔</span> ")}{pkg.Id}</td>");
                    sb.AppendLine($"\t\t\t\t<td>{pkg.VersionString}</td>");
                    if (inNuget)
                    {
                        var publishDate = pkg.VersionDetails.DatePublished.Value;
                        var latestPublishDate = pkg.LatestVersionDetails.DatePublished.Value;
                        var isDateValid = publishDate.Year > 2000;
                        var isLatestDateValid = latestPublishDate.Year > 2000;
                        List<string> urls = new List<string>();
                        if (!string.IsNullOrWhiteSpace(pkg.VersionDetails.ProjectUrl)) urls.Add($"<a href=\"{pkg.VersionDetails.ProjectUrl}\">Project</a>");
                        if (!string.IsNullOrWhiteSpace(pkg.VersionDetails.PackageUrl)) urls.Add($"<a href=\"{pkg.VersionDetails.PackageUrl}\">Package</a>");

                        var age = isDateValid ? $"({((DateTime.Now - publishDate).TotalDays / 365.0).ToString("0.0#")} yrs)" : "Age Unknown";
                        if (isDateValid) sb.AppendLine($"\t\t\t\t<td>{publishDate.Date.ToShortDateString()} {age}</td>"); else sb.AppendLine("<td>&nbsp;</td>");

                        sb.AppendLine($"\t\t\t\t<td>{pkg.LatestVersionDetails.VersionString}</td>");
                        if (isLatestDateValid) sb.AppendLine($"\t\t\t\t<td>{latestPublishDate.Date.ToShortDateString()}</td>"); else sb.AppendLine("<td>&nbsp;</td>");

                        sb.AppendLine($"\t\t\t\t<td class=\"overflow-wrap\">{pkg.VersionDetails.Authors}</td>");
                        sb.AppendLine($"\t\t\t\t<td class=\"overflow-wrap\">{pkg.VersionDetails.Summary ?? pkg.VersionDetails.Description} {string.Join(" ", urls)}</td>");

                    }
                    else
                    {
                        sb.AppendLine("\t\t\t\t<td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td>");
                    }
                    sb.AppendLine($"\t\t\t\t<td>{string.Join(" ", assemblies.Select(x => x.Name))}</td>");

                    sb.AppendLine($"\t\t\t</tr>");
                }
                sb.AppendLine($"\t\t</tbody>")
                    .AppendLine($"\t</table>");
            }

            if (!proj.IsReportProject && !proj.IsDatabaseProject)
                sb.Append(GenerateHierarchy(proj, data));

            sb.AppendLine("</section>");

            sb.AppendLine("<div class=\"pagebreak\"></div>");
            return sb.ToString();
        }
        protected static string GenerateNuGetPackagesReport(HarvestedData data)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<section id=\"nuget_packages\" class=\"table\">")
                .AppendLine($"\t<h2>NuGet Packages</h2>");
            var packages = new Dictionary<string, List<(NugetPackage, ProjectFile)>>();
            foreach (var project in data.Projects)
            {
                foreach (var pkg in project.NugetPackages)
                {
                    if (!packages.ContainsKey(pkg.Id))
                    {
                        packages[pkg.Id] = new List<(NugetPackage, ProjectFile)>();
                    }
                    packages[pkg.Id].Add((pkg, project));
                }
            }


            if (packages.Count > 0)
            {
                sb
                    .AppendLine($"\t<h3>Combined Package List</h3>")
                    .AppendLine($"\t<table>")
                    .AppendLine($"\t\t<thead>")
                    .AppendLine($"\t\t\t<tr>")
                    .AppendLine($"\t\t\t\t<th>Name</th><th>Versions</th><th>Published</th><th>Latest Version</th><th>Latest Published</th>")
                    .AppendLine($"\t\t\t\t<th>Author</th><th>Description</th><th>Projects</th>")
                    .AppendLine($"\t\t\t</tr>")
                    .AppendLine($"\t\t</thead>")
                    .AppendLine($"\t\t<tbody>");
                foreach (var packageId in packages.Keys.OrderBy(x => x))
                {
                    var pkgAndProjs = packages[packageId];
                    var pkg = pkgAndProjs[0].Item1;
                    var versions = new List<string>();
                    var projects = new List<ProjectFile>();
                    foreach (var pkgAndProj in pkgAndProjs)
                    {
                        if (!versions.Contains(pkgAndProj.Item1.VersionString)) versions.Add(pkgAndProj.Item1.VersionString);
                        projects.Add(pkgAndProj.Item2);
                    }
                    var inNuget = pkg.VersionDetails != null;

                    sb.AppendLine($"\t\t\t<tr>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{(inNuget ? "" : "<span style=\"error-text\">⛔</span> ")}{pkg.Id}</td>");
                    sb.AppendLine($"\t\t\t\t<td>{string.Join(" ", versions)}</td>");
                    if (inNuget)
                    {
                        var publishDate = pkg.VersionDetails.DatePublished.Value;
                        var latestPublishDate = pkg.LatestVersionDetails.DatePublished.Value;
                        var isDateValid = publishDate.Year > 2000;
                        var isLatestDateValid = latestPublishDate.Year > 2000;
                        List<string> urls = new List<string>();
                        if (!string.IsNullOrWhiteSpace(pkg.VersionDetails.ProjectUrl)) urls.Add($"<a href=\"{pkg.VersionDetails.ProjectUrl}\">Project</a>");
                        if (!string.IsNullOrWhiteSpace(pkg.VersionDetails.PackageUrl)) urls.Add($"<a href=\"{pkg.VersionDetails.PackageUrl}\">Package</a>");

                        var age = isDateValid ? $"({((DateTime.Now - publishDate).TotalDays / 365.0).ToString("0.0#")} yrs)" : "Age Unknown";
                        if (isDateValid) sb.AppendLine($"\t\t\t\t<td>{publishDate.Date.ToShortDateString()} {age}</td>"); else sb.AppendLine("<td>&nbsp;</td>");

                        sb.AppendLine($"\t\t\t\t<td>{pkg.LatestVersionDetails.VersionString}</td>");
                        if (isLatestDateValid) sb.AppendLine($"\t\t\t\t<td>{latestPublishDate.Date.ToShortDateString()}</td>"); else sb.AppendLine("<td>&nbsp;</td>");

                        sb.AppendLine($"\t\t\t\t<td class=\"overflow-wrap\">{pkg.VersionDetails.Authors}</td>");
                        sb.AppendLine($"\t\t\t\t<td class=\"overflow-wrap\">{pkg.VersionDetails.Summary ?? pkg.VersionDetails.Description} {string.Join(" ", urls)}</td>");
                    }
                    else
                    {
                        sb.AppendLine("\t\t\t\t<td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td>");
                    }
                    sb.AppendLine($"\t\t\t\t<td>{string.Join(" ", projects.Select(x => x.AssemblyName))}</td>");

                    sb.AppendLine($"\t\t\t</tr>");
                }
                sb.AppendLine($"\t\t</tbody>")
                    .AppendLine($"\t</table>");
            }
            sb.AppendLine($"</section>");

            sb.AppendLine("<div class=\"pagebreak\"></div>");
            return sb.ToString();
        }
        protected static string GenerateCustomLibrariesReport(string name, string anchor, bool isCustom, HarvestedData data)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<section id=\"{anchor}\" class=\"table\">")
                .AppendLine($"\t<h2>{name}</h2>");
            var assemblies = new Dictionary<string, List<(AssemblyReference, ProjectFile)>>();
            foreach (var project in data.Projects)
            {
                var subset = isCustom 
                    ? project.AssemblyReferences.Where(a => string.IsNullOrWhiteSpace(a.PackageId) && !string.IsNullOrWhiteSpace(a.HintPath) && (a.Name.ToLower().StartsWith("pcc") || a.Name.ToLower().StartsWith("gcr")))
                    : project.AssemblyReferences.Where(a => string.IsNullOrWhiteSpace(a.PackageId) && !string.IsNullOrWhiteSpace(a.HintPath) && (!a.Name.ToLower().StartsWith("pcc") && !a.Name.ToLower().StartsWith("gcr")));
                foreach (var ass in subset)
                {
                    if (!assemblies.ContainsKey(ass.Name))
                    {
                        assemblies[ass.Name] = new List<(AssemblyReference, ProjectFile)>();
                    }
                    assemblies[ass.Name].Add((ass, project));
                }
            }

            
            if (assemblies.Count > 0)
            {
                sb
                    .AppendLine($"\t<h3>Combined Assembly List</h3>")
                    .AppendLine($"\t<table>")
                    .AppendLine($"\t\t<thead>")
                    .AppendLine($"\t\t\t<tr>")
                    .AppendLine($"\t\t\t\t<th>Assembly</th><th>Versions</th><th>Location Hint</th><th>Hint Valid?</th><th>Projects</th><th>Issues</th>")
                    .AppendLine($"\t\t\t</tr>")
                    .AppendLine($"\t\t</thead>")
                    .AppendLine($"\t\t<tbody>");
                foreach (var assemblyName in assemblies.Keys.OrderBy(x => x))
                {
                    var assAndProjs = assemblies[assemblyName];
                    var assRef = assAndProjs[0].Item1;
                    var versions = new List<string>();
                    var projects = new List<ProjectFile>();
                    foreach (var assAndProj in assAndProjs)
                    {
                        var v = assAndProj.Item1.VersionString ?? assAndProj.Item1.FileLocation?.VersionString;
                        if (v != null) if (!versions.Contains(v)) versions.Add(v);
                        projects.Add(assAndProj.Item2);
                    }
                    var isCustomAssembly = assRef.Name.ToLower().StartsWith("pcc") || assRef.Name.ToLower().StartsWith("gcr");
                    var status = "&nbsp;";
                    var isInPackages = assRef.HintPath.Contains(@"\packages\");
                    var fileMissing = assRef.FileLocation == null;
                    if (isCustomAssembly)
                    {
                        if (isInPackages)
                        {
                            status = "<span class=\"tag\">Private Registry</span> ";
                            if (fileMissing) status += "<span class=\"tag error-tag\">Missing File</span> ";
                        }
                        else if (fileMissing) status = "<span class=\"tag error-tag\">Missing File</span> ";
                    }
                    else
                    {
                        if (fileMissing)
                        {
                            status = "<span class=\"tag error-tag\">Missing File</span> ";
                            if (isInPackages) status += "<span class=\"tag\">Invalid Registration</span> ";
                        }
                        else
                        {
                            if (isInPackages) status = "<span class=\"tag\">Invalid Registration</span> ";
                            else status = "<span class=\"tag\">Needs Registration</span>";
                        }
                    }
                    if (versions.Count > 1)
                    {
                        status += "<span class=\"tag\">Multiple Versions</span>"; status = status.Trim();
                    }
                    if ((assRef.FileLocation?.InOutputDirectory ?? false) || assRef.HintPath.Contains(@"\bin\Debug") || assRef.HintPath.Contains(@"\bin\Release"))
                    {
                        status += "<span class=\"tag\">BIN Reference</span>"; status = status.Trim();
                    }
                    sb.AppendLine($"\t\t\t<tr>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{assRef.Name}</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{string.Join(" ",versions)}</td>");
                    sb.AppendLine($"\t\t\t\t<td>{assRef.HintPath}</td>");
                    sb.AppendLine($"\t\t\t\t<td>{((assRef.FileLocation?.HintIsValid ?? false) ? "Yes" : "No")}</td>");
                    sb.AppendLine($"\t\t\t\t<td>{string.Join(" ", projects.Select(x => x.AssemblyName))}</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{status}</td>");
                    sb.AppendLine($"\t\t\t</tr>");
                }
                sb.AppendLine($"\t\t</tbody>")
                    .AppendLine($"\t</table>");
            }
            sb.AppendLine($"</section>");

            sb.AppendLine("<div class=\"pagebreak\"></div>");
            return sb.ToString();
        }

        #region CSS
        public const string CssBlock = @"
    <link href=""https://fonts.googleapis.com/css?family=Open+Sans:400,600,300"" rel=""stylesheet"" type=""text/css"">
    <style>
        html, body, td, th {
            font-family: ""Open Sans"";
            font-size: 12px;
        }

        h1 {
        }

        h2 {
            width: 100%;
            background-color: lightgray;
            border-bottom: 1px solid black;
            padding: 0.2em;
        }

        h3 {
            width: 100%;
            border-bottom: 1px dotted darkgray;
            padding: 0.2em;
        }

        .nowrap {
            white-space: nowrap;
        }

        .overflow-wrap {
            overflow-wrap: anywhere; 
        }

        .break-word {
            overflow-wrap: break-word; 
            
        }

        .centered {
            text-align: center;
            white-space: nowrap;
        }

        .column-icon {
            cursor: pointer;
        }

        .error-text {
            color: red;
        }

        .warning-text {
            color: red;
        }

        .warning-bg {
            background-color: yellow !important;
        }

        /* tables */
        .table table {
            border-collapse: collapse;
            margin: 25px 0;
            font-size: 0.9em;
            font-family: sans-serif;
            /*min-width: 400px;
            max-width: 1075px;*/
            width: 100%;
            box-shadow: 0 0 20px rgba(0, 0, 0, 0.15);
        }

        .table table thead tr {
            text-transform: uppercase;
            background-color: lightgray;
            color: black;
            border-bottom: black solid 1px;
        }

        .table table th,
        .table table td {
            padding: 0.2em 0.3em;
        }

        .table table td {
            vertical-align: top;
        }

        .table table tbody tr {
            border-bottom: 1px solid #dddddd;
        }

        .table table tbody tr:nth-of-type(even) {
            background-color: #e1e1e1;
        }

        .table table tbody tr:last-of-type {
            border-bottom: 2px solid black;
        }

        /* Table of Contents*/
        .toc ol {
            list-style: none;
            counter-reset: cupcake;
            padding-left: 32px;
        }

        .toc ol li {
            counter-increment: cupcake;
        }

        .toc ol li:before {
            content: counters(cupcake, ""."") "". "";
        }

        /* Anchors */
        a:link {
          color:DarkCyan;
          text-decoration: none;
        }

        a:visited {
          color:DarkCyan;
          text-decoration: none;
        }

        a:hover {
          color:cadetblue;
          text-decoration: underline green;
        }

        a:active {
          color:DarkCyan;
          text-decoration: none;
        }

        div.code {
            color: AntiqueWhite;
            font-family: consolas, courier new;
            font-size: 10px;
            background-color: DarkSlateGray;
            border: 1px solid black;
            padding: 10px;
            overflow-x: scroll;
            width: 95%;
        }

        .tag {
          border-radius: 4px;
          position: relative;
          background: orange;
          color: black;
          text-align:center;
          padding: 2px;
          padding-left: 6px;
          padding-right: 6px;
          font-size: 10px;

        }
        .error-tag {
          background: red;
          color: white;
        }
        @media print {
            .pagebreak {
                clear: both;
                page-break-after: always;
            }
        }
    </style>";
        #endregion
    }
}
