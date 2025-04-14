using Sirenix.OdinInspector;
using UnityEngine;

namespace Zenject
{
    public abstract class ScriptableObjectInstaller : ScriptableObject
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        public abstract void InstallBindings(InstallScheme scheme, Component context);

#if UNITY_EDITOR
        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (Injector.IsInjectionRequired(GetType()))
                result.AddError("Injection for ScriptableObjectInstaller is prohibited.");
        }
#endif
    }
}