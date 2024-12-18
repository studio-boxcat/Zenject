using System;
using System.Diagnostics;

namespace Zenject
{
    // DEBUG for validate the reflection baking
    [Conditional("DEBUG")]
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
    public class NoReflectionBakingAttribute : Attribute { }
}