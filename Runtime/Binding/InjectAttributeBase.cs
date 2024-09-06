using System;
using UnityEngine.Scripting;

namespace Zenject
{
    [Preserve]
    public abstract class InjectAttributeBase : Attribute
    {
        public readonly BindId Id;
        public readonly bool Optional;

        protected InjectAttributeBase(BindId id = 0, bool optional = false)
        {
            Id = id;
            Optional = optional;
        }
    }
}