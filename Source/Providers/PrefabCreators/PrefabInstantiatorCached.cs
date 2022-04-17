#if !NOT_UNITY3D

using System;
using UnityEngine;

namespace Zenject
{
    public class PrefabInstantiatorCached : IPrefabInstantiator
    {
        readonly IPrefabInstantiator _subInstantiator;

        GameObject _gameObject;

        public PrefabInstantiatorCached(IPrefabInstantiator subInstantiator)
        {
            _subInstantiator = subInstantiator;
        }

        public UnityEngine.Object GetPrefab(InjectContext context)
        {
            return _subInstantiator.GetPrefab(context);
        }

        public GameObject Instantiate(InjectContext context, out Action injectAction)
        {
            if (_gameObject != null)
            {
                injectAction = null;
                return _gameObject;
            }

            _gameObject = _subInstantiator.Instantiate(context, out injectAction);
            return _gameObject;
        }
    }
}

#endif
