using System;

namespace Zenject
{
#if ZENJECT_REFLECTION_BAKING
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
#endif
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
    public class InjectAttribute : InjectAttributeBase
    {
        public InjectAttribute()
        {
        }

        public InjectAttribute(string id)
            : base(Hasher.Hash(id))
        {
        }
    }
}