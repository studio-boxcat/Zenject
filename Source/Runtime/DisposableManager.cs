using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    public class DisposableManager : IDisposable
    {
        readonly List<IDisposable> _disposables;
        bool _disposed;

        public DisposableManager(
            [InjectLocal(optional: true)] List<IDisposable> disposables)
        {
            _disposables = disposables;
        }

        public void Dispose()
        {
            Assert.That(!_disposed, "Tried to dispose DisposableManager twice!");
            _disposed = true;

            foreach (var disposable in _disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception e)
                {
                    throw Assert.CreateException(
                        e, "Error occurred while disposing IDisposable with type '{0}'", disposable.GetType());
                }
            }
        }
    }
}