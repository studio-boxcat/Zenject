using System;

namespace ModestTree
{
    public static class TypeExtensions
    {
        public static bool DerivesFromOrEqual(this Type a, Type b)
        {
            return b == a || a.IsSubclassOf(b);
        }
    }
}