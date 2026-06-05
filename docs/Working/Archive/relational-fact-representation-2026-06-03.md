---
status: Superseded by: docs/Working/relational-narrowing-core-design-2026-06-03.md
superseded-reason: scoped too narrowly (fact representation only). An unbiased precept-reviewer pass found three soundness BLOCKERs this focused design rationalized past — (1) the guarded-relational-rule global-leak (its extraction folded guarded rules unconditionally), (2) the depth-1 control was asserted "by construction" with no concrete mechanism while IntervalOf recurses into ExtractFieldInterval, and (3) its Decision-2 counter-evidence claim that satisfiability uses a "different call site" than ExtractFieldInterval is FALSE (Satisfiability.cs:96,183,269 all read ExtractFieldInterval; a tighter interval there manufactures false UnsatisfiableGuard/VacuousRule/ContradictoryRule). The comprehensive successor re-derives unbiased and designs the sound mechanism for all four surfaces (fact rep, extraction+guard filter, depth-1 flag, consumer split).
phase-target: Phase 7 (total language conformance sweep) — the concrete-representation detail that the parent relational design (relational-rules-and-bounds-design-2026-06-02.md) left at the conceptual level; consumed by /lifecycle-3-plan as the first build-slice's representation decision
comparable-systems-research-status: strong — grounded in `research/architecture/compiler/relational-constraint-representation-survey.md` (octagon / difference-bound-matrix as the canonical *relation-as-fact* pair-representation, with primary-source Miné excerpts); the parent design's Decision 2 (= B) is the locked input this doc realizes concretely
sources-consulted:
  - "docs/Working/relational-rules-and-bounds-design-2026-06-02.md — Decision 2 (= B, relation-as-fact, share the guard core, no DTO change, named-rule witness); Decision 4 (single-pass octagon-closure-minus-widening); § Semantic Rules R-NARROW-GE/LE; § Inventory."
  - "CLAUDE.md § Catalog System — 'Use discriminated unions for varying shapes. Don't paper over shape differences with nullable fields on a flat record'; 'Never switch on *Kind enum identity to dispatch per-member behavior.'"
  - "src/Precept/Pipeline/ProofEngine.cs:88-93 — ScopedNumericFact(Subject, Comparison, decimal Value, …): the current field-op-constant fact; readonly record struct; private to ProofEngine."
  - "src/Precept/Pipeline/ProofEngine.cs:45-48 — FieldToFieldConstraint(LeftField, OperatorKind Comparison, RightField): the already-existing field-to-field relation record, today extracted only from guards."
  - "src/Precept/Pipeline/ProofEngine.Composition.cs:144-208 — CollectTrustedNumericFacts builds List<ScopedNumericFact> from rules/ensures via TryGetNumericConstraintFact; both arms call TryGetStaticNumericValue, so a field-op-field condition produces nothing today."
  - "src/Precept/Pipeline/ProofEngine.Strategies.cs:1100-1238 — the guard-sourced field-to-field machinery: ExtractFieldToFieldBranches → ExtractFieldToFieldLeaf (builds FieldToFieldConstraint) → GuardRelationImpliesObligation (subtraction-only operator table). The core Decision 2 says to SHARE."
  - "src/Precept/Pipeline/ProofEngine.Intervals.cs:202-220 — ExtractFieldInterval / FlagLowerBound / GetFieldBounds: the one-hop interval read where R-NARROW must contribute the related field's bound."
  - "src/Precept/Language/NumericInterval.cs:143-150 — Intersect IS the ⊓ meet (sentinel-safe: Unbounded acts as identity); Min/Max with ±∞ sentinels."
  - "src/Precept/Pipeline/ProofLedger.cs:24-115 — ProofObligation(… ProofStrategy? Strategy …); ProofStrategy enum (FlowNarrowing = 4 is the guard-sourced relational strategy); ProofDisposition {Proved, Unresolved}."
  - "src/Precept/Language/ProofRequirement.cs:190-198 — IntervalContainmentProofRequirement(… decimal? DeclaredMin, decimal? DeclaredMax …): the literal-only shape Decision 2 = B leaves untouched."
  - "tools/Precept.Mcp/Dtos/CompileToolDtos.cs:26-28 + Tools/CompileTool.cs:44-56 — the MCP obligation DTO reads only IntervalContainmentProofRequirement.DeclaredMin/Max; ScopedNumericFact and FieldToFieldConstraint never cross into the DTO (both private to ProofEngine)."
  - "research/architecture/compiler/relational-constraint-representation-survey.md — octagon strong closure (Miné) and difference-bound-matrix as the canonical relation-as-fact (B) pair-representation; the DBM is a set of ±x±y≤c facts and the closed form 'is a rule list'; witness should carry the contributing relation as a named fact."
---

> **Status:** Historical — superseded by [relational-narrowing-core-design-2026-06-03.md](relational-narrowing-core-design-2026-06-03.md) (Locked, promoted to canonical). Prior-art record only; do not update.

# Relational fact representation in the proof engine

## Goal

When done, the proof engine carries a field-to-field relational fact (`rule X >= Y`) as a concrete, named record in its fact vocabulary, sourced from declared rules (not only guards), and composes it through `ExtractFieldInterval` so the related field's declared interval tightens the subject's interval per the parent design's R-NARROW rules — with **zero change** to `IntervalContainmentProofRequirement` or any MCP DTO. Demonstrated by: the reorder precept's `rule OnHand > Reserved` narrowing `OnHand`'s interval from `Reserved`'s lower bound so `BatchCost / (OnHand - Reserved)` discharges; and the unbounded-`Y` case degrading to identity (no false narrowing, obligation stays unresolved).

## Scope

- **In scope**:
  - The concrete C# record shape that carries a field-to-field relational fact through the proof engine's fact stream (Decision 1).
  - The composition: how that fact turns into a tightened bound on the subject field's interval in the interval-read path, sharing the guard-sourced core rather than forking it (Decision 2).
  - The soundness-preservation argument that the narrowing only ever tightens-or-is-identity and can never manufacture a false "safe" (Decision 3).
- **Out of scope** (locked upstream by the parent design — cited, not re-decided here):
  - Decision 2 of the parent design (A-vs-B): **B is locked (2026-06-02)**. This doc does not re-litigate A-vs-B; it realizes B concretely.
  - Whether a field-reference bound is admitted as a constraint (parent Decision 1, §0.7-settled).
  - The emit-unconditionally fixes for BUG-017/018/019 (parent Decision 3) — those are emission-side; this doc is the discharge-side fact representation.
  - Single-pass / depth-bound policy (parent Decision 4, octagon-closure-minus-widening) — this doc inherits the depth bound, it does not set it.
  - Any new language surface. Relational rules and the bound modifiers already parse; this is proof-engine-internal fact vocabulary.
- **Deferred to future**: none. The representation is a complete, self-contained internal decision.

## Philosophy Alignment

| Principle | Affected? | How served (1 sentence + cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | The relational fact lets a declared rule discharge a downstream fault obligation at compile time, so the unprovable case is rejected rather than trapped at runtime (parent design Goal; §0.7 "no deferral"). | The narrowing must never *over*-tighten (manufacture a bound the relation doesn't license) or prevention becomes a false claim. | Accept identity-degradation (no narrowing when the related field is unbounded) over an optimistic guess. |
| 2. One file, complete rules | Y | The fact derives only from `semantics.Rules` / declared constraints in the same `.precept` (`ProofEngine.Composition.cs:144-151`). | N/A | N/A |
| 3. Determinism | Y | The fact is a value record over declared intervals; composition is one `Intersect` of the related field's pre-existing interval — no iteration, same input → same disposition (`NumericInterval.cs:143`). | N/A | N/A |
| 4. Full inspectability | Y | The chosen shape names both fields and the operator (`FieldToFieldConstraint(LeftField, op, RightField)`), so the witness can cite the contributing rule as a named fact — the octagon/DBM rule-list witness (survey Implication 4). | N/A | The strategy disposition must attribute to the *rule*, reusing `ProofStrategy.FlowNarrowing` (4) so the narrowed interval carries provenance. |
| 5. Keyword-anchored readability | N | No syntax change; proof-engine-internal record. | N/A | N/A |
| 6. Governance not validation | N | Runtime governance enforcement is unchanged; this is compile-time fact representation only. | N/A | N/A |
| 7. Compile-time-first static checking | Y | A declared relation now contributes a provable bound through the same interval path divisor/range obligations already read (`ExtractFieldInterval`, `Intervals.cs:202`). | Bounded single-hop closure is incomplete (inherited from parent Decision 4). | Accept incompleteness over fixpoint, per §0.4. |
| 8. Approximation honesty | N | Operates on exact decimal interval bounds; introduces no approximate lane. | N/A | N/A |
| 9. Mandatory rationale | N | The rule's `because` is unchanged; representation carries no rationale-bearing surface. | N/A | N/A |
| 10. Totality | Y | A fault-prone op bounded only by an unbounded-operand relation yields identity narrowing → obligation unresolved → rejection, never hopeful compile (§0.1 Principle 10). | N/A | N/A |
| 11. Static completeness | Y | The fact is the discharge half of the parent design's restoration of the obligation→diagnostic bridge for the relational path. | N/A | N/A |

**Tradeoff detail (Principles 1/7/10).** The fact shape carries no value of its own — it points at the *related field*, whose interval is read at composition time. This means the narrowing is exactly as strong as the related field's already-established bound and no stronger: when that bound is `±∞`, the `Intersect` is the identity (`NumericInterval.cs:145-146`: `Unbounded` intersected is the other operand) and the subject gains nothing. The accepted tradeoff is false-negative friction (the author must bound the related field) in exchange for the guarantee that the engine never manufactures a bound the relation does not license.

**Companion commitments.** *Stateless-first-class*: the fact is field-scoped, derived from rules/ensures that apply identically to stateless precepts; nothing here depends on a state machine (the existing `AnchorState`/`AnchorEvent` fields on `ScopedNumericFact` are optional and orthogonal). *Domain-expert-primary-author*: invisible to the author — no new vocabulary; the already-natural `rule X >= Y` does the proof work.

## Language Design Grounding

Omitted — this design introduces **no language surface**. It is a proof-engine-internal data-representation decision (which C# record carries a relational fact through the fact stream). The language-level grounding for *making a relational constraint participate in proof* lives in the parent design's § Language Design Grounding (CUE narrowing vs Zod no-propagation; the §0.4 single-pass divergence) and is not repeated.

## Audience and Teachability

Omitted — no language surface. The author-observable behavior (which precepts compile, the unbounded-`Y` diagnostic) is specified and taught in the parent design's § Audience and Teachability; this doc changes none of it.

## Semantic Rules

### The fact shape

A field-to-field relational fact is the record:

```
FieldToFieldConstraint(string LeftField, OperatorKind Comparison, string RightField)
```

— which **already exists** at `ProofEngine.cs:45-48`, today produced only from guard decomposition (`ExtractFieldToFieldLeaf`, `Strategies.cs:1203-1211`). The representation decision is: *route declared rules of shape `Xfield op Yfield` into this same record*, not invent a parallel one. This is the relation-as-fact (B) shape: the fact is about the **pair** `(LeftField, RightField)`, holding neither field's value — exactly the difference-bound-matrix `x − y ≤ c` element form (survey § "Gap 2", a DBM entry is a relation between two variables, not a per-variable value).

`ScopedNumericFact` (`ProofEngine.cs:88-93`) is left exactly as-is: it is the `field op decimal` shape (it carries a `decimal Value`) and is structurally incapable of `X op Y` — which is precisely why a second, sibling record is correct rather than extending it with nullable fields. The two records are **disjoint shapes for disjoint facts**: `ScopedNumericFact` = field-against-constant; `FieldToFieldConstraint` = field-against-field.

### R-NARROW composition (the new wiring)

The parent design's R-NARROW-GE / R-NARROW-LE (its § Semantic Rules) are realized at the interval-read path `ExtractFieldInterval(fieldName, semantics)` (`Intervals.cs:202-220`). Let `⟦F⟧` denote `ExtractFieldInterval(F)` *before* relational narrowing (declared bounds + flag-lower-bound folding, exactly today's body). For each declared `FieldToFieldConstraint(X, op, Y)` with `op ∈ {>=, >}` (lower-bound family) or `op ∈ {<=, <}` (upper-bound family), where `Y` is a field reference:

```
  rule  X >= Y      ⟦Y⟧ = [ylo, yhi]
  ─────────────────────────────────────────────  (R-NARROW-GE)
        ⟦X⟧ := ⟦X⟧ ⊓ [ylo, +∞)
              = ⟦X⟧.Intersect(new(ylo, +∞))

  rule  X <= Y      ⟦Y⟧ = [ylo, yhi]
  ─────────────────────────────────────────────  (R-NARROW-LE)
        ⟦X⟧ := ⟦X⟧ ⊓ (−∞, yhi]
              = ⟦X⟧.Intersect(new(−∞, yhi))
```

where `⊓` is `NumericInterval.Intersect` (`NumericInterval.cs:143`) and `±∞` are the `decimal.MinValue`/`decimal.MaxValue` sentinels.

- **Strict vs non-strict (`>` vs `>=`).** `X >= Y` contributes `ylo` to `X`'s lower bound. `X > Y` *also* contributes `ylo` (not `ylo + ε`): on `decimal` there is no representable ε, and `X >= ylo` is a sound (weaker) over-approximation of `X > Y` — it can only fail to discharge a boundary case, never falsely discharge one. This matches the existing guard table, which proves `X - Y >= 0` from `X > Y` but treats `>` and `>=` identically for the `Threshold == 0` lower-bound cases (`GuardRelationImpliesObligation`, `Strategies.cs:1228-1234`). (A strict downstream obligation — e.g. divisor `!= 0` on `X - Y` — is still discharged by the *subtraction* path, which `GuardRelationImpliesObligation` already handles strictly; the interval narrowing here is the non-strict floor that feeds range/sqrt/overflow obligations.)
- **Identity degradation (unbounded `⟦Y⟧`).** When `ylo = −∞` (R-NARROW-GE) or `yhi = +∞` (R-NARROW-LE), `Intersect` returns `⟦X⟧` unchanged (`NumericInterval.cs:145-146`: intersecting with an `Unbounded`-derived half-line where the relevant bound is the sentinel leaves the bound at the sentinel, so `X`'s effective bound is unchanged). The narrowing is the identity; any obligation depending on `X`'s narrowed bound stays `Unresolved`.
- **Conjunction with existing facts (multiple rules on X).** `Intersect` is associative and commutative, so multiple relational facts on `X` (`X >= Y`, `X >= W`) compose by successive `⊓` — `X` ends at `max(ylo, wlo)` for its lower bound — and compose identically with `X`'s own declared bounds and flag-lower-bounds (already an `⊓`-fold in `ExtractFieldInterval`). Order-independence gives determinism (Principle 3).
- **Single-pass / depth bound.** `⟦Y⟧` is `Y`'s interval *before* relational narrowing of `Y` from a third field — i.e. the read of `⟦Y⟧` does **not** recursively apply R-NARROW to `Y`. This is the depth-≤1 octagon-closure-minus-widening from parent Decision 4; the implementation reads `Y`'s declared/flag interval, not a transitively-narrowed one. Self-reference (`X >= X` → `⟦X⟧ ⊓ [xlo,+∞)` = `⟦X⟧`, vacuous) and mutual reference (`X >= Y` + `Y >= X` → each one hop, conjoining to `X == Y`) terminate by construction — no fixpoint.

### Sharing the guard core (not forking)

The guard-sourced machinery (`ExtractFieldToFieldBranches` → `ExtractFieldToFieldLeaf` → `GuardRelationImpliesObligation`, `Strategies.cs:1111-1238`) already turns a `FieldToFieldConstraint` into an obligation discharge for the **subtraction** family (`X - Y >= 0` from `X >= Y`). The rule-sourced path reuses the **same `FieldToFieldConstraint` record and the same operator-implication core**; it adds (a) a *source* — extract the constraint from `semantics.Rules` of shape `fieldRef op fieldRef`, the symmetric counterpart to `TryGetNumericConstraintFact`'s field-op-constant extraction (`Composition.cs:180-208`), and (b) a *consumer* — feed the related field's interval into `ExtractFieldInterval` (the range/sqrt/overflow family), where the guard path today only feeds the subtraction family. One record, one operator-implication core (`GuardRelationImpliesObligation`'s table is the shared truth-table); guard-sourced and rule-sourced differ only in *where the `FieldToFieldConstraint` comes from*.

### Proof obligations

This doc adds **no new `ProofRequirement` and no new `ProofRequirementKind`**. The relational fact is a *discharge input*, consumed by the existing range/divisor/sqrt/`IntervalContainment` obligations through `ExtractFieldInterval`. The discharge is attributed via the existing `ProofStrategy.FlowNarrowing` (= 4, `ProofLedger.cs:105`) — the same strategy the guard-sourced relational path already reports — so no `ProofStrategy` enum member is added either.

### Soundness preservation claim

- **Principle 7 / 10 / 11** hold because R-NARROW-* only ever *tightens* `⟦X⟧` by `⊓` against a half-line derived from `⟦Y⟧`'s *already-established* bound, and is the identity when that bound is the `±∞` sentinel (`NumericInterval.cs:145-146`). A `⊓` against a real value can only shrink an interval; it can never widen `X`'s provable range. Therefore the mechanism cannot manufacture a false "proved safe": any obligation the narrowed `⟦X⟧` does not cover stays `Unresolved` and emits (§0.7 "no deferral"). The change is monotonic — it adds discharges (when `Y` is bounded) and never removes a rejection.
- **Principle 3** holds because composition is `Intersect` (associative/commutative) over pre-existing intervals, single-pass and depth-bounded — disposition is a deterministic function of the declared intervals, no iteration-order dependence.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** Pipeline code (the proof engine), **not** catalog metadata. The fact record (`FieldToFieldConstraint`) and the narrowing are *reasoning over* catalog-declared obligations — the same layer where the guard-sourced Strategy-4 machinery already sits (`Strategies.cs:1111+`). The catalog declares *what* must be proved (the `ProofRequirement` DU); the strategy is *how*. A relational fact is not language metadata (it is derived per-precept from declared rules), so it does not belong in a catalog; it is a transient analysis artifact, correctly private to `ProofEngine` (as `ScopedNumericFact` and `FieldToFieldConstraint` already are). This respects the CLAUDE.md catalog rule: no new enum-identity dispatch (the operator-implication is a value table in `GuardRelationImpliesObligation`, not a `Kind switch`), and the DU rule (a sibling record for a disjoint shape, not nullable-field papering — see Decision 1).

**Cross-component propagation:**
- **Runtime (parser, type checker, evaluator, diagnostics):** Proof engine — adds the rule-sourced extraction (a field-op-field arm alongside `Composition.cs:180-208`) and the `ExtractFieldInterval` narrowing consumer. Parser / type checker / evaluator — **None** (no new node, no new typing rule, no runtime path). Diagnostics — **None new from the representation itself**; the relational-unprovable case reuses the existing divisor/range diagnostics with `FlowNarrowing` attribution (the parent design owns any new wording).
- **Tooling (highlighting, completions, hover, semantic tokens):** Hover/proof-attribution — the narrowed interval is attributed to `ProofStrategy.FlowNarrowing`, already a known strategy; no new tooling surface, no grammar/token/completion change. **None** structural.
- **MCP (vocabulary, DTOs, tool output):** **None.** `FieldToFieldConstraint` and `ScopedNumericFact` are `private` to `ProofEngine` and never serialized. The MCP obligation DTO (`CompileToolDtos.cs:26-28`, `CompileTool.cs:44-56`) reads only `IntervalContainmentProofRequirement.DeclaredMin/Max`, which this design leaves byte-identical. No DTO shape, no `CatalogFormatters` change.

**Breaking changes.** None. No public contract, no catalog member, no diagnostic code, no DTO. `FieldToFieldConstraint` is an existing private record; `ProofStrategy.FlowNarrowing` is an existing enum member. A program that previously compiled because a relation didn't discharge may now compile (more discharges) — strictly additive proving power, never a new rejection from the representation itself (new rejections come from the parent design's emit-unconditionally fixes, not from this fact shape).

### External architectural precedent

The architectural problem: *what concrete object represents a two-field relation in a constraint engine's fact store, such that it discharges by a bounded, legible operation?* The canonical answer is the **difference-bound matrix (DBM) / octagon** element, per `research/architecture/compiler/relational-constraint-representation-survey.md`:

> "**Octagons** … **B** — the relation `±x ± y ≤ c` is a first-class element of a shared DBM (difference-bound matrix) … the closed form *is* a rule list." (survey § "A-vs-B classification")

> "**Difference logic / DBM** … **B** — `x − y ≤ c` is the stored fact … the constraint graph; the canonical DBM is the tightest derivable constraint set." (survey § "A-vs-B classification")

**What Precept takes:** the relation-as-fact (B) shape — a record naming the *pair* of variables and the operator, holding neither variable's value (a DBM entry relates two variables; it is not a per-variable interval). Precept's `FieldToFieldConstraint(LeftField, op, RightField)` is the discrete, named-field analogue of a DBM entry, and the `⊓`-via-`Intersect` narrowing is the discrete analogue of the closure step that propagates a DBM bound into a variable's range. **What Precept deliberately diverges from:** the DBM stores `±x ± y ≤ c` over a *full matrix* closed by an O(n³) Floyd–Warshall pass; Precept does not materialize a matrix or run full closure — it does a single, depth-bounded hop (read `⟦Y⟧`, `⊓` into `⟦X⟧`), the parent design's "octagon closure minus widening, capped at one pass" (parent Decision 4; survey Implication 5: "Precept's single-pass, depth 0–1 is the discrete analogue: do the closure once, do not iterate"). The divergence buys termination and a legible per-rule witness at the cost of completeness (no transitive chains beyond the cap) — the standard Precept trade. The witness-shape choice (name the contributing relation, survey Implication 4) is realized by `FieldToFieldConstraint` naming both fields, so the proof witness can read "`X ≥ Y` narrowed `X`'s lower bound to `Y`'s lower bound" — the octagon/DBM rule-list witness, not a CUE unification trace.

## Inventory of what will be built

- `src/Precept/Pipeline/ProofEngine.cs` — **no change to the records.** `FieldToFieldConstraint` (lines 45-48) is the relational fact shape, reused as-is. `ScopedNumericFact` (lines 88-93) is left untouched (it stays the field-op-constant fact).
- `src/Precept/Pipeline/ProofEngine.Composition.cs` — add a rule-sourced extraction: a field-op-field arm that, when `TryGetNumericConstraintFact`'s constant arms both fail and both operands are field references, yields a `FieldToFieldConstraint` (mirror of the `field op constant` extraction at lines 193-205). This is the wiring point parent Decision 2 names ("the `field op field` case produces nothing today").
- `src/Precept/Pipeline/ProofEngine.Intervals.cs` — extend `ExtractFieldInterval` (lines 202-220) to fold rule-sourced `FieldToFieldConstraint`s into the subject's interval via `Intersect`, after the existing declared-bound + `FlagLowerBound` fold, depth-bounded (read `⟦Y⟧` without recursive relational narrowing). New private helper `RelationalLowerBound`/`RelationalUpperBound` (paralleling `FlagLowerBound`, lines 228+) that reads the related field's interval for each applicable rule.
- `src/Precept/Pipeline/ProofEngine.Strategies.cs` — the rule-sourced path reuses `GuardRelationImpliesObligation` (lines 1213-1238) and the `FieldToFieldConstraint` extraction shape; factor the constraint-extraction so guard-sourced (`ExtractFieldToFieldLeaf`, 1203) and rule-sourced share one leaf-extractor (the single-core requirement).
- Test stubs:
  - `test/Precept.Tests/ProofEngine/RelationalFactRepresentationTests.cs` — `rule OnHand > Reserved` narrows `OnHand` from `Reserved`'s interval; divisor `OnHand - Reserved` discharges; the disposition reports `ProofStrategy.FlowNarrowing`.
  - same file — `Y` unbounded ⇒ identity narrowing ⇒ obligation stays `Unresolved` (the never-over-prove guard).
  - same file — `X >= Y` + `X >= W` conjoin (subject ends at `max` of the two related lower bounds); mutual (`X >= Y` + `Y >= X`) and self (`X >= X`) terminate, no hang.
  - assertion that `precept_compile`'s obligation DTO is byte-identical for a literal-bound precept before/after (the no-DTO-change guard).

## Decisions

### Decision 1: Carry the field-to-field relational fact in the existing `FieldToFieldConstraint` record (a sibling shape to `ScopedNumericFact`), not by extending `ScopedNumericFact` with nullable field-reference fields

**Stakes**: medium (internal vocabulary; reversible pre-release; no public DTO — per parent design "likely medium").

- **Rationale**: `ScopedNumericFact` (`ProofEngine.cs:88-93`) carries a `decimal Value` and is structurally the `field op constant` fact; a `field op field` fact carries *no constant* — it references a second field. These are disjoint shapes for disjoint facts. CLAUDE.md's DU discipline ("don't paper over shape differences with nullable fields on a flat record — use a DU base + sealed subtypes") forbids bolting `string? OtherField` onto `ScopedNumericFact` and nulling `Value`. The decisive observation: the disjoint shape **already exists** as `FieldToFieldConstraint(LeftField, op, RightField)` (`ProofEngine.cs:45-48`), produced today from guards. Reusing it (a) honors DU discipline (two records, two shapes), (b) realizes parent Decision 2's "share the guard-sourced core" *at the data level* — guard-sourced and rule-sourced facts are literally the same record, so the operator-implication core (`GuardRelationImpliesObligation`) is shared by construction, and (c) requires zero new type. A formal shared DU base (`ScopedFact` → `ScopedNumericFact` + `ScopedRelationalFact`) is unnecessary because the two facts flow through *different* consumers (sign-set vs interval-narrowing) and are never collected into one polymorphic list — a base would be ceremony with no dispatch site to serve.
- **Tradeoff accepted**: The two fact records are not unified under a common base, so a future consumer wanting "all facts about field X regardless of shape" would query two collections, not one. Accepted: no such consumer exists today (sign-set reasoning reads `ScopedNumericFact`; interval narrowing reads `FieldToFieldConstraint`), and introducing a base now would be speculative generality forbidden by the DU rule's own spirit (a base earns its place when a polymorphic dispatch site needs it).
- **Alternatives considered**:
  - *(a) Extend `ScopedNumericFact` with nullable `string? OtherField` + a discriminator* — **rejected**: textbook nullable-shape-papering, explicitly forbidden by CLAUDE.md; `Value` becomes meaningless for the relational case, inviting the exact "is this field set?" branching the DU rule exists to kill. Also breaks the sign-set consumers that assume `Value` is meaningful.
  - *(c) A separate relational-fact stream alongside the numeric one, with a brand-new record* — **rejected**: introduces a *third* relation record when `FieldToFieldConstraint` already models the pair, splitting the operator-implication logic across two records and *violating* parent Decision 2's "share the guard core" (the guard path would keep its `FieldToFieldConstraint`, the rule path would get a new twin). Maximal duplication for no gain.
  - *(b) Sibling DU subtype under a new `ScopedFact` base (`ScopedNumericFact` + `ScopedRelationalFact`)* — **rejected as premature**: clean in the abstract, but there is no polymorphic collection or dispatch site that consumes a `ScopedFact` base today; the relational fact is already named by `FieldToFieldConstraint`. Adding the base + a new `ScopedRelationalFact` subtype duplicates `FieldToFieldConstraint`'s shape. If a future consumer needs a unified fact list, the base can be introduced then (Easy reversibility, pre-release).
- **Precedent**: In-tree, `FieldToFieldConstraint` is the existing relation-as-fact record (`ProofEngine.cs:45-48`, `Strategies.cs:1203-1238`). External, the DBM/octagon element is the canonical relation-as-fact (B) representation: "the relation `x − y ≤ c` is the stored fact … the canonical DBM is the tightest derivable constraint set" — a record about the *pair*, holding no per-variable value, exactly `FieldToFieldConstraint`'s shape (survey § "Gap 2"). The survey's central caution — A-vs-B is orthogonal to bounded-vs-fixpoint, so the pick turns on blast radius / witness uniformity — favors reuse: `FieldToFieldConstraint` minimizes blast radius (zero new type) and gives a uniform named-field witness.
- **Sources consulted for this decision**: `src/Precept/Pipeline/ProofEngine.cs:88-93` — `ScopedNumericFact(NumericSubjectRef Subject, OperatorKind Comparison, decimal Value, …)` (carries a constant; cannot hold X op Y); `ProofEngine.cs:45-48` — `record FieldToFieldConstraint(string LeftField, OperatorKind Comparison, string RightField)` (the existing relation record); `ProofEngine.Strategies.cs:1203-1211` — `ExtractFieldToFieldLeaf` building `FieldToFieldConstraint` from guards; `CLAUDE.md § Catalog System` — "Use discriminated unions for varying shapes. Don't paper over shape differences with nullable fields on a flat record"; `research/architecture/compiler/relational-constraint-representation-survey.md § "Gap 2"` — "`x − y ≤ c` is the stored fact". Grepped the parent design Decision 2: it locks **B** and "shares the guard-sourced core … NO public-DTO change … witness = named-rule list" but explicitly leaves the *concrete internal* shape open ("does not pin the concrete internal fact representation") — so this is a genuinely-open mechanism decision under a locked conceptual decision, not a re-litigation. Grepped `precept-language-spec.md §0.6/§0.7` — they specify the relational-reasoning *contract*, not the C# record; spec is silent on the concrete representation.
- **Strongest counter-evidence**: The DU rule's letter ("use a DU base + sealed subtypes") could be read to *mandate* the formal `ScopedFact` base (alternative b). Response: the rule's *purpose* is to forbid nullable-field papering (alternative a), which reusing `FieldToFieldConstraint` fully satisfies — two records, two shapes, no nullable discriminator. A base is required when a *polymorphic dispatch site* must treat varying shapes uniformly; there is none here (the two facts have disjoint consumers), so the base is the speculative-generality the rule does not demand. Reuse is the lower-blast-radius reading that satisfies the rule's intent.
- **Reversibility**: Easy — both records are `private` to `ProofEngine`, pre-release, no external consumer. Introducing a `ScopedFact` base later (if a unified consumer appears) is a mechanical refactor.
- **Blast radius**: `src/Precept/Pipeline/ProofEngine.Composition.cs` (extraction), `ProofEngine.Intervals.cs` (consumption), `ProofEngine.Strategies.cs` (shared leaf-extractor). No catalog, no DTO, no docs beyond the proof-engine stage doc, no samples, no external consumer.

### Decision 2: Compose the relational fact into the subject's interval via `NumericInterval.Intersect` inside `ExtractFieldInterval`, reading the related field's pre-existing interval (one hop), sharing `GuardRelationImpliesObligation`'s operator core

**Stakes**: medium.

- **Rationale**: `ExtractFieldInterval` (`Intervals.cs:202-220`) is already the single place a field's operand interval is assembled for divisor/range/sqrt/`IntervalContainment` discharge, and it already folds multiple bound sources (`GetFieldBounds` + `FlagLowerBound`) by tightening. Adding the relational fold there means every downstream obligation that reads a field's interval automatically benefits — one wiring point, no per-obligation plumbing. `Intersect` (`NumericInterval.cs:143`) *is* the `⊓` meet the parent design's R-NARROW rules call for, and it is sentinel-safe (Unbounded acts as identity), which gives the required identity-degradation for free. Reading `⟦Y⟧` without recursive relational narrowing of `Y` is exactly the depth-≤1 single-pass bound (parent Decision 4). The operator-implication (`>=`/`>` ⇒ lower-bound family, `<=`/`<` ⇒ upper-bound family) reuses `GuardRelationImpliesObligation`'s truth-table (`Strategies.cs:1226-1237`), so guard-sourced and rule-sourced narrowing share one core — satisfying parent Decision 2's "share, not fork."
- **Tradeoff accepted**: Folding into `ExtractFieldInterval` means the relational narrowing runs on every interval read, a small repeated cost (one extra scan of `semantics.Rules` per field-interval read). Accepted: the corpus compiles in ~44ms (project memory: compiler is fast, no incremental); the scan is linear in rule count and the interval read is already not on a hot inner loop. If profiling later shows it, a per-field relational-fact index is a trivial memoization (Easy).
- **Alternatives considered**:
  - *(A standalone relational discharge strategy that bypasses `ExtractFieldInterval`)* — rejected: would duplicate the interval-assembly logic and miss obligations that read the interval through the existing path (the `IntervalContainment` and overflow families); folding into the one interval-read point is strictly less code and strictly more coverage.
  - *(Recursively narrow `Y` from a third field before reading `⟦Y⟧`)* — rejected: that is the fixpoint/transitive-chase parent Decision 4 forbids (§0.4 no widening); one hop is the locked depth.
- **Precedent**: `ExtractFieldInterval`'s existing multi-source `⊓`-fold (`Intervals.cs:206-217`: `GetFieldBounds` then `Math.Max` with `FlagLowerBound`) is the in-tree precedent for "tighten the operand interval from another bound source." `GuardRelationImpliesObligation` (`Strategies.cs:1213-1238`) is the in-tree precedent for the operator-implication core. External: octagon strong closure propagates a DBM bound into a variable's range via the meet — "the closed form *is* a rule list" (survey § "Gap 1"), the single-pass analogue here.
- **Sources consulted for this decision**: `src/Precept/Pipeline/ProofEngine.Intervals.cs:202-220` — `ExtractFieldInterval` body, the `GetFieldBounds` + `FlagLowerBound` `Math.Max` fold; `src/Precept/Language/NumericInterval.cs:143-150` — `Intersect` (the `⊓`, sentinel-safe: `if (IsUnbounded) return other`); `src/Precept/Pipeline/ProofEngine.Strategies.cs:1213-1238` — `GuardRelationImpliesObligation` operator table (the shared core); `ProofEngine.Strategies.cs:1100-1131` — the guard-sourced relational discharge flow this mirrors; `docs/Working/relational-rules-and-bounds-design-2026-06-02.md § Semantic Rules` — R-NARROW-GE/LE (`⟦X⟧ ⊓ [ylo,+∞)`); `research/architecture/compiler/relational-constraint-representation-survey.md § "Gap 1"/Implication 5` — single-pass closure analogue. Grepped `precept-language-spec.md §0.4` — single-pass/no-widening is locked; this composition is implementation against it.
- **Strongest counter-evidence**: Folding relational facts into `ExtractFieldInterval` couples the relational mechanism to the interval-read path, which is also used by the satisfiability scan (`Intersect` is used there too, `NumericInterval.cs:138-142`) — risking the narrowing leaking into satisfiability where it isn't wanted. Response: the satisfiability scan computes per-rule intervals and intersects *those* (a different call site), not `ExtractFieldInterval`; the relational fold is added only to the operand-interval-read path (`ExtractFieldInterval`), so satisfiability is unaffected — and even if it read through, a *tighter* operand interval can only make a contradiction *more* detectable, never mask one (sound either way).

### Decision 3: The narrowing is sound by construction — `⊓`-tighten-or-identity, never widen — so it can never manufacture a false "safe"

**Stakes**: medium.

- **Rationale**: Every contribution from a relational fact is a `NumericInterval.Intersect` of `⟦X⟧` with a half-line derived from `⟦Y⟧`'s *already-established* bound. `Intersect` is monotone-decreasing on the interval lattice (the result is `⊆` both operands, `NumericInterval.cs:147-149`), so it can only shrink `X`'s provable range; and when `⟦Y⟧`'s relevant bound is the `±∞` sentinel, `Intersect` returns `⟦X⟧` unchanged (`NumericInterval.cs:145-146`). Therefore no relational fact can ever *add* coverage `X` didn't already have — it can only remove uncertainty by tightening from a real, declared `Y`-bound. An obligation the narrowed interval doesn't cover stays `Unresolved` and rejects (§0.7).
- **Tradeoff accepted**: False-negative friction — when `Y` is unbounded, the author must add a bound to `Y` even when the program is "obviously" safe. Accepted (this is the parent design's Principle-1/7/10 tradeoff, inherited): never emit a false "proved safe."
- **Alternatives considered**: *(treat unbounded `Y` optimistically — assume `Y >= 0` for a `nonnegative`-typed-looking field)* — rejected: any optimism beyond the declared bound is exactly the over-prove that breaches Principle 1; the `FlagLowerBound` fold already captures *declared* `nonnegative` soundly, and nothing beyond a declared bound may be assumed.
- **Precedent**: `ExtractFieldInterval`'s existing comment (`Intervals.cs:210-213`): "A field's value genuinely lies within these bounds, so this can only tighten the operand interval and help discharge; it never creates a rejection." The relational fold is the same monotone-tightening discipline applied to a second-field source. External: octagon closure is sound (the closed DBM is the *tightest* set of *implied* bounds, never an over-approximation in the unsafe direction) — survey § "Gap 1" (closure derives "the tightest implied two-field bounds").
- **Sources consulted for this decision**: `src/Precept/Language/NumericInterval.cs:143-150` — `Intersect` semantics (result `⊆` both operands; `Unbounded` identity); `src/Precept/Pipeline/ProofEngine.Intervals.cs:208-217` — the "can only tighten … never creates a rejection" soundness comment on the existing fold; `docs/language/precept-language-spec.md §0.7` — "no deferral" (an unproved obligation rejects, never defers); `docs/Working/relational-rules-and-bounds-design-2026-06-02.md § Falsifiers` — the never-over-prove falsifier ("If the relational-narrowing path ever discharges an obligation that a runtime trap then fires on … the mechanism is unsound"). Grepped `§0.1` Principles 1/7/10/11 — soundness-over-completeness is locked; this is implementation against it.

## Falsifiers

This design locks no language surface and no external-author-visible contract (it is proof-engine-internal), but it ships the soundness-critical discharge mechanism, so the never-over-prove falsifier is load-bearing:

- **Never-over-prove (load-bearing).** If a precept whose only relevant bound is a relational fact compiles clean and a runtime trap (`OutOfRange`, divide-by-zero, `CountBoundViolation`) then fires on it, the `⊓`-tighten-or-identity claim (Decision 3) is false and the mechanism is unsound — immediate redesign. This is the Principle-1 falsifier and the most important one.
- **Identity-degradation broken.** If `rule X >= Y` with `Y` unbounded ever narrows `X`'s lower bound to anything other than `X`'s pre-existing lower bound (i.e. `Intersect` with the `±∞` half-line is not the identity), the sentinel handling is wrong; verify `NumericInterval.Intersect` against an Unbounded operand.
- **Witness opacity.** If the narrowed interval's disposition cannot name the contributing rule (the `FieldToFieldConstraint` provenance is lost before the witness is built), the §0.6-item-3 inspectability claim is falsified and the shape must carry rule-index provenance — reconsider whether `FieldToFieldConstraint` needs a `RuleIndex` field.
- **DTO leak.** If realizing B requires any change to `IntervalContainmentProofRequirement`, `CompileToolDtos.cs`, or `CatalogFormatters.cs`, then the parent design's Decision-2 no-DTO-change invariant (its Falsifier #4) is breached and the A-vs-B call must be revisited — flag loudly to the owner. *(Verified false at design time: both fact records are private to ProofEngine; the DTO reads only `IntervalContainmentProofRequirement.DeclaredMin/Max`.)*

## Acceptance criteria

- A precept with `rule OnHand > Reserved` and `Reserved nonnegative` makes `BatchCost / (OnHand - Reserved)` discharge — **compiles clean** — and the divisor obligation's disposition reports `ProofStrategy.FlowNarrowing`. (`RelationalFactRepresentationTests`)
- The same precept with `Reserved` unbounded in the relevant direction **fails to compile** (obligation stays `Unresolved`) — the identity-degradation never manufactures a bound. (`RelationalFactRepresentationTests`)
- Two relational facts on one field (`X >= Y`, `X >= W`, both `Y`/`W` bounded) narrow `X`'s lower bound to `max(Ylo, Wlo)` — verified by a range obligation that discharges iff the conjoined bound covers it. (`RelationalFactRepresentationTests`)
- Mutual (`A >= B` + `B >= A`) and self (`X >= X`) relational facts **compile and terminate** (no hang, no stack growth) — confirming single-pass, no fixpoint. (`RelationalFactRepresentationTests`)
- A literal-bound-only precept produces a **byte-identical** `precept_compile` obligation DTO before and after this change — the no-DTO-change invariant. (`RelationalFactRepresentationTests` snapshot assertion)
- No new `ProofRequirement`, `ProofRequirementKind`, or `ProofStrategy` enum member is added (verified by diff). (review gate)
- Full suite green; `FieldToFieldConstraint` extraction is exercised from both a guard source and a rule source through the shared leaf-extractor. (`RelationalFactRepresentationTests` + existing guard-narrowing tests)

## Dependencies

- **Upstream**: `docs/Working/relational-rules-and-bounds-design-2026-06-02.md` — Decision 2 (= B, locked) and Decision 4 (single-pass) are the locked conceptual frame this doc realizes. The existing `FieldToFieldConstraint` record, `GuardRelationImpliesObligation` core, `NumericInterval.Intersect`, and `ExtractFieldInterval` (all shipped).
- **Downstream**: Unblocks the parent design's build slice (it can now reference a concrete fact shape). The narrowed interval feeds the BUG-017 `IntervalContainment` discharge and the divisor/range/sqrt families.

## Doc-update enumeration

Per the CLAUDE.md routing table — promote-stage obligations for `/lifecycle-5-promote`, **not edited this pass**:
- `docs/compiler/proof-engine.md § Proof Strategies / Strategy 4 (FlowNarrowing)` — document that the field-to-field relational fact (`FieldToFieldConstraint`) is now sourced from declared rules as well as guards, and that it composes through `ExtractFieldInterval` via `Intersect`; note the depth-≤1 single-pass bound.
- `docs/compiler/proof-engine.md § Internal Representations` (or equivalent) — record that `FieldToFieldConstraint` is the relational-fact shape and `ScopedNumericFact` stays the field-op-constant shape (the disjoint-shape rationale).
- `docs/tooling/mcp.md` — **no change** (Decision 2/3: no DTO, no formatter change); affirm the no-change explicitly so a future audit doesn't re-investigate.

## Operational dimensions

- **Observability** (triggered — touches proof/diagnostic surface): a relational narrowing that fails to discharge surfaces through the existing obligation diagnostic with `ProofStrategy.FlowNarrowing` attribution; the contributing rule is recoverable because `FieldToFieldConstraint` names both fields (witness provenance, §0.6 item 6). No new observability surface beyond what the parent design specifies.
- **Security**: N/A — no source-text-ingestion change (no new tokens/parser paths; proof-engine-internal).
- **Evolvability**: N/A — no external-standard dependency (operates on already-normalized decimal interval bounds).

## Open questions

None blocking lock at this stage. One item for the `precept-reviewer` pass to confirm (not blocking advancement to Externally-Grounded): whether the witness needs `FieldToFieldConstraint` to carry an explicit `RuleIndex` for provenance, or whether the rule is recoverable from the field-pair + operator at witness-build time. The Witness-opacity falsifier covers the failure mode; the reviewer/build slice resolves the representation detail (add a field vs recover by lookup) — it does not change the A-vs-B or sibling-vs-base decisions locked here.
