using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Zenject
{
    public struct DiContainerChain
    {
        readonly DiContainer _self;
        readonly DiContainer _parent;
        readonly DiContainer[] _containerChain;

        public DiContainerChain(DiContainer container)
        {
            _self = container;
            _parent = container.ParentContainer;
            _containerChain = BuildContainerChain(container);
        }

        static DiContainer[] BuildContainerChain(DiContainer root)
        {
            var containerCount = 1;
            var targetContainer = root.ParentContainer;
            while (targetContainer != null)
            {
                targetContainer = targetContainer.ParentContainer;
                containerCount++;
            }

            var containerChain = new DiContainer[containerCount];
            containerChain[0] = root;
            var pointer = 1;
            targetContainer = root.ParentContainer;
            while (targetContainer != null)
            {
                containerChain[pointer++] = targetContainer;
                targetContainer = targetContainer.ParentContainer;
            }

            return containerChain;
        }

        public readonly void GetMatchingProviders(BindingId bindingId, InjectSources sourceType, List<ProviderProxy> buffer)
        {
            if (sourceType == InjectSources.Local)
            {
                Internal_GetMatchingProviders(_self, bindingId, buffer);
                return;
            }

            if (sourceType == InjectSources.Parent)
            {
                Internal_GetMatchingProviders(_parent, bindingId, buffer);
                return;
            }

            foreach (var container in _containerChain)
                Internal_GetMatchingProviders(container, bindingId, buffer);

            static void Internal_GetMatchingProviders(DiContainer container, BindingId bindingId, List<ProviderProxy> buffer1)
            {
                container.FlushBindings();
                container.ProviderRepo.GetMatchingProviders(bindingId, buffer1);
            }
        }

        [Pure]
        public bool TryGetFirstProvider(BindingId bindingId, InjectSources sourceType, out ProviderProxy provider)
        {
            if (sourceType == InjectSources.Local)
                return Internal_TryGetFirstProvider(_self, bindingId, out provider);

            if (sourceType == InjectSources.Parent)
                return Internal_TryGetFirstProvider(_parent, bindingId, out provider);

            foreach (var container in _containerChain)
            {
                if (Internal_TryGetFirstProvider(container, bindingId, out provider))
                    return true;
            }

            provider = default;
            return false;

            static bool Internal_TryGetFirstProvider(DiContainer container, BindingId bindingId, out ProviderProxy provider)
            {
                container.FlushBindings();
                return container.ProviderRepo.TryGetFirstMatchingProvider(bindingId, out provider);
            }
        }
    }
}