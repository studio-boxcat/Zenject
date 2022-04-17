namespace Zenject
{
    public class NonLazyBinder
    {
        public readonly BindInfo BindInfo;

        public NonLazyBinder(BindInfo bindInfo)
        {
            BindInfo = bindInfo;
        }

        public NonLazyBinder WithArguments(params object[] args)
        {
            BindInfo.Arguments = args;
            return this;
        }

        public NonLazyBinder WithId(object identifier)
        {
            BindInfo.Identifier = identifier;
            return this;
        }

        public NonLazyBinder AsCached()
        {
            BindInfo.Scope = ScopeTypes.Singleton;
            return this;
        }

        public NonLazyBinder AsSingle()
        {
            BindInfo.Scope = ScopeTypes.Singleton;
            BindInfo.MarkAsUniqueSingleton = true;
            return this;
        }

        // Note that this is the default so it's not necessary to call this
        public NonLazyBinder AsTransient()
        {
            BindInfo.Scope = ScopeTypes.Transient;
            return this;
        }

        public NonLazyBinder NonLazy()
        {
            BindInfo.NonLazy = true;
            return this;
        }

        public NonLazyBinder Lazy()
        {
            BindInfo.NonLazy = false;
            return this;
        }
    }
}