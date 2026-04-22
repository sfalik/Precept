using System.Collections.Immutable;

namespace Precept.Runtime;

public sealed record class Version(
    Precept                              Precept,
    string                               State,
    ImmutableDictionary<string, object?> Data
)
{
    public Version Fire(string eventName, ImmutableDictionary<string, object?>? args = null)
        => throw new NotImplementedException();

    public Version Edit(string field, object? value)
        => throw new NotImplementedException();
}
