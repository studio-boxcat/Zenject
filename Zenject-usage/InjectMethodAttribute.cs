using System;

namespace Zenject
{
    [AttributeUsage(
        AttributeTargets.Constructor
        | AttributeTargets.Method)]
    public class InjectMethodAttribute : InjectAttributeBase
    {
    }
}