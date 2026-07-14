---
status: Externally-Grounded
phase-target: Proof-engine MVP §4a witness slice (compiler-readiness plan v3, 2026-07-12)
comparable-systems-research-status: adequate — the witness/validation architecture is owner-ruled (architecture §1.4); external precedent carried from `research/architecture/compiler/witness-checking-soundness-architecture-survey.md` + the certifying-algorithms primary-source mirror in `research/references/witness-checking/`
sources-consulted:
  - docs/Working/compiler-readiness-plan-2026-07-12-architecture.md: §1.1 verdict DU (ruled — every verdict carries a certificate; witness only on ProvenViolating) :21-26; §1.4 witness validation gate + 12-family grid + F8 caveats + four-leg rationale :52-75; §1.5 (§3 money catalog wiring, kernel ⊥-guard) :77-81
  - docs/Working/compiler-readiness-plan-2026-07-12.md: §4a slice stub :344-350; open-decision row (Presence/KeyPresence configuration-witness default deferred) :163; execution-methodology row for §4a ("Some design (rendering + open grid cells) | Moderate | Full witness family | Before & after") :475; sequencing note (§4a value-witness rows degrade to Unresolved on arg/element leaves until §2) :557-558
  - docs/Working/proof-engine-decision-ledger-2026-07-12.md: ruling #2 four-part admissibility (leg 3 = hard cap, never hang — the k ≤ 6 basis); ruling #6 (three-way verdict, "proven-violating (rejected, with a witness configuration)")
  - docs/Working/certificate-steps-membership-2026-07-12.md: settled Slice-0 certificate vocabulary — 21 step kinds / 11 premise kinds; Decision 4 alternative (c) ("witness … replays through point-interval IntervalArithmetic + Comparison if ever rendered, needing no new kind"); Scope note (witness is a separate ProvenViolating payload, not certificate content)
  - docs/Working/compiler-readiness-plan-2026-07-12-pipeline-evaluation.md: §4 obligation-family grid ("12 sealed obligation subtypes" — an undercount, see Drift ledger) :80-91; §5 tooling propagation surface :95-102
  - docs/Working/proof-engine-mvp-and-proof-phases-2026-07-12.md: :229 — the intended §4a rejection rendering ("EffectiveRate can reach 65.0, above its max of 60 … for example, BaseRate 50 with SurchargePercent 30 produces 65")
  - docs/compiler/proof-engine.md: §13 designed-not-built layer (three-way verdict + witness summary) :2607-2613; :2539 (pre-existing informal "named-rule witness" wording)
  - docs/compiler/diagnostic-system.md: :182 three-way verdict / "carries a concrete witness" text under an "Implemented" status header :1-11 (see Drift ledger)
  - docs/philosophy.md + docs/language/precept-language-spec.md §0.1 (eleven principles), §0.4 (no fixpoints, no search)
  - src/Precept/Pipeline/ProofEngine.Intervals.cs: IntervalOfNarrowed — narrowed dict consulted ONLY at the TypedFieldRef case :67-69; element reads bypass it :92-97; TypedArgRef routes to ExtractArgInterval, never the dict :99-100 (the F8 mechanism); ApplyStaticUnitScaling affine + price inversion :423-452; TrimIntervalPrecision :454-462; TryIntervalContainmentProofNarrowed (computedInterval out-param, ⊥ backstop) :489-517; BuildNarrowedIntervals (guard DNF per-branch narrowing :549-577, cross-branch union :579-606, sibling-reject exclusions :609-627, relational half-lines + ⊥ suppression :629-654); ExtractArgInterval :312
  - src/Precept/Pipeline/ProofEngine.Lengths.cs: TryLengthContainmentProof returns three-valued bool? over an (int, int?) tuple — no NumericInterval, no ComputedInterval population :25-46; StringLengthIntervalOf :55-127 (the F8 length-plumbing gap)
  - src/Precept/Pipeline/ProofLedger.cs: ProofObligation nullable siblings incl. ComputedInterval :24-32; binary ProofDisposition :94-98; ProofStrategy :100-115
  - src/Precept/Language/ProofRequirement.cs: 13 sealed obligation subtypes :55-270 (ModifierRequirement :157); AuthoredMin/Max display-only convention :184-197; CountLower/CountUpper carried on the count requirement :227-235; KeyPresenceProofRequirement (no key payload) :243-247; ProofRequirementMeta DU-as-identity :276-380
  - src/Precept/Language/ProofRequirementKind.cs: the THIRTEEN kinds :6-54 (grid coverage check)
  - src/Precept/Language/NumericInterval.cs: Empty sentinel :29; four-corner Multiply (repeated-variable independence — why corners need validation) :85-92; Union is the hull :131-135; Contains(⊥)=true :125-129
  - src/Precept/Pipeline/ProofEngine.Diagnostics.cs: AuthoredMin/Max + computed-interval rendering :117-127; count guard-naming message :141-179; today's length message :129-139
  - src/Precept/Pipeline/ProofEngine.cs: narrowed-vs-required qualifier rendering (the grid's "witness-equivalent" for the N/A families) :950-1048 (DescribeQualifiedExpression :950-968, FormatQualifierOrNarrowed :976-984)
  - samples/loan-application.precept, samples/invoice-line-item.precept, samples/incoming-material-inspection.precept (quantity `in 'kg'` conventions)
  - live precept_compile probes (2026-07-13): guarded `Total >= 900` + `set Total = Total + 200` (max 1000) → Unresolved, PRE0078, computedInterval [1100 .. 1200] (entirely outside band — the ProvenViolating shape the engine already computes); same-unit quantity add (`NetWeight + '0.6 kg'`) → computedInterval [−∞ .. +∞] (quantity IntervalTransfer not yet wired — lands in §3, which sequences before §4a)
---

# §4a Witness — validation gate, 13-family disposition map, rendering in authored units (slice design)

**How to read this document.** A *witness* is the one concrete breaking example a rejection shows the author: "for example, Total 900 gives 1100 — above your max of 1000." It is the payload of the `ProvenViolating` verdict case (the ruled `ProofVerdict` discriminated union, architecture §1.1) and exists only when the compiler has **actually executed** that example and watched it violate — a witness the compiler hasn't run is a claim, not evidence (architecture §1.4, locked). *Corner* = an assignment giving each input its smallest-or-largest allowed value. *Point-binding* = evaluating the expression with each input pinned to one exact number instead of a range. *Demote* = when a witness cannot be built or fails its re-check, the verdict falls back to `Unresolved` (still rejected, honest about not having an example). This design settles the slice's open substance — the witness record shape, the validation-gate semantics, the per-family disposition map as catalog metadata, and the rendering — inside the rails architecture §1.4 already locks. It settles **no** new language surface: the three items that would grow surface or amend a ruled grid are parked as Open Questions.

## Goal

When §4a lands, every `ProvenViolating` verdict carries a validated witness (or the site honestly reports `Unresolved`), every one of the **thirteen** obligation families has an explicit witness disposition readable from catalog metadata, and the rejection message shows the computed numbers plus the breaking example in the author's own units — demonstrated by the worked example below rendering as shown and by no corpus verdict ever carrying an unexecuted example.

## Scope

- **In scope**: the `ProofWitness` record shape (a closed DU riding `ProvenViolating`); the witness constructor (directional-corner point-binding over field leaves); the mandatory validation gate (exact re-evaluation + fact re-check; unvalidated ⇒ `Unresolved`; k ≤ 6 leaf cap); the per-family witness-disposition map encoded as `ProofRequirementMeta` metadata; witness rendering in authored units (the numbers-plus-example message shape); the count-witness specialization over the carried `[CountLower .. CountUpper]` endpoints; tooling propagation (hover, `precept_proofs`, MCP DTOs).
- **Out of scope (already locked — cited, not re-decided)**: the `ProofVerdict` DU itself and the witness-only-on-`ProvenViolating` payload rule (architecture §1.1, ruled 2026-07-12); the validation-at-mint requirement, the demote rule, and the k ≤ 6 cap (architecture §1.4 + ledger ruling #2 leg 3); the certificate vocabulary — this slice adds **no** certificate step or premise kind (settled Slice-0 vocabulary, `certificate-steps-membership-2026-07-12.md` Decision 4 alternative (c)); the searching-inward alternative (rejected in §1.4 — "Unresolved-with-missing-condition is the honest, free verdict there").
- **Deferred / parked (Open Questions — NOT settled here)**: (a) the Presence/KeyPresence **configuration witness** (default: post-MVP, per the readiness plan's open-decision row); (b) the F8 plumbing — arg-leaf/element-read point-binding and the length-witness computed interval (default: demote to `Unresolved`, sound via the gate); (c) the **Modifier** family's grid cell — the ruled 12-family grid covers 12 of the code's 13 obligation kinds (found by this pass; owner rules the 13th row).
- **Explicit exclusion**: §4b derived safe-condition printing (deferred by the phases roll-up); any witness search beyond the two directional corners (Decision 2's rejected alternatives); changes to which verdicts *reject* — the witness only ever chooses between two already-rejecting labels (see Semantic Rules § Soundness preservation).

## Philosophy Alignment

(The eleven principles per `precept-language-spec.md` §0.1.)

| Principle | Affected? (Y/N) | How served (1 sentence + cite) | Tension (1 sentence or N/A) | Tradeoff (1 sentence or N/A) |
|---|---|---|---|---|
| 1. Prevention not detection | Y | Unchanged in force — both `ProvenViolating` and `Unresolved` reject; the witness changes *which rejecting label and message* the author sees, never whether an invalid definition compiles (Semantic Rules § Soundness preservation). | N/A | N/A |
| 2. One file, complete rules | Y | Every witness binding quotes values admissible under the file's own declarations, guards, rules, and reject-rows — the gate re-checks the example against the authored facts, nothing external. | N/A | N/A |
| 3. Determinism | Y | Witness construction is a deterministic fold: fixed leaf order (site left-to-right), fixed candidate order (Decision 2), exact decimal point arithmetic — same definition ⇒ same witness. | N/A | N/A |
| 4. Full inspectability | Y | The witness is inspectability's sharpest form — the rejection surfaces the computed range AND one executed counterexample instead of today's bare "exceeded the representable range" (live probe, PRE0078). | N/A | N/A |
| 5. Keyword-anchored readability | N | N/A — compiler output only; no grammar or authored-syntax change. | N/A | N/A |
| 6. Explicit domain meaning | Y | Witness values render in the author's declared units via the inverse of the one normalization affine (Decision 5) — never the engine's normalized internals (mirrors the `AuthoredMin/Max` display convention, `ProofRequirement.cs:184-197`). | N/A | N/A |
| 7. Compile-time-first static checking | Y | The witness is produced and validated entirely at compile time; no check is added or removed. | N/A | Two extra point evaluations + a fact re-check per violating site — bounded, paid only on the (rare) entirely-outside-band path. |
| 8. Approximation honesty | Y | This is the slice's core principle: a corner's interval-*predicted* result can be unreal (`X − X` predicts −100 at the corner yet executes to 0 — four-corner `Multiply` treats repeated variables as independent, `NumericInterval.cs:85-92`) and a corner can be jointly inadmissible under the authored facts, so no corner is ever presented un-executed and the rendered result is always the executed value — unvalidated demotes, under-claiming rather than ever over-claiming (locked §1.4 tradeoff). | Some genuinely-violating sites report `Unresolved` (numbers only, still rejecting) when both directional corners fail the gate. | Accepted in the locked architecture (§1.4 tradeoff leg) — under-claim over over-claim. |
| 9. Mandatory rationale (`because`) | N | N/A — no constraint surface change; messages may quote the violated rule's existing `because` context. | N/A | N/A |
| 10. Static semantic checking | Y | The disposition map is statically exhaustive: `WitnessDisposition` lives on every `ProofRequirementMeta` subtype, so a new obligation family cannot compile without declaring its cell (Decision 4). | N/A | N/A |
| 11. Static completeness | Y | No new expression form, no new fault path; a witness-construction bug is structurally incapable of minting a false accept (the gate is fail-closed and the witness has no verdict authority beyond choosing between two rejections). | The MVP witness under-covers arg-leaf/length sites (F8) — visible as honest `Unresolved`, never as silent acceptance. | Accepted per architecture §1.4 F8; closes when §2 lands arg narrowing / when Open Question (b) is ruled. |

**Companion commitments.** *Stateless-first-class*: witness construction reads only the obligation's site, context, and collected facts — a stateless precept's computed-field and rule obligations get witnesses identically (no state machine required). *Domain-expert-primary-author*: the message names the field, the authored bound, the computed range, and the example in authored units; no engine vocabulary ("interval", "corner", "obligation") appears in the rendered text.

## Language Design Grounding

*Scope note*: this slice changes **no authored syntax** — no token, keyword, type, operator, modifier, construct, or expression form. The witness is compiler-output surface (diagnostic text, hover, `precept_proofs`), like the certificate vocabulary. The public-output vocabulary this slice introduces (the `ProofWitness` DU subtypes and the `WitnessDisposition` cells) stays strictly inside what architecture §1.4 already rules ("value / configuration / N-A per family"); the three places where genuinely new surface *could* arise are parked, not designed:

1. A **configuration-witness** payload shape (Presence/KeyPresence) — parked, Open Question (a).
2. Arg-leaf / length-witness plumbing beyond the ruled MVP demotion — parked, Open Question (b).
3. A **Modifier-family** grid row (the 13th family the ruled grid omits) — parked, Open Question (c).

No `CertificateSteps` growth: witness replay and validation are expressible entirely in the settled Slice-0 vocabulary — point-interval `IntervalArithmetic` + `Comparison` for W4(a); per-branch `Comparison`s under `CaseSplit` for W4(b) DNF membership; `Comparison` over the R(Y) half-line intersections for W4(c); `QualifierResolution` + `QualifierAgreement` for the `CategoryMismatchWitness` re-resolution (all existing kinds — membership doc :190, :221, :230, :415). **However, two settled texts are narrower than this slice needs**, and this design must not silently rely on linkage that does not exist: (i) the settled verdict linkage grants `ProvenViolating` only from "a `BandContainment`/`Comparison` step concluding *outside/violates*" (`certificate-steps-membership-2026-07-12.md:246`) — it does not license the qualifier/dimension families' `ProvenViolating` (concluded via `QualifierAgreement`-*disagrees* / `DimensionComposition`-*outside the curated set*, per Decision 1/W5); (ii) Decision 4 alt (c)'s replay note (:421) names only point-interval `IntervalArithmetic` + `Comparison`, not the W4(b)/(c) fact re-checks or the `CategoryMismatchWitness` re-resolution. Both are **proposed owner-gated amendments** in the Doc-update enumeration below — textual extensions of settled linkage/replay notes with zero vocabulary growth.

## Worked example (audience and teachability)

Syntax validated live via `precept_compile` (2026-07-13): compiles with exactly one diagnostic, PRE0078, computed interval `[1100 .. 1200]` — the entirely-outside-band shape that becomes `ProvenViolating` under the verdict DU.

```precept
precept ExpenseAccount

field Total as decimal default 0 min 0 max 1000

state Open initial
state Closed terminal

event AddExpense(Amount as decimal min 0 max 100)
event Close

from Open on AddExpense when Total >= 900
    -> set Total = Total + 200
    -> no transition
from Open on Close -> transition Closed
```

Today's rejection (verified live): *"PRE0078: Numeric computation exceeded the representable range on field 'Total'."* — no limit, no range, no example.

Under this design the site mints `ProvenViolating` with a validated witness, rendered *(proposed message text; `docs/compiler/diagnostic-system.md` owns the final shape)*:

```
PRE0078: 'Total' always exceeds its declared max 1000 after this set —
the computed range is 1100 to 1200. Breaking example: Total 900 (the
smallest value the guard 'Total >= 900' allows) gives 900 + 200 = 1100.
```

What the engine did to earn that sentence: (1) the containment fold computed `[900..1000] + 200 = [1100..1200]`, entirely above 1000; (2) the witness constructor bound the one field leaf `Total` to its low corner 900 (low, because the violation is above-max — "even at its smallest it breaks"); (3) the gate re-evaluated `900 + 200` exactly (point arithmetic, same walk as the prover), got 1100, confirmed `1100 > 1000`; (4) the gate re-checked `Total = 900` against every collected fact — the guard `Total >= 900` holds, no rules or sibling reject-rows constrain it; (5) only then was the witness minted. Had any step failed, the verdict would be `Unresolved` with the numbers but no example — never a fabricated example.

**Authored-units example.** For a quantity field the witness renders in the author's declared unit, not the engine's normalized base unit — e.g. a `NetWeight as quantity in 'kg' max '5 kg'` overflow renders `NetWeight 4.5 kg … gives 5.1 kg`, using the inverse of the same affine factors `ApplyStaticUnitScaling` applies on the way in (`ProofEngine.Intervals.cs:423-452`). *(Sequencing note, verified live: quantity/money `+` has no `IntervalTransfer` today — the computed interval is `[−∞ .. +∞]` — so quantity value-witnesses become live only after §3's catalog wiring, which the plan sequences before §4a. The rendering path is designed now so §3's transfers inherit it with zero further design.)*

**10-minute teaching path.** (1) `docs/philosophy.md` § honesty-about-approximation paragraph (1 min); (2) the witness section this design adds to `docs/compiler/proof-engine.md` — gate, grid, example (4 min); (3) `samples/loan-application.precept` — the guards/bounds that make examples constructible (3 min); (4) hover a rejected `set` line / call `precept_proofs` (2 min).

## Semantic Rules

Legibility note: *band* = the field's declared `[min .. max]` (or length/count analogue); *free input* = a leaf of the site expression whose value ranges (a field read), as opposed to a literal; *bindable leaf* = a free input the point evaluator can pin (MVP: field leaves only — the F8 rail); *fact* = one collected constraint (guard leaf, rule, ensure, sibling reject-row negation, prior-action effect).

**W1 — Witness candidacy (when construction is attempted).** A witness is attempted iff (i) the family's catalog disposition is `Value` (Decision 4), (ii) the discharge computed a concrete measure interval for the site, and (iii) that interval is **entirely outside** the band (`max < lo ∨ min > hi` — the `BandContainment` *outside* arm of the settled certificate vocabulary). Overlapping-band or unbounded results are `Unresolved` (not provably violating) — unchanged.

**W2 — Leaf classification and the cap.** Enumerate the site's free inputs in source order (left-to-right walk). If any free input is **not** a bindable field leaf — an event-arg leaf (`TypedArgRef` routes to `ExtractArgInterval`, never the point dict, `ProofEngine.Intervals.cs:99-100`), a collection element read (`:92-97`), or a quantifier binding — construction is not attempted and the verdict is `Unresolved` *(the F8 rail; Open Question (b) holds whether to build the arg plumbing now instead)*. If the count of distinct bindable leaves k > 6, construction is not attempted: `Unresolved` (ruled cap — ledger ruling #2 leg 3: "where a procedure could blow up it hits a hard cap and falls back … never hanging").

**W3 — Corner candidates (Decision 2).** At most two assignments are tried, in a fixed order chosen for legibility: violation above max ⇒ all-low corner first, then all-high; violation below min ⇒ all-high first, then all-low. Each leaf's corner value is the corresponding endpoint of its **narrowed** interval (the same `BuildNarrowedIntervals` product the prover used — guard DNF union, sibling-reject exclusions, relational half-lines, `Intervals.cs:519-657`). The first candidate that passes W4 is the witness; if both fail ⇒ `Unresolved`. No third candidate, no interior search (locked §1.4 rejected alternative). *The second (opposite-corner) candidate is a proposed extension of §1.4's singular construction — see Decision 2; owner-confirmable at the slice boundary.*

**W4 — The validation gate (the mint rule).** `mint ProvenViolating(w)` iff ALL of:

```
(a) Exact violation.   eval_point(site, w.bindings) = r  computed by re-running
                       IntervalOfNarrowed with every bindable leaf bound to
                       Point(v) (the prover's own arithmetic walk — prover and
                       verifier cannot disagree on semantics, locked §1.4), and
                       BOTH: (i) r is a single point — zero-width after the
                       precision trim (TrimIntervalPrecision, Intervals.cs:
                       454-462); (ii) r violates the band exactly (r < lo ∨
                       r > hi; length/count: integer comparison). The point
                       requirement is load-bearing: a conditional-containing
                       site yields the UNION of both arm results even under
                       full point binding (TypedConditional unions arms
                       unconditionally, Intervals.cs:135-140), and the rendered
                       sentence ("Total 900 … gives 900 + 200 = 1100") asserts
                       ONE executed value — minting from a non-point r would
                       fabricate an arithmetic identity the walk never computed.
                       Conditional-containing sites therefore demote honestly
                       until §2's arm-splitting lands (consistent with the F8
                       posture; converts automatically when §2 ships). Note the
                       violation half of (a) alone can never fail on a
                       candidacy-passing IntervalContainment site: the transfers
                       are inclusion-monotone (endpoint Add/Subtract, four-corner
                       Multiply — NumericInterval.cs:73-102), so the executed
                       point always lands inside the (entirely-outside-band)
                       computed interval. (a) is cheap defense-in-depth plus the
                       point gate; the demotion sources are (a)(i), (b), and (c).
(b) Fully-bound facts. every collected fact whose referenced fields are ALL bound
                       evaluates TRUE under w.bindings — including whole-guard
                       satisfaction: at least one DNF branch of the governing
                       guard is fully satisfied (corner values from the hull
                       union, Intervals.cs:579-606 / NumericInterval.Union
                       :131-135, may fall outside every branch — this check
                       catches that).
(c) Partially-bound    checked JOINTLY per unbound field, still zero-hop and
    facts.             bounded. For each unbound field Y touched by any collected
                       fact whose other side is bound, compute
                         R(Y) = declared(Y)
                                ∩ every half-line admitted by every partially-
                                  bound fact touching Y (bound value vs. Y — the
                                  existing RelationalHalfLine shape)
                                ∩ every collected constant fact on Y itself
                                  (e.g. `rule Y <= 100`; constant-RULE facts join
                                  the intersection once §6 collects them —
                                  guard-constant facts on Y participate now).
                       The gate passes only if every R(Y) is nonempty. Per-fact
                       checking is NOT sufficient: `rule X <= Y` + `rule W >= Y`
                       with bindings X=900, W=100 each pass individually against
                       declared(Y)=[0..1000] while their intersection [900..1000]
                       ∩ [0..100] is EMPTY — no reachable configuration realizes
                       the witness, and shipping it would be the fabricated
                       example Decision 3's gate exists to prevent. No fact whose
                       subject is an unbound partner of a checked fact is out of
                       the witness's claim; only facts touching neither a bound
                       field nor an unbound partner remain the satisfiability
                       scan's job (PRE0155/0159). Any fact on Y the gate cannot
                       fold into R(Y) fails the gate per (d). Zero-hop is
                       preserved: R(Y) reads declared(Y) and facts directly
                       touching Y — never a third field through Y.
(d) Evaluability.      any fact the gate cannot evaluate under (b)/(c) — an
                       unsupported shape, a stale fact, anything — FAILS the gate.
                       Fail-closed, never fail-open.
```

Any failure ⇒ demote: `Unresolved(condition)` with the computed numbers and the missing-condition text, no example. There is no "unvalidated witness" state — the `ValueWitness` type is constructible only through the gate (Decision 1).

**W5 — Family specializations.**
- *CountContainment*: the "interval" is the carried post-mutation `[CountLower .. CountUpper]` (`ProofRequirement.cs:227-235`); the witness binds the pre-mutation count to the violating endpoint minus the action's catalog delta and replays the delta (`ActionEffect` semantics); gate = integer replay + count-guard fact membership.
- *Numeric (divisor / sqrt / default-bound)*: the witness is the value-at-fault — for default-bound arms the authored default itself (already concrete); for divisor/sqrt, the point the narrowed interval pins (candidacy requires the interval to prove violation, e.g. `[0..0]` for a divisor).
- *IndexBounds*: value witness only where index and count are both static (ruled grid row); otherwise `Unresolved`.
- *LengthContainment*: no computed interval reaches the corner picker today (`TryLengthContainmentProof` computes a local `(int, int?)` tuple, `Lengths.cs:25-46`, and `ComputedInterval` is never populated on the length path) — `ProvenViolating` demotes to `Unresolved` under the gate *(default; Open Question (b))*.
- *Qualifier families (QualifierCompatibility, QualifierChain, AssignmentQualifier, Dimension, DimensionalProduct)*: disposition `NotApplicable` — category mismatch, every value violates; `ProvenViolating` carries a `CategoryMismatchWitness` wrapping exactly the narrowed-vs-required pair the renderer already computes (`ProofEngine.cs:950-984`); its gate is a deterministic re-resolution of the two qualifier values (Decision 1). *These families' `ProvenViolating` conclude via `QualifierAgreement`-disagrees / `DimensionComposition`-outside — legal only under the proposed owner-gated verdict-linkage amendment (Doc-update enumeration).*
- *Presence / KeyPresence*: no witness machinery in the MVP (deferred configuration-witness cell, Open Question (a)); with the gate mandatory and no witness constructible, these families mint only `Proven`/`Unresolved`.
- *Modifier*: **no ruled cell exists** — Open Question (c); until ruled, the family mints only `Proven`/`Unresolved` (the conservative posture that pre-judges nothing).

**Soundness preservation (principles 7/10/11).** This slice adds no typing rule, no expression form, and no verdict authority: (i) *reject-invariance* — `ProvenViolating` and `Unresolved` both reject, so no witness bug can change `HasErrors` or admit an invalid definition; the witness selects between two rejecting labels only; (ii) *fail-closed gate* — every gate failure demotes toward the weaker claim; a false "breaking example" is structurally unmintable, not merely untested (principle 8's honesty made structural); (iii) *no fixpoints, no search* (spec §0.4) — ≤ 2 candidates × one bounded point walk × one bounded fact scan, hard-capped at k ≤ 6 leaves; (iv) *prover-verifier arithmetic identity* — point evaluation reuses `IntervalOfNarrowed` itself with point intervals, so the gate can never disagree with the prover about operator semantics (locked §1.4 construction).

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The witness *disposition map* is **catalog metadata** — a `WitnessDisposition` supporting DU on `ProofRequirementMeta` (`src/Precept/Language/`), because "which witness a family gets" is per-member behavioral metadata, and a `kind switch` in engine code dispatching it is exactly the violation CLAUDE.md's catalog rules name (Decision 4). The witness *record* (`ProofWitness`) is a pipeline output type beside `ProofCertificate` (`src/Precept/Pipeline/`). The *constructor + gate* are pipeline code (`ProofEngine.Witness.cs`) — thin wiring over values the discharge already computes.

**Reuse verification (thin wiring, not a parallel definition).** Verified against source:
- corner inputs: the prover's own narrowed intervals (`BuildNarrowedIntervals`, `Intervals.cs:519-657`) — the constructor reads the same dict the containment proof used; it re-derives nothing;
- point evaluation: `IntervalOfNarrowed` with `Point(v)` entries in the existing narrowed dict (consulted at the `TypedFieldRef` case, `Intervals.cs:67-69`) — no second evaluator, per the locked §1.4 construction;
- violation check: the same band comparison `TryIntervalContainmentProofNarrowed` performs (`Intervals.cs:511-514`), applied to a point;
- count endpoints: carried on the requirement (`ProofRequirement.cs:227-235`) — no new plumbing;
- authored-value display: `AuthoredMin/Max` (`ProofRequirement.cs:184-197`) and the existing computed-interval message arm (`ProofEngine.Diagnostics.cs:117-127`);
- unit rescale for display: the inverse of `ApplyStaticUnitScaling`'s affine params (`Intervals.cs:423-452`), sourced from the same `TypedConstantNormalizer.TryGetStaticAffineParams` — one normalization source, two directions;
- qualifier mismatch payload: the values `DescribeQualifiedExpression` / `FormatQualifierOrNarrowed` already resolve (`ProofEngine.cs:950-984`).

**Stated new work (priced honestly, not hidden):** (1) the fact re-check evaluator — concrete boolean evaluation of collected facts over a point assignment (W4 b/c). The fact records are `(field, op, value)` / field-pair shapes (`ProofEngine.cs:31-48`), so each fully-bound check is one comparison; the W4(c) joint check is one bounded intersection fold per unbound field over the facts touching it (reusing `RelationalHalfLine` and `NumericInterval.Intersect`); the DNF branch-membership check reuses `ExtractGuardBranches` output. This is new *code*, not new *inference* — every rule it applies is a comparison the engine's own narrowing already encodes. (2) The `ProofWitness` DU and its DTO/hover projections. (3) Message-format changes across the six bounded-write diagnostic codes.

**Cross-component propagation.**
- Pipeline: `ProvenViolating` mint sites in `ProofEngine.Intervals.cs` / `.Lengths.cs` / `.Strategies.cs` route through the gate; the witness rides the Slice-0 `ProofVerdict` DU (dependency — see Dependencies).
- Language server: `RichHoverFactory` renders the witness line under the verdict (pipeline-eval §5 anchor).
- MCP: `CompileToolDtos` gains the witness projection; `precept_proofs` documents the disposition vocabulary from the catalog (`ProofRequirements.All`-derived, never a parallel list); `docs/tooling/mcp.md` + `language-server.md` updated in the same commit (the CLAUDE.md MCP-sync rule; per-slice obligation per pipeline-eval §5).
- Catalogs: `ProofRequirementMeta` widens with `WitnessDisposition`; no new catalog.
- Runtime evaluator: none (compile-time only). Diagnostics: message-argument changes on `NumericOverflow`/`OutOfRange`/`DivisionByZero`/`SqrtOfNegative`/`LengthBoundViolation`/`CountBoundViolation` — codes unchanged, no additions/retirements.
- Samples / corpus: none required by design; fixtures asserting the six message strings migrate (pipeline-eval §6 per-slice obligation).

**Breaking changes.** None beyond Slice 0's already-budgeted ledger reshape; this slice adds payload to a case Slice 0 creates.

### External architectural precedent

Comparator: **certifying algorithms** (the witness-checking survey's foundational frame — enumerated comparator A). "A certifying algorithm is an algorithm that produces, with each output, a certificate or witness … that the particular output has not been compromised by a bug. By inspecting the witness, the user can convince himself that the output is correct, or reject the output as buggy." (`research/references/witness-checking/certifying-algorithms-mcconnell-mehlhorn-naher-schweitzer-csr2011.txt:12,193-195`, mirror of McConnell/Mehlhorn/Näher/Schweitzer, CSR 2011.) Precept takes: the checker-validates-before-the-user-sees discipline — here the compiler itself is the checker (validation at mint), because the audience is a domain expert who must never be handed a bogus example to "inspect." Precept diverges: certifying algorithms attach witnesses to *positive* outputs; Precept's witness attaches to the *negative* verdict (the counterexample), where the nearest kin are model checkers replaying counterexample traces and property-based testing re-running every reported counterexample (the locked §1.4 precedent leg). Counter-pole guarded against: presenting the raw interval corner un-executed — over-claims exactly when the author declared the most rules (locked §1.4 rejected alternative), the abstract-domain analogue of a spurious CEGAR counterexample shipped without concretization.

## Inventory of what will be built

| Artifact | Path | Content |
|---|---|---|
| Witness DU | `src/Precept/Pipeline/ProofWitness.cs` | abstract `ProofWitness` + 2 sealed subtypes: `ValueWitness` (ordered bindings: leaf → normalized value + authored-display value/unit; exact result; violated edge), `CategoryMismatchWitness` (axis; left/right resolved values incl. "(from guard)" provenance). Gate-only construction (internal factory). No `ConfigurationWitness` subtype until Open Question (a) is ruled — no dead vocabulary. |
| Disposition metadata | `src/Precept/Language/ProofRequirement.cs` (meta section) | `WitnessDisposition` supporting DU (`Value` / `Configuration` / `NotApplicable`) + a required property on every `ProofRequirementMeta` subtype; Modifier's cell carries the conservative interim (`NotApplicable`-shaped no-witness) pending Open Question (c) — flagged in-code as pending the ruling via the canonical doc reference |
| Constructor + gate | `src/Precept/Pipeline/ProofEngine.Witness.cs` (new partial) | leaf classification (W2), corner candidates (W3), point evaluation via `IntervalOfNarrowed` point-binding, fact re-check (W4 b/c/d), count-delta replay (W5) |
| Mint-site wiring | `src/Precept/Pipeline/ProofEngine.Intervals.cs`, `.Lengths.cs`, `.Strategies.cs`, `.cs` | entirely-outside-band detections route through the gate; no site constructs a witness directly |
| Rendering | `src/Precept/Pipeline/ProofEngine.Diagnostics.cs` | numbers-plus-example message arms for the six bounded-write codes; inverse-affine authored-unit display helper |
| LS/MCP projection | `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs`, `tools/Precept.Mcp/Dtos/CompileToolDtos.cs` (+ formatter) | witness line in hover; witness DTO in `precept_proofs`/`precept_compile` |
| Docs (same commits) | `docs/compiler/proof-engine.md`, `docs/compiler/diagnostic-system.md`, `docs/tooling/mcp.md`, `docs/tooling/language-server.md` | per Doc-update enumeration below |
| Tests | `test/Precept.Tests/ProofWitnessTests.cs` (+ fixture migration) | per Acceptance criteria |

**Whole-family enumeration (the 13-row disposition map this slice makes explicit).** Rows 1–12 restate the ruled grid (architecture §1.4); row 13 is the gap this pass found:

| # | Family (`ProofRequirementKind`) | Disposition | MVP behavior |
|---|---|---|---|
| 1–3 | IntervalContainment, LengthContainment, CountContainment | `Value` | value witness; Length demotes pending OQ (b); Count via carried endpoints |
| 4 | Numeric (divisor / sqrt / default-bound) | `Value` | value-at-fault |
| 5 | IndexBounds | `Value` | only where index/count static; else `Unresolved` (ruled) |
| 6–7 | Presence, KeyPresence | `Configuration` (deferred) | no witness minted in MVP — OQ (a) |
| 8–12 | QualifierCompatibility, QualifierChain, AssignmentQualifier, Dimension, DimensionalProduct | `NotApplicable` | `CategoryMismatchWitness` wrapping the narrowed-vs-required pair |
| 13 | **Modifier** (`ProofRequirementKind.cs:19`; `ModifierRequirement`, `ProofRequirement.cs:157`) | **UNRULED — OQ (c)** | mints only `Proven`/`Unresolved` until ruled |

## Decisions

*Spec-first note applying to all decisions*: grepped `proof-engine.md` and `diagnostic-system.md` for "witness" at committed HEAD — `proof-engine.md:2611` and `diagnostic-system.md:182` state the ruled *target* (three-way verdict, "carries a concrete witness") but neither settles the record shape, gate semantics, disposition encoding, or rendering; architecture §1.4 rules the gate/grid/cap and expressly leaves "rendering + open grid cells" to this pass (the plan's execution-methodology row). The Pre-Design gate is satisfied upstream: the owner ruled §1.4 and the plan delegates this slice's design ("§4a witness — Some design (rendering + open grid cells)").

---

### Decision 1: `ProofWitness` is a closed two-subtype DU — `ValueWitness` and `CategoryMismatchWitness` — constructible only through the gate

**Stakes**: high (public payload on every `ProvenViolating` verdict, surfaced through hover/MCP).

- **Rationale**: the ruled DU (`ProvenViolating(witness)`) makes the witness payload non-nullable, but the ruled grid gives families three different evidence shapes (value / configuration / N-A). A DU resolves the tension without nullables: `ValueWitness` for the numeric rows; `CategoryMismatchWitness` for the N/A rows — honoring the grid's own text that for those families "the existing narrowed-vs-required rendering *is* the witness-equivalent" (§1.4) by carrying exactly that pair as typed payload rather than leaving the case unpopulatable. Gate-only construction (internal factory invoked solely by W4) makes "unvalidated witness" unrepresentable — the same make-illegal-states-unrepresentable move the verdict DU itself is ruled on (§1.1).
- **Alternatives considered and rejected**: (a) *nullable witness on `ProvenViolating` for N/A families* — rejected: re-imports the nullable-evidence shape the owner ruled out ("no nullable 'witness pending'", §1.4; CLAUDE.md DU-over-nullables). (b) *N/A families never mint `ProvenViolating`* — rejected: a same-axis `USD` vs `EUR` operation is the definition of provably violating; demoting it to `Unresolved` would blur the verdict taxonomy ruling #6 establishes. (c) *a `ConfigurationWitness` stub subtype now* — rejected: dead vocabulary until the deferred cell is pulled in (the no-dead-members discipline the certificate design's Decision 6 establishes for exactly this catalog family). (d) *one flat record with optional fields per shape* — rejected: the nullable-field smell, CLAUDE.md DU rule.
- **Precedent**: the `ProofVerdict` DU itself (ruled §1.1); `ProofRequirementMeta` DU-as-identity (`ProofRequirement.cs:276-380`); certifying-algorithms witnesses typed per output claim (comparator excerpt above).
- **Tradeoff accepted**: two witness shapes for consumers to switch on (MCP DTO renders per-subtype). Accepted: the shapes genuinely differ (bindings-and-result vs qualifier pair); a unified shape would stringify one of them.
- **Sources consulted**: architecture §1.1 :21-26 (non-overlapping payloads), §1.4 :63-66 (grid N/A text); `ProofEngine.cs:950-984` (the narrowed-vs-required pair reused as payload); CLAUDE.md DU rule.
- **Reversibility**: Easy pre-ship (solo-dev, pre-release); Hard post-ship (MCP consumers parse subtypes).
- **Blast radius**: `ProofVerdict` payload, DTOs, hover, fixtures; no authored-syntax or sample impact.

---

### Decision 2: Corner candidates = the two directional corners (all-low, all-high) of the prover's narrowed intervals, tried in violating-direction-first order; nothing wider

**Stakes**: high (fixes what the compiler will and won't show as an example; determinism-visible).

- **Rationale**: the locked construction says "pick each free input's narrowed-interval corner in the violating direction, then evaluate" (§1.4) — a **singular** procedure: the directional corner. The first candidate implements it verbatim. **The second candidate (the opposite full corner) is a proposed extension of §1.4's ruled construction, not a reading of it** — flagged here for owner confirmation at the slice-boundary review; it sits between the locked one-corner text and the rejected inward search (bounded: one extra point walk; deterministic; rescues asymmetric fact sets), and if declined, W3 drops to the single directional corner with no other change. Three reasons for the shape: (i) *Sufficiency*: candidacy requires the entire computed interval outside the band (W1), and point evaluation of any in-box assignment lands inside the interval-arithmetic result (inclusion-monotone transfers, `NumericInterval.cs:73-102`), so any candidate whose re-evaluation is a point yields a violating result — corners fail only on fact-level correlation (guard branch membership, cross-field rules including joint unbound-partner unsatisfiability) or a non-point result (conditional arms, W4a(i)), and no amount of extra corners fixes either. (ii) *Legibility*: the directional corner is the strongest sentence — "even at its smallest, Total reaches 1100" — and a deterministic first choice keeps the same definition producing the same example forever (principle 3). (iii) *Boundedness*: 2 candidates × one point walk keeps witness cost trivially inside the per-keystroke budget (ruling #2 leg 2), with the ruled k ≤ 6 cap retained as the never-hang backstop for the fact-scan cost.
- **Alternatives considered and rejected**: (a) *full 2^k corner enumeration (≤ 64 evals under the cap)* — rejected for the MVP: extra yield exists only for mixed-monotonicity sites whose directional corners fail facts but a mixed corner passes — a shape not yet observed in the corpus; the falsifier below re-opens this cheaply if real demotions show up. (b) *searching inward when corners fail* — already rejected in the locked architecture ("a capped search — Unresolved-with-missing-condition is the honest, free verdict there", §1.4). (c) *single directional corner, no fallback* — rejected: the opposite corner is free, deterministic, and rescues below-min/above-max asymmetric fact sets.
- **Precedent**: property-based testing frameworks report the *minimal/boundary* counterexample for legibility (locked §1.4 precedent leg: "property-based testing always concretely re-runs a reported counterexample"); internally, the sibling-reject narrowing already prefers boundary arithmetic over interior sets (`Intervals.cs:609-615` comment).
- **Tradeoff accepted**: some genuinely-witnessable sites demote because only a mixed corner satisfies the facts — under-claim accepted over cost/complexity, consistent with the locked §1.4 tradeoff ("under-claim accepted over ever over-claiming").
- **Falsifier**: if corpus/fixture runs show ≥ a handful of `Unresolved` demotions where a mixed corner validates, widen W3 to bounded corner enumeration (≤ 2^k, k ≤ 6) in a follow-up — a payload-level change, no design re-open.
- **Sources consulted**: architecture §1.4 :54 (construction + cap), :72 (unrealizable corners rationale — `X − X`, `NumericInterval.cs:85-92`); ledger ruling #2 legs 2–3; `Intervals.cs:519-657` (the narrowed dict the corners read).
- **Reversibility**: Easy (candidate-set widening is additive).
- **Blast radius**: `ProofEngine.Witness.cs` only.

---

### Decision 3: The validation gate = exact point re-evaluation through the prover's own walk, plus a three-tier fact re-check, fail-closed

**Stakes**: high (this is the soundness of every "breaking example" the product ever shows).

- **Rationale**: the locked gate says "concretely re-evaluated against every collected fact and actually violates" (§1.4); this decision fixes *how* each fact class is re-checked (W4). Point evaluation reuses `IntervalOfNarrowed` with point intervals — the locked construction's own device, eliminating a second arithmetic semantics — and requires a *point* result (W4a): the walk unions conditional arms unconditionally even under full point binding (`Intervals.cs:135-140`), and rendering "gives 1100" from a non-point result would assert an arithmetic identity the walk never computed. The fact re-check has to handle three shapes honestly: fully-bound facts check concretely (one comparison each); the governing guard checks as *whole-guard DNF membership* because the cross-branch union is a hull (`NumericInterval.Union` is min-of-mins/max-of-maxes, `:131-135`) whose corners can fall outside every actual branch — a per-leaf check would miss exactly the case the gate exists for; partially-bound facts check **jointly per unbound field** — R(Y) = declared(Y) ∩ all admitted half-lines ∩ Y's own constant facts, nonempty required (W4c) — because a per-fact half-line check is fail-OPEN for realizability: two facts sharing unbound Y can each be individually satisfiable while their intersection is empty, and the bare-declared-interval read that is *sound for proving* (over-approximating Y's freedom under-claims, the prover's own posture at `Intervals.cs:629-654`) points exactly the wrong way when *certifying an example realizable*. The joint fold stays zero-hop (declared(Y) + facts directly touching Y; never a third field through Y). Anything the gate cannot evaluate fails it: fail-closed is the only posture under which a gate bug demotes rather than fabricates.
- **Alternatives considered and rejected**: (a) *trust the corner because it came from the narrowed intervals* — rejected: the narrowing is per-field (a box); cross-field facts and OR-hulls are precisely what boxes lose; this is the "unvalidated interval corner" the architecture rejects. (b) *bind unbound relational partners to their own corners and check concretely* — rejected: grows the assignment transitively (a second hop in disguise), against the locked single-hop rail (architecture §1.6); the joint R(Y) fold is not this — it binds nothing and grows no assignment. (c) *skip fact re-check for guard-free sites* — rejected as a special case that invites drift; the re-check over zero facts is free. (d) *per-fact half-line satisfiability (this design's own first draft)* — rejected after adversarial review: jointly-unsatisfiable bindings pass one-at-a-time checks (`rule X <= Y` + `rule W >= Y`, X=900/W=100), shipping a fabricated example as `ProvenViolating` — below the locked §1.4 bar and inconsistent with W4(d)'s own fail-closed posture. (e) *demote whenever an unbound field is touched by more than one fact* — the simpler fail-closed alternative; not taken because the joint intersection is equally bounded, strictly more precise, and reuses the same half-line shape; kept as the fallback if the fold proves fiddly in the build slice.
- **Precedent**: certifying-algorithms checker discipline (comparator excerpt); CEGAR concretizes abstract counterexamples before reporting (the locked §1.4 model-checker precedent leg); internally, the ⊥-suppression guards on the relational fold (`Intervals.cs:646-652`) — the same "never let an unevaluated artifact discharge" posture.
- **Tradeoff accepted**: the gate demotes on any fact shape it doesn't understand, so early corpus runs may show more `Unresolved`-with-numbers than strictly necessary. Accepted: each such demotion is visible, diagnosable (the gate can log which check failed), and a candidate for targeted widening — the reverse (fail-open) is a shipped lie.
- **Sources consulted**: architecture §1.4 :54/:72-73; `Intervals.cs:549-606` (DNF branch product/union), `:629-654` (relational facts + single-hop comment), `NumericInterval.cs:131-135` (Union = hull — the load-bearing fact for whole-guard membership); `ProofEngine.cs:31-48` (fact record shapes the evaluator consumes).
- **Reversibility**: Easy to *widen* (each tier is additive); the fail-closed default is the part that must not reverse.
- **Blast radius**: `ProofEngine.Witness.cs`; no discharge-path logic changes.

---

### Decision 4: The 13-family disposition map lives on `ProofRequirementMeta` as a `WitnessDisposition` supporting DU — never a kind-switch in engine code

**Stakes**: medium.

- **Rationale**: "which witness shape does family F get" is per-member behavioral metadata over a cataloged enum — dispatching it via `requirement.Kind switch { … }` in `ProofEngine` is the exact violation CLAUDE.md names ("never switch on `*Kind` enum identity to dispatch per-member behavior … that behavior belongs in catalog metadata"). Encoding it on the meta also makes the whole-family enumeration *structural*: a future 14th obligation kind cannot compile without declaring its cell (CS8509-style exhaustiveness through the meta constructor), closing the recurring tripped-over-subset gap this very slice's grid demonstrates (the missing Modifier row).
- **Alternatives considered and rejected**: (a) *engine-side switch* — the named CLAUDE.md violation. (b) *a new standalone catalog* — rejected: no independent consumers, no `All`-enumeration need; the supporting-DU-on-existing-meta shape is the established pattern (`ProofSubject`, `CertificatePremise` precedent per the settled Slice-0 design's Decision 2). (c) *disposition as a bool pair (HasValueWitness/HasConfigWitness)* — rejected: flags papering over a three-way shape.
- **Precedent**: `ProofRequirementMeta.DiagnosticCode` — the same per-family behavioral fact already carried on the meta (`ProofRequirement.cs:282-289`); the catalog-system meta-shape rule.
- **Tradeoff accepted**: a language-layer type (`ProofRequirement.cs`) now names a proof-output concern; accepted because the meta already names diagnostics (an output concern) and the alternative is the forbidden switch.
- **Sources consulted**: CLAUDE.md catalog rules; `ProofRequirement.cs:276-380`; `certificate-steps-membership-2026-07-12.md` Decision 2 (supporting-type placement precedent).
- **Reversibility**: Easy.
- **Blast radius**: one meta property + 13 subtype declarations; docs `catalog-system.md` § Supporting Types cross-reference at promotion.

---

### Decision 5: Rendering — authored units via the inverse of the one normalization affine; message = numbers + one breaking example; the witness renders beside the certificate with zero new certificate vocabulary

**Stakes**: medium (every rejection message the product shows for bounded-write violations).

- **Rationale**: the engine computes in normalized (UCUM base-unit / price-space-inverted) magnitudes; the author declared `max '5 kg'` and must read `4.5 kg`, not a base-unit magnitude. The display transform is the *inverse* of `ApplyStaticUnitScaling`'s affine application (`Intervals.cs:423-452`, including the price-space `1/scale` inversion and the same 24-digit banker's-rounding trim `:454-462`), sourced from the identical `TypedConstantNormalizer.TryGetStaticAffineParams` — one normalization authority, both directions, so a UCUM table change can never split display from math. The message shape is the phases-doc target sentence (numbers + example, `proof-engine-mvp-and-proof-phases-2026-07-12.md:229`), anchored on the established `AuthoredMin/Max` display convention (`ProofRequirement.cs:184-197`, "used exclusively for diagnostic display"; already rendered at `ProofEngine.Diagnostics.cs:122-126`). The witness is **not** certificate content: it renders as its own line/section beside the certificate, and its replay story needs no new step kind — though the settled alt (c) replay note and verdict linkage need the owner-gated textual amendments enumerated under Doc-update enumeration (existing kinds only; see Language Design Grounding's `CertificateSteps` paragraph).
- **Alternatives considered and rejected**: (a) *render normalized values with the unit suffix* — rejected: presents the engine's internals as the author's numbers (principle 6 violation; the `AuthoredMin/Max` machinery exists precisely because this was already rejected for bounds display). (b) *a second display-scaling table* — rejected: parallel copy, drift risk (derive-don't-duplicate). (c) *embedding the witness as certificate steps* — rejected by the settled Slice-0 scope ("the witness is a separate `ProvenViolating` payload, not certificate content").
- **Precedent**: `ProofEngine.Diagnostics.cs:117-127` (authored-bounds + computed-interval arm — the message this extends); the count message's guard-naming shape (`:141-179`) as the house style for actionable rejections.
- **Tradeoff accepted**: for offset units (temperature-style affine with offset) the inverse transform must be exact through the same rounding trim — a small numeric-fidelity test surface the display path didn't previously carry. Accepted; acceptance criterion 8 pins it.
- **Sources consulted**: `Intervals.cs:423-462`; `ProofRequirement.cs:184-197`; phases doc :229; live probe (quantity transfers absent until §3 — the rendering path is exercised by decimal sites at first, quantity/money sites when §3 lands; sequencing per the plan, §3 before §4a).
- **Reversibility**: Easy (message text); the one-normalization-source rule is the part to hold.
- **Blast radius**: `ProofEngine.Diagnostics.cs`, hover/MCP formatters, `diagnostic-system.md` message table, fixture strings.

---

### Decision 6: Count witnesses replay the carried endpoints; no new interval plumbing for count or length in this slice

**Stakes**: medium.

- **Rationale**: `CountContainmentProofRequirement` already carries the post-mutation `[CountLower .. CountUpper]` (`ProofRequirement.cs:227-235`) — the witness binds the pre-count to the violating endpoint minus the action's catalog delta and replays the delta (the `ActionEffect` semantics of the settled certificate vocabulary), so the count row of the grid ships with zero plumbing. The length row cannot: `TryLengthContainmentProof` computes a local `(int, int?)` tuple and populates no `ComputedInterval` (`Lengths.cs:25-46`), so no interval reaches the corner picker — exactly the F8 caveat, and whether to build that plumbing now or demote for the MVP is the owner's cell (Open Question (b)); this design wires the demotion default so either ruling is a bounded delta.
- **Alternatives considered and rejected**: (a) *plumb a length `NumericInterval` now as part of this slice* — not rejected on merit but **parked**: the locked architecture flags it as an open slice-level item ("the length-witness plumbing (§1.4 F8)") rather than ruling it, so settling it here would exceed the delegation. (b) *derive count witnesses by re-running `SeedCountInterval`* — rejected: re-computation of what the requirement already carries (reuse rule).
- **Precedent**: the count diagnostic already renders from the carried endpoints (`ProofEngine.Diagnostics.cs:141-179`).
- **Tradeoff accepted**: LengthContainment shows numbers-only rejections until (b) is ruled/built — a visible, honest gap, not a silent one.
- **Sources consulted**: `ProofRequirement.cs:214-235`; `Lengths.cs:25-46`; architecture §1.4 F8 second bullet :70.
- **Reversibility**: Easy.
- **Blast radius**: count wiring in `ProofEngine.Witness.cs`; none elsewhere.

## Falsifiers

1. If corpus runs show recurring demotions that a mixed (non-directional) corner would have validated, Decision 2's two-candidate set is too narrow — widen to bounded corner enumeration under the existing k ≤ 6 cap.
2. If any family's gate needs information not derivable from (site, narrowed intervals, collected facts, requirement payload) — e.g. the length row needing the type checker's string-literal internals — the "thin wiring" claim failed; the missing value becomes explicit obligation payload (mirroring the certificate design's provenance lesson), not an engine re-derivation.
3. If witness construction measurably moves the interactive budget (corpus ~44 ms; worst file ~3 ms), the two-candidate cost model was wrong — gate to lazy construction at render time (the verdict's reject-invariance makes laziness sound).
4. If authors read the `CategoryMismatchWitness` line as "an example value" rather than "every value violates" (owner hover review), the N/A rendering register is mis-pitched — reword the template, not the payload.
5. If more than one family ships with a disposition cell that never mints its declared shape across corpus + fixtures, the map was speculative — re-open the cell, per the no-dead-vocabulary discipline.

## Acceptance criteria (design-level; the `# EXPECT:` failing-test matrix derives from corrected canon after owner sign-off)

1. **Gate demotes unrealizable corners**: a site whose corner fails validation yields `Unresolved` with numbers and no example — never `ProvenViolating`. The real demotion triggers, each with its own fixture: (i) non-point re-evaluation result — a conditional-containing site (W4a(i), the arm-union shape, `Intervals.cs:135-140`); (ii) hull-corner outside every guard DNF branch (W4b); (iii) cross-field fact conflict, including the joint unbound-partner unsatisfiability shape — `rule X <= Y` + `rule W >= Y`, corner X=900/W=100 (W4c). No fixture asserts a W4(a) *violation-check* failure on an IntervalContainment site: inclusion-monotone transfers make that row unwritable (see W4a note) — a test claiming it would be faked green.
2. **Worked example mints**: the ExpenseAccount sample above compiles to `ProvenViolating` whose `ValueWitness` binds `Total = 900`, result `1100`, violated edge `max 1000`; asserted via `Compiler.Compile(...)` (never type-checker-only helpers — proof-stage rule).
3. **Reject-invariance**: over the full corpus + fixtures, disabling witness construction changes zero `HasErrors` outcomes and zero accept/reject decisions — only verdict labels/messages.
4. **Cap**: a site with 7 distinct bindable field leaves yields `Unresolved` without any construction attempt.
5. **F8 demotions (default posture; contingent on Open Question (b))**: an entirely-outside-band site whose free inputs include an event-arg leaf or element read yields `Unresolved`; a LengthContainment provable violation yields `Unresolved` with the numbers.
6. **Whole-family map**: a test enumerates `ProofRequirements.All` and asserts every meta declares a `WitnessDisposition`; each `Value` family has ≥ 1 fixture minting (or provably demoting through) the gate; `Configuration` families mint no witness (contingent on Open Question (a)); the Modifier cell matches whatever Open Question (c)'s ruling lands.
7. **Category-mismatch payload**: a same-axis currency mismatch mints `ProvenViolating` with a `CategoryMismatchWitness` carrying the two resolved values (declared or "(from guard)"-provenanced), equal to what the existing narrowed-vs-required renderer computes.
8. **Authored units round-trip**: a quantity witness renders the binding and result in the field's authored unit with the inverse-affine + precision-trim path, agreeing with `AuthoredMin/Max` display to the digit (exercisable once §3 transfers land; decimal sites exercise the no-scaling arm now).
9. **Count replay**: a maxcount violation mints a witness naming the pre-count and the action delta ("with 5 items, this add reaches 6"), validated by integer replay.
10. **Determinism**: repeated compiles of the same definition produce byte-identical witnesses (ordering, values, rendering).
11. **Tooling projection**: `precept_proofs` and hover surface the witness (bindings, result, authored units) beside the certificate; `docs/tooling/mcp.md` / `language-server.md` updated in the same commit.
12. **No new certificate vocabulary**: compiling the corpus after §4a yields zero certificate steps/premises outside the settled Slice-0 membership (21 steps / 11 premises).
13. **Executed value, not interval corner**: the correlated `X − X` shape mints a TRUE witness whose rendered result is the executed point value — `X as decimal min 0 max 100`, band min 200 on the write: computed interval `[−100..100]` (four-corner independence) is entirely below the band, corner X=100 point-evaluates to **0**, which genuinely violates; the witness renders result `0`, never the interval corner `−100`. (Repurposed from the earlier draft's criterion 1, which wrongly claimed this shape demotes via W4(a) — it cannot; see W4a note.)

## Dependencies

- **Upstream (hard)**: Slice 0 — the `ProofVerdict` DU (the case this payload rides), the certificate format, and the four fail-open fixes (a witness must never adorn a verdict a known hole produced); the structural-severity relabels.
- **Upstream (sequenced by the plan)**: §3 money/quantity `IntervalTransfer` wiring — without it quantity/money sites have no computed interval (verified live) and the authored-unit rendering path idles on decimal sites; §2 arg narrowing — the F8 arg-leaf demotions convert to live witnesses only after it (per the plan's sequencing note :557-558).
- **Downstream**: §4b derived safe-condition printing (deferred) renders beside this witness; the §5b re-checker (deferred) replays witness validation through point-interval `IntervalArithmetic` + `Comparison` per the settled vocabulary.

## Doc-update enumeration (PROPOSED corrections — none applied by this pass)

Per the CLAUDE.md routing table; these land with the build slice / promotion, gated on owner sign-off:

- `docs/compiler/proof-engine.md` — new witness section (candidacy, gate W1–W5, the 13-row disposition map, rendering contract, F8 caveats); move the witness bullets of §13's designed-not-built list to implemented as they land; terminology note distinguishing the §4a witness from the pre-existing informal "named-rule witness" phrasing at `:2539`.
- `docs/compiler/diagnostic-system.md` — the rejection message shape (numbers + breaking example) for the six bounded-write codes; **status-honesty correction** for the present-tense witness text (see Drift ledger item 1).
- `docs/tooling/mcp.md`, `docs/tooling/language-server.md` — witness projection in `precept_proofs` / hover (same-commit MCP-sync rule).
- `docs/language/catalog-system.md` — `WitnessDisposition` under § Supporting Types (with `ProofRequirementMeta` cross-reference).
- `docs/Working/compiler-readiness-plan-2026-07-12-architecture.md` §1.4 — **proposed amendment, owner-gated**: add the Modifier row to the grid per Open Question (c)'s ruling (a ruled-doc change; not applied here).
- `docs/Working/certificate-steps-membership-2026-07-12.md` — **proposed amendment, owner-gated** (settled Slice-0 doc; same treatment as the §1.4 Modifier-row amendment above): (i) extend the Verdict-linkage sentence (:246) to name `QualifierAgreement` concluding *disagrees* and `DimensionComposition` concluding *outside the curated set* as legal `ProvenViolating` sources — Decision 1/W5 mints `ProvenViolating` for the qualifier/dimension families, which the settled linkage (only `BandContainment`/`Comparison`-*outside/violates*) does not license; (ii) widen Decision 4 alt (c)'s replay note (:421) to cover the W4(b) DNF-membership re-check (per-branch `Comparison`s under `CaseSplit`), the W4(c) joint half-line intersection (`Comparison`), and the `CategoryMismatchWitness` re-resolution (`QualifierResolution` + `QualifierAgreement`). Zero vocabulary growth either way — both amendments are textual extensions naming existing kinds. Until the owner rules, this design's qualifier/dimension `ProvenViolating` rows rest on linkage text that does not yet exist — flagged here rather than silently assumed; if the owner declines (i), the qualifier/dimension families fall back to `Unresolved` on the violating arm (Decision 1 alternative (b)'s cost, re-opened for the owner).
- `docs/Working/compiler-readiness-plan-2026-07-12-pipeline-evaluation.md` §4 — proposed correction note: "12 sealed obligation subtypes" → 13 (`ProofRequirement.cs:55-270`).
- No spec (`precept-language-spec.md`) change: no authored surface; the three-way-verdict spec edits are Slice-0/Stage-0c obligations already routed.

### Drift ledger (canonical docs this slice owns vs. code)

| # | Where | Divergence | Classification | Proposed correction |
|---|---|---|---|---|
| 1 | `docs/compiler/diagnostic-system.md:182` (+ status header `:1-11`) | Describes the three-way verdict in present tense — a containment obligation "carries a concrete witness" — under "Implementation state: Implemented", while code holds binary `ProofDisposition { Proved, Unresolved }` (`ProofLedger.cs:94-98`) and no witness exists | **Status-honesty drift to correct** (intended target stated as current; already flagged by pipeline-eval §7 as a Stage-0c status overclaim) | Mark the three-way/witness paragraphs as designed-target until Slice 0/§4a land (or land them with the build); do not weaken the target text itself — the spec-is-the-target rule |
| 2 | `docs/compiler/proof-engine.md:2607-2613` | Witness/verdict layer correctly labeled "designed, not yet implemented", but carries no witness-gate/grid/rendering detail | **Intended-not-yet-built** (no correction needed now) | Promotion of this design adds the witness section; flip status lines as the slice ships |
| 3 | `docs/compiler/proof-engine.md:2539` | "preserving the named-rule witness" uses *witness* informally (strategy attribution), predating the §4a term | **Terminology drift to correct** (low stakes) | One-line rewording at promotion ("named-rule attribution") |
| 4 | architecture §1.4 grid + pipeline-eval §4 (Working docs feeding this slice) vs `ProofRequirementKind.cs:6-54` | Ruled grid enumerates 12 families; pipeline-eval counts "12 sealed obligation subtypes"; code defines **13** (`Modifier`, kind 4; `ModifierRequirement` at `ProofRequirement.cs:157`) | **Enumeration gap in ruled/working docs** — surfaced, not patched (the grid is owner-ruled) | Open Question (c); grid row added only per the owner's ruling |
| 5 | architecture §1.4 value-witness rows (money/quantity) vs live engine | Quantity/money arithmetic currently yields `[−∞ .. +∞]` computed intervals (live probe 2026-07-13) — value witnesses for those types cannot arise until §3 wires the transfers | **Intended-not-yet-built** (matches the plan's §3-before-§4a sequencing; no doc text is wrong) | None; noted so §4a's acceptance run doesn't misread idle rendering paths as defects |

## Open questions (parked for the owner — neutrally framed; nothing here is settled by this design)

1. **(a) Presence/KeyPresence configuration-witness cell — confirm deferred, or pull in-MVP.** Canonical area checked: architecture §1.4 note (*"the configuration-witness is **deferred to a post-MVP cell** (the diagnostic already names the unguarded subject) unless the owner wants it in-MVP. This is the one grid cell the reuse and soundness lenses split on — flagged, low-stakes, defaulting to deferred"*) and the readiness plan's open-decision row (:163). Options: **(i) confirm deferred** — Presence/KeyPresence mint only `Proven`/`Unresolved` in the MVP; the existing diagnostics keep naming the unguarded subject; zero new machinery. **(ii) pull in-MVP** — design a configuration-witness payload (the path/scenario admitting the bad state) in a follow-up design pass; new non-interval machinery + a third `ProofWitness` subtype; the KeyPresence side additionally interacts with the key-identity fail-open candidate routed to the Slice-0 sweep (settled Slice-0 doc, coverage-table Key presence row). This design implements (i) as the default and leaves (ii) a bounded addition.
2. **(b) F8 plumbing — demote for the MVP, or build now.** Canonical area checked: architecture §1.4 F8 caveats (:68-70) — point-binding reaches only field leaves (`Intervals.cs:67-69` vs `:92-100`); the length path surfaces no computed interval (`Lengths.cs:25-46`). Options: **(i) demote (default)** — arg-leaf/element-read and LengthContainment violating sites report `Unresolved` with numbers; sound via the gate; arg sites convert automatically when §2 lands arg narrowing. **(ii) build now** — thread arg-leaf point-binding through `ExtractArgInterval` and plumb a length `NumericInterval` to the obligation in this slice; earlier witness coverage, at the cost of doing a §2-shaped piece of work ahead of §2 and ahead of its design. This design wires (i); (ii) is a scoped delta if ruled.
3. **(c) The Modifier family's witness-grid cell — new gap, found by this pass.** Canonical area checked: architecture §1.4's grid (12 families) and pipeline-eval §4 (counts "12 sealed obligation subtypes") vs the code's 13 kinds (`ProofRequirementKind.cs:19`; `ModifierRequirement`, `ProofRequirement.cs:157`; live diagnostic `UnprovedModifierRequirement`). The ruled grid simply has no row for it. Options, neutrally: **(i) `NotApplicable`** — category mismatch, mirroring the qualifier row (a required structural modifier like `ordered` is declared or it isn't; the diagnostic already names the required modifier); rendering reuses the mismatch register. **(ii) group with Presence as a deferred configuration-style cell** — "which declaration would need the modifier" is configuration-shaped evidence. **(iii) explicit no-witness row** — three-way verdict + certificate only, permanently. Until ruled, the built behavior is the conservative common denominator of all three (mints only `Proven`/`Unresolved`, no witness), so no rework lands wrong whichever way the owner rules. *Note: this touches an owner-ruled grid; adding the row is the owner's call, not this design's.*

---

## Status note (why Externally-Grounded, not Locked)

All decisions carry stakes-scaled four-leg rationale, and every behavioral claim was verified against source at file:line or by live compile probe (2026-07-13). The doc is held at **Externally-Grounded** because (i) three grid cells are owner-open (Open Questions a/b/c — two known to the plan, one found by this pass), (ii) the witness payload vocabulary is owner-reviewable public surface, (iii) three proposed deltas to ruled/settled text await owner ruling — the certificate doc's verdict-linkage + replay-note amendments (Doc-update enumeration) and Decision 2's opposite-corner extension of §1.4's singular construction — and (iv) this was an autonomous overnight pass: the owner locks or amends at the slice boundary review. This pass modified no canonical doc, spec, catalog, or code (verifiable: `git status`/`git diff` show no such file touched). No stronger per-pass attribution is claimed — the working tree carries other artifacts from the same overnight run (sibling slice designs, a plan-methodology edit), so "only artifact written" would not be reviewer-checkable.
