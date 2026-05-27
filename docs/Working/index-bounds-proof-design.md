---
status: Locked 2026-05-26
phase-target: Phase 4 W-E
comparable-systems-research-status: partial — Liquid Haskell precedent cited from existing `research/architecture/compiler/proof-attribution-witness-design-survey.md`; inline excerpts cover Roslyn's per-parameter-as-subject pattern via in-tree code reads. No `Stakes: irreversible` decisions; gate satisfied by inline survey discipline.
feature-gates: F-LANG-COLL-04, F-LANG-COLL-09
related-bugs: (none — net-new obligation, no field workaround in samples)
sources-consulted:
  - `src/Precept/Language/ProofRequirement.cs:1-95` — ProofSubject + NumericProofRequirement DU and the `ParamSubject(ParameterMeta)` framework
  - `src/Precept/Language/Operations.cs:96-132` — IntegerDivideInteger as ParamSubject precedent (divisor obligation)
  - `src/Precept/Language/Types.cs:194-302` — current `.at(N)` accessor catalog entries on Log / LogBy / List
  - `src/Precept/Language/Actions.cs:66-180,233-298` — existing Insert/RemoveAt action catalog + DynamicObligationGenerator pattern for IntervalContainment
  - `src/Precept/Pipeline/ProofEngine.cs:29-50,412-478` — GuardConstraint records + ResolveParam family
  - `src/Precept/Pipeline/ProofEngine.Diagnostics.cs:122-203` — IndexBoundsGuard site-shape dispatch vs obligation-kind dispatch contradiction
  - `src/Precept/Language/Diagnostics.cs:560-565,983-992` — UnguardedCollectionAccess (PRE0063) and IndexBoundsGuard (PRE0100) diagnostic entries
  - `src/Precept/Pipeline/SemanticIndex.cs:81-95` — TypedMemberAccess record with ChoiceMetadata slot from W-G
  - `src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs:1050-1141` — ResolveMethodCall (where resolvedArgs are consumed and dropped today)
  - `samples/shopping-cart.precept:185-205` — the single sample-corpus consumer of `.at` / `insert at` / `remove at`
  - `docs/language/precept-language-spec.md § 0.1` — eleven design principles
  - `docs/language/precept-language-spec.md § 0.6` — Proof Engine Design Contract; Soundness over completeness; Proven violations only
  - `research/architecture/compiler/proof-attribution-witness-design-survey.md § Liquid Haskell` — refinement-type precedent for index-bounds proof
---

# Index bounds proof obligation — `.at(N)`, `insert at N`, `remove at N`

## Goal

When this design lands, `.at(N)`, `insert F E at N`, and `remove F at N`
emit a proof obligation that the index N is within bounds, and that
obligation discharges from an author-written guard (`when N >= 0 and
N < F.count`) — exactly the way the existing `self.count > 0` obligation
discharges today for `.first` / `.last` / `Dequeue`.

A passing test for "this works": `samples/shopping-cart.precept` continues
to compile clean (its existing `when ReorderItem.FromIndex < LineItems.count`
guards discharge the new obligation), and `precept_compile` on a stripped
version without those guards emits `PRE0100 IndexBoundsGuard` at each
unguarded site.

## Scope

**In scope.**

- Static catalog declaration of `IndexBoundsProofRequirement` on `.at(N)`
  accessors (Log / LogBy / List) and on Insert / RemoveAt actions.
- `ParamSubject(IndexParam)` framework extension to expose accessor and
  action parameters as `ParameterMeta` instances, so the subject resolution
  uses the existing per-parameter framework (consistent with `Divide`'s
  divisor handling).
- `Arguments: ImmutableArray<TypedExpression>` slot on `TypedMemberAccess`
  so `.at(N)`'s N survives type-checking — continuation of the W-G
  `ChoiceMetadata` extension to the same record.
- Guard-constraint extractor extension recognizing `<paramExpr> <op>
  <field>.count` shapes (today the extractor only recognizes `field op
  literal` and `collection.count op literal`).
- Discharge strategy that walks each guard branch independently for both
  lower-bound (N >= 0) and upper-bound (N < or <= F.count) proofs.
- Diagnostic consolidation: reuse `PRE0100 IndexBoundsGuard` (which already
  exists and is routed for `.at(N)` sites at `ProofEngine.Diagnostics.cs:198`)
  with a refined message that actually describes the full bounds requirement.

**Out of scope.**

- Symbolic interval narrowing (e.g., deriving `N < count` from `N == count - 1`).
  Out of scope; the discharge matches structural guard shapes only.
- `.at(F.count - 1)` and other compound index expressions where the index
  is not a bare reference. Structural match requires the same TypedExpression
  shape at both the obligation site and the guard.
- The non-empty obligation on `.at(N)` (`self.count > 0`). It stays; this
  design adds the index-bounds obligation *alongside* the non-empty one,
  not in place of it.

**Deferred to future.**

- Quantifier-binding-as-index (`each i in IndexSet (List.at(i))`). The
  binding `i` carries its own narrowing context — that's a separate proof
  surface tracked in `docs/compiler/proof-engine.md` § quantifier narrowing.
  Without it, the natural form produces an unresolved obligation with the
  message *"index 'i' must be within bounds [0, F.count)"* — actionable
  (the author can rewrite to `when i < F.count`) but not as ergonomic as
  the fully-narrowed version. Acceptable for the V1 ship.

## Philosophy Alignment

| Principle | Affected? | How served | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | Compile-time obligation prevents index-out-of-bounds configurations before they reach runtime (spec § 0.1 #1) | N/A | N/A |
| 2. One file, complete rules | Y | The author writes the guard in the same `.precept` file as the access; no external bounds-check service | N/A | N/A |
| 3. Determinism | Y | Same precept + same data ⇒ same proof verdict (structural guard match, no SMT) | N/A | N/A |
| 4. Full inspectability | Y | Unresolved obligations surface in `precept_compile` diagnostics + proof ledger; hover can show the obligation | N/A | N/A |
| 5. Keyword-anchored readability | N/A | No new keyword / grammar surface — only obligation generation and discharge | N/A | N/A |
| 6. Governance not validation | Y | The bounds are enforced structurally on every `.at(N)` access path, not by a runtime check that some code paths might skip | N/A | N/A |
| 7. Compile-time totality | Y | Closes a gap where `.at(99)` on a 5-element list compiled clean — the type system was claiming totality it didn't deliver (spec § 0.4) | The current scaffolding declares `self.count > 0` as the only obligation; adding the upper bound is the second half of the totality story | One-time author migration cost — existing samples that use `.at` need bounds guards. Only `shopping-cart.precept` is affected and already has the right guard |
| 8. Honesty about approximation | N/A | No approximation involved | N/A | N/A |
| 9. Mandatory rationale (`because`) | N/A | Obligation discharge, not rule authoring | N/A | N/A |
| 10. Static semantic checking | Y | Direct fulfillment — this is the static check that makes `.at(N)` safe (spec § 0.1 #10) | N/A | N/A |
| 11. Static completeness | Y | Direct fulfillment — closes the runtime fault path `CollectionIndexOutOfBounds` by making it compile-time-rejectable (spec § 0.1 #11) | N/A — the obligation generation makes this path unreachable for well-typed programs | The runtime `CollectionEmptyOnAccess` fault is now defensive redundancy as designed; failure to discharge produces a compile error, not a runtime exception |

**Tradeoffs accepted.** Principle 7's "totality" claim is partially served by the existing `self.count > 0` obligation, which proves the collection is non-empty but not that the specific index N is in range. The current state lies about the totality claim — `.at(99)` on a 5-element list compiles clean even though it will panic at runtime. This design accepts the migration cost (one sample, two action-catalog declarations, three accessor-catalog declarations) to honor the totality contract precept-language-spec.md § 0.1 #7 actually makes.

**Companion commitments.** Stateless-first-class: unaffected — bounds proofs work identically on stateless precepts and stateful ones. Domain-expert-primary-author: served — the discharge story is "write the bounds guard you'd already write in plain English," not "invoke a refinement-type annotation."

## Language Design Grounding

This design touches *diagnostic surface* but not language *syntax* — `.at(N)`,
`insert at N`, and `remove at N` are unchanged. The author-visible change is
that some previously-passing programs now require an explicit bounds guard.
Per the skill's trigger criteria, language-surface design grounding is
partially required (error messages are author-visible).

**General language design — index-bounds proof obligations.** Three
comparable systems handle parameterized-index safety:

- **Liquid Haskell** (refinement types). `LiquidArray.elemAt :: { v: Vector a | 0 <= i && i < vlen v } -> Int -> a`.
  The bound is encoded in the type; the SMT solver (Z3 via liquid-fixpoint)
  discharges it. Cited:
  `research/architecture/compiler/proof-attribution-witness-design-survey.md § Liquid Haskell — "Liquid Haskell runs as a GHC compiler plugin … LH generates Horn clause constraints for each refinement type check … the SMT solver internally finds a model violating the subtype relation, but this is not surfaced in the diagnostic message."`
  Precept deliberately diverges from refinement types + SMT discharge for
  the reason spec § 0.6 #3 names: opaque solvers are excluded on principle.

- **F\*** (refinement types + tactic-mode proofs). Similar to Liquid
  Haskell; refinement-type discharge via Z3 with the option of explicit
  tactic-mode proofs that the author writes by hand.

- **Rust** (panic-based runtime bounds). Indexing `v[i]` traps at runtime
  if i is out of bounds; `v.get(i)` returns `Option<T>`. No compile-time
  proof; the static analysis is left to MIRI for verification or to
  optimization passes for elimination.

**Precept's positioning.** Precept proves index bounds at compile time
*and* refuses an opaque solver. The discharge is via author guards
(`when N < F.count`), discoverable by structural pattern-match. The author
writes a guard in the same language they author the rest of the precept
in; the proof engine matches the guard to the obligation by inspection,
not by Horn-clause solving. This is the language design choice spec § 0.6
#3 commits to: legibility over completeness.

**Precept-specific application.** Touches Principle 7 (compile-time
totality) and Principle 10/11 (static semantic checking + static
completeness). The current `.at(N)` accessor declares only the non-empty
obligation, leaving the upper bound as a runtime gap. This design closes
that gap.

## Audience and Teachability

**Worked example** (an inventory-management precept):

```precept
precept InventoryAdjustment
field Adjustments as list of integer
field LookupIndex as integer default 0 nonnegative
field LastAdjustment as integer default 0

state Open initial
state Closed terminal

event ReviewAdjustment(Index as integer)
from Open on ReviewAdjustment
    when ReviewAdjustment.Index >= 0
     and ReviewAdjustment.Index < Adjustments.count
    -> set LookupIndex = ReviewAdjustment.Index
    -> set LastAdjustment = Adjustments.at(ReviewAdjustment.Index)
    -> no transition
from Open on ReviewAdjustment
    -> reject "Index '{ReviewAdjustment.Index}' is out of range for the {Adjustments.count}-item adjustment list"
```

The first row has a guard that discharges the bounds obligation. The
second row catches all other cases with a domain-expert-readable reject.

**Error message** (when the bounds guard is missing):

```
PRE0100  'Adjustments' access at index 'ReviewAdjustment.Index' is not bounds-checked —
         add `when ReviewAdjustment.Index >= 0 and ReviewAdjustment.Index < Adjustments.count`
         to prove the index is in range.
```

The wording targets a domain expert: the message names the field and the
index expression in source terms, not in terms of compiler internals.
"Bounds-checked" is plain English; the recovery hint is the literal guard
the author writes.

**10-minute teaching path.**

1. `docs/language/collection-types.md § .at(N)` — what the accessor does (2 min)
2. `samples/shopping-cart.precept:185-205` — read the existing bounds-guarded
   `ReorderItem` row as the canonical worked example (3 min)
3. `docs/language/precept-language-spec.md § 0.6 Proof Engine Design Contract` —
   the proven-violations-only philosophy that frames the diagnostic (3 min)
4. `docs/compiler/proof-engine.md § Index bounds discharge` — the structural
   match the engine performs against the guard (2 min)

## Semantic Rules

**Obligation generation rule** (for `.at(N)` accessor):

```
  Γ ⊢ E : C    C ∈ { Log<T>, LogBy<T,P>, List<T> }    Γ ⊢ N : Integer
  ─────────────────────────────────────────────────────────────────────
  Γ ⊢ E.at(N) : T  with obligations
       { NonEmpty: SelfSubject(count) > 0,
         IndexBounds: ParamSubject(IndexParam), StrictlyBefore, count }
```

**Obligation generation rule** (for `insert F E at N`):

```
  Γ ⊢ F : List<T>    Γ ⊢ E : T    Γ ⊢ N : Integer
  ─────────────────────────────────────────────────
  Γ ⊢ insert F E at N  with obligations
       { IndexBounds: ParamSubject(IndexParam), AtOrBefore, F.count }
```

(`AtOrBefore` because position `N == F.count` is a valid insert location —
it appends to the end.)

**Obligation generation rule** (for `remove F at N`):

```
  Γ ⊢ F : List<T>    Γ ⊢ N : Integer
  ──────────────────────────────────────
  Γ ⊢ remove F at N  with obligations
       { IndexBounds: ParamSubject(IndexParam), StrictlyBefore, F.count }
```

**Discharge rule** (lower bound):

```
  guard contains constraint  `N >= 0`  OR
  guard contains constraint  `0 <= N`   OR
  field declaration of N includes ModifierKind.Nonnegative (transitively
    via type-system non-negativity)
  ──────────────────────────────────────────────────────────────────────
  IndexBoundsProofRequirement(N, _, _).LowerBound  ⊢ Proved
```

**Discharge rule** (upper bound, StrictlyBefore):

```
  guard contains constraint  `N < F.count`
  ──────────────────────────────────────────
  IndexBoundsProofRequirement(N, StrictlyBefore, count).UpperBound  ⊢ Proved
```

**Discharge rule** (upper bound, AtOrBefore):

```
  guard contains constraint  `N <= F.count`  OR
  guard contains constraint  `N < F.count`  (strict implies non-strict)
  ─────────────────────────────────────────────
  IndexBoundsProofRequirement(N, AtOrBefore, count).UpperBound  ⊢ Proved
```

**Branch independence.** In a disjunctive guard `(P) or (Q)`, both P
and Q must independently establish both bounds; the discharge does not
distribute across OR (consistent with `ProofEngine.Strategies.cs:334-352`).

**Soundness preservation claim.**

- **Principle 7 (compile-time totality)**: any precept that compiles
  without diagnostics now has a proof that every `.at(N)` site is in
  range, closing the gap the current scaffolding leaves open.
- **Principle 10 (static semantic checking)**: the new obligation is
  semantic — it relates the index parameter to the receiver's count.
  The discharge is checked at compile time; runtime fault paths become
  defensive redundancy as designed.
- **Principle 11 (static completeness)**: the `CollectionIndexOutOfBounds`
  fault is now compile-time-rejectable. Existing runtime emission of
  `CollectionEmptyOnAccess` fault stays as defense in depth (per
  `precept-language-spec.md § 0.1 #11 — "Runtime fault checks exist only
  as defensive redundancy, never as the primary enforcement mechanism"`).

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The obligation declaration lives in the **catalog**
(`Types.cs` for accessors, `Actions.cs` for actions) per the catalog-first
discipline at `CLAUDE.md § Catalog System (Non-Negotiable) — "A new keyword,
type, operator, modifier, or construct goes in the appropriate catalog
entry first."` The discharge strategy lives in the **proof engine pipeline
stage** (`ProofEngine.Strategies.cs`) where guard analysis already lives.
The diagnostic routing lives in the **proof engine diagnostics layer**
(`ProofEngine.Diagnostics.cs`).

Why catalog and not pipeline-only? Because the obligation is a *property
of the accessor/action*, not a property of any individual call site. Two
calls to `.at(N)` share the same obligation shape; the only thing that
varies per-call is the actual N TypedExpression, which the discharge
strategy resolves via the `ParamSubject` framework (same way Divide's
divisor obligation is resolved).

**Cross-component propagation.**

- **Runtime** (parser, type checker, evaluator, diagnostics): parser
  unchanged. Type checker passes the actual arguments to TypedMemberAccess
  (via the new `Arguments` slot — continuation of W-G's ChoiceMetadata
  pattern). Evaluator unchanged at this slice — runtime bounds checking
  remains in place as defensive redundancy. Diagnostics: PRE0100 message
  refined.
- **Tooling** (syntax highlighting, completions, hover, semantic tokens):
  hover on a TypedMemberAccess could surface the new obligation alongside
  the existing one (future enhancement, not in this slice). Completions
  unchanged. Semantic tokens unchanged.
- **MCP** (vocabulary, DTOs, tool output): `precept_compile` output
  includes the new obligation in the proof-ledger. No new tool needed.
  `precept_proofs` may want to mention the new `IndexBounds` requirement
  kind in its catalog dump (small DTO addition).

**Breaking changes.** Yes — author-visible. Two:

1. Programs that use `.at(N)` / `insert at N` / `remove at N` without a
   bounds guard now emit `PRE0100`. The migration is mechanical: add the
   guard. Only `samples/shopping-cart.precept` is affected and it already
   has the right guard.
2. `PRE0100` message format changes from *"add a 'when {0}.count > {1}'
   guard"* (currently wrong — that's not a sufficient bounds check) to
   *"add 'when {1} >= 0 and {1} < {0}.count'"* (the actual sufficient
   guard). Any LS / MCP consumer that parses the message string would
   need to be updated; consumers that read the code (`PRE0100`) and the
   substitution slots are unaffected.

### External architectural precedent

The architectural problem is: *how does a compile-time bounds check on a
parameterized accessor get its index parameter into the discharge
strategy?* Three solutions:

- **Liquid Haskell** (refinement types + SMT). `{v: T | 0 <= i && i < vlen v}`
  carries the bound in the type; the SMT solver discharges it.
  `research/architecture/compiler/proof-attribution-witness-design-survey.md § Liquid Haskell:185-188`
  describes the pipeline: *"GHC type-checks the module … LH plugin
  intercepts the GHC-typed AST … LH generates Horn clause constraints …
  liquid-fixpoint library solves the constraints via a fixpoint
  computation over a lattice of refinement predicates."*

  Precept diverges: refusing opaque solvers (`precept-language-spec.md § 0.6 #3`)
  means the discharge has to be inspectable. We take Liquid Haskell's idea
  of carrying the obligation as type-attached metadata (the catalog entry
  on `.at(N)`); we reject the SMT discharge path.

- **Roslyn analyzers** (per-parameter subject). Roslyn's analyzer framework
  exposes `IParameterSymbol` as a first-class entity; analyzer obligations
  bind to specific parameters by symbol reference. The `IsValidValue`
  check on a parameter is the per-parameter-as-subject pattern.

  Precept's `ParamSubject(ParameterMeta)` framework (visible at
  `Operations.cs:121` — *"new NumericProofRequirement(new ParamSubject(PInteger), OperatorKind.NotEquals, 0m, 'Divisor must be non-zero')"*)
  is the analogous internal abstraction. This design extends it from
  binary operations to accessors and actions — the same per-parameter
  subject pattern, applied uniformly.

- **OPA Rego** (deferred-to-runtime bounds). Rego's `array_get(arr, i)` is
  a runtime built-in; no compile-time index-bounds proof. Cited as the
  opposite end of the spectrum.

**Precept's divergence.** Precept proves bounds at compile time (like
Liquid Haskell) but discharges via author guards (not SMT). The
`ParamSubject` framework is the in-tree precedent — extending it to
accessor/action index parameters is the obvious continuation, not a novel
architectural choice. The CONCERN 1 in the reviewer's prior pass was
correct: embedding `TypedExpression` directly in the obligation record
would have been the novel choice. The ParamSubject path is the
already-established one.

## Inventory of what will be built

### Catalog extension (`src/Precept/Language/`)

- `ParameterMeta` instances (`Operations.cs:90-99` precedent: shared `PInteger`, etc.):
  - `PCollectionIndex = new ParameterMeta(TypeKind.Integer, "index")` — shared
    across `.at(N)` on Log/LogBy/List and across Insert/RemoveAt actions.
- `TypeAccessor` record (`src/Precept/Language/Type.cs`): add optional
  `Parameters: ImmutableArray<ParameterMeta>` slot (default empty;
  populated for parameterized accessors). The existing `ParameterType:
  TypeKind?` becomes `Parameters[0].Type` for single-parameter accessors.
  Plural slot symmetric with `ActionMeta.Parameters` below — chosen now
  rather than later to avoid a second migration when `.range(start, end)`
  / `.slice(start, count)` / other multi-arg accessors land.
- `ActionMeta` (`Actions.cs`): add optional `Parameters: ImmutableArray<ParameterMeta>`
  (default empty). Populated for Insert/RemoveAt (and other future
  parameterized actions). Each entry maps positionally to the action's
  syntax shape slots.
- `Types.cs` — Log/LogBy/List `.at(N)` entries: add `IndexBoundsProofRequirement(ParamSubject(PCollectionIndex), StrictlyBefore, CollectionCountAccessor, "Index must be within bounds")`
  to ProofRequirements alongside the existing `self.count > 0`.
- `Actions.cs` — Insert: declare `Parameters: [PInsertCollection, PInsertValue, PCollectionIndex]`, add `IndexBoundsProofRequirement(ParamSubject(PCollectionIndex), AtOrBefore, CollectionCountAccessor, "Insert index must be within bounds [0, count]")`.
- `Actions.cs` — RemoveAt: declare `Parameters: [PRemoveCollection, PCollectionIndex]`, add `IndexBoundsProofRequirement(ParamSubject(PCollectionIndex), StrictlyBefore, CollectionCountAccessor, "Index must be within bounds")`.

### Proof engine extension (`src/Precept/Pipeline/`)

- `ProofEngine.cs ResolveParamInMemberAccess` (line 474): currently returns null. Replace with: walk `access.ResolvedAccessor.Parameter` for ref-equality match; return `access.Arguments[0]` (single-parameter accessors only).
- `ProofEngine.cs ResolveParamInAction` (NEW): walk `Actions.GetMeta(action.Kind).Parameters` for ref-equality match; return the corresponding TypedAction slot (InputExpression / SecondaryExpression / FieldRef per ActionSyntaxShape).
- `ProofEngine.cs ResolveSubject` (line 422): add `TypedInputAction action => ResolveParamInAction(param.Parameter, action)` arm.
- `ProofEngine.cs GuardConstraint` records (line 29): add `ParamUpperBoundConstraint(TypedExpression IndexExpression, OperatorKind Comparison, string CollectionField, string AccessorName)`. Sibling to existing `GuardConstraint` / `ContainsGuardConstraint` / `FieldToFieldConstraint`.
- `ProofEngine.Strategies.cs ExtractGuardLeafConstraints` (line 432): add an arm for `<param_expr> <op> <fieldref>.count` shapes; produce the new `ParamUpperBoundConstraint`.
- `ProofEngine.Strategies.cs ExtractGuardBranches` return shape: the branch collection grows to carry the new constraint type. Either widen `GuardConstraint` to a DU (preferred — see D-2) or thread both collections through (status-quo with a second list).
- `ProofEngine.Strategies.cs TryIndexBoundsProof` (NEW): the discharge strategy.

### TypedMemberAccess `Arguments` slot (`src/Precept/Pipeline/`)

- `SemanticIndex.cs:81-95 TypedMemberAccess` record: add `Arguments: ImmutableArray<TypedExpression>` (default empty). Continuation of the W-G `ChoiceMetadata` slot — same record, second extension.
- `TypeChecker.Expressions.Callables.cs ResolveMethodCall` (line 1133): pass `resolvedArgs` to the TypedMemberAccess constructor.

### Diagnostic refinement (`src/Precept/Language/`, `src/Precept/Pipeline/`)

- `Diagnostics.cs PRE0100 IndexBoundsGuard` (line 983): refine the message
  format. Current: *"'{0}' access at index '{1}' is not bounds-checked —
  add a 'when {0}.count > {1}' guard"* (note: this guard is INSUFFICIENT;
  it only proves non-empty, not in-range). New: *"'{0}' access at index
  '{1}' is not bounds-checked — add `when {1} >= 0 and {1} < {0}.count`
  to prove the index is in range"*.
- `Diagnostics.cs PRE0100` RecoverySteps + ExampleAfter: refresh to the
  full bounds guard (currently the ExampleAfter only adds `Items.count > 0`).
- `ProofEngine.Diagnostics.cs:122-128`: route `IndexBoundsProofRequirement`
  to PRE0100 explicitly (via `ProofRequirementMeta.IndexBounds.DiagnosticCode`).
- `ProofEngine.Diagnostics.cs:198`: the site-shape dispatch for the
  existing `self.count > 0` obligation on `.at(N)` should continue to
  emit PRE0100 (mirror the obligation-kind routing). This is intentional
  redundancy — the message says the same thing whether the failure is
  "non-empty" or "in-bounds." If subsequent investigation shows the two
  should diverge, the messages can split — but with the same diagnostic
  code reused for both, the existing tests stay green.
- `ProofRequirementMeta.IndexBounds.DiagnosticCode`: change from
  `UnguardedCollectionAccess` (the scaffolding committed in `44739a8b`)
  to `IndexBoundsGuard`. Single-line edit.

### Tests

- `test/Precept.Tests/ProofEngine/IndexBoundsTests.cs` — new file.
  - Insert with explicit lower+upper guard → discharges
  - Insert without guard → emits PRE0100
  - Insert with only the upper guard → emits PRE0100 (lower bound missing)
  - Insert with `nonnegative` arg + explicit upper guard → discharges (lower from type)
  - RemoveAt with explicit guard → discharges
  - RemoveAt without guard → emits PRE0100
  - `.at(N)` with explicit guard → discharges
  - `.at(N)` without guard → emits PRE0100
  - Disjunctive guard — both branches must prove independently
  - Sample regression: `samples/shopping-cart.precept` continues clean
- `test/Precept.Tests/ProofEngine/GuardConstraintExtractorTests.cs` (or
  add to existing): the new `<param_expr> <op> <fieldref>.count` shape
  extracts to `ParamUpperBoundConstraint`.

## Decisions

### Decision 1: Capture the index parameter via `ParamSubject(IndexParam)` rather than embedding `TypedExpression` directly

**Stakes**: medium

- **Rationale**: The `ParamSubject(ParameterMeta)` framework is the
  established in-tree pattern for binding obligations to specific
  parameter positions — `Operations.cs:121` uses it for divisor obligations
  (`new ParamSubject(PInteger)`). Treating Insert's index slot like Divide's
  divisor slot is the architecturally consistent choice. The scaffolding
  in commit `44739a8b` embedded `TypedExpression` directly in the
  requirement record, but that creates a per-obligation-type carrier
  pattern alongside the existing per-parameter-subject pattern — two
  ways to do the same thing.
- **Tradeoff accepted**: To make ParamSubject work for accessors and
  actions, the catalog records `TypeAccessor` and `ActionMeta` gain a
  `Parameter[s]` slot exposing their parameters as `ParameterMeta`. This
  is a mechanical catalog extension touching 5-6 catalog entries; the
  cost is bounded. The reward: structural consistency means a future
  engineer adding another parameterized-position obligation (e.g.,
  `slice(start, end)`) extends the same framework, not a parallel one.
- **Alternatives considered**:
  - **Embed `TypedExpression IndexExpression` directly in the
    requirement record.** Simpler — no catalog extension needed. Rejected
    because it creates a structural outlier — `IndexBoundsProofRequirement`
    becomes the only requirement type that carries a TypedExpression
    field; every other requirement uses `ProofSubject`. A future engineer
    reading the codebase has to learn why this one is different.
  - **`SelfSubject` with positional walkback to action's SecondaryExpression.**
    Compact, but tightly couples the requirement to the action's syntax
    shape. If `ActionSyntaxShape.InsertAt` ever changes the position of
    the index expression, the discharge silently breaks. Rejected for
    fragility.
- **Precedent**:
  - `src/Precept/Language/Operations.cs:121` — *"`new NumericProofRequirement(new ParamSubject(PInteger), OperatorKind.NotEquals, 0m, "Divisor must be non-zero")`"* — this is the established pattern for parameterized obligations in the catalog.
  - `src/Precept/Pipeline/ProofEngine.cs:446-458 ResolveParamInBinaryOp` — *"`if (ReferenceEquals(param, bom.Rhs)) return bin.Right; if (ReferenceEquals(param, bom.Lhs)) return bin.Left;`"* — same `ParameterMeta`-as-identity pattern this design extends to accessors and actions.
- **Sources consulted for this decision**:
  - `src/Precept/Language/Operations.cs:116-132 IntegerDivideInteger` — *"`new ParamSubject(PInteger), OperatorKind.NotEquals, 0m, "Divisor must be non-zero"`"* — confirms the pattern for parameterized obligations.
  - `src/Precept/Pipeline/ProofEngine.cs:446-478` — *"`ResolveParamInBinaryOp`, `ResolveParamInFunctionCall`, `ResolveParamInMemberAccess`"* — three sites that pattern-match `ParameterMeta` by reference identity. `ResolveParamInMemberAccess` returns null today (no obligations use ParamSubject on member access); extending it is the natural completion.
  - `src/Precept/Language/ProofRequirement.cs:13-25` — *"`public sealed record ParamSubject(ParameterMeta Parameter) : ProofSubject; public sealed record SelfSubject(TypeAccessor? Accessor = null) : ProofSubject;`"* — the two subject variants; ParamSubject for parameterized operations.

### Decision 2: Extend `GuardConstraint` via a sibling record (`ParamUpperBoundConstraint`)

**Stakes**: medium

- **Rationale**: The existing extractor produces three sibling record
  types — `GuardConstraint`, `ContainsGuardConstraint`,
  `FieldToFieldConstraint` (`ProofEngine.cs:29-50`). Adding a fourth
  record for `<param> <op> <field>.accessor` is the established pattern.
  Each record captures one shape; consumers pattern-match on the type.
- **Tradeoff accepted**: `ExtractGuardBranches` return shape grows to
  carry the new constraint type. Either widen the existing `ImmutableArray<GuardConstraint>`
  to a DU (preferred — see § 3) or thread both collections side-by-side.
  Cost is bounded — touches `ExtractGuardBranches`,
  `ExtractGuardConstraints`, `ExtractGuardConstraintsCore`, and the
  three consumers that walk constraints.
- **Alternatives considered**:
  - **Widen `GuardConstraint`** with nullable `IsParamBound: bool` and
    `IndexExpression: TypedExpression?` fields. Rejected — violates
    CLAUDE.md's *"Use discriminated unions for varying shapes. Don't paper
    over shape differences with nullable fields on a flat record — use a
    DU base + sealed subtypes."*
  - **Side-table on context.** Stash the param-upper-bound constraints
    in a `Dictionary` keyed by guard span; let the discharge strategy
    look them up. Rejected — parallel data structure that drifts from
    the actual guard representation.
- **Precedent**:
  - `src/Precept/Pipeline/ProofEngine.cs:29-50` — *"`private record GuardConstraint(...)` … `private record ContainsGuardConstraint(string Field, bool Negated);` … `private record FieldToFieldConstraint(...)`"* — three sibling guard-constraint records already. Adding a fourth is the established pattern.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/ProofEngine.cs:29-50` — the three existing constraint records (one for `field op literal`, one for `contains`, one for `field op field`). The new shape (`param op field.accessor`) is the fourth.
  - `CLAUDE.md § Catalog System — "Use discriminated unions for varying shapes. Don't paper over shape differences with nullable fields on a flat record — use a DU base + sealed subtypes."`

### Decision 3: Static catalog declaration of the obligation (no `DynamicObligationGenerator`); explicit ban on per-receiver-kind dispatch

**Stakes**: medium

- **Rationale**: Once D-1 commits to `ParamSubject(IndexParam)`, the
  obligation can be declared statically in the catalog — the
  `ParameterMeta` is shared, and the resolution happens at discharge
  time via `ResolveParamInMemberAccess` / `ResolveParamInAction`. No
  per-call construction is needed. This is the same way Divide's
  obligation is declared statically and resolved at discharge time.
  Per-receiver-kind dispatch (`receiver.ResultType switch { Log => ..., LogBy => ..., List => ... }`)
  inside any generator body is explicitly banned: it would be exactly
  the *"switching on `*Kind` enum identity to dispatch per-member
  behavior"* smell CLAUDE.md prohibits.
- **Tradeoff accepted**: Three accessor catalog entries (Log/LogBy/List
  `.at`) repeat the same obligation declaration with a shared
  `ParameterMeta` — call it duplication. The alternative (dynamic
  generator) avoided the duplication but introduced the dispatch smell.
  Accept the three-entry repetition.
- **Alternatives considered**:
  - **`DynamicObligationGenerator` on `TypeAccessor`** (the path the
    original draft chose). Generator dispatches on receiver kind to
    produce the obligation. Rejected — the per-receiver-kind dispatch
    is exactly what the catalog rule prohibits. If the bounds semantics
    ever genuinely differ across collection kinds (they don't today),
    the right answer is per-kind catalog entries, not a generator with
    a switch.
- **Precedent**:
  - `src/Precept/Language/Types.cs:194-302` — Log/LogBy/List accessor
    blocks already repeat the same shape for analogous accessors (e.g.,
    `.first`/`.last` declared with the same proof requirements across
    all three kinds). This is the established catalog pattern.
  - `src/Precept/Language/Operations.cs:116-132` — Divide / Modulo share
    the same `NumericProofRequirement(PInteger != 0)` declaration
    repeated across IntegerDivideInteger, IntegerModuloInteger, etc.
- **Sources consulted for this decision**:
  - `src/Precept/Language/Types.cs:209-215` — *".at" entry on Log; `ProofRequirements: [new NumericProofRequirement(new SelfSubject(CollectionCountAccessor), OperatorKind.GreaterThan, 0m, "Log must be non-empty")]"*. Two more parallel entries at lines 233-239 (LogBy) and 271-277 (List). Three-way repetition is the established pattern.
  - `CLAUDE.md § Catalog System — "Never switch on `*Kind` enum identity to dispatch per-member behavior. The smell is `kind switch { FooKind.Bar => …, FooKind.Baz => … }` where each arm exists 'because the language says so.'"`

### Decision 4: Lower-bound discharge — hybrid type-derived + guard-derived, with explicit contract for bare-integer args

**Stakes**: medium

- **Rationale**: When N is declared as `integer nonnegative` (field-level
  or arg-level), the type system already proves N >= 0. The existing
  `DeclarationAttribute` strategy at `ProofEngine.Strategies.cs:137-148`
  walks effective modifiers and discharges `Numeric(>= 0)` from a
  `nonnegative` modifier. We reuse that strategy unchanged. When N is
  declared as `integer` (no modifier), the type system makes no
  non-negativity claim; the discharge requires an explicit `N >= 0`
  guard.
- **Tradeoff accepted**: Authors writing parameterized-index accessors
  in samples will typically declare the index arg as `integer nonnegative`
  — the lower bound discharges silently, the author only needs to
  supply the upper bound. Bare-integer args require both bounds in the
  guard. This is the right ergonomic split: declaring intent in the
  type system gets you a smaller guard surface.
- **Alternatives considered**:
  - **Require explicit `N >= 0` guard regardless of type.** Pros: uniform
    discharge story. Cons: doubles up — every parametric-index sample
    has redundant `N >= 0` guards that the type system already implies.
    Rejected for friction.
  - **Trust the type for any integer-typed N.** Cons: silently accepts
    `.at(-1)` when N is bare integer. Rejected — defeats the totality
    contract.
- **Precedent**:
  - `src/Precept/Pipeline/ProofEngine.Strategies.cs:137-148` —
    *"`foreach (var modifier in attributeField.Modifiers.Concat(attributeField.ImpliedModifiers)) { var meta = Modifiers.GetMeta(modifier); if (meta is not ValueModifierMeta fmm) continue; foreach (var satisfaction in fmm.ProofSatisfactions) { if (SatisfactionCovers(satisfaction, obligation.Requirement)) return true; } }`"* — the established type-derived nonnegativity path.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/ProofEngine.Strategies.cs:125-148` — *"Accessor-level nonnegative guarantee: collection count can never be negative … Walk declared + implied modifiers"*. The same pattern handles `field.count >= 0` (trivially nonnegative); we extend the same handling to `nonnegative`-declared args.
  - `src/Precept/Language/Modifiers.cs` Nonnegative modifier meta — provides the `ValueModifierMeta.ProofSatisfactions` entry that this discharge consults.

### Decision 5: Reuse PRE0100 (IndexBoundsGuard) with refined message; reserve PRE0042 (UnguardedCollectionAccess) for non-empty failures

**Stakes**: medium

- **Rationale**: PRE0100 already exists in the catalog
  (`Diagnostics.cs:983-992`) with the right name and the right
  PreventsFault routing (`FaultCode.CollectionEmptyOnAccess`). It's
  already wired in the site-shape dispatch
  (`ProofEngine.Diagnostics.cs:198`) for `.at(N)` failures of the
  existing `self.count > 0` obligation. Reusing PRE0100 for the new
  IndexBoundsProofRequirement keeps a single diagnostic surface for
  all index-related access issues. The original draft proposed reusing
  PRE0042 (UnguardedCollectionAccess); the reviewer caught that PRE0100
  was the catalog-correct code, and they're right.
- **Tradeoff accepted**: PRE0100's existing message text is wrong (*"add
  a 'when {0}.count > {1}' guard"* is not a sufficient bounds check —
  it only proves non-empty). The message refinement is a public-surface
  text change; users who have PRE0100 message text in screenshots,
  documentation, or training material will see the new wording. Single
  message refinement is the right cost.
- **Alternatives considered**:
  - **Allocate a new PRE0NNN code specifically for IndexBounds.**
    Rejected — duplicates an existing code; users have to learn
    two PRE codes for what's structurally the same problem (`.at(N)` is
    unsafe without a guard).
  - **Keep PRE0042 for IndexBoundsProofRequirement (per the scaffolding
    committed in `44739a8b`).** Rejected — PRE0042's name
    (UnguardedCollectionAccess) and message ('may be empty — guard with
    `if F.count > 0`') target the non-empty case specifically. The
    semantics don't match.
- **Precedent**:
  - `src/Precept/Language/Diagnostics.cs:983-992 PRE0100 IndexBoundsGuard`
    — *"'{0}' access at index '{1}' is not bounds-checked — add a 'when {0}.count > {1}' guard"*. The name and PreventsFault are right; the message text is wrong and is the part this design fixes.
  - `src/Precept/Pipeline/ProofEngine.Diagnostics.cs:198` —
    *"`TypedMemberAccess { ResolvedAccessor: { Name: "at", ParameterType: not null } } => DiagnosticCode.IndexBoundsGuard`"* — the existing site-shape dispatch confirms PRE0100 is already the catalog-correct code for this site.
- **Sources consulted for this decision**:
  - `src/Precept/Language/Diagnostics.cs:983-992` — PRE0100 catalog entry.
  - `src/Precept/Pipeline/ProofEngine.Diagnostics.cs:144-159,192-203` — the existing site-shape dispatch and obligation-kind dispatch (currently inconsistent because the `44739a8b` scaffolding routed IndexBoundsProofRequirement to UnguardedCollectionAccess instead).

## Acceptance criteria

1. `.at(N)`, `insert F E at N`, `remove F at N` emit `IndexBoundsProofRequirement` obligations (verified via proof-ledger snapshot in IndexBoundsTests).
2. A guarded row `when N >= 0 and N < F.count -> set X = F.at(N)` discharges the obligation (proof-ledger shows Proved).
3. The same row without the guard emits `PRE0100 IndexBoundsGuard` (diagnostic snapshot test).
4. The same row with `nonnegative`-declared N but missing the upper guard emits `PRE0100` (lower bound discharges silently from the modifier, upper bound is missing).
5. Disjunctive guard `when (N >= 0 and N < F.count) or (someOtherCondition)` requires BOTH branches to independently prove both bounds; if `someOtherCondition` doesn't establish bounds, the obligation stays Unresolved.
6. `samples/shopping-cart.precept` continues to compile clean (`ReorderItem`'s existing guard at line 193 is the canonical structural match).
7. PRE0100 message format is refined to name both bounds (`{N} >= 0 and {N} < {F}.count`) in the recovery hint; the existing ExampleAfter entry is refreshed to show the full bounds guard.
8. `ProofRequirementMeta.IndexBounds.DiagnosticCode` is `IndexBoundsGuard` (PRE0100), not `UnguardedCollectionAccess`.
9. All 4 test projects green: Precept.Tests, LanguageServer.Tests, Mcp.Tests, Analyzers.Tests.

## Dependencies

- **Upstream**: W-G's `TypedMemberAccess` extension pattern (ChoiceMetadata slot via default-null parameter) is the structural precedent for adding the `Arguments` slot in this slice.
- **Upstream**: F-LANG-COLL-05 (already shipped in `44739a8b`) — same workstream's KeyPresence discharge demonstrates the obligation-kind-driven discharge pattern this design extends.
- **Downstream**: None blocking. This slice closes Phase 4 W-E.

## Doc-update enumeration

| Change | Doc |
|---|---|
| New ProofRequirementKind member | `docs/language/catalog-system.md § ProofRequirementKind` — bump catalog count; add IndexBounds row |
| New DU subtype + ParamSubject-based shape | `docs/compiler/proof-engine.md § Proof requirements` — add IndexBoundsProofRequirement; ParamSubject extension for accessors and actions |
| Guard-constraint shape extension | `docs/compiler/proof-engine.md § Guard decomposition` — add ParamUpperBoundConstraint sibling |
| New discharge strategy section (referenced by teaching path) | `docs/compiler/proof-engine.md § Index bounds discharge` — author the new section that explains the structural-match discharge for IndexBoundsProofRequirement (cited from this design's teaching path step 4) |
| `.at(N)`, `insert at N`, `remove at N` bounds guard requirement | `docs/language/collection-types.md § .at`, `§ insert at`, `§ remove at` — clarify the new bounds guard discharge |
| Diagnostic message refinement | `docs/compiler/diagnostic-system.md § PRE0100` — refresh the example before/after to show the full bounds guard |
| W-E completion | `docs/Working/compiler-readiness-plan-2026-05-24.md § Phase 4 W-E` — mark complete; archive scaffolding-only state |

## Operational dimensions

- **Security**: N/A — this design touches proof obligation generation and discharge, not source-text ingestion. No new attack surface.
- **Observability**: Affected. Unresolved IndexBoundsProofRequirement obligations surface as PRE0100 diagnostics with structured site/field/index attribution. The proof ledger captures the unresolved obligation for `precept_proofs` consumers. Hover surface unchanged in this slice; future enhancement could surface the obligation on hover.
- **Evolvability**: N/A — no dependency on external standards (NodaTime, ICU, UCUM, ISO 4217, TZDB). The discharge logic is self-contained to Precept.

## Falsifiers

External-author-visible change → 2-5 specific observations that would force redesign post-ship.

- **If any sample needs an explicit `N >= 0` guard for an arg-declared `integer nonnegative` index**: the type-derived lower-bound discharge isn't firing as intended; D-4's hybrid story is broken in implementation. Re-investigate the `DeclarationAttribute` strategy's interaction with `ParamSubject`-resolved arg references.
- **If users repeatedly write the wrong upper bound (`N <= F.count` for `.at(N)` instead of `N < F.count`)**: the discharge for `StrictlyBefore` mode is silently accepting `<= count` as if it were `< count`, or the PRE0100 message is unclear about the strict-vs-non-strict distinction. Tighten the discharge match and rewrite the message.
- **If `samples/shopping-cart.precept` requires any guard refactoring to discharge the new obligation**: the structural matcher missed a guard shape the canonical sample uses. Trace through the extractor and add the missing arm.
- **If LS / MCP hover starts showing PRE0100 obligations on every accessor (including `.first`, `.last`)**: the obligation-kind routing mis-matched non-bounds obligations to PRE0100. Re-check `ProofRequirementMeta.IndexBounds.DiagnosticCode` and the dispatch paths.
- **If a single domain-expert user testing fails to author a working `.at(N)` example within 10 minutes using the worked-example-in-docs path**: the audience-fit claim in the teachability section is falsified; the docs or the diagnostic wording need a refresh.

## Open Questions

None — the design has resolved CONCERN 1 (ParamSubject path), CONCERN 2
(banned per-kind dispatch in D-3), CONCERN 3 (documented user-visible
behavior for the quantifier-binding deferral in § Scope), and CONCERN 4
(spelled out the bare-integer-arg negative contract in D-4). The two
BLOCKERs (PRE0100 reconciliation in D-5, TypedMemberAccess.Arguments
framed as W-G continuation) are addressed.
