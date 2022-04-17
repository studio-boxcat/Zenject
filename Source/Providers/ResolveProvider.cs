using System;
using System.Collections.Generic;
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
        readonly bool _matchAll;

        public ResolveProvider(
            Type contractType, DiContainer container, object identifier,
            bool isOptional, InjectSources source, bool matchAll)
        {
            _contractType = contractType;
            _identifier = identifier;
            _container = container;
            _isOptional = isOptional;
            _source = source;
            _matchAll = matchAll;
        }

        public Type GetInstanceType(InjectableInfo context)
        {
            return _contractType;
        }

        public void GetAllInstancesWithInjectSplit(InjectableInfo context, out Action injectAction, List<object> buffer)
        {
            Assert.IsNotNull(context);

            Assert.That(_contractType.DerivesFromOrEqual(context.MemberType));

            var subContext = new InjectableInfo(
                _contractType, _identifier, _source, _isOptional);

            injectAction = null;
            if (_matchAll)
            {
                _container.ResolveAll(subContext, buffer);
            }
            else
            {
                buffer.Add(_container.Resolve(subContext));
            }
        }
    }
}