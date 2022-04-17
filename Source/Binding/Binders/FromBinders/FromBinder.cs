using System;
using System.Collections.Generic;
using ModestTree;
using System.Linq;

#if !NOT_UNITY3D
using UnityEngine;
#endif

namespace Zenject
{
    public abstract class FromBinder : ScopeArgNonLazyBinder
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
        public ScopeArgNonLazyBinder FromNew()
        {
            BindingUtil.AssertIsNotComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            return this;
        }

        public ScopeArgNonLazyBinder FromResolve()
        {
            return FromResolve(null);
        }

        public ScopeArgNonLazyBinder FromResolve(object subIdentifier)
        {
            return FromResolve(subIdentifier, InjectSources.Any);
        }

        public ScopeArgNonLazyBinder FromResolve(object subIdentifier, InjectSources source)
        {
            return FromResolveInternal(subIdentifier, false, source);
        }

        public ScopeArgNonLazyBinder FromResolveAll()
        {
            return FromResolveAll(null);
        }

        public ScopeArgNonLazyBinder FromResolveAll(object subIdentifier)
        {
            return FromResolveAll(subIdentifier, InjectSources.Any);
        }

        public ScopeArgNonLazyBinder FromResolveAll(object subIdentifier, InjectSources source)
        {
            return FromResolveInternal(subIdentifier, true, source);
        }

        ScopeArgNonLazyBinder FromResolveInternal(object subIdentifier, bool matchAll, InjectSources source)
        {
            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new ResolveProvider(
                    type, container, subIdentifier, false, source, matchAll));

            return new ScopeArgNonLazyBinder(BindInfo);
        }

#if !NOT_UNITY3D

        public ScopeArgNonLazyBinder FromComponentsOn(GameObject gameObject)
        {
            BindingUtil.AssertIsValidGameObject(gameObject);
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectComponentProvider(
                    type, gameObject, false));

            return new ScopeArgNonLazyBinder(BindInfo);
        }

        public ScopeArgNonLazyBinder FromComponentOn(GameObject gameObject)
        {
            BindingUtil.AssertIsValidGameObject(gameObject);
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectComponentProvider(
                    type, gameObject, true));

            return new ScopeArgNonLazyBinder(BindInfo);
        }

        public ScopeArgNonLazyBinder FromComponentsOn(Func<DiContainer, GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectGetterComponentProvider(
                    container, type, gameObjectGetter, false));

            return new ScopeArgNonLazyBinder(BindInfo);
        }

        public ScopeArgNonLazyBinder FromComponentOn(Func<DiContainer, GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectGetterComponentProvider(
                    container, type, gameObjectGetter, true));

            return new ScopeArgNonLazyBinder(BindInfo);
        }

        public ScopeArgNonLazyBinder FromComponentsOnRoot()
        {
            return FromComponentsOn(
                container => container.Resolve<Context>().gameObject);
        }

        public ScopeArgNonLazyBinder FromComponentOnRoot()
        {
            return FromComponentOn(
                container => container.Resolve<Context>().gameObject);
        }

        public ScopeArgNonLazyBinder FromNewComponentOn(GameObject gameObject)
        {
            BindingUtil.AssertIsValidGameObject(gameObject);
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new AddToExistingGameObjectComponentProvider(
                    gameObject, container, type, BindInfo.Arguments));

            return new ScopeArgNonLazyBinder(BindInfo);
        }

        public ScopeArgNonLazyBinder FromNewComponentOn(Func<InjectableInfo, GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new AddToExistingGameObjectComponentProviderGetter(
                    gameObjectGetter, container, type, BindInfo.Arguments));

            return new ScopeArgNonLazyBinder(BindInfo);
        }

        public ScopeArgNonLazyBinder FromResource(string resourcePath)
        {
            BindingUtil.AssertDerivesFromUnityObject(ConcreteType);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (_, type) => new ResourceProvider(resourcePath, type, true));

            return new ScopeArgNonLazyBinder(BindInfo);
        }

        public ScopeArgNonLazyBinder FromResources(string resourcePath)
        {
            BindingUtil.AssertDerivesFromUnityObject(ConcreteType);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (_, type) => new ResourceProvider(resourcePath, type, false));

            return new ScopeArgNonLazyBinder(BindInfo);
        }

#endif

        public ScopeArgNonLazyBinder FromMethodUntyped(Func<InjectableInfo, object> method)
        {
            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new MethodProviderUntyped(method, container));

            return this;
        }

        protected ScopeArgNonLazyBinder FromMethodBase<TConcrete>(Func<InjectableInfo, TConcrete> method)
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

        protected ScopeArgNonLazyBinder FromResolveGetterBase<TObj, TResult>(
            object identifier, Func<TObj, TResult> method, InjectSources source, bool matchMultiple)
        {
            BindingUtil.AssertIsDerivedFromTypes(typeof(TResult), AllParentTypes);

            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetterProvider<TObj, TResult>(identifier, method, container, source, matchMultiple));

            return new ScopeArgNonLazyBinder(BindInfo);
        }

        protected ScopeArgNonLazyBinder FromInstanceBase(object instance)
        {
            BindingUtil.AssertInstanceDerivesFromOrEqual(instance, AllParentTypes);

            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new InstanceProvider(type, instance));

            return new ScopeArgNonLazyBinder(BindInfo);
        }
    }
}
