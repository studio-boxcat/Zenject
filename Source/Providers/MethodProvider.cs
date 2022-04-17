using System;

namespace Zenject
{
    public class MethodProvider<TReturn> : IProvider
    {
        readonly Func<TReturn> _method;

        public MethodProvider(Func<TReturn> method)
        {
            _method = method;
        }

        public object GetInstance()
        {
            // We cannot do a null assert here because in some cases they might intentionally
            // return null
            return _method();
        }
    }
}
