using System;

namespace Zenject
{
    [AttributeUsage(
        AttributeTargets.Constructor
        | AttributeTargets.Method
        | AttributeTargets.Parameter
        | AttributeTargets.Field)]
    public class InjectAttribute : InjectAttributeBase
    {
        public InjectAttribute()
        {
        }

        public InjectAttribute(string id)
            : base(id.GetHashCode())
        {
        }
    }
}