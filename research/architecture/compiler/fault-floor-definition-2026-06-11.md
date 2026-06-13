---
status: Active — horizon groundwork; feeds compiler-readiness-plan-2026-06-11.md (its floor/gap/scale-back content derives from this research)
authored: 2026-06-11
author: research-ultra workflow (10 agents: 2x independent fault adjudication, engine inventory, dependency analysis, 2x independent floor derivation, reconciliation, red-team, convergence critic, synthesis)
topic: The fault floor — exact adjudicated fault set, minimal prove-or-reject proof-engine floor, built-vs-floor reconciliation (gaps + scale-backs), floor->runtime->niche sequencing
external-engagement: purely-internal — adjudication of Precept's own fault registry/runtime design/proof engine against its own canon; external precedent was consumed upstream in the niche decision
---

# The Fault Floor — Definition, Reconciliation, and Right-Sizing Evidence

> What exactly must Precept's compiler prove-or-reject (the fault floor), what the proof engine built beyond it, what floor obligations are missing, and the dependency-forced sequencing (floor → runtime → flag-layer gravy). Full layer detail: `docs/Working/compiler-readiness-plan-2026-06-11-appendices/fault-floor-research-full.md`.

## Background

The compile-time-niche decision (`docs/Working/compile-time-niche-decision-packet-2026-06-10.md`) set the strategy: fault floor = prove-or-reject; universal layer = flag-sound, post-runtime. The owner suspected the engine had over-stepped the floor (band-containment rejections that are really rule-preservation) and asked for an exhaustive floor definition + reconciliation to ground a new compiler readiness plan.

## Methodology

- **Research question:** what is the fault floor, exactly — and where does the built engine fall short of or exceed it?
- **Method:** two independent fault-taxonomy adjudications (per-FaultCode "can-it-compute" test against the designed runtime), a complete built-engine inventory, runtime-dependency analysis; then two independent floor derivations and a built-vs-floor reconciliation; then a two-direction red-team (floor too narrow / too broad, carries-proof composition traced end-to-end) and a convergence/completeness critic; synthesis weighed all of it. Convergence between independent copies is the evidence.
- **Grounding:** `FaultCode.cs`/`StaticallyPreventableMap.cs`, `docs/runtime/*` (evaluator/fault-system/result-types/runtime-api), spec §0.1/0.4/0.6/0.7/2.4/3A, `docs/compiler/proof-engine.md`, the proof-engine source, `bugs.md`, the niche packet — plus 10+ live `precept_compile` probes. Stale guarantee strawmen denylisted (verified: zero denylisted reads).
- **Time bounds:** 2026-06-11, against the working tree of `spike/Precept-V2-Radical`.

## Findings

### The adjudicated fault set

**[true-fault] DivisionByZero (FaultCode 1, PRE0083/PRE0046, bijective)**

A+B converge, red-team confirms: x/0 has no result (Principle 10 forbids NaN/Infinity); fires mid-expression before any constraint verdict; only the Faulted trap exists. Floor posture (two-tier divisor obligations, spec 0.6 item 3) correct as built. Two defects ride the lane: obligation creation is position-incomplete (guard slots etc.), and the code is abused as the generic fallback fault label for every non-bijective proof family (ProofEngine.Diagnostics.cs:488-491) — fix the linkage, not the classification.

**[true-fault] SqrtOfNegative (FaultCode 2, PRE0084, bijective)**

No real result; same abort shape. Floor, correctly prove-or-rejected. Minor: all >=0/threshold-0 obligations route to this label (slightly lossy for pow-exponent); position-completeness caveat applies.

**[type-checker-owned] TypeMismatch (FaultCode 3)**

Genuine can't-compute, but prevention is total static type checking over the closed vocabulary; plans encode resolved slots/overloads. Registry entry stays as defense-in-depth trap for out-of-contract data only. No proof-engine obligation owed.

**[type-checker-owned] UndeclaredField (FaultCode 4)**

Binder-owned; compiled plans address fields by slot index. Defense-in-depth trap only.

**[true-fault] UnexpectedNull / unproved presence (FaultCode 5, partner UnprovedPresenceRequirement per FaultCode.cs:23)**

Reading an unset optional in value position yields nothing to continue with — often inside the very guard that would produce a verdict. Floor (PRE0116 presence obligations). Distinguish: missing required arg at API boundary = InvalidArgs; Kleene Unknown inspection = never a fault. Defects: StaticallyPreventableMap deliberately excludes it from bijective rows so presence traps mislabel as DivisionByZero; fault-system.md shows stale NullInNonNullableContext partner; presence-wrapping-contradiction false-Proved sits in this lane.

**[type-checker-owned] InvalidMemberAccess (FaultCode 6)**

Accessor validity is Types-catalog static knowledge. Defense-in-depth trap only.

**[type-checker-owned] FunctionArityMismatch (FaultCode 7)**

Static signature checking; plans encode resolved overloads. Defense-in-depth trap only.

**[mixed] FunctionArgConstraintViolation (FaultCode 8, PRE0022)**

Split: literal-arg violations = type-checker-owned (PRE0022, Type stage, stays). Computed-arg violations of COMPUTABILITY domains (pow exponent >= 0, sqrt operand >= 0 — Functions.cs:187-206) = true fault; B verified obligations are live via catalog ProofRequirements; A's per-FunctionMeta totality sweep still owed (every constrained parameter x every call position, intersects BUG-032). Boundary guard: catalog ParameterMeta constraints must remain computability-only or a discretionary band smuggles a rule into the fault lane.

**[true-fault] CollectionEmptyOnAccess (FaultCode 9, PRE0063, bijective)**

.first/.peek/.last/.min/.max on empty has no element. Floor: count>0 obligations, correctly prove-or-rejected; strictly distinct from the count-BAND lane (computability vs policy). Defect: collapse target for index-bounds (PRE0100) and key-presence (PRE0099) faults, producing false 'collection was empty' messages on non-empty collections.

**[true-fault] CollectionEmptyOnMutation (FaultCode 10, PRE0064, bijective)**

dequeue/pop/take from empty has nothing to move/bind. Floor; sequential staleness (pre-shrink facts must not discharge post-shrink mutations) is load-bearing and built. Defect: PRE0101 duplicate-key collapses here — semantically false under any reading.

**[mixed] QualifierMismatch (FaultCode 11, PRE0068)**

Four-way split, A+B converge on three: (a) operation-site cross-qualifier arithmetic (USD+EUR, kg+m) = TRUE FAULT, floor — qualifiers are part of the type, not 2.4 sugar; BUG-030 (dropped UCUM cancellation factor, false Proved) is a floor-grade fix in this lane; (b) wrong-qualifier external value at ingress = governance (InvalidArgs/ingress refusal — the current Faults.cs message describes this sub-case, mislabeling the registry entry); (c) typed-constant vs declared qualifier + counting units = type-checker-owned. (d) DISPUTED: the assignment lane (PRE0141 + the unbuilt qualifier-source-mutation obligation) — behavior consensus is KEEP both rejections and BUILD the qualifier-source obligation (silently-wrong-money is the flagship claim; no graceful typed outcome represents committed wrong-identity data), but the taxonomy (fault vs a named type-identity third category) is an owner call; A's keep was conditional on the category rename. Fault-link fix owed: family currently traps as DivisionByZero.

**[mixed] NumericOverflow (FaultCode 12, PRE0078, bijective)**

THE central inversion, triple-confirmed (A, B, red-team probes): sub-case A — decimal REPRESENTABILITY overflow = TRUE FAULT (decimal arithmetic throws mid-plan; Principle 10 forbids Infinity) and is UNPROVEN ANYWHERE (obligation creation gated on declared bounds, Actions.cs:294-305; unbounded A*B compiles clean with zero obligations). Sub-case B — DECLARED-band containment (what PRE0078 actually checks) = rule-misclassified: bounds are 2.4 sugar, runtime violation is recoverable ConstraintsFailed; live probes show error-on-merely-unknown intervals while spec-identical rule twins compile clean (twin-syntax inconsistency). Red-team adds a second inversion: min/max bounds currently discharge NO fault obligations (DeclarationValue satisfactions resolve to null — probe: min 1.0 does not discharge div0), so the band prove-or-reject protects a vacuous composition — while the constraints that DO discharge (sign modifiers, rule facts) carry no preservation obligations. The message ('exceeded the representable range') is factually false in the band case.

**[rule-misclassified-as-fault] OutOfRange (FaultCode 13, PRE0079)**

Unanimous: the fault message defines itself as a declared-band violation; an out-of-band value computes, stores, compares; every runtime lane refuses recoverably (ingress, sweep, Create sweep). No can't-compute referent — remove from registry. The compile check (literal default vs own bound) SURVIVES as definition incoherence (every Create would fail — PRE0164/PRE0115 genus, a third category), keeping Error severity with no author-visible change. Note this prover already has flag posture (fail-open on undecidable magnitude — the opposite of its count/length siblings), internal evidence the band family was never floor.

**[rule-misclassified-as-fault] LengthBoundViolation (FaultCode 14, PRE0135)**

Unanimous and clean: no string operation is undefined by length; minlength/maxlength are 2.4 sugar verbatim; the FaultMeta RecoveryHint concedes 'or remove the length bound if runtime validation is sufficient'; the rule twin compiles clean. Length bounds discharge NO fault obligation, so they exit the floor with no carries-proof residue. Remove from registry + bijective core. Incidental: broken '?' message placeholder.

**[rule-misclassified-as-fault] CountBoundViolation (FaultCode 15, PRE0136)**

Band containment (mincount/maxcount) = rule sugar; add-to-full computes; sweep refuses recoverably; the code's own comment calls the runtime trap 'a defense-in-depth backstop, not the boundary enforcer' (ProofEngine.cs:1106-1108). Emptiness faults stay under codes 9/10 (mechanically distinct lanes). CARRIES-PROOF CAVEAT (red-team): mincount>=1 discharges the count>0 access-safety floor obligation, so mincount keeps a preservation/sweep story — it is proof-carrying; maxcount is not. Mincount-at-birth false-Proved must be fixed or retired WITH the lane (no interim ledger lie); under reclassification empty-at-birth becomes a recoverable Create refusal plus an every-Create-fails definition flag, forcing a Create-semantics decision.

**[true-fault] IndexOutOfBounds (NO FaultCode — registry gap; obligation exists: IndexBoundsProofRequirement -> PRE0100)**

.at(5) on a 3-element list can't compute; same genus as empty-access but NOT emptiness. Compile side built (both-bounds-per-branch discipline, floor-keep); mint FaultCode.IndexOutOfBounds and stop the false-emptiness collapse.

**[true-fault] KeyNotFound (NO FaultCode — registry gap; obligation exists: KeyPresenceProofRequirement -> PRE0099)**

By-key read/remove at an absent key has no value to produce. Compile side built (contains-guard matching, floor-keep); mint FaultCode.KeyNotFound.

**[mixed] DuplicateKey / PRE0101 KeyUniquenessGuard**

CONTESTED pending owner semantics decision: under require-absence add semantics it is can't-compute (true fault, own DuplicateKey code); under put/overwrite or recoverable-refusal semantics it is a uniqueness RULE (governance). Keep the rejection conservatively until adjudicated; the collapse to CollectionEmptyOnMutation is wrong under either reading.

**[true-fault] DimensionalProduct nonexistence (PRE0157, no FaultCode)**

kg x m with no cataloged dimension has no representable result type — TypeMismatch genus. Prove-or-reject correct as built; needs honest fault identity (currently the DivisionByZero backstop).

**[true-fault] TemporalOverflow (no FaultCode, no obligation)**

date/datetime ± period beyond the representable range throws (NodaTime) — same representability genus. Confidence divergence between copies resolved as verify-then-build: run the temporal probe FIRST (read-before-asserting), then build the obligation + FaultCode in the representability family.

**[type-checker-owned] Choice-default membership (no fault code; the PRE0086 asymmetry)**

NEGATIVE adjudication, unanimous: a rogue choice value stores and compares — not a fault. It is a statically decidable closed-type-domain error the TC misses at the default position. Floor-adjacent only because it breaks 'Create from defaults always succeeds' (C59/C86). Close in the type checker; keep out of the fault registry.

**[rule-misclassified-as-fault] maxplaces (decimal-places band)**

Negative adjudication (B; dropped downstream, restored here): 2.4 sugar; rounding computes; approximation honesty governs. The niche packet's floor-breach row ('maxplaces vs division') resolves in the flag/governance lane: ship places-dataflow checking only WITH round(value, places) recognition (packet line 129) — never as fault-floor prove-or-reject.

**[type-checker-owned] Evaluation stack depth (no FaultCode)**

Raised by A, dropped downstream, restored: compile-time enforced (Builder Pass 5, MaxStackDepth <= 32; evaluator.md:526-532 'no runtime stack overflow risk'). No proof obligation owed. One-line disposition needed in the registry repartition: add a corrupted-model backstop code or document that the four existing backstop traps suffice.

### The floor

THE FAULT FLOOR — prove-or-reject, total, stable: if the compiler cannot prove a fault absent, the definition is rejected. A fault is a mid-evaluation abort with no recoverable typed outcome (only the Faulted trap); anything the working-copy discard or ingress refusal handles (ConstraintsFailed/InvalidArgs/Unmatched/Rejected) is governance.

OBLIGATIONS (11 families): (1) divisor != 0 at every division/modulo; (2) sqrt/threshold-0 operand >= 0 (catalog-sourced); (3) presence — every optional-field read in value position provably set; (4) collection non-empty on every value-yielding accessor; (5) collection non-empty on every element-consuming mutation; (6) index in-bounds (0 <= N < count; <= for insert); (7) key presence on by-key read/remove; (8) operation-site qualifier compatibility incl. dimensional products (cross-currency/cross-dimension arithmetic; with correct UCUM factors — BUG-030 fixed); (9) decimal-representability overflow at every arithmetic sink — THE one missing obligation family, discharged from declared bounds as carried facts (obligation-driven bounds demand), gated on the owner's numeric-representation decision; (10) temporal representability (probe-first, same family; includes UCUM factor application and compile-side constant folding, which must be checked-fold); (11) computed-arg function computability domains (ParamSubject, per-FunctionMeta totality sweep owed). The floor's static EDGE — TypeMismatch/UndeclaredField/InvalidMemberAccess/FunctionArityMismatch — is owed by binder/type-checker totality, not proof obligations.

POSITIONS — totality is itself a floor property: every obligation must be created at EVERY expression position of the 5 expression-bearing DUs (TypedExpression[15], TypedAction[3], TypedInterpolationSegment[2], TypedTransitionRow[2], TypedEventRow[2]). The 10 fail-open positions (5 guard slots incl. the never-iterated AccessModes collection; 2 because-interpolation slots; 2 default interiors; TypedMemberAccess.Arguments/BUG-032) plus 6 latent no-default-arm holes are floor breaches in every lane — live-proved: a compiler-proven-zero divisor in a `when` guard compiles clean today and faults on first Fire/InspectFire. Durability is guaranteed structurally by the catalog ChildExpressionPositions projection + 3-layer creation-exhaustiveness checker.

MINIMUM DISCHARGE POWER — exactly spec §0.7's three tiers: (1) literal fold / statically-known safe value; (2) standing declared constraint (modifier ProofSatisfactions subsumption, mincount>=1 -> count>0, guaranteed presence, unconditional rule facts); (3) author guard in path (OR-branch x AND-conjunct, all-branches discipline, is-set / count / contains / both-index-bounds shapes) — PLUS three soundness disciplines that are part of the minimum, not optional: (a) sequential staleness (ReassignedBefore/CountInvalidatedBefore — a guard fact must not survive reassignment); (b) THE RED-TEAM AMENDMENT — write-aware tier-2: a standing constraint (modifier or rule) may discharge a fault obligation only if no same-transaction prefix write to the constrained field is unaccounted for (live-proved unsound today: positive-modifier and rule-fact discharges survive an unbounded prefix write — green ledger, guaranteed contract-data fault); repair shape (preservation obligations vs staleness+re-establishment) is an open design decision; (c) intra-guard short-circuit conjunct narrowing for presence (`when X is set and X > 5`) — minimum, not precision: without it, walking the guard slots mass-false-rejects the canonical optional idiom. FLOOR PREREQUISITE (promoted by the red-team): the DesugarsToRule -> runtime constraint-plan wiring + ingress enforcement — committed-state tier-2 discharge of true faults is unsound unless the discharging constraints are enforced on every commit/ingress lane; zero consumers exist today.

FRICTION-REDUCTION (kept, labeled — sound machinery that stops the floor over-rejecting safe programs, never the only cited fix in a rejection message): flow narrowing (X>Y => X−Y != 0 — spec-claimed at §0.6, removal would be a spec regression), sign-set algebra + compositional transfer, collection-growth forward facts, qualifier guard-narrowing (open-operand pinning), event-ensure folds + trusted-rule-fact second pass (with mandatory circularity/self-unsat blocks), forwarding-fact vacuous suppression (NOW VERIFIED SOUND: GraphAnalyzer BuildEdges ignores row.Guard, so reachability over-approximates and unreachable=structural; add ProofStrategy.Vacuous for ledger honesty), PE-G13 error-taint suppression.

NOT FLOOR — the carries-proof boundary (red-team re-scope): NON-PROOF-CARRYING standing constraints exit; proof-carrying ones do not. Exits cleanly: length bands (discharge no fault), maxcount, data bands on fields feeding no fault proof, maxplaces, reject-on-unprovable band writes into non-proof-carrying fields — all to governance sweep + flag-sound proven-violation warnings. Does NOT exit: mincount's count>0 discharge role; sign-modifier/rule-fact preservation (ENTERS the floor — the live hole); bounds consumed by future representability proofs (become proof-carrying, substantially reinstating write-time checking for arithmetic-feeding numeric fields). THIRD CATEGORY — definition incoherence (PRE0079-on-default, PRE0164, PRE0115): proven self-contradictions where no valid Create exists; legitimate Error rejections, neither fault floor nor flag layer. FLAG LAYER (post-runtime): Pass 1.5 satisfiability (PRE0082/0153/0154/0155/0159), already warning-posture; soundness prerequisites (PRE0155 guard-scope fix, readable messages) owed before any identity claim; error promotion earned per-diagnostic against the shipped evaluator.

### Convergence

STRONG CONVERGENCE with productive divergences, all resolved on evidence. (1) A and B independently agreed on the headline adjudication for all 15 registry codes: 5 true faults (div0, sqrt, presence, 2x collection-empty), 4 type-checker-owned, 3 rule-misclassified (OutOfRange/Length/Count), 2 mixed with IDENTICAL sub-case splits (QualifierMismatch, NumericOverflow — including the central inversion: the engine prove-or-rejects the declared band while the representability fault has no obligation anywhere). Both independently derived the same minimum discharge power (spec §0.7's three tiers + staleness) and the same position-totality requirement. (2) Narrow A/B divergences, each resolved: qualifier assignment lane (largest — 2 obligations; behavior consensus keep, taxonomy to owner; recon's 'both keep' overstated A's conditional position — corrected here); BUG-017 split (representability half survives as the new obligation, band half dissolves); FunctionArg liveness (B source-grounded live, A's totality sweep still owed); temporal overflow (confidence only — resolved verify-then-build); PRE0101 (B's enumeration more complete); B's decisive DesugarsToRule zero-consumers finding (A lacked it; it became a hard ordering constraint and was then PROMOTED to floor prerequisite by the red-team). (3) The red-team CONFIRMED the misclassification verdict and the five core true faults, REINFORCED sequencing, and found the one hole both copies missed: tier-2 standing-constraint discharge is not write-aware (two new live false-Proved probes — sign-modifier and rule-fact discharge across an unaccounted prefix write), forcing the carries-proof re-scope of the scale-back ('non-proof-carrying constraints exit' replaces 'bands exit') and the representation-decision gate on the representability family. Probe 3 (min-bound discharge vacuity) STRENGTHENED the misclassification case — the band lane protects a composition that does not exist today. (4) The critic found no adjudication reversals; its 8 findings are carry-forward drops, all dispositioned in this synthesis: omit×global-rule join, binder shadowing, maxplaces/round-recognition (packet rows verified present this session), stack-depth trap, intra-guard conjunct narrowing (promoted to minimum power), constant-fold overflow, the recon-item-17 honesty fix, and the reachability-direction verification — which I CLOSED this session by reading GraphAnalyzer.cs: BuildEdges (:316-346) constructs edges from every transition row without consulting row.Guard, so reachability over-approximates and the forwarding-fact vacuous suppression is sound. Evaluator.md's own impossible-path table (listing exactly the 9 true-fault/TC-owned codes and omitting all five contested ones) is independent convergent evidence the design always knew the boundary the registry blurred.

### Gaps (floor obligations lacking a sound live implementation)

- **[critical (runtime-blocking)]** Obligation-position incompleteness: 10 fail-open positions (5 guard slots incl. never-iterated AccessModes, 2 because-interpolation slots, 2 default interiors, TypedMemberAccess.Arguments/BUG-032) + 6 latent no-default-arm holes. Live-proved: compiler-proven-zero divisor in a guard compiles clean; guards run per Fire and per-keystroke InspectFire with no fault lane in Prospect — falsifies spec 3A.6 on day one.
  - **Action:** Total walk over the 5 expression-bearing DUs. MUST bundle intra-guard short-circuit presence narrowing (when X is set and X > 5) or the guard-slot closure mass-false-rejects the canonical optional idiom. Expect a compat wave of new (correct) errors on existing corpus files.
- **[critical (floor-soundness; file as a bug alongside BUG-030)]** Write-unaware tier-2 discharge (NEW false-Proved family, red-team probes 1-2): declaration-attribute and trusted-rule-fact discharges consult no ReassignedBefore staleness; a same-chain unbounded write to a positive-modifier or rule-constrained divisor leaves the div0 obligation Proved — green ledger, guaranteed contract-data fault at Working-Copy step 4, before the step-6 sweep. Sign-modifier case is also cross-event (modifiers absent from the designed sweep).
  - **Action:** Add the write-awareness rule to tier-2. Repair shape is an open design decision: preservation obligations on writes into proof-carrying constraints (the PRE0078 mechanism re-aimed at constraints that actually discharge) vs staleness-invalidation + forward re-establishment from the written expression's interval (the discipline the collection lane already has). Per-lane mixes possible.
- **[critical (PROMOTED from scale-back ordering constraint to floor prerequisite: committed-state tier-2 discharge of true faults is unsound without runtime enforcement of the discharging constraints; also, retiring compile band gates before this wiring leaves bands enforced nowhere)]** DesugarsToRule -> runtime constraint-plan wiring + ingress enforcement: zero consumers today; bounds/modifiers are never synthesized into TypedRules, so the designed TypedRule/TypedEnsure-only sweep enforces neither bands nor sign modifiers.
  - **Action:** Wire modifier desugar into the constraint plan + ingress as part of the floor phase, landing as ONE coordinated move with the band scale-back and registry trim.
- **[high (the floor's one missing obligation family)]** Representability-overflow obligation: the registry's NumericOverflow fault is unproven anywhere (creation gated on declared bounds; unbounded A*B compiles clean). Includes UCUM conversion-factor application and compile-side constant folding (the fold itself must be checked or 2e28*2e28 literals crash the COMPILER).
  - **Action:** GATE on the owner numeric-representation decision first (fixed decimal + obligation-driven bounds demand vs arbitrary-precision numerics, which deletes the fault class). If decimal: unconditional obligation at every arithmetic sink, reusing IntervalOf/IntervalTransfer unchanged; honest friction statement — as derived it rejects unbounded A+B too, not just mul/pow chains.
- **[critical (must precede runtime)]** Qualifier-source mutation obligation: writes to a {X} qualifier base leave dependent money silently mislabeled under a Proved ledger — clean wrong success, live in fee-schedule.precept; flagship currency-safety claim ships false.
  - **Action:** New obligation on every mutation of a field referenced as a qualifier source: prove dependents re-established or reject.
- **[critical (must precede runtime)]** BUG-030: cross-unit cancellation drops the UCUM kg->g x1000 factor — false Proved admitting runtime magnitudes outside proven intervals.
  - **Action:** Apply the conversion factor in cancellation paths; strategy architecture unchanged.
- **[high]** Presence-wrapping-contradiction false-Proved (UnexpectedNull lane — floor).
  - **Action:** Fix the discharge path so the obligation stays Unresolved; floor phase.
- **[medium (runtime refuses recoverably either way — compile-side honesty breach)]** Mincount-at-birth false-Proved: a ledger lie consumed by the inspector's proof-attribution surface.
  - **Action:** Fix the seed or retire the obligation WITH the count-lane reclassification + emit the every-Create-fails definition flag; no interim state may leave the ledger lying. Forces the mincount-at-birth Create-semantics decision.
- **[high (floor honesty; a real presence or currency fault would report a divisor message)]** Fault registry/link repartition: 4 rule codes registered as faults; every non-bijective family traps as a literal `_ => FaultCode.DivisionByZero`; PRE0099/0100/0101 collapse to false-emptiness codes; UnexpectedNull excluded from bijective rows; fault-system.md drift (13 vs 15 members, stale partner).
  - **Action:** Execute the registryCorrections list as one pass.
- **[medium]** FunctionArg computed-arg totality: per-FunctionMeta sweep verifying every computability-constrained parameter carries a live ParamSubject obligation at every call position (intersects BUG-032).
  - **Action:** Cheap audit; also pin the catalog convention that ParameterMeta constraints stay computability-only.
- **[medium]** Temporal-arithmetic overflow: no obligation, no FaultCode; unprobed.
  - **Action:** Probe first (read-before-asserting), then build in the representability family with its own FaultCode.
- **[high (breaks C59/C86 'Create from defaults always succeeds')]** Choice-default membership (PRE0086 asymmetry at field/event-arg default positions): rogue default enters the hollow version at Create on a clean compile.
  - **Action:** Close in the TYPE CHECKER — not a proof obligation; route accordingly.
- **[medium]** omit x global-rule join (packet Layer-1 item, dropped between adjudication and reconciliation — restored).
  - **Action:** Adjudicate: likely an obligation-creation join in the presence lane (omit interacting with unconditional rules) — i.e. floor-shaped. Must get an explicit disposition row before the floor is declared enumerated.
- **[medium (spec-conformance gap)]** Min-bound discharge vacuity (red-team probe 3): spec §0.7 promises 'a declared modifier or rule' as a discharge tier, but min 1.0 discharges nothing (DeclarationValue satisfactions resolve to null).
  - **Action:** Either make DeclarationValue satisfactions resolvable — accepting this makes those bounds proof-carrying with the write-awareness consequences — or amend the spec sentence. Owner decision.
- **[medium]** Pre-runtime design reconciliations (in neither layer): fault-delivery contract (result-types.md:51 FaultException vs evaluator.md §7.6 EventOutcome.Faulted) must be settled before the evaluator lands; Restore mostly dissolves per spec :272 (next-operation re-governance + traps as out-of-contract backstop) with a definition-evolution residue routed to the separate deploy-lane decision.
  - **Action:** Settle Q1 as a design item between floor and runtime; note Restore as substantially resolved.
- **[low]** Binder shadowing / unused-binder (packet Layer-1 item, dropped — restored).
  - **Action:** Disposition: binder/TC-owned zero-ceremony syntactic lint per the packet verdict — not proof floor; file in the TC lane.

### Scale-backs (built beyond the floor)

- **PRE0078 declared-band IntervalContainment (set actions, computed fields, element writes)**
  - **Current:** Error on provable violation AND on merely-unprovable intervals ([-inf..+inf] rejects); obligation exists only where min/max declared; message claims 'exceeded the representable range' while checking the declared band; rule twins compile clean (twin-syntax inconsistency, live-verified)
  - **Recommended:** Reclassify-to-flag (proven-violation warning) + defer-to-governance (sweep refuses ConstraintsFailed) FOR NON-PROOF-CARRYING FIELDS ONLY. Carries-proof constraint: once representability obligations consume a field's bounds, writes into it need preservation or staleness handling — write-time prove-or-reject substantially reinstates itself for arithmetic-feeding numeric fields. Split/rename the diagnostic so the representability claim attaches only to the new representability obligation.
  - **Why:** Bounds are spec §2.4 rule sugar; runtime violation is a recoverable refusal, not an abort; the current message is factually false; but the red-team showed blanket exit would formalize the write-unaware discharge hole — the boundary is proof-carrying vs not, not band vs not.
- **PRE0135 LengthContainment (full static string-length interval algebra)**
  - **Current:** Error on proven violation and on unprovable (unbounded source into capped field), forcing bound-invention; FaultCode in the bijective core; broken '?' message placeholder
  - **Recommended:** Cleanest exit: proven violations -> flag-sound warning (provably-dead row); unprovable -> no compile diagnostic; enforcement -> governance sweep; remove FaultCode.LengthBoundViolation; fix the placeholder
  - **Why:** No string operation is undefined by length — length bounds discharge no fault obligation anywhere, so zero carries-proof residue; the FaultMeta RecoveryHint itself concedes 'or remove the length bound if runtime validation is sufficient'
- **PRE0136 CountContainment (~250-line sequential count-interval band tracking, 'no deferral' contract)**
  - **Current:** Error even on the FIRST unguarded add into a maxcount-1 collection; explicit prove-or-reject/no-deferral doc contracts (ProofRequirement.cs:214-226, ProofEngine.cs:1100-1108, proof-engine.md:1713/1731); mincount-at-birth false-Proved
  - **Recommended:** Maxcount direction: proven violations -> warning, unprovable -> silent, enforcement -> governance. Mincount: RETAINS a preservation/sweep story (proof-carrying — it discharges the count>0 access-safety floor obligation; the Strategy-2 mincount arm must survive the move explicitly). Remove FaultCode.CountBoundViolation; rewrite the no-deferral contracts; mincount-at-birth becomes a recoverable Create refusal + every-Create-fails flag
  - **Why:** Add-to-full computes; the code's own comment calls the runtime trap a defense-in-depth backstop, not the boundary enforcer; the emptiness faults are served by the separate count>0 lane; but mincount is the worked example of the carries-proof boundary
- **FaultCode.OutOfRange + PRE0079 default-vs-own-bound family**
  - **Current:** Registered as a statically-preventable fault; compile Error on literal default outside literal bound; prover already fails OPEN on undecidable magnitude (inconsistent with its count/length siblings)
  - **Recommended:** Remove the FaultCode (no can't-compute referent); KEEP the Error unchanged, reclassified as DEFINITION INCOHERENCE (third category — proven self-contradiction, every Create fails; PRE0164/PRE0115 genus). No author-visible change
  - **Why:** The fault message defines itself as a band violation; every runtime lane is recoverable; but a definition that contradicts its own defaults has no valid Create — rejecting it is legitimate on different grounds than fault prevention
- **PRE0164 DefaultViolatesRule + PRE0115 UnsatisfiableInitialState**
  - **Current:** Errors, proven-false-only folds (correct conservative posture)
  - **Recommended:** No behavior change; identity reframe to definition incoherence. Do NOT call them 'flag promotions' — an always-on Error 'earned' before the promotion oracle (the runtime) exists contradicts the flag contract's own rule; the three-category taxonomy (fault floor / definition incoherence / flag layer) both derivations converged on is the honest frame
  - **Why:** They serve creation-time coherence, not any runtime fault; the critic's taxonomy-consistency finding
- **BUG-017 (band half) / BUG-018 / BUG-021**
  - **Current:** Filed as floor bugs (band-direction obligation-creation holes)
  - **Recommended:** Dissolve as floor items — the lanes they describe reclassify to governance; BUG-017's representability half survives as the NEW obligation in the gap list, not as a band fix
  - **Why:** Under reclassification the sweep refuses recoverably; completing band-direction prove-or-reject would be building more beyond-floor machinery
- **Spec/canon edits baking the misclassification into the guarantee contract**
  - **Current:** Spec §0.7's fault-prevention sentence includes 'no result outside a declared bound'; Principle 11 lists 'constraint range impossibility' as a fault class; §0.6 item 6 implies band prove-or-reject; fault-system.md documents 13 members with a stale partner
  - **Recommended:** Owner-approved amendments surfaced explicitly — never bundled silently into implementation PRs (philosophy/spec edit gate). §0.6 item 6 reinterprets to proven-violation-only
  - **Why:** The reclassification cannot be canon while the spec asserts the opposite; CLAUDE.md philosophy rules require owner deliberation for guarantee-contract changes
- **HARD COORDINATION CONSTRAINT spanning all band scale-backs**
  - **Current:** DesugarsToRule has zero consumers; compile gates are currently the ONLY band enforcement anywhere
  - **Recommended:** The band scale-back, the DesugarsToRule->sweep/ingress wiring, the registry trim, and the spec edits land as ONE coordinated move — no intermediate state where bounds are enforced nowhere
  - **Why:** Retiring compile gates before runtime wiring would leave declared bounds entirely unenforced; verified zero-consumer grep

### Registry corrections

- REMOVE from the [StaticallyPreventable] registry and the bijective core: OutOfRange (13), LengthBoundViolation (14), CountBoundViolation (15) — no can't-compute referent; their compile checks survive under definition-incoherence or flag identities.
- RETARGET NumericOverflow (12): keep the code, but its bijective partner becomes the NEW representability obligation; the declared-band PRE0078 lane is split off and renamed so the 'exceeded the representable range' message attaches only to genuine representability.
- ADD FaultCode.IndexOutOfBounds and route PRE0100 to it (current collapse to CollectionEmptyOnAccess produces a false 'collection was empty' message on a non-empty list).
- ADD FaultCode.KeyNotFound and route PRE0099 to it (same false-emptiness collapse). PRE0101 gets DuplicateKey only if the owner adjudicates require-absence semantics; under put-semantics it exits the fault lane entirely.
- ADD the UnexpectedNull bijective row: route the presence family (PRE0116/UnprovedPresenceRequirement) to FaultCode.UnexpectedNull — StaticallyPreventableMap.cs:22-27 currently excludes it deliberately, so a presence trap would report a divisor message.
- RETIRE the generic `_ => FaultCode.DivisionByZero` backstop (ProofEngine.Diagnostics.cs:488-491): per-family honest routing — qualifier family (PRE0114/0141) -> QualifierMismatch; dimensional product (PRE0157) -> TypeMismatch-genus or a new code; dimension/modifier obligations likewise.
- ADD FaultCode.TemporalOverflow after the temporal probe confirms the gap (verify-then-build).
- KEEP TypeMismatch, UndeclaredField, InvalidMemberAccess, FunctionArityMismatch as defense-in-depth corrupted-model traps (no proof obligations owed); add a one-line disposition for evaluation-stack-depth exceedance (new backstop code or documented coverage by the existing four).
- FIX docs/runtime/fault-system.md drift in the same pass: 13-member listing vs 15-member code; stale NullInNonNullableContext partner vs FaultCode.cs:23 UnprovedPresenceRequirement.
- LEDGER honesty rider: add ProofStrategy.Vacuous so forwarding-fact suppression (verified sound this session — GraphAnalyzer.BuildEdges ignores guards, reachability over-approximates) stops recording as Proved/Literal.

## Implications for Precept

VALIDATED — floor -> runtime -> niche-as-gravy — with three load-bearing refinements the hypothesis must absorb. (1) The floor is simultaneously SMALLER in claim and LARGER in discipline than what is built: the band prove-or-reject lane exits (to governance + flags), while representability obligations, position totality, write-aware tier-2 discharge, the false-Proved repairs, the registry repartition, AND the DesugarsToRule->governance wiring enter. The wiring is promoted from scale-back ordering constraint to floor PREREQUISITE (tier-2 discharge of true faults is unsound without runtime enforcement of the discharging constraints), and the whole subtraction+wiring+registry+spec-edit package must land as ONE coordinated move — no window where bounds are enforced nowhere. (2) Floor completeness is a HARD runtime precondition, bidirectionally verified: every fail-open position is in the per-keystroke InspectFire path; the inspection result types have no fault lane (Prospect = Certain/Possible/Impossible); evaluator.md §10's 'no errors => never faults' and spec 3A.6 inspect honesty are written assuming the floor is total; live probes prove clean-compiling definitions exist TODAY whose first inspection faults. The band scale-back itself does NOT break 3A.6 (ConstraintsFailed has an inspection lane) — only the fault-shaped holes do, and they are all floor items. (3) The reverse direction holds: the runtime needs NOTHING from the flag layer (warnings never gate Precept.From; no evaluator contract references them), while the flag layer's error-promotion oracle, counterfactual/clock/projection unlocks, and teachable line all require the shipped runtime — and the live PRE0155 false positive shows premature promotion would actively reject correct precepts. Two design reconciliations sit between floor and runtime in neither layer: the fault-delivery contract (FaultException vs EventOutcome.Faulted) and the Restore residue (substantially dissolved by spec :272; remaining definition-evolution exposure routes to the separate deploy-lane decision per the niche packet). Order of work within the floor phase: probe/adjudicate the open items (temporal, omit-join, PRE0101) -> owner decisions (representation, write-awareness shape, taxonomy) -> the coordinated move -> position totality + checker -> false-Proved repairs.

## Conclusions

The fault floor is the 11-obligation-family, position-total, prove-or-reject core stated under Findings § The floor — with three soundness disciplines (sequential staleness, write-aware tier-2 discharge, intra-guard conjunct narrowing) and one promoted prerequisite (DesugarsToRule → runtime governance wiring) as part of the minimum. The band-containment lane (OutOfRange/Length/Count + PRE0078-band) exits the floor to governance + flag-sound warnings, as ONE coordinated move with the wiring/registry/spec edits (no window where bounds are enforced nowhere). Definition incoherence (PRE0079-on-default/PRE0164/PRE0115) is a third category: always-Error, neither fault nor flag.

- **Rationale:** can-it-compute adjudication, doubly derived, red-teamed; the evaluator's own impossible-path table independently lists exactly the true-fault/TC-owned codes and omits the contested ones.
- **Alternatives considered:** keeping the band lane as floor (rejected — no can't-compute referent; current prover already fails open on undecidable magnitude, and band bounds discharge no fault obligation today, so the prove-or-reject protects a vacuous composition); folding definition-incoherence into the flag layer (rejected — contradicts the earned-promotion oracle rule).
- **Precedent:** spec §0.7's own two-mechanism split + discharge tiers; the niche-decision packet.
- **Tradeoff:** floor friction moves — representability obligations add bounds-demand on arithmetic-feeding fields (under the decimal option) while band scale-backs remove invented-bound friction; the populations differ, so 'net neutral' is not claimed.

## What would change this conclusion

- The numeric-representation decision going to arbitrary precision (deletes the representability family and most reinstated write-time checking).
- The runtime shipping with a fault-delivery contract that makes band violations non-recoverable (would re-open the band adjudication).
- The owner adjudicating PRE0101 as require-absence (adds DuplicateKey to the fault set) or rejecting the three-category taxonomy.

## Open Questions (owner decisions the plan must gate on)

- Numeric representation (gates the representability family): fixed decimal with obligation-driven bounds demand and prove-or-reject representability — versus arbitrary-precision numerics, which deletes the NumericOverflow fault class and its friction wave entirely. The runtime value representation is still stubbed and choosable; the honest friction statement under decimal is that unbounded A+B rejects, not just mul/pow chains.
- Write-awareness repair shape for tier-2 discharge: (i) preservation obligations on writes into proof-carrying constraints (the existing PRE0078 mechanism re-aimed at the constraints that actually discharge), versus (ii) staleness-invalidation of declaration/rule facts with forward re-establishment from the written expression's interval (the discipline the collection lane already implements). Per-lane mixes are possible; real authoring-ergonomics consequences either way.
- Qualifier assignment-lane taxonomy: PRE0141 + the qualifier-source obligation as faults (QualifierMismatch identity) versus a named type-identity-integrity third category. Behavior consensus across all passes: keep both rejections and build the qualifier-source obligation; only the category label is open. (Copy A's keep was conditional on the rename — present accurately.)
- PRE0101 duplicate-key semantics: require-absence add (can't-compute -> DuplicateKey fault code) versus put/overwrite or recoverable-refusal (uniqueness rule -> governance). The current collapse to CollectionEmptyOnMutation is wrong under either reading.
- Mincount-at-birth Create semantics under the count reclassification: birth exemption, deferred first-mutation check, or defined every-Create-fails refusal + compile flag — the reclassification forces this decision; nobody has made it.
- Spec canon amendments (owner approval required, never bundled): §0.7's 'no result outside a declared bound' clause; Principle 11's 'constraint range impossibility' fault class; §0.6 item 6 to proven-violation-only; plus the §0.7 discharge-tier sentence if min-bound discharge vacuity is resolved by spec amendment rather than by making DeclarationValue satisfactions resolvable.
- Fault-delivery contract (Q1): result-types.md:51 'faults throw FaultException' versus evaluator.md §7.6 'faults are never thrown — EventOutcome.Faulted'. Must be settled before the evaluator lands either shape.
- Taxonomy adoption: the three-category model (fault floor / definition incoherence / flag layer) both derivations converged on, versus folding definition-incoherence Errors into the flag layer as pre-earned promotions (recon's frame — inconsistent with the promotion-oracle rule).
- omit x global-rule join adjudication: likely a presence-lane obligation-creation join (floor-shaped); needs an explicit disposition before the floor is declared enumerated. Surface explicitly that hook-guard PRE0082 coverage moves from the packet's Layer-1 list to the flag layer (no silent reclassification).
- Definition-evolution deploy lane (fleet stranding, restore-time skew): the most severe uncovered family; explicitly deferred to its own decision per the niche packet — confirm it stays out of floor scope.

## Threats to Validity

Calibration by claim class. HIGH: the registry adjudication (double-derived independently, red-teamed, critic-checked, grounded in 10+ live precept_compile probes and source reads of FaultCode.cs/StaticallyPreventableMap.cs/ProofEngine.*); the NumericOverflow inversion; the misclassification of OutOfRange/Length/Count; the position-incompleteness breach; the DesugarsToRule zero-consumers constraint; the sequencing verdict; the forwarding-fact suppression soundness (verified this session: GraphAnalyzer.BuildEdges ignores row.Guard). MEDIUM: the write-unaware discharge hole's RUNTIME consequence — the false-Proved ledger states are live-proved, but the mid-chain fault is an inference from the DESIGNED (stubbed) evaluator's working-copy semantics (evaluator.md steps 4->6), not observed execution; likewise the cross-event sign-modifier commit assumes the sweep ships without modifier desugar. MEDIUM-LOW: temporal overflow (asserted from .NET/NodaTime behavior, unprobed — verify-then-build); the omit x global-rule join's floor-shape (adjudication owed, not done); the carries-proof reinstatement scope (depends on the representability family shipping in its derived form — under the arbitrary-precision alternative the dependency vanishes, which is why the representation decision gates the family). Known frame risks: line-number cites from the prior passes may drift a few lines against working-tree edits; the friction estimate for representability is honest only under the decimal option; 'net friction neutral' would be dishonest — additions and subtractions land on different author populations; and recon item 17's 'both adjudications keep PRE0141' overstated copy A's conditional position (corrected in the open decisions). Nothing in this synthesis edits spec/philosophy — all canon changes are flagged as owner decisions per the non-negotiable gates.

## Sources

All Primary (internal): `src/Precept/Language/FaultCode.cs`, `StaticallyPreventableMap.cs`, `src/Precept/Pipeline/ProofEngine*.cs`, `GraphAnalyzer.cs`, `src/Precept/Runtime/Evaluator.cs`, `docs/runtime/{evaluator,fault-system,result-types,runtime-api}.md`, `docs/language/precept-language-spec.md` (§0.1/0.4/0.6/0.7/2.4/3A), `docs/compiler/proof-engine.md`, `docs/Working/bugs.md`, `docs/Working/compile-time-niche-decision-packet-2026-06-10.md`; 10+ live `precept_compile` probes (inputs recorded in the full-detail appendix).
