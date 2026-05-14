using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Tests for Slice 7 guard gate diagnostics:
///   PRE0013 OmitDoesNotSupportGuard
///   PRE0014 EventHandlerDoesNotSupportGuard
///   PRE0015 PreEventGuardNotAllowed
///
/// These validate that the parser emits precise, actionable diagnostics when
/// a 'when' guard appears in an invalid position, rather than falling through
/// to generic parse errors.
/// </summary>
public class ParserGuardValidationTests
{
    private static ConstructManifest Parse(string source) =>
        Pipeline.Parser.Parse(Lexer.Lex(source));

    // ════════════════════════════════════════════════════════════════════════════
    //  §1. OmitDeclaration guard gate (PRE0013)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OmitDeclaration_WithGuard_EmitsOmitDoesNotSupportGuard()
    {
        var source = """
            precept Example
            field Notes as string optional
            state Draft initial
            state Done terminal
            event Complete
            in Draft omit Notes when Notes is set
            from Draft on Complete -> transition Done
            """;

        var manifest = Parse(source);

        manifest.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.OmitDoesNotSupportGuard));
    }

    [Fact]
    public void OmitDeclaration_WithoutGuard_NoDiagnostic()
    {
        var source = """
            precept Example
            field Notes as string optional
            state Draft initial
            state Done terminal
            event Complete
            in Draft omit Notes
            from Draft on Complete -> transition Done
            """;

        var manifest = Parse(source);

        manifest.Diagnostics
            .Should().NotContain(d => d.Code == nameof(DiagnosticCode.OmitDoesNotSupportGuard));
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §2. EventHandler guard gate (PRE0014)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EventHandler_WithGuard_EmitsEventHandlerDoesNotSupportGuard()
    {
        var source = """
            precept Example
            field Name as string optional
            event SetName(N as string)
            on SetName when Name != "" -> set Name = SetName.N
            """;

        var manifest = Parse(source);

        manifest.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.EventHandlerDoesNotSupportGuard));
    }

    [Fact]
    public void EventHandler_WithoutGuard_NoDiagnostic()
    {
        var source = """
            precept Example
            field Name as string optional
            event SetName(N as string)
            on SetName -> set Name = SetName.N
            """;

        var manifest = Parse(source);

        manifest.Diagnostics
            .Should().NotContain(d => d.Code == nameof(DiagnosticCode.EventHandlerDoesNotSupportGuard));
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §3. TransitionRow pre-event guard gate (PRE0015)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionRow_GuardBeforeOnEvent_EmitsPreEventGuardNotAllowed()
    {
        var source = """
            precept Example
            field Active as boolean default false
            state Draft initial
            state Done terminal
            event Complete
            from Draft when Active on Complete -> transition Done
            """;

        var manifest = Parse(source);

        manifest.Diagnostics
            .Should().Contain(d => d.Code == nameof(DiagnosticCode.PreEventGuardNotAllowed));
    }

    [Fact]
    public void TransitionRow_GuardAfterOnEvent_NoDiagnostic()
    {
        var source = """
            precept Example
            field Active as boolean default false
            state Draft initial
            state Done terminal
            event Complete
            from Draft on Complete when Active -> transition Done
            """;

        var manifest = Parse(source);

        manifest.Diagnostics
            .Should().NotContain(d => d.Code == nameof(DiagnosticCode.PreEventGuardNotAllowed));
    }
}
