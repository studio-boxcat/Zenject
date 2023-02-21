using System.Diagnostics;
using UnityEngine;

namespace Zenject
{
    [DebuggerStepThrough]
    public abstract class MonoInstaller : MonoBehaviour, IInstaller, IZenject_Initializable
    {
        [Inject] public DiContainer Container;

        public virtual void Initialize(DependencyProvider dp)
        {
            Container = (DiContainer) dp.Resolve(typeof(DiContainer));
        }

        public abstract void InstallBindings();
    }
}