---
status: Locked 2026-06-01
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
---

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
- **Runtime (parser/type checker/evaluator/diagnostics):** type checker stops inline-emitting the three wired Class-O codes for defaults/bounds, stamps obligations instead; proof engine's default/computed walk extended to collect them; `DiagnosticStage` enum gains `Bind`/`Tooling`; meta relabels the two distorted code sets; `NoInitialState`/`CircularComputedField` emission consolidated to single owners.
- **Tooling (syntax/completions/hover/semantic tokens):** LS hover keys obligation-surfacing on obligation-presence, not `Stage == Proof` (`RichHoverFactory.cs:245` already half-does this).
- **MCP (vocabulary/DTOs/tool output):** `CatalogFormatters.cs:600` serializes the (now honest) stage string; the constant-default numeric code's identity may change (Decision 5); **and `precept_compile`'s per-stage error count changes** — `CompileTool.cs:70` computes `typeErrors` as `Count(Stage == Type)`, so moving binder codes `Type→Bind` (D2) and value-level codes `Type→Proof` (D3) *reduces* the reported type-error count and shifts those into bind/proof counts. All visible in `precept_compile` output. No external consumers (pre-release).

**Breaking changes.** Stage strings change for relabeled codes; `precept_compile`'s per-stage error counts shift (binder→Bind, value-level→Proof); possibly one diagnostic-code identity change (Decision 5). No `.precept` surface change. No external consumers (pre-release).

### External architectural precedent

**Roslyn analyzers enforcing compiler-codebase invariants** is the direct precedent — and Precept already runs 27 such analyzers (`Precept0001`–`Precept0027`), including `Precept0003DiagnosticMustUseCreate` (every diagnostic must use the factory — *why* Slice 1's single-factory closure held) and `Precept0027DiagnosticEmissionCoverage` (emit-or-allow-list coverage), built on a shared `DiagnosticCoverageScanner` that already locates every emission site and the code it emits. *What Precept takes*: the established pattern of encoding a compiler invariant as a build-time analyzer over emission sites. *What Precept diverges on / adds*: a new sibling analyzer on the *ownership* axis (site's enclosing stage must equal the code's catalog-declared owning stage), where `Precept0027` covers the *coverage* axis. Roslyn's `DiagnosticDescriptor` (which separates `Id` / `Category` / `DefaultSeverity` as independent fields, with suppression configured separately) is the external parallel for keeping `DiagnosticStage` a pure component classification with no precedence semantics — cited from general Roslyn-API knowledge, not a fetched excerpt. **The load-bearing, verified precedent is in-tree**: the 27 `Precept000x` analyzers + the shared `DiagnosticCoverageScanner` (read for this design) establish "enforce a compiler invariant via a build-time analyzer over emission sites" as an existing, working pattern here; the new ownership analyzer is a sibling on the ownership axis. Roslyn corroborates the axis-separation principle but the design does not rest on it.

## Inventory of what will be built

- **`DiagnosticStage` enum** (`Diagnostic.cs`): add `Bind`, `Tooling`; ordinal carries no semantics.
- **Meta table** (`Diagnostics.cs`): set each code's `Stage` to its true owning stage — relabel binder codes `→ Bind`, `McpToolInternalError → Tooling`; value-level codes (`OutOfRange`, `MaxPlacesExceeded`, `UnprovedAssignmentQualifierCompatibility`, future `NullInNonNullableContext`) `→ Proof`.
- **New analyzer** `PreceptNNNN DiagnosticStageOwnership` (`Precept.Analyzers`), reading the uniform surface the precursor produces (verified against `DiagnosticCoverageScanner`):
  - *Literal sites* — `Diagnostics.Create(DiagnosticCode.X, …)`: compare the emitting site's **containing type** (`TypeChecker`/`GraphAnalyzer`/`ProofEngine`/… — namespace is NOT a discriminator; all pipeline files share `namespace Precept.Pipeline`, so detection is by `ContainingType`, the same mechanism `Precept0003` already uses) to `Diagnostics.GetMeta(X).Stage`; flag mismatch.
  - *Catalog-mediated sites* — after the precursor, every dynamic code-selection is a read of a single catalog-meta `DiagnosticCode` field (`CIDiagnosticCode`, `Format/SemanticErrorCode`, `ProofRequirementMeta.DiagnosticCode`). The analyzer asserts the **catalog invariant**: every code reachable from such a field has `Stage` equal to the consuming stage (CI ⇒ `Type`; proof ⇒ `Proof`).
  - *Pattern-2 residue* — the precursor documents the bounded candidate set for each context-determined site (lexer mode-switch; proof `Numeric`/`KeyPresence`/`QualifierChain` cases); the analyzer checks each set's codes are owned by the emitting stage. Bounded, so still enforceable with no allow-list.
- **Type checker**: replace inline value-level emits for defaults/bounds with obligation stamping (`AssignmentQualifiers.cs` residual branch; `Modifiers.cs` `TryReportNumericViolation`/`OutOfRange`; `TypeChecker.cs` `MaxPlacesExceeded`).
- **Proof engine**: extend `CollectDefaultObligations`/`CollectArgDefaultObligations`/computed-field walk to collect the newly-stamped numeric-modifier + assignment-qualifier + presence obligations.
- **Dual-emission consolidation**: `NoInitialState` → Graph only (delete the `TypeChecker.cs:704` emit + the `GraphAnalyzer.cs:85` `HasDiagnostic` guard); `CircularComputedField` → single owner (consolidate `NameBinder.cs:305` + `Structural.cs:248`).
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

- **Rationale**: the three wired Class-O codes emit inline at the type stage *only* because the proof engine's default/computed walk doesn't yet stamp their obligation kinds. The hooks already exist (`CollectObligations` walks computed exprs + field/arg defaults for interval containment); extending what they stamp makes the proof stage the sole owner of value-level codes, which the Decision-1 analyzer then enforces. This is the principled end state — no code straddles stages.
- **Tradeoff accepted**: more work than allow-listing the Site-A residual, and it changes the constant-default numeric check's diagnostic identity (Decision 5). Accepted because the owner chose "no shortcuts" — the allow-list would have been permanent debt. **Caveat (collector shape gap)**: "extend what the hooks stamp" understates the work — `CollectDefaultObligations` (`ProofEngine.Analysis.cs:405`) today handles *only* `InterpolatedTypedConstant` defaults and bails on `IsUnbounded` (`:414`), whereas the inline `OutOfRange` fires on any `TryGetStaticMagnitude` success (incl. plain `TypedTypedConstant`). So the extension must (a) add the `TypedTypedConstant` default shape to the collector and (b) **not double-emit** on an `InterpolatedTypedConstant`-with-bounds default that the existing `IntervalContainment` walk already covers. The removed reconciliation step would have masked a double-emit here; with no dedup, the collector must be correct by construction.
- **Alternatives considered**: *Allow-list the Site-A dual until later.* Rejected by owner ("no shortcuts"). *Leave constant-default checks inline and exempt them.* Rejected — that's the Class-O-in-disguise the analyzer exists to catch.
- **Precedent**: `proof-engine.md` Decision 3 (stamp/discharge); the existing `CollectDefaultObligations` interval-containment walk is the in-tree template for stamping default obligations; the Site-A `set`-action discharge (`AssignmentQualifiers.cs:205-214`) is the template for the qualifier obligation.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/ProofEngine.cs:234-247` — the existing default/computed walk hooks.
  - `src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs:199-234` — the stamp-vs-inline split to extend to non-walked contexts.
- **Strongest counter-evidence**: extending the walk could surface obligations on defaults that previously compiled clean (behavior change). *Response*: that's correct-by-construction — if a default violated a value-level requirement, the inline check already caught it (`OutOfRange` etc.); the obligation discharges identically for constants. Net behavior is preserved for constants and improved (narrowing) for non-constants. Verified against the sample corpus is an acceptance criterion.
- **Reversibility**: Hard (proof-engine + type-checker contract), no public/author contract.
- **Blast radius**: `ProofEngine.cs` default/computed walk, type-checker stamping sites, proof-stage discharge tests. Couples to Decision 5 (code identity).

### Decision 4: Resolve the structural dual-emissions to single owners

**Stakes**: medium.

- **Rationale**: `NoInitialState` and `CircularComputedField` are each detected in two stages today; single-ownership requires one. `NoInitialState` is a graph property → Graph owns; the type-checker emit + the `HasDiagnostic` guard are deleted. `CircularComputedField` is a dependency-graph cycle → one of Bind/Type owns; consolidate.
- **Tradeoff accepted**: must verify neither dropped emitter caught a case its surviving owner misses (a small coverage check during execution).
- **Alternatives considered**: *Distinct codes per stage.* Rejected — same author-facing meaning; two codes would be noise.
- **Precedent**: Slice 1 §5 (the dual-emission inventory); `GraphAnalyzer.cs` owns the other initial-state/reachability diagnostics already.
- **Sources consulted for this decision**: `docs/Working/phase8-slice1-emission-inventory-2026-05-31.md` §5; `GraphAnalyzer.cs:85-88`, `TypeChecker.cs:704`, `NameBinder.cs:305`, `Structural.cs:248`.
- **Resolved at lock (2026-06-01) — `CircularComputedField` owner**: **Bind** owns it (a computed-field cycle is a name-dependency-graph property the binder already computes via topological sort at `NameBinder.cs:305`); the Type-stage DFS detector at `Structural.cs:248` is removed. *Falsifier*: if the binder's topological-sort detector misses a cycle class the Type-stage DFS catches (e.g. a cycle only visible after type resolution), the owner is Type instead — an execution-time coverage check confirms the binder detector is sufficient before the Type detector is deleted.

### Decision 5: Diagnostic identity for the relocated constant-default numeric check — keep `OutOfRange`, re-own to Proof

**Stakes**: medium (touches a diagnostic code visible in MCP/CLI; Phase 9 seam).

- **Rationale**: routing the constant-default numeric check through the proof stage means its unproved diagnostic is produced by the proof engine. **Decision: keep `OutOfRange` (PRE0079) as the code, re-owned to Proof** — minimizes author-facing identity churn (same code authors already see, now stage-correct). Whether `OutOfRange` ultimately consolidates with the other proof-stage numeric codes (`NumericOverflow` / `UnprovedModifierRequirement`) is a *catalog-completeness* question owned downstream by **Phase 9 / F-LANG-SPEC-12** ("wire `OutOfRange` constant-literal check or retire") — an emit-or-retire concern, not an emission-architecture one — so it does not block this design.
- **Tradeoff accepted**: keeping `OutOfRange` proof-owned means a numeric-bound violation surfaces under two codes depending on context (modifier-default vs operand) unless consolidated — a Phase 9 question.
- **Alternatives considered**: *Retire `OutOfRange` for `NumericOverflow`.* Possible, but a bigger author-facing change; defer the call to Phase 9 with this design flagging the coupling.
- **Precedent**: the PRE0141 re-stage (a code moved type→proof while keeping identity) is the in-tree template.
- **Sources consulted for this decision**: `docs/compiler/diagnostic-system.md:178` (the PRE0141 re-stage precedent); `phase8-slice1-emission-inventory-2026-05-31.md` §3.

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
