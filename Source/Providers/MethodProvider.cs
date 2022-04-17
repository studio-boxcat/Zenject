using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    public class MethodProvider<TReturn> : IProvider
    {
        readonly DiContainer _container;
        readonly Func<InjectableInfo, TReturn> _method;

        public MethodProvider(
            Func<InjectableInfo, TReturn> method,
            DiContainer container)
        {
            _container = container;
            _method = method;
        }

        public Type GetInstanceType(InjectableInfo context)
        {
            return typeof(TReturn);
        }

        public void GetAllInstancesWithInjectSplit(InjectableInfo context, out Action injectAction, List<object> buffer)
        {
            Assert.IsNotNull(context);

            Assert.That(typeof(TReturn).DerivesFromOrEqual(context.MemberType));

            injectAction = null;
            // We cannot do a null assert here because in some cases they might intentionally
            // return null
            buffer.Add(_method(context));
        }
    }
}
