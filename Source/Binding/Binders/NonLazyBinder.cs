namespace Zenject
{
    public class NonLazyBinder
    {
        public readonly BindInfo BindInfo;

        public NonLazyBinder(BindInfo bindInfo)
        {
            BindInfo = bindInfo;
        }

        public NonLazyBinder NonLazy()
        {
            BindInfo.NonLazy = true;
            return this;
        }

        public NonLazyBinder Lazy()
        {
            BindInfo.NonLazy = false;
            return this;
        }
    }
}