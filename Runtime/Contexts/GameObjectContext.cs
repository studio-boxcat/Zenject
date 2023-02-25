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

        void IZenjectInjectable.Inject(DependencyProvider dp)
        {
            Container = new DiContainer(dp.Container);

            ZenjectBindingCollection.TryBind(gameObject, Container);

            _installers.InjectAndInstall(Container);

            GetComponent<Kernel>().RegisterServices(Container);

            Container.ResolveNonLazyProviders();

            InjectTargetCollection.TryInject(gameObject, Container, dp.ExtraArgs);
        }
    }
}