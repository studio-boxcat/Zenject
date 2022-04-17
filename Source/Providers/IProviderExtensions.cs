using System;
using System.Collections.Generic;
using ModestTree;
using Zenject.Internal;

namespace Zenject
{
    public static class IProviderExtensions
    {
        public static void GetAllInstances(
            this IProvider creator, InjectableInfo context, List<object> buffer)
        {
            Assert.IsNotNull(context);

            creator.GetAllInstancesWithInjectSplit(context, out var injectAction, buffer);
            injectAction?.Invoke();
        }

        public static object TryGetInstance(
            this IProvider creator, InjectableInfo context)
        {
            var allInstances = ZenPools.SpawnList<object>();

            try
            {
                creator.GetAllInstances(context, allInstances);

                if (allInstances.Count == 0)
                {
                    return null;
                }

                Assert.That(allInstances.Count == 1,
                    "Provider returned multiple instances when one or zero was expected");

                return allInstances[0];
            }
            finally
            {
                ZenPools.DespawnList(allInstances);
            }
        }

        public static object GetInstance(
            this IProvider creator, InjectableInfo context)
        {
            var allInstances = ZenPools.SpawnList<object>();

            try
            {
                creator.GetAllInstances(context, allInstances);

                Assert.That(allInstances.Count > 0,
                    "Provider returned zero instances when one was expected when looking up type '{0}'", context.MemberType);

                Assert.That(allInstances.Count == 1,
                    "Provider returned multiple instances when only one was expected when looking up type '{0}'", context.MemberType);

                return allInstances[0];
            }
            finally
            {
                ZenPools.DespawnList(allInstances);
            }
        }
    }
}
