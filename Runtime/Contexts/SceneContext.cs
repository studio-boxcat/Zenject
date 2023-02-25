using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Zenject
{
    [RequireComponent(typeof(Kernel))]
    public class SceneContext : MonoBehaviour
    {
        public static Action<DiContainer> ExtraBindingsInstallMethod;

        public DiContainer Container;

        [SerializeField, InlineProperty, HideLabel]
        InstallerCollection _installers;

        public void Awake()
        {
            SceneContextRegistry.Add(this);

            Container = new DiContainer(ProjectContext.Instance.Container);

            if (ExtraBindingsInstallMethod != null)
            {
                ExtraBindingsInstallMethod(Container);
                ExtraBindingsInstallMethod = null;
            }

            ZenjectBindingCollection.TryBind(gameObject, Container);

            _installers.InjectAndInstall(Container);

            GetComponent<Kernel>().RegisterServices(Container);

            Container.ResolveNonLazyProviders();

            InjectTargetCollection.TryInject(gameObject, Container, default);
        }

        void OnDestroy()
        {
            SceneContextRegistry.Remove(this);
        }
    }
}