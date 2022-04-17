#if !NOT_UNITY3D

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zenject
{
    public class AddToNewGameObjectComponentProvider : AddToGameObjectComponentProviderBase
    {
        readonly GameObjectCreationParameters _gameObjectBindInfo;

        public AddToNewGameObjectComponentProvider(DiContainer container, Type componentType,
            object[] extraArguments, GameObjectCreationParameters gameObjectBindInfo,
            object concreteIdentifier)
            : base(container, componentType, extraArguments, concreteIdentifier)
        {
            _gameObjectBindInfo = gameObjectBindInfo;
        }

        protected override bool ShouldToggleActive
        {
            get { return true; }
        }

        protected override GameObject GetGameObject(InjectableInfo context)
        {
            return Container.CreateEmptyGameObject(_gameObjectBindInfo);
        }
    }
}

#endif
