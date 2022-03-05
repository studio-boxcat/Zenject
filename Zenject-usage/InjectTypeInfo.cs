using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;

namespace Zenject
{
    [NoReflectionBaking]
    public class InjectTypeInfo
    {
        public readonly Type Type;
        public readonly InjectConstructorInfo InjectConstructor;
        public readonly InjectMethodInfo[] InjectMethods;
        public readonly InjectFieldInfo[] InjectFields;
        public readonly InjectPropertyInfo[] InjectProperties;


        public InjectTypeInfo(
            Type type,
            InjectConstructorInfo injectConstructor,
            InjectMethodInfo[] injectMethods,
            InjectFieldInfo[] injectFields,
            InjectPropertyInfo[] injectProperties)
        {
            Type = type;
            InjectConstructor = injectConstructor;
            InjectMethods = injectMethods;
            InjectFields = injectFields;
            InjectProperties = injectProperties;
        }

        // Filled in later
        public InjectTypeInfo BaseTypeInfo
        {
            get; set;
        }

        public IEnumerable<InjectableInfo> AllInjectables
        {
            get
            {
                foreach (var info in InjectConstructor.Parameters)
                    yield return info;

                foreach (var info in InjectFields)
                    yield return info.Info;

                foreach (var info in InjectProperties)
                    yield return info.Info;

                foreach (var info in InjectMethods)
                foreach(var paramInfo in info.Parameters)
                {
                    yield return paramInfo;
                }
            }
        }

        public interface IInjectMemberSetter
        {
            void Invoke(object injectable, object value);
        }

        public struct InjectFieldInfo : IInjectMemberSetter
        {
            public readonly FieldInfo FieldInfo;
            public readonly InjectableInfo Info;

            public InjectFieldInfo(FieldInfo fieldInfo, InjectableInfo info) : this()
            {
                FieldInfo = fieldInfo;
                Info = info;
            }

            public void Invoke(object injectable, object value)
            {
                FieldInfo.SetValue(injectable, value);
            }
        }


        public struct InjectPropertyInfo : IInjectMemberSetter
        {
            public readonly PropertyInfo PropertyInfo;
            public readonly InjectableInfo Info;

            public InjectPropertyInfo(PropertyInfo propertyInfo, InjectableInfo info) : this()
            {
                PropertyInfo = propertyInfo;
                Info = info;
            }

            public void Invoke(object injectable, object value)
            {
                PropertyInfo.SetValue(injectable, value);
            }
        }

        public struct InjectConstructorInfo
        {
            // Null for abstract types
            public readonly ConstructorInfo ConstructorInfo;
            public readonly InjectableInfo[] Parameters;

            public InjectConstructorInfo(ConstructorInfo constructorInfo, InjectableInfo[] parameters)
            {
                ConstructorInfo = constructorInfo;
                Parameters = parameters;
            }
        }

        public struct InjectMethodInfo
        {
            public readonly MethodInfo MethodInfo;
            public readonly InjectableInfo[] Parameters;

            public InjectMethodInfo(MethodInfo methodInfo, InjectableInfo[] parameters)
            {
                MethodInfo = methodInfo;
                Parameters = parameters;
            }
        }
    }
}
