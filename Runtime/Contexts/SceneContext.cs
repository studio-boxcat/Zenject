using Sirenix.OdinInspector;
using UnityEngine;

namespace Zenject
{
    public class SceneContext : MonoBehaviour
    {
        static InstallScheme _prebuiltScheme;
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
        Kernel _kernel;

        [SerializeField, InlineProperty, HideLabel, HideInPlayMode]
        InstallerCollection _installers;

        public void Awake()
        {
            // Get Parent Container
            DiContainer parentContainer;
            if (ProjectContext.HasInstance)
            {
                parentContainer = ProjectContext.Instance.Container;
            }
            else
            {
                L.E("ProjectContext is not initialized. ProjectContext.Initialize() must be called before SceneContext.Awake().");
                parentContainer = ProjectContext.Initialize().Container;
            }


            // Install
            var scheme = _prebuiltScheme ?? new InstallScheme(64);
            _prebuiltScheme = null;

            // 1. ZenjectBindingCollection
            if (gameObject.TryGetComponent(out ZenjectBindingCollection zenjectBindings))
                zenjectBindings.Bind(scheme);

            // 2. Installers
            _installers.Install(scheme, parentContainer);
            _installers = default;


            // Build Container & Inject
            Container = scheme.Build(parentContainer, out _kernel);
            InjectTargetCollection.TryInject(gameObject, Container, default);


            // Register SceneContext at last.
            // If somehow exception occurs on Awake, OnDestroy will not be called.
            SceneContextRegistry.Add(this);
        }

        void OnDestroy()
        {
            // Clean up static variable as _kernel.Dispose() may throw exceptions.
            SceneContextRegistry.Remove(this);

            _kernel.Dispose();
            _kernel = default; // For GC.
            Container = null; // For GC.
        }

        void Update()
        {
            _kernel.Tick();
        }

#if UNITY_EDITOR
        [Button("Collect", ButtonSizes.Medium), HideInPlayMode]
        void Editor_Collect()
        {
            if (TryGetComponent<ZenjectBindingCollection>(out var zenjectBindings))
                zenjectBindings.Editor_Collect();
            if (TryGetComponent<InjectTargetCollection>(out var injectTargets))
                injectTargets.Editor_Collect();
        }
#endif
    }
}