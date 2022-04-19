using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public class MonoKernel : MonoBehaviour
    {
        [InjectLocal] readonly List<ITickable> _tickables;
        [InjectLocal] readonly List<ILateTickable> _lateTickables;
        [InjectLocal] readonly List<IDisposable> _disposables;

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
            // XXX: 재진입을 지원하기 위해서 Count 까지만 업데이트.
            var count = _tickables.Count;
            for (var i = 0; i < count; i++)
                _tickables[i].Tick();
        }

        void LateUpdate()
        {
            // XXX: 재진입을 지원하기 위해서 Count 까지만 업데이트.
            var count = _lateTickables.Count;
            for (var i = 0; i < count; i++)
                _lateTickables[i].LateTick();
        }
    }
}