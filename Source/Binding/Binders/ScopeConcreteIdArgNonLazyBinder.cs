namespace Zenject
{
    [NoReflectionBaking]
    public class ScopeConcreteIdArgNonLazyBinder : ConcreteIdArgNonLazyBinder
    {
        public ScopeConcreteIdArgNonLazyBinder(BindInfo bindInfo)
            : base(bindInfo)
        {
        }

        public ConcreteIdArgNonLazyBinder AsCached()
        {
            BindInfo.Scope = ScopeTypes.Singleton;
            return this;
        }

        public ConcreteIdArgNonLazyBinder AsSingle()
        {
            BindInfo.Scope = ScopeTypes.Singleton;
            BindInfo.MarkAsUniqueSingleton = true;
            return this;
        }

        // Note that this is the default so it's not necessary to call this
        public ConcreteIdArgNonLazyBinder AsTransient()
        {
            BindInfo.Scope = ScopeTypes.Transient;
            return this;
        }
    }
}
