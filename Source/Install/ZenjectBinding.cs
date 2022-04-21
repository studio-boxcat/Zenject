using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Zenject
{
    public class ZenjectBinding : ZenjectBindingBase
    {
        [FormerlySerializedAs("_components")]
        [SerializeField]
        public Component[] Components = null;

        [FormerlySerializedAs("_identifier")]
        [SerializeField]
        [ValidateInput("Validate_Identifier")]
        public string Identifier = string.Empty;

        [FormerlySerializedAs("_bindType")]
        [SerializeField]
        public BindTypes BindType = BindTypes.Self;

        public enum BindTypes
        {
            Self,
            AllInterfaces,
            AllInterfacesAndSelf,
        }

        public override void Bind(DiContainer container)
        {
            var identifier = 0;
            if (Identifier.Length > 0)
                identifier = Identifier.GetHashCode();

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

                switch (bindType)
                {
                    case BindTypes.Self:
                        container.Bind(component, identifier);
                        break;
                    case BindTypes.AllInterfaces:
                        container.BindInterfacesTo(component, identifier);
                        break;
                    case BindTypes.AllInterfacesAndSelf:
                        container.BindInterfacesAndSelfTo(component, identifier);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(bindType));
                }
            }
        }

#if UNITY_EDITOR
        bool Validate_Identifier(string identifier)
        {
            return identifier.Trim() == identifier;
        }
#endif
    }
}