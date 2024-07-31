using System;
using System.Diagnostics;

namespace Zenject
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field)]
    public class InjectionNotRequiredAttribute : Attribute
    {
    }
}