namespace Zenject
{
    public class InstanceProvider : IProvider
    {
        readonly object _instance;

        public InstanceProvider(object instance)
        {
            _instance = instance;
        }

        public object GetInstance()
        {
            return _instance;
        }
    }
}