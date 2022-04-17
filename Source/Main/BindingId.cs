using System;
using System.Diagnostics;
using ModestTree;

namespace Zenject
{
    [DebuggerStepThrough]
    public struct BindingId : IEquatable<BindingId>
    {
        public readonly Type Type;
        public readonly object Identifier;

        public BindingId(Type type, object identifier)
        {
            Type = type;
            Identifier = identifier;
        }

        public override string ToString()
        {
            if (Identifier == null) return Type.PrettyName();
            return "{0} (ID: {1})".Fmt(Type, Identifier);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 29 + Type.GetHashCode();
                hash = hash * 29 + (Identifier == null ? 0 : Identifier.GetHashCode());
                return hash;
            }
        }

        public override bool Equals(object other) => other is BindingId otherId && otherId == this;
        public bool Equals(BindingId that) => this == that;

        public static bool operator ==(BindingId left, BindingId right) => left.Type == right.Type && Equals(left.Identifier, right.Identifier);
        public static bool operator !=(BindingId left, BindingId right) => !left.Equals(right);
    }
}