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
    ProofSatisfaction[]? ProofSatisfactions = null)
{
    public ProofSatisfaction[] ProofSatisfactions { get; } = ProofSatisfactions ?? [];

    public sealed record Currency(string CurrencyCode,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.Currency, Origin, Preposition, ProofSatisfactions);

    public sealed record Unit(string UnitCode, string DimensionName,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.Unit, Origin, Preposition, ProofSatisfactions);

    public sealed record Dimension(string DimensionName,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.Of,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.Dimension, Origin, Preposition, ProofSatisfactions);

    public sealed record FromCurrency(string CurrencyCode,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.FromCurrency, Origin, Preposition, ProofSatisfactions);

    public sealed record ToCurrency(string CurrencyCode,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.To,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.ToCurrency, Origin, Preposition, ProofSatisfactions);

    public sealed record Timezone(string TimezoneId,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.Timezone, Origin, Preposition, ProofSatisfactions);

    public sealed record TemporalDimension(PeriodDimension Value,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.Of,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.TemporalDimension, Origin, Preposition, ProofSatisfactions);

    public sealed record TemporalUnit(string UnitName, PeriodDimension DerivedDimension,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.TemporalUnit, Origin, Preposition, ProofSatisfactions);
}
