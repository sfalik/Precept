---
status: Locked 2026-06-01 (amended 2026-06-01 — D6 added: MaxPlacesExceeded stays Type; D4 expanded: name-resolution family UndeclaredField/State/Event → Bind, 6 duals not 3)
phase-target: Phase 8 Slice 3 (diagnostic-emission ownership architecture)
comparable-systems-research-status: partial — load-bearing precedent is in-tree (the Precept000x analyzers + DiagnosticCoverageScanner, read for this design); Roslyn DiagnosticDescriptor cited as an external parallel from general API knowledge. No irreversible decision, so the research-adequacy gate does not fire.
sources-consulted:
  - docs/Working/phase8-slice1-emission-inventory-2026-05-31.md: the inventory — single factory, ~240 sites, Class O/S/F, the 3 dual-emissions, the spec-gap future-O
  - src/Precept/Pipeline/ProofEngine.cs: CollectObligations — already walks computed-field exprs (236-240), field defaults (244), arg defaults (247) for interval containment; the stamp/discharge loop (149-160)
  - src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs: the Site-A split (185-234) — stamp when proof-walked, inline-emit residual otherwise
  - src/Precept/Language/Diagnostics.cs: DiagnosticMeta.Stage field (the ownership key)
  - src/Precept.Analyzers/Precept0003DiagnosticMustUseCreate.cs, Precept0027DiagnosticEmissionCoverage.cs, DiagnosticCoverageScanner.cs, DiagnosticCoverageAllowLists.cs: the established emission-site analyzer + scanner infrastructure to extend
  - docs/compiler/proof-engine.md: Decision 3 (stamp-vs-discharge contract); the catalog-driven obligation model
  - docs/compiler/diagnostic-system.md: DiagnosticStage as producing-component classification (corrected 2026-06-01)
  - docs/philosophy.md / precept-language-spec.md § 0.1: prevention-not-detection, compile-time structural impossibility
  - docs/language/business-domain-types.md:1581-1590: the three-point maxplaces enforcement model (Decision 6) — point 1 compile-time, points 2–3 runtime
  - src/Precept/Pipeline/TypeChecker.cs:1078 + src/Precept/Language/ProofRequirement.cs: maxplaces is static-only at compile time; no decimal-places ProofRequirement kind (Decision 6)
  - name-resolution investigation 2026-06-01: NameBinder Undeclared* (:577,714,734,754,775) + type-checker twins (Normalization.cs:208,328,455; Expressions.Callables.cs:394,962; Expressions.cs:949; TypeChecker.cs:1150,1214,1300) — all symbol-table existence checks, none type-gated; double-emission documented at TypeCheckerTransitionTests.cs:196-199 (D4 amendment)
---

> **SUPERSEDED 2026-06-11** — replaced by [`compiler-readiness-plan-2026-06-11.md`](../compiler-readiness-plan-2026-06-11.md). Retained for history; all still-valid obligations were mined into that plan (see its `-appendices/working-docs-triage.md`). Do **not** treat as current strategy.


# Slice 3 — Diagnostic-Emission Ownership Architecture

*(The `DiagnosticStage` taxonomy work — honest stage values, no mislabeling — is the prerequisite half of this slice, folded in here rather than tracked separately.)*

## Goal

When done, **every `DiagnosticCode` is owned by exactly one pipeline stage, declared in the catalog (`DiagnosticMeta.Stage`) and enforced at compile time by a Roslyn analyzer** — emitting a code from any stage other than its owner is a build error. Enforcement is total because the **precursor slice** (`phase8-slice2-code-mediation-2026-06-01.md`) first standardizes every code selection into a uniform shape — a literal or a single catalog-meta `DiagnosticCode` field read — leaving only a small, documented Pattern-2 residue (lexer mode-switch, proof context cases) with bounded candidate sets. The analyzer reads that uniform surface; the guarantee is by construction, not coincidental or literal-only. Value-level codes are owned by the proof stage (the type checker stamps, the proof engine discharges); the three current cross-stage dual-emissions are resolved to single owners. Demonstrated by: the new analyzer is green with zero allow-list entries, and the `GraphAnalyzer.cs:85` ad-hoc `HasDiagnostic` dedup guard is deleted (no longer needed because no code is emitted twice). **No runtime reconciliation or dedup step exists.**

## Scope

- **In scope**: single-stage ownership as a catalog-declared, analyzer-enforced invariant; the honest `DiagnosticStage` taxonomy that makes the ownership key trustworthy (`Bind`/`Tooling`, no mislabeling); extending the proof-engine default/computed walk so value-level checks are stamped obligations (proof-owned) rather than type-stage inline emits; resolving the three dual-emissions to single owners; decoupling LS hover from the `Proof` stage label.
- **Out of scope**: diagnostic *message wording* / the `NonAssociativeComparison` category-vs-stage tension (Phase 9 diagnostic-identity). Incremental/query recompilation (closed NO — `research/architecture/README.md` #1).
- **Deferred to future**: none. (The former "reconciliation/dedup step" and the "allow-list the Site-A residual" shortcut are both **removed** — ownership is enforced with no escape hatch.)

## Philosophy Alignment

| Principle | Affected? | How served (cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | Dual-/wrong-stage emission becomes a **compile error** (analyzer), not a runtime cleanup — invalid emission is structurally impossible (`philosophy.md`; spec § 0.1 #1). | N/A | N/A |
| 2. One file, complete rules | N | No `.precept` surface change. | N/A | N/A |
| 3. Determinism | Y | One code → one stage → one emission site removes the ordering/dedup nondeterminism the reconciliation step would have managed. | N/A | N/A |
| 4. Full inspectability | Y | Honest `Stage` = accurate provenance in MCP/CLI/hover; the proof ledger remains the single record of value-level discharge. | N/A | N/A |
| 5. Keyword-anchored readability | N | No surface syntax. | N/A | N/A |
| 6. Governance not validation | N | N/A. | N/A | N/A |
| 7. Compile-time totality | Y | Routing **all** value-level checks (incl. the constant-default ones) through the stamp/discharge channel strengthens totality coverage — nothing value-level escapes the proof stage. | N/A | N/A |
| 8. Honesty about approximation | Y (thematic) | Stage stops misrepresenting which component produced a diagnostic. | N/A | N/A |
| 9. Mandatory rationale | N | N/A. | N/A | N/A |
| 10. Static semantic checking | Y | The ownership invariant is itself a statically-checked property of the compiler (analyzer); the value-level routing is statically enforced, not doc-prose. | N/A | N/A |
| 11. Static completeness | Y | Moving the constant-default checks into the discharge channel cannot weaken completeness — it moves checks *into* the channel that proves safety, never out. | The proof engine must discharge the now-stamped constant-default obligations as cheaply as the old inline check (a constant vs a bound). | Accept that a static-constant obligation is discharged by the numeric strategy (trivial) rather than an inline compare — same outcome, one channel. |

**Companion commitments**: Stateless-first-class / Domain-expert-primary-author — unaffected (internal). This slice is a strong instance of the prevention + catalog-driven + inspectability commitments acting together.

## Language Design Grounding

**N/A** — no language surface (no token/keyword/type/operator/modifier/construct/accessor/expression form). Compiler-internal emission architecture + a catalog classification field + a build-time analyzer. (Guard 2 not triggered.)

## Audience and Teachability

**N/A** — no domain-author-facing surface. Author-adjacent effect is more accurate provenance and (via the value-level routing) potentially narrowing-discharged diagnostics where today a constant-only inline check fired. (Guard 3 not triggered.)

## Semantic Rules

Decision 3 relocates value-level checks into the stamp/discharge channel, so the obligation rules are stated.

**Stamping rule (extension of `proof-engine.md` Decision 3).** For a field/arg **default**, modifier **bound**, or **computed** expression `e` carrying a value-level requirement `r` (numeric-modifier bound, assignment-qualifier compatibility, presence), the type checker stamps `r` onto `e`'s typed node instead of emitting inline:

```
type checker resolves e, derives requirement r from catalog metadata
  → stamps r onto the TypedExpression (does NOT emit a diagnostic)
proof engine's existing default/computed walk (CollectObligations: lines 236-247)
  collects r, attempts discharge:
     discharged (constant trivially, or via guard-narrowing) → no diagnostic
     not discharged                                          → proof-stage diagnostic
```

The hooks already exist (`CollectDefaultObligations`, `CollectArgDefaultObligations`, the computed-field `WalkExpression`); today they collect only interval-containment obligations. The change adds the numeric-modifier, assignment-qualifier, and presence requirements to what they stamp/collect.

**Soundness preservation (Principles 7/10/11).** Coverage is *strengthened*, not weakened: every value-level check that today emits inline at the type stage moves *into* the channel the proof engine discharges. For a static-constant default the proof engine's numeric strategy discharges the obligation deterministically (the same compare the inline check did); for a non-constant default it may now discharge via narrowing (a latent improvement). No expression form or typing rule changes. No opaque solver is introduced — the existing finite strategy set discharges these obligations (`proof-engine.md` opaque-solver rejection respected).

## Architecture Grounding

### Precept-internal placement

**Layer placement.** Three layers, each doing its proper job:
1. **Catalog** (`DiagnosticMeta.Stage`) — declares the single owning stage per code. This is the ownership source of truth.
2. **Pipeline** — the type checker stamps value-level obligations (Decision 3); each stage emits only its own codes.
3. **Build-time analyzer** (`Precept.Analyzers`) — enforces that every `Diagnostics.Create(DiagnosticCode.X, …)` site sits in `X`'s owning stage.

No runtime reconciliation layer — the invariant is enforced at compile time, which is where Precept puts structural guarantees.

**Cross-component propagation:**
- **Runtime (parser/type checker/evaluator/diagnostics):** type checker stops inline-emitting the three wired Class-O codes for defaults/bounds, stamps obligations instead; proof engine's default/computed walk extended to collect them; `DiagnosticStage` enum gains `Bind`/`Tooling`; meta relabels the two distorted code sets; the six dual-emissions (`NoInitialState`→Graph; `CircularComputedField` + the `Undeclared*` name-resolution family→Bind, type checker defers to binder markers) consolidated to single owners — fixing a latent double-emission of `Undeclared*`.
- **Tooling (syntax/completions/hover/semantic tokens):** LS hover keys obligation-surfacing on obligation-presence, not `Stage == Proof` (`RichHoverFactory.cs:245` already half-does this).
- **MCP (vocabulary/DTOs/tool output):** `CatalogFormatters.cs:600` serializes the (now honest) stage string; the constant-default numeric code's identity may change (Decision 5); **and `precept_compile`'s per-stage error count changes** — `CompileTool.cs:70` computes `typeErrors` as `Count(Stage == Type)`, so moving binder codes `Type→Bind` (D2) and value-level codes `Type→Proof` (D3) *reduces* the reported type-error count and shifts those into bind/proof counts. All visible in `precept_compile` output. No external consumers (pre-release).

**Breaking changes.** Stage strings change for relabeled codes; `precept_compile`'s per-stage error counts shift (binder→Bind, value-level→Proof); possibly one diagnostic-code identity change (Decision 5). No `.precept` surface change. No external consumers (pre-release).

### External architectural precedent

**Roslyn analyzers enforcing compiler-codebase invariants** is the direct precedent — and Precept already runs 27 such analyzers (`Precept0001`–`Precept0027`), including `Precept0003DiagnosticMustUseCreate` (every diagnostic must use the factory — *why* Slice 1's single-factory closure held) and `Precept0027DiagnosticEmissionCoverage` (emit-or-allow-list coverage), built on a shared `DiagnosticCoverageScanner` that already locates every emission site and the code it emits. *What Precept takes*: the established pattern of encoding a compiler invariant as a build-time analyzer over emission sites. *What Precept diverges on / adds*: a new sibling analyzer on the *ownership* axis (site's enclosing stage must equal the code's catalog-declared owning stage), where `Precept0027` covers the *coverage* axis. Roslyn's `DiagnosticDescriptor` (which separates `Id` / `Category` / `DefaultSeverity` as independent fields, with suppression configured separately) is the external parallel for keeping `DiagnosticStage` a pure component classification with no precedence semantics — cited from general Roslyn-API knowledge, not a fetched excerpt. **The load-bearing, verified precedent is in-tree**: the 27 `Precept000x` analyzers + the shared `DiagnosticCoverageScanner` (read for this design) establish "enforce a compiler invariant via a build-time analyzer over emission sites" as an existing, working pattern here; the new ownership analyzer is a sibling on the ownership axis. Roslyn corroborates the axis-separation principle but the design does not rest on it.

## Inventory of what will be built

- **`DiagnosticStage` enum** (`Diagnostic.cs`): add `Bind`, `Tooling`; ordinal carries no semantics.
- **Meta table** (`Diagnostics.cs`): set each code's `Stage` to its true owning stage — relabel binder codes `→ Bind`, `McpToolInternalError → Tooling`; proof-dischargeable value-level codes (`OutOfRange`, `UnprovedAssignmentQualifierCompatibility`, future `NullInNonNullableContext`) `→ Proof`. **`MaxPlacesExceeded` stays `Type`** (Decision 6 — no compile-time proof obligation).
- **New analyzer** `PreceptNNNN DiagnosticStageOwnership` (`Precept.Analyzers`), reading the uniform surface the precursor produces (verified against `DiagnosticCoverageScanner`):
  - *Literal sites* — `Diagnostics.Create(DiagnosticCode.X, …)`: compare the emitting site's **containing type** (`TypeChecker`/`GraphAnalyzer`/`ProofEngine`/… — namespace is NOT a discriminator; all pipeline files share `namespace Precept.Pipeline`, so detection is by `ContainingType`, the same mechanism `Precept0003` already uses) to `Diagnostics.GetMeta(X).Stage`; flag mismatch.
  - *Catalog-mediated sites* — after the precursor, every dynamic code-selection is a read of a single catalog-meta `DiagnosticCode` field (`CIDiagnosticCode`, `Format/SemanticErrorCode`, `ProofRequirementMeta.DiagnosticCode`). The analyzer asserts the **catalog invariant**: every code reachable from such a field has `Stage` equal to the consuming stage (CI ⇒ `Type`; proof ⇒ `Proof`).
  - *Pattern-2 residue* — the precursor documents the bounded candidate set for each context-determined site (lexer mode-switch; proof `Numeric`/`KeyPresence`/`QualifierChain` cases); the analyzer checks each set's codes are owned by the emitting stage. Bounded, so still enforceable with no allow-list.
- **Type checker**: replace inline value-level emits for defaults/bounds with obligation stamping — `AssignmentQualifiers.cs` residual branch; `Modifiers.cs` `TryReportNumericViolation`/`OutOfRange`. (`TypeChecker.cs` `MaxPlacesExceeded` is **not** relocated — Decision 6; it stays an inline Type-stage static check.)
- **Proof engine**: extend `CollectDefaultObligations`/`CollectArgDefaultObligations`/computed-field walk to collect the newly-stamped numeric-modifier + assignment-qualifier + presence obligations.
- **Dual-emission consolidation** (six duals — D4): `NoInitialState` → Graph only (delete the `TypeChecker.cs:704` emit + the `GraphAnalyzer.cs:85` `HasDiagnostic` guard); `CircularComputedField` → Bind (delete `Structural.cs:248`); **the name-resolution family `UndeclaredField`/`UndeclaredState`/`UndeclaredEvent` → Bind** — the type checker stops emitting `Undeclared*` (~9 sites: `Normalization.cs:208,328,455`, `Expressions.Callables.cs:394,962`, `Expressions.cs:949`, `TypeChecker.cs:1150,1214,1300`), deferring to the binder's `UnresolvedTarget` markers; the binder's state-list resolution is lifted from first-name-only to full-list. Fixes the current double-emission (tolerant tests tightened to exact counts).
- **LS hover**: `RichHoverFactory` keys on obligation-presence not `Stage == Proof`.
- **Tests**: ownership analyzer tests; `DiagnosticsTests` stage-assertion updates; proof-stage discharge tests for the relocated value-level checks; `NoInitialState`/`CircularComputedField` single-emission tests.
- **No** reconciliation/dedup code.

## Decisions

### Decision 1: Single-stage ownership — catalog-declared, analyzer-enforced; no runtime reconciliation/dedup

**Stakes**: high (defines the emission contract + adds a build-time analyzer; touches every stage's emission discipline).

- **Rationale**: dedup treats the symptom (a code emitted twice) at runtime; ownership removes the cause at compile time. `DiagnosticMeta.Stage` already exists; making it the authoritative *owning* stage and enforcing with an analyzer makes wrong-stage emission structurally impossible — the prevention-not-detection commitment applied to the compiler's own code.
- **Tradeoff accepted**: every emission site is now constrained — adding a `Diagnostics.Create` in the "wrong" stage fails the build until the catalog or the call moves. That friction is the point (it's the guarantee), but it means contributors must know a code's owning stage. Mitigated: the analyzer error names the owning stage.
- **Alternatives considered**:
  - *Runtime reconciliation/dedup step (my earlier hybrid).* Rejected — a design smell: it normalizes dual-emission instead of forbidding it, adds a runtime pass, and enforces nothing at authoring time.
  - *Doc-prose convention ("emit each code from one stage").* Rejected — unenforced; drifts immediately (the 3 current duals prove it).
- **Precedent**: `Precept0003`/`Precept0027` + `DiagnosticCoverageScanner` (in-tree analyzer-enforced emission invariants); Roslyn analyzer SDK generally.
- **Sources consulted for this decision**:
  - `src/Precept.Analyzers/DiagnosticCoverageScanner.cs` — "Pattern 1: `Diagnostics.Create(DiagnosticCode.X, ...)` … Pattern 2: `CIDiagnosticCode: DiagnosticCode.X` … Pattern 3: ProofEngine dispatch branches" (the scanner already finds every site).
  - `src/Precept.Analyzers/Precept0003DiagnosticMustUseCreate.cs` — existing emission-site analyzer (the factory invariant).
  - Spec-first: `grep -in "emission|DiagnosticStage" docs/language/precept-language-spec.md` — spec does not lock emission architecture; `proof-engine.md` Decision 3 (stamp/discharge) is respected, not changed.
- **Strongest counter-evidence**: a code might *genuinely* need two producers (the Site-A case is real today). *Response*: Decision 3 removes that need by relocating value-level checks to a single owner (Proof); Decision 4 removes the structural duals. With "no shortcuts / no allow-list," every code is genuinely single-owned — the counter-case is engineered away rather than tolerated.
- **Reversibility**: Hard (the analyzer + ownership labels touch every stage), but no public/author contract — reversible with bounded internal effort.
- **Blast radius**: `Diagnostics.cs` (Stage labels), new analyzer + tests, the type-checker/proof-engine/graph/binder emission-site refactors, `DiagnosticsTests`. No samples, no external consumers.

### Decision 2: Honest stage taxonomy — `Bind`/`Tooling`, relabel the two distortions; Stage is owning-component, no precedence

**Stakes**: medium (the ownership key must be honest; touches the enum that flows to MCP/CLI output).

- **Rationale**: ownership enforcement is only meaningful if `Stage` truthfully names the producer. `NameBinder→Type` and `Mcp→Lex` would make the analyzer either wrong or force the binder's codes to be "owned" by Type. Honest values are the prerequisite.
- **Tradeoff accepted**: MCP/CLI stage strings change for relabeled codes; `DiagnosticsTests` churns. Bounded; provenance-correctness improvement; no external consumers.
- **Alternatives considered**: *Keep distortions, special-case them in the analyzer.* Rejected — encodes the lie into the enforcement. *Free-form category string (Roslyn-style).* Rejected — Precept's pipeline is finite; a closed honest enum preserves the exhaustiveness the catalog relies on.
- **Precedent**: Roslyn `DiagnosticDescriptor` axis separation; the corrected `diagnostic-system.md § Diagnostic Stages`.
- **Sources consulted for this decision**: `src/Precept/Language/Diagnostics.cs` `McpToolInternalError` stage comment (emitted outside the pipeline); `tools/Precept.Mcp/CatalogFormatters.cs:600` (MCP exposes Stage).

### Decision 3: Value-level checks are proof-owned; extend the proof-walk to stamp default/bound/computed obligations — no allow-list shortcut

**Stakes**: high (touches proof-obligation placement and the type-checker→proof-engine contract).

- **Scope (amended 2026-06-01)**: this relocation applies to **two** value-level checks — `OutOfRange` and the `UnprovedAssignmentQualifierCompatibility` residual. `MaxPlacesExceeded` was originally in this set but is **excluded** — it has no compile-time proof obligation (see Decision 6).
- **Rationale**: these value-level codes emit inline at the type stage *only* because the proof engine's default/computed walk doesn't yet stamp their obligation kinds, **and** each maps to an existing, proof-narrowable obligation: `OutOfRange` → `NumericProofRequirement`/`ModifierRequirement` (min/max/positive are guard-narrowable; `proof-engine.md` Decision 5 modifier-proof), the residual → `AssignmentQualifierProofRequirement` (the Site-A pattern). The hooks already exist (`CollectObligations` walks computed exprs + field/arg defaults for interval containment); extending what they stamp makes the proof stage the sole owner of the *proof-dischargeable* value-level codes, which the Decision-1 analyzer then enforces.
- **Tradeoff accepted**: more work than allow-listing the Site-A residual, and it changes the constant-default numeric check's diagnostic identity (Decision 5). Accepted because the owner chose "no shortcuts" — the allow-list would have been permanent debt. **Caveat (collector shape gap)**: "extend what the hooks stamp" understates the work — `CollectDefaultObligations` (`ProofEngine.Analysis.cs:405`) today handles *only* `InterpolatedTypedConstant` defaults and bails on `IsUnbounded` (`:414`), whereas the inline `OutOfRange` fires on any `TryGetStaticMagnitude` success (incl. plain `TypedTypedConstant`). So the extension must (a) add the `TypedTypedConstant` default shape to the collector and (b) **not double-emit** on an `InterpolatedTypedConstant`-with-bounds default that the existing `IntervalContainment` walk already covers. The removed reconciliation step would have masked a double-emit here; with no dedup, the collector must be correct by construction.
- **Alternatives considered**: *Allow-list the Site-A dual until later.* Rejected by owner ("no shortcuts"). *Leave constant-default checks inline and exempt them.* Rejected — that's the Class-O-in-disguise the analyzer exists to catch.
- **Precedent**: `proof-engine.md` Decision 3 (stamp/discharge); the existing `CollectDefaultObligations` interval-containment walk is the in-tree template for stamping default obligations; the Site-A `set`-action discharge (`AssignmentQualifiers.cs:205-214`) is the template for the qualifier obligation.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/ProofEngine.cs:234-247` — the existing default/computed walk hooks.
  - `src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs:199-234` — the stamp-vs-inline split to extend to non-walked contexts.
- **Strongest counter-evidence**: extending the walk could surface obligations on defaults that previously compiled clean (behavior change). *Response*: that's correct-by-construction — if a default violated a value-level requirement, the inline check already caught it (`OutOfRange` etc.); the obligation discharges identically for constants. Net behavior is preserved for constants and improved (narrowing) for non-constants. Verified against the sample corpus is an acceptance criterion.
- **Reversibility**: Hard (proof-engine + type-checker contract), no public/author contract.
- **Blast radius**: `ProofEngine.cs` default/computed walk, type-checker stamping sites, proof-stage discharge tests. Couples to Decision 5 (code identity).

### Decision 4: Resolve the dual-emissions to single owners (amended 2026-06-01 — name-resolution family added)

**Stakes**: medium → (amendment) the name-resolution family is a larger consolidation than the original two structural duals.

**The real dual-emission set is six, not three** (Slice 1 undercounted — see corrected inventory). Each must become single-owned for the Decision-1 zero-allow-list analyzer:
1. `NoInitialState` (Type + Graph) → **Graph**.
2. `CircularComputedField` (Bind + Type) → **Bind**.
3–5. `UndeclaredField` / `UndeclaredState` / `UndeclaredEvent` (Bind + Type, ~10 sites) → **Bind** (the name-resolution family — added by this amendment).
6. `UnprovedAssignmentQualifierCompatibility` (Type + Proof) → resolved by D3 (proof-walk relocation), not here.

- **Rationale**: each code is detected in two stages today; single-stage ownership requires one owner. A focused investigation (2026-06-01) of the name-resolution family found the type-checker emissions are **redundant re-resolutions, not type-gated** — every `Undeclared*` site is a pure symbol-table existence check (the binder's three dictionaries suffice); the only genuinely type-dependent member-access path emits `InvalidMemberAccess`, a different code. So all of it consolidates to the **binder**, which runs first and already produces `UnresolvedTarget` markers. Mechanism: the type checker keeps *looking names up* for typing but **stops emitting `Undeclared*`**, deferring to the binder's markers; the binder's state-*list* resolution is lifted from first-name-only (`SlotValue.cs:84` compat getter) to full-list. `NoInitialState` → Graph and `CircularComputedField` → Bind are graph/name-graph properties (the binder already computes the cycle via topological sort at `NameBinder.cs:305`).
- **Tradeoff accepted**: this is **not behavior-neutral** — it *fixes a latent double-emission bug*. Today an undeclared name can produce two diagnostics (one per stage), documented in `TypeCheckerTransitionTests.cs:196-199` (*"Missing1 receives two UndeclaredState diagnostics, one from each pipeline stage"*); the suite tolerates it with `≥2`/`Contain` matchers. Consolidation makes it emit once — author-visible (fewer duplicates), a quality improvement, but the tolerant tests must be tightened to exact counts. Must verify the binder owner catches every case its dropped type-checker twin did (coverage check before deleting each type-checker emit).
- **Alternatives considered**: *Distinct codes per stage* — rejected (same author-facing meaning; noise). *A scoped allow-list for the name-resolution family* — rejected: the investigation showed consolidation is feasible (not type-gated), so an allow-list would be an unnecessary hole in the zero-allow-list guarantee. *Keep first-name-only in the binder* — rejected: it would leave the type checker as the only full-list resolver, re-creating the dual.
- **Precedent**: the binder is already the first-pass resolver (`NameBinder.cs` `ResolveReferences`); `GraphAnalyzer.cs` already owns the other initial-state/reachability diagnostics; the investigation (this session) grounds the type-free claim per-site.
- **Sources consulted for this decision**:
  - `docs/Working/phase8-slice1-emission-inventory-2026-05-31.md` §5 (the dual inventory, now corrected to 6).
  - Investigation (2026-06-01): NameBinder `Undeclared*` at `:577,714,734,754,775` (symbol-table-only); type-checker twins at `Normalization.cs:208,328,455`, `Expressions.Callables.cs:394,962`, `Expressions.cs:949`, `TypeChecker.cs:1150,1214,1300` — all existence checks, none type-gated. Double-emission documented at `test/Precept.Tests/.../TypeCheckerTransitionTests.cs:196-199`. Binder produces `UnresolvedTarget` markers at `NameBinder.cs:717,737,757,778`.
  - `GraphAnalyzer.cs:85-88`, `TypeChecker.cs:704` (`NoInitialState`); `NameBinder.cs:305`, `Structural.cs:248` (`CircularComputedField`).
- **Falsifier**: if any binder owner misses a case its dropped type-checker twin caught (a name reachable only in a type-checker context the binder doesn't walk), that emit can't be dropped — the coverage check gates each deletion. For `CircularComputedField`: if the binder's topological sort misses a cycle class the Type-stage DFS catches, Type owns instead.
- **Note (Phase-9/identity)**: `Event.notAnArg` is mis-coded — `UndeclaredArg` from the binder (`:577`) vs `UndeclaredField` from the type checker (`Callables.cs:962`). Consolidating to the binder naturally resolves it to `UndeclaredArg`; flag the identity reconciliation for Phase 9 if it surfaces author-facing.

### Decision 5: Diagnostic identity for the relocated constant-default numeric check — keep `OutOfRange`, re-own to Proof

**Stakes**: medium (touches a diagnostic code visible in MCP/CLI; Phase 9 seam).

- **Rationale**: routing the constant-default numeric check through the proof stage means its unproved diagnostic is produced by the proof engine. **Decision: keep `OutOfRange` (PRE0079) as the code, re-owned to Proof** — minimizes author-facing identity churn (same code authors already see, now stage-correct). Whether `OutOfRange` ultimately consolidates with the other proof-stage numeric codes (`NumericOverflow` / `UnprovedModifierRequirement`) is a *catalog-completeness* question owned downstream by **Phase 9 / F-LANG-SPEC-12** ("wire `OutOfRange` constant-literal check or retire") — an emit-or-retire concern, not an emission-architecture one — so it does not block this design.
- **Tradeoff accepted**: keeping `OutOfRange` proof-owned means a numeric-bound violation surfaces under two codes depending on context (modifier-default vs operand) unless consolidated — a Phase 9 question.
- **Alternatives considered**: *Retire `OutOfRange` for `NumericOverflow`.* Possible, but a bigger author-facing change; defer the call to Phase 9 with this design flagging the coupling.
- **Precedent**: the PRE0141 re-stage (a code moved type→proof while keeping identity) is the in-tree template.
- **Sources consulted for this decision**: `docs/compiler/diagnostic-system.md:178` (the PRE0141 re-stage precedent); `phase8-slice1-emission-inventory-2026-05-31.md` §3.

### Decision 6: `MaxPlacesExceeded` stays type-stage-owned — it has no compile-time proof obligation (amendment, 2026-06-01)

**Stakes**: medium (refines D3's scope and the relabel; touches the analyzer's ownership set; reversible, pre-release).

- **Rationale**: `maxplaces` enforcement is spec'd at three points (`business-domain-types.md:1581-1590`): point 1 (static literal) at **compile time**; points 2–3 (event-arg input, arithmetic-result-at-`set`) at **runtime**. A non-static value's decimal-place count isn't statically known, so the spec defers it to the runtime boundary + author `round()`, *not* to a compile-time proof. The compile-time `MaxPlacesExceeded` is therefore inherently the static-magnitude case (it bails on `!TryGetStaticMagnitude`), correctly produced at the **Type** stage. The proof engine has **no maxplaces role** — there is no proof-dischargeable obligation to relocate (no `ProofRequirement` kind for decimal-places, and rightly so). So `MaxPlacesExceeded` stays `Stage = Type`; the ownership analyzer (Decision 1) accepts it as legitimately Type-owned.
- **Tradeoff accepted**: a slight asymmetry with `OutOfRange` — both are emitted on static magnitudes today, but `OutOfRange`'s underlying obligation (numeric bounds) is proof-narrowable for non-static values (a guard can discharge `positive`), so it relocates to Proof; `maxplaces`'s non-static case is runtime-enforced, not provable, so it stays Type. The asymmetry is correct (it tracks proof-dischargeability), not arbitrary.
- **Alternatives considered**: (a) *Relocate `MaxPlacesExceeded` to Proof per the original D3* — rejected: no proof obligation exists or should (decimal-places of a non-static value isn't statically provable; inventing a places-`ProofRequirement` + strategy would be over-cataloging for the static case and wrong for the non-static case, which the spec assigns to runtime). (b) *Relabel `→ Proof` but keep emitting from Type* — rejected: that's the wrong-stage emission the analyzer forbids.
- **Precedent**: `business-domain-types.md:1581-1590` (the three-point maxplaces enforcement model, points 2–3 runtime); the typed-constant content codes are the in-tree precedent for static-value-form checks owned at the type stage; deep-dive `phase8-slice2-dispatch-deepdive-2026-06-01.md` §7 (don't over-catalog).
- **Sources consulted for this decision**:
  - `docs/language/business-domain-types.md:1581-1590` — "`maxplaces` is checked at three points: 1. Literal assignment (compile time) … 2. Event-arg input (runtime boundary) … 3. Arithmetic result assignment (runtime) … the author must apply `round()`."
  - `src/Precept/Pipeline/TypeChecker.cs:1078` — `ValidateMaxplaces` bails `if (!TypedExpressionMagnitude.TryGetStaticMagnitude(resolved, out var magnitude)) return;` — static-only.
  - `src/Precept/Language/ProofRequirement.cs` — the 12 `ProofRequirement` kinds; none is decimal-places/precision.
- **Known gap (not Slice 3 scope)**: maxplaces points 2–3 (runtime boundary + arithmetic-result enforcement) appear unimplemented (the runtime is still stub). That is **runtime-phase** work (Phase 12) and a spec-conformance item — *not* a proof-engine or emission-architecture concern. Flagged here so it isn't mistaken for a Slice 3 obligation.
- **Superseded-by (if built)**: this decision (maxplaces = Type-owned, no proof obligation) is the **interim**. **Phase 8 Slice 5** (`maxplaces` compile-time precision proof — a decimal-scale abstract domain + a `PrecisionContainmentProofRequirement`) would give maxplaces a real compile-time proof obligation and relocate it to `Proof` (like `OutOfRange`), making it prevention-not-detection. If Slice 5 ships, D6 is superseded. D6 stands until then.

## Falsifiers

- If the ownership analyzer cannot reach green without ≥1 allow-list entry after Decisions 3+4 land, then some code genuinely needs two owners and the "single-ownership, no shortcuts" premise is wrong — revisit whether a small allow-list is in fact necessary.
- If extending the proof-walk changes *which* diagnostics fire on any `samples/` file (beyond code-identity per Decision 5), the stamping is not behavior-preserving — reconsider Decision 3. Concrete regression to test: an `InterpolatedTypedConstant` default with declared bounds must **not** fire twice (the existing `IntervalContainment` walk + the newly-stamped numeric obligation) — double-emission there means the collector extension isn't dedup-correct.
- If `precept_compile`'s per-stage error counts shift in a way a consumer relied on (they don't today — pre-release, no external consumers), the count change was a real break; confirm no in-tree test/tool asserts the old `Stage==Type` totals before shipping.
- **Revisit trigger (Stage-as-ownership coupling)**: making `DiagnosticMeta.Stage` load-bearing for an analyzer couples a descriptive field to a compile-time gate. If a future feature legitimately needs a code emitted from two stages, or if the `Type` stage is ever subdivided, re-examine whether ownership should move to a dedicated `OwningStage` field decoupled from the descriptive taxonomy.
- If the analyzer's enclosing-stage detection misclassifies a shared helper that emits on behalf of a stage (a helper in a neutral namespace), stage detection by containing-type is too crude and needs an explicit per-site stage annotation.
- If deleting the `NoInitialState` type-checker emit drops the diagnostic on any precept the graph analyzer doesn't cover, the owner assignment is wrong — Graph doesn't fully own it.

## Acceptance criteria

- New ownership analyzer is green with **zero allow-list entries**; adding a deliberately wrong-stage `Diagnostics.Create` in a test fixture trips it.
- `GraphAnalyzer.cs:85` `HasDiagnostic` guard deleted; `NoInitialState` emits once (graph only); `CircularComputedField` emits from one owner — both asserted by tests.
- The three wired Class-O checks discharge at the proof stage; a constant default that violates a numeric modifier still produces a diagnostic (same condition, proof-owned), verified on the sample corpus.
- `DiagnosticStage` has `Bind`/`Tooling`; binder codes report `Bind`, `McpToolInternalError` reports `Tooling`.
- LS hover surfaces obligation-bearing diagnostics on the corpus identically, keyed on obligation-presence.
- Full suite green across all 4 projects.

## Dependencies

- **Upstream**: the **precursor slice** (`phase8-slice2-code-mediation-2026-06-01.md` — standardizes code selection into the uniform surface the analyzer reads); Slice 1 inventory (done); `proof-engine.md` Decision 3 (locked); the `DiagnosticCoverageScanner` infrastructure (exists). Owner alignment on this reshaped design (this Draft).
- **Downstream**: Phase 9 (emit-or-retire / diagnostic identity — Decision 5 hands off the `OutOfRange` identity question); a cleaner base for Slice 4 (witness richness builds on the unified proof emission point).

## Doc-update enumeration

- `docs/compiler/diagnostic-system.md` § Diagnostic Stages — `Bind`/`Tooling`, the ownership invariant, the analyzer reference.
- `src/Precept/Language/Diagnostic.cs` — the `DiagnosticStage` enum + `Stage`-field doc-comments still frame Stage as "WHEN it fires" (temporal/ordering); retitle to producing-component-classification to match Decision 2 (code-comment doc-sync).
- `docs/compiler/proof-engine.md` § Decision 3 — value-level emission is the exclusive obligation-channel path; the default/computed walk now stamps numeric-modifier/qualifier/presence obligations.
- `docs/compiler/README.md` — the new ownership analyzer in the analyzer roster.
- `docs/tooling/mcp.md` — if the stage-string / `OutOfRange` identity change warrants a note.
- `docs/Working/compiler-readiness-plan-2026-05-24.md` — Phase 8 slice log.

## Operational dimensions

- **Observability** (triggered): provenance becomes accurate; the analyzer makes the emission map auditable at build time. The proof ledger remains the discharge record. Determinism improves (one site per code).
- Security: N/A. Evolvability: N/A (the enum + analyzer are internal).

## Open questions

None — resolved at lock (2026-06-01):
- *Analyzer stage-detection* → **containing type** (decided; namespace is not a discriminator). The only residual is a shared-helper emission whose containing type isn't a stage class — covered by Falsifier 3; the helper-inventory check is an execution task, not a design open question.
- *`CircularComputedField` owner* → resolved in Decision 4 (**Bind**, with a falsifier guarding the binder-detector-sufficiency check).
- *`OutOfRange` identity* → resolved in Decision 5 (**keep, proof-owned**); the downstream consolidate-or-retire question is owned by Phase 9 / F-LANG-SPEC-12 (see Dependencies), not this design.

None required an owner decision — all execution-placement or downstream, with the architecture owner-aligned and the design adversarially reviewed.
