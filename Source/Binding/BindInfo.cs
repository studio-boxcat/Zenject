using System;
using UnityEngine.Assertions;

namespace Zenject
{
    public struct BindInfo
    {
        public Type ConcreteType;
        public int Identifier;
        public bool BindConcreteType;
        public bool BindInterfaces;


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