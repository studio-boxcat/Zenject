using System.Collections;
using JetBrains.Annotations;

namespace Zenject
{
    public readonly struct ProviderChain
    {
        readonly ProviderRepo[] _containerChain;

        public ProviderChain(DiContainer container)
        {
            _containerChain = BuildProviderChain(container);
        }

        static ProviderRepo[] BuildProviderChain(DiContainer root)
        {
            var containerCount = 1;
            var targetContainer = root.ParentContainer;
            while (targetContainer != null)
            {
                targetContainer = targetContainer.ParentContainer;
                containerCount++;
            }

            var providerChain = new ProviderRepo[containerCount];
            providerChain[0] = root.ProviderRepo;
            var pointer = 1;
            targetContainer = root.ParentContainer;
            while (targetContainer != null)
            {
                providerChain[pointer++] = targetContainer.ProviderRepo;
                targetContainer = targetContainer.ParentContainer;
            }

            return providerChain;
        }

        [Pure]
        public bool HasBinding(BindingId bindingId)
        {
            foreach (var currentContainer in _containerChain)
            {
                if (currentContainer.HasBinding(bindingId))
                    return true;
            }

            return false;
        }

        public void ResolveAll(BindingId bindingId, IList buffer)
        {
            foreach (var container in _containerChain)
                container.ResolveAll(bindingId, buffer);
        }

        [Pure]
        public bool TryResolve(BindingId bindingId, out object instance)
        {
            foreach (var container in _containerChain)
            {
                if (container.TryResolve(bindingId, out instance))
                    return true;
            }

            instance = default;
            return false;
        }
    }
}