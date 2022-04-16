#if !NOT_UNITY3D

using System;
using System.Collections.Generic;
using System.Threading;
using ModestTree;
using UnityEngine;
using Zenject.Internal;

namespace Zenject
{
    public class ProjectContext : Context
    {
        public static event Action PreInstall;
        public static event Action PostInstall;
        public static event Action PreResolve;
        public static event Action PostResolve;

        public const string ProjectContextResourcePath = "ProjectContext";
        public const string ProjectContextResourcePathOld = "ProjectCompositionRoot";

        static ProjectContext _instance;

        DiContainer _container;

        public override DiContainer Container
        {
            get { return _container; }
        }

        public static bool HasInstance
        {
            get { return _instance != null; }
        }

        public static ProjectContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    InstantiateAndInitialize();
                    Assert.IsNotNull(_instance);
                }

                return _instance;
            }
        }

        public override IEnumerable<GameObject> GetRootGameObjects()
        {
            return new[] { gameObject };
        }

        public static GameObject TryGetPrefab()
        {
            var prefabs = Resources.LoadAll(ProjectContextResourcePath, typeof(GameObject));

            if (prefabs.Length > 0)
            {
                Assert.That(prefabs.Length == 1,
                    "Found multiple project context prefabs at resource path '{0}'", ProjectContextResourcePath);
                return (GameObject)prefabs[0];
            }

            prefabs = Resources.LoadAll(ProjectContextResourcePathOld, typeof(GameObject));

            if (prefabs.Length > 0)
            {
                Assert.That(prefabs.Length == 1,
                    "Found multiple project context prefabs at resource path '{0}'", ProjectContextResourcePathOld);
                return (GameObject)prefabs[0];
            }

            return null;
        }

        static void InstantiateAndInitialize()
        {
            Assert.That(FindObjectsOfType<ProjectContext>().IsEmpty(),
                "Tried to create multiple instances of ProjectContext!");

            var prefab = TryGetPrefab();

            var prefabWasActive = false;

            if (prefab == null)
            {
                _instance = new GameObject("ProjectContext")
                    .AddComponent<ProjectContext>();
            }
            else
            {
                prefabWasActive = prefab.activeSelf;

                GameObject gameObjectInstance;
#if UNITY_EDITOR
                if (prefabWasActive)
                {
                    // This ensures the prefab's Awake() methods don't fire (and, if in the editor, that the prefab file doesn't get modified)
                    gameObjectInstance = GameObject.Instantiate(prefab, ZenUtilInternal.GetOrCreateInactivePrefabParent());
                    gameObjectInstance.SetActive(false);
                    gameObjectInstance.transform.SetParent(null, false);
                }
                else
                {
                    gameObjectInstance = GameObject.Instantiate(prefab);
                }
#else
                    if(prefabWasActive)
                    {
                        prefab.SetActive(false);
                        gameObjectInstance = GameObject.Instantiate(prefab);
                        prefab.SetActive(true);
                    }
                    else
                    {
                        gameObjectInstance = GameObject.Instantiate(prefab);
                    }
#endif

                _instance = gameObjectInstance.GetComponent<ProjectContext>();

                Assert.IsNotNull(_instance,
                    "Could not find ProjectContext component on prefab 'Resources/{0}.prefab'", ProjectContextResourcePath);
            }

            // Note: We use Initialize instead of awake here in case someone calls
            // ProjectContext.Instance while ProjectContext is initializing
            _instance.Initialize();

            if (prefabWasActive)
            {
                // We always instantiate it as disabled so that Awake and Start events are triggered after inject
                _instance.gameObject.SetActive(true);
            }
        }

        public void EnsureIsInitialized()
        {
            // Do nothing - Initialize occurs in Instance property
        }

        public void Awake()
        {
            if (Application.isPlaying)
                // DontDestroyOnLoad can only be called when in play mode and otherwise produces errors
                // ProjectContext is created during design time (in an empty scene) when running validation
                // and also when running unit tests
                // In these cases we don't need DontDestroyOnLoad so just skip it
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        void Initialize()
        {
            Assert.IsNull(_container);

            _container = new DiContainer();

            // Do this after creating DiContainer in case it's needed by the pre install logic
            if (PreInstall != null)
            {
                PreInstall();
            }

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

            if (PostInstall != null)
            {
                PostInstall();
            }

            if (PreResolve != null)
            {
                PreResolve();
            }

            _container.ResolveRoots();

            if (PostResolve != null)
            {
                PostResolve();
            }
        }

        protected override void GetInjectableMonoBehaviours(List<MonoBehaviour> monoBehaviours)
        {
            ZenUtilInternal.GetInjectableMonoBehavioursUnderGameObject(gameObject, monoBehaviours);
        }

        void InstallBindings(List<MonoBehaviour> injectableMonoBehaviours)
        {
            ZenjectManagersInstaller.InstallBindings(_container);

            _container.Bind<Context>().FromInstance(this).AsSingle();

            _container.Bind(typeof(ProjectKernel), typeof(MonoKernel))
                .To<ProjectKernel>().FromNewComponentOn(gameObject).AsSingle().NonLazy();

            InstallSceneBindings(injectableMonoBehaviours);

            InstallInstallers();

        }
    }
}

#endif
