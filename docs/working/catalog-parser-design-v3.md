# Catalog-Driven Parser: Full Vision Design (Round 3)

**By:** Frank (Lead Architect)
**Date:** 2026-04-27
**Status:** Design session Round 3 — synthesis, decisions, extensibility analysis
**References:**
- `docs/working/catalog-parser-design-v1.md` — Frank's Round 1 (superseded by this document)
- `docs/working/catalog-parser-design-v2.md` — George's Round 2 (superseded by this document)
- `src/Precept/Language/Constructs.cs` — actual catalog source
- `src/Precept/Pipeline/Parser.cs` — parser stub
- `docs/compiler/parser.md` — parser design doc

**This document supersedes v1 and v2.** It is the living design document for the catalog-driven parser.

---

## 1. George's Bug Fixes — Incorporated

George found two genuine implementation bugs in v1. Both are real, both are fixed.

### Bug 1: ActionChain Consuming `->` Before Outcome

**The bug:** In my v1 `ParseActionChain()`, the while loop consumes `->` then checks for outcome keywords. When it breaks on finding `transition`/`no`/`reject`, the `->` is already consumed. `ParseOutcome()` then looks for `->` as its introduction token and finds the outcome keyword instead — returns null — triggers a required-slot diagnostic on valid input.

**This fires on every TransitionRow that has an action chain.** Not an edge case.

**The fix — peek before consume:**

```csharp
private ActionChainNode? ParseActionChain()
{
    if (Current().Kind != TokenKind.Arrow) return null;

    var actions = ImmutableArray.CreateBuilder<ActionStatementNode>();

    while (Current().Kind == TokenKind.Arrow)
    {
        // Peek past '->' before consuming. If the token after '->' is an outcome
        // keyword, leave '->' in the stream for ParseOutcome() to consume as its
        // introduction token. This upholds the slot-parser introduction-token contract.
        if (IsOutcomeKeyword(Peek(1).Kind))
            break;

        Advance(); // consume '->' only when we know an action follows

        var actionToken = Current();
        if (!_actionKeywords.Contains(actionToken.Kind))
        {
            EmitDiagnostic(DiagnosticCode.ExpectedAction, actionToken);
            SyncToNextAction();
            break;
        }

        Advance(); // consume action keyword
        actions.Add(ParseActionStatement(actionToken));
    }

    return actions.Count > 0
        ? new ActionChainNode(
            SourceSpan.Covering(actions[0].Span, actions[^1].Span),
            actions.ToImmutable())
        : null;
}
```

**Verification against all three outcome forms:**

| Source fragment | `Peek(1)` sees | Action |
|----------------|----------------|--------|
| `-> set x = 1 -> transition Approved` | After last action `->`: `transition` → break. `->` stays for `ParseOutcome()`. ✅ |
| `-> set x = 1 -> no transition` | `no` → break. `->` stays. `ParseOutcome()` consumes `->`, then `no`, then `transition`. ✅ |
| `-> set x = 1 -> reject "msg"` | `reject` → break. `->` stays. `ParseOutcome()` consumes `->`, then `reject`, then string. ✅ |
| `-> transition Approved` (no actions) | First iteration: `transition` → break immediately. `actions.Count == 0` → returns null. `ParseOutcome()` sees `->`. ✅ |

The fix is correct and complete.

### Bug 2: `LeadingTokenSlot` on `DisambiguationEntry`

**The bug:** When `write all` is parsed at root level, the dispatch loop consumes `Write` as the leading token. `ParseConstructSlots()` then iterates AccessMode's slots: `[OptStateTarget, AccessModeKeyword, FieldTarget]`. When it reaches slot 1 (`AccessModeKeyword`), it calls `ParseAccessModeKeyword()` — but the current token is `all` (the field target), not `Write` (already consumed). The parser silently produces a wrong AST.

**George's fix is correct. `LeadingTokenSlot: ConstructSlotKind?` on `DisambiguationEntry`:**

```csharp
public sealed record DisambiguationEntry(
    TokenKind                      LeadingToken,
    ImmutableArray<TokenKind>?     DisambiguationTokens = null,
    /// <summary>
    /// When the leading token is also a slot value (not merely a dispatch signal),
    /// specifies which slot kind it occupies. The generic iterator injects a synthetic
    /// node for this slot rather than calling the slot parser fresh.
    /// Null for constructs where the leading token is purely a dispatch signal.
    /// </summary>
    ConstructSlotKind?             LeadingTokenSlot = null);
```

**Updated `ParseConstructSlots` incorporating the fix:**

```csharp
private Declaration ParseConstructSlots(
    ConstructMeta meta, SourceSpan leadingSpan,
    DisambiguationEntry entry, Token leadingToken)
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
    return BuildNode(meta.Kind, SourceSpan.Covering(leadingSpan, endSpan), slots);
}
```

**Only one construct currently uses this:** AccessMode's `Write` entry. The field exists for correctness, not generality — but it's the right shape because any future construct where the leading token doubles as slot content uses the same mechanism.

**Updated `DisambiguationEntry` table (definitive):**

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
| `AccessMode` | `Write` | null | `AccessModeKeyword` |
| `AccessMode` | `In` | `[Write, Read, Omit]` | null |
| `StateAction` | `To` | `[Arrow]` | null |
| `StateAction` | `From` | `[Arrow]` | null |
| `EventEnsure` | `On` | `[Ensure]` | null |
| `EventHandler` | `On` | `[Arrow]` | null |

---

## 2. Six Decisions — Resolved

### Decision F1: `LeadingTokenSlot` on `DisambiguationEntry` — ACCEPTED

George's `LeadingTokenSlot: ConstructSlotKind?` is the right fix. The alternatives — splitting AccessMode's root-level case into a separate hand-written method, or using a sentinel pattern — both break the generic iterator's uniformity for one edge case. The `LeadingTokenSlot` field is minimal, precise, and handles any future construct where the leading token doubles as slot content. The common case (null) has zero overhead. Accepted as-is.

### Decision F2: `BuildNode` Switch vs. Factory Dictionary — GEORGE WINS

I'm conceding this one. George's split-by-purpose argument is right:

- **`_slotParsers`: dictionary.** It's a keyed registry of parsing functions. Adding a new `ConstructSlotKind` means adding one entry. The dictionary IS the right shape for a registry where entries are independently addressable.

- **`BuildNode`: exhaustive switch with named local index constants.** The mapping from `ConstructKind` to an AST node constructor is an exhaustive invariant — every kind MUST have a mapping. CS8509 enforces this at compile time. My factory dictionary defers this invariant to a runtime test. George is right that this trades a compile-time guarantee for a runtime test without adding debugging value. The named local index constants (George's addition) make slot positions explicit and prevent silent ordering bugs.

**The settled shape:**

```csharp
// _slotParsers: dictionary — registry pattern, keyed dispatch
private readonly FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>> _slotParsers;

// BuildNode: exhaustive switch — CS8509 enforcement
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

        // ... remaining arms with same pattern

        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
```

The factory completeness test remains as belt-and-suspenders — it validates slot-count alignment, which CS8509 cannot check.

### Decision F3: ActionChain/Outcome Peek-Before-Consume — ACCEPTED, VERIFIED

George's fix is correct. I verified against all three outcome forms and the no-action case (see Bug 1 section above). The peek-before-consume pattern correctly handles:

- `-> set reviewer = approver -> transition Submitted` — ActionChain parses `set`, peeks past second `->`, sees `transition`, breaks. `->` stays for Outcome.
- `-> transition Approved` (no actions) — ActionChain peeks past first `->`, sees `transition`, breaks immediately with 0 actions (returns null, slot is optional). Outcome consumes `->`.
- `-> no transition` — same pattern, `no` is in `IsOutcomeKeyword`.
- `-> reject "msg"` — same pattern, `reject` is in `IsOutcomeKeyword`.

This also correctly handles constructs WITHOUT an Outcome slot (StateAction, EventHandler) — their action chains simply run until `Current().Kind != TokenKind.Arrow` (newline or end of source), and `IsOutcomeKeyword` is never triggered.

### Decision F4: Two-Position `when` Guard in TransitionRow — ACCEPTED WITH DOCUMENTATION

Both positions are valid:

**Position 1 (pre-disambiguation):** `from Draft when Status == "ready" on Submit -> ...`
**Position 2 (post-EventTarget):** `from Draft on Submit when Status == "ready" -> ...`

Position 2 is the canonical form shown in samples. Position 1 is a natural reading in English ("from this state, when this condition holds, on this event...") that the generic disambiguator handles for free.

**Why I accept it:** The disambiguator's `when` consumption is 4 lines of uniform code that costs nothing for constructs without a GuardClause slot — if no `when` keyword appears, the `if` block simply doesn't fire. Removing it to "simplify" the disambiguator would save 4 lines while losing syntactic flexibility. Not worth it.

**The constraint:** This only matters for TransitionRow — the only scoped construct with a GuardClause slot. Both positions inject into slot[2]. George's analysis of the injection mechanism is correct.

**Required documentation:** The spec must explicitly list both positions as valid. The samples should show both forms. The first form to appear in parser.md's examples should be the canonical Position 2; Position 1 should be documented as an accepted alternative.

### Decision F5: `DisambiguationTokens` Derivable from Slot Vocabulary — REJECT DERIVATION

I agree with George. The disambiguation tokens and the slot vocabulary are correlated but serve different purposes:

- **Disambiguation tokens** answer: "which construct did the user intend?" — a routing concern.
- **Slot vocabulary** answers: "what tokens are valid content for this slot?" — a grammar concern.

These happen to overlap for AccessMode (`[Write, Read, Omit]` both disambiguate and are slot content), but that's a coincidence of this construct, not an invariant. A future construct could disambiguate on a token that isn't the slot's content token.

Adding `IntroductionTokens: ImmutableArray<TokenKind>?` to `ConstructSlot` would create a dependency between routing and grammar internals. These are separable concerns. **Declare the `[Write, Read, Omit]` tokens explicitly in the catalog and document their correspondence with the slot vocabulary as a code comment, not a structural coupling.**

### Decision F6: Migration PR Sequence — ACCEPTED WITH BRIDGE

George's sequenced PR plan is correct. The catalog shape change (`DisambiguationEntry` replacing `LeadingToken`) is a breaking change to `ConstructMeta`'s public surface. It must land before parser work begins.

**I accept the `PrimaryLeadingToken` bridge approach.** Rationale:

1. **Safety.** LS and MCP consumers currently read `LeadingToken`. The bridge property `PrimaryLeadingToken => Entries[0].LeadingToken` (plus `LeadingToken` as a computed alias) means those consumers don't break when PR 1 lands. They migrate at their own pace.

2. **Cleanliness.** The alias is explicitly temporary — it's documented as a backward-compatibility bridge with an expiration date (removed when all consumers are migrated). This is cleaner than a hard cutover where LS/MCP consumers must update in the same PR as the catalog change.

3. **PR atomicity.** PR 1 (catalog shape) is testable independently. PR 2+ (parser work) builds on a stable catalog surface. Neither depends on LS/MCP migration timing.

The bridge property is removed in the PR where the last consumer migrates away from single-`LeadingToken` access.

---

## 3. Explicit Contract: Slot-Parser Introduction-Token Rule

George identified that the generic disambiguator leaves disambiguation tokens unconsumed, relying on slot parsers to consume their own introduction tokens. This is correct behavior but was implicit in v1. **It is now an explicit contract:**

> **Slot-Parser Introduction-Token Contract:** Each slot parser is responsible for consuming its own introduction token(s). The generic iterator never pre-consumes the token that leads into a slot. When a slot immediately follows a disambiguation point, the slot parser MUST consume the disambiguation token (e.g., `ParseEnsureClause()` calls `Expect(TokenKind.Ensure)`, not assume `Ensure` was pre-consumed). Violating this contract causes the next slot parser to see the wrong token.

This contract is documented as a code comment on the `_slotParsers` dictionary and as a section in `docs/compiler/parser.md`.

George's verification of all four preposition tokens against this contract (§ Issue C1 in v2) is confirmed:

| Leading token | Disambiguation token | Slot parser that consumes it | Contract upheld |
|--------------|---------------------|------------------------------|----------------|
| `In/To/From` | `Ensure` | `ParseEnsureClause()` → `Expect(Ensure)` | ✅ |
| `To/From` | `Arrow` | `ParseActionChain()` → `Current() == Arrow` | ✅ |
| `From` | `On` | `ParseEventTarget()` → `Expect(On)` | ✅ |
| `In` | `Write/Read/Omit` | `ParseAccessModeKeyword()` → consumes current | ✅ |

---

## 4. Language Extensibility Analysis

This is the core of Round 3. Shane asked: what does adding a new language feature cost under each design?

### Baseline: Adding a Construct Under the Current Stub Parser

Today the parser is `throw new NotImplementedException()`. But the *design intent* in `Parser.cs` is a hand-written dispatch switch with per-construct parse methods. Under that design, adding a new construct requires:

**Catalog layer (Language/):**
1. `ConstructKind.cs` — add enum member
2. `ConstructSlotKind.cs` — add new slot kinds (if the construct introduces new grammar shapes)
3. `Constructs.cs` — add `GetMeta()` switch arm with full slot sequence
4. `TokenKind.cs` — add leading keyword token
5. `Tokens.cs` — add `TokenMeta` entry for the keyword

**Parser layer (Pipeline/):**
6. New AST node record — `NewConstructNode` with strongly typed properties
7. `Parser.cs` — add dispatch case in the switch
8. `Parser.cs` — write dedicated `ParseNewConstruct()` method (~20-80 lines depending on complexity)
9. If disambiguation needed: modify the relevant `ParseXScoped()` method to add a new branch
10. If new slot shapes: write new parsing helper methods

**Semantic layers:**
11. `TypeChecker.cs` — add validation rules for the new construct
12. `GraphAnalyzer.cs` — if the construct affects state graph structure
13. `ProofEngine.cs` — if the construct introduces provable properties
14. `Evaluator.cs` — runtime execution semantics

**Tooling layers:**
15. Language server — completions context, hover info, semantic tokens
16. MCP — `precept_language` vocabulary (automatic from catalog), `precept_compile` DTO shape
17. Grammar — TextMate token classification (generated from catalog)

**Test + documentation:**
18. Parser tests — valid parse, missing required slots, error recovery
19. TypeChecker tests — valid/invalid combinations
20. Integration tests — sample file round-trips
21. `precept-language-spec.md` — grammar rule, examples
22. `docs/compiler/parser.md` — dispatch table update
23. Sample file(s) in `samples/`

**Total: ~18-23 files changed, ~200-500 lines of new code** depending on construct complexity.

### Adding a Construct Under the Catalog-Driven Design

Under the full vision from this document:

**Catalog layer (Language/) — SAME as baseline:**
1. `ConstructKind.cs` — add enum member
2. `ConstructSlotKind.cs` — add new slot kinds (only if genuinely new grammar shapes)
3. `Constructs.cs` — add `GetMeta()` switch arm with slots and `DisambiguationEntry`
4. `TokenKind.cs` — add leading keyword token
5. `Tokens.cs` — add `TokenMeta` entry

**Parser layer (Pipeline/) — DRAMATICALLY REDUCED:**
6. New AST node record — `NewConstructNode` with typed properties
7. `BuildNode` — add one switch arm (~5-8 lines with named constants)
8. `_slotParsers` — add entries for new slot kinds (only if new grammar shapes; 0 if all slot kinds already exist)
9. **NO dispatch code change** — the catalog lookup handles it automatically
10. **NO disambiguation code change** — `DisambiguationEntry` metadata routes correctly
11. **NO new parse method** — generic `ParseConstructSlots` handles the slot iteration

**Semantic layers — SAME as baseline:**
12-15. TypeChecker, GraphAnalyzer, ProofEngine, Evaluator — unchanged scope

**Tooling layers — SAME as baseline:**
16-18. LS, MCP, Grammar — same scope (mostly automatic from catalog)

**Test + documentation — SLIGHTLY REDUCED:**
19. Parser tests — fewer (no per-construct method to test; test the catalog routing + slot iteration)
20-23. Same scope

**Total: ~15-20 files changed, ~100-300 lines of new code.** The savings are concentrated in the parser layer:
- No dispatch method (~20-40 lines saved)
- No per-construct parse method (~30-80 lines saved)
- No disambiguation method changes (~10-30 lines saved)

### Where the Savings Are Real vs. Illusory

**Real savings (parser layer):**
- Dispatch: truly zero code. The catalog lookup is generic.
- Slot iteration: truly zero code if all needed slot kinds exist.
- Disambiguation: truly zero code. The `DisambiguationEntry` table routes automatically.
- Error recovery: truly zero code. Sync points are catalog-derived.

**Illusory savings (semantic layers):**
- TypeChecker: still needs per-construct validation. A new construct's semantic rules are domain knowledge.
- Evaluator: still needs per-construct execution semantics.
- GraphAnalyzer: still needs to know whether the construct affects state topology.
- These are irreducibly hand-written because they implement MEANING, not SYNTAX.

**The honest assessment:** The catalog-driven parser saves ~60-80% of parser-layer code per new construct. It saves 0% of semantic-layer code. For Precept at 11 constructs, this is roughly 50-150 lines per construct — meaningful but not transformative. The real value is structural: the parser cannot drift from the catalog, adding a construct cannot break existing parsing, and the dispatch table is provably complete by construction.

---

## 5. Generic AST — Architectural Evaluation

Shane asked four outside-the-box questions. I'll address each honestly.

### Option A: Generic `ConstructNode` with Slots

```csharp
public sealed record ConstructNode(
    SourceSpan Span,
    ConstructKind Kind,
    ImmutableArray<SlotValue> Slots);

// SlotValue is a discriminated union
public abstract record SlotValue;
public sealed record IdentifierListValue(ImmutableArray<string> Names) : SlotValue;
public sealed record TypeRefValue(TypeKind Type, /* qualifiers */) : SlotValue;
public sealed record ExpressionValue(Expression Expr) : SlotValue;
public sealed record TokenValue(TokenKind Token) : SlotValue;
// ... one per slot value shape
```

**What it enables:**
- Zero new AST node classes per construct. `ConstructNode` handles everything.
- Generic visitors: `foreach (var node in tree.Declarations) { switch(node.Kind) { ... } }` — no per-type pattern matching.
- `BuildNode` disappears entirely — the generic iterator produces `ConstructNode` directly from slots.
- Trivially serializable — the tree is uniform.

**What it costs:**
- **TypeChecker loses pattern matching.** Today: `case FieldDeclarationNode f => CheckField(f.Names, f.Type, f.Modifiers)`. Under generic AST: `case ConstructNode n when n.Kind == ConstructKind.FieldDeclaration => CheckField(((IdentifierListValue)n.Slots[0]).Names, ((TypeRefValue)n.Slots[1]).Type, ...)`. Every access is an index + cast. Compile-time safety drops to zero for slot access ordering.
- **IDE support degrades.** "Find all usages of `FieldDeclarationNode.Names`" becomes impossible — it's `Slots[0]` with a cast, which no IDE can track semantically.
- **Error locality worsens.** A wrong slot index produces a runtime `InvalidCastException` deep in the type checker, not a compile-time error at the call site.
- **Refactoring risk.** Changing a construct's slot order in the catalog silently breaks every consumer that accesses slots by index. With typed nodes, changing the record's constructor parameters triggers compile errors at every call site.

**Verdict: REJECT.** The cost to downstream consumers is catastrophic. The type checker and evaluator are the most complex parts of the pipeline — they are exactly where strong typing provides the most value. Saving ~8 lines per construct in AST node declarations is not worth making the type checker fragile and unrefactorable.

### Option B: AST = Catalog Tree with Resolved Slot Values

This is Option A taken to its logical conclusion. Instead of AST nodes, the "tree" is a list of `ConstructMeta` instances with their resolved `SlotValue` arrays attached. Pipeline stages navigate via catalog structural metadata.

```csharp
public sealed record ResolvedConstruct(
    ConstructMeta Meta,
    SourceSpan Span,
    ImmutableArray<SlotValue?> ResolvedSlots);
```

**What it enables:**
- The AST node hierarchy vanishes entirely. Zero per-construct code for AST.
- Pipeline stages can be written generically: "for each construct that has a GuardClause slot, validate the guard expression." This is a catalog query, not a switch.
- New constructs are instantly visible to any pipeline stage that operates on slot kinds rather than construct kinds.

**What it costs:**
Everything from Option A, plus:
- **Semantic validation becomes slot-centric, not construct-centric.** But constraints ARE construct-specific: a `GuardClause` in a `TransitionRow` means "condition for this transition to fire," while a `GuardClause` in a `RuleDeclaration` means "the rule itself." Same slot kind, completely different semantics. A generic slot-centric pipeline cannot distinguish these — it must still switch on `Meta.Kind`, at which point the genericity adds no value.
- **The proof engine requires construct-specific knowledge by definition.** Proving that "all reachable states have amount > 0" requires understanding StateEnsure semantics. A generic tree does not help.
- **Performance.** Every slot access is a dictionary lookup on slot kind plus an index scan. Typed property access is a direct field read.

**Verdict: REJECT.** This design confuses syntactic structure (which CAN be generic) with semantic meaning (which CANNOT). Precept's pipeline stages are fundamentally construct-specific because each construct carries domain-specific semantics. A generic tree is the wrong abstraction for consumers that need to know what things MEAN.

### Option C: Source-Generated Strongly Typed AST Nodes

A Roslyn source generator reads `ConstructMeta` entries at compile time and emits:
1. One `sealed record` per `ConstructKind` with typed properties matching the slot sequence
2. The `BuildNode` switch body (slot-to-property mapping with correct casts)
3. A visitor base class with one `Visit` method per construct kind
4. Slot-count alignment tests

```csharp
// Generated from Constructs.GetMeta(ConstructKind.FieldDeclaration)
// Slots: [IdentifierList(required), TypeExpression(required), ModifierList(optional), ComputeExpression(optional)]
public sealed record FieldDeclarationNode(
    SourceSpan Span,
    ImmutableArray<string> Names,        // from IdentifierList
    TypeRefNode Type,                     // from TypeExpression
    ImmutableArray<ModifierMeta> Modifiers, // from ModifierList
    Expression? ComputedValue)            // from ComputeExpression
    : Declaration(Span);
```

**What it enables:**
- New construct = new catalog entry → regenerate → AST node, BuildNode arm, and visitor method appear automatically.
- Strong typing preserved. Downstream consumers pattern-match on specific node types.
- Zero drift between catalog and AST — the generator enforces it.
- The factory completeness test becomes unnecessary — the generator IS the completeness guarantee.

**What it costs:**
- **Generator infrastructure.** A new project (`Precept.Generators`), incremental pipeline registration, attribute markers. This is ~1-2 days of setup.
- **Debugging indirection.** When the generated AST node has wrong types, you debug the generator, not the node. This is one more layer of abstraction between "what broke" and "where to fix it."
- **Naming conventions.** The generator must map `ConstructSlotKind.IdentifierList` to C# property name `Names` and type `ImmutableArray<string>`. This requires either a naming convention table in the catalog or convention-based inference. Both are fragile at the margin.
- **The slot-to-property-type mapping is domain knowledge.** `IdentifierList` → `ImmutableArray<string>` vs. `ImmutableArray<Token>` vs. `IdentifierListNode` — this depends on what downstream consumers need, not on what the slot kind says. The generator either hardcodes this mapping (defeating the purpose) or adds metadata to `ConstructSlot` (increasing catalog complexity).
- **At 11 constructs, the math doesn't work.** Generator infrastructure saves ~8 lines per construct (the AST record declaration) and ~5-8 lines per construct (the `BuildNode` arm). At 11 constructs, that's ~143-176 lines of code replaced by a generator that itself requires 200-400 lines of infrastructure. Net negative.

**Break-even: ~25-30 constructs.** At that scale, the generator saves more code than it costs to maintain. The design is generator-ready today.

**Verdict: NOT YET. The design accommodates this as a future evolution.** When construct count reaches ~25-30, or when a second consumer needs catalog-to-code derivation, revisit. The critical architectural decision is that `_nodeFactories` (now `BuildNode` switch) is the single point of coupling between catalog slots and AST node shapes — a generator replaces that one method, nothing else changes.

### Option D: Zero Hand-Written Code Per Construct — Is It Achievable?

**What "zero hand-written code" would require:**

For the parser layer:
1. Source generator emits AST node records from `ConstructMeta` → **achievable** (Option C)
2. Source generator emits `BuildNode` arms → **achievable** (Option C)
3. `_slotParsers` already covers all needed slot kinds → **achievable** if new constructs reuse existing grammar shapes

For the semantic layers:
4. TypeChecker reads validation rules from metadata → **requires a rule engine in the catalog**
5. Evaluator reads execution semantics from metadata → **requires an execution model in the catalog**
6. GraphAnalyzer reads topology effects from metadata → **requires a topology DSL in the catalog**

Items 4-6 are the crux. They require encoding construct-specific semantics as catalog metadata — which means the catalog becomes a programming language. A `ConstraintRule` record that says "field X must be positive when in state Y" is a program, not metadata. Precept already HAS a language for expressing these rules — it's Precept itself. Encoding them in C# metadata would be building a second, inferior language inside the catalog.

**The minimum invariants for zero parser-layer code:**

If we restrict "zero hand-written code" to the parser layer only:
1. Source generator for AST nodes and BuildNode (Option C at scale)
2. Every new construct composes exclusively from existing `ConstructSlotKind` values
3. `DisambiguationEntry` metadata handles all routing

This IS achievable. A new construct that uses only existing slot kinds (IdentifierList, TypeExpression, GuardClause, etc.) and enters through a unique leading token requires: one `GetMeta()` entry, one `ConstructKind` enum member, one `TokenKind` + `TokenMeta` — and the source generator produces everything else.

**But this only covers constructs that reuse existing grammar shapes.** A construct that introduces a genuinely new grammar shape (e.g., a `timeout` with a duration literal that has unique syntax) requires a new `ConstructSlotKind` and a new slot parser. This is irreducible — new grammar shapes require new grammar code. No metadata system changes this.

**Summary: Zero hand-written parser code is achievable for constructs that reuse existing grammar shapes. It is impossible for constructs that introduce new grammar shapes. Zero hand-written semantic code is impossible without making the catalog a programming language, which is architecturally wrong.**

---

## 6. The Extensibility Thought Experiment

### The Construct: `version` Declaration

```precept
precept LoanApplication
version 2
field amount as money nonnegative
...
```

A `version` declaration specifies the schema version of the precept. One number literal, appears once at the top level after the header.

### Under Today's (Stub) Parser Design

**Files changed:**

| File | Change | Lines |
|------|--------|-------|
| `ConstructKind.cs` | Add `VersionDeclaration` | +1 |
| `TokenKind.cs` | Add `Version` keyword | +1 |
| `Tokens.cs` | Add `TokenMeta` for `Version` | +8 |
| `ConstructSlotKind.cs` | Add `VersionNumber` | +1 |
| `ConstructSlot.cs` | No change | 0 |
| `Constructs.cs` | Add `GetMeta(VersionDeclaration)` + new shared slot | +12 |
| `Lexer.cs` | No change (keyword recognition is catalog-driven) | 0 |
| New: `VersionDeclarationNode.cs` | AST record | +5 |
| `Parser.cs` | Add `case TokenKind.Version:` to dispatch switch | +2 |
| `Parser.cs` | Write `ParseVersionDeclaration()` method | +15 |
| `TypeChecker.cs` | Validate: positive integer, appears at most once | +20 |
| Language server | Completion context, hover | +15 |
| MCP | Automatic from catalog | 0 |
| Tests | ~12 test cases | +80 |
| Spec + docs | Grammar rule, example | +20 |
| **Total** | | **~180 lines** |

### Under the Catalog-Driven Design (This Document)

**Files changed:**

| File | Change | Lines |
|------|--------|-------|
| `ConstructKind.cs` | Add `VersionDeclaration` | +1 |
| `TokenKind.cs` | Add `Version` keyword | +1 |
| `Tokens.cs` | Add `TokenMeta` for `Version` | +8 |
| `ConstructSlotKind.cs` | Add `VersionNumber` | +1 |
| `Constructs.cs` | Add `GetMeta(VersionDeclaration)` + shared slot | +12 |
| New: `VersionDeclarationNode.cs` | AST record | +5 |
| `Parser.cs` / `_slotParsers` | Add `ParseVersionNumber` slot parser | +8 |
| `Parser.cs` / `BuildNode` | Add one switch arm | +6 |
| `Parser.cs` / dispatch | **No change** — catalog lookup handles it | 0 |
| `TypeChecker.cs` | Same validation rules | +20 |
| Language server | Same completion/hover | +15 |
| MCP | Automatic from catalog | 0 |
| Tests | ~8 test cases (no dispatch/parse method tests needed) | +55 |
| Spec + docs | Same | +20 |
| **Total** | | **~152 lines** |

**Savings: ~28 lines, ~2 fewer files touched.** The parser dispatch and per-construct parse method vanish. The slot parser for `VersionNumber` is trivial (~8 lines: expect a number literal, wrap it).

### Under Generic AST + Source Generation

**Files changed:**

| File | Change | Lines |
|------|--------|-------|
| `ConstructKind.cs` | Add `VersionDeclaration` | +1 |
| `TokenKind.cs` | Add `Version` keyword | +1 |
| `Tokens.cs` | Add `TokenMeta` for `Version` | +8 |
| `ConstructSlotKind.cs` | Add `VersionNumber` | +1 |
| `Constructs.cs` | Add `GetMeta(VersionDeclaration)` + shared slot | +12 |
| `VersionDeclarationNode.cs` | **Generated** — 0 hand-written | 0 |
| `BuildNode` arm | **Generated** — 0 hand-written | 0 |
| `_slotParsers` | Add `ParseVersionNumber` slot parser | +8 |
| `TypeChecker.cs` | Same — but accesses `node.Slots[0]` not `node.Version` (if generic AST) | +20 |
| Language server, MCP, Tests, Docs | Same | +90 |
| **Total** | | **~141 lines** |

**Additional savings over catalog-driven: ~11 lines** (the AST record and BuildNode arm). But the TypeChecker code is slightly worse — if using generic AST, slot access is index-based; if using generated AST, it's the same as today.

### Thought Experiment Verdict

| Design | Parser-layer code | Total code | Type safety |
|--------|------------------|-----------|-------------|
| Stub parser (per-construct methods) | ~17 lines | ~180 lines | ✅ Full |
| Catalog-driven (this document) | ~14 lines | ~152 lines | ✅ Full |
| Generic AST + source gen | ~8 lines | ~141 lines | ✅ Full (if generated) / ⚠️ Partial (if generic) |

The `version` construct is intentionally simple. For a complex construct (like `timeout` with state scope, event scope, duration literal, and escalation action), the savings would be proportionally larger in the parser layer but unchanged in the semantic layer. The pattern holds: **the catalog-driven design eliminates parser ceremony; it does not reduce semantic implementation work.**

### A Harder Thought Experiment: `audit` Construct

```precept
in Submitted audit amount, reviewer because "Compliance check required"
```

A state-scoped audit declaration that logs specific fields with a reason. Enters through `In` (disambiguation required), uses existing slot kinds (StateTarget, IdentifierList, BecauseClause).

**Under the catalog-driven design:**

| File | Change | Lines |
|------|--------|-------|
| `ConstructKind.cs` | Add `AuditDeclaration` | +1 |
| `Constructs.cs` | Add `GetMeta(AuditDeclaration)` with Entry `(In, [Audit])` | +12 |
| `TokenKind.cs` + `Tokens.cs` | Add `Audit` keyword | +9 |
| `ConstructSlotKind.cs` | No change — reuses `StateTarget`, `IdentifierList`, `BecauseClause` | 0 |
| `_slotParsers` | No change — all slot parsers already exist | 0 |
| New: `AuditDeclarationNode.cs` | AST record | +5 |
| `BuildNode` | One switch arm | +5 |
| **Parser dispatch** | **Zero change** | 0 |
| **Disambiguation** | **Zero change** — `(In, [Audit])` entry routes automatically | 0 |

**This is the real win.** A scoped construct that reuses existing grammar shapes requires zero parser code changes. The entire dispatch + disambiguation + slot iteration path is catalog-driven. The only parser-layer work is the AST node and the `BuildNode` mapping — both of which are irreducible per-construct code (or generated at scale).

---

## 7. Irreducible Hand-Written Code — The Honest Inventory

Even in the full vision, some code will always be hand-written per construct. Here is the exhaustive list and why each item is irreducible:

### Per-Construct (Always Required)

1. **`ConstructKind` enum member** (~1 line). The kind is the identity of the construct. It cannot be derived.

2. **`GetMeta()` switch arm** (~8-15 lines). The construct's metadata: name, description, example, slot sequence, entries. This IS the construct's definition. It's metadata, not code — but it's hand-authored metadata.

3. **AST node record** (~3-8 lines). The strongly typed output shape for downstream consumers. Typed properties enable pattern matching, IDE navigation, and refactoring. Irreducible because downstream consumers need compile-time guarantees on the shape. (Eliminable via source generation at ~25-30 constructs.)

4. **`BuildNode` switch arm** (~5-8 lines). Maps positional slots to named typed properties. Irreducible because the slot-to-property mapping is construct-specific. (Eliminable via source generation.)

5. **TypeChecker rules** (~15-50 lines). What the construct means semantically — validity constraints, type compatibility, scope rules. Irreducible because this is DOMAIN KNOWLEDGE. No metadata system can express "a version must be a positive integer and appear at most once" without becoming a programming language.

6. **Evaluator semantics** (~10-40 lines). What the construct does at runtime. Same argument as TypeChecker — irreducible domain knowledge.

### Per-New-Grammar-Shape (Only When Introducing New Syntax)

7. **`ConstructSlotKind` enum member** (~1 line). Only if the construct introduces a genuinely new grammar shape.

8. **Slot parser method** (~10-50 lines). The grammar for parsing the new shape. Irreducible because grammar mechanics are hand-written by design — the catalog says WHAT to parse, the slot parser says HOW.

### Never Required Under Catalog-Driven Design

9. **Dispatch code** — catalog lookup handles it.
10. **Disambiguation code** — `DisambiguationEntry` metadata handles it.
11. **Slot iteration code** — generic `ParseConstructSlots` handles it.
12. **Error recovery code** — sync points are catalog-derived.
13. **LS completions for the leading keyword** — catalog-driven.
14. **MCP vocabulary** — catalog-driven.
15. **TextMate grammar classification** — generated from catalog.

### The Bottom Line

**Minimum per-construct cost: ~32-72 lines of hand-written code** (items 1-6), assuming existing grammar shapes.

**With a new grammar shape: ~43-123 lines** (items 1-8).

**Under source generation (future): ~24-58 lines** (items 1-2, 5-6, skip 3-4).

**Theoretical minimum (impossibility floor): items 1-2, 5-6.** The construct's identity, its metadata declaration, its semantic rules, and its runtime behavior. These are the four things that define what a construct IS. Everything else is derivable.

---

## 8. Revised Full Vision Statement

Updated to reflect what Rounds 1-3 have established:

### The Vision

The parser has **zero hardcoded vocabulary.** Every keyword, operator, type name, modifier, action, construct shape, disambiguation path, and sync token is derived from catalog metadata at parser construction time.

The parser's code contains **grammar mechanics** — how to navigate token structure — but never **domain knowledge** — what the language's vocabulary is or how constructs are composed.

### The Architecture (Settled)

| Layer | Mechanism | Coverage |
|-------|-----------|----------|
| A: Vocabulary Tables | Frozen dictionaries from `Operators.All`, `Types.All`, `Modifiers.All`, `Actions.All` | 100% vocabulary |
| B: Dispatch Table | `Constructs.ByLeadingToken` dictionary lookup | 100% dispatch |
| C: Disambiguation | `DisambiguationEntry` metadata + generic `ParseDisambiguated()` | 100% routing |
| D: Slot Iteration | Generic `ParseConstructSlots()` + `_slotParsers` registry | 100% production structure |
| E: Error Recovery | `Constructs.LeadingTokens` sync set | 100% recovery points |
| F: Anchor Derivation | First slot of any disambiguation candidate | 100% anchor type |

**Estimated catalog-driven coverage: ~85-90%.** The remaining 10-15% is:
- Pratt expression loop mechanics
- 15 slot parser method bodies (grammar mechanics per slot kind)
- `BuildNode` switch arms (slot-to-property mapping per construct)
- `ParseActionChain` loop structure and `ParseOutcome` sub-grammar dispatch
- `CreateLeadingTokenSlotNode` factory for the `LeadingTokenSlot` path

### The Design Choices (Locked)

| Decision | Resolution | Rationale |
|----------|-----------|-----------|
| `_slotParsers` | Dictionary (registry pattern) | Keyed dispatch, not exhaustive invariant |
| `BuildNode` | Exhaustive switch + named index constants | CS8509 compile-time enforcement |
| `DisambiguationEntry.LeadingTokenSlot` | `ConstructSlotKind?` on the entry | Handles leading-token-as-slot-value without breaking genericity |
| ActionChain/Outcome boundary | Peek-before-consume in ActionChain | Upholds introduction-token contract |
| Pre-disambiguation `when` guard | Accepted as valid syntax | 4 lines of uniform code, natural English reading |
| `DisambiguationTokens` derivation | Reject — declare explicitly | Routing and grammar are separable concerns |
| Source generation | Not now — design is generator-ready | Break-even at ~25-30 constructs |
| Generic AST | Reject | Downstream consumers need compile-time type safety |
| Migration | Sequenced PRs with bridge property | Catalog shape first, parser follows |

### The Extensibility Guarantee

Under this design, adding a new construct that **reuses existing grammar shapes** requires:
- 3 catalog entries (ConstructKind, GetMeta, TokenKind+TokenMeta)
- 1 AST node record
- 1 BuildNode switch arm
- 0 parser dispatch code
- 0 disambiguation code
- 0 slot iteration code

Adding a new construct that **introduces new grammar shapes** additionally requires:
- 1 ConstructSlotKind member
- 1 slot parser method

This is the irreducible minimum for a hand-written recursive descent parser with strongly typed AST nodes. Source generation can further reduce items 4-5 (AST node + BuildNode arm) when scale justifies the infrastructure.

---

## 9. For George — Round 4

### Push On These

1. **The `ParseConstructBodyWithPreParsedSlots` injection model.** I'm injecting the pre-parsed anchor at index 0 and the guard at the first `GuardClause` slot. George noted this creates coupling between the disambiguator and slot ordering. **Design a test that catches slot-ordering drift** — if a construct's slot sequence changes in the catalog but the injection assumes the old ordering, the test must fail. The named constants in `BuildNode` catch this for node construction; we need an equivalent for the pre-parsed injection path.

2. **The `_slotParsers` exhaustiveness contract.** The slot parser dictionary must have an entry for every `ConstructSlotKind` that appears in any construct's slot sequence. Currently this is an implicit invariant. **Design the analyzer or test that enforces it.** I want a test that iterates `Constructs.All`, extracts every unique `ConstructSlotKind` used in any slot sequence, and verifies `_slotParsers` has an entry for each one.

3. **The `when` guard before disambiguation — sample file.** I accepted `from X when expr on Y -> ...` as valid syntax. **Write a sample that uses both guard positions** and verify the generic disambiguator handles them identically. This is the acceptance test for Decision F4.

4. **The `Entries` population for all 11 constructs.** I showed the table; George verified 4 prepositions. **Write the complete `GetMeta` implementation with `Entries` replacing `LeadingToken`** for all 11 constructs. Surface any issues I missed. This is the first concrete PR artifact.

5. **Concrete slot parser signatures.** I've been hand-waving `Func<SyntaxNode?>` as the slot parser type. George should design the concrete signatures for the 3 hardest slot parsers: `ParseActionChain`, `ParseOutcome`, `ParseExpression(0)`. Specifically: what's the return type? Does `ParseActionChain` return `ActionChainNode?` that gets boxed to `SyntaxNode?`? Is that boxing acceptable? Is there a generic constraint that avoids it?

6. **Source generation feasibility spike.** I said "not now" but "generator-ready." George should validate this claim. **Write a 30-minute spike** that outlines exactly what a Roslyn source generator for `BuildNode` would read (which attributes, which metadata shape) and what it would emit. If the spike reveals that the current catalog shape makes generation awkward, surface that before we finalize the catalog changes in PR 1.

### The Round 4 Goal

Round 4 should produce **PR-ready artifacts**: the concrete `DisambiguationEntry` record, the updated `ConstructMeta` shape, the complete `GetMeta` population with `Entries`, and the `PrimaryLeadingToken` bridge property. If George delivers those, I'll review them as the first draft of PR 1.
