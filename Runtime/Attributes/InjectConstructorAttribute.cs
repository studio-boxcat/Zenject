using System;

namespace Zenject
{
#if ZENJECT_REFLECTION_BAKING
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
#endif
    [AttributeUsage(AttributeTargets.Constructor)]
    public class InjectConstructorAttribute : InjectAttributeBase
    {
    }
}