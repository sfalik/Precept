# Catalog-Driven Parser: Full Vision Design (Round 4)

**By:** George (Runtime Dev)
**Date:** 2026-04-27
**Status:** Design session Round 4 — concrete artifacts, correctness contracts, feasibility spikes
**References:**
- `docs/working/catalog-parser-design-v3.md` — Frank's Round 3 (superseded by this document)
- `docs/archive/language-design/precept-language-vision.md` — language surface source of truth (archived)
- `docs/language/precept-language-spec.md` — DSL spec (law)
- `docs/language/catalog-system.md` — metadata-driven architecture
- `docs/philosophy.md` — product identity
- `src/Precept/Language/Constructs.cs` — actual catalog source
- `src/Precept/Language/Construct.cs` — ConstructMeta record shape
- `src/Precept/Language/ConstructSlot.cs` — ConstructSlot/ConstructSlotKind
- `src/Precept/Pipeline/Parser.cs` — parser stub (throws NotImplementedException)

**This document supersedes v1, v2, and v3.** It is the living design document for the catalog-driven parser.

---

## 1. Doc-Reading Summary

I have read the following documents in full:

1. **`docs/archive/language-design/precept-language-vision.md`** — the future-state language surface (archived).
2. **`docs/language/precept-language-spec.md`** — the DSL spec (v2).
3. **`docs/language/catalog-system.md`** — metadata-driven catalog architecture.
4. **`docs/philosophy.md`** — product identity and positioning.
5. **Sample files:** `loan-application.precept`, `insurance-claim.precept`, `customer-profile.precept`, `computed-tax-net.precept`.

Here are the specific facts I found that affect the parser design:

**From the language vision:**
- `->` serves dual duty: (a) computed field introduction (`field Tax as number -> Expr`) and (b) action chain / outcome separator in transition rows and stateless event hooks. This is an explicit design choice per the spec: "The `->` arrow is deliberately overloaded to create a visual pipeline."
- The guard keyword `when` appears in three construct families: rules (`rule Expr when Guard because ...`), state/event ensures (`in State when Guard ensure ...`), and transition rows (`from State on Event when Guard -> ...`). Access modes also support guarded write: `in State when Guard write Field`.
- Computed fields use `field Name as Type [modifiers] -> Expr`. The `->` comes after modifiers, before computed expression. Multi-name declarations cannot have computed expressions.
- The preposition discipline is strict: `in` = while in state, `to` = entering, `from` = leaving, `on` = event fires.
- Spec § 2.2 says: "All three [in/to/from] support an optional `when` guard between the state target and the verb (except `from ... on`, where the guard is inside the transition row after the event name)."
- State actions (`to/from State -> actions`) also support `when` guard between state target and action chain.

**From the spec (law):**
- Transition row grammar: `from StateTarget on Identifier ("when" BoolExpr)? ("->" ActionStatement)* "->" Outcome` — guard is AFTER event target.
- There is NO explicit grammar rule for `from StateTarget when BoolExpr on Identifier ...` (pre-event guard position) in the spec.
- `Parser.cs` is a stub: `public static SyntaxTree Parse(TokenStream tokens) => throw new NotImplementedException();` — confirmed zero consumers.

**From catalog-system.md:**
- 12 catalogs form a closed registry. Constructs catalog covers grammar forms.
- The decision framework: if it's language surface → catalog it. If members have varying shapes → DU.
- Enforcement is via CS8509 exhaustive switch.

**From the catalog source (`Constructs.cs`):**
- Current `ConstructMeta` shape: `(Kind, Name, Description, UsageExample, AllowedIn, Slots, LeadingToken, SnippetTemplate?)`.
- `LeadingToken` is a single `TokenKind` — not `DisambiguationEntry[]`. The migration hasn't happened yet.
- 11 constructs, 15 `ConstructSlotKind` values, 15 shared slot instances.
- StateEnsure's `LeadingToken` is `TokenKind.In` — but it can also be led by `To` or `From`. This is the first concrete evidence that single `LeadingToken` is insufficient.
- AccessMode's `LeadingToken` is `TokenKind.Write` — but it can also be led by `In`. Same problem.

**From philosophy.md:**
- Prevention, not detection. Deterministic semantics. Full inspectability.
- No facts that contradict the parser design direction.

---

## 2. Tensions with Frank's v3

### TENSION 1: `from State when expr on Event` — Not in the Spec

Frank's Decision F4 accepts `from X when expr on Y -> ...` as valid syntax (pre-disambiguation guard position). The spec explicitly says: "All three support an optional `when` guard between the state target and the verb (**except `from ... on`, where the guard is inside the transition row after the event name**)."

The spec grammar for transition row is:
```
from StateTarget on Identifier ("when" BoolExpr)?
```

There is no `when` between `StateTarget` and `on`. Frank accepted this as a syntactic convenience handled "for free" by the generic disambiguator. I agree the disambiguator handles it mechanically. But:

1. **The spec says it's not valid.** The parenthetical exception is explicit.
2. **No sample file uses the pre-event guard position.** All 28 samples with guards in transition rows use `from State on Event when Guard`.
3. **The language vision's core declaration inventory shows only one form:** `from <State> on <Event> ... -> ...`.

**My position:** Accepting the pre-disambiguation guard position is a language surface expansion. It may be the RIGHT expansion (it reads naturally in English), but it's a language decision, not a parser implementation decision. It requires:
- Explicit spec update to show both positions as valid.
- At least one sample file demonstrating the alternative form.
- Shane sign-off on the language surface change.

**For Round 4, I will write the sample file (Task 3) to demonstrate both positions, but I flag this as requiring Shane approval before the parser implements it.** The disambiguator design should SUPPORT both positions (it costs nothing), but the spec must authorize them.

### TENSION 2: None Others Found

The rest of Frank's v3 is consistent with the docs. The architectural decisions (vocabulary tables from catalogs, generic slot iteration, `BuildNode` switch, `DisambiguationEntry` shape) all align with the catalog-system.md § Architectural Identity and the spec's parser section. No other contradictions.

---

## 3. Task 1: Slot-Ordering Drift Test for Pre-Parsed Injection

### The Problem

In `ParseConstructBodyWithPreParsedSlots`, the disambiguator injects:
- The pre-parsed **anchor** (state target or event target) at slot index 0.
- The pre-parsed **guard** (if `when` was consumed before disambiguation) at the first `GuardClause` slot.

Both use hardcoded positional assumptions. If a construct's slot sequence changes in the catalog — e.g., someone reorders `TransitionRow`'s slots from `[StateTarget, EventTarget, GuardClause, ActionChain, Outcome]` to `[EventTarget, StateTarget, GuardClause, ActionChain, Outcome]` — the injected anchor lands in the wrong slot.

The `BuildNode` switch arms use named index constants (`const int StateTarget = 0`), so they would catch a mismatch between the slot array and the AST node constructor at runtime. But the pre-parsed injection path has no equivalent safety net — it silently puts the right node in the wrong position.

### The Design

A compile-time test that validates the injection contract against the catalog:

```csharp
public class SlotOrderingDriftTests
{
    /// <summary>
    /// Validates that every construct reachable through pre-parsed injection
    /// has its anchor slot at index 0 and its guard slot (if present) at the
    /// expected position. If the catalog slot sequence changes, this test fails.
    /// </summary>
    [Fact]
    public void PreParsedInjection_AnchorSlotIsAlwaysAtIndex0()
    {
        // Every construct that enters through a scoped preposition
        // (In, To, From, On) has its anchor target parsed before
        // disambiguation. The disambiguator injects it at index 0.
        var scopedConstructs = new[]
        {
            ConstructKind.TransitionRow,
            ConstructKind.StateEnsure,
            ConstructKind.StateAction,
            ConstructKind.EventEnsure,
            ConstructKind.EventHandler,
            ConstructKind.AccessMode,
        };

        foreach (var kind in scopedConstructs)
        {
            var meta = Constructs.GetMeta(kind);
            meta.Slots[0].Kind.Should().BeOneOf(
                ConstructSlotKind.StateTarget,
                ConstructSlotKind.EventTarget,
                $"Construct {kind}: pre-parsed injection assumes anchor is at slot 0, " +
                $"but slot 0 is {meta.Slots[0].Kind}");
        }
    }

    [Fact]
    public void PreParsedInjection_GuardSlotPositionMatchesExpectation()
    {
        // The disambiguator injects pre-parsed guards at the first
        // GuardClause slot. This test validates that the guard slot
        // index hasn't drifted from the expected position.
        var constructsWithGuards = new (ConstructKind Kind, int ExpectedGuardIndex)[]
        {
            (ConstructKind.TransitionRow, 2),
            (ConstructKind.StateEnsure, -1),   // no guard slot on StateEnsure itself
            (ConstructKind.AccessMode, -1),     // no guard slot on AccessMode
        };

        foreach (var (kind, expectedIndex) in constructsWithGuards)
        {
            var meta = Constructs.GetMeta(kind);
            var actualIndex = meta.Slots
                .Select((s, i) => (s, i))
                .Where(t => t.s.Kind == ConstructSlotKind.GuardClause)
                .Select(t => t.i)
                .DefaultIfEmpty(-1)
                .First();

            actualIndex.Should().Be(expectedIndex,
                $"Construct {kind}: pre-parsed guard injection expects GuardClause at index {expectedIndex}, " +
                $"but found it at {actualIndex}");
        }
    }

    [Fact]
    public void PreParsedInjection_OnlyRecognizedConstructsUseInjectionPath()
    {
        // Safety net: if a new construct is added that shares a leading
        // token with an existing scoped construct, it must be added to
        // the injection-aware test above. This test catches the gap.
        var injectionLeadingTokens = new HashSet<TokenKind>
        {
            TokenKind.In, TokenKind.To, TokenKind.From, TokenKind.On
        };

        foreach (var meta in Constructs.All)
        {
            if (!injectionLeadingTokens.Contains(meta.LeadingToken))
                continue;

            // Every construct led by a scoped preposition MUST have
            // its anchor at slot 0
            meta.Slots[0].Kind.Should().BeOneOf(
                ConstructSlotKind.StateTarget,
                ConstructSlotKind.EventTarget,
                $"New construct {meta.Kind} uses scoped preposition {meta.LeadingToken} " +
                $"but doesn't have an anchor target at slot 0. " +
                $"Add it to the pre-parsed injection tests.");
        }
    }
}
```

### Why This Shape

1. **`AnchorSlotIsAlwaysAtIndex0`** — the hardest invariant. If someone reorders slots, this fails immediately with a message that explains the injection contract.

2. **`GuardSlotPositionMatchesExpectation`** — uses an explicit expected-index table. When a construct's slot sequence changes, the developer must consciously update this table AND the injection code. If they update one without the other, the test catches it.

3. **`OnlyRecognizedConstructsUseInjectionPath`** — a forward-looking safety net. When a new construct is added that enters through `In/To/From/On`, it must conform to the anchor-at-index-0 contract. This test makes the contract discoverable.

### The Complementary Invariant

The `BuildNode` named constants and these tests together form a complete ordering safety net:

| What can drift | What catches it |
|----------------|-----------------|
| `BuildNode` reads slots in wrong order | Named index constants + compiler errors on type mismatch |
| Pre-parsed injection writes slots at wrong index | `SlotOrderingDriftTests` above |
| New construct added without injection awareness | `OnlyRecognizedConstructsUseInjectionPath` |

---

## 4. Task 2: `_slotParsers` Exhaustiveness Contract

### The Problem

`_slotParsers` is a `FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>>`. Every `ConstructSlotKind` that appears in ANY construct's slot sequence must have an entry. Currently this is an implicit invariant — if someone adds a new slot kind and puts it in a construct's `Slots` but forgets to add a parser, the dictionary lookup throws `KeyNotFoundException` at parse time.

### The Design

A test, not an analyzer. Analyzers are for catalog metadata correctness; this is a parser infrastructure contract.

```csharp
public class SlotParserExhaustivenessTests
{
    [Fact]
    public void EverySlotKindUsedInAnyConstructHasASlotParser()
    {
        // Extract every unique ConstructSlotKind used in any construct's
        // slot sequence across the entire catalog.
        var usedSlotKinds = Constructs.All
            .SelectMany(meta => meta.Slots)
            .Select(slot => slot.Kind)
            .Distinct()
            .ToHashSet();

        // The slot parser registry must cover all of them.
        // We cannot access _slotParsers directly (it's a private field
        // on the parser instance), so we validate against the known
        // inventory of slot parser method names.
        var registeredSlotKinds = Parser.GetRegisteredSlotKinds();

        var missing = usedSlotKinds.Except(registeredSlotKinds).ToList();

        missing.Should().BeEmpty(
            "Every ConstructSlotKind used in any construct's slot sequence " +
            "must have a registered slot parser. Missing: {0}",
            string.Join(", ", missing));
    }

    [Fact]
    public void NoOrphanSlotParsers()
    {
        // Inverse check: every registered slot parser should correspond
        // to a ConstructSlotKind actually used by at least one construct.
        // An orphan parser means dead code or a forward-declaration for
        // a construct that doesn't exist yet.
        var usedSlotKinds = Constructs.All
            .SelectMany(meta => meta.Slots)
            .Select(slot => slot.Kind)
            .Distinct()
            .ToHashSet();

        var registeredSlotKinds = Parser.GetRegisteredSlotKinds();

        var orphans = registeredSlotKinds.Except(usedSlotKinds).ToList();

        orphans.Should().BeEmpty(
            "Slot parser registered for {0} but no construct uses this slot kind. " +
            "Remove the dead parser or add the construct that needs it.",
            string.Join(", ", orphans));
    }
}
```

### The `GetRegisteredSlotKinds` Surface

The parser needs to expose its slot parser registry keys for testability. Options:

**Option A: Internal static method (preferred).**
```csharp
// In Parser.cs
internal static IReadOnlySet<ConstructSlotKind> GetRegisteredSlotKinds()
    => _slotParserKeys; // pre-computed frozen set

// Test project uses [InternalsVisibleTo]
```

**Option B: Derive from naming convention.**
Use reflection to find all `Parse{SlotKindName}` methods on the parser and map them back to `ConstructSlotKind` members. Fragile and indirect — I don't recommend it.

**Option C: Test-time construction.**
Instantiate the parser in test code and extract the dictionary keys. This requires the parser to be constructible in test context, which it should be (vocabulary tables from catalogs, no runtime dependencies).

**I recommend Option C** because the parser is already stateless (it takes `TokenStream` and returns `SyntaxTree`). When the parser is refactored from a static class to an instance class with vocabulary tables injected at construction, the test can instantiate it and read the dictionary directly. Until then, Option A (internal static method) is the pragmatic bridge.

### The Exhaustiveness Guarantee

| What can go wrong | What catches it |
|-------------------|-----------------|
| New `ConstructSlotKind` added to a construct's `Slots` without a parser | `EverySlotKindUsedInAnyConstructHasASlotParser` fails |
| Slot parser registered but no construct uses it | `NoOrphanSlotParsers` fails |
| Typo in dictionary key | CS8509 exhaustive switch on `ConstructSlotKind` (if we use a switch instead of dictionary initializer) |

### Alternative: Switch Instead of Dictionary

If `_slotParsers` were implemented as a switch instead of a dictionary:

```csharp
private SyntaxNode? InvokeSlotParser(ConstructSlotKind kind) => kind switch
{
    ConstructSlotKind.IdentifierList    => ParseIdentifierList(),
    ConstructSlotKind.TypeExpression    => ParseTypeExpression(),
    // ... every member
    _ => throw new ArgumentOutOfRangeException(nameof(kind))
};
```

CS8509 would enforce exhaustiveness at compile time. But Frank's settled decision (F2) is dictionary for `_slotParsers` — it's a registry pattern, not an exhaustive invariant. The test is the correct enforcement mechanism for a dictionary.

---

## 5. Task 3: `when` Guard Both-Positions Sample

### The Tension (Restated)

The spec grammar for `from ... on ...` places the guard AFTER the event target:
```
from StateTarget on Identifier ("when" BoolExpr)?
```

Frank's Decision F4 accepts an alternative position BEFORE `on`:
```
from StateTarget when BoolExpr on Identifier -> ...
```

The spec's parenthetical exception ("except `from ... on`, where the guard is inside the transition row after the event name") explicitly excludes the pre-event position.

**I will write the sample showing both positions, but flag it as requiring a spec update and Shane approval.**

### The Sample

```precept
precept GuardPositionDemo

# Demonstrates both guard positions in transition rows.
# Position 1 (pre-event): from State when Guard on Event -> ...
# Position 2 (post-event, canonical): from State on Event when Guard -> ...
# Both produce identical AST nodes with the guard in the GuardClause slot.

field Amount as number default 0 nonnegative
field Verified as boolean default false
field Reviewer as string optional

state Draft initial
state Submitted
state Approved terminal

event Submit(Value as number)
on Submit ensure Submit.Value > 0 because "Submit value must be positive"

event Verify
event Approve(Note as string notempty)

# Post-event guard (canonical form, per spec):
from Draft on Submit when Amount == 0
    -> set Amount = Submit.Value
    -> transition Submitted

# Pre-event guard (alternative form, per Decision F4):
# This reads as: "from Submitted, when Verified, on Approve..."
from Submitted when Verified on Approve
    -> set Reviewer = Approve.Note
    -> transition Approved

# Post-event guard (canonical) — same construct, different row:
from Submitted on Verify when not Verified
    -> set Verified = true
    -> no transition

# Rejection row without guard:
from Submitted on Approve
    -> reject "Not verified"
```

### Verification Requirements

The acceptance test for this sample must verify:

1. **Both positions parse to the same AST shape.** The `TransitionRow` node for `from Submitted when Verified on Approve -> ...` must have its `GuardClause` slot populated identically to `from Draft on Submit when Amount == 0 -> ...`.

2. **The guard expression is semantically identical.** The guard's expression tree, span, and evaluation result must match regardless of lexical position.

3. **The disambiguation tokens are correct.** For the pre-event guard row: after consuming `From`, the disambiguator parses `StateTarget` (`Submitted`), then sees `when` — it consumes the guard expression, then sees `on` — disambiguation confirms `TransitionRow`. The guard is injected at the `GuardClause` slot index.

4. **Error case: guard without following `on`.** `from Submitted when Verified -> set ...` — this looks like a guarded `StateAction`, NOT a `TransitionRow`. The disambiguator must correctly route this to `StateAction` because the disambiguation token after the guard is `Arrow`, not `On`.

### Status

**This sample requires Shane approval to become a canonical syntax test.** If Shane rejects the pre-event guard position, the sample becomes a negative test (parser rejects it with a diagnostic). The disambiguator design supports both outcomes — the `when` consumption is a 4-line conditional that can be removed or gated.

---

## 6. Task 4: Complete `GetMeta` with `Entries`

### The `DisambiguationEntry` Record

```csharp
/// <summary>
/// One disambiguation path for a construct led by a shared token.
/// Constructs with unique leading tokens have a single entry with
/// no disambiguation tokens. Constructs sharing a leading token
/// (In, To, From, On) have multiple entries, each with disambiguation
/// tokens that resolve the ambiguity after the anchor target is parsed.
/// </summary>
public sealed record DisambiguationEntry(
    TokenKind                      LeadingToken,
    ImmutableArray<TokenKind>?     DisambiguationTokens = null,
    /// <summary>
    /// When the leading token also occupies a slot value (not merely a
    /// dispatch signal), identifies which slot kind it fills. The generic
    /// slot iterator injects a synthetic node for this slot rather than
    /// calling the slot parser fresh. Null when the leading token is
    /// purely a dispatch signal.
    /// </summary>
    ConstructSlotKind?             LeadingTokenSlot = null);
```

### The Updated `ConstructMeta` Shape

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
    /// <summary>Slot sequence for this construct's declaration shape.</summary>
    public IReadOnlyList<ConstructSlot> Slots { get; } = Slots;

    /// <summary>
    /// Backward-compatibility bridge: returns the primary leading token
    /// for consumers that only need single-token dispatch (LS completions,
    /// MCP vocabulary, TextMate grammar). Equivalent to <c>Entries[0].LeadingToken</c>.
    /// </summary>
    /// <remarks>
    /// Temporary — remove when all consumers migrate to <see cref="Entries"/>.
    /// </remarks>
    public TokenKind PrimaryLeadingToken => Entries[0].LeadingToken;

    /// <summary>
    /// Backward-compatibility alias for <see cref="PrimaryLeadingToken"/>.
    /// Preserved for existing LS and MCP consumers during migration.
    /// </summary>
    [Obsolete("Use Entries or PrimaryLeadingToken instead")]
    public TokenKind LeadingToken => PrimaryLeadingToken;
}
```

### Complete `GetMeta` with `Entries`

```csharp
public static ConstructMeta GetMeta(ConstructKind kind) => kind switch
{
    // ════════════════════════════════════════════════════════════════
    //  Unique leading token — single entry, no disambiguation
    // ════════════════════════════════════════════════════════════════

    ConstructKind.PreceptHeader => new(
        kind,
        "precept header",
        "File-level header that names the precept",
        "precept LoanApplication",
        [],
        [SlotIdentifierList],
        [new DisambiguationEntry(TokenKind.Precept)]),

    ConstructKind.FieldDeclaration => new(
        kind,
        "field declaration",
        "Declares one or more typed fields with optional modifiers and a computed expression",
        "field amount as money nonnegative",
        [],
        [SlotIdentifierList, SlotTypeExpression, SlotModifierList, SlotComputeExpression],
        [new DisambiguationEntry(TokenKind.Field)]),

    ConstructKind.StateDeclaration => new(
        kind,
        "state declaration",
        "Declares one or more lifecycle states with optional state modifiers",
        "state Draft initial, Submitted, Approved terminal success",
        [],
        [SlotIdentifierList, SlotStateModifierList],
        [new DisambiguationEntry(TokenKind.State)]),

    ConstructKind.EventDeclaration => new(
        kind,
        "event declaration",
        "Declares one or more named events with optional arguments and the initial modifier",
        "event Submit(approver as string)",
        [],
        [SlotIdentifierList, SlotArgumentList],
        [new DisambiguationEntry(TokenKind.Event)]),

    ConstructKind.RuleDeclaration => new(
        kind,
        "rule declaration",
        "Declares a data-truth constraint with a guard and reason",
        "rule amount > 0 because \"Amount must be positive\"",
        [],
        [SlotGuardClause, SlotBecauseClause],
        [new DisambiguationEntry(TokenKind.Rule)]),

    // ════════════════════════════════════════════════════════════════
    //  Shared leading token — multiple entries with disambiguation
    // ════════════════════════════════════════════════════════════════

    ConstructKind.TransitionRow => new(
        kind,
        "transition row",
        "State-to-state transition with guard, actions, and outcome",
        "from Draft on Submit -> set reviewer = approver -> transition Submitted",
        [],
        [SlotStateTarget, SlotEventTarget, SlotGuardClause, SlotActionChain, SlotOutcome],
        [new DisambiguationEntry(TokenKind.From, [TokenKind.On])]),

    ConstructKind.StateEnsure => new(
        kind,
        "state ensure",
        "State-scoped constraint that must hold on entry, exit, or while in a state",
        "in Approved ensure amount > 0 because \"Approved amount must be positive\"",
        [ConstructKind.StateDeclaration],
        [SlotStateTarget, SlotEnsureClause],
        [
            new DisambiguationEntry(TokenKind.In,   [TokenKind.Ensure]),
            new DisambiguationEntry(TokenKind.To,   [TokenKind.Ensure]),
            new DisambiguationEntry(TokenKind.From, [TokenKind.Ensure]),
        ]),

    ConstructKind.AccessMode => new(
        kind,
        "access mode",
        "Declares field write access per state via 'in' scope (write, read, or omit), or 'write all' at root level for stateless precepts",
        "in Draft write Amount",
        [],
        [SlotOptStateTarget, SlotAccessModeKeyword, SlotFieldTarget],
        [
            new DisambiguationEntry(TokenKind.Write, LeadingTokenSlot: ConstructSlotKind.AccessModeKeyword),
            new DisambiguationEntry(TokenKind.In, [TokenKind.Write, TokenKind.Read, TokenKind.Omit]),
        ]),

    ConstructKind.StateAction => new(
        kind,
        "state action",
        "Entry or exit hook that fires actions when entering or leaving a state",
        "to Submitted -> set submittedAt = now()",
        [ConstructKind.StateDeclaration],
        [SlotStateTarget, SlotActionChain],
        [
            new DisambiguationEntry(TokenKind.To,   [TokenKind.Arrow]),
            new DisambiguationEntry(TokenKind.From, [TokenKind.Arrow]),
        ]),

    ConstructKind.EventEnsure => new(
        kind,
        "event ensure",
        "Event-scoped constraint that must hold when an event fires",
        "on Submit ensure reviewer != \"\" because \"Reviewer required\"",
        [ConstructKind.EventDeclaration],
        [SlotEventTarget, SlotEnsureClause],
        [new DisambiguationEntry(TokenKind.On, [TokenKind.Ensure])]),

    ConstructKind.EventHandler => new(
        kind,
        "event handler",
        "Event handler with actions but no state transitions (stateless precepts)",
        "on UpdateName -> set name = newName",
        [],
        [SlotEventTarget, SlotActionChain],
        [new DisambiguationEntry(TokenKind.On, [TokenKind.Arrow])]),

    _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
        $"Unknown ConstructKind: {kind}"),
};
```

### Issues Surfaced

**Issue 1: `In`-led `when` guard for AccessMode.**
The spec shows: `in StateTarget ("when" BoolExpr)? (write|read|omit) FieldTarget`. The disambiguation tokens for `AccessMode` via `In` are `[Write, Read, Omit]`. But if there's a `when` guard between the state target and the access mode keyword, the disambiguator sees `When` after the state target — not `Write/Read/Omit`. The disambiguator must consume the optional `when` guard BEFORE checking disambiguation tokens. This is exactly the same mechanism as the pre-event guard in TransitionRow.

**The generic disambiguator flow must be:**
1. Consume leading token (preposition).
2. Parse anchor target (state or event name).
3. If `Current() == TokenKind.When`: consume guard expression, stash it for injection.
4. Check `Current()` against each candidate's `DisambiguationTokens`.
5. Route to the matched construct.
6. Inject stashed anchor and guard into the appropriate slots.

This is consistent with Frank's v3 design, but the AccessMode case makes it non-optional. The spec already authorizes guarded access modes: `in UnderReview when DocumentsVerified write DecisionNote` appears in `loan-application.precept` (line 28). The disambiguator MUST handle `when` before disambiguation for AccessMode to work. This is not a language surface expansion — it's required by the existing spec.

**Issue 2: AccessMode via `Write` — the `LeadingTokenSlot` path.**
When `write all` or `write FieldName` appears at root level (stateless precepts), `Write` is consumed as the leading token. The `LeadingTokenSlot: ConstructSlotKind.AccessModeKeyword` correctly injects `Write` as the access mode keyword. But the `OptStateTarget` slot (index 0) must be null — there's no state target in root-level access modes. The generic slot iterator must handle this: when `OptStateTarget.IsRequired == false` and we're on the `Write`-led path, slot 0 stays null. This works because `ParseStateTarget()` will return null when it doesn't see a state name (it sees the field target or `all` instead).

**Issue 3: EventHandler vs EventEnsure disambiguation after `when`.**
`on Event when Guard ensure ...` vs `on Event when Guard -> ...`. After consuming `On`, event target, and guard, the disambiguator checks: `Ensure` → EventEnsure, `Arrow` → EventHandler. This works. But `on Event when Guard` followed by a newline (missing both `ensure` and `->`) must emit a diagnostic. The disambiguator's fallback path needs a "no disambiguation token matched" diagnostic.

**Issue 4: `From`-led three-way disambiguation.**
`From` can lead to TransitionRow, StateEnsure, or StateAction. After consuming `From`, state target, and optional guard:
- `On` → TransitionRow
- `Ensure` → StateEnsure
- `Arrow` → StateAction

The `DisambiguationTokens` for each are `[On]`, `[Ensure]`, `[Arrow]` respectively. The disambiguator iterates each candidate's tokens until one matches. This is a linear scan over at most 3 candidates — trivial performance cost.

But there's an edge case: `from State when Guard on Event` — the guard appears before `On`. If guard consumption happens at step 3 (before disambiguation), then step 4 sees `On` and correctly routes to TransitionRow with the guard stashed. The guard then gets injected at slot index 2 (GuardClause). **This confirms that pre-disambiguation guard consumption is not optional — it's required for the generic disambiguator to work with the existing spec-legal `in State when Guard write Field` syntax.**

### The Derived Index

From the `Entries` table, two derived lookup structures:

```csharp
/// <summary>
/// Leading-token → candidate constructs. Used by the dispatch loop.
/// Built once at parser construction time.
/// </summary>
public static IReadOnlyDictionary<TokenKind, ImmutableArray<(ConstructKind Kind, DisambiguationEntry Entry)>>
    ByLeadingToken { get; } = Constructs.All
        .SelectMany(meta => meta.Entries.Select(entry => (meta.Kind, entry)))
        .GroupBy(t => t.entry.LeadingToken)
        .ToFrozenDictionary(
            g => g.Key,
            g => g.Select(t => (t.Kind, t.entry)).ToImmutableArray());

/// <summary>
/// All unique leading tokens — the sync-point set for error recovery.
/// </summary>
public static IReadOnlySet<TokenKind> LeadingTokens { get; } =
    ByLeadingToken.Keys.ToFrozenSet();
```

### Validation Test for Entries

```csharp
[Fact]
public void AllConstructsHaveAtLeastOneEntry()
{
    foreach (var meta in Constructs.All)
    {
        meta.Entries.Should().NotBeEmpty(
            $"Construct {meta.Kind} must have at least one DisambiguationEntry");

        // Every entry's LeadingToken must be a keyword token
        foreach (var entry in meta.Entries)
        {
            Tokens.GetMeta(entry.LeadingToken).Category.Should().Be(
                TokenCategory.Keyword,
                $"DisambiguationEntry for {meta.Kind} has non-keyword leading token {entry.LeadingToken}");
        }
    }
}

[Fact]
public void LeadingTokenSlot_OnlyUsedWhenLeadingTokenIsAlsoSlotContent()
{
    foreach (var meta in Constructs.All)
    {
        foreach (var entry in meta.Entries)
        {
            if (entry.LeadingTokenSlot is null) continue;

            // The LeadingTokenSlot must reference a slot kind that
            // actually exists in this construct's slot sequence.
            meta.Slots.Select(s => s.Kind).Should().Contain(entry.LeadingTokenSlot.Value,
                $"Construct {meta.Kind}: LeadingTokenSlot {entry.LeadingTokenSlot} " +
                $"does not match any slot in the construct's slot sequence");
        }
    }
}
```

---

## 7. Task 5: Concrete Slot Parser Signatures

### The Problem

Frank specified `Func<SyntaxNode?>` as the slot parser value type in `_slotParsers`. Each concrete slot parser returns a specific node type (`ActionChainNode?`, `OutcomeNode?`, `Expression?`). The question: does boxing/covariance matter, and what's the right dictionary value type?

### Analysis of the Three Hardest Slot Parsers

#### `ParseActionChain` → `ActionChainNode?`

```csharp
private ActionChainNode? ParseActionChain()
{
    if (Current().Kind != TokenKind.Arrow) return null;
    // ... peek-before-consume loop from Frank's v3 ...
    return actions.Count > 0
        ? new ActionChainNode(span, actions.ToImmutable())
        : null;
}
```

**Return type:** `ActionChainNode?`. This is a sealed record that extends `SyntaxNode`.

**Boxing:** `ActionChainNode` is a reference type (sealed record class). Covariant return from `ActionChainNode?` to `SyntaxNode?` is a reference conversion — **no boxing occurs.** Boxing only applies to value types. All AST nodes are reference types (records), so boxing is never a concern for any slot parser.

#### `ParseOutcome` → `OutcomeNode?`

```csharp
private OutcomeNode? ParseOutcome()
{
    if (Current().Kind != TokenKind.Arrow) return null;
    Advance(); // consume ->

    return Current().Kind switch
    {
        TokenKind.Transition => ParseTransitionOutcome(),
        TokenKind.No         => ParseNoTransitionOutcome(),
        TokenKind.Reject     => ParseRejectOutcome(),
        _                    => null // diagnostic emitted by caller
    };
}
```

**Return type:** `OutcomeNode?` where `OutcomeNode` is an abstract record with three sealed subtypes (`TransitionOutcomeNode`, `NoTransitionOutcomeNode`, `RejectOutcomeNode`).

**How it becomes `SyntaxNode?`:** Same as above — reference type, covariant conversion, no boxing.

#### `ParseExpression(0)` → `Expression?`

```csharp
// The Pratt expression parser signature
private Expression? ParseExpression(int minBindingPower)
```

**The slot parser wrapper:**
```csharp
// In _slotParsers initialization:
[ConstructSlotKind.ComputeExpression] = () => ParseComputeExpression(),
[ConstructSlotKind.GuardClause]       = () => ParseGuardClause(),
[ConstructSlotKind.EnsureClause]      = () => ParseEnsureClause(),

// Each wrapper consumes its introduction token, then calls ParseExpression(0)
private Expression? ParseComputeExpression()
{
    if (Current().Kind != TokenKind.Arrow) return null;
    Advance(); // consume ->
    return ParseExpression(0);
}

private Expression? ParseGuardClause()
{
    if (Current().Kind != TokenKind.When) return null;
    Advance(); // consume when
    return ParseExpression(0);
}

private SyntaxNode? ParseEnsureClause()
{
    if (Current().Kind != TokenKind.Ensure) return null;
    Advance(); // consume ensure
    var expr = ParseExpression(0);
    // BecauseClause is a separate slot, so we stop here
    return expr;
}
```

**Return type:** `Expression?` where `Expression` is the abstract base for all expression nodes. Covariant to `SyntaxNode?` via reference conversion.

### The Concrete Dictionary Value Type

```csharp
private readonly FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>> _slotParsers;
```

**`Func<SyntaxNode?>` is correct.** Here's why:

1. **No boxing.** All AST nodes are reference types. The covariant conversion from `ActionChainNode?` → `SyntaxNode?` is a reference widening, not a box.

2. **No generic constraint needed.** `Func<out TResult>` is already covariant in `TResult`. A `Func<ActionChainNode?>` is directly assignable to `Func<SyntaxNode?>` without wrapping.

   ```csharp
   // This compiles directly:
   Func<SyntaxNode?> parser = ParseActionChain; // covariant delegate conversion
   ```

3. **No `where T : SyntaxNode` constraint needed.** The dictionary stores `Func<SyntaxNode?>` and delegate covariance handles the rest. Adding a generic constraint would require a generic dictionary or a wrapper, both unnecessarily complex.

4. **The `BuildNode` switch recasts to concrete types anyway.** When `BuildNode` reads `slots[3]` and casts to `(Expression?)slots[Compute]`, the cast is a downcast from `SyntaxNode?` to `Expression?`. This is the same pattern as any typed AST consumer. The cast is safe because the slot parser for `ComputeExpression` always returns `Expression?` or null.

### The Complete `_slotParsers` Initialization

```csharp
private FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>> BuildSlotParsers()
    => new Dictionary<ConstructSlotKind, Func<SyntaxNode?>>
    {
        [ConstructSlotKind.IdentifierList]     = ParseIdentifierList,
        [ConstructSlotKind.TypeExpression]     = ParseTypeExpression,
        [ConstructSlotKind.ModifierList]       = ParseModifierList,
        [ConstructSlotKind.StateModifierList]  = ParseStateModifierList,
        [ConstructSlotKind.ArgumentList]       = ParseArgumentList,
        [ConstructSlotKind.ComputeExpression]  = ParseComputeExpression,
        [ConstructSlotKind.GuardClause]        = ParseGuardClause,
        [ConstructSlotKind.ActionChain]        = ParseActionChain,
        [ConstructSlotKind.Outcome]            = ParseOutcome,
        [ConstructSlotKind.StateTarget]        = ParseStateTarget,
        [ConstructSlotKind.EventTarget]        = ParseEventTarget,
        [ConstructSlotKind.EnsureClause]       = ParseEnsureClause,
        [ConstructSlotKind.BecauseClause]      = ParseBecauseClause,
        [ConstructSlotKind.AccessModeKeyword]  = ParseAccessModeKeyword,
        [ConstructSlotKind.FieldTarget]        = ParseFieldTarget,
    }.ToFrozenDictionary();
```

15 entries, one per `ConstructSlotKind`. The delegate covariance is invisible — each method naturally returns its specific node type, and the dictionary stores the widened delegate.

---

## 8. Task 6: Roslyn Source Generator Feasibility Spike

### What the Generator Would Read

**Input metadata shape:**

The generator reads `ConstructMeta` entries at compile time. It needs:

1. **`ConstructKind` enum** — one generated record per member.
2. **`Constructs.GetMeta(kind).Slots`** — slot sequence determines constructor parameters.
3. **`ConstructSlot.Kind`** — maps to a C# property name and type.
4. **`ConstructSlot.IsRequired`** — determines nullability (`T` vs `T?`).
5. **`DisambiguationEntry[]`** — not needed for AST node generation, but needed for `BuildNode` generation.

**The hard mapping: `ConstructSlotKind` → (C# type, property name).**

This is irreducible domain knowledge. The generator needs a mapping table:

| `ConstructSlotKind` | C# Type | Property Name |
|---------------------|---------|---------------|
| `IdentifierList` | `ImmutableArray<string>` | `Names` |
| `TypeExpression` | `TypeRefNode` | `Type` |
| `ModifierList` | `ImmutableArray<ModifierMeta>` | `Modifiers` |
| `StateModifierList` | `ImmutableArray<ModifierMeta>` | `StateModifiers` |
| `ArgumentList` | `ImmutableArray<ArgumentNode>` | `Arguments` |
| `ComputeExpression` | `Expression` | `ComputedValue` |
| `GuardClause` | `Expression` | `Guard` |
| `ActionChain` | `ActionChainNode` | `Actions` |
| `Outcome` | `OutcomeNode` | `Outcome` |
| `StateTarget` | `StateTargetNode` | `StateTarget` |
| `EventTarget` | `EventTargetNode` | `EventTarget` |
| `EnsureClause` | `EnsureClauseNode` | `Ensure` |
| `BecauseClause` | `BecauseClauseNode` | `Because` |
| `AccessModeKeyword` | `TokenKind` | `AccessMode` |
| `FieldTarget` | `FieldTargetNode` | `FieldTarget` |

This table could live as:
- **Attribute-based markers on `ConstructSlotKind`** (recommended for generator consumption):
  ```csharp
  public enum ConstructSlotKind
  {
      [SlotMapping(typeof(ImmutableArray<string>), "Names")]
      IdentifierList,
      // ...
  }
  ```
- **A static dictionary in a helper class** (simpler but not generator-friendly).
- **Convention-based inference** (fragile — `IdentifierList` → `Names` is not derivable).

### What the Generator Would Emit

**Per construct:**

```csharp
// Generated from Constructs.GetMeta(ConstructKind.FieldDeclaration)
// Slots: IdentifierList(req), TypeExpression(req), ModifierList(opt), ComputeExpression(opt)
[GeneratedCode("Precept.Generators", "1.0")]
public sealed record FieldDeclarationNode(
    SourceSpan Span,
    ImmutableArray<string> Names,
    TypeRefNode Type,
    ImmutableArray<ModifierMeta>? Modifiers,
    Expression? ComputedValue)
    : Declaration(Span);
```

**The `BuildNode` switch arm:**

```csharp
// Generated
ConstructKind.FieldDeclaration => new FieldDeclarationNode(
    span,
    (ImmutableArray<string>)slots[0]!,    // IdentifierList (required)
    (TypeRefNode)slots[1]!,               // TypeExpression (required)
    slots[2] as ImmutableArray<ModifierMeta>?,  // ModifierList (optional)
    slots[3] as Expression?),                    // ComputeExpression (optional)
```

**Visitor base class:**

```csharp
// Generated
public abstract class SyntaxVisitor
{
    public virtual void Visit(SyntaxNode node) => node switch
    {
        FieldDeclarationNode n => VisitFieldDeclaration(n),
        StateDeclarationNode n => VisitStateDeclaration(n),
        // ... 11 arms
        _ => VisitUnknown(node)
    };

    protected virtual void VisitFieldDeclaration(FieldDeclarationNode node) { }
    protected virtual void VisitStateDeclaration(StateDeclarationNode node) { }
    // ...
}
```

### Feasibility Verdict

**The current catalog shape is 80% generator-ready.** The missing 20% is the `ConstructSlotKind` → (type, name) mapping. Without it, the generator cannot determine property names or types from the slot kind alone.

**Two approaches to close the gap:**

**Approach A: Attribute markers on `ConstructSlotKind` (recommended).**
Add `[SlotMapping(Type, Name)]` attributes. The generator reads these at compile time via the Roslyn symbol model. This is standard incremental generator practice. Cost: ~15 attribute decorations + ~20 lines of generator infrastructure to read them.

**Approach B: Convention table in the generator project.**
Hardcode the mapping in the generator itself. This is simpler but creates a parallel copy of domain knowledge — directly violating the catalog system's "derive, never duplicate" principle. I reject this approach.

### Test Generation Feasibility (Shane's Addition)

Shane asked specifically about using the generator for **test scaffolding**. Here's what the catalog can support:

#### Round-Trip Parse Tests (text → AST → text)

**What the catalog provides:** `ConstructMeta.UsageExample` — one valid source string per construct.

**What the generator could emit:**
```csharp
[Theory]
[InlineData("precept LoanApplication")]           // from PreceptHeader.UsageExample
[InlineData("field amount as money nonnegative")]  // from FieldDeclaration.UsageExample
// ... one per construct
public void RoundTrip_ParseAndRender(string source)
{
    var tree = Parser.Parse(Lexer.Lex(source));
    tree.Diagnostics.Should().BeEmpty();
    tree.Render().Should().Be(source);
}
```

**Gap:** `UsageExample` is a single string. Real round-trip coverage needs multiple examples per construct (with/without optional slots, with guards, with modifiers, etc.). The catalog would need:

```csharp
// Proposed addition to ConstructMeta or as a parallel test-data catalog:
public ImmutableArray<string>? TestExamples { get; init; }
```

This is **test metadata**, not language metadata. I would put it on `ConstructMeta` as an optional field that the generator reads but core runtime ignores. Alternatively, a separate test-data registry that maps `ConstructKind` → example strings — this keeps test data out of production catalogs.

**My recommendation:** Separate test-data registry. Test examples are not part of "a complete description of Precept" — they don't belong in the language definition catalogs per the catalog-system.md completeness principle.

#### Slot Coverage Tests (positive + missing per required slot)

**What the catalog provides:** `ConstructMeta.Slots` with `IsRequired` flags.

**What the generator could emit:**
```csharp
// For each required slot, generate a test that omits it
[Fact]
public void FieldDeclaration_MissingTypeExpression_EmitsDiagnostic()
{
    // FieldDeclaration slots: IdentifierList(req), TypeExpression(req), ModifierList(opt), ComputeExpression(opt)
    // Omitting slot 1 (TypeExpression):
    var source = "field amount";  // missing "as number"
    var tree = Parser.Parse(Lexer.Lex(source));
    tree.Diagnostics.Should().ContainSingle(d =>
        d.Code == DiagnosticCode.ExpectedSlot);
}
```

**Gap:** The generator needs to know how to produce source text with a specific slot omitted. This requires:
1. A parseable base example (from `UsageExample`).
2. Knowledge of which tokens in that example correspond to which slot.

This is the hard part. The slot-to-token mapping requires either:
- A `ConstructSlot.ExampleTokenRange` field (span within `UsageExample`), or
- A source-decomposition step that parses `UsageExample` and maps AST nodes back to source ranges.

The second approach is self-bootstrapping (parse the example, then delete one slot's AST node worth of tokens), but requires a working parser first. **Slot coverage test generation is only feasible after the parser is functional** — it's a second-phase generator, not a bootstrapping aid.

#### Error Recovery Tests from Sync Points

**What the catalog provides:** `Constructs.LeadingTokens` — the sync set.

**What the generator could emit:**
```csharp
[Theory]
[MemberData(nameof(LeadingTokenPairs))]
public void ErrorRecovery_ResyncToNextDeclaration(string garbage, string validDecl)
{
    var source = $"{garbage}\n{validDecl}";
    var tree = Parser.Parse(Lexer.Lex(source));
    tree.Diagnostics.Should().NotBeEmpty();  // garbage causes diagnostics
    tree.Declarations.Should().ContainSingle();  // valid decl still parsed
}

public static IEnumerable<object[]> LeadingTokenPairs()
{
    foreach (var meta in Constructs.All)
        yield return new object[] { "@@@ invalid syntax @@@", meta.UsageExample };
}
```

**This is fully achievable with current catalog metadata.** The leading tokens set and usage examples are sufficient. No additional metadata needed.

### Generator Infrastructure Cost

| Component | Effort |
|-----------|--------|
| `Precept.Generators` project setup | 4h |
| Incremental generator pipeline | 8h |
| `SlotMapping` attributes + reading | 4h |
| AST node emission | 8h |
| `BuildNode` switch emission | 4h |
| Visitor base class emission | 4h |
| Test-data registry + round-trip test emission | 8h |
| Slot coverage test emission (phase 2) | 12h |
| Error recovery test emission | 4h |
| **Total** | **~56h** |

**Break-even for AST generation alone: ~25-30 constructs** (Frank's estimate confirmed).
**Break-even for test generation: immediately** — the test generator pays for itself at 11 constructs because it eliminates a category of manual test authoring.

**Recommendation:** If source generation is pursued, lead with test generation (round-trip + error recovery), not AST node generation. The test generator is cost-justified now; the AST generator is not yet.

---

## 9. The `->` vs `<-` Question

### Current State of `->` in the Grammar

`->` (TokenKind.Arrow) appears in three contexts:

| Context | Example | Role |
|---------|---------|------|
| Computed field expression | `field Tax as number -> Subtotal * TaxRate` | Introduces the computation |
| Action chain step | `-> set reviewer = approver` | Introduces each action |
| Outcome | `-> transition Approved` | Introduces the outcome |

### Is There Ambiguity?

**No.** The three contexts are structurally unambiguous at parse time:

1. **Computed field `->`.** Occurs inside `field` declarations, after modifiers and before an expression. The parser is inside `ParseFieldDeclaration()` or the generic slot iterator has reached the `ComputeExpression` slot. The slot parser `ParseComputeExpression()` checks for `Arrow` as its introduction token. There is no context where a `field` declaration's `->` could be confused with an action chain `->`.

2. **Action chain `->`.** Occurs after a state target + event target (in TransitionRow), after a state target (in StateAction), or after an event target (in EventHandler). The parser is inside the `ActionChain` slot parser. There is no context overlap with field declarations.

3. **Outcome `->`.** Occurs after the last action in a chain (or directly after the event target if no actions). Distinguished from action `->` by peeking at the next token (outcome keyword vs. action keyword). Frank's v3 peek-before-consume fix handles this correctly.

**The disambiguation is entirely positional/contextual.** `->` in a field declaration is unambiguous because you're parsing a field. `->` in a transition row is unambiguous because you're parsing a transition. There is no token-level ambiguity.

### Would `<-` Help?

**Analysis from five angles:**

**1. Token disambiguation.** `->` is already unambiguous in context. `<-` would eliminate a theoretical ambiguity that doesn't exist in practice. **No benefit.**

**2. Slot parsing simplicity.** `ParseComputeExpression()` checks for `TokenKind.Arrow`. If computed fields used `<-`, it would check for `TokenKind.LeftArrow` (new token). The slot parser body is identical — one introduction-token check, one `Advance()`, one `ParseExpression(0)`. **No simplification.**

**3. DisambiguationEntry table impact.** `->` does not appear in any `DisambiguationTokens` array. The disambiguation table routes on `On`, `Ensure`, `Arrow` (for StateAction/EventHandler), and `Write/Read/Omit` (for AccessMode). Changing computed field's `->` to `<-` would not affect the disambiguation table at all — `Arrow` in the table refers to action chains, not computed fields. **No impact.**

**4. ConstructMeta.Slots shape.** The `ComputeExpression` slot is already clean — it's an optional slot with an expression value. Whether its introduction token is `->` or `<-` doesn't change the slot's metadata shape. **No impact.**

**5. Lexer complexity.** `<-` requires lexer-level handling. The `<` character is already the start of `<`, `<=`, and `<>` (if we had it). Adding `<-` means the lexer must check for `-` after `<` before deciding whether it's `LessThan` or `LeftArrow`. This is the same scan-priority pattern as `->` (check for `>` after `-`), so it's mechanically straightforward. But it adds one more multi-character operator to the scan priority table. **Small cost, not zero.**

### Semantic Consideration

`->` in computed fields reads as "produces" or "derives to":
```
field Tax as number -> Subtotal * TaxRate
```
"Tax is a number that → Subtotal times TaxRate."

`<-` would read as "computed from" or "derived from":
```
field Tax as number <- Subtotal * TaxRate
```
"Tax is a number that ← Subtotal times TaxRate."

Both are readable. `<-` has a slight semantic advantage: it flows from the dependency toward the dependent field, matching the data flow direction. `->` flows from the field name toward the expression, matching the declaration reading direction.

The language vision doc says: "Computed fields remain part of the language." It uses `->` in the grammar rule: `field <Name> as <Type> -> <Expr>`. All sample files use `->`. The spec uses `->`.

### Verdict: NEUTRAL, Leaning No

`<-` does not help the parser design. It solves no disambiguation problem (there is none), simplifies no slot parsing, affects no catalog metadata shapes, and impacts no entries in the disambiguation table. It adds one multi-character token to the lexer and requires a spec/vision/sample update.

The only argument for `<-` is semantic — "computed from" is marginally more intuitive than "derives to." But Precept's existing `->` convention is consistent: the arrow always points toward the outcome. In `-> set x = 1`, the arrow points toward the action. In `-> transition Approved`, the arrow points toward the outcome. In `field Tax -> Expr`, the arrow points toward the expression. Switching to `<-` for computed fields breaks this consistency — the arrow would point in different directions depending on context.

**If Shane wants `<-` for semantic reasons, it's a clean swap with no parser-design cost or benefit. But it should not be motivated by disambiguation — there's nothing to disambiguate.**

---

## 10. For Frank — Round 5

### What I've Delivered

1. **Slot-ordering drift tests** (Task 1) — three tests covering anchor-at-index-0, guard slot position, and new-construct forward safety.

2. **`_slotParsers` exhaustiveness contract** (Task 2) — test design with two strategies for accessing the dictionary keys, recommendation for Option C (test-time construction).

3. **Both-positions guard sample** (Task 3) — with explicit flag that pre-event guard in TransitionRow requires spec update + Shane approval.

4. **Complete `GetMeta` with `Entries`** (Task 4) — all 11 constructs, all disambiguation paths, the `DisambiguationEntry` record, the updated `ConstructMeta` shape with `PrimaryLeadingToken` bridge and `[Obsolete]` `LeadingToken` alias, plus derived lookup indexes.

5. **Concrete slot parser signatures** (Task 5) — resolved: `Func<SyntaxNode?>` is correct, no boxing occurs (all AST nodes are reference types), delegate covariance handles the widening, complete `_slotParsers` initialization with all 15 entries.

6. **Source generator feasibility spike** (Task 6) — generator-ready at 80%, the missing piece is `ConstructSlotKind` → (type, name) mapping. Shane's test-generation extension is cost-justified NOW (especially round-trip and error recovery tests); AST generation breaks even at 25-30 constructs.

### Push On These in Round 5

**P1: The `when` consumption timing.**
I surfaced Issue 1 (§6): the generic disambiguator MUST consume `when` guards before checking disambiguation tokens, because `in State when Guard write Field` is already spec-legal. This is not a language expansion — it's a requirement. The disambiguator flow I described (consume leading token → parse anchor → consume optional `when` → check disambiguation tokens) is the correct order. Verify this matches your intended implementation shape and confirm there are no edge cases I missed.

**P2: The `EnsureClause` slot parser and `BecauseClause` coupling.**
In the current slot design, `EnsureClause` and `BecauseClause` are separate slots. But `ensure Expr because "msg"` is a tightly coupled pair — the `because` is mandatory whenever `ensure` appears. Two options:
- **Keep separate slots** — `ParseEnsureClause()` returns only the expression, `ParseBecauseClause()` returns only the reason string. Clean separation, but the parser can't enforce the `because` requirement at the slot level.
- **Merge into one slot** — `ParseEnsureWithReason()` returns a compound node with both expression and reason. Tighter coupling, but the slot parser can enforce the mandatory `because`.

The current catalog has them as separate slots. I recommend keeping them separate — the type checker already enforces mandatory `because` (it's a semantic rule, not a grammar rule). The slot parsers should be minimal grammar producers; semantic enforcement belongs downstream. But Frank should confirm.

**P3: The `RuleDeclaration` slot sequence is different from other guard-bearing constructs.**
`RuleDeclaration` has `[GuardClause, BecauseClause]` — the guard IS the rule expression, and `BecauseClause` is the reason. But the `GuardClause` slot kind is shared with transition row guards and state/event ensure guards. In a rule, the "guard" is actually the rule body (`rule amount > 0 because ...`); the `when` guard is an optional sub-part of the rule expression.

Wait — looking more carefully at the spec: `rule BoolExpr ("when" BoolExpr)? because StringExpr`. The `BoolExpr` is the rule expression, and the optional `when BoolExpr` is the guard. The current slot sequence `[GuardClause, BecauseClause]` maps `GuardClause` to the rule expression. But `GuardClause` in every other construct means "optional `when` condition." This is a naming mismatch — the rule body isn't a guard clause, it's the rule expression.

**I flag this for your review.** Either:
- The `RuleDeclaration` slot for the rule expression should be a different `ConstructSlotKind` (e.g., `RuleExpression`), or
- The `GuardClause` naming must be documented as "the primary boolean expression" regardless of whether it's a guard or a rule body.

The current naming will confuse implementers. A `ParseGuardClause()` method that expects `when` as its introduction token will fail for `RuleDeclaration` where the expression starts immediately without `when`.

**P4: `AccessMode` via `In` — `when` guard creates a three-token disambiguator path.**
`in State when Guard write Field` — after consuming `In`, state target, and the `when` guard, the disambiguator sees `Write`. But `in State when Guard ensure Expr because ...` also starts with `In`, state target, and a `when` guard — then sees `Ensure`. The disambiguator handles this fine (it just checks the next token after the guard). But document this edge case in the spec — an `In`-led construct with a guard must disambiguate on the token AFTER the guard, not the token after the state target.

**P5: PR 1 scope.**
I believe PR 1 (catalog shape migration) should include:
1. The `DisambiguationEntry` record definition.
2. The updated `ConstructMeta` shape with `Entries`, `PrimaryLeadingToken`, and `[Obsolete] LeadingToken`.
3. The complete `GetMeta` rewrite with all `Entries` populated.
4. The `ByLeadingToken` and `LeadingTokens` derived indexes.
5. The slot-ordering drift tests (Task 1).
6. The `_slotParsers` exhaustiveness test (Task 2 — test skeleton; the actual dictionary doesn't exist yet).
7. Migration of LS/MCP consumers from `LeadingToken` to `PrimaryLeadingToken` (or they can stay on `[Obsolete] LeadingToken` temporarily).

PR 2+ is the parser implementation itself.

Confirm this scope is right, or flag anything that should be deferred.

---

## Appendix A: Consolidated Decision Table

| ID | Decision | Resolution | Source |
|----|----------|-----------|--------|
| F1 | `LeadingTokenSlot` on `DisambiguationEntry` | Accepted (v3) | George v2 fix |
| F2 | `BuildNode` switch vs factory dict | Exhaustive switch wins (v3) | George v2 argument |
| F3 | ActionChain peek-before-consume | Accepted (v3) | George v2 fix |
| F4 | Two-position `when` guard | Accepted by Frank, flagged by George as requiring spec update + Shane approval | v3 F4, v4 Tension 1 |
| F5 | `DisambiguationTokens` derivation | Reject derivation — declare explicitly (v3) | Frank v3 |
| F6 | Migration PR sequence | Accepted with bridge (v3) | George v2 proposal |
| G1 | Pre-parsed injection must consume `when` before disambiguation | Required (not optional) — `in State when Guard write Field` is spec-legal | v4 Issue 1 |
| G2 | `Func<SyntaxNode?>` slot parser type | Confirmed correct — no boxing, delegate covariance handles widening | v4 Task 5 |
| G3 | Source generator: lead with test gen, not AST gen | Test gen cost-justified now; AST gen breaks even at 25-30 constructs | v4 Task 6 |
| G4 | `->` vs `<-` for computed fields | Neutral — no parser design impact; keep `->` for consistency unless Shane directs otherwise | v4 §9 |
| G5 | `RuleDeclaration` slot kind naming | Flagged — `GuardClause` is a misnomer for the rule body expression; needs resolution | v4 P3 |

## Appendix B: `ConstructSlotKind` → Slot Parser Cross-Reference

| Slot Kind | Introduction Token | Parser Method | Return Type |
|-----------|--------------------|---------------|-------------|
| `IdentifierList` | `Identifier` | `ParseIdentifierList` | `IdentifierListNode?` |
| `TypeExpression` | `As` | `ParseTypeExpression` | `TypeRefNode?` |
| `ModifierList` | (modifier keyword) | `ParseModifierList` | `ModifierListNode?` |
| `StateModifierList` | (state modifier keyword) | `ParseStateModifierList` | `ModifierListNode?` |
| `ArgumentList` | `LeftParen` | `ParseArgumentList` | `ArgumentListNode?` |
| `ComputeExpression` | `Arrow` | `ParseComputeExpression` | `Expression?` |
| `GuardClause` | `When` | `ParseGuardClause` | `Expression?` |
| `ActionChain` | `Arrow` | `ParseActionChain` | `ActionChainNode?` |
| `Outcome` | `Arrow` | `ParseOutcome` | `OutcomeNode?` |
| `StateTarget` | `Identifier`/`Any` | `ParseStateTarget` | `StateTargetNode?` |
| `EventTarget` | `Identifier` | `ParseEventTarget` | `EventTargetNode?` |
| `EnsureClause` | `Ensure` | `ParseEnsureClause` | `Expression?` |
| `BecauseClause` | `Because` | `ParseBecauseClause` | `BecauseClauseNode?` |
| `AccessModeKeyword` | `Write`/`Read`/`Omit` | `ParseAccessModeKeyword` | `TokenValueNode?` |
| `FieldTarget` | `Identifier`/`All` | `ParseFieldTarget` | `FieldTargetNode?` |

**Note:** `ComputeExpression` and `ActionChain` share `Arrow` as their introduction token. This is fine — they never appear in the same construct. `ComputeExpression` is a slot in `FieldDeclaration`; `ActionChain` is a slot in `TransitionRow`, `StateAction`, and `EventHandler`. The `_slotParsers` dictionary dispatches by slot kind, not by introduction token.

**Second note:** `Outcome` also uses `Arrow` as introduction. It follows `ActionChain` in `TransitionRow`. The peek-before-consume pattern (Frank's v3 accepted fix) ensures `ActionChain` leaves the `->` unconsumed when an outcome keyword follows.
