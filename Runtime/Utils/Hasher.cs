namespace Zenject
{
    public static class Hasher
    {
#if DEBUG
        static readonly System.Collections.Generic.Dictionary<int, string> _inverseHash = new();

        public static int Hash(string str)
        {
            var hash = str.GetHashCode();

            if (_inverseHash.TryGetValue(hash, out var oldStr))
            {
                UnityEngine.Assertions.Assert.AreEqual(oldStr, str);
            }
            else
            {
                _inverseHash.Add(hash, str);
            }

            return hash;
        }

        public static string ToHumanReadableString(int hash)
        {
            return _inverseHash.TryGetValue(hash, out var str) ? str : hash.ToString();
        }
#else
        public static int Hash(string str)
        {
            return str.GetHashCode();
        }

        public static string ToHumanReadableString(int hash)
        {
            return hash.ToString();
        }
#endif
    }
}