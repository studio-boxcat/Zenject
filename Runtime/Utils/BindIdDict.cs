#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.Assertions;

namespace Zenject
{
    static class BindIdDict
    {
        static Dictionary<BindId, string> _dictBacking;
        static Dictionary<BindId, string> _dict => _dictBacking ??= Build();
        public static Dictionary<BindId, string>.KeyCollection Keys => _dict.Keys;
        public static Dictionary<BindId, string>.ValueCollection Values => _dict.Values;

        static HashSet<BindId> _validSet;
        public static bool Contains(BindId bindId)
        {
            _validSet ??= new HashSet<BindId>(_dict.Keys);
            return _validSet.Contains(bindId);
        }

        static Dictionary<BindId, string> Build()
        {
            var dict = new Dictionary<BindId, string>();

            // Collect ids from containers.
            var containerTypes = TypeCache.GetTypesWithAttribute<BindIdDefinitionAttribute>();
            foreach (var containerType in containerTypes)
            foreach (var fieldInfo in containerType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (fieldInfo.FieldType != typeof(BindId)) continue;
                AddField(dict, fieldInfo);
            }

            // Collect ids from fields.
            var fieldTypes = TypeCache.GetFieldsWithAttribute<BindIdDefinitionAttribute>();
            foreach (var fieldInfo in fieldTypes)
            {
                Assert.AreEqual(typeof(BindId), fieldInfo.DeclaringType);
                AddField(dict, fieldInfo);
            }

            return dict;

            static void AddField(Dictionary<BindId, string> dict, FieldInfo fieldInfo)
            {
                Assert.IsTrue(fieldInfo.IsLiteral && !fieldInfo.IsInitOnly,
                    $"Field {fieldInfo.Name} must be const.");
                var bindId = (BindId) fieldInfo.GetValue(null);
                Assert.AreNotEqual(default, bindId, $"Field {fieldInfo.Name} must not be zero.");
                dict.Add(bindId, fieldInfo.Name); // Redundant bindId will throw an exception.
            }
        }
    }
}
#endif