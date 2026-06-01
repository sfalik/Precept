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
| **Slice 3b** | Proof-walk extension — value-level checks become proof-owned obligations | Slice 3 D3, D5 | none (Locked) | M (~2–3d) | **Next** |
| **Slice 3c** | The ownership analyzer (enforces single-stage ownership; zero allow-list) | Slice 3 D1 | none (Locked) | M (~2d) | Stub |

**Why this order (strict):** Slice 3c's analyzer can only reach green-with-no-allow-list once everything it checks holds — code uniformly sourced (Slice 2), stages honest + no structural duals (3a), value-level codes genuinely proof-owned (3b). So the analyzer lands **last**. Slice 2 is pure no-behavior-change (safest first); 3a carries one author-visible change (stage strings/counts); 3b is the behavioral change (relocating value-level checks); 3c is enforcement.

## Decisions captured

All decisions are **Locked** in the two design docs (2026-06-01) — nothing to re-litigate:
- **Slice 2 design** (Decisions P1–P4): P1 typed `DiagnosticCode?` carriage (null⇒catalog; kill round-trip + dual-surface); P2 fault map from `[StaticallyPreventable]` (keep collapse/backstop); P3 document Pattern-2 residue; P4 collapse CI field pair.
- **Slice 3 design** (Decisions D1–D5): D1 catalog-ownership + analyzer (no dedup); D2 honest `Bind`/`Tooling` taxonomy; D3 proof-walk extension; D4 (amended) dual-emission consolidation — 6 duals: `NoInitialState`→Graph, `CircularComputedField`+the `Undeclared*` name-resolution family→Bind (type checker defers to binder markers; fixes a latent double-emission), delete dedup guard; D5 `OutOfRange` kept proof-owned; D6 `MaxPlacesExceeded` stays Type.
- Cross-cutting: no runtime reconciliation/dedup; no allow-list; behavior-preservation is the hard gate for Slice 2 (and for the constant cases in Slice 3b).

## Open decisions

**None gate any build slice.** One downstream, non-gating item:
- *`OutOfRange` consolidate-or-retire* — owned by **Phase 9 / F-LANG-SPEC-12** (emit-or-retire). Slice 3b keeps `OutOfRange` proof-owned; whether it later merges with `NumericOverflow`/`UnprovedModifierRequirement` is a Phase-9 catalog-completeness call. Does not block Slice 3b or 3c. Land the answer in the Phase 9 finding register, not here.

## Completed slices

### Slice 2 — Uniform code selection (no behavior change) ✅ `59fe2666`

Converged every diagnostic-code selection onto two shapes (literal `DiagnosticCode.X` or a single catalog-meta `DiagnosticCode` field read). Delivered the Slice 2 design's Decisions P1–P4: killed the typed-constant string round-trip; `CreateDiagnostic` reads `ProofRequirementMeta.DiagnosticCode` for subtype-fixed kinds; fault map derived from `[StaticallyPreventable]` (`StaticallyPreventableMap`, with the collapse/backstop kept explicit); CI field pair collapsed. **Byte-identical** verified — full suite green (6624/417/291/67, 0 failed) + adversarial diff review clean. A pre-existing `[StaticallyPreventable]`↔`:412` drift (`UnprovedPresenceRequirement`) was found and behavior-preservingly documented (design Falsifier 2). Doc-sync: `diagnostic-system.md` "Emission shapes" subsection + `proof-engine.md` note.

## Heavyweight block (current)

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

## Lightweight stubs (later slices)

### Slice 3b — Proof-walk extension (value-level → proof-owned)
**Goal**: extend the proof-engine default/computed walk (`CollectObligations` / `CollectDefaultObligations` / `CollectArgDefaultObligations`) so the **two** proof-dischargeable value-level checks (`OutOfRange`, the `UnprovedAssignmentQualifierCompatibility` residual) become **proof-owned stamped obligations** instead of type-stage inline emits; the type checker stamps, the proof engine discharges; these codes relabel to `Proof`; `OutOfRange` kept proof-owned (D5). **`MaxPlacesExceeded` is NOT in scope** — Slice 3 Decision 6 keeps it Type-owned (no compile-time proof obligation; its non-static enforcement is runtime). **Design decisions**: Slice 3 D3 (amended), D5, D6. **Decisions required**: none (Locked). **Effort**: M (~2–3 days). **Status: Stub — TBD pending Slice 3a completion.** Key risk (from the Slice 3 review CONCERN): the existing collectors are `InterpolatedTypedConstant`-only — extend to `TypedTypedConstant` without double-emitting on interpolated-with-bounds defaults; behavior-preserving for constants, narrowing-discharge for non-constants.

### Slice 3c — The ownership analyzer
**Goal**: new `Precept.Analyzers` analyzer enforcing single-stage ownership — for each emission site (literal + catalog-mediated field reads + the bounded Pattern-2 residue candidate sets), the emitting stage equals `Diagnostics.GetMeta(code).Stage`. **Green with zero allow-list** (true by construction because Slice 2 + 3a + 3b made the surface uniform + single-owned). Reuses `DiagnosticCoverageScanner`; detection by containing type. **Design decision**: Slice 3 D1. **Decisions required**: none (Locked). **Effort**: M (~2 days). **Status: Stub — TBD pending Slice 3b completion** (the analyzer can't be green until value-level codes are proof-owned).

## Definition of done

The Slices 2–3 workstream is complete when:
- The ownership analyzer is **green with zero allow-list entries**; a deliberately wrong-stage `Diagnostics.Create` in a test fixture trips it.
- No `DiagnosticCode` is emitted from two stages (all **6** duals consolidated — incl. the `Undeclared*` family → Bind; `GraphAnalyzer.cs:85` guard gone; latent double-emission fixed).
- The 2 proof-dischargeable value-level checks (`OutOfRange`, assignment-qualifier residual) discharge at the proof stage; constant-default violations still produce a diagnostic (proof-owned), verified on the corpus. (`MaxPlacesExceeded` stays Type-owned — Decision 6.)
- `DiagnosticStage` has honest `Bind`/`Tooling`; no mislabels; no precedence semantics.
- All doc-touch obligations met (`diagnostic-system.md`, `proof-engine.md`, `Diagnostic.cs` comments, `mcp.md`).
- Full suite green across all 4 projects; `precept_compile` output stable except the **documented** Slice 3a stage-string/count changes and the Slice 3b `OutOfRange` re-stage.
- Scope gate opens: Slice 4 (witness richness) can start; Phase 9 inherits a uniform catalog-mediated surface.

## Discovered during planning

Guard-7 check (plan-touches ⊆ designs' `sources-consulted` ∪ Inventory): **no out-of-design sources.** Every file this plan touches — the per-type validators, `TypeChecker.Expressions.cs`, `ProofEngine.Diagnostics.cs`/`ProofEngine.cs`, `FaultCode.cs`, `Diagnostics.cs`, `Diagnostic.cs`, `Operation.cs`/`Function.cs`/`Operations.cs`/`Functions.cs`, `CI.cs`, `GraphAnalyzer.cs`, `NameBinder.cs`, `Structural.cs`, `TypeChecker.cs`, `RichHoverFactory.cs`, `CompileTool.cs`, `CatalogFormatters.cs`, `DiagnosticCoverageScanner.cs`, the new analyzer, and the test projects — appears in the two designs' Inventory / `sources-consulted` / doc-update, or in the deep-dive evidence index they cite. No design re-lock required.

## Plan update protocol

- On slice completion: flip its row to ✅ in the Build-slice summary; promote the next stub to heavyweight; update the readiness-plan Phase 8 slice log.
- If a behavior-preservation diff appears in Slice 2 (or Slice 3b constants), **stop** — it means a step changed semantics; treat as a design-falsifier hit, not a test to update.
- If new work surfaces, add it to the appropriate slice or a new stub; if it touches a surface neither design cited, re-lock the affected design (don't silently absorb).
- Execution rigor (failing-test-first, fresh-context agent, adversarial diff review, vertical slices) is `/lifecycle-4-execute`'s domain — this plan only sequences.
