using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Zenject.Internal;

namespace Zenject
{
    public static class Constructor
    {
        public static object Instantiate(Type concreteType, DiContainer container, ArgumentArray extraArgs)
        {
            // If the given type has generated constructor, use it.
            var constructor = GetGeneratedConstructor(concreteType);
            if (constructor != null)
            {
                var @params = RentGeneratedConstructorParams(container, extraArgs);
                var inst = constructor.Invoke(parameters: @params);
                ReturnGeneratedConstructorParams(@params);
                return inst;
            }

#if DEBUG
            Debug.Log("[Zenject] Analyze constructor with Reflection: " + concreteType.Name);
#endif

            // If the constructor has no parameters, just invoke it.
            var constructorInfo = GetConstructorInfo(concreteType);
            if (constructorInfo.Parameters.Length == 0)
                return constructorInfo.ConstructorInfo.Invoke(null);


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

        static readonly Dictionary<Type, ConstructorInfo> _generatedConstructorCache = new();
        static readonly Type[] _generatedConstructorParamTypes = {typeof(DependencyProviderRef)};

        [CanBeNull]
        static ConstructorInfo GetGeneratedConstructor(Type type)
        {
            if (_generatedConstructorCache.TryGetValue(type, out var constructor))
                return constructor;

            constructor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, _generatedConstructorParamTypes, null);
            _generatedConstructorCache.Add(type, constructor);
            return constructor;
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