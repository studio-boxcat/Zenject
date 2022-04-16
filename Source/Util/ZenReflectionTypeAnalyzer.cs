using System;
using System.Linq;
using System.Reflection;
using ModestTree;
using UnityEngine;
using UnityEngine.Pool;

namespace Zenject.Internal
{
    public static class ReflectionTypeAnalyzer
    {
        public static InjectTypeInfo GetReflectionInfo(Type type)
        {
            Assert.That(!type.IsEnum(), "Tried to analyze enum type '{0}'.  This is not supported", type);
            Assert.That(!type.IsArray, "Tried to analyze array type '{0}'.  This is not supported", type);

            return new InjectTypeInfo(
                type,
                GetConstructorInfo(type),
                GetMethodInfos(type),
                GetFieldInfos(type),
                GetPropertyInfos(type));
        }

        static InjectTypeInfo.InjectPropertyInfo[] GetPropertyInfos(Type type)
        {
            var list = ListPool<InjectTypeInfo.InjectPropertyInfo>.Get();

            foreach (var property in type.DeclaredInstanceProperties())
            {
                var injectAttr = property.GetCustomAttribute<InjectAttributeBase>();
                if (injectAttr == null) continue;
                var propertyInfo = new InjectTypeInfo.InjectPropertyInfo(property, GetInjectableInfoForMember(property, injectAttr));
                list.Add(propertyInfo);
            }

            var arr = list.ToArray();
            ListPool<InjectTypeInfo.InjectPropertyInfo>.Release(list);
            return arr;
        }

        static InjectTypeInfo.InjectFieldInfo[] GetFieldInfos(Type type)
        {
            var list = ListPool<InjectTypeInfo.InjectFieldInfo>.Get();

            foreach (var field in type.DeclaredInstanceFields())
            {
                var injectAttr = field.GetCustomAttribute<InjectAttributeBase>();
                if (injectAttr == null) continue;
                var propertyInfo = new InjectTypeInfo.InjectFieldInfo(field, GetInjectableInfoForMember(field, injectAttr));
                list.Add(propertyInfo);
            }

            var arr = list.ToArray();
            ListPool<InjectTypeInfo.InjectFieldInfo>.Release(list);
            return arr;
        }

        static InjectTypeInfo.InjectMethodInfo[] GetMethodInfos(Type type)
        {
            var list = ListPool<InjectTypeInfo.InjectMethodInfo>.Get();

            // Note that unlike with fields and properties we use GetCustomAttributes
            // This is so that we can ignore inherited attributes, which is necessary
            // otherwise a base class method marked with [Inject] would cause all overridden
            // derived methods to be added as well
            foreach (var methodInfo in type.DeclaredInstanceMethods())
            {
                if (methodInfo.IsDefined(typeof(InjectAttributeBase)) == false) continue;
                var injectMethodInfo = new InjectTypeInfo.InjectMethodInfo(methodInfo, BakeInjectParameterInfos(type, methodInfo));
                list.Add(injectMethodInfo);
            }

            var arr = list.ToArray();
            ListPool<InjectTypeInfo.InjectMethodInfo>.Release(list);
            return arr;
        }

        static InjectTypeInfo.InjectConstructorInfo GetConstructorInfo(Type type)
        {
            var constructor = TryGetInjectConstructor(type);
            return constructor != null
                ? new InjectTypeInfo.InjectConstructorInfo(constructor, BakeInjectParameterInfos(type, constructor))
                : new InjectTypeInfo.InjectConstructorInfo(null, Array.Empty<InjectableInfo>());
        }

        static InjectableInfo[] BakeInjectParameterInfos(Type type, MethodBase methodInfo)
        {
            var paramInfos =  methodInfo.GetParameters();
            var injectParamInfos = new InjectableInfo[paramInfos.Length];
            for (var i = 0; i < paramInfos.Length; i++)
                injectParamInfos[i] = CreateInjectableInfoForParam(type, paramInfos[i]);
            return injectParamInfos;
        }

        static InjectableInfo CreateInjectableInfoForParam(
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

            return new InjectableInfo(
                isOptionalWithADefaultValue || isOptional,
                identifier,
                paramInfo.Name,
                paramInfo.ParameterType,
                sourceType);
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
                sourceType);
        }

        static ConstructorInfo TryGetInjectConstructor(Type type)
        {
            if (type.DerivesFromOrEqual<Component>() || type.IsAbstract())
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
    }
}
