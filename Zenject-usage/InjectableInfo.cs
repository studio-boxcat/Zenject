using System;

namespace Zenject
{
    // An injectable is a field or property with [Inject] attribute
    // Or a constructor parameter
    public struct InjectableInfo
    {
        public readonly bool Optional;
        public readonly object Identifier;

        public readonly InjectSources SourceType;

        // The field type or property type from source code
        public readonly Type MemberType;

        public InjectableInfo(
            bool optional, object identifier, Type memberType,
            InjectSources sourceType)
        {
            Optional = optional;
            MemberType = memberType;
            Identifier = identifier;
            SourceType = sourceType;
        }
    }
}
