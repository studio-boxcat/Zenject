using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public class MonoKernel : MonoBehaviour, IZenject_Initializable
    {
        [InjectLocal] ITickable[] _tickables;
        [InjectLocal] ILateTickable[] _lateTickables;
        [InjectLocal] IDisposable[] _disposables;

        bool _disposed;

        void IZenject_Initializable.Initialize(DependencyProvider dp)
        {
            _tickables = (ITickable[]) dp.Resolve(typeof(ITickable[]), sourceType: InjectSources.Local);
            _lateTickables = (ILateTickable[]) dp.Resolve(typeof(ILateTickable[]), sourceType: InjectSources.Local);
            _disposables = (IDisposable[]) dp.Resolve(typeof(IDisposable[]), sourceType: InjectSources.Local);
        }

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