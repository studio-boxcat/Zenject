using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Zenject
{
    public class GameObjectContext : MonoBehaviour, IZenjectInjectable
    {
        [ShowInInspector] public DiContainer Container;
        [ShowInInspector]
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
            var parentContainer = dp.Container;


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

            // 3. Installers
            _installers.Install(scheme, parentContainer);
            _installers = default;


            // Build & Inject
            Container = scheme.Build(parentContainer, out _kernel);
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