using System.Collections.Generic;
using ModestTree;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public static class SceneContextRegistry
    {
        public static readonly List<SceneContext> List = new();

        public static void Add(SceneContext context)
        {
            Assert.IsFalse(List.Contains(context));
            List.Add(context);
        }

        public static void Remove(SceneContext context)
        {
            bool removed = List.Remove(context);
            if (!removed) Log.Warn("Failed to remove SceneContext from SceneContextRegistry");
        }

        public static void ForceUnloadAllScenes()
        {
            for (var i = List.Count - 1; i >= 0; i--)
            {
                var sceneContext = List[i];
                Object.Destroy(sceneContext.gameObject);
            }
        }
    }
}