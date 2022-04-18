using System;

namespace Zenject
{
    public class ResolveProvider : IProvider
    {
        readonly DiContainer _container;
        readonly Type _contractType;
        readonly int _identifier;
        readonly InjectSources _source;
        readonly bool _isOptional;

        public ResolveProvider(
            DiContainer container,
            Type contractType,
            int identifier,
            InjectSources source,
            bool isOptional)
        {
            _contractType = contractType;
            _identifier = identifier;
            _container = container;
            _isOptional = isOptional;
            _source = source;
        }

        public object GetInstance()
        {
            return _container.Resolve(new InjectableInfo(
                _contractType, _identifier, _source, _isOptional));
        }
    }
}