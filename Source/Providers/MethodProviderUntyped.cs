using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    public class MethodProviderUntyped : IProvider
    {
        readonly DiContainer _container;
        readonly Func<InjectableInfo, object> _method;

        public MethodProviderUntyped(
            Func<InjectableInfo, object> method,
            DiContainer container)
        {
            _container = container;
            _method = method;
        }

        public Type GetInstanceType(InjectableInfo context)
        {
            return context.MemberType;
        }

        public void GetAllInstancesWithInjectSplit(InjectableInfo context, out Action injectAction, List<object> buffer)
        {
            Assert.IsNotNull(context);

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

