namespace Precept;

/// <summary>
/// Suppresses PRECEPT0018 for an enum member at value 0.
/// Apply this when zero-initialization is intentional — e.g., a "don't-care" default
/// or a documented initial state. The attribute signals that default(T) routing to
/// this member was a deliberate design choice, not an accident.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class AllowZeroDefaultAttribute : Attribute { }
