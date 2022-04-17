using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    public class MethodProviderSimple<TReturn> : IProvider
    {
        readonly Func<TReturn> _method;

        public MethodProviderSimple(Func<TReturn> method)
        {
            _method = method;
        }

        public void GetAllInstancesWithInjectSplit(InjectableInfo context, out Action injectAction, List<object> buffer)
        {
            Assert.That(typeof(TReturn).DerivesFromOrEqual(context.MemberType));

            injectAction = null;
            buffer.Add(_method());
        }
    }
}
