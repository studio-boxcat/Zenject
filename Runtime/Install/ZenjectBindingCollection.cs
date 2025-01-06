using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Zenject
{
    [DisallowMultipleComponent]
    internal partial class ZenjectBindingCollection : ZenjectBindingBase
    {
        [SerializeField, Required]
        [FormerlySerializedAs("Bindings")]
        [ListDrawerSettings(IsReadOnly = true)]
        [ValidateInput("Validate_Bindings")]
        private ZenjectBindingBase[] _bindings;

        public override void Bind(InstallScheme scheme)
        {
            var len = _bindings.Length;

            for (var index = 0; index < len; index++)
            {
                var binding = _bindings[index];

#if UNITY_EDITOR
                if (binding == null)
                {
                    L.E($"Binding is null at index {index}.");
                    continue;
                }
#endif

                binding.Bind(scheme);
            }

            _bindings = null;
        }
    }
}