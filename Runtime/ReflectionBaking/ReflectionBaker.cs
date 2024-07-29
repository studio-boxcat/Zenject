#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public static class ReflectionBaker
    {
        public static bool GenerateCode()
        {
            var dirty = false;
            var typeDict = AnalyzeAllTypes();
            var injectableTypes = BuildInjectableTypes(typeDict);

            // Group types by assembly.
            var assemblyDict = new SortedList<Assembly, List<TypeInfo>>(new AssemblyComparer());
            foreach (var typeInfo in typeDict.Values)
            {
                var assembly = typeInfo.Type.Assembly;

                if (ShouldIgnoreAssembly(assembly))
                    continue;

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
                typeList.Sort((a, b) => a.Type.FullName.CompareTo(b.Type.FullName));

            // Generate injectables.
            foreach (var (assembly, typeInfos) in assemblyDict)
            {
                var assemblyName = assembly.GetName().Name;
                L.I("Generating code for assembly: " + assemblyName);
                var newContent = GenerateCode_Injectable(typeInfos, injectableTypes);
                var rootFound = TryGetCorrespondingRootForAssembly(assemblyName, out var rootDir);
                Assert.IsTrue(rootFound, $"Root directory for assembly '{assemblyName}' not found");
                dirty |= CompareAndWrite(rootDir + "Zenject_CodeGen.cs", newContent);
            }

            // Generate constructors.
            {
                var newContent = GenerateCode_Constructors(assemblyDict);
                dirty |= CompareAndWrite("Assets/Zenject_CodeGen_Constructors.cs", newContent);
            }

            static bool CompareAndWrite(string path, string content)
            {
                var orgContent = File.Exists(path) ? File.ReadAllText(path) : "";
                var newContent = content;
                if (orgContent == newContent) return false;
                File.WriteAllText(path, newContent);
                return true;
            }

            return dirty;
        }

        static readonly Dictionary<Assembly, bool> _assembliesToIgnore = new();

        static bool ShouldIgnoreAssembly(Assembly assembly)
        {
            if (_assembliesToIgnore.TryGetValue(assembly, out var shouldIgnore))
                return shouldIgnore;

            // Skip if the assembly is marked with NoReflectionBaking.
            if (assembly.IsDefined(typeof(NoReflectionBakingAttribute)))
                return _assembliesToIgnore[assembly] = true;

            // If the assembly is not in the Assets folder, skip it.
            var assemblyName = assembly.GetName().Name;
            shouldIgnore = TryGetCorrespondingRootForAssembly(assemblyName, out _) == false;
            return _assembliesToIgnore[assembly] = shouldIgnore;
        }

        static readonly Dictionary<string, string> _assemblyNameToDir = new();

        static bool TryGetCorrespondingRootForAssembly(string assemblyName, out string rootDir)
        {
            if (_assemblyNameToDir.Count == 0) SetUpAssemblyNameToDir();
            return _assemblyNameToDir.TryGetValue(assemblyName, out rootDir);

            static void SetUpAssemblyNameToDir()
            {
                // These are the default assembly names that Unity creates.
                _assemblyNameToDir["Assembly-CSharp-firstpass"] = "Assets/Plugins/";
                _assemblyNameToDir["Assembly-CSharp-Editor-firstpass"] = "Assets/Plugins/Editor/";
                _assemblyNameToDir["Assembly-CSharp"] = "Assets/";
                _assemblyNameToDir["Assembly-CSharp-Editor"] = "Assets/Editor/";

                // Find all asmdefs and add them to the dictionary.
                var asmdefGUIDs = AssetDatabase.FindAssets("t:asmdef");
                foreach (var asmdefGUID in asmdefGUIDs)
                {
                    var asmdefPath = AssetDatabase.GUIDToAssetPath(asmdefGUID);
                    var delimiterIndex = asmdefPath.LastIndexOf('/');
                    var asmdefName = asmdefPath[(delimiterIndex + 1)..^".asmdef".Length];
                    var asmdefDir = asmdefPath[..(delimiterIndex + 1)];
                    _assemblyNameToDir.Add(asmdefName, asmdefDir);
                }
            }
        }

        static Dictionary<Type, TypeInfo> AnalyzeAllTypes()
        {
            var typeDict = new Dictionary<Type, TypeInfo>();
            var ignoredTypes = new HashSet<Type>(TypeCache.GetTypesWithAttribute<NoReflectionBakingAttribute>());

            TypeInfo GetTypeInfo(Type type)
            {
                if (typeDict.TryGetValue(type, out var typeInfo))
                    return typeInfo;
                typeInfo = new TypeInfo(type);
                typeDict.Add(type, typeInfo);
                return typeInfo;
            }

            var constructors = TypeCache.GetMethodsWithAttribute<InjectConstructorAttribute>();
            foreach (var methodInfo in constructors)
            {
                var type = methodInfo.DeclaringType;
                // Skip if the type is marked with NoReflectionBaking.
                if (ignoredTypes.Contains(type)) continue;

                GetTypeInfo(type).Constructor = methodInfo;
            }

            foreach (var methodInfo in TypeCache.GetMethodsWithAttribute<InjectMethodAttribute>())
            {
                var type = methodInfo.DeclaringType;
                // Skip if the type is marked with NoReflectionBaking.
                if (ignoredTypes.Contains(type)) continue;

                var typeInfo = GetTypeInfo(type);
                Assert.IsNull(typeInfo.Method);
                typeInfo.Method = methodInfo;
            }

            foreach (var fieldInfo in TypeCache.GetFieldsWithAttribute<InjectAttributeBase>())
            {
                var type = fieldInfo.DeclaringType;
                // Skip if the type is marked with NoReflectionBaking.
                if (ignoredTypes.Contains(type)) continue;

                var typeInfo = GetTypeInfo(type);
                typeInfo.Fields ??= new List<FieldInfo>();
                typeInfo.Fields.Add(fieldInfo);
            }

            // Sort FieldInfos by name.
            foreach (var typeInfo in typeDict.Values)
                typeInfo.Fields?.Sort((a, b) => a.Name.CompareTo(b.Name));

            return typeDict;
        }

        static HashSet<Type> BuildInjectableTypes(Dictionary<Type, TypeInfo> typeDict)
        {
            var injectableTypes = new HashSet<Type>();
            foreach (var (type, typeInfo) in typeDict)
            {
                // Check if this type implements IZenjectInjectable
                if (typeInfo.ShouldImplementInjectable())
                {
                    injectableTypes.Add(type);
                    continue;
                }

                // Check if any base types implement IZenjectInjectable
                var baseType = type.BaseType;
                while (baseType != typeof(object))
                {
                    if (typeDict.TryGetValue(baseType, out var baseTypeInjectionInfo)
                        && baseTypeInjectionInfo.ShouldImplementInjectable())
                    {
                        injectableTypes.Add(type);
                        break;
                    }

                    baseType = type.BaseType;
                }
            }

            return injectableTypes;
        }

        static readonly StringBuilder _sb = new();

        static string GenerateCode_Injectable(List<TypeInfo> typeInfos, HashSet<Type> injectableTypes)
        {
            _sb.AppendLine("#if !UNITY_EDITOR");
            _sb.AppendLine("using Zenject;");

            var lastNamespaceName = "";
            foreach (var typeInfo in typeInfos)
            {
                if (typeInfo.ShouldImplementInjectable() == false)
                    continue;

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

                var shouldImplementInjectable = typeInfo.ShouldImplementInjectable();
                _sb.Append(GetAccessModifier(type)).Append(" partial class ").Append(type.Name)
                    .Append(shouldImplementInjectable ? " : IZenjectInjectable" : "")
                    .AppendLine(" {");

                if (shouldImplementInjectable)
                    GenerateInjectMethod(type, typeInfo.Fields, typeInfo.Method, injectableTypes, _sb);

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
                Type type, List<FieldInfo> fields, MethodInfo method, HashSet<Type> injectableTypes, StringBuilder sb)
            {
                var isBaseInjectable = injectableTypes.Contains(type.BaseType);
                sb.AppendLine(isBaseInjectable
                    ? "public override void Inject(DependencyProvider dp) {"
                    : "public void Inject(DependencyProvider dp) {");

                if (isBaseInjectable)
                    sb.AppendLine("base.Inject(dp);");

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
                    sb.Append("Zenject_Constructor(");

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
                    Assert.AreEqual(0, injectSpec.Identifier);
                    sb.Append(field.Name).AppendLine(" = dp.Container;");
                    return;
                }

                var typeName = "global::" + injectSpec.Type.FullName;

                if (injectSpec.Optional)
                {
                    sb.Append("dp.TryResolve(")
                        .Append(injectSpec.Identifier != 0 ? injectSpec.Identifier + "," : "")
                        .Append("ref ").Append(field.Name)
                        .AppendLine(");");
                }
                else
                {
                    sb.Append(field.Name).Append(" = ")
                        .Append('(').Append(typeName).Append(')')
                        .Append("dp.Resolve(typeof(").Append(typeName).Append(')')
                        .Append(injectSpec.Identifier != 0 ? ", identifier: " + injectSpec.Identifier : "")
                        .AppendLine(");");
                }
            }
        }

        static string GenerateCode_Constructors(SortedList<Assembly, List<TypeInfo>> assemblyDict)
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

                    sb.Length -= 2;
                }

                sb.AppendLine(");")
                    .AppendLine("}");
            }
        }

        static InjectSpec GetInjectSpecForParam(ParameterInfo parameter)
        {
            var paramType = parameter.ParameterType;
            var injectAttr = parameter.GetCustomAttribute<InjectAttribute>();
            return injectAttr != null
                ? new InjectSpec(paramType, injectAttr.Id, injectAttr.Optional)
                : new InjectSpec(paramType, default);
        }

        static void GenerateResolveType(InjectSpec injectSpec, StringBuilder sb)
        {
            if (injectSpec.Type == typeof(DiContainer))
            {
                Assert.IsFalse(injectSpec.Optional);
                Assert.AreEqual(0, injectSpec.Identifier);
                sb.Append("dp.Container");
                return;
            }

            var typeName = "global::" + injectSpec.Type.FullName;

            if (injectSpec.Optional)
            {
                sb.Append("dp.TryResolve<").Append(typeName).Append(">(")
                    .Append(injectSpec.Identifier != 0 ? "identifier: " + injectSpec.Identifier + "," : "");
                if (injectSpec.Identifier != 0)
                    sb.Length -= 1;
                sb.Append(')');
            }
            else
            {
                sb.Append('(').Append(typeName).Append(')')
                    .Append("dp.Resolve(typeof(").Append(typeName).Append(')')
                    .Append(injectSpec.Identifier != 0 ? ", identifier: " + injectSpec.Identifier : "")
                    .Append(')');
            }
        }

        static string GetAccessModifier(Type type)
        {
            if (type.IsPublic) return "public";
            if (type.IsNestedPublic) return "public";
            if (type.IsNestedFamily) return "protected";
            if (type.IsNestedFamORAssem) return "protected internal";
            if (type.IsNestedAssembly) return "internal";
            if (type.IsNestedPrivate) return "private";
            return "internal";
        }

        class TypeInfo
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

            public bool ShouldImplementInjectable() => Fields != null || Method != null;
        }

        class AssemblyComparer : IComparer<Assembly>
        {
            public int Compare(Assembly x, Assembly y)
                => x.FullName.CompareTo(y.FullName);
        }
    }
}
#endif