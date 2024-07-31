#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidator(typeof(Zenject.BindIdValidator))]

namespace Zenject
{
    public class BindIdValidator : ValueValidator<BindId>
    {
        protected override void Validate(ValidationResult result)
        {
            var value = ValueEntry.SmartValue;
            if (BindIdDict.Valid(value) is false)
                result.AddError("BindId is not valid: " + value);
        }
    }
}
#endif