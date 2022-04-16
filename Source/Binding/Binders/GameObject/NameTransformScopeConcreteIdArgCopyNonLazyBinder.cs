#if !NOT_UNITY3D

namespace Zenject
{
    [NoReflectionBaking]
    public class NameTransformScopeConcreteIdArgCopyNonLazyBinder : TransformScopeConcreteIdArgCopyNonLazyBinder
    {
        public NameTransformScopeConcreteIdArgCopyNonLazyBinder(
            BindInfo bindInfo,
            GameObjectCreationParameters gameObjectInfo)
            : base(bindInfo, gameObjectInfo)
        {
        }

        public TransformScopeConcreteIdArgCopyNonLazyBinder WithGameObjectName(string gameObjectName)
        {
            GameObjectInfo.Name = gameObjectName;
            return this;
        }
    }
}

#endif
