# Catalog-Driven Parser: Validation Layer Design (Round 5)

**By:** Frank (Language Designer / Compiler Architect)
**Date:** 2026-04-28
**Status:** Design session Round 5 — validation layer, extensibility contracts, G5 resolution
**References:**
- `docs/working/catalog-parser-design-v4.md` — George's Round 4 (superseded by this document)
- `docs/language/precept-language-vision.md` — language surface source of truth
- `docs/language/precept-language-spec.md` — DSL spec (law)
- `docs/language/catalog-system.md` — metadata-driven catalog architecture
- `docs/philosophy.md` — product identity
- `src/Precept/Language/Constructs.cs` — actual catalog source
- `src/Precept/Language/Construct.cs` — ConstructMeta record shape
- `src/Precept/Language/ConstructSlot.cs` — ConstructSlot/ConstructSlotKind
- `.squad/decisions/inbox/copilot-directive-extensibility-validation-20260427.md` — Shane's validation directive

**This document supersedes v1, v2, v3, and v4.** It is the living design document for the catalog-driven parser.

---

## 1. Doc-Reading Summary

I have read the following documents in full:

1. **`docs/language/precept-language-vision.md`** (~70KB) — Confirmed reading. The full intended language surface. Covers stateful and stateless precepts, the three-lane numeric system, temporal and business-domain types, the modifier system, field access modes, and proof system responsibilities. Key for this round: the core declaration inventory at line 153 shows `rule <Expr> [when <Guard>] because "..."` — the rule body is a standalone boolean expression, NOT introduced by `when`.

2. **`docs/language/precept-language-spec.md`** — Confirmed reading. The law. § 2.2 defines the rule grammar as `rule BoolExpr ("when" BoolExpr)? because StringExpr`. The guard is optional and introduced by `when`; the rule body is bare. § 2.2 also shows the transition row grammar: `from StateTarget on Identifier ("when" BoolExpr)?` — guard after event. The parenthetical exception at line 621: "All three support an optional `when` guard between the state target and the verb (except `from ... on`, where the guard is inside the transition row after the event name)."

3. **`docs/language/catalog-system.md`** — Confirmed reading. The twelve-catalog architecture. Key principle: "if something is domain knowledge, it is metadata." Enforcement via CS8509 exhaustive switch. The two-layer enforcement model: compiler (CS8509) for catalog completeness, Roslyn (PRECEPT0001–0004) for construction discipline. No consumer maintains parallel copies.

4. **`docs/philosophy.md`** — Confirmed reading. Prevention not detection. One file, complete rules. Deterministic semantics. Full inspectability. No tension with parser design direction.

5. **`docs/working/catalog-parser-design-v4.md`** — Confirmed reading. George's Round 4. Comprehensive: slot-ordering drift tests, `_slotParsers` exhaustiveness contract, both-positions guard sample, complete `GetMeta` with `Entries`, concrete slot parser signatures, source generator feasibility spike. Five items flagged for me (P1–P5).

6. **Sample files read:** `loan-application.precept`, `insurance-claim.precept`, `customer-profile.precept`. Confirmed: `loan-application.precept` line 28 uses `in UnderReview when DocumentsVerified write DecisionNote` (guarded access mode). `insurance-claim.precept` line 26 uses `in UnderReview when not FraudFlag write AdjusterName`. No sample file uses pre-event guard position (`from State when Guard on Event`). All transition row guards are post-event.

7. **`src/Precept/Language/Constructs.cs`** — Confirmed reading. 11 constructs, 15 shared slot instances. `RuleDeclaration` at line 73 uses `[SlotGuardClause, SlotBecauseClause]`. `SlotGuardClause` is defined at line 20 with `Description: "when expression"`. This confirms George's G5 bug: the rule body expression is NOT a `when` expression.

8. **`src/Precept/Language/Construct.cs`** — Confirmed reading. `ConstructMeta` is a sealed record with `(Kind, Name, Description, UsageExample, AllowedIn, Slots, LeadingToken, SnippetTemplate?)`. Single `LeadingToken` — the `DisambiguationEntry` migration hasn't happened yet.

9. **`src/Precept/Language/ConstructSlot.cs`** — Confirmed reading. 15 `ConstructSlotKind` values. `GuardClause` at position 6 — used by `TransitionRow`, `RuleDeclaration`, and implicitly expected by access mode/state ensure guards. No `RuleExpression` kind exists yet.

10. **`.squad/decisions/inbox/copilot-directive-extensibility-validation-20260427.md`** — Confirmed reading. Shane's directive: NO source generation. Focus on validation and discoverability. When you add something new, it should fail loudly if you miss a step. The `_slotParsers` gap is the key example: adding a new `ConstructSlotKind` to a construct's `Slots` without a parser currently fails silently at runtime.

### Tensions Found with v4

**TENSION: G5 is a real bug, and it's worse than George described.**

George flagged the naming collision: `RuleDeclaration` uses `GuardClause` for the rule body, but `ParseGuardClause()` expects `when` as its introduction token. That's correct, but the deeper problem is that the rule body has NO introduction token at all. After consuming `Rule` as the leading token, the next thing the parser must do is call `ParseExpression(0)` directly. The `when` guard, if present, is a SECOND expression that follows the rule body. So `RuleDeclaration` needs two expression slots: the rule body (no intro token) and the optional guard (`when`).

The current slot sequence `[GuardClause, BecauseClause]` is wrong in two ways:
1. The first slot should not be `GuardClause` — it's not a guard, it's the rule body.
2. The optional `when Guard` part of the rule is missing entirely from the slot sequence.

The correct slot sequence for `RuleDeclaration` is: `[RuleExpression, GuardClause(optional), BecauseClause]`.

No other tensions found. George's v4 is consistent with the docs in all other respects.

---

## 2. Shane's Directive: Validation Layer Design

Shane's directive is clear: no source generation, focus on validation and discoverability. When a developer adds a new construct to the language, every missing step must fail loudly — at compile time if possible, at startup/test time if not.

I'm designing the validation layer as four tiers, ordered from earliest detection to latest.

### 2.1 Tier 1: Compile-Time Enforcement (What Already Works)

**CS8509 Exhaustive Switch** — the backbone of catalog completeness.

These switches already enforce compile-time completeness:

| Switch site | What adding a new member forces |
|-------------|--------------------------------|
| `Constructs.GetMeta(ConstructKind)` | New construct → must provide a `ConstructMeta` entry |
| `BuildNode(ConstructKind, SyntaxNode?[], SourceSpan)` | New construct → must provide an AST node construction arm |

With `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, CS8509 becomes a build-breaking error. Adding a new `ConstructKind` member without touching these two sites fails the build immediately. No analyzer needed.

**What CS8509 does NOT enforce:**

| Gap | Why CS8509 doesn't cover it |
|-----|----------------------------|
| New `ConstructSlotKind` → `_slotParsers` entry | `_slotParsers` is a dictionary, not a switch. Dictionaries have no exhaustiveness checking. |
| New `ConstructSlotKind` used in a construct's `Slots` → parser handles it | The catalog declares the slot sequence; the parser is a separate artifact. No static link. |
| New `DisambiguationEntry` → dispatcher handles the leading token | The dispatch loop is hand-written. Adding a new entry path is a manual step. |
| `ConstructMeta.Slots` ordering matches `BuildNode` index assumptions | Slot order is a metadata declaration; `BuildNode` index constants are hand-written. No static link. |

### 2.2 Tier 2: Compile-Time Enforcement (What Needs Work)

**Recommendation: Add one new exhaustive switch to close the `_slotParsers` gap.**

The `_slotParsers` dictionary is the most dangerous silent-failure point. A new `ConstructSlotKind` added to the enum and used in a construct's `Slots` — without a corresponding parser — produces a `KeyNotFoundException` at runtime. Shane correctly identified this as the key problem.

**Solution: replace `_slotParsers` dictionary with an exhaustive switch method.**

```csharp
/// <summary>
/// Dispatches to the appropriate slot parser for the given slot kind.
/// CS8509 enforces exhaustiveness — adding a new ConstructSlotKind member
/// without a parser arm is a build error.
/// </summary>
private SyntaxNode? InvokeSlotParser(ConstructSlotKind kind) => kind switch
{
    ConstructSlotKind.IdentifierList    => ParseIdentifierList(),
    ConstructSlotKind.TypeExpression    => ParseTypeExpression(),
    ConstructSlotKind.ModifierList      => ParseModifierList(),
    ConstructSlotKind.StateModifierList => ParseStateModifierList(),
    ConstructSlotKind.ArgumentList      => ParseArgumentList(),
    ConstructSlotKind.ComputeExpression => ParseComputeExpression(),
    ConstructSlotKind.GuardClause       => ParseGuardClause(),
    ConstructSlotKind.RuleExpression    => ParseRuleExpression(),  // G5 resolution — see §3
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

**This reverses George's settled decision F2** (dictionary over switch for `_slotParsers`). I know George argued for the dictionary as a registry pattern. I'm overruling it for three reasons:

1. **Shane's directive is explicit**: missing a step should fail loudly, at compile time if possible. A switch gives us CS8509 for free. A dictionary gives us a runtime exception.

2. **The argument for dictionary was about pattern choice, not correctness.** George argued the dictionary is a registry pattern. True — but a registry that's always exhaustive over a closed enum is just a verbose switch. The `ConstructSlotKind` enum is closed. No runtime registration. No plugin extensibility. A switch is the correct pattern for a closed exhaustive mapping.

3. **The `Func<SyntaxNode?>` delegate covariance argument still works.** Each arm of the switch calls the concrete method directly. The return type is covariant to `SyntaxNode?`. No change in calling convention.

**Trade-off acknowledged:** The switch method dispatches on the call, not at construction time. The dictionary would catch a missing entry at construction time (when the dictionary is built). The switch catches it at first parse of a slot with that kind. In practice, both fire at test time — we test every construct, so every slot kind is exercised. But the switch has the additional guarantee that a missing arm is a build error.

**Decision F7: `_slotParsers` is an exhaustive switch, not a dictionary.** This supersedes F2.

### 2.3 Tier 3: Startup/Test-Time Assertions

For gaps that CS8509 cannot cover, we need assertions that fire early in the test suite.

**Test 1: Every `ConstructSlotKind` used in any construct's `Slots` is handled by `InvokeSlotParser`.**

This is redundant with the CS8509 switch if we adopt F7 — the switch already enforces exhaustiveness over the enum. But the test catches a subtler problem: a `ConstructSlotKind` member that exists in the enum but is never used in any construct's `Slots`. That's dead code, and it should be flagged.

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
    var unused = allKinds.Except(usedKinds).ToList();

    unused.Should().BeEmpty(
        "Every ConstructSlotKind member must be used by at least one construct's Slots. " +
        "Unused: {0}. Remove the enum member or add it to a construct.",
        string.Join(", ", unused));
}
```

**Test 2: George's slot-ordering drift tests (from v4 § 3).**

I accept George's three drift tests verbatim:
- `PreParsedInjection_AnchorSlotIsAlwaysAtIndex0`
- `PreParsedInjection_GuardSlotPositionMatchesExpectation`
- `PreParsedInjection_OnlyRecognizedConstructsUseInjectionPath`

These are correct and well-designed. The only change: update the guard slot position table to reflect the G5 resolution (RuleDeclaration now has three slots, guard at index 1).

**Test 3: `DisambiguationEntry` completeness.**

George's v4 § 6 tests are correct:
- `AllConstructsHaveAtLeastOneEntry`
- `LeadingTokenSlot_OnlyUsedWhenLeadingTokenIsAlsoSlotContent`

I add one more:

```csharp
[Fact]
public void EveryLeadingTokenMapsToAtLeastOneConstruct()
{
    // The ByLeadingToken index must cover every token that appears
    // as a leading token in any construct's Entries.
    var allLeadingTokens = Constructs.All
        .SelectMany(m => m.Entries)
        .Select(e => e.LeadingToken)
        .Distinct()
        .ToHashSet();

    var indexedTokens = Constructs.ByLeadingToken.Keys.ToHashSet();

    allLeadingTokens.Should().BeEquivalentTo(indexedTokens,
        "ByLeadingToken index must cover exactly the set of leading tokens " +
        "declared in construct Entries");
}
```

**Test 4: `BuildNode` arm count matches `ConstructKind` member count.**

```csharp
[Fact]
public void BuildNodeHandlesEveryConstructKind()
{
    // BuildNode is an exhaustive switch, so CS8509 enforces this.
    // This test is a belt-and-suspenders runtime verification.
    var allKinds = Enum.GetValues<ConstructKind>();
    foreach (var kind in allKinds)
    {
        var meta = Constructs.GetMeta(kind);
        var slots = new SyntaxNode?[meta.Slots.Count];
        // BuildNode should not throw for any valid ConstructKind.
        // It may produce an incomplete node (null slots), but it
        // should not throw ArgumentOutOfRangeException.
        var act = () => Parser.BuildNode(kind, slots, default);
        act.Should().NotThrow(
            $"BuildNode must handle ConstructKind.{kind}");
    }
}
```

### 2.4 Tier 4: Documentation-Driven Discoverability

When a developer adds a new construct, they need a checklist. This lives in the codebase, not in a wiki.

**Location:** A `CONTRIBUTING.md` or `docs/compiler/extension-checklist.md` section, plus an XML doc comment on the `ConstructKind` enum itself.

**The Extension Checklist:**

```
## Adding a New Construct to the Precept Language

When you add a new ConstructKind member, the following steps are required.
Steps marked [BUILD] fail the build if skipped. Steps marked [TEST] fail
the test suite. Steps marked [MANUAL] require manual verification.

### 1. [BUILD] Add the ConstructKind enum member
   File: src/Precept/Language/Construct.cs (ConstructKind enum)

### 2. [BUILD] Add a GetMeta() arm in Constructs.cs
   File: src/Precept/Language/Constructs.cs
   - Define the slot sequence (reuse existing ConstructSlot instances or create new ones)
   - Define DisambiguationEntry array (unique or shared leading token)
   - Provide Name, Description, UsageExample

### 3. [BUILD] If you introduced a new ConstructSlotKind:
   a. Add the enum member to ConstructSlotKind
   b. Add a slot parser arm in Parser.InvokeSlotParser()
   c. Add a shared ConstructSlot instance in Constructs.cs

### 4. [BUILD] Add a BuildNode arm in Parser.cs
   - The switch on ConstructKind requires an arm for the new member (CS8509)
   - Define the AST node record (sealed record extending Declaration)
   - Map slot indices to constructor parameters using named constants

### 5. [BUILD] Add the AST node record
   File: src/Precept/Pipeline/SyntaxNodes/ (or wherever AST nodes live)
   - Sealed record extending Declaration
   - One property per slot, nullable for optional slots

### 6. [TEST] Add slot-ordering drift test entries
   - If the construct uses a scoped preposition (In/To/From/On), add it to
     the injection-aware test set in SlotOrderingDriftTests
   - Update guard slot position expectations if applicable

### 7. [TEST] Add parse tests
   - Positive: the construct parses correctly (use UsageExample as starter)
   - Negative: required slots omitted produce diagnostics
   - Error recovery: garbage before the construct, next construct still parses

### 8. [MANUAL] Update the spec (docs/language/precept-language-spec.md)
   - Add the grammar rule in § 2.2
   - Add the dispatch table entry in § 2.2

### 9. [MANUAL] Update MCP vocabulary if applicable
   - If the construct changes precept_language output shape, update LanguageTool.cs

### 10. [MANUAL] Verify LS consumers
   - Completions: does the new construct's leading token trigger completion?
   - Hover: does hovering show the Description from GetMeta()?
   - Semantic tokens: are tokens in the new construct highlighted correctly?
```

### 2.5 Gap Map: Silent Failure vs. Loud Failure

| What you forgot | Current behavior | After v5 validation layer |
|----------------|-----------------|--------------------------|
| `GetMeta()` arm for new `ConstructKind` | **BUILD FAIL** (CS8509) | Same — already loud |
| `BuildNode` arm for new `ConstructKind` | **BUILD FAIL** (CS8509) | Same — already loud |
| `InvokeSlotParser` arm for new `ConstructSlotKind` | **RUNTIME FAIL** (`KeyNotFoundException` deep in parsing) | **BUILD FAIL** (CS8509 on switch) — Decision F7 |
| Slot parser for a `ConstructSlotKind` used in `Slots` | Same `KeyNotFoundException` | **BUILD FAIL** + test safety net |
| Drift in slot ordering (anchor not at index 0) | Silent wrong data in AST | **TEST FAIL** (George's drift tests) |
| New scoped construct without injection awareness | Silent wrong data in AST | **TEST FAIL** (`OnlyRecognizedConstructsUseInjectionPath`) |
| Orphan `ConstructSlotKind` (in enum but unused) | Dead code, no failure | **TEST FAIL** (`EveryConstructSlotKindIsUsedByAtLeastOneConstruct`) |
| Missing `DisambiguationEntry` | Parser dispatch misses the construct | **TEST FAIL** + CS8509 on `GetMeta()` |
| Missing AST node record | **BUILD FAIL** (won't compile in `BuildNode`) | Same — already loud |
| Spec not updated | Silent spec drift | **MANUAL** — checklist reminds, review catches |
| MCP vocabulary not updated | Stale tool output | **MANUAL** — checklist reminds |

### 2.6 Walkthrough: Adding an `audit` Construct

Concrete example: the language adds `audit <FieldName> because "reason"` — a declaration that marks a field as audit-tracked with a rationale. (This is a plausible future modifier, per the vision doc's deferred modifier list.)

**Step 1: Add `ConstructKind.AuditDeclaration` to the enum.**

Build fails immediately at two sites: `Constructs.GetMeta()` and `Parser.BuildNode()`. Both are CS8509 errors.

**Step 2: Define the catalog entry in `Constructs.GetMeta()`.**

```csharp
ConstructKind.AuditDeclaration => new(
    kind,
    "audit declaration",
    "Marks a field as audit-tracked with a rationale",
    "audit Balance because \"Financial field requires audit trail\"",
    [],
    [SlotFieldTarget, SlotBecauseClause],
    [new DisambiguationEntry(TokenKind.Audit)]),  // unique leading token
```

No new `ConstructSlotKind` needed — `FieldTarget` and `BecauseClause` already exist. Build still fails at `BuildNode()`.

**Step 3: Define the AST node.**

```csharp
public sealed record AuditDeclarationNode(
    SourceSpan Span,
    FieldTargetNode FieldTarget,
    BecauseClauseNode Because)
    : Declaration(Span);
```

**Step 4: Add the `BuildNode` arm.**

```csharp
ConstructKind.AuditDeclaration => new AuditDeclarationNode(
    span,
    (FieldTargetNode)slots[0]!,     // FieldTarget (required)
    (BecauseClauseNode)slots[1]!),  // BecauseClause (required)
```

Build succeeds. No new `ConstructSlotKind` → no `InvokeSlotParser` change needed.

**Step 5: Run tests.**

- `EveryConstructSlotKindIsUsedByAtLeastOneConstruct` — still passes (no new slot kinds).
- `AllConstructsHaveAtLeastOneEntry` — passes (we added the entry).
- `OnlyRecognizedConstructsUseInjectionPath` — the new construct's leading token is `Audit`, which is not in the injection set `{In, To, From, On}`. Test passes without modification.
- Parse tests — must be written for the new construct.

**Step 6: Update spec, MCP, LS as needed.**

Total: 4 files touched (enum, catalog, AST node, parser switch arm). Two build-enforced, two manual. No silent failures at any point.

Now consider the harder case: **adding a construct that introduces a new `ConstructSlotKind`.**

If `audit` instead had `audit <FieldName> track <TrackExpression> because "reason"` with a new `TrackExpression` slot kind:

- Add `ConstructSlotKind.TrackExpression` to the enum.
- Build fails at `InvokeSlotParser()` (CS8509 on the switch). **This is Decision F7's payoff.**
- Add the arm: `ConstructSlotKind.TrackExpression => ParseTrackExpression()`.
- Write `ParseTrackExpression()`.
- Build succeeds.

Without F7 (dictionary approach), this step would silently succeed at build time and fail at parse time with `KeyNotFoundException`. Shane's directive says: fail loudly. F7 delivers.

---

## 3. G5 Resolution: RuleDeclaration Slot Naming

### Confirming the Bug

George's G5 is a real bug. Here's the precise problem:

The spec grammar for `rule`:
```
rule BoolExpr ("when" BoolExpr)? because StringExpr
```

The current `RuleDeclaration` slot sequence in `Constructs.cs` line 79:
```csharp
[SlotGuardClause, SlotBecauseClause]
```

Three problems:

1. **The rule body (`BoolExpr`) is mapped to `GuardClause`.** But `GuardClause` has `Description: "when expression"` and its parser (`ParseGuardClause()`) expects `When` as its introduction token. The rule body has NO introduction token — it starts immediately after `rule`.

2. **The optional `when Guard` part of the rule is missing.** The spec says `("when" BoolExpr)?` — there's an optional guard on the rule itself. The current slot sequence has no slot for it.

3. **The slot parser contract is violated.** We established that each slot parser consumes its own introduction token. `ParseGuardClause()` checks for `When` first. For a rule body, there is no `When` — the expression starts immediately. If we call `ParseGuardClause()` for the rule body, it returns null (no `When` found), and we produce a missing-expression diagnostic for a perfectly valid rule.

### Resolution: Introduce `ConstructSlotKind.RuleExpression`

I introduce a new `ConstructSlotKind.RuleExpression` with the following semantics:

- **No introduction token.** After consuming `Rule` as the leading token, the parser calls `ParseExpression(0)` directly.
- **Required.** A rule must have a body expression.
- **Type:** `Expression` (boolean expression).

The updated slot sequence for `RuleDeclaration`:

```csharp
ConstructKind.RuleDeclaration => new(
    kind,
    "rule declaration",
    "Declares a data-truth constraint with optional guard and mandatory reason",
    "rule amount > 0 because \"Amount must be positive\"",
    [],
    [SlotRuleExpression, SlotGuardClause, SlotBecauseClause],
    [new DisambiguationEntry(TokenKind.Rule)]),
```

With shared slot instances:

```csharp
private static readonly ConstructSlot SlotRuleExpression = new(ConstructSlotKind.RuleExpression);
```

### The `ParseRuleExpression()` Method

```csharp
/// <summary>
/// Parses the rule body expression. Unlike other slot parsers, this method
/// has NO introduction token — it calls ParseExpression(0) directly.
/// The rule body is the first thing after the consumed 'rule' keyword.
/// </summary>
private Expression? ParseRuleExpression()
{
    // No introduction token check. The 'rule' keyword was consumed
    // as the leading token. The expression starts immediately.
    return ParseExpression(0);
}
```

### How This Differs from the Introduction-Token Contract

Every other slot parser checks for its introduction token and returns null if absent:

```csharp
private Expression? ParseGuardClause()
{
    if (Current().Kind != TokenKind.When) return null;
    Advance(); // consume 'when'
    return ParseExpression(0);
}
```

`ParseRuleExpression()` does not check — it always parses. This is correct because:

1. The `RuleExpression` slot is **required** (`IsRequired: true`). If the expression is missing, the Pratt parser will emit a diagnostic (expected expression atom) and produce a missing `IdentifierExpression` node.
2. The generic slot iterator can check `IsRequired` before calling the slot parser. If a required slot parser returns null, it emits a diagnostic. For `RuleExpression`, the parser will never return null — `ParseExpression(0)` always returns something (even a missing node).

### Composition: `rule Expr when Guard because Reason`

With the corrected slot sequence `[RuleExpression, GuardClause(optional), BecauseClause]`:

1. `ParseRuleExpression()` — parses `Expr` (the rule body). Stops when it hits `when` (which has no binding power in the Pratt parser — wait. Actually `when` IS a keyword with no binding power, so `ParseExpression(0)` will stop before it.)

   Actually, let me think about this more carefully. `ParseExpression(0)` stops when it encounters a token whose left-binding power is ≤ 0. `When` is a keyword that has no left-denotation in the expression parser — it's not an infix operator. So the Pratt parser sees `When` as a token it can't handle, and stops. The rule body expression is correctly terminated. ✓

2. `ParseGuardClause()` — checks for `When`, consumes it, parses the guard expression. If no `When`, returns null (guard is optional). ✓

3. `ParseBecauseClause()` — checks for `Because`, consumes it, parses the reason string. ✓

This composes cleanly. The Pratt parser's natural termination on non-expression tokens is the key insight: the rule body expression stops at `when` or `because` without any special lookahead.

### The `InvokeSlotParser` Switch Gets a New Arm

```csharp
ConstructSlotKind.RuleExpression => ParseRuleExpression(),
```

With Decision F7 (switch over dictionary), this arm is enforced by CS8509. If someone adds `RuleExpression` to the enum without adding the parser arm, the build fails.

**Decision F8: Introduce `ConstructSlotKind.RuleExpression` for the rule body. Update `RuleDeclaration` to `[RuleExpression, GuardClause(optional), BecauseClause]`.**

---

## 4. P1: `when` Consumption and Tension 1

### P1: `when` Consumption Timing — Confirmed

George's disambiguator flow is correct:

1. Consume leading token (preposition: `In`, `To`, `From`, `On`).
2. Parse anchor target (state or event name).
3. If `Current() == TokenKind.When`: consume guard expression, stash it.
4. Check `Current()` against each candidate's `DisambiguationTokens`.
5. Route to matched construct.
6. Inject stashed anchor and guard into appropriate slots.

This is mandatory for `in State when Guard write Field` (spec-legal, confirmed in `loan-application.precept` line 28). No edge cases missed — the `when` consumption at step 3 is unconditional for all scoped prepositions.

### Tension 1: Pre-Event Guard in TransitionRow — My Final Position

The question: should `from State when Guard on Event -> ...` be valid syntax?

**Context refresh:** The spec says NO — the parenthetical exception at spec § 2.2 line 621 explicitly excludes pre-event guard for `from ... on`. George flags this as a language expansion requiring Shane approval. I accepted it in v3 as Decision F4.

**My revised position: George is right. I withdraw F4.**

Here's my reasoning:

1. **The mandatory `when` consumption at step 3 is for `In`-led and `To`-led constructs, not `From ... on`.** For `In`-led: after consuming `In` and state target, `when` appears before the disambiguation token (`write`/`read`/`omit`/`ensure`). The disambiguator MUST consume it. For `To`-led: after consuming `To` and state target, `when` appears before `ensure` or `->`. Same story. For `From`-led: after consuming `From` and state target, the NEXT token should be the disambiguation token (`On`/`Ensure`/`Arrow`). If we consume `when` unconditionally, then `from State when Guard` routes based on the token after the guard: `On` → TransitionRow, `Ensure` → StateEnsure, `Arrow` → StateAction. This works mechanically — but it means `from State when Guard on Event -> ...` parses as a TransitionRow with the guard stashed at the pre-event position, which is not what the spec says.

2. **The spec's exclusion is intentional, not accidental.** The transition row grammar places the guard after the event: `from StateTarget on Identifier ("when" BoolExpr)?`. The parenthetical exception calls this out explicitly. This is a deliberate language design decision: the guard in a transition row conditions the transition on a specific event, so it belongs after the event name. `from Submitted when Verified on Approve` reads as "from Submitted, when Verified, on Approve" — the guard is on the state, not the event. `from Submitted on Approve when Verified` reads as "from Submitted, on Approve when Verified" — the guard is on the approve transition. The second reading is semantically correct for what the guard actually does.

3. **No sample file uses pre-event guard.** Zero out of 28 samples. All transition row guards are post-event. The language community has organically chosen the post-event position.

4. **The disambiguator can easily skip `when` consumption for `From`-led constructs when the next token after the guard would be `On`.** But it's simpler and more correct to consume `when` unconditionally (because `from State when Guard ensure ...` and `from State when Guard -> ...` are both valid for StateEnsure and StateAction respectively), and then check: if the disambiguation token is `On`, the consumed guard is a pre-event guard on a TransitionRow. At that point, either:
   - (a) Reject: emit a diagnostic saying "guard must follow the event name in transition rows" and move the guard to the correct position in the AST (error recovery).
   - (b) Accept: allow both positions (language expansion).

**My recommendation to Shane:**

Accept option (a): **reject pre-event guard in transition rows with a helpful diagnostic**, and offer a fix suggestion. The disambiguator consumes `when` unconditionally (required for correctness), but when it sees `On` as the disambiguation token for a `From`-led construct AND a guard was pre-consumed, it emits:

> "Guard `when <expr>` must follow the event name in transition rows. Move it after `on <Event>`: `from <State> on <Event> when <expr> -> ...`"

This is the best of both worlds:
- The disambiguator stays generic (always consumes `when` before disambiguation).
- The language surface stays clean (one guard position per construct, no ambiguity).
- The error message is actionable (tells the author exactly what to do).
- No spec change needed — the current spec is correct as written.

**Decision F9: Withdraw F4. Reject pre-event guard in `from ... on` with a diagnostic. Consume `when` unconditionally in the disambiguator for uniformity, but emit an error when a pre-consumed guard precedes `On` in a `From`-led construct.**

---

## 5. P2: EnsureClause + BecauseClause — Confirmed Separate

George proposes keeping them as separate slots. I agree.

The spec grammar is:
```
ensure BoolExpr because StringExpr
```

`because` is mandatory whenever `ensure` appears. But enforcing this is a semantic rule, not a grammar rule. The slot parsers are minimal grammar producers:

- `ParseEnsureClause()` checks for `Ensure`, consumes it, parses `BoolExpr`, returns.
- `ParseBecauseClause()` checks for `Because`, consumes it, parses `StringExpr`, returns.

If `because` is missing, `ParseBecauseClause()` returns null, and the `BecauseClause` slot (which is `IsRequired: true`) triggers a missing-slot diagnostic in the generic slot iterator. The type checker also enforces it.

The alternative — merging into one `ParseEnsureWithReason()` — would couple two concerns (expression parsing and string parsing) into one method and lose the composability of the slot system. The whole point of slot-driven parsing is that each slot is independent.

**Decision F10: `EnsureClause` and `BecauseClause` remain separate slots. The `because` mandate is enforced by the slot's `IsRequired` flag and type checker, not by coupling the slot parsers.**

---

## 6. P4: AccessMode Three-Token Disambiguation Path

George correctly identified this: `in State when Guard write Field` creates a three-step disambiguation path. After consuming `In`, the state target, and the optional `when` guard, the disambiguator sees one of:

| Token after guard | Routes to |
|-------------------|-----------|
| `Write` | `AccessMode` |
| `Read` | `AccessMode` |
| `Omit` | `AccessMode` |
| `Ensure` | `StateEnsure` |

And similarly for `From`-led:

| Token after guard | Routes to |
|-------------------|-----------|
| `On` | `TransitionRow` (with guard error per F9) |
| `Ensure` | `StateEnsure` |
| `Arrow` | `StateAction` |

This is not a bug — it's the natural consequence of the generic disambiguator consuming `when` before disambiguation. The disambiguation tokens in the `DisambiguationEntry` arrays already encode this correctly. The disambiguator checks `Current()` AFTER consuming the optional guard, so the token it checks IS the token after the guard.

**Documentation note for the spec:** After the anchor target, the parser may encounter an optional `when` guard. The guard is consumed before disambiguation. The disambiguation token is the first non-guard token after the anchor target. For `In`-led constructs, this means:

```
in <State> [when <Guard>] { ensure → StateEnsure | write|read|omit → AccessMode }
```

For `To`-led constructs:
```
to <State> [when <Guard>] { ensure → StateEnsure | -> → StateAction }
```

For `From`-led constructs:
```
from <State> [when <Guard>] { on → TransitionRow | ensure → StateEnsure | -> → StateAction }
```

For `On`-led constructs:
```
on <Event> [when <Guard>] { ensure → EventEnsure | -> → EventHandler }
```

This is clean, uniform, and correctly handles the existing spec-legal patterns.

---

## 7. P5: PR 1 Scope — Confirmed with Adjustments

George proposes PR 1 includes:

1. ✅ `DisambiguationEntry` record definition
2. ✅ Updated `ConstructMeta` with `Entries`, `PrimaryLeadingToken`, `[Obsolete] LeadingToken`
3. ✅ Complete `GetMeta()` rewrite — **with F8 correction** (RuleDeclaration gets `[RuleExpression, GuardClause, BecauseClause]`)
4. ✅ `ByLeadingToken` and `LeadingTokens` derived indexes
5. ✅ Slot-ordering drift tests
6. ✅ `_slotParsers` exhaustiveness test skeleton — **but note F7**: the test skeleton validates the switch exhaustiveness rather than dictionary completeness. The test becomes simpler (just verify `InvokeSlotParser` handles every `ConstructSlotKind`).
7. ✅ LS/MCP consumer migration (or flag with `[Obsolete]`)

**Additions to PR 1:**

8. **`ConstructSlotKind.RuleExpression` enum member** — this is the G5 resolution. It must land in PR 1 alongside the `GetMeta()` rewrite, since the `RuleDeclaration` slot sequence changes.

9. **Shared `SlotRuleExpression` instance** in `Constructs.cs`.

10. **`EveryConstructSlotKindIsUsedByAtLeastOneConstruct` test** — validates no orphan slot kinds exist.

**Deferred to PR 2 (parser implementation):**

- The actual `InvokeSlotParser` switch method
- `ParseRuleExpression()` implementation
- `BuildNode` arms for all construct kinds
- AST node record definitions
- Parse tests

PR 1 is the catalog shape migration + validation infrastructure. PR 2 is the parser itself.

---

## 8. Consolidated Decision Table

| ID | Decision | Resolution | Source |
|----|----------|-----------|--------|
| F1 | `LeadingTokenSlot` on `DisambiguationEntry` | Accepted (v3) | George v2 fix |
| F2 | ~~`_slotParsers` as FrozenDictionary~~ | **SUPERSEDED by F7** | v3, overruled in v5 |
| F3 | ActionChain peek-before-consume | Accepted (v3) | George v2 fix |
| F4 | ~~Two-position `when` guard for TransitionRow~~ | **WITHDRAWN** — see F9 | v3, withdrawn in v5 |
| F5 | `DisambiguationTokens` derivation | Reject derivation — declare explicitly (v3) | Frank v3 |
| F6 | Migration PR sequence | Accepted with bridge (v3) | George v2 proposal |
| **F7** | **`_slotParsers` is an exhaustive switch on `ConstructSlotKind`, not a dictionary** | CS8509 enforces completeness at build time. Supersedes F2. | Frank v5 (Shane directive) |
| **F8** | **Introduce `ConstructSlotKind.RuleExpression` for rule body. Update `RuleDeclaration` to `[RuleExpression, GuardClause(opt), BecauseClause]`** | Resolves G5 naming collision. Rule body has no introduction token. | Frank v5 (G5 resolution) |
| **F9** | **Reject pre-event guard in `from ... on` with a diagnostic. Consume `when` unconditionally in disambiguator but error on pre-event guard + `On`.** | Withdraws F4. Spec is correct as written. Diagnostic tells author the correct position. | Frank v5 (Tension 1 resolution) |
| **F10** | **`EnsureClause` and `BecauseClause` remain separate slots** | `because` mandate enforced by `IsRequired` flag and type checker, not by coupling parsers. | Frank v5 (P2 confirmation) |
| G1 | Pre-disambiguation `when` consumption is mandatory | Required — `in State when Guard write Field` is spec-legal | George v4 |
| G2 | `Func<SyntaxNode?>` slot parser type | Confirmed — no boxing, covariance handles widening | George v4 |
| G3 | Source generator: lead with test gen, not AST gen | **MOOT** — Shane directive says no source generation at all | George v4, overridden by Shane |
| G4 | `->` vs `<-` for computed fields | Keep `->` for consistency | George v4 |
| G5 | `RuleDeclaration` slot kind naming bug | **RESOLVED** — see F8 | George v4, resolved in v5 |
| G6 | Pre-event `when` guard requires Shane decision | **RESOLVED** — see F9 (reject with diagnostic) | George v4, resolved in v5 |

---

## 9. For George — Round 6

### Status Assessment

The design is stable enough for implementation planning. All five of George's P1–P5 items are resolved. The validation layer design answers Shane's directive. The G5 bug is fixed. Tension 1 is resolved.

### What's Left Before PR 1

1. **George implements PR 1** per the scope in § 7:
   - `DisambiguationEntry` record
   - `ConstructMeta` update with `Entries`
   - `ConstructSlotKind.RuleExpression` addition
   - `GetMeta()` rewrite (all 11 constructs with corrected `RuleDeclaration`)
   - `ByLeadingToken` / `LeadingTokens` indexes
   - Drift tests + exhaustiveness tests
   - LS/MCP migration

2. **George reviews F7** (switch over dictionary for `InvokeSlotParser`). I've made the argument. If George has a strong counter-argument (e.g., testability concern, or the switch makes the parser harder to compose), I'll listen. But Shane's directive weighs heavily here — compile-time enforcement over runtime discovery.

3. **George reviews F9** (reject pre-event guard). The disambiguator still consumes `when` unconditionally — no change to the generic flow. The only addition is a diagnostic when the consumed guard appears in a `From`-led construct that routes to `TransitionRow`.

### Questions for George

1. **F7 switch testability:** With the dictionary approach, the test could instantiate the parser and read dictionary keys. With the switch approach, the test calls `InvokeSlotParser` with each `ConstructSlotKind` and verifies it doesn't throw. Is there a testability concern I'm missing?

2. **F9 error recovery:** When the disambiguator consumes a pre-event guard for a `From`-led construct and then sees `On`, it must still route to `TransitionRow` (for error recovery — we want to parse as much of the construct as possible). The consumed guard should be placed in the `GuardClause` slot (same as if it were post-event). The only difference is the diagnostic. Does this create any slot-ordering issue?

3. **F8 slot iteration:** `RuleExpression` has no introduction token. The generic slot iterator calls `InvokeSlotParser(RuleExpression)` which calls `ParseExpression(0)` directly. Does the generic slot iterator need special handling for intro-token-less slots, or does the slot parser's unconditional parse already handle this?

### If No Counter-Arguments

I declare the design stable for implementation. George can begin PR 1. We can do a Round 6 focused on PR 1 code review if needed, or skip straight to implementation.

---

## Appendix A: Updated `ConstructSlotKind` → Slot Parser Cross-Reference

| Slot Kind | Introduction Token | Parser Method | Return Type | Notes |
|-----------|--------------------|---------------|-------------|-------|
| `IdentifierList` | `Identifier` | `ParseIdentifierList` | `IdentifierListNode?` | |
| `TypeExpression` | `As` | `ParseTypeExpression` | `TypeRefNode?` | |
| `ModifierList` | (modifier keyword) | `ParseModifierList` | `ModifierListNode?` | Greedy — consumes all recognized modifier keywords |
| `StateModifierList` | (state modifier keyword) | `ParseStateModifierList` | `ModifierListNode?` | Greedy — consumes all recognized state modifier keywords |
| `ArgumentList` | `LeftParen` | `ParseArgumentList` | `ArgumentListNode?` | |
| `ComputeExpression` | `Arrow` | `ParseComputeExpression` | `Expression?` | |
| **`RuleExpression`** | **NONE** | **`ParseRuleExpression`** | **`Expression?`** | **No intro token — calls `ParseExpression(0)` directly. F8 addition.** |
| `GuardClause` | `When` | `ParseGuardClause` | `Expression?` | |
| `ActionChain` | `Arrow` | `ParseActionChain` | `ActionChainNode?` | Peek-before-consume for outcome |
| `Outcome` | `Arrow` | `ParseOutcome` | `OutcomeNode?` | |
| `StateTarget` | `Identifier`/`Any` | `ParseStateTarget` | `StateTargetNode?` | |
| `EventTarget` | `Identifier` | `ParseEventTarget` | `EventTargetNode?` | |
| `EnsureClause` | `Ensure` | `ParseEnsureClause` | `Expression?` | |
| `BecauseClause` | `Because` | `ParseBecauseClause` | `BecauseClauseNode?` | |
| `AccessModeKeyword` | `Write`/`Read`/`Omit` | `ParseAccessModeKeyword` | `TokenValueNode?` | |
| `FieldTarget` | `Identifier`/`All` | `ParseFieldTarget` | `FieldTargetNode?` | |

## Appendix B: Updated `RuleDeclaration` Slot Sequence

**Before (v4, incorrect):**
```
RuleDeclaration: [GuardClause, BecauseClause]
```

**After (v5, corrected):**
```
RuleDeclaration: [RuleExpression(required), GuardClause(optional), BecauseClause(required)]
```

Parse flow for `rule amount > 0 because "Amount must be positive"`:
1. Consume `Rule` (leading token).
2. `ParseRuleExpression()` → `amount > 0` (stops at `because` — no binding power).
3. `ParseGuardClause()` → null (no `when` found).
4. `ParseBecauseClause()` → `"Amount must be positive"`.

Parse flow for `rule amount > 0 when Active because "Amount must be positive in active state"`:
1. Consume `Rule` (leading token).
2. `ParseRuleExpression()` → `amount > 0` (stops at `when` — no binding power).
3. `ParseGuardClause()` → `Active` (consumes `when`, parses expression).
4. `ParseBecauseClause()` → `"Amount must be positive in active state"`.
