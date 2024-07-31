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

        public InjectOptionalAttribute(BindId id)
            : base(id, optional: true)
        {
        }
    }
}