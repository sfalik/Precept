using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Catalog-aware tests for state-wildcard and broadcast-field-target token metadata,
/// and for the parser's recognition of <c>any</c> (state wildcard) and <c>all</c>
/// (broadcast field target) in transition rows, state hooks, and access-mode constructs.
///
/// Root causes addressed:
///   BUG-001: 'any' state wildcard — <c>IsStateWildcard</c> must be true in catalog.
///   BUG-026: 'modify all' treats 'all' as field name — <c>IsFieldBroadcast</c> must be true.
///   BUG-037: 'omit all' treats 'all' as field name — same root as BUG-026.
/// </summary>
public class StateTargetTests
{
    // ── Catalog metadata ─────────────────────────────────────────────────────────

    [Fact]
    public void TokenMeta_Any_IsStateWildcard_IsTrue()
    {
        var meta = Tokens.GetMeta(TokenKind.Any);

        meta.IsStateWildcard.Should().BeTrue(
            because: "'any' is the state wildcard token — name binder must not treat it as an identifier");
    }

    [Fact]
    public void TokenMeta_All_IsFieldBroadcast_IsTrue()
    {
        var meta = Tokens.GetMeta(TokenKind.All);

        meta.IsFieldBroadcast.Should().BeTrue(
            because: "'all' is the broadcast field target — name binder must not do a literal field lookup for it");
    }

    [Fact]
    public void TokenMeta_Any_IsStateWildcard_IsConsistentWithCatalogText()
    {
        var meta = Tokens.GetMeta(TokenKind.Any);

        meta.Text.Should().Be("any");
        meta.IsStateWildcard.Should().BeTrue();
    }

    [Fact]
    public void TokenMeta_All_IsFieldBroadcast_IsConsistentWithCatalogText()
    {
        var meta = Tokens.GetMeta(TokenKind.All);

        meta.Text.Should().Be("all");
        meta.IsFieldBroadcast.Should().BeTrue();
    }

    // ── Parser: 'from any' wildcard in TransitionRow ─────────────────────────────

    [Fact]
    public void Parser_FromAny_TransitionRow_ParsesWithoutDiagnostics()
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex("from any on Submit -> transition Approved"));

        manifest.Diagnostics.Should().BeEmpty(
            because: "'from any' is a valid state wildcard and must parse without errors");
    }

    [Fact]
    public void Parser_FromAny_TransitionRow_StateTargetSlot_CarriesAny()
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex("from any on Submit -> transition Approved"));

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        row.Slots.OfType<StateTargetSlot>().Single().StateName.Should().Be("any",
            because: "the StateTargetSlot must capture the wildcard token text 'any'");
    }

    // ── Parser: 'to any' wildcard in StateHook ───────────────────────────────────

    [Fact]
    public void Parser_ToAny_StateHook_ParsesWithoutDiagnostics()
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex(
            "from Draft on Submit -> transition Active\nto any -> set Flag = true"));

        manifest.Diagnostics.Should().BeEmpty(
            because: "'to any' is a valid state-exit wildcard hook and must parse cleanly");
    }

    [Fact]
    public void Parser_ToAny_StateHook_HasStateTargetSlot_WithAny()
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex(
            "from Draft on Submit -> transition Active\nto any -> set Flag = true"));

        var hook = manifest.Constructs.FirstOrDefault(c => c.Meta.Kind == ConstructKind.StateAction);
        hook.Should().NotBeNull(because: "a StateAction construct must be emitted for 'to any'");

        var stateSlot = hook!.Slots.OfType<StateTargetSlot>().FirstOrDefault();
        stateSlot.Should().NotBeNull();
        stateSlot!.StateName.Should().Be("any");
    }

    // ── Parser: 'modify all' broadcast field in AccessMode ───────────────────────

    [Fact]
    public void Parser_ModifyAll_AccessMode_ParsesWithoutDiagnostics()
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex("in Draft modify all readonly"));

        manifest.Diagnostics.Should().BeEmpty(
            because: "'modify all' uses a broadcast field target and must parse without errors");
    }

    [Fact]
    public void Parser_ModifyAll_AccessMode_FieldTargetSlot_CarriesAll()
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex("in Draft modify all readonly"));

        var access = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.AccessMode);
        access.Slots.OfType<FieldTargetSlot>().Single().FieldName.Should().Be("all",
            because: "the FieldTargetSlot must capture the broadcast token text 'all'");
    }

    // ── Parser: 'omit all' broadcast field in OmitDeclaration ───────────────────

    [Fact]
    public void Parser_OmitAll_OmitDeclaration_ParsesWithoutDiagnostics()
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex("in Draft omit all"));

        manifest.Diagnostics.Should().BeEmpty(
            because: "'omit all' uses a broadcast field target and must parse without errors");
    }

    [Fact]
    public void Parser_OmitAll_OmitDeclaration_FieldTargetSlot_CarriesAll()
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex("in Draft omit all"));

        var omit = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.OmitDeclaration);
        omit.Slots.OfType<FieldTargetSlot>().Single().FieldName.Should().Be("all",
            because: "the FieldTargetSlot must capture the broadcast token text 'all'");
    }

    // ── Parser: comma-list StateTarget ───────────────────────────────────────────

    [Fact]
    public void Parser_FromCommaList_TwoNames_StateTargetSlot_CarriesBothNames()
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex("from Draft, Pending on Submit -> no transition"));

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var slot = row.Slots.OfType<StateTargetSlot>().Single();

        slot.StateNames.Should().HaveCount(2);
        slot.StateNames.Should().ContainInOrder("Draft", "Pending");
    }

    [Fact]
    public void Parser_FromCommaList_ThreeNames_StateTargetSlot_CarriesAllThreeNames()
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex("from Draft, Pending, Review on Submit -> no transition"));

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var slot = row.Slots.OfType<StateTargetSlot>().Single();

        slot.StateNames.Should().HaveCount(3);
        slot.StateNames.Should().ContainInOrder("Draft", "Pending", "Review");
    }

    [Fact]
    public void Parser_FromCommaList_NoSpaces_ParsesCorrectly()
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex("from Draft,Pending on Submit -> no transition"));

        manifest.Diagnostics.Should().BeEmpty(
            because: "no spaces around comma should not affect parsing");

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var slot = row.Slots.OfType<StateTargetSlot>().Single();

        slot.StateNames.Should().HaveCount(2);
        slot.StateNames.Should().ContainInOrder("Draft", "Pending");
    }

    [Fact]
    public void Parser_FromCommaList_ExtraWhitespace_ParsesCorrectly()
    {
        var manifest = Pipeline.Parser.Parse(Lexer.Lex("from Draft ,  Pending on Submit -> no transition"));

        manifest.Diagnostics.Should().BeEmpty(
            because: "extra spaces around comma should not affect parsing");

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var slot = row.Slots.OfType<StateTargetSlot>().Single();

        slot.StateNames.Should().HaveCount(2);
        slot.StateNames.Should().ContainInOrder("Draft", "Pending");
    }

    [Fact]
    public void Parser_FromCommaList_TrailingComma_EmitsDiagnostic()
    {
        // 'from Draft, on Submit' — the parser sees a comma then a keyword, not an identifier
        var manifest = Pipeline.Parser.Parse(Lexer.Lex("from Draft, on Submit -> no transition"));

        manifest.Diagnostics.Should().NotBeEmpty(
            because: "a trailing comma (comma followed by a keyword instead of an identifier) must produce a parse diagnostic");
    }

    // ── Full compilation: wildcards round-trip cleanly ───────────────────────────

    [Fact]
    public void Compiler_FromAny_CompilesWithoutUndeclaredStateDiagnostic()
    {
        var compilation = Compiler.Compile("""
            precept WildcardFromAny
            field Flag as boolean default false writable
            state Draft initial
            state Done terminal
            event Toggle
            from any on Toggle -> set Flag = true -> no transition
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredState),
            because: "'any' is a wildcard — it must not trigger an UndeclaredState diagnostic");
    }

    [Fact]
    public void Compiler_ModifyAll_CompilesWithoutUndeclaredFieldDiagnostic()
    {
        var compilation = Compiler.Compile("""
            precept BroadcastModify
            field Name as string writable
            field Amount as number
            state Draft initial
            state Closed terminal
            in Closed modify all readonly
            event Close
            from Draft on Close -> transition Closed
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField),
            because: "'all' is a broadcast field target — it must not trigger an UndeclaredField diagnostic");
    }

    [Fact]
    public void Compiler_OmitAll_CompilesWithoutUndeclaredFieldDiagnostic()
    {
        var compilation = Compiler.Compile("""
            precept BroadcastOmit
            field Name as string optional writable
            field Amount as number optional
            state Draft initial
            state Closed terminal
            in Draft omit all
            event Close
            from Draft on Close -> transition Closed
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UndeclaredField),
            because: "'all' in 'omit all' is a broadcast target — it must not trigger an UndeclaredField diagnostic");
    }
}
