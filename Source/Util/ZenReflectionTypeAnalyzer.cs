using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;

namespace Zenject.Internal
{
    public static class ReflectionTypeAnalyzer
    {
        public static InjectTypeInfo GetReflectionInfo(Type type)
        {
            return new InjectTypeInfo(
                GetConstructorInfo(type),
                GetMethodInfo(type),
                GetFieldInfos(type));
        }

        static InjectTypeInfo.InjectConstructorInfo GetConstructorInfo(Type type)
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return new InjectTypeInfo.InjectConstructorInfo(null, Array.Empty<InjectableInfo>());

            var constructor = TryGetInjectConstructor(type);
            return new InjectTypeInfo.InjectConstructorInfo(constructor, BakeInjectParameterInfos(constructor));

            static ConstructorInfo TryGetInjectConstructor(Type type)
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

                throw new Exception("이용가능한 생성자가 2개 이상입니다.");
            }
        }

        static InjectTypeInfo.InjectMethodInfo GetMethodInfo(Type type)
        {
            var methodInfo = type.GetMethod("Zenject_Constructor",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return methodInfo != null
                ? new InjectTypeInfo.InjectMethodInfo(methodInfo, BakeInjectParameterInfos(methodInfo))
                : default;
        }

        static readonly List<InjectTypeInfo.InjectFieldInfo> _fieldInfoBuffer = new();

        static InjectTypeInfo.InjectFieldInfo[] GetFieldInfos(Type type)
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
                if (fieldType.IsArray)
                    continue;

                var injectAttr = field.GetCustomAttribute<InjectAttributeBase>();
                if (injectAttr == null)
                    continue;

                var fieldInfo = new InjectTypeInfo.InjectFieldInfo(field, GetInjectableInfoForMember(field, injectAttr));
                _fieldInfoBuffer.Add(fieldInfo);
            }

            return _fieldInfoBuffer.ToArray();

            static InjectableInfo GetInjectableInfoForMember(FieldInfo fieldInfo, InjectAttributeBase injectAttr)
            {
                return injectAttr != null
                    ? new InjectableInfo(fieldInfo.FieldType, injectAttr.Id, injectAttr.Source, injectAttr.Optional)
                    : new InjectableInfo(fieldInfo.FieldType, 0, InjectSources.Any);
            }
        }

        static InjectableInfo[] BakeInjectParameterInfos(MethodBase methodInfo)
        {
            var paramInfos = methodInfo.GetParameters();
            if (paramInfos.Length == 0) return Array.Empty<InjectableInfo>();

            var injectParamInfos = new InjectableInfo[paramInfos.Length];
            for (var i = 0; i < paramInfos.Length; i++)
                injectParamInfos[i] = CreateInjectableInfoForParam(paramInfos[i]);
            return injectParamInfos;

            static InjectableInfo CreateInjectableInfoForParam(ParameterInfo paramInfo)
            {
                var injectAttr = paramInfo.GetCustomAttribute<InjectAttributeBase>();
                return injectAttr != null
                    ? new InjectableInfo(paramInfo.ParameterType, injectAttr.Id, injectAttr.Source, injectAttr.Optional)
                    : new InjectableInfo(paramInfo.ParameterType, 0, InjectSources.Any);
            }
        }
    }
}