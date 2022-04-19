using System;
using System.Collections.Generic;
using Zenject.Internal;

namespace Zenject
{
    public static class TypeAnalyzer
    {
        static readonly Dictionary<Type, InjectTypeInfo> _typeInfo = new();

        public static bool HasInfo(Type type)
        {
            return _typeInfo.ContainsKey(type);
        }

        public static InjectTypeInfo GetInfo(Type type)
        {
            if (_typeInfo.TryGetValue(type, out var typeInfo))
                return typeInfo;

            typeInfo = ReflectionTypeAnalyzer.GetReflectionInfo(type);
            _typeInfo.Add(type, typeInfo);
            return typeInfo;
        }
    }
}