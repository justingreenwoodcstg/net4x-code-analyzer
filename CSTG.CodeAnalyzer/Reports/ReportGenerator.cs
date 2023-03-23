using CSTG.CodeAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSTG.CodeAnalyzer.Reports
{
    public class HtmlReportGenerator
    {
        public static bool UseCheckConstraintsInsteadOfEnums { get; set; } = true;

        private const string FileHeader = @"<html>
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
                .Replace("{title}", $"IN SOS Source Code Analysis - {DateTime.Now.ToString()}")
                .Replace("{css}", CssBlock));

            sb.AppendLine("<h1>IN SOS PSource Code Analysis</h1>");
            sb.AppendLine($"<p><i>Generated from the source code on {DateTime.Now.ToString()}.</i></p>");
            //sb.AppendLine(GenerateTOC(data));

            sb.AppendLine("<h1>Applications</h1>");
            foreach (var sln in data.Solutions)
            {
                sb.AppendLine(GenerateSolutionReport(sln, data));
            }

            sb.AppendLine("<h1>Projects</h1>");
            foreach (var proj in data.Projects)
            {
                sb.AppendLine(GenerateProjectReport(proj, data)); 
            }

            sb.AppendLine("<h1>NuGet Packages</h1>");
            sb.AppendLine(GenerateNuGetPackagesReport(data));

            sb.AppendLine("<h1>Custom Libraries</h1>");
            sb.AppendLine(GenerateCustomLibrariesReport(data));

            sb.AppendLine("<h1>Third Party Libraries</h1>");
            sb.AppendLine(Generate3rdPartyLibrariesReport(data));
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
            var sb = new StringBuilder();
            //sb.AppendLine("<section class=\"toc\">")
            //    .AppendLine("\t<h2>Table of Contents</h2>")
            //    .AppendLine("\t<ul>")
            //    .AppendLine("\t\t<li><a href=\"#erd\">Entity Relationship Diagram</a></li>")
            //    .AppendLine("\t\t<li>Tables</li>")
            //    .AppendLine("\t\t\t<ul>");
            //foreach (var tbl in tables)
            //{
            //    sb.AppendLine($"\t\t\t\t<li><a href=\"#tbl_{tbl.NameInCamelCase}\">{tbl.SheetName} ({tbl.Name})</a></li>");
            //}
            //sb.AppendLine("\t\t\t</ul>")
            //    .AppendLine("\t\t<li>Views</li>")
            //    .AppendLine("\t\t\t<ul>");
            //foreach (var vw in views)
            //{
            //    sb.AppendLine($"\t\t\t\t<li><a href=\"#vw_{vw.NameInCamelCase}\">{vw.NameInPascalCase} ({vw.Name})</a></li>");
            //}
            //sb.AppendLine("\t\t\t</ul>")
            //    .AppendLine("\t\t<li>Enums (Check Constraints)</li>")
            //    .AppendLine("\t\t\t<ul>");
            //foreach (var enx in enums)
            //{
            //    sb.AppendLine($"\t\t\t\t<li><a href=\"#enum_{enx.NameInCamelCase}\">{enx.Name}</a></li>");
            //}
            //enum_{ enx.NameInCamelCase}
            //sb.AppendLine("\t\t\t</ul>")
            //    .AppendLine("\t\t</li>")
            //    .AppendLine("\t</ul>")
            //    .AppendLine("</section>");
            return sb.ToString();
        }
        protected static string GenerateSolutionReport(SolutionFile sln, HarvestedData data)
        {
            var sb = new StringBuilder();

            var webProjects = new List<ProjectFile>();
            var otherProjects = new List<ProjectFile>();
            foreach (var projRef in sln.Projects)
            {
                var matchingProj = data.Projects.Where(x => x.ProjectId == projRef.ProjectId).FirstOrDefault();
                if (matchingProj != null)
                {
                    if (matchingProj.IsWebProject)
                        webProjects.Add(matchingProj);
                    else
                        otherProjects.Add(matchingProj);
                }
            }

            sb.AppendLine($"<section id=\"sln-{sln.File.Name}\" class=\"table\">")
                .AppendLine($"\t<h2>{sln.File.Name}</h2>")
                .AppendLine($"\t<h3>Reference Hierarchy</h3>")
                .AppendLine($"\t<ul>");

            Action<ProjectFile, int> renderSome = null;
            renderSome = new Action<ProjectFile, int>((proj, depth) =>
            {
                if (depth > 10) return;
                sb.AppendLine($"\t\t<li>{proj.AssemblyName}</li>");
                if (proj.ProjectReferences.Count > 0)
                {
                    sb.AppendLine($"\t\t<ul>");
                    foreach (var _childProjRef in proj.ProjectReferences)
                    {
                        var _childProj = otherProjects.Where(x => x.ProjectId == _childProjRef.ProjectId).FirstOrDefault() ??
                            otherProjects.Where(x => x.AssemblyName == _childProjRef.Name).FirstOrDefault();
                        if (_childProj != null)
                        {
                            renderSome(_childProj, depth+1);
                        }
                    }
                    sb.AppendLine($"\t\t</ul>");
                }
            });

            foreach (var project in webProjects)
            {
                renderSome(project, 0);
            }
            sb.AppendLine($"\t</ul>");
            sb.AppendLine($"</section>");
            return sb.ToString();
        }
        protected static string GenerateProjectReport(ProjectFile proj, HarvestedData data)
        {
            var sb = new StringBuilder();


            sb.AppendLine($"<section id=\"prj_{proj.AssemblyName}\" class=\"table\">")
                .AppendLine($"\t<h2>{proj.AssemblyName} ({(proj.IsWebProject ? "web" : proj.OutputType)})</h2>");

            if (proj.NugetPackages.Count > 0)
            {
                sb
                    .AppendLine($"\t<h3>NuGet Packages</h2>")
                    .AppendLine($"\t<table>")
                    .AppendLine($"\t\t<thead>")
                    .AppendLine($"\t\t\t<tr>")
                    .AppendLine($"\t\t\t\t<th>Name</th><th>Version</th><th>Age</th><th>Published</th><th>Latest Version</th><th>Latest Published</th>")
                    .AppendLine($"\t\t\t\t<th>Author</th><th>Description</th><th>URLs</th><th>DLLs Referenced In Project</th>")
                    .AppendLine($"\t\t\t</tr>")
                    .AppendLine($"\t\t</thead>")
                    .AppendLine($"\t\t<tbody>");
                foreach (var pkg in proj.NugetPackages)
                {
                    var inNuget = pkg.VersionDetails != null;
                    var assemblies = proj.AssemblyReferences.Where(x => x.PackageId == pkg.Id).ToList();

                    sb.AppendLine($"\t\t\t<tr>");

                    //sb.Append($"\t\t\t\t<td class=\"centered column-icon\">");
                    //if (col.IsPrimaryKey) sb.Append(" <span title=\"primary key\">&#128273;</span>");
                    //else if (col.IsUnique) sb.Append(" <span title=\"unique\">&#10052;</span>");
                    //if (fkType == RelationshipType.OneToMany) sb.Append(" 1&rightarrow;&infin;");
                    //else if (fkType == RelationshipType.OneToOne) sb.Append(" 1&rightarrow;1");
                    //else if (fkType == RelationshipType.ZeroToOne) sb.Append(" 0&rightarrow;1");
                    //else if (fkType == RelationshipType.ZeroToMany) sb.Append(" 0&rightarrow;&infin;");
                    //sb.AppendLine($" </td>");

                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{(inNuget ? "" : "<span style=\"error-text\">⛔</span> ")}{pkg.Id}</td>");
                    sb.AppendLine($"\t\t\t\t<td>{pkg.VersionString}</td>");
                    if (inNuget)
                    {
                        var publishDate = pkg.VersionDetails.DatePublished.Value;
                        var latestPublishDate = pkg.LatestVersionDetails.DatePublished.Value;
                        var isDateValid = publishDate.Year > 2000;

                        if (isDateValid) sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{((DateTime.Now - publishDate).TotalDays / 365.0).ToString("0.0#")} yrs</td>"); else sb.AppendLine("<td>Age Unknown</td>");
                        if (isDateValid) sb.AppendLine($"\t\t\t\t<td>{publishDate.Date.ToShortDateString()}</td>"); else sb.AppendLine("<td>&nbsp;</td>");

                        sb.AppendLine($"\t\t\t\t<td>{pkg.LatestVersionDetails.VersionString}</td>");
                        if (isDateValid) sb.AppendLine($"\t\t\t\t<td>{latestPublishDate.Date.ToShortDateString()}</td>"); else sb.AppendLine("<td>&nbsp;</td>");

                        sb.AppendLine($"\t\t\t\t<td>{pkg.VersionDetails.Authors}</td>");
                        sb.AppendLine($"\t\t\t\t<td>{pkg.VersionDetails.Summary ?? pkg.VersionDetails.Description}</td>");
                        sb.AppendLine($"\t\t\t\t<td><a href=\"{pkg.VersionDetails.ProjectUrl}\">Project</a> <br/> <a href=\"{pkg.VersionDetails.PackageUrl}\">Package</a></td>");
                    }
                    else
                    {
                        sb.AppendLine("\t\t\t\t<td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td>");
                    }
                    sb.AppendLine($"\t\t\t\t<td>{string.Join(" ", assemblies.Select(x => x.Name))}</td>");

                    sb.AppendLine($"\t\t\t</tr>");
                }
                sb.AppendLine($"\t\t</tbody>")
                    .AppendLine($"\t</table>");
            }
            if (proj.ProjectReferences.Count > 0)
            {
                sb
                    .AppendLine($"\t<h3>Project References</h2>")
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
            return sb.ToString();
        }
        protected static string GenerateNuGetPackagesReport(HarvestedData data)
        {
            var sb = new StringBuilder();
            return sb.ToString();
        }
        protected static string GenerateCustomLibrariesReport(HarvestedData data)
        {
            var sb = new StringBuilder();
            return sb.ToString();
        }
        protected static string Generate3rdPartyLibrariesReport(HarvestedData data)
        {
            var sb = new StringBuilder();
            return sb.ToString();
        }

        //protected static string GenerateTOC(HarvestedData data)
        //{
        //    var tables = dbInfo.Tables.OrderBy(x => x.Name);
        //    var views = dbInfo.Views.OrderBy(x => x.Name);
        //    var enums = dbInfo.Enums.OrderBy(x => x.Name);
        //    var sb = new StringBuilder();
        //    sb.AppendLine("<section class=\"toc\">")
        //        .AppendLine("\t<h2>Table of Contents</h2>")
        //        .AppendLine("\t<ul>")
        //        .AppendLine("\t\t<li><a href=\"#erd\">Entity Relationship Diagram</a></li>")
        //        .AppendLine("\t\t<li>Tables</li>")
        //        .AppendLine("\t\t\t<ul>");
        //    foreach (var tbl in tables)
        //    {
        //        sb.AppendLine($"\t\t\t\t<li><a href=\"#tbl_{tbl.NameInCamelCase}\">{tbl.SheetName} ({tbl.Name})</a></li>");
        //    }
        //    sb.AppendLine("\t\t\t</ul>")
        //        .AppendLine("\t\t<li>Views</li>")
        //        .AppendLine("\t\t\t<ul>");
        //    foreach (var vw in views)
        //    {
        //        sb.AppendLine($"\t\t\t\t<li><a href=\"#vw_{vw.NameInCamelCase}\">{vw.NameInPascalCase} ({vw.Name})</a></li>");
        //    }
        //    sb.AppendLine("\t\t\t</ul>")
        //        .AppendLine("\t\t<li>Enums (Check Constraints)</li>")
        //        .AppendLine("\t\t\t<ul>");
        //    foreach (var enx in enums)
        //    {
        //        sb.AppendLine($"\t\t\t\t<li><a href=\"#enum_{enx.NameInCamelCase}\">{enx.Name}</a></li>");
        //    }
        //    //enum_{enx.NameInCamelCase}
        //    sb.AppendLine("\t\t\t</ul>")
        //        .AppendLine("\t\t</li>")
        //        .AppendLine("\t</ul>")
        //        .AppendLine("</section>");
        //    return sb.ToString();
        //}

        //protected static string GenerateTableReport(TableInfo tbl, DatabaseInfo dbInfo)
        //{
        //    var sb = new StringBuilder();
        //    sb.AppendLine($"<section id=\"tbl_{tbl.NameInCamelCase}\" class=\"table\">")
        //        .AppendLine($"\t<h2>{tbl.SheetName} ({tbl.Name})</h2>")
        //        .AppendLine($"\t<p>{tbl.Comments}</p>")
        //        .AppendLine($"\t<table>")
        //        .AppendLine($"\t\t<thead>")
        //        .AppendLine($"\t\t\t<tr>")
        //        .AppendLine($"\t\t\t\t<th>&nbsp;</th><th>Column</th><th>Data Type</th><th>Required?</th><th>Validation</th><th>References</th><th>Description</th>")
        //        .AppendLine($"\t\t\t</tr>")
        //        .AppendLine($"\t\t</thead>")
        //        .AppendLine($"\t\t<tbody>");
        //    foreach (var col in tbl.Columns)
        //    {
        //        var fkType = tbl.GetFkType(col);
        //        var fkRef = tbl.GetFkReference(col);
        //        var enumType = dbInfo.Enums.FirstOrDefault(x => x.Name == col.DataType.TrimEnd('[', ']'));
        //        sb.AppendLine($"\t\t\t<tr>");

        //        sb.Append($"\t\t\t\t<td class=\"centered column-icon\">");
        //        if (col.IsPrimaryKey) sb.Append(" <span title=\"primary key\">&#128273;</span>");
        //        else if (col.IsUnique) sb.Append(" <span title=\"unique\">&#10052;</span>");
        //        if (fkType == RelationshipType.OneToMany) sb.Append(" 1&rightarrow;&infin;");
        //        else if (fkType == RelationshipType.OneToOne) sb.Append(" 1&rightarrow;1");
        //        else if (fkType == RelationshipType.ZeroToOne) sb.Append(" 0&rightarrow;1");
        //        else if (fkType == RelationshipType.ZeroToMany) sb.Append(" 0&rightarrow;&infin;");
        //        sb.AppendLine($" </td>");

        //        sb.AppendLine($"\t\t\t\t<td>{col.Name}</td>");
        //        if (enumType != null) sb.AppendLine($"\t\t\t\t<td><a href=\"#enum_{enumType.NameInCamelCase}\">{col.DataType}</a></td>");
        //        else sb.AppendLine($"\t\t\t\t<td>{col.DataType}</td>");
        //        sb.AppendLine($"\t\t\t\t<td class=\"centered\">{(col.IsRequired ? "&check;" : "&nbsp;")}</td>");
        //        sb.AppendLine($"\t\t\t\t<td>{col.ValidationRules}</td>");
        //        if (fkRef != null)
        //            sb.AppendLine($"\t\t\t\t<td><a href=\"#tbl_{fkRef.OtherTable.NameInCamelCase}\">{fkRef.OtherTable.Name}</a></td>");
        //        else
        //            sb.AppendLine($"\t\t\t\t<td>&nbsp;</td>");
        //        sb.AppendLine($"\t\t\t\t<td>{col.Comments}</td>");
        //        sb.AppendLine($"\t\t\t</tr>");
        //    }
        //    sb.AppendLine($"\t\t</tbody>")
        //        .AppendLine($"\t</table>");

        //    if (tbl.Data?.Rows?.Count > 0)
        //    {
        //        sb.AppendLine($"\t<h3>Seed Data</h3>")
        //            .AppendLine($"\t<table>")
        //            .AppendLine($"\t\t<thead>")
        //            .AppendLine($"\t\t\t<tr>");
        //        foreach (DataColumn col in tbl.Data?.Columns)
        //        {
        //            sb.AppendLine($"\t\t\t\t<th>{col.ColumnName ?? col.Caption}</th>");
        //        }
        //        sb.AppendLine($"\t\t\t</tr>")
        //            .AppendLine($"\t\t</thead>")
        //            .AppendLine($"\t\t<tbody>");
        //        foreach (DataRow row in tbl.Data?.Rows)
        //        {
        //            sb.AppendLine($"\t\t\t<tr>");
        //            foreach (DataColumn col in tbl.Data?.Columns)
        //            {
        //                var objVal = row[col];
        //                var val = objVal?.ToString() ?? "";
        //                if (!(objVal is string) && objVal != DBNull.Value)
        //                {
        //                    if (objVal is string[])
        //                        val = string.Join(", ", (objVal as string[]));
        //                }
        //                sb.AppendLine($"\t\t\t\t<td>{HttpUtility.HtmlEncode(val)}</td>");
        //            }
        //            sb.AppendLine($"\t\t\t</tr>");
        //        }
        //        sb.AppendLine($"\t\t</tbody>")
        //            .AppendLine($"\t</table>");
        //    }

        //    sb.AppendLine($"</section>");
        //    return sb.ToString();
        //}

        //protected static string GenerateViewReport(ViewInfo vw, DatabaseInfo dbInfo)
        //{
        //    var sb = new StringBuilder();
        //    sb.AppendLine($"<section id=\"vw_{vw.NameInCamelCase}\" class=\"table\">")
        //        .AppendLine($"\t<h2>{vw.NameInPascalCase} ({vw.Name})</h2>")
        //        .AppendLine($"\t<p>{vw.Comments}</p>")
        //        .AppendLine($"\t<table>")
        //        .AppendLine($"\t\t<thead>")
        //        .AppendLine($"\t\t\t<tr>")
        //        .AppendLine($"\t\t\t\t<th>&nbsp;</th><th>Column</th><th>Data Type</th><th>Required?</th><th>Description</th>")
        //        .AppendLine($"\t\t\t</tr>")
        //        .AppendLine($"\t\t</thead>")
        //        .AppendLine($"\t\t<tbody>");
        //    var first = true;
        //    foreach (var col in vw.Columns)
        //    {
        //        var enumType = dbInfo.Enums.FirstOrDefault(x => x.Name == col.DataType);
        //        sb.AppendLine($"\t\t\t<tr>");

        //        sb.Append($"\t\t\t\t<td class=\"centered column-icon\">");
        //        if (first) sb.Append(" <span title=\"unique\">&#10052;</span>");
        //        sb.AppendLine($" </td>");

        //        sb.AppendLine($"\t\t\t\t<td>{col.Name}</td>");
        //        if (dbInfo.Enums.Any(x => x.Name == col.DataType)) sb.AppendLine($"\t\t\t\t<td><a href=\"#enum_{enumType.NameInCamelCase}\">{col.DataType}</a></td>");
        //        else sb.AppendLine($"\t\t\t\t<td>{col.DataType}</td>");
        //        sb.AppendLine($"\t\t\t\t<td class=\"centered\">{(col.IsRequired ? "&check;" : "&nbsp;")}</td>");
        //        sb.AppendLine($"\t\t\t\t<td>{col.Comments}</td>");
        //        sb.AppendLine($"\t\t\t</tr>");
        //        first = false;
        //    }
        //    sb.AppendLine($"\t\t</tbody>")
        //        .AppendLine($"\t</table>");

        //    sb.Append($"\t<div class=\"code\"><pre>").Append(HttpUtility.HtmlEncode(vw.Definition))
        //        .AppendLine($"\t</pre></div>");

        //    sb.AppendLine($"</section>");
        //    return sb.ToString();
        //}

        //protected static string GenerateEnumConstraintReport(EnumInfo enx, DatabaseInfo dbInfo)
        //{
        //    var sb = new StringBuilder();
        //    sb.AppendLine($"<section id=\"enum_{enx.NameInCamelCase}\" class=\"table\">")
        //        .AppendLine($"\t<h2>{enx.Name}</h2>")
        //        .AppendLine($"\t<p>{enx.Comments}</p>");
        //    if (enx.Values?.Count > 0)
        //    {
        //        sb.AppendLine($"\t<table>")
        //            .AppendLine($"\t\t<thead>")
        //            .AppendLine($"\t\t\t<tr>")
        //            .AppendLine($"\t\t\t\t<th>Value</th>")
        //            .AppendLine($"\t\t\t</tr>")
        //            .AppendLine($"\t\t</thead>")
        //            .AppendLine($"\t\t<tbody>");
        //        foreach (var val in enx.Values)
        //        {
        //            sb.AppendLine($"\t\t\t<tr>");
        //            sb.AppendLine($"\t\t\t\t<td>{HttpUtility.HtmlEncode(val)}</td>");
        //            sb.AppendLine($"\t\t\t</tr>");
        //        }
        //        sb.AppendLine($"\t\t</tbody>")
        //            .AppendLine($"\t</table>");
        //    }

        //    sb.AppendLine($"</section>");
        //    return sb.ToString();
        //}

        #region CSS
        public const string CssBlock = @"
    <link href=""https://fonts.googleapis.com/css?family=Open+Sans:400,600,300"" rel=""stylesheet"" type=""text/css"">
    <style>
        html, body, td, th {
            font-family: ""Open Sans"";
            font-size: 12px;
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

        .centered {
            text-align: center;
            white-space: nowrap;
        }

        .column-icon {
            cursor: hand;
            cursor: pointer;
        }

        .error-text {
            color: red;
        }

        .warning-text {
            color: yellow;
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
            min-width: 400px;
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
    </style>";
        #endregion
    }
}
