using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    public class MethodProviderUntyped : IProvider
    {
        readonly Func<InjectableInfo, object> _method;

        public MethodProviderUntyped(Func<InjectableInfo, object> method)
        {
            _method = method;
        }

        public void GetAllInstancesWithInjectSplit(InjectableInfo context, out Action injectAction, List<object> buffer)
        {
            injectAction = null;
            var result = _method(context);

            if (result == null)
            {
                Assert.That(!context.MemberType.IsPrimitive,
                    "Invalid value returned from FromMethod.  Expected non-null.");
            }
            else
            {
                Assert.That(result.GetType().DerivesFromOrEqual(context.MemberType));
            }

            buffer.Add(result);
        }
    }
}

