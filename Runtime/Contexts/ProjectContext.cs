#nullable enable
using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public class ProjectContext : MonoBehaviour
    {
        private static ProjectContext? _instance;
        public static ProjectContext Instance
        {
            get
            {
                Assert.IsNotNull(_instance, "ProjectContext is not initialized. ProjectContext.Initialize() must be called before ProjectContext.Instance.");
                return _instance!;
            }
        }

        public static ProjectContext Resolve()
        {
            if (_instance is not null)
                return _instance;

            // rare-case.
            var scheme = new InstallScheme();
#if UNITY_EDITOR
            L.W("ProjectContext is not initialized, using fallback installer.");
            IProjectContextFallbackInstaller.ResolveAndInstallBindings(scheme);
#else
            L.E("ProjectContext is not initialized.");
#endif
            return Initialize(scheme);
        }

        [ShowInInspector] public DiContainer? Container;
        [ShowInInspector] private Kernel _kernel;

        public static ProjectContext Initialize(InstallScheme scheme)
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

        public static void DestroyInstance()
        {
            L.I("Destroying ProjectContext instance.");
            if (_instance is not null)
                Destroy(_instance.gameObject);
        }

        private void OnDestroy()
        {
            // Clear static variables first, as _kernel.Dispose() may throw exceptions.
            if (this.RefEq(_instance))
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

#if UNITY_EDITOR
    public interface IProjectContextFallbackInstaller
    {
        void InstallBindings(InstallScheme scheme);

        internal static void ResolveAndInstallBindings(InstallScheme scheme)
        {
            var installerTypes = UnityEditor.TypeCache.GetTypesDerivedFrom<IProjectContextFallbackInstaller>();

            if (installerTypes.Count is 0)
            {
                L.W("No fallback installer found for ProjectContext. Please ensure that you have a class implementing IProjectContextFallbackInstaller in your project.");
                return;
            }

            if (installerTypes.Count > 1)
            {
                L.W("Multiple fallback installers found for ProjectContext. Using the first one: " + installerTypes[0].Name);
            }

            L.I("Using fallback installer: " + installerTypes[0].Name);
            var installer = (IProjectContextFallbackInstaller) Activator.CreateInstance(installerTypes[0])!;
            installer.InstallBindings(scheme);
        }
    }
#endif
}