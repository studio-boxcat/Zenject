using System;
using System.Collections.Generic;
using ModestTree;
using System.Linq;

#if !NOT_UNITY3D
using UnityEngine;
#endif

namespace Zenject
{
    [NoReflectionBaking]
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

        public ScopeConcreteIdArgCopyNonLazyBinder FromMethod(Func<TContract> method)
        {
            return FromMethodBase<TContract>(ctx => method());
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromMethod(Func<InjectContext, TContract> method)
        {
            return FromMethodBase<TContract>(method);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromMethodMultiple(Func<InjectContext, IEnumerable<TContract>> method)
        {
            BindingUtil.AssertIsDerivedFromTypes(typeof(TContract), AllParentTypes);
            return FromMethodMultipleBase<TContract>(method);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResolveGetter<TObj>(Func<TObj, TContract> method)
        {
            return FromResolveGetter<TObj>(null, method);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResolveGetter<TObj>(object identifier, Func<TObj, TContract> method)
        {
            return FromResolveGetter<TObj>(identifier, method, InjectSources.Any);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResolveGetter<TObj>(object identifier, Func<TObj, TContract> method, InjectSources source)
        {
            return FromResolveGetterBase<TObj, TContract>(identifier, method, source, false);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResolveAllGetter<TObj>(Func<TObj, TContract> method)
        {
            return FromResolveAllGetter<TObj>(null, method);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResolveAllGetter<TObj>(object identifier, Func<TObj, TContract> method)
        {
            return FromResolveAllGetter<TObj>(identifier, method, InjectSources.Any);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResolveAllGetter<TObj>(object identifier, Func<TObj, TContract> method, InjectSources source)
        {
            return FromResolveGetterBase<TObj, TContract>(identifier, method, source, true);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromInstance(TContract instance)
        {
            return FromInstanceBase(instance);
        }

#if !NOT_UNITY3D

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentsInChildren(
            Func<TContract, bool> predicate, bool includeInactive = true)
        {
            return FromComponentsInChildren(false, predicate, includeInactive);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentsInChildren(
            bool excludeSelf = false, Func<TContract, bool> predicate = null, bool includeInactive = true)
        {
            Func<Component, bool> subPredicate;

            if (predicate != null)
            {
                subPredicate = component => predicate((TContract)(object)component);
            }
            else
            {
                subPredicate = null;
            }

            return FromComponentsInChildrenBase(
                excludeSelf, subPredicate, includeInactive);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentsInHierarchy(
            Func<TContract, bool> predicate = null, bool includeInactive = true)
        {
            Func<Component, bool> subPredicate;

            if (predicate != null)
            {
                subPredicate = component => predicate((TContract)(object)component);
            }
            else
            {
                subPredicate = null;
            }

            return FromComponentsInHierarchyBase(subPredicate, includeInactive);
        }
#endif
    }
}
