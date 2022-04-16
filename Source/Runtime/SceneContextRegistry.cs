using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    public static class SceneContextRegistry
    {
        public static readonly List<SceneContext> List = new();

        public static void Add(SceneContext context)
        {
            Assert.That(!List.Contains(context));
            List.Add(context);
        }

        public static void Remove(SceneContext context)
        {
            bool removed = List.Remove(context);
            if (!removed) Log.Warn("Failed to remove SceneContext from SceneContextRegistry");
        }
    }

}
