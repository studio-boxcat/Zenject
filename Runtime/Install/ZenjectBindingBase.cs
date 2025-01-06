using UnityEngine;

namespace Zenject
{
    internal abstract class ZenjectBindingBase : MonoBehaviour
    {
        public abstract void Bind(InstallScheme scheme);
    }
}