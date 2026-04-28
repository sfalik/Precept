# Catalog-Driven Parser: Full Vision Design (Round 2)

**By:** George (Runtime Dev)
**Date:** 2026-04-27
**Status:** Design session Round 2 — George's challenge and extension of Frank's v1
**References:**
- `docs/working/catalog-parser-design-v1.md` — Frank's Round 1 (the base this document revises)
- `docs/working/frank-ast-catalog-reference.md` — Frank's ruling on factory-on-catalog
- `docs/working/george-catalog-parser-cross-review.md` — George's prior cross-review
- `docs/working/frank-catalog-parser-cross-review.md` — Frank's prior cross-review
- `src/Precept/Language/Constructs.cs` — actual catalog source
- `src/Precept/Pipeline/Parser.cs` — parser stub
- `docs/compiler/parser.md` — parser design doc

---

## Executive Summary

Frank's v1 design is architecturally sound and substantially correct. I'm not tearing it down —
I'm hardening it. There are two genuine implementation bugs, one implicit contract that needs
making explicit, and one missing mechanism (`LeadingTokenSlot`) needed for AccessMode's `write`
leading-token path. The generic disambiguation design — which I previously dismissed — survives
scrutiny for all four preposition tokens. The strangler-fig migration path Frank skipped ("no
migration needed") is real: the catalog shape change breaks existing consumers before the parser
is even written. That needs a sequenced PR plan.

**What changed from v1:**

| Topic | v1 Position | v2 Correction |
|-------|------------|---------------|
| ActionChain/Outcome boundary | ActionChain consumes final `->`, Outcome also expects `->` | **Bug**: they fight over the `->`. Fix: ActionChain peeks before consuming when outcome keyword follows. |
| AccessMode `write` leading path | `ParseConstructSlots()` called with `Write` already consumed, then tries to parse `AccessModeKeyword` fresh | **Bug**: misses the `Write` token. Fix: add `LeadingTokenSlot` to `DisambiguationEntry`. |
| Disambiguation token = slot introduction token | Implicit | **Contract must be explicit.** All slot parsers for slots that immediately follow a disambiguation token must consume that token as their first action. |
| `DisambiguationEntry` breaks consumers | "No migration" | **Breaking catalog shape change.** Add `PrimaryLeadingToken` convenience property. Sequence the catalog PR before the parser PR. |
| Factory switch vs dictionary | Dictionary (testable) | **Split by purpose.** `_slotParsers`: dictionary (registry pattern, correct). `BuildNode`: exhaustive switch (compiler enforcement, correct). |
| All 4 preposition tokens under generic disambiguator | Claimed to work | **Verified**, with corrections above. |
| `when` guard in TransitionRow at two source positions | Claimed uniform | **Confirmed uniform**: pre-disambiguation guard and post-EventTarget guard both inject correctly into slot[2], from different parse-time positions. Needs explicit documentation. |

---

## Part I — Sections Inherited from Frank's v1 (with Annotations)

---

### 1. Vision Statement ✅ Sound — adopt as-is

"The parser has zero hardcoded vocabulary. Every keyword, operator, type name, modifier, action,
construct shape, disambiguation path, and sync token is derived from catalog metadata at parser
construction time."

This is the right target. The test ("can you change the language surface by editing only catalog
entries?") is the right acceptance criterion.

The framing shift — "we're writing the parser for the first time, not refactoring an existing one"
— is correct and was the key move that unlocked Layer D. I accept it fully.

---

### 2. Layer A: Vocabulary Tables ✅ Sound — adopt as-is

The frozen dictionary derivations are straightforward. All four catalogs (`Operators`, `Types`,
`Modifiers`, `Actions`) carry everything needed. The `SetType` parser-synthesis exception and
`Initial` dual-modifier resolution are documented correctly.

One notation note: Frank's code shows `Types.All.Where(t => t.Token is not null)`. This is
correct — internal types (like `SetType`) have no `Token`. The filter must be explicit.

Modifiers needs `IReadOnlyList<ModifierMeta>` not a single value for `ByToken` because
`TokenKind.Initial` maps to both `ModifierKind.InitialState` and `ModifierKind.InitialEvent`.
I documented this in my original estimate. Frank's v1 does not show the Modifiers.ByToken shape
explicitly — it should return a list type, not `ModifierMeta`.

---

### 3. Layer B: Dispatch Table ✅ Sound (with catalog migration caveat)

The `ByLeadingToken` derived index and `LeadingTokens` set are correct. The dispatch loop
structure — dictionary lookup + `Length == 1` shortcut + disambiguation fallthrough — is right.

**Caveat:** This is derived from `Entries` (plural) on `ConstructMeta`, not from the existing
single `LeadingToken` field. The existing catalog source (`Constructs.cs`) still has
`LeadingToken: TokenKind` as the sole field. Adding `Entries` is the catalog shape change,
and it breaks consumers. See Part II → Migration Strategy.

---

### 4. Layer C: DisambiguationEntry ⚠️ Needs adjustment

Frank's `DisambiguationEntry` design is largely correct. Two issues:

#### Issue C1: Implicit contract — disambiguation token = slot introduction token

The generic disambiguator leaves the disambiguation token unconsumed:

```csharp
// Step 3: Match disambiguation token
var disambigToken = Current().Kind;
foreach (var (meta, entry) in candidates)
{
    if (entry.DisambiguationTokens is null) continue;
    if (entry.DisambiguationTokens.Value.Contains(disambigToken))
        return ParseConstructBodyWithPreParsedSlots(meta, leadingSpan, anchor, guard);
}
```

The code finds a match on `disambigToken` but does NOT call `Advance()`. This means the
disambiguation token is still in the token stream when `ParseConstructBodyWithPreParsedSlots`
starts iterating slots.

This works ONLY IF the slot parser for the first non-anchor slot consumes the disambiguation
token as its introduction keyword. Let me verify all cases:

| Leading token | Disambiguation token | First slot after anchor | Slot parser consumes it |
|--------------|---------------------|------------------------|------------------------|
| `In`, `To`, `From` | `Ensure` | EnsureClause | `ParseEnsureClause()` calls `Expect(TokenKind.Ensure)` ✅ |
| `To`, `From` | `Arrow` (`->`) | ActionChain | `ParseActionChain()` checks `Current().Kind == TokenKind.Arrow` ✅ |
| `From` | `On` | EventTarget | `ParseEventTarget()` calls `Expect(TokenKind.On)` ✅ |
| `In` | `Write`, `Read`, `Omit` | AccessModeKeyword | `ParseAccessModeKeyword()` consumes the current token ✅ |

This contract holds for all current cases. But it's IMPLICIT — nowhere is it documented that
slot parsers must consume their introduction token. If a future slot parser is written assuming
the introduction token has already been consumed, the design breaks silently.

**Fix:** Document this as an explicit contract in the `_slotParsers` dictionary's code comment:

```csharp
// Slot parser contract: each parser is responsible for consuming its own
// introduction token(s). The generic disambiguator leaves disambiguation
// tokens unconsumed; the slot parser that immediately follows the anchor
// must consume that token as its first action (e.g., ParseEnsureClause
// must call Expect(TokenKind.Ensure), not assume it was pre-consumed).
private readonly FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>> _slotParsers;
```

#### Issue C2: AccessMode `Write`-leading path — the `LeadingTokenSlot` blocker

This is the genuine blocker. In `write all` (root-level stateless precept), the flow is:

1. Dispatch: see `Write`, `Advance()` consumes it, leadingSpan = span of `write`
2. `ByLeadingToken[Write]` → [(AccessMode, Entry{Write, null})] — single candidate
3. Dispatches to `ParseConstructSlots(candidates[0].Meta, leadingSpan)`
4. AccessMode's slots: `[SlotOptStateTarget, SlotAccessModeKeyword, SlotFieldTarget]`
5. Slot 0: `OptStateTarget` → `_slotParsers[StateTarget]()` → current is `all`, returns null ✅
6. Slot 1: `AccessModeKeyword` → `_slotParsers[AccessModeKeyword]()` → current is `all`, NOT `Write/Read/Omit` ❌

The `Write` token was the DISPATCH token. It is ALSO the AccessModeKeyword slot content.
The generic loop discards it. `ParseAccessModeKeyword()` finds the field target instead.

**This is a real bug that would silently parse `write all` incorrectly.**

**Fix:** Add `LeadingTokenSlot: ConstructSlotKind?` to `DisambiguationEntry`:

```csharp
/// <summary>
/// Describes one way a construct can be entered from its leading token.
/// Constructs with multiple leading tokens have multiple entries.
/// Constructs with one leading token have one entry.
/// </summary>
public sealed record DisambiguationEntry(
    TokenKind                      LeadingToken,
    ImmutableArray<TokenKind>?     DisambiguationTokens = null,
    /// <summary>
    /// When the leading token is also a slot value (not merely a dispatch signal),
    /// specifies which slot kind it occupies. The ParseSession injects a synthetic
    /// AccessModeKeywordNode (or equivalent) into this slot rather than parsing fresh.
    /// Null for constructs where the leading token is a pure dispatch token.
    /// </summary>
    ConstructSlotKind?             LeadingTokenSlot = null);
```

AccessMode's `Write` entry becomes:
```csharp
new(TokenKind.Write,
    DisambiguationTokens: null,
    LeadingTokenSlot: ConstructSlotKind.AccessModeKeyword)
```

`ParseConstructSlots` checks for this:
```csharp
private Declaration ParseConstructSlots(
    ConstructMeta meta, SourceSpan leadingSpan, DisambiguationEntry entry, Token leadingToken)
{
    var slots = new SyntaxNode?[meta.Slots.Count];
    for (int i = 0; i < meta.Slots.Count; i++)
    {
        var slot = meta.Slots[i];

        // If the leading token doubles as slot content, inject a synthetic node
        if (entry.LeadingTokenSlot == slot.Kind)
        {
            slots[i] = CreateLeadingTokenSlotNode(slot.Kind, leadingToken);
            continue;
        }

        slots[i] = _slotParsers[slot.Kind]();

        if (slots[i] is null && slot.IsRequired)
        {
            EmitDiagnostic(DiagnosticCode.ExpectedSlot, slot.Kind, meta.Kind);
            slots[i] = CreateMissingSlotNode(slot.Kind);
        }
    }

    var endSpan = PreviousToken().Span;
    return _nodeFactories[meta.Kind](SourceSpan.Covering(leadingSpan, endSpan), slots);
}
```

`CreateLeadingTokenSlotNode` is a small factory that produces the appropriate node type for
the given slot kind from the already-consumed token. For `AccessModeKeyword`, it creates an
`AccessModeKeywordNode(leadingToken.Span, leadingToken.Kind)`.

**Updated DisambiguationEntry table:**

| Construct | Entry.LeadingToken | Entry.DisambiguationTokens | Entry.LeadingTokenSlot |
|-----------|------------------|---------------------------|----------------------|
| `PreceptHeader` | `Precept` | null | null |
| `FieldDeclaration` | `Field` | null | null |
| `StateDeclaration` | `State` | null | null |
| `EventDeclaration` | `Event` | null | null |
| `RuleDeclaration` | `Rule` | null | null |
| `TransitionRow` | `From` | `[On]` | null |
| `StateEnsure` | `In` | `[Ensure]` | null |
| `StateEnsure` | `To` | `[Ensure]` | null |
| `StateEnsure` | `From` | `[Ensure]` | null |
| `AccessMode` | `Write` | null | `AccessModeKeyword` ← NEW |
| `AccessMode` | `In` | `[Write, Read, Omit]` | null |
| `StateAction` | `To` | `[Arrow]` | null |
| `StateAction` | `From` | `[Arrow]` | null |
| `EventEnsure` | `On` | `[Ensure]` | null |
| `EventHandler` | `On` | `[Arrow]` | null |

---

### 5. Layer D: Slot-Driven Productions ⚠️ Bug in ActionChain/Outcome boundary — must fix

The generic slot iteration loop is architecturally correct. The `_slotParsers` registry is right.
The factory dispatch is right. But Frank's `ParseActionChain()` implementation has a bug in its
boundary with `ParseOutcome()`.

**Frank's ActionChain pseudocode:**
```csharp
while (Current().Kind == TokenKind.Arrow)
{
    Advance(); // consume '->'

    // Check for outcome keywords — this is where ActionChain stops
    if (IsOutcomeKeyword(Current().Kind))
        break; // leave the token for ParseOutcome
```

**The bug:** ActionChain consumes `->` THEN checks for an outcome keyword. When it breaks,
the current token is `transition`/`no`/`reject` — the `->` is already gone.

Then `ParseOutcome()`:
```csharp
private OutcomeNode? ParseOutcome()
{
    if (Current().Kind != TokenKind.Arrow) return null;
    Advance(); // consume '->'
    ...
```

`ParseOutcome()` checks for `->` first and finds `transition` instead → returns null.
`SlotOutcome` is required → the generic iterator emits `ExpectedSlot` diagnostic on valid input.

**This is not an edge case. It fires on every TransitionRow that has an action chain.**

**Fix:** ActionChain must peek BEFORE consuming when an outcome keyword follows:

```csharp
private ActionChainNode? ParseActionChain()
{
    if (Current().Kind != TokenKind.Arrow) return null;

    var actions = ImmutableArray.CreateBuilder<ActionStatementNode>();

    while (Current().Kind == TokenKind.Arrow)
    {
        // Peek past '->' before consuming it. If the token after '->' is an outcome
        // keyword, leave the '->' in the stream for ParseOutcome() to consume as its
        // introduction token. This satisfies the slot-parser introduction-token contract.
        if (IsOutcomeKeyword(Peek(1).Kind))
            break;

        Advance(); // consume '->' only when we know an action follows

        var actionToken = Current();
        if (!_actionKeywords.Contains(actionToken.Kind))
        {
            EmitDiagnostic(DiagnosticCode.ExpectedAction, actionToken);
            SyncToNextAction(); // recover: skip to next '->' or outcome keyword
            break;
        }

        Advance(); // consume action keyword
        actions.Add(ParseActionStatement(actionToken));
    }

    return actions.Count > 0
        ? new ActionChainNode(SourceSpan.Covering(actions[0].Span, actions[^1].Span), actions.ToImmutable())
        : null;
}
```

With this fix, `ParseOutcome()` always sees `->` as the current token when there's an outcome.
The contract holds: `ParseOutcome()` is responsible for consuming its own `->` introduction token.

**Why this also works for event handlers and state actions (no Outcome slot):**

`ParseStateAction` and `ParseEventHandler` don't have an Outcome slot. Their slot sequences
end at ActionChain. `ParseActionChain()` runs until it sees a newline or `EndOfSource` — at
which point `Current().Kind != TokenKind.Arrow` is true, the while loop exits naturally,
and no `IsOutcomeKeyword` check is ever triggered. No behavior change.

---

### 6. Layer E: Error Recovery ✅ Sound — adopt as-is

Trivially derived from `Constructs.LeadingTokens`. Zero discussion needed. The derived
`LeadingTokens` set must be recomputed to read from `Entries`, not from the old `LeadingToken`
field.

---

### 7. Layer F: Anchor Type Derivation ✅ Sound — adopt as-is

The insight that "all candidates under the same leading token share the same first slot kind"
holds across all four preposition tokens. I verified against the actual catalog:

| Leading token | Candidate constructs | First slot |
|--------------|---------------------|-----------|
| `In` | StateEnsure, AccessMode | StateTarget (both) ✅ |
| `To` | StateEnsure, StateAction | StateTarget (both) ✅ |
| `From` | TransitionRow, StateEnsure, StateAction | StateTarget (all three) ✅ |
| `On` | EventEnsure, EventHandler | EventTarget (both) ✅ |

The anchor derivation from `candidates[0].Meta.Slots[0].Kind` is correct for all current
constructs. The assumption that all candidates under a leading token share the same first slot
kind is a structural property of the language, not an accident.

---

### 8. The `when` Guard in TransitionRow — Two Positions, One Mechanism ✅

Frank says the `when` guard is "uniform behavior" for scoped constructs. That's true but
incompletely described. There are actually two positions where a `when` expression can appear
in a TransitionRow source text, and the generic mechanism handles both:

**Position 1: Before the disambiguation token (pre-anchor guard)**
```precept
from Draft when Status == "ready" on Submit -> transition Submitted
```
Flow: `ParseFromScoped()` parses StateTarget `Draft` → sees `when` → consumes guard `Status == "ready"` → sees `on` → matches TransitionRow → calls `ParseConstructBodyWithPreParsedSlots(..., guard=exprNode)` → guard is injected into slot[2] (GuardClause).

**Position 2: After the EventTarget (standard position)**
```precept
from UnderReview on Approve when PoliceReportRequired ...
```
Flow: generic disambiguator parses StateTarget `UnderReview` → sees `on` (no `when` before disambiguation) → matches TransitionRow → calls `ParseConstructBodyWithPreParsedSlots(..., guard=null)` → slot[2] GuardClause is parsed normally via `_slotParsers[GuardClause]()`.

Both positions correctly populate slot[2] of `TransitionRowNode`. The pre-disambiguation guard
is parsed at parse-time before the slot iteration, then injected at the right position. The
post-EventTarget guard is parsed during slot iteration in its natural sequence. These are two
paths to the same result.

The only constraint: guard injection uses the "first GuardClause slot" heuristic. This is fine
for all current constructs. A construct with two GuardClause slots would need a more explicit
injection map. No current construct has this shape.

**Is `from X when expr on Y` actually semantically valid?** Per `parser.md` disambiguation
tables, `When` is a valid "then re-check" option for all four preposition methods. So yes, this
form is supported. The documentation should call out both positions explicitly.

---

### 9. Source Generation ✅ Frank's verdict stands — not yet, design is generator-ready

Frank's analysis is correct. I'll add two observations:

**The dependency-inversion argument for a generator:** A generator that reads `ConstructMeta`
would create a Language → Pipeline dependency (wrong direction). But a generator that reads
AST node record constructors (Pipeline) and infers the slot mapping by naming convention
(`IdentifierListNode` → `ConstructSlotKind.IdentifierList`) avoids this. The dependency stays
Pipeline → Language. This is theoretically possible.

**Why it still doesn't help now:** At 11 constructs, the generator infrastructure costs more
than the code it replaces. Naming conventions are fragile and would need enforcement by an
analyzer. The debugging story is worse. Frank's break-even estimate (~25–30 constructs) is right.

**What "generator-ready" means concretely:** The `ParseConstruct`/`ParseDisambiguated` methods
don't know whether `_nodeFactories` was built by hand or by a generator. If we ever add source
generation, those methods don't change. The generator just replaces the dictionary initializer.

---

### 10. Factory Dictionary vs. Exhaustive Switch ⚠️ Split by purpose

Frank: factory dictionary for `BuildNode`.
Me: exhaustive switch for `BuildNode`.
Frank: testability over CS8509.

**My updated position:** Split by the right data structure for each concern.

**`_slotParsers`: dictionary is correct.** It's a keyed registry of parsing functions. Adding
a new `ConstructSlotKind` means adding one entry. The dictionary IS the right shape for a
registry. CS8509 doesn't apply to a registry because slots are not exhaustively switched — they
are dispatched by key. Using a switch here would need a nested approach that's worse.

**`BuildNode`/`_nodeFactories`: exhaustive switch is correct.** The mapping from `ConstructKind`
to an AST node constructor IS an exhaustive per-construct pattern match. Every `ConstructKind`
MUST have a factory — this is an invariant, not a lookup. CS8509 enforces this at compile time.
The factory dictionary defers this invariant to a runtime test that might not catch every
deployment scenario.

**Proposed compromise:** Use the exhaustive switch but add named local indices to make slot
positions explicit and prevent silent ordering bugs:

```csharp
private static Declaration BuildNode(ConstructKind kind, SourceSpan span, SyntaxNode?[] slots)
    => kind switch
    {
        ConstructKind.FieldDeclaration => (static (span, slots) =>
        {
            const int Names = 0, TypeExpr = 1, Modifiers = 2, Compute = 3;
            return new FieldDeclarationNode(span,
                ((IdentifierListNode)slots[Names]!).Names,
                (TypeRefNode)slots[TypeExpr]!,
                ((ModifierListNode?)slots[Modifiers])?.Modifiers ?? [],
                (Expression?)slots[Compute]);
        })(span, slots),

        ConstructKind.TransitionRow => (static (span, slots) =>
        {
            const int FromState = 0, Event = 1, Guard = 2, Actions = 3, Outcome = 4;
            return new TransitionRowNode(span,
                (StateTargetNode)slots[FromState]!,
                (EventTargetNode)slots[Event]!,
                (Expression?)slots[Guard],
                ((ActionChainNode?)slots[Actions])?.Actions ?? [],
                (OutcomeNode)slots[Outcome]!);
        })(span, slots),

        // ... 9 more arms

        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
```

The named constants make the intent readable. A slot ordering change in the catalog immediately
makes the mismatch visible. If you change TransitionRow's slot sequence in `Constructs.GetMeta()`
and forget to update `BuildNode`, the slot constant names will look wrong even before compilation.

This preserves CS8509. The factory completeness test remains necessary as belt-and-suspenders
(it validates slot count alignment, not just key existence).

---

## Part II — New Sections

---

## Implementation Architecture

### The `_slotParsers` Registry: Concrete Shape

```csharp
private struct ParseSession
{
    // ═══════════════════════════════════════════════════════════════════════
    //  Catalog-derived vocabulary tables — built once at ParseSession init
    // ═══════════════════════════════════════════════════════════════════════
    private readonly FrozenDictionary<TokenKind, OperatorMeta>           _operators;
    private readonly FrozenSet<TokenKind>                                _typeKeywords;
    // IReadOnlyList because Initial maps to both InitialState + InitialEvent
    private readonly FrozenDictionary<TokenKind, IReadOnlyList<ModifierMeta>> _fieldModifiers;
    private readonly FrozenSet<TokenKind>                                _actionKeywords;

    // ═══════════════════════════════════════════════════════════════════════
    //  Slot parser registry — keyed dispatch, not exhaustive switch
    //  SLOT PARSER CONTRACT: Each parser is responsible for consuming its
    //  own introduction token. The generic iterator never pre-consumes the
    //  token that leads into a slot. See docs/working/catalog-parser-design-v2.md
    //  § Issue C1 for the invariant this contract upholds.
    // ═══════════════════════════════════════════════════════════════════════
    private readonly FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>> _slotParsers;

    // ═══════════════════════════════════════════════════════════════════════
    //  Node factory — exhaustive switch, NOT a dictionary
    //  Exhaustive = CS8509 fires when ConstructKind #12 is added
    // ═══════════════════════════════════════════════════════════════════════
    // private static Declaration BuildNode(ConstructKind, SourceSpan, SyntaxNode?[])
}
```

At `ParseSession` construction:

```csharp
_operators    = Operators.All.ToFrozenDictionary(o => o.Token.Kind);
_typeKeywords = Types.All.Where(t => t.Token is not null)
                    .Select(t => t.Token!.Kind).ToFrozenSet();
_fieldModifiers = Modifiers.All.OfType<FieldModifierMeta>()
                    .GroupBy(m => m.Token.Kind)
                    .ToFrozenDictionary(g => g.Key,
                        g => (IReadOnlyList<ModifierMeta>)g.ToArray());
_actionKeywords = Actions.All.Select(a => a.Token.Kind).ToFrozenSet();

_slotParsers = new Dictionary<ConstructSlotKind, Func<SyntaxNode?>>
{
    // Simple slots
    [ConstructSlotKind.IdentifierList]    = ParseIdentifierList,
    [ConstructSlotKind.TypeExpression]    = ParseTypeExpression,
    [ConstructSlotKind.ModifierList]      = ParseFieldModifiers,
    [ConstructSlotKind.StateModifierList] = ParseStateModifiers,
    [ConstructSlotKind.ArgumentList]      = ParseArgumentList,
    [ConstructSlotKind.ComputeExpression] = ParseComputeExpression,
    [ConstructSlotKind.StateTarget]       = ParseStateTarget,
    [ConstructSlotKind.EventTarget]       = ParseEventTarget,        // consumes 'on' keyword
    [ConstructSlotKind.EnsureClause]      = ParseEnsureClause,       // consumes 'ensure' keyword
    [ConstructSlotKind.BecauseClause]     = ParseBecauseClause,      // consumes 'because' keyword
    [ConstructSlotKind.AccessModeKeyword] = ParseAccessModeKeyword,  // consumes write/read/omit
    [ConstructSlotKind.FieldTarget]       = ParseFieldTarget,
    // Complex slots — each owns its own internal grammar
    [ConstructSlotKind.GuardClause]       = ParseGuardClause,        // consumes 'when' keyword
    [ConstructSlotKind.ActionChain]       = ParseActionChain,        // owns '->' loop
    [ConstructSlotKind.Outcome]           = ParseOutcome,            // consumes '->' introduction
}.ToFrozenDictionary();
```

### Type Signature of Factory Delegates — Why `SyntaxNode?[]` is Acceptable

The factory delegate signature is `Func<SourceSpan, SyntaxNode?[], Declaration>`. Every factory
receives a heterogeneous array and performs explicit casts. Frank asked if this fragility is
acceptable.

**It is acceptable with three mitigations, all of which we must implement:**

1. **Named local index constants** (described in § 10 above) — prevent wrong-index mistakes
   silently compiling.

2. **Slot-count alignment test** — for each `ConstructKind`, verify that the `BuildNode` arm
   expects exactly `Constructs.GetMeta(kind).Slots.Count` slots:

   ```csharp
   [Theory]
   [MemberData(nameof(AllConstructKinds))]
   public void BuildNode_SlotCountMatchesCatalog(ConstructKind kind)
   {
       var meta = Constructs.GetMeta(kind);
       // Parse a minimal valid example, capture the slot array passed to BuildNode
       // Verify slots.Length == meta.Slots.Count
   }
   ```

3. **Type-alignment comment in BuildNode** — each arm documents the expected type at each index:

   ```csharp
   ConstructKind.FieldDeclaration => (static (span, slots) =>
   {
       // Slot layout from Constructs.GetMeta(FieldDeclaration).Slots:
       // [0] IdentifierList (required) → IdentifierListNode
       // [1] TypeExpression (required) → TypeRefNode
       // [2] ModifierList (optional)   → ModifierListNode?
       // [3] ComputeExpression (optional) → Expression?
       const int Names = 0, TypeExpr = 1, Modifiers = 2, Compute = 3;
       ...
   ```

With these three in place, a wrong-slot-type cast produces an immediately obvious exception
on first exercise, not a silent wrong-value bug. The test suite catches slot count mismatches
before deployment.

### Source Generation — Conditions for Revisiting

Source generation becomes the right call when:
- Construct count reaches ~25–30
- OR the `BuildNode` arm comments + named constants become a maintenance burden during active language evolution
- OR a second pipeline stage (TypeChecker, Evaluator) needs the same catalog-to-code derivation

The design is generator-ready now. The generator would replace the `BuildNode` switch with a
generated method. `ParseConstruct` and `ParseDisambiguated` don't change.

---

## Migration Strategy — Sequenced PRs

"No migration needed" is true for the PARSER (it's a stub). It's false for the CATALOG SHAPE
CHANGE. `DisambiguationEntry` replacing `LeadingToken` is a breaking change. These consumers
read `LeadingToken` today:
- Language server: completions, `ByLeadingToken` lookup
- MCP: `precept_language` vocabulary output
- Existing `Constructs.All` derivations

The PR sequence:

### PR 1: Catalog Shape — `DisambiguationEntry` + Backward-Compatible Bridge

**What changes:**
- Add `DisambiguationEntry` record type to `src/Precept/Language/Construct.cs`
- Add `Entries: ImmutableArray<DisambiguationEntry>` to `ConstructMeta`
- Add `PrimaryLeadingToken` computed convenience property = `Entries[0].LeadingToken`
  (preserves backward compat for consumers that only need one leading token)
- Keep `LeadingToken` as an alias: `public TokenKind LeadingToken => PrimaryLeadingToken;`
- Populate all 11 `GetMeta` entries with their `Entries` arrays
- Add `Constructs.ByLeadingToken` derived index (new shape, returns Entry pairs)
- Add `Constructs.LeadingTokens` derived set (recomputed from Entries)

**Tests that gate this PR:**
- `ConstructMeta_AllKinds_HaveAtLeastOneEntry` — every construct has ≥1 entry
- `ByLeadingToken_AllCandidates_ShareFirstSlotKind` — the anchor derivation invariant
- `DisambiguationEntry_AccessMode_HasLeadingTokenSlot` — the Write path has LeadingTokenSlot set
- `ByLeadingToken_PrepositionTokens_HaveMultipleCandidates` — `In/To/From/On` each have ≥2

### PR 2: Vocabulary Tables — Layer A in ParseSession

**What changes:**
- Add `Types.ByToken`, `Modifiers.ByToken`, `Actions.ByToken` derived indexes to their catalogs
- ParseSession init: build vocabulary frozen dictionaries from catalog (Layer A)
- Update parser stub comment to reference the new architecture

**Tests that gate this PR:**
- Vocabulary coverage: all 80 catalog members appear in exactly one parser table
- Modifiers.ByToken returns list for `Initial`, single-item list for all others
- `SetType` exception documented: verify `Types.ByToken` does not contain a SetType entry

### PR 3: Dispatch + Disambiguation — Generic ParseAll

**What changes:**
- `ParseAll()`: dictionary lookup on `ByLeadingToken`, `candidates.Length == 1` shortcut
- `ParseDisambiguated()`: generic method consuming anchor, guard, then routing
- `SyncToNextDeclaration()`: built from `Constructs.LeadingTokens`
- `ParseConstructSlots()` + `ParseConstructBodyWithPreParsedSlots()` stubs
- `_slotParsers` dictionary registration (methods all still stub: `throw new NotImplementedException()`)
- `BuildNode` exhaustive switch stub (all arms throw)

**Tests that gate this PR:**
- Route test: mock `ParseConstruct` (return empty nodes), verify each leading token routes to the expected `ConstructKind`
- Disambiguation test: for `In/To/From/On`, verify each disambiguation token routes to the correct construct
- Sync recovery test: unknown token causes `UnexpectedToken` diagnostic and resync to next known leading token

### PRs 4–N: Slot Parsers + Node Factories — Vertical Slices by Construct

One or two constructs per PR. Suggested order by dependency and difficulty:

1. `PreceptHeader`, `FieldDeclaration` — simple, no disambiguation
2. `StateDeclaration`, `EventDeclaration`, `RuleDeclaration` — slightly more complex
3. `StateEnsure` (covers `In/To/From` entries), `AccessMode` (covers `Write`+`In` entries)
4. `StateAction` (covers `To/From` entries)
5. `EventEnsure`, `EventHandler` (covers `On` entries)
6. `TransitionRow` — last, because it needs ActionChain and Outcome

Each PR delivers:
- Slot parser implementations for new slots this construct introduces
- `BuildNode` arm for this construct
- Parse round-trip tests: valid source → correct AST structure
- Error recovery tests: missing required slot → diagnostic + missing node
- Factory alignment test: slot count in test matches `Constructs.GetMeta(kind).Slots.Count`

**Integration test gate (after all PRs):** Parse every file in `samples/`. Compare output AST
structure against golden snapshots. This is the "strangler fig" cutover test — when all 11
constructs pass sample file round-trips, the generic parser is complete.

---

## The Hard Cases — Concrete Walkthrough

### BuildDeclaration: AccessMode

AccessMode has two entry paths, each produces the same `AccessModeNode` shape:

**Path 1: `write all` (root-level)**
- Entry: `DisambiguationEntry(TokenKind.Write, null, LeadingTokenSlot: AccessModeKeyword)`
- Leading token `Write` consumed by dispatch loop
- `ParseConstructSlots` called with the `Write` token reference
- Slot 0 (`OptStateTarget`): `_slotParsers[StateTarget]()` → null (next token is `all`) ✅
- Slot 1 (`AccessModeKeyword`): `entry.LeadingTokenSlot == AccessModeKeyword` → inject
  `CreateLeadingTokenSlotNode(Write, leadingToken)` → `AccessModeKeywordNode(Write)` ✅
- Slot 2 (`FieldTarget`): `_slotParsers[FieldTarget]()` → parses `all` ✅
- `BuildNode` factory: `(span, slots) => new AccessModeNode(span, null, (AccessModeKeywordNode)slots[1]!, (FieldTargetNode)slots[2]!)`

**Path 2: `in Draft write Amount`**
- Leading token `In`, StateEnsure and AccessMode are candidates
- Anchor: parse StateTarget `Draft`
- No `when`
- Disambiguation token: current is `Write` → matches AccessMode `[Write, Read, Omit]`
- `ParseConstructBodyWithPreParsedSlots(AccessMode, ...)` called with anchor=`Draft`, guard=null
- Slot 0: injected anchor `Draft` ✅
- Slot 1 (`AccessModeKeyword`): `entry.LeadingTokenSlot` is null for the `In` entry;
  `_slotParsers[AccessModeKeyword]()` → current token is `Write`, consumed ✅
- Slot 2 (`FieldTarget`): `_slotParsers[FieldTarget]()` → parses `Amount` ✅

Both paths produce `AccessModeNode(span, stateTarget?, accessKeyword, fieldTarget)`. Same `BuildNode` arm handles both.

### ActionChain: The Loop-Until-EndOfAction Case

As fixed in § 5 (Outcome boundary bug):

```csharp
while (Current().Kind == TokenKind.Arrow)
{
    // Peek before consuming — if outcome keyword follows '->', preserve '->' for ParseOutcome
    if (IsOutcomeKeyword(Peek(1).Kind))
        break;

    Advance(); // consume '->'
    // ... parse action statement
}
```

From the generic iterator's perspective, `ActionChain` is one slot call that returns either
an `ActionChainNode` (if at least one action was parsed) or null (if the first `->` saw an
outcome keyword immediately). The slot is optional (`IsRequired = false`), so null is valid.

The generic iterator doesn't know about the loop, the `->` separator, or the outcome keyword
termination. It calls `_slotParsers[ActionChain]()` once and gets a result. The slot parser
owns all of that internal complexity.

**The hard sub-case: TransitionRow with no actions (`-> transition State` immediately)**

Source: `from Draft on Submit -> transition Approved`

- ActionChain slot: `ParseActionChain()` → sees `->`, peeks `transition` → breaks immediately
  → `actions.Count == 0` → returns null ✅ (slot is optional)
- Outcome slot: `ParseOutcome()` → sees `->` (still in stream) → `Advance()` → sees `transition`
  → `ParseTransitionOutcome()` ✅

The peek-before-consume fix handles this correctly.

### Outcome: The Three-Form Sub-Grammar

`ParseOutcome()` is called as a slot parser. It owns:
1. `-> transition <StateTarget>` — consume `->`, consume `transition`, parse state target
2. `-> no transition` — consume `->`, consume `no`, consume `transition`
3. `-> reject <StringExpr>` — consume `->`, consume `reject`, parse string expression

The generic iterator calls `_slotParsers[Outcome]()` once. All three forms are handled inside
`ParseOutcome()`. The iterator does not see the sub-grammar — it receives an `OutcomeNode`
(or null if the outcome parse fails, which triggers the required-slot diagnostic).

The `->` is always consumed as the introduction token inside `ParseOutcome()`. The ActionChain
fix ensures `->` is never pre-consumed when an outcome keyword follows.

### When Guard: Frank Says Uniform — I Confirm, With One Note

The `when` guard IS handled generically by the disambiguator — **but only for the pre-disambiguation
position**. For TransitionRow, the guard can also appear in the post-EventTarget position
(inside slot iteration). Both positions are handled correctly (see § 8 analysis above).

The generic disambiguator's guard handling:
```csharp
// Step 2: Consume optional guard (uniform for ALL scoped constructs at this position)
Expression? guard = null;
if (Current().Kind == TokenKind.When)
{
    Advance(); // consume 'when'
    guard = ParseExpression(0);
    // ParseExpression stops at the next disambiguation token (Arrow, Ensure, On)
    // because those tokens have no binding power in the Pratt expression grammar.
}
```

This works because disambiguation tokens (`Ensure`, `->`, `On`) have zero binding power in
the Pratt grammar. The expression parser stops naturally before them. No per-construct
termination tokens needed.

---

## Revised Estimate

My estimate in the cross-review was L/2.5–3 weeks. The corrections in this document add:

| Item | Addendum |
|------|---------|
| `LeadingTokenSlot` on DisambiguationEntry + CreateLeadingTokenSlotNode | +4h |
| ActionChain peek-before-consume fix + tests | +2h |
| Documenting slot-parser introduction-token contract | +2h |
| Pre-parsed slot map robustness (anchor/guard injection) | +4h |
| `PrimaryLeadingToken` bridge + consumer migration in LS/MCP | +6h |

**Total addendum: ~18h ≈ 2.5 days.**

**Updated estimate: 3–3.5 weeks** for a complete, correct, fully-catalog-driven parser
on a clean-slate build. This is still L, not XL. The corrections are correctness hardening,
not architectural rework.

The 1-week Option 1 (A+B+C+E only, no Layer D) is unchanged — these corrections don't affect
the vocabulary tables or disambiguation metadata layers.

---

## For Frank

These are the decisions I need you to make or validate in Round 3. Be specific — I'm not
asking for confirmation, I'm asking for your position.

### Decision F1: `LeadingTokenSlot` on DisambiguationEntry

I've identified a genuine bug: `write all` parsing fails because `Write` is consumed as the
dispatch token but is also the `AccessModeKeyword` slot value. My fix adds
`LeadingTokenSlot: ConstructSlotKind?` to `DisambiguationEntry`.

**Do you accept this shape?** If you prefer a different mechanism (e.g., a sentinel-based
approach, or splitting AccessMode's root-level case into a separate hand-written parse method
outside the generic loop), say so explicitly. The bug is real — the question is which fix shape.

### Decision F2: `BuildNode` switch vs factory dictionary

I maintain the exhaustive switch with named local index constants. You proposed the dictionary
for testability. We need one answer.

**My position is firm:** CS8509 enforces completeness; the factory alignment test handles the
slot-ordering risk; the switch is centralized and greppable. The dictionary trades a compile-time
invariant for a runtime test without adding any debugging value.

**If you still prefer the dictionary:** Tell me exactly what you gain that the switch cannot
provide, with specific reference to a scenario the switch fails and the dictionary handles.

### Decision F3: ActionChain/Outcome boundary — is my peek-before-consume fix correct?

The bug: ActionChain consumes `->` then checks for outcome keywords. Outcome also expects `->`.
They fight. My fix: peek before consuming in ActionChain.

**Verify this against the actual language grammar.** The parser.md sample:
```
-> set reviewer = approver -> transition Submitted
```
ActionChain should parse `set reviewer = approver`, then peek at `Peek(1)` and see `transition`
→ break. `ParseOutcome()` then sees `->` and consumes it correctly. Does this hold for the
`-> no transition` and `-> reject "msg"` forms as well?

### Decision F4: Two-position `when` guard in TransitionRow

I claim both `from X when expr on Y -> ...` and `from X on Y when expr -> ...` are valid forms
that the generic disambiguator handles correctly. The first uses the pre-disambiguation guard
path; the second uses the slot-iteration guard path. Both inject into slot[2].

**Confirm:** Is `from X when expr on Y -> ...` actually valid Precept syntax? The parser.md
tables list `When` as a re-check option for all preposition methods, but the samples only
show the post-EventTarget form. If the pre-disambiguation form is NOT valid, the generic
disambiguator should NOT consume a `when` guard before the disambiguation token — or at least,
it should only do so for constructs that declare a GuardClause slot.

### Decision F5: `DisambiguationTokens` derivable from slot vocabulary?

You asked whether `[Write, Read, Omit]` in AccessMode's `In` entry could be derived from the
`AccessModeKeyword` slot's known vocabulary instead of declared separately. I analyzed this
and concluded: not worth the added field. But you should confirm — you own the catalog shape.

If you want derivation, the mechanism is `ConstructSlot.IntroductionTokens: ImmutableArray<TokenKind>?`
(new field on ConstructSlot). This is explicitly what you avoided in v1. If you accept derivation,
re-open that field. If you don't, I'll declare the `[Write, Read, Omit]` tokens explicitly in
the catalog and document their correspondence with the slot vocabulary.

### Decision F6: Migration PR sequence — catalog change before or alongside parser?

My PR sequence puts the `DisambiguationEntry` catalog shape change FIRST (PR 1), before any
parser work. This is a breaking change for LS and MCP consumers of `ConstructMeta.LeadingToken`.
The `PrimaryLeadingToken` bridge property mitigates this.

**Do you accept the bridge property approach?** Or do you want a harder cutover where consumers
migrate in the same PR as the catalog change? The bridge approach is safer for incremental work;
the cutover approach is cleaner for the catalog's public shape.
