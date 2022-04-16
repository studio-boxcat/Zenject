namespace Zenject
{
    [NoReflectionBaking]
    public class IdScopeConcreteIdArgCopyNonLazyBinder : ScopeConcreteIdArgCopyNonLazyBinder
    {
        public IdScopeConcreteIdArgCopyNonLazyBinder(BindInfo bindInfo)
            : base(bindInfo)
        {
        }

        public ScopeConcreteIdArgCopyNonLazyBinder WithId(object identifier)
        {
            BindInfo.Identifier = identifier;
            return this;
        }
    }
}
