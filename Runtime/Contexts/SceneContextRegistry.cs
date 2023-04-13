using System.Collections.Generic;
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
            var removed = List.Remove(context);
            Assert.IsTrue(removed);
        }
    }
}