using System;
using System.Collections.Generic;
using ModestTree;
using System.Linq;

#if !NOT_UNITY3D
using UnityEngine;
#endif

namespace Zenject
{
    public abstract class FromBinder : ScopeConcreteIdArgNonLazyBinder
    {
        public FromBinder(
            DiContainer bindContainer, BindInfo bindInfo,
            BindStatement bindStatement)
            : base(bindInfo)
        {
            BindStatement = bindStatement;
            BindContainer = bindContainer;
        }

        protected DiContainer BindContainer
        {
            get; private set;
        }

        protected BindStatement BindStatement
        {
            get;
            private set;
        }

        protected IBindingFinalizer SubFinalizer
        {
            set { BindStatement.SetFinalizer(value); }
        }

        protected IEnumerable<Type> AllParentTypes => BindInfo.ToType != null
            ? BindInfo.ContractTypes.Append(BindInfo.ToType) : BindInfo.ContractTypes;

        protected Type ConcreteType
        {
            get
            {
                if (BindInfo.ToChoice == ToChoices.Self)
                {
                    return BindInfo.ContractTypes.Single();
                }

                Assert.IsNotNull(BindInfo.ToType);
                return BindInfo.ToType;
            }
        }

        // This is the default if nothing else is called
        public ScopeConcreteIdArgNonLazyBinder FromNew()
        {
            BindingUtil.AssertIsNotComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            return this;
        }

        public ScopeConcreteIdArgNonLazyBinder FromResolve()
        {
            return FromResolve(null);
        }

        public ScopeConcreteIdArgNonLazyBinder FromResolve(object subIdentifier)
        {
            return FromResolve(subIdentifier, InjectSources.Any);
        }

        public ScopeConcreteIdArgNonLazyBinder FromResolve(object subIdentifier, InjectSources source)
        {
            return FromResolveInternal(subIdentifier, false, source);
        }

        public ScopeConcreteIdArgNonLazyBinder FromResolveAll()
        {
            return FromResolveAll(null);
        }

        public ScopeConcreteIdArgNonLazyBinder FromResolveAll(object subIdentifier)
        {
            return FromResolveAll(subIdentifier, InjectSources.Any);
        }

        public ScopeConcreteIdArgNonLazyBinder FromResolveAll(object subIdentifier, InjectSources source)
        {
            return FromResolveInternal(subIdentifier, true, source);
        }

        ScopeConcreteIdArgNonLazyBinder FromResolveInternal(object subIdentifier, bool matchAll, InjectSources source)
        {
            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new ResolveProvider(
                    type, container, subIdentifier, false, source, matchAll));

            return new ScopeConcreteIdArgNonLazyBinder(BindInfo);
        }

#if !NOT_UNITY3D

        public ScopeConcreteIdArgNonLazyBinder FromComponentsOn(GameObject gameObject)
        {
            BindingUtil.AssertIsValidGameObject(gameObject);
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectComponentProvider(
                    type, gameObject, false));

            return new ScopeConcreteIdArgNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgNonLazyBinder FromComponentOn(GameObject gameObject)
        {
            BindingUtil.AssertIsValidGameObject(gameObject);
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectComponentProvider(
                    type, gameObject, true));

            return new ScopeConcreteIdArgNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgNonLazyBinder FromComponentsOn(Func<DiContainer, GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectGetterComponentProvider(
                    container, type, gameObjectGetter, false));

            return new ScopeConcreteIdArgNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgNonLazyBinder FromComponentOn(Func<DiContainer, GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectGetterComponentProvider(
                    container, type, gameObjectGetter, true));

            return new ScopeConcreteIdArgNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgNonLazyBinder FromComponentsOnRoot()
        {
            return FromComponentsOn(
                container => container.Resolve<Context>().gameObject);
        }

        public ScopeConcreteIdArgNonLazyBinder FromComponentOnRoot()
        {
            return FromComponentOn(
                container => container.Resolve<Context>().gameObject);
        }

        public ScopeConcreteIdArgNonLazyBinder FromNewComponentOn(GameObject gameObject)
        {
            BindingUtil.AssertIsValidGameObject(gameObject);
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new AddToExistingGameObjectComponentProvider(
                    gameObject, container, type, BindInfo.Arguments, BindInfo.ConcreteIdentifier));

            return new ScopeConcreteIdArgNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgNonLazyBinder FromNewComponentOn(Func<InjectableInfo, GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new AddToExistingGameObjectComponentProviderGetter(
                    gameObjectGetter, container, type, BindInfo.Arguments, BindInfo.ConcreteIdentifier));

            return new ScopeConcreteIdArgNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgNonLazyBinder FromResource(string resourcePath)
        {
            BindingUtil.AssertDerivesFromUnityObject(ConcreteType);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (_, type) => new ResourceProvider(resourcePath, type, true));

            return new ScopeConcreteIdArgNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgNonLazyBinder FromResources(string resourcePath)
        {
            BindingUtil.AssertDerivesFromUnityObject(ConcreteType);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (_, type) => new ResourceProvider(resourcePath, type, false));

            return new ScopeConcreteIdArgNonLazyBinder(BindInfo);
        }

#endif

        public ScopeConcreteIdArgNonLazyBinder FromMethodUntyped(Func<InjectableInfo, object> method)
        {
            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new MethodProviderUntyped(method, container));

            return this;
        }

        protected ScopeConcreteIdArgNonLazyBinder FromMethodBase<TConcrete>(Func<InjectableInfo, TConcrete> method)
        {
            BindingUtil.AssertIsDerivedFromTypes(typeof(TConcrete), AllParentTypes);

            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new MethodProvider<TConcrete>(method, container));

            return this;
        }

        protected ScopeConcreteIdArgNonLazyBinder FromResolveGetterBase<TObj, TResult>(
            object identifier, Func<TObj, TResult> method, InjectSources source, bool matchMultiple)
        {
            BindingUtil.AssertIsDerivedFromTypes(typeof(TResult), AllParentTypes);

            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetterProvider<TObj, TResult>(identifier, method, container, source, matchMultiple));

            return new ScopeConcreteIdArgNonLazyBinder(BindInfo);
        }

        protected ScopeConcreteIdArgNonLazyBinder FromInstanceBase(object instance)
        {
            BindingUtil.AssertInstanceDerivesFromOrEqual(instance, AllParentTypes);

            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new InstanceProvider(type, instance));

            return new ScopeConcreteIdArgNonLazyBinder(BindInfo);
        }
    }
}
