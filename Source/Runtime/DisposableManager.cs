using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModestTree;

namespace Zenject
{
    public class DisposableManager : IDisposable
    {
        readonly List<DisposableInfo> _disposables;
        bool _disposed;

        [Inject]
        public DisposableManager(
            [Inject(Optional = true, Source = InjectSources.Local)]
            List<IDisposable> disposables)
        {
            _disposables = new List<DisposableInfo>(disposables.Count);

            foreach (var disposable in disposables)
            {
                // Note that we use zero for unspecified priority
                // This is nice because you can use negative or positive for before/after unspecified
                var priority = disposable.GetType().GetCustomAttribute<ExecutionPriorityAttribute>()?.Priority ?? 0;
                _disposables.Add(new DisposableInfo(disposable, priority));
            }
        }

        public void Dispose()
        {
            Assert.That(!_disposed, "Tried to dispose DisposableManager twice!");
            _disposed = true;

            // Dispose in the reverse order that they are initialized in
            _disposables.Sort((a, b) => b.Priority.CompareTo(a.Priority));

#if UNITY_EDITOR
            foreach (var disposable in _disposables.Select(x => x.Disposable).GetDuplicates())
            {
                Assert.That(false, "Found duplicate IDisposable with type '{0}'".Fmt(disposable.GetType()));
            }
#endif

            foreach (var disposable in _disposables)
            {
                try
                {
                    disposable.Disposable.Dispose();
                }
                catch (Exception e)
                {
                    throw Assert.CreateException(
                        e, "Error occurred while disposing IDisposable with type '{0}'", disposable.Disposable.GetType());
                }
            }
        }

        struct DisposableInfo
        {
            public readonly IDisposable Disposable;
            public readonly int Priority;

            public DisposableInfo(IDisposable disposable, int priority)
            {
                Disposable = disposable;
                Priority = priority;
            }
        }
    }
}
