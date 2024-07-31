using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenject
{
    public sealed class Kernel : MonoBehaviour
    {
        readonly List<ITickable> _tickables = new();
        readonly List<IDisposable> _disposables = new();

        public void RegisterServices(DiContainer diContainer)
        {
            diContainer.ResolveAll(_tickables);
            diContainer.ResolveAll(_disposables);

            // If there are no tickables, disable the kernel to prevent unnecessary updates.
            if (_tickables.Count is 0)
                enabled = false;
        }

        void OnDestroy()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();
        }

        void Update()
        {
            foreach (var tickable in _tickables)
                tickable.Tick();
        }
    }
}