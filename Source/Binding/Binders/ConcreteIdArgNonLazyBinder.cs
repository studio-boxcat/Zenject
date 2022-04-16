namespace Zenject
{
    [NoReflectionBaking]
    public class ConcreteIdArgNonLazyBinder : ArgNonLazyBinder
    {
        public ConcreteIdArgNonLazyBinder(BindInfo bindInfo)
            : base(bindInfo)
        {
        }

        public ArgNonLazyBinder WithConcreteId(object id)
        {
            BindInfo.ConcreteIdentifier = id;
            return this;
        }
    }
}
