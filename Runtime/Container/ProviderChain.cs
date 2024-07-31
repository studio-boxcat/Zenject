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
        public bool HasBinding(BindingId bindingId)
        {
            foreach (var currentContainer in _chain)
            {
                if (currentContainer.HasBinding(bindingId))
                    return true;
            }

            return false;
        }

        public void ResolveAll(BindingId bindingId, IList buffer)
        {
            foreach (var container in _chain)
                container.ResolveAll(bindingId, buffer);
        }

        [Pure]
        public bool TryResolve(BindingId bindingId, out object instance)
        {
            foreach (var container in _chain)
            {
                if (container.TryResolve(bindingId, out instance))
                    return true;
            }

            instance = default;
            return false;
        }
    }
}