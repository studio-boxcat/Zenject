using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using ModestTree;
using Zenject.Internal;
#if !NOT_UNITY3D
using UnityEngine;
#endif

namespace Zenject
{
    // Responsibilities:
    // - Expose methods to configure object graph via BindX() methods
    // - Look up bound values via Resolve() method
    // - Instantiate new values via InstantiateX() methods
    [NoReflectionBaking]
    public class DiContainer
    {
        readonly Dictionary<BindingId, List<ProviderInfo>> _providers = new Dictionary<BindingId, List<ProviderInfo>>();

        readonly DiContainer[][] _containerLookups = new DiContainer[3][];

        readonly HashSet<LookupId> _resolvesInProgress = new HashSet<LookupId>();
        readonly HashSet<LookupId> _resolvesTwiceInProgress = new HashSet<LookupId>();

        readonly LazyInstanceInjector _lazyInjector;

        readonly SingletonMarkRegistry _singletonMarkRegistry = new SingletonMarkRegistry();
        readonly Queue<BindStatement> _currentBindings = new Queue<BindStatement>();

#if !NOT_UNITY3D
        Transform _contextTransform;
        bool _hasLookedUpContextTransform;
#endif

        bool _hasResolvedRoots;
        bool _isFinalizingBinding;
        bool _isInstalling;
#if DEBUG || UNITY_EDITOR
        bool _hasDisplayedInstallWarning;
#endif

        public DiContainer(
            [CanBeNull] DiContainer parentContainer = null)
        {
            _lazyInjector = new LazyInstanceInjector(this);

            InstallDefaultBindings();
            FlushBindings();
            Assert.That(_currentBindings.Count == 0);

            _containerLookups[(int)InjectSources.Local] = new[] { this };
            _containerLookups[(int)InjectSources.Parent] = parentContainer != null
                ? new[] { parentContainer } : Array.Empty<DiContainer>();

            var containerChain = new List<DiContainer> {this};
            var current = this;
            while (current.ParentContainer != null)
            {
                containerChain.Add(current.ParentContainer);
                current = current.ParentContainer;
            }
            _containerLookups[(int)InjectSources.Any] = containerChain.ToArray();

            if (parentContainer != null)
            {
                parentContainer.FlushBindings();

                Assert.That(_currentBindings.Count == 0);
            }
        }

        internal SingletonMarkRegistry SingletonMarkRegistry
        {
            get { return _singletonMarkRegistry; }
        }

        void InstallDefaultBindings()
        {
            Bind(typeof(DiContainer)).FromInstance(this);
            Bind(typeof(LazyInject<>)).FromMethodUntyped(CreateLazyBinding).Lazy();
        }

        object CreateLazyBinding(InjectContext context)
        {
            // By cloning it this also means that Ids, optional, etc. are forwarded properly
            var newContext = context.Clone();
            newContext.MemberType = context.MemberType.GetGenericArguments().Single();

            var result = Activator.CreateInstance(
                typeof(LazyInject<>)
                .MakeGenericType(newContext.MemberType), this, newContext);

            return result;
        }

#if !NOT_UNITY3D
        // This might be null in some rare cases like when used in ZenjectUnitTestFixture
        Transform ContextTransform
        {
            get
            {
                if (!_hasLookedUpContextTransform)
                {
                    _hasLookedUpContextTransform = true;

                    var context = TryResolve<Context>();

                    if (context != null)
                    {
                        _contextTransform = context.transform;
                    }
                }

                return _contextTransform;
            }
        }
#endif

        [CanBeNull]
        public DiContainer ParentContainer
        {
            get { return _containerLookups[(int)InjectSources.Parent].FirstOrDefault(); }
        }

        // When this is true, it will log warnings when Resolve or Instantiate
        // methods are called
        // Used to ensure that Resolve and Instantiate methods are not called
        // during bind phase.  This is important since Resolve and Instantiate
        // make use of the bindings, so if the bindings are not complete then
        // unexpected behaviour can occur
        public bool IsInstalling
        {
            get { return _isInstalling; }
            set { _isInstalling = value; }
        }

        public void ResolveRoots()
        {
            Assert.That(!_hasResolvedRoots);

            FlushBindings();

            ResolveDependencyRoots();

            _lazyInjector.LazyInjectAll();

            Assert.That(!_hasResolvedRoots);
            _hasResolvedRoots = true;
        }

        void ResolveDependencyRoots()
        {
            var rootBindings = new List<(BindingId, ProviderInfo)>();

            foreach (var bindingPair in _providers)
            foreach (var provider in bindingPair.Value)
            {
                if (provider.NonLazy == false) continue;

                // Save them to a list instead of resolving for them here to account
                // for the rare case where one of the resolves does another binding
                // and therefore changes _providers, causing an exception.
                rootBindings.Add((bindingPair.Key, provider));
            }

            var instances = ZenPools.SpawnList<object>();

            try
            {
                foreach (var (bindId, providerInfo) in rootBindings)
                {
                    using var context = ZenPools.SpawnInjectContext(this, bindId.Type);
                    context.Identifier = bindId.Identifier;
                    context.SourceType = InjectSources.Local;

                    // Should this be true?  Are there cases where you are ok that NonLazy matches
                    // zero providers?
                    // Probably better to be false to catch mistakes
                    context.Optional = false;

                    instances.Clear();

                    SafeGetInstances(providerInfo, context, instances);

                    // Zero matches might actually be valid in some cases
                    //Assert.That(matches.Any());
                }
            }
            finally
            {
                ZenPools.DespawnList(instances);
            }
        }

        public void QueueForInject(object[] instances)
        {
            _lazyInjector.AddInstances(instances);
        }

        public void RegisterProvider(
            BindingId bindingId, IProvider provider, bool nonLazy)
        {
            var info = new ProviderInfo(provider, nonLazy, this);

            List<ProviderInfo> providerInfos;

            if (!_providers.TryGetValue(bindingId, out providerInfos))
            {
                providerInfos = new List<ProviderInfo>();
                _providers.Add(bindingId, providerInfos);
            }

            providerInfos.Add(info);
        }

        void GetProviderMatches(
            InjectContext context, List<ProviderInfo> buffer)
        {
            Assert.IsNotNull(context);
            Assert.That(buffer.Count == 0);

            var allMatches = ZenPools.SpawnList<ProviderInfo>();

            try
            {
                GetProvidersForContract(
                    context.BindingId, context.SourceType, allMatches);

                for (int i = 0; i < allMatches.Count; i++)
                {
                    buffer.Add(allMatches[i]);
                }
            }
            finally
            {
                ZenPools.DespawnList(allMatches);
            }
        }

        ProviderInfo? TryGetUniqueProvider(InjectContext context)
        {
            Assert.IsNotNull(context);

            var bindingId = context.BindingId;
            var sourceType = context.SourceType;

            var containerLookups = _containerLookups[(int)sourceType];

            for (int i = 0; i < containerLookups.Length; i++)
            {
                containerLookups[i].FlushBindings();
            }

            var localProviders = ZenPools.SpawnList<ProviderInfo>();

            try
            {
                ProviderInfo? selected = null;
                int selectedDistance = int.MaxValue;
                bool ambiguousSelection = false;

                for (int i = 0; i < containerLookups.Length; i++)
                {
                    var container = containerLookups[i];

                    int curDistance = GetContainerHeirarchyDistance(container);

                    if (curDistance > selectedDistance)
                    {
                        // If matching provider was already found lower in the hierarchy => don't search for a new one,
                        // because there can't be a better or equal provider in this container.
                        continue;
                    }

                    localProviders.Clear();
                    container.GetLocalProviders(bindingId, localProviders);

                    for (int k = 0; k < localProviders.Count; k++)
                    {
                        var provider = localProviders[k];

                        // The distance can't decrease becuase we are iterating over the containers with increasing distance.
                        // The distance can't increase because  we skip the container if the distance is greater than selected.
                        // So the distances are equal and only the condition can help resolving the amiguity.
                        Assert.That(selected == null || selectedDistance == curDistance);

                        if (selected != null)
                        {
                            // Both providers don't have a condition and are on equal depth.
                            ambiguousSelection = true;
                        }

                        if (ambiguousSelection)
                        {
                            continue;
                        }

                        selectedDistance = curDistance;
                        selected = provider;
                    }
                }

                if (ambiguousSelection)
                {
                    throw Assert.CreateException(
                        "Found multiple matches when only one was expected for type '{0}'{1}. Object graph:\n {2}",
                        context.MemberType,
                        (context.ObjectType == null
                            ? ""
                            : " while building object with type '{0}'".Fmt(context.ObjectType)),
                        context.GetObjectGraphString());
                }

                return selected;
            }
            finally
            {
                ZenPools.DespawnList(localProviders);
            }
        }

        void GetLocalProviders(BindingId bindingId, List<ProviderInfo> buffer)
        {
            List<ProviderInfo> localProviders;

            if (_providers.TryGetValue(bindingId, out localProviders))
            {
                buffer.AllocFreeAddRange(localProviders);
                return;
            }

            // If we are asking for a List<int>, we should also match for any localProviders that are bound to the open generic type List<>
            // Currently it only matches one and not the other - not totally sure if this is better than returning both
            if (bindingId.Type.IsGenericType && _providers.TryGetValue(new BindingId(bindingId.Type.GetGenericTypeDefinition(), bindingId.Identifier), out localProviders))
            {
                buffer.AllocFreeAddRange(localProviders);
            }

            // None found
        }

        void GetProvidersForContract(
            BindingId bindingId, InjectSources sourceType, List<ProviderInfo> buffer)
        {
            var containerLookups = _containerLookups[(int)sourceType];

            for (int i = 0; i < containerLookups.Length; i++)
            {
                containerLookups[i].FlushBindings();
            }

            for (int i = 0; i < containerLookups.Length; i++)
            {
                containerLookups[i].GetLocalProviders(bindingId, buffer);
            }
        }

        public IList ResolveAll(InjectContext context)
        {
            var buffer = ZenPools.SpawnList<object>();

            try
            {
                ResolveAll(context, buffer);
                return ReflectionUtil.CreateGenericList(context.MemberType, buffer);
            }
            finally
            {
                ZenPools.DespawnList(buffer);
            }
        }

        public void ResolveAll(InjectContext context, List<object> buffer)
        {
            Assert.IsNotNull(context);
            // Note that different types can map to the same provider (eg. a base type to a concrete class and a concrete class to itself)

            FlushBindings();
            CheckForInstallWarning(context);

            var matches = ZenPools.SpawnList<ProviderInfo>();

            try
            {
                GetProviderMatches(context, matches);

                if (matches.Count == 0)
                {
                    if (!context.Optional)
                    {
                        throw Assert.CreateException(
                            "Could not find required dependency with type '{0}' Object graph:\n {1}", context.MemberType, context.GetObjectGraphString());
                    }

                    return;
                }

                var instances = ZenPools.SpawnList<object>();
                var allInstances = ZenPools.SpawnList<object>();

                try
                {
                    for (int i = 0; i < matches.Count; i++)
                    {
                        var match = matches[i];

                        instances.Clear();
                        SafeGetInstances(match, context, instances);

                        for (int k = 0; k < instances.Count; k++)
                        {
                            allInstances.Add(instances[k]);
                        }
                    }

                    if (allInstances.Count == 0 && !context.Optional)
                    {
                        throw Assert.CreateException(
                            "Could not find required dependency with type '{0}'.  Found providers but they returned zero results!", context.MemberType);
                    }

                    buffer.AllocFreeAddRange(allInstances);
                }
                finally
                {
                    ZenPools.DespawnList(instances);
                    ZenPools.DespawnList(allInstances);
                }
            }
            finally
            {
                ZenPools.DespawnList(matches);
            }
        }

        void CheckForInstallWarning(InjectContext context)
        {
            if (!ZenjectSettings.DisplayWarningWhenResolvingDuringInstall)
            {
                return;
            }

            Assert.IsNotNull(context);

#if DEBUG || UNITY_EDITOR
            if (!_isInstalling)
            {
                return;
            }

            if (_hasDisplayedInstallWarning)
            {
                return;
            }

            if (context == null)
            {
                // No way to tell whether this is ok or not so just assume ok
                return;
            }

#if UNITY_EDITOR
            if (context.MemberType.DerivesFrom<Context>())
            {
                // This happens when getting default transform parent so ok
                return;
            }
#endif

            var rootContext = context.ParentContextsAndSelf.Last();

            if (rootContext.MemberType.DerivesFrom<IInstaller>())
            {
                // Resolving/instantiating/injecting installers is valid during install phase
                return;
            }

            _hasDisplayedInstallWarning = true;

            // Feel free to comment this out if you are comfortable with this practice
            Log.Warn("Zenject Warning: It is bad practice to call Inject/Resolve/Instantiate before all the Installers have completed!  This is important to ensure that all bindings have properly been installed in case they are needed when injecting/instantiating/resolving.  Detected when operating on type '{0}'.  If you don't care about this, you can disable this warning by setting flag 'ZenjectSettings.DisplayWarningWhenResolvingDuringInstall' to false (see docs for details on ZenjectSettings).", rootContext.MemberType);
#endif
        }

        // Returns the concrete type that would be returned with Resolve<T>
        // without actually instantiating it
        // This is safe to use within installers
        public Type ResolveType<T>()
        {
            return ResolveType(typeof(T));
        }

        // Returns the concrete type that would be returned with Resolve(type)
        // without actually instantiating it
        // This is safe to use within installers
        public Type ResolveType(Type type)
        {
            using (var context = ZenPools.SpawnInjectContext(this, type))
            {
                return ResolveType(context);
            }
        }

        // Returns the concrete type that would be returned with Resolve(context)
        // without actually instantiating it
        // This is safe to use within installers
        public Type ResolveType(InjectContext context)
        {
            Assert.IsNotNull(context);

            FlushBindings();

            var providerInfo = TryGetUniqueProvider(context);

            if (providerInfo == null)
            {
                throw Assert.CreateException(
                    "Unable to resolve {0}{1}. Object graph:\n{2}", context.BindingId,
                    (context.ObjectType == null ? "" : " while building object with type '{0}'".Fmt(context.ObjectType)),
                    context.GetObjectGraphString());
            }

            return providerInfo.Value.Provider.GetInstanceType(context);
        }

        public List<Type> ResolveTypeAll(Type type)
        {
            return ResolveTypeAll(type, null);
        }

        public List<Type> ResolveTypeAll(Type type, object identifier)
        {
            using (var context = ZenPools.SpawnInjectContext(this, type))
            {
                context.Identifier = identifier;
                return ResolveTypeAll(context);
            }
        }

        // Returns all the types that would be returned if ResolveAll was called with the given values
        public List<Type> ResolveTypeAll(InjectContext context)
        {
            Assert.IsNotNull(context);

            FlushBindings();

            var matches = ZenPools.SpawnList<ProviderInfo>();

            try
            {
                GetProviderMatches(context, matches);

                if (matches.Count > 0 )
                {
                    return matches.Select(
                        x => x.Provider.GetInstanceType(context))
                        .Where(x => x != null).ToList();
                }

                return new List<Type>();
            }
            finally
            {
                ZenPools.DespawnList(matches);
            }
        }

        public object Resolve(BindingId id)
        {
            using (var context = ZenPools.SpawnInjectContext(this, id.Type))
            {
                context.Identifier = id.Identifier;
                return Resolve(context);
            }
        }

        public object Resolve(InjectContext context)
        {
            // Note: context.Container is not necessarily equal to this, since
            // you can have some lookups recurse to parent containers
            Assert.IsNotNull(context);

            var memberType = context.MemberType;

            FlushBindings();
            CheckForInstallWarning(context);

            var lookupContext = context;

            // The context used for lookups is always the same as the given context EXCEPT for LazyInject<>
            // In CreateLazyBinding above, we forward the context to a new instance of LazyInject<>
            // The problem is, we want the binding for Bind(typeof(LazyInject<>)) to always match even
            // for members that are marked for a specific ID, so we need to discard the identifier
            // for this one particular case
            if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(LazyInject<>))
            {
                lookupContext = context.Clone();
                lookupContext.Identifier = null;
                lookupContext.SourceType = InjectSources.Local;
                lookupContext.Optional = false;
            }

            var providerInfo = TryGetUniqueProvider(lookupContext);

            if (providerInfo == null)
            {
                // If it's an array try matching to multiple values using its array type
                if (memberType.IsArray && memberType.GetArrayRank() == 1)
                {
                    var subType = memberType.GetElementType();

                    var subContext = context.Clone();
                    subContext.MemberType = subType;
                    // By making this optional this means that all injected fields of type T[]
                    // will pass validation, which could be error prone, but I think this is better
                    // than always requiring that they explicitly mark their array types as optional
                    subContext.Optional = true;

                    var results = ZenPools.SpawnList<object>();

                    try
                    {
                        ResolveAll(subContext, results);
                        return ReflectionUtil.CreateArray(subContext.MemberType, results);
                    }
                    finally
                    {
                        ZenPools.DespawnList(results);
                    }
                }

                // If it's a generic list then try matching multiple instances to its generic type
                if (memberType.IsGenericType
                    && (memberType.GetGenericTypeDefinition() == typeof(List<>)
                        || memberType.GetGenericTypeDefinition() == typeof(IList<>)
#if NET_4_6
                        || memberType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
#endif
                        || memberType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {
                    var subType = memberType.GetGenericArguments().Single();

                    var subContext = context.Clone();
                    subContext.MemberType = subType;
                    // By making this optional this means that all injected fields of type List<>
                    // will pass validation, which could be error prone, but I think this is better
                    // than always requiring that they explicitly mark their list types as optional
                    subContext.Optional = true;

                    return ResolveAll(subContext);
                }

                if (context.Optional)
                {
                    return null;
                }

                throw Assert.CreateException("Unable to resolve '{0}'{1}. Object graph:\n{2}", context.BindingId,
                    (context.ObjectType == null ? "" : " while building object with type '{0}'".Fmt(context.ObjectType)),
                    context.GetObjectGraphString());
            }

            var instances = ZenPools.SpawnList<object>();

            try
            {
                SafeGetInstances(providerInfo.Value, context, instances);

                if (instances.Count == 0)
                {
                    if (context.Optional)
                    {
                        return null;
                    }

                    throw Assert.CreateException(
                        "Unable to resolve '{0}'{1}. Object graph:\n{2}", context.BindingId,
                        (context.ObjectType == null
                            ? ""
                            : " while building object with type '{0}'".Fmt(context.ObjectType)),
                        context.GetObjectGraphString());
                }

                if (instances.Count() > 1)
                {
                    throw Assert.CreateException(
                        "Provider returned multiple instances when only one was expected!  While resolving '{0}'{1}. Object graph:\n{2}", context.BindingId,
                        (context.ObjectType == null
                            ? ""
                            : " while building object with type '{0}'".Fmt(context.ObjectType)),
                        context.GetObjectGraphString());
                }

                return instances.First();
            }
            finally
            {
                ZenPools.DespawnList(instances);
            }
        }

        void SafeGetInstances(ProviderInfo providerInfo, InjectContext context, List<object> instances)
        {
            Assert.IsNotNull(context);

            var provider = providerInfo.Provider;

            var lookupId = new LookupId(provider, context.BindingId);

            // Use the container associated with the provider to address some rare cases
            // which would otherwise result in an infinite loop.  Like this:
            // Container.Bind<ICharacter>().FromComponentInNewPrefab(Prefab).AsTransient()
            // With the prefab being a GameObjectContext containing a script that has a
            // ICharacter dependency.  In this case, we would otherwise use the _resolvesInProgress
            // associated with the GameObjectContext container, which will allow the recursive
            // lookup, which will trigger another GameObjectContext and container (since it is
            // transient) and the process continues indefinitely
            var providerContainer = providerInfo.Container;

            if (providerContainer._resolvesTwiceInProgress.Contains(lookupId))
            {
                // Allow one before giving up so that you can do circular dependencies via postinject or fields
                throw Assert.CreateException(
                    "Circular dependency detected! Object graph:\n {0}", context.GetObjectGraphString());
            }

            bool twice = false;
            if (!providerContainer._resolvesInProgress.Add(lookupId))
            {
                bool added = providerContainer._resolvesTwiceInProgress.Add(lookupId);
                Assert.That(added);
                twice = true;
            }

            try
            {
                provider.GetAllInstances(context, instances);
            }
            finally
            {
                if (twice)
                {
                    bool removed = providerContainer._resolvesTwiceInProgress.Remove(lookupId);
                    Assert.That(removed);
                }
                else
                {
                    bool removed = providerContainer._resolvesInProgress.Remove(lookupId);
                    Assert.That(removed);
                }
            }
        }

        int GetContainerHeirarchyDistance(DiContainer container)
        {
            return GetContainerHeirarchyDistance(container, 0).Value;
        }

        int? GetContainerHeirarchyDistance(DiContainer container, int depth)
        {
            return container == this ? depth : ParentContainer?.GetContainerHeirarchyDistance(container, depth + 1);
        }

        public void InjectExplicit(
            object injectable, Type injectableType,
            object[] extraArgs, InjectContext context, object concreteIdentifier)
        {
            Assert.That(injectable != null);

            if (TypeAnalyzer.GetInfo(injectableType, out var typeInfo) == false)
            {
                Assert.That(extraArgs == null);
                return;
            }

            // Installers are the only things that we instantiate/inject on during validation

            Assert.IsEqual(injectable.GetType(), injectableType);

#if !NOT_UNITY3D
            if (injectableType == typeof(GameObject))
            {
                Assert.CreateException(
                    "Use InjectGameObject to Inject game objects instead of Inject method. Object graph: {0}", context.GetObjectGraphString());
            }
#endif

            FlushBindings();
            CheckForInstallWarning(context);

            InjectMembersTopDown(
                injectable, injectableType, typeInfo, extraArgs, context, concreteIdentifier);

            CallInjectMethodsTopDown(
                injectable, injectableType, typeInfo, extraArgs, context, concreteIdentifier);
        }

        void CallInjectMethodsTopDown(
            object injectable, Type injectableType,
            InjectTypeInfo typeInfo, object[] extraArgs,
            InjectContext context, object concreteIdentifier)
        {
            foreach (var method in typeInfo.InjectMethods)
            {
                var paramValues = ParamArrayPool.Rent(method.Parameters.Length);

                try
                {
                    for (int k = 0; k < method.Parameters.Length; k++)
                    {
                        var injectInfo = method.Parameters[k];

                        if (!InjectUtil.TryGetValueWithType(extraArgs, injectInfo.MemberType, out var value))
                        {
                            using var subContext = ZenPools.SpawnInjectContext(
                                this, injectInfo, context, injectable, injectableType, concreteIdentifier);
                            value = Resolve(subContext);
                        }

                        paramValues[k] = value;
                    }

                    method.MethodInfo.Invoke(injectable, paramValues);
                }
                finally
                {
                    ParamArrayPool.Release(paramValues);
                }
            }
        }

        void InjectMembersTopDown(
            object injectable, Type injectableType,
            InjectTypeInfo typeInfo, object[] extraArgs,
            InjectContext context, object concreteIdentifier)
        {
            foreach (var injectField in typeInfo.InjectFields)
                InjectMember(injectField.Info, injectField, injectable, injectableType, extraArgs, context, concreteIdentifier);
            foreach (var injectProperty in typeInfo.InjectProperties)
                InjectMember(injectProperty.Info, injectProperty, injectable, injectableType, extraArgs, context, concreteIdentifier);
        }

        void InjectMember<T>(InjectableInfo injectInfo, T setter,  object injectable, Type injectableType, object[] extraArgs, InjectContext context, object concreteIdentifier)
            where T : InjectTypeInfo.IInjectMemberSetter
        {
            object value;

            if (InjectUtil.TryGetValueWithType(extraArgs, injectInfo.MemberType, out value))
            {
                setter.Invoke(injectable, value);
            }
            else
            {
                using (var subContext = ZenPools.SpawnInjectContext(
                           this, injectInfo, context, injectable, injectableType, concreteIdentifier))
                {
                    value = Resolve(subContext);
                }

                if (injectInfo.Optional && value == null)
                {
                    // Do not override in this case so it retains the hard-coded value
                }
                else
                {
                    setter.Invoke(injectable, value);
                }
            }
        }

#if !NOT_UNITY3D

        // Don't use this unless you know what you're doing
        // You probably want to use InstantiatePrefab instead
        // This one will only create the prefab and will not inject into it
        // Also, this will always return the new game object as disabled, so that injection can occur before Awake / OnEnable / Start
        internal GameObject CreateAndParentPrefabResource(
            string resourcePath, GameObjectCreationParameters gameObjectBindInfo, InjectContext context, out bool shouldMakeActive)
        {
            var prefab = (GameObject)Resources.Load(resourcePath);

            Assert.IsNotNull(prefab,
                "Could not find prefab at resource location '{0}'".Fmt(resourcePath));

            return CreateAndParentPrefab(prefab, gameObjectBindInfo, context, out shouldMakeActive);
        }

        GameObject GetPrefabAsGameObject(UnityEngine.Object prefab)
        {
            if (prefab is GameObject)
            {
                return (GameObject)prefab;
            }

            Assert.That(prefab is Component, "Invalid type given for prefab. Given object name: '{0}'", prefab.name);
            return ((Component)prefab).gameObject;
        }

        // Don't use this unless you know what you're doing
        // You probably want to use InstantiatePrefab instead
        // This one will only create the prefab and will not inject into it
        internal GameObject CreateAndParentPrefab(
            UnityEngine.Object prefab, GameObjectCreationParameters gameObjectBindInfo,
            InjectContext context, out bool shouldMakeActive)
        {
            Assert.That(prefab != null, "Null prefab found when instantiating game object");

            FlushBindings();

            var prefabAsGameObject = GetPrefabAsGameObject(prefab);

            var prefabWasActive = prefabAsGameObject.activeSelf;

            shouldMakeActive = prefabWasActive;

            var parent = gameObjectBindInfo.ParentTransform;

            Transform initialParent;
#if !UNITY_EDITOR
            if (prefabWasActive)
            {
                prefabAsGameObject.SetActive(false);
            }
#else
            if (prefabWasActive)
            {
                initialParent = ZenUtilInternal.GetOrCreateInactivePrefabParent();
            }
            else
#endif
            {
                if (parent != null)
                {
                    initialParent = parent;
                }
                else
                {
                    // This ensures it gets added to the right scene instead of just the active scene
                    initialParent = ContextTransform;
                }
            }

            bool positionAndRotationWereSet;
            GameObject gameObj;

            if (gameObjectBindInfo.Position.HasValue && gameObjectBindInfo.Rotation.HasValue)
            {
                gameObj = GameObject.Instantiate(
                    prefabAsGameObject, gameObjectBindInfo.Position.Value, gameObjectBindInfo.Rotation.Value, initialParent);
                positionAndRotationWereSet = true;
            }
            else if (gameObjectBindInfo.Position.HasValue)
            {
                gameObj = GameObject.Instantiate(
                    prefabAsGameObject, gameObjectBindInfo.Position.Value, prefabAsGameObject.transform.rotation, initialParent);
                positionAndRotationWereSet = true;
            }
            else if (gameObjectBindInfo.Rotation.HasValue)
            {
                gameObj = GameObject.Instantiate(
                    prefabAsGameObject, prefabAsGameObject.transform.position, gameObjectBindInfo.Rotation.Value, initialParent);
                positionAndRotationWereSet = true;
            }
            else
            {
                gameObj = GameObject.Instantiate(prefabAsGameObject, initialParent);
                positionAndRotationWereSet = false;
            }

#if !UNITY_EDITOR
            if (prefabWasActive)
            {
                prefabAsGameObject.SetActive(true);
            }
#else
            if (prefabWasActive)
            {
                gameObj.SetActive(false);

                if (parent == null)
                {
                    gameObj.transform.SetParent(ContextTransform, positionAndRotationWereSet);
                }
            }
#endif

            if (gameObj.transform.parent != parent)
            {
                gameObj.transform.SetParent(parent, positionAndRotationWereSet);
            }

            return gameObj;
        }

        public GameObject CreateEmptyGameObject(GameObjectCreationParameters gameObjectBindInfo)
        {
            FlushBindings();

            var gameObj = new GameObject();
            var parent = gameObjectBindInfo.ParentTransform;

            if (parent == null)
            {
                // This ensures it gets added to the right scene instead of just the active scene
                gameObj.transform.SetParent(ContextTransform, false);
                gameObj.transform.SetParent(null, false);
            }
            else
            {
                gameObj.transform.SetParent(parent, false);
            }

            return gameObj;
        }

#endif

        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public T Instantiate<T>(object[] extraArgs = null)
        {
            return (T)Instantiate(typeof(T), extraArgs);
        }

        public object Instantiate(Type concreteType, [CanBeNull] object[] extraArgs = null)
        {
            return InstantiateExplicit(
                concreteType,
                true,
                extraArgs,
                new InjectContext(this, concreteType, null),
                null);
        }

#if !NOT_UNITY3D
        // Add new component to existing game object and fill in its dependencies
        // This is the same as AddComponent except the [Inject] fields will be filled in
        // NOTE: Gameobject here is not a prefab prototype, it is an instance
        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public TContract InstantiateComponent<TContract>(
            GameObject gameObject, [CanBeNull] object[] extraArgs = null)
            where TContract : Component
        {
            return (TContract)InstantiateComponent(typeof(TContract), gameObject, extraArgs);
        }

        // Add new component to existing game object and fill in its dependencies
        // This is the same as AddComponent except the [Inject] fields will be filled in
        // NOTE: Gameobject here is not a prefab prototype, it is an instance
        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public Component InstantiateComponent(
            Type componentType, GameObject gameObject, [CanBeNull] object[] extraArgs = null)
        {
            Assert.That(componentType.DerivesFrom<Component>());

            FlushBindings();

            var monoBehaviour = gameObject.AddComponent(componentType);
            Inject(monoBehaviour, extraArgs);
            return monoBehaviour;
        }

        public T InstantiateComponentOnNewGameObject<T>()
            where T : Component
        {
            return InstantiateComponentOnNewGameObject<T>(Array.Empty<object>());
        }

        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public T InstantiateComponentOnNewGameObject<T>(object[] extraArgs) where T : Component
        {
            return InstantiateComponent<T>(CreateEmptyGameObject(default), extraArgs);
        }

        // Create a new game object from a prefab and fill in dependencies for all children
        public GameObject InstantiatePrefab(UnityEngine.Object prefab, Transform parentTransform)
        {
            return InstantiatePrefab(
                prefab, new GameObjectCreationParameters { ParentTransform = parentTransform });
        }

        // Create a new game object from a prefab and fill in dependencies for all children
        public GameObject InstantiatePrefab(
            UnityEngine.Object prefab, Vector3 position, Quaternion rotation, Transform parentTransform)
        {
            return InstantiatePrefab(
                prefab, new GameObjectCreationParameters
                {
                    ParentTransform = parentTransform,
                    Position = position,
                    Rotation = rotation
                });
        }

        // Create a new game object from a prefab and fill in dependencies for all children
        public GameObject InstantiatePrefab(
            UnityEngine.Object prefab, GameObjectCreationParameters gameObjectBindInfo = default)
        {
            FlushBindings();

            bool shouldMakeActive;
            var gameObj = CreateAndParentPrefab(
                prefab, gameObjectBindInfo, null, out shouldMakeActive);

            InjectGameObject(gameObj);

            if (shouldMakeActive)
            {
                gameObj.SetActive(true);
            }

            return gameObj;
        }

        // Create a new game object from a resource path and fill in dependencies for all children
        public GameObject InstantiatePrefabResource(string resourcePath, Transform parentTransform)
        {
            return InstantiatePrefabResource(resourcePath, new GameObjectCreationParameters { ParentTransform = parentTransform });
        }

        public GameObject InstantiatePrefabResource(
            string resourcePath, Vector3 position, Quaternion rotation, Transform parentTransform)
        {
            return InstantiatePrefabResource(
                resourcePath, new GameObjectCreationParameters
                {
                    ParentTransform = parentTransform,
                    Position = position,
                    Rotation = rotation
                });
        }

        // Create a new game object from a resource path and fill in dependencies for all children
        public GameObject InstantiatePrefabResource(
            string resourcePath, GameObjectCreationParameters creationInfo = default)
        {
            var prefab = (GameObject)Resources.Load(resourcePath);

            Assert.IsNotNull(prefab,
                "Could not find prefab at resource location '{0}'".Fmt(resourcePath));

            return InstantiatePrefab(prefab, creationInfo);
        }

        // Inject dependencies into any and all child components on the given game object
        public void InjectGameObject(GameObject gameObject)
        {
            FlushBindings();

            var injectTargetCollection = gameObject.GetComponent<InjectTargetCollection>();
            foreach (var target in injectTargetCollection.Targets)
                Inject(target);
        }
#endif

        // Same as Inject(injectable) except allows adding extra values to be injected
        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public void Inject(object injectable, [CanBeNull] object[] extraArgs = null)
        {
            var injectableType = injectable.GetType();

            InjectExplicit(
                injectable,
                injectableType,
                extraArgs,
                new InjectContext(this, injectableType, null),
                null);
        }

        // Resolve<> - Lookup a value in the container.
        //
        // Note that this may result in a new object being created (for transient bindings) or it
        // may return an already created object (for FromInstance or ToSingle, etc. bindings)
        //
        // If a single unique value for the given type cannot be found, an exception is thrown.
        //
        public TContract Resolve<TContract>()
        {
            return (TContract)Resolve(typeof(TContract));
        }

        public object Resolve(Type contractType)
        {
            return ResolveId(contractType, null);
        }

        public TContract ResolveId<TContract>(object identifier)
        {
            return (TContract)ResolveId(typeof(TContract), identifier);
        }

        public object ResolveId(Type contractType, object identifier)
        {
            using (var context = ZenPools.SpawnInjectContext(this, contractType))
            {
                context.Identifier = identifier;
                return Resolve(context);
            }
        }

        // Same as Resolve<> except it will return null if a value for the given type cannot
        // be found.
        public TContract TryResolve<TContract>()
            where TContract : class
        {
            return (TContract)TryResolve(typeof(TContract));
        }

        public object TryResolve(Type contractType)
        {
            return TryResolveId(contractType, null);
        }

        public TContract TryResolveId<TContract>(object identifier)
            where TContract : class
        {
            return (TContract)TryResolveId(
                typeof(TContract), identifier);
        }

        public object TryResolveId(Type contractType, object identifier)
        {
            using (var context = ZenPools.SpawnInjectContext(this, contractType))
            {
                context.Identifier = identifier;
                context.Optional = true;
                return Resolve(context);
            }
        }

        // Same as Resolve<> except it will return all bindings that are associated with the given type
        public List<TContract> ResolveAll<TContract>()
        {
            return (List<TContract>)ResolveAll(typeof(TContract));
        }

        public IList ResolveAll(Type contractType)
        {
            return ResolveIdAll(contractType, null);
        }

        public List<TContract> ResolveIdAll<TContract>(object identifier)
        {
            return (List<TContract>)ResolveIdAll(typeof(TContract), identifier);
        }

        public IList ResolveIdAll(Type contractType, object identifier)
        {
            using (var context = ZenPools.SpawnInjectContext(this, contractType))
            {
                context.Identifier = identifier;
                context.Optional = true;
                return ResolveAll(context);
            }
        }

        // Removes all bindings
        public void UnbindAll()
        {
            FlushBindings();
            _providers.Clear();
        }

        // Remove all bindings bound to the given contract type
        public bool Unbind<TContract>()
        {
            return Unbind(typeof(TContract));
        }

        public bool Unbind(Type contractType)
        {
            return UnbindId(contractType, null);
        }

        public bool UnbindId<TContract>(object identifier)
        {
            return UnbindId(typeof(TContract), identifier);
        }

        public bool UnbindId(Type contractType, object identifier)
        {
            FlushBindings();

            var bindingId = new BindingId(contractType, identifier);

            return _providers.Remove(bindingId);
        }

        public void UnbindInterfacesTo<TConcrete>()
        {
            UnbindInterfacesTo(typeof(TConcrete));
        }

        public void UnbindInterfacesTo(Type concreteType)
        {
            foreach (var i in concreteType.Interfaces())
            {
                Unbind(i, concreteType);
            }
        }

        public bool Unbind<TContract, TConcrete>()
        {
            return Unbind(typeof(TContract), typeof(TConcrete));
        }

        public bool Unbind(Type contractType, Type concreteType)
        {
            return UnbindId(contractType, concreteType, null);
        }

        public bool UnbindId<TContract, TConcrete>(object identifier)
        {
            return UnbindId(typeof(TContract), typeof(TConcrete), identifier);
        }

        public bool UnbindId(Type contractType, Type concreteType, object identifier)
        {
            FlushBindings();

            var bindingId = new BindingId(contractType, identifier);

            List<ProviderInfo> providers;

            if (!_providers.TryGetValue(bindingId, out providers))
            {
                return false;
            }

            var matches = providers.Where(x => x.Provider.GetInstanceType(new InjectContext(this, contractType, identifier)).DerivesFromOrEqual(concreteType)).ToList();

            if (matches.Count == 0)
            {
                return false;
            }

            foreach (var info in matches)
            {
                bool success = providers.Remove(info);
                Assert.That(success);
            }

            return true;
        }

        // Returns true if the given type is bound to something in the container
        public bool HasBinding<TContract>()
        {
            return HasBinding(typeof(TContract));
        }

        public bool HasBinding(Type contractType)
        {
            return HasBindingId(contractType, null);
        }

        public bool HasBindingId<TContract>(object identifier)
        {
            return HasBindingId(typeof(TContract), identifier);
        }

        public bool HasBindingId(Type contractType, object identifier)
        {
            return HasBindingId(contractType, identifier, InjectSources.Any);
        }

        public bool HasBindingId(Type contractType, object identifier, InjectSources sourceType)
        {
            using (var ctx = ZenPools.SpawnInjectContext(this, contractType))
            {
                ctx.Identifier = identifier;
                ctx.SourceType = sourceType;
                return HasBinding(ctx);
            }
        }

        // You shouldn't need to use this
        public bool HasBinding(InjectContext context)
        {
            Assert.IsNotNull(context);

            FlushBindings();

            var matches = ZenPools.SpawnList<ProviderInfo>();

            try
            {
                GetProviderMatches(context, matches);
                return matches.Count > 0;
            }
            finally
            {
                ZenPools.DespawnList(matches);
            }
        }

        // You shouldn't need to use this
        public void FlushBindings()
        {
            while (_currentBindings.Count > 0)
            {
                var binding = _currentBindings.Dequeue();

                FinalizeBinding(binding);

                binding.Dispose();
            }
        }

        void FinalizeBinding(BindStatement binding)
        {
            _isFinalizingBinding = true;

            try
            {
                binding.FinalizeBinding(this);
            }
            finally
            {
                _isFinalizingBinding = false;
            }
        }

        // Don't use this method
        public BindStatement StartBinding(bool flush = true)
        {
            Assert.That(!_isFinalizingBinding,
                "Attempted to start a binding during a binding finalizer.  This is not allowed, since binding finalizers should directly use AddProvider instead, to allow for bindings to be inherited properly without duplicates");

            if (flush)
            {
                FlushBindings();
            }

            var bindStatement = ZenPools.SpawnStatement();
            _currentBindings.Enqueue(bindStatement);
            return bindStatement;
        }

        // Map the given type to a way of obtaining it
        // Note that this can include open generic types as well such as List<>
        public ConcreteIdBinderGeneric<TContract> Bind<TContract>()
        {
            return Bind<TContract>(StartBinding());
        }

        // This is only useful for complex cases where you want to add multiple bindings
        // at the same time and can be ignored by 99% of users
        public ConcreteIdBinderGeneric<TContract> BindNoFlush<TContract>()
        {
            return Bind<TContract>(StartBinding(false));
        }

        ConcreteIdBinderGeneric<TContract> Bind<TContract>(
            BindStatement bindStatement)
        {
            var bindInfo = bindStatement.SpawnBindInfo();

            Assert.That(!bindInfo.ContractTypes.Contains(typeof(TContract)));
            bindInfo.ContractTypes.Add(typeof(TContract));

            return new ConcreteIdBinderGeneric<TContract>(
                this, bindInfo, bindStatement);
        }

        // Non-generic version of Bind<> for cases where you only have the runtime type
        // Note that this can include open generic types as well such as List<>
        public ConcreteIdBinderNonGeneric Bind(params Type[] contractTypes)
        {
            var statement = StartBinding();
            var bindInfo = statement.SpawnBindInfo();
            bindInfo.ContractTypes.AllocFreeAddRange(contractTypes);
            return BindInternal(bindInfo, statement);
        }

        ConcreteIdBinderNonGeneric BindInternal(
            BindInfo bindInfo, BindStatement bindingFinalizer)
        {
            return new ConcreteIdBinderNonGeneric(this, bindInfo, bindingFinalizer);
        }

        // Bind all the interfaces for the given type to the same thing.
        //
        // Example:
        //
        //    public class Foo : ITickable, IInitializable
        //    {
        //    }
        //
        //    Container.BindInterfacesTo<Foo>().AsSingle();
        //
        //  This line above is equivalent to the following:
        //
        //    Container.Bind<ITickable>().ToSingle<Foo>();
        //    Container.Bind<IInitializable>().ToSingle<Foo>();
        //
        // Note here that we do not bind Foo to itself.  For that, use BindInterfacesAndSelfTo
        public FromBinderNonGeneric BindInterfacesTo<T>()
        {
            return BindInterfacesTo(typeof(T));
        }

        public FromBinderNonGeneric BindInterfacesTo(Type type)
        {
            var statement = StartBinding();
            var bindInfo = statement.SpawnBindInfo();

            var interfaces = type.Interfaces();

            if (interfaces.Length == 0)
            {
                Log.Warn("Called BindInterfacesTo for type {0} but no interfaces were found", type);
            }

            bindInfo.ContractTypes.AllocFreeAddRange(interfaces);

            // Almost always, you don't want to use the default AsTransient so make them type it
            bindInfo.RequireExplicitScope = true;
            return BindInternal(bindInfo, statement).To(type);
        }

        // Same as BindInterfaces except also binds to self
        public FromBinderNonGeneric BindInterfacesAndSelfTo<T>()
        {
            return BindInterfacesAndSelfTo(typeof(T));
        }

        public FromBinderNonGeneric BindInterfacesAndSelfTo(Type type)
        {
            var statement = StartBinding();
            var bindInfo = statement.SpawnBindInfo();

            bindInfo.ContractTypes.AllocFreeAddRange(type.Interfaces());
            bindInfo.ContractTypes.Add(type);

            // Almost always, you don't want to use the default AsTransient so make them type it
            bindInfo.RequireExplicitScope = true;
            return BindInternal(bindInfo, statement).To(type);
        }

        //  This is simply a shortcut to using the FromInstance method.
        //
        //  Example:
        //      Container.BindInstance(new Foo());
        //
        //  This line above is equivalent to the following:
        //
        //      Container.Bind<Foo>().FromInstance(new Foo());
        //
        public IdScopeConcreteIdArgNonLazyBinder BindInstance<TContract>(TContract instance)
        {
            var statement = StartBinding();
            var bindInfo = statement.SpawnBindInfo();
            bindInfo.ContractTypes.Add(typeof(TContract));

            statement.SetFinalizer(
                new ScopableBindingFinalizer(
                    bindInfo,
                    (container, type) => new InstanceProvider(type, instance)));

            return new IdScopeConcreteIdArgNonLazyBinder(bindInfo);
        }

        // Unfortunately we can't support setting scope / condition / etc. here since all the
        // bindings are finalized one at a time
        public void BindInstances(params object[] instances)
        {
            for (int i = 0; i < instances.Length; i++)
            {
                var instance = instances[i];

                Assert.That(!ZenUtilInternal.IsNull(instance),
                    "Found null instance provided to BindInstances method");

                Bind(instance.GetType()).FromInstance(instance);
            }
        }

        public object InstantiateExplicit(Type concreteType, bool autoInject, [CanBeNull] object[] extraArgs, InjectContext context, object concreteIdentifier)
        {
#if !NOT_UNITY3D
            Assert.That(!concreteType.DerivesFrom<Component>(),
                "Error occurred while instantiating object of type '{0}'. Instantiator should not be used to create new mono behaviours.  Must use InstantiatePrefabForComponent, InstantiatePrefab, or InstantiateComponent.", concreteType);
#endif

            Assert.That(!concreteType.IsAbstract, "Expected type '{0}' to be non-abstract", concreteType);

            FlushBindings();
            CheckForInstallWarning(context);

            if (TypeAnalyzer.GetInfo(concreteType, out var typeInfo) == false)
                throw new Exception($"Tried to create type '{concreteType}' but could not find type information");

            object newObj;

            {
                Assert.IsNotNull(typeInfo.InjectConstructor.ConstructorInfo,
                    "More than one (or zero) constructors found for type '{0}' when creating dependencies.  Use one [Inject] attribute to specify which to use.", concreteType);

                // Make a copy since we remove from it below
                var paramValues = ParamArrayPool.Rent(typeInfo.InjectConstructor.Parameters.Length);

                try
                {
                    for (int i = 0; i < typeInfo.InjectConstructor.Parameters.Length; i++)
                    {
                        var injectInfo = typeInfo.InjectConstructor.Parameters[i];

                        object value;

                        if (!InjectUtil.TryGetValueWithType(
                            extraArgs, injectInfo.MemberType, out value))
                        {
                            using var subContext = ZenPools.SpawnInjectContext(
                                this, injectInfo, context, null, concreteType, concreteIdentifier);
                            value = Resolve(subContext);
                        }

                        Assert.IsNotNull(value);
                        paramValues[i] = value;
                    }

                    //ModestTree.Log.Debug("Zenject: Instantiating type '{0}'", concreteType);
                    try
                    {
                        newObj = typeInfo.InjectConstructor.ConstructorInfo.Invoke(paramValues);
                    }
                    catch (Exception e)
                    {
                        throw Assert.CreateException(
                            e, "Error occurred while instantiating object with type '{0}'", concreteType);
                    }
                }
                finally
                {
                    ParamArrayPool.Release(paramValues);
                }
            }

            if (autoInject)
                InjectExplicit(newObj, concreteType, extraArgs, context, concreteIdentifier);

            return newObj;
        }

        struct ProviderInfo
        {
            public ProviderInfo(
                IProvider provider, bool nonLazy, DiContainer container)
            {
                Provider = provider;
                NonLazy = nonLazy;
                Container = container;
            }

            public readonly DiContainer Container;
            public readonly bool NonLazy;
            public readonly IProvider Provider;
        }
    }
}
