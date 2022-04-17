#if !NOT_UNITY3D

using ModestTree;
using UnityEngine;
using System;

namespace Zenject
{
    public class PrefabProviderCustom : IPrefabProvider
    {
        readonly Func<InjectableInfo, UnityEngine.Object> _getter;

        public PrefabProviderCustom(Func<InjectableInfo, UnityEngine.Object> getter)
        {
            _getter = getter;
        }

        public UnityEngine.Object GetPrefab(InjectableInfo context)
        {
            var prefab = _getter(context);
            Assert.That(prefab != null, "Custom prefab provider returned null");
            return prefab;
        }
    }
}

#endif

