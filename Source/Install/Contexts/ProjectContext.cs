#if !NOT_UNITY3D

using System;
using ModestTree;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Zenject
{
    public class ProjectContext : Context
    {
        static ProjectContext _instance;

        DiContainer _container;

        public override DiContainer Container => _container;

        public static bool HasInstance => _instance != null;

        public static ProjectContext Instance
        {
            get
            {
                if (_instance == null)
                    InstantiateAndInitialize();
                return _instance;
            }
        }

        static void InstantiateAndInitialize()
        {
            Assert.That(FindObjectsOfType<ProjectContext>().IsEmpty(),
                "Tried to create multiple instances of ProjectContext!");

            var prefab = Resources.Load<GameObject>("ProjectContext");
            prefab.SetActive(false);
            _instance = Instantiate(prefab, null, false).GetComponent<ProjectContext>();
            prefab.SetActive(true);

            // Note: We use Initialize instead of awake here in case someone calls
            // ProjectContext.Instance while ProjectContext is initializing
            _instance.Initialize();

            // We always instantiate it as disabled so that Awake and Start events are triggered after inject
            _instance.gameObject.SetActive(true);
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

            var injectableMonoBehaviours = gameObject.TryGetComponent(out InjectTargetCollection injectTargetCollection)
                ? injectTargetCollection.Targets : Array.Empty<Object>();
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

            _container.ResolveRoots();
        }

        void InstallBindings(Object[] injectableMonoBehaviours)
        {
            ContextUtils.InstallBindings_Managers(_container);

            _container.Bind<Context>().FromInstance(this).AsSingle();

            _container.Bind(typeof(ProjectKernel))
                .To<ProjectKernel>().FromNewComponentOn(gameObject).AsSingle().NonLazy();

            ContextUtils.InstallBindings_ZenjectBindings(this, injectableMonoBehaviours);

            InstallInstallers();
        }
    }
}

#endif