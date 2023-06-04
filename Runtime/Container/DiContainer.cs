using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
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
        [CanBeNull] public readonly DiContainer Parent;

        public readonly ProviderRepo ProviderRepo;

        readonly ProviderChain _providerChain;
        readonly List<int> _nonLazyProviders = new();

        public DiContainer(
            [CanBeNull] DiContainer parent, int capacity)
        {
            Parent = parent;
            ProviderRepo = new ProviderRepo(this, capacity);
            _providerChain = parent is not null
                ? new ProviderChain(Parent._providerChain, ProviderRepo)
                : new ProviderChain(ProviderRepo);
            Bind(this);
        }

        public void ResolveNonLazyProviders()
        {
            foreach (var providerIndex in _nonLazyProviders)
                ProviderRepo.Resolve(providerIndex);
            _nonLazyProviders.Clear();
        }

        public void RegisterProvider(BindSpec bindSpec, object instance)
        {
            var contractTypes = bindSpec.BakeContractTypes();
            var identifier = bindSpec.Identifier;
            ProviderRepo.Register(contractTypes, identifier, instance);
        }

        public void RegisterProvider(BindSpec bindSpec, ProvideDelegate provider, ArgumentArray extraArgument, bool nonLazy)
        {
            var contractTypes = bindSpec.BakeContractTypes();
            var identifier = bindSpec.Identifier;

            provider ??= (container, concreteType, args) => container.Instantiate(concreteType, args);
            var providerIndex = ProviderRepo.Register(contractTypes, identifier, provider, bindSpec.PrimaryType, extraArgument);
            if (nonLazy) _nonLazyProviders.Add(providerIndex);
        }

        public bool HasBinding(Type type, int identifier = 0)
        {
            return HasBinding(new InjectSpec(type, identifier));
        }

        // You shouldn't need to use this
        public bool HasBinding(InjectSpec injectSpec)
        {
            return _providerChain.HasBinding(injectSpec.BindingId);
        }

        public void Bind(Type type, int identifier = 0, ProvideDelegate provider = null, ArgumentArray arguments = default, bool nonLazy = false)
        {
            RegisterProvider(new BindSpec
            {
                PrimaryType = type,
                Identifier = identifier,
                BindFlag = BindFlag.Primary,
            }, provider, arguments, nonLazy);
        }

        public void Bind<TContract>(int identifier = 0, ProvideDelegate provider = null, ArgumentArray arguments = default, bool nonLazy = false)
        {
            Bind(typeof(TContract), identifier, provider, arguments, nonLazy);
        }

        public void Bind(object instance)
        {
            Assert.IsNotNull(instance);

            RegisterProvider(new BindSpec
            {
                PrimaryType = instance.GetType(),
                BindFlag = BindFlag.Primary,
            }, instance);
        }

        public void Bind(Type type, object instance)
        {
            Assert.IsNotNull(instance);
            Assert.IsTrue(type.IsInstanceOfType(instance));

            RegisterProvider(new BindSpec
            {
                PrimaryType = type,
                BindFlag = BindFlag.Primary,
            }, instance);
        }

        public void Bind(object instance, int id)
        {
            RegisterProvider(new BindSpec
            {
                PrimaryType = instance.GetType(),
                Identifier = id,
                BindFlag = BindFlag.Primary,
            }, instance);
        }

        public void Bind(object instance, string id)
        {
            RegisterProvider(new BindSpec
            {
                PrimaryType = instance.GetType(),
                Identifier = Hasher.Hash(id),
                BindFlag = BindFlag.Primary,
            }, instance);
        }

        public void Bind(Type type, string id, ProvideDelegate provider, ArgumentArray arguments = default, bool nonLazy = false)
        {
            RegisterProvider(new BindSpec
            {
                PrimaryType = type,
                Identifier = Hasher.Hash(id),
                BindFlag = BindFlag.Primary,
            }, provider, arguments, nonLazy);
        }

        public void BindInterfacesTo(Type type, int identifier = 0, ProvideDelegate provider = null, ArgumentArray arguments = default, bool nonLazy = false)
        {
            RegisterProvider(new BindSpec
            {
                PrimaryType = type,
                Identifier = identifier,
                BindFlag = BindFlag.Interfaces,
            }, provider, arguments, nonLazy);
        }

        public void BindInterfacesTo<T>(int identifier = 0, ProvideDelegate provider = null, ArgumentArray arguments = default, bool nonLazy = false)
        {
            BindInterfacesTo(typeof(T), identifier, provider, arguments, nonLazy);
        }

        public void BindInterfacesTo(object instance, int identifier = 0)
        {
            RegisterProvider(new BindSpec
            {
                PrimaryType = instance.GetType(),
                Identifier = identifier,
                BindFlag = BindFlag.Interfaces,
            }, instance);
        }

        public void BindInterfacesAndSelfTo(Type type, int identifier = 0, ArgumentArray arguments = default, bool nonLazy = false)
        {
            RegisterProvider(new BindSpec
            {
                PrimaryType = type,
                Identifier = identifier,
                BindFlag = BindFlag.PrimaryAndInterfaces,
            }, null, arguments, nonLazy: nonLazy);
        }

        public void BindInterfacesAndSelfTo<T>(int identifier = 0, ArgumentArray arguments = default, bool nonLazy = false)
        {
            BindInterfacesAndSelfTo(typeof(T), identifier, arguments, nonLazy);
        }

        public void BindInterfacesAndSelfTo(object instance, int identifier = 0)
        {
            RegisterProvider(new BindSpec
            {
                PrimaryType = instance.GetType(),
                Identifier = identifier,
                BindFlag = BindFlag.PrimaryAndInterfaces,
            }, instance);
        }

        public void Inject(object injectable, ArgumentArray extraArgs)
        {
            Initializer.Initialize(injectable, this, extraArgs);
        }

        public bool TryResolve(Type type, int identifier, out object instance)
        {
            if (type.IsArray)
            {
                instance = ResolveAll(type, identifier);
                return true;
            }

            return _providerChain.TryResolve(new BindingId(type, identifier), out instance);
        }

        public bool TryResolve<TContract>(int identifier, out TContract instance)
        {
            if (TryResolve(typeof(TContract), identifier, out var instance2))
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
            return TryResolve(type, 0, out instance);
        }

        public bool TryResolve<TContract>(string identifier, out TContract instance)
        {
            return TryResolve(Hasher.Hash(identifier), out instance);
        }

        public bool TryResolve<TContract>(out TContract instance)
        {
            return TryResolve(0, out instance);
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
            return Resolve<TContract>(Hasher.Hash(identifier));
        }

        public object Resolve(Type contractType, int identifier = 0)
        {
            if (TryResolve(contractType, identifier, out var instance))
            {
                return instance;
            }
            else
            {
                throw new Exception($"Failed to Resolve: {contractType.Name}, {Hasher.ToHumanReadableString(identifier)}");
            }
        }

        public object Resolve(InjectSpec injectSpec)
        {
            if (TryResolve(injectSpec.Type, injectSpec.Identifier, out var instance))
            {
                return instance;
            }

            if (injectSpec.Optional == false)
            {
                throw new Exception($"Failed to Resolve: {injectSpec.Type.Name}, {Hasher.ToHumanReadableString(injectSpec.Identifier)}");
            }

            return null;
        }

        static readonly Stack<List<object>> _listPool = new();

        public Array ResolveAll(Type arrayType, int identifier)
        {
            // XXX: IsClassType 으로 체크하는 경우, interface 가 false 로 취급됨.
            Assert.IsTrue(arrayType.IsArray && arrayType.GetElementType()!.IsValueType == false);

            if (_listPool.TryPop(out var list) == false)
                list = new List<object>();
            Assert.AreEqual(0, list.Count);

            var elementType = arrayType.GetElementType()!;
            _providerChain.ResolveAll(new BindingId(elementType, identifier), list);

            var result = Array.CreateInstance(elementType, list.Count);
            for (var i = 0; i < list.Count; i++)
                result.SetValue(list[i], i);

            list.Clear();
            _listPool.Push(list);
            return result;
        }

        public void ResolveAll<T>(int identifier, IList<T> instances)
        {
            _providerChain.ResolveAll(new BindingId(typeof(T), identifier), (IList) instances);
        }

        public T Instantiate<T>(ArgumentArray extraArgs = default)
        {
            return (T) Instantiate(typeof(T), extraArgs);
        }

        public object Instantiate(Type concreteType, ArgumentArray extraArgs = default)
        {
            Assert.IsFalse(concreteType.IsSubclassOf(typeof(Component)),
                $"'{concreteType.PrettyName()}' is a component.  Use InstantiateComponent instead.");
            Assert.IsFalse(concreteType.IsAbstract,
                $"'{concreteType.PrettyName()}' should be non-abstract");

            var newObj = Constructor.Instantiate(concreteType, this, extraArgs);
            Assert.IsTrue(newObj.GetType() == concreteType);
            Initializer.Initialize(newObj, this, extraArgs);
            return newObj;
        }

        public TContract InstantiateComponent<TContract>(
            GameObject gameObject, ArgumentArray extraArgs = default)
            where TContract : Component
        {
            return (TContract) InstantiateComponent(typeof(TContract), gameObject, extraArgs);
        }

        public Component InstantiateComponent(
            Type componentType, GameObject gameObject, ArgumentArray extraArgs = default)
        {
            Assert.IsTrue(componentType.IsSubclassOf(typeof(Component)));

            var monoBehaviour = gameObject.AddComponent(componentType);
            Inject(monoBehaviour, extraArgs);
            return monoBehaviour;
        }

        public GameObject InstantiatePrefab(GameObject prefab, Transform parent, ArgumentArray extraArgs = default)
        {
            Assert.IsNotNull(prefab, "Null prefab found when instantiating game object");

            var orgActive = prefab.activeSelf;
            prefab.SetActive(false);

            var inst = Object.Instantiate(prefab, parent, false);
            if (GameObjectContext.TryInject(inst, this, extraArgs) == false)
                InjectTargetCollection.TryInject(inst, this, extraArgs);

            prefab.SetActive(orgActive);
            inst.SetActive(orgActive);
            return inst;
        }
    }
}