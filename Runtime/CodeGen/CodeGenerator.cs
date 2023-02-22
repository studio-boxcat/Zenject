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
            var dirty = false;
            var assemblyDict = AnalyzeAllTypes();

            foreach (var (assemblyName, typeDict) in assemblyDict)
            {
                Debug.Log($"Generating code for assembly '{assemblyName}'");

                if (TryGetCorrespondingRootForAssembly(assemblyName, out var rootDir) == false)
                    continue;

                var codeGenPath = rootDir + "Zenject_CodeGen.cs";
                var orgContent = File.Exists(codeGenPath) ? File.ReadAllText(codeGenPath) : "";
                var newContent = GenerateCode(typeDict);
                if (orgContent != newContent)
                {
                    File.WriteAllText(codeGenPath, newContent);
                    dirty = true;
                }
            }

            if (dirty)
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

        static Dictionary<string, Dictionary<Type, (MethodInfo Constructor, InjectFieldInfo[] InjectFields, InjectMethodInfo InjectMethod)>> AnalyzeAllTypes()
        {
            var assemblyDict = new Dictionary<string, Dictionary<Type, (MethodInfo, InjectFieldInfo[], InjectMethodInfo)>>();

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
                var isBaseInitializable = typeof(IZenject_Initializable).IsAssignableFrom(type.BaseType);
                var typeDict = GetTypeDict(assemblyName);
                var value = typeDict.GetValueOrDefault(type);
                value.InjectFields = TypeAnalyzer.GetFieldInfos(type, isBaseInitializable);
                value.InjectMethod = TypeAnalyzer.GetMethodInfo(type, true);
                typeDict[type] = value;
            }

            return assemblyDict;

            Dictionary<Type, (MethodInfo Constructor, InjectFieldInfo[] InjectFields, InjectMethodInfo InjectMethod)> GetTypeDict(string assemblyName)
            {
                if (assemblyDict.TryGetValue(assemblyName, out var typeDict))
                    return typeDict;

                typeDict = new Dictionary<Type, (MethodInfo, InjectFieldInfo[], InjectMethodInfo)>();
                assemblyDict.Add(assemblyName, typeDict);
                return typeDict;
            }
        }

        static readonly StringBuilder _sb = new();

        static string GenerateCode(Dictionary<Type, (MethodInfo Constructor, InjectFieldInfo[] InjectFields, InjectMethodInfo InjectMethod)> typeDict)
        {
            _sb.AppendLine("using Zenject;");

            foreach (var (type, (constructor, fields, method)) in typeDict)
            {
                var namespaceName = type.Namespace;
                if (string.IsNullOrEmpty(namespaceName) == false)
                    _sb.Append("namespace ").Append(type.Namespace).AppendLine(" {");

                _sb.Append("public partial class ").Append(type.Name).AppendLine(" {");
                if (constructor != null)
                    GenerateConstructor(type, constructor, _sb);
                if (fields is {Length: > 0} || method.MethodInfo != null)
                    GenerateInitializer(type, fields, method, _sb);
                _sb.AppendLine("}");

                if (string.IsNullOrEmpty(namespaceName) == false)
                    _sb.AppendLine("}").AppendLine();
            }

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
                    var paramType = parameter.ParameterType;
                    var injectAttr = parameter.GetCustomAttribute<InjectAttribute>();
                    var injectSpec = injectAttr != null
                        ? new InjectSpec(paramType, injectAttr.Id, injectAttr.Source, injectAttr.Optional)
                        : new InjectSpec(paramType, default, InjectSources.Any, false);
                    GenerateResolveType(injectSpec, sb);
                    sb.AppendLine(",");
                }

                if (parameters.Length > 0)
                    sb.Length -= 2;

                sb.AppendLine("){}");
            }

            static void GenerateInitializer(Type type, InjectFieldInfo[] fields, InjectMethodInfo method, StringBuilder sb)
            {
                var isBaseInitializable = typeof(IZenject_Initializable).IsAssignableFrom(type.BaseType);
                sb.AppendLine(isBaseInitializable
                    ? "public override void Initialize(DependencyProvider dp) {"
                    : "public void Initialize(DependencyProvider dp) {");

                if (isBaseInitializable)
                    sb.AppendLine("base.Initialize(dp);");

                if (fields != null)
                {
                    foreach (var field in fields)
                        GenerateAssignField(field, sb);
                }

                if (method.MethodInfo != null)
                {
                    sb.Append("Zenject_Constructor(");

                    foreach (var paramSpec in method.Parameters)
                    {
                        GenerateResolveType(paramSpec, sb);
                        sb.AppendLine(",");
                    }

                    if (method.Parameters.Length > 0)
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

            static void GenerateAssignField(InjectFieldInfo field, StringBuilder sb)
            {
                var injectSpec = field.Info;
                var typeName = "global::" + injectSpec.Type.FullName;

                if (injectSpec.Optional)
                {
                    sb.Append("dp.TryResolve(")
                        .Append(injectSpec.Identifier != 0 ? injectSpec.Identifier + "," : "")
                        .Append(injectSpec.SourceType != 0 ? "InjectSources." + injectSpec.SourceType + "," : "")
                        .Append("out ").Append(field.FieldInfo.Name)
                        .AppendLine(");");
                }
                else
                {
                    sb.Append(field.FieldInfo.Name).Append(" = ")
                        .Append('(').Append(typeName).Append(')')
                        .Append("dp.Resolve(typeof(").Append(typeName).Append(')')
                        .Append(injectSpec.Identifier != 0 ? ", identifier: " + injectSpec.Identifier : "")
                        .Append(injectSpec.SourceType != 0 ? ", sourceType: InjectSources." + injectSpec.SourceType : "")
                        .AppendLine(");");
                }
            }
        }
    }
}
#endif