# Architectural Analysis: SemanticSubjects Design Smell

**Author:** Frank (Lead/Architect)
**Date:** 2026-05-12T01:56:47-04:00
**Status:** Analysis complete — awaiting Shane's decision

---

## 1. What the Spec Says

### TypedRule and TypedEnsure (type-checker.md §7.1, lines 326–343)

```csharp
public sealed record TypedRule(
    TypedExpression Condition,
    TypedExpression? Guard,
    TypedExpression Message,
    ImmutableArray<string> SemanticSubjects,  // field names referenced
    ParsedConstruct Syntax
);

public sealed record TypedEnsure(
    ConstraintKind Kind,
    string? AnchorState,
    string? AnchorEvent,
    TypedExpression Condition,
    TypedExpression? Guard,
    TypedExpression Message,
    ImmutableArray<string> SemanticSubjects,
    ParsedConstruct Syntax
);
```

The inline comment on `TypedRule.SemanticSubjects` says **"field names referenced"** — i.e., the field names syntactically referenced in the constraint expression. No further specification exists for this field anywhere in the document.

### ConstraintFieldRefs (type-checker.md §7.1, lines 514–518)

```csharp
public sealed record ConstraintFieldRefs(
    ConstraintIdentity ConstraintIdentity,
    ImmutableArray<string> ReferencedFields,
    ImmutableArray<string> ReferencedArgs
);
```

This is a separate SemanticIndex-level record that captures the same information (field and arg names referenced by a constraint), but organized differently — keyed by `ConstraintIdentity` rather than embedded in the TypedRule/TypedEnsure record. It's specified as a "Dependency Fact" alongside `ComputedFieldDep`.

### SemanticIndex record (type-checker.md §7.1, lines 527–560)

The SemanticIndex carries **both**:
- `ImmutableArray<TypedRule> Rules` — each TypedRule has its own `SemanticSubjects`
- `ImmutableArray<ConstraintFieldRefs> ConstraintRefs` — a separate inventory of the same data, keyed by identity

### W1 open item (type-checker.md §13, line 962)

> Remaining open items: W1 (SemanticSubjects extraction), W2 (NodaTime dispatch refactor — non-blocking), G1–G3 (catalog-driven opportunities — low priority).

W1 is explicitly listed as an open item. The spec acknowledges SemanticSubjects was never implemented.

### Guarantees (type-checker.md §10, lines 897–904)

SemanticSubjects is NOT part of any stated guarantee. The guarantees cover total function, diagnostic completeness, declaration preservation, no-cascade, and determinism. None mention SemanticSubjects or ConstraintRefs population.

---

## 2. What the Code Says

### Construction sites — TypedRule (TypeChecker.cs line 712)

```csharp
ctx.Rules.Add(new TypedRule(condition, guard, message, ImmutableArray<string>.Empty, construct));
```

One construction site. Always `Empty`.

### Construction sites — TypedEnsure (TypeChecker.cs lines 775, 829)

```csharp
// State ensures (line 775)
SemanticSubjects: ImmutableArray<string>.Empty,

// Event ensures (line 829)
SemanticSubjects: ImmutableArray<string>.Empty,
```

Two construction sites. Always `Empty`.

### ConstraintRefs population (CheckContext.cs line 110)

```csharp
public List<ConstraintFieldRefs> ConstraintRefs { get; } = [];
```

Declared. **Never added to.** `ctx.ConstraintRefs.Add(...)` appears zero times in the entire codebase. The list flows into `BuildSemanticIndex` at line 1285 as `ctx.ConstraintRefs.ToImmutableArray()`, producing an always-empty array.

### ProofEngine consumption (ProofEngine.cs lines 1758–1775)

```csharp
private static ImmutableArray<ConstraintInfluenceEntry> ProjectConstraintInfluence(SemanticIndex semantics)
{
    var entries = new List<ConstraintInfluenceEntry>();
    foreach (var cfr in semantics.ConstraintRefs) { ... }
    return entries.ToImmutableArray();
}
```

The ProofEngine iterates `semantics.ConstraintRefs`. Since that array is always empty, the loop body never executes. **`ConstraintInfluenceEntry` is always empty.** The proof engine's S10 stage is dead code.

### Consumer search — complete results

| Consumer | Reads `SemanticSubjects`? | Reads `ConstraintInfluence`? |
|---|---|---|
| TypeChecker | No | N/A (runs before proof) |
| ProofEngine | No | Produces it — but always empty |
| GraphAnalyzer | No | No |
| Evaluator | No | No |
| PreceptBuilder | No | No (would consume, but nothing to consume) |
| Language Server | No | No |
| MCP tools | No | No |
| Tests | No (assert construction only) | Assert it's **empty** (see below) |

### Test coverage (ProofEngineTests.cs)

Eight proof engine tests explicitly assert `ConstraintInfluence.Should().BeEmpty()` with the note:

> "ConstraintRefs is not yet populated by the TypeChecker"

The tests **document the known gap** rather than exercising the feature.

---

## 3. SemanticSubjects vs. ConstraintInfluenceEntry — Comparison

| Aspect | `TypedRule.SemanticSubjects` | `ConstraintFieldRefs` | `ConstraintInfluenceEntry` |
|---|---|---|---|
| **Location** | Embedded in TypedRule/TypedEnsure record | SemanticIndex dependency facts | ProofLedger output |
| **Identity** | Implicit (belongs to parent record) | Explicit `ConstraintIdentity` DU | Explicit `ConstraintIdentity` DU |
| **Fields** | `ImmutableArray<string>` | `ImmutableArray<string> ReferencedFields` | `ImmutableArray<string> ReferencedFields` |
| **Args** | Not separated | `ImmutableArray<string> ReferencedArgs` | `ImmutableArray<EventArgReference> ReferencedArgs` |
| **Pipeline stage** | Type checker output | Type checker output | Proof engine output |
| **Populated** | ❌ Never | ❌ Never | ❌ Never (depends on ConstraintFieldRefs) |
| **Consumers** | Zero | ProofEngine (dead loop) | Hover design specifies it; zero actual consumers today |

**Key finding:** `SemanticSubjects` is a **subset** of `ConstraintFieldRefs`. SemanticSubjects carries only field names; ConstraintFieldRefs carries field names AND arg names. They represent the same data at the same pipeline stage — the type checker — but in different shapes. `ConstraintInfluenceEntry` is the proof engine's enriched projection of `ConstraintFieldRefs`, where bare arg names become event-qualified `EventArgReference` records.

**The design has the same data specified in three places, populated in zero.**

---

## 4. Root Cause — Why Is It Empty?

### It was explicitly deferred

W1 is listed as an open item in the spec's §13. The decisions archive (lines 49091–49099, 49597–49601) records this as a known gap from the original implementation:

> W1: `PopulateRules` line 940: `ImmutableArray<string>.Empty` is passed for `SemanticSubjects`. The spec defines these as "field names referenced" — they should be extracted from the resolved `Condition` expression tree by walking `TypedFieldRef` nodes.

> W4: `ctx.ConstraintRefs` is wired into `BuildSemanticIndex` but nothing ever adds to it.

This was **not** an intentional replacement by ConstraintInfluenceEntry. The implementation sequence was:

1. Pre-Slice 0 committed the record shapes (including SemanticSubjects on TypedRule/TypedEnsure and ConstraintFieldRefs on SemanticIndex)
2. Slices 1–10 implemented all expression resolution, constraint normalization, etc.
3. W1 (SemanticSubjects extraction) and W4 (ConstraintRefs population) were deferred as non-blocking follow-ups
4. The proof engine's S10 was implemented to read ConstraintRefs, but since ConstraintRefs was never populated, S10 is functionally inert
5. No one came back to implement W1 or W4

### There was no conscious replacement

The proof engine spec (proof-engine.md §Decision 4, line 1805) explicitly describes the designed data flow:

> **Decision:** The proof engine produces `ConstraintInfluenceEntry` records as part of its output, by reading `SemanticIndex.ConstraintRefs` (populated by the TypeChecker) and projecting them into the richer `ConstraintInfluenceEntry` shape.

The design was always: TypeChecker populates ConstraintRefs → ProofEngine projects into ConstraintInfluenceEntry. The first step never happened, so the second step runs but produces nothing.

---

## 5. The Design Confusion

**Yes, there is a design confusion — and it's worse than "two names for the same thing."**

The problem is that the same conceptual data was specified in **two independent locations within the same pipeline stage** and **neither was implemented**:

1. **`TypedRule.SemanticSubjects`** — embedded directly on the constraint record, available inline when you have a TypedRule in hand. Field names only.
2. **`SemanticIndex.ConstraintRefs`** — a separate dependency-facts inventory on the SemanticIndex, keyed by ConstraintIdentity. Field names AND arg names.

These represent **two different access patterns for the same underlying data**:
- SemanticSubjects = "I have a rule; what fields does it touch?" (navigational, per-record)
- ConstraintRefs = "Give me a table of all constraint→field dependencies" (analytical, cross-cutting)

Both are syntactic extraction (available at type-check time). Neither is causal/runtime influence. ConstraintInfluenceEntry adds event-qualified arg identity on top, which is proof-engine enrichment, but the base field data is identical.

### Are "fields syntactically referenced" and "fields causally influenced" different?

**Yes, but only in theory.** In Precept's current constraint model, every field referenced in a constraint condition IS a field that influences whether the constraint is satisfied. There's no indirection layer (no computed intermediaries that could be referenced without being influential). `rule Balance >= 0` references `Balance` syntactically and `Balance` is the field whose value determines constraint satisfaction.

This could diverge if Precept ever adds:
- Constraint expressions that reference a field for computation but aren't "influenced by" it in the causal sense (unlikely given the DSL's semantic model)
- Aggregate or derived expressions where the syntactic reference set differs from the causal influence set

For now, they're the same data. The proof engine's enrichment step (bare arg name → EventArgReference) is the only value-add.

---

## 6. Recommended Path Forward: Option B+A Hybrid

### Remove `SemanticSubjects` from TypedRule and TypedEnsure. Populate `ConstraintRefs`.

**Rationale:**

1. **SemanticSubjects is redundant with ConstraintRefs.** ConstraintRefs carries strictly more information (field names + arg names + constraint identity). SemanticSubjects carries only field names with no identity key. Any consumer that needs "what fields does this rule touch" can look up `ConstraintRefs` by `RuleIdentity(ruleIndex)`.

2. **SemanticSubjects has the wrong shape.** It doesn't separate fields from args. It has no identity key. It's embedded in the record rather than being a cross-cutting dependency fact. The ConstraintFieldRefs shape is the correct shape for this data.

3. **ConstraintRefs is the designed input to ConstraintInfluenceEntry.** The spec explicitly chains TypeChecker→ConstraintRefs→ProofEngine→ConstraintInfluenceEntry. Populating ConstraintRefs unblocks the entire chain with zero design changes.

4. **Zero consumers break.** Nothing reads SemanticSubjects. Removing it is a mechanical deletion with no behavioral impact.

### Concrete changes:

#### Source changes

**A. Remove SemanticSubjects from TypedRule (SemanticIndex.cs line 377)**
```csharp
// BEFORE
public sealed record TypedRule(
    TypedExpression Condition,
    TypedExpression? Guard,
    TypedExpression Message,
    ImmutableArray<string> SemanticSubjects,
    ParsedConstruct Syntax
);

// AFTER
public sealed record TypedRule(
    TypedExpression Condition,
    TypedExpression? Guard,
    TypedExpression Message,
    ParsedConstruct Syntax
);
```

**B. Remove SemanticSubjects from TypedEnsure (SemanticIndex.cs line 389)**
```csharp
// BEFORE (showing only the change)
    ImmutableArray<string> SemanticSubjects,
// AFTER
    // (field removed)
```

**C. Remove from construction sites (TypeChecker.cs lines 712, 775, 829)**
Delete the `SemanticSubjects: ImmutableArray<string>.Empty,` argument at all three sites.

**D. Populate ConstraintRefs in PopulateRules and PopulateEnsures (TypeChecker.cs)**

After building each TypedRule, walk the resolved `Condition` expression tree to extract `TypedFieldRef` names and `TypedArgRef` names, then add a `ConstraintFieldRefs` entry:

```csharp
// In PopulateRules, after ctx.Rules.Add:
var fields = CollectFieldRefs(condition).ToImmutableArray();
var args = CollectArgRefs(condition).ToImmutableArray();
ctx.ConstraintRefs.Add(new ConstraintFieldRefs(
    new RuleIdentity(ctx.Rules.Count - 1),
    fields,
    args));
```

Same pattern for each ensure.

**E. Implement the expression-tree walkers:**

```csharp
private static IEnumerable<string> CollectFieldRefs(TypedExpression expr) => expr switch
{
    TypedFieldRef f => [f.FieldName],
    TypedBinaryOp b => CollectFieldRefs(b.Left).Concat(CollectFieldRefs(b.Right)),
    TypedUnaryOp u => CollectFieldRefs(u.Operand),
    TypedParenthesized p => CollectFieldRefs(p.Inner),
    TypedFunctionCall fc => fc.Arguments.SelectMany(CollectFieldRefs),
    TypedMethodCall mc => CollectFieldRefs(mc.Receiver).Concat(mc.Arguments.SelectMany(CollectFieldRefs)),
    TypedMemberAccess ma => CollectFieldRefs(ma.Target),
    TypedQuantifier q => CollectFieldRefs(q.Collection).Concat(CollectFieldRefs(q.Predicate)),
    TypedIsSetCheck c => CollectFieldRefs(c.Target),
    TypedConditional c => CollectFieldRefs(c.Condition)
        .Concat(CollectFieldRefs(c.WhenTrue)).Concat(CollectFieldRefs(c.WhenFalse)),
    _ => []
};

// Same pattern for CollectArgRefs, matching TypedArgRef instead.
```

The walker must be exhaustive over TypedExpression subtypes and deduplicate results.

**F. Update ProofEngine tests** — the eight tests asserting `ConstraintInfluence.Should().BeEmpty()` with "not yet populated" notes should flip to positive assertions.

#### Spec changes

**G. Update type-checker.md §7.1:**
- Remove `SemanticSubjects` from TypedRule and TypedEnsure record definitions (lines 330, 341)
- Add a note in §13 marking W1 as resolved: "W1: Resolved — SemanticSubjects removed from TypedRule/TypedEnsure; ConstraintRefs populated instead."

**H. Update proof-engine.md:**
- Remove the design note about ConstraintRefs not being populated (line 1318)
- Update Decision 4 rationale to note that this is now implemented

#### What does NOT change

- `ConstraintFieldRefs` record shape — stays as-is
- `ConstraintInfluenceEntry` record shape — stays as-is
- `ProofEngine.ProjectConstraintInfluence()` — already correct, just needs non-empty input
- `ProofLedger` shape — stays as-is
- GraphAnalyzer — doesn't consume either field
- Evaluator — doesn't consume either field
- MCP tools — no changes needed

---

## 7. Impact on Hover v3

### Elaine's design is correct

The hover design (docs/Working/hover-design.md) explicitly specifies:

> Use `ConstraintInfluenceEntry` for the "Referenced fields" line, NOT `TypedRule.SemanticSubjects` (currently empty — Kramer N10)

This routing decision was architecturally sound — `ConstraintInfluenceEntry` IS the right long-term data source for referenced fields in hover. Once ConstraintRefs is populated, ConstraintInfluenceEntry will contain the data the hover needs.

### What Kramer needs to know

1. **No hover implementation change needed.** The data source Elaine specified (ConstraintInfluenceEntry) is the correct one. Once ConstraintRefs is populated, hover's referenced-fields line will light up automatically.

2. **Kramer should NOT add workaround code** to walk expression trees directly from hover. The fix belongs in the TypeChecker, not the LanguageServer.

3. **The "currently empty" caveat in Kramer's history** (line 43) will be resolved by this fix. After the TypeChecker populates ConstraintRefs, the entire chain activates: TypeChecker → ConstraintRefs → ProofEngine → ConstraintInfluenceEntry → Hover.

4. **Sequencing:** This fix can ship independently of hover v3. If it ships first, hover gets the data immediately. If hover ships first, the "Referenced fields" line will simply be empty until this fix lands — which is the current behavior anyway.

---

## 8. Pipeline Dependency Analysis

> Does anything need "fields referenced by this constraint" at type-check time?

**Not today.** The type checker does not use SemanticSubjects or ConstraintRefs for any validation, disambiguation, or diagnostic. The type checker validates constraint expressions structurally (type compatibility, field existence, operator resolution) without needing to know the aggregate set of referenced fields.

**Could it need it?** Theoretically, if we added cross-constraint validation (e.g., "ensure constraints don't create contradictions" or "warn if a rule references fields that are never writable"), that would need constraint→field dependency data at type-check or graph-analysis time. But:
- Contradiction detection is a proof-engine concern, and the proof engine already has the data (once populated)
- Writability analysis is a graph-analyzer concern, and it could read ConstraintRefs from SemanticIndex

The pipeline dependency story is clean: **ConstraintRefs (type checker output) serves the proof engine and graph analyzer. ConstraintInfluenceEntry (proof engine output) serves downstream consumers like hover.** No stage needs this data earlier than it's available.

---

## 9. Summary

| Finding | Detail |
|---|---|
| **What smells** | Same data specified in 3 places (SemanticSubjects, ConstraintRefs, ConstraintInfluenceEntry), populated in 0 |
| **Root cause** | W1/W4 deferred during implementation, never revisited |
| **Is it a design problem?** | Partially — SemanticSubjects on the record is redundant with ConstraintRefs on the index |
| **Is it a conceptual confusion?** | No — syntactic reference and causal influence are the same thing in current Precept |
| **Is there a missed implementation?** | Yes — ConstraintRefs was always intended to be populated |
| **Recommendation** | Remove SemanticSubjects (dead redundant field), populate ConstraintRefs (the designed path) |
| **Hover v3 impact** | None — Elaine's design already routes to the correct data source |
| **Risk** | Low — zero consumers of SemanticSubjects, mechanical expression-tree walk for ConstraintRefs |
