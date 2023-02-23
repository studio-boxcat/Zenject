using System;
using System.Diagnostics;

namespace Zenject
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
    public class NoReflectionBakingAttribute : Attribute
    {
    }
}