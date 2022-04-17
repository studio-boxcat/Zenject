using System.Reflection;

namespace Zenject
{
    public readonly struct InjectTypeInfo
    {
        public readonly InjectConstructorInfo InjectConstructor;
        public readonly InjectMethodInfo[] InjectMethods;
        public readonly InjectFieldInfo[] InjectFields;
        public readonly InjectPropertyInfo[] InjectProperties;


        public InjectTypeInfo(
            InjectConstructorInfo injectConstructor,
            InjectMethodInfo[] injectMethods,
            InjectFieldInfo[] injectFields,
            InjectPropertyInfo[] injectProperties)
        {
            InjectConstructor = injectConstructor;
            InjectMethods = injectMethods;
            InjectFields = injectFields;
            InjectProperties = injectProperties;
        }

        public bool IsInjectionRequired()
        {
            return InjectFields.Length != 0
                   || InjectProperties.Length != 0
                   || InjectMethods.Length != 0
                   || InjectConstructor.ConstructorInfo != null;
        }

        public interface IInjectMemberSetter
        {
            void Invoke(object injectable, object value);
        }

        public readonly struct InjectFieldInfo : IInjectMemberSetter
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

        public readonly struct InjectPropertyInfo : IInjectMemberSetter
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