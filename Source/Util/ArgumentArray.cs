using System;
using ModestTree;

namespace Zenject
{
    public readonly struct ArgumentArray
    {
        public readonly object Arg1;
        public readonly object Arg2;
        public readonly object Arg3;
        public readonly int Length;


        public ArgumentArray(object arg1)
        {
            Arg1 = arg1;
            Arg2 = null;
            Arg3 = null;
            Length = 1;
        }

        public ArgumentArray(object arg1, object arg2)
        {
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = null;
            Length = 2;
        }

        public ArgumentArray(object arg1, object arg2, object arg3)
        {
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Length = 3;
        }

        public ArgumentArray(object arg1, object arg2, object arg3, int length)
        {
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Length = length;
        }

        public bool TryGetValueWithType(Type injectedFieldType, out object value)
        {
            if (Length < 1)
            {
                value = null;
                return false;
            }

            if (injectedFieldType.IsInstanceOfType(Arg1))
            {
                value = Arg1;
                return true;
            }

            if (Length < 2)
            {
                value = null;
                return false;
            }

            if (injectedFieldType.IsInstanceOfType(Arg2))
            {
                value = Arg2;
                return true;
            }

            if (Length < 3)
            {
                value = null;
                return false;
            }

            if (injectedFieldType.IsInstanceOfType(Arg3))
            {
                value = Arg3;
                return true;
            }

            value = null;
            return false;
        }

        public static implicit operator ArgumentArray((object, object) tuple)
        {
            return new ArgumentArray(tuple.Item1, tuple.Item2);
        }
    }
}