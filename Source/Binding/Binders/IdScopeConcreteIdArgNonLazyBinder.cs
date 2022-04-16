namespace Zenject
{
    [NoReflectionBaking]
    public class IdScopeConcreteIdArgNonLazyBinder : ScopeConcreteIdArgNonLazyBinder
    {
        public IdScopeConcreteIdArgNonLazyBinder(BindInfo bindInfo)
            : base(bindInfo)
        {
        }

        public ScopeConcreteIdArgNonLazyBinder WithId(object identifier)
        {
            BindInfo.Identifier = identifier;
            return this;
        }
    }
}
