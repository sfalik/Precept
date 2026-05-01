namespace Precept.Language;

/// <summary>
/// Marks a method as handling a specific member of a catalog enum (e.g. ExpressionFormKind.Literal).
/// Used in conjunction with <see cref="HandlesCatalogExhaustivelyAttribute"/> on the containing class.
/// PRECEPT0019 pairs the class marker's declared enum type with these method annotations
/// to verify complete coverage.
/// </summary>
/// <remarks>
/// The constructor accepts <c>object</c> so that any catalog enum value can be passed.
/// At the call site, callers write the typed enum literal for full IntelliSense support:
/// <code>[HandlesForm(ExpressionFormKind.Literal)]</code>
/// The analyzer matches the runtime type of <see cref="Kind"/> against the class marker's
/// declared <c>CatalogEnum</c> type to pair them correctly.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class HandlesFormAttribute : Attribute
{
    public HandlesFormAttribute(object kind) => Kind = kind;

    /// <summary>The catalog enum member this method handles.</summary>
    public object Kind { get; }
}
