using System;
using System.Collections.Generic;
using ModestTree;
using Zenject.Internal;

namespace Zenject
{
    public static class TypeAnalyzer
    {
        static Dictionary<Type, InjectTypeInfo> _typeInfo = new();

        public static bool HasInfo<T>()
        {
            return HasInfo(typeof(T));
        }

        public static bool HasInfo(Type type)
        {
            return TryGetInfo(type) != null;
        }

        public static InjectTypeInfo GetInfo<T>()
        {
            return GetInfo(typeof(T));
        }

        public static InjectTypeInfo GetInfo(Type type)
        {
            var info = TryGetInfo(type);
            Assert.IsNotNull(info, "Unable to get type info for type '{0}'", type);
            return info;
        }

        public static InjectTypeInfo TryGetInfo<T>()
        {
            return TryGetInfo(typeof(T));
        }

        public static InjectTypeInfo TryGetInfo(Type type)
        {
            InjectTypeInfo info;

            {
                if (_typeInfo.TryGetValue(type, out info))
                {
                    return info;
                }
            }

            info = GetInfoInternal(type);

            if (info != null)
            {
                Assert.IsEqual(info.Type, type);
                Assert.IsNull(info.BaseTypeInfo);

                var baseType = type.BaseType();

                if (baseType != null)
                {
                    if (_typeInfo.TryGetValue(baseType, out var baseTypeInfo))
                    {
                        info.BaseTypeInfo = baseTypeInfo;
                    }
                    else if (!ShouldSkipTypeAnalysis(baseType))
                    {
                        info.BaseTypeInfo = TryGetInfo(baseType);
                    }
                }
            }

            {
                _typeInfo[type] = info;
            }

            return info;
        }

        static InjectTypeInfo GetInfoInternal(Type type)
        {
            if (ShouldSkipTypeAnalysis(type))
            {
                return null;
            }

            {
                return ReflectionTypeAnalyzer.GetReflectionInfo(type);
            }
        }

        public static bool ShouldSkipTypeAnalysis(Type type)
        {
            if (type == null || type.IsEnum() || type.IsArray || type.IsInterface()
                || type.ContainsGenericParameters() || IsStaticType(type)
                || type == typeof(object))
            {
                return true;
            }

            var @namespace = type.Namespace;
            if (@namespace != null && (
                @namespace.StartsWith("System", StringComparison.Ordinal)
                || @namespace.StartsWith("Unity", StringComparison.Ordinal)
                || @namespace.StartsWith("TouchScript", StringComparison.Ordinal)
                || @namespace.StartsWith("Coffee", StringComparison.Ordinal)
                || @namespace.StartsWith("DG.", StringComparison.Ordinal)
                || @namespace.StartsWith("E7.", StringComparison.Ordinal)
                || @namespace.StartsWith("RedBlueGames", StringComparison.Ordinal)
                || @namespace.StartsWith("SuperScrollView", StringComparison.Ordinal)
                ))
            {
                return true;
            }

            return false;
        }

        static bool IsStaticType(Type type)
        {
            // Apparently this is unique to static classes
            return type.IsAbstract() && type.IsSealed();
        }
    }
}
