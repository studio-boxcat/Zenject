using System;

namespace Zenject
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
    public class InjectAttribute : InjectAttributeBase
    {
        public InjectAttribute()
        {
        }

        public InjectAttribute(BindId id)
            : base(id)
        {
        }
    }
}