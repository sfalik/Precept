using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language;

public class Track2PhaseAFunctionCatalogTests
{
    [Fact]
    public void Abs_AllOverloads_ReturnNonnegative_True()
        => Functions.GetMeta(FunctionKind.Abs).Overloads.Should().OnlyContain(overload => overload.ReturnNonnegative);

    [Fact]
    public void Sqrt_Overload_ReturnNonnegative_False()
        => Functions.GetMeta(FunctionKind.Sqrt).Overloads.Single().ReturnNonnegative.Should().BeFalse();
}
