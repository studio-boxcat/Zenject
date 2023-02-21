using System;

namespace Zenject
{
    public readonly struct DependencyProvider
    {
        readonly DiContainer _diContainer;
        readonly ArgumentArray _arguments;


        public DependencyProvider(DiContainer diContainer, ArgumentArray arguments)
        {
            _diContainer = diContainer;
            _arguments = arguments;
        }

        public object Resolve(Type type, int identifier = default, InjectSources sourceType = default)
        {
            if (identifier == default && _arguments.TryGetValueWithType(type, out var inst))
                return inst;
            return _diContainer.Resolve(type, identifier, sourceType);
        }

        public object TryResolve(Type type, int identifier = default, InjectSources sourceType = default)
        {
            if (identifier == default && _arguments.TryGetValueWithType(type, out var inst))
                return inst;
            return _diContainer.TryResolve(type, identifier, sourceType, out inst) ? inst : null;
        }
    }
}