using System;

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

        public bool TryGetValueWithType(Type type, out object value)
        {
            if (Length < 1)
            {
                value = null;
                return false;
            }

            if (type.IsInstanceOfType(Arg1))
            {
                value = Arg1;
                return true;
            }

            if (Length < 2)
            {
                value = null;
                return false;
            }

            if (type.IsInstanceOfType(Arg2))
            {
                value = Arg2;
                return true;
            }

            if (Length < 3)
            {
                value = null;
                return false;
            }

            if (type.IsInstanceOfType(Arg3))
            {
                value = Arg3;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetValueWithType<T>(out T value)
        {
            if (TryGetValueWithType(typeof(T), out var valueObj))
            {
                value = (T) valueObj;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public static ArgumentArray Concat(ArgumentArray arr, object arg)
        {
            return arr.Length switch
            {
                0 => new ArgumentArray(arg),
                1 => new ArgumentArray(arr.Arg1, arg),
                2 => new ArgumentArray(arr.Arg1, arr.Arg2, arg),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static implicit operator ArgumentArray((object, object) tuple)
        {
            return new ArgumentArray(tuple.Item1, tuple.Item2);
        }
    }
}