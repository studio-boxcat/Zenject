using UnityEngine;

namespace Zenject
{
    public abstract class ZenjectBindingBase : MonoBehaviour
    {
        public abstract void Bind(DiContainer container);
    }
}