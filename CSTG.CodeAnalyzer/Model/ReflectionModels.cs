using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSTG.CodeAnalyzer.Model.Reflection
{
    public class ApplicationAssemblyInfo
    {
        public string ReportTitle { get; set; }
        public List<AssemblyTypeInfo> Assemblies { get; set; }
        public List<AppRouteInfo> Routes { get; set; }
    }

    public class AssemblyTypeInfo
    {
        public string Name { get; set; }
        public string CodeBase { get; set; }
        public List<CustomTypeInfo> Types { get; set; }
    }

    public class CustomTypeBaseInfo
    {
        public string AssemblyName { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string FullName => $"{(string.IsNullOrWhiteSpace(Namespace) ? (Namespace + ".") : "")}{Name}";
        public List<CustomTypeBaseInfo> GenericTypes { get; set; }

        public string Definition
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(Name.Substring(0, Name.IndexOf('`') >= 0 ? Name.IndexOf('`') : Name.Length));
                if ((GenericTypes?.Count ?? 0) > 0)
                {
                    sb.Append("<");
                    for (var i = 0; i < GenericTypes.Count; i++)
                    {
                        var gt = GenericTypes[i];
                        if (i > 0) sb.Append(", ");
                        sb.Append(gt.ToString());
                    }
                    sb.Append(">");
                }
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            return Definition;
        }
    }

    public class AppRouteInfo
    {
        public string Url { get; set; }
        public string Handler { get; set; }

        public List<KeyValuePair<string, object>> DataTokens { get; set; }
        public List<KeyValuePair<string, object>> Defaults { get; set; }
    }


    public class CustomTypeInfo : CustomTypeBaseInfo
    {
        public CustomTypeInfo BaseType { get; set; }
        public bool IsApiController { get; set; }
        public bool IsMvcController { get; set; }
        public List<ApiMethod> PublicMethods { get; set; }
        public List<ApiProperty> PublicProperties { get; set; }
        public List<CustomAttributeInfo> Attributes { get; set; }
        [JsonIgnore]
        public bool IsActivity 
        { 
            get
            {
                var bt = this.BaseType;
                while (bt != null)
                {
                    if (bt.Name == "Activity" && bt.Namespace == "System.Activities") return true;
                    bt = bt.BaseType;
                }
                return false;
            }
        }
    }

    public class ApiMethod
    {
        public string Name { get; set; }
        public CustomTypeBaseInfo ReturnType { get; set; }
        public List<KeyValuePair<string, CustomTypeBaseInfo>> Parameters { get; set; }
        public List<CustomAttributeInfo> Attributes { get; set; }
        public List<CustomTypeBaseInfo> GenericTypes { get; set; }
        public bool HasHttpAttributes { get; set; }
        public bool HasMvcAttributes { get; set; }

        public string Definition
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(ReturnType == null ? "void" : ReturnType.ToString()).Append(" ");
                sb.Append(Name.Substring(0, Name.IndexOf('`') >= 0 ? Name.IndexOf('`') : Name.Length));
                if ((GenericTypes?.Count ?? 0) > 0)
                {
                    sb.Append("<");
                    for (var i = 0; i < GenericTypes.Count; i++)
                    {
                        var gt = GenericTypes[i];
                        if (i > 0) sb.Append(", ");
                        sb.Append(gt.ToString());
                    }
                    sb.Append(">");
                }
                sb.Append("(");
                if ((this.Parameters?.Count ?? 0) > 0)
                {
                    for (var i = 0; i < Parameters.Count; i++)
                    {
                        var gt = Parameters[i];
                        if (i > 0) sb.Append(", ");
                        sb.Append(gt.Value.ToString() + " " + gt.Key);
                    }
                }
                sb.Append(")");
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            return Definition;
        }
    }
    public class ApiProperty
    {
        public string Name { get; set; }
        public CustomTypeBaseInfo PropertyType { get; set; }
        public List<CustomAttributeInfo> Attributes { get; set; }
        public bool ReadOnly { get; set; }
        public string Definition
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(PropertyType == null ? "void" : PropertyType.ToString()).Append(" ");
                sb.Append(Name);
                if (ReadOnly) sb.Append(" { get; }");
                else sb.Append(" { get; set; }");
                return sb.ToString();
            }
        }
        public string ShortDefinition
        {
            get
            {
                var sb = new StringBuilder();
                if (PropertyType.GenericTypes.Count == 1)
                sb.Append(PropertyType == null ? "void" : PropertyType.ToString()).Append(" ");
                sb.Append(Name);
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            return Definition;
        }
    }

    public class CustomAttributeInfo : CustomTypeBaseInfo
    {
        public object AttributeInstance { get; set; }
    }
}
