using System;
using System.Linq;
using System.Reflection;
using ModestTree;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Zenject.Internal
{
    public static class ReflectionTypeAnalyzer
    {
        public static InjectTypeInfo GetReflectionInfo(Type type)
        {
            return new InjectTypeInfo(
                GetConstructorInfo(type),
                GetMethodInfo(type),
                GetFieldInfos(type));
        }

        static InjectTypeInfo.InjectFieldInfo[] GetFieldInfos(Type type)
        {
            var list = ListPool<InjectTypeInfo.InjectFieldInfo>.Get();

            while (type != null
                   && type != typeof(object)
                   && type != typeof(Object)
                   && type != typeof(MonoBehaviour)
                   && type != typeof(ScriptableObject))
            {
                foreach (var field in type.InstanceFields())
                {
                    var injectAttr = field.GetCustomAttribute<InjectAttributeBase>();
                    if (injectAttr == null) continue;
                    var propertyInfo = new InjectTypeInfo.InjectFieldInfo(field, GetInjectableInfoForMember(field, injectAttr));
                    list.Add(propertyInfo);
                }

                type = type.BaseType;
            }

            var arr = list.ToArray();
            ListPool<InjectTypeInfo.InjectFieldInfo>.Release(list);
            return arr;
        }

        static InjectTypeInfo.InjectMethodInfo GetMethodInfo(Type type)
        {
            var methodInfos = type.InstanceMethods();
            Assert.That(methodInfos.Count(x => x.IsDefined(typeof(InjectAttributeBase))) <= 1);

            // Note that unlike with fields and properties we use GetCustomAttributes
            // This is so that we can ignore inherited attributes, which is necessary
            // otherwise a base class method marked with [Inject] would cause all overridden
            // derived methods to be added as well
            foreach (var methodInfo in methodInfos)
            {
                if (methodInfo.IsDefined(typeof(InjectAttributeBase)) == false) continue;
                return new InjectTypeInfo.InjectMethodInfo(methodInfo, BakeInjectParameterInfos(methodInfo));
            }

            return default;
        }

        static InjectTypeInfo.InjectConstructorInfo GetConstructorInfo(Type type)
        {
            var constructor = TryGetInjectConstructor(type);
            return constructor != null
                ? new InjectTypeInfo.InjectConstructorInfo(constructor, BakeInjectParameterInfos(constructor))
                : new InjectTypeInfo.InjectConstructorInfo(null, Array.Empty<InjectableInfo>());
        }

        static InjectableInfo[] BakeInjectParameterInfos(MethodBase methodInfo)
        {
            var paramInfos = methodInfo.GetParameters();
            var injectParamInfos = new InjectableInfo[paramInfos.Length];
            for (var i = 0; i < paramInfos.Length; i++)
                injectParamInfos[i] = CreateInjectableInfoForParam(paramInfos[i]);
            return injectParamInfos;
        }

        static InjectableInfo CreateInjectableInfoForParam(ParameterInfo paramInfo)
        {
            var injectAttr = paramInfo.GetCustomAttribute<InjectAttributeBase>();
            return injectAttr != null
                ? new InjectableInfo(paramInfo.ParameterType, injectAttr.Id, injectAttr.Source, injectAttr.Optional)
                : new InjectableInfo(paramInfo.ParameterType, 0, InjectSources.Any);
        }

        static InjectableInfo GetInjectableInfoForMember(FieldInfo fieldInfo, InjectAttributeBase injectAttr)
        {
            return injectAttr != null
                ? new InjectableInfo(fieldInfo.FieldType, injectAttr.Id, injectAttr.Source, injectAttr.Optional)
                : new InjectableInfo(fieldInfo.FieldType, 0, InjectSources.Any);
        }

        static ConstructorInfo TryGetInjectConstructor(Type type)
        {
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