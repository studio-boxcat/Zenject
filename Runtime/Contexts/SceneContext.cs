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

            Container = new DiContainer(ProjectContext.Instance.Container, 128);

            if (ExtraBindingsInstallMethod != null)
            {
                ExtraBindingsInstallMethod(Container);
                ExtraBindingsInstallMethod = null;
            }

            ZenjectBindingCollection.TryBind(gameObject, Container);

            _installers.InjectAndInstall(Container, default);

            GetComponent<Kernel>().RegisterServices(Container);

            Container.ResolveNonLazyProviders();

            InjectTargetCollection.TryInject(gameObject, Container, default);
        }

        void OnDestroy()
        {
            SceneContextRegistry.Remove(this);
        }

#if UNITY_EDITOR
        [Button("Collect", ButtonSizes.Medium)]
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