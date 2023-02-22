using System.Reflection;
using JetBrains.Annotations;

namespace Zenject
{
    public readonly struct InjectConstructorInfo
    {
        [NotNull]
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
}