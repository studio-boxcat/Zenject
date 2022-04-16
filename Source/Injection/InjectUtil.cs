using System;
using System.Diagnostics;
using ModestTree;

namespace Zenject
{
    [DebuggerStepThrough]
    public static class InjectUtil
    {
        // Find the first match with the given type and remove it from the list
        // Return true if it was removed
        public static bool TryGetValueWithType(
            object[] extraArgMap, Type injectedFieldType, out object value)
        {
            if (extraArgMap == null)
            {
                value = null;
                return false;
            }

            foreach (var arg in extraArgMap)
            {
                if (arg.GetType().DerivesFromOrEqual(injectedFieldType))
                {
                    value = arg;
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}