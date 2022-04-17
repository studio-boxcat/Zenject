#if !NOT_UNITY3D

using System;
using System.Collections.Generic;
using ModestTree;
using UnityEngine;

namespace Zenject
{
    public class AddToExistingGameObjectComponentProvider : AddToGameObjectComponentProviderBase
    {
        readonly GameObject _gameObject;

        public AddToExistingGameObjectComponentProvider(GameObject gameObject, DiContainer container, Type componentType,
            object[] extraArguments, object concreteIdentifier)
            : base(container, componentType, extraArguments, concreteIdentifier)
        {
            _gameObject = gameObject;
        }

        // This will cause [Inject] to be triggered after awake / start
        // We could return true, but what if toggling active has other negative repercussions?
        // For now let's just not do anything
        protected override bool ShouldToggleActive
        {
            get { return false; }
        }

        protected override GameObject GetGameObject(InjectableInfo context)
        {
            return _gameObject;
        }
    }

    public class AddToExistingGameObjectComponentProviderGetter : AddToGameObjectComponentProviderBase
    {
        readonly Func<InjectableInfo, GameObject> _gameObjectGetter;

        public AddToExistingGameObjectComponentProviderGetter(Func<InjectableInfo, GameObject> gameObjectGetter, DiContainer container, Type componentType,
            object[] extraArguments, object concreteIdentifier)
            : base(container, componentType, extraArguments, concreteIdentifier)
        {
            _gameObjectGetter = gameObjectGetter;
        }

        // This will cause [Inject] to be triggered after awake / start
        // We could return true, but what if toggling active has other negative repercussions?
        // For now let's just not do anything
        protected override bool ShouldToggleActive
        {
            get { return false; }
        }

        protected override GameObject GetGameObject(InjectableInfo context)
        {
            var gameObj = _gameObjectGetter(context);
            Assert.IsNotNull(gameObj, "Provided Func<InjectableInfo, GameObject> returned null value for game object when using FromComponentOn");
            return gameObj;
        }
    }
}

#endif
