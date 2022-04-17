using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Zenject.Internal;

namespace Zenject
{
    public enum ScopeTypes
    {
        Unset,
        Transient,
        Singleton
    }

    public enum ToChoices
    {
        Self,
        Concrete
    }

    public enum InvalidBindResponses
    {
        Assert,
        Skip
    }

    public class BindInfo : IDisposable
    {
        public bool MarkAsCreationBinding;
        public bool MarkAsUniqueSingleton;
        public object ConcreteIdentifier;
        public bool RequireExplicitScope;
        public object Identifier;
        public readonly List<Type> ContractTypes;
        public InvalidBindResponses InvalidBindResponse;
        public bool NonLazy;
        public ToChoices ToChoice;
        public readonly List<Type> ToTypes; // Only relevant with ToChoices.Concrete
        public ScopeTypes Scope;
        [CanBeNull]
        public object[] Arguments;

        public BindInfo()
        {
            ContractTypes = new List<Type>();
            ToTypes = new List<Type>();
            Arguments = null;

            Reset();
        }

        public void Dispose()
        {
            ZenPools.DespawnBindInfo(this);
        }

        public void Reset()
        {
            MarkAsCreationBinding = true;
            MarkAsUniqueSingleton = false;
            ConcreteIdentifier = null;
            RequireExplicitScope = false;
            Identifier = null;
            ContractTypes.Clear();
            InvalidBindResponse = InvalidBindResponses.Assert;
            NonLazy = false;
            ToChoice = ToChoices.Self;
            ToTypes.Clear();
            Scope = ScopeTypes.Unset;
            Arguments = null;
        }
    }
}
