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
            // TODO: Should resolve all object regardless of the identifier.
            var providerRepo = diContainer.ProviderRepo;
            providerRepo.ResolveAll(new BindingId(typeof(ITickable)), _tickables);
            providerRepo.ResolveAll(new BindingId(typeof(ILateTickable)), _lateTickables);
            providerRepo.ResolveAll(new BindingId(typeof(IDisposable)), _disposables);
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

        public void ForceDispose()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();
            _disposables.Clear();
        }
    }
}