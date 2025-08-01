using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Zenject
{
    public class GameObjectContext : MonoBehaviour, IZenjectInjectable
    {
        [ShowInInspector, HideInEditorMode]
        public DiContainer Container;
        [ShowInInspector, HideInEditorMode]
        private Kernel _kernel;

        [SerializeField, InlineProperty, HideLabel]
        private InstallerCollection _installers;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryInject(GameObject gameObject, DiContainer diContainer, ArgumentArray extraArgs)
        {
            if (gameObject.TryGetComponent(out GameObjectContext context) is false)
                return false;

            context.Inject(diContainer, extraArgs);
            return true;
        }

        void IZenjectInjectable.Inject(DependencyProvider dp)
        {
            // Install
            var scheme = new InstallScheme(8);

            // 1. ExtraArgs
            var extraArgsLen = dp.ExtraArgs.Length;
            for (var i = 0; i < extraArgsLen; i++)
            {
                var arg = dp.ExtraArgs[i];
                scheme.Bind(arg.GetType(), dp.ExtraArgs[i]);
            }

            // 2. ZenjectBindingCollection
            if (gameObject.TryGetComponent(out ZenjectBindingCollection zenjectBindings))
                zenjectBindings.Bind(scheme);

            // 3. Build & Inject
            Container = _installers.BuildContainer(scheme, parent: dp.Container, this, out _kernel);
            _installers = default;
            InjectTargetCollection.TryInject(gameObject, Container, dp.ExtraArgs);
        }

        private void OnDestroy()
        {
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