using System;

namespace Zenject
{
    public class InstanceProvider : IProvider
    {
        readonly object _instance;

        public InstanceProvider(object instance)
        {
            _instance = instance;
        }

        public object GetInstanceWithInjectSplit(InjectableInfo context, out Action injectAction)
        {
            injectAction = null;

            return _instance;
        }
    }
}
