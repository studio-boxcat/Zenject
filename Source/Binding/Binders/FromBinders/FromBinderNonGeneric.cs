using System;
using System.Collections.Generic;

#if !NOT_UNITY3D
using UnityEngine;
#endif

namespace Zenject
{
    [NoReflectionBaking]
    public class FromBinderNonGeneric : FromBinder
    {
        public FromBinderNonGeneric(
            DiContainer bindContainer, BindInfo bindInfo,
            BindStatement bindStatement)
            : base(bindContainer, bindInfo, bindStatement)
        {
        }

        public ScopeConcreteIdArgNonLazyBinder FromMethod<TConcrete>(Func<InjectContext, TConcrete> method)
        {
            return FromMethodBase<TConcrete>(method);
        }

        public ScopeConcreteIdArgNonLazyBinder FromMethodMultiple<TConcrete>(Func<InjectContext, IEnumerable<TConcrete>> method)
        {
            BindingUtil.AssertIsDerivedFromTypes(typeof(TConcrete), AllParentTypes);
            return FromMethodMultipleBase<TConcrete>(method);
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

#if !NOT_UNITY3D

        public ScopeConcreteIdArgNonLazyBinder FromComponentsInChildren(
            Func<Component, bool> predicate, bool includeInactive = true)
        {
            return FromComponentsInChildren(false, predicate, includeInactive);
        }

        public ScopeConcreteIdArgNonLazyBinder FromComponentsInChildren(
            bool excludeSelf = false, Func<Component, bool> predicate = null, bool includeInactive = true)
        {
            return FromComponentsInChildrenBase(excludeSelf, predicate, includeInactive);
        }
#endif
    }
}
