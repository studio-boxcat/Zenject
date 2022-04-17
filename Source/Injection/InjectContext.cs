using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModestTree;
using Zenject.Internal;

namespace Zenject
{
    [NoReflectionBaking]
    public class InjectContext : IDisposable
    {
        public BindingId BindingId => new(MemberType, Identifier);

        // The type of the object which is having its members injected
        // NOTE: This is null for root calls to Resolve<> or Instantiate<>
        public Type ObjectType;

        // Parent context that triggered the creation of ObjectType
        // This can be used for very complex conditions using parent info such as identifiers, types, etc.
        // Note that ParentContext.MemberType is not necessarily the same as ObjectType,
        // since the ObjectType could be a derived type from ParentContext.MemberType
        public InjectContext ParentContext;

        // The instance which is having its members injected
        // Note that this is null when injecting into the constructor
        public object ObjectInstance;

        // Identifier - most of the time this is null
        // It will match 'foo' in this example:
        //      ... In an installer somewhere:
        //          Container.Bind<Foo>("foo").AsSingle();
        //      ...
        //      ... In a constructor:
        //          public Foo([Inject(Id = "foo") Foo foo)
        public object Identifier;

        // The type of the constructor parameter, field or property
        public Type MemberType;

        // When optional, null is a valid value to be returned
        public bool Optional;

        // When set to true, this will only look up dependencies in the local container and will not
        // search in parent containers
        public InjectSources SourceType;

        public object ConcreteIdentifier;

        // The container used for this injection
        public DiContainer Container;

        public InjectContext()
        {
            Reset();
        }

        public InjectContext(DiContainer container, Type memberType)
            : this()
        {
            Container = container;
            MemberType = memberType;
        }

        public InjectContext(DiContainer container, Type memberType, object identifier)
            : this(container, memberType)
        {
            Identifier = identifier;
        }

        public InjectContext(DiContainer container, Type memberType, object identifier, bool optional)
            : this(container, memberType, identifier)
        {
            Optional = optional;
        }

        public void Dispose()
        {
            ZenPools.DespawnInjectContext(this);
        }

        public void Reset()
        {
            ObjectType = null;
            ParentContext = null;
            ObjectInstance = null;
            Optional = false;
            SourceType = InjectSources.Any;
            Container = null;
            Identifier = default;
            MemberType = default;
        }

        public IEnumerable<InjectContext> ParentContexts
        {
            get
            {
                if (ParentContext == null)
                {
                    yield break;
                }

                yield return ParentContext;

                foreach (var context in ParentContext.ParentContexts)
                {
                    yield return context;
                }
            }
        }

        public IEnumerable<InjectContext> ParentContextsAndSelf
        {
            get
            {
                yield return this;

                foreach (var context in ParentContexts)
                {
                    yield return context;
                }
            }
        }

        public InjectContext CreateSubContext(Type memberType, object identifier)
        {
            var subContext = new InjectContext();

            subContext.ParentContext = this;
            subContext.Identifier = identifier;
            subContext.MemberType = memberType;

            // Clear these
            subContext.ConcreteIdentifier = null;

            // Inherit these ones by default
            subContext.ObjectType = ObjectType;
            subContext.ObjectInstance = ObjectInstance;
            subContext.Optional = Optional;
            subContext.SourceType = SourceType;
            subContext.Container = Container;

            return subContext;
        }

        public InjectContext Clone()
        {
            var clone = new InjectContext();

            clone.ObjectType = ObjectType;
            clone.ParentContext = ParentContext;
            clone.ConcreteIdentifier = ConcreteIdentifier;
            clone.ObjectInstance = ObjectInstance;
            clone.Identifier = Identifier;
            clone.MemberType = MemberType;
            clone.Optional = Optional;
            clone.SourceType = SourceType;
            clone.Container = Container;

            return clone;
        }

        // This is very useful to print out for debugging purposes
        public string GetObjectGraphString()
        {
            var result = new StringBuilder();

            foreach (var context in ParentContextsAndSelf.Reverse())
            {
                if (context.ObjectType == null)
                {
                    continue;
                }

                result.AppendLine(context.ObjectType.PrettyName());
            }

            return result.ToString();
        }
    }
}
