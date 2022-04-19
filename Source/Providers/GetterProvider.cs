using System;

namespace Zenject
{
    public class GetterProvider<TObj, TResult> : IProvider
    {
        readonly DiContainer _container;
        readonly int _identifier;
        readonly Func<TObj, TResult> _method;
        readonly InjectSources _sourceType;

        public GetterProvider(
            int identifier, Func<TObj, TResult> method,
            DiContainer container, InjectSources sourceType)
        {
            _container = container;
            _identifier = identifier;
            _method = method;
            _sourceType = sourceType;
        }

        public object GetInstance()
        {
            return _method((TObj) _container.Resolve(typeof(TObj), _identifier, _sourceType));
        }
    }
}