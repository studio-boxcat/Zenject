namespace Zenject
{
    [NoReflectionBaking]
    public class WithKernelScopeConcreteIdArgCopyNonLazyBinder : ScopeConcreteIdArgCopyNonLazyBinder
    {
        SubContainerCreatorBindInfo _subContainerBindInfo;

        public WithKernelScopeConcreteIdArgCopyNonLazyBinder(
            SubContainerCreatorBindInfo subContainerBindInfo, BindInfo bindInfo)
            : base(bindInfo)
        {
            _subContainerBindInfo = subContainerBindInfo;
        }

        public ScopeConcreteIdArgCopyNonLazyBinder WithKernel()
        {
            _subContainerBindInfo.CreateKernel = true;
            return this;
        }

        // This would be used in cases where you want to control the execution order for the
        // subcontainer
        public ScopeConcreteIdArgCopyNonLazyBinder WithKernel<TKernel>()
            where TKernel : Kernel
        {
            _subContainerBindInfo.CreateKernel = true;
            _subContainerBindInfo.KernelType = typeof(TKernel);
            return this;
        }
    }
}
