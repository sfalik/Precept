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
/// How a binary operation's result qualifier is derived when the result carries
/// structured qualifier identity beyond raw <see cref="QualifierMatch"/>.
/// </summary>
public enum ResultQualifierPolicy
{
    [Precept.AllowZeroDefault]
    None,
    CompoundUnitCancellation,
    /// <summary>
    /// Result inherits qualifiers from the qualifier-bearing operand in a scalar operation
    /// (e.g., money × decimal → money with same qualifier).
    /// </summary>
    InheritFromQualifiedOperand,
    /// <summary>
    /// Result currency is the exchangerate's ToCurrency.
    /// Used by <c>ExchangeRateTimesMoney</c>.
    /// </summary>
    CurrencyConversion,

    /// <summary>
    /// Result is price: currency inherited from price (left operand),
    /// unit dimension elevated from compound-quantity numerator (right operand).
    /// Used by <c>PriceDivideQuantity</c>.
    /// </summary>
    CompoundDimensionElevation,
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
    ProofRequirement[]? ProofRequirements = null,
    bool HasCIVariant = false,
    DiagnosticCode? CIDiagnosticCode = null,
    ResultQualifierPolicy ResultQualifierPolicy = ResultQualifierPolicy.None)
    : OperationMeta(Kind, Op, Result, Description)
{
    /// <summary>Proof obligations the type checker must verify at call sites.</summary>
    public ProofRequirement[] ProofRequirements { get; } = ProofRequirements ?? [];
}
