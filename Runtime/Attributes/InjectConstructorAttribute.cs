using System;

namespace Zenject
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public class InjectConstructorAttribute : InjectAttributeBase
    {
    }
}