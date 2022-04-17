#if !NOT_UNITY3D

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using ModestTree;
using UnityEngine;

namespace Zenject
{
    public abstract class AddToGameObjectComponentProviderBase : IProvider
    {
        readonly Type _componentType;
        readonly DiContainer _container;
        [CanBeNull] readonly object[] _extraArguments;

        public AddToGameObjectComponentProviderBase(DiContainer container, Type componentType,
            [CanBeNull] object[] extraArguments)
        {
            Assert.That(componentType.DerivesFrom<Component>());

            _extraArguments = extraArguments;
            _componentType = componentType;
            _container = container;
        }

        public Type GetInstanceType(InjectableInfo context)
        {
            return _componentType;
        }

        public void GetAllInstancesWithInjectSplit(InjectableInfo context, out Action injectAction, List<object> buffer)
        {
            object instance;

            // We still want to make sure we can get the game object during validation
            var gameObj = GetGameObject(context);

            if (_componentType == typeof(Transform))
                // Treat transform as a special case because it's the one component that's always automatically added
                // Otherwise, calling AddComponent below will fail and return null
                // This is nice to allow doing things like
                //      Container.Bind<Transform>().FromNewComponentOnNewGameObject();
            {
                instance = gameObj.transform;
            }
            else
            {
                instance = gameObj.AddComponent(_componentType);
            }

            Assert.IsNotNull(instance);

            injectAction = () =>
            {
                _container.Inject(instance, _extraArguments);
            };

            buffer.Add(instance);
        }

        protected abstract GameObject GetGameObject(InjectableInfo context);
    }
}

#endif
