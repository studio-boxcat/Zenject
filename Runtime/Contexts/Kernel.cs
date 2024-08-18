using System;
using System.Collections.Generic;
using System.Linq;

namespace Zenject
{
    readonly struct Kernel
    {
        readonly List<IDisposable> _disposables;
        readonly List<ITickable> _tickables;

        public Kernel(List<IDisposable> disposables, List<ITickable> tickables)
        {
            _disposables = disposables;
            _tickables = tickables;

            // Log.
            L.I("Kernel initialized\n"
                + $"Tickables: [{string.Join(", ", _tickables.Select(t => t.GetType().Name))}]\n"
                + $"Disposables: [{string.Join(", ", _disposables.Select(d => d.GetType().Name))}]");
        }

        public void Dispose()
        {
            L.I("Kernel destroyed\n" +
                $"Disposing {nameof(IDisposable)}s: [{string.Join(", ", _disposables.Select(d => d.GetType().Name))}]");

            // Dispose in reverse order.
            var len = _disposables.Count;
            for (var i = len - 1; i >= 0; i--)
                _disposables[i].Dispose();
        }

        public void Tick()
        {
            foreach (var tickable in _tickables)
                tickable.Tick();
        }
    }
}