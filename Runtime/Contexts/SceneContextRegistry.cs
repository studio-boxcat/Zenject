using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace Zenject
{
    public static class SceneContextRegistry
    {
        public static readonly List<SceneContext> List = new();

        public static SceneContext Last => List[^1];

        public static void Add(SceneContext context)
        {
            L.I($"[SceneContextRegistry] Add: {context.gameObject.scene.name}", context);

            Assert.IsFalse(List.Contains(context));
            List.Add(context);
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

        [CanBeNull]
        public static object TryResolve(Type type)
        {
            for (var i = List.Count - 1; i >= 0; i--)
            {
                var container = List[i].Container;
                if (container.TryResolve(type, out var concrete))
                    return concrete;
            }

            return null;
        }

        [CanBeNull]
        public static T TryResolve<T>()
        {
            return (T) TryResolve(typeof(T));
        }
    }
}