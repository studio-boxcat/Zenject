using System;
using ModestTree;
using UnityEngine;

namespace Zenject
{
    public class AddToExistingGameObjectComponentProvider : AddToGameObjectComponentProviderBase
    {
        readonly GameObject _gameObject;

        public AddToExistingGameObjectComponentProvider(GameObject gameObject, DiContainer container, Type componentType,
            object[] extraArguments)
            : base(container, componentType, extraArguments)
        {
            _gameObject = gameObject;
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
            object[] extraArguments)
            : base(container, componentType, extraArguments)
        {
            _gameObjectGetter = gameObjectGetter;
        }

        protected override GameObject GetGameObject(InjectableInfo context)
        {
            var gameObj = _gameObjectGetter(context);
            Assert.IsNotNull(gameObj, "Provided Func<InjectableInfo, GameObject> returned null value for game object when using FromComponentOn");
            return gameObj;
        }
    }
}