using System;
using UnityEngine.Assertions;

namespace Zenject
{
    public readonly struct TypeArray
    {
        public readonly Type Type1;
        public readonly Type Type2;
        public readonly Type Type3;
        public readonly Type Type4;
        public readonly int Length;

        const int _maxLength = 4;

        public TypeArray(Type type)
        {
            Assert.IsNotNull(type);

            Type1 = type;
            Type2 = null;
            Type3 = null;
            Type4 = null;
            Length = 1;
        }

        public TypeArray(Type[] types)
        {
            Assert.IsTrue(types.Length is > 0 and <= _maxLength);

            var len = types.Length;
            if (len == 1)
            {
                Type1 = types[0];
                Type2 = null;
                Type3 = null;
                Type4 = null;
                Length = 1;
            }
            else if (len == 2)
            {
                Type1 = types[0];
                Type2 = types[1];
                Type3 = null;
                Type4 = null;
                Length = 2;
            }
            else if (len == 3)
            {
                Type1 = types[0];
                Type2 = types[1];
                Type3 = types[2];
                Type4 = null;
                Length = 3;
            }
            else if (len == 4)
            {
                Type1 = types[0];
                Type2 = types[1];
                Type3 = types[2];
                Type4 = types[3];
                Length = 4;
            }
            else
            {
                throw new Exception("지원되는 타입개수가 아닙니다: " + len);
            }
        }

        public TypeArray(Type type, Type[] additionalTypes)
        {
            Assert.IsNotNull(type);
            Assert.IsTrue(additionalTypes.Length is > 0 and < _maxLength);

            Type1 = type;

            var len = additionalTypes.Length;
            if (len == 1)
            {
                Type2 = additionalTypes[0];
                Type3 = null;
                Type4 = null;
                Length = 2;
            }
            else if (len == 2)
            {
                Type2 = additionalTypes[0];
                Type3 = additionalTypes[1];
                Type4 = null;
                Length = 3;
            }
            else if (len == 3)
            {
                Type2 = additionalTypes[0];
                Type3 = additionalTypes[1];
                Type4 = additionalTypes[2];
                Length = 4;
            }
            else
            {
                throw new Exception("지원되는 타입개수가 아닙니다: " + len);
            }
        }

        public Type this[int index]
        {
            get
            {
                Assert.IsTrue(index < Length);

                return index switch
                {
                    0 => Type1,
                    1 => Type2,
                    2 => Type3,
                    3 => Type4,
                    _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
                };
            }
        }

        public bool Contains(Type type)
        {
            return Type1 == type || Type2 == type || Type3 == type || Type4 == type;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            readonly TypeArray _arr;
            int _pointer;

            public Enumerator(TypeArray arr)
            {
                _arr = arr;
                _pointer = -1;
            }

            public bool MoveNext() => ++_pointer < _arr.Length;

            public Type Current => _arr[_pointer];
        }
    }
}