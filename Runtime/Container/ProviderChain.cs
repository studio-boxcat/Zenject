using System;
using System.Collections;
using JetBrains.Annotations;

namespace Zenject
{
    readonly struct ProviderChain
    {
        readonly ProviderRepo[] _chain;

        public ProviderChain(ProviderRepo provider)
        {
            _chain = new ProviderRepo[1];
            _chain[0] = provider;
        }

        public ProviderChain(ProviderChain chain, ProviderRepo provider)
        {
            var baseChain = chain._chain;
            _chain = new ProviderRepo[baseChain.Length + 1];
            _chain[0] = provider;
            Array.Copy(baseChain, 0, _chain, 1, baseChain.Length);
        }

        [Pure]
        public bool HasBinding(BindPath bindPath)
        {
            foreach (var repo in _chain)
            {
                if (repo.HasBinding(bindPath))
                    return true;
            }

            return false;
        }

        public void ResolveAll(BindPath bindPath, IList result)
        {
            foreach (var repo in _chain)
                repo.ResolveAll(bindPath, result);
        }

        [Pure]
        public bool TryResolve(BindPath bindPath, out object instance)
        {
            foreach (var repo in _chain)
            {
                if (repo.TryResolve(bindPath, out instance))
                    return true;
            }

            instance = default;
            return false;
        }
    }
}