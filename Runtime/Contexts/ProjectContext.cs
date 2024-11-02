using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public class ProjectContext : MonoBehaviour
    {
        static ProjectContext _instance;
        public static bool HasInstance => _instance is not null;
        public static ProjectContext Instance
        {
            get
            {
                Assert.IsNotNull(_instance, "ProjectContext is not initialized. ProjectContext.Initialize() must be called before ProjectContext.Instance.");
                return _instance;
            }
        }


        [ShowInInspector] public DiContainer Container;
        [ShowInInspector] Kernel _kernel;

        [SerializeField, Required, AssetsOnly]
        ScriptableObjectInstaller _installer;

        public static ProjectContext Initialize(InstallScheme scheme = null)
        {
            Assert.IsTrue(_instance is null, "Tried to create multiple instances of ProjectContext!");
            Assert.IsTrue(FindAnyObjectByType<ProjectContext>(FindObjectsInactive.Include) is null,
                "Tried to create multiple instances of ProjectContext!");

            var prefab = Resources.Load<ProjectContext>("ProjectContext");
            var instance = Instantiate(prefab, null, false);
            instance.DoInitialize(scheme);
            DontDestroyOnLoad(instance.gameObject);

            return _instance = instance;
        }

        void DoInitialize([CanBeNull] InstallScheme scheme)
        {
            // Install
            scheme ??= new InstallScheme(16);
            _installer.InstallBindings(scheme); // No injection for ProjectContext.
            _installer = null;

            // Build Container
            Container = scheme.Build(null, out _kernel);
        }

        void OnDestroy()
        {
            // Clear static variables first, as _kernel.Dispose() may throw exceptions.
            if (ReferenceEquals(this, _instance))
            {
                _instance = null;
                SceneContext.ClearPrebuiltScheme();
            }

            _kernel.Dispose();
            _kernel = default; // For GC.
            Container = null; // For GC.
        }

        void Update()
        {
            _kernel.Tick();
        }
    }
}