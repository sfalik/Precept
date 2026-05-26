namespace Precept.Runtime;

/// <summary>
/// A monetary amount in a single currency. The currency is part of the value's
/// identity — arithmetic respects dimensional rules (you cannot add USD to EUR).
/// Precision is the field author's responsibility, opt-in via a field-level
/// <c>maxplaces</c> constraint. ISO 4217 minor-unit data is available via
/// <c>CurrencyCatalog.Get(code).MinorUnit</c> for author-side rounding decisions;
/// it is metadata, not an implicit type constraint.
/// </summary>
public readonly record struct Money(decimal Amount, Currency Currency);
