#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;

[assembly: RegisterValidator(typeof(Zenject.InjectionNotRequiredValidator_GameObject))]
[assembly: RegisterValidator(typeof(Zenject.InjectionNotRequiredValidator_Component<>))]

namespace Zenject
{
    class InjectionNotRequiredValidator_GameObject : AttributeValidator<InjectionNotRequiredAttribute, GameObject>
    {
        protected override void Validate(ValidationResult result)
        {
            var value = ValueEntry.SmartValue;
            if (value == null) return;
            Validate(value, result);
        }

        public static void Validate(GameObject go, ValidationResult result)
        {
            if (go.TryGetComponent(out InjectTargetCollection _))
                result.AddError("Given GameObject has an InjectTargetCollection component.");
            if (go.TryGetComponent(out GameObjectContext _))
                result.AddError("Given GameObject has a GameObjectContext component.");
        }
    }

    class InjectionNotRequiredValidator_Component<TComponent> : AttributeValidator<InjectionNotRequiredAttribute, TComponent>
        where TComponent : Component
    {
        protected override void Validate(ValidationResult result)
        {
            var value = ValueEntry.SmartValue;
            if (value == null) return;
            InjectionNotRequiredValidator_GameObject.Validate(value.gameObject, result);
        }
    }
}
#endif