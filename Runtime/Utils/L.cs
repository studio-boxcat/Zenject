using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Zenject
{
    static class L
    {
        [Conditional("DEBUG"), HideInCallstack]
        public static void I(string message, Object context)
        {
            Debug.Log("[Zenject] " + message, context);
        }

        [HideInCallstack]
        public static void E(string message, Object context)
        {
            Debug.LogError("[Zenject] " + message, context);
        }
    }
}