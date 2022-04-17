using System.Diagnostics;
using UnityEngine;

namespace Zenject
{
    [DebuggerStepThrough]
    public abstract class MonoInstaller : MonoBehaviour, IInstaller
    {
        [Inject]
        public readonly DiContainer Container;

        public abstract void InstallBindings();
    }
}