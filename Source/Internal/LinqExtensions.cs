using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace ModestTree
{
    public static class LinqExtensions
    {
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        // Return the first item when the list is of length one and otherwise returns default
        public static TSource OnlyOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            Assert.IsNotNull(source);

            if (source.Count() > 1)
            {
                return default(TSource);
            }

            return source.FirstOrDefault();
        }

        public static bool IsEmpty(this string str) => str.Length == 0;
        public static bool IsEmpty(this ICollection collection) => collection.Count == 0;
        public static bool IsEmpty<T>(this IReadOnlyCollection<T> collection) => collection.Count == 0;
        public static bool IsEmpty<T>(this IEnumerable<T> enumerable) => !enumerable.Any();
        public static bool IsEmpty<T>(this T[] arr) => arr.Length == 0;
        public static bool IsEmpty<T>(this List<T> list) => list.Count == 0;
        public static bool IsEmpty<K, V>(this Dictionary<K, V> dictionary) => dictionary.Count == 0;
        public static bool IsEmpty<T>(this Stack<T> stack) => stack.Count == 0;

        public static IEnumerable<T> GetDuplicates<T>(this IEnumerable<T> list)
        {
            return list.GroupBy(x => x).Where(x => x.Skip(1).Any()).Select(x => x.Key);
        }

        public static IEnumerable<T> Except<T>(this IEnumerable<T> list, T item)
        {
            return list.Except(item.Yield());
        }

        // LINQ already has a method called "Contains" that does the same thing as this
        // BUT it fails to work with Mono 3.5 in some cases.
        // For example the following prints False, True in Mono 3.5 instead of True, True like it should:
        //
        // IEnumerable<string> args = new string[]
        // {
        //     "",
        //     null,
        // };

        // Log.Info(args.ContainsItem(null));
        // Log.Info(args.Where(x => x == null).Any());
        public static bool ContainsItem<T>(this IEnumerable<T> list, T value)
        {
            // Use object.Equals to support null values
            return list.Where(x => object.Equals(x, value)).Any();
        }
    }
}
