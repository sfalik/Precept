using System.Text.Json;

namespace Precept.Runtime;

/// <summary>
/// The unified value type at the API boundary. All field and arg values are
/// <see cref="PreceptValue"/> when read back from the runtime.
/// </summary>
/// <remarks>
/// Sealed class hierarchy — concrete subtypes correspond to Precept's declared types
/// (integer, decimal, text, boolean, etc.). Convert via <see cref="ToClr{T}"/> or
/// <see cref="ToJson"/>. Construct via <see cref="FromJson"/> or <see cref="FromClr{T}"/>.
/// </remarks>
public abstract class PreceptValue
{
    private protected PreceptValue() { }

    public static PreceptValue FromJson(JsonElement element)
        => throw new NotImplementedException();

    public static PreceptValue FromClr<T>(T value)
        => throw new NotImplementedException();

    public abstract T ToClr<T>();

    public abstract JsonElement ToJson();
}
