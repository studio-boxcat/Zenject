using System.Diagnostics;
using UnityEngine;

namespace Zenject
{
    [DebuggerStepThrough]
    public abstract class MonoBehaviourInstaller : MonoBehaviour, IInstaller
    {
        public abstract void InstallBindings(InstallScheme scheme);
    }
}