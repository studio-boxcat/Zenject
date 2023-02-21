using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Zenject.Internal;

namespace Zenject
{
    public static class Constructor
    {
        public static object Instantiate(Type concreteType, DiContainer container, ArgumentArray extraArgs)
        {
            var constructorInfo = GetConstructorInfo(concreteType);
            if (constructorInfo.Parameters.Length == 0)
                return constructorInfo.ConstructorInfo.Invoke(null);

#if DEBUG
            try
#endif
            {
                var paramValues = ParamArrayPool.Rent(constructorInfo.Parameters.Length);
                ParamUtils.ResolveParams(container, constructorInfo.Parameters, paramValues, extraArgs);
                var newObj = constructorInfo.ConstructorInfo.Invoke(paramValues);
                ParamArrayPool.Release(paramValues);
                return newObj;
            }
#if DEBUG
            catch (ParamResolveException e)
            {
                throw new MethodInvokeException(constructorInfo.ConstructorInfo, e.ParamSpec, e.ParamIndex, e);
            }
#endif
        }

        static readonly Dictionary<Type, InjectConstructorInfo> _constructorCache = new();

        static InjectConstructorInfo GetConstructorInfo(Type type)
        {
            Assert.IsFalse(type.IsSubclassOf(typeof(UnityEngine.Object)));

            if (_constructorCache.TryGetValue(type, out var constructorInfo))
                return constructorInfo;

            constructorInfo = TypeAnalyzer.GetConstructorInfo(type);
            _constructorCache.Add(type, constructorInfo);
            return constructorInfo;
        }
    }
}