## Core Context

- Owns architecture, language design, and final review gates across runtime, tooling, and documentation.
- Co-owns language research with George and keeps `docs/PreceptLanguageDesign.md` aligned with actual implementation.
- Standing architecture rules: keep MCP as thin wrappers, keep docs factual, surface open decisions instead of inventing behavior, and preserve philosophy boundaries.
- Historical summary (pre-2026-04-13): drove proposal/design work for conditional guards, event hooks, verdict modifiers, modifier ordering, computed fields, and related issue-review passes; also reviewed the Squad `@copilot` lane retirement and other cross-surface contract updates.
- Owns architecture, system boundaries, and review gates across the runtime, tooling, and documentation surfaces.
- **Language Designer:** Owns Precept language design, grammar evolution, and Superpower parser strategy. `docs/PreceptLanguageDesign.md` is the foundational document.
- **Language research co-owner:** Co-owns `docs/research/language/` with George. Knows the comparative studies (xstate, Polly, FluentValidation, Zod/Valibot, LINQ, FluentAssertions, FEEL/DMN, Cedar), expression audit, verbosity analysis, and all PLT references.
- Core architectural discipline: keep MCP tools as thin wrappers, keep docs honest about implemented behavior, and document open decisions instead of inventing values.
- Technical-surface work flows through Elaine (UX), Peterman (brand compliance), Frank (architectural fit), then Shane (sign-off).
- README and brand-spec changes should reflect actual runtime semantics, not speculative future behavior.

## Recent Updates

### 2026-04-25 — Deep parser/lexer architecture analysis for catalog-driven improvements
- Comprehensive source-level analysis of `Pipeline/Lexer.cs` (~850 LOC) and `Pipeline/Parser.cs` (~1,350 LOC).
- Identified 10 vocabulary tables in the parser that should become catalog-derived frozen dictionaries: operator precedence, type keyword mapping, 3 modifier recognition sets, action keyword set, sync token set, display names, state modifier mapping, field modifier dispatch.
- Grammar productions (~50-60% of parser code) correctly stay hand-written — no catalog derivation.
- Confirmed M7 (TextMateScope), M8 (SemanticTokenType), I27 (ConstructSlotKind), and core M9 (slot model) are already implemented — action list overstated these as gaps.
- Confirmed I18 (LeadingToken on ConstructMeta) and I19 (Arrow category) are real gaps.
- Confirmed I24 (no TokenKind.With exists anywhere in codebase).
- Lexer is ~85% catalog-driven. Operator/punctuation char-matching is the remaining 15% but should stay hand-written for performance.
- P1-P4 (highest value refactors) have zero blocking dependencies — all needed catalogs already exist.
- Filed full analysis at `.squad/decisions/inbox/frank-parser-catalog-analysis.md`.

### 2026-04-25 — Parser metadata-drivenness reassessment (correcting original review)
- Shane flagged that my original pipeline review anchored to a pre-existing doc statement ("parser remains hand-written") instead of independently evaluating the v2 parser code. He was right.
- Re-read the entire v2 parser (1,315 lines) and traced every language-knowledge decision. Found ~40-50% of the parser's language knowledge decisions are vocabulary tables that CAN and SHOULD be catalog-derived: operator precedence, type keyword mappings, modifier recognition sets, action keyword sets, sync-token sets.
- The grammar productions (declaration parsing, expression Pratt loop, error recovery, dual-use disambiguation) correctly stay hand-written. My original claim that "the parser should NOT become catalog-driven" was overclaimed — the accurate position is "grammar stays hand-written; vocabulary tables become catalog-derived."
- Key practical impact: for the most common language evolution (new types, modifiers, operators, functions), catalog-driven vocabulary eliminates parser edits entirely.
- Recommended qualifying the catalog-system.md statement and provided a 5-step implementation plan tied to catalog landing order.
- Filed at `.squad/decisions/inbox/frank-parser-metadata-reassessment.md`.

### 2026-04-25 — Comprehensive catalog-driven pipeline architecture review
- Wrote full review to `.squad/decisions/inbox/frank-catalog-metadata-pipeline-review.md` at Shane's request.
- Assessed all 6 pipeline stages for metadata-drivenness: Lexer ~85%, Type Checker ~80% (biggest win), Proof Engine ~70% obligation discovery, Graph Analyzer partial, Parser correctly hand-written (not catalog-derivable), Evaluator ~60% (future).
- Found the catalog-system.md design 90% complete for the metadata-driven vision. Three gaps: (1) FunctionMeta needs evaluation delegate shape, (2) TokenMeta should add TypeKind?/OperatorKind? cross-references, (3) Constructs catalog's non-generative relationship to parser needs explicit documentation.
- String elimination audit: already achieved for all logic paths. No action required.
- Cross-catalog integrity: dependency graph is acyclic; all references via enum values. No circular dependency risk.
- Recommended 7-catalog implementation order respecting dependency graph: Types → Operators → Functions/Operations (parallel) → Modifiers/Actions/Constructs (parallel).
- Key architectural position: parser stays hand-written; Constructs catalog is documentation/validation artifact, not parser-generation artifact. StateModifierKind stays bare (6 members don't justify catalog overhead).

### 2026-04-25 — catalog-system.md: DQ1–DQ4 design question resolutions incorporated
- **DQ1 — ConstructMeta.AllowedIn:** `ConstructMeta` gains `AllowedIn ConstructKind[] = []`. Empty = precept body level; populated = valid parent construct kinds. LS completions filter context-sensitive suggestions using this field. Full shape block and examples added to §8.
- **DQ2 — ActionMeta.AllowedIn:** `ActionMeta` gains `AllowedIn ConstructKind[] = []` — same field name and type as `ConstructMeta`, uniform LS context logic. Action table updated with `AllowedIn` column; all eight verbs (`set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`) declare `[ConstructKind.EventDeclaration]`.
- **DQ3 — DimensionProofRequirement:** New `PeriodDimension { Any, Date, Time }` enum and `DimensionProofRequirement` record added as a third `ProofRequirement` subtype, valid only on `BinaryOperationMeta.ProofRequirements`. Valid-subject table updated to "valid proof requirement types" format. Proof obligation inventory gains two new rows (date±period, time±period). PRECEPT0006 updated to note `DimensionProofRequirement` is only valid on `BinaryOperationMeta`.
- **DQ4 — QualifierAxis + FixedReturnAccessor.ReturnsQualifier:** New `QualifierAxis` enum (`None`, `Currency`, `Unit`, `Dimension`, `FromCurrency`, `ToCurrency`, `Timezone`) added to §2 Types. `FixedReturnAccessor` gains `QualifierAxis ReturnsQualifier = QualifierAxis.None`. Bullet note added: accessors with `ReturnsQualifier != None` return the qualifier value on that axis; LS hover uses this for "returns the currency of this field."

### 2026-04-25 — catalog-system.md: incorporated all 7 catalog gap resolutions
- **Gaps 1–4 (Types + Operations + Modifiers):** `TypeMeta` gains `Traits`, `WidensTo`, `ImpliedModifiers`, `Accessors`. New `TypeTrait` flags enum and `TypeAccessor` discriminated union (`FixedReturnAccessor` subtype). `OperationMeta` becomes an abstract DU with `UnaryOperationMeta` and `BinaryOperationMeta` sealed subtypes; two internal indexes replace the single `_index`; `Lhs`/`Rhs`/`Operand` are now `ParameterMeta`. `ModifierMeta` gains `Subsumes ModifierKind[]` and `ApplicableTo` migrates from `TypeKind[]` to `TypeTarget[]`; new subsections for modifier subsumption and implied modifiers.
- **Gaps 5–6 (Functions + Actions):** `FunctionOverload` and `FunctionMeta` shapes documented with `ParameterMeta[]` parameters and nullable `QualifierMatch?`; business-type overload semantics noted. `ActionMeta.TargetCollectionKind?` replaced by `ApplicableTo TypeTarget[]` with `ProofRequirements[]`. Action table updated to reflect `TypeTarget` entries including `clear`'s dual-target shape.
- **Gap 6 cross-cut — TypeTarget DU:** New `## Supporting Types` section with `TypeTarget`/`ModifiedTypeTarget` discriminated union and shared `IsApplicable` helper.
- **Gap 7 — ProofRequirement system:** New `## Proof Obligations` section: `ProofSubject` DU (`ParamSubject`, `SelfSubject`), `ProofRequirement` DU (`NumericProofRequirement`, `PresenceProofRequirement`), valid-subjects-per-catalog table, object-reference safety note, complete proof obligation inventory. `ProofRequirements[]` field added to `TypeAccessor`, `FixedReturnAccessor`, `FunctionOverload`, `BinaryOperationMeta`, `ActionMeta` shapes.
- **Roslyn layer:** PRECEPT0005 and PRECEPT0006 added to Future Rules table. ProofEngine row in Pipeline Stage Impact updated to reflect catalog-driven obligation model.

### 2026-04-24 — TypeChecker Slice 4 review: Expression Checking Core + Rule Declarations
- **Verdict: APPROVED.** No blockers. 3 observations.
- **G1:** `IsAssignableTo` is defined in this slice but never called. `CheckRuleDeclaration` uses inline `is not BooleanType and not ErrorType` patterns throughout instead of delegating to it. Behaviorally equivalent for all types in play, but the helper is dead code in the slice that introduced it. Will likely be picked up in later slices.
- **G2:** `expectedType` parameter on `CheckExpression` is propagated through `ParenthesizedExpression` recursion but otherwise unused in the switch body. Bottom-up inference design is correct — callers check the returned type — but the parameter name implies bidirectional intent not yet implemented. Consider a comment or `_` discard if it's reserved for future slices.
- **G3:** `InvalidInterpolationCoercion` branch in `CheckInterpolatedString` has no test. `CollectionTypeRef` stubs to `ErrorType` until slice 8, so the branch is currently unreachable. Not a defect, but flag for a regression anchor when slice 8 lands.
- **Checklist:** All 10 items ✓ — `IsMissing` first, 5 dispatch cases + `_` fallback, `TryGetValue` field lookup, `UndeclaredField` with name arg, ErrorType on miss, segments iterated, collection rejection implemented, `IsAssignableTo` correct (unused — G1), `TypeDisplayName` covers all 5 scalars + ErrorType + 3 collection forms + fallback, condition/guard/message all checked with correct TypeMismatch args, ErrorType cascade suppressed, `_rules.Add` present, `RuleDeclaration` wired in `CheckDeclarations`, 9+ plan cases tested plus shape/message extras.

### 2026-04-24 — TypeChecker Slice 3 review: State + Event Registration
- **Verdict: APPROVED.** No blockers. 1 observation.
- **G1:** Duplicate-state + initial interaction: if a state entry is a duplicate AND has `IsInitial`, the initial check runs outside the `else` branch, so a rejected duplicate can still set `_initialState` (if no prior initial exists) or emit `MultipleInitialStates("Foo", "Foo")`. This is defensible error-recovery behavior but not tested. Not a blocker — it only fires in already-broken input.
- **Checklist:** All 9 items ✓ — `RegisterStateDeclaration` iterates `Entries`, skips `Length==0`, builds `StateSymbol` with correct 4 params, first-initial-wins tracking, post-loop `NoInitialState` guard placed correctly after foreach, `RegisterEventDeclaration` iterates `Names` (multi-event), args rebuilt per event name from shared `decl.Args`, `DuplicateArgName` args `(argName, eventName)` match template `{0}`/`{1}`, `MultipleInitialStates` args `(firstInitial, secondInitial)` correct, test suite far exceeds 9 plan cases (27 tests), design doc alignment solid.

### 2026-04-24 — TypeChecker Slice 2 review: Field Registration
- **Verdict: APPROVED.** No blockers. 3 observations.
- **G1:** `SpanOf(Token t)` duplicated between `TypeChecker.cs` and `Parser.cs`. Identical logic, correct in both. Future cleanup candidate — if `SourceSpan`'s contract changes the two copies will diverge.
- **G2:** Most tests use `:` syntax, which produces parse diagnostics. The lexer emits `InvalidCharacter` for `:`, parser error-recovers past it. Tests don't assert `Diagnostics.Should().BeEmpty()` so no false pass — but inputs are subtly broken. `Register_SingleField_NoDiagnosticsEmitted` correctly uses `as` and explains why.
- **G3:** `emptyMods` constructed once outside the name loop — correct for now. All names in a multi-name declaration share the same instance. When modifier validation lands (slice 5), verify this doesn't need to move inside the loop.
- **Checklist:** All 9 items ✓ — `RegisterFieldDeclaration` logic, duplicate handling (first wins), `ResolveTypeRef` dispatch, `ResolveScalarType` 5-scalar mapping, `token.Length == 0` missing check, `SpanOf` arg order, `emptyMods` 12-param order, 7 plan test cases covered, design doc alignment.

### 2026-04-24 — TypeChecker Slice 1 review: Foundation Scaffold
- **Verdict: APPROVED.** No blockers. 4 minor observations.
- All 7 checklist items ✓: CheckSession struct fields, RegisterDeclarations/CheckDeclarations loops, BuildModel merging parser diagnostics, PreceptName extraction, TypedModel constructor arg order, diagnostic builder initialization, StringComparer.Ordinal on dictionaries.

### 2026-04-24 — Full design review: docs.next/ architecture lens (type-checker, spec, temporal, business-domain, READMEs)
- **Verdict: BLOCKED.** Four blockers, all fixable without design changes. B2 requires a design alignment decision.
- **B1:** Language README (`docs.next/language/README.md`) uses NodaTime C# names (`localdate`, `localtime`, `localdatetime`) instead of DSL surface names (`date`, `time`, `datetime`). Also missing `zoneddatetime`.
- **B2:** Spec §3.6 and type-checker doc §4.2a directly contradict on `decimal op number` arithmetic. Spec says widening to `number`; type-checker says type error requiring `approximate()`. Must align — recommend bridging in arithmetic context, widening in assignment context.
- **B3:** Type-checker function catalog table lists 8 of 14 functions. Missing: `floor`, `ceil`, `truncate`, `round()` (0-arg), `approximate`, `pow`. Bridge functions described in Lane Integrity section but absent from the formal catalog. `pow` absent entirely.
- **B4:** Type-checker Deliberate Exclusion says "does not validate typed constant content format — runtime via NodaTime." Temporal design doc specifies compile-time errors for `'2026-02-30'`, `'25:00:00'`, etc. Inconsistent.
- **Clean-room:** Both temporal and business-domain docs pass. "v1" = proposal-internal versions. No agent names, no `.squad/`, no `src/Precept/`. `PreceptTypeChecker.*` references in Implementation Scope are #118 context (appropriate).
- **Structural alignment:** Type-checker doc follows compiler README pattern (Overview → Design Principles → Architecture → domain → Error Recovery → Consumer Contracts → Deliberate Exclusions → Cross-References → Source Files). CheckSession struct matches Scanner/ParseSession pattern exactly.
- **Noteworthy:** T1 (flat symbol tables) decision is exceptionally well-argued — consumer path tracing eliminates phantom abstractions. Spec §3.3 and type-checker doc both missing `exchangerate` shape in typed constant resolution table. `PriceType()` and `ExchangeRateType()` records may need qualifier parameters for denominator cancellation.
- Full review at `.squad/decisions/inbox/frank-docs-next-design-review.md`.

### 2026-04-23 — v2 AST & parser design review (SyntaxNodes.cs + parser.md)
- **Verdict: BLOCKED.** Two blocking issues; clean room is fully clean; AST-to-doc sync is tight.
- **B1:** No method-call expression support (`expr.method(args)`). The vision specifies `.inZone(tz)` as a temporal accessor, but the AST has no `MethodCallExpression` and the Pratt Led has no `LeftParen` handler. Decision needed: add method-call support or redesign as prefix function `inZone(expr, tz)`.
- **B2:** Transition row action-chain grammar is internally inconsistent in parser.md — the mapping double-consumes the outcome arrow and disagrees with the stateless hook action-chain format.
- **Clean room:** Zero v1 contamination. All v2 renames verified: `OptionalModifier`, `AccessModeDeclaration`, `IsSetExpression`, `StatelessEventHookDeclaration`, `TypeQualifier`.
- **Completeness:** All 14 declaration forms, 8 actions, 3 outcomes, 3 type refs, 14 modifiers, 16 expression forms, 25 scalar type kinds, 6 state modifiers present and accounted for.
- **Sync:** `SyntaxNodes.cs` and `parser.md` Complete Node Catalog match exactly — property names, types, enum values all identical.
- **Non-blocking observations:** (G5) nud Identifier peek logic is non-standard Pratt; (G7) event arg syntax disagrees between vision (parens) and parser/spec (`with`); (G8) guarded write ordering disagrees between vision (guard-before-mode) and parser (guard-after-fields); (G9) `LeftParen` in BP table but no Led handler.
- Full review filed at `.squad/decisions/inbox/frank-v2-ast-review.md`.

## Learnings

### 2026-04-25 — Type checker catalog-driven analysis (clean-room)
- v2 type checker is ~390 LOC + ~90 LOC OperatorTable. 14 of 22 methods are correctly hand-written structural machinery. 8 methods hardcode domain knowledge that should be catalog-derived.
- **OperatorTable is the biggest win.** The entire 90-LOC `OperatorTable.cs` is a legacy bridge — `Operations.FindCandidates()` and `Operations.FindUnary()` already provide O(1) frozen dictionary lookups for all 130+ operation combinations, with bidirectional support, qualifier disambiguation, and proof requirements. Delete and replace wholesale.
- **ResolvedType needs a `Kind` property.** Every catalog lookup starting from a `ResolvedType` currently requires pattern-matching. Adding `abstract TypeKind Kind` to `ResolvedType` (~25 one-line overrides) is the critical-path enabler for all catalog integrations. Without it, every refactor has an extraction tax.
- **`IsAssignableTo` widening rules duplicate `TypeMeta.WidensTo`.** Two `if` chains hardcode `integer→decimal` and `integer→number`. The catalog already declares these. Wire before temporal/business-domain types land to avoid a growing `if` forest.
- **`TypeDisplayName` and `BinaryOpDisplayName` are vocabulary tables.** Both are switches mapping types/ops to strings. Both have exact catalog equivalents (`TypeMeta.Token.Symbol`, `OperatorMeta.Token.Symbol`). Both will grow as types/ops are added. Catalog-drive them to eliminate maintenance.
- **`ResolveScalarType` is the deferred high-impact item.** Today it's a 5-arm switch (stubs temporal/business to ErrorType). When those types land, it becomes 25 arms with qualifier extraction. Wire to `Types.GetMeta()` at that point — the qualifier shape validation and type construction become catalog-informed.
- **Structural code stays structural.** AST dispatch (expression switch, declaration loop), symbol-table population, error recovery (IsMissing, ErrorType propagation), literal shape analysis (dot/exponent), and model assembly are all generic machinery — no catalog derivation warranted.

- **Method-style accessors are a language decision, not just a parser gap.** If `.inZone(tz)` is the only case, a prefix-function redesign is defensible. If the language ever needs more (`.format(pattern)`, `.withZone(tz)`), method-call Led support is the correct investment. The decision should be made now, not deferred.
- **Action chain format must be consistent across all declaration types.** Transition rows, stateless hooks, and state actions all contain action chains. The parser grammar mapping must use one mechanism, not different ones per declaration form.
- **Vision doc and spec/parser can silently diverge on surface syntax.** The event arg syntax (parens vs `with`) and guarded write ordering (guard position) were different across files with no explicit decision record. Cross-referencing all three documents (vision, spec, parser design) in every review pass is non-negotiable.
- **Pratt parser nud peek patterns are a code smell.** The standard approach (nud returns atoms, Led handles all infix/postfix) is more extensible and avoids ambiguity about which dispatch point handles a given token.

### 2026-04-19 — Drafted `docs/TypeCheckerDesign.md` (Track B design doc)
- Canonical design document describing the CURRENT implemented architecture of `PreceptTypeChecker`.
- 6-partial decomposition (3,783 LOC): Main, TypeInference, Narrowing, ProofChecks, Helpers, FieldConstraints.
- 7 design decisions with rationale, alternatives rejected, and tradeoffs accepted.
- Research-grounded: references both architecture survey (Frank) and implementation patterns (George).
- Philosophy-traced: 6 principles each linking to `docs/philosophy.md`.
- Integration points table covering proof engine (~7 call sites), language server (3 consumers), MCP (4 tools), and compiler pipeline.
- Re-evaluation triggers from research (2K LOC per file, 5K total, phase formalization, incremental compilation).
- Decision filed at `.squad/decisions/inbox/frank-typechecker-design-doc.md`.

### 2026-04-19 — Design review of issue #118: PreceptTypeChecker decomposition
- **Verdict: APPROVED WITH NOTES.** Proposal is architecturally sound and ready for implementation planning.
- Verified all 67 class-level methods against proposed 6-file grouping. Method inventory is complete — no methods missing, no phantom methods.
- Actual file size: 3695 lines (proposal said ~3200 — likely from a pre-PR#108 baseline).
- Main file size underestimated by ~100-200 lines (computed ~1132 methods-only vs estimated ~950-1050). Not blocking.
- ProofChecks overestimated by ~100-150 lines (computed ~404 vs estimated ~500-550). The vague "C94-C98 helpers" bullet is redundant with individually named methods.
- Copy helpers (CopyRelationalFacts, CopyFieldIntervals, CopyFlags, CopyExprFacts) correctly placed in Helpers — called from both Main and Narrowing, genuinely cross-cutting.
- 7 non-blocking notes filed: size estimates, fully explicit Helpers inventory, front-matter type placement in acceptance criteria, per-file `using` directives.
- Full review at `.squad/decisions/inbox/frank-issue-118-design-review.md`.
- **Key architectural learning:** partial class decomposition of a static class is the correct pattern for large single-concern files in this codebase. The zero-visibility-change property is the key enabler.

### 2026-04-10 — Issue #31 shipped
- PR #50 merged to main (squash SHA `305ec03`). Issue #31 closed. 775 tests passing.

### 2026-04-10 - PR #50 final review — Issue #31 keyword logical operators

**Verdict: APPROVED.** All 8 slices verified. 774/774 tests passing. Full review filed at `.squad/decisions/inbox/frank-31-review.md`.

**Key architectural confirmation:** `BuildKeywordDictionary()` uses `symbol.All(char.IsLetter)` to auto-include `and`/`or`/`not` via reflection. Registered with `requireDelimiters: true` — whole-word safety against prefixes like `android` is architectural, not ad-hoc. Consistent with `contains` precedent.

**ApplyNarrowing() confirmed updated** — all three arms (`"not"`, `"and"`, `"or"`) correctly updated at their respective pattern-match sites.

**MCP is catalog-driven** — zero hardcoded operator strings. `TokenSymbol` attribute update is the only change needed; LanguageTool picks it up automatically.

**Minor non-blocking gap:** `LanguageToolTests.LogicalOperatorsAreKeywordForms` asserts `&&` and `||` are absent, but does not assert `!` is absent. Not blocking because `!` was removed from the tokenizer entirely and has no path to the inventory.

**Learnings:**
- The attribute-driven `BuildKeywordDictionary()` + `requireDelimiters` is the correct mechanism for any future operator-to-keyword migrations. No tokenizer code changes are needed — only the `[TokenSymbol]` attribute value.
- When reviewing logical operator migrations, `ApplyNarrowing()` is the easy-to-miss site — it uses pattern-match syntax (`{ Operator: "..." }` and string comparison) separately from the main type-checker switch. Both were correctly updated here.

### 2026-04-10 - Issue #31 Slice 8 docs — PreceptLanguageDesign.md sync
- Updated `docs/PreceptLanguageDesign.md` on branch `squad/31-keyword-logical-operators` to reflect #31 as fully implemented.
- **Sections updated:** (1) "Keyword vs Symbol Design Framework" — migration table heading changed from "Implementation Pending" to "Implemented", table columns renamed from "Current/Target/Pending" to "Symbolic/Keyword/Implemented", removed "Until implementation" paragraph; (2) Reserved keywords list — removed "Pending additions (#31)" note, added `and`, `or`, `not` to the main keyword list; (3) Nullability and narrowing — updated Pattern 1 and Pattern 2 code examples from `&&`/`||` to `and`/`or`, updated prose labels and inline descriptions to match; (4) Expressions section — removed `pending migration` annotations, updated `!` → `not` and `&&`/`||` → `and`/`or` in bullet list; (5) Event asserts section and Minimal Example — updated `&&` → `and` in both code examples.
- Committed as `03d50b7`, pushed to origin. PR #50 Slice 8 docs checkbox now checked.

### 2026-04-10 - Philosophy Refresh Assessment — full research corpus review
- Systematically reviewed all 32 files in `docs/research/language/` against the rewritten `docs/philosophy.md` (commits `dd14a4c` entity-first rewrite, `185e20b` governance strengthening).
- Assessment written to `docs/research/language/philosophy-refresh-assessment.md`.
- **Findings:** 16 files aligned, 14 need framing updates, 2 need significant refresh (`xstate.md`, one framing gap). No file contradicts the philosophy. Two cross-cutting problems dominate: (1) zero files use "governed integrity" — the philosophy's unifying principle; (2) 14 files don't consider data-only/stateless entities.
- Identified 4 new research gaps: constraint patterns for data-only entities, Inspect/Fire semantics for stateless instances, governance-vs-validation positioning evidence, MDM/industry-standard comparison depth.
- Recommended 16-file refresh order prioritized by impact × dependency.
- Quality bar (`computed-fields.md`) confirmed as excellent research but needs minor framing update for governed integrity language and stateless entity implications.
- Gold standard files: `entity-first-positioning-evidence.md`, `entity-modeling-surface.md`, `static-reasoning-expansion.md`.

### 2026-04-08 - PR #48 code review — data-only precepts (Slices 1–7)
- Reviewed `feature/issue-22-data-only-precepts` (commits e0eac05–833422e) against all 12 Q&A design decisions from `.squad/decisions/decisions.md`.
- **Verdict: CHANGES REQUESTED.** All 12 decisions faithfully implemented (11 PASS, 1 ISSUE — Decision 11 sample files absent). Architecture is sound; no silent drift from design.
- **Blocking issue:** `docs/PreceptLanguageDesign.md`, `docs/RuntimeApiDesign.md`, and `docs/McpServerDesign.md` are entirely missing stateless-precept documentation. This violates the project's non-negotiable doc-sync mandate.
- **Required changes:** (1) Update three design docs before merge. (2) Add multi-event C49 test (Decision 10 contract untested: 3 events → 3 separate warnings).
- **Code quality nits:** `DiagnosticCatalog.C55.MessageTemplate` used instead of `FormatMessage()` at PreceptRuntime.cs:1824; `currentState!` null-forgiving in MCP tool stateful branches; null-forgiving on `InitialState!` in `CollectCompileTimeDiagnostics` relies on implicit guard.
- **Recommendations (non-blocking):** Add explicit `IsStateless` guard before "2. Validate initial state asserts" block; add at least one sample `.precept` file (Decision 11 placeholder names are fine); add `in State edit all` engine test; replace `MessageTemplate` with `FormatMessage()` on C55.
- Full review filed at `.squad/decisions/inbox/frank-pr48-review.md`.

### 2026-04-08 - Issue #22 semantic rules review (4 spawns)
- Reviewed and rewrote #22 semantic rules for stateless precepts across 4 spawns: (1) decomposed the "states, events, and transitions forbidden" rule — states tautological, transitions structurally impossible, events the only real boundary; (2) deep analysis of stateless event boundary — confirmed binary taxonomy (data vs. behavioral), no middle tier, single-state escape hatch is correct not ceremonial; (3) warning model research — audited C48–C53 diagnostic infrastructure, external precedent survey; (4) full #22 issue body rewrite.
- **Owner decision (Shane):** events-without-states = warning (not error). C50 upgraded from hint to warning for consistency. Binary taxonomy confirmed.
- Wrote `frank-stateless-event-boundary.md` and `frank-warning-model-research.md` to decisions inbox; both merged into decisions.md by Scribe.

### 2026-04-08 - Language research corpus completed
- Closed the domain-first corpus with the final sweep in `3cc5343`, keeping `docs/research/language/domain-map.md` as the canonical map and leaving proposal bodies untouched.
- The finished corpus now spans Batch 1 `54a77da`, Batch 2 `48860ae`, and the final corpus/index pass `3cc5343`, while preserving `computed-fields.md` as the quality bar and keeping horizon domains visible.

### 2026-04-08 - Language research corpus map locked
- Preserved `docs/research/language/domain-map.md` as the canonical research-corpus map and kept the corpus organized by domain rather than by issue.
- Held the session guardrails: no proposal-body edits during corpus curation, `computed-fields.md` remains the quality bar, and horizon domains stay in scope even before proposals exist.
- The alternate map-file experiment was folded back out; Batch 1 research landed in `54a77da`, Batch 2 landed in `48860ae`, and only Batch 3 plus the final README/index sweep remain.

### 2026-04-07 - PR #35 merge: finalize README cleanup and record Squad decision
- Merged PR #35 (chore: finalize README cleanup and record Squad decision) to `main`.
- Branch `chore/upgrade-squad-latest` carried 2 commits: (1) Scribe post-task recording for PR #34 merge, and (2) README Quick Example refactoring (removed explanatory hedge, copyable DSL block, replaced markdown image syntax with fixed-width HTML img tag).
- Cleaned up merged inbox entries from `.squad/decisions/inbox/` and updated `.squad/agents/j-peterman/history.md` with team update.
- Workflow: Clean working tree before push (separated uncommitted Squad sync artifacts from committed README cleanup work), explicit PR creation with `gh pr create --base main --head sfalik:chore/upgrade-squad-latest`, merged with merge-commit strategy.
- User directive honored: branch retained locally and remotely per user request (NOT deleted post-merge).
- Verified zero scope creep: 81 additions, 3 files changed (README.md, .squad/decisions.md, .squad/agents/j-peterman/history.md), no unrelated code changes.
- Co-authored-by trailer included in original commits.

### 2026-04-07 - PR #34 merge with Squad config and README image fixes
- Merged PR #34 (chore: upgrade Squad configuration and fix README image links) to `main`.
- Branch `chore/upgrade-squad-latest` carried both architectural Squad updates and related README.md image path corrections.
- Image references corrected: `brand/readme-hero.svg` → `design/brand/readme-hero.svg` (added `design/` prefix per canonical asset layout).
- Workflow: Committed image fix with Co-authored-by trailer, pushed with `-u` for upstream tracking, created PR via `gh pr create` with explicit `--base` and `--head` flags, merged with merge-commit strategy, deleted remote branch post-merge.
- Remote branch cleaned up post-merge; no uncommitted changes remaining.
- User directive captured: keep branch open for future work (logged in decisions).
- Verified zero scope creep: only Squad config + image link fixes, no unrelated code changes.

### 2026-04-05 - Named rule proposal converged
- Reached the final proposal framing for issue #8: rule <Name> when <BoolExpr>, with reuse allowed in when, invariant, and state assert, but not on <Event> assert.
- The standing architecture filter now treats philosophy, non-programmer readability, and configuration-like legibility as explicit review criteria instead of secondary polish.

### 2026-05-01 - Expression feature research & proposals
- Produced comprehensive expression-surface research at `docs/research/dsl-expressiveness/expression-feature-proposals.md`.
- Confirmed current expression limitations via MCP compile: no ternary (`?` parse error), no `.length` on strings (PRECEPT038), no function calls, no named guards.
- Proposed 7 features across 3 waves: Wave 1 (ternary + `.length`), Wave 2 (named guards + conditional invariants), Wave 3 (`.contains()` + numeric functions), Future (computed fields).
- Extended research base beyond existing 6-library set to include FEEL/DMN, Drools DRL, and Cedar (AWS). FEEL is the strongest comparator for business-rule DSL expression design — it has ternary, string functions, numeric functions, and range membership, all of which Precept currently lacks.
- Updated `docs/research/dsl-expressiveness/README.md` with the new file entry.
- Decision note filed at `.squad/decisions/inbox/frank-expression-research.md`. No implementation authorized — Shane sign-off required per wave.

### 2026-04-05 - Proposal bodies expanded for issues #11-#13
- Expanded GitHub issues #11, #12, and #13 into fuller proposal narratives with before/after Precept examples, reference-language snippets, and explicit architectural cautions.
- Logged the wave placement and guardrails in .squad/decisions.md so the issue bodies stay aligned with keyword-anchored flat statements and first-match routing.

### 2026-04-05 - Trunk consolidation dissent logged
- Audited the repo topology and argued for force-promoting 'feature/language-redesign' to 'main' because 'main' still carries only placeholder history.
- The team did not adopt that path: Uncle Leo's review blocked direct trunk replacement, so Frank's recommendation now stands as a documented dissent pending Shane sign-off.

---

2026-04-05T03:20:00Z: Steinbrenner applied branch protection to main (pull requests required, force pushes/admin only, no branch deletion).

### 2026-04-08 - Issue #17 computed fields proposal revamp
- Revamped `temp/issue-body-rewrites/17.md` to incorporate all findings from the research document and all 7 team re-reviews.
- Resolved all 6 semantic contracts from the research: scope boundary, recomputation timing (Fire+Update+Inspect), nullability/accessor safety, dependency ordering, writeability/external input, tooling surface.
- Used Steinbrenner's default-if-skipped options: (a) conservative nullable rejection, (a) `.count` only for collection accessors, (a) Terraform-model external input rejection, (a) single pass after all mutations.
- Added 3 new locked decisions (#9 nullable conservative rejection, #10 external input rejection, #11 `.count`-only collection accessor restriction).
- Expanded locked decision #8 from Fire-only to all three pipelines citing George's `CommitCollections` analysis.
- Added `->` dual-use acknowledgment note to locked decision #1.
- Replaced "Open Questions: None" with a "Resolved decisions" table mapping all 6 research contracts to locked decisions.
- Changed wave from "Wave 4: Compactness + Composition" to "Composition" framing per Steinbrenner's recommendation.
- Added new sections: Tooling surface (hover, MCP compile, inspect/fire, completions), Teachable error messages (9 scenarios), Before/after example using travel-reimbursement, Interleaved fields examples, `->` reading guide callout.
- Surfaced the research's cross-category positioning claim in Motivation section.
- Added spreadsheet mental model sentence to Summary.
- Linked research document in Research and rationale links.
- Expanded Explicit exclusions with: silent default fallbacks, writable computed fields, null-coalescing operator, unsafe collection accessors. Merged fixed-point evaluation with cycle detection.
- Expanded acceptance criteria from 19 to 37 items, organized by category (parser, type checker, runtime, LS, MCP, general). Added all items from Soup Nazi's must-add and should-add lists plus Uncle Leo's blockers.
- Expanded implementation scope from 10 to 16 items covering nullable validation, accessor safety, Update pipeline, external input rejection, LS filtering, MCP specifics.

### 2026-04-08 - Issue #22 semantic rule review for Shane
- Reviewed the "states, events, and transitions forbidden in stateless precepts" semantic rule at Shane's request before implementation.
- Confirmed Shane's critique that the "states forbidden" and "transitions forbidden" parts are tautological/redundant — the real design decision is the events boundary.
- The issue body already acknowledges the tautology in the exclusions section ("this exclusion is tautological") but contradicts itself in the semantic rules section by framing it as a prohibition. Recommended rewrite to eliminate the contradiction.
- Key architectural insight: the only non-trivial semantic boundary is events. Events are forbidden because they dispatch transitions between states (a semantic dependency), not because of a tautology or structural impossibility. This is a real design choice with a potential future evolution path (stateless event-triggered mutations), deliberately closed for now.

### 2026-04-08 - Stateless event boundary deep analysis
- Shane flagged the revised #22 semantic rules as "a slippery slope." Conducted deep analysis of whether events should be forbidden in stateless precepts.
- **Recommendation: events require states. The binary taxonomy (data vs. behavioral) is correct.** The "middle tier" (data + events, no states) is a category error, not a missing feature.
- Key insight: events in Precept are not generic triggers — they are routed through states via `from State on Event -> Outcome`. The `from` is the routing context, not decoration. Events without routing are a different concept ("commands") that would need parallel syntax, semantics, and tooling.
- The single-state escape hatch (`state Active initial`) is correct, not ceremonial: it communicates "this entity has exactly one behavioral mode" — a true structural statement, not dummy ceremony. Cost: 1 line.
- Precedent survey confirms no surveyed system (Terraform, SQL, DDD, GraphQL, Protobuf) provides "data entity with named commands but no lifecycle." Every system with named commands routes them through a behavioral layer.
- The slippery slope concern is acknowledged but self-correcting: the escape hatch is cheap enough that demand for stateless events won't reach critical mass.
- Decision filed at `.squad/decisions/inbox/frank-stateless-event-boundary.md`.

### 2026-04-24 — Precept.Next contract consistency review (docs.next vs src/Precept.Next/Pipeline)
- **Verdict: 4 BLOCKERS, 3 GAPS, 2 NOTES. TypeChecker implementation is NOT safe to start.**
- **B1 (TypedModel stub):** TypedModel has only `Diagnostics`. The doc specifies 12 fields plus 27 ResolvedType subtypes, 5 symbol types, 9 resolved declaration types, TypedExpression. None exist in code. Must be defined before TypeChecker.Check can be implemented.
- **B2 (Root nullability):** `SyntaxTree.Root` is `PreceptNode?` (nullable). Type-checker doc guarantees it is always non-null. Hard contradiction. Fix: change to `PreceptNode` (non-nullable) and enforce the guarantee at the Parser boundary via IsMissing synthesis.
- **B3 (Missing DiagnosticCodes):** DiagnosticCode enum has 6 Type-stage codes. Type-checker doc specifies at least 13 more (DuplicateFieldName, DuplicateStateName, DuplicateEventName, DuplicateArgName, UndeclaredState, UndeclaredEvent, MultipleInitialStates, NoInitialState, CaseInsensitiveStringOnNonCollection, InvalidModifierBounds, UnguardedCollectionAccess, UnguardedCollectionMutation, NonOrderableCollectionExtreme, plus more in temporal/business-domain sections).
- **B4 (SourceSpan→SourceRange bridge):** AST nodes carry SourceSpan (offset/length). Diagnostics require SourceRange (line/column). TypeChecker.Check(SyntaxTree) has no source text, no line map, no token stream. No conversion utility exists. Three options: (a) add source text param to Check, (b) store SourceRange on SyntaxNode (Parser already has it from Token), (c) unify Diagnostic to SourceSpan. Decision must be documented before implementation.
- **G5/G6:** GraphResult and ProofModel are also single-field stubs. Don't block TypeChecker but block downstream stages.
- **G7:** All three stage classes (TypeChecker, GraphAnalyzer, ProofEngine) throw NotImplementedException — correct scaffolding state.
- **N8:** compiler/README.md says "GraphModel"; code and all other docs say "GraphResult". One-word fix.
- **N9:** Pipeline-artifacts type strategy table lists DiagnosticStage as "Parse, Type, Graph, Proof" — missing "Lex". Code has 5 values including Lex (used by the working Lexer).
- Full review filed at `.squad/decisions/inbox/frank-precept-next-contract-review.md`.
- **Key file paths:** `src/Precept.Next/Pipeline/TypedModel.cs`, `SyntaxTree.cs`, `DiagnosticCode.cs`, `Diagnostics.cs`, `Diagnostic.cs`, `SourceSpan.cs`, `Token.cs`, `Compiler.cs`, `TypeChecker.cs`.
- **Compiler.cs pipeline order matches docs exactly.** CompilationResult shape matches docs exactly. Both are clean.

## Learnings

### 2026-04-25 — Catalog I-items batch (I8, I9, I12, I23, I26, I32, I35)
- **`init` property on abstract record base:** For optional metadata (e.g., `MutuallyExclusiveWith`), use `init` property with empty-array default. Keeps all construction code unchanged; callers set only where meaningful.
- **Cross-cutting fields on base, not DU subtypes:** Consumers need uniform access across modifier kinds without casting.
- **`QualifierAxis` precision:** Period's temporal dimension specifier (`TemporalDimension`) is not a UCUM unit (`Unit`). Must be distinct enum values.
- **`GraphAnalysisKind` drives analyzer dispatch:** Catalog specifies what to check; analyzer dispatches on enum. No analyzer code encodes per-modifier knowledge.
- **`TypeKind.Error` as sentinel:** Making `TypeTarget.Kind` nullable is the correct fix, but it's a breaking change requiring full consumer audit.
- **Rename tax vs doc comment:** `AllowedIn` is accurate; semantic clarification via doc comment beats a rename touching all consumers.
- **Binary proof requirements need two subjects:** Unary `DimensionProofRequirement` is wrong shape for UCUM dimensional homogeneity (two operands must match).



- **A doc guarantee about non-nullability is worthless if the type signature allows null.** "Always non-null" in prose and `T?` in code is a hard contradiction. The type system is the contract; prose is commentary on it. Fix the type; don't rely on the comment.
- **SourceSpan/SourceRange coordinate model must be decided at architecture time, not deferred.** AST offset/length and diagnostic line/column are different coordinate systems. If stages emit diagnostics, the bridge must be designed and accessible before the first stage is implemented. Token carries both — don't discard line/column at parse time if downstream stages need it.
- **Pre-implementation scaffolding should define model stubs that match their doc specs, not single-field placeholders.** A stub with only Diagnostics makes it impossible to write tests against the stage contract. Even empty symbol tables and empty arrays are better scaffolding than a wrong shape.

- **Cross-document function catalogs must be a single source of truth.** The type-checker doc's Lane Integrity section described bridge functions (approximate, floor, ceil, truncate, round) that the same document's formal catalog table didn't list. When a function exists in narrative prose but not in the implementer's contract table, it's invisible to implementation. One canonical table, referenced everywhere else.

- **Widening rules and lane bridge rules occupy different scopes and must not be conflated.** `decimal → number` widening for *assignment* (putting a decimal value into a number slot) is safe and lossless in one direction. `decimal op number` *arithmetic* produces a `number` result, silently losing decimal precision. These are different operations with different safety properties. The spec conflated them into one "widening" rule. Architecture review must always check whether a type system rule that's correct for assignment is also correct for arithmetic.

### 2026-04-24 — Slice 5 review: Numeric literals + scalar arithmetic + OperatorTable

**Verdict: APPROVED.** No blockers. 6 observations.

**G1: OperatorTable design alignment gap — static class, not catalog.** The implementation uses a plain `static class OperatorTable` with `ResolveBinary(BinaryOp, ResolvedType, ResolvedType) → ResolvedType?` switch logic. This diverges from catalog-system.md (envisions `OperatorKind` enum + `GetMeta` + `All` + `Create` pattern) and type-checker.md T7 (says operators validated via catalog system). However, the static-class approach is architecturally sound. The catalog pattern requires an enum of all operator-type combinations — for the full temporal + business-domain operator table, this would be hundreds of entries, making an enum impractical. `ResolveBinary` encapsulates dispatch without per-combination enumeration. **Recommendation:** Keep `OperatorTable` as a static class. Before Slice 6, update catalog-system.md Operators (planned) section and type-checker.md T7 to describe the actual `ResolveBinary` dispatch table pattern, explaining why per-combination enumeration is impractical at scale. The "blocked on TypeKind" note in catalog-system.md is also stale — `TypeKind` is explicitly listed as NOT earning a catalog, yet both Functions and Operators are "blocked" on it. The actual type system uses `ResolvedType` record hierarchy. Resolve this contradiction in the same doc update.

**G2: Null context for binary operands drops operator-peer inference.** `CheckBinaryExpression` passes `null` expectedType to both operands. The type-checker doc (§4.2a Numeric Lane Resolution) lists "operator peer" as a valid context source — meaning `IntegerField + 42` should resolve `42` as `integer` from the peer's type. The implementation instead emits "cannot determine numeric type from context" for all bare number literals in binary expressions. This is a known, deliberate limitation for Slice 5, documented clearly in test names and comments. **Recommendation:** Add a `// TODO: propagate peer type as context per type-checker.md §4.2a` comment at the `CheckExpression(expr.Left, null)` / `CheckExpression(expr.Right, null)` call sites so the gap is tracked in code, not just in tests.

**G3: TypeMismatch message framing is awkward for binary operator errors.** The diagnostic uses `TypeDisplayName(left.Type)` as "expected" and `BinaryOpDisplayName(expr.Op) + " on " + TypeDisplayName(right.Type)` as "found" — producing messages like `Expected 'decimal', found '* on number'`. The expected/found framing doesn't map naturally to operator type errors. A more natural form would be `'decimal' and 'number' are incompatible for '*'`. Not blocking — the message contains the right information and the template will likely be revisited when Slice 6 adds comparison operators.

**G4: No integration test for `integer + number → NumberType` widening.** OperatorTable unit tests cover this path, but no TypeCheckerTests integration test exercises the full path (field resolution → OperatorTable → widening → condition type check). Minor gap.

**G5: `BinaryOpDisplayName` falls back to `op.ToString()` for non-arithmetic operators.** Produces enum member names ("Equal", "And") in diagnostics. Acceptable for Slice 5 since these operators return `null` from `ResolveBinary`, but Slice 6 must add display names for all operators it implements.

**G6: `CheckNumberLiteral` silent ErrorType path for non-numeric expectedType.** When `expectedType` is e.g. `BooleanType`, the method returns `ErrorType` without emitting a diagnostic, with a comment "caller handles the mismatch report." This path is currently unreachable from `CheckBinaryExpression` (which passes `null`) and `CheckRuleDeclaration` (which passes `BooleanType` to `CheckExpression`, not directly to `CheckNumberLiteral`). Defensible for future callers (default value checking), but unreachable and untested today.

**Correctness: All arithmetic rules correct.** Integer×integer→integer, decimal×decimal→decimal, number×number→number, integer×decimal→decimal (commutative), integer×number→number (commutative), decimal×number→null (type error), string+string→string (+ only), ErrorType propagation. `IsAssignableTo` correctly implements integer→decimal and integer→number widening without decimal→number. `CheckNumberLiteral` literal shape classification (whole/fractional/scientific) and target-type validation are correct per the design doc.

**Test coverage: Thorough.** 34 OperatorTableTests covering all implemented paths including commutative checks, ErrorType propagation, deferred-operator null returns, and CommonNumericType. 12 TypeCheckerTests for Slice 5 covering null-context diagnostics, field-based arithmetic, widening, cross-lane errors, string concatenation, ErrorType cascade suppression. All 632 tests pass.

**Key architectural assessment for Shane:** OperatorTable should NOT become a full catalog. Keep the static class with `ResolveBinary` as the dispatch mechanism. Update the design docs to reflect reality. The catalog pattern (enum + GetMeta + All + Create) is designed for registries where multiple external consumers iterate all members to present them — operators are an internal dispatch mechanism, not a user-enumerable vocabulary. The evaluator (consumer #2) will call the same `ResolveBinary` (or a parallel `EvaluateBinary`) rather than iterating `All`. The MCP `precept_language` tool's Operators section can be served by a derived summary, not a catalog enumeration.

- **READMEs are the first thing readers see and the last thing authors check.** The language README had NodaTime C# names (`localdate`) instead of DSL surface names (`date`) despite the temporal design doc being internally consistent. Navigation READMEs must be reviewed against the docs they describe, not written once and forgotten.

- **Deliberate Exclusions must be versioned against the doc's own type inventory.** The type-checker doc's exclusion "does not validate typed constant content format" was correct when the doc only covered primitives. But the doc now includes temporal and business-domain types in its ResolvedType hierarchy and typed constant resolution table — the exclusion became stale. When a doc's scope expands, its exclusions must be re-evaluated.

- Design review gate formalization (2026-04-19): Formalized the two-track design review model across 7 files (CONTRIBUTING.md, ceremonies.md, copilot-instructions.md, squad.agent.md, frank/charter.md, proposal-review/SKILL.md, decisions.md). Track A = issue-comment reviews; Track B = issue + inline PR review comments on design doc markdown. Owner sign-off is the universal completion gate. For Track B, all inline review threads must also be resolved. Key process files: `CONTRIBUTING.md` § 3. Design Review (canonical), `.squad/ceremonies.md` (ceremony definition), `.squad/skills/proposal-review/SKILL.md` (reviewer workflow). The implementation gate in `squad.agent.md` now checks design review status before authorizing implementation planning.

- ProofEngineDesign.md (2026-04-17): Created `docs/ProofEngineDesign.md` as the dedicated design doc for the non-SMT proof engine (Slices 11–15). Key doc structure decisions: (1) organized as a 5-layer architecture (sequential flow → intervals → interval inference → relational → conditional synthesis) rather than by slice number — layers communicate the conceptual stack, slices are implementation ordering; (2) included the full `TryInferInterval` dispatch table because it's the contract that implementers code against; (3) documented design decisions with explicit alternatives-rejected and tradeoffs-accepted per CONTRIBUTING.md requirements; (4) cross-referenced from both PreceptLanguageDesign.md (divisor-safety section) and ConstraintViolationDesign.md (C93 entry). Also updated PreceptLanguageDesign.md to replace the stale "compound expressions assumed satisfiable" statement with a reference to the proof engine.

- Non-SMT proof stack implementation planning (2026-04-17): Authored the 5-slice implementation plan (Slices 11–15) for PR #108, extending divisor safety with sequential assignment flow, interval arithmetic, relational inference, and conditional proof synthesis. Key planning insights: (1) George's "skip sign analysis, go straight to intervals" recommendation is correct — intervals subsume signs, saving ~100 lines of redundant code; (2) the 4 existing `Check_DivisorCompound_*_NoWarning` tests that document Principle #8 conservatism will ALL flip when interval analysis lands — they were not "passing tests" but "known gaps documented as tests"; (3) string-encoded interval markers (`$ival:key:lower:lowerInc:upper:upperInc`) in the existing symbol table avoid threading a second dictionary through the entire narrowing pipeline — minimal API churn; (4) relational markers (`$gt:A:B`) only need strict inequality to prove nonzero — `>=` alone is insufficient because `A >= B` allows `A == B → A-B == 0`; (5) conditional expression proof synthesis is essentially free — it's 5 lines inside `TryInferInterval` (hull of both branches) plus dedicated test coverage. Total estimate: ~340 new + ~60 changed lines, 48 new + 5 updated tests — 30% smaller than George's original ~500+80 estimate because intervals subsume sign analysis.

- Proof technique re-evaluation — Precept execution model grounding (2026-04-17): The team's proof-technique analyses (sign analysis, intervals, relational inference, etc.) were framed using general PL static analysis concepts — join rules, widening, fixpoint computation, branch reconvergence, control-flow graphs — that DO NOT EXIST in Precept. Precept has no loops, no control-flow branches (only `if/then/else` as an expression ternary), no scope nesting, no user-defined functions, and no aliasing. The execution model is: guards select a row → actions execute sequentially left-to-right → done. This means every proof technique is a SINGLE FORWARD PASS, not an iterative fixpoint. Sign analysis: ~100-120 lines (two lookup tables, no iteration). Interval arithmetic: ~130-150 lines (forward propagation only, no widening). Relational inference: ~55-70 lines (declared facts from rules, kill on mutation, no transitive closure). Total for all techniques: ~350-415 lines, not the ~1000+ estimated under general PL framing. Key lesson: ALWAYS derive complexity from the actual language model, not from academic literature about general-purpose languages. The absence of loops and branches eliminates 80%+ of the machinery those techniques normally require.

- Layer 3 sequential symbol-table evaluation (2026-04-17): Full sequential symbol-table update in action chains is NOT recommended — false-positive surface on non-literal assignments (`set Rate = Rate * 2`) is too broad because `ApplyNarrowing` cannot derive proofs from arithmetic output. Recommended: Layer 3a (literal-only proof invalidation) — update proofs only when a `set` assigns a literal. Catches `set Rate = 0 -> set X = Amount / Rate`. Zero false positives. The current snapshot approach violates Principle #8 spirit: checker assumes `$positive:Rate` survives `set Rate = 0` when it has syntactic evidence to the contrary. Runtime still prevents data corruption (evaluator rejects div-by-zero), so this is compile-time quality, not safety. Key insight: Principle #8 is two-sided — "never guess" means don't guess proofs hold AND don't guess proofs are broken. Literal assignments are provable; arithmetic outcomes are not. File as three-tier proposal (3a literal, 3b arithmetic inference, 3c full sequential).

- Stateless events evaluation (2026-04-17): `on EventName` bare form (without `from State`) is the right syntax for stateless transition rows. LL(1) resolvable: `ensure` token = event ensure; `when`/`->`/action keyword = stateless row. Philosophy gap: stateless precepts described as having no "transition surface" — stateless events create a mutation surface, not a routing surface. Flag to Shane before implementation. C49 must become conditional (suppress when event has a stateless row). `no transition` outcome is valid but slightly awkward in stateless context — open question for syntax pass. Key file: `docs/PreceptLanguageDesign.md` § Stateless Precepts (Locked). Computed fields and stateless events are complementary, not alternatives. Verdict: recommended-with-caveats.

- Stateless guardrails design (2026-04-17): Designed the full guidance stack for preventing pseudo-lifecycle misuse of stateless `on` events. Three tiers: (1) structural constraints — `transition` is a hard error in stateless rows (no states to route to); bare `on` rows are a hard error when states are declared (C_STATELESS_MIXING); (2) compile-time pseudo-lifecycle heuristic (C_PSEUDO_LIFECYCLE, warning): same field appearing as discriminator in ≥2 stateless event guards with distinct literal values — especially `choice` fields — indicates a lifecycle hiding in a field; severity calibrated as warning (not error) because guarded mutations are valid when they are genuinely data-conditional, not lifecycle-positional; (3) MCP tool output augmentation: `precept_compile` should include a structured `suggestion` block when the heuristic fires; `precept_language` should document the stateless-vs-stateful decision rule; `precept_inspect` should include pattern detection in the response. Key heuristic: all three features together = pseudo-lifecycle: (a) single field as discriminator, (b) finite named vocabulary (choice/string literals), (c) mutual exclusivity across events. Diagnostic message must teach, not just warn — name the field, list the events, explain the intended use. Philosophy framing: stateless events are the mutation surface; states are the routing surface. One-sentence rule: "If knowing WHERE the entity is affects WHAT it can do next, you need states." The precept-authoring SKILL.md needs a Step 3 decision fork: stateless vs. stateful checklist. Mixing prohibition is symmetrical to C55 (root edit not valid when states declared).

- PR #108 code review (issue #106): APPROVED. Unified narrowing architecture is clean — bespoke `$nonneg:` loop replaced by rule iteration through `ApplyNarrowing`, zero dead code. The C76 dotted key fix (`TryGetIdentifierKey` replacing `idArg.Name`) closed a pre-existing gap for event-arg sqrt() proofs. C42 soundness comment in `TryDecomposeNullOrPattern` is the model for dependency documentation. Three non-blocking warnings: regex allocation in code action handler, missing trailing newline, and "must be nonzero" because-message on a `> 0` ensure (should say "must be positive").
- Principle #8 revision analysis (2026-04-17): Shane challenged "assumes satisfiable" as enabling false sense of security. After full analysis: the principle's posture is correct for genuinely undecidable cases, but the wording has a loophole — it doesn't distinguish "can't prove" (genuinely undecidable) from "didn't prove" (syntactically decidable but unchecked). Recommended Option C: revise to mandate "exercise full proving power against declared syntax" while retaining "assumes satisfiable" for undecidable remainder. Opposed "assumes unsatisfiable" (Option A) — false-positive surface is catastrophic. This makes Layer 3a principle-mandated rather than optional. Key insight: both blanket assumptions (satisfiable and unsatisfiable) destroy author trust when applied to the wrong category. The answer is domain separation: decidable → must prove; undecidable → assume satisfiable.

- Review findings resolution (2026-04-17): Addressed all George + Soup Nazi review findings on `docs/ProofEngineDesign.md` and `temp/proof-stack-implementation-plan.md`. Fixes applied: (1) G7 — `$interval:FieldName` → `$ival:FieldName` inconsistency fixed in plan; (2) S4 — "must flip" table resolved: all three ambiguous tests now mandate option (a): add constraints, keep `BeEmpty()` (Addition → `nonnegative`, Multiplication → `positive` on D and C, AbsFunction → `positive` on D); (3) S9 — AbsFunction clarified: `$nonzero:` alone maps to `Unknown` interval, so `abs(Unknown)=[0,∞)` does NOT exclude zero — must use `positive` not `rule D != 0`; (4) Open Design Decision #2 marked RESOLVED. PR #108 body updated to match: Slice 12 → 19 tests (added 3 ExcludesZero negative-range), Slice 13 → 16 new + 4 updated (added PostMutationInterval, 2 modulo, ProvablyZeroDivisors theory, renamed AbsNonzero → AbsPositive), Slice 15 → 6 tests (added RelationalThenBranch), total → 56 new + 5 updated. Key S9 insight worth internalizing: nonzero ≠ positive for interval proof — `$nonzero:` is a gap marker, not a bound marker, so it maps to `Unknown` in the interval domain.

- "Runtime values" framing correction (2026-04-17): Shane challenged the "runtime values" clause in the proposed Principle #8 revision. He's right — "runtime values" imports a general-purpose PL concept that doesn't apply to Precept's closed-world model. In C#/Rust/TS, "runtime values" means genuinely unknowable external data (user input, network, DB). In Precept, every field is typed, constrained, and mutated only through declared channels. Event args are the closest thing to external data, but even those are typed and ensured. The actual boundary is REASONING DEPTH, not DATA AVAILABILITY: (1) arithmetic composition — checker can't compose proofs through operations like `Rate * 2`; (2) cross-constraint interaction — individual proofs don't prove joint satisfiability; (3) inference rules the checker doesn't have. Replaced "runtime values" with "arithmetic composition, cross-constraint interaction, or reasoning beyond the checker's current inference depth" in the proposed text. This correctly frames the boundary as computational, not informational. Precept's closed-world model closes the information gap that general-purpose languages have — the checker's limitation is about how deep it can reason, not what it can see.

- Bounded proof engine evaluation (2026-04-17): Shane proposed "checker defines the proof boundary, authors write within it" — Rust borrow-checker analogy. After full analysis: SUPPORT. Five pillars: (1) business domain arithmetic is empirically simple — 5 division expressions across 25 samples, all trivially handled by sign analysis; (2) sign analysis (positive × positive = positive) covers 100% of corpus with zero false positives, ~300 lines, no deps; (3) author adaptation is minimal and beneficial — add a guard, add a rule, or split expression — makes implicit assumptions explicit; (4) Rust precedent validates the model — strict, sound, bounded checkers succeed when sound, learnable, and growing; (5) "assumes satisfiable" and "assumes unsatisfiable" both destroy trust — bounded proof with explicit obligations is the only model that preserves trust in both directions. Critical constraint: no escape hatch (unlike Rust's `unsafe`), so the boundary must be wide enough that adaptation is always reasonable. Implementation: constant propagation (shipped) + sign analysis (Phase 2) + dataflow/Layer 3b (Phase 3). Phase 2 must ship with Phase 1 — Layer 3a without sign analysis creates a boundary too narrow for real authors. Revised Principle #8 drafted: replaces POSTURE ("assumes satisfiable") with CONTRACT ("defined proof boundary, documented, versioned, expanded over time"). File: `temp/frank-bounded-proof-opinion.md`.

- Comprehensive enforcement design (2026-04-17): Re-evaluated the non-SMT proof engine against Precept's full constraint surface using MCP tools and source code. **Verdict: optimal architecture, no revision needed.** Precept's execution model (no loops, no branches, no reconverging flow) makes single-pass interval arithmetic not just sufficient but the correct choice. Five alternatives rejected (SMT, octagons, constraint propagation, symbolic execution, pattern-based). Designed 6 new enforcement diagnostics (C94-C99) leveraging the interval infrastructure from PR #108: C94 (assignment constraint enforcement — proven violation only, no possible-violation noise), C95 (contradictory rule), C96 (vacuous rule), C97 (dead guard), C98 (vacuous guard), C99 (cross-event invariant, opt-in fixed-point). Key design decisions: (1) proven-violation-only policy — possible violations → no diagnostic, runtime handles them; (2) simple single-field scope for rule/guard analysis — cross-field and complex expressions left to "unproven"; (3) C99 opt-in — only enforcement requiring fixed-point iteration; (4) error/warning severity split by dead-code vs redundancy distinction; (5) no new DSL constructs required. All new enforcements recommended as follow-up issues A-E after PR #108. Deliverable: `docs/ProofEngineDesign.md` +512 lines (optimality assessment, enforcement summary table, detailed C94-C99 specs, soundness guarantees, design decisions, phasing plan). Commit `6fe8ad4`.

- Bounded proof real-world domain reassessment (2026-04-17): Shane challenged the sample-corpus-based analysis as circular reasoning — and he was RIGHT. Resurveyed arithmetic patterns across finance, insurance, healthcare, supply chain, and HR independent of our 25 samples. Key findings: (1) sign analysis alone covers ~75% of real-world division patterns, not 100%; (2) ~10-15% of division patterns involve subtraction-in-denominator (`TotalCost / (UnitsProduced - DefectiveUnits)`, `Amount / (Total - Discount)`, `Cost / (1 - WasteRate)`) which sign analysis CANNOT handle; (3) relational inference (`A > B → A-B > 0`) is the load-bearing technique that closes this gap, raising coverage to ~95-97%; (4) function-specific proofs (max(0,...), pow(positive,n), abs()) add another ~2-3% — critical for insurance `max(0, Claim - Deductible)` pattern; (5) remaining ~3% is genuinely complex computation (amortization formulas, compound financial instruments) that belongs in application code, not the governance DSL. The "no escape hatch" position holds IF relational inference ships — without it, the boundary is too narrow for the "category error" argument. Previous "100% coverage" claim retracted. Restructuring tax: ~60-70% trivial (add guard), ~20-25% moderate (intermediate field), ~5-10% push to app code. Relational inference PROMOTED from "nice to have" to "load-bearing" — must ship before we publicly claim "handles all business domain arithmetic." File: `temp/frank-design-alternatives.md`.

- Divisor safety docs (Slice 9, #106): The `nonnegative ≠ nonzero` distinction is the most important teaching point — it's the one thing that trips authors. Context-aware C93 messaging makes it explicit. Proof sources for C76 and C93 are now unified: constraints, rules, ensures, and guards all feed the same narrowing markers (`$positive:`, `$nonneg:`, `$nonzero:`). Doc updates touched three files (language design, constraint violation, README) — RuntimeApiDesign.md was correctly scoped to public API and didn't need narrowing internals.
- Proof Engine Design Review — comprehensive, philosophy-rooted (2026-04-18): Full review of `docs/ProofEngineDesign.md` + PR #108 implementation per Shane's directive. **Verdict: APPROVED with 2 blockers.** Architecture (ProofContext + LinearForm + RelationalGraph) is the right design — simplified Zone abstract domain hitting exact cost/power sweet spot. Philosophy alignment is direct and complete: prevention, one-file completeness, inspectability, determinism all served. B1: conditional else-branch doesn't receive negated guard narrowing (`WithGuard(condition, false)`) — design specifies it, implementation doesn't do it. One-line fix in `TryInferInterval`, already in frozen blind-spot list. B2: hover attribution mechanism unspecified — Elaine's "from:" UX requirement needs a design decision for how `IntervalOf` carries provenance. Recommended lazy reconstruction (separate `AttributeInterval` query, no API churn). W1-W6: doc header premature "Implemented"; ConstantOffsetScan GTE/c==0 inclusivity (unreachable, Commit 10 fix); WithRule no GCD-normalize (Commit 10); `BuildEventEnsureNarrowings` doesn't propagate typed `_relationalFacts`; no automated perf benchmark; floor/ceil transfer rule precision loss. 10 strengths documented (G1-G10). Key insight: the 3 execution model assumptions (no loops, no branches, no reconverging flow) are what make interval arithmetic OPTIMAL, not just sufficient — widening is the primary precision loss in general analyzers, and Precept avoids it by construction. Implementation plan (15 commits) is well-ordered, dependency chain correct, engine-closure completion bar is the right mechanism. Filed as `.squad/decisions/inbox/frank-proof-engine-review.md`.

- PR #108 code review (issue #106): APPROVED. Unified narrowing architecture is clean — bespoke `$nonneg:` loop replaced by rule iteration through `ApplyNarrowing`, zero dead code. The C76 dotted key fix (`TryGetIdentifierKey` replacing `idArg.Name`) closed a pre-existing gap for event-arg sqrt() proofs. C42 soundness comment in `TryDecomposeNullOrPattern` is the model for dependency documentation. Three non-blocking warnings: regex allocation in code action handler, missing trailing newline, and "must be nonzero" because-message on a `> 0` ensure (should say "must be positive").
- Divisor safety docs (Slice 9, #106): The `nonnegative ≠ nonzero` distinction is the most important teaching point — it's the one thing that trips authors. Context-aware C93 messaging makes it explicit. Proof sources for C76 and C93 are now unified: constraints, rules, ensures, and guards all feed the same narrowing markers (`$positive:`, `$nonneg:`, `$nonzero:`). Doc updates touched three files (language design, constraint violation, README) — RuntimeApiDesign.md was correctly scoped to public API and didn't need narrowing internals.
- Computed fields: the critical semantic contract is one recomputation pass after all mutations and before constraint evaluation.
- Modifier ordering: parser rigidity lived mostly in parser/completion surface; runtime/model layers were already largely order-independent.
- Guarded declarations: scope-inherited guards are the correct model; implementation gaps should be treated separately from design soundness.
- Docs work: `PreceptLanguageDesign.md`, editability/runtime docs, and MCP docs must stay synchronized whenever wording around constraints, updates, or inspection/editability changes.

## Recent Updates

### 2026-04-18 — Rewrote ProofEngineDesign.md for unified architecture

Rewrote `docs/ProofEngineDesign.md` (893 → 1095 lines) to reflect the unified ProofContext + LinearForm + RelationalGraph architecture from `temp/unified-proof-plan.md`.

Key structural decisions:

1. **Architecture framing replaced.** The "five-layer stack" is gone. The doc now describes three composing types (`ProofContext`, `LinearForm`, `RelationalGraph`) and one composing query (`IntervalOf`) as the architecture spine. Layer 1 → `WithAssignment`, Layer 2 → `NumericInterval` (unchanged), Layer 3 → `IntervalOf` dispatch, Layer 4 → LinearForm-keyed `_relationalFacts` + `RelationalGraph`, Layer 5 → `IntervalOf` conditional case.

2. **New sections added:** Research Foundations (zone domain / Cousot & Cousot / CodeContracts Pentagons / Boogie), Rational Type, LinearForm Normalization, Transitive Closure, Proof State Lifecycle and Scope, Coverage Matrix (22 patterns), Unsupported Patterns (19 rows), Risk Register, IntervalOf Query Path.

3. **Design Decisions #1 and #4 marked SUPERSEDED.** #1 (string-encoded interval markers) superseded by typed `ProofContext._fieldIntervals`. #4 (relational markers as a separate layer) superseded by LinearForm-keyed `_relationalFacts`. New decisions noted inline in each.

4. **Phasing updated.** Replaced "PR #108, Slices 11–15" framing with the unified PR six-commit structure. Follow-up A–E sections updated to reference `ProofContext` and `IntervalOf` instead of string markers.

5. **Doc is self-contained.** A reader can understand the full architecture without reading `temp/unified-proof-plan.md`. Implementation artifacts (commit ordering, test counts, file inventory) deliberately omitted — those belong in the PR body. The five gaps are described as problems the architecture solves, with the mechanism for each.

6. **NumericInterval details, IEEE 754 handling, and C94–C99 enforcement specs preserved intact** — these are correct and unchanged; only their framing was updated (e.g., `ctx.IntervalOf(expr)` instead of `TryInferInterval` call sites).

### 2026-04-17 — Applied review feedback to unified proof plan
- Applied all accepted George + Soup Nazi review findings to `temp/unified-proof-plan.md` per Shane's resolved overrides.
- B1: Added `checked` long arithmetic and cross-GCD pre-reduction to Rational spec (§0 constraint #1, §4 Rational.cs entry).
- B2: Added `ProofEngineUnsupportedPatternTests.cs` (4 tests) to §4 test table and §5 commit 3.
- Q2: Added §8 rows 21-22 (negated operand rule, compound-expression rule); updated coverage delta to 12 new-proves. Added §8a rows 18 (scalar-multiple, now "works") and 19 (pow/truncate opacity).
- NB2: Added parens depth-budget note to §4 LinearForm.cs entry.
- NB3 (Shane override): Added scalar-multiple GCD normalization to §2 Gap 1, §4 ProofContext.cs (~350→~360 LOC), §4 CompoundDivisorTests (15→17).
- NB5-NB9: Updated all test counts — LinearFormTests 35→40, RationalTests 20→23, ProofContextTests 30→33, TransitiveClosureTests 14→17, SoundnessInvariantTests 10→15.
- Cascaded all count changes through §5 commit ordering (1427→1437→1493→1511→1528→1555).
- Final totals: 191 new tests, ~1555 total. No architectural spine changes.

### 2026-04-17 — Unified proof plan updated with research findings and Rational decision
- Updated `temp/unified-proof-plan.md` with all post-endorsement research and decisions.
- Added §0 "Research findings" subsection: academic grounding (zone abstract domain / Cousot & Cousot 1977, Miné zone/octagon domains, QF_LRA under-approximation), reference implementations (CodeContracts/Clousot Pentagons domain, Boogie IntervalDomain.cs, Crab/IKOS/Apron), library survey conclusion (no reusable .NET library exists).
- Updated §3 soundness assertion #1: replaced BigInteger-backed Rational spec with `readonly record struct Rational(long Numerator, long Denominator)` implementing `INumber<Rational>`, .NET 10 native, ~100 LOC, `long/long`. Included full decision chain as rationale.
- Added §3 "Grounding audit" subsection: verified all six marker conventions, narrowing method shapes, kill loop, and scope model against codebase.
- Updated §4 manifest entries for Rational.cs (~150→~100 LOC) and RationalTests.cs (expanded test spec).
- Updated §7 Division in LinearForm exclusion to specify `long/long` Rational coefficients.
- Added §9 "Post-endorsement update" note: architectural spine unchanged, endorsements remain valid.

- PreceptTypeChecker.cs decomposition analysis (2026-04-18): Analyzed the full 3197-line file (53 methods) for partial-class decomposition. Key findings: (1) Shane's proposed "Diagnostics" group does not exist as a natural seam — diagnostic emissions are inline, not standalone methods. Replaced with TypeInference (~750 lines) and FieldConstraints (~330 lines). (2) IntervalInference (~157 lines) too small for its own file — merged into ProofChecks (~380 lines combined). (3) Zero visibility changes needed — C# partial classes compile as one class, so `private` members are accessible across all partial files. This eliminates the biggest risk. (4) Commit ordering is bottom-up by dependency: Helpers → FieldConstraints → Narrowing → ProofChecks → TypeInference. (5) Future-proofing validated: #111's C94–C99 land cleanly in ProofChecks, #107/#95 type additions land in TypeInference, #112 stateless events land in main orchestration file. Filed as issue #118, milestone M4. Decision in `.squad/decisions/inbox/frank-typechecker-decomposition.md`.
- No architectural decisions changed. All edits are refinements and external validation.

### 2026-04-18 — PR #108 body updated: replaced stale Slices 11–16 with unified proof plan
- Replaced Slices 11–16 (old per-slice narrowing approach) in PR #108 body with the 6-commit unified ProofContext + LinearForm architecture from `temp/unified-proof-plan.md`.
- Preserved Slices 1–10 content exactly (all checked — shipped). New Commits 1–6 all unchecked.
- Updated Summary: reflects Slices 1–10 as shipped plus the unified proof engine delivering ProofContext + LinearForm + Rational + RelationalGraph, closing all 5 proof gaps.
- Updated Why: added the linear-form decision rationale and explicit SMT/Z3 rejection.
- Updated Implementation Plan: 6 commits with method-level specificity, exact file paths, per-commit test counts (1469 baseline → 1532 → 1542 → 1598 → 1616 → 1633 → 1660), regression anchors, dependency graph, and tests-that-MUST-FLIP table.
- Added Coverage Matrix (22 patterns, 12 new provers, 0 regressions), Unsupported Patterns (§8a) summary, and Risk Register (§6) table per task requirements.
- Updated File Inventory (18 new files), Tooling/MCP sync assessment, and Validation (1469 current → ~1660 target, 191 new tests).
- Key decision: used 1660 as the actual target (1469 + 191) rather than the plan document's 1555 (which used a stale 1364 baseline). The additional 105 tests came from post-Slices-1-10 commits (e.g., span precision work).

### 2026-04-18 — Issue #118 revised: type checker decomposition grounded in ProofEngineDesign.md
- Shane flagged that issue #118's decomposition was built from the current file state, not the post-PR-#108 target state described in `docs/ProofEngineDesign.md`. Revised the entire issue body.
- Key corrections: (1) Added architectural boundary section clarifying what lives on `ProofContext.cs` (IntervalOf, KnowsNonzero, WithAssignment, WithRule, WithGuard, Child, Dump, LookupRelationalInterval, GcdNormalize) vs `PreceptTypeChecker.cs` — the design doc's Integration Points table is the authoritative source. (2) ProofChecks group grew from ~380 to ~500-550 lines to account for the shared assessment model (Commit 11) and C94-C98 enforcement methods (Commit 12) that will exist in the file before this refactoring runs. (3) Updated line estimates from ~3200 to ~3500-3600 for the post-PR-#108 file. (4) Fixed future-proofing: #111 is now scoped to the `nonzero` modifier, not C94-C99 (those land in PR #108). (5) Added C95/C96 integration note for ValidateRules in the main file. (6) Updated Narrowing group for typed RelationalFact rekeying (Commit 9).
- Lesson: decomposition issues for files under active development MUST target the to-be state from the canonical design doc, not the as-is state that will change before the refactoring runs.
### 2026-04-15 — Issue #100 precept name token scope fix (PR #101)
- Fixed `machineDeclaration` capture 4 scope from `entity.name.precept.message.precept` → `entity.name.type.precept.precept` in the TextMate grammar.
- Removed the old scope from the `preceptMessage` semanticTokenScopes mapping in package.json.
- Added explicit Structure Indigo (`#6366F1`) textMateRule for `entity.name.type.precept.precept`.
- No runtime, language server, MCP, or doc changes required — purely a tooling scope correction.
- All 1,476 tests pass. PR #101 marked ready for review.



### 2026-04-13 — Issue #88 docs sync completed for PR #90
- Reconciled the editability/documentation story across `docs/PreceptLanguageDesign.md`, `docs/EditableFieldsDesign.md`, `docs/RuntimeApiDesign.md`, and `docs/McpServerDesign.md`.
- Validation recorded on branch `squad/88-docs-reconcile-editability`: `git diff --check` and `dotnet test --no-restore`.
- Commit `9ea5609` pushed; PR #90 checklist updated and left clean for review.

### 2026-04-12 — Squad `@copilot` lane retirement contract review
- Confirmed the change is narrowly scoped to retiring the Squad-owned `squad:copilot` routing lane while preserving general repo-wide Copilot tooling.
- Required all live workflows, mirrored templates, and squad docs to agree so the lane is retired cleanly.

### 2026-04-18 — Wave 2 code review: PR #108 strangler-fig migration (Steps 7b-i through 8)
- **APPROVED** — 6 commits, 8 files, +507/−386 lines. Strangler-fig migration from string-encoded proof markers to typed stores, followed by scope split.
- Marker elimination total: zero `$ival:`, `$positive:`, `$nonneg:`, `$nonzero:`, `$gt:`, `$gte:` in `src/Precept/Dsl/`. `ToMarkerKey`, `TryParseMarkerKey`, `LookupLegacyRelationalInterval` all deleted.
- All Wave 1 carry-forward findings verified: G1 (GCD normalization) resolved — all storage uses `GcdNormalize`. G2 (flag implications) resolved — all 8 write sites correct. G4 (reference aliasing) partially resolved — `WithRule` still shares `_fieldIntervals`/`_flags`/`_exprFacts` by reference (safe today, fragile by design).
- `BuildEventEnsureNarrowings` rewrite eliminates string-surgery bug class by construction — `LinearForm.Rekey()` replaces `:` split/reassemble.
- Scope split simpler than spec (single class with `Child()`/`ChildMerging()` instead of two-class hierarchy) — correct and sufficient.
- 4 non-blocking warnings: `WithRule` reference sharing (W1), stale `ExtractIntervalFromMarkers` name (W2), 5 stale `ProofContext` comment references (W3), `ValidateStateActions` manual construction instead of `ChildMerging` (W4).
- Key insight: the strangler-fig sequence (dual-write → switch reads → remove old → scope split) is the model for any future migration of shared mutable state in the type checker.

### 2026-04-12 — Issue #9 design review resolutions incorporated
- Folded the resolved design decisions back into the issue body, including null-handling, split diagnostics (C72/C73), and inspect trace expectations.
- Design left implementation-ready after proposal/body synchronization.

### 2026-04-18 — Issue #118 future-proofing: temporal + quantity fit analysis
- Mapped detailed designs for #107 (temporal, 8 types) and #95 (currency/quantity, 7 types) against the proposed 6-file partial class decomposition in issue #118.
- **TypeInference.cs scaling problem:** Combined growth from both proposals balloons the file from ~800 to ~1680-2135 lines (operator tables, typed constants, dot-accessor resolution). Planned split point: extract `TryInferTemporalBinaryKind` and `TryInferDomainBinaryKind` dispatch into a 7th partial `PreceptTypeChecker.DomainTypeInference.cs` when the first proposal lands — not pre-emptively.
- **All other 5 files confirmed clean fit:** FieldConstraints (+90-160 lines from `in`/`of` validation, temporal constraint rejection), ProofChecks (+20-40, neither proposal adds proof engine integration — currency doc explicitly says "proof engine does not need modification"), Narrowing (+70-120, discrete equality narrowing via `TryApplyEqualityNarrowing` fits existing pattern), Helpers (+100-160, mechanical `StaticValueKind`/`MapScalarType` additions), Main (stable).
- Key insight: discrete equality narrowing (`$eq:` markers for unit/currency/dimension compatibility) is a parallel layer to existing numeric/null narrowing — same pipeline, different proof surface.
- Updated issue #118 body with expanded per-file impact table, scaling analysis, planned split documentation, and revised routing table.

## 2026-04-19 — Research: Type Checker Architecture (meta-evaluation)
- Researched 6 production typecheckers (Roslyn, TypeScript, Rust, Swift, Kotlin K2, F#) to evaluate Precept's 6-partial-class split.
- Verdict: KEEP AS-IS. Our split has good external precedent. Strongest match is Kotlin K2's phase model.
- Output: research/architecture/typechecker-architecture-survey-frank.md
