using UnityEngine;

namespace Zenject
{
    public abstract class ScriptableObjectInstaller : ScriptableObject, IInstaller
    {
        public DiContainer Container { get; set; }
        public abstract void InstallBindings();
    }
}