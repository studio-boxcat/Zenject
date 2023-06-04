using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace Zenject
{
    public interface IConstructorHook
    {
        bool TryCreateInstance(Type concreteType, DiContainer container, ArgumentArray extraArgs, out object instance);
    }

    public static class Constructor
    {
        [CanBeNull]
        public static IConstructorHook Hook;

        static Binder _binder;
        static CultureInfo _cultureInfo;

        public static object Instantiate(Type concreteType, DiContainer container, ArgumentArray extraArgs)
        {
            // If the given type has generated constructor, use it.
            if (Hook != null && Hook.TryCreateInstance(concreteType, container, extraArgs, out var instance))
                return instance;

            // If the constructor has no parameters, instantiate by Activator.
            // Note that calling Activator.CreateInstance() is 2x faster than calling ConstructorInfo.Invoke().
            var constructorInfo = GetConstructorInfo(concreteType);
            if (constructorInfo.Parameters.Length == 0)
            {
                _binder ??= Type.DefaultBinder;
                _cultureInfo ??= CultureInfo.InvariantCulture;
                return Activator.CreateInstance(
                    concreteType,
                    BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    _binder,
                    Array.Empty<object>(),
                    _cultureInfo);
            }

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
            UnityEngine.Debug.LogWarning("[Zenject] Unregistered type detected: " + type.PrettyName());
#endif

            return constructorInfo;
        }
    }
}