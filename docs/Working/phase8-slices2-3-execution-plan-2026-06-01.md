# Phase 8 Slices 2–3 Execution Plan — 2026-06-01

**Status**: Draft
**Companion docs**: [`phase8-slice2-code-mediation-2026-06-01.md`](phase8-slice2-code-mediation-2026-06-01.md) (Locked), [`phase8-slice3-emission-ownership-2026-05-31.md`](phase8-slice3-emission-ownership-2026-05-31.md) (Locked); grounded by [`phase8-slice2-dispatch-deepdive-2026-06-01.md`](phase8-slice2-dispatch-deepdive-2026-06-01.md) and [`phase8-slice1-emission-inventory-2026-05-31.md`](phase8-slice1-emission-inventory-2026-05-31.md).
**Scope gate**: closes the core of Phase 8 (diagnostic-emission architecture); unblocks Slice 4 (witness richness) and hands a clean catalog-mediated surface to Phase 9 (emit-or-retire). Compiler-internal, pre-release, no external consumers.

> **Naming**: this plan decomposes readiness-plan **Phase 8 Slice 2 + Slice 3** into four build phases (P1–P4). "Phase" here = a build phase of this plan; "Slice" = the readiness-plan unit. The dependency order is strict (see Phase summary).

## Phase summary

| Build phase | Slice | Goal | Items | Decisions req. | Effort | Status |
|---|---|---|---|---|---|---|
| **P1** | Slice 2 | Uniform code selection (literal or single catalog field); no behavior change | P1–P4 | none (Locked) | M (~2–3d) | **Next** |
| **P2** | Slice 3a | Honest `DiagnosticStage` taxonomy + dual-emission consolidation + hover decouple | D2, D4 | none (Locked) | S–M (~1–2d) | Planned (heavyweight below) |
| **P3** | Slice 3b | Proof-walk extension — value-level checks become proof-owned obligations | D3, D5 | none (Locked) | M (~2–3d) | Stub |
| **P4** | Slice 3c | The ownership analyzer (enforces single-stage ownership; zero allow-list) | D1 | none (Locked) | M (~2d) | Stub |

**Why this order (strict):** P4's analyzer can only reach green-with-no-allow-list once everything it checks holds — code uniformly sourced (P1), stages honest + no structural duals (P2), value-level codes genuinely proof-owned (P3). So the analyzer lands **last**. P1 is pure no-behavior-change and is the safest first step; P2 is structural (one author-visible change: stage strings/counts); P3 is the behavioral change (relocating value-level checks); P4 is enforcement.

## Decisions captured

All decisions are **Locked** in the two design docs (2026-06-01) — nothing to re-litigate:
- Slice 2: P1 (typed `DiagnosticCode?` carriage, null⇒catalog; kill round-trip + dual-surface), P2 (fault map from `[StaticallyPreventable]`, keep collapse/backstop), P3 (document Pattern-2 residue), P4 (collapse CI field pair).
- Slice 3: D1 (catalog-ownership + analyzer, no dedup), D2 (honest `Bind`/`Tooling` taxonomy), D3 (proof-walk extension), D4 (`NoInitialState`→Graph, `CircularComputedField`→Bind, delete dedup guard), D5 (`OutOfRange` kept proof-owned).
- Cross-cutting: no runtime reconciliation/dedup; no allow-list; behavior-preservation is the hard gate for P1 (and for the constant cases in P3).

## Open decisions

**None gate any phase.** One downstream, non-gating item:
- *`OutOfRange` consolidate-or-retire* — owned by **Phase 9 / F-LANG-SPEC-12** (emit-or-retire). P3 keeps `OutOfRange` proof-owned; whether it later merges with `NumericOverflow`/`UnprovedModifierRequirement` is a Phase-9 catalog-completeness call. Does not block P3 or P4. Land the answer in the Phase 9 finding register, not here.

## Heavyweight phase blocks (current + next)

### P1 — Slice 2: Uniform code selection (no behavior change)

**Goal**: every diagnostic-code selection is either a literal `DiagnosticCode.X` or a single catalog-meta `DiagnosticCode` field read; the string round-trip and the `CreateDiagnostic` dual-surface are gone; the fault map derives from `[StaticallyPreventable]`; the CI field pair is collapsed — with byte-identical diagnostics + fault links on the sample corpus.

**Items in scope**: P1, P2, P3, P4.

**Decisions required before kicking off**: none (Locked).

**Step-by-step** (behavior-preservation is the gate at every step):
1. **Golden snapshot first** (TDD spine): capture current diagnostics + fault links for the full `samples/` corpus as a golden fixture. Every subsequent step must keep it byte-identical. This is the failing-safe harness, written before any production change.
2. **P1a — kill the typed-constant string round-trip**: the per-type validators (`CurrencyValidator`, `MoneyValidator`, `QuantityValidator`, `PriceValidator`, `ExchangeRateValidator`, `ClosedSetValidator`, `RegexValidator`, `Ucum*`, `Temporal*`) carry a typed `DiagnosticCode?` (null ⇒ catalog `Format/SemanticErrorCode` mapping); remove `Enum.TryParse<DiagnosticCode>` in `SelectTypedConstantDiagnosticCode` (`TypeChecker.Expressions.cs:344-356`); the concrete-vs-interpolated brace-gate now keys on the typed value. Validate: snapshot unchanged.
3. **P1b — kill the proof dual-surface**: `CreateDiagnostic` (`ProofEngine.Diagnostics.cs`) reads `meta.DiagnosticCode` for the 10 subtype-fixed kinds; per-arm code retained only for message-arg formatting. `Numeric`/`KeyPresence`/`QualifierChain`-override stay explicit dispatch (Pattern 2). Validate: snapshot unchanged.
4. **P2 — fault map from attribute**: build the bijective `DiagnosticCode→FaultCode` rows by reflecting `[StaticallyPreventable]` (`FaultCode.cs`); keep the many-to-one collapse + backstop as explicit, commented policy (`:412`). Add a test pinning derived == prior bijective rows. Validate: snapshot unchanged.
5. **P4 — collapse CI field pair**: single nullable `CIDiagnosticCode` on `BinaryOperationMeta`/`FunctionMeta` (`Operation.cs`/`Function.cs`); update the ~5 catalog entries (`Operations.cs`/`Functions.cs`), the CI dispatch guard (`CI.cs`), and the MCP formatter read (`CatalogFormatters.cs`). Validate: snapshot unchanged.
6. **P3 — document the residue + emission shapes**: add the "Emission shapes" subsection to `diagnostic-system.md` (the recurring-blind-spot fix — see Slice 2 doc-update obligation) and document the Pattern-2 residue with bounded candidate sets.

**Dependencies**: Slice 1 inventory (done); both locked designs. No upstream code dependency.

**Exit criteria** (testable):
- Golden corpus snapshot **byte-identical** (diagnostics + fault links) end-to-end.
- No `Enum.TryParse<DiagnosticCode>(...)` remains in the typed-constant path (grep-assert in a test).
- `CreateDiagnostic`'s subtype-fixed arms read `meta.DiagnosticCode`; only the 3 Pattern-2 cases retain dispatch.
- Test: `[StaticallyPreventable]`-derived fault map equals the prior `:412` bijective rows.
- `CIDiagnosticCode` is a single nullable field; no `HasCIVariant`.
- `diagnostic-system.md` has the "Emission shapes" subsection pointing at `DiagnosticCoverageScanner`'s Pattern 1/2/3.
- Full suite green across all 4 projects.

**Estimated effort**: M (~2–3 days).

**Doc-update obligations** (per Slice 2 doc-update enumeration + CLAUDE.md routing):
- `docs/compiler/diagnostic-system.md` — "Emission shapes" subsection (catalog-mediation convention + the `[StaticallyPreventable]`-derived fault map + the scanner Pattern 1/2/3 pointer).
- `docs/compiler/proof-engine.md` — `CreateDiagnostic` reads `ProofRequirementMeta.DiagnosticCode`; fault map derives from the attribute.

### P2 — Slice 3a: Honest taxonomy + dual-emission consolidation

**Goal**: `DiagnosticStage` truthfully names the producer (`Bind`/`Tooling` added, `NameBinder→Type`/`Mcp→Lex` mislabels removed, no precedence semantics); the structural dual-emissions are consolidated to single owners; LS hover surfaces obligations by obligation-presence, not the `Proof` stage label.

**Items in scope**: D2, D4 (+ the hover decouple).

**Decisions required before kicking off**: none (Locked; `CircularComputedField`→Bind settled with a falsifier).

**Step-by-step**:
1. Add `Bind`, `Tooling` to the `DiagnosticStage` enum (`Diagnostic.cs`); update the enum + `Stage`-field doc-comments to "producing-component classification, no precedence" (the comment doc-sync from Slice 3's doc-update).
2. Relabel meta (`Diagnostics.cs`): NameBinder-produced codes → `Bind`; `McpToolInternalError` → `Tooling`. (Value-level codes that become proof-owned are relabeled to `Proof` in **P3**, when the proof-walk actually emits them — not here.) Update `DiagnosticsTests` stage assertions.
3. D4 — `NoInitialState` → Graph only: delete the `TypeChecker.cs:704` emit and the `GraphAnalyzer.cs:85` `HasDiagnostic` guard; confirm the graph path covers every case the type-checker emit did (coverage check).
4. D4 — `CircularComputedField` → Bind only: delete the `Structural.cs:248` DFS detector; run the falsifier coverage check (binder topological-sort detector catches every cycle class the DFS did) before deleting.
5. Hover decouple: `RichHoverFactory` keys obligation-surfacing on obligation-presence rather than `Stage == Proof`.

**Dependencies**: P1 (uniform surface — so the relabel and consolidation don't fight the round-trip/dual-surface). 

**Exit criteria** (testable):
- Test: every NameBinder-produced code reports `Bind`; `McpToolInternalError` reports `Tooling`.
- Test: `NoInitialState` emits exactly once (Graph); `CircularComputedField` emits exactly once (Bind); the binder-detector-sufficiency coverage check passes.
- `GraphAnalyzer.cs:85` `HasDiagnostic` guard deleted.
- LS hover surfaces obligation-bearing diagnostics identically on the corpus, keyed on obligation-presence.
- `precept_compile` per-stage error counts shift as expected (binder errors now `Bind`) — asserted, not silent.
- Full suite green.

**Estimated effort**: S–M (~1–2 days).

**Doc-update obligations**:
- `docs/compiler/diagnostic-system.md § Diagnostic Stages` — `Bind`/`Tooling`, the two distortions resolved.
- `src/Precept/Language/Diagnostic.cs` — `DiagnosticStage`/`Stage`-field doc-comments retitled to producing-component (code-comment doc-sync).
- `docs/tooling/mcp.md` — note the stage-string + per-stage-count change in `precept_compile` output.

## Lightweight phase stubs (later phases)

### P3 — Slice 3b: Proof-walk extension (value-level → proof-owned)
**Goal**: extend the proof-engine default/computed walk (`CollectObligations` / `CollectDefaultObligations` / `CollectArgDefaultObligations`) so the 3 wired Class-O checks (`OutOfRange`, `MaxPlacesExceeded`, the `UnprovedAssignmentQualifierCompatibility` residual) become **proof-owned stamped obligations** instead of type-stage inline emits; the type checker stamps, the proof engine discharges; value-level codes relabel to `Proof`; `OutOfRange` kept proof-owned (D5). **Scope**: D3, D5. **Decisions required**: none (Locked). **Effort**: M (~2–3 days). **Status: Stub — TBD pending P2 completion.** Key risk (from Slice 3 CONCERN): the existing collectors are `InterpolatedTypedConstant`-only — extend to `TypedTypedConstant` without double-emitting on interpolated-with-bounds defaults; behavior-preserving for constants, narrowing-discharge for non-constants.

### P4 — Slice 3c: The ownership analyzer (D1)
**Goal**: new `Precept.Analyzers` analyzer enforcing single-stage ownership — for each emission site (literal + catalog-mediated field reads + the bounded Pattern-2 residue candidate sets), the emitting stage equals `Diagnostics.GetMeta(code).Stage`. **Green with zero allow-list** (true by construction because P1–P3 made the surface uniform + single-owned). Reuses `DiagnosticCoverageScanner`; detection by containing type. **Scope**: D1. **Decisions required**: none (Locked). **Effort**: M (~2 days). **Status: Stub — TBD pending P3 completion** (the analyzer can't be green until value-level codes are proof-owned).

## Definition of done

The Slices 2–3 workstream is complete when:
- The ownership analyzer is **green with zero allow-list entries**; a deliberately wrong-stage `Diagnostics.Create` in a test fixture trips it.
- No `DiagnosticCode` is emitted from two stages (the 3 duals consolidated; `GraphAnalyzer.cs:85` guard gone).
- The 3 wired Class-O checks discharge at the proof stage; constant-default violations still produce a diagnostic (proof-owned), verified on the corpus.
- `DiagnosticStage` has honest `Bind`/`Tooling`; no mislabels; no precedence semantics.
- All doc-touch obligations met (`diagnostic-system.md`, `proof-engine.md`, `Diagnostic.cs` comments, `mcp.md`).
- Full suite green across all 4 projects; `precept_compile` output stable except the **documented** P2 stage-string/count changes and the P3 `OutOfRange` re-stage.
- Scope gate opens: Slice 4 (witness richness) can start; Phase 9 inherits a uniform catalog-mediated surface.

## Discovered during planning

Guard-7 check (plan-touches ⊆ designs' `sources-consulted` ∪ Inventory): **no out-of-design sources.** Every file this plan touches — the per-type validators, `TypeChecker.Expressions.cs`, `ProofEngine.Diagnostics.cs`/`ProofEngine.cs`, `FaultCode.cs`, `Diagnostics.cs`, `Diagnostic.cs`, `Operation.cs`/`Function.cs`/`Operations.cs`/`Functions.cs`, `CI.cs`, `GraphAnalyzer.cs`, `NameBinder.cs`, `Structural.cs`, `TypeChecker.cs`, `RichHoverFactory.cs`, `CompileTool.cs`, `CatalogFormatters.cs`, `DiagnosticCoverageScanner.cs`, the new analyzer, and the test projects — appears in the two designs' Inventory / `sources-consulted` / doc-update, or in the deep-dive evidence index they cite. No design re-lock required.

## Plan update protocol

- On phase completion: flip its row to ✅ in the Phase summary; promote the next stub to heavyweight; update the readiness-plan Phase 8 slice log.
- If a behavior-preservation snapshot diff appears in P1 (or P3 constants), **stop** — it means a step changed semantics; treat as a design-falsifier hit, not a test to update.
- If new work surfaces, add it to the appropriate phase or a new stub; if it touches a surface neither design cited, re-lock the affected design (don't silently absorb).
- Execution rigor (failing-test-first, fresh-worktree agent, adversarial diff review, vertical slices) is `/lifecycle-4-execute`'s domain — this plan only sequences.
