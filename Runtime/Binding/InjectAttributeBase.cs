using UnityEngine.Scripting;

namespace Zenject
{
    public abstract class InjectAttributeBase : PreserveAttribute
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