using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject.Internal
{
    public readonly struct LookupId
    {
        public readonly IProvider Provider;
        public readonly BindingId BindingId;

        public LookupId(IProvider provider, BindingId bindingId)
        {
            Assert.IsNotNull(provider);
            Assert.IsNotNull(bindingId);

            Provider = provider;
            BindingId = bindingId;
        }

        public override int GetHashCode() => HashCode.Combine(Provider, BindingId);
        public bool Equals(LookupId other) => Equals(Provider, other.Provider) && BindingId.Equals(other.BindingId);
        public override bool Equals(object obj) => obj is LookupId other && Equals(other);


        public static IEqualityComparer<LookupId> Comparer { get; } = new ProviderBindingIdEqualityComparer();

        sealed class ProviderBindingIdEqualityComparer : IEqualityComparer<LookupId>
        {
            public bool Equals(LookupId x, LookupId y) => x.Equals(y);
            public int GetHashCode(LookupId obj) => obj.GetHashCode();
        }
    }
}