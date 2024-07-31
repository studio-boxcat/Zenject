using Sirenix.OdinInspector;
using UnityEngine;

namespace Zenject
{
    public class ZenjectBindingMulti : ZenjectBindingBase
    {
        [ListDrawerSettings(IsReadOnly = true)]
        [SerializeField, Required]
        public Component[] Components = null;

        [SerializeField]
        public BindId Id;

        [SerializeField]
        public ZenjectBinding.BindTypes BindType = ZenjectBinding.BindTypes.Self;

        public override void Bind(DiContainer container)
        {
            var identifier = Id;

            foreach (var component in Components)
            {
                var bindType = BindType;

                if (component == null)
                {
#if DEBUG
                    Debug.LogWarning($"Found null component in ZenjectBinding on object '{name}'");
#endif
                    continue;
                }

                ZenjectBinding.Bind(container, component, bindType, identifier);
            }
        }
    }
}