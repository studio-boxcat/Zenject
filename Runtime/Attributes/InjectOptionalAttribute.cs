using System;

namespace Zenject
{
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