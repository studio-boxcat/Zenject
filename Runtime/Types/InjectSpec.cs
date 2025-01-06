using System;

namespace Zenject
{
    // An injectable is a field or property with [Inject] attribute
    // Or a constructor parameter
    internal readonly struct InjectSpec
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
        public readonly BindId Id;

        // When optional, null is a valid value to be returned
        public readonly bool Optional;


        public InjectSpec(Type type, BindId id, bool optional = false)
        {
            Type = type;
            Id = id;
            Optional = optional;
        }

        public override string ToString()
        {
            return $"({Type.Name}, {Id}, {Optional})";
        }
    }
}