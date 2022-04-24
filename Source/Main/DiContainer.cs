using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using ModestTree;
using Zenject.Internal;
using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.Assertions;

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

        public readonly ProviderRepo ProviderRepo;

        readonly ProviderChain _providerChain;
        readonly LazyInstanceInjector _lazyInjector;
        readonly List<int> _nonLazyProviders = new();

        public DiContainer(
            [CanBeNull] DiContainer parentContainer = null,
            [CanBeNull] Transform contextTransform = null)
        {
            ParentContainer = parentContainer;
            ContextTransform = contextTransform;

            ProviderRepo = new ProviderRepo(this);

            _providerChain = new ProviderChain(this);
            _lazyInjector = new LazyInstanceInjector(this);

            Bind(this);
        }

        public void ResolveRoots()
        {
            ResolveDependencyRoots();

            _lazyInjector.LazyInjectAll();
        }

        void ResolveDependencyRoots()
        {
            foreach (var providerIndex in _nonLazyProviders)
                ProviderRepo.Resolve(providerIndex);
            _nonLazyProviders.Clear();
        }

        public void QueueForInject(object instance)
        {
            _lazyInjector.AddInstance(instance);
        }

        public void QueueForInject(object[] instances)
        {
            _lazyInjector.AddInstances(instances);
        }

        public void RegisterProvider(BindInfo bindInfo, object instance)
        {
            var contractTypes = bindInfo.BakeContractTypes();
            var identifier = bindInfo.Identifier;
            ProviderRepo.Register(contractTypes, identifier, instance);
        }

        public void RegisterProvider(BindInfo bindInfo, ProvideDelegate provider, ArgumentArray extraArgument, bool nonLazy)
        {
            var contractTypes = bindInfo.BakeContractTypes();
            var identifier = bindInfo.Identifier;

            provider ??= (container, concreteType, args) => container.Instantiate(concreteType, args);
            var providerIndex = ProviderRepo.Register(contractTypes, identifier, provider, bindInfo.ConcreteType, extraArgument);
            if (nonLazy) _nonLazyProviders.Add(providerIndex);
        }

        public IList ResolveAll(Type type, int identifier, InjectSources sourceType)
        {
            Assert.IsTrue(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));

            var list = (IList) Activator.CreateInstance(type);
            var elementType = type.GetGenericArguments()[0];
            _providerChain.ResolveAll(new BindingId(elementType, identifier), sourceType, list);
            return list;
        }

        void CallInjectMethods(object injectable, InjectTypeInfo.InjectMethodInfo method, ArgumentArray extraArgs)
        {
            var paramValues = ParamArrayPool.Rent(method.Parameters.Length);

            for (var i = 0; i < method.Parameters.Length; i++)
            {
                var injectInfo = method.Parameters[i];
                if (!extraArgs.TryGetValueWithType(injectInfo.Type, out var value))
                    value = Resolve(injectInfo);
                paramValues[i] = value;
            }

            method.MethodInfo.Invoke(injectable, paramValues);

            ParamArrayPool.Release(paramValues);
        }

        void InjectMember(InjectableInfo injectInfo, InjectTypeInfo.InjectFieldInfo setter,
            object injectable, ArgumentArray extraArgs)
        {
            if (extraArgs.TryGetValueWithType(injectInfo.Type, out var value))
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

        // Don't use this unless you know what you're doing
        // You probably want to use InstantiatePrefab instead
        // This one will only create the prefab and will not inject into it
        GameObject InstantiateGameObjectInactive(GameObject prefab, GameObjectCreationParameters creationParameters)
        {
            Assert.IsTrue(prefab != null, "Null prefab found when instantiating game object");

            prefab.SetActive(false);

            var parent = creationParameters.ParentTransform;
            var initialParent = parent ?? ContextTransform;

            GameObject gameObj;
            bool positionOrRotationWereSet;

            if (creationParameters.Position.HasValue && creationParameters.Rotation.HasValue)
            {
                gameObj = Object.Instantiate(prefab, creationParameters.Position.Value, creationParameters.Rotation.Value, initialParent);
                positionOrRotationWereSet = true;
            }
            else if (creationParameters.Position.HasValue)
            {
                gameObj = Object.Instantiate(prefab, creationParameters.Position.Value, prefab.transform.rotation, initialParent);
                positionOrRotationWereSet = true;
            }
            else if (creationParameters.Rotation.HasValue)
            {
                gameObj = Object.Instantiate(prefab, prefab.transform.position, creationParameters.Rotation.Value, initialParent);
                positionOrRotationWereSet = true;
            }
            else
            {
                gameObj = Object.Instantiate(prefab, initialParent);
                positionOrRotationWereSet = false;
            }

            prefab.SetActive(true);

            if (gameObj.transform.parent != parent)
                gameObj.transform.SetParent(parent, positionOrRotationWereSet);

            return gameObj;
        }

        public GameObject CreateEmptyGameObject(GameObjectCreationParameters gameObjectBindInfo)
        {
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

        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public T Instantiate<T>(ArgumentArray extraArgs = default)
        {
            return (T) Instantiate(typeof(T), extraArgs);
        }

        public object Instantiate(Type concreteType, ArgumentArray extraArgs = default)
        {
            Assert.IsFalse(concreteType.IsSubclassOf(typeof(Component)),
                "Error occurred while instantiating object of type '{0}'. Instantiator should not be used to create new mono behaviours.  Must use InstantiatePrefabForComponent, InstantiatePrefab, or InstantiateComponent."
                    .Fmt(concreteType));

            Assert.IsFalse(concreteType.IsAbstract, "Expected type '{0}' to be non-abstract".Fmt(concreteType));

            object newObj;

            var injectableInfo = TypeAnalyzer.GetInfo(concreteType);
            var constructorInfo = injectableInfo.InjectConstructor;
            if (constructorInfo.Parameters.Length == 0)
            {
                newObj = constructorInfo.ConstructorInfo.Invoke(null);
            }
            else
            {
                var paramValues = ParamArrayPool.Rent(constructorInfo.Parameters.Length);

                try
                {
                    ResolveParamArray(this, constructorInfo.Parameters, paramValues, extraArgs);
                    newObj = constructorInfo.ConstructorInfo.Invoke(paramValues);
                }
                finally
                {
                    ParamArrayPool.Release(paramValues);
                }
            }

            Assert.IsTrue(newObj.GetType() == concreteType);
            Inject(newObj, injectableInfo, extraArgs);
            return newObj;

            static void ResolveParamArray(DiContainer container, InjectableInfo[] paramInfos, object[] paramValues, ArgumentArray extraArgs)
            {
                for (var i = 0; i < paramInfos.Length; i++)
                {
                    var injectInfo = paramInfos[i];
                    if (!extraArgs.TryGetValueWithType(injectInfo.Type, out var value))
                        value = container.Resolve(injectInfo);
                    Assert.IsNotNull(value);
                    paramValues[i] = value;
                }
            }
        }

#if !NOT_UNITY3D
        // Add new component to existing game object and fill in its dependencies
        // This is the same as AddComponent except the [Inject] fields will be filled in
        // NOTE: Gameobject here is not a prefab prototype, it is an instance
        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public TContract InstantiateComponent<TContract>(
            GameObject gameObject, ArgumentArray extraArgs = default)
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
            Type componentType, GameObject gameObject, ArgumentArray extraArgs = default)
        {
            Assert.IsTrue(componentType.IsSubclassOf(typeof(Component)));

            var monoBehaviour = gameObject.AddComponent(componentType);
            Inject(monoBehaviour, extraArgs);
            return monoBehaviour;
        }

        public T InstantiateComponentOnNewGameObject<T>(ArgumentArray extraArgs = default) where T : Component
        {
            return InstantiateComponent<T>(CreateEmptyGameObject(default), extraArgs);
        }

        // Create a new game object from a prefab and fill in dependencies for all children
        public GameObject InstantiatePrefab(GameObject prefab, Transform parentTransform)
        {
            return InstantiatePrefab(
                prefab, new GameObjectCreationParameters {ParentTransform = parentTransform});
        }

        // Create a new game object from a prefab and fill in dependencies for all children
        public GameObject InstantiatePrefab(GameObject prefab, Vector3 position, Quaternion rotation, Transform parentTransform)
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
        public GameObject InstantiatePrefab(GameObject prefab, GameObjectCreationParameters gameObjectBindInfo = default)
        {
            var gameObj = InstantiateGameObjectInactive(prefab, gameObjectBindInfo);
            gameObj.GetComponent<InjectTargetCollection>().Inject(this);
            gameObj.SetActive(true);
            return gameObj;
        }

#endif

        public void Inject(object injectable, InjectTypeInfo typeInfo, ArgumentArray extraArgs)
        {
            if (typeInfo.InjectFields != null)
            {
                foreach (var injectField in typeInfo.InjectFields)
                    InjectMember(injectField.Info, injectField, injectable, extraArgs);
            }

            var method = typeInfo.InjectMethod;
            if (method.MethodInfo != null)
                CallInjectMethods(injectable, method, extraArgs);
        }

        public void Inject(object injectable, ArgumentArray extraArgs = default)
        {
            Inject(injectable, TypeAnalyzer.GetInfo(injectable.GetType()), extraArgs);
        }

        public bool TryResolve(Type type, int identifier, InjectSources sourceType, out object instance)
        {
            // If it's a generic list then try matching multiple instances to its generic type
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                instance = ResolveAll(type, identifier, sourceType);
                return true;
            }

            return _providerChain.TryResolve(new BindingId(type, identifier), sourceType, out instance);
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

        public object Resolve(Type contractType, int identifier = 0, InjectSources sourceType = InjectSources.Any)
        {
            if (TryResolve(contractType, identifier, sourceType, out var instance))
            {
                return instance;
            }
            else
            {
                throw new Exception($"Failed to Resolve: {contractType.Name}, {identifier}, {sourceType}");
            }
        }

        public object Resolve(InjectableInfo context)
        {
            if (TryResolve(context.Type, context.Identifier, context.SourceType, out var instance))
            {
                return instance;
            }

            if (context.Optional == false)
            {
                throw new Exception($"Failed to Resolve: {context.Type.Name}, {context.Identifier}, {context.SourceType}");
            }

            return null;
        }

        public bool HasBinding(Type contractType, int identifier = 0, InjectSources sourceType = InjectSources.Any)
        {
            return HasBinding(new InjectableInfo(contractType, identifier, sourceType));
        }

        // You shouldn't need to use this
        public bool HasBinding(InjectableInfo context)
        {
            return _providerChain.HasBinding(context.BindingId, context.SourceType);
        }

        public void Bind(Type contractType, int identifier = 0, ProvideDelegate provider = null, ArgumentArray arguments = default, bool nonLazy = false)
        {
            RegisterProvider(new BindInfo
            {
                ConcreteType = contractType,
                Identifier = identifier,
                BindConcreteType = true,
            }, provider, arguments, nonLazy);
        }

        public void Bind<TContract>(int identifier = 0, ProvideDelegate provider = null, ArgumentArray arguments = default, bool nonLazy = false)
        {
            Bind(typeof(TContract), identifier, provider, arguments, nonLazy);
        }

        public void Bind(object instance)
        {
            RegisterProvider(new BindInfo
            {
                ConcreteType = instance.GetType(),
                BindConcreteType = true,
            }, instance);
        }

        public void Bind(object instance, int id)
        {
            RegisterProvider(new BindInfo
            {
                ConcreteType = instance.GetType(),
                Identifier = id,
                BindConcreteType = true,
            }, instance);
        }

        public void Bind(object instance, string id)
        {
            RegisterProvider(new BindInfo
            {
                ConcreteType = instance.GetType(),
                Identifier = id.GetHashCode(),
                BindConcreteType = true,
            }, instance);
        }

        public void BindInterfacesTo(Type type, int identifier = 0, ArgumentArray arguments = default, bool nonLazy = false)
        {
            RegisterProvider(new BindInfo
            {
                ConcreteType = type,
                Identifier = identifier,
                BindInterfaces = true,
            }, null, arguments, nonLazy: nonLazy);
        }

        public void BindInterfacesTo<T>(int identifier = 0, ArgumentArray arguments = default, bool nonLazy = false)
        {
            BindInterfacesTo(typeof(T), identifier, arguments, nonLazy);
        }

        public void BindInterfacesTo(object instance, int identifier = 0)
        {
            RegisterProvider(new BindInfo
            {
                ConcreteType = instance.GetType(),
                Identifier = identifier,
                BindInterfaces = true,
            }, instance);
        }

        public void BindInterfacesAndSelfTo(Type type, int identifier = 0, ArgumentArray arguments = default, bool nonLazy = false)
        {
            RegisterProvider(new BindInfo
            {
                ConcreteType = type,
                Identifier = identifier,
                BindConcreteType = true,
                BindInterfaces = true,
            }, null, arguments, nonLazy: nonLazy);
        }

        public void BindInterfacesAndSelfTo<T>(int identifier = 0, ArgumentArray arguments = default, bool nonLazy = false)
        {
            BindInterfacesAndSelfTo(typeof(T), identifier, arguments, nonLazy);
        }

        public void BindInterfacesAndSelfTo(object instance, int identifier = 0)
        {
            RegisterProvider(new BindInfo
            {
                ConcreteType = instance.GetType(),
                Identifier = identifier,
                BindConcreteType = true,
                BindInterfaces = true,
            }, instance);
        }
    }
}