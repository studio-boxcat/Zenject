using System;

namespace Zenject
{
    public class FromBinderNonGeneric : FromBinder
    {
        public FromBinderNonGeneric(
            DiContainer bindContainer, BindInfo bindInfo,
            BindStatement bindStatement)
            : base(bindContainer, bindInfo, bindStatement)
        {
        }

        public ScopeConcreteIdArgNonLazyBinder FromMethod<TConcrete>(Func<InjectableInfo, TConcrete> method)
        {
            return FromMethodBase<TConcrete>(method);
        }

        public ScopeConcreteIdArgNonLazyBinder FromResolveGetter<TObj, TContract>(Func<TObj, TContract> method)
        {
            return FromResolveGetter<TObj, TContract>(null, method);
        }

        public ScopeConcreteIdArgNonLazyBinder FromResolveGetter<TObj, TContract>(object identifier, Func<TObj, TContract> method)
        {
            return FromResolveGetter<TObj, TContract>(identifier, method, InjectSources.Any);
        }

        public ScopeConcreteIdArgNonLazyBinder FromResolveGetter<TObj, TContract>(object identifier, Func<TObj, TContract> method, InjectSources source)
        {
            return FromResolveGetterBase<TObj, TContract>(identifier, method, source, false);
        }

        public ScopeConcreteIdArgNonLazyBinder FromResolveAllGetter<TObj, TContract>(Func<TObj, TContract> method)
        {
            return FromResolveAllGetter<TObj, TContract>(null, method);
        }

        public ScopeConcreteIdArgNonLazyBinder FromResolveAllGetter<TObj, TContract>(object identifier, Func<TObj, TContract> method)
        {
            return FromResolveAllGetter<TObj, TContract>(identifier, method, InjectSources.Any);
        }

        public ScopeConcreteIdArgNonLazyBinder FromResolveAllGetter<TObj, TContract>(object identifier, Func<TObj, TContract> method, InjectSources source)
        {
            return FromResolveGetterBase<TObj, TContract>(identifier, method, source, true);
        }

        public ScopeConcreteIdArgNonLazyBinder FromInstance(object instance)
        {
            return FromInstanceBase(instance);
        }
    }
}
