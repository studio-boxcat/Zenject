using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public class MonoKernel : MonoBehaviour
    {
        [InjectLocal] readonly ITickable[] _tickables;
        [InjectLocal] readonly ILateTickable[] _lateTickables;
        [InjectLocal] readonly IDisposable[] _disposables;

        bool _disposed;

        void OnDestroy()
        {
            Assert.IsFalse(_disposed, "Tried to dispose DisposableManager twice!");
            _disposed = true;

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