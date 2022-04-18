using System;
using System.Diagnostics;

namespace ModestTree
{
    // Simple wrapper around unity's logging system
    public static class Log
    {
        [Conditional("DEBUG")]
        public static void Debug(string message, params object[] args)
        {
            UnityEngine.Debug.Log(message.Fmt(args));
        }

        [Conditional("DEBUG")]
        public static void Info(string message, params object[] args)
        {
            UnityEngine.Debug.Log(message.Fmt(args));
        }

        [Conditional("DEBUG")]
        public static void Warn(string message, params object[] args)
        {
            UnityEngine.Debug.LogWarning(message.Fmt(args));
        }

        public static void Exception(Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }

        public static void Error(string message, params object[] args)
        {
            UnityEngine.Debug.LogError(message.Fmt(args));
        }
    }
}