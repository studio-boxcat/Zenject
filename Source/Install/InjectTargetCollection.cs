using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Zenject
{
    public partial class InjectTargetCollection : MonoBehaviour
    {
        [ListDrawerSettings(IsReadOnly = true)]
        [ValidateInput("Validate_Targets")]
        public Object[] Targets;

        public static void TryInject(GameObject gameObject, DiContainer diContainer)
        {
            if (gameObject.TryGetComponent(out InjectTargetCollection explicitInjectTargetCollection))
                explicitInjectTargetCollection.Inject(diContainer);
        }

        public void Inject(DiContainer diContainer)
        {
            foreach (var target in Targets)
                diContainer.Inject(target);
        }
    }
}