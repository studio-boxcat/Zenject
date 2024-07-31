using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Zenject
{
    sealed class Kernel : MonoBehaviour
    {
        readonly List<ITickable> _tickables = new();
        readonly List<IDisposable> _disposables = new();

        public void RegisterServices(DiContainer diContainer)
        {
            // Do not resolve from parent containers
            diContainer.ResolveAllFromSelf(_tickables);
            diContainer.ResolveAllFromSelf(_disposables);

            // If there are no tickables, disable the kernel to prevent unnecessary updates.
            if (_tickables.Count is 0)
                enabled = false;

            // Log
            L.I($"Kernel initialized: {name}\n" +
                $"Tickables: [{string.Join(", ", _tickables.Select(t => t.GetType().Name))}]\n" +
                $"Disposables: [{string.Join(", ", _disposables.Select(d => d.GetType().Name))}]");
        }

        void OnDestroy()
        {
            L.I($"Kernel destroyed: {name}\n" +
                $"Disposables: [{string.Join(", ", _disposables.Select(d => d.GetType().Name))}]");

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