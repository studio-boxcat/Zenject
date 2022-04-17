using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    public class GetterProvider<TObj, TResult> : IProvider
    {
        readonly DiContainer _container;
        readonly object _identifier;
        readonly Func<TObj, TResult> _method;
        readonly bool _matchAll;
        readonly InjectSources _sourceType;

        public GetterProvider(
            object identifier, Func<TObj, TResult> method,
            DiContainer container, InjectSources sourceType, bool matchAll)
        {
            _container = container;
            _identifier = identifier;
            _method = method;
            _matchAll = matchAll;
            _sourceType = sourceType;
        }

        public void GetAllInstancesWithInjectSplit(InjectableInfo context, out Action injectAction, List<object> buffer)
        {
            Assert.That(typeof(TResult).DerivesFromOrEqual(context.MemberType));

            var subContext = new InjectableInfo(typeof(TObj), _identifier, _sourceType);

            injectAction = null;

            if (_matchAll)
            {
                Assert.That(buffer.Count == 0);
                _container.ResolveAll(subContext, buffer);

                for (var i = 0; i < buffer.Count; i++)
                {
                    buffer[i] = _method((TObj) buffer[i]);
                }
            }
            else
            {
                buffer.Add(_method((TObj) _container.Resolve(subContext)));
            }
        }
    }
}