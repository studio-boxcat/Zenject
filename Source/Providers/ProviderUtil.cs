using System;
using ModestTree;

namespace Zenject
{
    public static class ProviderUtil
    {
        public static Type GetTypeToInstantiate(Type contractType, Type concreteType)
        {
            Assert.DerivesFromOrEqual(concreteType, contractType);
            return concreteType;
        }
    }
}

