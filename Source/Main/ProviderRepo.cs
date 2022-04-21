using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Assertions;

namespace Zenject
{
    public delegate object ProvideDelegate(DiContainer container, Type concreteType, ArgumentArray extraArguments);


    public class ProviderRepo
    {
        readonly DiContainer _container;
        readonly List<ProviderInfo> _providers = new(128);
        readonly Dictionary<BindingId, int> _primaryProviderMap = new(128, BindingId.Comparer);


        public ProviderRepo(DiContainer container)
        {
            _container = container;
        }

        public int Register(TypeArray contractTypes, int identifier, ProvideDelegate provider, Type concreteType, ArgumentArray extraArguments)
        {
            var providerIndex = _providers.Count;

            _providers.Add(new ProviderInfo(contractTypes, identifier, provider, concreteType, extraArguments));

            foreach (var contractType in contractTypes)
            {
                var bindingId = new BindingId(contractType, identifier);
                if (_primaryProviderMap.ContainsKey(bindingId) == false)
                    _primaryProviderMap.Add(bindingId, providerIndex);
            }

            return providerIndex;
        }

        public int Register(TypeArray contractTypes, int identifier, object instance)
        {
            var providerIndex = _providers.Count;

            _providers.Add(new ProviderInfo(contractTypes, identifier, instance));

            foreach (var contractType in contractTypes)
            {
                var bindingId = new BindingId(contractType, identifier);
                if (_primaryProviderMap.ContainsKey(bindingId) == false)
                    _primaryProviderMap.Add(bindingId, providerIndex);
            }

            return providerIndex;
        }

        public bool HasBinding(BindingId bindingId)
        {
            return _primaryProviderMap.ContainsKey(bindingId);
        }

        public object Resolve(int providerIndex)
        {
            var providerInfo = _providers[providerIndex];

            if (providerInfo.HasInstance)
                return providerInfo.Instance;

            MarkResolvesInProgress(providerIndex);
            var instance = providerInfo.Provider(_container, providerInfo.ConcreteType, providerInfo.Arguments);
            UnmarkResolvesInProgress(providerIndex);
            providerInfo = new ProviderInfo(providerInfo.ContractTypes, providerInfo.Identifier, instance);
            _providers[providerIndex] = providerInfo;
            return instance;
        }

        public bool TryResolve(BindingId bindingId, out object instance)
        {
            if (_primaryProviderMap.TryGetValue(bindingId, out var providerIndex) == false)
            {
                instance = default;
                return false;
            }

            instance = Resolve(providerIndex);
            return true;
        }

        public void ResolveAll(BindingId bindingId, IList buffer)
        {
            for (var index = 0; index < _providers.Count; index++)
            {
                var providerInfo = _providers[index];

                if (providerInfo.Identifier != bindingId.Identifier)
                    continue;

                if (providerInfo.ContractTypes.Contains(bindingId.Type) == false)
                    continue;

                if (providerInfo.HasInstance == false)
                {
                    MarkResolvesInProgress(index);
                    var instance = providerInfo.Provider(_container, providerInfo.ConcreteType, providerInfo.Arguments);
                    UnmarkResolvesInProgress(index);
                    providerInfo = new ProviderInfo(providerInfo.ContractTypes, providerInfo.Identifier, instance);
                    _providers[index] = providerInfo;
                }

                buffer.Add(providerInfo.Instance);
            }
        }

#if DEBUG
        readonly HashSet<int> _resolvesInProgress = new();
#endif

        [Conditional("DEBUG")]
        public void MarkResolvesInProgress(int providerIndex)
        {
            if (!_resolvesInProgress.Add(providerIndex))
                throw new Exception("Circular dependency detected!");
        }

        [Conditional("DEBUG")]
        public void UnmarkResolvesInProgress(int providerIndex)
        {
            var removed = _resolvesInProgress.Remove(providerIndex);
            Assert.IsTrue(removed);
        }

        readonly struct ProviderInfo
        {
            public readonly TypeArray ContractTypes;
            public readonly int Identifier;

            public readonly ProvideDelegate Provider;
            public readonly Type ConcreteType;
            public readonly ArgumentArray Arguments;
            public readonly bool HasInstance;
            public readonly object Instance;

            public ProviderInfo(TypeArray contractTypes, int identifier, ProvideDelegate provider, Type concreteType, ArgumentArray arguments) : this()
            {
                ContractTypes = contractTypes;
                Identifier = identifier;

                Provider = provider;
                ConcreteType = concreteType;
                Arguments = arguments;
            }

            public ProviderInfo(TypeArray contractTypes, int identifier, object instance) : this()
            {
                ContractTypes = contractTypes;
                Identifier = identifier;

                HasInstance = true;
                Instance = instance;
            }
        }
    }
}