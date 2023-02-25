namespace Zenject
{
    /// <summary>
    /// This interface is used to mark classes that need to be initialized by generated code.
    /// </summary>
    public interface IZenjectInjectable
    {
        void Inject(DependencyProvider dp);
    }

    public static class IZenjectInjectableExtensions
    {
        public static void Inject(
            this IZenjectInjectable thiz, DiContainer diContainer, ArgumentArray extraArgs)
        {
            thiz.Inject(new DependencyProvider(diContainer, extraArgs));
        }
    }
}