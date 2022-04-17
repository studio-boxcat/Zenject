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

        public NonLazyBinder FromMethod<TConcrete>(Func<TConcrete> method)
        {
            return FromMethodBase(method);
        }

        public NonLazyBinder FromResolveGetter<TObj, TContract>(Func<TObj, TContract> method, object identifier = null, InjectSources source = InjectSources.Any)
        {
            return FromResolveGetterBase(identifier, method, source);
        }

        public NonLazyBinder FromInstance(object instance)
        {
            return FromInstanceBase(instance);
        }
    }
}