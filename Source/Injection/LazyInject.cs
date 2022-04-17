using ModestTree;

namespace Zenject
{
    public class LazyInject<T>
    {
        readonly DiContainer _container;
        readonly InjectContext _context;

        bool _hasValue;
        T _value;

        public LazyInject(DiContainer container, InjectContext context)
        {
            Assert.DerivesFromOrEqual<T>(context.MemberType);

            _container = container;
            _context = context;
        }

        public T Value
        {
            get
            {
                if (!_hasValue)
                {
                    _value = (T)_container.Resolve(_context);
                    _hasValue = true;
                }

                return _value;
            }
        }

        public bool TryResolve(out T value)
        {
            if (_hasValue)
            {
                value = _value;
                return true;
            }

            if (_container.HasBinding(_context))
            {
                value = _value = (T) _container.Resolve(_context);
                _hasValue = true;
                return true;
            }

            value = default;
            return false;
        }
    }
}
