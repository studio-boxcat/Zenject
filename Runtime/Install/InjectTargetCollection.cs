using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Zenject
{
    [HideMonoScript, DisallowMultipleComponent]
    public partial class InjectTargetCollection : MonoBehaviour, IZenjectInjectable
    {
        [ListDrawerSettings(IsReadOnly = true)]
        [ValidateInput("Validate_Targets")]
        public Object[] Targets;

        public static void TryInject(GameObject gameObject, DiContainer diContainer, ArgumentArray extraArgs)
        {
            if (gameObject.TryGetComponent(out InjectTargetCollection injectTargets))
                injectTargets.Inject(diContainer, extraArgs);
        }

        void IZenjectInjectable.Inject(DependencyProvider dp)
        {
            Inject(dp.Container, dp.ExtraArgs);
        }

        void Inject(DiContainer diContainer, ArgumentArray extraArgs)
        {
            foreach (var target in Targets)
                diContainer.Inject(target, extraArgs);
            Targets = null;
        }
    }
}