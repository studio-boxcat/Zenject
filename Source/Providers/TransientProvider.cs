using System;
using JetBrains.Annotations;
using ModestTree;

namespace Zenject
{
    public class TransientProvider : IProvider
    {
        readonly Type _concreteType;
        readonly DiContainer _container;
        [CanBeNull] readonly object[] _extraArguments;

        public TransientProvider(
            Type concreteType,
            DiContainer container,
            [CanBeNull] object[] extraArguments)
        {
            Assert.That(!concreteType.IsAbstract,
                "Expected non-abstract type for given binding but instead found type '{0}'",
                concreteType);

            _concreteType = concreteType;
            _container = container;
            _extraArguments = extraArguments;
        }

        public object GetInstance()
        {
            return _container.Instantiate(_concreteType, _extraArguments);
        }
    }
}