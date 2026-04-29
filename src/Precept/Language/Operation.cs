namespace Precept.Language;

/// <summary>Describes a single operand slot in an operation.</summary>
public record ParameterMeta(TypeKind Kind, string? Name = null);

/// <summary>
/// Qualifier matching mode for operations whose result type depends on
/// whether the operands share the same qualifier (currency, dimension, etc.).
/// </summary>
public enum QualifierMatch
{
    /// <summary>No qualifier inspection needed. Default for ~95% of entries.</summary>
    [Precept.AllowZeroDefault]
    Any,
    /// <summary>Operand qualifiers are equal (same currency, same dimension).</summary>
    Same,
    /// <summary>Operand qualifiers differ (different currency, different dimension).</summary>
    Different,
}

/// <summary>
/// Base metadata for a typed operator combination. Discriminated union:
/// <see cref="UnaryOperationMeta"/> and <see cref="BinaryOperationMeta"/>.
/// </summary>
public abstract record OperationMeta(
    OperationKind Kind,
    OperatorKind Op,
    TypeKind Result,
    string Description);

/// <summary>Unary operation metadata (e.g., -integer → integer, not boolean → boolean).</summary>
public sealed record UnaryOperationMeta(
    OperationKind Kind,
    OperatorKind Op,
    ParameterMeta Operand,
    TypeKind Result,
    string Description)
    : OperationMeta(Kind, Op, Result, Description);

/// <summary>Binary operation metadata (e.g., money * decimal → money).</summary>
public sealed record BinaryOperationMeta(
    OperationKind Kind,
    OperatorKind Op,
    ParameterMeta Lhs,
    ParameterMeta Rhs,
    TypeKind Result,
    string Description,
    bool BidirectionalLookup = false,
    QualifierMatch Match = QualifierMatch.Any,
    ProofRequirement[]? ProofRequirements = null)
    : OperationMeta(Kind, Op, Result, Description)
{
    /// <summary>Proof obligations the type checker must verify at call sites.</summary>
    public ProofRequirement[] ProofRequirements { get; } = ProofRequirements ?? [];
}
