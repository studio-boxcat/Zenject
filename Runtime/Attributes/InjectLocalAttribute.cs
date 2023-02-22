using System;

namespace Zenject
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
    public class InjectLocalAttribute : InjectAttributeBase
    {
        public InjectLocalAttribute()
            : base(source: InjectSources.Local)
        {
        }

        public InjectLocalAttribute(int id = 0, bool optional = false)
            : base(id, InjectSources.Local, optional)
        {
        }
    }
}