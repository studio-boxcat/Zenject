#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Zenject
{
    public static class SceneContextRegistry
    {
        public static readonly List<SceneContext> List = new();

        public static bool Any() => List.Count is not 0;

        public static SceneContext Last => List[^1];

        public static void Add(SceneContext context)
        {
            L.I($"[SceneContextRegistry] Add: {context.gameObject.scene.name}", context);

            Assert.IsFalse(List.ContainsRef(context));
            List.Add(context);

#if UNITY_EDITOR
            if (List.Count is not 1)
                L.W($"[SceneContextRegistry] Multiple SceneContexts detected: {context.gameObject.scene.name}. This is supported but accidental.");
#endif
        }

        public static void Remove(SceneContext context)
        {
            L.I($"[SceneContextRegistry] Remove: {context}", context);

            var removed = List.Remove(context);
#if DEBUG
            if (!removed)
                L.E($"[SceneContextRegistry] Remove failed: {context}", context);
#endif
        }

        public static object? TryResolve(Type type)
        {
            for (var i = List.Count - 1; i >= 0; i--)
            {
                var container = List[i].Container;
                if (container.TryResolve(type, out var concrete))
                    return concrete;
            }

            return null;
        }

        public static T? TryResolve<T>() => (T?) TryResolve(typeof(T));

        public static T Instantiate<T>() => Last.Container.Instantiate<T>();
    }
}