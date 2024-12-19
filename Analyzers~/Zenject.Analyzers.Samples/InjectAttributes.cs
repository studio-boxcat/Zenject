using System;

namespace Zenject.Analyzers.Samples;

public class InjectAttribute : Attribute
{ }

public class InjectOptionalAttribute : Attribute
{ }

public class InjectMethodAttribute : Attribute
{ }

public class InjectConstructorAttribute : Attribute
{ }

public class NoReflectionBakingAttribute : Attribute
{ }