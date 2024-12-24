namespace Zenject.Analyzers.Samples;

public partial class ReadonlyInjectField
{
    [Inject] private readonly int _foo;
}