using System.Runtime.CompilerServices;

namespace Zenject
{
    /// <summary>
    /// This interface is used to mark classes that need to be initialized by generated code.
    /// </summary>
    public interface IZenjectInjectable
    {
        void Inject(DependencyProvider dp);
    }

    public static class ZenjectExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Inject(this IZenjectInjectable injectable, DiContainer diContainer, ArgumentArray extraArgs)
        {
            injectable.Inject(new DependencyProvider(diContainer, extraArgs));
        }
    }
}