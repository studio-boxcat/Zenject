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
            var count = Targets.Length;

            for (var index = 0; index < count; index++)
            {
                var target = Targets[index];

#if DEBUG
                try
#endif
                {
                    diContainer.Inject(target, extraArgs);
                }
#if DEBUG
                catch
                {
                    L.E($"Failed to inject target: target={target}, index={index}",
                        target != null ? target : this);
                    throw;
                }
#endif
            }

            Targets = null;
        }
    }
}