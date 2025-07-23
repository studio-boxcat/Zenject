using System;
using System.Collections.Generic;

namespace Zenject
{
    internal static class BindKey
    {
#if DEBUG
        private static readonly Dictionary<ulong, string> _debugNames = new();
#endif

        public static ulong Hash(Type type, BindId id)
        {
            var key = Numeric.PackU64(type.GetHashCode(), (uint) id);
#if DEBUG
            _debugNames[key] = id == default
                ? type.Name : $"{type.Name}:{id}";
#endif
            return key;
        }

        public static string ToString(ulong key)
        {
#if DEBUG
            return _debugNames[key];
#else
            return key.ToString();
#endif
        }

        private static ulong _selfBindKey;
        public static ulong GetSelfBindKey()
        {
            if (_selfBindKey is 0)
                _selfBindKey = Hash(typeof(DiContainer), default);
            return _selfBindKey;
        }
    }
}