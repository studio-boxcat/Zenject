#if !NOT_UNITY3D

using System;
using ModestTree;
using UnityEngine;

namespace Zenject
{
    public class PrefabInstantiator : IPrefabInstantiator
    {
        readonly IPrefabProvider _prefabProvider;
        readonly DiContainer _container;
        readonly GameObjectCreationParameters _gameObjectBindInfo;

        public PrefabInstantiator(DiContainer container,
            GameObjectCreationParameters gameObjectBindInfo,
            IPrefabProvider prefabProvider)
        {
            _prefabProvider = prefabProvider;
            _container = container;
            _gameObjectBindInfo = gameObjectBindInfo;
        }

        public UnityEngine.Object GetPrefab(InjectableInfo context)
        {
            return _prefabProvider.GetPrefab(context);
        }

        public GameObject Instantiate(InjectableInfo context, out Action injectAction)
        {
            bool shouldMakeActive;
            var gameObject = _container.CreateAndParentPrefab(
                GetPrefab(context), _gameObjectBindInfo, out shouldMakeActive);
            Assert.IsNotNull(gameObject);

            injectAction = () =>
            {
                _container.InjectGameObject(gameObject);

                if (shouldMakeActive)
                {
                    gameObject.SetActive(true);
                }
            };

            return gameObject;
        }
    }
}

#endif