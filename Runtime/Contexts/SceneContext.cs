using Sirenix.OdinInspector;
using UnityEngine;

namespace Zenject
{
    public class SceneContext : MonoBehaviour
    {
        private static InstallScheme _prebuiltScheme;
        public static void SetPrebuiltScheme(InstallScheme scheme)
        {
            L.I("SetPrebuiltScheme: " + scheme);
            if (_prebuiltScheme is not null)
                L.E("PrebuiltScheme is already set.");
            _prebuiltScheme = scheme;
        }

        internal static void ClearPrebuiltScheme()
        {
#if DEBUG
            if (_prebuiltScheme is not null)
                L.I("ClearPrebuiltScheme");
#endif
            _prebuiltScheme = null;
        }


        [ShowInInspector, ShowIf("@Container != null")]
        public DiContainer Container;
        [ShowInInspector, ShowIf("@Container != null")]
        private Kernel _kernel;

        [SerializeField, InlineProperty, HideLabel, HideInPlayMode]
        private InstallerCollection _installers;

        public void Awake()
        {
            // Install
            var scheme = _prebuiltScheme ?? new InstallScheme(64);
            _prebuiltScheme = null;

            // 1. ZenjectBindingCollection
            if (gameObject.TryGetComponent(out ZenjectBindingCollection zenjectBindings))
                zenjectBindings.Bind(scheme);

            // 2. Installers
            var parent = ProjectContext.Resolve().Container;
            _installers.Install(scheme, parent, this);
            _installers = default;


            // Build Container & Inject
            Container = scheme.Build(parent, out _kernel);
            InjectTargetCollection.TryInject(gameObject, Container, default);


            // Register SceneContext at last.
            // If somehow exception occurs on Awake, OnDestroy will not be called.
            SceneContextRegistry.Add(this);
        }

        private void OnDestroy()
        {
            // Clean up static variable as _kernel.Dispose() may throw exceptions.
            SceneContextRegistry.Remove(this);

            _kernel.Dispose();
            _kernel = default; // For GC.
            Container = null; // For GC.
        }

        private void Update()
        {
            _kernel.Tick();
        }

#if UNITY_EDITOR
        [ContextMenu("Collect _c")]
        private void Collect()
        {
            if (TryGetComponent<ZenjectBindingCollection>(out var zenjectBindings))
                zenjectBindings.Editor_Collect();
            if (TryGetComponent<InjectTargetCollection>(out var injectTargets))
                injectTargets.Editor_Collect();
        }
#endif
    }
}