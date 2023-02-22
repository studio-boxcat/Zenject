using System;
using System.Reflection;

namespace Zenject
{
    public class FieldResolveException : Exception
    {
        public readonly Type InjectableType;
        public readonly InjectSpec FieldSpec;

        public FieldResolveException(Type injectableType, InjectSpec fieldSpec, Exception innerException)
            : base(FormatMessage(injectableType, fieldSpec), innerException)
        {
            InjectableType = injectableType;
            FieldSpec = fieldSpec;
        }

        static string FormatMessage(Type injectableType, InjectSpec fieldSpec)
        {
            return $"Failed to Resolve Field: injectableType={injectableType.Name}, fieldSpec={fieldSpec.ToString()}";
        }
    }

    public class ParamResolveException : Exception
    {
        public readonly InjectSpec ParamSpec;
        public readonly int ParamIndex;

        public ParamResolveException(InjectSpec paramSpec, int paramIndex)
            : base(FormatMessage(paramSpec, paramIndex))
        {
            ParamSpec = paramSpec;
            ParamIndex = paramIndex;
        }

        static string FormatMessage(InjectSpec paramSpec, int paramIndex)
        {
            return $"Failed to Resolve Param: paramSpec={paramSpec.ToString()}, paramIndex={paramIndex}";
        }
    }

    public class MethodInvokeException : Exception
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

        static string FormatMessage(MethodBase methodBase, InjectSpec paramSpec, int paramIndex)
        {
            return $"Failed to Invoke Method: method={methodBase.Name}, paramSpec={paramSpec.ToString()}, paramIndex={paramIndex}";
        }
    }
}