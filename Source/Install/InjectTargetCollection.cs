using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Zenject
{
    [HideMonoScript, DisallowMultipleComponent]
    public partial class InjectTargetCollection : MonoBehaviour
    {
        [ListDrawerSettings(IsReadOnly = true)]
        [ValidateInput("Validate_Targets")]
        public Object[] Targets;

        public static void TryInject(GameObject gameObject, DiContainer diContainer, ArgumentArray extraArgs)
        {
            if (gameObject.TryGetComponent(out InjectTargetCollection injectTargetCollection))
                injectTargetCollection.Inject(diContainer, extraArgs);
        }

        public void Inject(DiContainer diContainer, ArgumentArray extraArgs)
        {
            foreach (var target in Targets)
            {
                if (target is InjectTargetCollection injectTargetCollection)
                {
                    injectTargetCollection.Inject(diContainer, extraArgs);
                }
                else
                {
                    diContainer.Inject(target, extraArgs);
                }
            }
            Targets = null;
        }

        public void QueueForInject(DiContainer container)
        {
            foreach (var target in Targets)
            {
                if (target is InjectTargetCollection injectTargetCollection)
                {
                    injectTargetCollection.QueueForInject(container);
                }
                else
                {
                    container.QueueForInject(target);
                }
            }
            Targets = null;
        }
    }
}