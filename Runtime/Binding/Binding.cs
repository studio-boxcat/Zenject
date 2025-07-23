using System;
using System.Collections.Generic;

namespace Zenject
{
    internal struct Binding
    {
        public readonly ulong Key;
        public object Value; // Instance or ConcreteType
        public bool Payload;

        public Binding(ulong key, object instance) : this()
        {
            Key = key;
            Value = instance;
        }

        public Binding(ulong key, Type concreteType, bool payload) : this()
        {
            Key = key;
            Value = concreteType;
            Payload = payload;
        }

        public override string ToString() => (Value is Type ? "T:" : "I:") + BindKey.ToString(Key);

        public static bool BinarySearch(Binding[] array, int count, ulong key, out int index)
        {
            index = Array.BinarySearch(array, 0, count, new Binding(key, null), Comparer.Instance);
            return index >= 0;
        }

        public static void Sort(Binding[] array, int count)
            => Array.Sort(array, 0, count, Comparer.Instance);

        private class Comparer : IComparer<Binding>
        {
            public static readonly IComparer<Binding> Instance = new Comparer();

            public int Compare(Binding x, Binding y)
            {
                return x.Key.CompareTo(y.Key);
            }
        }
    }

    internal readonly struct Payload
    {
        public readonly ProvideDelegate Provider;
        public readonly ArgumentArray Arguments;

        public Payload(ProvideDelegate provider, ArgumentArray arguments)
        {
            Provider = provider;
            Arguments = arguments;
        }
    }
}