using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

public class ArgReferenceTests
{
    [Fact]
    public void TypeChecker_BareArgReference_EmitsUnqualifiedEventArgReference()
    {
        // Spec §3.5: event args are accessed only via dotted EventName.ArgName.
        // A bare reference to an in-scope arg (no same-name field) is rejected with
        // PRE0163 rather than resolving to the arg. (The dotted form's
        // ArgReference recording is covered by RecordedOnMemberAccessResolution.)
        var src = """
            precept LoanWorkflow
            field StoredAmount as decimal default 0
            state Draft initial
            state Approved
            event Submit(Amount as decimal)
            from Draft on Submit when Amount > 0 -> transition Approved
            """;

        var compilation = Compiler.Compile(src);

        compilation.HasErrors.Should().BeTrue();
        compilation.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.UnqualifiedEventArgReference),
            because: "a bare in-scope event-arg reference must be qualified as EventName.ArgName (PRE0163)");
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
