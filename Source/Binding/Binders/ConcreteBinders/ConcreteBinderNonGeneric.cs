using System;
using ModestTree;

namespace Zenject
{
    public class ConcreteBinderNonGeneric : FromBinderNonGeneric
    {
        public ConcreteBinderNonGeneric(
            DiContainer bindContainer, BindInfo bindInfo,
            BindStatement bindStatement)
            : base(bindContainer, bindInfo, bindStatement)
        {
            ToSelf();
        }

        // Note that this is the default, so not necessary to call
        public FromBinderNonGeneric ToSelf()
        {
            Assert.IsEqual(BindInfo.ToChoice, ToChoices.Self);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo, (container, type) => new TransientProvider(
                    type, container, BindInfo.Arguments));

            return this;
        }

        public FromBinderNonGeneric To<TConcrete>()
        {
            return To(typeof(TConcrete));
        }

        public FromBinderNonGeneric To(Type concreteType)
        {
            BindInfo.ToChoice = ToChoices.Concrete;
            BindInfo.ToType = concreteType;

            if (BindInfo.ToType != null && BindInfo.ContractTypes.Count > 1)
            {
                // Be more lenient in this case to behave similar to convention based bindings
                BindInfo.InvalidBindResponse = InvalidBindResponses.Skip;
            }
            else
            {
                BindingUtil.AssertIsDerivedFromTypes(concreteType, BindInfo.ContractTypes, BindInfo.InvalidBindResponse);
            }

            return this;
        }
    }
}
