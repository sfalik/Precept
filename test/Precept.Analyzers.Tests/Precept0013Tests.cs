using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0013Tests
{
    // ── Shared stubs ────────────────────────────────────────────────────────

    private const string ActionStubs = @"
namespace Precept.Language
{
    public enum ActionKind { Set, Add, Remove, Clear }
    public enum TokenKind { Set, Add, Remove, Clear }
    public enum ConstructKind { EventDeclaration, StateAction, TransitionRow }
    public enum TypeKind { String, Set }

    public sealed record TokenMeta(TokenKind Kind, string Text);

    public static class Tokens
    {
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Set    => new(kind, ""set""),
            TokenKind.Add    => new(kind, ""add""),
            TokenKind.Remove => new(kind, ""remove""),
            TokenKind.Clear  => new(kind, ""clear""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public record TypeTarget(TypeKind? Kind);
    public sealed record ActionMeta(
        ActionKind Kind, TokenMeta Token, string Description,
        TypeTarget[] ApplicableTo, bool ValueRequired = false,
        ConstructKind[] AllowedIn = null)
    {
        public ConstructKind[] AllowedIn { get; } = AllowedIn ?? System.Array.Empty<ConstructKind>();
    }
";

    private const string CloseBrace = @"
}";

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0013a — Inline Token
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Token_ViaGetMeta_NoDiagnostic()
    {
        var source = ActionStubs + @"
    public static class Actions
    {
        public static ActionMeta GetMeta(ActionKind kind) => kind switch
        {
            ActionKind.Set => new(kind, Tokens.GetMeta(TokenKind.Set), ""set a value"",
                new TypeTarget[] { }, ValueRequired: true,
                AllowedIn: new[] { ConstructKind.EventDeclaration }),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0013ActionsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0013ActionsCrossRef.DiagnosticId_InlineToken)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Token_InlineNew_ReportsWarning()
    {
        var source = ActionStubs + @"
    public static class Actions
    {
        public static ActionMeta GetMeta(ActionKind kind) => kind switch
        {
            ActionKind.Set => new(kind, new TokenMeta(TokenKind.Set, ""set""), ""set a value"",
                new TypeTarget[] { }, ValueRequired: true,
                AllowedIn: new[] { ConstructKind.EventDeclaration }),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0013ActionsCrossRef>(source);
        var inlineDiags = diagnostics.Where(d => d.Id == PRECEPT0013ActionsCrossRef.DiagnosticId_InlineToken).ToList();
        inlineDiags.Should().ContainSingle();
        inlineDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0013b — Empty AllowedIn
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllowedIn_WithElements_NoDiagnostic()
    {
        var source = ActionStubs + @"
    public static class Actions
    {
        public static ActionMeta GetMeta(ActionKind kind) => kind switch
        {
            ActionKind.Set => new(kind, Tokens.GetMeta(TokenKind.Set), ""set a value"",
                new TypeTarget[] { }, ValueRequired: true,
                AllowedIn: new[] { ConstructKind.EventDeclaration, ConstructKind.StateAction }),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0013ActionsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0013ActionsCrossRef.DiagnosticId_EmptyAllowed)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task AllowedIn_EmptyInline_ReportsError()
    {
        var source = ActionStubs + @"
    public static class Actions
    {
        public static ActionMeta GetMeta(ActionKind kind) => kind switch
        {
            ActionKind.Set => new(kind, Tokens.GetMeta(TokenKind.Set), ""set a value"",
                new TypeTarget[] { }, ValueRequired: true,
                AllowedIn: new ConstructKind[] { }),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0013ActionsCrossRef>(source);
        var emptyDiags = diagnostics.Where(d => d.Id == PRECEPT0013ActionsCrossRef.DiagnosticId_EmptyAllowed).ToList();
        emptyDiags.Should().ContainSingle();
        emptyDiags[0].GetMessage().Should().Contain("Set");
    }

    [Fact]
    public async Task AllowedIn_NotSpecified_ReportsError()
    {
        // AllowedIn omitted entirely — defaults to empty.
        var source = ActionStubs + @"
    public static class Actions
    {
        public static ActionMeta GetMeta(ActionKind kind) => kind switch
        {
            ActionKind.Set => new(kind, Tokens.GetMeta(TokenKind.Set), ""set a value"",
                new TypeTarget[] { }, ValueRequired: true),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0013ActionsCrossRef>(source);
        var emptyDiags = diagnostics.Where(d => d.Id == PRECEPT0013ActionsCrossRef.DiagnosticId_EmptyAllowed).ToList();
        emptyDiags.Should().ContainSingle();
    }

    [Fact]
    public async Task AllowedIn_SharedNonEmptyArray_NoDiagnostic()
    {
        var source = ActionStubs + @"
    public static class Actions
    {
        private static readonly ConstructKind[] AllContexts =
            new[] { ConstructKind.EventDeclaration, ConstructKind.StateAction, ConstructKind.TransitionRow };

        public static ActionMeta GetMeta(ActionKind kind) => kind switch
        {
            ActionKind.Set => new(kind, Tokens.GetMeta(TokenKind.Set), ""set"",
                new TypeTarget[] { }, AllowedIn: AllContexts),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0013ActionsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0013ActionsCrossRef.DiagnosticId_EmptyAllowed)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task AllowedIn_SharedEmptyArray_ReportsError()
    {
        var source = ActionStubs + @"
    public static class Actions
    {
        private static readonly ConstructKind[] NoContexts = new ConstructKind[] { };

        public static ActionMeta GetMeta(ActionKind kind) => kind switch
        {
            ActionKind.Set => new(kind, Tokens.GetMeta(TokenKind.Set), ""set"",
                new TypeTarget[] { }, AllowedIn: NoContexts),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0013ActionsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0013ActionsCrossRef.DiagnosticId_EmptyAllowed)
            .Should().ContainSingle();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Scope guards
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task WrongEnumKind_NoDiagnostic()
    {
        var source = @"
namespace Precept.Language
{
    public enum TypeKind { String }
    public sealed record TypeMeta(TypeKind Kind, string Description);

    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String => new(kind, ""text""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0013ActionsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task WrongNamespace_NoDiagnostic()
    {
        var source = @"
namespace Other
{
    public enum ActionKind { Set }
    public enum TokenKind { Set }
    public enum ConstructKind { EventDeclaration }

    public sealed record TokenMeta(TokenKind Kind, string Text);
    public record TypeTarget(object Kind);
    public sealed record ActionMeta(
        ActionKind Kind, TokenMeta Token, string Description,
        TypeTarget[] ApplicableTo, ConstructKind[] AllowedIn = null)
    {
        public ConstructKind[] AllowedIn { get; } = AllowedIn ?? System.Array.Empty<ConstructKind>();
    }

    public static class Actions
    {
        public static ActionMeta GetMeta(ActionKind kind) => kind switch
        {
            ActionKind.Set => new(kind, new TokenMeta(TokenKind.Set, ""set""), ""set"",
                new TypeTarget[] { }),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0013ActionsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Edge: discard arm
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DiscardArm_NoDiagnostic()
    {
        var source = ActionStubs + @"
    public static class Actions
    {
        public static ActionMeta GetMeta(ActionKind kind) => kind switch
        {
            _ => new ActionMeta(kind, Tokens.GetMeta(TokenKind.Set), ""fallback"",
                new TypeTarget[] { }, AllowedIn: new[] { ConstructKind.EventDeclaration }),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0013ActionsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }
}
