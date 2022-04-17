using System;
using ModestTree;

namespace Zenject
{
    public class CachedProvider : IProvider
    {
        readonly IProvider _creator;
        object _instance;
        bool _isCreatingInstance;

        public CachedProvider(IProvider creator)
        {
            _creator = creator;
        }

        // This method can be called if you want to clear the memory for an AsSingle instance,
        // See isssue https://github.com/svermeulen/Zenject/issues/441
        public void ClearCache()
        {
            _instance = null;
        }

        public object GetInstanceWithInjectSplit(InjectableInfo context, out Action injectAction)
        {
            if (_instance != null)
            {
                injectAction = null;
                return _instance;
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
            _instance =_creator.GetInstanceWithInjectSplit(context, out injectAction);
            _isCreatingInstance = false;
            return _instance;
        }
    }
}
