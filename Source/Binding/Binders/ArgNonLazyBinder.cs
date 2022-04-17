namespace Zenject
{
    public class ArgNonLazyBinder : NonLazyBinder
    {
        public ArgNonLazyBinder(BindInfo bindInfo)
            : base(bindInfo)
        {
        }

        public NonLazyBinder WithArguments(params object[] args)
        {
            BindInfo.Arguments = args;
            return this;
        }
    }
}