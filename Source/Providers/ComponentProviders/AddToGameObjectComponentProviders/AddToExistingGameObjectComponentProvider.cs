using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public class AddToExistingGameObjectComponentProvider : AddToGameObjectComponentProviderBase
    {
        readonly GameObject _gameObject;

        public AddToExistingGameObjectComponentProvider(
            GameObject gameObject,
            DiContainer container,
            Type componentType,
            object[] extraArguments)
            : base(container, componentType, extraArguments)
        {
            _gameObject = gameObject;
        }

        protected override GameObject GetGameObject()
        {
            return _gameObject;
        }
    }

    public class AddToExistingGameObjectComponentProviderGetter : AddToGameObjectComponentProviderBase
    {
        readonly Func<GameObject> _gameObjectGetter;

        public AddToExistingGameObjectComponentProviderGetter(
            Func<GameObject> gameObjectGetter,
            DiContainer container,
            Type componentType,
            object[] extraArguments)
            : base(container, componentType, extraArguments)
        {
            _gameObjectGetter = gameObjectGetter;
        }

        protected override GameObject GetGameObject()
        {
            var gameObj = _gameObjectGetter();
            Assert.IsNotNull(gameObj, "Provided Func<GameObject> returned null value for game object when using FromComponentOn");
            return gameObj;
        }
    }
}