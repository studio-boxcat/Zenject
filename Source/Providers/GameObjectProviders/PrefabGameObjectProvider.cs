#if !NOT_UNITY3D

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenject
{
    public class PrefabGameObjectProvider : IProvider
    {
        readonly IPrefabInstantiator _prefabCreator;

        public PrefabGameObjectProvider(
            IPrefabInstantiator prefabCreator)
        {
            _prefabCreator = prefabCreator;
        }

        public Type GetInstanceType(InjectContext context)
        {
            return typeof(GameObject);
        }

        public void GetAllInstancesWithInjectSplit(InjectContext context, out Action injectAction, List<object> buffer)
        {
            var instance = _prefabCreator.Instantiate(context, out injectAction);

            buffer.Add(instance);
        }
    }
}

#endif
