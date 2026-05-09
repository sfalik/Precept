namespace Precept.Language;

public readonly record struct DimensionVector(
    int Length,
    int Mass,
    int Time,
    int ElectricCurrent,
    int Temperature,
    int AmountOfSubstance,
    int LuminousIntensity)
{
    public static DimensionVector None => new(0, 0, 0, 0, 0, 0, 0);

    public bool IsDimensionless => this == None;

    public DimensionVector Multiply(DimensionVector other) => new(
        Length + other.Length,
        Mass + other.Mass,
        Time + other.Time,
        ElectricCurrent + other.ElectricCurrent,
        Temperature + other.Temperature,
        AmountOfSubstance + other.AmountOfSubstance,
        LuminousIntensity + other.LuminousIntensity);

    public DimensionVector Divide(DimensionVector other) => new(
        Length - other.Length,
        Mass - other.Mass,
        Time - other.Time,
        ElectricCurrent - other.ElectricCurrent,
        Temperature - other.Temperature,
        AmountOfSubstance - other.AmountOfSubstance,
        LuminousIntensity - other.LuminousIntensity);

    public DimensionVector Pow(int exponent) => new(
        Length * exponent,
        Mass * exponent,
        Time * exponent,
        ElectricCurrent * exponent,
        Temperature * exponent,
        AmountOfSubstance * exponent,
        LuminousIntensity * exponent);
}
