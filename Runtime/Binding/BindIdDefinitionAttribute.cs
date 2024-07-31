using System;
using System.Diagnostics;

namespace Zenject
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
    [Conditional("UNITY_EDITOR")]
    public class BindIdDefinitionAttribute : Attribute
    {
    }
}