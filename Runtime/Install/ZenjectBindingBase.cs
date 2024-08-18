using UnityEngine;

namespace Zenject
{
    abstract class ZenjectBindingBase : MonoBehaviour
    {
        public abstract void Bind(InstallScheme scheme);
    }
}