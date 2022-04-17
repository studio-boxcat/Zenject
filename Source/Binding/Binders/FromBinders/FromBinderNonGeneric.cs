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

        public NonLazyBinder FromMethod<TConcrete>(Func<InjectableInfo, TConcrete> method)
        {
            return FromMethodBase(method);
        }

        public NonLazyBinder FromResolveGetter<TObj, TContract>(Func<TObj, TContract> method)
        {
            return FromResolveGetter(null, method);
        }

        public NonLazyBinder FromResolveGetter<TObj, TContract>(object identifier, Func<TObj, TContract> method)
        {
            return FromResolveGetter(identifier, method, InjectSources.Any);
        }

        public NonLazyBinder FromResolveGetter<TObj, TContract>(object identifier, Func<TObj, TContract> method, InjectSources source)
        {
            return FromResolveGetterBase(identifier, method, source, false);
        }

        public NonLazyBinder FromResolveAllGetter<TObj, TContract>(Func<TObj, TContract> method)
        {
            return FromResolveAllGetter(null, method);
        }

        public NonLazyBinder FromResolveAllGetter<TObj, TContract>(object identifier, Func<TObj, TContract> method)
        {
            return FromResolveAllGetter(identifier, method, InjectSources.Any);
        }

        public NonLazyBinder FromResolveAllGetter<TObj, TContract>(object identifier, Func<TObj, TContract> method, InjectSources source)
        {
            return FromResolveGetterBase(identifier, method, source, true);
        }

        public NonLazyBinder FromInstance(object instance)
        {
            return FromInstanceBase(instance);
        }
    }
}
