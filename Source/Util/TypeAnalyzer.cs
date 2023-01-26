using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;

namespace Zenject
{
    public static class TypeAnalyzer
    {
        static readonly Dictionary<Type, InjectTypeInfo> _typeInfo = new();
        static readonly InjectSpec[] _emptyInjectableArray = Array.Empty<InjectSpec>();

        public static InjectTypeInfo GetInfo(Type type)
        {
            if (_typeInfo.TryGetValue(type, out var typeInfo))
                return typeInfo;

            typeInfo = AnalyzeType(type);
            _typeInfo.Add(type, typeInfo);
            return typeInfo;
        }

        static InjectTypeInfo AnalyzeType(Type type)
        {
            var fieldInfos = GetFieldInfos(type);
            if (fieldInfos.Length == 0) fieldInfos = null;

            return new InjectTypeInfo(
                GetConstructorInfo(type),
                GetMethodInfo(type),
                fieldInfos);
        }

        static InjectTypeInfo.InjectConstructorInfo GetConstructorInfo(Type type)
        {
            if (type.IsSubclassOf(typeof(UnityEngine.Object)))
                return default;

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
                if (fieldType.IsGenericType)
                    continue;

                var injectAttr = field.GetCustomAttribute<InjectAttributeBase>();
                if (injectAttr == null)
                    continue;

                var fieldInfo = new InjectTypeInfo.InjectFieldInfo(field, GetInjectableInfoForMember(field, injectAttr));
                _fieldInfoBuffer.Add(fieldInfo);
            }

            return _fieldInfoBuffer.ToArray();

            static InjectSpec GetInjectableInfoForMember(FieldInfo fieldInfo, InjectAttributeBase injectAttr)
            {
                return injectAttr != null
                    ? new InjectSpec(fieldInfo.FieldType, injectAttr.Id, injectAttr.Source, injectAttr.Optional)
                    : new InjectSpec(fieldInfo.FieldType, 0, InjectSources.Any);
            }
        }

        static InjectSpec[] BakeInjectParameterInfos(MethodBase methodInfo)
        {
            var paramInfos = methodInfo.GetParameters();
            if (paramInfos.Length == 0) return _emptyInjectableArray;

            var injectParamInfos = new InjectSpec[paramInfos.Length];
            for (var i = 0; i < paramInfos.Length; i++)
                injectParamInfos[i] = CreateInjectableInfoForParam(paramInfos[i]);
            return injectParamInfos;

            static InjectSpec CreateInjectableInfoForParam(ParameterInfo paramInfo)
            {
                var injectAttr = paramInfo.GetCustomAttribute<InjectAttributeBase>();
                return injectAttr != null
                    ? new InjectSpec(paramInfo.ParameterType, injectAttr.Id, injectAttr.Source, injectAttr.Optional)
                    : new InjectSpec(paramInfo.ParameterType, 0, InjectSources.Any);
            }
        }
    }
}