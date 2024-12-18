using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public static class Injector
    {
        static readonly Dictionary<Type, TypeInfo> _typeInfoCache = new();

        public static void Inject(object inst, DiContainer diContainer, ArgumentArray extraArgs)
        {
#if DEBUG
            if (inst is ScriptableObject)
                L.W("Injecting ScriptableObject could lead to unexpected behavior");
#endif

            // If the object implements IZenjectInjectable, then we call Initialize.
            if (inst is IZenjectInjectable injectable)
            {
                injectable.Inject(diContainer, extraArgs);
                return;
            }

            var initializerInfo = GetInfo(inst.GetType());

            if (initializerInfo.Fields != null)
            {
                foreach (var injectField in initializerInfo.Fields)
                    InjectMember(inst, diContainer, injectField, injectField.Info, extraArgs);
            }

            var method = initializerInfo.Method;
            if (method.MethodInfo != null)
                InjectMethod(inst, diContainer, method, extraArgs);
        }

        public static bool IsInjectionRequired(Type type)
        {
            return typeof(IZenjectInjectable).IsAssignableFrom(type)
                   || GetInfo(type).IsInjectionRequired();
        }

        static TypeInfo GetInfo(Type type)
        {
            if (_typeInfoCache.TryGetValue(type, out var initializer))
                return initializer;

#if !UNITY_EDITOR
            L.W($"Reflection baking is disabled for {type.Name}");
#endif

            var fieldInfos = TypeAnalyzer.GetFieldInfos(type, false);
            if (fieldInfos.Length == 0) fieldInfos = null;
            var methodInfo = TypeAnalyzer.GetMethodInfo(type, false);
            initializer = new TypeInfo(fieldInfos, methodInfo);
            _typeInfoCache.Add(type, initializer);

#if DEBUG && !UNITY_EDITOR // Only when reflection baking is enabled
            if (initializer.Fields is {Length: > 0} || initializer.Method.MethodInfo != null)
                L.W($"Unregistered type detected: {type.Name}");
#endif

            return initializer;
        }

        public static void ClearCache()
        {
            _typeInfoCache.Clear();
        }

        static void InjectMember(
            object inst, DiContainer container, InjectFieldInfo setter, InjectSpec injectSpec, ArgumentArray extraArgs)
        {
            if (extraArgs.TryGet(injectSpec.Type, out var value))
            {
                setter.Invoke(inst, value);
                return;
            }

#if DEBUG
            try
#endif
            {
                value = container.Resolve(injectSpec);
            }
#if DEBUG
            catch (Exception e)
            {
                L.E($"Failed to resolve field: {injectSpec.ToString()} → {inst.GetType()}", inst as UnityEngine.Object);
                L.E(e, inst as UnityEngine.Object);
                throw;
            }
#endif

            if (injectSpec.Optional && value is null)
            {
                // Do not overwrite in this case, so it retains the hard-coded value
            }
            else
            {
                setter.Invoke(inst, value);
            }
        }

        static void InjectMethod(
            object inst, DiContainer container, InjectMethodInfo method, ArgumentArray extraArgs)
        {
#if DEBUG
            try
#endif
            {
                var paramValues = ParamArrayPool.Rent(method.Parameters.Length);
                ParamUtils.ResolveParams(container, method.Parameters, paramValues, extraArgs);
                method.MethodInfo!.Invoke(inst, paramValues);
                ParamArrayPool.Release(paramValues);
            }
#if DEBUG
            catch (ParamResolveException e)
            {
                throw new MethodInvokeException(method.MethodInfo, e.ParamSpec, e.ParamIndex, e);
            }
#endif
        }

        readonly struct TypeInfo
        {
            [CanBeNull] public readonly InjectFieldInfo[] Fields;
            public readonly InjectMethodInfo Method;


            public TypeInfo(
                InjectFieldInfo[] fields,
                InjectMethodInfo method)
            {
                Assert.IsTrue(fields == null || fields.Length > 0);
                Assert.AreEqual(method.MethodInfo == null, method.Parameters == null);

                Fields = fields;
                Method = method;
            }

            public bool IsInjectionRequired()
            {
                return Fields != null
                       || Method.MethodInfo != null;
            }
        }
    }
}