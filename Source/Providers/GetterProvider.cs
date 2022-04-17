using System;
using ModestTree;

namespace Zenject
{
    public class GetterProvider<TObj, TResult> : IProvider
    {
        readonly DiContainer _container;
        readonly object _identifier;
        readonly Func<TObj, TResult> _method;
        readonly InjectSources _sourceType;

        public GetterProvider(
            object identifier, Func<TObj, TResult> method,
            DiContainer container, InjectSources sourceType)
        {
            _container = container;
            _identifier = identifier;
            _method = method;
            _sourceType = sourceType;
        }

        public object GetInstance()
        {
            var subContext = new InjectableInfo(typeof(TObj), _identifier, _sourceType);
            return _method((TObj) _container.Resolve(subContext));
        }
    }
}