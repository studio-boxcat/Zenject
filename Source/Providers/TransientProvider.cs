using System;
using JetBrains.Annotations;
using ModestTree;

namespace Zenject
{
    public class TransientProvider : IProvider
    {
        readonly DiContainer _container;
        readonly Type _concreteType;
        [CanBeNull] readonly object[] _extraArguments;

        public TransientProvider(Type concreteType, DiContainer container,
            [CanBeNull] object[] extraArguments)
        {
            Assert.That(!concreteType.IsAbstract,
                "Expected non-abstract type for given binding but instead found type '{0}'",
                concreteType);

            _container = container;
            _concreteType = concreteType;
            _extraArguments = extraArguments;
        }

        public object GetInstanceWithInjectSplit(InjectableInfo context, out Action injectAction)
        {
            var instanceType = GetTypeToCreate(context.MemberType);

            var instance = _container.InstantiateExplicit(instanceType, false, _extraArguments);

            injectAction = () => _container.Inject(instance, _extraArguments);

            return instance;
        }

        Type GetTypeToCreate(Type contractType)
        {
            return ProviderUtil.GetTypeToInstantiate(contractType, _concreteType);
        }
    }
}