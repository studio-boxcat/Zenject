using System;

namespace Zenject
{
    public readonly struct DependencyProvider
    {
        readonly DiContainer _diContainer;
        readonly ArgumentArray _extraArgs;


        public DependencyProvider(DiContainer diContainer, ArgumentArray extraArgs)
        {
            _diContainer = diContainer;
            _extraArgs = extraArgs;
        }

        public DiContainer Container => _diContainer;
        public ArgumentArray ExtraArgs => _extraArgs;

        public object Resolve(Type type, int identifier = default)
        {
            if (identifier == default && _extraArgs.TryGetValueWithType(type, out var inst))
                return inst;
            return _diContainer.Resolve(type, identifier);
        }

        public T TryResolve<T>(int identifier = default)
        {
            if (identifier == default && _extraArgs.TryGetValueWithType(out T obj))
                return obj;
            return _diContainer.TryResolve(identifier, out obj) ? obj : default;
        }

        public void TryResolve<T>(int identifier, ref T value)
        {
            if (identifier == default && _extraArgs.TryGetValueWithType(out T temp))
            {
                value = temp;
                return;
            }

            if (_diContainer.TryResolve(identifier, out temp))
            {
                value = temp;
            }
        }

        public void TryResolve<T>(ref T value) => TryResolve(default, ref value);
    }
}