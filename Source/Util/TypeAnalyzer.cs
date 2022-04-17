using System;
using System.Collections.Generic;
using Zenject.Internal;

namespace Zenject
{
    public static class TypeAnalyzer
    {
        static readonly Dictionary<Type, InjectTypeInfo?> _typeInfo = new();

        public static bool HasInfo(Type type)
        {
            return _typeInfo.ContainsKey(type);
        }

        public static bool GetInfo(Type type, out InjectTypeInfo info)
        {
            if (_typeInfo.TryGetValue(type, out var found))
            {
                if (found.HasValue)
                {
                    info = found.Value;
                    return true;
                }
                else
                {
                    info = default;
                    return false;
                }
            }

            var shouldSkipTypeAnalysis =
                type.IsEnum
                || type.IsArray
                || type.IsInterface
                || type.ContainsGenericParameters
                || type.IsAbstract
                || type == typeof(object);

            if (shouldSkipTypeAnalysis)
            {
                _typeInfo.Add(type, null);
                info = default;
                return false;
            }

            info = ReflectionTypeAnalyzer.GetReflectionInfo(type);
            var injectionRequired = info.IsInjectionRequired();
            if (injectionRequired)
            {
                _typeInfo.Add(type, info);
                return true;
            }
            else
            {
                _typeInfo.Add(type, null);
                return false;
            }
        }
    }
}