using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Zenject
{
    static class L
    {
        [Conditional("DEBUG"), HideInCallstack]
        public static void I(string message, Object context = null)
        {
            Debug.Log("[Zenject] " + message, context);
        }

        [Conditional("DEBUG"), HideInCallstack]
        public static void W(string message, Object context = null)
        {
            Debug.LogWarning("[Zenject] " + message, context);
        }

        [HideInCallstack]
        public static void E(string message, Object context = null)
        {
            Debug.LogError("[Zenject] " + message, context);
        }

        [HideInCallstack]
        public static void E(System.Exception e, Object context = null)
        {
            Debug.LogException(e, context);
        }
    }
}