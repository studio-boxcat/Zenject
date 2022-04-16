using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using ModestTree.Util;
using Zenject;

namespace ModestTree
{
    public static class ReflectionUtil
    {
        public static Array CreateArray(Type elementType, List<object> instances)
        {
            var array = Array.CreateInstance(elementType, instances.Count);

            for (int i = 0; i < instances.Count; i++)
            {
                var instance = instances[i];

                if (instance != null)
                {
                    Assert.That(instance.GetType().DerivesFromOrEqual(elementType),
                        "Wrong type when creating array, expected something assignable from '"+ elementType +"', but found '" + instance.GetType() + "'");
                }

                array.SetValue(instance, i);
            }

            return array;
        }

        public static IList CreateGenericList(Type elementType, List<object> instances)
        {
            var listForWellKnownTypes = CreateGenericListForWellKnownTypes(elementType, instances);
            if (listForWellKnownTypes != null)
                return listForWellKnownTypes;

            var genericType = typeof(List<>).MakeGenericType(elementType);

            var list = (IList)Activator.CreateInstance(genericType);

            for (int i = 0; i < instances.Count; i++)
            {
                var instance = instances[i];

                if (instance != null)
                {
                    Assert.That(instance.GetType().DerivesFromOrEqual(elementType),
                        "Wrong type when creating generic list, expected something assignable from '"+ elementType +"', but found '" + instance.GetType() + "'");
                }

                list.Add(instance);
            }

            return list;
        }

        // XXX: Activator 를 통해서 리스트를 생성하는 것이 매우 느리기 때문에 몇몇 알려진 타입은 수동으로 객체를 생성함.
        [CanBeNull]
        static IList CreateGenericListForWellKnownTypes(Type elementType, List<object> instances)
        {
            var count = instances.Count;

            IList list = null;
            if (elementType == typeof(IDisposable))
                list = new List<IDisposable>(count);
            else if (elementType == typeof(IFixedTickable))
                list = new List<IFixedTickable>(count);
            else if (elementType == typeof(ILateDisposable))
                list = new List<ILateDisposable>(count);
            else if (elementType == typeof(ILateTickable))
                list = new List<ILateTickable>(count);
            else if (elementType == typeof(ITickable))
                list = new List<ITickable>(count);
            else
                return null;

            foreach (var instance in instances)
                list.Add(instance);
            return list;
        }

        public static string ToDebugString(this MethodInfo method)
        {
            return "{0}.{1}".Fmt(method.DeclaringType.PrettyName(), method.Name);
        }

        public static string ToDebugString(this Action action)
        {
#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
            return action.ToString();
#else
            return action.Method.ToDebugString();
#endif
        }

        public static string ToDebugString<TParam1>(this Action<TParam1> action)
        {
#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
            return action.ToString();
#else
            return action.Method.ToDebugString();
#endif
        }

        public static string ToDebugString<TParam1, TParam2>(this Action<TParam1, TParam2> action)
        {
#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
            return action.ToString();
#else
            return action.Method.ToDebugString();
#endif
        }

        public static string ToDebugString<TParam1, TParam2, TParam3>(this Action<TParam1, TParam2, TParam3> action)
        {
#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
            return action.ToString();
#else
            return action.Method.ToDebugString();
#endif
        }

        public static string ToDebugString<TParam1, TParam2, TParam3, TParam4>(this Action<TParam1, TParam2, TParam3, TParam4> action)
        {
#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
            return action.ToString();
#else
            return action.Method.ToDebugString();
#endif
        }

        public static string ToDebugString<TParam1, TParam2, TParam3, TParam4, TParam5>(this
#if NET_4_6
            Action<TParam1, TParam2, TParam3, TParam4, TParam5> action)
#else
            ModestTree.Util.Action<TParam1, TParam2, TParam3, TParam4, TParam5> action)
#endif
        {
#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
            return action.ToString();
#else
            return action.Method.ToDebugString();
#endif
        }

        public static string ToDebugString<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>(this
#if NET_4_6
            Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6> action)
#else
            ModestTree.Util.Action<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6> action)
#endif
        {
#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
            return action.ToString();
#else
            return action.Method.ToDebugString();
#endif
        }

        public static string ToDebugString<TParam1>(this Func<TParam1> func)
        {
#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
            return func.ToString();
#else
            return func.Method.ToDebugString();
#endif
        }

        public static string ToDebugString<TParam1, TParam2>(this Func<TParam1, TParam2> func)
        {
#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
            return func.ToString();
#else
            return func.Method.ToDebugString();
#endif
        }

        public static string ToDebugString<TParam1, TParam2, TParam3>(this Func<TParam1, TParam2, TParam3> func)
        {
#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
            return func.ToString();
#else
            return func.Method.ToDebugString();
#endif
        }

        public static string ToDebugString<TParam1, TParam2, TParam3, TParam4>(this Func<TParam1, TParam2, TParam3, TParam4> func)
        {
#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
            return func.ToString();
#else
            return func.Method.ToDebugString();
#endif
        }
    }
}
