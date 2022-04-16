namespace Zenject
{
    [NoReflectionBaking]
    public class NullBindingFinalizer : IBindingFinalizer
    {
        public void FinalizeBinding(DiContainer container)
        {
            // Do nothing
        }
    }
}

