namespace Zenject
{
    public interface IInstaller
    {
        DiContainer Container { get; set; }
        void InstallBindings();
    }
}