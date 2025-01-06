using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Zenject
{
    public readonly struct ArgumentArray
    {
        public readonly int Length;
        private readonly object _arg1;
        private readonly object _arg2;
        private readonly object _arg3;
        private readonly object _arg4;


        public ArgumentArray(object arg1)
        {
            Assert.IsNotNull(arg1, "Argument 1 is null");

            Length = 1;
            _arg1 = arg1;
            _arg2 = null;
            _arg3 = null;
            _arg4 = null;
        }

        public ArgumentArray(object arg1, object arg2)
        {
            Assert.IsNotNull(arg1, "Argument 1 is null");
            Assert.IsNotNull(arg2, "Argument 2 is null");
            Assert.AreNotEqual(arg1.GetType(), arg2.GetType(), "Argument 1 and 2 have the same type");

            Length = 2;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = null;
            _arg4 = null;
        }

        public ArgumentArray(object arg1, object arg2, object arg3)
        {
            Assert.IsNotNull(arg1, "Argument 1 is null");
            Assert.IsNotNull(arg2, "Argument 2 is null");
            Assert.IsNotNull(arg3, "Argument 3 is null");
            Assert.AreNotEqual(arg1.GetType(), arg2.GetType(), "Argument 1 and 2 have the same type");
            Assert.AreNotEqual(arg1.GetType(), arg3.GetType(), "Argument 1 and 3 have the same type");
            Assert.AreNotEqual(arg2.GetType(), arg3.GetType(), "Argument 2 and 3 have the same type");

            Length = 3;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = null;
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

            Length = 4;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
        }

        public object this[int index] => index switch
        {
            0 => _arg1,
            1 => _arg2,
            2 => _arg3,
            3 => _arg4,
            _ => throw new ArgumentOutOfRangeException()
        };

        public bool Any() => Length is not 0;

        public bool TryGet(Type type, out object value)
        {
            if (Length < 1)
            {
                value = null;
                return false;
            }

            if (type.IsInstanceOfType(_arg1))
            {
                value = _arg1;
                return true;
            }

            if (Length < 2)
            {
                value = null;
                return false;
            }

            if (type.IsInstanceOfType(_arg2))
            {
                value = _arg2;
                return true;
            }

            if (Length < 3)
            {
                value = null;
                return false;
            }

            if (type.IsInstanceOfType(_arg3))
            {
                value = _arg3;
                return true;
            }

            if (Length < 4)
            {
                value = null;
                return false;
            }

            if (type.IsInstanceOfType(_arg4))
            {
                value = _arg4;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGet<T>(out T value)
        {
            if (TryGet(typeof(T), out var valueObj))
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

        public T Get<T>()
        {
            if (TryGet(typeof(T), out var valueObj))
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
                1 => new ArgumentArray(arr._arg1, arg),
                2 => new ArgumentArray(arr._arg1, arr._arg2, arg),
                3 => new ArgumentArray(arr._arg1, arr._arg2, arr._arg3, arg),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static ArgumentArray operator +(ArgumentArray a, ArgumentArray b)
        {
            if (a.Length == 0) return b;
            if (b.Length == 0) return a;
            if (b.Length == 1) return a + b._arg1;

            return a.Length switch
            {
                1 => b.Length switch
                {
                    2 => new ArgumentArray(a._arg1, b._arg1, b._arg2),
                    3 => new ArgumentArray(a._arg1, b._arg1, b._arg2, b._arg3),
                    _ => throw new ArgumentOutOfRangeException()
                },
                2 => b.Length switch
                {
                    2 => new ArgumentArray(a._arg1, a._arg2, b._arg1, b._arg2),
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}