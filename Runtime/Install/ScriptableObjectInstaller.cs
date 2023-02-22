using UnityEngine;

namespace Zenject
{
    public abstract class ScriptableObjectInstaller : ScriptableObject, IInstaller
    {
        public abstract void InstallBindings(DiContainer container);
    }
}