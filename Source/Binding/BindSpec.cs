using System;
using UnityEngine.Assertions;

namespace Zenject
{
    [Flags]
    public enum BindFlag : byte
    {
        Primary = 1 << 0,
        Interfaces = 1 << 1,
        PrimaryAndInterfaces = Primary | Interfaces,
    }

    public struct BindSpec
    {
        public Type PrimaryType;
        public int Identifier;
        public BindFlag BindFlag;


        public TypeArray BakeContractTypes()
        {
            Assert.IsTrue(BindFlag != default);
            if (BindFlag == BindFlag.Primary)
                return new TypeArray(PrimaryType);
            if (BindFlag == BindFlag.Interfaces)
                return new TypeArray(PrimaryType.GetInterfaces());
            Assert.IsTrue(BindFlag == BindFlag.PrimaryAndInterfaces);
            return new TypeArray(PrimaryType, PrimaryType.GetInterfaces());
        }
    }
}