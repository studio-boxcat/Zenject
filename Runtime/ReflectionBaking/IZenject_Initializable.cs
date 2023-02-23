namespace Zenject
{
    /// <summary>
    /// This interface is used to mark classes that need to be initialized by generated code.
    /// </summary>
    public interface IZenject_Initializable
    {
        void Initialize(DependencyProvider dp);
    }
}