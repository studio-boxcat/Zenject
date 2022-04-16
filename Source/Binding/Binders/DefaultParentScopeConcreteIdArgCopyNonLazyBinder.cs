namespace Zenject
{
    [NoReflectionBaking]
    public class DefaultParentScopeConcreteIdArgCopyNonLazyBinder : ScopeConcreteIdArgCopyNonLazyBinder
    {
        public DefaultParentScopeConcreteIdArgCopyNonLazyBinder(
            SubContainerCreatorBindInfo subContainerBindInfo, BindInfo bindInfo)
            : base(bindInfo)
        {
            SubContainerCreatorBindInfo = subContainerBindInfo;
        }

        protected SubContainerCreatorBindInfo SubContainerCreatorBindInfo
        {
            get; private set;
        }

        public ScopeConcreteIdArgCopyNonLazyBinder WithDefaultGameObjectParent(string defaultParentName)
        {
            SubContainerCreatorBindInfo.DefaultParentName = defaultParentName;
            return this;
        }
    }
}
