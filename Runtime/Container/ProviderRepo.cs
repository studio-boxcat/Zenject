using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Assertions;

namespace Zenject
{
    public delegate object ProvideDelegate(DiContainer container, Type concreteType, ArgumentArray extraArguments);


    readonly struct ProviderRepo
    {
        readonly DiContainer _container;
        readonly List<ProviderInfo> _providers;
        readonly Dictionary<BindPath, int> _primaryProviderMap;
#if DEBUG
        readonly HashSet<int> _resolvesInProgress;
#endif

        public ProviderRepo(DiContainer container, int capacity)
        {
            _container = container;
            _providers = new List<ProviderInfo>(capacity);
            _primaryProviderMap = new Dictionary<BindPath, int>(capacity, BindPath.Comparer);
#if DEBUG
            _resolvesInProgress = new HashSet<int>();
#endif
        }

        public int Register(TypeArray contractTypes, BindId identifier, ProvideDelegate provider, Type concreteType, ArgumentArray extraArguments)
        {
            var providerIndex = _providers.Count;

            _providers.Add(new ProviderInfo(contractTypes, identifier, provider, concreteType, extraArguments));

            foreach (var contractType in contractTypes)
            {
                var bindingId = new BindPath(contractType, identifier);
                _primaryProviderMap.TryAdd(bindingId, providerIndex);
            }

            return providerIndex;
        }

        public int Register(TypeArray contractTypes, BindId identifier, object instance)
        {
            var providerIndex = _providers.Count;

            _providers.Add(new ProviderInfo(contractTypes, identifier, instance));

            foreach (var contractType in contractTypes)
            {
                var bindingId = new BindPath(contractType, identifier);
                _primaryProviderMap.TryAdd(bindingId, providerIndex);
            }

            return providerIndex;
        }

        public bool HasBinding(BindPath bindPath)
        {
            return _primaryProviderMap.ContainsKey(bindPath);
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

        public bool TryResolve(BindPath bindPath, out object instance)
        {
            if (_primaryProviderMap.TryGetValue(bindPath, out var providerIndex) is false)
            {
                instance = default;
                return false;
            }

            instance = Resolve(providerIndex);
            return true;
        }

        public void ResolveAll(BindPath bindPath, IList buffer)
        {
            var (type, identifier) = bindPath;
            Assert.IsNotNull(type);

            for (var index = 0; index < _providers.Count; index++)
            {
                var providerInfo = _providers[index];

                // identifier == 0 means resolve all.
                if (identifier != 0 && providerInfo.Identifier != identifier)
                    continue;

                if (providerInfo.ContractTypes.Contains(type) == false)
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

        [Conditional("DEBUG")]
        void MarkResolvesInProgress(int providerIndex)
        {
#if DEBUG
            if (!_resolvesInProgress.Add(providerIndex))
                throw new Exception("Circular dependency detected! " + _providers[providerIndex].ContractTypes[0].Name);
#endif
        }

        [Conditional("DEBUG")]
        void UnmarkResolvesInProgress(int providerIndex)
        {
#if DEBUG
            var removed = _resolvesInProgress.Remove(providerIndex);
            Assert.IsTrue(removed);
#endif
        }

        readonly struct ProviderInfo
        {
            public readonly TypeArray ContractTypes;
            public readonly BindId Identifier;

            public readonly ProvideDelegate Provider;
            public readonly Type ConcreteType;
            public readonly ArgumentArray Arguments;
            public readonly bool HasInstance;
            public readonly object Instance;

            public ProviderInfo(TypeArray contractTypes, BindId identifier, ProvideDelegate provider, Type concreteType, ArgumentArray arguments) : this()
            {
                ContractTypes = contractTypes;
                Identifier = identifier;

                Provider = provider;
                ConcreteType = concreteType;
                Arguments = arguments;
            }

            public ProviderInfo(TypeArray contractTypes, BindId identifier, object instance) : this()
            {
                ContractTypes = contractTypes;
                Identifier = identifier;

                HasInstance = true;
                Instance = instance;
            }
        }
    }
}