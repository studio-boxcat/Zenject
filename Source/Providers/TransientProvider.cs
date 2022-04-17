using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using ModestTree;

namespace Zenject
{
    [NoReflectionBaking]
    public class TransientProvider : IProvider
    {
        readonly DiContainer _container;
        readonly Type _concreteType;
        [CanBeNull] readonly object[] _extraArguments;
        readonly object _concreteIdentifier;

        public TransientProvider(Type concreteType, DiContainer container,
            [CanBeNull] object[] extraArguments,
            object concreteIdentifier)
        {
            Assert.That(!concreteType.IsAbstract,
                "Expected non-abstract type for given binding but instead found type '{0}'",
                concreteType);

            _container = container;
            _concreteType = concreteType;
            _extraArguments = extraArguments;
            _concreteIdentifier = concreteIdentifier;
        }

        public Type GetInstanceType(InjectContext context)
        {
            if (!_concreteType.DerivesFromOrEqual(context.MemberType))
            {
                return null;
            }

            return GetTypeToCreate(context.MemberType);
        }

        public void GetAllInstancesWithInjectSplit(InjectContext context, out Action injectAction, List<object> buffer)
        {
            Assert.IsNotNull(context);

            var instanceType = GetTypeToCreate(context.MemberType);

            var instance = _container.InstantiateExplicit(instanceType, false, _extraArguments, context, _concreteIdentifier);

            injectAction = () =>
            {
                _container.InjectExplicit(
                    instance, instanceType, _extraArguments, context, _concreteIdentifier);
            };

            buffer.Add(instance);
        }

        Type GetTypeToCreate(Type contractType)
        {
            return ProviderUtil.GetTypeToInstantiate(contractType, _concreteType);
        }
    }
}