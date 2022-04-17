namespace Zenject
{
    public class IdScopeArgNonLazyBinder : ScopeArgNonLazyBinder
    {
        public IdScopeArgNonLazyBinder(BindInfo bindInfo)
            : base(bindInfo)
        {
        }

        public ScopeArgNonLazyBinder WithId(object identifier)
        {
            BindInfo.Identifier = identifier;
            return this;
        }
    }
}
