using System;

namespace Zenject
{
#if ZENJECT_REFLECTION_BAKING
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
#endif
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
    public class InjectOptionalAttribute : InjectAttributeBase
    {
        public InjectOptionalAttribute()
            : base(optional: true)
        {
        }

        public InjectOptionalAttribute(string id)
            : base(Hasher.Hash(id), optional: true)
        {
        }
    }
}