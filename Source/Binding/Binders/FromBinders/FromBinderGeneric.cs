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

        public NonLazyBinder FromMethod(Func<TContract> method)
        {
            return FromMethodBase(ctx => method());
        }

        public NonLazyBinder FromMethod(Func<InjectableInfo, TContract> method)
        {
            return FromMethodBase(method);
        }

        public NonLazyBinder FromResolveGetter<TObj>(Func<TObj, TContract> method)
        {
            return FromResolveGetter(null, method);
        }

        public NonLazyBinder FromResolveGetter<TObj>(object identifier, Func<TObj, TContract> method)
        {
            return FromResolveGetter(identifier, method, InjectSources.Any);
        }

        public NonLazyBinder FromResolveGetter<TObj>(object identifier, Func<TObj, TContract> method, InjectSources source)
        {
            return FromResolveGetterBase(identifier, method, source, false);
        }

        public NonLazyBinder FromResolveAllGetter<TObj>(Func<TObj, TContract> method)
        {
            return FromResolveAllGetter(null, method);
        }

        public NonLazyBinder FromResolveAllGetter<TObj>(object identifier, Func<TObj, TContract> method)
        {
            return FromResolveAllGetter(identifier, method, InjectSources.Any);
        }

        public NonLazyBinder FromResolveAllGetter<TObj>(object identifier, Func<TObj, TContract> method, InjectSources source)
        {
            return FromResolveGetterBase(identifier, method, source, true);
        }

        public NonLazyBinder FromInstance(TContract instance)
        {
            return FromInstanceBase(instance);
        }
    }
}
