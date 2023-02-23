#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Zenject
{
    public static class ReflectionBaker
    {
        public static bool GenerateCode()
        {
            var dirty = false;
            var typeDict = AnalyzeAllTypes();
            var initializableTypes = BuildInitializableTypes(typeDict);

            // Group types by assembly.
            var assemblyDict = new Dictionary<string, List<InjectionInfo>>();
            foreach (var typeInfo in typeDict.Values)
            {
                var assembly = typeInfo.Type.Assembly;

                // Skip if the assembly is marked with NoReflectionBaking.
                if (assembly.IsDefined(typeof(NoReflectionBakingAttribute)))
                    continue;

                var assemblyName = assembly.GetName().Name;
                if (assemblyDict.TryGetValue(assemblyName, out var typeList) == false)
                {
                    typeList = new List<InjectionInfo>();
                    assemblyDict.Add(assemblyName, typeList);
                }
                typeList.Add(typeInfo);
            }

            // Generate code for each assembly.
            foreach (var (assemblyName, injectionInfos) in assemblyDict)
            {
                Debug.Log($"Generating code for assembly '{assemblyName}'");

                if (TryGetCorrespondingRootForAssembly(assemblyName, out var rootDir) == false)
                    continue;

                var codeGenPath = rootDir + "Zenject_CodeGen.cs";
                var orgContent = File.Exists(codeGenPath) ? File.ReadAllText(codeGenPath) : "";
                var newContent = GenerateCode(injectionInfos, initializableTypes);
                if (orgContent != newContent)
                {
                    File.WriteAllText(codeGenPath, newContent);
                    dirty = true;
                }
            }

            return dirty;
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

        static Dictionary<Type, InjectionInfo> AnalyzeAllTypes()
        {
            var typeDict = new Dictionary<Type, InjectionInfo>();
            var ignoredTypes = new HashSet<Type>(TypeCache.GetTypesWithAttribute<NoReflectionBakingAttribute>());

            InjectionInfo GetTypeInfo(Type type)
            {
                if (typeDict.TryGetValue(type, out var typeInfo))
                    return typeInfo;
                typeInfo = new InjectionInfo(type);
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

            return typeDict;
        }

        static HashSet<Type> BuildInitializableTypes(Dictionary<Type, InjectionInfo> typeDict)
        {
            var initializableTypes = new HashSet<Type>();
            foreach (var (type, injectionInfo) in typeDict)
            {
                // Check if this type implements IInitializable
                if (injectionInfo.ShouldImplementInitializable())
                {
                    initializableTypes.Add(type);
                    continue;
                }

                // Check if any base types implement IInitializable
                var baseType = type.BaseType;
                while (baseType != typeof(object))
                {
                    if (typeDict.TryGetValue(baseType, out var baseTypeInjectionInfo)
                        && baseTypeInjectionInfo.ShouldImplementInitializable())
                    {
                        initializableTypes.Add(type);
                        break;
                    }

                    baseType = type.BaseType;
                }
            }

            return initializableTypes;
        }

        static readonly StringBuilder _sb = new();

        static string GenerateCode(List<InjectionInfo> injectionInfos, HashSet<Type> initializableTypes)
        {
            _sb.AppendLine("#if ZENJECT_REFLECTION_BAKING");
            _sb.AppendLine("using Zenject;");

            var lastNamespaceName = "";
            foreach (var injectionInfo in injectionInfos)
            {
                var type = injectionInfo.Type;
                var namespaceName = type.Namespace;
                var namespaceChanged = lastNamespaceName != namespaceName;

                if (namespaceChanged)
                {
                    if (string.IsNullOrEmpty(lastNamespaceName) == false)
                        _sb.AppendLine("}").AppendLine();
                    if (string.IsNullOrEmpty(namespaceName) == false)
                        _sb.Append("namespace ").Append(type.Namespace).AppendLine(" {").AppendLine();
                }

                var shouldImplementInitializable = injectionInfo.ShouldImplementInitializable();
                _sb.Append("public partial class ").Append(type.Name)
                    .Append(shouldImplementInitializable ? " : IZenject_Initializable" : "")
                    .AppendLine(" {");

                if (injectionInfo.Constructor != null)
                    GenerateConstructor(type, injectionInfo.Constructor, _sb);

                if (shouldImplementInitializable)
                    GenerateInitializer(type, injectionInfo.Fields, injectionInfo.Method, initializableTypes, _sb);

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

            static void GenerateConstructor(Type type, MethodInfo constructor, StringBuilder sb)
            {
                sb.Append("[UnityEngine.Scripting.Preserve] public ").Append(type.Name).AppendLine("(DependencyProviderRef dp) : this(");

                var parameters = constructor.GetParameters();
                foreach (var parameter in parameters)
                {
                    var injectSpec = GetInjectSpecForParam(parameter);
                    GenerateResolveType(injectSpec, sb);
                    sb.AppendLine(",");
                }

                if (parameters.Length > 0)
                    sb.Length -= 2;

                sb.AppendLine("){}");
            }

            static void GenerateInitializer(
                Type type, List<FieldInfo> fields, MethodInfo method, HashSet<Type> initializableTypes, StringBuilder sb)
            {
                var isBaseInitializable = initializableTypes.Contains(type.BaseType);
                sb.AppendLine(isBaseInitializable
                    ? "public override void Initialize(DependencyProvider dp) {"
                    : "public void Initialize(DependencyProvider dp) {");

                if (isBaseInitializable)
                    sb.AppendLine("base.Initialize(dp);");

                if (fields != null)
                {
                    foreach (var field in fields)
                    {
                        var injectAttr = field.GetCustomAttribute<InjectAttributeBase>();
                        var injectSpec = new InjectSpec(field.FieldType, injectAttr.Id, injectAttr.Source, injectAttr.Optional);
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

            static void GenerateResolveType(InjectSpec injectSpec, StringBuilder sb)
            {
                var typeName = "global::" + injectSpec.Type.FullName;

                if (injectSpec.Optional)
                {
                    sb.Append("dp.TryResolve<").Append(typeName).Append(">(")
                        .Append(injectSpec.Identifier != 0 ? "identifier: " + injectSpec.Identifier + "," : "")
                        .Append(injectSpec.SourceType != 0 ? ", sourceType: InjectSources." + injectSpec.SourceType + "," : "");
                    if (injectSpec.Identifier != 0 || injectSpec.SourceType != 0)
                        sb.Length -= 1;
                    sb.Append(')');
                }
                else
                {
                    sb.Append('(').Append(typeName).Append(')')
                        .Append("dp.Resolve(typeof(").Append(typeName).Append(')')
                        .Append(injectSpec.Identifier != 0 ? ", identifier: " + injectSpec.Identifier : "")
                        .Append(injectSpec.SourceType != 0 ? ", sourceType: InjectSources." + injectSpec.SourceType : "")
                        .Append(')');
                }
            }

            static void GenerateAssignField(FieldInfo field, InjectSpec injectSpec, StringBuilder sb)
            {
                var typeName = "global::" + injectSpec.Type.FullName;

                if (injectSpec.Optional)
                {
                    sb.Append("dp.TryResolve(")
                        .Append(injectSpec.Identifier != 0 ? injectSpec.Identifier + "," : "")
                        .Append(injectSpec.SourceType != 0 ? "InjectSources." + injectSpec.SourceType + "," : "")
                        .Append("ref ").Append(field.Name)
                        .AppendLine(");");
                }
                else
                {
                    sb.Append(field.Name).Append(" = ")
                        .Append('(').Append(typeName).Append(')')
                        .Append("dp.Resolve(typeof(").Append(typeName).Append(')')
                        .Append(injectSpec.Identifier != 0 ? ", identifier: " + injectSpec.Identifier : "")
                        .Append(injectSpec.SourceType != 0 ? ", sourceType: InjectSources." + injectSpec.SourceType : "")
                        .AppendLine(");");
                }
            }
        }

        static InjectSpec GetInjectSpecForParam(ParameterInfo parameter)
        {
            var paramType = parameter.ParameterType;
            var injectAttr = parameter.GetCustomAttribute<InjectAttribute>();
            return injectAttr != null
                ? new InjectSpec(paramType, injectAttr.Id, injectAttr.Source, injectAttr.Optional)
                : new InjectSpec(paramType, default, InjectSources.Any, false);
        }

        class InjectionInfo
        {
            public readonly Type Type;
            [CanBeNull]
            public MethodInfo Constructor;
            [CanBeNull]
            public MethodInfo Method;
            [CanBeNull]
            public List<FieldInfo> Fields;

            public InjectionInfo(Type type)
            {
                Type = type;
            }

            public bool ShouldImplementInitializable() => Fields != null || Method != null;
        }
    }
}
#endif