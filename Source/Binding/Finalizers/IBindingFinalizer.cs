namespace Zenject
{
    public interface IBindingFinalizer
    {
        void FinalizeBinding(DiContainer container);
    }
}
