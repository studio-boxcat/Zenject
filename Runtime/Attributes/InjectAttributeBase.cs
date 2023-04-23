using System;
using JetBrains.Annotations;

namespace Zenject
{
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    public abstract class InjectAttributeBase : Attribute
    {
        public readonly int Id;
        public readonly bool Optional;

        protected InjectAttributeBase(int id = 0, bool optional = false)
        {
            Id = id;
            Optional = optional;
        }
    }
}