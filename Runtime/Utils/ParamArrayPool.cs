using UnityEngine.Assertions;

namespace Zenject
{
    public static class ParamArrayPool
    {
        static readonly Container[] _containers = new Container[16];

        public static object[] Rent(int len)
        {
            // 범위를 넘어서는 경우 생성해서 리턴.
            if (len >= 16)
            {
                return new object[len];
            }

            if (_containers[len].Buffer0 == null)
                _containers[len] = new Container(len);

            return _containers[len].Rent();
        }

        public static void Release(object[] arr)
        {
            var len = arr.Length;

            // 범위를 넘어서는 경우 무시.
            if (len >= 16)
            {
                return;
            }

            _containers[len].Release(arr);
        }

        struct Container
        {
            public readonly object[] Buffer0;
            public readonly object[] Buffer1;
            public readonly object[] Buffer2;
            public readonly object[] Buffer3;
            public bool Rented0;
            public bool Rented1;
            public bool Rented2;
            public bool Rented3;

            public Container(int count)
            {
                Buffer0 = new object[count];
                Buffer1 = new object[count];
                Buffer2 = new object[count];
                Buffer3 = new object[count];

                Rented0 = false;
                Rented1 = false;
                Rented2 = false;
                Rented3 = false;
            }

            public object[] Rent()
            {
                if (Rented0 == false)
                {
                    Rented0 = true;
                    return Buffer0;
                }

                if (Rented1 == false)
                {
                    Rented1 = true;
                    return Buffer1;
                }

                if (Rented2 == false)
                {
                    Rented2 = true;
                    return Buffer2;
                }

                if (Rented3 == false)
                {
                    Rented3 = true;
                    return Buffer3;
                }

                return new object[Buffer0.Length];
            }

            public void Release(object[] arr)
            {
                Assert.AreEqual(Buffer0.Length, arr.Length);

                if (Buffer0 == arr)
                {
                    Rented0 = false;
                    return;
                }

                if (Buffer1 == arr)
                {
                    Rented1 = false;
                    return;
                }

                if (Buffer2 == arr)
                {
                    Rented2 = false;
                    return;
                }

                if (Buffer3 == arr)
                {
                    Rented3 = false;
                    return;
                }

                return;
            }
        }
    }
}