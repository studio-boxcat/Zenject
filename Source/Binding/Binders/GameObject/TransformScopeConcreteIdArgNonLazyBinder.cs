#if !NOT_UNITY3D

using UnityEngine;

namespace Zenject
{
    [NoReflectionBaking]
    public class TransformScopeConcreteIdArgNonLazyBinder : ScopeConcreteIdArgNonLazyBinder
    {
        public TransformScopeConcreteIdArgNonLazyBinder(
            BindInfo bindInfo,
            GameObjectCreationParameters gameObjectInfo)
            : base(bindInfo)
        {
            GameObjectInfo = gameObjectInfo;
        }

        protected GameObjectCreationParameters GameObjectInfo
        {
            get;
            private set;
        }

        public ScopeConcreteIdArgNonLazyBinder UnderTransform(Transform parent)
        {
            GameObjectInfo.ParentTransform = parent;
            return this;
        }
    }
}

#endif
