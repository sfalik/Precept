namespace Precept.Runtime;

/// <summary>
/// Measurement category identity. The validated dimension category name.
/// A lightweight API proxy; the UCUM and temporal partitions govern which names
/// are valid (UCUM partition for <c>quantity</c>/<c>unitofmeasure</c>, temporal
/// partition for <c>period</c>).
/// </summary>
public readonly record struct Dimension(string Name);
