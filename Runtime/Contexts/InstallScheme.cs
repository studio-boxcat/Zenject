#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine.Assertions;

namespace Zenject
{
    public delegate object ProvideDelegate(DiContainer container, Type concreteType, ArgumentArray extraArguments);

    public partial class InstallScheme
    {
        private Binding[] _bindings;
        private int _bindingPtr;
        private readonly Dictionary<ulong, Payload> _payloads;
        private readonly KernelServices _kernelServices;

        public InstallScheme(int capacity = 16)
        {
            _bindings = new Binding[capacity];
            _payloads = new Dictionary<ulong, Payload>(capacity / 4);
            _kernelServices = KernelServices.Create(capacity / 4);
        }

        public override string ToString()
        {
            return "[" + string.Join(", ", _bindings.Take(_bindingPtr)) + "]";
        }

        private void AddBinding(Binding binding)
        {
            Assert.IsTrue(binding.Key is not 0, "Binding key must not be zero.");
            Assert.IsTrue(binding.Key != BindKey.GetSelfBindKey(), "Binding must not be self-referential.");

            if (_bindingPtr == _bindings.Length)
            {
                L.W($"Binding array is full: {_bindings.Length}\n{this}");
                Array.Resize(ref _bindings, _bindings.Length * 2);
            }

            _bindings[_bindingPtr++] = binding;
        }

        // Instance version.
        public void Bind(Type contractType, object instance, BindId id = default)
        {
            // L.I($"Binding: {contractType}:{id}");

#if DEBUG
            Assert.IsTrue(instance as UnityEngine.Object ?? instance is not null, "Instance must not be null");
            Assert.IsTrue(contractType.IsInstanceOfType(instance), $"ContractType must be assignable from instance type");
            Assert.IsFalse((id == default) && (contractType == typeof(object)), "ContractType is too general. Use a more specific type.");
            Assert.IsFalse((id == default) && (contractType == typeof(Object)), "ContractType is too general. Use a more specific type.");
            Assert.IsFalse(DebugHasBinding(contractType, id), $"Binding already exists: {contractType}:{id}");
#endif

            var bindKey = Hash(contractType, id);
            AddBinding(new Binding(bindKey, instance));
            if (instance is IDisposable disposable) _kernelServices.Disposables1.Add(disposable);
            if (instance is ITickable tickable) _kernelServices.Tickables1.Add(tickable);
        }

        public void Bind<TContractType>(TContractType instance, BindId id = default)
        {
            Bind(typeof(TContractType), instance!, id);
        }

        // ProvideDelegate version.
        public void Bind(
            Type contractType, Type concreteType,
            BindId identifier = default,
            ProvideDelegate? provider = null, ArgumentArray arguments = default,
            bool disposable = false, bool tickable = false)
        {
            // L.I($"Binding: {contractType}:{identifier} → {concreteType}");

#if DEBUG
            Assert.IsTrue(contractType.IsAssignableFrom(concreteType), "ContractType must be assignable from ConcreteType");
            Assert.AreEqual(typeof(IDisposable).IsAssignableFrom(concreteType), disposable, "ConcreteType must implement IDisposable: " + concreteType);
            Assert.AreEqual(typeof(ITickable).IsAssignableFrom(concreteType), tickable, "ConcreteType must implement ITickable: " + concreteType);
            Assert.IsFalse(DebugHasBinding(contractType, identifier), $"Binding already exists: {contractType}:{identifier}");
#endif

            var bindKey = Hash(contractType, identifier);
            var payload = provider is not null || arguments.Any();
            AddBinding(new Binding(bindKey, concreteType, payload));
            if (provider is not null || arguments.Any())
                _payloads.Add(bindKey, new Payload(provider, arguments));
            if (disposable) _kernelServices.Disposables2.Add(bindKey);
            if (tickable) _kernelServices.Tickables2.Add(bindKey);
        }

        public void Bind<TContract>(BindId identifier = 0,
            ProvideDelegate? provider = null, ArgumentArray arguments = default,
            bool disposable = false, bool tickable = false)
        {
            var t = typeof(TContract);
            Bind(t, t, identifier, provider, arguments, disposable, tickable);
        }

        public void Bind<TContract, TConcrete>(BindId identifier = 0,
            ProvideDelegate? provider = null, ArgumentArray arguments = default,
            bool disposable = false, bool tickable = false)
            where TConcrete : TContract
        {
            Assert.AreNotEqual(typeof(TContract), typeof(TConcrete), "Use Bind<TContract> instead.");
            Bind(typeof(TContract), typeof(TConcrete), identifier, provider, arguments, disposable, tickable);
        }

        internal DiContainer Start(DiContainer? parent) => new(parent, _payloads);
        internal void Update(DiContainer container) => container.InternalUpdateBindings(_bindings, _bindingPtr);
        internal void End(DiContainer container, out Kernel kernel)
        {
            L.I($"Building DiContainer: bindings={_bindingPtr}, payloads={_payloads.Count}\n" +
                "[" + string.Join(',', _bindings.Take(_bindingPtr).Select(b => b.ToString())) + "]");
            Update(container);
            _kernelServices.ResolveAll(container);
            kernel = new Kernel(_kernelServices.Disposables1, _kernelServices.Tickables1);
        }

        internal DiContainer Build(out Kernel kernal)
        {
            var container = Start(null);
            Update(container);
            End(container, out kernal);
            return container;
        }

#if DEBUG
        [IgnoredByDeepProfiler]
        private bool DebugHasBinding(ulong bindKey) => _bindings.Any(b => b.Key == bindKey);
        [IgnoredByDeepProfiler]
        public bool DebugHasBinding(Type type, BindId id = default) => DebugHasBinding(Hash(type, id));
        [IgnoredByDeepProfiler]
        public bool DebugHasBinding<TContractType>(BindId id = default) => DebugHasBinding(typeof(TContractType), id);
#endif

        private static ulong Hash(Type type, BindId id) => BindKey.Hash(type, id);
    }
}