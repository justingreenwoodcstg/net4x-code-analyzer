using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSTG.CodeAnalyzer.Model
{
    public class HarvestedData
    {
        public string ReportTitle { get; set; } = ".Net Solution";
        public List<SolutionFile> Solutions { get; set; } = new List<SolutionFile>();
        public List<ProjectFile> Projects { get; set; } = new List<ProjectFile>();
        //public List<AssemblyInfo> Assemblies { get; set; } = new List<AssemblyInfo>();
    }

    public class AssemblyReferenceFile
    {
        public bool InLibraries => (File?.FullName.Contains(@"\Libraries\") ?? false) || (File?.FullName.Contains(@"\Library\") ?? false);
        public bool InOutputDirectory => (File?.FullName.Contains(@"\bin\") ?? false);
        public bool InPackages => (File?.FullName.Contains(@"\packages\") ?? false);
        public bool HintIsValid { get; set; }
        [JsonIgnore]
        public Version Version { get; set; }
        [JsonProperty(propertyName: "Version")]
        public string VersionString
        {
            get => this.Version.ToString();
            set => this.Version = string.IsNullOrWhiteSpace(value) ? null : Version.Parse(value);
        }
        public FileInfo File { get; set; }
    }
    public class AssemblyInfo
    {
        public string Name { get; set; }
        public List<FileInfo> Files { get; set; }
    }

    public class SolutionFile
    {
        public FileInfo File { get; set; }
        public List<ProjectFileReference> Projects { get; set; } = new List<ProjectFileReference>();
    }

    public class ProjectFileReference
    {
        public string Name { get; set; }
        public string RelativePath { get; set; }
        public Guid ProjectId { get; set; }
        public FileInfo ProjectFile { get; set; }
    }

    public class ProjectFile
    {
        public FileInfo File { get; set; }
        public Guid ProjectId { get; set; }
        public string AssemblyName { get; set; }
        public string NameSpace { get; set; }
        public string FrameworkVersion { get; set; }
        public string OutputType { get; set; }
        public bool IsWebProject { get; set; } = false;
        public bool IsTestProject { get; set; } = false;
        public bool IsDatabaseProject => OutputType?.ToLower() == "database";
        public bool IsReportProject => OutputType?.ToLower() == "report";

        public string ProjectType => IsWebProject ? "Web" : (IsTestProject ? "UnitTest" : OutputType);

        public List<FileInfo> SolutionFiles { get; set; } = new List<FileInfo>();

        public List<NugetPackage> NugetPackages { get; set; } = new List<NugetPackage>();
        public List<ProjectReference> ProjectReferences { get; set; } = new List<ProjectReference>();
        public List<AssemblyReference> AssemblyReferences { get; set; } = new List<AssemblyReference>();
        public List<ConfigFile> ConfigFiles { get; set; } = new List<ConfigFile>();
        public List<ProjectItemTypeInfo> IncludedFileTypes { get; set; } = new List<ProjectItemTypeInfo>();
        public List<string> MissingFiles { get; set; } = new List<String>();
    }
    public enum FileClassificationEnum
    {
        Unknown = 0,
        XML,
        JSON,
        XAML,
        HTML,
        WSDL,
        XMLSchema,
        JavaScript,
        CSharp,
        CSharpRazorTemplate,
        RastorGraphics,
        VectorGraphics,
        Icon,
        StyleSheet,
        CompiledExecutable,
        CompiledLibrary,
        ShockwaveFlash,
        MicrosoftInstallerPackage,
        CompiledHtmlHelp,
        ConfigFile,
        SettingsFile,
        Document,
        XmlPublishProfile,
        Text,
        SQL,
        ASPNetWebForm,
        ASPNetApplication,
        Font,
        MultiMedia,
        Resource,
        SourceMap,
        PerpetuumSoftReport,
        SSRSReport,
        SSRSDataSource,
        SSRSDataSet,
        EntityDataModel,        
        StoreSchemaDefinitionLanguage,
        MappingSpecificationLanguage,
        ConceptualSchemaDefinitionLanguage,

        CenuityReport,  //crpt
        CenuityMTC, //cmtc
        CenuityWML, //cwml
        CenuityMeta, //cmeta
        CenuityEntity, //cent
        CenuityORM, //cormd
    }
    public class ProjectItemTypeInfo
    {
        public FileClassificationEnum Classification { get; set; } = FileClassificationEnum.Unknown;
        public List<string> FileExtentions { get; set; } = new List<string>();
        public int Count { get; set; } = 0;
        public long SizeInBytes { get; set; } = 0;
        public long? Lines { get; set; }
        public long? EmptyLines { get; set; }
    }
    public class NugetPackage
    {
        public string Id { get; set; }
        [JsonIgnore]
        public Version Version { get; set; }
        [JsonProperty(propertyName:"Version")]
        public string VersionString
        {
            get => this.Version?.ToString();
            set => this.Version = string.IsNullOrWhiteSpace(value) ? null : Version.Parse(value);
        }
        public string TargetFramework { get; set; }
        public NugetPackageVersion VersionDetails { get; set; }
        public NugetPackageVersion LatestVersionDetails { get; set; }
        public bool IsLatestVersion => VersionDetails != null && LatestVersionDetails != null && LatestVersionDetails.Version == VersionDetails.Version;
    }

    public class NugetPackageVersion
    {
        public string Summary { get; set; }
        public string Description { get; set; }
        [JsonIgnore]
        public Version Version { get; set; }
        [JsonProperty(propertyName: "Version")]
        public string VersionString
        {
            get => this.Version.ToString();
            set => this.Version = string.IsNullOrWhiteSpace(value) ? null : Version.Parse(value);
        }
        public string Title { get; set; }
        public string Authors { get; set; }
        public string PackageUrl { get; set; }
        public string ProjectUrl { get; set; }
        public string Tags { get; set; }
        public DateTimeOffset? DatePublished { get; set; }
    }
    public class ProjectReference
    {
        public string Name { get; set; }
        public string RelativePath { get; set; }
        public Guid ProjectId { get; set; }
    }

    public class AssemblyReference
    {
        public AssemblyReference(string refString, string hintPath = null)
        {
            var parts = (refString ?? "").Split(',');

            if (parts.Length > 1)
            {
                this.Name = parts[0];
                for (var i = 1; i < parts.Length; i++)
                {
                    var nv = parts[i].Trim().Split('=');
                    if (nv.Length == 2)
                    {
                        switch (nv[0].Trim().ToLower())
                        {
                            case "version": this.Version = Version.Parse(nv[1].Trim()); break;
                            case "culture": this.Culture = nv[1].Trim(); break;
                            case "publickeytoken": this.PublicKeyToken = nv[1].Trim(); break;
                            case "processorarchitecture": this.ProcessorArchitecture = nv[1].Trim(); break;
                        }
                    }
                }
            }
            else
            {
                this.Name = refString;
            }
            HintPath = hintPath;
        }
        public string Name { get; set; }
        public string HintPath { get; set; }
        [JsonIgnore]
        public Version Version { get; set; }
        [JsonProperty(propertyName: "Version")]
        public string VersionString {
            get => this.Version?.ToString();
            set => this.Version = string.IsNullOrWhiteSpace(value) ? null : Version.Parse(value);
        }
        public string Culture { get; set; }
        public string ProcessorArchitecture { get; set; }
        public string PublicKeyToken { get; set; }
        public string PackageId { get; set; }
        public AssemblyReferenceFile FileLocation { get; set; } 

    }

    public class ConfigFile
    {
        public FileInfo File { get; set; }
        public List<NameValuePair> ConnectionStrings { get; set; } = new List<NameValuePair>();
        public List<NameValuePair> Settings { get; set; } = new List<NameValuePair>();
        public List<NameValuePair> ServiceModelEndpoints { get; set; } = new List<NameValuePair>();
        public List<NameValuePair> MailSettings { get; set; } = new List<NameValuePair>();
    }

    public class NameValuePair
    {
        public NameValuePair(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
