#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.Assertions;

namespace Zenject
{
    public static class ReflectionBaker
    {
        public static bool GenerateCode(string constructorPath)
        {
            var typeDict = AnalyzeAllTypes();

            // Group types by assembly.
            var assemblyDict = new SortedList<Assembly, List<TypeInfo>>(new AssemblyComparer());
            foreach (var typeInfo in typeDict.Values)
            {
                var assembly = typeInfo.Type.Assembly;

                // Add the type to the assembly.
                if (assemblyDict.TryGetValue(assembly, out var typeList) == false)
                {
                    typeList = new List<TypeInfo>();
                    assemblyDict.Add(assembly, typeList);
                }
                typeList.Add(typeInfo);
            }

            // Sort typeInfos by name.
            foreach (var typeList in assemblyDict.Values)
                typeList.Sort((a, b) => string.Compare(a.Type.FullName, b.Type.FullName, StringComparison.Ordinal));

            // Generate injectables.
            var dirty = false;
            foreach (var (assembly, typeInfos) in assemblyDict)
            {
                var assemblyName = assembly.GetName().Name;
                L.I("Generating code for assembly: " + assemblyName);
                var newContent = GenerateCode_Injectable(typeInfos);
                var rootFound = TryGetCorrespondingRootForAssembly(assemblyName, out var rootDir);
                Assert.IsTrue(rootFound, $"Root directory for assembly '{assemblyName}' not found");
                dirty |= CompareAndWrite(rootDir + "Zenject_CodeGen.cs", newContent);
            }

            // Generate constructors.
            {
                var newContent = GenerateCode_Constructors(assemblyDict);
                dirty |= CompareAndWrite(constructorPath, newContent);
            }

            return dirty;

            static bool CompareAndWrite(string path, string content)
            {
                var orgContent = File.Exists(path) ? File.ReadAllText(path) : null;
                if (orgContent == content) return false;
                File.WriteAllText(path, content);
                return true;
            }
        }

        private static readonly Dictionary<string, string> _assemblyNameToDir = new();

        private static bool TryGetCorrespondingRootForAssembly(string assemblyName, out string rootDir)
        {
            if (_assemblyNameToDir.Count is 0) SetUpAssemblyNameToDir(_assemblyNameToDir);
            return _assemblyNameToDir.TryGetValue(assemblyName, out rootDir);

            static void SetUpAssemblyNameToDir(Dictionary<string, string> map)
            {
                // These are the default assembly names that Unity creates.
                map["Assembly-CSharp-firstpass"] = "Assets/Plugins/";
                map["Assembly-CSharp-Editor-firstpass"] = "Assets/Plugins/Editor/";
                map["Assembly-CSharp"] = "Assets/";
                map["Assembly-CSharp-Editor"] = "Assets/Editor/";

                // Find all asmdefs and add them to the dictionary.
                var asmdefGUIDs = AssetDatabase.FindAssets("t:asmdef");
                foreach (var asmdefGUID in asmdefGUIDs)
                {
                    var asmdefPath = AssetDatabase.GUIDToAssetPath(asmdefGUID);
                    var delimiterIndex = asmdefPath.LastIndexOf('/');
                    var asmdefName = asmdefPath[(delimiterIndex + 1)..^".asmdef".Length];
                    var asmdefDir = asmdefPath[..(delimiterIndex + 1)];
                    map.Add(asmdefName, asmdefDir);
                }
            }
        }

        private static Dictionary<Type, TypeInfo> AnalyzeAllTypes()
        {
            var typeDict = new Dictionary<Type, TypeInfo>();

            var constructors = TypeCache.GetMethodsWithAttribute<InjectConstructorAttribute>();
            foreach (var methodInfo in constructors)
            {
                var type = methodInfo.DeclaringType;
                if (ShouldIgnoreType(type)) continue; // Skip if the type is marked with NoReflectionBaking.

                var typeInfo = GetTypeInfo(type, typeDict);
                typeInfo.Constructor = methodInfo;
            }

            foreach (var methodInfo in TypeCache.GetMethodsWithAttribute<InjectMethodAttribute>())
            {
                var type = methodInfo.DeclaringType;
                if (ShouldIgnoreType(type)) continue; // Skip if the type is marked with NoReflectionBaking.

                var typeInfo = GetTypeInfo(type, typeDict);
                Assert.IsNull(typeInfo.Method);
                typeInfo.Method = methodInfo;
            }

            foreach (var fieldInfo in TypeCache.GetFieldsWithAttribute<InjectAttributeBase>())
            {
                var type = fieldInfo.DeclaringType;
                if (ShouldIgnoreType(type)) continue; // Skip if the type is marked with NoReflectionBaking.

                var typeInfo = GetTypeInfo(type, typeDict);
                typeInfo.Fields ??= new List<FieldInfo>();
                typeInfo.Fields.Add(fieldInfo);
            }

            // Sort FieldInfos by name.
            foreach (var typeInfo in typeDict.Values)
                typeInfo.Fields?.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            return typeDict;

            static TypeInfo GetTypeInfo(Type type, Dictionary<Type, TypeInfo> typeDict)
            {
                if (typeDict.TryGetValue(type, out var typeInfo))
                    return typeInfo;
                typeInfo = new TypeInfo(type);
                typeDict.Add(type, typeInfo);
                return typeInfo;
            }
        }

        private static HashSet<Assembly> _ignoredAssemblies;
        private static HashSet<Type> _ignoredTypes;

        public static bool ShouldIgnoreType(Type type)
        {
            _ignoredAssemblies ??= CollectIgnoredAssembly();
            _ignoredTypes ??= new HashSet<Type>(TypeCache.GetTypesWithAttribute<NoReflectionBakingAttribute>());
            return _ignoredAssemblies.Contains(type.Assembly) || _ignoredTypes.Contains(type);
        }

        private static HashSet<Assembly> CollectIgnoredAssembly()
        {
            var ret = new HashSet<Assembly>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Skip if the assembly is marked with NoReflectionBaking.
                if (assembly.IsDefined(typeof(NoReflectionBakingAttribute)))
                    ret.Add(assembly);

                // If the assembly is not in the Assets folder, skip it.
                var assemblyName = assembly.GetName().Name;
                if (TryGetCorrespondingRootForAssembly(assemblyName, out _) is false)
                    ret.Add(assembly);
            }

            return ret;
        }

        private static readonly StringBuilder _sb = new();

        private static string GenerateCode_Injectable(List<TypeInfo> typeInfos)
        {
            _sb.AppendLine("#if !UNITY_EDITOR");
            _sb.AppendLine("using Zenject;");

            var lastNamespaceName = "";
            foreach (var typeInfo in typeInfos)
            {
                var type = typeInfo.Type;
                var namespaceName = type.Namespace;
                var namespaceChanged = lastNamespaceName != namespaceName;

                if (namespaceChanged)
                {
                    if (string.IsNullOrEmpty(lastNamespaceName) == false)
                        _sb.AppendLine("}").AppendLine();
                    if (string.IsNullOrEmpty(namespaceName) == false)
                        _sb.Append("namespace ").Append(type.Namespace).AppendLine(" {").AppendLine();
                }

                _sb.Append(GetAccessModifier(type)).Append(" partial class ").Append(type.Name)
                    .Append(" : ").Append(nameof(IZenjectInjectable)).AppendLine(" {");

                // For types with no field injection & no method injection, default implementation (empty) will be used.
                if (typeInfo.Fields is not null || typeInfo.Method is not null)
                    GenerateInjectMethod(typeInfo.Fields, typeInfo.Method, _sb);

                _sb.AppendLine("}").AppendLine();

                lastNamespaceName = namespaceName;
            }

            if (string.IsNullOrEmpty(lastNamespaceName) == false)
                _sb.AppendLine("}");

            _sb.AppendLine("#endif");

            var content = _sb.ToString();
            _sb.Clear();

            // No need to explicitly write out the namespace Zenject.
            content = content.Replace("global::Zenject.", "");
            return content;

            static void GenerateInjectMethod(
                List<FieldInfo> fields, MethodInfo method, StringBuilder sb)
            {
                Assert.IsTrue(fields is not null || method is not null, "At least one of fields or method must be non-null.");

                sb.AppendLine($"public void Inject({nameof(DependencyProvider)} dp) {{");

                if (fields != null)
                {
                    foreach (var field in fields)
                    {
                        var injectAttr = field.GetCustomAttribute<InjectAttributeBase>();
                        var injectSpec = new InjectSpec(field.FieldType, injectAttr.Id, injectAttr.Optional);
                        GenerateAssignField(field, injectSpec, sb);
                    }
                }

                if (method != null)
                {
                    sb.Append(method.Name).Append('(');

                    var parameters = method.GetParameters();
                    foreach (var parameter in parameters)
                    {
                        var injectSpec = GetInjectSpecForParam(parameter);
                        GenerateResolveType(injectSpec, sb);
                        sb.AppendLine(",");
                    }

                    if (parameters.Length > 0)
                        sb.Length -= 2;

                    sb.AppendLine(");");
                }

                sb.AppendLine("}");
            }

            static void GenerateAssignField(FieldInfo field, InjectSpec injectSpec, StringBuilder sb)
            {
                if (injectSpec.Type == typeof(DiContainer))
                {
                    Assert.IsFalse(injectSpec.Optional);
                    Assert.AreEqual(default, injectSpec.Id);
                    sb.Append(field.Name).AppendLine(" = dp.Container;");
                    return;
                }

                var typeName = "global::" + injectSpec.Type.FullName;

                if (injectSpec.Optional)
                {
                    sb.Append("dp.TryResolve(")
                        .Append(injectSpec.Id != 0 ? ToArgumentString(injectSpec.Id) + "," : "")
                        .Append("ref ").Append(field.Name)
                        .AppendLine(");");
                }
                else
                {
                    sb.Append(field.Name).Append(" = ")
                        .Append('(').Append(typeName).Append(')')
                        .Append("dp.Resolve(typeof(").Append(typeName).Append(')')
                        .Append(injectSpec.Id != 0 ? ", id: " + ToArgumentString(injectSpec.Id) : "")
                        .AppendLine(");");
                }
            }
        }

        private static string GenerateCode_Constructors(SortedList<Assembly, List<TypeInfo>> assemblyDict)
        {
            _sb.AppendLine("#if !UNITY_EDITOR")
                .AppendLine("using System;")
                .AppendLine("using Zenject;")
                .AppendLine("public class ConstructorHook : IConstructorHook {")
                .AppendLine("public bool TryCreateInstance(Type concreteType, DiContainer container, ArgumentArray extraArgs, out object instance) {")
                .AppendLine("var dp = new DependencyProvider(container, extraArgs);")
                .AppendLine("if (false) {}");

            foreach (var (_, typeInfos) in assemblyDict)
            foreach (var typeInfo in typeInfos)
            {
                // For non-unity object, we generate a constructor even if there's no explicit constructor.
                if (typeInfo.Constructor == null && typeof(UnityEngine.Object).IsAssignableFrom(typeInfo.Type))
                    continue;

                GenerateConstructor(typeInfo.Type, typeInfo.Constructor, _sb);
            }

            _sb.AppendLine("else { instance = null; return false; }")
                .AppendLine("return true;")
                .AppendLine("}")
                .AppendLine("}")
                .AppendLine("#endif");

            var content = _sb.ToString();
            _sb.Clear();
            return content;

            static void GenerateConstructor(Type type, [CanBeNull] MethodInfo constructor, StringBuilder sb)
            {
                var typeName = "global::" + type.FullName;

                sb.Append("else if (concreteType == typeof(").Append(typeName).AppendLine(")) {")
                    .Append("instance = new ").Append(typeName).AppendLine("(");

                if (constructor != null)
                {
                    var parameters = constructor.GetParameters();
                    foreach (var parameter in parameters)
                    {
                        var injectSpec = GetInjectSpecForParam(parameter);
                        GenerateResolveType(injectSpec, sb);
                        sb.AppendLine(",");
                    }

                    if (parameters.Length is not 0)
                        sb.Length -= 2;
                }

                sb.AppendLine(");")
                    .AppendLine("}");
            }
        }

        private static InjectSpec GetInjectSpecForParam(ParameterInfo parameter)
        {
            var paramType = parameter.ParameterType;
            var injectAttr = parameter.GetCustomAttribute<InjectAttributeBase>();
            return injectAttr != null
                ? new InjectSpec(paramType, injectAttr.Id, injectAttr.Optional)
                : new InjectSpec(paramType, default);
        }

        private static void GenerateResolveType(InjectSpec injectSpec, StringBuilder sb)
        {
            if (injectSpec.Type == typeof(DiContainer))
            {
                Assert.IsFalse(injectSpec.Optional);
                Assert.AreEqual(default, injectSpec.Id);
                sb.Append("dp.Container");
                return;
            }

            var typeName = "global::" + injectSpec.Type.FullName;

            if (injectSpec.Optional)
            {
                sb.Append("dp.TryResolve<").Append(typeName).Append(">(")
                    .Append(injectSpec.Id != 0 ? "id: " + ToArgumentString(injectSpec.Id) + "," : "");
                if (injectSpec.Id != 0)
                    sb.Length -= 1;
                sb.Append(')');
            }
            else
            {
                sb.Append('(').Append(typeName).Append(')')
                    .Append("dp.Resolve(typeof(").Append(typeName).Append(')')
                    .Append(injectSpec.Id != 0 ? ", id: " + ToArgumentString(injectSpec.Id) : "")
                    .Append(')');
            }
        }

        private static string GetAccessModifier(Type type)
        {
            if (type.IsPublic) return "public";
            if (type.IsNestedPublic) return "public";
            if (type.IsNestedFamily) return "protected";
            if (type.IsNestedFamORAssem) return "protected internal";
            if (type.IsNestedAssembly) return "internal";
            if (type.IsNestedPrivate) return "private";
            return "internal";
        }

        private static string ToArgumentString(BindId bindId)
        {
            return "(BindId) " + ((uint) bindId).ToString();
        }

        private class TypeInfo
        {
            public readonly Type Type;
            [CanBeNull]
            public MethodInfo Constructor;
            [CanBeNull]
            public MethodInfo Method;
            [CanBeNull]
            public List<FieldInfo> Fields;

            public TypeInfo(Type type)
            {
                Type = type;
            }
        }

        private class AssemblyComparer : IComparer<Assembly>
        {
            public int Compare(Assembly x, Assembly y)
                => string.Compare(x!.FullName, y!.FullName, StringComparison.Ordinal);
        }
    }
}
#endif