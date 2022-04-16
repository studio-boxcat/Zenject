#if !NOT_UNITY3D

using UnityEngine;

namespace Zenject
{
    [NoReflectionBaking]
    public class TransformScopeConcreteIdArgCopyNonLazyBinder : ScopeConcreteIdArgCopyNonLazyBinder
    {
        public TransformScopeConcreteIdArgCopyNonLazyBinder(
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

        public ScopeConcreteIdArgCopyNonLazyBinder UnderTransform(Transform parent)
        {
            GameObjectInfo.ParentTransform = parent;
            return this;
        }
    }
}

#endif
