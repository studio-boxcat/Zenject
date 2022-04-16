using System;
using System.Collections.Generic;
using ModestTree;
using System.Linq;

#if !NOT_UNITY3D
using UnityEngine;
#endif

using Zenject.Internal;

namespace Zenject
{
    public abstract class FromBinder : ScopeConcreteIdArgCopyNonLazyBinder
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

        protected IEnumerable<Type> AllParentTypes
        {
            get { return BindInfo.ContractTypes.Concat(BindInfo.ToTypes); }
        }

        protected IEnumerable<Type> ConcreteTypes
        {
            get
            {
                if (BindInfo.ToChoice == ToChoices.Self)
                {
                    return BindInfo.ContractTypes;
                }

                Assert.IsNotEmpty(BindInfo.ToTypes);
                return BindInfo.ToTypes;
            }
        }

        // This is the default if nothing else is called
        public ScopeConcreteIdArgCopyNonLazyBinder FromNew()
        {
            BindingUtil.AssertTypesAreNotComponents(ConcreteTypes);
            BindingUtil.AssertTypesAreNotAbstract(ConcreteTypes);

            return this;
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResolve()
        {
            return FromResolve(null);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResolve(object subIdentifier)
        {
            return FromResolve(subIdentifier, InjectSources.Any);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResolve(object subIdentifier, InjectSources source)
        {
            return FromResolveInternal(subIdentifier, false, source);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResolveAll()
        {
            return FromResolveAll(null);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResolveAll(object subIdentifier)
        {
            return FromResolveAll(subIdentifier, InjectSources.Any);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResolveAll(object subIdentifier, InjectSources source)
        {
            return FromResolveInternal(subIdentifier, true, source);
        }

        ScopeConcreteIdArgCopyNonLazyBinder FromResolveInternal(object subIdentifier, bool matchAll, InjectSources source)
        {
            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new ResolveProvider(
                    type, container, subIdentifier, false, source, matchAll));

            return new ScopeConcreteIdArgCopyNonLazyBinder(BindInfo);
        }

        public SubContainerBinder FromSubContainerResolveAll()
        {
            return FromSubContainerResolveAll(null);
        }

        public SubContainerBinder FromSubContainerResolveAll(object subIdentifier)
        {
            return FromSubContainerResolveInternal(subIdentifier, true);
        }

        public SubContainerBinder FromSubContainerResolve()
        {
            return FromSubContainerResolve(null);
        }

        public SubContainerBinder FromSubContainerResolve(object subIdentifier)
        {
            return FromSubContainerResolveInternal(subIdentifier, false);
        }

        SubContainerBinder FromSubContainerResolveInternal(
            object subIdentifier, bool resolveAll)
        {
            // It's unlikely they will want to create the whole subcontainer with each binding
            // (aka transient) which is the default so require that they specify it
            BindInfo.RequireExplicitScope = true;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            return new SubContainerBinder(
                BindInfo, BindStatement, subIdentifier, resolveAll);
        }

#if !NOT_UNITY3D

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentsOn(GameObject gameObject)
        {
            BindingUtil.AssertIsValidGameObject(gameObject);
            BindingUtil.AssertIsComponent(ConcreteTypes);
            BindingUtil.AssertTypesAreNotAbstract(ConcreteTypes);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectComponentProvider(
                    type, gameObject, false));

            return new ScopeConcreteIdArgCopyNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentOn(GameObject gameObject)
        {
            BindingUtil.AssertIsValidGameObject(gameObject);
            BindingUtil.AssertIsComponent(ConcreteTypes);
            BindingUtil.AssertTypesAreNotAbstract(ConcreteTypes);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectComponentProvider(
                    type, gameObject, true));

            return new ScopeConcreteIdArgCopyNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentsOn(Func<InjectContext, GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteTypes);
            BindingUtil.AssertTypesAreNotAbstract(ConcreteTypes);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectGetterComponentProvider(
                    type, gameObjectGetter, false));

            return new ScopeConcreteIdArgCopyNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentOn(Func<InjectContext, GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteTypes);
            BindingUtil.AssertTypesAreNotAbstract(ConcreteTypes);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetFromGameObjectGetterComponentProvider(
                    type, gameObjectGetter, true));

            return new ScopeConcreteIdArgCopyNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentsOnRoot()
        {
            return FromComponentsOn(
                ctx => ctx.Container.Resolve<Context>().gameObject);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentOnRoot()
        {
            return FromComponentOn(
                ctx => ctx.Container.Resolve<Context>().gameObject);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromNewComponentOn(GameObject gameObject)
        {
            BindingUtil.AssertIsValidGameObject(gameObject);
            BindingUtil.AssertIsComponent(ConcreteTypes);
            BindingUtil.AssertTypesAreNotAbstract(ConcreteTypes);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new AddToExistingGameObjectComponentProvider(
                    gameObject, container, type, BindInfo.Arguments, BindInfo.ConcreteIdentifier, BindInfo.InstantiatedCallback));

            return new ScopeConcreteIdArgCopyNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromNewComponentOn(Func<InjectContext, GameObject> gameObjectGetter)
        {
            BindingUtil.AssertIsComponent(ConcreteTypes);
            BindingUtil.AssertTypesAreNotAbstract(ConcreteTypes);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new AddToExistingGameObjectComponentProviderGetter(
                    gameObjectGetter, container, type, BindInfo.Arguments, BindInfo.ConcreteIdentifier, BindInfo.InstantiatedCallback));

            return new ScopeConcreteIdArgCopyNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromNewComponentSibling()
        {
            BindingUtil.AssertIsComponent(ConcreteTypes);
            BindingUtil.AssertTypesAreNotAbstract(ConcreteTypes);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new SingleProviderBindingFinalizer(
                BindInfo, (container, type) => new AddToCurrentGameObjectComponentProvider(
                    container, type, BindInfo.Arguments, BindInfo.ConcreteIdentifier, BindInfo.InstantiatedCallback));

            return new ScopeConcreteIdArgCopyNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromNewComponentOnRoot()
        {
            return FromNewComponentOn(
                ctx => ctx.Container.Resolve<Context>().gameObject);
        }

        public NameTransformScopeConcreteIdArgCopyNonLazyBinder FromNewComponentOnNewGameObject()
        {
            return FromNewComponentOnNewGameObject(new GameObjectCreationParameters());
        }

        internal NameTransformScopeConcreteIdArgCopyNonLazyBinder FromNewComponentOnNewGameObject(
            GameObjectCreationParameters gameObjectInfo)
        {
            BindingUtil.AssertIsComponent(ConcreteTypes);
            BindingUtil.AssertTypesAreNotAbstract(ConcreteTypes);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new AddToNewGameObjectComponentProvider(
                    container,
                    type,
                    BindInfo.Arguments,
                    gameObjectInfo, BindInfo.ConcreteIdentifier, BindInfo.InstantiatedCallback));

            return new NameTransformScopeConcreteIdArgCopyNonLazyBinder(BindInfo, gameObjectInfo);
        }

        public NameTransformScopeConcreteIdArgCopyNonLazyBinder FromNewComponentOnNewPrefabResource(string resourcePath)
        {
            return FromNewComponentOnNewPrefabResource(resourcePath, new GameObjectCreationParameters());
        }

        internal NameTransformScopeConcreteIdArgCopyNonLazyBinder FromNewComponentOnNewPrefabResource(
            string resourcePath, GameObjectCreationParameters gameObjectInfo)
        {
            BindingUtil.AssertIsValidResourcePath(resourcePath);
            BindingUtil.AssertIsComponent(ConcreteTypes);
            BindingUtil.AssertTypesAreNotAbstract(ConcreteTypes);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new PrefabResourceBindingFinalizer(
                BindInfo, gameObjectInfo, resourcePath,
                (contractType, instantiator) => new InstantiateOnPrefabComponentProvider(contractType, instantiator));

            return new NameTransformScopeConcreteIdArgCopyNonLazyBinder(BindInfo, gameObjectInfo);
        }

        public NameTransformScopeConcreteIdArgCopyNonLazyBinder FromNewComponentOnNewPrefab(UnityEngine.Object prefab)
        {
            return FromNewComponentOnNewPrefab(prefab, new GameObjectCreationParameters());
        }

        internal NameTransformScopeConcreteIdArgCopyNonLazyBinder FromNewComponentOnNewPrefab(
            UnityEngine.Object prefab, GameObjectCreationParameters gameObjectInfo)
        {
            BindingUtil.AssertIsValidPrefab(prefab);
            BindingUtil.AssertIsComponent(ConcreteTypes);
            BindingUtil.AssertTypesAreNotAbstract(ConcreteTypes);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new PrefabBindingFinalizer(
                BindInfo, gameObjectInfo, prefab,
                (contractType, instantiator) =>
                    new InstantiateOnPrefabComponentProvider(contractType, instantiator));

            return new NameTransformScopeConcreteIdArgCopyNonLazyBinder(BindInfo, gameObjectInfo);
        }

        public NameTransformScopeConcreteIdArgCopyNonLazyBinder FromComponentInNewPrefab(UnityEngine.Object prefab)
        {
            return FromComponentInNewPrefab(
                prefab, new GameObjectCreationParameters());
        }

        internal NameTransformScopeConcreteIdArgCopyNonLazyBinder FromComponentInNewPrefab(
            UnityEngine.Object prefab, GameObjectCreationParameters gameObjectInfo)
        {
            BindingUtil.AssertIsValidPrefab(prefab);
            BindingUtil.AssertIsInterfaceOrComponent(AllParentTypes);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new PrefabBindingFinalizer(
                BindInfo, gameObjectInfo, prefab,
                (contractType, instantiator) => new GetFromPrefabComponentProvider(contractType, instantiator, true));

            return new NameTransformScopeConcreteIdArgCopyNonLazyBinder(BindInfo, gameObjectInfo);
        }

        public NameTransformScopeConcreteIdArgCopyNonLazyBinder FromComponentsInNewPrefab(UnityEngine.Object prefab)
        {
            return FromComponentsInNewPrefab(
                prefab, new GameObjectCreationParameters());
        }

        internal NameTransformScopeConcreteIdArgCopyNonLazyBinder FromComponentsInNewPrefab(
            UnityEngine.Object prefab, GameObjectCreationParameters gameObjectInfo)
        {
            BindingUtil.AssertIsValidPrefab(prefab);
            BindingUtil.AssertIsInterfaceOrComponent(AllParentTypes);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new PrefabBindingFinalizer(
                BindInfo, gameObjectInfo, prefab,
                (contractType, instantiator) => new GetFromPrefabComponentProvider(contractType, instantiator, false));

            return new NameTransformScopeConcreteIdArgCopyNonLazyBinder(BindInfo, gameObjectInfo);
        }

        public NameTransformScopeConcreteIdArgCopyNonLazyBinder FromComponentInNewPrefabResource(string resourcePath)
        {
            return FromComponentInNewPrefabResource(resourcePath, new GameObjectCreationParameters());
        }

        internal NameTransformScopeConcreteIdArgCopyNonLazyBinder FromComponentInNewPrefabResource(
            string resourcePath, GameObjectCreationParameters gameObjectInfo)
        {
            BindingUtil.AssertIsValidResourcePath(resourcePath);
            BindingUtil.AssertIsInterfaceOrComponent(AllParentTypes);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new PrefabResourceBindingFinalizer(
                BindInfo, gameObjectInfo, resourcePath,
                (contractType, instantiator) => new GetFromPrefabComponentProvider(contractType, instantiator, true));

            return new NameTransformScopeConcreteIdArgCopyNonLazyBinder(BindInfo, gameObjectInfo);
        }

        public NameTransformScopeConcreteIdArgCopyNonLazyBinder FromComponentsInNewPrefabResource(string resourcePath)
        {
            return FromComponentsInNewPrefabResource(resourcePath, new GameObjectCreationParameters());
        }

        internal NameTransformScopeConcreteIdArgCopyNonLazyBinder FromComponentsInNewPrefabResource(
            string resourcePath, GameObjectCreationParameters gameObjectInfo)
        {
            BindingUtil.AssertIsValidResourcePath(resourcePath);
            BindingUtil.AssertIsInterfaceOrComponent(AllParentTypes);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new PrefabResourceBindingFinalizer(
                BindInfo, gameObjectInfo, resourcePath,
                (contractType, instantiator) => new GetFromPrefabComponentProvider(contractType, instantiator, false));

            return new NameTransformScopeConcreteIdArgCopyNonLazyBinder(BindInfo, gameObjectInfo);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResource(string resourcePath)
        {
            BindingUtil.AssertDerivesFromUnityObject(ConcreteTypes);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (_, type) => new ResourceProvider(resourcePath, type, true));

            return new ScopeConcreteIdArgCopyNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromResources(string resourcePath)
        {
            BindingUtil.AssertDerivesFromUnityObject(ConcreteTypes);

            BindInfo.RequireExplicitScope = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (_, type) => new ResourceProvider(resourcePath, type, false));

            return new ScopeConcreteIdArgCopyNonLazyBinder(BindInfo);
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentInChildren(
            bool includeInactive = true)
        {
            BindingUtil.AssertIsInterfaceOrComponent(AllParentTypes);

            BindInfo.RequireExplicitScope = false;

            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, concreteType) => new MethodMultipleProviderUntyped(ctx =>
                    {
                        Assert.That(ctx.ObjectType.DerivesFromOrEqual<MonoBehaviour>(),
                            "Cannot use FromComponentInChildren to inject data into non monobehaviours!");

                        Assert.IsNotNull(ctx.ObjectInstance);

                        var monoBehaviour = (MonoBehaviour)ctx.ObjectInstance;

                        var match = monoBehaviour.GetComponentInChildren(concreteType, includeInactive);

                        if (match == null)
                        {
                            Assert.That(ctx.Optional,
                                "Could not find any component with type '{0}' through FromComponentInChildren binding", concreteType);
                            return Enumerable.Empty<object>();
                        }

                        return new object[] { match };
                    },
                    container));

            return this;
        }

        protected ScopeConcreteIdArgCopyNonLazyBinder FromComponentsInChildrenBase(
            bool excludeSelf, Func<Component, bool> predicate, bool includeInactive)
        {
            BindingUtil.AssertIsInterfaceOrComponent(AllParentTypes);

            BindInfo.RequireExplicitScope = false;

            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, concreteType) => new MethodMultipleProviderUntyped(ctx =>
                    {
                        Assert.That(ctx.ObjectType.DerivesFromOrEqual<MonoBehaviour>(),
                            "Cannot use FromComponentsInChildren to inject data into non monobehaviours!");

                        Assert.IsNotNull(ctx.ObjectInstance);

                        var monoBehaviour = (MonoBehaviour)ctx.ObjectInstance;

                        var res = monoBehaviour.GetComponentsInChildren(concreteType, includeInactive)
                            .Where(x => !ReferenceEquals(x, ctx.ObjectInstance));

                        if (excludeSelf)
                        {
                            res = res.Where(x => x.gameObject != monoBehaviour.gameObject);
                        }

                        if (predicate != null)
                        {
                            res = res.Where(predicate);
                        }

                        return res.Cast<object>();
                    },
                    container));

            return this;
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentInParents(
            bool excludeSelf = false, bool includeInactive = true)
        {
            BindingUtil.AssertIsInterfaceOrComponent(AllParentTypes);

            BindInfo.RequireExplicitScope = false;

            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, concreteType) => new MethodMultipleProviderUntyped(ctx =>
                    {
                        Assert.That(ctx.ObjectType.DerivesFromOrEqual<MonoBehaviour>(),
                            "Cannot use FromComponentSibling to inject data into non monobehaviours!");

                        Assert.IsNotNull(ctx.ObjectInstance);

                        var monoBehaviour = (MonoBehaviour)ctx.ObjectInstance;

                        var matches = monoBehaviour.GetComponentsInParent(concreteType, includeInactive)
                            .Where(x => !ReferenceEquals(x, ctx.ObjectInstance));

                        if (excludeSelf)
                        {
                            matches = matches.Where(x => x.gameObject != monoBehaviour.gameObject);
                        }

                        var match = matches.FirstOrDefault();

                        if (match == null)
                        {
                            Assert.That(ctx.Optional,
                                "Could not find any component with type '{0}' through FromComponentInParents binding", concreteType);

                            return Enumerable.Empty<object>();
                        }

                        return new object[] { match };
                    },
                    container));

            return this;
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentsInParents(
            bool excludeSelf = false, bool includeInactive = true)
        {
            BindingUtil.AssertIsInterfaceOrComponent(AllParentTypes);

            BindInfo.RequireExplicitScope = false;

            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, concreteType) => new MethodMultipleProviderUntyped(ctx =>
                    {
                        Assert.That(ctx.ObjectType.DerivesFromOrEqual<MonoBehaviour>(),
                            "Cannot use FromComponentSibling to inject data into non monobehaviours!");

                        Assert.IsNotNull(ctx.ObjectInstance);

                        var monoBehaviour = (MonoBehaviour)ctx.ObjectInstance;

                        var res = monoBehaviour.GetComponentsInParent(concreteType, includeInactive)
                            .Where(x => !ReferenceEquals(x, ctx.ObjectInstance));

                        if (excludeSelf)
                        {
                            res = res.Where(x => x.gameObject != monoBehaviour.gameObject);
                        }

                        return res.Cast<object>();
                    },
                    container));

            return this;
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentSibling()
        {
            BindingUtil.AssertIsInterfaceOrComponent(AllParentTypes);

            BindInfo.RequireExplicitScope = false;

            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, concreteType) => new MethodMultipleProviderUntyped(ctx =>
                    {
                        Assert.That(ctx.ObjectType.DerivesFromOrEqual<MonoBehaviour>(),
                            "Cannot use FromComponentSibling to inject data into non monobehaviours!");

                        Assert.IsNotNull(ctx.ObjectInstance);

                        var monoBehaviour = (MonoBehaviour)ctx.ObjectInstance;

                        var match = monoBehaviour.GetComponent(concreteType);

                        if (match == null)
                        {
                            Assert.That(ctx.Optional,
                                "Could not find any component with type '{0}' through FromComponentSibling binding", concreteType);
                            return Enumerable.Empty<object>();
                        }

                        return new object[] { match };
                    },
                    container));

            return this;
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentsSibling()
        {
            BindingUtil.AssertIsInterfaceOrComponent(AllParentTypes);

            BindInfo.RequireExplicitScope = false;

            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, concreteType) => new MethodMultipleProviderUntyped(ctx =>
                    {
                        Assert.That(ctx.ObjectType.DerivesFromOrEqual<MonoBehaviour>(),
                            "Cannot use FromComponentSibling to inject data into non monobehaviours!");

                        Assert.IsNotNull(ctx.ObjectInstance);

                        var monoBehaviour = (MonoBehaviour)ctx.ObjectInstance;

                        return monoBehaviour.GetComponents(concreteType)
                            .Where(x => !ReferenceEquals(x, monoBehaviour)).Cast<object>();
                    },
                    container));

            return this;
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromComponentInHierarchy(
            bool includeInactive = true)
        {
            BindingUtil.AssertIsInterfaceOrComponent(AllParentTypes);

            // Since this is a pretty heavy operation, let's require an explicit scope
            // Most of the time they should use AsCached or AsSingle
            BindInfo.RequireExplicitScope = true;

            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, concreteType) => new MethodMultipleProviderUntyped(ctx =>
                    {
                        var match = container.Resolve<Context>().GetRootGameObjects()
                            .Select(x => x.GetComponentInChildren(concreteType, includeInactive))
                            .Where(x => x != null && !ReferenceEquals(x, ctx.ObjectInstance)).FirstOrDefault();

                        if (match == null)
                        {
                            Assert.That(ctx.Optional,
                                "Could not find any component with type '{0}' through FromComponentInHierarchy binding", concreteType);
                            return Enumerable.Empty<object>();
                        }

                        return new object[] { match };
                    },
                    container));

            return this;
        }

        protected ScopeConcreteIdArgCopyNonLazyBinder FromComponentsInHierarchyBase(
            Func<Component, bool> predicate = null, bool includeInactive = true)
        {
            BindingUtil.AssertIsInterfaceOrComponent(AllParentTypes);

            BindInfo.RequireExplicitScope = true;

            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;

            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, concreteType) => new MethodMultipleProviderUntyped(ctx =>
                    {
                        var res = container.Resolve<Context>().GetRootGameObjects()
                            .SelectMany(x => x.GetComponentsInChildren(concreteType, includeInactive))
                            .Where(x => !ReferenceEquals(x, ctx.ObjectInstance));

                        if (predicate != null)
                        {
                            res = res.Where(predicate);
                        }

                        return res.Cast<object>();
                    },
                    container));

            return this;
        }
#endif

        public ScopeConcreteIdArgCopyNonLazyBinder FromMethodUntyped(Func<InjectContext, object> method)
        {
            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new MethodProviderUntyped(method, container));

            return this;
        }

        public ScopeConcreteIdArgCopyNonLazyBinder FromMethodMultipleUntyped(Func<InjectContext, IEnumerable<object>> method)
        {
            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new MethodMultipleProviderUntyped(method, container));

            return this;
        }

        protected ScopeConcreteIdArgCopyNonLazyBinder FromMethodBase<TConcrete>(Func<InjectContext, TConcrete> method)
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

        protected ScopeConcreteIdArgCopyNonLazyBinder FromMethodMultipleBase<TConcrete>(Func<InjectContext, IEnumerable<TConcrete>> method)
        {
            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new MethodProviderMultiple<TConcrete>(method, container));

            return this;
        }

        protected ScopeConcreteIdArgCopyNonLazyBinder FromResolveGetterBase<TObj, TResult>(
            object identifier, Func<TObj, TResult> method, InjectSources source, bool matchMultiple)
        {
            BindingUtil.AssertIsDerivedFromTypes(typeof(TResult), AllParentTypes);

            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new GetterProvider<TObj, TResult>(identifier, method, container, source, matchMultiple));

            return new ScopeConcreteIdArgCopyNonLazyBinder(BindInfo);
        }

        protected ScopeConcreteIdArgCopyNonLazyBinder FromInstanceBase(object instance)
        {
            BindingUtil.AssertInstanceDerivesFromOrEqual(instance, AllParentTypes);

            BindInfo.RequireExplicitScope = false;
            // Don't know how it's created so can't assume here that it violates AsSingle
            BindInfo.MarkAsCreationBinding = false;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo,
                (container, type) => new InstanceProvider(type, instance, container));

            return new ScopeConcreteIdArgCopyNonLazyBinder(BindInfo);
        }
    }
}
