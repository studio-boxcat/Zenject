using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenject
{
    public sealed class Kernel : MonoBehaviour
    {
        readonly List<ITickable> _tickables = new();
        readonly List<ILateTickable> _lateTickables = new();
        readonly List<IDisposable> _disposables = new();

        public void RegisterServices(DiContainer diContainer)
        {
            diContainer.ResolveAll(_tickables);
            diContainer.ResolveAll(_lateTickables);
            diContainer.ResolveAll(_disposables);
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

        void LateUpdate()
        {
            foreach (var tickable in _lateTickables)
                tickable.LateTick();
        }
    }
}