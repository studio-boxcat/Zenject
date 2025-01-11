namespace Zenject.Analyzers.Samples;

public partial class InjectMethodName
{
    [InjectMethod]
    public void InjectMethod()
    { }

    [InjectMethod]
    public void Zenject_Constructor(int foo)
    { }
}