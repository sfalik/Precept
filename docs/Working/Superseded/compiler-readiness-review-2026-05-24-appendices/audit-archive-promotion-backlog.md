# Appendix — Archive Promotion Backlog Audit

**Date**: 2026-05-24
**Scope**: All 41 files in `docs/Working/Archive/` (excluding 6 pre-audited during Decision triage: `hover-design.md`, `interval-hover-design.md`, `constructor-semantics.md`, `diagnostic-enforcement.md`, `diagnostic-enforcement-implementation-notes.md`, `language-server-implementation-plan.md`)
**Author**: Sub-agent of the 2026-05-24 compiler-readiness review
**Findings**: 12 promotion obligations (7 fully stranded + 5 partially promoted); 28 properly historical
**Aggregate effort**: 15-22 hours (~2-3 working days)
**Parent doc**: [../compiler-readiness-review-2026-05-24.md](../compiler-readiness-review-2026-05-24.md)

## § A. Methodology

Per-file: read header + executive summary + tail/recommendation sections to classify. For each candidate finding, grep-checked canonical-doc routing targets in `docs/language/`, `docs/compiler/`, `docs/runtime/`, `docs/tooling/` for the load-bearing concepts and decision names. Cross-checked implementation status against `src/Precept/Language/DiagnosticCode.cs`, `src/Precept/Language/Operations.cs`, `src/Precept/Pipeline/`. Deep design docs (>50 KB) received structural reading rather than full reads.

## § B. Promotion Obligations (12)

### Shipped-with-why-stranded (7)

#### `catalog-compliance-audit.md` — M
- **Topic**: Pipeline catalog-compliance violation taxonomy (27 violations, 7 pattern classes A-H), Missing Catalog Fields master list (16 fields), Linked Bug Map (BUG-001..054)
- **Shipped**: Companion `precept-toolchain-plan.md` Slices 1-13 ✅ Complete; all listed BUGs marked Fixed
- **Canonical home**: `docs/language/catalog-system.md` (master taxonomy + field origin map); per-stage `docs/compiler/*.md` (rationale)
- **Stranded why**: Pattern A-H architectural vocabulary; 16-row "Missing Catalog Fields — Master List" mapping each newly-added field to its purpose; 6-tier Remediation Priority list
- **Notes**: Becomes "Architectural Violation Patterns" subsection in catalog-system.md + "Catalog Field Origin Map" appendix

#### `frank-bounds-qualifier-audit.md` — S
- **Topic**: `quantity max '5 kg'` without qualifier silently accepted; proposes BoundsRequireQualifier/BoundsQualifierMismatch/UnextractableBound diagnostics
- **Shipped**: `DiagnosticCode.cs` has PRE0133 + PRE0134
- **Canonical home**: `business-domain-types.md` (rule statement) + `type-checker.md` + `diagnostic-system.md`
- **Stranded why**: The rule itself: "Declaring `min`/`max` on `money`/`quantity`/`price` requires matching qualifier context — `in` for money/price; `in` or `of` for quantity"

#### `frank-qualifier-deferred-scoping.md` — M
- **Topic**: 3 deferred items: ProofEngine.Qualifiers ↔ TypeChecker AssignmentQualifiers unification (tri-state Resolved/Unknown/Absent), quantity Unit→Dimension fallback gaps, `TypedFunctionCall.ResultQualifiers` propagation
- **Shipped**: `TypedFunctionCall.ResultQualifiers` exists in `ProofEngine.Qualifiers.cs:376`; `ValidateFunctionQualifierCompatibility` exists in `TypeChecker.Expressions.Callables.cs:731`
- **Canonical home**: `proof-engine.md` § 5 (Qualifier Compatibility) + `type-checker.md` § Expressions + `catalog-system.md` (FunctionOverload.QualifierMatch)
- **Stranded why**: Tri-state `ResolvedQualifierAxis` model; "two parallel qualifier-resolution subsystems must converge" architectural rationale; `ResultQualifiers` propagation through `QualifierMatch.Same`

#### `pipeline-audit-fix-plan.md` — S
- **Topic**: Release-only build mandate; `Debug.Assert` → unconditional throw conversion (D26/D5 invariants); `Directory.Build.props` infrastructure
- **Shipped**: All 7 fixes ✅, 4,598 tests passing; `Directory.Build.props` exists at repo root
- **Canonical home**: `CONTRIBUTING.md` § Build & Test (new "Release-Only Builds (Non-Negotiable)" subsection)
- **Stranded why**: "Precept builds Release-only — `Debug.Assert` and `#if DEBUG` forbidden because stripped in Release"; "invariants must hold in production"

#### `research-conditional-construction.md` — S
- **Topic**: Cross-language survey of constructor-failure mechanisms (C++ throw, Swift `init?`/`init throws`, Rust `TryFrom`, Haskell smart constructors, etc.) grounding Precept's `on Event` + `reject` design
- **Shipped**: precept-language-spec.md § 1683, 1846 reference construction-row `reject`
- **Canonical home**: `research/language/` subfolder (per `/research` skill folder discipline) — not currently present
- **Stranded why**: Whole-doc evidence base for the construction-failure mechanism decision
- **Notes**: Pure relocation to `research/language/` + add citation pointer from `precept-language-spec.md` § 1880

#### `typed-constants-and-proof-coverage-plan.md` — L
- **Topic**: 253KB plan covering Part A (Interpolated Typed Constants, Slices 1-6) + Parts B-H (proof engine qualifier coverage gaps G1-G15). Includes the **Type-Grammar-Driven Slot Classification** core design decision
- **Shipped**: Status tracker shows all parts ✅ Done with commit refs
- **Canonical home**: `docs/compiler/literal-system.md` (Type-Grammar Slot Classification + per-type valid form grammars) + `docs/compiler/type-checker.md` § Interpolated typed constants + `business-domain-types.md` + `temporal-type-system.md`
- **Stranded why**: Quotes worth preserving: *"Each typed constant type that supports interpolation defines a closed set of valid segment-sequence patterns (a type grammar)."* / *"Alternative rejected — position-text heuristics: Examining surrounding text fragments requires the type checker to duplicate content-validation knowledge at the slot level and fails for compound qualifiers."* / *"Alternative rejected — parser-level slot classification: The parser doesn't know the target type."* The `T(num) H[slot]` per-type grammar notation is a canonical reference future authors will need.
- **Notes**: **Single largest stranded design surface in the audit.** Affects every author who writes `'{x} kg'` or `'{Amt} {Curr}'`.

#### `elaine-typed-literal-autocomplete-ux.md` + `kramer-typed-literal-impl-plan.md` — M (paired)
- **Topic**: Elaine's UX design spec for typed-literal completion (per-type behavior tables, trigger semantics for `'`/space/Ctrl+Space, qualifier-aware mode, compound-temporal continuation, "prefer no completions over wrong completions") + Kramer's 5-slice implementation plan
- **Shipped**: `GetTypedConstantItems`, `GetQuantitySlotItems`, currency completion etc. present in code
- **Canonical home**: `docs/tooling/language-server.md` § 7.3 (one paragraph there is the entire promotion; per-type tables, trigger semantics, design principles belong here)
- **Stranded why**: Trigger contracts (`'`, space, Ctrl+Space) nowhere in canonical; compound-temporal continuation flow undocumented; "Treat this as the canonical UX for all quoted scalar literals, not just the currently common quote-delimited types" / "Prefer no completions over wrong completions" design principles

### Partially promoted (5)

#### `completions-bugs.md` — M
- **Topic**: 8 bug + 2 crash + 3 interpolation root-cause analysis (Frank-7 triage of Kramer's `be2afdde`). Includes 5-slice plan for qualifier-aware completion architecture
- **Canonical home**: `language-server.md` § 7.3 (qualifier-aware mode decisions); `type-checker.md` (if `DeclaredQualifiers` shape on TypedArgRef/TypedFieldRef is load-bearing)
- **Stranded why**: Slot-vs-expression context bug class root cause ("Stop coercing qualifier literals to expression literals — add a qualifier-site resolver ahead of enclosing-field expression fallback"); "DeclaredQualifiers must flow through TypedArgRef/TypedFieldRef" decision invisible in canonical

#### `frank-catalog-obligation-audit.md` — S
- **Topic**: Replace `TypeKind` hardcoding in Actions.cs with metadata reads of `ApplicableTypes`/`ProofSatisfactions`
- **Shipped**: `interval-proof-engine-design.md` Slice 7 ✅ Done
- **Canonical home**: `proof-engine.md` § Catalog-Driven Obligation Architecture (does not exist as heading); `catalog-system.md` ProofSatisfactions
- **Stranded why**: 4-step remediation shape (derive emission from modifier metadata, emit for all declared constraints, per-family integration tests, keep metadata-driven) is the architectural rule not stated explicitly

#### `mcp-dto-free-design.md` — S
- **Topic**: Shane-approved Approach 4 (Hybrid) — markdown/text for catalog/reference tools; minimal JSON only where genuinely programmatic
- **Shipped**: `mcp.md` table shows 8 of 9 tools return "compact markdown"
- **Canonical home**: `docs/tooling/mcp.md` (Design Rationale section)
- **Stranded why**: 4 alternatives evaluated and rejected; "raw core-type serialization rejected", "no DTO generator", "no serialization attributes pushed into core runtime" — load-bearing constraints for future MCP decisions

#### `quantity-normalization-design.md` — L
- **Topic**: 316KB design for UCUM-driven cross-phase unit-aware comparison. Two-layer value architecture, UCUM scale table, runtime arg normalization at intake boundary, compiler↔runtime code-sharing seam, ~26 vertical slices
- **Shipped**: `proof-engine.md` § Normalization boundary; `runtime/evaluator.md` documents `PreceptValue` 32-byte tagged struct
- **Canonical home**: `runtime/evaluator.md` + `proof-engine.md` + `business-domain-types.md` (UCUM scale table) + new/extended `runtime/` content (intake-boundary normalization)
- **Stranded why**: Two-layer value architecture rationale; UCUM scale table itself; runtime intake-boundary normalization design; compiler↔runtime code-sharing seam
- **Notes**: Largest residual stranded; doc explicitly notes "shipped"

#### `syntax-coloring-fix-design.md` — S
- **Topic**: Root-cause analysis of "syntax coloring shifts when LS loads" + architectural fix
- **Shipped**: `language-server.md` § 7.2 documents the two-pass design with `VisualCategory` projection
- **Canonical home**: `language-server.md` § 7.2 (semantic tokens design rationale)
- **Stranded why**: Architectural rule "semantic tokens should only override TM when they provide information TM cannot"; "TM owns keyword classification, LS owns identifier classification" decision; the TM/semantic-tokens visible-shift bug class

## § C. Properly Historical (28)

| Filename | Classification rationale |
|---|---|
| `comma-list-syntax-spike.md` | Decisions promoted to `precept-language-spec.md` § 2.3 |
| `constraint-refs-proof-plan.md` | W1/W4 resolution promoted to `type-checker.md:1015` + `proof-engine.md` |
| `diagnostic-name-message-review.md` | Renames not adopted; standing recommendation, no shipped state |
| `field-state-guarantees.md` (v1) | Superseded by v3 (shipped: D130-D134) |
| `field-state-guarantees-v2.md` | Self-superseded by v3 |
| `field-state-guarantees-v3.md` | Shipped (D130-D134 in code; rules in spec § 2.2 + 1004-1008) |
| `frank-constructor-terminal-design.md` | Adoption pending owner decision; not shipped |
| `frank-grammar-comprehensive-review-2026-05-10.md` | Fixes verified applied in `precept-grammar.md` |
| `frank-grammar-spec-audit-2026-05-10.md` | `SupportsPostActionEnsure` removed (grep 0 hits) |
| `frank-initial-design-critique.md` | Superseded by `frank-constructor-terminal-design.md` |
| `frank-initial-event-semantics.md` | Promoted to `precept-language-spec.md` § 1836, 1877-1932 |
| `frank-money-modifiers.md` | Shipped in `business-domain-types.md` + diagnostic table |
| `frank-price-qualifier-enforcement-gap.md` | Superseded by full-analysis; gap fixed |
| `frank-price-qualifier-full-analysis.md` | PRE0139/PRE0141 shipped; `CompoundPrice` in catalog/business-domain |
| `frank-price-qualifier-shape-analysis.md` | Self-superseded; `QualifierAxis.PriceIn` + `CompoundPrice` shipped |
| `frank-v2-consistency-review.md` | Review that BLOCKED v2; v3 incorporated corrections |
| `frank-when-guard-audit.md` | Sample files fixed; grammar doc + spec aligned |
| `frank-when-guard-audit-2.md` | Byte-identical duplicate of `-3` and `-4-final` |
| `frank-when-guard-audit-3.md` | Same duplicate |
| `frank-when-guard-audit-4-final.md` | Same duplicate |
| `interval-proof-engine-design.md` | All 13 slices ✅ Done; `proof-engine.md` has Strategy 7 + IntervalContainment + Normalization boundary |
| `overflow-prevention-design-analysis.md` | Self-marked SUPERSEDED |
| `precept-toolchain-bugs.md` | 54/54 fixed; per-bug rationale in commits |
| `precept-toolchain-plan.md` | Master execution plan; architectural principle in `catalog-system.md` |
| `proof-engine-qualifier-audit.md` | Gaps fixed (`Operations.cs` has `QualifierCompatibilityProofRequirement` on relevant ops) |
| `proof-engine-remediation-review.md` | One-time review; subsequent slices shipped |
| `proof-gaps-issues.md` | Gaps closed |
| `temporal-businessunit-completions-proposal.md` | "Design complete — pending Shane sign-off"; not yet shipped |

## § D. Unclear

| Filename | What's unclear |
|---|---|
| (none) | All 41 files classified cleanly; partially-promoted vs properly-historical line was clear after spot-checks. |

## § E. Summary

- Files scanned: **41**
- Shipped-with-why-stranded: **7**
- Partially-promoted: **5**
- Properly-historical: **28**
- Unclear: **0**
- Total promotion obligations: **12**
- Aggregate effort: 5 S + 5 M + 2 L → **15-22 hours = ~2-3 working days**

### Top 3 Most Consequential

1. **`typed-constants-and-proof-coverage-plan.md` (L)** — Type-Grammar Slot Classification architecture for interpolated typed constants; touches 4 canonical docs; largest stranded design surface; affects every author who writes interpolated typed constants
2. **`catalog-compliance-audit.md` (M)** — Pattern A-H taxonomy + Missing Catalog Fields master list; the architectural vocabulary used by Slices 1-13; rosetta stone for why `catalog-system.md` is shaped the way it is
3. **`quantity-normalization-design.md` (L)** — Two-layer value architecture rationale + UCUM scale table + runtime intake-boundary normalization design; durable architectural commitments that ground `evaluator.md`'s dual-shape boundary invariant

### Adjustment for Phase 1 Scope

Combined with the 4 pre-audited promotions (hover-design, constructor-semantics, diagnostic-enforcement, language-server-implementation-plan), **Phase 1 now has 16 total promotion obligations**. Of those:
- 4 already scoped (from earlier Decisions 1, 2, 6, 7)
- 12 newly scoped (from this audit)

Effort: ~3-4 days for the 16 promotions if running sequentially. Faster if parallelized (most are independent).
