#if !NOT_UNITY3D

using System;

namespace Zenject
{
    public class SceneContext : Context
    {
        public static Action<DiContainer> ExtraBindingsInstallMethod;

        DiContainer _container;

        public override DiContainer Container => _container;

        public void Awake()
        {
            SceneContextRegistry.Add(this);

            _container = new DiContainer(ProjectContext.Instance.Container);

            _container.Bind(typeof(TickableManager));
            _container.Bind(typeof(DisposableManager));
            _container.Bind(typeof(MonoKernel)).FromNewComponentOn(gameObject).NonLazy();

            gameObject.GetComponent<ZenjectBindingCollection>().Bind(_container);
            _container.QueueForInject(gameObject.GetComponent<InjectTargetCollection>().Targets);

            if (ExtraBindingsInstallMethod != null)
            {
                ExtraBindingsInstallMethod(_container);
                ExtraBindingsInstallMethod = null;
            }

            InstallInstallers();

            _container.ResolveRoots();
        }

        void OnDestroy()
        {
            SceneContextRegistry.Remove(this);
        }
    }
}

#endif