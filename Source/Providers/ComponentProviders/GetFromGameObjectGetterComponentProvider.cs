#if !NOT_UNITY3D

using System;
using System.Collections.Generic;
using ModestTree;
using UnityEngine;

namespace Zenject
{
    public class GetFromGameObjectGetterComponentProvider : IProvider
    {
        readonly DiContainer _container;
        readonly Func<DiContainer, GameObject> _gameObjectGetter;
        readonly Type _componentType;
        readonly bool _matchSingle;

        // if concreteType is null we use the contract type from inject context
        public GetFromGameObjectGetterComponentProvider(
            DiContainer container, Type componentType, Func<DiContainer, GameObject> gameObjectGetter, bool matchSingle)
        {
            _container = container;
            _componentType = componentType;
            _matchSingle = matchSingle;
            _gameObjectGetter = gameObjectGetter;
        }

        public void GetAllInstancesWithInjectSplit(InjectableInfo context, out Action injectAction, List<object> buffer)
        {
            injectAction = null;

            var gameObject = _gameObjectGetter(_container);

            if (_matchSingle)
            {
                var match = gameObject.GetComponent(_componentType);

                Assert.IsNotNull(match, "Could not find component with type '{0}' on game object '{1}'",
                _componentType, gameObject.name);

                buffer.Add(match);
                return;
            }

            var allComponents = gameObject.GetComponents(_componentType);

            Assert.That(allComponents.Length >= 1,
            "Expected to find at least one component with type '{0}' on prefab '{1}'",
            _componentType, gameObject.name);

            buffer.AllocFreeAddRange(allComponents);
        }
    }
}

#endif


