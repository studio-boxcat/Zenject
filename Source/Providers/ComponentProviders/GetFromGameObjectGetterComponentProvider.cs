using System;
using ModestTree;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public class GetFromGameObjectGetterComponentProvider : IProvider
    {
        readonly DiContainer _container;
        readonly Func<DiContainer, GameObject> _gameObjectGetter;
        readonly Type _componentType;

        // if concreteType is null we use the contract type from inject context
        public GetFromGameObjectGetterComponentProvider(
            DiContainer container, Type componentType, Func<DiContainer, GameObject> gameObjectGetter)
        {
            _container = container;
            _componentType = componentType;
            _gameObjectGetter = gameObjectGetter;
        }

        public object GetInstance()
        {
            var gameObject = _gameObjectGetter(_container);
            var match = gameObject.GetComponent(_componentType);
            Assert.IsNotNull(match, "Could not find component with type '{0}' on game object '{1}'".Fmt(_componentType, gameObject.name));
            return match;
        }
    }
}