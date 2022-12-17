using System;

namespace Zenject
{
    [AttributeUsage(
        AttributeTargets.Constructor
        | AttributeTargets.Field
        | AttributeTargets.Parameter)]
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