using UnityEngine;

namespace Zenject
{
    public abstract class ScriptableObjectInstaller : ScriptableObject, IInstaller
    {
        [Inject]
        public readonly DiContainer Container;

        public abstract void InstallBindings();
    }
}