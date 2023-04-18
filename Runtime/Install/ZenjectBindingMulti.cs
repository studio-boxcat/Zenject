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
        [ValidateInput("Identifier_Validate")]
        public string Identifier = string.Empty;

        [SerializeField]
        public ZenjectBinding.BindTypes BindType = ZenjectBinding.BindTypes.Self;

        public override void Bind(DiContainer container)
        {
            var identifier = 0;
            if (Identifier.Length > 0)
                identifier = Hasher.Hash(Identifier);

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

#if UNITY_EDITOR
        bool Identifier_Validate(string identifier)
        {
            return identifier.Trim() == identifier;
        }
#endif
    }
}