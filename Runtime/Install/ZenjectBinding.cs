using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Zenject
{
    class ZenjectBinding : ZenjectBindingBase
    {
        [SerializeField, Required, ShowIf("@Target == null")]
        public Object Target;
        [SerializeField]
        public BindId Identifier;

        public override void Bind(InstallScheme scheme)
        {
            if (Target == null)
            {
#if DEBUG
                L.W($"Found null component in ZenjectBinding on object '{name}'", this);
#endif
                return;
            }

            scheme.Bind(Target.GetType(), Target, Identifier);
        }

#if UNITY_EDITOR
        [ShowInInspector, LabelText("Target"), PropertyOrder(-2), ShowIf("@Target != null")]
        Object _target
        {
            get => Target;
            set => Target = value;
        }

        [ShowInInspector, LabelText("Component"), PropertyOrder(-1),
         ShowIf("@Target != null"), ValueDropdown("Target_Dropdown")]
        Object _targetDropdown
        {
            get => Target;
            set => Target = value;
        }

        [ShowInInspector, DisplayAsString]
        string _contractType => Target == null ? "" : Target.GetType().Name;

        ValueDropdownList<Object> Target_Dropdown()
        {
            if (Target == null)
                return null;

            var list = new ValueDropdownList<Object>();

            var targetGO = Target is Component c ? c.gameObject : (GameObject) Target;
            list.Add("GameObject", targetGO);

            var components = targetGO.GetComponents<Component>();
            foreach (var component in components)
                list.Add(component.GetType().Name, component);

            return list;
        }
#endif
    }
}