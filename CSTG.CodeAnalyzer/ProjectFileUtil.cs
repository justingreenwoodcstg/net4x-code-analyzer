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

            /*
                     <WebProjectProperties>
          <UseIIS>False</UseIIS>
          <AutoAssignPort>True</AutoAssignPort>
          <DevelopmentServerPort>49694</DevelopmentServerPort>
          <DevelopmentServerVPath>/</DevelopmentServerVPath>
          <IISUrl>http://localhost:49694/</IISUrl>
             */

            if (projectIds.Count == 1) { Console.WriteLine("\t" + projectIds[0].InnerText); projectFile.ProjectId = new Guid(projectIds[0].InnerText); }
            if (outputTypes.Count == 1) { Console.WriteLine("\t" + outputTypes[0].InnerText); projectFile.OutputType = outputTypes[0].InnerText; }
            if (rootNamespaces.Count == 1) { Console.WriteLine("\t" + rootNamespaces[0].InnerText); projectFile.NameSpace = rootNamespaces[0].InnerText; }
            if (assemblyNames.Count == 1) { Console.WriteLine("\t" + assemblyNames[0].InnerText); projectFile.AssemblyName = assemblyNames[0].InnerText; }
            if (targetFrameworkVersions.Count == 1) { Console.WriteLine("\t" + targetFrameworkVersions[0].InnerText); projectFile.FrameworkVersion = targetFrameworkVersions[0].InnerText; }
            if (useIISExpress.Count == 1) { Console.WriteLine("\t" + useIISExpress[0].InnerText); projectFile.IsWebProject = useIISExpress[0].InnerText.ToLower() == "true"; }

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
    }
}
