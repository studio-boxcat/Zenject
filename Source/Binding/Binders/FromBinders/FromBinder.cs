using System;
using System.Collections.Generic;
using ModestTree;
using System.Linq;

#if !NOT_UNITY3D
using UnityEngine;
#endif

namespace Zenject
{
    public abstract class FromBinder : NonLazyBinder
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
        public NonLazyBinder FromNew()
        {
            BindingUtil.AssertIsNotComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            return this;
        }

        public NonLazyBinder FromResolve(object subIdentifier = null, InjectSources source = InjectSources.Any)
        {
            return FromResolveInternal(subIdentifier, false, source);
        }

        public NonLazyBinder FromResolveAll(object subIdentifier = null, InjectSources source = InjectSources.Any)
        {
            return FromResolveInternal(subIdentifier, true, source);
        }

        NonLazyBinder FromResolveInternal(object subIdentifier, bool matchAll, InjectSources source)
        {
            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new ResolveProvider(
                    type, container, subIdentifier, false, source, matchAll));

            return new NonLazyBinder(BindInfo);
        }

#if !NOT_UNITY3D

        public NonLazyBinder FromComponentsOn(GameObject gameObject)
        {
            BindingUtil.AssertIsValidGameObject(gameObject);
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectComponentProvider(
                    type, gameObject, false));

            return new NonLazyBinder(BindInfo);
        }

        public NonLazyBinder FromComponentOn(GameObject gameObject)
        {
            BindingUtil.AssertIsValidGameObject(gameObject);
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectComponentProvider(
                    type, gameObject, true));

            return new NonLazyBinder(BindInfo);
        }

        public NonLazyBinder FromComponentsOn(Func<DiContainer, GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectGetterComponentProvider(
                    container, type, gameObjectGetter, false));

            return new NonLazyBinder(BindInfo);
        }

        public NonLazyBinder FromComponentOn(Func<DiContainer, GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectGetterComponentProvider(
                    container, type, gameObjectGetter, true));

            return new NonLazyBinder(BindInfo);
        }

        public NonLazyBinder FromComponentsOnRoot()
        {
            return FromComponentsOn(
                container => container.Resolve<Context>().gameObject);
        }

        public NonLazyBinder FromComponentOnRoot()
        {
            return FromComponentOn(
                container => container.Resolve<Context>().gameObject);
        }

        public NonLazyBinder FromNewComponentOn(GameObject gameObject)
        {
            BindingUtil.AssertIsValidGameObject(gameObject);
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new AddToExistingGameObjectComponentProvider(
                    gameObject, container, type, BindInfo.Arguments));

            return new NonLazyBinder(BindInfo);
        }

        public NonLazyBinder FromNewComponentOn(Func<InjectableInfo, GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new AddToExistingGameObjectComponentProviderGetter(
                    gameObjectGetter, container, type, BindInfo.Arguments));

            return new NonLazyBinder(BindInfo);
        }

#endif

        public NonLazyBinder FromMethodUntyped(Func<InjectableInfo, object> method)
        {
            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (_, _) => new MethodProviderUntyped(method));

            return this;
        }

        protected NonLazyBinder FromMethodBase<TConcrete>(Func<InjectableInfo, TConcrete> method)
        {
            BindingUtil.AssertIsDerivedFromTypes(typeof(TConcrete), AllParentTypes);

            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (_, _) => new MethodProvider<TConcrete>(method));

            return this;
        }

        protected NonLazyBinder FromResolveGetterBase<TObj, TResult>(
            object identifier, Func<TObj, TResult> method, InjectSources source, bool matchMultiple)
        {
            BindingUtil.AssertIsDerivedFromTypes(typeof(TResult), AllParentTypes);

            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetterProvider<TObj, TResult>(identifier, method, container, source, matchMultiple));

            return new NonLazyBinder(BindInfo);
        }

        protected NonLazyBinder FromInstanceBase(object instance)
        {
            BindingUtil.AssertInstanceDerivesFromOrEqual(instance, AllParentTypes);

            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new InstanceProvider(type, instance));

            return new NonLazyBinder(BindInfo);
        }
    }
}
