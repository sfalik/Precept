---
status: Locked 2026-06-03
phase-target: Phase 7 (total language conformance sweep) — count-bound containment is one of the three bound-containment breaches (BUG-017/018/019); couples to the value-level obligation emit seam tracked alongside the relational-rules-and-bounds lock
comparable-systems-research-status: strong — grounded in `research/architecture/compiler/count-cardinality-bound-proof-survey.md` (Stage-1 survey: Dafny membership-guarded set-cardinality axiom, SPARK formal-container Insert precondition, SMT finite-sets+cardinality LMCS 2018, sized-types AI TPLP 2014, Liquid Haskell, with verbatim mirrored excerpts) — and the committed length/divisor siblings in `proof-engine.md` / `ProofEngine.Lengths.cs`
sources-consulted:
  - "git show HEAD:docs/language/precept-language-spec.md §0.6 — Proof philosophy items 1 (soundness over completeness), 2 (proven violations only), 5 (truth-based three-way classification), 7 (sequential proof flow, boolean count model + named incompleteness)."
  - "git show HEAD:docs/language/precept-language-spec.md §0.6 items 3 (divisor obligation, 'supply a constraint … a guard') and 4 (non-negative obligation)."
  - "git show HEAD:docs/language/precept-language-spec.md §0.7 — fault prevention 'rejects the definition … no deferral'; 'an author guard' named as a carrier; governance 'on external input' scoped to event args/construction/edits; the boundary paragraph."
  - "git show HEAD:docs/language/precept-language-spec.md §3A.4 — Mutation Atomicity: post-mutation sweep re-checks every constraint against the completed working copy; ingress vs sweep two enforcement points (lines 1963-1969)."
  - "git show HEAD:docs/language/precept-language-spec.md §2.4 — constraint modifier desugars to the equivalent rule (line 1133); modifier↔type applicability (mincount/maxcount → collections, line 1672); InvalidModifierBounds / InvalidModifierValue (lines 1689-1690)."
  - "git show HEAD:docs/compiler/proof-engine.md — Strategy 10 Count Containment ('always-unresolved by design … no value-establishing site … present so future whole-collection-value operations flow through a uniform path … CountBoundViolation when emission is wired, currently in the Gate 1 allow-list'); Obligation Generation Contract item 2 ('maxcount 10 must produce a count-containment obligation on every mutation that grows c'); Sequential proof flow (boolean grow/shrink, named count-interval incompleteness)."
  - "git show HEAD:src/Precept/Pipeline/ProofEngine.Lengths.cs — TryLengthContainmentProof: returns false (provable violation) OR null (unprovable, e.g. unbounded source into capped field), BOTH emit; doc comment 'Per §0.7 prove-or-reject, both false and null leave the obligation unresolved so the diagnostic is emitted'."
  - "precept_diagnostic CountBoundViolation (PRE0136) — Proof/Error; 'Collection count {0} is outside the declared bounds [{1}..{2}] on field {3}'."
  - "precept_diagnostic UnguardedCollectionMutation (PRE0064) — Type/Safety; guard-naming message ''{0}' may be empty — guard with 'when {0}.count > 0' …, or apply 'notempty''; the empty-collection precondition analog of the guard-naming shape."
  - "samples/shopping-cart.precept:94 — the one count-bounded usage in the corpus: 'CatalogItems as set of string notempty mincount 1' is an EVENT ARGUMENT on Create (ingress/governance), set whole via 'set Catalog = Create.CatalogItems'; no in-transition add/remove mutation against an in-place count-bounded field exists in samples/."
  - "research/architecture/compiler/count-cardinality-bound-proof-survey.md — Dafny set-card axiom (!a[x] ⇒ +1, a[x] ⇒ +0), multiset unconditional +1; SPARK Insert precondition 'Length < Capacity OR Contains'; SMT finite-sets+cardinality Venn-region blowup (Bansal et al. LMCS 2018); sized-types AI size-as-interval (Serrano et al. TPLP 2014); Liquid Haskell membership-without-cardinality."
  - "docs/Working/relational-rules-and-bounds-design-2026-06-02.md (Locked) — frames the three containment breaches BUG-017/018/019 as 'emitted unconditionally and discharge-or-emit'; establishes the obligation-emission mandate. Cited for the emission-side mandate; the count discharge LINE within it is the artifact this design supersedes."
  - "git diff HEAD src/Precept/Pipeline/ProofEngine.cs / ProofEngine.Lengths.cs / ProofRequirement.cs — the uncommitted BUG-018 artifact-under-review (CountLowerAfter/CountUpperAfter post-mutation interval; emit-only-on-provable-violation; 'a single unprovable add stays clean'). Described as disputed artifact, not endorsed."
  - "git show HEAD:docs/compiler/diagnostic-system.md — LengthBoundViolation (PRE0135) 'an unbounded source flowing into a capped field emits'; CountBoundViolation = 136; proof-stage obligation codes."
---

**Promoted to:** 2026-06-04 — `docs/compiler/proof-engine.md` (§ Strategy 10: Count Containment Proof; § Sequential proof flow — count-band tracking; § Obligation Generation Contract item 2), `docs/language/precept-language-spec.md` (§ 0.6 item 7 — count-interval alongside the boolean non-empty model), `docs/compiler/diagnostic-system.md` (PRE0136 `CountBoundViolation` obligation entry, off the Gate-1 allow-list), `docs/language/collection-types.md` (§ Constraint Catalog — `mincount`/`maxcount` proof participation). BUG-018 in `docs/Working/bugs.md` reconciled to the Reading-A prove-or-reject line. (Shipped commit `c27a382b`.)

# Count-bound discharge semantics for collection `mincount`/`maxcount` on mutations

## Goal

When done, a transition that mutates a count-bounded collection field (`add`/`remove`/`clear` against a `mincount`/`maxcount` field, plus the field's `default [...]` literal) is governed by a single, spec-grounded discharge rule that names *exactly when the compiler emits `CountBoundViolation` (PRE0136) versus compiles clean*, and assigns ownership of that rule to a named spec mechanism — demonstrated by a regression precept where an unguarded `add` into a `maxcount`-bounded field rejects with a message naming the guard that would make it provable, while the same `add` under `when C.count < N` compiles clean.

## Scope

- **In scope**: the discharge *line* for count-bound enforcement on the count-changing mutation shapes (`add`/grow, `remove`/`clear`/shrink) plus the `default [...]` literal; the violation-vs-obligation classification of an unproven count mutation; which spec mechanism owns count discharge; the diagnostic kind/wording under the chosen reading; the dedup-set delta precision question (only if Reading B is chosen).
- **Out of scope**: numeric `min`/`max`, string `minlength`/`maxlength`, divisor/non-negative obligations (settled committed siblings, cited as precedent only); the boolean `count > 0` non-empty model for accessor safety (committed §0.6 item 7, unaffected — this design is about the *cardinality band*, not the non-empty fact); whole-collection-value operations that don't yet exist in v1 (Strategy 10's future path); membership-across-shrink (committed out-of-scope).
- **Deferred to future**: Option-2 bounded literal-distinctness precision (only meaningful under Reading B; sequenced after the floor); exact count-interval tracking beyond the boolean model for non-empty accessor safety (committed as a future refinement, orthogonal).

## Philosophy Alignment

| Principle | Affected? | How served (1 sentence + cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | Under the chosen reading (A), an unprovable count mutation is rejected at compile time and the invalid configuration is made structurally impossible before any instance exists (`philosophy.md` line 49–53; §0.7 fault prevention). | Reading B would push the catch to runtime governance, which is still prevention (working-copy discard, §3A.4) but detection-shaped at compile time. | Accepted: compile-time rejection over a runtime-only catch (see Decision 1). |
| 2. One file, complete rules | Y | The count band and the guard that discharges it both live in the one `.precept` file; nothing is enforced outside the contract (§0.7 governance). | N/A | N/A |
| 3. Determinism | Y | The discharge rule is a pure function of the action chain + declared band + guard; same definition ⇒ same verdict (§0.6 item 7 sequential flow). | N/A | N/A |
| 4. Full inspectability | Y | The count obligation is a structured `CountContainmentProofRequirement` surfaced via `precept_proofs`/hover; no opaque solver (§0.6 item 3). | N/A | N/A |
| 5. Keyword-anchored readability | N | No new keyword, type, operator, modifier, or construct — `mincount`/`maxcount` and `when` already exist; discharge is a pipeline-internal proof rule. | N/A — discharge semantics, not surface | N/A |
| 6. Governance not validation | Y | The count band is enforced structurally on every mutation, not by a boundary validator that can be bypassed (§0.7 governance, §3A.4 sweep). | Reading A's compile-time reject and Reading B's runtime sweep are both governance; the question is *where in the contract* the enforcement first bites. | N/A |
| 7. Compile-time totality | Y | Reading A discharges the count obligation at compile time or rejects — `CountBoundViolation` is `[StaticallyPreventable]` for fault `CountBoundViolation`, so the totality claim is honored (§0.7 'no deferral'). | §0.6 item 2 'proven violations only' resists rejecting a *possible* (not proven) overflow; this is the crux. | Accepted: name a guard on the unprovable grow (Decision 1). |
| 8. Honesty about approximation | Y | The discharge rule states exactly which mutations are provable and which require a guard; no mutation is silently presented as proven-safe (§0.6 item 5 three-way classification). | N/A | N/A |
| 9. Mandatory rationale (`because`) | N | Count modifiers desugar to a rule with a generated rationale (§2.4 line 1133); no author-facing `because` change. | N/A | N/A |
| 10. Static semantic checking | Y | The count band is checked statically against the post-mutation interval; an unprovable grow is not deferred to a runtime type error (§0.7 Principles 7/10/11). | Same crux tension as Principle 7. | Accepted (Decision 1). |
| 11. Static completeness (no runtime faults from well-typed programs) | Y | `CountBoundViolation` is `[StaticallyPreventable(CountBoundViolation)]`; Reading A makes the runtime count-fault path unreachable for contract data, closing the soundness hole BUG-018 named (§0.7; `philosophy.md` 'make every evaluator error path unreachable'). | Under Reading B the compile-time check is best-effort and the runtime sweep is the *primary* enforcer for unprovable adds — which §0.7 says runtime checks must never be ('never … in the hope a runtime check catches it'). | This tension is the decision (Decision 2 classification). |

**Tradeoff statements (Affected=Y rows with non-N/A tradeoff).** Principles 7/10/11 carry the load-bearing tradeoff: choosing Reading A (obligation; emit-and-name-a-guard on the unprovable grow) accepts a one-time author guard-tax on count-bounded grows in exchange for keeping the §0.7 "no deferral / prove-or-reject" contract intact and the `[StaticallyPreventable]` totality claim honest. The tradeoff is justified because count-bound is in §0.7's *fault-prevention* bucket ("no result outside a declared bound"), and §0.7 forbids leaving a fault-prone operation to a runtime check; the guard-tax is the same legitimate obligation the divisor (§0.6 item 3) and non-negative (item 4) families already impose, and the corpus cost is zero in-transition usages today (one count-bounded sample field, and it is an event-arg governed at ingress, not an in-transition mutation).

**Companion commitments.** *Stateless-first-class*: count-bounded collection mutations occur in stateless precepts identically (event hooks / `update`) — the discharge rule is state-agnostic, walking action chains uniformly (§3A.4 'all mutation surfaces … uniform'). *Domain-expert-primary-author*: the chosen reading's diagnostic must name the fix in domain vocabulary ("guard with `when Items.count < 10`") rather than "obligation unresolved" — addressed in Audience and Teachability below; this is the decisive ergonomic axis the decision turns on.

## Language Design Grounding

**General language design.** The question — *is a bounded-container capacity violation a static-error-requiring-proof, or a runtime check?* — has a well-mapped answer space across prevention-grade systems, surveyed verbatim in `research/architecture/compiler/count-cardinality-bound-proof-survey.md`:

- **SPARK / Ada formal containers (GNATprove)** make capacity a *discharged precondition*: bounded `Append` carries `Pre => Length (Container) < Container.Capacity`, and set `Insert` relaxes it to `Length < Capacity OR Contains (Container, New_Item)` (a duplicate add cannot overflow). The analyzer never forward-infers `|s ∪ {x}|`; the caller proves the precondition. SPARK has *no runtime backstop*, so it must be complete-given-precondition — the strongest precedent that capacity is a static obligation, not a runtime check.
- **Dafny (Boogie/Z3)** axiomatizes set cardinality membership-conditionally — `!a[x] ⇒ Set#Card(a∪{x}) == Set#Card(a)+1`, `a[x] ⇒ … == Set#Card(a)` — with multiset add unconditional `+1`. This is the set-vs-multiset asymmetry directly. But discharge is via Z3, an *opaque solver* — which Precept's §0.6 item 3 ("Opaque solvers are rejected on principle") excludes. The axiom *shape* transfers; the *discharge mechanism* does not.
- **SMT finite-sets-with-cardinality** (Bansal/Barrett/Reynolds/Tinelli, LMCS 2018) shows the general membership×cardinality combination is decidable but Venn-region-combinatorial ("reasoning about the cardinalities of Venn regions is the main bottleneck"). This bounds how far Precept can go without a solver: only *bounded* literal-distinctness within a single loop-free transition stays legible.
- **Sized types / abstract-interpretation resource analysis** (Serrano/Lopez-Garcia/Hermenegildo, TPLP 2014) abstracts a collection's size as an inferred `[lower, upper]` numeric interval propagated through forward AI — "sound but incomplete," with trivial bounds where the delta is data-dependent. This is the recognized AI domain shape Precept's count-interval reuses, and grounds that its incompleteness on dedup-set adds is the *expected* tradeoff of staying count-only.
- **Liquid Haskell** is a negative datapoint: a mature refinement system models set *membership* and deliberately omits set *cardinality* — confirming the two are separable.

What Precept takes: SPARK's "capacity is a static obligation discharged by a carried fact (a guard)" — adapted from precondition to Precept's `when`-guard carrier (§0.7 names "an author guard" as a carrier). What Precept diverges from: Dafny/SMT's solver-based membership-aware exactness (excluded by §0.6 item 3); Precept stays count-only-plus-bounded-literal-distinctness, accepting sized-types-style incompleteness on data-dependent deltas.

This domain has a dedicated research file (cited); no gap.

**Precept-specific application.** Touches §0.6 (item 2 proven-violations-only vs items 3/4/5 obligation classification), §0.7 (fault-prevention no-deferral; governance-on-external-input scope), §2.4 (count modifier = rule sugar), §3A.4 (post-mutation sweep). It does not introduce surface; it resolves the discharge line for an existing modifier family. The §0.8 audience constraint (no iteration, keyword-anchored) is unaffected.

## Audience and Teachability

**Worked example** (inventory reservation — a domain-expert scenario, plausible `maxcount` cap on a bounded reservation list):

```precept
precept ReservationSlot
field Holders as set of string maxcount 3
state Open initial
state Full terminal
event Reserve(MemberId as string notempty)
# Reading A — the author guards the grow so the cap is provable:
from Open on Reserve when Holders.count < 3
    -> add Holders Reserve.MemberId
    -> no transition
from Open on Reserve -> to Full   # cap reached: route elsewhere, do not over-add
```

The domain expert reads top-to-bottom: "reserve only while there's room; otherwise the slot is full." The guard `when Holders.count < 3` is the carrier that discharges the count obligation — the same shape as the already-shipped non-empty guard `when Q.count > 0` (PRE0064).

**Error message** (the misuse: an unguarded `add` into a capped field). Under the chosen reading (A), `CountBoundViolation` (PRE0136) is re-pointed from its current value-shaped wording to a guard-naming, obligation-shaped wording:

```
PRE0136  'Holders' may exceed its maximum of 3 — guard with 'when Holders.count < 3'
         before 'add', or raise the 'maxcount' on the field. (count band [0..3])
```

This serves the domain-expert reader because it names the *fix in their vocabulary* (a `when` clause they already write for emptiness) rather than reporting "count-containment obligation unresolved." It deliberately mirrors the shipped PRE0064 non-empty message (`'{0}' may be empty — guard with 'when {0}.count > 0' …, or apply 'notempty'`) so the two collection-safety obligations teach as one pattern. (The symmetric `mincount` underflow message names the floor and the shrink: `'Holders' may fall below its minimum of 1 — guard the 'remove' with 'when Holders.count > 1'`.)

**10-minute teaching path.**
1. `samples/shopping-cart.precept` lines 20–31, 124–135 — read a real collection-mutation transition (3 min).
2. Spec §2.4 `mincount`/`maxcount` rows + the "constraint modifier desugars to a rule" paragraph (2 min).
3. The PRE0064 `UnguardedCollectionMutation` diagnostic (via `precept_diagnostic` or hover) — the analogous non-empty guard the author already knows (2 min).
4. The worked example above (3 min).

A competent domain expert who already guards `remove` for emptiness transfers the pattern to count caps in well under 10 minutes — the guard idiom is identical.

## Semantic Rules

**Setting.** A count-bounded field `C` declares `[mincount Lo? .. maxcount Hi?]` (either bound optional). A transition is a loop-free, straight-line action chain (§0.4). Count discharge tracks a single per-field count interval `⟦C⟧ = [lo, hi]` (hi `= ∞` when no `maxcount`), seeded once on first touch and advanced per action by its catalog `ActionMeta.Effect` delta.

**Seeding.** On the first action in the chain that touches `C` (grow or shrink), seed `⟦C⟧ := [Lo ?? 0, Hi ?? ∞]`, then narrow by any same-context `count`-comparison guard (`when C.count < N` ⇒ `hi := min(hi, N−1)`; `when C.count > N` ⇒ `lo := max(lo, N+1)`; etc.). The seed encodes the governed invariant: entering the chain, `C` already satisfies its declared band (§3A.4 — prior committed state was swept).

**Per-effect delta** (catalog-classified `ActionEffectClass`, never an `ActionKind` list):
```
grow  (Grows ∧ EstablishesValue):  list/queue/stack/log/multiset:  [lo, hi] → [lo+1, hi+1]
                                    set/lookup (dedup):             [lo, hi] → [lo,   hi+1]   (Reading B only; see Decision 3)
shrink (Shrinks):                  [lo, hi] → [max(lo−1,0), hi−1 floored at 0]
clear  (Empties):                  [lo, hi] → [0, 0]
set C = ⟨non-literal⟩ (ReplacesValue):     drop ⟦C⟧ (unknown count; re-seed on next grow); emit nothing
                                           (a literal RHS is type-rejected, not a count site — see case F)
default [e₁…eₙ]:                   point obligation [n, n]
```

**Emit condition — the decision (Reading A, chosen).** Each count-bounded mutation stamps a `CountContainmentProofRequirement` carrying the post-mutation `[lo, hi]`. The obligation discharges (compiles clean) **iff the post-mutation interval is provably within the band**: `lo ≥ (Lo ?? 0)` *and* `hi ≤ (Hi ?? ∞)`. Otherwise — a provable violation **or** a merely-unprovable case (e.g. an unguarded grow whose seed `hi` was `∞`-capped at `Hi`, giving post-`hi = Hi+1 > Hi`) — the obligation is **unresolved and `CountBoundViolation` emits**, naming the guard. This is the *prove-or-reject* line, identical to the committed length sibling (`TryLengthContainmentProof`: both `false` and `null` emit).

> Reduction sketch (Reading A):
> ```
> E[ add C x ]  with  ⟦C⟧ = [lo, hi]  and band [Lo..Hi]
>   ─ if (hi+1) ≤ Hi  ⇒  ⟦C⟧' = [lo+1, hi+1],  obligation DISCHARGED
>   ─ else            ⇒  CountBoundViolation(C, [Lo..Hi]) EMITTED, names `when C.count < Hi`
> ```
> The guard `when C.count < Hi` narrows the seed `hi := Hi−1`, so `hi+1 = Hi ≤ Hi` discharges — the guard is the carrier (§0.7).

**Typing rule.** No new typing. `mincount`/`maxcount` applicability is already typed (§2.4: collections only; `InvalidModifierForType` on scalars). Count discharge is a proof-stage obligation, not a type rule:
```
  C : collection,  C declares [Lo..Hi],  action a mutates C,  ⟦C⟧_after = δ(a, ⟦C⟧_before)
  ⟦C⟧_after ⊆ [Lo..Hi]
  ────────────────────────────────────────────────────────────────────────────────  (CountContain-Discharge)
  Γ ⊢ a ok
```
Premise failure ⇒ `CountBoundViolation`.

**Proof obligation.** Maps to the existing `CountContainmentProofRequirement` (catalog `ProofRequirementKind.CountContainment`, Strategy 10). This design *activates* the strategy for the per-mutation grow/shrink/clear/literal sites (committed Strategy 10 only covered whole-collection-value sites and was always-unresolved); the catalog DU subtype and diagnostic already exist.

**Soundness preservation claim.**
- **Principle 7 (totality)** holds because every count-bounded mutation is discharged-or-rejected at compile time; no count mutation is compiled "in the hope a runtime check catches it" (§0.7).
- **Principle 10 (static semantic checking)** holds because the post-mutation interval is computed statically from the action chain and declared band; the guard is a static carrier, not a runtime value.
- **Principle 11 (static completeness)** holds because `CountBoundViolation` is `[StaticallyPreventable(CountBoundViolation)]` — Reading A makes the runtime count-fault path unreachable for contract data, exactly closing the BUG-018 hole. (Under Reading B, Principle 11's claim for unprovable adds would rest on the §3A.4 runtime sweep, which §0.7 forbids as the *primary* enforcer — the soundness reason Reading A is chosen.)

The delta model is sound by construction: grow `+1` (both bounds for ordered/multiset kinds) never under-counts; shrink `−1` floored at 0; the seed over-approximates from the governed band. The one *un*sound cell is the dedup-set lower-bound `+1` (the BUG-018 follow-up) — addressed in Decision 3, and *moot* under Reading A worst-case (a guarded or routed grow is sound for all kinds; an unguarded grow emits regardless of kind).

## Architecture Grounding

### Precept-internal placement

**Layer placement.** Two layers, matching the committed length sibling:
- **Catalog metadata** owns *what* is obligated: `ModifierMeta.ProofSatisfactions` for `mincount`/`maxcount` (`Numeric(Accessor("count"), ≥/≤, DeclarationValue)`, committed proof-engine.md lines 786–787, 1022–1023) and `ActionMeta.Effect`/`WriteSemantics` for the per-action delta classification. Emission must be catalog-derived per the committed Obligation Generation Contract item 1 ("derive emission from modifier metadata, not from `TypeKind` checks").
- **Proof engine** owns *how* it discharges: the sequential count-interval walk in `WalkActions` (reusing the committed `ActionEffectClass` machinery that already drives the boolean non-empty model, §0.6 item 7) and `TryCountContainmentProof` (Strategy 10). This belongs in the proof engine, not the catalog, because it is flow-sensitive (sequential per §0.6 item 7) — catalog metadata is per-modifier/per-action, not per-chain.

The discharge *line* (emit-on-unprovable vs emit-on-provable-violation) is the *only* novel decision; it sits in `TryCountContainmentProof`'s return contract, exactly where the length sibling's `null ⇒ emit` line sits.

**Cross-component propagation.**
- **Runtime (parser, type checker, evaluator, diagnostics)**: Type checker — count obligation stamped on grow/shrink/clear/literal sites (currently only whole-value). Proof engine — Strategy 10 activated + sequential count-interval walk. Diagnostics — `CountBoundViolation` (PRE0136) re-pointed to guard-naming wording and removed from the Gate-1 allow-list (it currently suppresses emission). Evaluator — the runtime `CountBoundViolation` fault trap becomes a backstop (unreachable for contract data under Reading A), unchanged in shape.
- **Tooling (highlighting, completions, hover, semantic tokens)**: hover/`precept_proofs` surface the new count obligations and their discharge/guard attribution; no new vocabulary. Completions/highlighting: None.
- **MCP (vocabulary, DTOs, tool output)**: `precept_compile` surfaces the activated count obligations in `proofObligations`; `precept_diagnostic CountBoundViolation` text updates to the guard-naming wording. No DTO shape change (the obligation DU already exists).

**Breaking changes.** No public-API or catalog-member-name change. `CountBoundViolation` (PRE0136) *message text* changes (currently value-shaped "count {0} is outside …" → guard-naming) — author-visible diagnostic wording, not a code/number change. No diagnostic code renumber. Pre-release solo-dev: no downstream consumer to break.

### External architectural precedent

**SPARK formal containers** is the closest architectural comparator for the *placement* question (where does capacity enforcement live). SPARK puts capacity in the *contract layer* as a precondition discharged by the caller, with no runtime fallback:

> `Pre => (SPARKlib_Defensive => Length (Container) < Container.Capacity)` — `spark-containers-formal-doubly_linked_lists.ads`; for sets, relaxed to `Length < Capacity OR Contains (Container, New_Item)` (`spark-containers-formal-hashed_sets.ads`), cited in the research survey.

What Precept takes: capacity is a compile-time obligation discharged by a *carried fact*, not a runtime check. What Precept diverges from: SPARK requires the author to write the precondition explicitly per call site; Precept *derives* the obligation from the declared `maxcount` modifier and lets a `when` guard (or a routing reject row) discharge it — lighter author burden, because Precept additionally has the §3A.4 runtime sweep as a defense-in-depth backstop SPARK lacks (so an unprovable case that slips a guard is still caught, where SPARK would be unsound). This is the architectural justification for Reading A *with* a runtime trap: prove-or-reject at compile time (SPARK's posture), backstopped at runtime (Precept's §0.7 composition) — strictly stronger than either alone.

## Inventory of what will be built

- `src/Precept/Language/ProofRequirement.cs` — `CountContainmentProofRequirement` already exists. Under Reading A the obligation carries the declared band **plus** the post-mutation count interval `[CountLower, CountUpper]` (the flow-sensitive count interval the sequential `WalkActions` pass computes and the prover checks). What was dropped from the disputed uncommitted artifact is the *emit-on-provable-violation* discharge semantics and the `*After` field naming — **not** the interval carrier; the discharge contract is two-way prove-or-reject. (Field shape resolved in Decision 1's reversibility note.)
- `src/Precept/Pipeline/ProofEngine.cs` `WalkActions` — sequential count-interval seeding + per-effect advance, reusing `ActionEffectClass`. (The uncommitted artifact's `CountContainmentObligation`/`AdvanceCount` walk is reusable *structurally*; its `TryCountContainmentProof` return line is replaced.)
- `src/Precept/Pipeline/ProofEngine.Lengths.cs` `TryCountContainmentProof` — replace the disputed emit-on-provable-violation body with the prove-or-reject body mirroring `TryLengthContainmentProof` (both `false` and `null` ⇒ emit).
- `src/Precept/Pipeline/ProofEngine.Analysis.cs` — `default [...]` count obligation collector (the uncommitted `CollectCountDefaultObligation` is correct under both readings — a default's count is exact).
- `src/Precept/Pipeline/ProofEngine.Diagnostics.cs` — `CountBoundViolation` message re-pointed to guard-naming wording; remove from `DiagnosticCoverageAllowLists.cs` Gate-1 suppression.
- `src/Precept/Language/Diagnostics.cs` — PRE0136 message string update.
- `test/Precept.Tests/ProofEngine/CountContainmentEmissionTests.cs` — the test matrix in Acceptance below.
- `samples/` — no edit needed (the one count-bounded field is an event-arg, governed at ingress; verified no in-transition count mutation exists). A new `samples/reservation-slot.precept` (the worked example) is optional documentation, not a corpus fix.

## Decisions

### Decision 1: An unproven count mutation is an *obligation* (Reading A) — emit `CountBoundViolation` and name a guard, not "proven-violations-only stay-clean" (Reading B)

**Stakes**: high — this is the count-discharge line, author-visible via PRE0136 wording and the compile/clean verdict on every count-bounded grow; reversal re-points a diagnostic and flips a soundness posture.

- **Rationale.** §0.7 places "no result outside a declared bound" in the *fault-prevention* bucket and states the contract verbatim: the compiler "discharg[es], at every fault-prone operation, an obligation that each operand *carries* a sufficient constraint — a declared modifier or rule, **an author guard**, or a statically-known safe value … If it cannot, it **rejects the definition** … It never compiles a fault-prone operation in the hope a runtime check catches it; **there is no deferral.**" A count overflow is a result outside a declared bound; the unprovable grow has not discharged its obligation; §0.7 therefore *requires* rejection-with-named-carrier, and §0.7 explicitly names "an author guard" as the carrier — so the guard `when C.count < N` is the legitimate discharge, exactly as `maxlength`-on-the-source discharges length. This makes count an *obligation* (§0.6 items 3/4/5 shape), not a *violation* (§0.6 item 2 shape).
- **Tradeoff accepted.** A one-time author guard-tax: every count-bounded grow that isn't already routed/guarded must carry a `when C.count < Hi` guard (or a reject row) to compile clean. Authors lose the "unguarded add silently compiles" ergonomic; they gain a structurally-prevented overflow and an honest `[StaticallyPreventable]` totality claim.
- **Alternatives considered.**
  - *Reading B (violation; proven-violations-only; unprovable grow stays clean, runtime sweep catches it)* — rejected because it makes the §3A.4 runtime sweep the *primary* enforcer for the unprovable add, which §0.7 forbids ("never … in the hope a runtime check catches it; there is no deferral"). It also re-creates exactly the BUG-018 soundness-hole shape (`[StaticallyPreventable]` fault that the compiler doesn't actually statically prevent) the relational-rules lock set out to close. Its one genuine merit (no false `CountBoundViolation`, honoring §0.6 item 2's "nag" concern) is answered below.
  - *SPARK-style mandatory `Contains`/capacity precondition on every add* — rejected as the floor: heavier author burden than a `when` guard, and unnecessary given Precept's runtime backstop (research Conclusion 1(c)).
  - *Leave count unenforced (status quo ante BUG-018)* — rejected: a declared `maxcount` silently not enforced is the prevention-vs-detection failure Precept exists to prevent (Obligation Generation Contract, "Why this is a contract").
- **Precedent.** Committed length sibling: `TryLengthContainmentProof` returns `false` (provable violation) *or* `null` (unprovable, e.g. unbounded source into a capped field) and **both emit** `LengthBoundViolation` — "Per §0.7 prove-or-reject, both `false` and `null` leave the obligation unresolved so the diagnostic is emitted" (`ProofEngine.Lengths.cs:21-23, 33-45`). Committed divisor sibling: §0.6 item 3 "divisors with no compile-time nonzero proof are obligation diagnostics requiring the author to supply a constraint … a guard." SPARK: capacity is a discharged static obligation with no runtime fallback (research survey). The committed Obligation Generation Contract item 2: "`field c as set of string maxcount 10` *must* produce a count-containment obligation on every mutation that grows `c`."
- **Sources consulted for this decision.**
  - `git show HEAD:docs/language/precept-language-spec.md §0.7` — "an obligation that each operand *carries* a sufficient constraint — a declared modifier or rule, an author guard, or a statically-known safe value … it **rejects the definition** … there is no deferral."
  - `git show HEAD:docs/language/precept-language-spec.md §0.6 item 3` — "divisors with no compile-time nonzero proof are **obligation diagnostics requiring the author to supply a constraint** (e.g., `nonzero`, `positive`, a rule, or a guard)."
  - `src/Precept/Pipeline/ProofEngine.Lengths.cs:18-46` — "`null` (unresolved) when the interval cannot establish containment — e.g. an unbounded upper bound against a declared maxlength. Per §0.7 prove-or-reject, both `false` and `null` leave the obligation unresolved so the diagnostic is emitted."
  - `git show HEAD:docs/compiler/proof-engine.md` Obligation Generation Contract item 2 — "must produce a count-containment obligation on every mutation that grows `c`."
  - `research/architecture/compiler/count-cardinality-bound-proof-survey.md` — SPARK `Pre => Length (Container) < Container.Capacity` (capacity as discharged static obligation).
  - **Spec-prior-settlement check (guard 17):** grepped `git show HEAD:` of `precept-language-spec.md` (§0.6/§0.7/§3A.4/§2.4) and `proof-engine.md` (Strategy 10, Sequential proof flow, Obligation Generation Contract). The spec is *genuinely silent on the count-discharge line*: committed Strategy 10 says count-containment is "always-unresolved by design … present so future whole-collection-value operations flow through a uniform path … `CountBoundViolation` when emission is wired (**currently in the Gate 1 allow-list**)" — i.e. emission is *suppressed* and the per-grow discharge mechanism is *not specified*. §0.6 item 7 sequential-flow specifies only the boolean `count > 0` non-empty model and names count-interval as future incompleteness. So both §0.6 item 2 (violation) and §0.7 (obligation) are committed and in tension *with no committed resolution for the count-mutation discharge line* — this is a genuine open decision, not a spec re-litigation. (Verified provenance: the count-specific discharge text in the working tree / `bugs.md` BUG-018 / `ProofEngine.cs` is uncommitted relative to HEAD `be614ff9`; it is the artifact-under-review, not canon.)
- **Strongest counter-evidence.** §0.6 item 2 verbatim: "Proven violations only. The language reports what is definitively broken, not what might be broken. Flagging possible violations turns the compiler into a nag that trains authors to ignore warnings." An unguarded `add` into `maxcount 3` from an empty-seed *might* not overflow (the prior count could be 0); flagging it looks like flagging a *possible* violation — the exact nag §0.6 item 2 forbids. **Response:** the resolution is the §0.6 item-5 / §0.7 framing of *obligation* vs *violation* (Decision 2): item 2 governs *violation* diagnostics (the compiler asserting "this IS broken"); divisor (item 3) and non-negative (item 4) are *obligation* diagnostics (the compiler saying "I cannot prove this safe — supply a carrier") and item 2's nag-prohibition does not apply to them, else divisor-safety would itself be a nag. Reading A emits an *obligation* diagnostic ("cannot prove the cap holds — name a guard"), not a *violation* assertion ("the cap IS exceeded"), so it sits with divisor/non-negative, not under item 2's prohibition. The seed is `[mincount?? 0 .. maxcount?? ∞]` narrowed to the band — an unguarded grow's post-`hi = Hi+1` is genuinely unproven-within-band, which is the obligation trigger, not a false positive.
- **Reversibility**: `Hard` — flipping to Reading B re-points PRE0136 wording (author-visible) and reverses the soundness posture; recoverable with a bounded edit (the discharge line is one function) but visible to anyone who learned the guard idiom. Not effectively-irreversible (pre-release, no external authors).
- **Blast radius**: catalogs — none renamed (`ModifierMeta.ProofSatisfactions`/`ActionMeta.Effect` already present); proof engine — `WalkActions` + `TryCountContainmentProof` + Strategy 10 activation; diagnostics — PRE0136 message + Gate-1 allow-list removal; docs — proof-engine.md Strategy 10 + §0.6 item 7 + spec §0.6/§0.7 cross-refs + diagnostic-system.md; samples — none required (verified); tests — `CountContainmentEmissionTests`. External consumers — none (pre-release).

### Decision 2: Count discharge is owned by §0.6 items 3/5 (obligation classification) + §0.7 (fault-prevention), NOT §0.6 item 2 (violation); §3A.4 sweep is the defense-in-depth backstop, not the primary enforcer

**Stakes**: high — this is the mechanism-ownership half of the crux; it decides which spec section the promoted doc-sync edits attach to and how the §0.6-item-2 nag concern is dispositioned.

- **Rationale.** §0.6 item 5 (committed) classifies proof outcomes three ways — *proved dangerous*, *proved safe*, *unresolved → "supply additional constraints to help the compiler"* — and "Diagnostics are classified by **proof outcome, not by syntax shape**." An unguarded count grow is *unresolved* (third category), whose prescribed author action is "supply additional constraints" (a guard) — definitionally the obligation path (items 3/4), not the violation path (item 2, which is the *proved-dangerous* category). §0.7 confirms the count band is fault-prevention ("no result outside a declared bound"). §3A.4's post-mutation sweep re-checks every constraint (including the desugared count rule) against the working copy — but §0.7 explicitly relegates the runtime trap to a *backstop* ("runtime fault traps backstop any out-of-contract value … for contract data those traps are unreachable, and a fault that fires for contract data is a compiler defect"). So the sweep cannot be the primary count enforcer for contract data; the compile-time obligation must be.
- **Tradeoff accepted.** Committing count to the obligation bucket means the §0.6-item-2 "nag" concern must be actively dispositioned (done in Decision 1's counter-evidence response) rather than deferred; the cost is that the obligation message must be carefully worded to read as "help me prove this," not "you are wrong."
- **Alternatives considered.**
  - *Own it under §0.6 item 2 (violation), making the runtime sweep primary (Reading B)* — rejected: contradicts §0.7's "no deferral / traps are a backstop" and re-opens the `[StaticallyPreventable]` hole. This is the disputed-artifact framing.
  - *Own it under §3A.4 governance alone (runtime-only, no compile-time obligation)* — rejected: §0.7 governance is scoped to "external input … event arguments, construction inputs, direct field edits"; an authored in-transition `add C x` is an *authored operation*, not external input (sub-question 1, settled from §0.7 text below), so the fault-prevention paragraph governs it, not the governance paragraph.
- **Precedent.** §0.6 item 3 divisor and item 4 non-negative are the committed in-family precedents for "unresolved ⇒ obligation diagnostic naming a carrier." §0.7 boundary paragraph for "traps are a backstop, not the enforcer." Research Conclusion 3: SPARK is a prevention-grade system making cardinality a discharged obligation; Precept's §0.7 composition lets it be *both* obligation-at-compile-time *and* backstopped-at-runtime.
- **Sources consulted for this decision.**
  - `git show HEAD:docs/language/precept-language-spec.md §0.6 item 5` — "*unresolved* (the compiler cannot determine either) … supply additional constraints to help the compiler … Diagnostics are classified by proof outcome, not by syntax shape."
  - `git show HEAD:docs/language/precept-language-spec.md §0.7` governance paragraph — "enforced at runtime on **external input** … event arguments, construction inputs, direct field edits"; boundary paragraph — "runtime fault traps **backstop** … for contract data those traps are unreachable."
  - `git show HEAD:docs/language/precept-language-spec.md §3A.4` (lines 1963–1969) — "post-mutation sweep … re-checks every constraint against the completed working copy"; the count rule (desugared, §2.4) participates.
  - **Spec-prior-settlement check (guard 17):** §0.7's governance scope ("external input") and the boundary paragraph ("backstop … compiler defect if a trap fires for contract data") *do* settle sub-question 1 (an authored in-transition `add` is an authored operation under fault-prevention, not external input under governance) — so that sub-question is *implementation against locked spec*, cited not re-litigated. What the spec does *not* settle is the discharge *line* (Decision 1), because Strategy 10 is committed as always-unresolved-with-suppressed-emission and the per-grow mechanism is unspecified. The classification (this Decision 2) is the *reasoning from* the settled §0.6-item-5/§0.7 framing to resolve the open line — it does not contradict any committed text.
- **Strongest counter-evidence.** One could read §0.7's governance paragraph as covering the *result* of any mutation (the sweep re-checks "every constraint against the resulting working copy"), implying count is governance-owned and thus a runtime concern. **Response:** §0.7 itself splits the two — the sweep governs *the result* of external input entering, but the *fault-prevention* paragraph independently covers "no result outside a declared bound" at compile time, and the boundary paragraph forbids the trap being the enforcer for contract data. The sweep is the §3A.4 atomicity mechanism (discard-on-violation), which is real and load-bearing for *external* over-adds (an event that injects a whole over-sized collection) — but for an *authored* `add`, the compile-time obligation is primary. Both are true; they compose (§0.7 Composition).

### Decision 3: Dedup-set delta — per-collection-kind sound count-only deltas (dedup-set add = upper`+1`/lower-unchanged; ordered/multiset add = exact `+1`); largely moot under Reading A, retained as the sound floor

**Stakes**: medium — a soundness correctness choice in the delta model; reversible (one `switch` arm on collection kind); only *observably* live under Reading B, but must be stated so the floor is sound regardless.

- **Rationale.** A set/lookup `add x` is membership-dependent: `|s ∪ {x}| = |s| + (0 if x∈s else 1)`. Modelling it as a deterministic lower-bound `+1` is unsound — `add x; add x` into `maxcount 1` would falsely raise `lo` to 2. The sound floor is per-kind: dedup kinds raise only `hi` (`+1`), leave `lo` unchanged (the add *might* be a no-op); ordered/multiset kinds raise both (`+1` is exact). Under Reading A this is *largely moot*: an unguarded grow emits regardless of kind (the obligation is unproven), and a guarded/routed grow (`when C.count < Hi`) is sound for all kinds because the guard caps `hi`. The cell matters only for the *provable-violation* sub-case (distinct literals past the cap), which Decision 1's prove-or-reject already emits on for ordered kinds and which dedup-correctness keeps honest for sets.
- **Tradeoff accepted.** Incompleteness: a dedup-set overflow built from unknown-membership adds is never *proved* at compile time (only the unproven-obligation emit fires, which is the same outcome) — the §0.6 item-2/§0.7 sound-but-incomplete posture, licensed by the runtime backstop.
- **Alternatives considered.**
  - *Unconditional `+1` on both bounds for all kinds* — rejected: unsound for dedup sets (the BUG-018 follow-up over-rejection `add x; add x` into `maxcount 1`); contradicts every membership-aware comparator (Dafny, SMT, SPARK).
  - *Membership-aware SMT domain (Dafny/cvc style)* — rejected: requires an opaque solver (§0.6 item 3 excludes it) and the general problem is Venn-region-combinatorial (Bansal et al. LMCS 2018).
  - *Option 2 (bounded literal-distinctness within a transition)* — **out of scope (owner-confirmed 2026-06-03), not merely deferred.** A solver-free sharpening (repeat literal in a chain provably `+0`) — but under Reading A it does not help the overflow direction at all (distinct literals against unknown prior membership can all be genuinely new → upper bound still `+1` each), so it would only rescue the degenerate "same literal added 2+ times" case. Not worth carrying inert machinery; dropped from scope rather than sequenced. Revisit only if a real sample ever needs it.
- **Precedent.** Dafny prelude membership-guarded set-card axiom (`!a[x] ⇒ +1`, `a[x] ⇒ +0`) + multiset unconditional `+1` — the exact per-kind split, minus the membership oracle (`research/architecture/compiler/count-cardinality-bound-proof-survey.md`, Findings/Conclusion 1). Serrano et al. TPLP 2014 — size-as-interval is a recognized sound-but-incomplete AI domain.
- **Sources consulted for this decision.**
  - `research/architecture/compiler/count-cardinality-bound-proof-survey.md` — Dafny `axiom … !a[x] ==> Set#Card(Set#UnionOne(a, x)) == Set#Card(a) + 1` and `a[x] ==> … == Set#Card(a)`; multiset `MultiSet#Card(MultiSet#UnionOne(a, x)) == MultiSet#Card(a) + 1`; Conclusion 1 "dedup-set add = upper`+1`/lower-unchanged".
  - `git diff HEAD src/Precept/Pipeline/ProofEngine.cs` (artifact under review) — `AdvanceCount(… delta: +1)` applied to both bounds unconditionally; the unsound cell this decision corrects.
- **Strongest counter-evidence.** The cited research file frames the whole problem under a Reading-B-shaped posture ("rejects a mutation only when the post-mutation interval *provably* leaves the band"), which could be read as endorsing Reading B. **Response:** the research's *spec-first check (Step 1b)* is explicit that "the spec **does not lock a decision on the dedup-set delta**" and that its scope is the *delta-soundness* sub-question, not the violation-vs-obligation crux; it presupposes a count-interval model without resolving the discharge line. Its delta findings (per-kind sound deltas) are correct and inherited; its incidental Reading-B framing is not load-bearing for Decision 1 and is not inherited.

## Falsifiers

(Required — Decision 1 locks author-visible diagnostic behavior; PRE0136 wording + the compile/clean verdict are external-author-visible.)

1. If, after Reading A ships, **three or more** plausible domain samples need a `when C.count < N` guard *added solely to silence PRE0136* on a grow that the domain author considers obviously fine (e.g. a grow immediately after a `clear`), the seed/guard-narrowing is too coarse and must sharpen (recognize post-`clear` `[0,0]` seeds — already in the delta model) or relax toward Reading B.
2. If a domain expert in a usability test cannot author a working count-capped reservation transition within 10 minutes using the guard idiom, the audience-fit claim for Reading A is falsified and the obligation wording or the guard requirement should be reconsidered.
3. If the guard-naming PRE0136 message is observed to train authors to *blanket-guard every grow* without understanding the cap (cargo-culting), the message wording failed its "help me prove this" intent and must be revised.
4. If a count-bounded grow that is *provably* in-band (e.g. seeded `[0,0]` after `clear`, one `add` into `maxcount 5`) ever emits PRE0136, the discharge is over-rejecting (a soundness-of-the-checker bug, not a design flaw) — forces an immediate fix.
5. If the dedup-set floor (Decision 3) is observed over-rejecting `add x; add x` into `maxcount 1` (the BUG-018 repro), the per-kind delta was not actually applied — forces the fix the research grounds.

## Acceptance criteria

Test-shaped (`test/Precept.Tests/ProofEngine/CountContainmentEmissionTests.cs`):

- **A. Unguarded grow over cap emits.** `field C as set of string maxcount 1` + two `add C` (distinct literals) in one chain ⇒ `CountBoundViolation` (PRE0136) emits, message names a `when C.count < 1`-shaped guard. *(Reading A: even one unguarded `add` into a `maxcount`-from-`∞`-seed emits, because post-`hi = Hi+1`.)*
- **B. Guarded grow discharges clean.** Same field, `from S on E when C.count < 1 -> add C E.x` ⇒ no diagnostic (guard narrows seed `hi := 0`, post-`hi = 1 ≤ 1`).
- **C. Routed reject row discharges clean.** A `when C.count >= Hi -> reject` sibling row + an unguarded `add` row ⇒ no diagnostic on the add row (sibling-reject narrowing, the committed "increment to cap" composition).
- **D. Shrink below `mincount` emits.** `field C as list of string mincount 1` fully determined to `[1,1]` + a `remove`/`pop` ⇒ post-`[0,0]` underflows ⇒ PRE0136 names a `when C.count > 1` guard; a guarded shrink discharges clean.
- **E. `clear` against `mincount` emits.** `mincount 1` + `clear C` ⇒ post-`[0,0]` underflows ⇒ PRE0136 emits.
- **F. `set C = [literal]` is type-rejected.** A list literal is legal only in `default` (PriorFieldsOnly scope); in a handler `set C = [a, b, c]` is rejected as `ListLiteralOutsideDefault` (PRE0044) plus a set↔list `TypeMismatch`. No count obligation is generated on this already-invalid construct, so `CountBoundViolation` does **not** emit (the count path is correctly absent). The live literal-count site is `default [...]` (case G).
- **G. `default [...]` against band.** `mincount 1` field with `default []` ⇒ `[0,0]` underflows ⇒ emits (this case is correct under both readings; the uncommitted `CollectCountDefaultObligation` covers it).
- **H. Dedup-set floor (Decision 3).** `set` `maxcount 1` + a *guarded* `when C.count < 1 -> add C x -> add C x` (same non-literal `x`) ⇒ NOT over-rejected only if the chain is genuinely provable; an *unguarded* `add x; add x` ⇒ emits the unproven obligation under Reading A (Option 2 is out, so the same-literal `+0` case is not specially recognized — acceptable, it's degenerate). The floor's role here is soundness of the lower bound on dedup kinds (no false *provable-violation* claim), not recovering unguarded degenerate adds.
- **I. Corpus stays clean.** Full `samples/` corpus compiles with no new PRE0136 (verified: the one count-bounded field is an event-arg, not an in-transition mutation).
- **J. Documented.** proof-engine.md Strategy 10 + §0.6 item 7, spec §0.6/§0.7 cross-refs, and diagnostic-system.md PRE0136 entry describe the prove-or-reject line and the guard carrier.

## Dependencies

- **Upstream**: the committed `ActionEffectClass` sequential-flow machinery (§0.6 item 7, shipped) — count-interval tracking reuses it. The `CountContainmentProofRequirement` DU subtype + PRE0136 + `CountBoundViolation` fault (all committed, currently dead/suppressed). The relational-rules-and-bounds lock (`docs/Working/relational-rules-and-bounds-design-2026-06-02.md`) established the obligation-emission mandate for the three breaches; this design resolves the count-discharge *line* that lock left at the disputed value.
- **Downstream**: enables Strategy 10's future whole-collection-value path to share the same prove-or-reject discharge contract; enables Option-2 literal-distinctness precision as an additive sharpening.

## Doc-update enumeration

Per the CLAUDE.md routing table:
- `docs/compiler/proof-engine.md` § Strategy 10 (Count Containment) — replace "always-unresolved by design" with the activated prove-or-reject per-mutation discharge + the per-effect delta + the dedup-set floor; § Sequential proof flow (§0.6 item 7 region) — add the count-interval (band) tracking alongside the boolean non-empty model; § Obligation Generation Contract — confirm item 2's "must produce a count-containment obligation on every mutation that grows `c`" now matches behavior.
- `docs/language/precept-language-spec.md` § 0.6 item 7 — note the count-band tracking now joins the boolean `count > 0` model (the named incompleteness for the *band* is closed; the boolean-vs-interval incompleteness for *non-empty accessor safety* remains, distinct); cross-ref § 0.7 fault-prevention for count as a carrier-discharged obligation.
- `docs/compiler/diagnostic-system.md` § proof-stage codes — PRE0136 `CountBoundViolation` re-pointed to guard-naming wording; removed from Gate-1 allow-list; document the obligation (not violation) classification.
- `docs/language/collection-types.md` — `mincount`/`maxcount` proof participation: an in-place grow/shrink against a count-bounded field discharges via a `when C.count …` guard or routing, mirroring the non-empty guard.
- `docs/Working/bugs.md` BUG-018 — reconcile the entry's emit-on-provable-violation framing to the chosen Reading-A prove-or-reject line at promotion (the entry is the artifact under review).

## Operational dimensions

- **Observability** (triggered — touches diagnostic surface): when a count obligation is unresolved, the author diagnoses it via PRE0136's guard-naming message + `precept_proofs`/hover showing the `CountContainmentProofRequirement` with its post-mutation `[lo,hi]` and the declared band, and the structured attribution naming which guard (if any) narrowed the seed. The obligation is structured data (§0.6 item 6), not parsed prose.
- Security — N/A (no source-text ingestion surface change; the proof rule operates on already-parsed typed actions).
- Evolvability — N/A (no dependency on an external standard; UCUM/NodaTime are not involved in count cardinality).

## Open questions

None — all resolved at lock (owner sign-off 2026-06-03):

- **OQ1 — RESOLVED: floor only; Option 2 (literal-distinctness) is OUT, not merely deferred.** Under Reading A the dedup precision buys nothing for real programs: distinct-literal adds against unknown prior membership can all be genuinely new, so the worst-case upper bound rises `+1` each regardless — literal-distinctness does not tighten the overflow direction. It would only rescue the degenerate "same literal added 2+ times in one chain" case, which is not worth carrying inert machinery for. The dedup-set sound floor (Decision 3, lower-bound-unchanged on dedup kinds) stays; the additive Option-2 sharpening is dropped from scope (revisit only if a real sample ever needs it). Owner-confirmed 2026-06-03.
- **OQ2 — RESOLVED: two direction-specific messages, guard-naming.** Overflow (`maxcount`): *"Cannot prove `C` stays within `maxcount N` after this add — guard with `when C.count < N`."* Underflow (`mincount`): *"Cannot prove `C` stays at or above `mincount M` — guard with `when C.count > M`."* Mirrors the divisor obligation's supply-a-carrier shape (§0.6 item 3). Owner-confirmed 2026-06-03.
- **OQ3 — RESOLVED: Reading A confirmed by owner.** The §0.6 item-2 "nag" concern is dispositioned via the violation-vs-obligation distinction (Decision 1/2): an unproven count mutation is an *obligation* (like an unproven divisor — §0.6 item 3), not a *possible violation* the compiler stays silent about. Owner sign-off 2026-06-03, explicitly framing it as "yes, unproven divisor." Decision 1 is high (not irreversible — pre-release, reversible with a bounded edit), so no 24h cooling-off is mandatory; the unbiased committed-HEAD-grounded redo served as the second pass over the disputed prior framing.
