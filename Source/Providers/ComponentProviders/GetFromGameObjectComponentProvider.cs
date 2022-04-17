using System;
using ModestTree;
using UnityEngine;

namespace Zenject
{
    public class GetFromGameObjectComponentProvider : IProvider
    {
        readonly GameObject _gameObject;
        readonly Type _componentType;

        // if concreteType is null we use the contract type from inject context
        public GetFromGameObjectComponentProvider(
            Type componentType, GameObject gameObject)
        {
            _componentType = componentType;
            _gameObject = gameObject;
        }

        public object GetInstance()
        {
            var match = _gameObject.GetComponent(_componentType);

            Assert.IsNotNull(match, "Could not find component with type '{0}' on prefab '{1}'",
                _componentType, _gameObject.name);

            return match;
        }
    }
}