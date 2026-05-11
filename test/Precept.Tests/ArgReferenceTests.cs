using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

public class ArgReferenceTests
{
    [Fact]
    public void TypeChecker_ArgReference_RecordedOnIdentifierResolution()
    {
        var src = """
            precept LoanWorkflow
            field StoredAmount as decimal default 0
            state Draft initial
            state Approved
            event Submit(Amount as decimal)
            from Draft on Submit when Amount > 0 -> transition Approved
            """;

        var compilation = Compiler.Compile(src);

        compilation.HasErrors.Should().BeFalse();
        compilation.Semantics.ArgReferences.Should().Contain(
            r => r.Arg.EventName == "Submit" && r.Arg.Name == "Amount",
            because: "unqualified event-arg identifier resolution should record semantic-token sites");
    }

    [Fact]
    public void TypeChecker_ArgReference_RecordedOnMemberAccessResolution()
    {
        var src = """
            precept LoanWorkflow
            field StoredAmount as decimal default 0
            state Draft initial
            state Approved
            event Submit(Amount as decimal)
            from Draft on Submit when Submit.Amount > 0 -> transition Approved
            """;

        var compilation = Compiler.Compile(src);

        compilation.HasErrors.Should().BeFalse();
        compilation.Semantics.ArgReferences.Should().Contain(
            r => r.Arg.EventName == "Submit" && r.Arg.Name == "Amount",
            because: "qualified event-arg member access should record semantic-token sites");
    }

    [Fact]
    public void TypeChecker_ArgReference_SiteSpanMatchesSource()
    {
        var src = """
            precept LoanWorkflow
            field StoredAmount as decimal default 0
            state Draft initial
            state Approved
            event Submit(Amount as decimal)
            from Draft on Submit when Submit.Amount > 0 -> transition Approved
            """;

        var compilation = Compiler.Compile(src);
        var argReference = compilation.Semantics.ArgReferences.Single(r => r.Arg.Name == "Amount");

        compilation.HasErrors.Should().BeFalse();
        src.Substring(argReference.Site.Offset, argReference.Site.Length).Should().Be("Amount");
    }
}
