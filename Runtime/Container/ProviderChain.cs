using System.Collections;
using JetBrains.Annotations;

namespace Zenject
{
    public struct ProviderChain
    {
        readonly ProviderRepo _self;
        readonly ProviderRepo _parent;
        readonly ProviderRepo[] _containerChain;

        public ProviderChain(DiContainer container)
        {
            _self = container.ProviderRepo;
            _parent = container.ParentContainer?.ProviderRepo;
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
        public bool HasBinding(BindingId bindingId, InjectSources sourceType)
        {
            if (sourceType == InjectSources.Local)
                return _self.HasBinding(bindingId);

            if (sourceType == InjectSources.Parent)
                return _parent.HasBinding(bindingId);

            foreach (var currentContainer in _containerChain)
            {
                if (currentContainer.HasBinding(bindingId))
                    return true;
            }

            return false;
        }

        public readonly void ResolveAll(BindingId bindingId, InjectSources sourceType, IList buffer)
        {
            if (sourceType == InjectSources.Local)
            {
                _self.ResolveAll(bindingId, buffer);
                return;
            }

            if (sourceType == InjectSources.Parent)
            {
                _parent.ResolveAll(bindingId, buffer);
                return;
            }

            foreach (var container in _containerChain)
                container.ResolveAll(bindingId, buffer);
        }

        [Pure]
        public bool TryResolve(BindingId bindingId, InjectSources sourceType, out object instance)
        {
            if (sourceType == InjectSources.Local)
                return _self.TryResolve(bindingId, out instance);

            if (sourceType == InjectSources.Parent)
                return _parent.TryResolve(bindingId, out instance);

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