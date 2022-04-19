using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Zenject
{
    public class ZenjectBinding : ZenjectBindingBase
    {
        [FormerlySerializedAs("_components")]
        [Tooltip("The component to add to the Zenject container")]
        [SerializeField]
        public Component[] Components = null;

        [FormerlySerializedAs("_identifier")]
        [Tooltip("Note: This value is optional and can be ignored in most cases.  This can be useful to differentiate multiple bindings of the same type.  For example, if you have multiple cameras in your scene, you can 'name' them by giving each one a different identifier.  For your main camera you might call it 'Main' then any class can refer to it by using an attribute like [Inject('Main')]")]
        [SerializeField]
        [ValidateInput("Validate_Identifier")]
        public string Identifier = string.Empty;

        [FormerlySerializedAs("_bindType")]
        [Tooltip("This value is used to determine how to bind this component.  When set to 'Self' is equivalent to calling Container.FromInstance inside an installer. When set to 'AllInterfaces' this is equivalent to calling 'Container.BindInterfaces<MyMonoBehaviour>().ToInstance', and similarly for InterfacesAndSelf")]
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

                var bindInfoBuilder = bindType switch
                {
                    BindTypes.Self => container.Bind(component),
                    BindTypes.AllInterfaces => container.BindInterfacesTo(component),
                    BindTypes.AllInterfacesAndSelf => container.BindInterfacesAndSelfTo(component),
                };

                if (identifier != 0)
                    bindInfoBuilder.WithId(identifier);
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