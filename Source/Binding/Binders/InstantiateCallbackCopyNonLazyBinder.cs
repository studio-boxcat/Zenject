using System;
using ModestTree;

namespace Zenject
{
    [NoReflectionBaking]
    public class InstantiateCallbackCopyNonLazyBinder : CopyNonLazyBinder
    {
        public InstantiateCallbackCopyNonLazyBinder(BindInfo bindInfo)
            : base(bindInfo)
        {
        }

        public CopyNonLazyBinder OnInstantiated(
            Action<InjectContext, object> callback)
        {
            BindInfo.InstantiatedCallback = callback;
            return this;
        }

        public CopyNonLazyBinder OnInstantiated<T>(
            Action<InjectContext, T> callback)
        {
            // Can't do this here because of factory bindings
            //Assert.That(BindInfo.ContractTypes.All(x => x.DerivesFromOrEqual<T>()));

            BindInfo.InstantiatedCallback = (ctx, obj) =>
            {
                Assert.That(obj == null || obj is T,
                    "Invalid generic argument to OnInstantiated! {0} must be type {1}", obj.GetType(), typeof(T));

                callback(ctx, (T)obj);
            };
            return this;
        }
    }
}
