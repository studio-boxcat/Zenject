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
            return FromResolveInternal(subIdentifier, source);
        }

        NonLazyBinder FromResolveInternal(object subIdentifier, InjectSources source)
        {
            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new ResolveProvider(container, type, subIdentifier, source, false));

            return this;
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
                    type, gameObject));

            return this;
        }

        public NonLazyBinder FromComponentOn(Func<DiContainer, GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectGetterComponentProvider(
                    container, type, gameObjectGetter));

            return this;
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

            return this;
        }

        public NonLazyBinder FromNewComponentOn(Func<GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteType);
            BindingUtil.AssertIsNotAbstract(ConcreteType);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new AddToExistingGameObjectComponentProviderGetter(
                    gameObjectGetter, container, type, BindInfo.Arguments));

            return this;
        }

        protected NonLazyBinder FromMethodBase<TConcrete>(Func<TConcrete> method)
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
            object identifier, Func<TObj, TResult> method, InjectSources source)
        {
            BindingUtil.AssertIsDerivedFromTypes(typeof(TResult), AllParentTypes);

            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetterProvider<TObj, TResult>(identifier, method, container, source));

            return this;
        }

        protected NonLazyBinder FromInstanceBase(object instance)
        {
            BindingUtil.AssertInstanceDerivesFromOrEqual(instance, AllParentTypes);

            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new InstanceProvider(instance));

            return this;
        }
    }
}
