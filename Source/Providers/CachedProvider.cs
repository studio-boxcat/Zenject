using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    public class CachedProvider : IProvider
    {
        readonly IProvider _creator;
        List<object> _instances;
        bool _isCreatingInstance;

        public CachedProvider(IProvider creator)
        {
            _creator = creator;
        }

        // This method can be called if you want to clear the memory for an AsSingle instance,
        // See isssue https://github.com/svermeulen/Zenject/issues/441
        public void ClearCache()
        {
            _instances = null;
        }

        public void GetAllInstancesWithInjectSplit(InjectableInfo context, out Action injectAction, List<object> buffer)
        {
            if (_instances != null)
            {
                injectAction = null;
                buffer.AllocFreeAddRange(_instances);
                return;
            }

            // This should only happen with constructor injection
            // Field or property injection should allow circular dependencies
            if (_isCreatingInstance)
            {
                throw Assert.CreateException(
                    "Found circular dependency when creating type '{0}'. {1}\n",
                    context);
            }

            _isCreatingInstance = true;

            var instances = new List<object>();
            _creator.GetAllInstancesWithInjectSplit(context, out injectAction, instances);
            Assert.IsNotNull(instances);

            _instances = instances;
            _isCreatingInstance = false;
            buffer.AllocFreeAddRange(instances);
        }
    }
}
