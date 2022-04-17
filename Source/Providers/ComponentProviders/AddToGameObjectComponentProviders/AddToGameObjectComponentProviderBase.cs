#if !NOT_UNITY3D

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using ModestTree;
using UnityEngine;
using Zenject.Internal;

namespace Zenject
{
    public abstract class AddToGameObjectComponentProviderBase : IProvider
    {
        readonly Type _componentType;
        readonly DiContainer _container;
        [CanBeNull] readonly object[] _extraArguments;
        readonly object _concreteIdentifier;

        public AddToGameObjectComponentProviderBase(DiContainer container, Type componentType,
            [CanBeNull] object[] extraArguments, object concreteIdentifier)
        {
            Assert.That(componentType.DerivesFrom<Component>());

            _extraArguments = extraArguments;
            _componentType = componentType;
            _container = container;
            _concreteIdentifier = concreteIdentifier;
        }

        protected DiContainer Container
        {
            get { return _container; }
        }

        protected Type ComponentType
        {
            get { return _componentType; }
        }

        protected abstract bool ShouldToggleActive
        {
            get;
        }

        public Type GetInstanceType(InjectContext context)
        {
            return _componentType;
        }

        public void GetAllInstancesWithInjectSplit(InjectContext context, out Action injectAction, List<object> buffer)
        {
            Assert.IsNotNull(context);

            object instance;

            // We still want to make sure we can get the game object during validation
            var gameObj = GetGameObject(context);

            var wasActive = gameObj.activeSelf;

            if (wasActive && ShouldToggleActive)
            {
                // We need to do this in some cases to ensure that [Inject] always gets
                // called before awake / start
                gameObj.SetActive(false);
            }

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
                try
                {
                    _container.InjectExplicit(instance, _componentType, _extraArguments, context, _concreteIdentifier);
                }
                finally
                {
                    if (wasActive && ShouldToggleActive)
                    {
                        gameObj.SetActive(true);
                    }
                }
            };

            buffer.Add(instance);
        }

        protected abstract GameObject GetGameObject(InjectContext context);
    }
}

#endif
