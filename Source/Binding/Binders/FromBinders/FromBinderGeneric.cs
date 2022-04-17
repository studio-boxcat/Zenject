using System;

namespace Zenject
{
    public class FromBinderGeneric<TContract> : FromBinder
    {
        public FromBinderGeneric(
            DiContainer bindContainer,
            BindInfo bindInfo,
            BindStatement bindStatement)
            : base(bindContainer, bindInfo, bindStatement)
        {
            BindingUtil.AssertIsDerivedFromTypes(typeof(TContract), BindInfo.ContractTypes);
        }

        public ScopeArgNonLazyBinder FromMethod(Func<TContract> method)
        {
            return FromMethodBase(ctx => method());
        }

        public ScopeArgNonLazyBinder FromMethod(Func<InjectableInfo, TContract> method)
        {
            return FromMethodBase(method);
        }

        public ScopeArgNonLazyBinder FromResolveGetter<TObj>(Func<TObj, TContract> method)
        {
            return FromResolveGetter(null, method);
        }

        public ScopeArgNonLazyBinder FromResolveGetter<TObj>(object identifier, Func<TObj, TContract> method)
        {
            return FromResolveGetter(identifier, method, InjectSources.Any);
        }

        public ScopeArgNonLazyBinder FromResolveGetter<TObj>(object identifier, Func<TObj, TContract> method, InjectSources source)
        {
            return FromResolveGetterBase(identifier, method, source, false);
        }

        public ScopeArgNonLazyBinder FromResolveAllGetter<TObj>(Func<TObj, TContract> method)
        {
            return FromResolveAllGetter(null, method);
        }

        public ScopeArgNonLazyBinder FromResolveAllGetter<TObj>(object identifier, Func<TObj, TContract> method)
        {
            return FromResolveAllGetter(identifier, method, InjectSources.Any);
        }

        public ScopeArgNonLazyBinder FromResolveAllGetter<TObj>(object identifier, Func<TObj, TContract> method, InjectSources source)
        {
            return FromResolveGetterBase(identifier, method, source, true);
        }

        public ScopeArgNonLazyBinder FromInstance(TContract instance)
        {
            return FromInstanceBase(instance);
        }
    }
}
