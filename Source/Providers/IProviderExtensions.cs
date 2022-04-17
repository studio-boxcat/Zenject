using JetBrains.Annotations;

namespace Zenject
{
    public static class IProviderExtensions
    {
        [CanBeNull]
        public static object GetInstance(this IProvider creator, InjectableInfo context)
        {
            var instance = creator.GetInstanceWithInjectSplit(context, out var injectAction);
            injectAction?.Invoke();
            return instance;
        }
    }
}
