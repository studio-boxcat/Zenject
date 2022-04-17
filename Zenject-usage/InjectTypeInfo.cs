using System.Reflection;

namespace Zenject
{
    public readonly struct InjectTypeInfo
    {
        public readonly InjectConstructorInfo InjectConstructor;
        public readonly InjectMethodInfo InjectMethod;
        public readonly InjectFieldInfo[] InjectFields;


        public InjectTypeInfo(
            InjectConstructorInfo injectConstructor,
            InjectMethodInfo injectMethod,
            InjectFieldInfo[] injectFields)
        {
            InjectConstructor = injectConstructor;
            InjectMethod = injectMethod;
            InjectFields = injectFields;
        }

        public bool IsInjectionRequired()
        {
            return InjectFields.Length != 0
                   || InjectMethod.MethodInfo != null
                   || InjectConstructor.ConstructorInfo != null;
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
            // Null for abstract types
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