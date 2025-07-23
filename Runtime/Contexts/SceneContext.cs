#nullable enable
using Sirenix.OdinInspector;
using UnityEngine;

namespace Zenject
{
    public class SceneContext : MonoBehaviour
    {
        private static InstallScheme? _prebuiltScheme;
        public static void SetPrebuiltScheme(InstallScheme scheme)
        {
            L.I("SetPrebuiltScheme: " + scheme);
            if (_prebuiltScheme is not null)
                L.E("PrebuiltScheme is already set.");
            _prebuiltScheme = scheme;
        }

        [ShowInInspector, ShowIf("@Container != null")]
        public DiContainer Container = null!;
        [ShowInInspector, ShowIf("@Container != null")]
        private Kernel _kernel;

        [SerializeField, InlineProperty, HideLabel, HideInPlayMode]
        private InstallerCollection _installers;

        public void Awake()
        {
            L.I("SceneContext.Awake()");

            // Install
            var scheme = _prebuiltScheme ?? new InstallScheme(64);
            _prebuiltScheme = null;

            // 1. ZenjectBindingCollection
            if (gameObject.TryGetComponent(out ZenjectBindingCollection zenjectBindings))
                zenjectBindings.Bind(scheme);

            // 2. Build & Inject
            var parent = ProjectContext.Resolve().Container;
            Container = _installers.BuildContainer(scheme, parent: parent, context: this, out _kernel);
            _installers = default;
            InjectTargetCollection.TryInject(gameObject, Container, default);


            // Register SceneContext at last.
            // If somehow exception occurs on Awake, OnDestroy will not be called.
            SceneContextRegistry.Add(this);
        }

        private void OnDestroy()
        {
            L.I("SceneContext.OnDestroy()");

            _kernel.Dispose();
            _kernel = default; // For GC.
            Container = null!; // For GC. never use this value.

            SceneContextRegistry.Remove(this);
        }

        private void Update()
        {
            _kernel.Tick();
        }
    }
}