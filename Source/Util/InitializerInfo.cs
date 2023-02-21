using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace Zenject
{
    public readonly struct InitializerInfo
    {
        [CanBeNull] public readonly InjectFieldInfo[] Fields;
        public readonly InjectMethodInfo Method;


        public InitializerInfo(
            InjectFieldInfo[] fields,
            InjectMethodInfo method)
        {
            Assert.IsTrue(fields == null || fields.Length > 0);
            Assert.AreEqual(method.MethodInfo == null, method.Parameters == null);

            Fields = fields;
            Method = method;
        }

        public bool IsInjectionRequired()
        {
            return Fields != null
                   || Method.MethodInfo != null;
        }
    }
}