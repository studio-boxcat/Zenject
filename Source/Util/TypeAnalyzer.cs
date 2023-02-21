using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;

namespace Zenject
{
    public static class TypeAnalyzer
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
                Assert.IsTrue(constructors.Count(x => x.IsDefined(typeof(InjectAttributeBase))) <= 1, type.Name);

                if (constructors.Length == 1)
                    return constructors[0];

                foreach (var constructor in constructors)
                {
                    if (constructor.IsDefined(typeof(InjectAttributeBase)))
                        return constructor;
                }

                throw new Exception("There are multiple constructors but none are marked with [Inject]");
            }
        }

        public static InjectMethodInfo GetMethodInfo(Type type)
        {
            var methodInfo = type.GetMethod("Zenject_Constructor",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return methodInfo != null
                ? new InjectMethodInfo(methodInfo, ParamUtils.BakeParams(methodInfo))
                : default;
        }

        static readonly List<InjectFieldInfo> _fieldInfoBuffer = new();
        static readonly InjectFieldInfo[] _emptyFieldInfoArray = Array.Empty<InjectFieldInfo>();

        public static InjectFieldInfo[] GetFieldInfos(Type type)
        {
            _fieldInfoBuffer.Clear();

            foreach (var field in (FieldInfo[]) type.GetRuntimeFields())
            {
                var fieldAttributes = field.Attributes;
                if ((fieldAttributes & FieldAttributes.Static) != default)
                    continue;
                if ((fieldAttributes & FieldAttributes.InitOnly) == default)
                    continue;

                var fieldType = field.FieldType;
                if (fieldType.IsGenericType)
                    continue;

                var injectAttr = field.GetCustomAttribute<InjectAttributeBase>();
                if (injectAttr == null)
                    continue;

                var fieldInfo = new InjectFieldInfo(field, GetInjectableInfoForMember(field, injectAttr));
                _fieldInfoBuffer.Add(fieldInfo);
            }

            return _fieldInfoBuffer.Count > 0
                ? _fieldInfoBuffer.ToArray()
                : _emptyFieldInfoArray;

            static InjectSpec GetInjectableInfoForMember(FieldInfo fieldInfo, InjectAttributeBase injectAttr)
            {
                return injectAttr != null
                    ? new InjectSpec(fieldInfo.FieldType, injectAttr.Id, injectAttr.Source, injectAttr.Optional)
                    : new InjectSpec(fieldInfo.FieldType, 0, InjectSources.Any);
            }
        }
    }
}