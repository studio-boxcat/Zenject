using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Zenject
{
    public static class Constructor
    {
        public static object Instantiate(Type concreteType, DiContainer container, ArgumentArray extraArgs)
        {
            // If the given type has generated constructor, use it.
            var @params = RentGeneratedConstructorParams(container, extraArgs);
            try
            {
                return Activator.CreateInstance(concreteType, args: @params);
            }
            catch (MissingMethodException)
            {
            }
            finally
            {
                ReturnGeneratedConstructorParams(@params);
            }

            // If the constructor has no parameters, instantiate by Activator.
            // Note that calling Activator.CreateInstance() is 2x faster than calling ConstructorInfo.Invoke().
            var constructorInfo = GetConstructorInfo(concreteType);
            if (constructorInfo.Parameters.Length == 0)
                return Activator.CreateInstance(concreteType);

            // Otherwise, resolve the parameters and invoke the constructor.
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

#if DEBUG && ZENJECT_REFLECTION_BAKING
            if (constructorInfo.Parameters.Length > 0)
                UnityEngine.Debug.LogWarning("[Zenject] Unregistered type detected: " + type.PrettyName());
#endif

            return constructorInfo;
        }

        static readonly Stack<object[]> _generatedConstructorParamsPool = new();

        static object[] RentGeneratedConstructorParams(DiContainer container, ArgumentArray extraArgs)
        {
            if (_generatedConstructorParamsPool.Count == 0)
                return new object[] {new DependencyProviderRef(container, extraArgs)};

            var paramValues = _generatedConstructorParamsPool.Pop();
            var dp = (DependencyProviderRef) paramValues[0];
            dp.Reset(container, extraArgs);
            return paramValues;
        }

        static void ReturnGeneratedConstructorParams(object[] paramValues)
        {
            var dp = (DependencyProviderRef) paramValues[0];
            dp.Reset();
            _generatedConstructorParamsPool.Push(paramValues);
        }
    }
}