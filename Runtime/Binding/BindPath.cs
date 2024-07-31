using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Zenject
{
    [DebuggerStepThrough]
    readonly struct BindPath : IEquatable<BindPath>
    {
        public readonly Type Type;
        public readonly BindId Id; // 0 means wildcard.

        public BindPath([NotNull] Type type, BindId id = 0)
        {
            Type = type;
            Id = id;
        }

        public void Deconstruct(out Type type, out BindId identifier)
        {
            type = Type;
            identifier = Id;
        }

        public override string ToString() => $"{Type.PrettyName()} ({Id})";

        public bool Equals(BindPath other) => Type == other.Type && Id == other.Id;
        public override bool Equals(object obj) => obj is BindPath other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Type, Id);

        public static bool operator ==(BindPath a, BindPath b) => a.Equals(b);
        public static bool operator !=(BindPath a, BindPath b) => !a.Equals(b);

        public static readonly IEqualityComparer<BindPath> Comparer = new EqualityComparer();

        sealed class EqualityComparer : IEqualityComparer<BindPath>
        {
            public bool Equals(BindPath x, BindPath y) => x.Equals(y);
            public int GetHashCode(BindPath obj) => obj.GetHashCode();
        }
    }
}