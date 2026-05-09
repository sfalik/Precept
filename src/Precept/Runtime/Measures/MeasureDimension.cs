using Precept.Language;

namespace Precept.Runtime.Measures;

internal readonly record struct MeasureDimension(DimensionVector Vector)
{
    public static MeasureDimension FromVector(DimensionVector vector)
        => throw new NotImplementedException();
}
