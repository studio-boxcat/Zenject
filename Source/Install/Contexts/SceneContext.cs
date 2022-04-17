#if !NOT_UNITY3D

using System;
using ModestTree;
using Object = UnityEngine.Object;

namespace Zenject
{
    public class SceneContext : Context
    {
        public static Action<DiContainer> ExtraBindingsInstallMethod;
        public static Action<DiContainer> ExtraBindingsLateInstallMethod;

        DiContainer _container;

        public override DiContainer Container => _container;

        public bool HasResolved { get; private set; }
        public bool HasInstalled { get; private set; }

        public void Awake()
        {
            SceneContextRegistry.Add(this);

            Install();
            Resolve();
        }

        void OnDestroy()
        {
            SceneContextRegistry.Remove(this);
        }

        void Install()
        {
            Assert.That(!HasInstalled);
            Assert.IsNull(_container);

            HasInstalled = true;
            _container = new DiContainer(ProjectContext.Instance.Container);

            // Record all the injectable components in the scene BEFORE installing the installers
            // This is nice for cases where the user calls InstantiatePrefab<>, etc. in their installer
            // so that it doesn't inject on the game object twice
            // InitialComponentsInjecter will also guarantee that any component that is injected into
            // another component has itself been injected
            var injectableMonoBehaviours = gameObject.GetComponent<InjectTargetCollection>().Targets;
            _container.QueueForInject(injectableMonoBehaviours);

            _container.IsInstalling = true;

            try
            {
                InstallBindings(injectableMonoBehaviours);
            }
            finally
            {
                _container.IsInstalling = false;
            }
        }

        void Resolve()
        {
            Assert.That(HasInstalled);
            Assert.That(!HasResolved);
            HasResolved = true;
            _container.ResolveRoots();
        }

        void InstallBindings(Object[] injectableMonoBehaviours)
        {
            ContextUtils.InstallBindings_Managers(_container);

            _container.Bind(typeof(Context), typeof(SceneContext)).To<SceneContext>().FromInstance(this);

            ContextUtils.InstallBindings_ZenjectBindings(this, injectableMonoBehaviours);

            _container.Bind(typeof(MonoKernel))
                .To<MonoKernel>().FromNewComponentOn(gameObject).AsSingle().NonLazy();

            if (ExtraBindingsInstallMethod != null)
            {
                ExtraBindingsInstallMethod(_container);
                // Reset extra bindings for next time we change scenes
                ExtraBindingsInstallMethod = null;
            }

            InstallInstallers();

            if (ExtraBindingsLateInstallMethod != null)
            {
                ExtraBindingsLateInstallMethod(_container);
                // Reset extra bindings for next time we change scenes
                ExtraBindingsLateInstallMethod = null;
            }
        }
    }
}

#endif