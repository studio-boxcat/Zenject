namespace Zenject
{
    [NoReflectionBaking]
    public class DefaultParentScopeConcreteIdArgNonLazyBinder : ScopeConcreteIdArgNonLazyBinder
    {
        public DefaultParentScopeConcreteIdArgNonLazyBinder(
            SubContainerCreatorBindInfo subContainerBindInfo, BindInfo bindInfo)
            : base(bindInfo)
        {
            SubContainerCreatorBindInfo = subContainerBindInfo;
        }

        protected SubContainerCreatorBindInfo SubContainerCreatorBindInfo
        {
            get; private set;
        }

        public ScopeConcreteIdArgNonLazyBinder WithDefaultGameObjectParent(string defaultParentName)
        {
            SubContainerCreatorBindInfo.DefaultParentName = defaultParentName;
            return this;
        }
    }
}
