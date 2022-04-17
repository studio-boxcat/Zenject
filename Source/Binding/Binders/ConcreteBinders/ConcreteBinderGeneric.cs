using System;
using System.Collections.Generic;
using System.Linq;
using ModestTree;

namespace Zenject
{
    public class ConcreteBinderGeneric<TContract> : FromBinderGeneric<TContract>
    {
        public ConcreteBinderGeneric(
            DiContainer bindContainer, BindInfo bindInfo,
            BindStatement bindStatement)
            : base(bindContainer, bindInfo, bindStatement)
        {
            ToSelf();
        }

        // Note that this is the default, so not necessary to call
        public FromBinderGeneric<TContract> ToSelf()
        {
            Assert.IsEqual(BindInfo.ToChoice, ToChoices.Self);

            BindInfo.RequireExplicitScope = true;
            SubFinalizer = new ScopableBindingFinalizer(
                BindInfo, (container, type) => new TransientProvider(
                    type, container, BindInfo.Arguments, BindInfo.ConcreteIdentifier));

            return this;
        }

        public FromBinderGeneric<TConcrete> To<TConcrete>()
            where TConcrete : TContract
        {
            BindInfo.ToChoice = ToChoices.Concrete;
            BindInfo.ToTypes.Clear();
            BindInfo.ToTypes.Add(typeof(TConcrete));

            return new FromBinderGeneric<TConcrete>(
                BindContainer, BindInfo, BindStatement);
        }

        public FromBinderNonGeneric To(params Type[] concreteTypes)
        {
            return To((IEnumerable<Type>)concreteTypes);
        }

        public FromBinderNonGeneric To(IEnumerable<Type> concreteTypes)
        {
            BindingUtil.AssertIsDerivedFromTypes(
                concreteTypes, BindInfo.ContractTypes, BindInfo.InvalidBindResponse);

            BindInfo.ToChoice = ToChoices.Concrete;
            BindInfo.ToTypes.Clear();
            BindInfo.ToTypes.AddRange(concreteTypes);

            return new FromBinderNonGeneric(
                BindContainer, BindInfo, BindStatement);
        }
    }
}
