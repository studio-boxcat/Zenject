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
            return FromResolveGetterBase(identifier, method, source);
        }

        public NonLazyBinder FromInstance(object instance)
        {
            return FromInstanceBase(instance);
        }
    }
}
