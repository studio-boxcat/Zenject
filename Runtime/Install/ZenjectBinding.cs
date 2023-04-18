using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Zenject
{
    public class ZenjectBinding : ZenjectBindingBase
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField, Required, ShowIf("@Target == null")]
        public Object Target;

        [SerializeField]
        [ValidateInput("Identifier_Validate")]
        public string Identifier = string.Empty;

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
                identifier = Hasher.Hash(Identifier);

            if (Target == null)
            {
#if DEBUG
                Debug.LogWarning($"Found null component in ZenjectBinding on object '{name}'");
#endif
                return;
            }

            Bind(container, Target, BindType, identifier);
        }

        public static void Bind(DiContainer container, object target, BindTypes bindType, int identifier)
        {
            switch (bindType)
            {
                case BindTypes.Self:
                    container.Bind(target, identifier);
                    break;
                case BindTypes.AllInterfaces:
                    container.BindInterfacesTo(target, identifier);
                    break;
                case BindTypes.AllInterfacesAndSelf:
                    container.BindInterfacesAndSelfTo(target, identifier);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bindType));
            }
        }

#if UNITY_EDITOR
        [ShowInInspector, LabelText("Target"), PropertyOrder(-2), ShowIf("@Target != null")]
        GameObject _targetGameObject
        {
            get => Target is Component component ? component.gameObject : (GameObject) Target;
            set => Target = value;
        }

        [ShowInInspector, LabelText("Component"), PropertyOrder(-1),
         ShowIf("@Target != null"), ValueDropdown("Target_Dropdown")]
        Object _targetDropdown
        {
            get => Target;
            set => Target = value;
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (Target == null)
                return;

            if (BindType is BindTypes.AllInterfaces or BindTypes.AllInterfacesAndSelf)
            {
                var type = Target.GetType();
                if (type.GetInterfaces().Length == 0)
                    result.AddError("Target does not implement any interfaces");
            }
        }

        ValueDropdownList<Object> Target_Dropdown()
        {
            if (Target == null)
                return null;

            var list = new ValueDropdownList<Object>();

            var targetGO = _targetGameObject;
            list.Add("GameObject", targetGO);

            var components = targetGO.GetComponents<Component>();
            foreach (var component in components)
                list.Add(component.GetType().Name, component);

            return list;
        }

        bool Identifier_Validate(string identifier)
        {
            return identifier.Trim() == identifier;
        }
#endif
    }
}