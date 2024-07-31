using System;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;

namespace Zenject.Tests
{
    public class InjectAttributeTests
    {
        static readonly StringBuilder _sb = new();

        [Test]
        public void Test_AllMethodInjection()
        {
            var methods = TypeCache.GetMethodsWithAttribute<InjectMethodAttribute>();
            foreach (var methodInfo in methods)
            {
                var message = _sb.Append(methodInfo.DeclaringType!.Name).Append(':').Append(methodInfo.Name).ToString();
                _sb.Clear();

                Assert.IsFalse(methodInfo.IsStatic, message);
                Assert.IsTrue(methodInfo.IsPrivate, message);
                Assert.AreEqual("Zenject_Constructor", methodInfo.Name, message);
                Assert.AreEqual(typeof(void), methodInfo.ReturnType, message);

                // Check if BindId is valid.
                var attr = methodInfo.GetCustomAttribute<InjectAttributeBase>();
                Assert.IsTrue(BindIdDict.Valid(attr.Id), message);
                foreach (var paramInfo in methodInfo.GetParameters())
                {
                    var paramInjectAttr = paramInfo.GetCustomAttribute<InjectAttributeBase>();
                    if (paramInjectAttr == null) continue;
                    Assert.IsTrue(BindIdDict.Valid(paramInjectAttr.Id), message);
                }
            }
        }

        [Test]
        public void Test_AllFieldInjection()
        {
            var fieldInfos = TypeCache.GetFieldsWithAttribute<InjectAttributeBase>();
            foreach (var fieldInfo in fieldInfos)
            {
                var message = _sb.Append(fieldInfo.DeclaringType!.Name).Append(':').Append(fieldInfo.Name).ToString();
                _sb.Clear();

                Assert.IsFalse(fieldInfo.IsStatic, message);
                Assert.IsFalse(fieldInfo.IsInitOnly, message);
                Assert.IsFalse(fieldInfo.FieldType.IsGenericType, message);

                // If the field could be serialized, it should be marked with NonSerializedAttribute.
                var fieldType = fieldInfo.FieldType;
                if (IsSerializableType(fieldInfo.DeclaringType) && fieldInfo.IsPublic && IsSerializableType(fieldType))
                {
                    Assert.IsTrue(fieldInfo.IsDefined(typeof(NonSerializedAttribute), false), message);
                }

                // Check if BindId is valid.
                var attr = fieldInfo.GetCustomAttribute<InjectAttributeBase>();
                Assert.IsTrue(BindIdDict.Valid(attr.Id), message);
            }
            return;

            static bool IsSerializableType(Type type)
            {
                if (type.IsPrimitive)
                    return true;
                if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                    return true;
                if (type.IsDefined(typeof(SerializableAttribute), true))
                    return true;
                return false;
            }
        }
    }
}