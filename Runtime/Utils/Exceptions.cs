using System;
using System.Reflection;

namespace Zenject
{
    internal class ParamResolveException : Exception
    {
        public readonly InjectSpec ParamSpec;
        public readonly int ParamIndex;

        public ParamResolveException(InjectSpec paramSpec, int paramIndex)
            : base(FormatMessage(paramSpec, paramIndex))
        {
            ParamSpec = paramSpec;
            ParamIndex = paramIndex;
        }

        private static string FormatMessage(InjectSpec paramSpec, int paramIndex)
        {
            return $"Failed to Resolve Param: paramSpec={paramSpec.ToString()}, paramIndex={paramIndex}";
        }
    }

    internal class MethodInvokeException : Exception
    {
        public readonly MethodBase MethodBase;
        public readonly InjectSpec ParamSpec;
        public readonly int ParamIndex;

        public MethodInvokeException(MethodBase methodBase, InjectSpec paramSpec, int paramIndex, Exception innerException)
            : base(FormatMessage(methodBase, paramSpec, paramIndex), innerException)
        {
            MethodBase = methodBase;
            ParamSpec = paramSpec;
            ParamIndex = paramIndex;
        }

        private static string FormatMessage(MethodBase methodBase, InjectSpec paramSpec, int paramIndex)
        {
            return $"Failed to Invoke Method: method={methodBase.Name}, paramSpec={paramSpec.ToString()}, paramIndex={paramIndex}";
        }
    }
}