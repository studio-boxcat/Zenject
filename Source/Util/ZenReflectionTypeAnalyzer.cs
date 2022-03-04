using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModestTree;
#if !NOT_UNITY3D
using UnityEngine;
#endif

namespace Zenject.Internal
{
    public static class ReflectionTypeAnalyzer
    {
        public static ReflectionTypeInfo GetReflectionInfo(Type type)
        {
            Assert.That(!type.IsEnum(), "Tried to analyze enum type '{0}'.  This is not supported", type);
            Assert.That(!type.IsArray, "Tried to analyze array type '{0}'.  This is not supported", type);

            var baseType = type.BaseType();

            if (baseType == typeof(object))
            {
                baseType = null;
            }

            return new ReflectionTypeInfo(
                type, baseType, GetConstructorInfo(type), GetMethodInfos(type),
                GetFieldInfos(type), GetPropertyInfos(type));
        }

        static List<ReflectionTypeInfo.InjectPropertyInfo> GetPropertyInfos(Type type)
        {
            var list = new List<ReflectionTypeInfo.InjectPropertyInfo>();
            foreach (var property in type.DeclaredInstanceProperties())
            {
                var injectAttr = property.GetCustomAttribute<InjectAttributeBase>();
                if (injectAttr == null) continue;
                var propertyInfo = new ReflectionTypeInfo.InjectPropertyInfo(property, GetInjectableInfoForMember(property, injectAttr));
                list.Add(propertyInfo);
            }
            return list;
        }

        static List<ReflectionTypeInfo.InjectFieldInfo> GetFieldInfos(Type type)
        {
            var list = new List<ReflectionTypeInfo.InjectFieldInfo>();
            foreach (var field in type.DeclaredInstanceFields())
            {
                var injectAttr = field.GetCustomAttribute<InjectAttributeBase>();
                if (injectAttr == null) continue;
                var propertyInfo = new ReflectionTypeInfo.InjectFieldInfo(field, GetInjectableInfoForMember(field, injectAttr));
                list.Add(propertyInfo);
            }
            return list;
        }

        static List<ReflectionTypeInfo.InjectMethodInfo> GetMethodInfos(Type type)
        {
            var injectMethodInfos = new List<ReflectionTypeInfo.InjectMethodInfo>();

            // Note that unlike with fields and properties we use GetCustomAttributes
            // This is so that we can ignore inherited attributes, which is necessary
            // otherwise a base class method marked with [Inject] would cause all overridden
            // derived methods to be added as well
            foreach (var methodInfo in type.DeclaredInstanceMethods())
            {
                if (methodInfo.IsDefined(typeof(InjectAttributeBase)) == false) continue;
                var injectMethodInfo = new ReflectionTypeInfo.InjectMethodInfo(methodInfo, BakeInjectParameterInfos(type, methodInfo));
                injectMethodInfos.Add(injectMethodInfo);
            }

            return injectMethodInfos;
        }

        static ReflectionTypeInfo.InjectConstructorInfo GetConstructorInfo(Type type)
        {
            var constructor = TryGetInjectConstructor(type);
            return constructor != null
                ? new ReflectionTypeInfo.InjectConstructorInfo(constructor, BakeInjectParameterInfos(type, constructor))
                : new ReflectionTypeInfo.InjectConstructorInfo(null, Array.Empty<ReflectionTypeInfo.InjectParameterInfo>());
        }

        static ReflectionTypeInfo.InjectParameterInfo[] BakeInjectParameterInfos(Type type, MethodBase methodInfo)
        {
            var paramInfos =  methodInfo.GetParameters();
            var injectParamInfos = new ReflectionTypeInfo.InjectParameterInfo[paramInfos .Length];
            for (var i = 0; i < paramInfos.Length; i++)
                injectParamInfos[i] = CreateInjectableInfoForParam(type, paramInfos[i]);
            return injectParamInfos;
        }

        static ReflectionTypeInfo.InjectParameterInfo CreateInjectableInfoForParam(
            Type parentType, ParameterInfo paramInfo)
        {
            var injectAttributes = paramInfo.GetCustomAttributes<InjectAttributeBase>().ToList();

            Assert.That(injectAttributes.Count <= 1,
                "Found multiple 'Inject' attributes on type parameter '{0}' of type '{1}'.  Parameter should only have one", paramInfo.Name, parentType);

            var injectAttr = injectAttributes.SingleOrDefault();

            object identifier = null;
            bool isOptional = false;
            InjectSources sourceType = InjectSources.Any;

            if (injectAttr != null)
            {
                identifier = injectAttr.Id;
                isOptional = injectAttr.Optional;
                sourceType = injectAttr.Source;
            }

            bool isOptionalWithADefaultValue = (paramInfo.Attributes & ParameterAttributes.HasDefault) == ParameterAttributes.HasDefault;

            return new ReflectionTypeInfo.InjectParameterInfo(
                paramInfo,
                new InjectableInfo(
                    isOptionalWithADefaultValue || isOptional,
                    identifier,
                    paramInfo.Name,
                    paramInfo.ParameterType,
                    isOptionalWithADefaultValue ? paramInfo.DefaultValue : null,
                    sourceType));
        }

        static InjectableInfo GetInjectableInfoForMember(MemberInfo memInfo, InjectAttributeBase injectAttr)
        {
            object identifier = null;
            bool isOptional = false;
            InjectSources sourceType = InjectSources.Any;

            if (injectAttr != null)
            {
                identifier = injectAttr.Id;
                isOptional = injectAttr.Optional;
                sourceType = injectAttr.Source;
            }

            Type memberType = memInfo is FieldInfo
                ? ((FieldInfo)memInfo).FieldType : ((PropertyInfo)memInfo).PropertyType;

            return new InjectableInfo(
                isOptional,
                identifier,
                memInfo.Name,
                memberType,
                null,
                sourceType);
        }

        static ConstructorInfo TryGetInjectConstructor(Type type)
        {
#if !NOT_UNITY3D
            if (type.DerivesFromOrEqual<Component>())
            {
                return null;
            }
#endif

            if (type.IsAbstract())
            {
                return null;
            }

            var constructors = type.Constructors();

            if (constructors.Length == 0)
                return null;

            if (constructors.Length == 1)
                return constructors[0];

            foreach (var constructor in constructors)
            {
                if (constructor.IsDefined(typeof(InjectAttributeBase)))
                    return constructor;
            }

            throw new Exception("이용가능한 생성자가 2개 이상입니다.");
        }

#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
        static bool IsWp8GeneratedConstructor(ConstructorInfo c)
        {
            ParameterInfo[] args = c.GetParameters();

            if (args.Length == 1)
            {
                return args[0].ParameterType == typeof(UIntPtr)
                    && (string.IsNullOrEmpty(args[0].Name) || args[0].Name == "dummy");
            }

            if (args.Length == 2)
            {
                return args[0].ParameterType == typeof(UIntPtr)
                    && args[1].ParameterType == typeof(Int64*)
                    && (string.IsNullOrEmpty(args[0].Name) || args[0].Name == "dummy")
                    && (string.IsNullOrEmpty(args[1].Name) || args[1].Name == "dummy");
            }

            return false;
        }
#endif
    }
}
