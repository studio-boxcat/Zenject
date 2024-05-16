using System;
using UnityEngine.Scripting;

namespace Zenject
{
    [Preserve]
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