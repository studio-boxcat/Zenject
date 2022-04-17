namespace Zenject
{
    // The given InjectableInfo values here should always be non-null
    public interface IProvider
    {
        // Return an instance which might be not yet injected to.
        // injectAction should handle the actual injection
        // This way, providers that call CreateInstance() can store the instance immediately,
        // and then return that if something gets created during injection that refers back
        // to the newly created instance
        object GetInstance();
    }
}