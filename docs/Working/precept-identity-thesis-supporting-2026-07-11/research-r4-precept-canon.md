# Precept's Canonical Substrate — Faithful Synthesis

What Precept's own canon commits it to be and to guarantee. Every load-bearing claim carries a source I read first-hand. Neutral throughout: where the canon leaves a question open or contradicts itself, I mark it rather than resolve it.

---

## 1. Stated identity and "governed integrity"

**The one-line identity.** "Precept is a domain integrity engine for .NET — a single declarative contract that governs how a business entity's data evolves under business rules across its lifecycle, making invalid configurations structurally impossible." (`philosophy.md:7`)

**The unifying principle is named "governed integrity."** It covers four entity categories with one contract language: lifecycle-driven entities, validation-heavy entities, reference/config entities, and data-only entities with no lifecycle at all. "The unifying principle is governed integrity — the entity's data satisfies its declared rules at every moment, whether those rules involve lifecycle transitions, field constraints, or both." (`philosophy.md:9`)

**Data and rules are primary; states are the mechanism.** "Data and rules are the primary concern… The engine's job is to ensure that no invalid combination of field values can persist." (`philosophy.md:33`) "States are the structural mechanism that makes data protection lifecycle-aware — when lifecycle is present… state is instrumental, not primary. It is the coordinate system; the entity's data is the substance the system governs." (`philosophy.md:37`) **States are optional and stateless precepts are first-class**, not a secondary capability (`philosophy.md:39`).

**The guarantee is about configurations, not isolated values.** "The full guarantee the engine provides is about configurations — the pair of (current lifecycle position, current field values), or for stateless entities, simply the current field values. Invalid configurations are structurally impossible." (`philosophy.md:43`; restated verbatim at spec `§0.3:152` and `§0.2:134`)

**The term's lineage (grounded, not yet promoted to canon).** The `domain-integrity-formal-concept.md` research (Frank, 2026-04-19, status "Active — horizon groundwork… promotion pending", `:2-7`) establishes that "domain" is used in the **DDD sense (business domain), not the relational model's typed-value domain** (`:29`, `:78`), and that Precept is Layer 3 — "governed integrity" — which *subsumes* relational domain integrity (Layer 1) and *formalizes* DDD aggregate invariants (Layer 2), then adds lifecycle-awareness, compile-time proof, structural prevention, and inspectability (`:223-235`). It recommends "domain integrity engine" as the category label and "governed integrity" as the precise guarantee term (`:257-267`). Note this is research feeding a *pending* philosophy insertion, not locked canon (`:271-281`).

**Scope boundary — single entity.** Precept governs integrity *within* a single entity type; referential integrity *across* entities and cross-aggregate invariants are explicitly out of scope, matching Greg Young's correctness-boundary argument (`domain-integrity-formal-concept.md:237`, `:100-102`).

---

## 2. The §0.1 principles, and which bear on the prove-vs-govern boundary

The eleven principles (`spec §0.1:90-112`), each "non-negotiable — any redesign that violates one has departed from Precept's identity" (`:90`):

1. **Prevention, not detection** (`:92`) — invalid configurations structurally prevented before any change commits.
2. **One file, complete rules** (`:94`) — all proof facts, types, constraint scope, routing from a single file; no imports.
3. **Deterministic semantics** (`:96`) — no non-deterministic solvers, no timing/culture dependence.
4. **Full inspectability** (`:98`) — preview every action + outcome + reasoning; "extends to proof reasoning — proven ranges, source attribution, and what the engine could not prove."
5. **Keyword-anchored readability** (`:100`).
6. **Explicit domain meaning over primitive convenience** (`:102`).
7. **Compile-time-first static checking** (`:104`) — "proves what it can, rejects what it can prove invalid, and does not guess."
8. **Approximation honesty** (`:106`).
9. **Mandatory rationale** (`:108`) — `because` syntactically required; modifiers carry a *generated* rationale.
10. **Totality** (`:110`) — every expression evaluates to a result, never NaN/Infinity/null; every fault-prone op is proven safe or emits a diagnostic; "Runtime fault traps exist only as defensive redundancy for paths the compiler has already proven unreachable."
11. **Static completeness** (`:112`) — "If a precept compiles without diagnostics, it does not fault at runtime… Runtime fault checks exist only as defensive redundancy, never as the primary enforcement mechanism. This is the bridge between the compiler and the evaluator." Explicitly points to §0.7 for composition.

**Which bear directly on the prove-vs-govern boundary:**

- **Principles 7, 10, 11 are the compile-time (prove) leg.** §0.7 attributes fault prevention to "Principles 7, 10, 11" (`spec:266`). These are what commit the compiler to prove-or-reject with no deferral.
- **Principles 1 and 6 are the runtime (govern) leg.** §0.7 attributes governance to "Principles 1, 6" (`spec:268`) — prevention via atomic working-copy discard, and expression purity that makes ingress enforcement well-defined.
- **Principle 4 (inspectability) is load-bearing on *where* the boundary sits.** The compile-time-guarantee-boundary research derives that inspection-honesty (§3A.6) forces the proof engine to reach "exactly far enough that the only thing left 'unknown' to live inspection is genuinely-external input not yet supplied" (`compile-time-guarantee-boundary:72`). Principle 4's extension to proof reasoning is why opaque solvers are excluded (see §0.6 below).
- **Principle 8 (approximation honesty)** caps how far value-level proof may go and forbids overclaiming "the compiler proves your data valid" (`compile-time-guarantee-boundary:90`, the "Principle 8 overclaim" tradeoff).
- **Principle 3 (determinism)** and the §0.4 execution-model properties (no loops, no branches, closed vocabulary, finite state, purity, no separate compilation — `spec:158-172`) are what make tractable compile-time proof *possible at all*; "The absence of widening is a feature" (`:174`).

---

## 3. The fault-floor concept and the FAULT-vs-OUTCOME division

**The division principle (a single rule).** A check is compile-time "iff it is decidable from the definition alone and no runtime input could ever repair its failure; it is runtime iff it depends on a concrete external value not present until ingress" (`compile-time-guarantee-boundary:39`). This maps onto the evaluator's own two failure categories:

- **FAULT** — "an aborted plan with no graceful, recoverable outcome; it fires mid-execution… *before any constraint verdict exists*, leaving only `Faulted(fault)` as a defense-in-depth backstop" — must be made **impossible at compile time** (`compile-time-guarantee-boundary:41`; `fault-floor-definition:122`).
- **OUTCOME** — "a recoverable typed result (`Unmatched`/`ConstraintsFailed`/`Rejected`) produced by working-copy discard — is **governed at runtime**" (`compile-time-guarantee-boundary:42`).

**The fault floor defined.** "THE FAULT FLOOR — prove-or-reject, total, stable: if the compiler cannot prove a fault absent, the definition is rejected. A fault is a mid-evaluation abort with no recoverable typed outcome (only the Faulted trap); anything the working-copy discard or ingress refusal handles… is governance." (`fault-floor-definition:122`)

**The 11 obligation families** (`fault-floor-definition:124`): (1) divisor ≠ 0; (2) sqrt/threshold-0 operand ≥ 0; (3) presence of optional-field reads; (4) collection non-empty on value-yielding accessors; (5) collection non-empty on element-consuming mutations; (6) index in-bounds; (7) key presence on by-key read/remove; (8) operation-site qualifier/dimensional compatibility; (9) decimal-representability overflow ("THE one missing obligation family"); (10) temporal representability; (11) computed-arg function computability domains. The static edge (TypeMismatch, UndeclaredField, InvalidMemberAccess, FunctionArityMismatch) is owed by binder/type-checker totality, not proof obligations.

**Position-totality is itself a floor property** (`fault-floor-definition:126`): every obligation must be created at *every* expression position; the research found 10 live fail-open positions (guard slots, because-interpolation slots, default interiors, member-access arguments) — "a compiler-proven-zero divisor in a `when` guard compiles clean today and faults on first Fire/InspectFire," falsifying §3A.6 inspection-honesty.

**The adjudication verdict (double-derived, red-teamed).** Of the 15 registry FaultCodes: **5 are true faults** (DivisionByZero, SqrtOfNegative, presence/UnexpectedNull, two collection-empty), **4 are type-checker-owned** (defense-in-depth traps only), **3 are rule-misclassified-as-fault** (OutOfRange, LengthBoundViolation, CountBoundViolation), and **2 are mixed with sub-case splits** (QualifierMismatch, NumericOverflow) (`fault-floor-definition:26-136`). The central inversion: the engine currently prove-or-rejects the *declared band* (which is recoverable governance) while the genuine *representability overflow* fault has no obligation anywhere (`:72-74`).

**A three-category taxonomy** both derivations converged on (`fault-floor-definition:132`, `:227`): **fault floor** (prove-or-reject) / **definition incoherence** (PRE0164/PRE0115/PRE0079-on-default — always-Error, "proven self-contradictions where no valid Create exists", neither fault nor flag) / **flag layer** (post-runtime, warning-posture universal checks).

**Load-bearing caveat on this whole section:** these are *research adjudications*, not locked spec. The research itself flags that its band-reclassification "cannot be canon while the spec asserts the opposite" and lists owner-gated spec amendments (`fault-floor-definition:199-202`, `:247`). See §6 and the tensions list.

---

## 4. Constraint = rule (decidability, not syntax, governs proof participation)

**Constraint modifiers are shorthand for rules — locked spec.** "A constraint modifier — `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces` — desugars to the equivalent `rule` with a generated rationale. `field Qty as number min 5` is shorthand for `field Qty as number` plus `rule Qty >= 5 because "Qty must be at least 5"`." (`spec §2.4:1135`)

Two consequences the spec draws explicitly:
- The generated rationale satisfies Principle 9; an author wanting a custom reason writes the full `rule … because` — "the two are equivalent" (`spec:1137`).
- **"A constraint modifier participates in compile-time proof identically to the equivalent rule… `min 5` and `rule X >= 5` are interchangeable to the proof engine. Proof participation is a function of a constraint's decidability, not its syntactic form: a literal bound is always decidable, and a relational constraint is decidable only insofar as the referenced fields' own bounds make it so — equally true whether written as a modifier or as a `rule`."** (`spec §2.4:1138`)

The three *flag* modifiers (`optional`, `editable`, `ordered`) are **not** rule shorthand — they declare nullability, per-state write access, and ordinal-comparison capability (`spec:1140`).

**Desugaring is syntactic, evaluation-engine-preserving.** The composition research grounds this: field constraints and named rules "both desugar to existing constructs… The desugaring is syntactic — no new runtime primitives" (`constraint-composition.md:82-105`), with a resugaring/diagnostic-fidelity requirement so violations attribute to the original construct (`:107`, `:320`). Precept's constraint evaluation is a Boolean lattice — multiple constraints are the *meet* (implicit conjunction), collect-all semantics reporting every failure (`constraint-composition.md:29-39`). Rules are field-scoped, closed over fields, **not** parameterized and **not** rule-to-rule referencing — "each rule stands alone" (`:52`, `:277`, `:316`).

**Where "decidability, not syntax" bites — the bounds-only finding.** The `NumericInterval` domain represents exactly `X ∈ [a,b]`; the relational fact represents exactly `x − y ≤ c` (`bounds-only:35`, `:44`). It structurally *cannot* represent equality, disequality, disjunction, 3-field sums, or non-numeric domains (`bounds-only:40-47`). So a *modifier* and a *rule* of the same shape are equally (un)decidable — but shape determines decidability: `A == B`, `A + B == C`, string/boolean/currency rules are undecidable by intervals regardless of how they're written (`bounds-only:64-79`). The value-fold against defaults is a *distinct, complementary* mechanism (concrete-value question) that the interval engine cannot subsume, and vice versa (`bounds-only:140-143`, C1/C2/C3 `:147-166`).

---

## 5. The two-mechanism model and the Composition seam

**§0.7 is the canonical statement** (`spec:262-274`), opening: "Precept's guarantees are delivered by two distinct mechanisms; keeping them distinct is what makes the guarantee precise rather than magical." (`:264`)

**Mechanism 1 — Fault prevention, entirely at compile time.** "A precept that compiles without diagnostics cannot produce a runtime fault — no division by zero, no overflow, no empty-collection access, no result outside a declared bound. The compiler delivers it by discharging, at every fault-prone operation, an obligation that each operand *carries* a sufficient constraint… It proves a structural fact — that the operand carries its constraint — never the concrete runtime value. If it cannot, it **rejects the definition**… there is no deferral." (`spec §0.7:266`)

**Mechanism 2 — Governance, at runtime on external input.** "Every declared constraint is enforced on every value entering the entity from outside the definition — event arguments, construction inputs, direct field edits — at the moment it enters, before any computation derives from it. This enforcement is operation-blind… Because mutations execute on a working copy that is discarded if any constraint fails (§3A.4), an invalid configuration never persists — this is prevention, not detection." (`spec §0.7:268`)

**The Composition seam (the linchpin, and philosophy.md:55).** "For a value supplied at runtime, the compiler's proof that the operation cannot fault rests on the structural fact that the value carries a declared constraint; governance makes that constraint true of the value at ingress. The proof is therefore complete at compile time and the fault never occurs — proven by the compiler, its precondition discharged by governance, nothing left to a runtime check." (`spec §0.7:270`)

This is `philosophy.md:55` stated verbatim as commitment: "It requires the value to *carry* a constraint sufficient to prove the calculation safe, and the engine enforces that constraint the moment the value enters. The compiler proves a structural fact — that the value carries its constraint — never the value itself; the runtime enforcement is what makes that carried constraint true, **not a second line of defense.**"

**The boundary of the guarantee — the contract-ingress surface.** "The guarantee covers every value entering through the contract — construction, events, edits. Data that enters *outside* the contract is not re-proven. Restored state is trusted as valid… hydration is fast and does not re-validate." (`spec §0.7:272`) The compile-time-guarantee-boundary research independently converged on this exact line from four directions (`:85-89`), and its key structural insight: the compiler must NOT decide everything — the inspector's "Possible" verdict (missing arg → Kleene Unknown; or genuine first-match ambiguity) *presupposes* outcomes that stay open until input arrives, so "if it decided every outcome, there would be nothing to inspect" (`:60-72`).

**What governance-vs-validation adds.** The reference doc grounds the "not validation — governance" claim as a *spectrum*: pure validation → convention-enforced → layer-enforced → structural governance, with only Precept (and DB CHECK constraints, at the storage layer) reaching structural governance (`governance-vs-validation.md:41-51`). It catalogs the four failure modes governance closes — Bypass, Timing Gap, Scattered Rules, Silent Mutation (`:127-195`) — and, importantly for neutrality, names **where validation is sufficient**: ephemeral input, display formatting, stateless transforms, external ingestion, hot paths (`:199-209`).

---

## 6. The OPEN question the niche packet frames

**The question, verbatim.** "Is compile-time checking beyond the fault floor Precept's niche, or right-size to fault floor + governance?" (`compile-time-niche-decision-packet:1-2`). The packet is explicitly **"a decision packet — evidence for owner decision, NOT a decision"** (`:1`, `:13`).

**Its own verdict is deliberately unsettled.** "Clear case for Option 2: **partial** · Confidence: **medium**" (`:15`). The packet leaves the owner two live choice points it does not settle: "whether Layer 2 becomes the marketed identity or remains an unmarketed quality layer, and when the evolution deploy-lane decision is scheduled" (`:133` caveat 6).

**The two poles:**
- **Option 1** — fault floor (completed) + runtime governance + the live per-keystroke inspector, plus zero-ceremony warning-tier lints (`:163-188`, the Null Red-Team position).
- **Option 2** — compile-time checking as Precept's *defining niche*: "deterministic, solver-free, universal reasoning (all inputs / all paths / all future time, within one definition) plus a static-first consumption paradigm" with the inspector retained as escape hatch (`:137`, the Advocate position).

**The recommended middle shape — three layers** (`:123-125`): Layer 1 fault floor (prove-or-reject, total, completed); Layer 2 proven-violations universal layer (conservative, **flag-sound**, warning-default with per-diagnostic earned error promotion); Layer 3 static contract projection. Explicitly *deferred to separate decisions*: definition-evolution/deploy-diff lane, doomed-trajectory runtime refusal, blanket-mandate postures, and the sketched language unlocks (expiry rows, conversion action) which route to `/design` individually.

**The flag-soundness contract is the packet's key predictability move** (`:117`): the fault floor stays prove-or-reject (total, stable); everything beyond ships under a contract where "a flag is always a true universal statement about the definition; silence beyond the fault floor promises nothing." Under it, an edit crossing the fragment boundary can only *lose* a warning, engine growth only *adds* flags, and the green ledger never claims more than the floor.

**What the packet concedes on both sides (kept neutral):**
- FOR Option 2: probe-verified defect classes structurally mute to *both* governance and the inspector — silent wrong-success routing, definition self-contradiction, clock-decay, guard-locked strands, qualifier-source mutation, decision-band gaps (`:23-85`); three of the four highest-value analyses are *already* §0.6 obligations 7-10/12, specification-only (`:34`, `:148`); three inspector-blind defects shipped in hand-polished samples (`:121`, `:145`).
- FOR Option 1: the single most business-severe class (wrong-but-in-band formula/constant) is undecidable for *any* compile-time system — the inspector is the only catch (`:90-92`, `:175`); today's corpus catch-rate for the niche classes is "honestly near zero" against 77 idiomatic samples, so "the case rests on floors-plus-demand, not measured corpus incidence" (`:119-121`).
- The **predictability red-team, verified live**: PRE0155 fires a *false* "no valid configuration" claim across provably-disjoint guards (an idiomatic tiered policy) — "the shipped increment over-claims today, so promote-to-error is demonstrably unsafe as-is" (`:117`, `:171`). Twin precepts encoding one business meaning three ways (modifier / guarded-rule / literal-rule) get three different guarantee levels (`:172`, `:184`). This is the sharpest tension between the two-mechanism identity and any expansion of the prove leg.

---

## Tensions internal to the canon (flagged, not resolved)

1. **The spec's fault-prevention clause asserts what the fault-floor research says is misclassified.** §0.7:266 lists "no result outside a declared bound" as a compile-time fault-prevention guarantee, and Principle 11 lists "constraint range impossibility" as a fault class (`spec:112`). The fault-floor research adjudicates band-containment (OutOfRange/Length/Count) as *recoverable governance, not fault* — "the current message is factually false" (`fault-floor-definition:74-86`, `:132`). **This is a live spec-vs-research contradiction requiring owner-gated amendment** (`fault-floor-definition:199-202`, `:247`); it cannot be resolved inside a design pass. Quoting the current locked text: spec §0.7:266 *"…no result outside a declared bound."* — the research recommends this clause be reinterpreted to proven-violation-only, an owner decision.

2. **§0.6 item 6 ("Assignment range impossibility… is a compile-time error", `spec:208`) implies band prove-or-reject** — the same misclassification the research flags. The research recommends "§0.6 item 6 reinterprets to proven-violation-only" (`fault-floor-definition:201`).

3. **Principle 11 / §3A.6 assume a floor that isn't total today.** "If a precept compiles without diagnostics, it does not fault at runtime" (`spec:112`) is contradicted by the 10 live fail-open obligation positions — "clean-compiling definitions exist TODAY whose first inspection faults" (`fault-floor-definition:140`, `:223`). The *design* is sound; whether it is *honored* is an open completeness question (`compile-time-guarantee-boundary:99`). This is drift between an "Implemented"-flavored claim and code, per the standing CLAUDE.md doc-sync rule — surfacing it, not resolving it.

4. **The §0.6 discharge-tier sentence promises a tier that is vacuous today.** Spec §0.7 promises "a declared modifier or rule" as a discharge tier, but `min 1.0` discharges nothing (DeclarationValue satisfactions resolve to null) — "Either make DeclarationValue satisfactions resolvable… or amend the spec sentence. Owner decision." (`fault-floor-definition:166`)

5. **A verified canon self-contradiction on Restore semantics** (validate vs trusted-hydration) that "must be reconciled regardless" (`compile-time-niche-decision-packet:133` caveat 3) — though §0.7:272 leans to trusted-hydration.

6. **The "domain integrity" terminology gap is acknowledged-but-unpromoted.** The philosophy uses "domain integrity engine" as if self-evident; the research says the relational reading is too narrow and recommends a one-sentence lineage note plus elevating "governed integrity" as the specific term (`domain-integrity-formal-concept.md:271-281`). This is a pending, owner-gated philosophy insertion, not a change to make unilaterally.

---

## Method notes for the reader

- I read every named source first-hand. Direct quotes are verbatim from the lines cited.
- I did **not** read the quarantined boundary-ruling cluster (`proof-engine-boundary-ruling-*`, `d2-*`, `band-guarantee-*`, `prove-govern-*`), per instruction.
- Where I cite research adjudications (fault-floor, niche packet, boundary, bounds-only), I have marked them as research/evidence — not as locked canon. The niche packet, fault-floor, and boundary docs carry agent-authored "locked"/"converged"/"verdict" language; per the neutrality discipline I engage those as arguments, not as binding the owner. The only *locked* canon here is `philosophy.md` and `precept-language-spec.md §0.1/§0.3/§0.4/§0.6/§0.7/§2.4`.
- Two load-bearing facts about the epistemic state, stated once: the runtime evaluator is stubbed, so all runtime-mechanism claims in canon are design intent + prototype + §3A behavior, not observed execution (`compile-time-guarantee-boundary:23`, `compile-time-niche-decision-packet:133`); and the niche-packet corpus catch-rate for the beyond-floor classes is near-zero today, so that leg of the case is prospective (demand + corpus-bias reasoning), not measured.