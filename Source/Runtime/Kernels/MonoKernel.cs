using UnityEngine;

namespace Zenject
{
    public class MonoKernel : MonoBehaviour
    {
        [InjectLocal] TickableManager _tickableManager;
        [InjectLocal] DisposableManager _disposablesManager;

        public void Update()
        {
            // Don't spam the log every frame if initialization fails and leaves it as null
            _tickableManager.Update();
        }

        public void LateUpdate()
        {
            // Don't spam the log every frame if initialization fails and leaves it as null
            _tickableManager.LateUpdate();
        }

        public virtual void OnDestroy()
        {
            // _disposablesManager can be null if we get destroyed before the Start event
            _disposablesManager.Dispose();
        }
    }
}