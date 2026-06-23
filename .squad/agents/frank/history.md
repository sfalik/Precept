## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for Precept.
- Catalog metadata stays the language truth; tooling and runtime should derive from it instead of enum-identity switches or parallel keyword lists.
- Constructor-semantics work stays complete only when docs, diagnostics, samples, and downstream tooling match shipped behavior.

## Live Guidance

- Quantity normalization has two durable lanes: compile-time normalization for declarations/literals and runtime normalization for ingress values.
- `TypedField` is the normalization handshake between analysis and execution: authored bounds stay available for display, normalized bounds feed proof/comparison surfaces.
- Comparison/equality checking must stay strict about explicit counting-unit identity.
- When the grammar can make an invalid form impossible, do that instead of adding a later semantic ban.
- Documentation updates for shipped features must be verified against source and validation runs.

## Durable Learnings

- Any claim that work happens only at compile time must be stress-tested against Fire/Update/Restore ingress paths.
- Construction row syntax is declaration-driven: `on <Event>` is the honest construction surface.
- Graph analysis for construction must stay semantic, not topological.
- Hollow-entity validation should be shared across all pre-materialization expression lanes.
- Constructor semantics and governed draft-state construction are complementary idioms.
- Runtime docs must distinguish designed behavior from shipped behavior.
- Exact vs approximate behavior should stay visible in the type system and public surface.
- Quantity and qualifier checks need separate compile-time and runtime assumptions.

## Historical Summary

- 2026-05-12 through 2026-05-16 concentrated work around hover contract reviews, field-state guarantees, constructor semantics, reject-surface structure, interval-proof design, quantity normalization, diagnostic enforcement, and counting-unit comparison gaps.
- The constructor/reject track settled on `on <Event>`, valid fallback `reject`, and grammar-level structural exclusion whenever the language already knows a path is impossible.
- Detailed batch chronology now lives in `.squad/decisions.md`; this file keeps only durable guidance.

## Recent Updates

- 2026-05-17 — Constructor semantics fully closed across syntax, checker, proof, tooling, docs, and samples.
- 2026-06-16 — Compiler readiness plan review verdict: Sound with required revisions; B1-GATE-F under-enumeration, B2 `DesugarsToRule` precision fix, and B3 28-vs-32 provenance mismatch.
- 2026-06-16 — Coordinator bumped Frank and Elaine model overrides to `claude-opus-4.8`.

- 2026-06-21 — Scribe merged the amendment-verification inbox note into `.squad/decisions.md`, deleted the inbox file, and recorded the batch logs. No archive gate or history summarization was needed; the plan remains sign-off-ready pending Shane.

## Learnings

- 2026-06-21 — Re-reviewed `docs/Working/compiler-readiness-plan-2026-06-16.md` (compiler-only re-scope after the owner caught a runtime-enforcement boundary leak in 06-11). Review: `docs/Working/compiler-readiness-plan-2026-06-16-frank-review.md`. Verdict: **Revisions required — ONE blocking finding (B1), otherwise sign-off-ready.** Does NOT yet clear for owner Phase-0 sign-off until B1 lands (half-hour fix).
  - **Boundary integrity HOLDS — verified, not trusted.** Hunted every retained-slice exit for a runtime-execution assertion; found none. Every in-scope exit is `precept_compile`/`dotnet build`/`dotnet test`/`Compiler.Compile`/doc-capture. Confirmed the runtime is a pure stub against `Evaluator.cs:80/99/121/132/155` (all five `NotImplementedException`). §11 gives every `RUNTIME_CODE_DEFER` a real `docs/runtime/*.md` capture pointer (principal target `evaluator.md §7.6` live at `:1326`). Matched-pair rule sound incl. D8 dynamic-write (stays BLOCKED until `evaluator.md §Intake-Boundary` lands; only static-fold subset admitted). Stress 4 (matched-pair leak hunt) is a genuine boundary probe I'd have written myself.
  - **D1 overflow park = HONEST relaxation, not a fig leaf.** `FaultCode.TemporalOverflow` ABSENT (verified `FaultCode.cs`); overflow never claimed prevented (DoD-4); `philosophy.md` L49/51/53/57/61 surfaced owner-gated (DoD-9, verified verbatim — L53 literally lists overflow as a compile-time impossibility, so the gap is real). It genuinely erodes the absolute claim for overflow but stops *claiming* the guarantee it no longer delivers — that's honesty-about-approximation done right.
  - **D2 band split = genuine relocation-not-weakening (my A6 confirmed).** For non-proof-carrying/ingress fields, compile-time prove-or-reject was over-*rejection* (rejecting unprovable, not preventing invalid data); runtime-governance atomic refusal IS prevention. Carries-proof set (mincount count>0, sign/rule-fact) STAYS in the compile floor. Bands are NOT in philosophy.md L53's impossibility list, so band relocation needs no philosophy.md edit. Plan never blurs D1 (relaxed) vs D2 (relocated) — the single most important philosophical discipline in the doc.
  - **D3 atomicity relaxation SAFE** given stub + no users; one hard ordering (registry-honest-before-Phase-2-checker) is necessary AND unique (only Phase 2 bakes a structural property over the registry). Stress 1/2 are genuine adversarial searches.
  - **Prior findings ALL cleared (verified against plan text + source):** B1 GATE-F enumeration complete incl. Principle 11 L112 defensive-redundancy clause (verified verbatim); B2 "zero constraint-synthesis/enforcement consumers; 2 cosmetic" (source-confirmed exactly 2: `CatalogFormatters.cs:253`, `GrammarGen/Program.cs:173`); B3 28+4=32 provenance; A1/A2/A3/A6/A7 folded.
  - **The one miss (B1, new):** the D1 honesty sweep reconciles every PROSE canon sentence and builds Slice 6.2's liveness gate with a parked-NumericOverflow carve-out — but stops one truth-surface short: the `PRECEPT0002` Roslyn analyzer description (`Precept0002FaultCodeMustHaveStaticallyPreventable.cs:19-24`) still asserts every fault's diagnostic "makes this fault unreachable at runtime," which is false for parked NumericOverflow. DoD-4's doc-grep scopes `docs/` only, not `src/Precept.Analyzers/*.cs`. Same shape as my prior B1/B3 (an honesty/completeness sweep under-enumerating its surface), squarely in the catalog/metadata-honesty lane → blocking, but a half-hour fix entirely inside Slice 6.2's existing scope. NOTE for future: when a decision relaxes a guarantee, the honesty sweep must cover the analyzer family (PRECEPT0002/0027/0029/0019), not just prose canon.
  - **Rigour: EXCEEDS the 06-11 bar** — all 8 phases heavyweight (06-11 stubbed Phases 2-7); coverage ledger reconciles (16/8/10/236/43 + Point A/B/C/Q4/Q5 residues recovered, not asserted); calibration substantive ("no claim rated above its evidence"); red-team genuine.

### 2026-06-21T05:06:48Z — Decision ledger batch ingested
- Frank's 06-16 compiler-readiness re-review and the related Frank inbox reviews were merged into the decision ledger.
- The compiler-readiness item still carries one blocker (B1), so Phase-0 sign-off remains gated.

- 2026-06-21 — Amendment verification of `docs/Working/compiler-readiness-plan-2026-06-16.md` after it was amended in response to my re-review. **B1 CLEARED — plan is sign-off-ready.** Source-checked, not trusted:
  - **B1 — CLEARED, both legs, correct shape.** (a) DoD-4 grep scope widened to `docs/` AND `src/Precept.Analyzers/*.cs` AND `Diagnostics.cs`/`Faults.cs` *prevention*-claim strings (plan `:1679`) — correctly scoped to prevention-claims, not fault message templates. (b) PRECEPT0002 reconciliation enumerated as an in-scope **(B1) Code-as-canon edit** in Slice 6.2 (`:1044` work, `:1051` exit, `:1053` doc-touch) — rewords to the **linkage invariant** / adds a **`known-not-prevented` carve-out**, NOT a honesty-requirement deletion. Confirmed against live source: `Precept0002FaultCodeMustHaveStaticallyPreventable.cs:24-25` over-claims "unreachable at runtime" for parked NumericOverflow; the carve-out precedent it mirrors actually exists (`StaticallyPreventableMap.cs:18-26` design-time-linkage docstring for UnexpectedNull); attribute must stay (NumericOverflow still bijective `:34` + emitted).
  - **A1 — CLEARED.** General honesty-sweep principle named with the analyzer family (PRECEPT0002/0027/0029/0019) at plan `:1045`.
  - **A3 — CLEARED.** `CollectComputedFieldBoundObligations` (`ProofEngine.Analysis.cs:534`) named with target-state-not-true-at-HEAD framing at plan `:281`; honest gap becomes true only AFTER Slice 1.3 reclassifies the band IntervalContainment. Source-confirmed.
  - **No regressions.** Both added verification surfaces are static string-greps, not runtime-execution exits (Slice 6.2 `:1052` keeps "runtime emits the fault" out, §11). Coverage ledger untouched/reconciles. D1-relaxed vs D2-relocated distinction intact and reinforced.
  - **Updated sign-off call: READY FOR OWNER SIGN-OFF on the Phase-0 gate set — Yes.** No residual blockers. The Phase-0 gate ratification (GATE-F spec amendments, philosophy.md overflow reconciliation, GATE-I fault-delivery pick) remains Shane's, as it must be.

- 2026-06-23 — Two-part assessment for Shane: (1) re-review of `docs/Working/compiler-readiness-plan-2026-06-16.md` as-it-stands; (2) assess proposed amendment "force authors to propagate constraints to args to guard against assignments." Recommendation memo: `.squad/decisions/inbox/frank-arg-constraint-propagation-assessment.md`.
  - **Part 1 — plan still sound; no new blockers.** Phase ordering holds; the single hard ordering (registry-honest-before-Phase-2-position-checker, D3) re-searched and confirmed unique — Phase-5 2c-ii↔Slice-1.2 synthesis coupling and Phase-2's intra-guard-narrowing bundle are intra-slice/natural-order, not new cross-phase atomic constraints. Boundary spot-checked on the slices re-read this pass (5.1/2.2/3.2/3.1): every exit is `precept_compile`/`dotnet build/test`, each carries an explicit "no runtime exit criterion" disclaimer. ONE mild new Part-1 finding (P1-1, advisory): GATE-F enumerates the band/assignment sentences to soften (incl. §0.6 item 6) but frames them one-directionally ("soften to proven-violation-only"); it does NOT surface the arg→field **ingress** assignment as a distinct decision axis, nor quote the §0.7 L266 "no deferral" counter-position the Part-2 amendment leans on. Neutral-framing discipline (Slice 0.2) wants both directions visible. Fix: add the ingress-arg sub-axis to the GATE-F owner conversation.
  - **Part 2 — PROBED CURRENT BEHAVIOR (precept_compile, verbatim).** Three-field comparison (`min 0 max 100` band / `positive` sign / `rule X>=0`), each assigned an UNconstrained event arg via `set Field = Arg`:
    - **Band (min+max literal):** IntervalContainment obligation IS created on the assignment; unconstrained arg → interval `[−∞..+∞]` → **Unresolved → PRE0078 reject** (over-rejects merely-unprovable). A constrained arg (`Val min 0 max 100`) → interval `[0..100]` → **Proved → clean**. So author-propagation IS already the discharge path today; the band lane already "forces" it.
    - **Min-only / sign (`positive`/`nonnegative`):** NO per-assignment obligation → **FAIL-OPEN (false-accept)**. = **BUG-021** (`bugs.md:203`, "set-action assignment does not enforce a field's lower bound when no max"; same family as BUG-017/018/019 "obligation never *created*").
    - **Global rule (`rule X>=0`):** `set X = -5` (LITERAL provable violation) → **success, ZERO obligations.** Rules are NOT assignment-checked at compile time at all — by design they are runtime-governance/ensure enforced (RUNTIME_CODE_DEFER, §11 governance sweep / A6).
  - **Verdict: the amendment is NOT a distinct unaddressed gap and NOT a simple restatement of one slice — it is a COUNTER-PROPOSAL to the D2/GATE-F band-right-sizing decision, scoped to event-arg ingress assignments.** The plan already OWNS this decision surface: D2 (Slice 1.3 band reclassification), A6 (prevention-relocated-to-runtime-governance), GATE-F (the §0.7 L266 / §0.6 item 6 / Principle-11 amendments), BUG-021/Slice 3.3 (min-only fail-open), and the §11 governance-sweep capture. The amendment doesn't add a missing work item; it disputes the DIRECTION of an existing, not-yet-ratified Phase-0 decision. Crucially, the amendment ALIGNS with the CURRENT locked spec (§0.7 L266 "no result outside a declared bound … rejects … there is no deferral"; Principle 11 L112 "constraint range impossibility … defensive redundancy, never the primary enforcement") — it is D2/GATE-F that proposes to REVERSE those.
  - **Tier classification: Tier 3 (spec conflict).** Touches language surface (a new forced-authoring obligation + new rejection behavior) AND a locked-spec area that is mid-amendment (§0.7 L266 / Principle 11 / §0.6 item 6). Must route through `/design` AFTER owner alignment — NOT designed in this assessment. Recommendation: do NOT add a new slice; FOLD the amendment into the open GATE-F owner conversation as an explicit ingress-arg decision axis, with both directions (force-propagation prove-or-reject vs relax-to-governance) and the §0.7 L266 "no deferral" sentence quoted verbatim.
  - **Philosophy gap surfaced (not resolved):** where is the prevention boundary between compile-time-structural-impossibility and runtime-governance-refusal for ingress arg→field assignments? D2/A6 = governance refusal is prevention relocated; amendment = compile-time prove-or-reject is stronger prevention (structurally impossible, never reaches runtime, fully AI-legible/deterministic at author time). `philosophy.md` L53 impossibility list does not include bands (per D2) — the amendment would argue it should for assignments. Shane's call.

- 2026-06-23 — Promoted the arg→field constraint-propagation assessment to `docs/Working/compiler-readiness-plan-2026-06-16-arg-constraint-propagation-assessment.md` (companion to the readiness plan; full four-leg rationale, verbatim `precept_compile` probe table, Tier-3 classification, surfaced philosophy gap). **HELD by owner** — recorded for later revisit, not active; plan/spec/philosophy untouched.
