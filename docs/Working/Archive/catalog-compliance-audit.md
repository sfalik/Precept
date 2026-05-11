# Catalog Compliance Audit — Pipeline Stages
**Date:** 2026-05-09
**Auditor:** Frank
**Scope:** All compiler pipeline stages in `src/Precept/`

## Executive Summary
27 catalog-compliance violations found across the audited pipeline files.

- **By stage:** Parser 13, Name Binder 1, Type Checker 6, Proof Engine 5, Graph Analyzer 2, Lexer 0, StateGraph/Compilation 0
- **By pattern:** A×4, B×1, C×3, D×8, E×4, F×4, H×3
- **Key conclusion:** the largest blast radius is still parser/type-checker/proof-engine logic that answers language questions with local token checks, enum checks, string literals, and operator tables instead of catalog metadata.

## Architectural Identity Reminder
Precept is metadata-driven. Catalogs in `src/Precept/Language/` are the machine-readable language specification. Pipeline stages must be generic consumers of that metadata. When a stage hardcodes token groupings, grammar suffixes, modifier meaning, proof semantics, wildcard/broadcast handling, or qualifier rules, the stage stops being generic machinery and becomes a second language specification.

## Stage-by-Stage Findings

### Stage 1: Lexer
✅ Catalog-compliant — no violations found in `Lexer.cs`. Keyword recognition, operator tables, and punctuation tables are catalog-derived from `Tokens`.

### Stage 2: Parser

## Violation: State wildcard token hardcoded
**Stage:** Parser
**File:** `src/Precept/Pipeline/Parser.cs`
**Method/Location:** `ParseStateTarget` (lines 827-839)
**Pattern:** E
**What it hardcodes:** Treats `TokenKind.Any` as the only wildcard state target beside an identifier and collapses wildcard and named states into the same slot payload.
**What catalog field is missing or unused:** Missing `TokenMeta.IsStateWildcard` / target-kind metadata on `ConstructSlotKind.StateTarget`; the parser does not preserve wildcard semantics as metadata.
**Linked bug(s):** BUG-001
**Severity:** High

## Violation: Field broadcast grammar hardcoded
**Stage:** Parser
**File:** `src/Precept/Pipeline/Parser.cs`
**Method/Location:** `ParseFieldTarget` (lines 875-887)
**Pattern:** E
**What it hardcodes:** Accepts only a single identifier or literal `all`; there is no catalog-driven support for broadcast vs. list targets.
**What catalog field is missing or unused:** Missing slot metadata for `FieldTarget` cardinality/broadcast (`allowAll`, `allowList`, separator token).
**Linked bug(s):** BUG-005, BUG-026, BUG-037
**Severity:** High

## Violation: Event-argument TypeRef parsing bypasses type-shape metadata
**Stage:** Parser
**File:** `src/Precept/Pipeline/Parser.cs`
**Method/Location:** `ParseArgumentList` (lines 723-781)
**Pattern:** D
**What it hardcodes:** Event args parse only a simple type keyword plus qualifiers; they never reuse full `TypeRef` parsing for choice types, collection inner types, keyed collections, or future type forms.
**What catalog field is missing or unused:** `TypeMeta.Category`, collection/choice shape metadata, and the existing `ParseTypeReference` path are bypassed; missing slot metadata saying argument types use the full type grammar.
**Linked bug(s):** BUG-027
**Severity:** High

## Violation: Event-argument modifiers bypass value-modifier metadata
**Stage:** Parser
**File:** `src/Precept/Pipeline/Parser.cs`
**Method/Location:** `ParseArgumentList` trailing modifier loop (lines 750-756)
**Pattern:** C
**What it hardcodes:** Consumes every value modifier as a bare keyword; valued modifiers such as `default`, `min`, `max`, `minlength`, and `maxcount` never parse their value expressions on event args.
**What catalog field is missing or unused:** The event-arg grammar bypasses `ValueModifierMeta` shape metadata — it ignores both value-carrying metadata (`HasValue`) and declaration-site applicability when parsing arg modifiers.
**Linked bug(s):** BUG-004
**Severity:** High

## Violation: Choice domain literal grammar hardcoded
**Stage:** Parser
**File:** `src/Precept/Pipeline/Parser.cs`
**Method/Location:** `ParseChoiceType` (lines 438-496)
**Pattern:** D
**What it hardcodes:** Manually whitelists `StringLiteral | NumberLiteral | True | False | Identifier` plus leading sign handling for choice members.
**What catalog field is missing or unused:** `TypeMeta.ChoiceLiteralTokens` is present on types but unused here; missing metadata for signed choice-domain forms.
**Linked bug(s):** BUG-010
**Severity:** Medium

## Violation: Collection type suffix grammar hardcoded
**Stage:** Parser
**File:** `src/Precept/Pipeline/Parser.cs`
**Method/Location:** `ParseCollectionType` / `ParseInnerTypeReference` (lines 498-574)
**Pattern:** D
**What it hardcodes:** Switches on `TypeKind.Lookup`, `TypeKind.LogBy`, `TypeKind.QueueBy`, and literal `by` / `to` tokens to decide key/value grammar and by-variant promotion.
**What catalog field is missing or unused:** Missing collection-syntax metadata on `TypeMeta` (`requiresElementType`, key separator, value separator, ordering-direction suffixes).
**Linked bug(s):** BUG-045
**Severity:** High

## Violation: Valued-modifier expression starts hardcoded
**Stage:** Parser
**File:** `src/Precept/Pipeline/Parser.cs`
**Method/Location:** `ParseModifierList` (lines 623-666)
**Pattern:** A
**What it hardcodes:** Uses a local token allowlist for modifier value expressions, excluding other catalog-valid expression starts such as typed constants and keyword-named function calls.
**What catalog field is missing or unused:** `ExpressionStartTokens` already exists but is unused in this method.
**Linked bug(s):** BUG-006, BUG-019, BUG-051
**Severity:** High

## Violation: Message-position grammar hardcoded to plain string literal
**Stage:** Parser
**File:** `src/Precept/Pipeline/Parser.cs`, `src/Precept/Pipeline/Parser.Expressions.cs`
**Method/Location:** `ParseBecauseClause` (lines 798-823), `ParseOutcomeStringLiteralArg` (lines 631-646)
**Pattern:** A
**What it hardcodes:** `because` and `reject` only accept `TokenKind.StringLiteral`; interpolated-string forms are rejected before semantic analysis.
**What catalog field is missing or unused:** `TokenMeta.IsMessagePosition` is unused; missing slot metadata for allowed message expression forms.
**Linked bug(s):** BUG-031
**Severity:** High

## Violation: Function-call starts hardcoded to identifiers
**Stage:** Parser
**File:** `src/Precept/Pipeline/Parser.Expressions.cs`
**Method/Location:** `ParseNud` / `ParseIdentifierOrFunctionCall` (lines 37-95, 187-203)
**Pattern:** A
**What it hardcodes:** Only `TokenKind.Identifier` can start a function call. Keyword-named functions like `min` and `max` can never enter function-call parsing.
**What catalog field is missing or unused:** Missing function-call lead-token metadata derived from `Functions` / dual-use token metadata; current expression-form metadata is too weak.
**Linked bug(s):** BUG-006, BUG-051
**Severity:** High

## Violation: Member-access vocabulary duplicated in token flags
**Stage:** Parser
**File:** `src/Precept/Pipeline/Parser.Expressions.cs`
**Method/Location:** `ParseMemberAccessOrMethodCall` / `IsMemberNameToken` (lines 288-324)
**Pattern:** A
**What it hardcodes:** Member names must be `Identifier` or a token pre-labeled `IsValidAsMemberName`; the parser does not derive allowed accessor names from the type catalog.
**What catalog field is missing or unused:** `TypeMeta.Accessors` is unused at parse time; token member-name flags are a parallel vocabulary copy.
**Linked bug(s):** BUG-025, BUG-039
**Severity:** High

## Violation: Postfix presence operator sequence hardcoded
**Stage:** Parser
**File:** `src/Precept/Pipeline/Parser.Expressions.cs`
**Method/Location:** `GetLedBindingPower` and `ParsePostfixIs` (lines 142-179, 327-356)
**Pattern:** B
**What it hardcodes:** `is set` and `is not set` recognition, validation, and binding-power handling are special-cased with literal token comparisons.
**What catalog field is missing or unused:** `Operators.ByTokenSequence` is only partially used; missing operator metadata for led token sequences and right-binding power.
**Linked bug(s):** Not yet reported
**Severity:** Medium

## Violation: Action suffix markers hardcoded in parser helpers
**Stage:** Parser
**File:** `src/Precept/Pipeline/Parser.cs`
**Method/Location:** `ParseActionByShape` and shape-specific helpers (lines 932-1100)
**Pattern:** C
**What it hardcodes:** `=`, `into`, `by`, and `at` are embedded in parser helpers and boundary predicates instead of being described by action metadata.
**What catalog field is missing or unused:** `ActionMeta.SyntaxShape` is too shallow; missing metadata for operand roles, separator tokens, optional suffix clauses, and terminators.
**Linked bug(s):** BUG-021, BUG-048, BUG-049
**Severity:** Critical

## Violation: Guarded state-scoped construct forms not cataloged into parser routing
**Stage:** Parser
**File:** `src/Precept/Pipeline/Parser.cs`, `src/Precept/Pipeline/Parser.Expressions.cs`
**Method/Location:** `ParseScopedConstruct` (lines 230-260), `ParseEnsureClause` (lines 532-562), state-action routing
**Pattern:** C
**What it hardcodes:** Scoped constructs are routed as target + disambiguation token + remaining slots. There is no catalog-driven place for optional `when` guards on `StateEnsure`, `EventEnsure`, or `StateAction` forms, so `when` is rejected as unexpected routing noise.
**What catalog field is missing or unused:** Missing guard-placement metadata on `ConstructMeta` / slot metadata for guarded variants.
**Linked bug(s):** BUG-020, BUG-044, BUG-054
**Severity:** High

### Stage 3: Name Binder

## Violation: Wildcard and broadcast targets rebound as declared names
**Stage:** Name Binder
**File:** `src/Precept/Pipeline/NameBinder.cs`
**Method/Location:** `ResolveReferences`, `ResolveStateReference`, `ResolveFieldReference` (lines 195-237, 521-566)
**Pattern:** E
**What it hardcodes:** Every parsed `StateTarget` and `FieldTarget` is rebound as an ordinary declaration lookup; wildcard/broadcast forms are not first-class semantic targets.
**What catalog field is missing or unused:** Missing target-kind metadata on parsed slots or token metadata (`IsStateWildcard`, `IsFieldBroadcast`) for binder dispatch.
**Linked bug(s):** BUG-001, BUG-026, BUG-037
**Severity:** High

### Stage 4: Type Checker

## Violation: Qualifier domain knowledge embedded in pipeline-local unit tables
**Stage:** Type Checker
**File:** `src/Precept/Pipeline/TypeChecker.cs`
**Method/Location:** `CountQualifierUnitCodes`, `NonCountDimensionlessUnitCodes`, `MapUnitQualifier`, `DeriveUnitDimensionName` (lines 24-49, 163-196)
**Pattern:** D
**What it hardcodes:** Count-like UCUM units and non-count dimensionless units are maintained as local string sets inside the checker.
**What catalog field is missing or unused:** Missing unit/dimension metadata in language catalogs (`IsCountLike`, `IsPureDimensionless`, dimension classification).
**Linked bug(s):** Not yet reported
**Severity:** Critical

## Violation: Qualifier resolution hardcoded by axis switch
**Stage:** Type Checker
**File:** `src/Precept/Pipeline/TypeChecker.cs`
**Method/Location:** `ExtractQualifiers` and `Map*Qualifier` helpers (lines 122-217)
**Pattern:** D
**What it hardcodes:** Each qualifier axis is validated and projected in a handwritten switch with stage-owned behavior.
**What catalog field is missing or unused:** `QualifierShape` only describes syntax; missing validator/projector metadata per qualifier axis.
**Linked bug(s):** Not yet reported
**Severity:** High

## Violation: Modifier semantics keyed off enum identity
**Stage:** Type Checker
**File:** `src/Precept/Pipeline/TypeChecker.cs`
**Method/Location:** `PopulateFields`, `PopulateStates`, `PopulateEvents`, `ResolveFieldExpressions`, `PopulateAccessModes` (lines 224-301, 309-349, 356-389, 401-444, 721-725)
**Pattern:** H
**What it hardcodes:** `Optional`, `Writable`, `Default`, `InitialState`, and fallback `Read` semantics are applied through direct `ModifierKind` checks.
**What catalog field is missing or unused:** Missing semantic-role fields on `ModifierMeta` (`AffectsPresence`, `AffectsWritability`, `CarriesDefaultValue`, `MarksInitial`, default access mode behavior).
**Linked bug(s):** BUG-004, BUG-029, BUG-038
**Severity:** High

## Violation: Literal typing hardcoded by token kind and text shape
**Stage:** Type Checker
**File:** `src/Precept/Pipeline/TypeChecker.Expressions.cs`
**Method/Location:** `ResolveLiteral` / `ResolveNumericLiteral` (lines 119-157)
**Pattern:** D
**What it hardcodes:** Literal result types are inferred from token kind and decimal-point text checks; choice/contextual literal typing does not read per-type literal metadata.
**What catalog field is missing or unused:** `TypeMeta.ChoiceLiteralTokens` and richer literal-resolution metadata are unused.
**Linked bug(s):** BUG-010, BUG-019
**Severity:** High

## Violation: CI enforcement tied to field refs instead of qualifier metadata
**Stage:** Type Checker
**File:** `src/Precept/Pipeline/TypeChecker.Validation.cs`
**Method/Location:** `EnforceCIInExpression`, `IsCIExpression`, `GetCIFieldName` (lines 328-401)
**Pattern:** D
**What it hardcodes:** Case-insensitive enforcement only recognizes `TypedFieldRef { IsCaseInsensitive: true }`; quantifier bindings and other CI-typed values are invisible to the rule.
**What catalog field is missing or unused:** Missing typed-expression / qualifier metadata that projects CI-ness from type metadata instead of field identity.
**Linked bug(s):** BUG-046
**Severity:** High

## Violation: Modifier-bound relationships missing from generic validation
**Stage:** Type Checker
**File:** `src/Precept/Pipeline/TypeChecker.Validation.cs`
**Method/Location:** `ValidateFieldModifiers` (lines 41-127)
**Pattern:** H
**What it hardcodes:** Validation knows duplicates/conflicts/subsumption only; it has no catalog-driven concept of bound pairs like `min/max`, `minlength/maxlength`, or `mincount/maxcount`, so contradictory bounds are never generically checked.
**What catalog field is missing or unused:** Missing `ModifierMeta.BoundCounterpart` / `BoundRole` metadata.
**Linked bug(s):** BUG-029, BUG-038
**Severity:** High

### Stage 5: Proof Engine

## Violation: Action proof obligations lose the action target subject
**Stage:** Proof Engine
**File:** `src/Precept/Pipeline/ProofEngine.cs`
**Method/Location:** `WalkActions` (lines 218-236)
**Pattern:** F
**What it hardcodes:** Action proof obligations are attached to `InputExpression` or a synthetic literal site, so `SelfSubject(count)` obligations on action metadata cannot resolve back to the mutated field.
**What catalog field is missing or unused:** Missing action-site projection metadata / proof subject resolver for action targets.
**Linked bug(s):** BUG-008, BUG-050
**Severity:** Critical

## Violation: Proof subsumption tables hardcoded in the engine
**Stage:** Proof Engine
**File:** `src/Precept/Pipeline/ProofEngine.cs`
**Method/Location:** `TryLiteralProof`, `SatisfactionCovers`, `GuardSubsumes`, `InvertOp`, `NegateOp` (lines 334-360, 439-500, 622-669)
**Pattern:** F
**What it hardcodes:** Comparison implication, inversion, negation, and proof-satisfaction coverage are encoded as handwritten operator tables.
**What catalog field is missing or unused:** Missing `ProofRequirementMeta` / `OperatorMeta` subsumption and negation metadata (`InverseComparison`, `NegatedComparison`, `SatisfactionCovers`).
**Linked bug(s):** BUG-013
**Severity:** Critical

## Violation: Guard proof extraction hardcodes logical and accessor semantics
**Stage:** Proof Engine
**File:** `src/Precept/Pipeline/ProofEngine.cs`
**Method/Location:** `ExtractGuardConstraintsCore`, `ExtractFieldToFieldCore` (lines 548-620, 723-775)
**Pattern:** F
**What it hardcodes:** Guard reasoning only understands `and` / `or` / `not`, field-literal comparisons, and accessor name `count`.
**What catalog field is missing or unused:** Missing proof-aware metadata on operators and accessors (`LogicalRole`, `CountAccessor`, guard-decomposition rules).
**Linked bug(s):** BUG-008, BUG-050
**Severity:** High

## Violation: Proof diagnostic mapping hardcoded to two numeric cases
**Stage:** Proof Engine
**File:** `src/Precept/Pipeline/ProofEngine.cs`
**Method/Location:** `CreateDiagnostic`, `GetNumericRequirementDiagnosticCode`, `CreateFaultSiteLink` (lines 840-929)
**Pattern:** F
**What it hardcodes:** Numeric proof failures are mapped to `SqrtOfNegative` only for `>= 0`, otherwise `DivisionByZero`, regardless of the originating catalog obligation family.
**What catalog field is missing or unused:** `ProofRequirements` metadata is unused at diagnostic emission time; missing `ProofRequirementMeta.FailureDiagnostic` / `FaultCode`.
**Linked bug(s):** BUG-050
**Severity:** Critical

## Violation: Initial-state satisfiability hardcodes type defaults and operator evaluation
**Stage:** Proof Engine
**File:** `src/Precept/Pipeline/ProofEngine.cs`
**Method/Location:** `GetTypeDefault`, `FoldValue`, `EvaluateBinaryOp` (lines 1027-1176)
**Pattern:** D
**What it hardcodes:** Abstract default values for each `TypeKind` and constant-folding behavior for boolean, numeric, and string operators are encoded in the proof engine.
**What catalog field is missing or unused:** Missing abstract-value/default metadata on `TypeMeta` and reusable constant-evaluation metadata/runtime hooks for operations.
**Linked bug(s):** Not yet reported
**Severity:** High

### Stage 6: Graph Analyzer

## Violation: Initial-state and initial-event semantics hardcoded outside modifier metadata
**Stage:** Graph Analyzer
**File:** `src/Precept/Pipeline/GraphAnalyzer.cs`
**Method/Location:** `Analyze`, `GetStateFlags` (lines 69-149, 587-613)
**Pattern:** H
**What it hardcodes:** Initial states are detected via `ModifierKind.InitialState`; initial events are inferred from edges out of the initial state instead of event modifier metadata.
**What catalog field is missing or unused:** Missing `StateModifierMeta.IsInitialEntryPoint`; `EventModifierMeta.RequiredAnalysis` exists conceptually but is unused for graph projection.
**Linked bug(s):** Not yet reported
**Severity:** High

## Violation: Wildcard transition broadcast semantics hardcoded in graph expansion
**Stage:** Graph Analyzer
**File:** `src/Precept/Pipeline/GraphAnalyzer.cs`
**Method/Location:** `BuildEdges`, `TryAddEdge` (lines 234-302)
**Pattern:** E
**What it hardcodes:** `row.FromState == null` means broadcast-to-all-states with explicit-row override semantics; wildcard behavior is carried by a null sentinel, not catalog metadata.
**What catalog field is missing or unused:** Missing target-kind metadata / transition-row broadcast policy metadata.
**Linked bug(s):** BUG-001
**Severity:** High

### Stage 7: Runtime / Evaluator
No runtime/evaluator implementation files were in the requested audit scope. `StateGraph.cs` and `Compilation.cs` are data carriers only and were found catalog-compliant.

## Missing Catalog Fields — Master List

| Catalog | Field | Purpose | Which stages need it |
|---|---|---|---|
| `Tokens` | `TokenMeta.IsStateWildcard` | Distinguish `any` state targets from named states. | Parser, Name Binder, Graph Analyzer |
| `Tokens` / parsed-slot metadata | `IsFieldBroadcast` / `FieldTargetKind` | Preserve `all` as broadcast rather than an identifier-shaped payload. | Parser, Name Binder |
| `Constructs` / slot metadata | `FieldTargetCardinality` + separator metadata | Support single, list, and broadcast field targets generically. | Parser |
| `Constructs` | `GuardPlacement` / `SupportsWhenGuard` | Describe whether `when` appears before or after verbs/clauses for scoped constructs. | Parser, Type Checker |
| `Actions` | `SyntaxParts` / separator-token metadata | Describe `=`, `into`, `by`, `at`, optional captures, and action terminators in metadata. | Parser |
| `Functions` | `LeadTokenKinds` / dual-use token mapping | Allow keyword-named functions to parse without identifier-only assumptions. | Parser |
| `Types` | `CollectionSyntax` | Describe element/key/value suffix grammar, by-variants, and ordering-direction suffixes. | Parser |
| `Types` | `UnitSemantics` (`IsCountLike`, `IsPureDimensionless`) | Remove unit classification tables from the type checker. | Type Checker |
| qualifier metadata | `Validator` / `Projector` per qualifier axis | Let the checker validate and normalize qualifiers without an axis switch. | Type Checker |
| `Modifiers` | semantic-role flags (`AffectsPresence`, `AffectsWritability`, `CarriesDefaultValue`, `MarksInitial`) | Remove enum-identity checks from symbol population and access normalization. | Type Checker, Graph Analyzer |
| `Modifiers` | `BoundCounterpart` / `BoundRole` | Enable generic contradictory-bound validation. | Type Checker |
| proof/action metadata | `ActionProofSiteProjection` | Resolve `SelfSubject` obligations for actions against the mutated field/site. | Proof Engine |
| `ProofRequirements` / `Operators` | subsumption + negation metadata | Remove handwritten implication/inversion tables from discharge logic. | Proof Engine |
| `ProofRequirements` | `FailureDiagnostic` / `FaultCode` | Map proof failures to the correct diagnostics without hardcoded heuristics. | Proof Engine |
| `Types` / operation metadata | `AbstractDefaultValue` + constant-folding hooks | Remove hardcoded default-value and fold semantics from initial-state satisfiability. | Proof Engine |
| `Modifiers` / event analysis metadata | explicit initial-event/initial-state graph flags | Make graph entry semantics catalog-driven. | Graph Analyzer |

## Remediation Priority
1. **Action proof-site metadata + proof diagnostic metadata** — fixes the highest-severity false proof failures and removes the worst proof-engine hardcoding (`BUG-008`, `BUG-050`).
2. **Parser action/type/member/function grammar metadata** — biggest blast radius across authoring surface (`BUG-004`, `BUG-005`, `BUG-006`, `BUG-019`, `BUG-020`, `BUG-021`, `BUG-025`, `BUG-027`, `BUG-031`, `BUG-039`, `BUG-044`, `BUG-048`, `BUG-049`, `BUG-051`, `BUG-054`).
3. **Wildcard/broadcast target metadata** — one cross-stage fix for parser, binder, and graph semantics (`BUG-001`, `BUG-026`, `BUG-037`).
4. **Modifier semantic-role + bound-pair metadata** — removes enum identity checks and closes contradictory-bound gaps (`BUG-029`, `BUG-038`).
5. **Qualifier/unit metadata** — pulls non-language lookup tables out of the type checker and makes qualifier validation generic.
6. **Graph entry semantics metadata** — closes the remaining graph-stage modifier hardcoding.

## Linked Bug Map

| Bug | Violation name | Stage | Root cause |
|---|---|---|---|
| BUG-001 | State wildcard token hardcoded | Parser / Name Binder / Graph Analyzer | Wildcard state targets are carried as raw names/null sentinels instead of metadata. |
| BUG-004 | Event-argument modifiers bypass value-modifier metadata | Parser | Event-arg grammar bypasses `ValueModifierMeta` metadata, including value shape and declaration-site applicability. |
| BUG-005 | Field broadcast grammar hardcoded | Parser | `FieldTarget` is parsed as one name instead of list/broadcast metadata. |
| BUG-006 | Function-call starts hardcoded to identifiers | Parser | Keyword-named functions are not cataloged as callable lead tokens. |
| BUG-008 | Action proof obligations lose the action target subject | Proof Engine | `SelfSubject` action proofs are attached to ad hoc expression sites. |
| BUG-010 | Choice domain literal grammar / literal typing hardcoded | Parser / Type Checker | Choice literal compatibility is not derived from type metadata. |
| BUG-019 | Valued-modifier expression starts hardcoded; literal typing hardcoded | Parser / Type Checker | Typed constants are excluded from parser value-start logic and contextual literal typing remains shallow. |
| BUG-020 | Guarded state-scoped construct forms not cataloged into parser routing | Parser | `when` placement for `ensure` forms is not described in construct metadata. |
| BUG-021 | Action suffix markers hardcoded in parser helpers | Parser | `by`/`at`/`into`/`=` live in parser code instead of action metadata. |
| BUG-025 | Member-access vocabulary duplicated in token flags | Parser | Accessor legality is copied into token flags instead of derived from type accessors. |
| BUG-026 | Wildcard and broadcast targets rebound as declared names | Name Binder | `all` is rebound as a field name. |
| BUG-027 | Event-argument TypeRef parsing bypasses type-shape metadata | Parser | Arg declarations do not reuse full `TypeRef` parsing. |
| BUG-029 | Modifier-bound relationships missing from generic validation | Type Checker | No metadata links lower/upper-bound modifiers. |
| BUG-031 | Message-position grammar hardcoded to plain string literal | Parser | Message slots do not advertise allowed expression forms. |
| BUG-037 | Field broadcast grammar hardcoded | Parser / Name Binder | `all` broadcast remains identifier-shaped through binding. |
| BUG-038 | Modifier-bound relationships missing from generic validation | Type Checker | No metadata links `minlength/maxlength` and `mincount/maxcount`. |
| BUG-039 | Member-access vocabulary duplicated in token flags | Parser | Keyword-collision accessors are blocked by token classification. |
| BUG-044 | Guarded state-scoped construct forms not cataloged into parser routing | Parser | State-action guard placement is missing from construct metadata. |
| BUG-045 | Collection type suffix grammar hardcoded | Parser | Ordering-direction/type-suffix grammar is encoded locally. |
| BUG-046 | CI enforcement tied to field refs instead of qualifier metadata | Type Checker | CI-ness is a field flag, not propagated type metadata. |
| BUG-048 | Action suffix markers hardcoded in parser helpers | Parser | Priority-key suffix parsing depends on local `by` handling. |
| BUG-049 | Action suffix markers hardcoded in parser helpers | Parser | Index-suffix parsing depends on local `at` handling and ambiguous boundaries. |
| BUG-050 | Action proof obligations lose subject; proof diagnostic mapping hardcoded | Proof Engine | Collection mutation proofs are projected onto the wrong site and then mapped to the wrong diagnostic family. |
| BUG-051 | Function-call starts hardcoded to identifiers | Parser | Dual-use keyword functions are not represented as callable token starts. |
| BUG-054 | Guarded state-scoped construct forms not cataloged into parser routing | Parser | Event-hook/state-hook guard support is absent from construct metadata. |
