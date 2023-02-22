using System;

namespace Zenject
{
    [AttributeUsage(AttributeTargets.Method)]
    public class InjectMethodAttribute : InjectAttributeBase
    {
    }
}