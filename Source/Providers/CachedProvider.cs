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

        public object GetInstance()
        {
            if (_instance != null)
                return _instance;

            // This should only happen with constructor injection
            // Field or property injection should allow circular dependencies
            if (_isCreatingInstance)
            {
                throw Assert.CreateException(
                    "Found circular dependency when creating type '{0}'.",
                    _creator);
            }

            _isCreatingInstance = true;
            _instance = _creator.GetInstance();
            _isCreatingInstance = false;
            return _instance;
        }
    }
}
