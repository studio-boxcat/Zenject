using System;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Zenject
{
    [Preserve]
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    public abstract class InjectAttributeBase : Attribute
    {
        public bool Optional;
        public object Id;
        public InjectSources Source;
    }
}