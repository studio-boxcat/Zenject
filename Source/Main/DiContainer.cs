using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using ModestTree;
using Zenject.Internal;
using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.Pool;

namespace Zenject
{
    // Responsibilities:
    // - Expose methods to configure object graph via BindX() methods
    // - Look up bound values via Resolve() method
    // - Instantiate new values via InstantiateX() methods
    public class DiContainer
    {
        [CanBeNull] public readonly DiContainer ParentContainer;
        [CanBeNull] public readonly Transform ContextTransform;
        public readonly ProviderRepo ProviderRepo = new();

        readonly DiContainerChain _containerChain;
        readonly LazyInstanceInjector _lazyInjector;
        readonly List<int> _nonLazyProviders = new();
        readonly List<BindInfoBuilder> _currentBindings = new();

        bool _hasResolvedRoots;
        bool _isFinalizingBinding;

        public DiContainer(
            [CanBeNull] DiContainer parentContainer = null,
            [CanBeNull] Transform contextTransform = null)
        {
            ParentContainer = parentContainer;
            ContextTransform = contextTransform;

            _containerChain = new DiContainerChain(this);
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
            foreach (var providerIndex in _nonLazyProviders)
                ProviderRepo.Resolve(providerIndex);
            _nonLazyProviders.Clear();
        }

        public void QueueForInject(object[] instances)
        {
            _lazyInjector.AddInstances(instances);
        }

        public void RegisterProvider(BindInfo bindInfo)
        {
            var contractTypes = bindInfo.BakeContractTypes();
            var identifier = bindInfo.Identifier;

            if (ReferenceEquals(bindInfo.Instance, null) == false)
            {
                ProviderRepo.Register(bindInfo.Instance, contractTypes, identifier);
                return;
            }

            var provider = bindInfo.ProviderFactory != null
                ? bindInfo.ProviderFactory(this, bindInfo)
                : new TransientProvider(bindInfo.ConcreteType, this, bindInfo.Arguments);

            var providerIndex = ProviderRepo.Register(provider, contractTypes, identifier);
            if (bindInfo.NonLazy) _nonLazyProviders.Add(providerIndex);
        }

        public IList ResolveAll(Type type, int identifier, InjectSources sourceType)
        {
            Assert.That(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));

            var list = (IList) Activator.CreateInstance(type);
            var elementType = type.GetGenericArguments()[0];

            // Note that different types can map to the same provider (eg. a base type to a concrete class and a concrete class to itself)

            FlushBindings();

            var providers = ListPool<ProviderProxy>.Get();

            _containerChain.GetMatchingProviders(new BindingId(elementType, identifier), sourceType, providers);

            try
            {
                foreach (var provider in providers)
                    list.Add(provider.GetInstance());
                return list;
            }
            finally
            {
                ListPool<ProviderProxy>.Release(providers);
            }
        }

        public object Resolve(BindingId id)
        {
            if (TryResolve(id.Type, id.Identifier, InjectSources.Any, out var instance))
            {
                return instance;
            }
            else
            {
                throw new Exception("Failed to Resolve: " + id);
            }
        }

        public object Resolve(InjectableInfo context)
        {
            if (TryResolve(context.Type, context.Identifier, context.SourceType, out var instance))
            {
                return instance;
            }
            else
            {
                throw new Exception("Failed to Resolve: " + context.BindingId);
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

                    if (!InjectUtil.TryGetValueWithType(extraArgs, injectInfo.Type, out var value))
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
            if (InjectUtil.TryGetValueWithType(extraArgs, injectInfo.Type, out var value))
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
            var prefab = (GameObject) Resources.Load(resourcePath);

            Assert.IsNotNull(prefab,
                "Could not find prefab at resource location '{0}'".Fmt(resourcePath));

            return CreateAndParentPrefab(prefab, gameObjectBindInfo, out shouldMakeActive);
        }

        GameObject GetPrefabAsGameObject(Object prefab)
        {
            if (prefab is GameObject)
            {
                return (GameObject) prefab;
            }

            Assert.That(prefab is Component, "Invalid type given for prefab. Given object name: '{0}'", prefab.name);
            return ((Component) prefab).gameObject;
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
            return (T) Instantiate(typeof(T), extraArgs);
        }

        public object Instantiate(Type concreteType, [CanBeNull] object[] extraArgs = null)
        {
            Assert.That(!concreteType.DerivesFrom<Component>(),
                "Error occurred while instantiating object of type '{0}'. Instantiator should not be used to create new mono behaviours.  Must use InstantiatePrefabForComponent, InstantiatePrefab, or InstantiateComponent.",
                concreteType);

            Assert.That(!concreteType.IsAbstract, "Expected type '{0}' to be non-abstract", concreteType);

            FlushBindings();

            var typeInfo = TypeAnalyzer.GetInfo(concreteType);

            object newObj;

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
                            extraArgs, injectInfo.Type, out var value))
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

            Assert.That(newObj.GetType() == concreteType);
            Inject(newObj, extraArgs);

            return newObj;
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
            return (TContract) InstantiateComponent(typeof(TContract), gameObject, extraArgs);
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
                prefab, new GameObjectCreationParameters {ParentTransform = parentTransform});
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
            var typeInfo = TypeAnalyzer.GetInfo(injectable.GetType());

            FlushBindings();

            foreach (var injectField in typeInfo.InjectFields)
                InjectMember(injectField.Info, injectField, injectable, extraArgs);

            CallInjectMethodsTopDown(injectable, typeInfo, extraArgs);
        }

        public bool TryResolve(Type type, int identifier, InjectSources sourceType, out object instance)
        {
            // If it's a generic list then try matching multiple instances to its generic type
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                instance = ResolveAll(type, identifier, sourceType);
                return true;
            }

            FlushBindings();

            if (_containerChain.TryGetFirstProvider(new BindingId(type, identifier), sourceType, out var provider) == false)
            {
                instance = null;
                return false;
            }

            instance = provider.GetInstance();
            return true;
        }

        public bool TryResolve<TContract>(int identifier, InjectSources sourceType, out TContract instance)
        {
            if (TryResolve(typeof(TContract), identifier, sourceType, out var instance2))
            {
                instance = (TContract) instance2;
                return true;
            }
            else
            {
                instance = default;
                return false;
            }
        }

        public bool TryResolve(Type type, out object instance)
        {
            return TryResolve(type, 0, InjectSources.Any, out instance);
        }

        public bool TryResolve<TContract>(string identifier, out TContract instance)
        {
            return TryResolve(identifier.GetHashCode(), InjectSources.Any, out instance);
        }

        public bool TryResolve<TContract>(out TContract instance)
        {
            return TryResolve(0, InjectSources.Any, out instance);
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
            return (TContract) Resolve(typeof(TContract));
        }

        public TContract Resolve<TContract>(int identifier)
        {
            return (TContract) Resolve(typeof(TContract), identifier);
        }

        public TContract Resolve<TContract>(string identifier)
        {
            return Resolve<TContract>(identifier.GetHashCode());
        }

        public object Resolve(Type contractType, int identifier = 0)
        {
            return Resolve(new InjectableInfo(contractType, identifier));
        }

        public bool HasBinding(Type contractType, int identifier = 0, InjectSources sourceType = InjectSources.Any)
        {
            return HasBinding(new InjectableInfo(contractType, identifier, sourceType));
        }

        // You shouldn't need to use this
        public bool HasBinding(InjectableInfo context)
        {
            Assert.IsNotNull(context);
            FlushBindings();
            return _containerChain.TryGetFirstProvider(context.BindingId, context.SourceType, out _);
        }

        // You shouldn't need to use this
        public void FlushBindings()
        {
            _isFinalizingBinding = true;

            try
            {
                foreach (var bindInfoBuilder in _currentBindings)
                    RegisterProvider(bindInfoBuilder.GetBindInfo());
            }
            finally
            {
                _currentBindings.Clear();
                _isFinalizingBinding = false;
            }
        }

        // Don't use this method
        public BindInfoBuilder StartBinding(bool flush = true)
        {
            Assert.That(!_isFinalizingBinding,
                "Attempted to start a binding during a binding finalizer.  This is not allowed, since binding finalizers should directly use AddProvider instead, to allow for bindings to be inherited properly without duplicates");

            if (flush) FlushBindings();

            var builder = new BindInfoBuilder();
            _currentBindings.Add(builder);
            return builder;
        }

        // Map the given type to a way of obtaining it
        // Note that this can include open generic types as well such as List<>
        public BindInfoBuilder Bind<TContract>()
        {
            return StartBinding().BindSelf(typeof(TContract));
        }

        // This is only useful for complex cases where you want to add multiple bindings
        // at the same time and can be ignored by 99% of users
        public BindInfoBuilder BindNoFlush<TContract>()
        {
            return StartBinding(false).BindSelf(typeof(TContract));
        }

        // Non-generic version of Bind<> for cases where you only have the runtime type
        // Note that this can include open generic types as well such as List<>
        public BindInfoBuilder Bind(Type contractType)
        {
            return StartBinding().BindSelf(contractType);
        }

        // Bind all the interfaces for the given type to the same thing.
        //
        // Example:
        //
        //    public class Foo : ITickable, IInitializable
        //    {
        //    }
        //
        //    Container.BindInterfacesTo<Foo>();
        //
        //  This line above is equivalent to the following:
        //
        //    Container.Bind<ITickable>().ToSingle<Foo>();
        //    Container.Bind<IInitializable>().ToSingle<Foo>();
        //
        // Note here that we do not bind Foo to itself.  For that, use BindInterfacesAndSelfTo
        public BindInfoBuilder BindInterfacesTo(Type type)
        {
            return StartBinding().BindInterfaces(type);
        }

        public BindInfoBuilder BindInterfacesTo<T>()
        {
            return BindInterfacesTo(typeof(T));
        }

        public BindInfoBuilder BindInterfacesAndSelfTo(Type type)
        {
            return StartBinding().BindInterfacesAndSelf(type);
        }

        // Same as BindInterfaces except also binds to self
        public BindInfoBuilder BindInterfacesAndSelfTo<T>()
        {
            return BindInterfacesAndSelfTo(typeof(T));
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
        public BindInfoBuilder BindInstance<TContract>(TContract instance)
        {
            return StartBinding().BindSelf(typeof(TContract)).FromInstance(instance);
        }
    }
}