using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using Zenject;

namespace Tests
{
    public class InjectAttributeTests
    {
        [Test]
        public void Test_AllMethodInjection()
        {
            var methods = TypeCache.GetMethodsWithAttribute<InjectMethodAttribute>();
            foreach (var methodInfo in methods)
            {
                var message = $"{methodInfo.DeclaringType.Name}:{methodInfo.Name}";
                Assert.IsFalse(methodInfo.IsStatic, message);
                Assert.AreEqual("Zenject_Constructor", methodInfo.Name, message);
                Assert.AreEqual(typeof(void), methodInfo.ReturnType, message);
            }
        }

        [Test]
        public void Test_AllFieldInjection()
        {
            var fieldInfos = TypeCache.GetFieldsWithAttribute<InjectAttributeBase>();
            foreach (var fieldInfo in fieldInfos)
            {
                var message = $"{fieldInfo.DeclaringType.Name}:{fieldInfo.Name}";
                Assert.IsFalse(fieldInfo.IsStatic, message);
                Assert.IsFalse(fieldInfo.IsInitOnly, message);
                Assert.IsFalse(fieldInfo.FieldType.IsGenericType, message);

                var attr = fieldInfo.GetCustomAttribute<InjectAttributeBase>();
                Assert.IsTrue(attr is InjectAttribute or InjectOptionalAttribute or InjectLocalAttribute, message);
            }
        }
    }
}