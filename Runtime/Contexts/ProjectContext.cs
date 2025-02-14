using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public class ProjectContext : MonoBehaviour
    {
        private static ProjectContext _instance;
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
        [ShowInInspector] private Kernel _kernel;


        public static ProjectContext Initialize(InstallScheme scheme = null)
        {
            Assert.IsTrue(_instance is null, "Tried to create multiple instances of ProjectContext!");
            Assert.IsTrue(FindAnyObjectByType<ProjectContext>(FindObjectsInactive.Include) is null,
                "Tried to create multiple instances of ProjectContext!");

            var instanceGO = new GameObject("ProjectContext", typeof(ProjectContext));
            DontDestroyOnLoad(instanceGO);

            _instance = instanceGO.GetComponent<ProjectContext>();

#if UNITY_EDITOR
            try
            {
#endif
                scheme ??= new InstallScheme(16);
                Resources.Load<ScriptableObjectInstaller>("ProjectInstaller").InstallBindings(scheme);
                _instance.Container = scheme.Build(null, out _instance._kernel);
#if UNITY_EDITOR
            }
            catch (Exception e)
            {
                L.E("Exception occurred during ProjectContext initialization.\n" + e);
                L.E(e);
                throw;
            }
#endif

            return _instance;
        }

        private void OnDestroy()
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

        private void Update()
        {
            _kernel.Tick();
        }
    }
}