using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;

namespace CSTG.Net4xTypeInspector
{

    public static class ReflectionThingy
    {
        private const string JustinsWorkDir = @"C:\git-repos\net4x-code-analyzer\CSTG.CodeAnalyzer\bin\Debug";
        public static void RipIt(Func<object, string> serializer, List<Route> routes = null)
        {
            Debug.WriteLine("");
            Debug.WriteLine("-=-=-=-=--=-=-=-=--=-=-=-=--=-=-=-=--=-=-=-=--=-=-=-=--=-=-=-=--=-=-=-=--=-=-=-=--=-=-=-=-");
            var execAss = Assembly.GetCallingAssembly();
            var thisAss = Assembly.GetExecutingAssembly();
            Debug.WriteLine($"-=-=-=-=- {execAss.GetName().Name}\t{execAss.GetName().CodeBase}");

            var assemblies = new List<Assembly> { execAss };
            assemblies.Insert(0, execAss);
            foreach (var refAss in execAss.GetReferencedAssemblies())
            {
                var fullAss = Assembly.Load(refAss);
                if (fullAss.GetName().GetPublicKeyToken().Length == 0 && fullAss != thisAss)
                {
                    assemblies.Add(fullAss);
                }
            }

            var output = new ApplicationAssemblyInfo
            {
                Assemblies = new List<AssemblyTypeInfo>()
            };

            foreach (var ass in assemblies) output.Assemblies.Add( new AssemblyTypeInfo { Name = ass.GetName().Name, CodeBase = execAss.GetName().CodeBase, Types = CollectTypes(ass) });

            if (routes != null)
            {
                output.Routes = new List<AppRouteInfo>();
                foreach (var route in routes)
                {
                    var r = new AppRouteInfo
                    {
                        Url = route.Url,
                        Handler = route.RouteHandler?.GetType().Name,
                    };
                    output.Routes.Add(r);
                    if (route.DataTokens != null && route.DataTokens.Count > 0)
                    {
                        r.DataTokens = new List<KeyValuePair<string, object>>();
                        foreach (var kvp in route.DataTokens) { r.DataTokens.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value)); }
                    }
                    if (route.Defaults != null && route.Defaults.Count > 0)
                    {
                        r.Defaults = new List<KeyValuePair<string, object>>();
                        foreach (var kvp in route.Defaults) { r.Defaults.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value)); }
                    }
                }
                //output.Routes = new List<>();
            }
            var f = new FileInfo(thisAss.GetName().CodeBase.Substring(8));
            File.WriteAllText(Path.Combine(f.Directory.FullName, $"{execAss.GetName().Name}.type-details.json"), serializer(output));
            if (Directory.Exists(JustinsWorkDir))
                File.WriteAllText(Path.Combine(JustinsWorkDir, $"{execAss.GetName().Name}.type-details.json"), serializer(output));
        }
        private static List<CustomTypeInfo> CollectTypes(Assembly ass)
        {
            var customTypes = new List<CustomTypeInfo>();
            Debug.WriteLine($"-=-=-=-=- {ass.GetName().Name}\t{ass.GetName().CodeBase}");
            foreach (var type in ass.DefinedTypes)
            {
                var customType = GetCustomType(type);
                if (customType != null)
                {
                    customTypes.Add(customType);
                }
            }
            return customTypes;
        }
        private static CustomTypeInfo GetCustomType(Type type)
        {
            CustomTypeInfo rType = null;
            if (type.CustomAttributes.Any(x => x.AttributeType == typeof(CompilerGeneratedAttribute) || x.AttributeType == typeof(GeneratedCodeAttribute))) return null;
            if (type.Name.StartsWith("_") || type.Name.StartsWith("<"))
            {
                var x = type.Name;
            }
            else
            {
                var ptype = type;
                var parents = new List<Type>();
                while (ptype.BaseType != null && ptype.BaseType != typeof(System.Object))
                {
                    ptype = ptype.BaseType;
                    parents.Add(ptype);
                }

                var typeInfo = type.GetTypeInfo();
                rType = GetCustomTypeBaseInfo(type, rType);
                rType.BaseType = parents.Any() ? GetCustomType(type.BaseType) : null;

                
                if (type.IsClass)
                {
                    rType.IsApiController = parents.Any(x => x.FullName?.Contains("System.Web.Http.ApiController") ?? false);
                    rType.IsMvcController = parents.Any(x => x.FullName?.Contains("System.Web.Mvc.Controller") ?? false);

                    rType.PublicMethods = new List<ApiMethod>();
                    foreach (var method in type.GetMethods())
                    {
                        if (method.MemberType == MemberTypes.Method && !method.IsSpecialName && method.IsPublic)
                        {
                            var m = GetApiMethod(method);
                            if (m != null)
                            {
                                rType.PublicMethods.Add(m);
                                m.HasMvcAttributes = (m.Attributes?.Any(x => x.FullName.IndexOf("Mvc") >= 0) ?? false);
                                m.HasHttpAttributes = (m.Attributes?.Any(x => x.FullName.IndexOf("Http") >= 0) ?? false);
                            }
                        }
                    }

                    foreach (var prop in type.GetProperties())
                    {
                        rType.PublicProperties = rType.PublicProperties ?? new List<ApiProperty>();
                        if (prop.CanRead && !prop.GetMethod.IsAbstract && prop.GetMethod.IsPublic)
                        {
                            var p = GetProperty(prop);
                            if (p != null) rType.PublicProperties.Add(p);
                        }
                    }
                }
            }
            return rType;
        }

        private static ApiProperty GetProperty(PropertyInfo prop)
        {
            ApiProperty p = null;
            if (!prop.GetMethod.IsAbstract && prop.GetMethod.IsPublic)
            {
                p = new ApiProperty
                {
                    Name = prop.Name,
                    PropertyType = GetCustomTypeBaseInfo<CustomTypeBaseInfo>(prop.PropertyType),
                    Attributes = new List<CustomAttributeInfo>(),
                    ReadOnly = !prop.CanWrite
                    
                };
                foreach (var cattr in prop.GetCustomAttributes())
                {
                    var attrType = cattr.GetType();
                    if (attrType.Name.StartsWith("_") || attrType.Name.StartsWith("<")) continue;

                    var attrInfo = GetCustomTypeBaseInfo<CustomAttributeInfo>(attrType);
                    
                    //TODO: We need to get the instance here... like the data that goes into the attribute.
                    attrInfo.AttributeInstance = cattr?.ToString();
                    p.Attributes.Add(attrInfo);
                    foreach (var data in attrType.GetCustomAttributesData())
                    {
                        var x = data.NamedArguments;
                    }
                }
            }
            return p;
        }

        private static ApiMethod GetApiMethod(MethodInfo method)
        {
            ApiMethod m = null;
            if (method.IsPublic && !method.IsAbstract)
            {
                m = new ApiMethod
                {
                    Name = method.Name,
                    ReturnType = GetCustomTypeBaseInfo<CustomTypeBaseInfo>(method.ReturnType),
                    Parameters = new List<KeyValuePair<string, CustomTypeBaseInfo>>(),
                    Attributes = new List<CustomAttributeInfo>()
                };
                if (method.ContainsGenericParameters)
                {
                    m.GenericTypes = new List<CustomTypeBaseInfo>();
                    foreach (var gtype in method.GetGenericArguments())
                    {
                        m.GenericTypes.Add(GetCustomTypeBaseInfo<CustomTypeBaseInfo>(gtype));
                    }
                }
                foreach (var param in method.GetParameters())
                {
                    //TODO: add in/out/ref/default
                    m.Parameters.Add(new KeyValuePair<string, CustomTypeBaseInfo>(param.Name, GetCustomTypeBaseInfo<CustomTypeBaseInfo>(param.ParameterType)));
                }

                try
                {
                    foreach (var cattr in method.GetCustomAttributes())
                    {
                        //TODO: add other stuff
                        var attrType = cattr.GetType();
                        if (attrType.Name.StartsWith("_") || attrType.Name.StartsWith("<")) continue;

                        var attrInfo = GetCustomTypeBaseInfo<CustomAttributeInfo>(attrType);
                        attrInfo.AttributeInstance = cattr;
                        m.Attributes.Add(attrInfo);
                        foreach (var data in attrType.GetCustomAttributesData())
                        {
                            var x = data.NamedArguments;
                        }
                    }
                }
                catch { }
            }
            return m;
        }
        private static T GetCustomTypeBaseInfo<T>(Type type, T o = null) where T : CustomTypeBaseInfo, new()
        {
            o = o ?? new T();
            o.Name = type.Name;
            o.Namespace = type.Namespace;
            o.AssemblyName = type.Assembly.GetName().Name;
            if (type.IsGenericType)
            {
                o.GenericTypes = new List<CustomTypeBaseInfo>();
                foreach (var gtype in type.GenericTypeArguments)
                {
                    o.GenericTypes.Add(GetCustomTypeBaseInfo<CustomTypeBaseInfo>(gtype));
                }
            }
            return o;
        }
    }
}
