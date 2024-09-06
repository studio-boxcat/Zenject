using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    [HideReferenceObjectPicker]
    public partial class DiContainer
    {
        Binding[] _bindings;
        int _bindingCount;
        Dictionary<ulong, Payload> _payloads;
        [ShowInInspector, ReadOnly, CanBeNull]
        DiContainer _parent;

        internal void Initialize(
            Binding[] bindings,
            int bindingCount,
            Dictionary<ulong, Payload> payloads,
            [CanBeNull] DiContainer parent)
        {
            Binding.Validate(bindings, bindingCount);

            _bindings = bindings;
            _bindingCount = bindingCount;
            _payloads = payloads;
            _parent = parent;
        }

        public void Inject(object injectable, ArgumentArray extraArgs)
        {
            Injector.Inject(injectable, this, extraArgs);
        }

        public bool TryResolve(ulong bindKey, out object instance)
        {
            var found = Binding.BinarySearch(_bindings, _bindingCount, bindKey, out var index);

            // Check parent container.
            if (found is false)
            {
                if (_parent is not null)
                    return _parent.TryResolve(bindKey, out instance);
                instance = default;
                return false;
            }

            // Found a binding.
            ref var binding = ref _bindings[index];
            Assert.IsNotNull(binding.Value, "Circular dependency detected: " + BindKey.ToString(bindKey));

            if (binding.Value is not Type concreteType)
            {
                instance = binding.Value;
                return true;
            }

#if DEBUG
            binding.Value = null; // Prevent circular dependencies.
#endif

            // Materialize.
            instance = Materialize(binding, this, concreteType, _payloads);
            binding.Value = instance;
            binding.Payload = false;
            return true;


            static object Materialize(Binding binding, DiContainer diContainer, Type concreteType, Dictionary<ulong, Payload> payloads)
            {
                if (binding.Payload is false)
                    return diContainer.Instantiate(concreteType);

                var hasPayload = payloads.Remove(binding.Key, out var payload);
                Assert.IsTrue(hasPayload, "Payload not found for binding: " + BindKey.ToString(binding.Key));
                return payload.Provider is null
                    ? diContainer.Instantiate(concreteType, payload.Arguments)
                    : payload.Provider(diContainer, concreteType, payload.Arguments);
            }
        }

        public bool TryResolve(Type type, BindId identifier, out object instance)
        {
            Assert.IsFalse(type.IsArray, "Array types are not supported here.");
            var bindKey = Hash(type, identifier);
            return TryResolve(bindKey, out instance);
        }

        public bool TryResolve<TContract>(BindId identifier, out TContract instance)
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

        public bool TryResolve<TContract>(out TContract instance)
        {
            return TryResolve(0, out instance);
        }

        public object Resolve(ulong bindKey)
        {
            if (TryResolve(bindKey, out var instance))
            {
                return instance;
            }
            else
            {
                throw new Exception($"Failed to Resolve: {BindKey.ToString(bindKey)}");
            }
        }

        public object Resolve(Type contractType, BindId id = 0)
        {
            return Resolve(Hash(contractType, id));
        }

        public TContract Resolve<TContract>()
        {
            return (TContract) Resolve(typeof(TContract));
        }

        public TContract Resolve<TContract>(BindId id)
        {
            return (TContract) Resolve(typeof(TContract), id);
        }

        internal object Resolve(InjectSpec injectSpec)
        {
            if (TryResolve(injectSpec.Type, injectSpec.Id, out var instance))
            {
                return instance;
            }

            if (injectSpec.Optional is false)
            {
                throw new Exception($"Failed to Resolve: {injectSpec.Type.Name}:{injectSpec.Id}");
            }

            return null;
        }

        public T Instantiate<T>(ArgumentArray extraArgs = default)
        {
            return (T) Instantiate(typeof(T), extraArgs);
        }

        public object Instantiate(Type concreteType, ArgumentArray extraArgs = default)
        {
            Assert.IsFalse(concreteType.IsSubclassOf(typeof(Component)),
                $"'{concreteType.Name}' is a component.  Use InstantiateComponent instead.");
            Assert.IsFalse(concreteType.IsAbstract,
                $"'{concreteType.Name}' should be non-abstract");

            var newObj = Constructor.Instantiate(concreteType, this, extraArgs);
            Assert.IsTrue(newObj.GetType() == concreteType);
            Injector.Inject(newObj, this, extraArgs);
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

        public GameObject InstantiatePrefabDeactivated(GameObject prefab, Transform parent, ArgumentArray extraArgs = default)
        {
            L.I($"Instantiating prefab '{prefab.name}'", prefab);

            Assert.IsNotNull(prefab, "Null prefab found when instantiating game object");

            var orgActive = prefab.activeSelf;
            prefab.SetActive(false);
            var inst = Object.Instantiate(prefab, parent, false);
            prefab.SetActive(orgActive);

            if (GameObjectContext.TryInject(inst, this, extraArgs) is false)
                InjectTargetCollection.TryInject(inst, this, extraArgs);
            return inst;
        }

        public GameObject InstantiatePrefab(GameObject prefab, Transform parent, ArgumentArray extraArgs = default)
        {
            var inst = InstantiatePrefabDeactivated(prefab, parent, extraArgs);
            inst.SetActive(true);
            return inst;
        }

        static ulong Hash(Type type, BindId id) => BindKey.Hash(type, id);
    }
}