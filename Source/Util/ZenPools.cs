using System.Collections.Generic;
using UnityEngine.Pool;

namespace Zenject.Internal
{
    public static class ZenPools
    {
        static readonly StaticMemoryPool<BindInfo> _bindInfoPool = new();
        static readonly StaticMemoryPool<BindStatement> _bindStatementPool = new();

        public static Dictionary<TKey, TValue> SpawnDictionary<TKey, TValue>()
        {
            return DictionaryPool<TKey, TValue>.Get();
        }

        public static BindStatement SpawnStatement()
        {
            return _bindStatementPool.Spawn();
        }

        public static void DespawnStatement(BindStatement statement)
        {
            statement.Reset();
            _bindStatementPool.Despawn(statement);
        }

        public static BindInfo SpawnBindInfo()
        {
            return _bindInfoPool.Spawn();
        }

        public static void DespawnBindInfo(BindInfo bindInfo)
        {
            bindInfo.Reset();
            _bindInfoPool.Despawn(bindInfo);
        }

        public static void DespawnDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            DictionaryPool<TKey, TValue>.Release(dictionary);
        }

        public static List<T> SpawnList<T>()
        {
            return ListPool<T>.Get();
        }

        public static void DespawnList<T>(List<T> list)
        {
            ListPool<T>.Release(list);
        }
    }
}