namespace Precept.Runtime;

/// <summary>
/// A currency-per-unit ratio. Carries both a currency numerator and a unit
/// denominator. The key operation: <c>price * quantity → money</c>
/// (dimensional cancellation).
/// </summary>
public readonly record struct Price(decimal Amount, Currency Currency, UnitOfMeasure Unit);
