using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class Precept0029Tests
{
    // ── Shared stubs ────────────────────────────────────────────────────────
    //
    // Minimal Precept.Language surface for Precept0029ModifierAxisDisjoint: the
    // ValueModifierMeta shape with an ApplicableTo TypeTarget[] arg, plus the
    // CollectionTypes array the analyzer derives the collection-kind set from.

    private const string ModStubs = @"
namespace Precept.Language
{
    public enum ModifierKind { Notempty, Mincount, Maxcount, Minlength, Optional, Bogus }
    public enum TokenKind { Notempty, Mincount, Maxcount, Minlength, Optional, Bogus }
    public enum ModifierCategory { Structural, Semantic }
    public enum TypeKind { String, Integer, Set, Queue, Stack, Log, LogBy, Bag, List, QueueBy, Lookup }

    public sealed record TokenMeta(TokenKind Kind, string Text);

    public static class Tokens
    {
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Notempty  => new(kind, ""notempty""),
            TokenKind.Mincount  => new(kind, ""mincount""),
            TokenKind.Maxcount  => new(kind, ""maxcount""),
            TokenKind.Minlength => new(kind, ""minlength""),
            TokenKind.Optional  => new(kind, ""optional""),
            TokenKind.Bogus     => new(kind, ""bogus""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public record TypeTarget(TypeKind? Kind);

    public abstract record ModifierMeta(
        ModifierKind Kind, TokenMeta Token, string Description,
        ModifierCategory Category, ModifierKind[] MutuallyExclusiveWith = null);

    public sealed record ValueModifierMeta(
        ModifierKind Kind, TokenMeta Token, string Description,
        ModifierCategory Category, TypeTarget[] ApplicableTo,
        bool HasValue = false, ModifierKind[] Subsumes = null,
        string HoverDescription = null, ModifierKind[] MutuallyExclusiveWith = null)
        : ModifierMeta(Kind, Token, Description, Category, MutuallyExclusiveWith);
";

    // The CollectionTypes array — the analyzer derives the collection-kind set from this,
    // never a parallel hardcoded list. Mirrors the real Modifiers.CollectionTypes (9 kinds).
    private const string CollectionTypesField = @"
        private static readonly TypeTarget[] CollectionTypes =
        [
            new(TypeKind.Set), new(TypeKind.Queue), new(TypeKind.Stack),
            new(TypeKind.Log), new(TypeKind.LogBy), new(TypeKind.Bag),
            new(TypeKind.List), new(TypeKind.QueueBy), new(TypeKind.Lookup),
        ];
";

    private const string CloseBrace = @"
}";

    // ════════════════════════════════════════════════════════════════════════════
    //  Flags a synthetic both-axes modifier (collection + scalar in ApplicableTo).
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BothAxes_CollectionAndScalar_ReportsError()
    {
        var source = ModStubs + @"
    public static class Modifiers
    {" + CollectionTypesField + @"
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Bogus => new ValueModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Bogus), ""bogus"",
                ModifierCategory.Structural,
                new TypeTarget[] { new(TypeKind.Set), new(TypeKind.String) }),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0029ModifierAxisDisjoint>(source);
        var bothAxes = diagnostics.Where(d => d.Id == Precept0029ModifierAxisDisjoint.DiagnosticId_BothAxes).ToList();
        bothAxes.Should().ContainSingle();
        bothAxes[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        bothAxes[0].GetMessage().Should().Contain("Bogus");
        bothAxes[0].GetMessage().Should().Contain("Set");
        bothAxes[0].GetMessage().Should().Contain("String");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Faithful post-retarget catalog shape is clean (notempty=StringOnly,
    //  mincount/maxcount=CollectionTypes, optional=AnyType). Guards the rule and the
    //  AnyType exemption against false positives.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PostRetargetCatalogShape_NoDiagnostic()
    {
        var source = ModStubs + @"
    public static class Modifiers
    {" + CollectionTypesField + @"
        private static readonly TypeTarget[] StringOnly = [new(TypeKind.String)];
        private static readonly TypeTarget[] AnyType = [];

        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            // notempty: string-only (post-retarget) — scalar axis only.
            ModifierKind.Notempty => new ValueModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Notempty), ""notempty"",
                ModifierCategory.Structural, StringOnly),
            // minlength: string-only — scalar axis only.
            ModifierKind.Minlength => new ValueModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Minlength), ""minlength"",
                ModifierCategory.Structural, StringOnly),
            // mincount/maxcount: collection-only — collection axis only.
            ModifierKind.Mincount => new ValueModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Mincount), ""mincount"",
                ModifierCategory.Structural, CollectionTypes),
            ModifierKind.Maxcount => new ValueModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Maxcount), ""maxcount"",
                ModifierCategory.Structural, CollectionTypes),
            // optional: AnyType (empty ApplicableTo) — exempt, not an axis overlap.
            ModifierKind.Optional => new ValueModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Optional), ""optional"",
                ModifierCategory.Structural, AnyType),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0029ModifierAxisDisjoint>(source);
        diagnostics.Where(d => d.Id == Precept0029ModifierAxisDisjoint.DiagnosticId_BothAxes)
            .Should().BeEmpty();
    }

    // ── A collection-only modifier (mincount) alone is clean. ────────────────

    [Fact]
    public async Task CollectionOnly_NoDiagnostic()
    {
        var source = ModStubs + @"
    public static class Modifiers
    {" + CollectionTypesField + @"
        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Mincount => new ValueModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Mincount), ""mincount"",
                ModifierCategory.Structural, CollectionTypes),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0029ModifierAxisDisjoint>(source);
        diagnostics.Where(d => d.Id == Precept0029ModifierAxisDisjoint.DiagnosticId_BothAxes)
            .Should().BeEmpty();
    }

    // ── AnyType (empty ApplicableTo) is exempt even though it covers all kinds. ──

    [Fact]
    public async Task AnyType_EmptyApplicableTo_NoDiagnostic()
    {
        var source = ModStubs + @"
    public static class Modifiers
    {" + CollectionTypesField + @"
        private static readonly TypeTarget[] AnyType = [];

        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Optional => new ValueModifierMeta(
                kind, Tokens.GetMeta(TokenKind.Optional), ""optional"",
                ModifierCategory.Structural, AnyType),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0029ModifierAxisDisjoint>(source);
        diagnostics.Where(d => d.Id == Precept0029ModifierAxisDisjoint.DiagnosticId_BothAxes)
            .Should().BeEmpty();
    }

    // ── Scope guard: wrong namespace is ignored. ─────────────────────────────

    [Fact]
    public async Task WrongNamespace_NoDiagnostic()
    {
        var source = @"
namespace Other
{
    public enum ModifierKind { Bogus }
    public enum TokenKind { Bogus }
    public enum ModifierCategory { Structural }
    public enum TypeKind { String, Set }

    public sealed record TokenMeta(TokenKind Kind, string Text);
    public record TypeTarget(TypeKind? Kind);
    public abstract record ModifierMeta(
        ModifierKind Kind, TokenMeta Token, string Description,
        ModifierCategory Category, ModifierKind[] MutuallyExclusiveWith = null);
    public sealed record ValueModifierMeta(
        ModifierKind Kind, TokenMeta Token, string Description,
        ModifierCategory Category, TypeTarget[] ApplicableTo,
        ModifierKind[] MutuallyExclusiveWith = null)
        : ModifierMeta(Kind, Token, Description, Category, MutuallyExclusiveWith);

    public static class Modifiers
    {
        private static readonly TypeTarget[] CollectionTypes = [new(TypeKind.Set)];

        public static ModifierMeta GetMeta(ModifierKind kind) => kind switch
        {
            ModifierKind.Bogus => new ValueModifierMeta(
                kind, new TokenMeta(TokenKind.Bogus, ""bogus""), ""bogus"",
                ModifierCategory.Structural,
                new TypeTarget[] { new(TypeKind.Set), new(TypeKind.String) }),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0029ModifierAxisDisjoint>(source);
        diagnostics.Should().BeEmpty();
    }
}
