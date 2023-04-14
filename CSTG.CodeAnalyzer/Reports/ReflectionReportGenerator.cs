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
using CSTG.CodeAnalyzer.Model.Reflection;
using System.Security.Cryptography;
using System.Web.UI.WebControls;

namespace CSTG.CodeAnalyzer.Reports
{
    public class ReflectionReportGenerator
    {
        private const string FileHeader = @"<!DOCTYPE html>
<html lang=""en"">
	<head>
		<title>{title}</title>
{css}
	</head>
	<body>";

        private const string FileFooter = @"</body>
</html>";

        public static string GenerateHtml(ApplicationAssemblyInfo data)
        {
            string s;
            var sb = new StringBuilder();

            sb.AppendLine(FileHeader
                .Replace("{title}", $"{data.ReportTitle} - API Analysis - {DateTime.Now.ToString()}")
                .Replace("{css}", CssBlock));

            sb.AppendLine($"<h1>{data.ReportTitle} - API Analysis</h1>");
            sb.AppendLine($"<p><i>Generated from the source code on {DateTime.Now.ToString()}.</i></p>");
            sb.AppendLine(GenerateTOC(data));

            if (data.Routes?.Any() ?? false)
            {
                sb.AppendLine("<h1>Routes</h1>");
                sb.AppendLine(GenerateRouteReport(data));
            }

            var apiControllers = new List<(CustomTypeInfo, AssemblyTypeInfo)>();
            var mvcControllers = new List<(CustomTypeInfo, AssemblyTypeInfo)>();
            var activities = new List<(CustomTypeInfo, AssemblyTypeInfo)>();


            foreach (var ass in data.Assemblies)
            {
                mvcControllers.AddRange(ass.Types.Where(t => t.IsMvcController).Select(x=> (x, ass)));
                apiControllers.AddRange(ass.Types.Where(t => t.IsApiController).Select(x => (x, ass)));
                activities.AddRange(ass.Types.Where(t => t.IsActivity).Select(x => (x, ass)));
            }
            if (activities.Any())
            {
                sb.AppendLine("<h1>WF Activities</h1>");
                sb.AppendLine(GenerateActivityReport(activities, data));
            }
            if (apiControllers.Any())
            {
                sb.AppendLine("<h1>Web API Controllers</h1>");
                foreach (var (ctrl, ass) in apiControllers)
                {
                    sb.AppendLine(GenerateApiControllerReport(ctrl, ass, data));
                }
                sb.AppendLine("<div class=\"pagebreak\"></div>");
            }
            if (mvcControllers.Any())
            {
                sb.AppendLine("<h1>MVC Controllers</h1>");
                foreach (var (ctrl, ass) in mvcControllers)
                {
                    sb.AppendLine(GenerateMvcControllerReport(ctrl, ass, data));
                }
                sb.AppendLine("<div class=\"pagebreak\"></div>");
            }
            sb.AppendLine(FileFooter);

            return sb.ToString();
        }

        //GenerateApiControllerReport GenerateMvcControllerReport
        protected static string GenerateTOC(ApplicationAssemblyInfo data)
        {
            var apiControllers = new List<(CustomTypeInfo, AssemblyTypeInfo)>();
            var mvcControllers = new List<(CustomTypeInfo, AssemblyTypeInfo)>();
            var activities = new List<(CustomTypeInfo, AssemblyTypeInfo)>();

            foreach (var ass in data.Assemblies)
            {
                mvcControllers.AddRange(ass.Types.Where(t => t.IsMvcController).Select(x => (x, ass)));
                apiControllers.AddRange(ass.Types.Where(t => t.IsApiController).Select(x => (x, ass)));
                activities.AddRange(ass.Types.Where(t => t.IsActivity).Select(x => (x, ass)));
            }

            var sb = new StringBuilder();
            sb.AppendLine("<section class=\"toc\">")
                .AppendLine("\t<h2>Table of Contents</h2>")
                .AppendLine("\t<ul>");
            if (data.Routes?.Any() ?? false)
                sb.AppendLine("\t\t<li><a href=\"#routes\">Routes</a></li>");
            if (activities?.Any() ?? false)
                sb.AppendLine("\t\t<li><a href=\"#activities\">WF Activities</a></li>");
            if (apiControllers.Any())
            {
                sb.AppendLine("\t\t<li>API Controllers")
                    .AppendLine("\t\t\t<ul>");


                foreach (var proj in apiControllers)
                {
                    var skipMethods = new string[] { "Dispose", "Equals", "ToString", "GetType", "GetHashCode", "ExecuteAsync" };
                    var apiMethods = proj.Item1.PublicMethods.Where(x => !skipMethods.Contains(x.Name));
                    if (apiMethods.Any())
                    {
                        var shortenedNS = ShortenNS(proj.Item1.Namespace, proj.Item2.Name, " - ");
                        sb.AppendLine($"\t\t\t\t<li><a href=\"#api_{proj.Item1.Namespace}_{proj.Item1.Name}\">{shortenedNS}{HttpUtility.HtmlEncode(proj.Item1.Definition)}</a></li>");
                    }
                }
                sb.AppendLine("\t\t\t</ul></li>");
            }

            if (mvcControllers.Any())
            {
                sb.AppendLine("\t\t<li>MVC Controllers")
                    .AppendLine("\t\t\t<ul>");


                foreach (var proj in mvcControllers)
                {
                    var skipMethods = new string[] { "Dispose", "Equals", "ToString", "GetType", "GetHashCode" };
                    var mvcMethods = proj.Item1.PublicMethods.Where(x => !skipMethods.Contains(x.Name));
                    if (mvcMethods.Any())
                    {
                        var shortenedNS = ShortenNS(proj.Item1.Namespace, proj.Item2.Name, " - ");
                        sb.AppendLine($"\t\t\t\t<li><a href=\"#mvc_{proj.Item1.Namespace}_{proj.Item1.Name}\">{shortenedNS}{HttpUtility.HtmlEncode(proj.Item1.Definition)}</a></li>");
                    }
                }
                sb.AppendLine("\t\t\t</ul></li>");
            }

            sb.AppendLine("\t</ul>")
                .AppendLine("</section>");
            sb.AppendLine("<div class=\"pagebreak\"></div>");
            return sb.ToString();
        }

        protected static string GenerateRouteReport(ApplicationAssemblyInfo data)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<section id=\"routes\" class=\"table\">")
                    .AppendLine($"<h2>Route Table</h2>");
            if (data.Routes?.Any() ?? false)
            {
                sb
                    .AppendLine($"\t<table>")
                    .AppendLine($"\t\t<thead>")
                    .AppendLine($"\t\t\t<tr>")
                    .AppendLine($"\t\t\t\t<th>Url</th><th>Handler</th><th>Tokens</th><th>Defaults</th>")
                    .AppendLine($"\t\t\t</tr>")
                    .AppendLine($"\t\t</thead>")
                    .AppendLine($"\t\t<tbody>");
                foreach (var route in data.Routes)
                {
                    sb.AppendLine($"\t\t\t<tr>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{route.Url}</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{route.Handler}</td>");
                    sb.AppendLine($"\t\t\t\t<td>{(route.DataTokens == null ? "&nbsp;" : string.Join(", ", route.DataTokens.Select(x => x.Key + "=" + (x.Value ?? "null"))))}</td>");
                    sb.AppendLine($"\t\t\t\t<td>{(route.Defaults == null ? "&nbsp;" : string.Join(", ", route.Defaults.Select(x => x.Key + "=" + (x.Value ?? "null"))))}</td>");
                    sb.AppendLine($"\t\t\t</tr>");
                }
                sb.AppendLine($"\t\t</tbody>")
                    .AppendLine($"\t</table>");
            }
            sb.AppendLine($"\t</section>");
            sb.AppendLine("<div class=\"pagebreak\"></div>");

            return sb.ToString();
        }

        protected static string GenerateActivityReport(List<(CustomTypeInfo, AssemblyTypeInfo)> activities, ApplicationAssemblyInfo data)
        {
            var sb = new StringBuilder();
            //var skipMethods = new string[] { "Dispose", "Equals", "ToString", "GetType", "GetHashCode", "ExecuteAsync" };
            //var apiMethods = ctrl.PublicMethods.Where(x => !skipMethods.Contains(x.Name));

            if (activities.Any())
            {
                sb.AppendLine($"<section id=\"activities\" class=\"table\">")
                    .AppendLine($"\t<h2>Activity List</h2>");
                sb
                    .AppendLine($"\t<table>")
                    .AppendLine($"\t\t<thead>")
                    .AppendLine($"\t\t\t<tr>")
                    .AppendLine($"\t\t\t\t<th>Group</th><th>Name</th><th>Inputs</th><th>Outputs</th>")
                    .AppendLine($"\t\t\t</tr>")
                    .AppendLine($"\t\t</thead>")
                    .AppendLine($"\t\t<tbody>");
                foreach (var (activity, ass) in activities)
                {

                    string verb = "Default";
                    //var httpAttr = activity.Attributes.Where(x => x.Name.StartsWith("RequiredArgument") && x.Name.EndsWith("Attribute")).FirstOrDefault();
                    //if (httpAttr != null) verb = httpAttr.Name.Substring(4, httpAttr.Name.Length - 13).ToString();
                    sb.AppendLine($"\t\t\t<tr>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{ShortenNS(activity.Namespace, ass.Name)}</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{HttpUtility.HtmlEncode(activity.Definition)}</td>");

                    var inputs = new List<ApiProperty>();
                    var outputs = new List<ApiProperty>();
                    foreach (var p in activity.PublicProperties)
                    {
                        if (p.PropertyType.Name.StartsWith("InOutArgument"))
                        {
                            inputs.Add(p);
                            outputs.Add(p);
                        }
                        else if (p.PropertyType.Name.StartsWith("OutArgument"))
                        {
                            outputs.Add(p);
                        }
                        else if (p.PropertyType.Name.StartsWith("InArgument"))
                        {
                            inputs.Add(p);
                        }
                    }

                    var funkify = new Func<ApiProperty, string>(p =>
                    {
                        var _sb = new StringBuilder().Append("<span class=\"data-type\">");
                        if (p.PropertyType?.GenericTypes?.Count == 1) _sb.Append(HttpUtility.HtmlEncode(p.PropertyType.GenericTypes[0].Definition));
                        else _sb.Append(p.PropertyType == null ? "void" : p.PropertyType.ToString());
                        _sb.Append("</span> ").Append(HttpUtility.HtmlEncode(p.Name));
                        return _sb.ToString();
                    });

                    sb.AppendLine($"\t\t\t\t<td>{string.Join("<br/>", inputs.Select(x => funkify(x)))}</td>");
                    sb.AppendLine($"\t\t\t\t<td>{string.Join("<br/>", outputs.Select(x => funkify(x)))}</td>");
                    sb.AppendLine($"\t\t\t</tr>");
                }
                sb.AppendLine($"\t\t</tbody>")
                    .AppendLine($"\t</table>");
                sb.AppendLine($"\t</section>");
                sb.AppendLine("<div class=\"pagebreak\"></div>");
            }

            return sb.ToString();
        }

        protected static string GenerateMvcControllerReport(CustomTypeInfo ctrl, AssemblyTypeInfo ass, ApplicationAssemblyInfo data)
        {
            var sb = new StringBuilder();

            var skipMethods = new string[] { "Dispose", "Equals", "ToString", "GetType", "GetHashCode" };
            var mvcMethods = ctrl.PublicMethods.Where(x => !skipMethods.Contains(x.Name));

            if (mvcMethods.Any())
            {
                sb.AppendLine($"<section id=\"mvc_{ctrl.Namespace}_{ctrl.Name}\" class=\"table\">")
                    .AppendLine($"\t<h2>{ctrl.Namespace} - {HttpUtility.HtmlEncode(ctrl.Definition)}</h2>");
                sb
                    .AppendLine($"\t<table>")
                    .AppendLine($"\t\t<thead>")
                    .AppendLine($"\t\t\t<tr>")
                    .AppendLine($"\t\t\t\t<th>Method</th><th>Name</th><th>Definition</th><th>Attributes</th>")
                    .AppendLine($"\t\t\t</tr>")
                    .AppendLine($"\t\t</thead>")
                    .AppendLine($"\t\t<tbody>");
                foreach (var mvcMethod in mvcMethods)
                {
                    //HttpGetAttribute 
                    string verb = "Default";
                    var httpAttr = mvcMethod.Attributes.Where(x => x.Name.StartsWith("Http") && x.Name.EndsWith("Attribute")).FirstOrDefault();
                    if (httpAttr != null) verb = httpAttr.Name.Substring(4, httpAttr.Name.Length - 13).ToString();
                    sb.AppendLine($"\t\t\t<tr>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{verb}</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{mvcMethod.Name}</td>");
                    sb.AppendLine($"\t\t\t\t<td>{HttpUtility.HtmlEncode(mvcMethod.Definition)}</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{string.Join(", ", mvcMethod.Attributes.Select(x => x.Name.EndsWith("Attribute") ? x.Name.Substring(0, x.Name.Length-9) : x.Name))}</td>");
                    sb.AppendLine($"\t\t\t</tr>");
                }
                sb.AppendLine($"\t\t</tbody>")
                    .AppendLine($"\t</table>");
                sb.AppendLine($"\t</section>");
            }

            return sb.ToString();
        }

        protected static string GenerateApiControllerReport(CustomTypeInfo ctrl, AssemblyTypeInfo ass, ApplicationAssemblyInfo data)
        {
            var sb = new StringBuilder();
            var skipMethods = new string[] { "Dispose", "Equals", "ToString", "GetType", "GetHashCode", "ExecuteAsync" };
            var apiMethods = ctrl.PublicMethods.Where(x => !skipMethods.Contains(x.Name));

            if (apiMethods.Any())
            {
                sb.AppendLine($"<section id=\"api_{ctrl.Namespace}_{ctrl.Name}\" class=\"table\">")
                    .AppendLine($"\t<h2>{ctrl.Namespace} - {HttpUtility.HtmlEncode(ctrl.Definition)}</h2>");
                sb
                    .AppendLine($"\t<table>")
                    .AppendLine($"\t\t<thead>")
                    .AppendLine($"\t\t\t<tr>")
                    .AppendLine($"\t\t\t\t<th>Method</th><th>Name</th><th>Definition</th><th>Attributes</th>")
                    .AppendLine($"\t\t\t</tr>")
                    .AppendLine($"\t\t</thead>")
                    .AppendLine($"\t\t<tbody>");
                foreach (var apiMethod in apiMethods)
                {
                    string verb = "Default";
                    var httpAttr = apiMethod.Attributes.Where(x => x.Name.StartsWith("Http") && x.Name.EndsWith("Attribute")).FirstOrDefault();
                    if (httpAttr != null) verb = httpAttr.Name.Substring(4, httpAttr.Name.Length - 13).ToString();
                    sb.AppendLine($"\t\t\t<tr>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{verb}</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{apiMethod.Name}</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{HttpUtility.HtmlEncode(apiMethod.Definition)}</td>");
                    sb.AppendLine($"\t\t\t\t<td class=\"nowrap\">{string.Join(", ", apiMethod.Attributes.Select(x => x.Name.EndsWith("Attribute") ? x.Name.Substring(0, x.Name.Length - 9) : x.Name))}</td>");
                    sb.AppendLine($"\t\t\t</tr>");
                }
                sb.AppendLine($"\t\t</tbody>")
                    .AppendLine($"\t</table>");
                sb.AppendLine($"\t</section>");
            }

            return sb.ToString();
        }

        private static string ShortenNS(string ns, string assemblyName, string suffix = null)
        {
            var shortenedNS = ns;
            if (shortenedNS.StartsWith(assemblyName)) shortenedNS = shortenedNS.Substring(assemblyName.Length).Trim('.');
            if (suffix != null && shortenedNS.Length > 0) shortenedNS += suffix;
            return shortenedNS;
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

        .data-type {
            font-style: italic;
            background-color: rgba(0,234,234,.1);
            color: darkgreen;
            padding-left: 4px;
            padding-right: 4px;
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
