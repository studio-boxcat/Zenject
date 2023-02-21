using UnityEngine;

namespace Zenject
{
    public abstract class ScriptableObjectInstaller : ScriptableObject, IInstaller, IZenject_Initializable
    {
        [Inject] public DiContainer Container;

        public virtual void Initialize(DependencyProvider dp)
        {
            Container = (DiContainer) dp.Resolve(typeof(DiContainer));
        }

        public abstract void InstallBindings();
    }
}