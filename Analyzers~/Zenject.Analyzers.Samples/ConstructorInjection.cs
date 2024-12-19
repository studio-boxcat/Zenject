#pragma warning disable CS0169 // Field is never used

namespace Zenject.Analyzers.Samples;

public class ConstructorInjection
{
    [InjectConstructor]
    public ConstructorInjection()
    { }
}