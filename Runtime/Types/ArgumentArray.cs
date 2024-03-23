using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Zenject
{
    public readonly struct ArgumentArray
    {
        public readonly object Arg1;
        public readonly object Arg2;
        public readonly object Arg3;
        public readonly object Arg4;
        public readonly int Length;


        public ArgumentArray(object arg1)
        {
            Assert.IsNotNull(arg1, "Argument 1 is null");

            Arg1 = arg1;
            Arg2 = null;
            Arg3 = null;
            Arg4 = null;
            Length = 1;
        }

        public ArgumentArray(object arg1, object arg2)
        {
            Assert.IsNotNull(arg1, "Argument 1 is null");
            Assert.IsNotNull(arg2, "Argument 2 is null");
            Assert.AreNotEqual(arg1.GetType(), arg2.GetType(), "Argument 1 and 2 have the same type");

            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = null;
            Arg4 = null;
            Length = 2;
        }

        public ArgumentArray(object arg1, object arg2, object arg3)
        {
            Assert.IsNotNull(arg1, "Argument 1 is null");
            Assert.IsNotNull(arg2, "Argument 2 is null");
            Assert.IsNotNull(arg3, "Argument 3 is null");
            Assert.AreNotEqual(arg1.GetType(), arg2.GetType(), "Argument 1 and 2 have the same type");
            Assert.AreNotEqual(arg1.GetType(), arg3.GetType(), "Argument 1 and 3 have the same type");
            Assert.AreNotEqual(arg2.GetType(), arg3.GetType(), "Argument 2 and 3 have the same type");

            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = null;
            Length = 3;
        }

        public ArgumentArray(object arg1, object arg2, object arg3, object arg4)
        {
            Assert.IsNotNull(arg1, "Argument 1 is null");
            Assert.IsNotNull(arg2, "Argument 2 is null");
            Assert.IsNotNull(arg3, "Argument 3 is null");
            Assert.IsNotNull(arg4, "Argument 4 is null");
            Assert.AreNotEqual(arg1.GetType(), arg2.GetType(), "Argument 1 and 2 have the same type");
            Assert.AreNotEqual(arg1.GetType(), arg3.GetType(), "Argument 1 and 3 have the same type");
            Assert.AreNotEqual(arg1.GetType(), arg4.GetType(), "Argument 1 and 4 have the same type");
            Assert.AreNotEqual(arg2.GetType(), arg3.GetType(), "Argument 2 and 3 have the same type");
            Assert.AreNotEqual(arg2.GetType(), arg4.GetType(), "Argument 2 and 4 have the same type");
            Assert.AreNotEqual(arg3.GetType(), arg4.GetType(), "Argument 3 and 4 have the same type");

            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
            Length = 4;
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

            if (Length < 4)
            {
                value = null;
                return false;
            }

            if (type.IsInstanceOfType(Arg4))
            {
                value = Arg4;
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

        public T GetValueWithType<T>()
        {
            if (TryGetValueWithType(typeof(T), out var valueObj))
            {
                return (T) valueObj;
            }
            else
            {
                throw new KeyNotFoundException(typeof(T).Name);
            }
        }

        public static ArgumentArray operator +(ArgumentArray arr, object arg)
        {
            return arr.Length switch
            {
                0 => new ArgumentArray(arg),
                1 => new ArgumentArray(arr.Arg1, arg),
                2 => new ArgumentArray(arr.Arg1, arr.Arg2, arg),
                3 => new ArgumentArray(arr.Arg1, arr.Arg2, arr.Arg3, arg),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static ArgumentArray operator +(ArgumentArray a, ArgumentArray b)
        {
            if (a.Length == 0) return b;
            if (b.Length == 0) return a;
            if (b.Length == 1) return a + b.Arg1;

            return a.Length switch
            {
                1 => b.Length switch
                {
                    2 => new ArgumentArray(a.Arg1, b.Arg1, b.Arg2),
                    3 => new ArgumentArray(a.Arg1, b.Arg1, b.Arg2, b.Arg3),
                    _ => throw new ArgumentOutOfRangeException()
                },
                2 => b.Length switch
                {
                    2 => new ArgumentArray(a.Arg1, a.Arg2, b.Arg1, b.Arg2),
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static implicit operator ArgumentArray((object, object) tuple)
        {
            return new ArgumentArray(tuple.Item1, tuple.Item2);
        }
    }
}