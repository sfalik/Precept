namespace Precept.Language;

public enum QualifierOrigin
{
    Explicit = 1,
    Derived  = 2,
    Baseline = 3,
}

public abstract record DeclaredQualifierMeta(
    QualifierAxis Axis,
    QualifierOrigin Origin,
    TokenKind? Preposition,
    ProofSatisfaction[]? ProofSatisfactions = null,
    string? SourceFieldName = null)
{
    public ProofSatisfaction[] ProofSatisfactions { get; } = ProofSatisfactions ?? [];

    /// <summary>
    /// The name of the source field whose value fills this qualifier at runtime (e.g. "CatalogCurrency").
    /// Populated at type-check time for interpolated qualifiers and at proof time via CreateQualifierFromSlotExpression.
    /// Used as the primary equality criterion in QualifiersSymbolicallyEqual (Option B — field-identity comparison).
    /// </summary>
    public string? SourceFieldName { get; } = SourceFieldName;

    public sealed record Currency(string CurrencyCode,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null,
        string? SourceFieldName = null)
        : DeclaredQualifierMeta(QualifierAxis.Currency, Origin, Preposition, ProofSatisfactions, SourceFieldName);

    public sealed record Unit(string UnitCode, string DimensionName,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null,
        string? SourceFieldName = null)
        : DeclaredQualifierMeta(QualifierAxis.Unit, Origin, Preposition, ProofSatisfactions, SourceFieldName);

    public sealed record Dimension(string DimensionName,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.Of,
        ProofSatisfaction[]? ProofSatisfactions = null,
        string? SourceFieldName = null)
        : DeclaredQualifierMeta(QualifierAxis.Dimension, Origin, Preposition, ProofSatisfactions, SourceFieldName);

    public sealed record FromCurrency(string CurrencyCode,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null,
        string? SourceFieldName = null)
        : DeclaredQualifierMeta(QualifierAxis.FromCurrency, Origin, Preposition, ProofSatisfactions, SourceFieldName);

    public sealed record ToCurrency(string CurrencyCode,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.To,
        ProofSatisfaction[]? ProofSatisfactions = null,
        string? SourceFieldName = null)
        : DeclaredQualifierMeta(QualifierAxis.ToCurrency, Origin, Preposition, ProofSatisfactions, SourceFieldName);

    public sealed record Timezone(string TimezoneId,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null,
        string? SourceFieldName = null)
        : DeclaredQualifierMeta(QualifierAxis.Timezone, Origin, Preposition, ProofSatisfactions, SourceFieldName);

    public sealed record TemporalDimension(PeriodDimension Value,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.Of,
        ProofSatisfaction[]? ProofSatisfactions = null,
        string? SourceFieldName = null)
        : DeclaredQualifierMeta(QualifierAxis.TemporalDimension, Origin, Preposition, ProofSatisfactions, SourceFieldName);

    public sealed record TemporalUnit(string UnitName, PeriodDimension DerivedDimension,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null,
        string? SourceFieldName = null)
        : DeclaredQualifierMeta(QualifierAxis.TemporalUnit, Origin, Preposition, ProofSatisfactions, SourceFieldName);
}
