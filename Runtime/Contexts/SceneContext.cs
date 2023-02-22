using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Zenject
{
    [RequireComponent(typeof(Kernel))]
    public class SceneContext : MonoBehaviour
    {
        public static Action<DiContainer> ExtraBindingsInstallMethod;

        public DiContainer Container;

        [FormerlySerializedAs("InstallerCollection")]
        [SerializeField, InlineProperty, HideLabel]
        InstallerCollection _installers;

        public void Awake()
        {
            SceneContextRegistry.Add(this);

            Container = new DiContainer(ProjectContext.Instance.Container);

            if (gameObject.TryGetComponent(out ZenjectBindingCollection zenjectBindings))
                zenjectBindings.Bind(Container);

            if (gameObject.TryGetComponent(out InjectTargetCollection injectTargets))
                injectTargets.QueueForInject(Container);

            if (ExtraBindingsInstallMethod != null)
            {
                ExtraBindingsInstallMethod(Container);
                ExtraBindingsInstallMethod = null;
            }

            _installers.InjectAndInstall(Container);
            _installers = default;

            GetComponent<Kernel>().RegisterServices(Container);

            Container.ResolveRoots();
        }

        void OnDestroy()
        {
            SceneContextRegistry.Remove(this);
        }
    }
}