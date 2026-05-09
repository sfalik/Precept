namespace Precept.Runtime;

/// <summary>
/// A numeric value with a unit of measure. The unit is part of the value's
/// identity — <c>'5 kg'</c> and <c>'5 lbs'</c> are different quantities.
/// Arithmetic respects dimensional rules (you cannot add kilograms to meters).
/// </summary>
public readonly record struct Quantity(decimal Amount, UnitOfMeasure Unit);
