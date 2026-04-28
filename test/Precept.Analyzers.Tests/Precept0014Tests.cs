using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0014Tests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  X31 — AllowedIn must not self-reference
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// AllowedIn references a different ConstructKind — valid.
    /// </summary>
    private const string AllowedIn_ValidRef = @"
namespace Precept.Language
{
    public enum ConstructKind { PreceptHeader, StateDeclaration, TransitionRow }
    public enum TokenKind { Precept, State, From }

    public sealed record ConstructSlot();

    public sealed record ConstructMeta(
        ConstructKind Kind, string Name, string Description, string UsageExample,
        ConstructKind[] AllowedIn, System.Collections.Generic.IReadOnlyList<ConstructSlot> Slots,
        TokenKind LeadingToken, string SnippetTemplate = null);

    public static class Constructs
    {
        public static ConstructMeta GetMeta(ConstructKind kind) => kind switch
        {
            ConstructKind.PreceptHeader => new(
                kind, ""header"", ""desc"", ""example"",
                [],
                new ConstructSlot[0],
                TokenKind.Precept),

            ConstructKind.StateDeclaration => new(
                kind, ""state"", ""desc"", ""example"",
                [],
                new ConstructSlot[0],
                TokenKind.State),

            ConstructKind.TransitionRow => new(
                kind, ""transition"", ""desc"", ""example"",
                [ConstructKind.StateDeclaration],
                new ConstructSlot[0],
                TokenKind.From),

            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task AllowedIn_ReferencesOtherKind_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0014ConstructsCrossRef>(AllowedIn_ValidRef);
        diagnostics.Where(d => d.Id == PRECEPT0014ConstructsCrossRef.DiagnosticId_SelfRef)
            .Should().BeEmpty();
    }

    /// <summary>
    /// AllowedIn references its own ConstructKind — self-reference error.
    /// </summary>
    private const string AllowedIn_SelfRef = @"
namespace Precept.Language
{
    public enum ConstructKind { PreceptHeader, TransitionRow }
    public enum TokenKind { Precept, From }

    public sealed record ConstructSlot();

    public sealed record ConstructMeta(
        ConstructKind Kind, string Name, string Description, string UsageExample,
        ConstructKind[] AllowedIn, System.Collections.Generic.IReadOnlyList<ConstructSlot> Slots,
        TokenKind LeadingToken, string SnippetTemplate = null);

    public static class Constructs
    {
        public static ConstructMeta GetMeta(ConstructKind kind) => kind switch
        {
            ConstructKind.PreceptHeader => new(
                kind, ""header"", ""desc"", ""example"",
                [],
                new ConstructSlot[0],
                TokenKind.Precept),

            ConstructKind.TransitionRow => new(
                kind, ""transition"", ""desc"", ""example"",
                [ConstructKind.TransitionRow],
                new ConstructSlot[0],
                TokenKind.From),

            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task AllowedIn_SelfReference_ReportsError()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0014ConstructsCrossRef>(AllowedIn_SelfRef);
        var selfRefDiags = diagnostics.Where(d => d.Id == PRECEPT0014ConstructsCrossRef.DiagnosticId_SelfRef).ToList();
        selfRefDiags.Should().ContainSingle();
        selfRefDiags[0].GetMessage().Should().Contain("TransitionRow");
        selfRefDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    /// <summary>
    /// AllowedIn has multiple entries, one is self-referencing.
    /// </summary>
    private const string AllowedIn_MixedSelfRef = @"
namespace Precept.Language
{
    public enum ConstructKind { PreceptHeader, StateDeclaration, TransitionRow }
    public enum TokenKind { Precept, State, From }

    public sealed record ConstructSlot();

    public sealed record ConstructMeta(
        ConstructKind Kind, string Name, string Description, string UsageExample,
        ConstructKind[] AllowedIn, System.Collections.Generic.IReadOnlyList<ConstructSlot> Slots,
        TokenKind LeadingToken, string SnippetTemplate = null);

    public static class Constructs
    {
        public static ConstructMeta GetMeta(ConstructKind kind) => kind switch
        {
            ConstructKind.PreceptHeader => new(
                kind, ""header"", ""desc"", ""example"",
                [],
                new ConstructSlot[0],
                TokenKind.Precept),

            ConstructKind.StateDeclaration => new(
                kind, ""state"", ""desc"", ""example"",
                [],
                new ConstructSlot[0],
                TokenKind.State),

            ConstructKind.TransitionRow => new(
                kind, ""transition"", ""desc"", ""example"",
                [ConstructKind.StateDeclaration, ConstructKind.TransitionRow],
                new ConstructSlot[0],
                TokenKind.From),

            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task AllowedIn_MixedWithSelfRef_ReportsOnlySelfRef()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0014ConstructsCrossRef>(AllowedIn_MixedSelfRef);
        var selfRefDiags = diagnostics.Where(d => d.Id == PRECEPT0014ConstructsCrossRef.DiagnosticId_SelfRef).ToList();
        selfRefDiags.Should().ContainSingle();
        selfRefDiags[0].GetMessage().Should().Contain("TransitionRow");
    }

    /// <summary>
    /// Empty AllowedIn → no diagnostic.
    /// </summary>
    private const string AllowedIn_Empty = @"
namespace Precept.Language
{
    public enum ConstructKind { PreceptHeader }
    public enum TokenKind { Precept }

    public sealed record ConstructSlot();

    public sealed record ConstructMeta(
        ConstructKind Kind, string Name, string Description, string UsageExample,
        ConstructKind[] AllowedIn, System.Collections.Generic.IReadOnlyList<ConstructSlot> Slots,
        TokenKind LeadingToken, string SnippetTemplate = null);

    public static class Constructs
    {
        public static ConstructMeta GetMeta(ConstructKind kind) => kind switch
        {
            ConstructKind.PreceptHeader => new(
                kind, ""header"", ""desc"", ""example"",
                [],
                new ConstructSlot[0],
                TokenKind.Precept),

            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task AllowedIn_EmptyArray_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0014ConstructsCrossRef>(AllowedIn_Empty);
        diagnostics.Where(d => d.Id == PRECEPT0014ConstructsCrossRef.DiagnosticId_SelfRef)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  X32 — LeadingToken uniqueness across arms
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Each arm uses a distinct TokenKind for LeadingToken — valid.
    /// </summary>
    [Fact]
    public async Task LeadingToken_AllDistinct_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0014ConstructsCrossRef>(AllowedIn_ValidRef);
        diagnostics.Where(d => d.Id == PRECEPT0014ConstructsCrossRef.DiagnosticId_DupToken)
            .Should().BeEmpty();
    }

    /// <summary>
    /// Two arms share the same LeadingToken — warning.
    /// </summary>
    private const string DuplicateLeadingToken = @"
namespace Precept.Language
{
    public enum ConstructKind { StateEnsure, AccessMode }
    public enum TokenKind { In }

    public sealed record ConstructSlot();

    public sealed record ConstructMeta(
        ConstructKind Kind, string Name, string Description, string UsageExample,
        ConstructKind[] AllowedIn, System.Collections.Generic.IReadOnlyList<ConstructSlot> Slots,
        TokenKind LeadingToken, string SnippetTemplate = null);

    public static class Constructs
    {
        public static ConstructMeta GetMeta(ConstructKind kind) => kind switch
        {
            ConstructKind.StateEnsure => new(
                kind, ""state ensure"", ""desc"", ""example"",
                [],
                new ConstructSlot[0],
                TokenKind.In),

            ConstructKind.AccessMode => new(
                kind, ""access mode"", ""desc"", ""example"",
                [],
                new ConstructSlot[0],
                TokenKind.In),

            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task LeadingToken_DuplicateIdenticalSlots_ReportsError()
    {
        // Both arms have identical (empty) slot sequences → genuinely ambiguous → error.
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0014ConstructsCrossRef>(DuplicateLeadingToken);
        var dupDiags = diagnostics.Where(d => d.Id == PRECEPT0014ConstructsCrossRef.DiagnosticId_DupToken).ToList();
        dupDiags.Should().ContainSingle();
        dupDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        var msg = dupDiags[0].GetMessage();
        msg.Should().Contain("AccessMode");
        msg.Should().Contain("In");
        msg.Should().Contain("StateEnsure");
    }

    /// <summary>
    /// Three arms share the same LeadingToken — two diagnostics (second and third).
    /// </summary>
    private const string TripleDuplicateLeadingToken = @"
namespace Precept.Language
{
    public enum ConstructKind { StateEnsure, AccessMode, StateAction }
    public enum TokenKind { In }

    public sealed record ConstructSlot();

    public sealed record ConstructMeta(
        ConstructKind Kind, string Name, string Description, string UsageExample,
        ConstructKind[] AllowedIn, System.Collections.Generic.IReadOnlyList<ConstructSlot> Slots,
        TokenKind LeadingToken, string SnippetTemplate = null);

    public static class Constructs
    {
        public static ConstructMeta GetMeta(ConstructKind kind) => kind switch
        {
            ConstructKind.StateEnsure => new(
                kind, ""state ensure"", ""desc"", ""example"",
                [],
                new ConstructSlot[0],
                TokenKind.In),

            ConstructKind.AccessMode => new(
                kind, ""access mode"", ""desc"", ""example"",
                [],
                new ConstructSlot[0],
                TokenKind.In),

            ConstructKind.StateAction => new(
                kind, ""state action"", ""desc"", ""example"",
                [],
                new ConstructSlot[0],
                TokenKind.In),

            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task LeadingToken_TripleDuplicate_ReportsAllPairs()
    {
        // Three arms all with identical (empty) slots → 3 ambiguous pairs → 3 errors.
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0014ConstructsCrossRef>(TripleDuplicateLeadingToken);
        var dupDiags = diagnostics.Where(d => d.Id == PRECEPT0014ConstructsCrossRef.DiagnosticId_DupToken).ToList();
        dupDiags.Should().HaveCount(3);
        dupDiags.Should().AllSatisfy(d => d.Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
    }

    /// <summary>
    /// Two arms share the same LeadingToken but their slot sequences diverge —
    /// the parser has a lookahead disambiguation point → no diagnostic.
    /// </summary>
    private const string SharedTokenDivergingSlots = @"
namespace Precept.Language
{
    public enum ConstructKind { StateEnsure, AccessMode }
    public enum TokenKind { In }
    public enum ConstructSlotKind { StateTarget, EnsureClause, AccessModeKeyword }

    public sealed record ConstructSlot(ConstructSlotKind Kind, bool IsRequired = true);

    public sealed record ConstructMeta(
        ConstructKind Kind, string Name, string Description, string UsageExample,
        ConstructKind[] AllowedIn, System.Collections.Generic.IReadOnlyList<ConstructSlot> Slots,
        TokenKind LeadingToken, string SnippetTemplate = null);

    public static class Constructs
    {
        private static readonly ConstructSlot SlotStateTarget = new(ConstructSlotKind.StateTarget);

        public static ConstructMeta GetMeta(ConstructKind kind) => kind switch
        {
            ConstructKind.StateEnsure => new(
                kind, ""state ensure"", ""desc"", ""example"",
                [],
                [SlotStateTarget, new ConstructSlot(ConstructSlotKind.EnsureClause)],
                TokenKind.In),

            ConstructKind.AccessMode => new(
                kind, ""access mode"", ""desc"", ""example"",
                [],
                [SlotStateTarget, new ConstructSlot(ConstructSlotKind.AccessModeKeyword)],
                TokenKind.In),

            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task LeadingToken_SharedToken_DivergingSlots_NoDiagnostic()
    {
        // Slots diverge at position 1 (EnsureClause vs AccessModeKeyword) →
        // parser can disambiguate by lookahead → no error.
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0014ConstructsCrossRef>(SharedTokenDivergingSlots);
        diagnostics.Where(d => d.Id == PRECEPT0014ConstructsCrossRef.DiagnosticId_DupToken)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Scope guards
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Not a ConstructKind switch (OperatorKind) → no PRECEPT0014 diagnostic.
    /// </summary>
    private const string WrongEnumKind = @"
namespace Precept.Language
{
    public enum OperatorKind { Plus, Minus }

    public sealed record OperatorMeta(OperatorKind Kind, string Description);

    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Plus  => new(kind, ""Addition""),
            OperatorKind.Minus => new(kind, ""Subtraction""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task NonConstructKindSwitch_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0014ConstructsCrossRef>(WrongEnumKind);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Wrong namespace → no diagnostic.
    /// </summary>
    private const string WrongNamespace = @"
namespace Other
{
    public enum ConstructKind { A, B }
    public enum TokenKind { X, Y }

    public sealed record ConstructSlot();

    public sealed record ConstructMeta(
        ConstructKind Kind, string Name, string Description, string UsageExample,
        ConstructKind[] AllowedIn, System.Collections.Generic.IReadOnlyList<ConstructSlot> Slots,
        TokenKind LeadingToken, string SnippetTemplate = null);

    public static class Constructs
    {
        public static ConstructMeta GetMeta(ConstructKind kind) => kind switch
        {
            ConstructKind.A => new(
                kind, ""a"", ""d"", ""e"",
                [ConstructKind.A],
                new ConstructSlot[0],
                TokenKind.X),
            ConstructKind.B => new(
                kind, ""b"", ""d"", ""e"",
                [],
                new ConstructSlot[0],
                TokenKind.X),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task WrongNamespace_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0014ConstructsCrossRef>(WrongNamespace);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Wrong method name → no diagnostic.
    /// </summary>
    private const string WrongMethodName = @"
namespace Precept.Language
{
    public enum ConstructKind { A, B }
    public enum TokenKind { X, Y }

    public sealed record ConstructSlot();

    public sealed record ConstructMeta(
        ConstructKind Kind, string Name, string Description, string UsageExample,
        ConstructKind[] AllowedIn, System.Collections.Generic.IReadOnlyList<ConstructSlot> Slots,
        TokenKind LeadingToken, string SnippetTemplate = null);

    public static class Constructs
    {
        public static ConstructMeta Lookup(ConstructKind kind) => kind switch
        {
            ConstructKind.A => new(
                kind, ""a"", ""d"", ""e"",
                [ConstructKind.A],
                new ConstructSlot[0],
                TokenKind.X),
            ConstructKind.B => new(
                kind, ""b"", ""d"", ""e"",
                [],
                new ConstructSlot[0],
                TokenKind.X),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task WrongMethodName_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0014ConstructsCrossRef>(WrongMethodName);
        diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Combined: both checks in one switch
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// One arm has self-ref in AllowedIn AND two arms share the same LeadingToken.
    /// Both diagnostics should fire.
    /// </summary>
    private const string BothViolations = @"
namespace Precept.Language
{
    public enum ConstructKind { A, B }
    public enum TokenKind { X }

    public sealed record ConstructSlot();

    public sealed record ConstructMeta(
        ConstructKind Kind, string Name, string Description, string UsageExample,
        ConstructKind[] AllowedIn, System.Collections.Generic.IReadOnlyList<ConstructSlot> Slots,
        TokenKind LeadingToken, string SnippetTemplate = null);

    public static class Constructs
    {
        public static ConstructMeta GetMeta(ConstructKind kind) => kind switch
        {
            ConstructKind.A => new(
                kind, ""a"", ""d"", ""e"",
                [ConstructKind.A],
                new ConstructSlot[0],
                TokenKind.X),

            ConstructKind.B => new(
                kind, ""b"", ""d"", ""e"",
                [],
                new ConstructSlot[0],
                TokenKind.X),

            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    [Fact]
    public async Task BothViolations_ReportsBothDiagnostics()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0014ConstructsCrossRef>(BothViolations);
        diagnostics.Where(d => d.Id == PRECEPT0014ConstructsCrossRef.DiagnosticId_SelfRef)
            .Should().ContainSingle();
        diagnostics.Where(d => d.Id == PRECEPT0014ConstructsCrossRef.DiagnosticId_DupToken)
            .Should().ContainSingle();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Edge: discard-only arms → no diagnostic
    // ════════════════════════════════════════════════════════════════════════════

    private const string DiscardOnly = @"
namespace Precept.Language
{
    public enum ConstructKind { A }
    public enum TokenKind { X }

    public sealed record ConstructSlot();

    public sealed record ConstructMeta(
        ConstructKind Kind, string Name, string Description, string UsageExample,
        ConstructKind[] AllowedIn, System.Collections.Generic.IReadOnlyList<ConstructSlot> Slots,
        TokenKind LeadingToken, string SnippetTemplate = null);

    public static class Constructs
    {
        public static ConstructMeta GetMeta(ConstructKind kind) => kind switch
        {
            _ => new(kind, ""x"", ""d"", ""e"", [], new ConstructSlot[0], TokenKind.X),
        };
    }
}";

    [Fact]
    public async Task DiscardOnly_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0014ConstructsCrossRef>(DiscardOnly);
        diagnostics.Should().BeEmpty();
    }
}
