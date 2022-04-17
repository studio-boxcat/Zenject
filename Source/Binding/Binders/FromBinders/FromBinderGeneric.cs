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

        public NonLazyBinder FromResolveGetter<TObj>(object identifier, Func<TObj, TContract> method, InjectSources source = InjectSources.Any)
        {
            return FromResolveGetterBase(identifier, method, source);
        }

        public NonLazyBinder FromInstance(TContract instance)
        {
            return FromInstanceBase(instance);
        }
    }
}
