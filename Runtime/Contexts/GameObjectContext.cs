using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Zenject
{
    [RequireComponent(typeof(Kernel))]
    public class GameObjectContext : MonoBehaviour, IZenjectInjectable
    {
        public DiContainer Container;

        [SerializeField, InlineProperty, HideLabel]
        InstallerCollection _installers;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryInject(GameObject gameObject, DiContainer diContainer, ArgumentArray extraArgs)
        {
            if (gameObject.TryGetComponent(out GameObjectContext context) == false)
                return false;

            context.Inject(diContainer, extraArgs);
            return true;
        }

        void IZenjectInjectable.Inject(DependencyProvider dp)
        {
            Container = new DiContainer(dp.Container, 32);

            ZenjectBindingCollection.TryBind(gameObject, Container);

            _installers.InjectAndInstall(Container, dp.ExtraArgs);

            GetComponent<Kernel>().RegisterServices(Container);

            Container.ResolveNonLazyProviders();

            InjectTargetCollection.TryInject(gameObject, Container, dp.ExtraArgs);
        }
    }
}