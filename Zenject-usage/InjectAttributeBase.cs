using System;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Zenject
{
    [Preserve]
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    public abstract class InjectAttributeBase : Attribute
    {
        public readonly int Id;
        public readonly InjectSources Source;
        public readonly bool Optional;

        protected InjectAttributeBase(int id = 0, InjectSources source = InjectSources.Any, bool optional = false)
        {
            Id = id;
            Source = source;
            Optional = optional;
        }
    }
}