#pragma warning disable CS0169 // Field is never used

namespace Zenject.Analyzers.Samples;

public class FiendInjection
{
    [Inject] private int _foo;
    [InjectOptional] private int _bar;
}