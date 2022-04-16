namespace Zenject
{
    [NoReflectionBaking]
    public class ConcreteIdArgCopyNonLazyBinder : ArgCopyNonLazyBinder
    {
        public ConcreteIdArgCopyNonLazyBinder(BindInfo bindInfo)
            : base(bindInfo)
        {
        }

        public ArgCopyNonLazyBinder WithConcreteId(object id)
        {
            BindInfo.ConcreteIdentifier = id;
            return this;
        }
    }
}
