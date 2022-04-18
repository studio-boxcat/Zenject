using System;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace Zenject
{
    public delegate IProvider ProviderFactory(DiContainer container, BindInfo bindInfo);

    public struct BindInfo
    {
        public Type ConcreteType;
        public int Identifier;
        public object[] Arguments;
        public bool BindConcreteType;
        public bool BindInterfaces;
        public bool NonLazy;

        [CanBeNull]
        public ProviderFactory ProviderFactory;
        [CanBeNull]
        public object Instance;

        // BindInfo.ProviderFactory = (container, type) => new TransientProvider(
        //     type, container, BindInfo.Arguments));

        public bool ContractTypeExists()
        {
            return (ConcreteType != null && BindConcreteType) || BindInterfaces;
        }

        public TypeArray BakeContractTypes()
        {
            Assert.IsTrue(BindConcreteType || BindInterfaces);
            if (!BindInterfaces)
                return new TypeArray(ConcreteType);
            if (!BindConcreteType)
                return new TypeArray(ConcreteType.GetInterfaces());
            return new TypeArray(ConcreteType, ConcreteType.GetInterfaces());
        }
    }
}