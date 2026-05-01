namespace Precept;

/// <summary>
/// Marks a pipeline class as requiring exhaustive coverage of every member in a catalog enum.
/// PRECEPT0019 audits decorated classes: for each member of <paramref name="catalogEnum"/>,
/// at least one method in this class must be annotated with <c>[HandlesForm(EnumType.Member)]</c>.
/// </summary>
/// <remarks>
/// This is the catalog-agnostic class marker for the annotation-bridge pattern.
/// It works for any catalog enum (ExpressionFormKind, OperatorKind, etc.) —
/// the analyzer discovers which enum to enforce from the <see cref="CatalogEnum"/> property.
/// </remarks>
/// <example>
/// <code>
/// [HandlesCatalogExhaustively(typeof(ExpressionFormKind))]
/// public sealed class Parser { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class HandlesCatalogExhaustivelyAttribute : Attribute
{
    public HandlesCatalogExhaustivelyAttribute(Type catalogEnum) => CatalogEnum = catalogEnum;

    /// <summary>The catalog enum type whose members must all be handled.</summary>
    public Type CatalogEnum { get; }
}
