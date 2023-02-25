using Sirenix.OdinInspector;
using UnityEngine;

namespace Zenject
{
    [DisallowMultipleComponent]
    public partial class ZenjectBindingCollection : MonoBehaviour
    {
        [ListDrawerSettings(IsReadOnly = true)]
        [ValidateInput("Validate_Bindings")]
        public ZenjectBindingBase[] Bindings;

        public static void TryBind(GameObject gameObject, DiContainer diContainer)
        {
            if (gameObject.TryGetComponent(out ZenjectBindingCollection zenjectBindings))
                zenjectBindings.Bind(diContainer);
        }

        public void Bind(DiContainer diContainer)
        {
            foreach (var binding in Bindings)
                binding.Bind(diContainer);
            Bindings = null;
        }
    }
}