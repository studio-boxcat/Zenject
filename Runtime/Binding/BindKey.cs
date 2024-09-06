using System;
using System.Collections.Generic;

namespace Zenject
{
    static class BindKey
    {
#if DEBUG
        static readonly Dictionary<ulong, string> _debugNames = new();
#endif

        public static ulong Hash(Type type, BindId id)
        {
            var key = (ulong) type.GetHashCode() << 32 | (uint) id;
#if DEBUG
            _debugNames[key] = $"{type.Name}:{id}";
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
    }
}