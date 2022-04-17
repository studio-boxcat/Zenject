using System;

namespace Zenject
{
    // An injectable is a field or property with [Inject] attribute
    // Or a constructor parameter
    public readonly struct InjectableInfo
    {
        // The type of the constructor parameter, field or property
        public readonly Type MemberType;

        // Identifier - most of the time this is null
        // It will match 'foo' in this example:
        //      ... In an installer somewhere:
        //          Container.Bind<Foo>("foo").AsSingle();
        //      ...
        //      ... In a constructor:
        //          public Foo([Inject(Id = "foo") Foo foo)
        public readonly object Identifier;

        // When set to true, this will only look up dependencies in the local container and will not
        // search in parent containers
        public readonly InjectSources SourceType;

        // When optional, null is a valid value to be returned
        public readonly bool Optional;

        public BindingId BindingId => new(MemberType, Identifier);

        public InjectableInfo(Type memberType)
            : this()
        {
            MemberType = memberType;
        }

        public InjectableInfo(Type memberType, object identifier)
            : this(memberType)
        {
            Identifier = identifier;
        }

        public InjectableInfo(Type memberType, object identifier, bool optional) : this()
        {
            MemberType = memberType;
            Identifier = identifier;
            Optional = optional;
        }

        public InjectableInfo(Type memberType, object identifier, InjectSources sourceType, bool optional = false)
        {
            MemberType = memberType;
            Identifier = identifier;
            SourceType = sourceType;
            Optional = optional;
        }

        public InjectableInfo(BindingId bindingId) : this()
        {
            MemberType = bindingId.Type;
            Identifier = bindingId.Identifier;
        }

        public InjectableInfo MutateMemberType(Type memberType)
        {
            return new InjectableInfo(memberType, Identifier, SourceType, Optional);
        }
    }
}