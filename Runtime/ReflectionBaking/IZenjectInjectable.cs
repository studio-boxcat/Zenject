using System.Runtime.CompilerServices;

namespace Zenject
{
    /// <summary>
    /// This interface is used to mark classes that need to be initialized by generated code.
    /// </summary>
    public interface IZenjectInjectable
    {
        // For types with no field injection & no method injection, default implementation (empty) will be used.
        void Inject(DependencyProvider dp) { }
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