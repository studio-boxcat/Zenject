using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace Zenject
{
    public readonly struct InitializerInfo
    {
        public readonly InjectMethodInfo Method;
        [CanBeNull] public readonly InjectFieldInfo[] Fields;


        public InitializerInfo(
            InjectMethodInfo method,
            InjectFieldInfo[] fields)
        {
            Assert.AreEqual(method.MethodInfo == null, method.Parameters == null);
            Assert.IsTrue(fields == null || fields.Length > 0);

            Method = method;
            Fields = fields;
        }

        public bool IsInjectionRequired()
        {
            return Fields != null
                   || Method.MethodInfo != null;
        }
    }
}