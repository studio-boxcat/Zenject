using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    // Zero params

    [NoReflectionBaking]
    public class MethodProviderWithContainer<TValue> : IProvider
    {
        readonly Func<DiContainer, TValue> _method;

        public MethodProviderWithContainer(Func<DiContainer, TValue> method)
        {
            _method = method;
        }

        public bool IsCached
        {
            get { return false; }
        }

        public bool TypeVariesBasedOnMemberType
        {
            get { return false; }
        }

        public Type GetInstanceType(InjectContext context)
        {
            return typeof(TValue);
        }

        public void GetAllInstancesWithInjectSplit(
            InjectContext context, List<TypeValuePair> args, out Action injectAction, List<object> buffer)
        {
            Assert.IsEmpty(args);
            Assert.IsNotNull(context);

            Assert.That(typeof(TValue).DerivesFromOrEqual(context.MemberType));

            injectAction = null;
            buffer.Add(_method(context.Container));
        }
    }

    // One params

    [NoReflectionBaking]
    public class MethodProviderWithContainer<TParam1, TValue> : IProvider
    {
        readonly Func<DiContainer, TParam1, TValue> _method;

        public MethodProviderWithContainer(Func<DiContainer, TParam1, TValue> method)
        {
            _method = method;
        }

        public bool IsCached
        {
            get { return false; }
        }

        public bool TypeVariesBasedOnMemberType
        {
            get { return false; }
        }

        public Type GetInstanceType(InjectContext context)
        {
            return typeof(TValue);
        }

        public void GetAllInstancesWithInjectSplit(
            InjectContext context, List<TypeValuePair> args, out Action injectAction, List<object> buffer)
        {
            Assert.IsEqual(args.Count, 1);
            Assert.IsNotNull(context);

            Assert.That(typeof(TValue).DerivesFromOrEqual(context.MemberType));
            Assert.That(args[0].Type.DerivesFromOrEqual(typeof(TParam1)));

            injectAction = null;
            buffer.Add(
                _method(
                    context.Container,
                    (TParam1)args[0].Value));
        }
    }

    // Two params

    [NoReflectionBaking]
    public class MethodProviderWithContainer<TParam1, TParam2, TValue> : IProvider
    {
        readonly Func<DiContainer, TParam1, TParam2, TValue> _method;

        public MethodProviderWithContainer(Func<DiContainer, TParam1, TParam2, TValue> method)
        {
            _method = method;
        }

        public bool IsCached
        {
            get { return false; }
        }

        public bool TypeVariesBasedOnMemberType
        {
            get { return false; }
        }

        public Type GetInstanceType(InjectContext context)
        {
            return typeof(TValue);
        }

        public void GetAllInstancesWithInjectSplit(
            InjectContext context, List<TypeValuePair> args, out Action injectAction, List<object> buffer)
        {
            Assert.IsEqual(args.Count, 2);
            Assert.IsNotNull(context);

            Assert.That(typeof(TValue).DerivesFromOrEqual(context.MemberType));
            Assert.That(args[0].Type.DerivesFromOrEqual(typeof(TParam1)));
            Assert.That(args[1].Type.DerivesFromOrEqual(typeof(TParam2)));

            injectAction = null;
            buffer.Add(
                _method(
                    context.Container,
                    (TParam1)args[0].Value,
                    (TParam2)args[1].Value));
        }
    }
}

