using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;

[assembly: RegisterValidator(typeof(Zenject.Editor.InjectionNotRequiredValidator_GameObject))]
[assembly: RegisterValidator(typeof(Zenject.Editor.InjectionNotRequiredValidator_Component<>))]

namespace Zenject.Editor
{
    class InjectionNotRequiredValidator_GameObject : AttributeValidator<InjectionNotRequiredAttribute, GameObject>
    {
        protected override void Validate(ValidationResult result)
        {
            var value = ValueEntry.SmartValue;
            Validate(value, result);
        }

        public static void Validate(GameObject go, ValidationResult result)
        {
            if (go.TryGetComponent(out InjectTargetCollection _))
            {
                result.ResultType = ValidationResultType.Error;
                result.Message = "Given GameObject has an InjectTargetCollection component.";
                return;
            }

            if (go.TryGetComponent(out GameObjectContext _))
            {
                result.ResultType = ValidationResultType.Error;
                result.Message = "Given GameObject has a GameObjectContext component.";
                return;
            }
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