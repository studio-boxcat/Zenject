using System;
using JetBrains.Annotations;

namespace Zenject
{
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse(ImplicitUseKindFlags.Access)]
    public class InjectMethodAttribute : InjectAttributeBase { }
}