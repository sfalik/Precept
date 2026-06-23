---
status: Draft review — 2026-06-16
reviewer: Frank (Lead/Architect & Language Designer)
target: docs/Working/compiler-readiness-plan-2026-06-11.md
companion-research: research/architecture/compiler/fault-floor-definition-2026-06-11.md; docs/Working/compile-time-niche-decision-packet-2026-06-10.md
appendices-checked: compiler-readiness-plan-2026-06-11-appendices/{spec-coverage-audit.md, working-docs-triage.md}
---

# Architectural Review — Compiler Readiness Plan (2026-06-11)

> Reviewed against `docs/philosophy.md`, `docs/language/precept-language-spec.md` (§0.6/§0.7/Principle 11), the live registry (`FaultCode.cs`, `StaticallyPreventableMap.cs`), the proof engine (`ProofEngine*.cs`, `ProofLedger.cs`), `GraphAnalyzer.cs`, `fault-system.md`, and the companion research. Every load-bearing factual claim in the plan was spot-checked against source — see the closing section for the verified/took-on-faith ledger.

---

## Verdict

**Sound with required revisions.**

This is the best strategic artifact this project has produced. The technical thesis — an 11-family, position-total, prove-or-reject floor with a static type-checker edge, right-sized by a *carries-proof* boundary rather than a *band-vs-not* boundary, delivered as one atomic coordinated move ahead of any spec widening — is correct, and it is grounded in source reality to a degree I rarely see. I verified the linchpin claims myself; they hold. The sequencing red-team is genuine, not theater. The philosophy handling is disciplined.

It does not get a clean "proceed to sign-off" because three things sit in load-bearing positions and are either internally inconsistent, literally false-as-stated, or under-enumerated for the owner gate they feed. None of them invalidate the strategy. All three are cheap to fix. But this document is the contract that gates **all** runtime work, and a contract that gates everything is held to exactness — a plan that rests its entire atomic-move safety argument on a "ZERO consumers" claim does not get to be imprecise about that claim, and an owner-gated canon-amendment set does not get to be partial. Clear B1–B3 and this is a sign-off-ready plan.

---

## Executive summary — what the plan gets right

I will say this plainly because it is true and earned:

- **The floor thesis is right.** Separating *prove-or-reject floor* / *definition incoherence* / *flag layer* into three categories (not two) is the honest taxonomy, and the research converged on it from two independent derivations (plan line 37, research line 132/193). The "carries-proof boundary, not band-vs-not" reframe (line 35) is the single most important idea in the document, and it is **philosophically load-bearing and correct**: what exits the floor is what discharges no proof; what stays is what carries proof (mincount's `count>0`, sign/rule preservation, representability-feeding bounds). That is a principled line, not a convenience.

- **The prerequisite ordering is real and the atomicity argument is sound.** Promoting `DesugarsToRule`→governance/ingress wiring from an ordering nicety to a hard *floor prerequisite* (line 34), and refusing to retire any compile band gate before the wiring lands (Slice 2 before Slice 3, line 165/177/269), is exactly right. Without it you get a window where declared bounds are enforced **nowhere** — that would break prevention outright. The plan sees this and structurally forbids it.

- **The registry-whole-in-one-pass decision is correct over the split alternative.** The red-team's preference for Draft A's whole-registry move over Draft B's split (line 267, 273) is well-argued: a position-totality checker (Phase 2) that asserts against a half-repartitioned registry is a soundness-of-the-checker hole. Landing the registry whole in Phase 1 so Phase 2 reads honest codes is the right call.

- **The factual base is verified, not asserted.** I checked the registry state, the `_ => DivisionByZero` backstop, the `ProofStrategy.Literal // vacuously proved` mislabel, the `StaticallyPreventableMap` exclusion of presence, the 13-vs-15 `fault-system.md` drift, and the vacuous-suppression soundness argument against `GraphAnalyzer.BuildEdges`. **Every one is accurate.** The plan's HIGH-confidence calibration (line 289) is justified.

- **Philosophy is handled like an adult handled it.** `philosophy.md` is treated as owner-gated, never auto-edited, surfaced through GATE-F with a commitment to verbatim prior-decision quotes (line 83, 195, 293). That is exactly the CLAUDE.md contract. The plan does not silently rewrite the guarantee.

- **The coverage ledger arithmetic is sound.** 619 − 383 fully = 236 non-fully rows = 85 + 20 + 53 + 78 = 67 + 126 + 43 across Phases 4/5/6 (line 259). The 43-mined-obligation breakdown sums to 43 (line 261). I checked the appendix; the counts match.

Credit where due: this is a document that did its homework. Now the corrections.

---

## Blocking findings (required revisions before owner sign-off)

### B1 — GATE-F under-enumerates the owner-gated canon-amendment set; the coordinated move can ship leaving canon self-contradictory

**Defect.** GATE-F (lines 79–83) and Slice 5 (lines 193–197) enumerate the spec amendments the owner must approve: "§0.7's 'no result outside a declared bound' clause; Principle 11's 'constraint range impossibility' fault class; §0.6 item 6 → proven-violation-only; the §0.7 discharge-tier sentence." That enumeration is incomplete in two verified ways:

1. **§0.7 asserts band prove-or-reject in more than one sentence.** It is not a single clause. I read the spec: line 266 is the headline ("no result outside a declared bound"), but line 256 (relational/divisor reasoning, "never compiles a false 'safe'") and line 258 (count cardinality: *"count is 'no result outside a declared bound', discharged by an author guard, never deferred to a runtime check"*) **both** assert the same prove-or-reject contract for the bands being reclassified. The count sentence at line 258 is the most dangerous — it is woven into the cardinality narrative and will read as still-true after the move unless explicitly amended.

2. **Principle 11 carries a second load-bearing clause GATE-F does not name.** Spec line 112 says, in the same breath as "constraint range impossibility," that *"Runtime fault checks exist only as defensive redundancy, never as the primary enforcement mechanism."* The plan's whole move wires governance/ingress as the enforcement lane for non-proof-carrying bands. Whether that contradicts Principle 11 depends on a distinction the amendment set must make explicit (governance refusal is *not* a "fault check," so the literal clause may survive — but the reader must be told that). This is the same defensive-redundancy scoping gap I flagged previously against the ingress path; it is now live and the owner must see it.

**Why it blocks.** GATE-F is owner-gated. The owner deliberates the *enumerated* set. Slice 5's catch-all ("verify the whole move is internally consistent," line 195) is a backstop, not a substitute — an owner cannot approve an amendment that was never surfaced, and the plan's own discipline (line 83: "Surface each amendment explicitly with verbatim prior-decision quotes") requires the enumeration to be complete *before* the conversation. An incomplete set risks the owner approving the headline §0.7 clause while §0.7 lines 256/258 and the Principle-11 second clause survive — shipping a reclassification whose canon still asserts the opposite. That is precisely the failure mode the atomic capstone exists to prevent.

**What clears it.** Expand GATE-F's amendment enumeration to name **every** spec sentence that asserts band prove-or-reject for the reclassified families — at minimum §0.7 lines 256, 258, 266 and the §0.6 item-6 sentence (line 208) — and add an explicit sub-item addressing Principle 11's "defensive redundancy" clause: state whether it survives (because bands exit to governance, not to fault checks) or needs the ingress-scoping amendment, and quote it verbatim for the owner. The enumeration is the deliverable; the owner's deliberation is downstream of it.

### B2 — The linchpin "DesugarsToRule has ZERO consumers today" is literally false; the load-bearing version is true but must be stated as such

**Defect.** The "Decisions captured" block (line 34) states as a locked decision: *"it has ZERO consumers today, so retiring compile band gates before it leaves bands enforced nowhere."* This is the premise the entire coordinated-move atomicity rests on. It is literally false. `DesugarsToRule` has **two** consumers today, both of which I located:

- `tools/Precept.GrammarGen/Program.cs:173` — `.Where(m => m.DesugarsToRule && m.Token.Text is not null)`, used to pick which modifier tokens get a dedicated TextMate color lane. Cosmetic.
- `tools/Precept.Mcp/CatalogFormatters.cs:253/270/273/276/279` — renders `DesugarsToRule` into MCP modifier markdown. Display-only.

**The load-bearing claim is nonetheless true,** and Slice 2 already states it precisely (line 177: *"bounds/modifiers are never synthesized into TypedRules"*). I confirmed it: there are **zero** reads of `.DesugarsToRule` anywhere in `src/Precept/` — no pipeline stage (type checker, proof engine, evaluator, constraint plan, ingress) consumes it to synthesize or enforce a constraint. So the soundness argument holds. Only the *wording* of the captured decision is wrong.

**Why it blocks.** This is the prerequisite that licenses the entire atomic move, recorded as a *captured decision* — the highest-status statement in the document. An executor or reviewer who reads "ZERO consumers," greps, and finds the grammar-gen and MCP reads is entitled to conclude the premise is false and the move unsafe. On a claim this load-bearing, "approximately zero" is not good enough. Frank does not let a foundational decision state a falsehood, even a benign one.

**What clears it.** Reword line 34 (and any echo in the risks section, line 269 "the verified zero-consumer DesugarsToRule grep") to the precise, verified claim: *"zero constraint-synthesis / enforcement consumers — `DesugarsToRule` is read only by the grammar generator (cosmetic) and the MCP formatter (display); no pipeline stage synthesizes it into a `TypedRule`/`TypedEnsure`, so bounds/modifiers are enforced nowhere at the governance/ingress lane today."* That is what Slice 2 already says; make the captured decision match it.

### B3 — Internal inconsistency in the "nothing is lost" provenance: "28 superseded docs" vs "all 32 prior docs"

**Defect.** The Context section (line 15) states the 43 mined obligations were *"recovered from the 28 superseded docs so nothing is lost."* The appendix pointer (line 306) describes the triage as *"the disposition of all 32 prior docs and the 43-item obligation register."* These disagree, and I resolved which is right by counting the triage: it carries **28 `[SUPERSEDE]` + 4 `[ARCHIVE]` = 32** tagged doc rows. So 32 is the triaged total; 28 is the superseded subset. The 4 archived docs are not obligation-free — the triage explicitly says so (e.g., the archived `proof-engine-contract-grounding` entry: *"The runtime-governance obligation it carries is recorded in spec §0.7 and transfers via the obligation register"*).

**Why it blocks.** The "nothing is lost" guarantee is a completeness claim, and a completeness claim that misstates its own source set undercuts itself. As written, line 15 says obligations came from 28 docs while 4 more docs (archived) also contributed obligations into the same 43-register. A reader auditing whether the register is complete will hit the 28/32 contradiction and lose confidence in the ledger — which is the one thing the ledger exists to provide.

**What clears it.** Reconcile to the verified provenance: *"43 mined obligations recovered from the triage of 32 prior docs (28 superseded + 4 archived) so nothing is lost."* Confirm in passing that the 4 archived docs' transferred obligations are present in the 43-register (the triage implies they are; state it).

---

## Advisory findings (improvements; non-blocking)

### A1 — Front-matter `status: Active` is off-taxonomy and overclaims settledness

`status: Active` (line 2) is not a valid `docs/Working/` status. Per CLAUDE.md, working-doc statuses are `Draft`, `Draft <kind> — YYYY-MM-DD`, `Locked YYYY-MM-DD`, `Promoted to: <link>`. `Active` is a *canonical*-doc status. Worse, it overclaims: this is a plan whose entire Phase 0 is nine unresolved owner gates — it is not settled. Recommend `status: Draft plan — 2026-06-11`, promoted to `Locked 2026-06-11` only once the owner clears the Phase-0 gate set. (Compare the niche packet's honest `status: Decision packet — evidence for owner decision, NOT a decision`.)

### A2 — Registry-correction asymmetry: OutOfRange is not in the bijective core, but the plan treats all three symmetrically

Slice 4 (line 189) and the ledger (line 257) say "REMOVE OutOfRange/Length/Count from `[StaticallyPreventable]` + bijective core." Verified nuance: `StaticallyPreventableMap.BijectiveDiagnosticCodes` (L28–37) contains `LengthBoundViolation` and `CountBoundViolation` but **not** `OutOfRange` — `FaultCode.OutOfRange` carries the attribute but its `DiagnosticCode` partner was never added to the bijective set. So "remove from bijective core" is a no-op for OutOfRange and real only for Length/Count. The research inherits the same imprecision (research line 210). Not a defect in intent — just note in Slice 4 that the three are not symmetric in the live registry so the executor does not go hunting for an OutOfRange bijective row that isn't there.

### A3 — Catalog-driven guardrail for Slice 4: route via the bijective map, do not proliferate switch arms

This is my Catalog-Driven Enforcement lane, so I will be explicit. Slice 4's "RETIRE the generic `_ => DivisionByZero` backstop → per-family honest routing" (line 189) is the **right** direction *if* implemented by moving routing into the attribute-derived `StaticallyPreventableMap` bijective core (add the new `DiagnosticCode`s as bijective rows; the plan's own "the UnexpectedNull bijective row" language at line 38 shows it understands this). It is the **wrong** direction if it adds hand-maintained arms to the `CreateFaultSiteLink` collapse switch (`ProofEngine.Diagnostics.cs:482–491`) — that would trade one catalog smell (generic backstop) for another (per-member switch-on-identity). State the mechanism in the plan: the honest routing is delivered by extending the bijective map + `[StaticallyPreventable]` attributes, and the residual hand-switch shrinks rather than grows. (Direction is already catalog-correct; I want it pinned so execution can't drift.)

### A4 — The replaced-plan reference gives no path

The blockquote (line 11) and the supersession story reference `compiler-readiness-plan-2026-05-24.md` with no path. It exists — at `docs/Working/Superseded/compiler-readiness-plan-2026-05-24.md` (I confirmed the supersession header points back here, so the link is bidirectional from the old side). Add the `Superseded/` path so the forward reference resolves and a reader in `docs/Working/` is not left thinking the file is missing. Not a dangling reference; just an unqualified one.

### A5 — Two different "43"s invite confusion

"43 mined obligations" (line 15/261) and Phase 6's "43 spec gaps" (line 259) are distinct registers that happen to share a number. One line of disambiguation in the coverage ledger ("note: the 43 Phase-6 spec gaps are unrelated to the 43-item mined-obligation register") prevents a reader from assuming a relationship.

### A6 — Make the philosophy-preservation argument explicit, not implicit

The plan handles `philosophy.md` correctly (owner-gated, GATE-F), but it leaves the *strongest* philosophical point implicit. State it: **"prevention, not detection" is preserved, not weakened.** Two facts support this and both are verified: (1) `philosophy.md` line 53's concrete "compile-time impossibilities" examples are *division by zero, arithmetic overflow, empty collection access* — every one is a **retained** floor family; bands are not in that list. (2) For non-proof-carrying / ingress-sourced values, compile-time never had the value to prevent on; runtime governance refusal is still prevention (atomic, never persists). So the move relocates *where* prevention happens for those values (compile → runtime governance), it does not remove prevention. Saying this out loud turns GATE-F from "we are touching the absolute claims" (which sounds like erosion) into "we are making the compile/runtime split honest" (which is what is actually happening). It will make the owner conversation shorter and correct a likely misread.

### A7 — Name Phase 3 as the highest-hidden-risk stub

Among the stubs, Phase 3 (write-aware tier-2 discharge + the representability-overflow family at every arithmetic sink, lines 209–215) carries the most concealed complexity behind its `L (10–15)` estimate: write-awareness shape (GATE-B) and representability shape (GATE-A) are both unresolved, and the representability family touches every arithmetic sink plus UCUM factor application plus checked constant-fold. The plan's relief valve is real and worth stating in the stub: GATE-A's arbitrary-precision branch *deletes* the representability family (line 291), collapsing Phase 3 substantially. Flag Phase 3 as the estimate most sensitive to a Phase-0 decision, so a slip there is read as expected variance, not plan failure.

---

## Per-dimension assessment

### 1. Strategic soundness & scope-gate validity — SOUND

"Blocks runtime; complete when the compiler is at 100% of canonical spec on a sound, right-sized floor" is the correct boundary, and the two-decision reset (compile-time niche → prove-or-reject floor; fault-floor research → registry adjudication) is applied consistently from the Context (line 13–15) through the Definition of Done (line 285). The niche layer (Layer 2/3) is correctly held post-runtime with a non-trivial justification — the runtime is the error-promotion oracle, and the live PRE0155 false positive proves premature promotion rejects correct precepts (line 39, corroborated in the research). The definition-evolution deploy lane is correctly excluded as its own decision (GATE-N). Nothing in scope belongs deferred; nothing deferred is something the *runtime build itself* needs (the runtime needs the floor sound/complete/right-sized and the API final — all in scope; it does not need the niche layer — correctly out). The one thing I'd sharpen: the scope-gate front-matter (line 6) and Definition of Done are strong, but "100% of canonical spec" leans entirely on the 619-row audit's notion of canonical — which is fine, but the gate should name the audit as the definition-of-canonical of record so "100%" is measurable against a fixed denominator, not a moving one.

### 2. Philosophy alignment — SOUND (this was the highest-stakes check; it passes, with B1/A6 attached)

This is where I pushed hardest. The reshape moves the band-containment lane to governance + flag-sound warnings, and that *sounds* like it weakens "invalid configurations are structurally impossible." It does not — provided the wiring lands first, which the plan guarantees. My reasoning, verified:

- The retained floor families are exactly `philosophy.md` line 53's enumerated "compile-time impossibilities" (divisor, overflow, empty-collection). Bands are not enumerated there. So the philosophy's concrete compile-time promises are **untouched** by the move.
- The broad absolutes — line 49 "No errors. No bugs.", line 43 "invalid configurations are structurally impossible" — are *system* guarantees (compile + runtime together), which is already the documented position. Moving bands from compile-proof to runtime-governance keeps the system guarantee; governance refusal is atomic prevention, not post-hoc detection.
- The "carries-proof boundary, not band-vs-not" line is philosophically defensible and I verified it against research line 132: the exit set discharges no proof, the retained set carries proof. Principled.

The plan correctly treats `philosophy.md` as owner-gated and never auto-edits it (lines 83, 195, 293). Every place it changes the guarantee contract (§0.7, §0.6, Principle 11, and the philosophy absolutes) is surfaced as an owner gap, not silently resolved. The **one** failure is enumeration completeness (B1): §0.7 is multi-site and Principle 11 has a second clause. Fix B1 and add A6's explicit preservation argument, and the philosophy treatment is exemplary. The guarantee survives the right-sizing.

### 3. Catalog-driven architecture compliance — SOUND

Strong, and in the right direction throughout. The `ChildExpressionPositions` projection (Phase 2, line 204) is textbook catalog-before-code: position-totality becomes a structural property derived from a catalog projection over the 5 expression-bearing DUs, checked by generic machinery, rather than spot fixes — exactly the inversion the architecture demands. I confirmed the 5 DUs exist (`SemanticIndex.cs`) and `TypedExpression` has exactly the 15 subtypes the plan claims. `ProofStrategy.Vacuous` (line 38) is a clean metadata addition that makes the ledger honest (it stops the `Literal // vacuously proved` lie at `ProofEngine.cs:1181/1194`); it is a new disposition label, not a switch-on-identity. The registry repartition is expressed as `[StaticallyPreventable]` attribute + bijective-map changes (line 38's "bijective row" framing), not as per-member enum dispatch — compliant. My only guardrail is A3: keep Slice 4's "honest routing" inside the bijective map, don't grow the collapse switch.

### 4. Fault-floor technical soundness — SOUND

The 11-family floor + static type-checker edge is faithfully transcribed from research line 124 (I diffed them). The three categories (prove-or-reject / definition-incoherence / flag) are cleanly separated and each has a distinct *behavioral* signature: floor = prove-or-reject; definition-incoherence = always-Error, every Create fails (PRE0079-on-default/PRE0164/PRE0115); flag = warning-default with earned promotion. The load-bearing factual claims all verified:

- **DesugarsToRule zero enforcement consumers** — TRUE in the load-bearing sense (B2 is wording only).
- **Registry state** — `FaultCode.cs` has 15 codes; OutOfRange(13)/Length(14)/Count(15) all `[StaticallyPreventable]`; the generic `_ => FaultCode.DivisionByZero` backstop is live at `ProofEngine.Diagnostics.cs:490`; the shape-keyed sqrt-vs-divzero misroute is at L390–392 exactly as described; `StaticallyPreventableMap` excludes `UnprovedPresenceRequirement` so a presence trap reports a divisor message — all confirmed.
- **Forwarding-fact vacuous-suppression soundness** — confirmed. `GraphAnalyzer.TryAddEdge` (L379–384) adds an edge for every transition row unconditionally; `row.Guard` is recorded only as `HasGuard` metadata, never gating edge creation. Reachability therefore over-approximates the truly-reachable set, so "unreachable" is unreachable under all guard valuations — structural. Suppressing obligations on transitions *from* structurally-unreachable states is sound, and `ProofStrategy.Vacuous` records it honestly. The plan's argument is correct.

The floor is correctly bounded: representability-overflow is the genuinely missing family (gated on GATE-A), temporal-overflow is honestly marked probe-first/unverified (MEDIUM-LOW confidence, line 289), and the type-checker edge (TypeMismatch/UndeclaredField/InvalidMemberAccess/FunctionArityMismatch) is correctly classified as defense-in-depth traps owing no proof obligation. No overreach, no under-claim.

### 5. Sequencing & dependency correctness — SOUND

The red-team (lines 265–281) is the real thing. I stress-tested the atomicity guarantee and it holds: Slice 2 (wiring) strictly precedes Slice 3 (band retirement), so there is never a window where declared bounds are enforced nowhere — and the premise that makes this *necessary* (no enforcement consumer today) is verified. The whole-registry-in-Phase-1 decision genuinely fixes Draft B's split-registry checker hole (the Phase-2 checker must read honest codes). Cross-phase gates are correctly hoisted to Phase 0 and re-cited at the phase they block (line 275). Phase 0 is correctly front-loaded as its own phase rather than folded into a heavyweight slice — the Tier-3 spec conflicts need owner deliberation that would otherwise stall the merge. I did not find a missed cross-phase dependency. The one structural observation: Phase 6 consuming GATE-M, which Phase 5's 2c-ii resolves as a side effect (line 279, 124), is a soft dependency carried correctly, but it is the place a Phase-5 slip propagates furthest — worth a watch, not a change.

### 6. Per-decision rationale completeness — QUALIFIED

The 14 GATEs mostly carry Options + Gates + Recommendation, and several carry genuine four-leg reasoning (GATE-A's friction statement, GATE-H's earned-promotion-oracle argument, GATE-N's "blurs the teachable line"). Where a GATE carries "no recommendation per neutral-framing discipline" (GATE-A line 53, GATE-B line 59, GATE-D, GATE-E, GATE-G), that is **appropriate, not an evasion** — these are genuine owner calls on runtime-value representation and authoring-ergonomics tradeoffs where a carried recommendation would prejudice a decision the owner must own. Neutral framing here is the *correct* discipline, and the plan still supplies the decision substance (options, consequences, what each gates) so the owner is not deciding blind. That is the right shape. **However**, the "Decisions captured" block (lines 32–43) is where my four-leg standard bites: these are stated as *captured* (settled) decisions, but several state WHAT without WHY — e.g., the standing declines (line 42, convex polyhedra/predicate abstraction off-path) cite R1/R2 determinism, which is good; but the three-soundness-disciplines decision (line 33) and the definition-evolution exclusion (line 40) assert the conclusion without the rejected alternative and the tradeoff accepted inline. For *captured* decisions feeding an execution contract, attach the rejected-alternative and tradeoff legs (they exist in the research; cite them). The GATEs are owner-facing and fine; the captured block should meet the bar it implicitly claims.

### 7. Coverage-ledger integrity — SOUND (one inconsistency = B3)

The arithmetic is clean and I checked it against the appendix, not just internally: 619 rows, 383 fully, 236 non-fully = 85+20+53+78, distributed 67/126/43 across Phases 4/5/6 (line 259). The 16 floor gaps each carry a phase placement (line 253); the 8 scale-backs and 10 registry corrections are placed whole in Phase 1 (lines 255/257); the 43 mined obligations carry per-bucket placements summing to 43 (line 261); the explicit deferrals each carry a reason (line 263). No orphans, no double-counts that I found, no item hand-waved into a stub without a reason. The single integrity defect is the 28-vs-32 provenance inconsistency (B3) — a counting error in the "nothing is lost" claim, not a placement error. The "43 vs 43" collision (A5) is cosmetic.

### 8. Stub-phase specification adequacy — ADEQUATE for a phased plan, with Phase 3 flagged

The plan's own protocol (line 310, "Phases beyond the next stay stubs until their predecessor lands") is the correct discipline — Phase 0 (current) and Phase 1 (next) are heavyweight and well-specified down to slice exit-criteria and doc-touch; Phases 2–7 are stubs with effort *classes* (not false-precision estimates) plus scope bullets and gates. That is right; over-specifying Phase 6 now would be waste. The estimates are trustworthy *as classes* because each stub names its gates and its principal work items. The hidden risk concentrates in Phase 3 (A7): its `L` is the most decision-sensitive, swinging hard on GATE-A/GATE-B. Phase 5 is the largest raw surface (126 spec gaps + 2c-ii, the largest unbuilt locked design) but it is *breadth* risk (parallelizable, well-understood) rather than *depth* risk. Phase 3 is depth risk. Name it.

### 9. Doc-sync & obligation discipline — SOUND

The per-slice doc-touch enumerations are correct against the CLAUDE.md routing table: proof-engine changes → `proof-engine.md` (Slice 3, line 185); registry/diagnostic routing → `diagnostic-system.md` + `result-types.md` + `fault-system.md` (Slice 4, line 191); spec amendments → `precept-language-spec.md` §0.6/§0.7/Principle 11 + `proof-engine.md` (Slice 5, line 197); runtime wiring → `evaluator.md` + `compiler-and-runtime-design.md` (Slice 2, line 179). The `fault-system.md` 13-vs-15 drift and the stale `NullInNonNullableContext` partner are both real (I confirmed L190 "all 13 members" and L125's stale attribute) and correctly slated for Slice 4. The `philosophy.md` handling is correct everywhere it appears: owner-gated, never auto-edited, rides the separate guarantee-statement promotion (lines 197, 261, 293). B1 is the one doc-sync completeness gap — the §0.7 multi-site and Principle-11 second-clause edits are owed and under-enumerated.

### 10. Concrete defects — the DO-NOT-RESURRECT directive is consistently applied; minor reference defects

The DO-NOT-RESURRECT directive (line 297) is sound and consistently applied: I confirmed `ProofChecker` is **gone** from `src/` (no class, no references), so the two later items that cite the deleted verify-don't-trust architecture (conditional-totality proof-engine half in Phase 5; witness richness in Phase 6) are correctly flagged for re-grounding rather than execution from stale citations, and the witness/inspectability distinction (Principle 4, not witness-checking) is correctly drawn. Remaining concrete defects are the ones enumerated above: B2 (zero-consumers literal-false), B3 (28-vs-32), A1 (status), A2 (OutOfRange asymmetry), A4 (unqualified prior-plan path). No internal contradiction beyond B3; the plan's caveats (line 289 calibration) are honest and *strengthen* rather than undercut confidence — a plan that labels its own temporal-overflow claim MEDIUM-LOW/UNPROBED is calibrating correctly, not hedging.

---

## Per-phase assessment

**Phase 0 — Decision & Probe Gate — SOUND.** Correctly front-loaded; correctly scoped to conversation + probes + reconciliation with no canon edits (spec amendments approved here, applied in Phase 1). Slice 1's verify-then-build probes (temporal overflow, omit-join classification, PRE0101 collapse) are the right discipline — confirm the gap before building the obligation. The probe for temporal overflow is especially right given the MEDIUM-LOW confidence. Only change: Slice 2's gate walk (line 151) must carry B1's completed GATE-F enumeration, or the owner conversation surfaces a partial amendment set.

**Phase 1 — The Coordinated Floor Move — SOUND; highest-risk, correctly identified as such.** The atomicity (wiring → band scale-back → whole registry → spec amendments behind one merge) is the correct structure and the Slice 2-before-Slice 3 ordering is non-negotiable and honored. This is the phase that earns the plan its verdict. Attach B1 (Slice 5 amendment completeness), B2 (the captured-decision wording the phase executes against), A2/A3 (Slice 4 registry precision + catalog-routing guardrail). With those, this phase is executable as written.

**Phase 2 — Position-Totality + Creation-Exhaustiveness Checker — SOUND (stub).** The catalog `ChildExpressionPositions` projection is the right mechanism and the dependency on Phase 1's honest registry is correctly stated. The bundled intra-guard short-circuit narrowing (so the guard-slot closure doesn't mass-false-reject `when X is set and X > Y`) is a genuinely important coupling the plan catches (line 204, risk at line 295). Adequate for a stub.

**Phase 3 — False-Proved Repairs + Missing Floor Obligations — SOUND (stub); highest hidden risk (A7).** Closes the false-Proved holes and adds representability/temporal/qualifier-source families. Most decision-sensitive phase; flag the GATE-A collapse explicitly so a swing reads as expected.

**Phase 4 — Spec Completeness: datatypes/literals — SOUND (stub).** The no-floor-entanglement surface, correctly sequenced after the floor holds. GATE-J (sqrt §3.2-vs-§3.7 + choice field-vs-field) and GATE-K (log-unit) are real internal-canon contradictions, correctly routed to owner reading / small `/design`. GATE-L (absolute-measurement positions) correctly deferred as new surface.

**Phase 5 — Spec Completeness: collections/modifiers/expression/rules/states — SOUND (stub); largest surface.** 126 gaps + 2c-ii (the largest unbuilt locked design, now unblocked). Breadth risk, parallelizable. GATE-M's resolution-as-side-effect-of-2c-ii is the soft dependency feeding Phase 6 — the one cross-phase thread to watch.

**Phase 6 — Diagnostic + Pipeline-Tail + Emission Ownership — SOUND (stub).** Last in breadth because it consumes GATE-M. The merge-the-three-lenses (this phase + conformance [Covers] + Stage-2 audit = ONE register, line 237) is the right consolidation. The witness-richness item correctly re-grounded per the DO-NOT-RESURRECT directive.

**Phase 7 — Conformance, API Solidity + Runtime-Ready Gate — SOUND (stub).** Correctly ends at "ready to begin runtime," enumerates (does not build) the runtime backlog, settles GATE-I (fault-delivery) and confirms GATE-N (definition-evolution out). The API-solidity-as-runtime-precursor framing (F-API-01 typed field descriptors) is the right gate. Owner sign-off correctly terminal.

---

## What I verified vs. what I took on faith

**Verified against source (all confirmed accurate unless noted):**

| Claim | Source checked | Result |
|---|---|---|
| `DesugarsToRule` has zero *enforcement* consumers | `grep .DesugarsToRule` across `src/`,`tools/`,`test/` | TRUE in load-bearing sense; 2 metadata consumers exist (B2) |
| 15 FaultCodes; OutOfRange/Length/Count `[StaticallyPreventable]` | `FaultCode.cs` L11–54 | Confirmed |
| OutOfRange NOT in bijective core (Length/Count are) | `StaticallyPreventableMap.cs` L28–37 | Confirmed (A2) |
| Generic `_ => DivisionByZero` backstop | `ProofEngine.Diagnostics.cs` L490 (+386,392) | Confirmed |
| Shape-keyed sqrt-vs-divzero misroute | `ProofEngine.Diagnostics.cs` L390–392 | Confirmed |
| `StaticallyPreventableMap` excludes presence → divisor message | `StaticallyPreventableMap.cs` L20–37 + L490 backstop | Confirmed |
| `ProofStrategy.Vacuous` does not yet exist; Literal-mislabel live | `ProofLedger.cs` L100–115; `ProofEngine.cs` L1181/1194 | Confirmed |
| Vacuous-suppression soundness (BuildEdges ignores guard) | `GraphAnalyzer.cs` L326–384 | Confirmed — argument holds |
| 5 expression-bearing DUs; TypedExpression[15] | `SemanticIndex.cs` | Confirmed |
| `fault-system.md` 13-vs-15 drift + stale NullInNonNullableContext | `fault-system.md` L125, L190 vs `FaultCode.cs` | Confirmed (drift real) |
| philosophy.md "No errors. No bugs." / "compile-time impossibilities" | `philosophy.md` L49, L53 | Confirmed; L53 examples are retained families |
| §0.7 / §0.6 item 6 / Principle 11 text | spec L266/256/258, L208, L112 | Confirmed; §0.7 multi-site; Principle 11 second clause (B1) |
| 11 obligation families faithfully transcribed | research L124 | Confirmed |
| Coverage arithmetic 619→236→67/126/43; 43-mined sums | `spec-coverage-audit.md` L12–16; plan L259/261 | Confirmed |
| 28 SUPERSEDE + 4 ARCHIVE = 32 triaged | `working-docs-triage.md` tag count | Confirmed (B3: plan says 28 in one place, 32 in another) |
| Prior plan location | `docs/Working/Superseded/compiler-readiness-plan-2026-05-24.md` | Exists; bidirectional supersession header present (A4: path unqualified in plan) |
| ProofChecker deleted (DO-NOT-RESURRECT target) | `grep ProofChecker src/` | Confirmed gone |
| Niche packet verdict / framing | niche packet L11 | Confirmed: "partial — three-layer under flag-soundness" |

**Taken on faith (not independently verified; confidence noted):**

- The **236-row audit's per-row canon citations** — I verified the row-count totals and spot-read ~8 rows; I did not re-adjudicate all 236 gap classifications. Confidence: MEDIUM-HIGH (the totals reconcile and the sampled rows were accurate).
- The **temporal-overflow gap** — correctly self-marked UNPROBED/MEDIUM-LOW by the plan (line 289). I did not run the NodaTime probe; the plan defers it to Phase 0 Slice 1, which is correct.
- The **effort-class estimates** (S/M/L day ranges) for Phases 2–7 — accepted as classes, not validated as day counts. Appropriate for stubs.
- The **omit × global-rule join floor/flag classification** — the plan itself marks this as adjudication-owed in Phase 0 (line 289); I did not pre-empt it.
- The **research's full red-team/critic convergence** (line 41, "two adjudications + red-team + critic agree on 5 true / 4 TC-owned / 3 rule-misclassified / 2 mixed") — I verified the registry facts these rest on, not the full adjudication transcripts in `fault-floor-research-full.md`.

---

## Closing

Clear B1, B2, B3. They are a half-day of work between them and they are the difference between a plan that *is* exact about its load-bearing claims and one that merely *appears* to be. Fold in A1–A7 as you go. Then this plan has my architectural approval to go to Shane for the gate — and it earns it. The thesis is right, the sequencing is right, the philosophy survives, and the homework is real. That is not faint praise from me.

The owner sign-off on the Phase-0 gate set remains required and is not mine to give — my approval clears the architecture, not the guarantee-contract amendments, which are Shane's. Surface GATE-F complete (B1) and the conversation will be clean.

— Frank
