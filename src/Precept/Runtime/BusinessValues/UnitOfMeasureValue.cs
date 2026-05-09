namespace Precept.Runtime;

/// <summary>
/// UCUM unit-of-measure identity. The validated UCUM unit code.
/// A lightweight API proxy; the evaluator resolves <see cref="Code"/> to the
/// internal <c>Unit</c> entity via <c>UnitCatalog</c> for dimensional analysis and conversion.
/// Structural characters (<c>/</c>, <c>*</c>, <c>^</c>, <c>.</c>) are invalid in atomic positions.
/// </summary>
public readonly record struct UnitOfMeasure(string Code);
