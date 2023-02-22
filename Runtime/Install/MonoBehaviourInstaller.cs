using System.Diagnostics;
using UnityEngine;

namespace Zenject
{
    [DebuggerStepThrough]
    public abstract class MonoBehaviourInstaller : MonoBehaviour, IInstaller
    {
        public DiContainer Container { get; set; }
        public abstract void InstallBindings();
    }
}