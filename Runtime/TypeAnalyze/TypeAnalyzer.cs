using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;

namespace Zenject
{
    static class TypeAnalyzer
    {
        public static InjectConstructorInfo GetConstructorInfo(Type type)
        {
            Assert.IsFalse(type.IsSubclassOf(typeof(UnityEngine.Object)));

            var constructor = SelectConstructor(type);
            var constructorInfo = new InjectConstructorInfo(
                constructor, ParamUtils.BakeParams(constructor));
            return constructorInfo;

            static ConstructorInfo SelectConstructor(Type type)
            {
                var constructors = type.GetConstructors(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.AreNotEqual(0, constructors.Length, type.Name);
                Assert.IsTrue(constructors.Count(x => x.IsDefined(typeof(InjectConstructorAttribute))) <= 1, type.Name);

                if (constructors.Length == 1)
                    return constructors[0];

                foreach (var constructor in constructors)
                {
                    if (constructor.IsDefined(typeof(InjectConstructorAttribute)))
                        return constructor;
                }

                throw new Exception("There are multiple constructors but none are marked with [Inject]");
            }
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

        static readonly List<InjectFieldInfo> _fieldInfoBuffer = new();
        static readonly InjectFieldInfo[] _emptyFieldInfoArray = Array.Empty<InjectFieldInfo>();

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