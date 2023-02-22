using System;

namespace Zenject
{
    // An injectable is a field or property with [Inject] attribute
    // Or a constructor parameter
    public readonly struct InjectSpec
    {
        // The type of the constructor parameter, field or property
        public readonly Type Type;

        // Identifier - most of the time this is null
        // It will match 'foo' in this example:
        //      ... In an installer somewhere:
        //          Container.Bind<Foo>("foo").AsSingle();
        //      ...
        //      ... In a constructor:
        //          public Foo([Inject(Id = "foo") Foo foo)
        public readonly int Identifier;

        // When set to true, this will only look up dependencies in the local container and will not
        // search in parent containers
        public readonly InjectSources SourceType;

        // When optional, null is a valid value to be returned
        public readonly bool Optional;

        public BindingId BindingId => new(Type, Identifier);


        public InjectSpec(Type type, int identifier, InjectSources sourceType, bool optional = false)
        {
            Type = type;
            Identifier = identifier;
            SourceType = sourceType;
            Optional = optional;
        }

        public override string ToString()
        {
            return $"({Type.Name}, {Hasher.ToHumanReadableString(Identifier)}, {SourceType}, {Optional})";
        }
    }
}