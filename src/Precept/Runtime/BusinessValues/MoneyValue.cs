namespace Precept.Runtime;

/// <summary>
/// A monetary amount in a single currency. The currency is part of the value's
/// identity — arithmetic respects dimensional rules (you cannot add USD to EUR).
/// Precision is governed by the ISO 4217 minor unit of <see cref="Currency"/>
/// unless overridden by a field-level <c>maxplaces</c> constraint (D10).
/// </summary>
public readonly record struct Money(decimal Amount, Currency Currency);
