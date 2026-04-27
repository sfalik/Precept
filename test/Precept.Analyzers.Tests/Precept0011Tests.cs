using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0011Tests
{
    // ── Shared stubs ────────────────────────────────────────────────────────

    private const string ModStubs = @"
namespace Precept.Language
{
    public enum ModifierKind { Nonnegative, Positive, Nonzero, Success, Warning, Error, Write, Read, Omit }
    public enum TokenKind { Nonnegative, Positive, Nonzero, Success, Warning, Error, Write, Read, Omit }
    public enum ModifierCategory { Structural, Semantic }
    public enum TypeKind { Integer }

    public sealed record TokenMeta(TokenKind Kind, string Text);

    public static class Tokens
    {
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Nonnegative => new(kind, ""nonnegative""),
            TokenKind.Positive    => new(kind, ""positive""),
            TokenKind.Nonzero     => new(kind, ""nonzero""),
            TokenKind.Success     => new(kind, ""success""),
            TokenKind.Warning     => new(kind, ""warning""),
            TokenKind.Error       => new(kind, ""error""),
            TokenKind.Write       => new(kind, ""write""),
            TokenKind.Read        => new(kind, ""read""),
            TokenKind.Omit        => new(kind, ""omit""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public record TypeTarget(TypeKind? Kind);

    public abstract record ModifierMeta(
        ModifierKind Kind, TokenMeta Token, string Description,
        ModifierCategory Category, ModifierKind[] MutuallyExclusiveWith = null);

    public sealed record FieldModifierMeta(
        ModifierKind Kind, TokenMeta Token, string Description,
        ModifierCategory Category, TypeTarget[] ApplicableTo,
        bool HasValue = false, ModifierKind[] Subsumes = null,
        string HoverDescription = null, ModifierKind[] MutuallyExclusiveWith = null)
        : ModifierMeta(Kind, Token, Description, Category, MutuallyExclusiveWith);

    public sealed record StateModifierMeta(
        ModifierKind Kind, TokenMeta Token, string Description,
        ModifierCategory Category, ModifierKind[] MutuallyExclusiveWith = null)
        : ModifierMeta(Kind, Token, Description, Category, MutuallyExclusiveWith);
";

    private const string CloseBrace = @"
}";

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0011a — Subsumes self-reference
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Subsumes_OtherModifiers_NoDiagnostic()
    {
        var source = ModStubs + @"
    public static class Modifiers
    {
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Positive => new FieldModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Positive), ""positive"",
                ModifierCategory.Structural, new TypeTarget[] { },
                Subsumes: [ModifierKind.Nonnegative, ModifierKind.Nonzero]),
            ModifierKind.Nonnegative => new FieldModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Nonnegative), ""nonneg"",
                ModifierCategory.Structural, new TypeTarget[] { }),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0011ModifiersCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0011ModifiersCrossRef.DiagnosticId_SubsumesSelf)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Subsumes_SelfReference_ReportsError()
    {
        var source = ModStubs + @"
    public static class Modifiers
    {
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Positive => new FieldModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Positive), ""positive"",
                ModifierCategory.Structural, new TypeTarget[] { },
                Subsumes: [ModifierKind.Positive]),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0011ModifiersCrossRef>(source);
        var selfDiags = diagnostics.Where(d => d.Id == PRECEPT0011ModifiersCrossRef.DiagnosticId_SubsumesSelf).ToList();
        selfDiags.Should().ContainSingle();
        selfDiags[0].GetMessage().Should().Contain("Positive");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0011b — MutuallyExclusiveWith self-reference
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Mutex_OtherModifiers_NoDiagnostic()
    {
        var source = ModStubs + @"
    public static class Modifiers
    {
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Success => new StateModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Success), ""success"",
                ModifierCategory.Semantic,
                MutuallyExclusiveWith: [ModifierKind.Warning, ModifierKind.Error]),
            ModifierKind.Warning => new StateModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Warning), ""warning"",
                ModifierCategory.Semantic,
                MutuallyExclusiveWith: [ModifierKind.Success, ModifierKind.Error]),
            ModifierKind.Error => new StateModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Error), ""error"",
                ModifierCategory.Semantic,
                MutuallyExclusiveWith: [ModifierKind.Success, ModifierKind.Warning]),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0011ModifiersCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0011ModifiersCrossRef.DiagnosticId_MutexSelf)
            .Should().BeEmpty();
        diagnostics.Where(d => d.Id == PRECEPT0011ModifiersCrossRef.DiagnosticId_MutexAsymmetric)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Mutex_SelfReference_ReportsError()
    {
        var source = ModStubs + @"
    public static class Modifiers
    {
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Success => new StateModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Success), ""success"",
                ModifierCategory.Semantic,
                MutuallyExclusiveWith: [ModifierKind.Success]),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0011ModifiersCrossRef>(source);
        var selfDiags = diagnostics.Where(d => d.Id == PRECEPT0011ModifiersCrossRef.DiagnosticId_MutexSelf).ToList();
        selfDiags.Should().ContainSingle();
        selfDiags[0].GetMessage().Should().Contain("Success");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0011c — Mutex asymmetry
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Mutex_Asymmetric_ReportsError()
    {
        // Success lists Warning, but Warning does NOT list Success.
        var source = ModStubs + @"
    public static class Modifiers
    {
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Success => new StateModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Success), ""success"",
                ModifierCategory.Semantic,
                MutuallyExclusiveWith: [ModifierKind.Warning]),
            ModifierKind.Warning => new StateModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Warning), ""warning"",
                ModifierCategory.Semantic),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0011ModifiersCrossRef>(source);
        var asymDiags = diagnostics.Where(d => d.Id == PRECEPT0011ModifiersCrossRef.DiagnosticId_MutexAsymmetric).ToList();
        asymDiags.Should().ContainSingle();
        asymDiags[0].GetMessage().Should().Contain("Success");
        asymDiags[0].GetMessage().Should().Contain("Warning");
    }

    [Fact]
    public async Task Mutex_Symmetric_NoDiagnostic()
    {
        // Both list each other — symmetric.
        var source = ModStubs + @"
    public static class Modifiers
    {
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Write => new StateModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Write), ""write"",
                ModifierCategory.Structural,
                MutuallyExclusiveWith: [ModifierKind.Read]),
            ModifierKind.Read => new StateModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Read), ""read"",
                ModifierCategory.Structural,
                MutuallyExclusiveWith: [ModifierKind.Write]),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0011ModifiersCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0011ModifiersCrossRef.DiagnosticId_MutexAsymmetric)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0011d — Subsumes circularity
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Subsumes_Circular_ReportsError()
    {
        // Positive subsumes Nonnegative AND Nonnegative subsumes Positive — circular.
        var source = ModStubs + @"
    public static class Modifiers
    {
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Positive => new FieldModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Positive), ""positive"",
                ModifierCategory.Structural, new TypeTarget[] { },
                Subsumes: [ModifierKind.Nonnegative]),
            ModifierKind.Nonnegative => new FieldModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Nonnegative), ""nonneg"",
                ModifierCategory.Structural, new TypeTarget[] { },
                Subsumes: [ModifierKind.Positive]),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0011ModifiersCrossRef>(source);
        var circDiags = diagnostics.Where(d => d.Id == PRECEPT0011ModifiersCrossRef.DiagnosticId_SubsumesCircular).ToList();
        // Both arms trigger (A→B and B→A), so expect 2.
        circDiags.Should().HaveCount(2);
    }

    [Fact]
    public async Task Subsumes_OneWay_NoDiagnostic()
    {
        // Positive subsumes Nonnegative (one-way) — not circular.
        var source = ModStubs + @"
    public static class Modifiers
    {
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Positive => new FieldModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Positive), ""positive"",
                ModifierCategory.Structural, new TypeTarget[] { },
                Subsumes: [ModifierKind.Nonnegative]),
            ModifierKind.Nonnegative => new FieldModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Nonnegative), ""nonneg"",
                ModifierCategory.Structural, new TypeTarget[] { }),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0011ModifiersCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0011ModifiersCrossRef.DiagnosticId_SubsumesCircular)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  PRECEPT0011e — Inline Token
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Token_ViaGetMeta_NoDiagnostic()
    {
        var source = ModStubs + @"
    public static class Modifiers
    {
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Nonnegative => new FieldModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Nonnegative), ""nonneg"",
                ModifierCategory.Structural, new TypeTarget[] { }),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0011ModifiersCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0011ModifiersCrossRef.DiagnosticId_InlineToken)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Token_InlineNew_ReportsWarning()
    {
        var source = ModStubs + @"
    public static class Modifiers
    {
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Nonnegative => new FieldModifierMeta(
                kind, new TokenMeta(TokenKind.Nonnegative, ""nonnegative""), ""nonneg"",
                ModifierCategory.Structural, new TypeTarget[] { }),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0011ModifiersCrossRef>(source);
        var inlineDiags = diagnostics.Where(d => d.Id == PRECEPT0011ModifiersCrossRef.DiagnosticId_InlineToken).ToList();
        inlineDiags.Should().ContainSingle();
        inlineDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
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
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0011ModifiersCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task WrongNamespace_NoDiagnostic()
    {
        var source = @"
namespace Other
{
    public enum ModifierKind { A, B }
    public enum TokenKind { X }
    public enum ModifierCategory { Structural }

    public sealed record TokenMeta(TokenKind Kind, string Text);
    public abstract record ModifierMeta(
        ModifierKind Kind, TokenMeta Token, string Description,
        ModifierCategory Category, ModifierKind[] MutuallyExclusiveWith = null);
    public sealed record FieldModifierMeta(
        ModifierKind Kind, TokenMeta Token, string Description,
        ModifierCategory Category, bool HasValue = false,
        ModifierKind[] Subsumes = null, ModifierKind[] MutuallyExclusiveWith = null)
        : ModifierMeta(Kind, Token, Description, Category, MutuallyExclusiveWith);

    public static class Modifiers
    {
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.A => new FieldModifierMeta(kind, new TokenMeta(TokenKind.X, ""x""), ""a"", ModifierCategory.Structural,
                Subsumes: [ModifierKind.A]),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0011ModifiersCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Edge: discard arm
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DiscardArm_NoDiagnostic()
    {
        var source = ModStubs + @"
    public static class Modifiers
    {
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            _ => new FieldModifierMeta(kind, Tokens.GetMeta(TokenKind.Nonnegative), ""fallback"",
                ModifierCategory.Structural, new TypeTarget[] { }),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0011ModifiersCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }
}
