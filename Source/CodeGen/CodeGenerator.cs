#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Zenject
{
    public static class CodeGenerator
    {
        [MenuItem("Tools/Zenject/Generate Code #G")]
        public static void Generate()
        {
            var assemblyDict = AnalyzeAllTypes();

            foreach (var (assemblyName, typeDict) in assemblyDict)
            {
                Debug.Log($"Generating code for assembly '{assemblyName}'");

                if (TryGetCorrespondingRootForAssembly(assemblyName, out var rootDir) == false)
                    continue;

                File.WriteAllText(rootDir + "Zenject_CodeGen.cs", GenerateCode(typeDict));
            }

            AssetDatabase.Refresh();
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

        static Dictionary<string, Dictionary<Type, (MethodInfo Constructor, bool Initializer)>> AnalyzeAllTypes()
        {
            var assemblyDict = new Dictionary<string, Dictionary<Type, (MethodInfo Constructor, bool Initializer)>>();

            var methods = TypeCache.GetMethodsWithAttribute<InjectConstructorAttribute>();
            var initializableTypes = TypeCache.GetTypesDerivedFrom<IZenject_Initializable>();

            foreach (var method in methods)
            {
                var type = method.DeclaringType;
                var assemblyName = type.Assembly.GetName().Name;
                var typeDict = GetTypeDict(assemblyName);

                var value = typeDict.GetValueOrDefault(type);
                value.Constructor = method;
                typeDict[type] = value;
            }

            foreach (var type in initializableTypes)
            {
                var assemblyName = type.Assembly.GetName().Name;
                var typeDict = GetTypeDict(assemblyName);

                var value = typeDict.GetValueOrDefault(type);
                value.Initializer = true;
                typeDict[type] = value;
            }

            return assemblyDict;

            Dictionary<Type, (MethodInfo Constructor, bool Initializer)> GetTypeDict(string assemblyName)
            {
                if (assemblyDict.TryGetValue(assemblyName, out var typeDict))
                    return typeDict;

                typeDict = new Dictionary<Type, (MethodInfo, bool)>();
                assemblyDict.Add(assemblyName, typeDict);
                return typeDict;
            }
        }

        static readonly StringBuilder _sb = new StringBuilder();

        static string GenerateCode(Dictionary<Type, (MethodInfo Constructor, bool Initializer)> typeDict)
        {
            _sb.AppendLine("using Zenject;");

            foreach (var (type, (constructor, initializer)) in typeDict)
            {
                var namespaceName = type.Namespace;
                if (string.IsNullOrEmpty(namespaceName) == false)
                    _sb.Append("namespace ").Append(type.Namespace).AppendLine(" {");

                _sb.Append("public partial class ").Append(type.Name).AppendLine(" {");
                if (constructor != null)
                    GenerateConstructor(type, constructor, _sb);
                if (initializer)
                    GenerateInitializer(type, _sb);
                _sb.AppendLine("}");

                if (string.IsNullOrEmpty(namespaceName) == false)
                    _sb.AppendLine("}");
            }

            var content = _sb.ToString();
            _sb.Clear();

            // No need to explicitly write out the namespace Zenject.
            content = content.Replace("Zenject.", "");
            return content;

            static void GenerateConstructor(Type type, MethodInfo constructor, StringBuilder sb)
            {
                sb.Append("public ").Append(type.Name).AppendLine("(DependencyProviderRef dp) : this(");

                var parameters = constructor.GetParameters();
                foreach (var parameter in parameters)
                {
                    var paramType = parameter.ParameterType;
                    var injectAttr = parameter.GetCustomAttribute<InjectAttribute>();
                    var injectSpec = new InjectSpec(paramType, injectAttr.Id, injectAttr.Source, injectAttr.Optional);
                    GenerateResolveType(injectSpec, sb);
                    sb.AppendLine(",");
                }

                if (parameters.Length > 0)
                    sb.Length--;

                sb.AppendLine("){}");
            }

            static void GenerateInitializer(Type type, StringBuilder sb)
            {
                var isBaseInitializable = typeof(IZenject_Initializable).IsAssignableFrom(type.BaseType);
                sb.AppendLine(isBaseInitializable
                    ? "public override void Initialize(DependencyProvider dp) {"
                    : "public void Initialize(DependencyProvider dp) {");

                if (isBaseInitializable)
                    sb.AppendLine("base.Initialize(dp);");

                var fields = TypeAnalyzer.GetFieldInfos(type, isBaseInitializable);
                foreach (var field in fields)
                {
                    sb.Append(field.FieldInfo.Name).Append(" = ");
                    GenerateResolveType(field.Info, sb);
                    sb.AppendLine(";");
                }

                var method = TypeAnalyzer.GetMethodInfo(type);
                if (method.MethodInfo != null)
                {
                    sb.Append("Zenject_Constructor(");

                    foreach (var paramSpec in method.Parameters)
                    {
                        GenerateResolveType(paramSpec, sb);
                        sb.Append(",");
                    }

                    if (method.Parameters.Length > 0)
                        sb.Length--;

                    sb.AppendLine(");");
                }

                sb.AppendLine("}");
            }

            static void GenerateResolveType(InjectSpec injectSpec, StringBuilder sb)
            {
                var typeName = injectSpec.Type.FullName;
                sb.Append('(').Append(typeName).Append(')').Append("dp.")
                    .Append(injectSpec.Optional ? "Resolve(" : "TryResolve(")
                    .Append("typeof(").Append(typeName).Append(')')
                    .Append(injectSpec.Identifier != 0 ? ", identifier: " + injectSpec.Identifier : "")
                    .Append(injectSpec.SourceType != 0 ? ", sourceType: InjectSources." + injectSpec.SourceType : "")
                    .Append(')');
            }
        }
    }
}
#endif