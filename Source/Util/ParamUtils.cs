using System;
using System.Reflection;

namespace Zenject
{
    public static class ParamUtils
    {
        static readonly InjectSpec[] _emptyInjectableArray = Array.Empty<InjectSpec>();

        public static InjectSpec[] BakeParams(MethodBase methodInfo)
        {
            var paramInfos = methodInfo.GetParameters();
            if (paramInfos.Length == 0) return _emptyInjectableArray;

            var injectParamInfos = new InjectSpec[paramInfos.Length];
            for (var i = 0; i < paramInfos.Length; i++)
                injectParamInfos[i] = CreateInjectableInfoForParam(paramInfos[i]);
            return injectParamInfos;

            static InjectSpec CreateInjectableInfoForParam(ParameterInfo paramInfo)
            {
                var injectAttr = paramInfo.GetCustomAttribute<InjectAttributeBase>();
                return injectAttr != null
                    ? new InjectSpec(paramInfo.ParameterType, injectAttr.Id, injectAttr.Source, injectAttr.Optional)
                    : new InjectSpec(paramInfo.ParameterType, 0, InjectSources.Any);
            }
        }

        public static void ResolveParams(DiContainer container, InjectSpec[] paramSpecs, object[] paramValues, ArgumentArray extraArgs)
        {
            for (var i = 0; i < paramSpecs.Length; i++)
            {
                var paramSpec = paramSpecs[i];

                if (!extraArgs.TryGetValueWithType(paramSpec.Type, out var value))
                    value = container.Resolve(paramSpec);

                if (value == null && !paramSpec.Optional)
                    throw new ParamResolveException(paramSpec, i);

                paramValues[i] = value;
            }
        }
    }
}