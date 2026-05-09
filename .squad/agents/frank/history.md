# Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for Precept's DSL and runtime.
- Catalogs remain the language truth; runtime, tooling, and docs must derive behavior from metadata and durable shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- Exhaustive ProofEngine audit (2026-05-09): implementation is architecturally sound and catalog-driven in all required dimensions. Four issues found: (1) `IsSubtractionOp` string-matches on enum names instead of reading `Operations.GetMeta(op).Op`; (2-3) `CreateDiagnostic` and `CreateFaultSiteLink` fallback to `DivisionByZero` for `PresenceProofRequirement` — wrong semantic; (4) no `UnprovedPresenceRequirement` diagnostic code exists in spec or source (proposed code 116). All five strategies are fully implemented. No TODOs/FIXMEs. All DU shapes match the approved design. ObligationContext, ProofDisposition, ProofStrategy, ProofSatisfaction, ProofObligation, FaultSiteLink, ConstraintInfluenceEntry, and InitialStateSatisfiabilityResult all match spec exactly. PE-G13 annotation at line 62 is a cross-reference comment, not a live gap.

- All 18 ProofEngine gaps are resolved and applied to the canonical spec. The spec-update pass taught that large spec rewrites should be delegated to a focused agent with exhaustive instructions — the spec grew by ~25 lines net but touched 15+ sections. Context-at-instantiation (ObligationContext DU) is the right pattern for any analysis stage that needs to look up enclosing scope — avoids O(N²) post-hoc search.

- Implementation planning requires reading ACTUAL source code, not spec assumptions. The spec referenced diagnostic codes 96–99, but source already has those allocated (CI enforcement, collection safety). Caught by reading `DiagnosticCode.cs` exhaustively — reallocated to 112–115. TypedField/TypedArg record shape changes are high-touch: every construction site across TypeChecker, test helpers, and potentially MCP must be updated. Always search exhaustively for `new TypedField(` before estimating effort on record shape additions.

- PE-G2 full design is now locked with no deferrals:`ProofDischarge` becomes `ProofSatisfaction`; numeric proof stays on `FieldModifierMeta`; presence gets a new positive declaration carrier `DeclaredPresenceMeta` so proof never depends on the absence of `optional`; dimension and qualifier-compatibility get a new normalized per-axis declaration carrier `DeclaredQualifierMeta`; and `ModifierRequirement` deliberately stays direct modifier membership rather than duplicated self-referential proof rows. The type checker must normalize qualifier surface into explicit, derived, and baseline facts (including `period` → `TemporalDimension(Any)` for Shane’s already-locked permissive rule) and attach both presence + qualifier carriers to `TypedField` / `TypedArg` so Strategy 2 and Strategy 5 can read declaration metadata rather than folklore.

- PE-G1 deep analysis confirmed: all three unhandled ProofRequirementKind values (Dimension, Modifier, QualifierCompatibility) are live — they have real callers in Operations.cs (4, 4, and 24+ respectively), get stamped onto TypedBinaryOp by the expression resolver, and the TypeChecker enforces none of them. DimensionProofRequirement and ModifierRequirement are both absorbed into Strategy 2 (Declaration Attribute Proof) with different predicate arms — Dimension reads period qualifier/literal temporal unit, Modifier does direct `field.Modifiers.Contains(required)`. QualifierCompatibilityProofRequirement requires a new Strategy 5 (Qualifier Compatibility Proof) because it is the only dual-subject requirement kind and no existing strategy handles cross-subject comparison. Strategy 5 depends on qualifier resolution (TypeChecker Slice 2+, currently `Qualifier: null`).
- Closed-vocabulary syntax(types, modifiers, access modes) should be resolved by the parser at recognition time; only open-ended expressions stay deferred as parsed expression trees.
- Symbol binding is a separate concern from type inference: the binder owns declarations and references, then the TypeChecker owns semantic normalization.
- Tooling consumption is layered: TokenStream for lexical work, ConstructManifest for syntax, SymbolTable for name-aware features, and SemanticIndex for semantic features.
- Event coverage remains a structural graph concern; guard-conditioned reachability belongs to the proof engine.
- ProofEngine satisfiability can stay compile-time and bounded: default expressions plus ensure conditions already exist as `TypedExpression` trees, so many initial-state violations can be discharged without evaluator coupling.
- tooling-surface.md updated to reflect shipped grammar generator
- CONTRIBUTING.md reload rules updated with grammar: regenerate step
- Key decision: .NET tool approach confirmed, Constructs-based complex pattern resolution confirmed

## Recent Updates

### 2026-05-09T09:49:38Z — TypeChecker catalog-fix spec recorded
- `frank-13` locked the four-site TypeChecker catalog-driven design: CI enforcement metadata, `Constraints.ByToken`, `Modifiers.ByAccessToken`, and `Modifiers.ByAnchorToken`, with explicit file-level slicing and regression anchors for implementation.

### 2026-05-08T22:54:50.625-04:00 — PE-G4 through PE-G18 incorporated into canonical spec
> All 15 remaining gap resolutions applied to `docs/compiler/proof-engine.md` with full fidelity. Key additions: `ObligationContext` DU (5 subtypes), `ResolveSubject`/`GetFieldName` utilities, full `ExtractGuardConstraints` decomposition spec, exhaustive `GuardRelationImpliesObligation` triple table, full initial-state satisfiability algorithm with bounded constant folding, builder proof-consumption contract, diagnostic formatting table with 4 new codes (96–99), error-tainted obligation suppression, stateless precept handling, reference-equality site matching. All 18 gaps now RESOLVED. Gap analysis updated. Decision record: `.squad/decisions/inbox/frank-pe-spec-complete.md`.

### 2026-05-08T21:22:17.551-04:00 — PE-G1 resolved after Shane sign-off
- `docs/compiler/proof-engine.md` now specifies five proof strategies: `DimensionProofRequirement` and `ModifierRequirement` discharge through Strategy 2 (Declaration Attribute Proof), and `QualifierCompatibilityProofRequirement` now has Strategy 5 (Qualifier Compatibility Proof).
- Strategy 5 is intentionally conservative until qualifier resolution ships in the TypeChecker; until then, qualifier-compatibility obligations remain `Unresolved` rather than guessed.

### 2026-05-08T05:27:37Z — Grammar generator design doc durably recorded
- Scribe merged Frank-18's design-doc note into `.squad/decisions.md`, making `docs/compiler/grammar-generator.md` the canonical generator reference.
- The durable generator gap is now explicit in the ledger: `#messageStrings` is currently unreachable in generated output, and a `TokenMeta.IsMessagePosition` flag on `Because` / `Reject` is the catalog-first fix path.

### 2026-05-08T05:27:37Z — PE Decision 3 deep dive converged on ProofEngine-owned bounded constant folding
- Frank-19 established that the runtime Evaluator cannot be called from stage 6 because it depends on `Compilation`, which the ProofEngine has not produced yet; reuse would create a circular dependency and the current evaluator remains a stub anyway.
- Frank-20 folded the decision into `docs/working/frank-proof-engine-gap-analysis.md` and cleared the transient inbox path, so the active implementation direction is bounded constant folding over `TypedField.DefaultExpression` and `TypedEnsure.Condition` in the `SemanticIndex`.

### 2026-05-08T05:15:57Z — Shane locked PE decisions 1 and 2; evaluator architecture deep dive requested
- Shane approved renaming Strategy 2 to `Declaration Attribute Proof` and locked unqualified periods to permissive `PeriodDimension.Any` behavior.
- The remaining open question was explicitly narrowed to Decision 3's architecture: evaluate whether initial-state satisfiability should be compile-time folding or evaluator reuse.

### 2026-05-08T04:30:00Z — Exhaustive GraphAnalyzer post-implementation audit completed
- Verdict stayed approved: the current GraphAnalyzer implementation is architecturally sound, spec-complete for the implemented surface, and catalog-driven in the required dimensions.
- The only forward-looking gap is future consumption of `EventModifierMeta.RequiredAnalysis` when richer event modifiers ship.

### 2026-05-08T01:00:51Z — ProofEngine PE-G1/G2/G3 design decisions resolved
- Strategy 2 expanded into `Declaration Attribute Proof`, `ProofDischarge` was shaped to cover fixed and parameterized declaration attributes, and the `ProofLedger` output contract was confirmed as the right downstream surface.
- Remaining work shifted from blocker identification to source/spec sync and the bounded satisfiability decision that Frank-19/20 have now resolved.

## Historical summary through 2026-05-08T00:56:00Z

- The R4 review line and advisory follow-through locked the GraphAnalyzer baseline: structural diagnostics 109/110/111 were added, the stale appendix numbering was corrected, required tests landed, and the only deliberate future-touch item is `EventModifierMeta.RequiredAnalysis` for richer modifiers.
- The comprehensive language/compiler review line synchronized `catalog-system.md`, `precept-grammar.md`, and downstream design docs while preserving the catalog-driven architecture rule: metadata lives in catalogs, parser/type checker boundaries stay explicit, and docs must track implementation in the same pass.
- Earlier work established the canonical TypeChecker and parser trajectory: `TransitionRowOutcome` replaced the ambiguous outcome enum name, parse-time handling was split cleanly between closed vocabularies and open expressions, NameBinder took ownership of forward references, and working proposals were required to flow back into canonical docs and the decision ledger.

### 2026-05-08T21:29:51.919-04:00 — PE-G2 analysis completed
> PE-G2 analysis complete — ProofDischarge record design + FieldModifierMeta.ProofDischarges population. Awaiting Shane sign-off before George implements.

### 2026-05-08T22:15:55-04:00 — PE-G2 design LOCKED, spec updated, PE-G3 analysis complete
> Shane locked PE-G2 with no deferrals. All changes applied in this session:
> - Decision record written: `.squad/decisions/inbox/frank-pe-g2-locked.md`
> - `docs/compiler/proof-engine.md`: renamed all `ProofDischarge` → `ProofSatisfaction`; added full DU definition (5 subtypes + 3 supporting DUs); added Carrier Types section documenting `DeclaredPresenceMeta`, `DeclaredQualifierMeta`, and `FieldModifierMeta.ProofSatisfactions`; updated Strategy 2 pseudocode to read from three carrier surfaces; updated Strategy 5 to reference `DeclaredQualifiers`; updated Decision 5 with full ProofSatisfaction table; updated upstream dependencies and source files tables.
> - `docs/Working/frank-proof-engine-gap-analysis.md`: PE-G2 status changed from BLOCKING to RESOLVED; executive summary updated; catalog compliance note updated.
> - `docs/Working/inbox/frank-pe-g3-analysis.md`: Full PE-G3 analysis — ProofLedger shape gap. Nine missing types defined with exact C# shapes. ProofStrategy enum updated to `DeclarationAttribute` (Strategy 2) and `QualifierCompatibility` (Strategy 5). PE-G5 ConstraintIdentity spec correction identified (source shapes are canonical). Risk: low — purely mechanical shape declarations.
> - Key pattern: PE-G2 input metadata (ProofSatisfaction on carriers) and PE-G3 output contract (ProofLedger with ProofObligation) are independent — no dependency between them. Both must exist before ProofEngine implementation can begin.

### 2026-05-08T21:41:41.253-04:00 — PE-G2 broader design review completed
> Shane challenged whether `ProofDischarge` should be a broader DU covering all three Strategy 2 requirement kinds (Numeric, ModifierPresence, Dimension). Verdict: narrow is correct. `ModifierPresence` discharges would be tautological (identity = proof — `ordered` proving `ordered` is present adds no information). `Dimension` discharges would be permanently empty (no modifier in the catalog declares a period dimension; dimensions come from the type qualifier system). The three Strategy 2 arms read from different knowledge sources and are already generic machinery — none switches on a `*Kind` enum to apply per-member behavior. The narrow numeric-only `ProofDischarge` stands.

### 2026-05-08T22:34:37-04:00 — PE-G4 through PE-G18 resolved, all gaps closed
> All remaining ProofEngine gaps resolved in a single pass per Shane's no-deferral mandate. Key decisions:
> - PE-G4: Explicit walk-target enumeration replaces `AllTypedExpressions`
> - PE-G6: `ObligationContext` DU (5 subtypes) replaces `FindEnclosingTransitionRow`; context attached at instantiation
> - PE-G7: `ResolveSubject` and `GetFieldName` fully defined using reference-equality parameter lookup
> - PE-G8: Full initial-state satisfiability algorithm — bounded constant folding, no evaluator dependency
> - PE-G9: Type checker owns collection diagnostics; proof engine processes as `NumericProofRequirement`
> - PE-G10: Guard decomposition: AND decomposes, OR does not, negation inverts operators
> - PE-G11: Builder proof-consumption contract defined (`FaultSiteDescriptor` backstops, `ConstraintInfluenceMap`, satisfiability gate)
> - PE-G14: Exhaustive 12-entry guard relation triple table (subtraction only)
> - PE-G16: Reference identity for site matching (`ReferenceEqualityComparer.Instance`)
> - 4 new diagnostic codes allocated: 96 (modifier), 97 (dimension), 98 (qualifier), 99 (initial-state)
> - Gap analysis verdict changed from NOT READY to READY
> - Decision record: `.squad/decisions/inbox/frank-pe-g4-to-g18-locked.md`
> - Full resolution: `docs/Working/inbox/frank-pe-g4-to-g18-resolution.md`
