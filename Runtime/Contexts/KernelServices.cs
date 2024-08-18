using System;
using System.Collections.Generic;

namespace Zenject
{
    public partial class InstallScheme
    {
        readonly struct KernelServices
        {
            public readonly List<IDisposable> Disposables1;
            public readonly List<ulong> Disposables2;
            public readonly List<ITickable> Tickables1;
            public readonly List<ulong> Tickables2;

            KernelServices(
                List<IDisposable> disposables1,
                List<ulong> disposables2,
                List<ITickable> tickables1,
                List<ulong> tickables2)
            {
                Disposables1 = disposables1;
                Disposables2 = disposables2;
                Tickables1 = tickables1;
                Tickables2 = tickables2;
            }

            public static KernelServices Create(int capacity)
            {
                return new KernelServices(
                    new List<IDisposable>(capacity),
                    new List<ulong>(capacity),
                    new List<ITickable>(capacity),
                    new List<ulong>(capacity));
            }

            public void ResolveAll(DiContainer diContainer)
            {
                var tickables = Tickables1;
                foreach (var bindKey in Tickables2)
                    tickables.Add((ITickable) diContainer.Resolve(bindKey));
                Tickables2.Clear();

                var disposables = Disposables1;
                foreach (var bindKey in Disposables2)
                    disposables.Add((IDisposable) diContainer.Resolve(bindKey));
                Disposables2.Clear();
            }
        }
    }
}