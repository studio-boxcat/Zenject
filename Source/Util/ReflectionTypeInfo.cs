using System;
using System.Collections.Generic;
using System.Reflection;

namespace Zenject.Internal
{
    public struct ReflectionTypeInfo
    {
        public readonly Type Type;
        public readonly Type BaseType;
        public readonly List<InjectPropertyInfo> InjectProperties;
        public readonly List<InjectFieldInfo> InjectFields;
        public readonly InjectConstructorInfo InjectConstructor;
        public readonly List<InjectMethodInfo> InjectMethods;

        public ReflectionTypeInfo(
            Type type,
            Type baseType,
            InjectConstructorInfo injectConstructor,
            List<InjectMethodInfo> injectMethods,
            List<InjectFieldInfo> injectFields,
            List<InjectPropertyInfo> injectProperties)
        {
            Type = type;
            BaseType = baseType;
            InjectFields = injectFields;
            InjectConstructor = injectConstructor;
            InjectMethods = injectMethods;
            InjectProperties = injectProperties;
        }

        public struct InjectFieldInfo
        {
            public readonly FieldInfo FieldInfo;
            public readonly InjectableInfo InjectableInfo;

            public InjectFieldInfo(
                FieldInfo fieldInfo,
                InjectableInfo injectableInfo)
            {
                InjectableInfo = injectableInfo;
                FieldInfo = fieldInfo;
            }
        }

        public struct InjectParameterInfo
        {
            public readonly ParameterInfo ParameterInfo;
            public readonly InjectableInfo InjectableInfo;

            public InjectParameterInfo(
                ParameterInfo parameterInfo,
                InjectableInfo injectableInfo)
            {
                InjectableInfo = injectableInfo;
                ParameterInfo = parameterInfo;
            }
        }

        public struct InjectPropertyInfo
        {
            public readonly PropertyInfo PropertyInfo;
            public readonly InjectableInfo InjectableInfo;

            public InjectPropertyInfo(
                PropertyInfo propertyInfo,
                InjectableInfo injectableInfo)
            {
                InjectableInfo = injectableInfo;
                PropertyInfo = propertyInfo;
            }
        }

        public struct InjectMethodInfo
        {
            public readonly MethodBase MethodInfo;
            public readonly InjectParameterInfo[] Parameters;

            public InjectMethodInfo(
                MethodBase methodInfo,
                InjectParameterInfo[] parameters)
            {
                MethodInfo = methodInfo;
                Parameters = parameters;
            }

            public InjectableInfo[] BakeParameterInjectableInfoArray()
            {
                var arr = new InjectableInfo[Parameters.Length];
                for (var i = 0; i < arr.Length; i++)
                    arr[i] = Parameters[i].InjectableInfo;
                return arr;
            }
        }

        public struct InjectConstructorInfo
        {
            public readonly ConstructorInfo ConstructorInfo;
            public readonly InjectParameterInfo[] Parameters;

            public InjectConstructorInfo(
                ConstructorInfo constructorInfo,
                InjectParameterInfo[] parameters)
            {
                ConstructorInfo = constructorInfo;
                Parameters = parameters;
            }

            public InjectableInfo[] BakeParameterInjectableInfoArray()
            {
                var arr = new InjectableInfo[Parameters.Length];
                for (var i = 0; i < arr.Length; i++)
                    arr[i] = Parameters[i].InjectableInfo;
                return arr;
            }
        }
    }
}

