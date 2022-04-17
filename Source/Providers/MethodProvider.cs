using System;
using ModestTree;

namespace Zenject
{
    public class MethodProvider<TReturn> : IProvider
    {
        readonly Func<InjectableInfo, TReturn> _method;

        public MethodProvider(Func<InjectableInfo, TReturn> method)
        {
            _method = method;
        }

        public object GetInstance(InjectableInfo context)
        {
            Assert.That(typeof(TReturn).DerivesFromOrEqual(context.MemberType));

            // We cannot do a null assert here because in some cases they might intentionally
            // return null
            return _method(context);
        }
    }
}
