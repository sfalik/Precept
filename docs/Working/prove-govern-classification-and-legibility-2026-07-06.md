---
status: Locked 2026-07-06
phase-target: Resolves the 06-16 readiness plan's D2 / GATE-H / Point A — lands as Phase 0 gate confirmation (GATE-H taxonomy, GATE-F §0.6/§0.7 amendment set) plus Phase 1 reclassification slices (Slice 1.3 band routing, Slice 1.4 definition-incoherence identity reframe); the legibility-surface metadata is a runtime-API + tooling obligation. /plan finalizes placement.
comparable-systems-research-status: partial — inline survey: SPARK/Ada published assurance levels and gradual verification (POPL 2017 / VMCAI 2018) carried with stable identifiers via `band-guarantee-boundary-analysis-2026-07-03.md` §6; shared-evaluation-core dependency grounded in the Stage-1 survey.
sources-consulted:
  - docs/philosophy.md: the load-bearing principles — :19 fire-commits-only-if-constraints-hold, :25 Principle-8 hard-line-visible-in-public-surface, :35 governance-not-validation, :43 invalid-configs-structurally-impossible, :49 the absolutist "No errors. No bugs." copy, :55 the carries-proof passage, :57 the precise clean-compile enumeration, :68 runtime structurally prevents
  - docs/language/precept-language-spec.md: §0.1 the eleven principles; §0.6 Proof Engine Design Contract (item 6 assignment-range-impossibility = compile-time error; proof philosophy #1 soundness, #2 proven-violations-only, #5 truth-based classification); §0.7 the Compile-Time and Runtime Guarantee Contract (fault-prevention = prove-or-reject-no-deferral; governance = runtime-on-external-input; Composition = carries-proof)
  - src/Precept/Language/Diagnostics.cs: OutOfRange :697 (Error), UnsatisfiableInitialState :867 (Error), DefaultViolatesRule :877 (Error), DivisionByZero :821 (Error), UnsatisfiableGuard :770 (Warning), VacuousRule :792 (Warning), ContradictoryRule :802 (Warning)
  - docs/Working/band-guarantee-boundary-analysis-2026-07-03.md: the consequence/decidability axis, the false-security-is-a-legibility-problem finding, the hard-middle-case (for-all vs exists vs unprovable), SPARK/gradual-verification prior art §6
  - docs/Working/d2-proven-violation-philosophy-analysis-2026-07-03.md: the provenance chain, the two internal inconsistencies (definition-incoherence Error family; all-inputs-vs-some-path not drawn), the four crux questions
  - docs/Working/shared-expression-evaluation-core-2026-07-06.md: the just-locked upstream dependency — one evaluation core so fold-proved value ≡ runtime-computed value; makes the compiler's "prove" verdict trustworthy
  - docs/Working/compiler-readiness-plan-2026-06-16.md: GATE-H three-category taxonomy (:278), definition-incoherence identity (:45/:416/:1582), Point A (:63/:1684/:1717), D2 band split scope, GATE-F §0.6/§0.7 amendment set (:1987)
  - docs/Working/compiler-readiness-plan-2026-06-11.md: the "Decisions captured" three-category taxonomy (fault floor / definition incoherence / flag layer)
  - research/architecture/compiler/static-vs-runtime-expression-evaluation-survey.md: grounds the shared-core dependency (Rust/Zig/Dhall one-engine; the drift pole)
  - docs/compiler/diagnostic-system.md: the severity split (:180 fault-prevention-obligations-block / structural-soundness-diagnostics-report); 164 active codes; per-code numbering ownership
  - docs/compiler/proof-engine.md: classification placement (satisfiability scan, rule-level scans :485); the count/length containment prove-or-reject strategies (:1713/:1731); §0.6 item-6 severity sourcing (:2537)
  - docs/runtime/runtime-api.md: `Precept.Constraints` / `ConstraintDescriptor` (:407) — the definition-level constraint-metadata surface (Kind/ScopeTarget/Because) the disposition field extends
  - docs/tooling/language-server.md: hover + semantic-token surfaces (:178/:251) that render per-rule metadata inline
  - git commit 2df5b37d: the band-split provenance — entered 2026-06-23 in a docs-revision commit co-authored by Claude, labelled "band split" among "four owner decisions applied", with no separate owner-decision record
---

# Prove/Govern Classification and Its Legibility Surface

## Goal

When done, a definition that provably always violates a declared bound on a reachable path is **rejected** at compile time (an Error, in the definition-incoherence category — parity with `OutOfRange`-on-default), a bound whose satisfaction depends on a runtime-supplied value is **governed** at runtime (the compiler neither rejects it nor warns as if it had proven a violation), and the compiler's per-rule enforcement disposition — *proven-at-compile-time* vs *governed-at-runtime* — is derived by the compiler and surfaced back to the author (compile report, inspectability API, editor) so "compiles clean" is legible rather than over-read. Demonstrated by: a `set X = <always-violating-literal>` action emitting an Error; a `set X = <runtime-arg>` action compiling clean with no proven-violation warning; and every rule/constraint in `Precept.Constraints` carrying a queryable disposition.

## Scope

- **In scope**:
  - The classification rule that decides, per bound-bearing constraint, one of three dispositions by **consequence** (does violating it make an operation undefined, or produce a recoverably-refusable configuration?) and **information-availability** (is the violation decidable from the definition alone, or does it depend on a runtime value?): **fault** (prove-or-reject), **definition incoherence** (reject as Error), **runtime-dependent** (govern at runtime).
  - Correcting the unratified band-split routing so a provable-always-violated-reachable set-action is an Error, not a warning.
  - The optional informational advisory on the some-reachable-paths case.
  - The compiler-derived per-rule disposition (proven-at-compile-time vs governed-at-runtime) and its three surfaces: compile report, inspectability API metadata on `Precept`, editor hover/semantic rendering.
- **Out of scope**:
  - The fault floor's membership and per-family proof mechanics (divisor≠0, sqrt≥0, presence, collection-nonempty, index/key, length/count containment) — untouched; those bounds that *feed a fault obligation* stay prove-or-reject including the merely-unprovable case (carries-proof, §0.7).
  - Building the runtime governance sweep that enforces a runtime-dependent bound at ingress (RUNTIME_CODE_DEFER in the readiness plan).
  - Any new authoring syntax — the disposition is compiler-derived, not author-declared.
  - The absolutist-copy philosophy question (surfaced to the owner, not resolved here; philosophy.md is not edited).
- **Deferred to future**:
  - Promotion criteria that could later lift the some-paths advisory to a warning on real runtime evidence (flag-layer promotion, a separate initiative).
  - MCP tool-output shaping of the disposition beyond the existing `precept_proofs`/constraint DTO reuse.

## Philosophy Alignment

Every principle from `precept-language-spec.md § 0.1` is a row. The skill's paraphrase labels and the §0.1 wording differ slightly; §0.1 is the authority.

| Principle | Affected? | How served (cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | The provable-always-violated case now **rejects** (was a shipped warning under the band split), and runtime-dependent bounds are governed structurally at ingress with no bypass — "the invalid configuration never exists" (`philosophy.md:19`; `:68` "the runtime structurally prevents any operation from producing a result that violates it") | For a runtime-dependent bound, enforcement is a runtime atomic refusal, not a compile-time reject — a reader could mistake relocation for weakening | Enforcement of runtime-dependent bounds is relocated to runtime governance; accepted because atomic refusal *is* prevention (carries-proof, `philosophy.md:55`) and Decision 2 makes the relocation legible |
| 2. One file, complete rules | Y | Every disposition derives from the `.precept` file alone — no external oracle (proof philosophy #4, `spec §0.6`); no new cross-file surface | N/A | N/A |
| 3. Determinism | Y | Same definition ⇒ same disposition and same reject/govern outcome; the upstream shared-evaluation-core makes the compiler's proved value equal the runtime's computed value (`shared-expression-evaluation-core-2026-07-06.md`), so the classification is not a coincidence | N/A | N/A |
| 4. Full inspectability | Y | The disposition is a **new inspectability surface** — `precept_proofs`/`Precept.Constraints` metadata, hover, compile report; Principle 4 is where the truth of "which guarantee you stand on" lives (`spec §0.1 P4`; `philosophy.md:60`) | N/A | N/A |
| 5. Keyword-anchored readability | N | No token, keyword, construct, or layout change; the classification is compiler-derived and the surfaces are output, not authoring (`spec §0.1 P5`) | N/A | N/A |
| 6. Governance not validation | Y | The runtime-dependent disposition **is** governance — structural, unbypassable, atomic (`philosophy.md:35`, `spec §0.7` Governance); distinguishing govern from compile-reject sharpens the identity rather than blurring it | N/A | N/A |
| 7. Compile-time totality | Y | Bounds that feed a fault obligation stay prove-or-reject; the reclassification moves **only** non-fault value bounds, so the totality of the fault surface is untouched (`spec §0.7` fault-prevention; §0.6 item 3) | A bound misclassified as governance when it actually feeds a fault (e.g. a `max` that also bounds an index) would open a hole | The consequence predicate keys on whether the bound discharges a fault obligation; the fault-floor test is the classifier's first gate, so a fault-feeding bound cannot exit to governance |
| 8. Honesty about approximation | Y | **The core of Decision 2.** Principle 8 requires the exact/approximate line "visible in the type system and public surface" (`philosophy.md:25`); the disposition surface makes the compile-time/runtime enforcement boundary that same kind of legible line | The absolutist `:49` copy ("No errors. No bugs.") can be over-read as covering runtime-dependent bounds as compile-time-proven | Surfaced as an owner-gated philosophy question (below); the legibility surface is the mitigation, not a philosophy edit |
| 9. Mandatory rationale (`because`) | N | `because` semantics untouched; the disposition rides alongside the existing `Because` metadata without altering it (`spec §0.1 P9`) | N/A | N/A |
| 10. Static semantic checking | Y | The definition-incoherence reject is static semantic checking — it proves an always-violation from the definition and rejects (`spec §0.1 P10`; §0.6 item 6) | N/A | N/A |
| 11. Static completeness | Y | "no error ⇒ no fault" holds because the reclassified bounds are **non-fault** — a violation yields a recoverable governance refusal (`ConstraintsFailed`), not an undefined computation; moving them off the compile-error path creates no fault hole, and `:57`'s clean-compile enumeration never claimed bounds (`spec §0.1 P11`; `philosophy.md:57`) | The absolutist `:49` copy again over-reads relative to the precise `:57` enumeration | Same owner-gated flag as Principle 8; the design is self-consistent without a philosophy edit (the `:57` enumeration already omits bounds) |

**Accepted tradeoffs (Affected=Y rows with non-N/A tension): Principles 1, 7, 8, 11.**
- *Principle 1* — enforcement of a runtime-dependent bound is relocated to runtime governance. Justified: an atomic refusal that discards the working copy so the invalid configuration never persists is prevention, not detection; this is the carries-proof model Precept already runs (`philosophy.md:55`), and Decision 2 makes the relocation visible so it is not silently over-read.
- *Principle 7* — a fault-feeding bound misclassified as governance would open a fault hole. Justified: the classifier's first gate is the fault-floor test (does this bound discharge a fault obligation?); only bounds that fail that gate are eligible for governance, so a fault-feeding bound is structurally prevented from exiting to the runtime.
- *Principles 8 & 11* — the absolutist `:49` copy over-reads the precise `:57` enumeration. Justified as a **surfaced owner-gated flag**, not resolved: the design is philosophy-consistent without an edit (the `:57` enumeration is a closed three-item list that never covered declared bounds), and the legibility surface directly mitigates the over-read. Per CLAUDE.md, a change to "the core guarantee" or "the constraint model" is flagged to the owner, not resolved in an implementation pass. **philosophy.md is NOT edited by this design.**

**Companion commitments.** *Stateless-first-class*: the classification is per rule/constraint and applies identically to a stateless precept (rules and `ensure`s without any state machine) — a stateless precept's runtime-dependent bound is governed at `Update`/`Create` ingress exactly as a stateful one's is. *Domain-expert-primary-author*: the reject diagnostic and the disposition surface are authored **for** the domain expert (see Audience and Teachability) — the disposition answers "which of my rules did the compiler prove, and which does the engine enforce at runtime?" in domain terms, not compiler-internal ones.

### Philosophy gap surfaced (owner-gated — not resolved here)

`philosophy.md:49` reads "**Prevention, not detection.** No errors. No bugs. Business logic cannot produce a wrong answer." and `:49` closes "not what bugs you try to catch, but what bugs cannot exist." Read absolutely, a domain expert internalizes this as "everything the compiler accepts is compile-time-proven." But `philosophy.md:57` states the *precise* guarantee as a closed enumeration — "no unproven evaluation faults, no unreachable business process states, and no structural dead ends" — which does **not** include "every declared bound holds." This design governs runtime-dependent bounds at runtime (correctly, per `:55`/`:68`), which the precise `:57` enumeration accommodates but the absolutist `:49` copy over-reads. **This is a philosophy-surface question for the owner** — whether the `:49` copy should be narrowed to match the `:57` enumeration. Per CLAUDE.md this is flagged, not resolved, and philosophy.md is not edited in this pass. The legibility surface (Decision 2) is the mechanism that keeps the gap from producing false security in the interim.

## Language Design Grounding

**Omitted** — this design introduces no token, keyword, construct, modifier, type, operator, accessor, or expression form. The classification is compiler-derived from the existing constraint surface, and the legibility surface is compiler **output** / inspectability API / tooling rendering, not authoring surface. No new way to *write* a `.precept` is added, so language-surface grounding is not applicable. (The domain-expert-facing outputs — reject diagnostic and disposition — are covered in Audience and Teachability below, which the prompt requires because the report and reject are author-facing.)

## Audience and Teachability

Precept's primary author is the domain expert (`philosophy.md § Who authors a precept`; `spec § 0.7 Authoring Audience`). Both the reject diagnostic and the disposition report are author-facing, so this section is required.

**Worked example.** A plausible credit-line domain, showing both dispositions in one file:

```precept
precept CreditLine
  field Limit as money in 'USD' default '1000.00 USD' max '5000.00 USD'

  state Active initial
  state Closed terminal

  event ApplyPromo
  event RaiseLimit(newLimit as money in 'USD')

  from Active on ApplyPromo -> set Limit = '7500.00 USD' -> no transition
  from Active on RaiseLimit -> set Limit = RaiseLimit.newLimit -> no transition
```

- `ApplyPromo` always assigns `'7500.00 USD'` to `Limit`, whose declared `max '5000.00 USD'` can never allow it — **definition incoherence → Error, rejected.** The definition ships an operation guaranteed to attempt a forbidden write; the author fixes it at author time.
- `RaiseLimit` assigns a **runtime-supplied** argument to `Limit`. Whether it violates depends on data the compiler does not have — **runtime-dependent → governed.** The compiler neither rejects (rejecting would be guessing at the value, `philosophy.md:55`) nor warns as if it had proven a violation. The runtime refuses a `newLimit` above the cap at ingress; the invalid configuration never persists.

**Error message** (for the always-violating set-action — the definition-incoherence case):

```
Action on 'Limit' always assigns '7500.00 USD', which the declared 'max 5000.00 USD'
can never allow — every time this operation runs it would produce an invalid entity.
Change the assigned value, or adjust 'max 5000.00 USD'.
```

The wording serves the domain expert, not the developer: it names the field and the declared bound in the author's own terms, states the consequence in domain language ("would produce an invalid entity", "every time this operation runs"), and offers the two author-level fixes — with no compiler-internal vocabulary (no "unsatisfiable", "interval", "fold", or "satisfiability scan"). It is the set-action sibling of the shipped `OutOfRange`-on-default message ("Default value {1} for field '{0}' violates declared '{2}'", `Diagnostics.cs:697`), reworded for an operation rather than a default. (Exact PRE code assigned at implementation; `diagnostic-system.md` owns numbering. It carries the definition-incoherence identity, Error severity.)

**10-minute teaching path** (ordered, ≤10 min for a competent domain expert):
1. `philosophy.md § What makes it different` — prevention vs detection; the compiler proves, the runtime governs (2 min).
2. `spec § 0.7 The Compile-Time and Runtime Guarantee Contract` — the fault-prevention vs governance split, in two short paragraphs (3 min).
3. The worked example above + the reject message — read the two dispositions on one file (2 min).
4. The disposition line in the compile report / hover — "which of my rules are proven, which are governed" (3 min).

## Semantic Rules

This design changes **classification and diagnostic disposition**, not expression evaluation or typing — but it introduces a new proof-outcome routing and a soundness claim, so the section is required.

**The classification predicate.** For a bound-bearing constraint `C` on field `X`, applied at a position `p` (a default, a `set`/mutation action, an `ensure`, or a rule), the disposition is decided in order:

```
classify(C, p) =
  1. FAULT              if violating C makes an operation at p undefined
                        (C discharges a fault obligation: divisor≠0, sqrt≥0,
                         index/length/count-in-bounds, presence, qualifier/dimension)
                        → prove-or-reject, including the merely-unprovable case (§0.7 carries-proof)

  2. DEFINITION-INCOHERENCE   if C is a non-fault value bound AND
                              the assigned/default value at p is decidable from the
                              definition alone (a literal, a default, or an expression
                              over definition-internal constants) AND
                              (reachable(p) ∧ C) is UNSATISFIABLE
                        → reject (Error)      [the for-all / always-violated case]

  3. RUNTIME-DEPENDENT  if C is a non-fault value bound AND
                        the value at p is supplied at runtime (an event arg, an edited
                        field, an input-derived expression)
                        → govern at runtime; no reject, no proven-violation warning

  4. ADVISORY (optional) if C is a non-fault value bound AND
                         both (reachable(p) ∧ C) and (reachable(p) ∧ ¬C) are satisfiable
                         (some reachable path violates, some satisfies)
                        → informational advisory only; the runtime still governs
```

- **"Non-fault value bound"** = a `min`/`max`/`minlength`/`maxlength`/`mincount`/`maxcount` (or equivalent rule) whose violation the runtime refuses *recoverably* (`ConstraintsFailed`), i.e. it does not leave the fault floor's ten families (`compiler-readiness-plan-2026-06-11.md` Decisions captured). The fault-floor gate (step 1) is checked first, so a bound that *also* feeds a fault (a `max` that bounds an array index) stays FAULT.
- **"Provably-always-violated"** = the same satisfiability query that already decides `UnsatisfiableGuard` (`Diagnostics.cs:770`) and `ContradictoryRule` (`:802`): `(reachable-constraints ∧ C)` is unsatisfiable at `p`. It is the exact shape of `OutOfRange`-on-default (`:697`) and `UnsatisfiableInitialState` (`:867`) — a value that always violates its own governing bound — differing only in position (a `set`-action rather than a default/initial-ensure).

**Reachability must over-approximate (soundness).** The for-all / exists line depends on computing "values reachable to `p`." That computation **must over-approximate** (be sound in the direction that only ever *widens* the reachable set). An over-approximating analysis can misclassify a true for-all as exists — it only ever downgrades reject→advisory, never fabricates a rejection. An *under*-approximating analysis could misclassify exists as for-all and **falsely reject a valid definition** — the over-rejection horn the whole design avoids. The no-false-reject guarantee rests entirely on this direction and must be verified in the engine, not assumed (`band-guarantee-boundary-analysis-2026-07-03.md` §4).

**Proof obligations.** No new fault obligation. The definition-incoherence reject reuses the existing satisfiability scan (`proof-engine.md § rule-level scans`, `:485`) — the same `(reachable ∧ C)` unsatisfiability query already run for the default/initial-ensure cases, now also evaluated at reachable set-action positions. The classification adds a routing step, not a new solver.

**Soundness-preservation claim.** The principles most at risk are `spec §0.1` P7 (totality), P10 (static semantic checking), P11 (static completeness):
- **P11 holds.** "no error ⇒ no fault" requires every fault class to be caught at compile time. The reclassified bounds are **non-fault** by the step-1 gate — a violation produces a recoverable `ConstraintsFailed`, never an undefined computation — so moving them off the compile-error path removes no fault-prevention. Fault-feeding bounds never leave step 1. And the upstream shared-evaluation-core makes the compiler's proved value equal the runtime's computed value, so a "prove" verdict the classifier relies on is not a fold-vs-runtime guess (`shared-expression-evaluation-core-2026-07-06.md`).
- **P10 holds and is extended.** The definition-incoherence reject is a *stronger* static check than the band split's warning — it rejects an always-violating operation the author can fix, rather than shipping it with a caveat.
- **P7 holds.** Totality of the fault surface is untouched; only non-fault value bounds are reclassified, and the over-approximating reachability guarantees no valid definition is falsely rejected.

## Architecture Grounding

### Precept-internal placement

**Layer placement — the classification.** It belongs in the **proof engine's diagnostic-classification layer**, not in new pipeline branches. The satisfiability scan that already decides `UnsatisfiableGuard`/`ContradictoryRule`/`OutOfRange`-on-default (`proof-engine.md:485`; `Diagnostics.cs`) is the natural home: the new work is a *routing* of an existing proof outcome (`(reachable ∧ C)` unsatisfiable at a set-action position) to the definition-incoherence Error identity, plus a suppression of the reject/warn on the runtime-dependent case. This is consistent with the catalog-driven rule that diagnostics are "classified by proof outcome, not by syntax shape" (`spec §0.6` proof philosophy #5) — the disposition is a proof-outcome fact, so it lives where proof outcomes are computed.

**Layer placement — the disposition metadata.** It belongs on the **compiled `Precept`'s constraint metadata** (`ConstraintDescriptor`, `runtime-api.md:407`), which already carries per-constraint `Kind`/`ScopeTarget`/`Because`. The disposition (proven-at-compile-time vs governed-at-runtime) is one more derived metadata facet on that descriptor — recorded **once**, as a field value, not a repeated caveat. The compile report and editor surfaces read it from there; they never recompute it. This is the same "truth lives in the inspectable model, tooling projects it" discipline the language server already follows (`language-server.md:168` "query the catalog, format the response").

**Cross-component propagation:**
- **Runtime (parser, type checker, evaluator, diagnostics):** *Proof engine / diagnostics* — the reclassification (definition-incoherence Error for the always-violated set-action; suppress reject+warn on runtime-dependent; optional advisory on some-paths) and the derived disposition. *Precept builder* — records the disposition onto each `ConstraintDescriptor` at `Precept.From()`. *Evaluator* — **None** now (runtime governance sweep is deferred, RUNTIME_CODE_DEFER); the disposition merely *names* the runtime-enforcement fact the future sweep discharges. *Parser* — **None**. *Type checker* — **None** (it resolves types and stamps obligations; it does not classify proof outcomes).
- **Tooling (syntax highlighting, completions, hover, semantic tokens):** hover renders each rule's disposition inline; a semantic-token/decoration may mark governed-at-runtime constraints distinctly. Completions/syntax highlighting — **None**.
- **MCP (vocabulary, DTOs, tool output):** `precept_proofs` / the constraint DTO gains the disposition field (a projection of the `ConstraintDescriptor` facet); no new vocabulary, no new tool. Reuses the existing formatter path (`CatalogFormatters.cs` / constraint DTO).

**Breaking changes:** The `ConstraintDescriptor` gains an additive disposition field (no rename, no removal) — a public-API addition, not a break. A new definition-incoherence diagnostic code (or a set-action variant of `OutOfRange`) is additive. The severity change is a **correction** of the never-ratified band-split routing, not a break of a shipped contract (the band split never shipped; commit `2df5b37d`). No catalog member rename flows to grammar/completions/MCP vocabulary.

### External architectural precedent

**SPARK/Ada — published assurance levels (GNATprove; AdaCore stone→bronze→silver→gold→platinum).** The sharpest precedent for "keep partial static coverage honest by publishing the guarantee surface" [via `band-guarantee-boundary-analysis-2026-07-03.md` §6, point 3, which carries the tool/level identifiers]:

> "SPARK/Ada (GNATprove) — proves Absence of Run-Time Errors where it can; where it cannot, the obligation does *not* vanish — it remains an undischarged check that must be discharged by proof *or* by test/review, and unproven checks compile to runtime assertions. AdaCore publishes this as graduated assurance *levels* (stone→bronze→silver→gold→platinum), each stating precisely how much is statically proven versus runtime-guarded. The honesty move is the *published surface*: the reader always knows which guarantee they stand on."

**What Precept takes:** the *published-surface* move — the reader must be able to tell, per obligation, which guarantee they stand on. Precept's disposition metadata (proven-at-compile-time vs governed-at-runtime) is the per-rule analogue of SPARK's per-unit assurance level. This is the same honesty commitment `philosophy.md:25` already makes for approximation, applied to the compile-time/runtime enforcement boundary. **What Precept deliberately diverges from:** SPARK compiles unproven checks to *runtime assertions the developer reads*, and its levels are *project-configured targets*. Precept's disposition is **not author-declared** — it is *derived* by the compiler from the classification predicate and surfaced back; the domain expert never sets an assurance level, they read the one the compiler computed. The divergence is warranted by Precept's audience: a domain expert cannot be asked to choose an assurance level or discharge a proof obligation, so the surface must be output, not input.

**Gradual verification** (Lehmann & Tanter, *Gradual Refinement Types*, POPL 2017; Bader, Aldrich & Tanter, *Gradual Program Verification*, VMCAI 2018) [via `band-guarantee-boundary-analysis-2026-07-03.md` §6, point 1] grounds the deeper claim that partial static *coverage* is not partial *guarantee*: a residual runtime check materialized exactly where static proof stops yields a total guarantee. Precept's runtime-dependent disposition is that residual-check frontier, made legible. **Divergence:** gradual verification materializes a per-site dynamic *assertion*; Precept's governance is a whole-configuration atomic refusal (`spec §0.7`), a stronger enforcement shape than a per-site assert.

The upstream shared-evaluation-core dependency is itself grounded in the Rust/Zig/Dhall one-engine precedent (`static-vs-runtime-expression-evaluation-survey.md` §1) — that is what makes the compiler's "prove" verdict, which the classification routes on, trustworthy.

## Inventory of what will be built

- **Classification / diagnostics.**
  - Proof-engine routing: at a reachable set-action / mutation position, run the existing `(reachable ∧ C)` satisfiability query for each non-fault value bound; on unsatisfiable **and** definition-internal-decidable value → emit the definition-incoherence Error; on runtime-dependent value → emit nothing (no reject, no warning); on some-paths → optionally emit the informational advisory.
  - A definition-incoherence diagnostic (a set-action sibling of `OutOfRange`, Error severity, definition-incoherence identity) — message per Audience and Teachability. `src/Precept/Language/Diagnostics.cs` (+ `DiagnosticCode` member; `diagnostic-system.md` numbering).
  - Suppress the band-split warning path for the runtime-dependent case (correcting the unratified D2 routing).
- **Disposition metadata.**
  - `ConstraintDescriptor` (`runtime-api.md:407`) gains a `Disposition` facet (`ProvenAtCompileTime | GovernedAtRuntime`), populated in `Precept.From()`. Recorded once per constraint.
- **Surfaces (each derives from the one below).**
  - Compile report: per rule/constraint line with its disposition.
  - Inspectability API: the `ConstraintDescriptor.Disposition` field, queryable on `Precept.Constraints`.
  - Editor: hover renders the disposition; optional semantic-token/decoration for governed-at-runtime constraints (`language-server.md`).
  - MCP: `precept_proofs` / constraint DTO projects the field.
- **Tests.** `test/Precept.Tests/` — the always-violating set-action rejects (Error); the runtime-dependent set-action compiles clean (no reject, no warning); the some-paths case emits at most an advisory; an over-approximation soundness test (no false reject on a runtime-dependent value); disposition present and correct for every rule/ensure on a representative corpus. Exercised via `Compiler.Compile` / `precept_compile` (not type-checker-only helpers, so proof-stage verdicts are real).
- **Readiness-plan amendment.** Records that D2's band-split routing of the *provable* case is corrected to Error; GATE-H taxonomy confirmed; Point A settled (govern, not reject, for runtime-dependent). A `/plan` amendment to a Working doc.

## Decisions

### Decision 1: Classify bound violations by consequence + information-availability — reject the provable-always-violated-reachable case, govern the runtime-dependent case

**Stakes**: high — changes diagnostic behavior (a severity correction), touches the guarantee surface, and settles a genuinely-open, never-owner-ratified question (the band split). Reversible pre-release (no external authors; the severity/routing choices are recoverable), so not irreversible.

- **Rationale**: The three-category taxonomy is captured but its *routing of the provable set-action case* was never settled — the band split (D2) routed it to a **warning**, which makes the taxonomy internally inconsistent: an always-violating set-action is the same shape as `OutOfRange`-on-default (`Diagnostics.cs:697`, Error) and `UnsatisfiableInitialState` (`:867`, Error) — a value that always violates its own governing bound, differing only in position. Routing identical shapes to different severities by *position* is the inconsistency the D2 philosophy analysis names (`d2-...analysis` §3b-#1/#2). Drawing the line by **consequence** (fault vs recoverable-governance) and **information-availability** (definition-decidable vs runtime-supplied) — not by *effort* or by *position* — dissolves the over-rejection ratchet (there is no "catch more" gradient across a hard mathematical edge; `band-...analysis` §2) while keeping prevention where the compiler has positive proof. The canonical spec already rejects the provable case: `spec §0.6 item 6` — "An assignment expression provably outside the target field's constraint range is a compile-time error." This decision **affirms** that locked statement and declines the unratified band-split amendment that would have downgraded it; the genuinely-new settlement is that the *runtime-dependent / merely-unprovable* case is **governed, not rejected** (removing the over-rejection D2's own rationale flagged) and **not warned as if proven** (proof philosophy #2, "proven violations only").
- **Tradeoff accepted**: A runtime-dependent bound is enforced only at runtime — during the interval before the governance sweep is built (RUNTIME_CODE_DEFER), such a bound is enforced *nowhere*. Accepted because (a) pre-release there are no live entities to protect, and (b) Decision 2 makes the deferral legible so it is not silently over-read (the band analysis' load-bearing precondition: never ship the static relaxation with the boundary illegible).
- **Alternatives considered**:
  - *Keep the band split (provable case → warning, defer all enforcement)* — rejected: it ships a definition the compiler has *proven* breaks its own bound, with a caveat — the shape of detection, not prevention (`d2-...analysis` §3b); and it contradicts locked `spec §0.6 item 6`.
  - *Prove-everything (push every value bound to compile-time prove-or-reject, including runtime-dependent)* — rejected: any sound checker is incomplete, so it over-rejects valid definitions it cannot prove; rejecting a runtime value is "guessing at the value," which `philosophy.md:55` forbids, and the over-rejection tax is specifically disqualifying for a domain-expert author (`band-...analysis` §3, the refinement-types evidence).
  - *Draw the line by position (default → Error, set-action → warning)* — rejected: position is not a principled axis; the some-paths vs all-paths and fault vs governance distinctions are, and position cross-cuts them (`d2-...analysis` §3b-#2).
- **Precedent**: `OutOfRange`-on-default and `UnsatisfiableInitialState` already reject the definition-internal always-violation as Error (`Diagnostics.cs:697/:867`); `UnsatisfiableGuard`/`VacuousRule`/`ContradictoryRule` already *warn* the dead/vacuous structural cases that cannot produce a bad entity (`:770/:792/:802`) — the existing severity table is the principled "does this produce an invalid configuration on a reachable path?" line, and this decision places the set-action case on the correct side of that same line. Externally: gradual verification's soundness result that a residual runtime check at the static frontier yields a total guarantee from partial coverage (POPL 2017 / VMCAI 2018, via `band-...analysis` §6).
- **Sources consulted for this decision**:
  - `docs/language/precept-language-spec.md § 0.6 item 6` — "**Assignment range impossibility.** An assignment expression provably outside the target field's constraint range is a compile-time error." (LOCKED; this decision affirms it and declines the unratified downgrade — guard-17: the provable case rejecting is implementation against locked spec, not a fresh choice.)
  - `docs/language/precept-language-spec.md § 0.6` proof philosophy #2 — "**Proven violations only.** The language reports what is definitively broken, not what might be broken." (grounds: the merely-unprovable case is not flagged as a violation.)
  - `docs/language/precept-language-spec.md § 0.7` — "It never compiles a fault-prone operation in the hope a runtime check catches it; there is no deferral." vs. "Governance — enforced at runtime on external input … an invalid configuration never persists — this is prevention, not detection." (the fault-vs-governance axis this decision keys on.)
  - `src/Precept/Language/Diagnostics.cs:697` — `OutOfRange … Severity.Error … "Default value {1} for field '{0}' violates declared '{2}'"`; `:867` `UnsatisfiableInitialState … Severity.Error`; `:770` `UnsatisfiableGuard … Severity.Warning … "this row can never fire"` (the parity + contrast the classification rests on).
  - `docs/Working/compiler-readiness-plan-2026-06-16.md:278` (GATE-H) — the three-category taxonomy with definition incoherence as "always Error severity, covers cases like 'every Create fails'"; `:63`/`:1684` (Point A) — "whether statically-decidable-but-unproven rules are rejected at compile time or governed at runtime" — the open question this decision settles.
  - Spec-first (guard 17): grepped `precept-language-spec.md` §0.6/§0.7 + `proof-engine.md`. Found: §0.6 item 6 **settles** the provable-reachable case (reject) — affirmed, not re-litigated; §0.6 proof philosophy #2 settles that unprovable is not flagged. **Spec is silent** on the fault-vs-governance *classification of a non-fault value bound* and on the runtime-dependent govern-vs-reject disposition (`band-...analysis` §7 Q5: "What philosophy.md is silent on is the classification of a declared bound as fault-versus-governance"); that silence is the genuine design surface here. The band-split's specific downgrade of the provable case lives only in a Working doc (commit `2df5b37d`) and was never owner-ratified — so there is no locked decision to override, and the pre-design consultation gate is satisfied by owner direction to run this design.
- **Strongest counter-evidence**: `spec §0.6` proof philosophy #2 ("proven violations only … flagging possible violations turns the compiler into a nag") could be read to argue that even the definition-incoherence *reject* over-flags — that the compiler should only ever *report*, never reject, a bound. Response: #2 governs *what to report* (only proven, not possible); it does not say a proven violation must be a warning rather than an Error — and `spec §0.6 item 6` explicitly makes the proven assignment-range case an *error*. The reject is confined to the *proven* (unsatisfiable) case; the *possible* (some-paths) case gets at most an advisory, and the *unprovable* case gets nothing — which is exactly #2's discipline. No further counter-evidence found after checking §0.6/§0.7, the two 2026-07-03 analyses, and the diagnostic severity table.
- **Reversibility**: `Easy` — pre-release, no external authors; the severity/routing of a diagnostic is a bounded change (a `DiagnosticCode` severity + a routing branch), reversible if the runtime-governance sequencing forces a different posture.
- **Blast radius**: Catalogs/diagnostics — `Diagnostics.cs` (+1 code, definition-incoherence identity), the `[StaticallyPreventable]`/routing tables the readiness plan Slice 1.3/1.4 touch. Docs — `proof-engine.md`, `diagnostic-system.md`, `precept-language-spec.md §0.6/§0.7` (GATE-F amendment set), the readiness plan (D2/GATE-H/Point A). Samples — verify no sample relies on the band-split warning-clean behavior. External consumers — none (pre-release; band split never shipped).

### Decision 2: Surface the compiler-derived per-rule disposition (proven-at-compile-time vs governed-at-runtime) on the report, the inspectability API, and the editor

**Stakes**: high — adds to the public inspectability API surface (`ConstraintDescriptor`) and is author-visible (report, hover). Additive and reversible pre-release, so not irreversible.

- **Rationale**: Decision 1 relocates enforcement of runtime-dependent bounds to runtime governance. Per `philosophy.md:25`, that boundary must be "visible in the type system and public surface" — otherwise "compiles clean" is over-read as blanket compile-time proof (the false-security failure mode: false security comes from a *fuzzy* boundary, not a *partial* one; `band-...analysis` §3). The honesty move the mature systems make is the **published surface** (SPARK assurance levels; `band-...analysis` §6). Deriving the disposition (not asking the author to declare it) fits Precept's audience — the domain expert reads which guarantee they stand on, per rule, without doing proof work. Placing it on `Precept.Constraints` metadata (`runtime-api.md:407`) puts the truth where full inspectability already lives (Principle 4), and the report/editor/MCP surfaces project it.
- **Tradeoff accepted**: One additive field on the compiled model plus three projection surfaces to keep in sync (report, hover, MCP DTO). Accepted because the alternative — an illegible boundary — is precisely the configuration in which partial static coverage misleads (`band-...analysis` §3); the sync cost is bounded by the "record once, project everywhere" placement.
- **Alternatives considered**:
  - *No surface (rely on docs to explain the boundary)* — rejected: docs are not per-rule and not queryable; the domain expert reading a specific `.precept` cannot tell which of *their* rules is governed vs proven, which is exactly the false-security gap.
  - *Author-declared disposition (a modifier the author writes)* — rejected: it is new authoring surface (out of scope, and against the audience), and it lets the author *assert* a guarantee the compiler must then check — inverting the derive-and-surface model. Diverges from SPARK deliberately (SPARK levels are project-configured; Precept's are compiler-derived).
  - *A repeated per-diagnostic caveat ("this rule is runtime-governed") on every relevant message* — rejected: it belongs as a field value recorded once on the constraint, not a caveat repeated at every mention (per the placement discipline); repetition trains authors to ignore it.
- **Precedent**: `ConstraintDescriptor` already carries derived per-constraint metadata (`Kind`, `ScopeTarget`, `Because`, referenced fields, guard presence — `runtime-api.md:407/:248`); the disposition mirrors that established facet pattern. The language server already projects catalog/model metadata to hover without recomputing it (`language-server.md:168`). Externally: SPARK's published assurance levels (`band-...analysis` §6).
- **Sources consulted for this decision**:
  - `docs/philosophy.md:25` — "Precept therefore draws a hard line between exact and approximate behavior and requires that line to be visible in the type system and public surface." (the Principle-8 obligation this surface discharges.)
  - `docs/philosophy.md:60` — "Full inspectability. … The engine exposes the complete reasoning … Nothing is hidden." (Principle 4 — where the disposition lives.)
  - `docs/runtime/runtime-api.md:407` — `public sealed record ConstraintDescriptor(ConstraintKind Kind, string? ScopeTarget, … string Because, …)` and `:248` "`Precept.Constraints` exposes every declared constraint … with full metadata: kind, scope, anchor, `because` rationale, referenced fields, guard presence." (the surface the disposition field extends.)
  - `docs/tooling/language-server.md:168` — "Completions and hover must not duplicate catalog knowledge — query the catalog, format the response." (the projection discipline the editor surface follows.)
  - `docs/Working/band-guarantee-boundary-analysis-2026-07-03.md § 3 / § 6` — "Partial coverage misleads exactly when the reader cannot tell … where the guarantee stops"; SPARK "the honesty move is the *published surface*: the reader always knows which guarantee they stand on."
  - Spec-first (guard 17): grepped `runtime-api.md` — `ConstraintDescriptor` exists with derived metadata facets but no enforcement-disposition field; the spec is silent on exposing the compile-time/runtime disposition. Genuine additive API decision, not a re-litigation.
- **Strongest counter-evidence**: `spec §0.6` proof philosophy #3 ("Opaque solvers are rejected … proof reasoning must be legible") plus #6 (proof attribution) could be read to say the existing proof-attribution surface *already* carries this, making the disposition field redundant. Response: proof attribution answers "what did the engine prove about this rule and how" for the *proven* rules; it does not name, per rule, whether enforcement is compile-time or runtime-governance for the *unproven-but-governed* rules — that is a distinct fact (the enforcement location, not the proof witness). The disposition is the missing complement, not a duplicate. Looked in `spec §0.6`, `runtime-api.md`, and `proof-engine.md`; found no existing field carrying the enforcement-location fact.
- **Reversibility**: `Easy` — an additive metadata field and three projection surfaces; pre-release, no external consumers pinned to the shape.
- **Blast radius**: Catalogs — none (metadata on the compiled model, not a catalog member). Docs — `runtime-api.md` (`ConstraintDescriptor`), `language-server.md` (hover/semantic), `diagnostic-system.md` (report), `mcp.md` (constraint DTO). Samples — none. External consumers — none (pre-release).

## Falsifiers

1. **A runtime-dependent value is falsely rejected.** If any `samples/` file (or a plausible domain definition) needs a literal-workaround or an added guard to stop the compiler rejecting a genuinely runtime-supplied assignment (`set X = <event-arg>`), the reachability analysis is under-approximating (the unsound direction) and the classification must relax — the design's no-over-rejection guarantee is falsified.
2. **An always-violating set-action compiles clean.** If a `set X = <always-violating-literal>` under an incompatible bound emits only a warning (or nothing) rather than an Error, the definition-incoherence routing has regressed to the band-split warning and parity with `OutOfRange`-on-default is broken.
3. **The disposition mislabels a fault-feeding bound.** If a bound that discharges a fault obligation (a `max` that also bounds an index) surfaces as `GovernedAtRuntime` rather than `ProvenAtCompileTime`, the classifier's fault-floor gate (step 1) is miswired and a fault hole is possible.
4. **The boundary is still over-read.** If, in review, a domain expert reads "compiles clean" as covering runtime-dependent bounds *despite* the disposition surface being present, the surface is not legible enough (wrong placement, wrong wording, not surfaced where the author looks) and the legibility design must be revised — the honesty burden is not discharged by a field no one reads.

## Acceptance criteria

1. `precept_compile` on a definition with `set Limit = '7500.00 USD'` under `field Limit … max '5000.00 USD'` emits an **Error** in the definition-incoherence category — not a warning, not clean.
2. `precept_compile` on `set Limit = RaiseLimit.newLimit` (a runtime event arg) **compiles clean** — no reject and no proven-violation warning.
3. A some-reachable-paths-violating set-action emits **at most an informational advisory**, never an Error, and the compiled model still records the constraint as `GovernedAtRuntime` (runtime still governs).
4. `Precept.Constraints[i]` exposes a `Disposition` valued `ProvenAtCompileTime | GovernedAtRuntime` for **every** rule and `ensure` on a representative precept; a fault-feeding bound reads `ProvenAtCompileTime`, a non-fault runtime-dependent bound reads `GovernedAtRuntime`.
5. The compile report lists each rule/constraint with its disposition; the language-server hover renders it.
6. An over-approximation soundness test demonstrates **no valid definition is rejected** for a runtime-dependent assignment across the sample corpus (`Compiler.Compile`, not type-checker-only helpers).
7. `dotnet test` green; `precept_compile` behavior on the existing sample corpus is unchanged except for the intended reclassifications (verified diff of diagnostics).

## Dependencies

- **Upstream**:
  - `shared-expression-evaluation-core-2026-07-06.md` (Locked) — one evaluation core so the compiler's proved value equals the runtime's computed value; this is what makes a "prove" verdict (which Decision 1 routes on) trustworthy. Without it, a fold-vs-runtime mismatch could make the classifier reject or clear on a value the runtime computes differently.
  - GATE-H (three-category taxonomy confirmation) and GATE-F (the §0.6/§0.7 amendment set) as owner gates — this design supplies their content; `/plan` sequences the confirmation.
  - Owner direction to run this design (satisfies the pre-design consultation gate; the band split it corrects was never owner-ratified — commit `2df5b37d` — so there is no locked decision being overridden).
- **Downstream**:
  - The legibility surface enables an honest "compiles clean" — the precondition the band analysis names for shipping any static relaxation.
  - The runtime governance sweep (RUNTIME_CODE_DEFER): the disposition metadata *names* the runtime-enforcement obligation the sweep will discharge; the sweep, when built, makes the `GovernedAtRuntime` disposition true.
  - Resolves Point A / GATE-H / D2 in the readiness plan; `/plan` folds the resolution into the phase table.

## Doc-update enumeration

- `docs/compiler/proof-engine.md` — the classification predicate and its placement in the satisfiability-scan / diagnostic-classification layer; the definition-incoherence routing of the always-violated reachable set-action; the over-approximating-reachability soundness requirement; the derived disposition.
- `docs/compiler/diagnostic-system.md` — the new definition-incoherence diagnostic (code + Error severity + message), the corrected band routing (runtime-dependent case emits nothing), and the disposition as a report surface; update the severity-split section (`:180`) to state the fault vs definition-incoherence vs governance mapping.
- `docs/language/precept-language-spec.md § 0.6 / § 0.7` — the prove/govern statements (the Frank-B1 / GATE-F amendment set): §0.6 item 6 retained for the provable case; §0.7 stating that a non-fault value bound whose value is runtime-supplied is governed, not rejected, and not warned-as-proven.
- `docs/runtime/runtime-api.md` — the `ConstraintDescriptor.Disposition` field (the disposition metadata; where a governed rule's runtime-enforcement note lives, recorded once).
- `docs/tooling/language-server.md` — the hover / semantic-token rendering of the disposition.
- `docs/tooling/mcp.md` — the constraint DTO / `precept_proofs` projection of the disposition field.
- `docs/Working/compiler-readiness-plan-2026-06-16.md` — D2 / GATE-H / Point A resolution (band-split provable-case routing corrected to Error; taxonomy confirmed; Point A settled as govern-for-runtime-dependent). A plan amendment `/plan` handles (the plan is a Working doc).
- `docs/philosophy.md` — **SURFACED, NOT EDITED.** The absolutist `:49` copy over-reads the precise `:57` enumeration relative to runtime-governed bounds; flagged for owner deliberation (see Philosophy gap surfaced). No edit in this or the implementation pass.

## Operational dimensions

- **Security** — N/A: the design does not touch source-text ingestion (lexer/parser/MCP input); it classifies and surfaces diagnostics for already-parsed, already-type-checked constraints.
- **Observability** — Addressed: this design's *whole point* is observability of the enforcement boundary. When a runtime-dependent bound is later refused by the governance sweep, the author already knows from the compile-time disposition that this rule is `GovernedAtRuntime` and its violation surfaces as a runtime refusal outcome (`ConstraintsFailed`) the surrounding host code handles — the disposition names that consequence at author time. The definition-incoherence Error names the failing action, field, and bound in domain terms for author-time diagnosis. No new runtime trace/log surface is introduced (the sweep is deferred).
- **Evolvability** — N/A: depends on no external standard (NodaTime/ICU/UCUM/ISO-4217/TZDB).

## Open questions

None. The philosophy over-read is a **surfaced owner-gated flag**, not an unresolved question about this design's decisions — the design is philosophy-consistent without a philosophy edit (the `:57` enumeration already omits declared bounds), and the flag is routed to the owner per CLAUDE.md rather than resolved here. No `Stakes: exploratory` decisions remain; no `TBD` markers remain.
