---
status: Locked 2026-06-01
phase-target: Phase 8 Slice 2 (code-mediation standardization — precursor to Slice 3)
comparable-systems-research-status: not-applicable — internal refactor standardizing how an existing per-member metadata pattern is expressed; no new language-surface or comparable-systems claim
sources-consulted:
  - docs/Working/phase8-slice2-dispatch-deepdive-2026-06-01.md: the deep dive — five dynamic-dispatch families, two patterns, the standardization (§4) and wider-adoption (§7) findings
  - docs/Working/phase8-slice3-emission-ownership-2026-05-31.md: Slice 3 — the ownership analyzer that consumes this precursor's uniform surface
  - src/Precept/Pipeline/ProofEngine.Diagnostics.cs: CreateDiagnostic (dual-surface), CreateFaultSiteLink (:406 catalog read, :412 hardcoded switch)
  - src/Precept/Pipeline/TypeChecker.Expressions.cs: the typed-constant string round-trip (:344-356)
  - src/Precept/Language/FaultCode.cs: [StaticallyPreventable(DiagnosticCode)] (16 members, declared-but-never-read)
  - src/Precept/Language/ProofRequirement.cs:264-266: ProofRequirementMeta.DiagnosticCode (the catalog-mediated proof code, the template)
---

**Promoted to:** `compiler/diagnostic-system.md § Emission shapes` (the literal-vs-catalog-mediated convention + the `DiagnosticCoverageScanner` Pattern 1/2/3 pointer) and `§ The [StaticallyPreventable]-derived fault map`; `compiler/proof-engine.md § ProofLedger Construction` (`CreateDiagnostic` reads `ProofRequirementMeta.DiagnosticCode`; the genuinely-context residue). **Archived 2026-06-04 — canon synced in ship commit `59fe2666` (same-pass doc-sync); no canonical edit needed at promotion, archival only.**

# Slice 2 — Diagnostic-Code Selection Standardization (precursor to Slice 3)

*(A no-behavior-change refactor that lands before Slice 3 (the ownership architecture). It makes every diagnostic emission source its code in one of two uniform ways — a literal, or a single catalog-meta `DiagnosticCode` field — and explicitly marks the irreducible context-determined residue. Slice 3's ownership analyzer then reads a uniform surface instead of coping with three ad-hoc dispatch shapes.)*

## Goal

When done, every diagnostic-code *selection* in the pipeline is one of exactly two shapes — **(1)** a literal `DiagnosticCode.X` at the branch that decides it, or **(2)** a read of a single catalog-meta `DiagnosticCode` field — with a short, explicitly-documented list of **(3)** irreducible context-determined sites (where the code is a function of discharge-site shape or open lexer state, with a bounded candidate set). The stringly-typed `DiagnosticCode.ToString()`↔`Enum.TryParse` round-trip is gone; `CreateDiagnostic`'s subtype-fixed codes are read from catalog metadata (not re-hardcoded); the `DiagnosticCode→FaultCode` switch's bijective rows derive from `[StaticallyPreventable]`. **Zero behavior change** — identical diagnostics and fault links on the full sample corpus.

## Scope

- **In scope**: the *code-selection mechanism* at emission sites — converging the catalog-mediated cases on one convention, eliminating the round-trip and the dual-surface re-hardcoding, reconciling the `:412` fault map with the `[StaticallyPreventable]` attribute, and documenting the Pattern-2 residue with its bounded candidate sets.
- **Out of scope**: stage *ownership* / the honest `Bind`/`Tooling` taxonomy / the proof-walk extension / dual-emission consolidation — all Slice 3. The ownership *analyzer* itself — Slice 3 (this precursor only makes its input uniform). Diagnostic message wording — Phase 9.
- **Deferred to future**: none.

## Philosophy Alignment

| Principle | Affected? | How served | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | N | No change to what's prevented. | N/A | N/A |
| 2. One file, complete rules | N | No `.precept` surface. | N/A | N/A |
| 3. Determinism | N | Behavior-preserving; same code/fault for same input. | N/A | N/A |
| 4. Full inspectability | Y | "Where is code X selected / where does it map to a fault" becomes answerable from the catalog rather than from grepping selectors + a string round-trip. | N/A | N/A |
| 5. Keyword-anchored readability | N | No surface syntax. | N/A | N/A |
| 6. Governance not validation | N | N/A. | N/A | N/A |
| 7. Compile-time totality | N | No fault-coverage change. | N/A | N/A |
| 8. Honesty about approximation | N | N/A. | N/A | N/A |
| 9. Mandatory rationale | N | N/A. | N/A | N/A |
| 10. Static semantic checking | Y | Removing the `Enum.TryParse(string)` seam replaces a typo-silent runtime parse with a typed, compile-checked reference. | N/A | N/A |
| 11. Static completeness | N | No runtime-fault-surface change. | N/A | N/A |

**Companion commitments**: unaffected (internal refactor). The slice is squarely a catalog-driven + inspectability cleanup.

## Language Design Grounding

**N/A** — no language surface. (Guard 2 not triggered.)

## Audience and Teachability

**N/A** — no domain-author-facing surface. (Guard 3 not triggered.)

## Semantic Rules

**N/A** — no evaluation/typing/proof-obligation change. Code *selection* is reorganized; what each obligation/check decides is unchanged. (The hard invariant is behavior preservation — see Acceptance.) (Guard 4 not triggered.)

## Architecture Grounding

### Precept-internal placement

**Layer placement.** Code-per-member associations move fully into **catalog metadata** (the meta records already host them: `CIDiagnosticCode`, `Format/SemanticErrorCode`, `ProofRequirementMeta.DiagnosticCode`, `[StaticallyPreventable]`); pipeline code *reads* them. This is the catalog-before-code rule applied to the last places diagnostic-code selection still lives partly in pipeline logic.

**Cross-component propagation:**
- **Runtime:** `TypeChecker.Expressions.cs` (validators carry a typed code/kind, no string round-trip); `ProofEngine.Diagnostics.cs` (`CreateDiagnostic` reads `meta.DiagnosticCode` for subtype-fixed kinds; `CreateFaultSiteLink` derives bijective fault rows from `[StaticallyPreventable]`); optional `Operation.cs`/`Function.cs` (collapse the CI field pair).
- **Tooling:** None (hover/completions/semantic-tokens don't touch code selection).
- **MCP:** None — output is identical (behavior-preserving). The MCP CI formatter keeps reading `CIDiagnosticCode`.

**Breaking changes.** None — this is the no-behavior-change precursor. No code identity changes (that's Slice 3 D5), no stage changes (Slice 3 D2), no output changes.

### External architectural precedent

**Roslyn `DiagnosticDescriptor`/analyzer model** keeps the diagnostic id as data on a descriptor object that consumers read, never re-deriving it via string round-trips or parallel switches — the same "id is metadata, read it" discipline this precursor finishes. Internally, `ProofRequirementMeta.DiagnosticCode` (introduced by Slice 9C) is the in-tree template: it already proved the subtype-fixed proof codes belong in catalog metadata and that `CreateFaultSiteLink` can read them (`ProofEngine.Diagnostics.cs:406`); this precursor extends that proven move to `CreateDiagnostic` and to the `[StaticallyPreventable]` fault map. Reflecting over a declared attribute to build the inverse map is the standard .NET pattern for "single source of truth declared as an attribute."

## Inventory of what will be built

- **C (typed-constant)**: remove `SelectTypedConstantDiagnosticCode`'s `Enum.TryParse(diagnostic.Code)` round-trip (`TypeChecker.Expressions.cs:344-356`) — have the per-type validators carry a typed `DiagnosticCode?` (or just their Format/Semantic kind) so the checker reads a typed value; the concrete-vs-interpolated gate stays but keys on the typed value.
- **D (proof subtype-fixed)**: `CreateDiagnostic` reads `meta.DiagnosticCode` for the 10 subtype-fixed kinds instead of re-hardcoding them (`ProofEngine.Diagnostics.cs` arms); per-arm code stays only for message-argument formatting. The two genuinely-context cases (`Numeric`, `KeyPresence`) and the `QualifierChain` override remain explicit dispatch (Pattern 2).
- **`:412` fault map**: build the bijective `DiagnosticCode→FaultCode` rows by reflecting over `[StaticallyPreventable]` (making the attribute *read*, not just declared); retain the many-to-one collapse (`KeyPresenceSafety`/`IndexBoundsGuard`→`CollectionEmptyOnAccess`) and the conservative backstop as explicit, commented policy.
- **B (CI, optional)**: collapse `HasCIVariant` + `CIDiagnosticCode` to a single nullable `CIDiagnosticCode` (presence ⇒ has-variant), removing the hand-synced pair.
- **Pattern-2 residue doc**: a short documented list (lexer mode-switch `A`; proof context cases `E`) with each site's bounded candidate set — the explicitly-marked "not catalog-mediated, and correctly so" residue.
- **Tests**: behavior-preservation guard (corpus diff = empty); a test that `[StaticallyPreventable]`-derived fault map matches the prior `:412` bijective rows; round-trip-removal regression tests.

## Decisions

### Decision P1: Converge code selection on "literal or single catalog-meta field"; remove the string round-trip and the dual-surface re-hardcoding

**Stakes**: medium (touches the validator↔checker contract and the proof emission method; reversible; no behavior change).

- **Rationale**: the deep dive found these are the two genuine debts — C's stringly-typed round-trip (typo-silent, the one seam that makes C un-analyzable) and D's dual-surface drift (the same 10 codes hardcoded in `CreateDiagnostic` while `CreateFaultSiteLink` reads them from the catalog). Both are *incomplete* applications of the catalog-mediation the project already uses; finishing them removes the debt and yields a uniform surface.
- **Tradeoff accepted**: touching every typed-constant validator (to carry a typed code) is broader than a localized fix; accepted because the alternative (leave the round-trip) keeps a typo-silent seam and blocks static analysis of family C.
- **Alternatives considered**: *Leave as-is, special-case in the analyzer.* Rejected — pushes the debt downstream and keeps the round-trip. *Convert to literals at each branch.* Rejected for the catalog-mediated cases — would re-introduce a parallel `*Kind→code` association the catalog rule forbids.
- **Precedent**: Slice 9C (`ProofRequirementMeta.DiagnosticCode` + `CreateFaultSiteLink` reading it) is the in-tree proof that this exact move is correct and behavior-preserving.
- **Sources consulted for this decision**: `TypeChecker.Expressions.cs:344-356` (the round-trip); `ProofEngine.Diagnostics.cs:406` (catalog read) vs the `CreateDiagnostic` arms (re-hardcode); `docs/Working/phase8-slice2-dispatch-deepdive-2026-06-01.md` §3-4.
- **Resolved at lock (2026-06-01) — validator typed-code carriage**: validators carry a typed `DiagnosticCode?` (null ⇒ the catalog Format/Semantic mapping), and the concrete-vs-interpolated gate keys on the typed value. This kills the string round-trip and respects layering — a validator carries a specific code only where it genuinely owns one (e.g. `QuantityValidator` → `DimensionCategoryMismatch`); otherwise null and the catalog maps it. Falsifier 3 guards the layering-inversion risk.

### Decision P2: Derive the `DiagnosticCode→FaultCode` bijective rows from `[StaticallyPreventable]`; keep the collapse + backstop as explicit policy

**Stakes**: medium (touches the runtime fault-link mapping; behavior-preserving).

- **Rationale**: `[StaticallyPreventable(DiagnosticCode)]` is declared on 16 `FaultCode` members but **never read** by anything — meanwhile `:412` hardcodes a parallel `DiagnosticCode→FaultCode` switch whose bijective rows are exactly the attribute's inverse. Reading the attribute makes it a real single source of truth and removes the parallel rows; a test pins that the derived map equals the prior rows (behavior preservation).
- **Tradeoff accepted**: `:412` is only a *partial* parallel map — it also encodes a many-to-one collapse and a backstop default that the attribute can't express. So the reconciliation is "derive the bijective part, keep the collapse/backstop as explicit code" — not a full elimination. Accepted: the residual policy is small, real, and now clearly separated from the derivable part.
- **Alternatives considered**: *Leave `:412` fully hardcoded.* Rejected — keeps a parallel map of declared metadata and leaves `[StaticallyPreventable]` write-only. *Push the collapse/backstop into metadata too.* Deferred — the collapse is genuine runtime-backstop policy, not a clean per-member fact; over-cataloging it would be the inverse smell.
- **Precedent**: `Precept0002` already enforces that every `FaultCode` *has* a `[StaticallyPreventable]`; reading it to build the inverse is the natural next use. Standard .NET attribute-reflection.
- **Sources consulted for this decision**: `src/Precept/Language/FaultCode.cs:4-6` + the 16 attribute declarations; `ProofEngine.Diagnostics.cs:410-428` (the hardcoded switch with its collapse + backstop comment).

### Decision P3: Explicitly document the irreducible Pattern-2 residue with bounded candidate sets

**Stakes**: low.

- **Rationale**: the lexer mode-switch (`A`) and the proof context cases (`E`: Numeric-by-Site, KeyPresence-flag, QualifierChain-override) genuinely can't be a pure catalog field — the code is a function of runtime context. Documenting them (with each site's bounded, statically-enumerable candidate set) marks them as the deliberate residue so a future reader doesn't mistake them for unfinished standardization, and gives Slice 3's analyzer the candidate sets it needs to bound them.
- **Tradeoff accepted**: a small documented residue persists rather than 100% catalog-mediation; accepted because forcing these into metadata would be over-cataloging (the inverse smell).

### Decision P4: Collapse the CI field pair to one nullable field

**Stakes**: low.

- **Rationale**: `HasCIVariant` + `CIDiagnosticCode` encode one fact in two hand-synced fields; presence of the code implies the variant. Collapsing to a single nullable `CIDiagnosticCode` removes the hand-synced invariant.
- **Tradeoff accepted**: touches the meta record + ~5 catalog entries + the CI dispatch guard; trivially behavior-preserving. The MCP CI formatter reads `CIDiagnosticCode`, which survives as the single field, so its read stays clean (the no-behavior-change acceptance test confirms). **Settled at lock (2026-06-01): include the collapse** — it's a clean simplification on-theme with the slice, not deferred.

## Falsifiers

- If the sample-corpus diagnostic+fault-link output is **not byte-identical** before/after, the refactor isn't behavior-preserving — stop; something in selection changed semantics.
- If the `[StaticallyPreventable]`-derived fault map differs from the prior `:412` bijective rows on any code, the attribute and the switch had already drifted — surface it (a latent bug the reconciliation just exposed), don't silently adopt either side.
- If removing the typed-constant round-trip forces a validator to know a `DiagnosticCode` it structurally shouldn't (a layering inversion), the typed-code-on-validator choice is wrong — reconsider carrying only the Format/Semantic kind + catalog lookup.

## Acceptance criteria

- Sample-corpus diagnostics and fault links are identical before/after (a diff test asserts empty).
- No `Enum.TryParse<DiagnosticCode>(string)` remains in the typed-constant path; validators carry a typed code/kind.
- `CreateDiagnostic`'s subtype-fixed arms read `meta.DiagnosticCode`; only `Numeric`/`KeyPresence`/`QualifierChain`-override retain explicit dispatch.
- `[StaticallyPreventable]` is read to build the fault map; a test pins derived == prior bijective rows; the collapse/backstop remain as explicit, commented code.
- The Pattern-2 residue is documented with bounded candidate sets.
- Full suite green across all 4 projects.

## Dependencies

- **Upstream**: the deep dive (done); Slice 9C's `ProofRequirementMeta.DiagnosticCode` (shipped — the template).
- **Downstream**: **Slice 3** — its ownership analyzer reads this precursor's uniform surface (literal or `meta.<Field>`), and its dynamic-site coverage reduces to "read the catalog field's Stage + check the bounded Pattern-2 candidate sets." This precursor is what makes Slice 3's "zero allow-list, fully enforced" true by construction rather than coincidental.

## Doc-update enumeration

- `docs/compiler/diagnostic-system.md` — **add an "Emission shapes" subsection** (closes a recurring blind spot): diagnostics are emitted via `Diagnostics.Create` in two shapes — a **literal** `DiagnosticCode.X`, or a **catalog-mediated** read of a `DiagnosticCode` field (`CIDiagnosticCode`, `Format/SemanticErrorCode`, `ProofRequirementMeta.DiagnosticCode`) — plus the small documented context-determined residue. Point to `DiagnosticCoverageScanner`'s Pattern 1/2/3 as the authoritative enumeration, so future analysis doesn't rely on the (incomplete) literal `Diagnostics.Create(DiagnosticCode.X)` grep. Also document the catalog-mediation convention + the `[StaticallyPreventable]`-derived fault map. *(Rationale: the dynamic-dispatch families have been missed repeatedly — Slice 1 nearly, Slice 3 until review — because the prose doesn't foreground them and the obvious grep is incomplete; surfacing the scanner's enumeration in the canonical doc is the structural fix.)*
- `docs/compiler/proof-engine.md` — `CreateDiagnostic` reads `ProofRequirementMeta.DiagnosticCode`; the fault map derives from the attribute.
- `docs/Working/compiler-readiness-plan-2026-05-24.md` — Phase 8 slice log (precursor).

## Operational dimensions

- **Observability** (triggered — diagnostic surface): output is unchanged; the win is internal traceability (code selection answerable from the catalog). No trace/log change.
- Security / Evolvability: N/A.

## Open questions

None — resolved at lock (2026-06-01):
- *Validator typed-code carriage* → resolved in Decision P1 (typed `DiagnosticCode?`, null ⇒ catalog Format/Semantic mapping; no string round-trip).
- *CI field-pair collapse* → resolved in Decision P4 (include the collapse).

Neither required an owner decision (both execution-shape calls on a no-behavior-change refactor); the no-behavior-change acceptance test is the backstop for both.
