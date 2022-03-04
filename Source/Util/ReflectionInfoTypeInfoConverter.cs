//#define ZEN_DO_NOT_USE_COMPILED_EXPRESSIONS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModestTree;
#if !NOT_UNITY3D
using UnityEngine;
#endif

namespace Zenject.Internal
{
    public static class ReflectionInfoTypeInfoConverter
    {
        public static InjectTypeInfo.InjectMethodInfo ConvertMethod(
            ReflectionTypeInfo.InjectMethodInfo injectMethod)
        {
            var methodInfo = injectMethod.MethodInfo;
            ZenInjectMethod action = (obj, args) => methodInfo.Invoke(obj, args);

            return new InjectTypeInfo.InjectMethodInfo(
                action,
                injectMethod.BakeParameterInjectableInfoArray(),
                methodInfo.Name);
        }

        public static InjectTypeInfo.InjectConstructorInfo ConvertConstructor(
            ReflectionTypeInfo.InjectConstructorInfo injectConstructor, Type type)
        {
            return new InjectTypeInfo.InjectConstructorInfo(
                TryCreateFactoryMethod(type, injectConstructor),
                injectConstructor.BakeParameterInjectableInfoArray());
        }

        public static InjectTypeInfo.InjectMemberInfo ConvertField(
            Type parentType, ReflectionTypeInfo.InjectFieldInfo injectField)
        {
            return new InjectTypeInfo.InjectMemberInfo(
                GetSetter(parentType, injectField.FieldInfo), injectField.InjectableInfo);
        }

        public static InjectTypeInfo.InjectMemberInfo ConvertProperty(
            Type parentType, ReflectionTypeInfo.InjectPropertyInfo injectProperty)
        {
            return new InjectTypeInfo.InjectMemberInfo(
                GetSetter(parentType, injectProperty.PropertyInfo), injectProperty.InjectableInfo);
        }

        static ZenFactoryMethod TryCreateFactoryMethod(
            Type type, ReflectionTypeInfo.InjectConstructorInfo reflectionInfo)
        {
#if !NOT_UNITY3D
            if (type.DerivesFromOrEqual<Component>())
            {
                return null;
            }
#endif

            if (type.IsAbstract())
            {
                Assert.That(reflectionInfo.Parameters.IsEmpty());
                return null;
            }

            var constructor = reflectionInfo.ConstructorInfo;

            ZenFactoryMethod factoryMethod = null;

            if (constructor == null)
            {
                // No choice in this case except to use the slow Activator.CreateInstance
                // as far as I know
                // This should be rare though and only seems to occur when instantiating
                // structs on platforms that don't support lambda expressions
                // Non-structs should always have a default constructor
                factoryMethod = args =>
                {
                    Assert.That(args.Length == 0);
                    return Activator.CreateInstance(type, Array.Empty<object>());
                };
            }
            else
            {
                factoryMethod = constructor.Invoke;
            }

            return factoryMethod;
        }

#if !(UNITY_WSA && ENABLE_DOTNET) || UNITY_EDITOR
        static IEnumerable<FieldInfo> GetAllFields(Type t, BindingFlags flags)
        {
            if (t == null)
            {
                return Enumerable.Empty<FieldInfo>();
            }

            return t.GetFields(flags).Concat(GetAllFields(t.BaseType, flags)).Distinct();
        }

        static ZenMemberSetterMethod GetOnlyPropertySetter(
            Type parentType,
            string propertyName)
        {
            Assert.That(parentType != null);
            Assert.That(!string.IsNullOrEmpty(propertyName));

            var allFields = GetAllFields(
                parentType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).ToList();

            var writeableFields = allFields.Where(f => f.Name == string.Format("<" + propertyName + ">k__BackingField", propertyName)).ToList();

            if (!writeableFields.Any())
            {
                throw new ZenjectException(string.Format(
                    "Can't find backing field for get only property {0} on {1}.\r\n{2}",
                    propertyName, parentType.FullName, string.Join(";", allFields.Select(f => f.Name).ToArray())));
            }

            return (injectable, value) => writeableFields.ForEach(f => f.SetValue(injectable, value));
        }
#endif

        static ZenMemberSetterMethod GetSetter(Type parentType, MemberInfo memInfo)
        {
            var fieldInfo = memInfo as FieldInfo;
            var propInfo = memInfo as PropertyInfo;

            if (fieldInfo != null)
            {
                return ((injectable, value) => fieldInfo.SetValue(injectable, value));
            }

            Assert.IsNotNull(propInfo);

#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
            return ((object injectable, object value) => propInfo.SetValue(injectable, value, null));
#else
            if (propInfo.CanWrite)
            {
                return ((injectable, value) => propInfo.SetValue(injectable, value, null));
            }

            return GetOnlyPropertySetter(parentType, propInfo.Name);
#endif
        }
    }
}
