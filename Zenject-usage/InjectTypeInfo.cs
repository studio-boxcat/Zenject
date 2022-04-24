using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace Zenject
{
    public readonly struct InjectTypeInfo
    {
        public readonly InjectConstructorInfo InjectConstructor;
        public readonly InjectMethodInfo InjectMethod;
        [CanBeNull]
        public readonly InjectFieldInfo[] InjectFields;


        public InjectTypeInfo(
            InjectConstructorInfo injectConstructor,
            InjectMethodInfo injectMethod,
            InjectFieldInfo[] injectFields)
        {
            Assert.AreEqual(injectConstructor.ConstructorInfo == null, injectConstructor.Parameters == null);
            Assert.AreEqual(injectMethod.MethodInfo == null, injectMethod.Parameters == null);
            Assert.IsTrue(injectFields == null || injectFields.Length > 0);

            InjectConstructor = injectConstructor;
            InjectMethod = injectMethod;
            InjectFields = injectFields;
        }

        public bool IsInjectionRequired()
        {
            return InjectFields != null
                   || InjectMethod.MethodInfo != null
                   || InjectConstructor.Parameters.Length > 0;
        }

        public readonly struct InjectFieldInfo
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

        public readonly struct InjectConstructorInfo
        {
            [CanBeNull]
            public readonly ConstructorInfo ConstructorInfo;
            public readonly InjectableInfo[] Parameters;

            public InjectConstructorInfo(ConstructorInfo constructorInfo, InjectableInfo[] parameters)
            {
                ConstructorInfo = constructorInfo;
                Parameters = parameters;
            }
        }

        public readonly struct InjectMethodInfo
        {
            [CanBeNull]
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