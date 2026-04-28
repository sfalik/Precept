# Catalog-Driven Parser: Final Design & Implementation Plan (Round 7)

**By:** Frank (Language Designer / Compiler Architect)
**Date:** 2026-04-27
**Status:** Final design document — all open items closed, implementation plan complete
**References:**
- `docs/working/catalog-parser-design-v5.md` — Round 5 (validation layer, extensibility, G5 resolution)
- `docs/working/catalog-parser-design-v6.md` — George's Round 6 review (confirmations + open items)
- `docs/working/catalog-parser-design-v5-lang-simplify.md` — George's language simplification analysis
- `docs/language/precept-language-spec.md` — DSL spec (law)
- `docs/language/catalog-system.md` — metadata-driven catalog architecture
- `src/Precept/Language/Constructs.cs` — current catalog source
- `src/Precept/Language/Construct.cs` — ConstructMeta record
- `src/Precept/Language/ConstructSlot.cs` — ConstructSlot/ConstructSlotKind
- `CONTRIBUTING.md` — implementation plan quality bar

**This document supersedes v1–v6.** It is the final design document for the catalog-driven parser design loop. After this round, the design transitions to implementation.

---

## §1: Open Items Closed

### L1 — Language Change Decision: Guard Position (CLOSED → SUPERSEDED by F12)

**Open item from v6:** George's language simplification analysis proposed moving access mode guards from pre-verb to post-field position (`in UnderReview write DecisionNote when DocumentsVerified` instead of `in UnderReview when DocumentsVerified write DecisionNote`). The proposal was pending Shane's input.

**Original Shane answer (v7):** "in UnderReview when DocumentsVerified write DecisionNote is more natural to me."

**Original resolution (F11):** Pre-verb guard position confirmed. Disambiguator pre-consumption `when` path retained.

**Subsequent override:** Shane moved the guard to post-field position in a later session (2026-04-28). The spec and vision doc were updated to `in State write Field when Guard`. This was further superseded by the B4 vocabulary decision below.

> **Decision F11:** ~~Pre-verb access mode guard position confirmed.~~ **SUPERSEDED by F12.** The guard is now post-field: `in State modify Field readonly when Guard`. See F12.

### F12 — Access Mode Vocabulary: `modify`/`readonly`/`editable` (CLOSED — LOCKED)

**Context:** The B4 vocabulary design round (documented in `docs/working/frank-access-mode-design-round.md` § B4) evaluated Shane's `modify` verb proposal. Frank recommended `modify` + `readonly`/`editable` as the adjective pair. Shane accepted and locked the vocabulary on 2026-04-28.

**Locked grammar:**

```
in StateTarget modify FieldTarget readonly ("when" BoolExpr)?   ← constrain to read-only
in StateTarget modify FieldTarget editable ("when" BoolExpr)?   ← declare editable (upgrade)
in StateTarget omit FieldTarget                                  ← structural exclusion (no guard, no adjective)
```

**Key design points:**
- `modify` = verb (constraint declaration, parallel to `omit`). Consumed by the disambiguator as the disambiguation token — not stored as a slot.
- `readonly` / `editable` = access mode adjectives. Stored in the `AccessModeKeyword` slot.
- `omit` = exclusion verb (field absent from state entirely). Cannot be guarded (locked decision from A2).
- Guard position is **post-field** (adjective precedes guard): `in Draft modify Amount readonly when not Finalized`.
- `write` and `read` are retired from access mode context. They become dead in the grammar for this construct family.

**Token catalog impact (implementation task — not yet implemented):**
- Add `TokenKind.Modify` (new keyword)
- Add `TokenKind.Readonly` (new keyword — access mode adjective)
- Add `TokenKind.Editable` (new keyword — access mode adjective)
- `TokenKind.Write` and `TokenKind.Read` no longer serve as access mode keywords (they remain in the catalog for other contexts if any, but are removed from the `AccessMode` disambiguation entry)

**Why:** The verb/adjective split is semantically clean — `modify` is the operation on the field's access configuration, the adjective names the resulting mode. `readonly`/`editable` are paradox-free with `modify` (unlike `fixed`, which creates a modify-to-fix tension). True verb parallelism with `omit`. Supersedes the B2 `editable`/`fixed` adjective-as-verb recommendation and the original `write`/`read` vocabulary.

> **Decision F12:** Access mode vocabulary locked as `modify`/`readonly`/`editable`. Grammar: `in State modify Field readonly|editable [when Guard]` / `in State omit Field`. Post-field guard position (adjective before guard). `write`/`read` retired from access mode context. New tokens needed: `Modify`, `Readonly`, `Editable`. Shane's selection, Frank's recommendation from B4.

### T1 — Test Nit: `BuildNodeHandlesEveryConstructKind` Assertion (CLOSED)

**Open item from v6:** George identified that Test 4 (`BuildNodeHandlesEveryConstructKind`) calls `BuildNode` with null-filled slot arrays. The assertion `Should().NotThrow()` may pass or fail for the wrong reason: `NullReferenceException` or `InvalidCastException` from null slot propagation inside `BuildNode`'s cast logic (e.g., `(FieldTargetNode)slots[0]!`) could fire before any `ArgumentOutOfRangeException` for an unhandled `ConstructKind`. The test cannot reliably distinguish "this kind is handled" from "this kind's handler crashed on null input."

**Resolution:** Restructure the test to verify exhaustiveness without exercising the handler bodies. Two correct approaches:

**Approach A (Preferred): Reflection-based arm count verification.**

```csharp
[Fact]
public void BuildNodeHandlesEveryConstructKind()
{
    // Verify the switch has an arm for every ConstructKind by calling with
    // each kind and catching the expected null-propagation exceptions.
    // An UNHANDLED kind throws ArgumentOutOfRangeException — that's the gap.
    var allKinds = Enum.GetValues<ConstructKind>();
    foreach (var kind in allKinds)
    {
        var meta = Constructs.GetMeta(kind);
        var slots = new SyntaxNode?[meta.Slots.Count]; // null-filled

        try
        {
            Parser.BuildNode(kind, slots, default);
        }
        catch (ArgumentOutOfRangeException)
        {
            // This is the gap we're testing for — an unhandled kind.
            Assert.Fail($"BuildNode has no arm for ConstructKind.{kind}");
        }
        catch (Exception)
        {
            // NullReferenceException, InvalidCastException, etc. from null slots
            // are expected — they prove the arm EXISTS and attempted to execute.
            // The handler reached its body code, which is all we need to verify.
        }
    }
}
```

**Approach B (Alternative): Mock slot arrays with `IsMissing` sentinel nodes.**

Create minimal `MissingSyntaxNode` stubs that satisfy the casts in each `BuildNode` arm. This avoids exception-based control flow but requires maintaining sentinel instances that match each slot's expected type. Higher maintenance cost, lower test fragility.

**Adopted approach:** Approach A. The try/catch pattern is self-documenting: `ArgumentOutOfRangeException` = gap (fail), any other exception = arm exists (pass). The comment block in the test explains the contract. This pattern is documented in the validation layer section of v5 § 2.3, replacing the original `Should().NotThrow()` assertion.

---

## §2: Design Status — Stable, Complete

The catalog-driven parser design is **fully stable**. All open items from Rounds 1–6 are resolved. No pending Shane decisions, no unresolved George objections, no design tensions.

### Consolidated Decision Table (F1–F11 + G1–G6)

| ID | Decision | Resolution | Source |
|----|----------|-----------|--------|
| F1 | `LeadingTokenSlot` on `DisambiguationEntry` | Accepted | George v2 fix, Frank v3 |
| F2 | ~~`_slotParsers` as FrozenDictionary~~ | **SUPERSEDED by F7** | v3, overruled in v5 |
| F3 | ActionChain peek-before-consume | Accepted | George v2 fix, Frank v3 |
| F4 | ~~Two-position `when` guard for TransitionRow~~ | **WITHDRAWN** — see F9 | v3, withdrawn in v5 |
| F5 | `DisambiguationTokens` derivation | Reject — declare explicitly | Frank v3 |
| F6 | Migration PR sequence | Accepted with bridge | George v2, Frank v3 |
| F7 | `_slotParsers` → exhaustive switch (`InvokeSlotParser`) | CS8509 enforces completeness at build time | Frank v5 (Shane directive) |
| F8 | `ConstructSlotKind.RuleExpression` for rule body | Resolves G5. No intro token. `RuleDeclaration` → `[RuleExpression, GuardClause(opt), BecauseClause]` | Frank v5 |
| F9 | Reject pre-event guard in `from ... on` with diagnostic | Withdraws F4. Spec is correct. Consume `when` unconditionally, error on pre-event guard + `On` | Frank v5 |
| F10 | `EnsureClause` + `BecauseClause` separate slots | `because` mandate via `IsRequired` + type checker, not parser coupling | Frank v5 |
| **F11** | **~~Pre-verb access mode guard position confirmed~~** | **SUPERSEDED by F12. Guard is now post-field.** | **Frank v7 (Shane decision, later overridden)** |
| **F12** | **Access mode vocabulary locked: `modify`/`readonly`/`editable`** | **`in State modify Field readonly\|editable [when Guard]` / `in State omit Field`. Post-field guard. `write`/`read` retired from access mode context.** | **Frank v7 (Shane B4 selection)** |
| G1 | Pre-disambiguation `when` consumption mandatory | **SIMPLIFIED by F12** — `modify` is the disambiguation token after state target; `when` guard is post-field, not pre-disambiguation. The pre-consumption `when` path (stashing) is no longer needed for access modes. | George v4 |
| G2 | `Func<SyntaxNode?>` slot parser type | Confirmed — covariance handles widening | George v4 |
| G3 | Source generator | **MOOT** — Shane directive: no source generation | George v4 |
| G4 | `->` vs `<-` for computed fields | Keep `->` | George v4 |
| G5 | `RuleDeclaration` slot naming bug | **RESOLVED** — see F8 | George v4 |
| G6 | Pre-event `when` guard decision | **RESOLVED** — see F9 | George v4 |

### Architecture Summary (Reference v5 for Full Spec)

The parser architecture is a five-layer design:

- **Layer A — Vocabulary FrozenDictionaries:** Operator precedence, type keywords, modifier sets, action recognition — all derived from catalog metadata at startup. No hardcoded vocabulary in the parser.
- **Layer B — Top-level dispatch:** Keyword-dispatched loop. Leading token → dispatch to construct parser or disambiguator. `Constructs.ByLeadingToken` index provides the mapping.
- **Layer C — Generic slot iteration:** `ParseConstructSlots()` iterates `ConstructMeta.Slots`, calling `InvokeSlotParser()` per slot. CS8509 exhaustive switch on `ConstructSlotKind`.
- **Layer D — Disambiguation:** Four scoped prepositions (`in`, `to`, `from`, `on`) share a generic disambiguator. Pre-consume anchor target, match disambiguation token (`modify`/`omit`/`ensure`/`on`/`arrow`), route to construct, parse remaining slots. Post-field guard for access modes (F12). Pre-event guard rejection for transition rows (F9).
- **Layer E — Error sync:** `SyncToNextDeclaration()` recovers from parse errors by advancing to the next known leading token, derived from `Constructs.LeadingTokens`.

The validation layer (v5 § 2) provides four enforcement tiers: compile-time CS8509 (Tiers 1–2), startup/test-time assertions (Tier 3), and documentation-driven discoverability (Tier 4).

---

## §3: Implementation Plan — PR Sequence

### PR Dependency Graph

```
PR 1: Catalog Migration
  ↓
PR 2: Parser Infrastructure
  ↓
PR 3: Non-Disambiguated Constructs
  ↓
PR 4: Disambiguated Constructs — Simple
  ↓
PR 5: Disambiguated Constructs — From + Error Sync
```

Each PR depends on the previous. No parallelization between PRs — they build on each other sequentially.

---

### PR 1: Catalog Migration

**Goal:** Migrate `ConstructMeta` from single `LeadingToken` to `DisambiguationEntry[]`. Add `RuleExpression` slot kind. Rewrite `GetMeta()` with complete disambiguation entries for all 11 constructs. Establish validation test infrastructure.

#### Slice 1.1: `DisambiguationEntry` Record

**Create:** `src/Precept/Language/DisambiguationEntry.cs` (~15 lines)

```csharp
public sealed record DisambiguationEntry(
    TokenKind                      LeadingToken,
    ImmutableArray<TokenKind>?     DisambiguationTokens = null,
    ConstructSlotKind?             LeadingTokenSlot = null);
```

- `LeadingToken`: the keyword token that begins this construct form.
- `DisambiguationTokens`: for shared leading tokens, the tokens that distinguish this construct from siblings. Null for unique leading tokens.
- `LeadingTokenSlot`: when the leading token is ALSO slot content, identifies which slot receives the consumed token value. See F1. (Note: with F12, root-level `write all` is removed from the language — the `LeadingTokenSlot` mechanism is no longer used by `AccessMode`. It remains available for future constructs that need it.)

**Tests:** No standalone tests — validated transitively by Slice 1.3 tests.

#### Slice 1.2: `ConstructMeta` Migration

**Modify:** `src/Precept/Language/Construct.cs`

- Replace `TokenKind LeadingToken` parameter with `ImmutableArray<DisambiguationEntry> Entries`.
- Add bridge property: `public TokenKind PrimaryLeadingToken => Entries[0].LeadingToken;`
- Add obsolete alias: `[Obsolete("Use PrimaryLeadingToken or Entries")] public TokenKind LeadingToken => PrimaryLeadingToken;`
- Update `Slots` property declaration to remain unchanged.

Updated record signature (~25 lines):

```csharp
public sealed record ConstructMeta(
    ConstructKind                       Kind,
    string                              Name,
    string                              Description,
    string                              UsageExample,
    ConstructKind[]                     AllowedIn,
    IReadOnlyList<ConstructSlot>        Slots,
    ImmutableArray<DisambiguationEntry> Entries,
    string?                             SnippetTemplate = null)
{
    public IReadOnlyList<ConstructSlot> Slots { get; } = Slots;
    public TokenKind PrimaryLeadingToken => Entries[0].LeadingToken;

    [Obsolete("Use PrimaryLeadingToken or Entries")]
    public TokenKind LeadingToken => PrimaryLeadingToken;
}
```

**Tests:** Existing `ConstructsTests.LeadingToken_IsCorrect` theory — update to use `PrimaryLeadingToken` instead of `LeadingToken`. All 10 inline data rows must pass unchanged (same token values).

**Regression anchors:** All 19 existing tests in `test/Precept.Tests/ConstructsTests.cs` must pass.

#### Slice 1.3: `ConstructSlotKind.RuleExpression` Addition

**Modify:** `src/Precept/Language/ConstructSlot.cs` — add `RuleExpression` member to `ConstructSlotKind` enum (after `BecauseClause` or at an appropriate position).

**Modify:** `src/Precept/Language/Constructs.cs` — add shared slot instance:

```csharp
private static readonly ConstructSlot SlotRuleExpression = new(ConstructSlotKind.RuleExpression);
```

**Tests:** Covered by Slice 1.4 (the `RuleDeclaration` slot sequence test).

#### Slice 1.4: `GetMeta()` Rewrite with Complete Entries

**Modify:** `src/Precept/Language/Constructs.cs` — rewrite entire `GetMeta()` switch body.

All 11 constructs get `Entries` arrays. Key changes from current code:

**Unique leading token constructs (5):**

| Construct | Entries |
|-----------|---------|
| `PreceptHeader` | `[new(TokenKind.Precept)]` |
| `FieldDeclaration` | `[new(TokenKind.Field)]` |
| `StateDeclaration` | `[new(TokenKind.State)]` |
| `EventDeclaration` | `[new(TokenKind.Event)]` |
| `RuleDeclaration` | `[new(TokenKind.Rule)]` |

**Shared leading token constructs (6) — disambiguation required:**

| Construct | Entries |
|-----------|---------|
| `TransitionRow` | `[new(TokenKind.From, [TokenKind.On])]` |
| `StateEnsure` | `[new(TokenKind.In, [TokenKind.Ensure]), new(TokenKind.To, [TokenKind.Ensure]), new(TokenKind.From, [TokenKind.Ensure])]` |
| `AccessMode` | `[new(TokenKind.In, [TokenKind.Modify, TokenKind.Omit])]` |
| `StateAction` | `[new(TokenKind.To, [TokenKind.Arrow]), new(TokenKind.From, [TokenKind.Arrow])]` |
| `EventEnsure` | `[new(TokenKind.On, [TokenKind.Ensure])]` |
| `EventHandler` | `[new(TokenKind.On, [TokenKind.Arrow])]` |

**Slot sequence corrections:**

- `RuleDeclaration`: `[SlotRuleExpression, SlotGuardClause, SlotBecauseClause]` (was `[SlotGuardClause, SlotBecauseClause]` — F8 fix)
- All other slot sequences unchanged.

Full `GetMeta()` rewrite (~130 lines). See v4 § 6 for the complete PR-ready C# code; apply F8 correction to `RuleDeclaration`.

**Tests (new):**

```csharp
[Fact]
public void AllConstructsHaveAtLeastOneEntry()
{
    foreach (var meta in Constructs.All)
        meta.Entries.Should().NotBeEmpty($"{meta.Kind} must have at least one DisambiguationEntry");
}

[Fact]
public void LeadingTokenSlot_OnlyUsedWhenLeadingTokenIsAlsoSlotContent()
{
    foreach (var meta in Constructs.All)
    foreach (var entry in meta.Entries)
    {
        if (entry.LeadingTokenSlot is { } slotKind)
            meta.Slots.Should().Contain(s => s.Kind == slotKind,
                $"{meta.Kind} entry with LeadingTokenSlot {slotKind} must have that slot in its Slots sequence");
    }
}

[Fact]
public void RuleDeclaration_HasRuleExpressionSlot()
{
    var meta = Constructs.GetMeta(ConstructKind.RuleDeclaration);
    meta.Slots.Should().HaveCount(3);
    meta.Slots[0].Kind.Should().Be(ConstructSlotKind.RuleExpression);
    meta.Slots[0].IsRequired.Should().BeTrue();
    meta.Slots[1].Kind.Should().Be(ConstructSlotKind.GuardClause);
    meta.Slots[1].IsRequired.Should().BeFalse();
    meta.Slots[2].Kind.Should().Be(ConstructSlotKind.BecauseClause);
    meta.Slots[2].IsRequired.Should().BeTrue();
}

[Theory]
[InlineData(ConstructKind.StateEnsure, 3)]   // In, To, From
[InlineData(ConstructKind.AccessMode, 1)]     // In (state-scoped only; root-level write all removed)
[InlineData(ConstructKind.StateAction, 2)]    // To, From
[InlineData(ConstructKind.EventEnsure, 1)]    // On (with Ensure disambiguation)
[InlineData(ConstructKind.EventHandler, 1)]   // On (with Arrow disambiguation)
[InlineData(ConstructKind.TransitionRow, 1)]  // From (with On disambiguation)
public void DisambiguatedConstructs_HaveCorrectEntryCount(ConstructKind kind, int expectedCount)
{
    Constructs.GetMeta(kind).Entries.Should().HaveCount(expectedCount);
}
```

**Regression anchors:** `GetMeta_ReturnsForEveryConstructKind`, `All_ContainsEveryKindExactlyOnce`, `All_IsInDeclarationOrder`, `Total_Count`, `TransitionRow_HasGuardClauseAndActionChainAsOptional`, `TransitionRow_HasRequiredOutcomeSlot`, `AllConstructs_HaveSlots`, `KeyConstructs_HaveMinimumSlotCount`.

#### Slice 1.5: Derived Indexes

**Modify:** `src/Precept/Language/Constructs.cs` — add two static properties after `All`:

```csharp
/// Leading token → list of (ConstructKind, DisambiguationEntry) for dispatch.
public static IReadOnlyDictionary<TokenKind, ImmutableArray<(ConstructKind Kind, DisambiguationEntry Entry)>>
    ByLeadingToken { get; } = All
        .SelectMany(meta => meta.Entries.Select(entry => (meta.Kind, entry)))
        .GroupBy(t => t.entry.LeadingToken)
        .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());

/// All leading tokens that can begin a declaration (for error sync).
public static FrozenSet<TokenKind> LeadingTokens { get; } = All
    .SelectMany(m => m.Entries)
    .Select(e => e.LeadingToken)
    .ToFrozenSet();
```

**Tests (new):**

```csharp
[Fact]
public void EveryLeadingTokenMapsToAtLeastOneConstruct()
{
    var allLeadingTokens = Constructs.All
        .SelectMany(m => m.Entries)
        .Select(e => e.LeadingToken)
        .Distinct()
        .ToHashSet();

    var indexedTokens = Constructs.ByLeadingToken.Keys.ToHashSet();
    allLeadingTokens.Should().BeEquivalentTo(indexedTokens);
}

[Fact]
public void LeadingTokens_ContainsAllExpectedTokens()
{
    Constructs.LeadingTokens.Should().Contain(TokenKind.Field);
    Constructs.LeadingTokens.Should().Contain(TokenKind.State);
    Constructs.LeadingTokens.Should().Contain(TokenKind.Event);
    Constructs.LeadingTokens.Should().Contain(TokenKind.Rule);
    Constructs.LeadingTokens.Should().Contain(TokenKind.From);
    Constructs.LeadingTokens.Should().Contain(TokenKind.In);
    Constructs.LeadingTokens.Should().Contain(TokenKind.To);
    Constructs.LeadingTokens.Should().Contain(TokenKind.On);
    Constructs.LeadingTokens.Should().Contain(TokenKind.Precept);
    // Note: TokenKind.Write is no longer a leading token — access modes use
    // TokenKind.Modify as disambiguation token under In, not as a leading token.
    // New tokens needed: Modify, Readonly, Editable (see F12).
}

[Theory]
[InlineData(TokenKind.In, 2)]    // StateEnsure + AccessMode
[InlineData(TokenKind.To, 2)]    // StateEnsure + StateAction
[InlineData(TokenKind.From, 3)]  // TransitionRow + StateEnsure + StateAction
[InlineData(TokenKind.On, 2)]    // EventEnsure + EventHandler
public void SharedLeadingTokens_HaveCorrectCandidateCount(TokenKind token, int expectedCount)
{
    Constructs.ByLeadingToken[token].Should().HaveCount(expectedCount);
}
```

#### Slice 1.6: Consumer Migration

**Modify:** `test/Precept.Tests/ConstructsTests.cs` — update `LeadingToken_IsCorrect` theory to use `PrimaryLeadingToken`:

```csharp
public void LeadingToken_IsCorrect(ConstructKind kind, TokenKind expectedToken)
{
    Constructs.GetMeta(kind).PrimaryLeadingToken.Should().Be(expectedToken, ...);
}
```

**Modify:** `src/Precept/Pipeline/Parser.cs` — no changes needed (parser is a stub, no `LeadingToken` references).

**Check:** `tools/Precept.Mcp/` — if any MCP tool serializes `LeadingToken`, update to `PrimaryLeadingToken`. Currently MCP tools are minimal (`PingTool.cs` only), so likely no changes needed.

**Check:** `tools/Precept.LanguageServer/` — search for `LeadingToken` references. Update to `PrimaryLeadingToken` or `Entries` as appropriate for completions and hover.

**Regression anchors:** Full existing test suite (all 3 test projects) must pass green.

---

### PR 2: Parser Infrastructure

**Goal:** Build the parser skeleton — dispatch loop, generic slot iteration, `InvokeSlotParser` exhaustive switch, `BuildNode` exhaustive switch, vocabulary FrozenDictionaries, AST node base types. No construct parsing yet — every parse method is a `throw new NotImplementedException()` stub.

**Depends on:** PR 1 (Entries, derived indexes, RuleExpression slot kind).

#### Slice 2.1: AST Node Type Hierarchy

**Create:** `src/Precept/Pipeline/SyntaxNodes/` directory with base types:

- `SyntaxNode.cs` — abstract base: `public abstract record SyntaxNode(SourceSpan Span);`
- `Declaration.cs` — abstract: `public abstract record Declaration(SourceSpan Span) : SyntaxNode(Span);`
- `Expression.cs` — abstract: `public abstract record Expression(SourceSpan Span) : SyntaxNode(Span);`
- One concrete `Declaration` subtype per `ConstructKind` (11 records, ~5–15 lines each).
- Shared node types: `IdentifierListNode`, `TypeRefNode`, `ModifierListNode`, `ArgumentListNode`, `ActionChainNode`, `OutcomeNode`, `StateTargetNode`, `EventTargetNode`, `BecauseClauseNode`, `FieldTargetNode`, `TokenValueNode`.

Each declaration node has one property per slot, typed to the slot's expected node type, nullable for optional slots. Example:

```csharp
public sealed record FieldDeclarationNode(
    SourceSpan Span,
    IdentifierListNode Identifiers,
    TypeRefNode TypeExpression,
    ModifierListNode? Modifiers,
    Expression? ComputeExpression)
    : Declaration(Span);
```

~200 lines total across files.

**Tests:** Type hierarchy tests — each declaration node is a `SyntaxNode`, sealed, has expected property count.

#### Slice 2.2: Vocabulary FrozenDictionaries (Layer A)

**Modify:** `src/Precept/Pipeline/Parser.cs` — convert from static class to non-static (instance holds state), or keep static with lazy initialization. Add:

- `OperatorPrecedence: FrozenDictionary<TokenKind, (int Precedence, bool RightAssociative)>` — derived from `Operators.All`.
- `TypeKeywords: FrozenSet<TokenKind>` — derived from `Types.All`.
- `ModifierKeywords: FrozenSet<TokenKind>` — derived from `Modifiers.All`.
- `ActionKeywords: FrozenSet<TokenKind>` — derived from `Actions.All`.

Each is a one-liner derivation from the appropriate catalog. ~30 lines total.

**Tests:**

```csharp
[Fact]
public void VocabularyDictionaries_ArePopulatedFromCatalogs()
{
    // Verify each vocabulary dictionary has at least one entry
    // and that known tokens are present.
    Parser.OperatorPrecedence.Should().ContainKey(TokenKind.Plus);
    Parser.TypeKeywords.Should().Contain(TokenKind.String);
    // ... etc
}
```

#### Slice 2.3: `InvokeSlotParser` Exhaustive Switch

**Modify:** `src/Precept/Pipeline/Parser.cs` — add `InvokeSlotParser` method:

```csharp
private SyntaxNode? InvokeSlotParser(ConstructSlotKind kind) => kind switch
{
    ConstructSlotKind.IdentifierList    => ParseIdentifierList(),
    ConstructSlotKind.TypeExpression    => ParseTypeExpression(),
    ConstructSlotKind.ModifierList      => ParseModifierList(),
    ConstructSlotKind.StateModifierList => ParseStateModifierList(),
    ConstructSlotKind.ArgumentList      => ParseArgumentList(),
    ConstructSlotKind.ComputeExpression => ParseComputeExpression(),
    ConstructSlotKind.GuardClause       => ParseGuardClause(),
    ConstructSlotKind.RuleExpression    => ParseRuleExpression(),
    ConstructSlotKind.ActionChain       => ParseActionChain(),
    ConstructSlotKind.Outcome           => ParseOutcome(),
    ConstructSlotKind.StateTarget       => ParseStateTarget(),
    ConstructSlotKind.EventTarget       => ParseEventTarget(),
    ConstructSlotKind.EnsureClause      => ParseEnsureClause(),
    ConstructSlotKind.BecauseClause     => ParseBecauseClause(),
    ConstructSlotKind.AccessModeKeyword => ParseAccessModeKeyword(),
    ConstructSlotKind.FieldTarget       => ParseFieldTarget(),
    _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
        $"No slot parser registered for {kind}")
};
```

Each `Parse*` method is initially a `throw new NotImplementedException()` stub (~16 stub methods, ~3 lines each).

CS8509 enforcement: adding a new `ConstructSlotKind` member without an arm is a build error.

**Tests:**

```csharp
[Fact]
public void EveryConstructSlotKindIsUsedByAtLeastOneConstruct()
{
    var usedKinds = Constructs.All
        .SelectMany(m => m.Slots)
        .Select(s => s.Kind)
        .Distinct()
        .ToHashSet();
    var allKinds = Enum.GetValues<ConstructSlotKind>().ToHashSet();
    allKinds.Except(usedKinds).Should().BeEmpty(
        "Every ConstructSlotKind member must be used by at least one construct");
}
```

#### Slice 2.4: `BuildNode` Exhaustive Switch

**Modify:** `src/Precept/Pipeline/Parser.cs` — add public `BuildNode` method:

```csharp
public static Declaration BuildNode(ConstructKind kind, SyntaxNode?[] slots, SourceSpan span) => kind switch
{
    ConstructKind.PreceptHeader     => new PreceptHeaderNode(span, ...),
    ConstructKind.FieldDeclaration  => new FieldDeclarationNode(span, ...),
    ConstructKind.StateDeclaration  => new StateDeclarationNode(span, ...),
    ConstructKind.EventDeclaration  => new EventDeclarationNode(span, ...),
    ConstructKind.RuleDeclaration   => new RuleDeclarationNode(span, ...),
    ConstructKind.TransitionRow     => new TransitionRowNode(span, ...),
    ConstructKind.StateEnsure       => new StateEnsureNode(span, ...),
    ConstructKind.AccessMode        => new AccessModeNode(span, ...),
    ConstructKind.StateAction       => new StateActionNode(span, ...),
    ConstructKind.EventEnsure       => new EventEnsureNode(span, ...),
    ConstructKind.EventHandler      => new EventHandlerNode(span, ...),
    _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
        $"Unknown ConstructKind: {kind}")
};
```

Each arm casts `slots[i]` to the expected node type. Named index constants per construct for clarity:

```csharp
// FieldDeclaration slot indices
private const int FieldSlot_Identifiers = 0;
private const int FieldSlot_Type = 1;
private const int FieldSlot_Modifiers = 2;
private const int FieldSlot_Compute = 3;
```

**Tests:**

```csharp
[Fact]
public void BuildNodeHandlesEveryConstructKind()
{
    // See T1 resolution in v7 §1 — try/catch pattern.
    var allKinds = Enum.GetValues<ConstructKind>();
    foreach (var kind in allKinds)
    {
        var meta = Constructs.GetMeta(kind);
        var slots = new SyntaxNode?[meta.Slots.Count];
        try
        {
            Parser.BuildNode(kind, slots, default);
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.Fail($"BuildNode has no arm for ConstructKind.{kind}");
        }
        catch (Exception)
        {
            // Null-propagation exceptions expected — arm exists.
        }
    }
}
```

#### Slice 2.5: `ParseConstructSlots` Generic Loop

**Modify:** `src/Precept/Pipeline/Parser.cs` — add generic slot iteration:

```csharp
private SyntaxNode?[] ParseConstructSlots(ConstructMeta meta)
{
    var slots = new SyntaxNode?[meta.Slots.Count];
    for (int i = 0; i < meta.Slots.Count; i++)
    {
        var slot = meta.Slots[i];
        var node = InvokeSlotParser(slot.Kind);

        if (node is null && slot.IsRequired)
        {
            EmitDiagnostic(DiagnosticCode.MissingRequiredSlot, Current().Span);
            // Produce a missing/error node for error recovery
        }

        slots[i] = node;
    }
    return slots;
}
```

~15 lines.

**Tests:** Integration tests deferred to PR 3 (needs actual slot parsers).

#### Slice 2.6: Slot-Ordering Drift Tests

**Create:** `test/Precept.Tests/SlotOrderingDriftTests.cs`

George's three drift tests from v4 § 3:

```csharp
[Fact]
public void PreParsedInjection_AnchorSlotIsAlwaysAtIndex0()
{
    // For all constructs whose Entries include a scoped preposition
    // (In, To, From, On), verify Slots[0] is StateTarget or EventTarget.
    var scopedConstructs = Constructs.All
        .Where(m => m.Entries.Any(e =>
            e.LeadingToken is TokenKind.In or TokenKind.To
                           or TokenKind.From or TokenKind.On));

    foreach (var meta in scopedConstructs)
    {
        meta.Slots[0].Kind.Should().BeOneOf(
            ConstructSlotKind.StateTarget,
            ConstructSlotKind.EventTarget,
            $"{meta.Kind} scoped construct must have anchor at Slots[0]");
    }
}

[Fact]
public void PreParsedInjection_GuardSlotPositionMatchesExpectation()
{
    // Constructs that support pre-disambiguation guards must have
    // GuardClause at a known index for injection.
    var guardExpectations = new Dictionary<ConstructKind, int>
    {
        [ConstructKind.TransitionRow] = 2,  // [StateTarget, EventTarget, GuardClause, ...]
        [ConstructKind.StateEnsure] = -1,   // No guard slot (guard is in ensure expression)
        [ConstructKind.AccessMode] = 2,    // [FieldTarget, AccessModeKeyword, GuardClause] — guard is post-field (F12)
    };
    // ... verify each expectation
}

[Fact]
public void PreParsedInjection_OnlyRecognizedConstructsUseInjectionPath()
{
    // Only constructs with In/To/From/On leading tokens use injection.
    var injectionTokens = new HashSet<TokenKind> { TokenKind.In, TokenKind.To, TokenKind.From, TokenKind.On };
    foreach (var meta in Constructs.All)
    {
        var usesInjection = meta.Entries.Any(e => injectionTokens.Contains(e.LeadingToken));
        // If it uses injection, it must have StateTarget or EventTarget at Slots[0]
        // If it doesn't use injection, it must NOT have scoped preposition entries
    }
}
```

**Regression anchors:** All PR 1 tests pass unchanged.

---

### PR 3: Non-Disambiguated Constructs

**Goal:** Implement parsing for the 5 constructs with unique leading tokens: `PreceptHeader`, `FieldDeclaration`, `StateDeclaration`, `EventDeclaration`, `RuleDeclaration`. Replace `NotImplementedException` stubs with real parsers.

**Depends on:** PR 2 (parser skeleton, AST nodes, slot infrastructure).

#### Slice 3.1: Core Expression Parser (Pratt)

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement `ParseExpression(int minPrecedence)`:

- Pratt precedence climbing using `OperatorPrecedence` vocabulary dictionary.
- Atom parsing: identifiers, literals (string, number, boolean), function calls, parenthesized expressions.
- Infix operators: `+`, `-`, `*`, `/`, `>`, `<`, `>=`, `<=`, `==`, `!=`, `and`, `or`.
- Prefix operators: `not`, `-`.
- Natural termination on tokens with no left-binding power (`when`, `because`, `->`, keywords, end of line).

~100–150 lines. This is the largest single method.

**Tests:**

```csharp
[Theory]
[InlineData("amount > 0", "amount > 0")]
[InlineData("a + b * c", "a + (b * c)")]
[InlineData("not active", "not active")]
[InlineData("x > 0 and y > 0", "(x > 0) and (y > 0)")]
public void ParseExpression_ProducesPrecedenceCorrectTree(string input, string expected) { ... }
```

~15 expression parse tests.

#### Slice 3.2: Slot Parsers for Simple Constructs

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement the slot parsers needed by non-disambiguated constructs:

| Method | ~Lines | Behavior |
|--------|--------|----------|
| `ParseIdentifierList()` | ~15 | Consume identifiers separated by commas. Return `IdentifierListNode`. |
| `ParseTypeExpression()` | ~20 | Check for `As`, consume type keyword from `TypeKeywords`, parse qualifiers. |
| `ParseModifierList()` | ~15 | Greedily consume tokens in `ModifierKeywords`. |
| `ParseStateModifierList()` | ~15 | Greedily consume state modifier keywords. |
| `ParseArgumentList()` | ~25 | Check for `(`, parse `name as type` pairs separated by commas, check for `)`. |
| `ParseComputeExpression()` | ~10 | Check for `=`, call `ParseExpression(0)`. |
| `ParseRuleExpression()` | ~5 | Call `ParseExpression(0)` directly — no intro token (F8). |
| `ParseGuardClause()` | ~8 | Check for `When`, consume, call `ParseExpression(0)`. Return null if no `When`. |
| `ParseBecauseClause()` | ~8 | Check for `Because`, consume, parse string literal. |

**Tests per construct (round-trip parse tests):**

```csharp
// FieldDeclaration
[Theory]
[InlineData("field amount as money nonnegative")]
[InlineData("field name, description as string")]
[InlineData("field total as money = principal + interest")]
public void ParseFieldDeclaration_ValidInputs(string input) { ... }

// StateDeclaration
[Theory]
[InlineData("state Draft initial, Submitted, Approved terminal success")]
[InlineData("state Active")]
public void ParseStateDeclaration_ValidInputs(string input) { ... }

// EventDeclaration
[Theory]
[InlineData("event Submit")]
[InlineData("event Submit(approver as string)")]
[InlineData("event Create initial")]
public void ParseEventDeclaration_ValidInputs(string input) { ... }

// RuleDeclaration
[Theory]
[InlineData("rule amount > 0 because \"Amount must be positive\"")]
[InlineData("rule amount > 0 when Active because \"Active amount must be positive\"")]
public void ParseRuleDeclaration_ValidInputs(string input) { ... }
```

~20 positive tests, ~10 negative tests (missing required slots produce diagnostics).

**Regression anchors:** All PR 1 and PR 2 tests pass.

#### Slice 3.3: Top-Level Dispatch (Unique Tokens)

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement the dispatch loop for unique leading tokens:

```csharp
public static SyntaxTree Parse(TokenStream tokens)
{
    var parser = new ParserState(tokens);
    while (parser.Current().Kind != TokenKind.EndOfSource)
    {
        var token = parser.Current();
        if (Constructs.ByLeadingToken.TryGetValue(token.Kind, out var candidates))
        {
            if (candidates.Length == 1 && candidates[0].Entry.DisambiguationTokens is null)
            {
                // Unique leading token — direct parse
                parser.ParseConstruct(candidates[0].Kind);
            }
            else
            {
                // Disambiguation needed — PR 4/5
                parser.EmitDiagnostic(...);
                parser.SyncToNextDeclaration();
            }
        }
        else
        {
            parser.EmitDiagnostic(...);
            parser.SyncToNextDeclaration();
        }
    }
    return parser.BuildSyntaxTree();
}
```

**Tests:** End-to-end parse tests using multi-declaration inputs:

```csharp
[Fact]
public void Parse_MultipleDeclarations_ProducesCorrectNodeCount()
{
    var input = """
        precept TestApp
        field name as string
        state Draft initial
        event Submit
        rule name != "" because "Name required"
        """;
    var tree = Parse(Lex(input));
    tree.Declarations.Should().HaveCount(5);
}
```

---

### PR 4: Disambiguated Constructs — Simple

**Goal:** Implement the generic disambiguator and parsing for constructs with `in`, `to`, and `on` leading tokens. These are "simple" because their disambiguation is straightforward: consume anchor, optionally consume `when` guard (G1), match disambiguation token, route.

**Depends on:** PR 3 (slot parsers, expression parser).

#### Slice 4.1: Generic Disambiguator

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement `DisambiguateAndParse(ImmutableArray<(ConstructKind, DisambiguationEntry)> candidates)`:

```csharp
private Declaration DisambiguateAndParse(
    ImmutableArray<(ConstructKind Kind, DisambiguationEntry Entry)> candidates,
    TokenKind leadingToken)
{
    // 1. Leading token already consumed by caller.
    // 2. Parse anchor target (state or event name).
    var anchor = ParseAnchorTarget(leadingToken);

    // 3. Pre-consume optional 'when' guard (G1 — mandatory).
    Expression? stashedGuard = null;
    if (Current().Kind == TokenKind.When)
    {
        Advance(); // consume 'when'
        stashedGuard = ParseExpression(0);
    }

    // 4. Match current token against each candidate's DisambiguationTokens.
    var disambigToken = Current().Kind;
    foreach (var (kind, entry) in candidates)
    {
        if (entry.DisambiguationTokens is { } tokens && tokens.Contains(disambigToken))
        {
            return ParseDisambiguatedConstruct(kind, anchor, stashedGuard);
        }
    }

    // No match — emit diagnostic, error recovery.
    EmitDiagnostic(DiagnosticCode.UnexpectedTokenInDisambiguation, Current().Span);
    SyncToNextDeclaration();
    return CreateErrorNode();
}
```

~40 lines.

#### Slice 4.2: Anchor + Guard Injection

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement `ParseDisambiguatedConstruct()`:

The method:
1. Looks up `ConstructMeta` for the routed `ConstructKind`.
2. Allocates the slot array.
3. Injects the pre-parsed anchor into `slots[0]` (always index 0 — verified by drift test).
4. Injects the stashed guard into the `GuardClause` slot index (if present).
5. Calls `ParseConstructSlots()` for remaining slots, skipping already-filled indices.
6. Calls `BuildNode()`.

~25 lines.

#### Slice 4.3: Remaining Slot Parsers

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement remaining slot parsers:

| Method | ~Lines | Behavior |
|--------|--------|----------|
| `ParseStateTarget()` | ~10 | Consume identifier or `Any` quantifier. |
| `ParseEventTarget()` | ~10 | Consume identifier. |
| `ParseEnsureClause()` | ~10 | Check for `Ensure`, consume, call `ParseExpression(0)`. |
| `ParseActionChain()` | ~25 | Peek for `->` (F3), consume arrows and action statements. |
| `ParseOutcome()` | ~20 | Check for `->`, parse `transition State` / `no transition` / `reject "msg"`. |
| `ParseAccessModeKeyword()` | ~8 | Match `Readonly`/`Editable`, return `TokenValueNode`. (Note: these are the B4 adjective keywords. `Modify` is consumed by the disambiguator, not stored as a slot.) |
| `ParseFieldTarget()` | ~15 | Consume identifier, comma-separated identifier list, or `All`. Supports singular (`Amount`), list (`Amount, Balance, Notes`), and `all` forms. |

#### Slice 4.4: `In`-Scoped Constructs

Implements parsing for:
- `AccessMode` via `in State modify Field readonly|editable [when Guard]`
- `StateEnsure` via `in State [when Guard] ensure Expr because Msg`

The disambiguator for `In`-scoped constructs:
1. Consumes `in` and parses state target.
2. Peeks for disambiguation token: `modify` → `AccessMode`, `omit` → `AccessMode` (omit sub-case), `ensure` → `StateEnsure`.
3. For `modify`: consumes `modify`, parses `FieldTarget`, then `AccessModeKeyword` (`readonly`/`editable`), then optional `GuardClause`.
4. For `omit`: consumes `omit`, parses `FieldTarget` only — no adjective slot, no guard.

**Tests:**

```csharp
[Theory]
[InlineData("in Draft modify Amount editable")]
[InlineData("in UnderReview modify DecisionNote editable when DocumentsVerified")]
[InlineData("in Approved modify Balance readonly")]
[InlineData("in Processing modify Amount readonly when not Finalized")]
[InlineData("in Closed omit Amount")]
[InlineData("in Red modify VehiclesWaiting, LeftTurnQueued editable")]
[InlineData("in Draft modify EmployeeName, Department, AccessReason editable")]
[InlineData("in Approved modify all readonly")]
[InlineData("in Archived omit Notes, Attachments")]
[InlineData("in Terminal omit all")]
public void ParseAccessMode_InScoped(string input) { ... }

[Theory]
[InlineData("in Approved ensure amount > 0 because \"Approved amount positive\"")]
public void ParseStateEnsure_InScoped(string input) { ... }

// Disambiguation tests
[Fact]
public void InScoped_RoutesToAccessMode_WhenModifyFollowsState()
{
    var tree = Parse(Lex("in Draft modify Amount editable"));
    tree.Declarations[0].Should().BeOfType<AccessModeNode>();
}

[Fact]
public void InScoped_RoutesToAccessMode_WhenOmitFollowsState()
{
    var tree = Parse(Lex("in Closed omit Amount"));
    tree.Declarations[0].Should().BeOfType<AccessModeNode>();
}

[Fact]
public void InScoped_RoutesToStateEnsure_WhenEnsureFollowsState()
{
    var tree = Parse(Lex("in Draft ensure amount > 0 because \"msg\""));
    tree.Declarations[0].Should().BeOfType<StateEnsureNode>();
}
```

#### Slice 4.5: `To`-Scoped Constructs

- `StateEnsure` via `to State ensure Expr because Msg`
- `StateAction` via `to State -> Actions`

#### Slice 4.6: `On`-Scoped Constructs

- `EventEnsure` via `on Event ensure Expr because Msg`
- `EventHandler` via `on Event -> Actions`

**Tests per slice:** ~8 positive disambiguation tests, ~4 negative (wrong disambiguation token) per leading token.

**Regression anchors:** All PR 1–3 tests pass.

---

### PR 5: Disambiguated Constructs — From + Error Sync

**Goal:** Implement `from`-scoped three-way disambiguation, pre-event guard rejection (F9), and `SyncToNextDeclaration()` error recovery. (Note: Slice 5.3 `write all` LeadingTokenSlot injection has been removed — `write all` is no longer valid syntax.)

**Depends on:** PR 4 (disambiguator, all slot parsers).

#### Slice 5.1: `From`-Scoped Three-Way Disambiguation

`from` has three candidates:

| Token after guard | Routes to |
|-------------------|-----------|
| `On` | `TransitionRow` |
| `Ensure` | `StateEnsure` |
| `Arrow` (`->`) | `StateAction` |

The generic disambiguator from PR 4 handles this — no special case needed. The three entries in `TransitionRow.Entries`, `StateEnsure.Entries`, and `StateAction.Entries` all include `TokenKind.From` as a leading token with the appropriate disambiguation tokens.

**Tests:**

```csharp
[Fact]
public void FromScoped_RoutesToTransitionRow_WhenOnFollows()
{
    var tree = Parse(Lex("from Draft on Submit -> transition Submitted"));
    tree.Declarations[0].Should().BeOfType<TransitionRowNode>();
}

[Fact]
public void FromScoped_RoutesToStateEnsure_WhenEnsureFollows()
{
    var tree = Parse(Lex("from Submitted ensure amount > 0 because \"msg\""));
    tree.Declarations[0].Should().BeOfType<StateEnsureNode>();
}

[Fact]
public void FromScoped_RoutesToStateAction_WhenArrowFollows()
{
    var tree = Parse(Lex("from Submitted -> set submittedAt = now()"));
    tree.Declarations[0].Should().BeOfType<StateActionNode>();
}
```

#### Slice 5.2: Pre-Event Guard Rejection (F9)

**Modify:** `src/Precept/Pipeline/Parser.cs` — in `ParseDisambiguatedConstruct()`, add diagnostic:

When a `from`-scoped construct routes to `TransitionRow` (disambiguation token is `On`) AND a guard was pre-consumed (stashedGuard is not null):

```csharp
if (kind == ConstructKind.TransitionRow && stashedGuard is not null)
{
    EmitDiagnostic(
        DiagnosticCode.PreEventGuardNotAllowed,
        guardSpan,
        "Guard must follow the event name in transition rows. " +
        "Move it after the event: 'from <State> on <Event> when <expr> -> ...'");
    // Error recovery: inject the guard at the post-event GuardClause slot anyway.
}
```

~10 lines.

**Add:** `DiagnosticCode.PreEventGuardNotAllowed` to `src/Precept/Language/DiagnosticCode.cs`.

**Tests:**

```csharp
[Fact]
public void FromScoped_PreEventGuard_EmitsDiagnostic()
{
    var tree = Parse(Lex("from Submitted when Active on Approve -> transition Approved"));
    tree.Diagnostics.Should().ContainSingle(d =>
        d.Code == DiagnosticCode.PreEventGuardNotAllowed);
    // The construct still parses (error recovery) — guard lands in GuardClause slot.
    tree.Declarations[0].Should().BeOfType<TransitionRowNode>();
}

[Fact]
public void FromScoped_PostEventGuard_NoDiagnostic()
{
    var tree = Parse(Lex("from Submitted on Approve when Active -> transition Approved"));
    tree.Diagnostics.Should().BeEmpty();
}
```

#### Slice 5.3: ~~`write all` LeadingTokenSlot Injection~~ REMOVED

~~**Modify:** `src/Precept/Pipeline/Parser.cs`~~

Root-level `write all` has been removed from the language (decision 2026-04-28). Stateless precepts use the `writable` modifier on field declarations. The `LeadingTokenSlot` injection path is no longer needed for `AccessMode`. This slice is removed from the implementation plan.

The `LeadingTokenSlot` mechanism in `DisambiguationEntry` remains available for future constructs that need it, but has no current consumer.

~~**Tests:**~~

Removed. The following tests are no longer applicable:

```csharp
// REMOVED — write all is no longer valid syntax
// [Theory]
// [InlineData("write all")]
// [InlineData("read all")]
// [InlineData("omit all")]
// public void ParseAccessMode_RootLevel(string input) { ... }
```

#### Slice 5.4: Error Sync (Layer E)

**Modify:** `src/Precept/Pipeline/Parser.cs` — implement `SyncToNextDeclaration()`:

```csharp
private void SyncToNextDeclaration()
{
    while (Current().Kind != TokenKind.EndOfSource)
    {
        if (Constructs.LeadingTokens.Contains(Current().Kind))
            return; // Found the start of the next declaration.
        Advance();
    }
}
```

~10 lines. Uses `Constructs.LeadingTokens` — the `FrozenSet<TokenKind>` derived from catalog metadata in PR 1.

**Tests:**

```csharp
[Fact]
public void ErrorSync_SkipsGarbageAndResumesAtNextDeclaration()
{
    var input = """
        field name as string
        @@garbage@@
        state Draft initial
        """;
    var tree = Parse(Lex(input));
    tree.Declarations.Should().HaveCount(2);
    tree.Diagnostics.Should().NotBeEmpty("garbage should produce a diagnostic");
}

[Fact]
public void ErrorSync_RecoverAfterMissingSlot()
{
    var input = """
        field as string
        state Draft initial
        """;
    var tree = Parse(Lex(input));
    // Field has missing identifier — diagnostic emitted, but state still parses.
    tree.Declarations.Should().HaveCount(2);
}
```

~8 error recovery tests.

**Regression anchors:** All PR 1–4 tests pass. Full sample file round-trip tests added:

```csharp
[Theory]
[InlineData("samples/loan-application.precept")]
[InlineData("samples/insurance-claim.precept")]
[InlineData("samples/customer-profile.precept")]
public void SampleFile_ParsesWithoutErrors(string path)
{
    var source = File.ReadAllText(path);
    var tree = Parser.Parse(Lexer.Lex(source));
    tree.Diagnostics.Should().BeEmpty();
}
```

---

## §4: Tooling/MCP Sync Assessment

| Surface | PR | Impact | Action |
|---------|-----|--------|--------|
| **`precept_language` MCP tool** | PR 1 | `ConstructMeta` shape changes — `LeadingToken` → `Entries`. If `LanguageTool.cs` serializes `LeadingToken`, it will hit the `[Obsolete]` alias. | Check `LanguageTool.cs` for `LeadingToken` references. Update to `PrimaryLeadingToken` or serialize `Entries` array. Currently MCP tools are minimal (only `PingTool.cs` exists), so likely no changes needed. |
| **`precept_compile` MCP tool** | PR 5 | Parser becomes functional. The tool will produce real AST output instead of `NotImplementedException`. | No code changes to MCP — it already wraps `Compiler.Compile()`. The compiler's parser stage now works. |
| **TextMate grammar** | None | Grammar is generated from `Tokens` catalog, not from `Constructs` catalog or parser. Parser changes do not affect syntax highlighting. | No changes needed. |
| **Language server completions** | PR 1 | LS may reference `LeadingToken` for context-aware completions. | Search LS codebase for `LeadingToken` references. Update to `PrimaryLeadingToken` or `Entries` as appropriate. |
| **Language server semantic tokens** | None | Semantic tokens derive from the type checker's output, not from the parser's construct dispatch. | No changes needed until type checker is updated. |
| **Language server diagnostics** | PR 5 | Parser diagnostics (missing slots, pre-event guard error, unexpected tokens) will flow through to LS diagnostic display. | Verify diagnostic format and severity match LS expectations. |

---

## §5: Design Loop Conclusion

### What Was Accomplished Across R1–R7

Seven rounds of design review between Frank (language designer) and George (runtime dev), with Shane's input at key decision points, produced a complete, validated parser architecture.

**Round 1** established the full vision: `DisambiguationEntry`, generic disambiguation, generic slot iteration, and the five-layer parser shape.

**Round 2** (George) identified six concrete issues: `LeadingTokenSlot`, `BuildNode` switch, `ActionChain` peek, `when` guard positions, explicit disambiguation tokens, and the migration bridge.

**Round 3** resolved all six with decisions F1–F6. The architectural shape stabilized.

**Round 4** (George) provided PR-ready C# code for the complete `GetMeta()` rewrite, slot parser signatures, slot-ordering drift tests, and identified the G5 `RuleDeclaration` slot naming bug.

**Round 5** designed the four-tier validation layer responding to Shane's directive, resolved G5 with `RuleExpression` slot kind (F8), withdrew pre-event guard support (F9), confirmed `EnsureClause`/`BecauseClause` separation (F10), and replaced dictionary with exhaustive switch for slot parsers (F7).

**Round 6** (George) confirmed all F7–F10 decisions, identified the T1 test nit, and surfaced the language simplification analysis for Shane's consideration.

**Round 7** closed both remaining open items (L1 guard position confirmed by Shane as F11, T1 test fix documented) and authored the full implementation plan.

### What the Implementation Team Receives

1. **A stable, fully validated design** with 17 resolved decisions (F1–F11, G1–G6) and zero open items.

2. **Five PRs in dependency order** with method-level specificity, exact file paths, tests per slice, regression anchors, and dependency ordering — meeting the CONTRIBUTING.md implementation plan quality bar.

3. **PR-ready C# code** for key artifacts: `DisambiguationEntry` record, `ConstructMeta` migration, `InvokeSlotParser` switch, `BuildNode` switch, disambiguator flow, error sync, and all test patterns.

4. **A validation layer** ensuring that adding a new language construct fails loudly at build time (CS8509) or test time (drift tests, exhaustiveness assertions) — never silently at runtime.

5. **A tooling/MCP sync assessment** per PR, with explicit "changes needed" or "no changes needed" per surface.

### File Inventory

| File | PRs |
|------|-----|
| `src/Precept/Language/DisambiguationEntry.cs` | PR 1 (create) |
| `src/Precept/Language/Construct.cs` | PR 1 (modify) |
| `src/Precept/Language/ConstructSlot.cs` | PR 1 (modify) |
| `src/Precept/Language/Constructs.cs` | PR 1 (modify) |
| `src/Precept/Language/DiagnosticCode.cs` | PR 5 (modify) |
| `src/Precept/Pipeline/Parser.cs` | PR 2–5 (modify) |
| `src/Precept/Pipeline/SyntaxTree.cs` | PR 2 (modify) |
| `src/Precept/Pipeline/SyntaxNodes/*.cs` | PR 2 (create ~15 files) |
| `test/Precept.Tests/ConstructsTests.cs` | PR 1 (modify) |
| `test/Precept.Tests/SlotOrderingDriftTests.cs` | PR 2 (create) |
| `test/Precept.Tests/ParserTests.cs` | PR 3–5 (create, then extend) |
| `test/Precept.Tests/DisambiguationTests.cs` | PR 4–5 (create, then extend) |
| `test/Precept.Tests/ErrorRecoveryTests.cs` | PR 5 (create) |

---

*This document marks the end of the catalog-driven parser design loop. The design is complete and ready for implementation.*
