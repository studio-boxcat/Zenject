using System;

namespace Zenject
{
    [ExecutionPriority(-1)]
    public class SceneContextRegistryAdderAndRemover : IInitializable, IDisposable
    {
        readonly SceneContext _sceneContext;

        public SceneContextRegistryAdderAndRemover(
            SceneContext sceneContext)
        {
            _sceneContext = sceneContext;
        }

        public void Initialize()
        {
            SceneContextRegistry.Add(_sceneContext);
        }

        public void Dispose()
        {
            SceneContextRegistry.Remove(_sceneContext);
        }
    }
}

