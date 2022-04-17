using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    public class InstanceProvider : IProvider
    {
        readonly object _instance;
        readonly Type _instanceType;

        public InstanceProvider(
            Type instanceType, object instance)
        {
            _instanceType = instanceType;
            _instance = instance;
        }

        public Type GetInstanceType(InjectableInfo context)
        {
            return _instanceType;
        }

        public void GetAllInstancesWithInjectSplit(InjectableInfo context, out Action injectAction, List<object> buffer)
        {
            Assert.IsNotNull(context);

            Assert.That(_instanceType.DerivesFromOrEqual(context.MemberType));

            injectAction = null;

            buffer.Add(_instance);
        }
    }
}
