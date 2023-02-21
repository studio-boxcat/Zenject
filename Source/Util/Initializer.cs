using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject.Internal;

namespace Zenject
{
    public static class Initializer
    {
        static readonly Dictionary<Type, InitializerInfo> _initializers = new();

        public static void Initialize(object inst, DiContainer diContainer, ArgumentArray extraArgs)
        {
            // If the object implements IZenject_Initializable, then we call Initialize.
            if (inst is IZenject_Initializable initializable)
            {
                initializable.Initialize(new DependencyProvider(diContainer, extraArgs));
                return;
            }

            Debug.Log("[Zenject] Analyze initializer with Reflection: " + inst.GetType().Name);

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

        public static bool IsInjectionRequired(object inst)
        {
            return inst is IZenject_Initializable
                   || GetInfo(inst.GetType()).IsInjectionRequired();
        }

        static InitializerInfo GetInfo(Type type)
        {
            if (_initializers.TryGetValue(type, out var initializer))
                return initializer;

            var fieldInfos = TypeAnalyzer.GetFieldInfos(type, false);
            if (fieldInfos.Length == 0) fieldInfos = null;
            var methodInfo = TypeAnalyzer.GetMethodInfo(type, false);
            initializer = new InitializerInfo(fieldInfos, methodInfo);
            _initializers.Add(type, initializer);
            return initializer;
        }

        static void InjectMember(
            object inst, DiContainer container, InjectFieldInfo setter, InjectSpec injectSpec, ArgumentArray extraArgs)
        {
            if (extraArgs.TryGetValueWithType(injectSpec.Type, out var value))
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
                throw new FieldResolveException(inst.GetType(), injectSpec, e);
            }
#endif

            if (injectSpec.Optional && value == null)
            {
                // Do not override in this case so it retains the hard-coded value
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
                method.MethodInfo.Invoke(inst, paramValues);
                ParamArrayPool.Release(paramValues);
            }
#if DEBUG
            catch (ParamResolveException e)
            {
                throw new MethodInvokeException(method.MethodInfo, e.ParamSpec, e.ParamIndex, e);
            }
#endif
        }
    }
}