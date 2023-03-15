using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Zenject
{
    [DebuggerStepThrough]
    public readonly struct BindingId : IEquatable<BindingId>
    {
        public readonly Type Type;
        public readonly int Identifier;

        public BindingId([NotNull] Type type, int identifier = 0)
        {
            Type = type;
            Identifier = identifier;
        }

        public void Deconstruct(out Type type, out int identifier)
        {
            type = Type;
            identifier = Identifier;
        }

        public override string ToString() => $"{Type.PrettyName()} ({Identifier})";

        public bool Equals(BindingId other) => Type == other.Type && Identifier == other.Identifier;
        public override bool Equals(object obj) => obj is BindingId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Type, Identifier);

        public static bool operator ==(BindingId a, BindingId b) => a.Equals(b);
        public static bool operator !=(BindingId a, BindingId b) => !a.Equals(b);

        public static readonly IEqualityComparer<BindingId> Comparer = new EqualityComparer();

        sealed class EqualityComparer : IEqualityComparer<BindingId>
        {
            public bool Equals(BindingId x, BindingId y) => x.Equals(y);
            public int GetHashCode(BindingId obj) => obj.GetHashCode();
        }
    }
}