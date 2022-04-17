using System;
using ModestTree;

namespace Zenject
{
    public class ResolveProvider : IProvider
    {
        readonly object _identifier;
        readonly DiContainer _container;
        readonly Type _contractType;
        readonly bool _isOptional;
        readonly InjectSources _source;

        public ResolveProvider(
            Type contractType, DiContainer container, object identifier,
            bool isOptional, InjectSources source)
        {
            _contractType = contractType;
            _identifier = identifier;
            _container = container;
            _isOptional = isOptional;
            _source = source;
        }

        public object GetInstanceWithInjectSplit(InjectableInfo context, out Action injectAction)
        {
            Assert.That(_contractType.DerivesFromOrEqual(context.MemberType));

            var subContext = new InjectableInfo(
                _contractType, _identifier, _source, _isOptional);

            injectAction = null;
            return _container.Resolve(subContext);
        }
    }
}