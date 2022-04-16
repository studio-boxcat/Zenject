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
        readonly Type _argumentTarget;

        public PrefabInstantiator(DiContainer container,
            GameObjectCreationParameters gameObjectBindInfo,
            Type argumentTarget,
            IEnumerable<TypeValuePair> extraArguments,
            IPrefabProvider prefabProvider)
        {
            _prefabProvider = prefabProvider;
            _extraArguments = extraArguments.ToList();
            _container = container;
            _gameObjectBindInfo = gameObjectBindInfo;
            _argumentTarget = argumentTarget;
        }

        public GameObjectCreationParameters GameObjectCreationParameters
        {
            get { return _gameObjectBindInfo; }
        }

        public Type ArgumentTarget
        {
            get { return _argumentTarget; }
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
            Assert.That(_argumentTarget == null || _argumentTarget.DerivesFromOrEqual(context.MemberType));

            bool shouldMakeActive;
            var gameObject = _container.CreateAndParentPrefab(
                GetPrefab(context), _gameObjectBindInfo, context, out shouldMakeActive);
            Assert.IsNotNull(gameObject);

            injectAction = () =>
            {
                var allArgs = ZenPools.SpawnList<TypeValuePair>();

                allArgs.AllocFreeAddRange(_extraArguments);
                allArgs.AllocFreeAddRange(args);

                if (_argumentTarget == null)
                {
                    Assert.That(
                        allArgs.IsEmpty(),
                        "Unexpected arguments provided to prefab instantiator.  Arguments are not allowed if binding multiple components in the same binding");
                }

                if (_argumentTarget == null || allArgs.IsEmpty())
                {
                    _container.InjectGameObject(gameObject);
                }
                else
                {
                    _container.InjectGameObjectForComponentExplicit(
                        gameObject, _argumentTarget, allArgs, context, null);

                    Assert.That(allArgs.Count == 0);
                }

                ZenPools.DespawnList(allArgs);

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
