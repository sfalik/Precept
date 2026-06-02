# Phase 8 Slices 2–3 Execution Plan — 2026-06-01

**Status**: Active
**Companion docs**: [`phase8-slice2-code-mediation-2026-06-01.md`](phase8-slice2-code-mediation-2026-06-01.md) (Locked), [`phase8-slice3-emission-ownership-2026-05-31.md`](phase8-slice3-emission-ownership-2026-05-31.md) (Locked); grounded by [`phase8-slice2-dispatch-deepdive-2026-06-01.md`](phase8-slice2-dispatch-deepdive-2026-06-01.md) and [`phase8-slice1-emission-inventory-2026-05-31.md`](phase8-slice1-emission-inventory-2026-05-31.md).
**Scope gate**: closes the core of Phase 8 (diagnostic-emission architecture); unblocks Slice 4 (witness richness) and hands a clean catalog-mediated surface to Phase 9 (emit-or-retire). Compiler-internal, pre-release, no external consumers.

> **Vocabulary**: the build units below are the slices themselves. **Slice 2** is one build unit (done). **Slice 3** is large, so it executes as three sub-slices **3a → 3b → 3c**. No separate "phase" numbering. (The Slice 2 *design* doc names its internal decisions P1–P4 and the Slice 3 doc names its D1–D5 — those are design-decision labels within each locked doc, distinct from these build units.)

## Build-slice summary

| Build slice | Goal | Design decisions | Decisions req. | Effort | Status |
|---|---|---|---|---|---|
| **Slice 2** | Uniform code selection (literal or single catalog field); no behavior change | Slice 2 P1–P4 | none (Locked) | M (~2–3d) | ✅ **Complete `59fe2666`** |
| **Slice 3a** | Honest `DiagnosticStage` taxonomy + dual-emission consolidation (6 duals incl. name-resolution → Bind) + hover decouple | Slice 3 D2, D4 (amended) | none (Locked) | M (~2–3d) | ✅ **Complete `f7e5dee6`** |
| **Slice 3b** | Declared-bound obligation ownership — numeric (`OutOfRange`/`NumericOverflow`) + string-length defaults + qualifier residual | dedicated locked design `phase8-value-level-obligation-ownership-2026-06-01.md` (expands Slice 3 D3, resolves D5) | none (Locked + reviewed) | M–L (~3–4d) | ✅ **Complete `ecce5d51`** |
| **Slice 3b-count** | Build the stubbed `TryCountContainmentProof` + stamp `CountContainment` for collection/notempty defaults; revive the dead `CountBoundViolation` | same design (D9) | none (Locked) | M (~2d) | **Next** |
| **Slice 3c** | The ownership analyzer (enforces single-stage ownership; one documented carve-out: the `TypedConditional` qualifier residual) | Slice 3 D1 | none (Locked) | M (~2d) | Stub |

**Why this order (strict):** Slice 3c's analyzer can only reach green-with-one-carve-out once everything it checks holds — code uniformly sourced (Slice 2), stages honest + no structural duals (3a), and the whole declared-bound family genuinely proof-owned (3b numeric/length/qualifier + 3b-count). So the analyzer lands **last**. Slice 2 is pure no-behavior-change (safest first); 3a carries one author-visible change (stage strings/counts); 3b/3b-count are the behavioral changes (relocating + reviving declared-bound checks); 3c is enforcement. 3b-count follows 3b because it builds a new proof strategy (the stubbed count prover) rather than relocating an existing check — separable and independently verifiable.

## Decisions captured

All decisions are **Locked** in the two design docs (2026-06-01) — nothing to re-litigate:
- **Slice 2 design** (Decisions P1–P4): P1 typed `DiagnosticCode?` carriage (null⇒catalog; kill round-trip + dual-surface); P2 fault map from `[StaticallyPreventable]` (keep collapse/backstop); P3 document Pattern-2 residue; P4 collapse CI field pair.
- **Slice 3 design** (Decisions D1–D5): D1 catalog-ownership + analyzer (no dedup); D2 honest `Bind`/`Tooling` taxonomy; D3 proof-walk extension; D4 (amended) dual-emission consolidation — 6 duals: `NoInitialState`→Graph, `CircularComputedField`+the `Undeclared*` name-resolution family→Bind (type checker defers to binder markers; fixes a latent double-emission), delete dedup guard; D5 `OutOfRange` kept proof-owned; D6 `MaxPlacesExceeded` stays Type.
- Cross-cutting: no runtime reconciliation/dedup; no allow-list; behavior-preservation is the hard gate for Slice 2 (and for the constant cases in Slice 3b).

## Open decisions

- **PRE0141 bound-expression ownership (GATES Slice 3c — surfaced during 3b execution `ecce5d51`).** `UnprovedAssignmentQualifierCompatibility` is emitted by the type checker on min/max **bound expressions** (verified: an open field-ref bound `min Floor` on a qualified field fires it), because the proof engine does not walk bound expressions. The design's D5 single-stage-ownership target wants this resolved. Options:
  - **(a) Walk bounds** — add a proof-engine collector that stamps the `AssignmentQualifier` obligation on bound expressions. Pro: uniform proof-ownership, no carve-out. Con: new collector; *and* a bound declaration has no guard scope, so the proof engine can't narrow an open bound qualifier — it would emit the same PRE0141, just relocated (single-ownership gain, no capability gain). Also implicitly blesses dynamic field-ref bounds.
  - **(b) Split the diagnostic code** — a distinct code for the static type-stage qualifier check (bounds, and possibly the `TypedConditional` case), leaving PRE0141 proof-only. Pro: each code single-stage, honest about static-vs-narrowable. Con: a new author-visible diagnostic code for what reads as the same problem.
  - **(c) Accept a carve-out** — the 3c analyzer allow-lists the type-stage PRE0141 bound (and conditional) emissions. Pro: zero new work. Con: the analyzer isn't pure one-code-one-stage.
  - **(d) Tighten bounds to static** — if dynamic field-ref bounds (`min SomeField`) aren't a wanted feature, require static bounds → the open-qualifier bound case can't arise → no carve-out. Larger language decision.
  - **Recommended process**: a focused `/lifecycle-2-design` touch-up on D5 (it touches a public diagnostic code and/or proof-engine architecture) with owner direction — NOT settled inside an execute pass. **Resolve before 3c.**
- *`OutOfRange` consolidate-or-retire* — owned by **Phase 9 / F-LANG-SPEC-12** (emit-or-retire). Slice 3b keeps `OutOfRange` proof-owned; whether it later merges with `NumericOverflow`/`UnprovedModifierRequirement` is a Phase-9 catalog-completeness call. Does not block Slice 3b or 3c. Land the answer in the Phase 9 finding register, not here.

## Completed slices

### Slice 2 — Uniform code selection (no behavior change) ✅ `59fe2666`

Converged every diagnostic-code selection onto two shapes (literal `DiagnosticCode.X` or a single catalog-meta `DiagnosticCode` field read). Delivered the Slice 2 design's Decisions P1–P4: killed the typed-constant string round-trip; `CreateDiagnostic` reads `ProofRequirementMeta.DiagnosticCode` for subtype-fixed kinds; fault map derived from `[StaticallyPreventable]` (`StaticallyPreventableMap`, with the collapse/backstop kept explicit); CI field pair collapsed. **Byte-identical** verified — full suite green (6624/417/291/67, 0 failed) + adversarial diff review clean. A pre-existing `[StaticallyPreventable]`↔`:412` drift (`UnprovedPresenceRequirement`) was found and behavior-preservingly documented (design Falsifier 2). Doc-sync: `diagnostic-system.md` "Emission shapes" subsection + `proof-engine.md` note.

## Heavyweight blocks (Slice 3a ✅ complete · Slice 3b current)

### Slice 3a — Honest taxonomy + dual-emission consolidation ✅ `f7e5dee6`

**Outcome**: delivered all six steps below. Full suite green (**6648**/417/291/67, 0 failed — +24 over the Slice 2 baseline of 6624). Adversarial diff review (`precept-reviewer`) confirmed the D26 soundness gate holds (every dropped type-checker name-resolution emit traces to a binder emit at the same syntactic position) and surfaced two issues fixed before commit: (1) the binder's Kahn detector over-reported `CircularComputedField` on fields merely *downstream* of a cycle (fabricated cycle string) — now restricted to fields genuinely on a cycle (reachable from themselves); (2) a repeated unknown state name in a from-list double-emitted `UndeclaredState` — the binder now dedups the list so the duplicate yields only the type checker's `DuplicateStateInList`. One scope-expansion surfaced during the build: the D26 self-containment checks became upstream-aware (`CheckContext.UpstreamDiagnostics`) because name-resolution error nodes now get their diagnostic from the binder.

**Goal**: `DiagnosticStage` truthfully names the producer (`Bind`/`Tooling` added, `NameBinder→Type`/`Mcp→Lex` mislabels removed, no precedence semantics); **all six** dual-emissions consolidate to single owners (incl. the name-resolution family → Bind, fixing a latent double-emission); LS hover surfaces obligations by obligation-presence, not the `Proof` stage label.

**Design decisions delivered**: Slice 3 D2, D4 (amended — 6 duals incl. the `Undeclared*` family) (+ the hover decouple).

**Decisions required before kicking off**: none (Locked; `CircularComputedField`/`Undeclared*` → Bind settled in D4 with a coverage falsifier).

**Step-by-step**:
1. Add `Bind`, `Tooling` to the `DiagnosticStage` enum (`Diagnostic.cs`); update the enum + `Stage`-field doc-comments to "producing-component classification, no precedence" (the comment doc-sync from Slice 3's doc-update).
2. Relabel meta (`Diagnostics.cs`): NameBinder-produced codes → `Bind`; `McpToolInternalError` → `Tooling`; the `Undeclared*` family → `Bind` (they're Bind-owned post-consolidation, step 5). (Value-level codes that become proof-owned are relabeled to `Proof` in **Slice 3b** — not here.) Update `DiagnosticsTests` stage assertions.
3. D4 — `NoInitialState` → Graph only: delete the `TypeChecker.cs:704` emit and the `GraphAnalyzer.cs:85` `HasDiagnostic` guard; confirm the graph path covers every case the type-checker emit did (coverage check).
4. D4 — `CircularComputedField` → Bind only: delete the `Structural.cs:248` DFS detector; run the falsifier coverage check (binder topological-sort detector catches every cycle class the DFS did) before deleting.
5. D4 — **name-resolution family `UndeclaredField`/`UndeclaredState`/`UndeclaredEvent` → Bind**: the type checker stops emitting `Undeclared*` at its ~9 sites (`Normalization.cs:208,328,455`, `Expressions.Callables.cs:394,962`, `Expressions.cs:949`, `TypeChecker.cs:1150,1214,1300`), deferring to the binder's `UnresolvedTarget` markers (it keeps *looking names up* for typing — only the emit moves). Lift the binder's state-*list* resolution from first-name-only to full-list. Per-site coverage check before deleting each emit. (Also reconcile the `Event.notAnArg` mis-coding: type checker's `UndeclaredField` at `Callables.cs:962` → the binder's `UndeclaredArg`.)
6. Hover decouple: `RichHoverFactory` keys obligation-surfacing on obligation-presence rather than `Stage == Proof`.

**Dependencies**: Slice 2 (uniform surface — so the relabel and consolidation don't fight the round-trip/dual-surface).

**Exit criteria** (testable):
- Test: every NameBinder-produced code reports `Bind`; `McpToolInternalError` reports `Tooling`; the `Undeclared*` family reports `Bind`.
- Test: each of the six dual codes emits **exactly once** — `NoInitialState` (Graph); `CircularComputedField` (Bind); `UndeclaredField`/`UndeclaredState`/`UndeclaredEvent` (Bind). **Double-emission fixed**: the tolerant tests (`TypeCheckerTransitionTests.cs:196-199`, `≥2`/`Contain`) tightened to exact counts; an undeclared name in a state-list produces one diagnostic per missing name.
- Per-emit coverage check passed before each type-checker emit deletion (binder owner catches every case the dropped twin did).
- `GraphAnalyzer.cs:85` `HasDiagnostic` guard deleted.
- LS hover surfaces obligation-bearing diagnostics identically on the corpus, keyed on obligation-presence.
- `precept_compile` per-stage error counts shift as expected (binder errors now `Bind`; fewer total on undeclared-name inputs) — asserted, not silent.
- Full suite green.

**Estimated effort**: **M (~2–3 days)** — the name-resolution consolidation (defer ~9 type-checker emits to binder markers + lift the binder's state-list resolution) is a real refactor with per-site coverage checks, not the original "relabel + 2 duals" S–M.

**Doc-update obligations**:
- `docs/compiler/diagnostic-system.md § Diagnostic Stages` — `Bind`/`Tooling`, the two distortions resolved.
- `src/Precept/Language/Diagnostic.cs` — `DiagnosticStage`/`Stage`-field doc-comments retitled to producing-component (code-comment doc-sync).
- `docs/tooling/mcp.md` — note the stage-string + per-stage-count change in `precept_compile` output.

### Slice 3b — Declared-bound obligation ownership (numeric + length + qualifier) ✅ `ecce5d51`

**Outcome (corrected 2026-06-01)**: delivered the `OutOfRange` relocation (kills the arg double-emit + field/arg split), the `NumericOverflow`/`OutOfRange` Type→Proof relabels, the string-length default closure (D8), and the qualifier-residual relocation for walked sites (D5). Suite green (**6672**/417/291/67); numeric-constant golden byte-identical; no sample newly errors. Adversarial diff review caught + fixed two regressions before commit (multi-modifier defaults fanned out to N `OutOfRange` → first-violation-wins one-per-default; decimal display value was culture-dependent → `InvariantCulture`), confirmed by two new regression tests. **⚠️ D4 NOT fully delivered**: its headline ("close the computed-field-result-vs-bounds Principle-11 gap") was claimed but only the **bounded-result** case was closed; an **unbounded** result is silently skipped and still compiles clean — **BUG-017**, open. The design doc's "P11 strengthened / acceptable conservative boundary" framing was a rewrite of Principle 11 (it qualified P11 to "statically-decidable values" and leaned on the bug as precedent); corrected in the design doc's ⚠️ Correction header. Treat 3b as: relocation + decidable-case closure done; the computed-result P11 gap still open (BUG-017). **Surfaced during execution (NOT resolved here — see Open Decisions)**: D5 listed *bound* expressions among the flip targets, but the proof engine does not currently walk min/max bound expressions, so the build left bound-expression qualifier residuals **type-emitted** (stamping there would have dropped the diagnostic — verified: an open field-ref bound like `min Floor` does fire PRE0141). This means PRE0141 currently emits from the type checker at two sites (the `TypedConditional` value **and** bound expressions). What to do about it — walk bounds / split the code / accept a carve-out — is an **open design decision**, deferred to a design touch-up before 3c. Not decided in this execute pass.

**Design**: [`phase8-value-level-obligation-ownership-2026-06-01.md`](phase8-value-level-obligation-ownership-2026-06-01.md) (Locked, 3 review rounds → LOCKABLE; expands Slice 3 D3, resolves D5). 3b builds the **numeric** (D1–D4), **string-length** (D8), and **qualifier-residual** (D5) closure + the stage relabels (D3). The **collection-count** family (D9) is split to **3b-count** (it builds a new proof strategy). `notempty`-on-strings is in 3b (length min=1); `notempty`-on-collections is in 3b-count.

**Goal**: every numeric/length declared-bound violation on a default, and every numeric computation-result overflow, is proof-owned and single-coded (numeric default → `OutOfRange`; computation → `NumericOverflow`; string default → `LengthBoundViolation`; qualifier residual → proof-stage PRE0141), with no double-emit, no field/arg split, and no silent length-default gap.

**Decisions required before kicking off**: none (design Locked + reviewed; D1–D10 approved).

**Step-by-step** (enumerate → failing-test matrix first, per `/lifecycle-4-execute`):
1. **Stage relabels (D3)** — `Diagnostics.cs`: `NumericOverflow` + `OutOfRange` meta `Type → Proof`; update `DiagnosticsTests` stage assertions.
2. **OutOfRange dispatch arm (D2)** — `ProofEngine.Diagnostics.cs` `GetNumericRequirementDiagnosticCode`: add an arm returning `OutOfRange` when `obligation.Context` is `FieldDefaultContext`/`ArgDefaultContext` (Context-axis, non-overlapping with the existing Site-shape arms).
3. **Numeric default stamping (D2)** — `TypeChecker.cs:799-815`/`:985-1004`: replace `ValidateDefaultAgainstNumericModifiers` with stamping `NumericProofRequirement(SelfValue,⊕,bound)` per applicable modifier (preserve field implied-modifiers + reuse the magnitude/normalization helpers from `Modifiers.cs:524-614`). `ProofEngine.Analysis.cs` collectors: drop the IntervalContainment-for-numeric-defaults usage; route numeric defaults through the stamped Numeric obligation.
4. **Computed-field gap (D4)** — extend the computed-expr walk (`ProofEngine.cs:233-241`) / add a collector stamping `IntervalContainmentProofRequirement` for a computed numeric field's result vs its declared bounds → `NumericOverflow` (reuses `IntervalOfNarrowed`).
5. **Length defaults (D8)** — `ProofEngine.Analysis.cs` collectors: add a `LengthContainment` branch for string defaults (declared minlength/maxlength + literal string); fold `notempty`-on-string as `min=1` (detect `ModifierKind.Notempty`, since it sets no `DeclaredMinLength`). Discharged by the existing literal-only `TryLengthContainmentProof`.
6. **Qualifier residual (D5)** — `AssignmentQualifiers.cs`: set `dischargedAtProofStage` true for field/arg-default, bound, computed-expr sites; leave `false` only for `TypedConditional` (the carve-out). PRE0141 becomes proof-owned except the conditional.
7. **Doc-sync** (same commit): proof-engine.md, diagnostic-system.md, mcp.md, `ProofRequirementKind.cs` comment.

**Dependencies**: Slice 3a (✅ `f7e5dee6` — hover already keyed on obligation-presence); Slice 2 uniform surface.

**Exit criteria** (testable — from the design's acceptance matrix):
- **Numeric single-emission**: {field, arg} default × {literal, TypedTypedConstant, resolvable interpolated} below `min` → exactly one `OutOfRange` (Proof); **no `NumericOverflow` on any default cell** (double-emit + split gone).
- **Numeric computed-field (D4) — PARTIAL**: `field T as number max N <- e` with a **bounded** `IntervalOf(e)` exceeding `[..N]` → exactly one `NumericOverflow` (Proof); in-bounds computed field clean. **Does NOT cover an *unbounded* result** (e.g. `e` over an operand with no `max`): silently skipped, compiles clean — **BUG-017**, open. This criterion was satisfiable by bounded cases only; the unbounded P11 gap is not closed.
- **Length (D8)**: `field/arg as string minlength M default "<short>"` → exactly one `LengthBoundViolation` (Proof); `notempty` string default `""` → one `LengthBoundViolation`; in-bounds clean; **set-action length unchanged**.
- **Qualifier residual (D5)**: open-qualifier default/bound/computed → PRE0141 from **Proof**; `TypedConditional` value → PRE0141 from **Type** (the only remaining type-stage PRE0141 site).
- **Behavior-preservation golden**: constant numeric defaults produce byte-identical message+code (only stage differs).
- **Stage relabels**: `GetMeta(NumericOverflow).Stage == Proof`, `… OutOfRange … == Proof`.
- **Full suite green**; `precept_compile` per-stage counts shift as documented, asserted not silent.
- **NOT in 3b**: collection-count (→ 3b-count), `MaxPlacesExceeded` (D6/Slice 5), the analyzer (3c).

**Estimated effort**: **M–L (~3–4 days)** — three surfaces' default stamping + the OutOfRange dispatch + the computed-field collector + the qualifier-residual relocation, each behavior-changing with per-cell coverage tests; numeric-constant behavior-preservation is the hard gate.

**Doc-update obligations**:
- `docs/compiler/proof-engine.md` — Numeric 1:many OutOfRange arm; IntervalContainment extended to computed fields; defaults route through the stamped Numeric obligation; `ProofRequirementKind` "eleven"→"thirteen"; `TryLengthContainmentProof` file-location fix (`ProofEngine.Lengths.cs`) + strategy-number/kind-ordinal reconcile.
- `docs/compiler/diagnostic-system.md` — `NumericOverflow`/`OutOfRange` stage = Proof; the declared-value-vs-computation partition.
- `docs/tooling/mcp.md` — per-stage count shift + new length-default diagnostics.
- `src/Precept/Language/ProofRequirementKind.cs` — "eleven"→"thirteen" (code-comment doc-sync).

## Lightweight stubs (later slices)

### Slice 3b-count — Build the collection-count proof strategy (D9)
**Goal**: implement the stubbed `TryCountContainmentProof` (mirror `TryLengthContainmentProof` on `TypedListLiteral.Elements.Length`); stamp `CountContainment` for collection field/arg defaults (list-literal shape) + `notempty`-on-collection as `mincount 1`; **revive the dead `CountBoundViolation`** and re-place it in `DiagnosticCoverageAllowLists`. **Scope**: reaches `list`-typed defaults; set/queue/stack/bag/log reject list-literal defaults upstream (PRE0044/PRE0018 — named, out of scope). Keep `CountContainment` disjoint from the `Numeric(count-accessor)` action-safety path (D10). **Design decisions**: D9, D10. **Decisions required**: none (Locked). **Effort**: M (~2 days). **Status: Stub — TBD pending Slice 3b completion.** Key acceptance: a NEW default-bearing `list of string mincount 2 default ["a"]` test asserts exactly one `CountBoundViolation` (Proof); the existing no-default `set` test (`ProofEngineStringCollectionBoundTests.cs:253-267`) KEEPS asserting `BeEmpty`; `TryCountContainmentProof` returns `false` on a violating list literal, `null` on a non-literal collection.

### Slice 3c — The ownership analyzer
**Goal**: new `Precept.Analyzers` analyzer enforcing single-stage ownership — for each emission site (literal + catalog-mediated field reads + the bounded Pattern-2 residue candidate sets), the emitting stage equals `Diagnostics.GetMeta(code).Stage`. The design's target is **one** documented carve-out (the `TypedConditional` PRE0141 residual). **Blocked on the Open Decision below**: 3b surfaced that PRE0141 also emits from the type checker on min/max bound expressions; depending on how that's resolved (walk bounds / split the code / accept it), 3c expects one or two carve-outs. Resolve before building 3c. Every other declared-bound/value-level code is single-owned by construction after Slice 2 + 3a + 3b + 3b-count. Reuses `DiagnosticCoverageScanner`; detection by containing type. **Design decision**: Slice 3 D1 (+ the D5 carve-out). **Decisions required**: none (Locked). **Effort**: M (~2 days). **Status: Stub — TBD pending Slice 3b + 3b-count completion** (the analyzer can't be green until value-level codes are proof-owned).

## Definition of done

The Slices 2–3 workstream is complete when:
- The ownership analyzer is **green with the carve-out set settled by the open PRE0141-bound-ownership decision** (the `TypedConditional` residual, plus possibly min/max bound expressions unless that decision walks-bounds or splits-the-code); a deliberately wrong-stage `Diagnostics.Create` in a test fixture trips it.
- No `DiagnosticCode` is emitted from two stages (all 6 Slice-3a duals consolidated; the numeric `OutOfRange`/`NumericOverflow` double-emit + field/arg split removed; `GraphAnalyzer.cs:85` guard gone).
- The **whole declared-bound family** is proof-owned and single-coded: numeric defaults → `OutOfRange`, computations (set-action + computed field) → `NumericOverflow` **only when the result interval is bounded** (an *unbounded* result is silently skipped — **BUG-017**, an open Principle-11 hole this workstream does NOT close), string defaults → `LengthBoundViolation`, collection defaults → `CountBoundViolation`, qualifier residual → proof-stage PRE0141. Constant-default violations still produce a diagnostic (behavior-preserving), verified on the corpus. The **dead `CountBoundViolation` is revived** (genuine — 3b-count). (`MaxPlacesExceeded` stays Type-owned — Decision 6 / Slice 5.)
- `DiagnosticStage` has honest `Bind`/`Tooling`; `NumericOverflow`/`OutOfRange` relabeled to `Proof`; no mislabels; no precedence semantics.
- All doc-touch obligations met (`diagnostic-system.md`, `proof-engine.md`, `Diagnostic.cs` comments, `mcp.md`, `ProofRequirementKind.cs` comment).
- Full suite green across all 4 projects; `precept_compile` output stable except the **documented** changes (3a stage-strings/counts; 3b numeric Type→Proof + new length-default diagnostics; 3b-count new collection-count diagnostics).
- Scope gate opens: Slice 4 (witness richness) can start; Phase 9 inherits a uniform catalog-mediated surface with no dead `[StaticallyPreventable]` declared-bound codes.

## Discovered during planning

Guard-7 check (plan-touches ⊆ designs' `sources-consulted` ∪ Inventory): **no out-of-design sources.** Every file Slices 2/3a touch — the per-type validators, `TypeChecker.Expressions.cs`, `ProofEngine.Diagnostics.cs`/`ProofEngine.cs`, `FaultCode.cs`, `Diagnostics.cs`, `Diagnostic.cs`, `Operation.cs`/`Function.cs`/`Operations.cs`/`Functions.cs`, `CI.cs`, `GraphAnalyzer.cs`, `NameBinder.cs`, `Structural.cs`, `TypeChecker.cs`, `RichHoverFactory.cs`, `CompileTool.cs`, `CatalogFormatters.cs`, `DiagnosticCoverageScanner.cs`, the new analyzer, and the test projects — appears in those designs' Inventory / `sources-consulted` / doc-update, or the deep-dive evidence index they cite.

Slice 3b/3b-count derive from `phase8-value-level-obligation-ownership-2026-06-01.md`; every file they touch — `ProofEngine.Diagnostics.cs`, `ProofEngine.Analysis.cs`, `ProofEngine.Lengths.cs`, `ProofEngine.Strategies.cs`, `TypeChecker.cs`, `TypeChecker.Validation.Modifiers.cs`, `AssignmentQualifiers.cs`, `Diagnostics.cs`, `ProofRequirement.cs`, `ProofRequirementKind.cs`, `Actions.cs`, `SemanticIndex.cs`, `DiagnosticCoverageAllowLists.cs`, `collection-types.md`, the proof-stage test projects — is in that design's `sources-consulted`/Inventory. **One execution surface acknowledged in the design body but not its frontmatter**: `ProofEngine.Intervals.cs` (`IntervalOfNarrowed`), the discharge path the D4 computed-field obligation reuses — listed here for completeness; not a design gap (the design references `IntervalOfNarrowed` and proof-engine.md:1672 explicitly), no re-lock required.

## Plan update protocol

- On slice completion: flip its row to ✅ in the Build-slice summary; promote the next stub to heavyweight; update the readiness-plan Phase 8 slice log.
- If a behavior-preservation diff appears in Slice 2 (or Slice 3b constants), **stop** — it means a step changed semantics; treat as a design-falsifier hit, not a test to update.
- If new work surfaces, add it to the appropriate slice or a new stub; if it touches a surface neither design cited, re-lock the affected design (don't silently absorb).
- Execution rigor (failing-test-first, fresh-context agent, adversarial diff review, vertical slices) is `/lifecycle-4-execute`'s domain — this plan only sequences.
