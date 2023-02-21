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

        public object Resolve(Type type, int identifier = default, InjectSources sourceType = default)
        {
            if (identifier == default && _extraArgs.TryGetValueWithType(type, out var inst))
                return inst;
            return _diContainer.Resolve(type, identifier, sourceType);
        }

        public object TryResolve(Type type, int identifier = default, InjectSources sourceType = default)
        {
            if (identifier == default && _extraArgs.TryGetValueWithType(type, out var inst))
                return inst;
            return _diContainer.TryResolve(type, identifier, sourceType, out inst) ? inst : null;
        }
    }

    public class DependencyProviderRef
    {
        DiContainer _diContainer;
        ArgumentArray _extraArgs;


        public DependencyProviderRef(DiContainer diContainer, ArgumentArray extraArgs)
        {
            _diContainer = diContainer;
            _extraArgs = extraArgs;
        }

        public void Reset(DiContainer diContainer, ArgumentArray extraArgs)
        {
            _diContainer = diContainer;
            _extraArgs = extraArgs;
        }

        public void Reset()
        {
            _diContainer = default;
            _extraArgs = default;
        }

        public object Resolve(Type type, int identifier = default, InjectSources sourceType = default)
        {
            if (identifier == default && _extraArgs.TryGetValueWithType(type, out var inst))
                return inst;
            return _diContainer.Resolve(type, identifier, sourceType);
        }

        public object TryResolve(Type type, int identifier = default, InjectSources sourceType = default)
        {
            if (identifier == default && _extraArgs.TryGetValueWithType(type, out var inst))
                return inst;
            return _diContainer.TryResolve(type, identifier, sourceType, out inst) ? inst : null;
        }
    }
}