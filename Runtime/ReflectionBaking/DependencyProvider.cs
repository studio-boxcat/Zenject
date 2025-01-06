using System;

namespace Zenject
{
    public readonly struct DependencyProvider
    {
        private readonly DiContainer _diContainer;
        private readonly ArgumentArray _extraArgs;


        public DependencyProvider(DiContainer diContainer, ArgumentArray extraArgs)
        {
            _diContainer = diContainer;
            _extraArgs = extraArgs;
        }

        public DiContainer Container => _diContainer;
        public ArgumentArray ExtraArgs => _extraArgs;

        public object Resolve(Type type, BindId id = default)
        {
            if (id == default && _extraArgs.TryGet(type, out var inst))
                return inst;
            return _diContainer.Resolve(type, id);
        }

        public T TryResolve<T>(BindId id = default)
        {
            if (id == default && _extraArgs.TryGet(out T obj))
                return obj;
            return _diContainer.TryResolve(id, out obj) ? obj : default;
        }

        public void TryResolve<T>(BindId id, ref T value)
        {
            if (id == default && _extraArgs.TryGet(out T temp))
            {
                value = temp;
                return;
            }

            if (_diContainer.TryResolve(id, out temp))
            {
                value = temp;
            }
        }

        public void TryResolve<T>(ref T value) => TryResolve(default, ref value);
    }
}