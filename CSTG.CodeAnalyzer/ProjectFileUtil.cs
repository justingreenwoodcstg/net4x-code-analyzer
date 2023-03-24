using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using CSTG.CodeAnalyzer.Model;
using static System.Net.Mime.MediaTypeNames;

namespace CSTG.CodeAnalyzer
{
    public class ProjectFileUtil
    {
        public static ProjectFile Read(FileInfo projFileInfo)
        {

            var projectFile = new ProjectFile
            {
                File = projFileInfo
            };
            Console.WriteLine(projFileInfo.Name);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(projFileInfo.FullName);
            XmlNodeList projectIds = xmlDoc.GetElementsByTagName("ProjectGuid");
            XmlNodeList outputTypes = xmlDoc.GetElementsByTagName("OutputType");
            XmlNodeList rootNamespaces = xmlDoc.GetElementsByTagName("RootNamespace");
            XmlNodeList assemblyNames = xmlDoc.GetElementsByTagName("AssemblyName");
            XmlNodeList targetFrameworkVersions = xmlDoc.GetElementsByTagName("TargetFrameworkVersion");
            XmlNodeList refs = xmlDoc.GetElementsByTagName("Reference");
            XmlNodeList projectRefs = xmlDoc.GetElementsByTagName("ProjectReference");
            XmlNodeList contents = xmlDoc.GetElementsByTagName("Content");
            XmlNodeList nones = xmlDoc.GetElementsByTagName("None");
            XmlNodeList useIISExpress = xmlDoc.GetElementsByTagName("UseIISExpress");
            XmlNodeList testProjType = xmlDoc.GetElementsByTagName("TestProjectType");

            ////EmbeddedResource  None Content Compile
            var nodes = new List<XmlNode>(xmlDoc.GetElementsByTagName("EmbeddedResource").OfType<XmlNode>());
            nodes.AddRange(xmlDoc.GetElementsByTagName("EmbeddedResource").OfType<XmlNode>());
            nodes.AddRange(xmlDoc.GetElementsByTagName("Content").OfType<XmlNode>());
            nodes.AddRange(xmlDoc.GetElementsByTagName("Compile").OfType<XmlNode>());
            nodes.AddRange(xmlDoc.GetElementsByTagName("Build").OfType<XmlNode>());
            foreach (var item in nodes)
            {
                var filename = item.Attributes["Include"].Value;
                var fileInfo = new FileInfo(Path.Combine(projFileInfo.Directory.FullName, HttpUtility.UrlDecode(filename)));
                if (!fileInfo.Exists)
                {
                    projectFile.MissingFiles.Add(filename);
                    continue;
                }
                var ftype = FileClassificationEnum.Unknown;
                var ext = fileInfo.Extension.ToLower();
                var isText = true;
                switch (ext)
                {
                    case ".sql": ftype = FileClassificationEnum.SQL; break;
                    case ".xml": ftype = FileClassificationEnum.XML; break;
                    case ".json": ftype = FileClassificationEnum.JSON; break;
                    case ".xaml": ftype = FileClassificationEnum.XAML; break;
                    case ".html": case ".htm": ftype = FileClassificationEnum.HTML; break;
                    case ".wsdl": ftype = FileClassificationEnum.WSDL; break;
                    case ".xsd": ftype = FileClassificationEnum.XMLSchema; break;
                    case ".js": ftype = FileClassificationEnum.JavaScript; break;
                    case ".cs": ftype = FileClassificationEnum.CSharp; break;
                    case ".cshtml": ftype = FileClassificationEnum.CSharpRazorTemplate; break;
                    case ".png": case ".jpg": case ".jpeg": case ".gif": case ".bmp": 
                        ftype = FileClassificationEnum.RastorGraphics; isText = false; break;
                    case ".svg": ftype = FileClassificationEnum.VectorGraphics; isText = false; break;
                    case ".ico": ftype = FileClassificationEnum.Icon; isText = false; break;
                    case ".exe": ftype = FileClassificationEnum.CompiledExecutable; isText = false; break;
                    case ".dll": ftype = FileClassificationEnum.CompiledLibrary; isText = false; break;
                    case ".css": case ".scss": case ".sass": ftype = FileClassificationEnum.StyleSheet; break;
                    case ".config": ftype = FileClassificationEnum.ConfigFile; break;
                    case ".doc": case ".pdf": case ".md": case ".xls": case ".xlsx": case ".docx": ftype = FileClassificationEnum.Document; isText = false; break;
                    case ".txt": ftype = FileClassificationEnum.Text; break;
                    case ".aspx": ftype = FileClassificationEnum.ASPNetWebForm; break;
                    case ".asax": ftype = FileClassificationEnum.ASPNetApplication; break;
                    case ".woff": case ".ttf": case ".eot": case ".woff2": case ".otf": ftype = FileClassificationEnum.Font; isText = false; break;
                    case ".map": ftype = FileClassificationEnum.SourceMap; break;
                    case ".ogg": case ".mp3": case ".mp4": case ".wav": case ".webm": ftype = FileClassificationEnum.MultiMedia; isText = false; break;
                    case ".resource": case ".resx": ftype = FileClassificationEnum.Resource; break;
                    case ".rst": ftype = FileClassificationEnum.PerpetuumSoftReport; break;

                    case ".crpt": ftype = FileClassificationEnum.CenuityReport; break;
                    case ".cmtc": ftype = FileClassificationEnum.CenuityMTC; break;
                    case ".cwml": ftype = FileClassificationEnum.CenuityWML; break;
                    case ".cmeta": ftype = FileClassificationEnum.CenuityMeta; break;
                    case ".cent": ftype = FileClassificationEnum.CenuityEntity; break;
                    case ".cormd": ftype = FileClassificationEnum.CenuityORM; break;
                    default: isText = false; break;
                }
                //.woff .ttf .eot .woff2 .otf
                //asax
                //.mp4 .aspx .ogg .webm .woff .ttf .eot .woff2 .otf .db .map .asax .ctheme
                //.rst .crpt
                //.resource .resx
                var pft = projectFile.IncludedFileTypes.Where(x => x.Classification == ftype).FirstOrDefault();
                if (pft == null) projectFile.IncludedFileTypes.Add(pft = new ProjectItemTypeInfo { Classification = ftype });
                if (!pft.FileExtentions.Contains(ext)) pft.FileExtentions.Add(ext);
                pft.Count++;
                pft.SizeInBytes += fileInfo.Length;
                var lineData = GetFileLineCount(fileInfo);
                if (isText) pft.Lines = (pft.Lines ?? 0) + lineData.Item1;
                if (isText) pft.EmptyLines = (pft.EmptyLines ?? 0) + lineData.Item2;
            }

            if (projectIds.Count == 1) { Console.WriteLine("\t" + projectIds[0].InnerText); projectFile.ProjectId = new Guid(projectIds[0].InnerText); }
            if (outputTypes.Count == 1) { Console.WriteLine("\t" + outputTypes[0].InnerText); projectFile.OutputType = outputTypes[0].InnerText; }
            if (rootNamespaces.Count == 1) { Console.WriteLine("\t" + rootNamespaces[0].InnerText); projectFile.NameSpace = rootNamespaces[0].InnerText; }
            if (assemblyNames.Count == 1) { Console.WriteLine("\t" + assemblyNames[0].InnerText); projectFile.AssemblyName = assemblyNames[0].InnerText; }
            if (targetFrameworkVersions.Count == 1) { Console.WriteLine("\t" + targetFrameworkVersions[0].InnerText); projectFile.FrameworkVersion = targetFrameworkVersions[0].InnerText; }
            if (useIISExpress.Count == 1) { Console.WriteLine("\t" + useIISExpress[0].InnerText); projectFile.IsWebProject = useIISExpress[0].InnerText.ToLower() == "true"; }
            if (testProjType.Count >= 1) { Console.WriteLine("\t" + testProjType[0].InnerText); projectFile.IsTestProject = testProjType[0].InnerText.ToLower() == "unittest"; }

            FileInfo packagesFile = null;
            foreach (XmlNode contentNode in contents)
            {
                if (contentNode.Attributes["Include"].Value.EndsWith("packages.config"))
                {
                    packagesFile = new FileInfo(Path.Combine(projFileInfo.Directory.FullName, contentNode.Attributes["Include"].Value));
                }
            }
            if (packagesFile == null)
            {
                foreach (XmlNode noneNode in nones)
                {
                    if (noneNode.Attributes["Include"].Value.EndsWith("packages.config"))
                    {
                        packagesFile = new FileInfo(Path.Combine(projFileInfo.Directory.FullName, noneNode.Attributes["Include"].Value));
                    }
                }
            }

            if (packagesFile?.Exists ?? false)
            {
                //https://devblogs.microsoft.com/nuget/play-with-packages/1
                // get the packages!!!
                var packagesXml = new XmlDocument();
                packagesXml.Load(packagesFile.FullName);
                XmlNodeList packages = packagesXml.GetElementsByTagName("package");
                foreach (XmlNode p in packages)
                {
                    projectFile.NugetPackages.Add(new NugetPackage
                    {
                        Id = p.Attributes["id"].Value,
                        Version = Version.Parse(p.Attributes["version"]?.Value),
                        TargetFramework = p.Attributes["targetFramework"]?.Value,
                    });
                    Console.WriteLine("\t P-> " + p.Attributes["id"].Value);
                }
                //id="Microsoft.jQuery.Unobtrusive.Validation" version="3.0.0" targetFramework="net45"
            }

            if (refs.Count > 0)
            {
                foreach (XmlNode r in refs)
                {
                    Console.WriteLine("\t => " + r.Attributes["Include"].Value);
                    string hintPath = null;
                    foreach (XmlNode chnode in r.ChildNodes)
                    {
                        if (chnode.Name == "HintPath") hintPath = chnode.InnerText;
                    }
                    var aref = new AssemblyReference(r.Attributes["Include"].Value, hintPath);
                    projectFile.AssemblyReferences.Add(aref);
                }
            }
            if (projectRefs.Count > 0)
            {
                foreach (XmlNode r in projectRefs)
                {
                    Console.WriteLine("\t +> " + r.Attributes["Include"].Value);
                    var pref = new ProjectReference
                    {
                        RelativePath = r.Attributes["Include"].Value
                    };
                    foreach (XmlNode chnode in r.ChildNodes)
                    {
                        if (chnode.Name == "Project") pref.ProjectId = new Guid(chnode.InnerText);
                        else if (chnode.Name == "Name") pref.Name = chnode.InnerText;
                    }
                    projectFile.ProjectReferences.Add(pref);
                }
            }

            return projectFile;
        }

        public static (long, long) GetFileLineCount(FileInfo f)
        {
            long lineCount = 0, empty = 0;
            using (var sr = new StreamReader(f.OpenRead()))
            {
                string line;
                while ((line = sr.ReadLine()) != null) 
                {
                    if (string.IsNullOrWhiteSpace(line)) empty++; else lineCount++;
                }
            }
            return (lineCount, empty);
        }
    }
}
