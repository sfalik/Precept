# ConstraintRefs Population & SemanticSubjects Removal — Implementation Plan

**Author:** Frank (analysis), George (implementation)
**Status:** Approved — implement in full, no deferrals
**Date:** 2026-05-12

---

## Problem

The same conceptual data ("which fields and args does this constraint reference?") was specified in three places in the pipeline, and populated in zero:

1. **`TypedRule.SemanticSubjects`** / **`TypedEnsure.SemanticSubjects`** — embedded on the constraint record. Always `ImmutableArray<string>.Empty`. Zero consumers.
2. **`SemanticIndex.ConstraintRefs`** (`ImmutableArray<ConstraintFieldRefs>`) — the designed TypeChecker output. `ctx.ConstraintRefs.Add(...)` appears zero times in the codebase. Always empty.
3. **`ProofLedger.ConstraintInfluence`** (`ImmutableArray<ConstraintInfluenceEntry>`) — ProofEngine S10 projection of ConstraintRefs. Loop never executes because ConstraintRefs is empty. Always empty.

**Root cause:** W1 and W4 were deferred during the original implementation slices and never revisited.

**Impact on hover v3:** Elaine's design correctly routes to `ConstraintInfluenceEntry` for the "Referenced fields" hover line. The data source is right; the data is missing. Fixing ConstraintRefs activates the entire chain automatically.

---

## What Does NOT Change

- `ConstraintFieldRefs` record shape — stays as-is
- `ConstraintInfluenceEntry` record shape — stays as-is
- `ProofEngine.ProjectConstraintInfluence()` — already correct, just needs non-empty input
- `ProofLedger` shape — stays as-is
- GraphAnalyzer, Evaluator, MCP tools — no changes needed

---

## Implementation — All Required, No Deferrals

### A. Remove `SemanticSubjects` from `TypedRule` (SemanticIndex.cs ~line 377)

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

### B. Remove `SemanticSubjects` from `TypedEnsure` (SemanticIndex.cs ~line 389)

Same pattern — remove the `ImmutableArray<string> SemanticSubjects` field.

### C. Remove from all construction sites (TypeChecker.cs ~lines 712, 775, 829)

Delete `SemanticSubjects: ImmutableArray<string>.Empty,` at all three construction sites.

### D. Populate ConstraintRefs after each constraint is built (TypeChecker.cs)

In `PopulateRules`, after `ctx.Rules.Add(...)`:

```csharp
var fields = CollectFieldRefs(condition).Distinct().ToImmutableArray();
var args   = CollectArgRefs(condition).Distinct().ToImmutableArray();
ctx.ConstraintRefs.Add(new ConstraintFieldRefs(
    new RuleIdentity(ctx.Rules.Count - 1),
    fields,
    args));
```

Same pattern in `PopulateEnsures` for state ensures and event ensures, using the appropriate `ConstraintIdentity` subtype (`StateEnsureIdentity` / `EventEnsureIdentity`).

### E. Implement the expression-tree walkers

```csharp
private static IEnumerable<string> CollectFieldRefs(TypedExpression expr) => expr switch
{
    TypedFieldRef f         => [f.FieldName],
    TypedBinaryOp b         => CollectFieldRefs(b.Left).Concat(CollectFieldRefs(b.Right)),
    TypedUnaryOp u          => CollectFieldRefs(u.Operand),
    TypedParenthesized p    => CollectFieldRefs(p.Inner),
    TypedFunctionCall fc    => fc.Arguments.SelectMany(CollectFieldRefs),
    TypedMethodCall mc      => CollectFieldRefs(mc.Receiver).Concat(mc.Arguments.SelectMany(CollectFieldRefs)),
    TypedMemberAccess ma    => CollectFieldRefs(ma.Target),
    TypedQuantifier q       => CollectFieldRefs(q.Collection).Concat(CollectFieldRefs(q.Predicate)),
    TypedIsSetCheck c       => CollectFieldRefs(c.Target),
    TypedConditional c      => CollectFieldRefs(c.Condition)
                                   .Concat(CollectFieldRefs(c.WhenTrue))
                                   .Concat(CollectFieldRefs(c.WhenFalse)),
    _                       => []
};

// Same pattern for CollectArgRefs, matching TypedArgRef instead of TypedFieldRef.
```

**The walker MUST be exhaustive** — every TypedExpression subtype must have a branch. Use the existing switch exhaustiveness pattern. Deduplicate results at the call site (`.Distinct()`).

### F. Update ProofEngine tests (ProofEngineTests.cs)

Eight tests currently assert `ConstraintInfluence.Should().BeEmpty()` with a "not yet populated" note. After this fix:

- Flip each test to assert the correct non-empty `ConstraintInfluenceEntry` content based on the precept under test.
- Remove the "not yet populated" comments.
- These tests are the behavioral acceptance gate for this change — they must pass.

### G. Update spec docs

**type-checker.md §7.1:**
- Remove `SemanticSubjects` from the `TypedRule` and `TypedEnsure` record definitions
- Add to §13 open items: `W1: Resolved — SemanticSubjects removed; ConstraintRefs populated via expression-tree walkers in PopulateRules/PopulateEnsures.`
- Same for W4.

**proof-engine.md:**
- Remove the design note that ConstraintRefs is not yet populated (~line 1318)
- Update Decision 4 rationale to note this is now implemented

---

## Acceptance Criteria

- [ ] `TypedRule` has no `SemanticSubjects` field
- [ ] `TypedEnsure` has no `SemanticSubjects` field
- [ ] `ctx.ConstraintRefs.Add(...)` is called after every `Rules.Add`, `StateEnsures.Add`, and `EventEnsures.Add`
- [ ] `CollectFieldRefs` and `CollectArgRefs` are exhaustive over all TypedExpression subtypes
- [ ] All 8 "BeEmpty" ProofEngine tests converted to positive assertions and passing
- [ ] `dotnet test test/Precept.Tests/` passes clean
- [ ] type-checker.md W1/W4 marked resolved
- [ ] proof-engine.md "not yet populated" note removed

---

## Key Files

| File | Change |
|---|---|
| `src/Precept/Pipeline/SemanticIndex.cs` | Remove SemanticSubjects from TypedRule (~377) and TypedEnsure (~389) |
| `src/Precept/Pipeline/TypeChecker.cs` | Remove 3 construction sites; add ConstraintRefs population; add walkers |
| `test/Precept.Tests/` ProofEngineTests.cs | Flip 8 BeEmpty assertions to positive |
| `docs/compiler/type-checker.md` | Remove SemanticSubjects from record specs; mark W1/W4 resolved in §13 |
| `docs/compiler/proof-engine.md` | Remove "not yet populated" note; update Decision 4 |
