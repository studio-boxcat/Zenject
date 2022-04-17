using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using ModestTree;
using Zenject.Internal;
using Object = UnityEngine.Object;
using UnityEngine;

namespace Zenject
{
    // Responsibilities:
    // - Expose methods to configure object graph via BindX() methods
    // - Look up bound values via Resolve() method
    // - Instantiate new values via InstantiateX() methods
    public class DiContainer
    {
        [CanBeNull] public readonly DiContainer ParentContainer;

        readonly DiContainer[] _containerChain;

        readonly Dictionary<BindingId, List<ProviderInfo>> _providers = new Dictionary<BindingId, List<ProviderInfo>>();

        readonly HashSet<LookupId> _resolvesInProgress = new HashSet<LookupId>();
        readonly HashSet<LookupId> _resolvesTwiceInProgress = new HashSet<LookupId>();

        readonly LazyInstanceInjector _lazyInjector;

        readonly SingletonMarkRegistry _singletonMarkRegistry = new SingletonMarkRegistry();
        readonly Queue<BindStatement> _currentBindings = new Queue<BindStatement>();

        Transform _contextTransform;
        bool _hasLookedUpContextTransform;

        bool _hasResolvedRoots;
        bool _isFinalizingBinding;
        bool _isInstalling;

        public DiContainer(
            [CanBeNull] DiContainer parentContainer = null)
        {
            ParentContainer = parentContainer;

            _containerChain = BuildContainerChain(this);

            _lazyInjector = new LazyInstanceInjector(this);

            Bind(typeof(DiContainer)).FromInstance(this);
            FlushBindings();
            Assert.That(_currentBindings.Count == 0);

            if (parentContainer != null)
            {
                parentContainer.FlushBindings();

                Assert.That(_currentBindings.Count == 0);
            }
        }

        static DiContainer[] BuildContainerChain(DiContainer root)
        {
            var containerCount = 1;
            var targetContainer = root.ParentContainer;
            while (targetContainer != null)
            {
                targetContainer = targetContainer.ParentContainer;
                containerCount++;
            }

            var containerChain = new DiContainer[containerCount];
            containerChain[0] = root;
            var pointer = 1;
            targetContainer = root.ParentContainer;
            while (targetContainer != null)
            {
                containerChain[pointer++] = targetContainer;
                targetContainer = targetContainer.ParentContainer;
            }

            return containerChain;
        }

        internal SingletonMarkRegistry SingletonMarkRegistry
        {
            get { return _singletonMarkRegistry; }
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

            foreach (var (bindId, providerInfo) in rootBindings)
            {
                var context = new InjectableInfo(bindId.Type, bindId.Identifier, InjectSources.Local);

                var _ = SafeGetInstances(providerInfo, context);

                // Zero matches might actually be valid in some cases
                //Assert.That(matches.Any());
            }
        }

        public void QueueForInject(object[] instances)
        {
            _lazyInjector.AddInstances(instances);
        }

        public void RegisterProvider(
            BindingId bindingId, IProvider provider, bool nonLazy)
        {
            var info = new ProviderInfo(provider, this, nonLazy);

            List<ProviderInfo> providerInfos;

            if (!_providers.TryGetValue(bindingId, out providerInfos))
            {
                providerInfos = new List<ProviderInfo>();
                _providers.Add(bindingId, providerInfos);
            }

            providerInfos.Add(info);
        }

        void GetProviderMatches(
            InjectableInfo context, List<ProviderInfo> buffer)
        {
            Assert.That(buffer.Count == 0);

            GetProvidersForContract(
                context.BindingId, context.SourceType, buffer);
        }

        ProviderInfo? TryGetUniqueProvider(BindingId bindingId, InjectSources sourceType)
        {
            if (sourceType == InjectSources.Local)
                return Internal_TryGetUniqueProvider(this, bindingId);

            if (sourceType == InjectSources.Parent)
                return Internal_TryGetUniqueProvider(ParentContainer, bindingId);

            foreach (var container in _containerChain)
            {
                var provider = Internal_TryGetUniqueProvider(container, bindingId);
                if (provider != null) return provider.Value;
            }

            return null;

            static ProviderInfo? Internal_TryGetUniqueProvider(DiContainer container, BindingId bindingId)
            {
                container.FlushBindings();

                if (container._providers.TryGetValue(bindingId, out var localProviders))
                    return localProviders[0];

                return null;
            }
        }

        void GetProvidersForContract(BindingId bindingId, InjectSources sourceType, List<ProviderInfo> buffer)
        {
            if (sourceType == InjectSources.Local)
            {
                Internal_GetProvidersForContract(this, bindingId, buffer);
                return;
            }

            if (sourceType == InjectSources.Parent)
            {
                Internal_GetProvidersForContract(ParentContainer, bindingId, buffer);
                return;
            }

            foreach (var container in _containerChain)
                Internal_GetProvidersForContract(container, bindingId, buffer);

            static void Internal_GetProvidersForContract(DiContainer container, BindingId bindingId, List<ProviderInfo> buffer)
            {
                container.FlushBindings();

                if (container._providers.TryGetValue(bindingId, out var localProviders))
                    buffer.AddRange(localProviders);
            }
        }

        public IList ResolveAll(InjectableInfo context)
        {
            Assert.That(context.MemberType.IsGenericType && context.MemberType.GetGenericTypeDefinition() == typeof(List<>));

            var list = (IList) Activator.CreateInstance(context.MemberType);
            var elementType = context.MemberType.GetGenericArguments()[0];

            // Note that different types can map to the same provider (eg. a base type to a concrete class and a concrete class to itself)

            FlushBindings();

            var providers = ZenPools.SpawnList<ProviderInfo>();

            try
            {
                GetProviderMatches(context.MutateMemberType(elementType), providers);

                if (providers.Count == 0)
                {
                    if (!context.Optional)
                    {
                        throw Assert.CreateException(
                            "Could not find required dependency with type '{0}'", context.MemberType);
                    }

                    return list;
                }

                foreach (var provider in providers)
                {
                    var instance = SafeGetInstances(provider, context);
                    if (instance != null)
                        list.Add(instance);
                }

                if (list.Count == 0 && !context.Optional)
                {
                    throw Assert.CreateException(
                        "Could not find required dependency with type '{0}'.  Found providers but they returned zero results!", context.MemberType);
                }

                return list;
            }
            finally
            {
                ZenPools.DespawnList(providers);
            }
        }

        public object Resolve(BindingId id)
        {
            return Resolve(new InjectableInfo(id.Type, id.Identifier));
        }

        public object Resolve(InjectableInfo context)
        {
            var memberType = context.MemberType;

            // If it's a generic list then try matching multiple instances to its generic type
            if (memberType.IsGenericType
                && memberType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return ResolveAll(context);
            }

            FlushBindings();

            var lookupContext = context;

            var providerInfo = TryGetUniqueProvider(lookupContext.BindingId, lookupContext.SourceType);

            if (providerInfo == null)
            {

                if (context.Optional)
                {
                    return null;
                }

                throw Assert.CreateException("Unable to resolve '{0}'.", context.BindingId);
            }

            var instance = SafeGetInstances(providerInfo.Value, context);

            if (instance == null)
            {
                if (context.Optional)
                    return null;

                throw Assert.CreateException(
                    "Unable to resolve '{0}'.", context.BindingId);
            }

            return instance;
        }

        [CanBeNull]
        static object SafeGetInstances(ProviderInfo providerInfo, InjectableInfo context)
        {
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
                throw Assert.CreateException("Circular dependency detected!");
            }

            var twice = false;
            if (!providerContainer._resolvesInProgress.Add(lookupId))
            {
                var added = providerContainer._resolvesTwiceInProgress.Add(lookupId);
                Assert.That(added);
                twice = true;
            }

            try
            {
                return provider.GetInstance(context);
            }
            finally
            {
                if (twice)
                {
                    var removed = providerContainer._resolvesTwiceInProgress.Remove(lookupId);
                    Assert.That(removed);
                }
                else
                {
                    var removed = providerContainer._resolvesInProgress.Remove(lookupId);
                    Assert.That(removed);
                }
            }
        }

        void CallInjectMethodsTopDown(object injectable, InjectTypeInfo typeInfo, object[] extraArgs)
        {
            var method = typeInfo.InjectMethod;
            if (method.MethodInfo == null)
                return;

            var paramValues = ParamArrayPool.Rent(method.Parameters.Length);

            try
            {
                for (int k = 0; k < method.Parameters.Length; k++)
                {
                    var injectInfo = method.Parameters[k];

                    if (!InjectUtil.TryGetValueWithType(extraArgs, injectInfo.MemberType, out var value))
                    {
                        value = Resolve(injectInfo);
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

        void InjectMember(InjectableInfo injectInfo, InjectTypeInfo.InjectFieldInfo setter,
            object injectable, object[] extraArgs)
        {
            if (InjectUtil.TryGetValueWithType(extraArgs, injectInfo.MemberType, out var value))
            {
                setter.Invoke(injectable, value);
                return;
            }

            value = Resolve(injectInfo);

            if (injectInfo.Optional && value == null)
            {
                // Do not override in this case so it retains the hard-coded value
            }
            else
            {
                setter.Invoke(injectable, value);
            }
        }

#if !NOT_UNITY3D

        // Don't use this unless you know what you're doing
        // You probably want to use InstantiatePrefab instead
        // This one will only create the prefab and will not inject into it
        // Also, this will always return the new game object as disabled, so that injection can occur before Awake / OnEnable / Start
        internal GameObject CreateAndParentPrefabResource(
            string resourcePath, GameObjectCreationParameters gameObjectBindInfo, InjectableInfo context, out bool shouldMakeActive)
        {
            var prefab = (GameObject)Resources.Load(resourcePath);

            Assert.IsNotNull(prefab,
                "Could not find prefab at resource location '{0}'".Fmt(resourcePath));

            return CreateAndParentPrefab(prefab, gameObjectBindInfo, out shouldMakeActive);
        }

        GameObject GetPrefabAsGameObject(Object prefab)
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
            Object prefab, GameObjectCreationParameters gameObjectBindInfo, out bool shouldMakeActive)
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
                gameObj = Object.Instantiate(
                    prefabAsGameObject, gameObjectBindInfo.Position.Value, gameObjectBindInfo.Rotation.Value, initialParent);
                positionAndRotationWereSet = true;
            }
            else if (gameObjectBindInfo.Position.HasValue)
            {
                gameObj = Object.Instantiate(
                    prefabAsGameObject, gameObjectBindInfo.Position.Value, prefabAsGameObject.transform.rotation, initialParent);
                positionAndRotationWereSet = true;
            }
            else if (gameObjectBindInfo.Rotation.HasValue)
            {
                gameObj = Object.Instantiate(
                    prefabAsGameObject, prefabAsGameObject.transform.position, gameObjectBindInfo.Rotation.Value, initialParent);
                positionAndRotationWereSet = true;
            }
            else
            {
                gameObj = Object.Instantiate(prefabAsGameObject, initialParent);
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
        public T Instantiate<T>([CanBeNull] object[] extraArgs = null)
        {
            return (T)Instantiate(typeof(T), extraArgs);
        }

        public object Instantiate(Type concreteType, [CanBeNull] object[] extraArgs = null)
        {
            return InstantiateExplicit(
                concreteType,
                true,
                extraArgs);
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
        public GameObject InstantiatePrefab(Object prefab, Transform parentTransform)
        {
            return InstantiatePrefab(
                prefab, new GameObjectCreationParameters { ParentTransform = parentTransform });
        }

        // Create a new game object from a prefab and fill in dependencies for all children
        public GameObject InstantiatePrefab(
            Object prefab, Vector3 position, Quaternion rotation, Transform parentTransform)
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
            Object prefab, GameObjectCreationParameters gameObjectBindInfo = default)
        {
            FlushBindings();

            bool shouldMakeActive;
            var gameObj = CreateAndParentPrefab(
                prefab, gameObjectBindInfo, out shouldMakeActive);

            InjectGameObject(gameObj);

            if (shouldMakeActive)
            {
                gameObj.SetActive(true);
            }

            return gameObj;
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
            var hasTypeInfo = TypeAnalyzer.GetInfo(injectable.GetType(), out var typeInfo);
            Assert.That(hasTypeInfo);

            FlushBindings();

            foreach (var injectField in typeInfo.InjectFields)
                InjectMember(injectField.Info, injectField, injectable, extraArgs);

            CallInjectMethodsTopDown(injectable, typeInfo, extraArgs);
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
            return Resolve(new InjectableInfo(contractType, identifier));
        }

        // Same as Resolve<> except it will return null if a value for the given type cannot
        // be found.
        public TContract TryResolve<TContract>(object identifier = null)
            where TContract : class
        {
            return (TContract)TryResolve(typeof(TContract), identifier);
        }

        public object TryResolve(Type contractType, object identifier = null)
        {
            return Resolve(new InjectableInfo(contractType, identifier, true));
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

        // Returns true if the given type is bound to something in the container
        public bool HasBinding<TContract>(object identifier = null)
        {
            return HasBinding(typeof(TContract), identifier);
        }

        public bool HasBinding(Type contractType, object identifier = null, InjectSources sourceType = InjectSources.Any)
        {
            return HasBinding(new InjectableInfo(contractType, identifier, sourceType));
        }

        // You shouldn't need to use this
        public bool HasBinding(InjectableInfo context)
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
        public ConcreteBinderGeneric<TContract> Bind<TContract>()
        {
            return Bind<TContract>(StartBinding());
        }

        // This is only useful for complex cases where you want to add multiple bindings
        // at the same time and can be ignored by 99% of users
        public ConcreteBinderGeneric<TContract> BindNoFlush<TContract>()
        {
            return Bind<TContract>(StartBinding(false));
        }

        ConcreteBinderGeneric<TContract> Bind<TContract>(
            BindStatement bindStatement)
        {
            var bindInfo = bindStatement.SpawnBindInfo();

            Assert.That(!bindInfo.ContractTypes.Contains(typeof(TContract)));
            bindInfo.ContractTypes.Add(typeof(TContract));

            return new ConcreteBinderGeneric<TContract>(
                this, bindInfo, bindStatement);
        }

        // Non-generic version of Bind<> for cases where you only have the runtime type
        // Note that this can include open generic types as well such as List<>
        public ConcreteBinderNonGeneric Bind(Type contractType)
        {
            var statement = StartBinding();
            var bindInfo = statement.SpawnBindInfo();
            bindInfo.ContractTypes.Add(contractType);
            return BindInternal(bindInfo, statement);
        }

        ConcreteBinderNonGeneric BindInternal(
            BindInfo bindInfo, BindStatement bindingFinalizer)
        {
            return new ConcreteBinderNonGeneric(this, bindInfo, bindingFinalizer);
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

            bindInfo.ContractTypes.AddRange(interfaces);

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

            bindInfo.ContractTypes.AddRange(type.Interfaces());
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
        public NonLazyBinder BindInstance<TContract>(TContract instance)
        {
            var statement = StartBinding();
            var bindInfo = statement.SpawnBindInfo();
            bindInfo.ContractTypes.Add(typeof(TContract));

            statement.SetFinalizer(
                new ScopableBindingFinalizer(
                    bindInfo,
                    (container, type) => new InstanceProvider(instance)));

            return new NonLazyBinder(bindInfo);
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

        public object InstantiateExplicit(Type concreteType, bool autoInject, [CanBeNull] object[] extraArgs)
        {
#if !NOT_UNITY3D
            Assert.That(!concreteType.DerivesFrom<Component>(),
                "Error occurred while instantiating object of type '{0}'. Instantiator should not be used to create new mono behaviours.  Must use InstantiatePrefabForComponent, InstantiatePrefab, or InstantiateComponent.", concreteType);
#endif

            Assert.That(!concreteType.IsAbstract, "Expected type '{0}' to be non-abstract", concreteType);

            FlushBindings();

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
                    for (var i = 0; i < typeInfo.InjectConstructor.Parameters.Length; i++)
                    {
                        var injectInfo = typeInfo.InjectConstructor.Parameters[i];

                        if (!InjectUtil.TryGetValueWithType(
                                extraArgs, injectInfo.MemberType, out var value))
                        {
                            value = Resolve(injectInfo);
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
            {
                Assert.That(newObj.GetType() == concreteType);
                Inject(newObj, extraArgs);
            }

            return newObj;
        }

        struct ProviderInfo
        {
            public ProviderInfo(IProvider provider, DiContainer container, bool nonLazy)
            {
                Provider = provider;
                Container = container;
                NonLazy = nonLazy;
            }

            public readonly DiContainer Container;
            public readonly IProvider Provider;
            public readonly bool NonLazy;
        }
    }
}
