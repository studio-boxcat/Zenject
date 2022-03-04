using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModestTree;
using Zenject.Internal;

namespace Zenject
{
    public static class TypeAnalyzer
    {
        static Dictionary<Type, InjectTypeInfo> _typeInfo = new();

        // We store this separately from InjectTypeInfo because this flag is needed for contract
        // types whereas InjectTypeInfo is only needed for types that are instantiated, and
        // we want to minimize the types that generate InjectTypeInfo for
        static Dictionary<Type, bool> _allowDuringValidation = new();

        public static bool ShouldAllowDuringValidation<T>()
        {
            return ShouldAllowDuringValidation(typeof(T));
        }

        public static bool ShouldAllowDuringValidation(Type type)
        {
            bool shouldAllow;

            if (!_allowDuringValidation.TryGetValue(type, out shouldAllow))
            {
                shouldAllow = ShouldAllowDuringValidationInternal(type);
                _allowDuringValidation.Add(type, shouldAllow);
            }

            return shouldAllow;
        }

        static bool ShouldAllowDuringValidationInternal(Type type)
        {
            // During validation, do not instantiate or inject anything except for
            // Installers, IValidatable's, or types marked with attribute ZenjectAllowDuringValidation
            // You would typically use ZenjectAllowDuringValidation attribute for data that you
            // inject into factories

            if (type.DerivesFrom<IInstaller>() || type.DerivesFrom<IValidatable>() || type.DerivesFrom<Context>())
            {
                return true;
            }

            return type.IsDefined(typeof(ZenjectAllowDuringValidationAttribute));
        }

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

            using (ProfileBlock.Start("Zenject Reflection"))
            {
                info = GetInfoInternal(type);
            }

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
                return CreateTypeInfoFromReflection(type);
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

            return type.GetCustomAttribute(typeof(PreventTypeAnalysisAttribute), false) != null;
        }

        static bool IsStaticType(Type type)
        {
            // Apparently this is unique to static classes
            return type.IsAbstract() && type.IsSealed();
        }

        static InjectTypeInfo CreateTypeInfoFromReflection(Type type)
        {
            var reflectionInfo = ReflectionTypeAnalyzer.GetReflectionInfo(type);

            var injectConstructor = ReflectionInfoTypeInfoConverter.ConvertConstructor(
                reflectionInfo.InjectConstructor, type);

            var injectMethods = reflectionInfo.InjectMethods.Select(
                ReflectionInfoTypeInfoConverter.ConvertMethod).ToArray();

            var memberInfos = reflectionInfo.InjectFields.Select(
                x => ReflectionInfoTypeInfoConverter.ConvertField(type, x)).Concat(
                    reflectionInfo.InjectProperties.Select(
                        x => ReflectionInfoTypeInfoConverter.ConvertProperty(type, x))).ToArray();

            return new InjectTypeInfo(
                type, injectConstructor, injectMethods, memberInfos);
        }
    }
}
