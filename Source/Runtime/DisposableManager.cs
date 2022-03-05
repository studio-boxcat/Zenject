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
        readonly List<LateDisposableInfo> _lateDisposables;
        bool _disposed;
        bool _lateDisposed;

        [Inject]
        public DisposableManager(
            [Inject(Optional = true, Source = InjectSources.Local)]
            List<IDisposable> disposables,
            [Inject(Optional = true, Source = InjectSources.Local)]
            List<ILateDisposable> lateDisposables)
        {
            _disposables = new List<DisposableInfo>(disposables.Count);
            _lateDisposables = new List<LateDisposableInfo>(lateDisposables.Count);

            foreach (var disposable in disposables)
            {
                // Note that we use zero for unspecified priority
                // This is nice because you can use negative or positive for before/after unspecified
                var priority = disposable.GetType().GetCustomAttribute<ExecutionPriorityAttribute>()?.Priority ?? 0;
                _disposables.Add(new DisposableInfo(disposable, priority));
            }

            foreach (var lateDisposable in lateDisposables)
            {
                var priority = lateDisposable.GetType().GetCustomAttribute<ExecutionPriorityAttribute>()?.Priority ?? 0;
                _lateDisposables.Add(new LateDisposableInfo(lateDisposable, priority));
            }
        }

        public void Add(IDisposable disposable)
        {
            Add(disposable, 0);
        }

        public void Add(IDisposable disposable, int priority)
        {
            _disposables.Add(
                new DisposableInfo(disposable, priority));
        }

        public void AddLate(ILateDisposable disposable)
        {
            AddLate(disposable, 0);
        }

        public void AddLate(ILateDisposable disposable, int priority)
        {
            _lateDisposables.Add(
                new LateDisposableInfo(disposable, priority));
        }

        public void Remove(IDisposable disposable)
        {
            _disposables.RemoveWithConfirm(
                _disposables.Single(x => ReferenceEquals(x.Disposable, disposable)));
        }

        public void LateDispose()
        {
            Assert.That(!_lateDisposed, "Tried to late dispose DisposableManager twice!");
            _lateDisposed = true;

            // Dispose in the reverse order that they are initialized in
            _lateDisposables.Sort((a, b) => b.Priority.CompareTo(a.Priority));

#if UNITY_EDITOR
            foreach (var disposable in _lateDisposables.Select(x => x.LateDisposable).GetDuplicates())
            {
                Assert.That(false, "Found duplicate ILateDisposable with type '{0}'".Fmt(disposable.GetType()));
            }
#endif

            foreach (var disposable in _lateDisposables)
            {
                try
                {
                    disposable.LateDisposable.LateDispose();
                }
                catch (Exception e)
                {
                    throw Assert.CreateException(
                        e, "Error occurred while late disposing ILateDisposable with type '{0}'", disposable.LateDisposable.GetType());
                }
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

        struct LateDisposableInfo
        {
            public readonly ILateDisposable LateDisposable;
            public readonly int Priority;

            public LateDisposableInfo(ILateDisposable lateDisposable, int priority)
            {
                LateDisposable = lateDisposable;
                Priority = priority;
            }
        }
    }
}
