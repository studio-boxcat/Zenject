#if !NOT_UNITY3D

using System;
using System.Collections.Generic;
using System.Linq;
using Zenject.Internal;
using ModestTree;
using UnityEngine;

namespace Zenject
{
    [NoReflectionBaking]
    public class PrefabInstantiator : IPrefabInstantiator
    {
        readonly IPrefabProvider _prefabProvider;
        readonly DiContainer _container;
        readonly List<TypeValuePair> _extraArguments;
        readonly GameObjectCreationParameters _gameObjectBindInfo;

        public PrefabInstantiator(DiContainer container,
            GameObjectCreationParameters gameObjectBindInfo,
            IEnumerable<TypeValuePair> extraArguments,
            IPrefabProvider prefabProvider)
        {
            _prefabProvider = prefabProvider;
            _extraArguments = extraArguments.ToList();
            _container = container;
            _gameObjectBindInfo = gameObjectBindInfo;
        }

        public GameObjectCreationParameters GameObjectCreationParameters
        {
            get { return _gameObjectBindInfo; }
        }

        public List<TypeValuePair> ExtraArguments
        {
            get { return _extraArguments; }
        }

        public UnityEngine.Object GetPrefab(InjectContext context)
        {
            return _prefabProvider.GetPrefab(context);
        }

        public GameObject Instantiate(InjectContext context, List<TypeValuePair> args, out Action injectAction)
        {
            bool shouldMakeActive;
            var gameObject = _container.CreateAndParentPrefab(
                GetPrefab(context), _gameObjectBindInfo, context, out shouldMakeActive);
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
