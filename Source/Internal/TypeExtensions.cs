using System;
using System.Reflection;

namespace ModestTree
{
    public static class TypeExtensions
    {
        public static bool DerivesFrom<T>(this Type a)
        {
            return DerivesFrom(a, typeof(T));
        }

        // This seems easier to think about than IsAssignableFrom
        public static bool DerivesFrom(this Type a, Type b)
        {
            return b != a && a.DerivesFromOrEqual(b);
        }

        public static bool DerivesFromOrEqual<T>(this Type a)
        {
            return DerivesFromOrEqual(a, typeof(T));
        }

        public static bool DerivesFromOrEqual(this Type a, Type b)
        {
            return b == a || b.IsAssignableFrom(a);
        }

        public static ConstructorInfo[] Constructors(this Type type)
        {
            return type.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static MethodInfo[] InstanceMethods(this Type type)
        {
            return type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static FieldInfo[] InstanceFields(this Type type)
        {
            return type.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}