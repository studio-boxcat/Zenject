using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Assertions;

namespace Zenject
{
    public struct ProviderProxy
    {
        readonly object _initialCache;

        readonly ProviderRepo _providerRepo;
        readonly TypeArray _contractTypes;
        readonly int _identifier;
        readonly IProvider _provider;
        readonly int _providerIndex;

        public ProviderProxy(object initialCache) : this()
        {
            _initialCache = initialCache;
        }

        public ProviderProxy(ProviderRepo providerRepo, IProvider provider, int providerIndex) : this()
        {
            _providerRepo = providerRepo;
            _provider = provider;
            _providerIndex = providerIndex;
        }

        public object GetInstance()
        {
            if (ReferenceEquals(_initialCache, null) == false)
                return _initialCache;

            if (_providerRepo.TryGetCachedInstance(_providerIndex, out var instance))
                return instance;

            _providerRepo.MarkResolvesInProgress(_providerIndex);
            instance = _provider.GetInstance();
            _providerRepo.UnmarkResolvesInProgress(_providerIndex);

            _providerRepo.CacheInstance(_providerIndex, instance);
            return instance;
        }
    }

    public class ProviderRepo
    {
        readonly List<ProviderInfo> _providers = new(128);
        readonly Dictionary<BindingId, int> _primaryProviderMap = new(128, BindingId.Comparer);
        readonly HashSet<int> _resolvesInProgress = new();


        public int Register(IProvider provider, TypeArray contractTypes, int identifier)
        {
            var providerIndex = _providers.Count;

            _providers.Add(new ProviderInfo(contractTypes, identifier, provider));

            foreach (var contractType in contractTypes)
            {
                var bindingId = new BindingId(contractType, identifier);
                if (_primaryProviderMap.ContainsKey(bindingId) == false)
                    _primaryProviderMap.Add(bindingId, providerIndex);
            }

            return providerIndex;
        }

        public int Register(object instance, TypeArray contractTypes, int identifier)
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

        public bool TryGetCachedInstance(int providerIndex, out object instance)
        {
            var providerInfo = _providers[providerIndex];

            if (providerInfo.HasInstance)
            {
                instance = providerInfo.Instance;
                return true;
            }
            else
            {
                instance = default;
                return false;
            }
        }

        public void CacheInstance(int providerIndex, object instance)
        {
            var providerInfo = _providers[providerIndex];
            Assert.IsFalse(providerInfo.HasInstance);
            var newProviderInfo = new ProviderInfo(providerInfo.ContractTypes, providerInfo.Identifier, instance);
            _providers[providerIndex] = newProviderInfo;
        }

        public object Resolve(int providerIndex)
        {
            var providerInfo = _providers[providerIndex];

            if (providerInfo.HasInstance)
                return providerInfo.Instance;

            var instance = providerInfo.Provider.GetInstance();
            var newProviderInfo = new ProviderInfo(providerInfo.ContractTypes, providerInfo.Identifier, instance);
            _providers[providerIndex] = newProviderInfo;
            return instance;
        }

        public bool TryGetFirstMatchingProvider(BindingId bindingId, out ProviderProxy provider)
        {
            for (var index = 0; index < _providers.Count; index++)
            {
                var providerInfo = _providers[index];

                if (providerInfo.Identifier != bindingId.Identifier)
                    continue;

                if (providerInfo.ContractTypes.Contains(bindingId.Type) == false)
                    continue;

                provider = providerInfo.HasInstance
                    ? new ProviderProxy(providerInfo.Instance)
                    : new ProviderProxy(this, providerInfo.Provider, index);
                return true;
            }

            provider = default;
            return false;
        }

        public void GetMatchingProviders(BindingId bindingId, List<ProviderProxy> buffer)
        {
            for (var index = 0; index < _providers.Count; index++)
            {
                var providerInfo = _providers[index];

                if (providerInfo.Identifier != bindingId.Identifier)
                    continue;

                if (providerInfo.ContractTypes.Contains(bindingId.Type) == false)
                    continue;

                buffer.Add(providerInfo.HasInstance
                    ? new ProviderProxy(providerInfo.Instance)
                    : new ProviderProxy(this, providerInfo.Provider, index));
            }
        }

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

            public readonly IProvider Provider;
            public readonly bool HasInstance;
            public readonly object Instance;

            public ProviderInfo(TypeArray contractTypes, int identifier, IProvider provider) : this()
            {
                ContractTypes = contractTypes;
                Identifier = identifier;

                Provider = provider;
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