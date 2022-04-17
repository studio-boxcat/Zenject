using System;

namespace Zenject
{
    [AttributeUsage(AttributeTargets.Parameter
        | AttributeTargets.Field)]
    public class InjectLocalAttribute : InjectAttributeBase
    {
        public InjectLocalAttribute()
        {
            Source = InjectSources.Local;
        }
    }
}
