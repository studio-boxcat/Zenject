using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;

namespace Zenject
{
    internal static class TypeAnalyzer
    {
        public static InjectConstructorInfo GetConstructorInfo(Type type)
        {
            Assert.IsFalse(type.IsSubclassOf(typeof(UnityEngine.Object)));

            var constructor = SelectConstructor(type);
            var constructorInfo = new InjectConstructorInfo(
                constructor, ParamUtils.BakeParams(constructor));

#if UNITY_EDITOR
            if (IsGeneratedCodeTouchesConstructor(type, constructor) is false // no code gen took in place
                && IsEditorOrSandboxAssembly(type) is false // editor or sandbox assembly -> no need to check
                && type.IsDefined(typeof(UnityEngine.Scripting.PreserveAttribute)) is false) // preserve attribute -> everything will be preserved
            {
                throw new Exception($"Type {type.FullName} is not marked with [Inject*] or [Preserve] and will be stripped by managed code stripping");
            }
#endif

            return constructorInfo;

            static ConstructorInfo SelectConstructor(Type type)
            {
                var constructors = type.GetConstructors(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.AreNotEqual(0, constructors.Length, type.Name);
                Assert.IsTrue(constructors.Count(x => x.IsDefined(typeof(InjectConstructorAttribute))) <= 1, type.Name);

                if (constructors.Length == 0)
                    throw new Exception($"Constructor not found for {type.FullName}. Stripped by managed code stripping.");

                if (constructors.Length == 1)
                    return constructors[0];

                foreach (var constructor in constructors)
                {
                    if (constructor.IsDefined(typeof(InjectConstructorAttribute)))
                        return constructor;
                }

                throw new Exception("There are multiple constructors but none are marked with [Inject]");
            }

#if UNITY_EDITOR
            static bool IsGeneratedCodeTouchesConstructor(Type type, ConstructorInfo constructor)
            {
                // no reflection baking -> no code gen took in place.
                if (ReflectionBaker.ShouldIgnoreType(type))
                    return false;
                // when the code generated, the constructor is accessed by generated code.
                return Injector.IsInjectionRequired(type) // field or method injection
                       || constructor.GetCustomAttribute<InjectConstructorAttribute>() is not null; // constructor attribute
            }

            static bool IsEditorOrSandboxAssembly(Type type)
            {
                var assemblyName = type.Assembly.GetName().Name;
                return assemblyName.EndsWithOrdinal(".Editor")
                       || assemblyName.EndsWithOrdinal(".Sandbox");
            }
#endif
        }

        public static InjectMethodInfo GetMethodInfo(Type type, bool excludeNonDeclaringFields)
        {
            var methodInfo = type.GetMethod("Zenject_Constructor",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (methodInfo == null)
                return default;
            if (excludeNonDeclaringFields && methodInfo.DeclaringType != type)
                return default;
            return new InjectMethodInfo(methodInfo, ParamUtils.BakeParams(methodInfo));
        }

        private static readonly List<InjectFieldInfo> _fieldInfoBuffer = new();
        private static readonly InjectFieldInfo[] _emptyFieldInfoArray = Array.Empty<InjectFieldInfo>();

        public static InjectFieldInfo[] GetFieldInfos(Type type, bool excludeNonDeclaringFields)
        {
            _fieldInfoBuffer.Clear();

            foreach (var field in (FieldInfo[]) type.GetRuntimeFields())
            {
                var fieldAttributes = field.Attributes;
                if ((fieldAttributes & FieldAttributes.Static) != default)
                    continue;

                var fieldType = field.FieldType;
                if (fieldType.IsGenericType)
                    continue;

                if (excludeNonDeclaringFields && field.DeclaringType != type)
                    continue;

                var injectAttr = field.GetCustomAttribute<InjectAttributeBase>();
                if (injectAttr == null)
                    continue;

                var injectSpec = new InjectSpec(field.FieldType, injectAttr.Id, injectAttr.Optional);
                var fieldInfo = new InjectFieldInfo(field, injectSpec);
                _fieldInfoBuffer.Add(fieldInfo);
            }

            return _fieldInfoBuffer.Count > 0
                ? _fieldInfoBuffer.ToArray()
                : _emptyFieldInfoArray;
        }
    }
}