namespace Precept.Runtime;

/// <summary>
/// A currency-per-currency ratio. Enables explicit, governed currency conversion.
/// The compiler verifies that currency pairs match during conversion.
/// Rates are always positive — zero and negative exchange rates are invalid
/// configurations (D16 Corollary 2).
/// </summary>
public readonly record struct ExchangeRate(decimal Rate, Currency From, Currency To);
