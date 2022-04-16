#if !NOT_UNITY3D

using System;
using System.Collections.Generic;
using System.Linq;
using ModestTree;
using ModestTree.Util;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject.Internal;
using UnityEngine.Events;

namespace Zenject
{
    public class SceneContext : RunnableContext
    {
        public event Action PreInstall;
        public event Action PostInstall;
        public event Action PreResolve;
        public event Action PostResolve;

        public UnityEvent OnPreInstall;
        public UnityEvent OnPostInstall;
        public UnityEvent OnPreResolve;
        public UnityEvent OnPostResolve;

        public static Action<DiContainer> ExtraBindingsInstallMethod;
        public static Action<DiContainer> ExtraBindingsLateInstallMethod;

        DiContainer _container;

        bool _hasInstalled;
        bool _hasResolved;

        public override DiContainer Container
        {
            get { return _container; }
        }

        public bool HasResolved
        {
            get { return _hasResolved; }
        }

        public bool HasInstalled
        {
            get { return _hasInstalled; }
        }

        public void Awake()
        {
            Initialize();
        }

        protected override void RunInternal()
        {
            // We always want to initialize ProjectContext as early as possible
            ProjectContext.Instance.EnsureIsInitialized();

            Install();

            Resolve();
        }

        public override IEnumerable<GameObject> GetRootGameObjects()
        {
            return ZenUtilInternal.GetRootGameObjects(gameObject.scene);
        }

        public void Install()
        {
            Assert.That(!_hasInstalled);
            _hasInstalled = true;

            Assert.IsNull(_container);

            _container = new DiContainer(ProjectContext.Instance.Container);

            // Do this after creating DiContainer in case it's needed by the pre install logic
            PreInstall?.Invoke();
            OnPreInstall?.Invoke();

            // Record all the injectable components in the scene BEFORE installing the installers
            // This is nice for cases where the user calls InstantiatePrefab<>, etc. in their installer
            // so that it doesn't inject on the game object twice
            // InitialComponentsInjecter will also guarantee that any component that is injected into
            // another component has itself been injected
            var injectableMonoBehaviours = new List<MonoBehaviour>();
            GetInjectableMonoBehaviours(injectableMonoBehaviours);
            foreach (var instance in injectableMonoBehaviours)
            {
                _container.QueueForInject(instance);
            }

            _container.IsInstalling = true;

            try
            {
                InstallBindings(injectableMonoBehaviours);
            }
            finally
            {
                _container.IsInstalling = false;
            }

            PostInstall?.Invoke();
            OnPostInstall?.Invoke();
        }

        public void Resolve()
        {
            PreResolve?.Invoke();
            OnPreResolve?.Invoke();

            Assert.That(_hasInstalled);
            Assert.That(!_hasResolved);
            _hasResolved = true;

            _container.ResolveRoots();

            PostResolve?.Invoke();
            OnPostResolve?.Invoke();
        }

        void InstallBindings(List<MonoBehaviour> injectableMonoBehaviours)
        {
            ZenjectManagersInstaller.InstallBindings(_container);

            _container.Bind(typeof(Context), typeof(SceneContext)).To<SceneContext>().FromInstance(this);
            _container.BindInterfacesTo<SceneContextRegistryAdderAndRemover>().AsSingle();

            InstallSceneBindings(injectableMonoBehaviours);

            _container.Bind(typeof(SceneKernel), typeof(MonoKernel))
                .To<SceneKernel>().FromNewComponentOn(gameObject).AsSingle().NonLazy();

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

        protected override void GetInjectableMonoBehaviours(List<MonoBehaviour> monoBehaviours)
        {
            var scene = gameObject.scene;

            ZenUtilInternal.GetInjectableMonoBehavioursInScene(scene, monoBehaviours);
        }

        // These methods can be used for cases where you need to create the SceneContext entirely in code
        // Note that if you use these methods that you have to call Run() yourself
        // This is useful because it allows you to create a SceneContext and configure it how you want
        // and add what installers you want before kicking off the Install/Resolve
        public static SceneContext Create()
        {
            return CreateComponent<SceneContext>(
                new GameObject("SceneContext"));
        }
    }
}

#endif
