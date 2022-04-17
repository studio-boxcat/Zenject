using System;

namespace Zenject
{
    [AttributeUsage(AttributeTargets.Constructor
        | AttributeTargets.Method | AttributeTargets.Parameter
        | AttributeTargets.Property | AttributeTargets.Field)]
    public class InjectAttribute : InjectAttributeBase
    {
    }
}

