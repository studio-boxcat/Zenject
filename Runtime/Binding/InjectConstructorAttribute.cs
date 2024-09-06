using System;
using JetBrains.Annotations;

namespace Zenject
{
    [AttributeUsage(AttributeTargets.Constructor)]
    [MeansImplicitUse(ImplicitUseKindFlags.Access)]
    public class InjectConstructorAttribute : InjectAttributeBase { }
}