using System.Collections.Immutable;
using Precept.Pipeline;

namespace Precept.Runtime;

public sealed class Precept
{
    private Precept() { }

    public static Precept From(CompilationResult compilation)
    {
        if (compilation.HasErrors)
            throw new InvalidOperationException("Cannot create a Precept from a compilation with errors.");

        throw new NotImplementedException();
    }

    public Version From(string state, ImmutableDictionary<string, object?> data)
        => throw new NotImplementedException();
}
