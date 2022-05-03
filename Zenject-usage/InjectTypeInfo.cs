using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace Zenject
{
    public readonly struct InjectTypeInfo
    {
        public readonly InjectConstructorInfo Constructor;
        public readonly InjectMethodInfo Method;
        [CanBeNull]
        public readonly InjectFieldInfo[] Fields;


        public InjectTypeInfo(
            InjectConstructorInfo constructor,
            InjectMethodInfo method,
            InjectFieldInfo[] fields)
        {
            Assert.AreEqual(constructor.ConstructorInfo == null, constructor.Parameters == null);
            Assert.AreEqual(method.MethodInfo == null, method.Parameters == null);
            Assert.IsTrue(fields == null || fields.Length > 0);

            Constructor = constructor;
            Method = method;
            Fields = fields;
        }

        public bool IsInjectionRequired()
        {
            return Fields != null
                   || Method.MethodInfo != null
                   || Constructor.Parameters != null;
        }

        public readonly struct InjectFieldInfo
        {
            public readonly FieldInfo FieldInfo;
            public readonly InjectSpec Info;

            public InjectFieldInfo(FieldInfo fieldInfo, InjectSpec info) : this()
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
            public readonly InjectSpec[] Parameters;

            public InjectConstructorInfo(ConstructorInfo constructorInfo, InjectSpec[] parameters)
            {
                ConstructorInfo = constructorInfo;
                Parameters = parameters;
            }
        }

        public readonly struct InjectMethodInfo
        {
            [CanBeNull]
            public readonly MethodInfo MethodInfo;
            public readonly InjectSpec[] Parameters;

            public InjectMethodInfo(MethodInfo methodInfo, InjectSpec[] parameters)
            {
                MethodInfo = methodInfo;
                Parameters = parameters;
            }
        }
    }
}