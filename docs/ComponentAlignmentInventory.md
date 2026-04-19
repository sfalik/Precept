# Component Alignment Inventory

**Date:** 2026-04-19
**Author:** Frank (Lead/Architect)
**Purpose:** Structured inventory of every component that defines or consumes language knowledge, every alignment relationship, every enforcement mechanism, and every gap — input for the unified drift prevention design doc that supersedes `docs/CatalogInfrastructureDesign.md`.

---

## Part 1: Component Registry

### Core Definitions

#### 1.1 PreceptToken Enum + Attributes

**File:** `src/Precept/Dsl/PreceptToken.cs` (lines 1–540, includes `PreceptTokenMeta` at line 510+)

**What it defines:**
- Every token kind in the DSL (68 members: 14 Declaration, 6 Control, 2 Grammar, 8 Action, 3 Outcome, 10 Constraint, 9 Type, 3 Literal, 14 Operator, 6 Punctuation, 2 Structure, 3 Value)
- `TokenCategory` enum (12 categories: Control, Declaration, Action, Outcome, Grammar, Constraint, Type, Literal, Operator, Punctuation, Structure, Value)
- Three attribute types: `[TokenCategory]`, `[TokenDescription]`, `[TokenSymbol]`
- `PreceptTokenMeta` reflection helpers: `GetCategory()`, `GetCategories()`, `GetDescription()`, `GetSymbol()`, `GetByCategory()`, `BuildKeywordDictionary()`

**What it consumes:** Nothing — pure source of truth.

**Alignment risk:** Every consumer of vocabulary knowledge depends on this file. A missing attribute silently breaks downstream derivation.

---

#### 1.2 PreceptExpression AST

**File:** `src/Precept/Dsl/PreceptModel.cs` (lines 130–190)

**What it defines:**
- `PreceptExpression` abstract record base class
- 7 concrete subtypes: `PreceptLiteralExpression`, `PreceptIdentifierExpression`, `PreceptUnaryExpression`, `PreceptBinaryExpression`, `PreceptParenthesizedExpression`, `PreceptFunctionCallExpression`, `PreceptConditionalExpression`
- `SourceSpan` for position tracking

**What it consumes:** Nothing — pure type definitions.

**Alignment risk:** Every `switch` over `PreceptExpression` subtypes (in type checker, evaluator, and analysis code) must handle all 7 subtypes. Adding an 8th subtype without updating all switch sites produces runtime `default` fallthrough. Currently convention-only; planned for Roslyn PREC002 enforcement.

---

#### 1.3 StaticValueKind Enum

**File:** `src/Precept/Dsl/PreceptTypeChecker.cs` (lines 8–18)

**What it defines:**
- `[Flags]` enum with 8 members: None, String, Number, Boolean, Null, Integer, Decimal, OrderedChoice, UnorderedChoice
- The type system's internal representation of value kinds for static analysis

**What it consumes:** Nothing — pure type definition.

**Alignment risk:** The type checker, evaluator, function registry, operator dispatch, and MCP LanguageTool all switch on or combine `StaticValueKind` flags. Missing a flag in any consumer creates a silent type-system gap. Planned for Roslyn PREC008 enforcement.

---

#### 1.4 FunctionRegistry

**File:** `src/Precept/Dsl/FunctionRegistry.cs`

**What it defines:**
- `FunctionDefinition` records (name, description, overloads)
- `FunctionOverload` records (parameters, return type, min arity)
- `FunctionParameter` records (name, accepted types, constraint)
- `FunctionArgConstraint` enum (None, MustBeIntegerLiteral, RequiresNonNegativeProof)
- 18 built-in functions: abs, floor, ceil, round, truncate, min, max, clamp, pow, sqrt, toLower, toUpper, trim, startsWith, endsWith, left, right, mid

**What it consumes:** `StaticValueKind` for parameter/return types.

**How it consumes:** Direct reference to `StaticValueKind` flags.

**Alignment risk:**
- The **type checker** reads the registry for overload resolution and argument validation.
- The **evaluator** has a **parallel hand-coded switch** that independently implements every function body — the archetype drift problem. Adding a function to the registry without updating the evaluator creates a type-checks-but-fails-at-runtime gap.
- The **TextMate grammar** has a hardcoded function name alternation pattern (`abs|floor|ceil|...`) — must be updated manually.
- The **MCP LanguageTool** reflects the registry via `FunctionRegistry.AllFunctions` — automatic.
- The **LS completions** have hardcoded function names in completion lists.
- The **LS hover** has hardcoded function documentation in `PreceptDocumentIntellisense.FunctionHoverContent`.

---

#### 1.5 DiagnosticCatalog

**File:** `src/Precept/Dsl/DiagnosticCatalog.cs`

**What it defines:**
- `LanguageConstraint` records (Id, Phase, Rule, MessageTemplate, Severity)
- `ConstraintSeverity` enum (Error, Warning, Hint)
- `ConstraintViolationException` for parse-time enforcement
- 79 constraints: C1–C25 (parse), C26–C32 (compile structural), C33–C37 (runtime), C38–C98 (compile type/proof)
- `ToDiagnosticCode()` → PRECEPT{NNN} mapping

**What it consumes:** Nothing — pure source of truth.

**Alignment risk:**
- Every enforcement point in parser, type checker, and runtime must reference a catalog entry via `// SYNC:CONSTRAINT:Cnn` comments.
- MCP LanguageTool serializes constraints automatically via `DiagnosticCatalog.Constraints`.
- Language server diagnostics derive codes via `ToDiagnosticCode()`.
- Missing SYNC comment = invisible to drift tests. Orphaned SYNC comment = references a non-existent constraint.

---

#### 1.6 ConstructCatalog

**File:** `src/Precept/Dsl/ConstructCatalog.cs`

**What it defines:**
- `ConstructInfo` records (Name, Form, Context, Description, Example)
- Registration via parser combinator extension method `.Register()`

**What it consumes:** Populated by `PreceptParser` static initializers (parser combinators register constructs at class-load time).

**How it consumes:** Extension method on `TokenListParser<PreceptToken, T>`.

**Alignment risk:** Construct examples must parse successfully (enforced by drift test). Adding a parser combinator without calling `.Register()` creates an undocumented construct.

---

#### 1.7 PreceptModel Types

**File:** `src/Precept/Dsl/PreceptModel.cs`

**What it defines:**
- `PreceptDefinition`, `PreceptState`, `PreceptEvent`, `PreceptEventArg`, `PreceptField`, `PreceptCollectionField`
- `PreceptScalarType` enum (String, Number, Boolean, Null, Integer, Decimal, Choice)
- `PreceptCollectionKind` enum (Set, Queue, Stack)
- `FieldConstraint` hierarchy (9 subtypes: Nonnegative, Positive, Min, Max, Notempty, Minlength, Maxlength, Mincount, Maxcount, Maxplaces)
- `PreceptCollectionMutationVerb` enum
- Outcome types: `StateTransition`, `Rejection`, `NoTransition`
- `EnsureAnchor` enum (In, To, From)

**What it consumes:** `PreceptExpression` types for expression fields.

**Alignment risk:** MCP CompileTool DTOs mirror model types — if model types gain/lose properties, MCP DTOs may drift. Language server hover/completions extract data from model types — new model properties need LS rendering.

---

#### 1.8 ConstraintViolation Types

**File:** `src/Precept/Dsl/ConstraintViolation.cs`

**What it defines:**
- `ConstraintTarget` hierarchy (Field, EventArg, Event, State, Definition)
- `ConstraintSource` hierarchy (Rule, StateEnsure, EventEnsure, TransitionRejection)

**What it consumes:** `EnsureAnchor` from PreceptModel.

**Alignment risk:** MCP ViolationDto mirrors this hierarchy — drift if new target/source kinds are added.

---

### Pipeline Stages

#### 1.9 PreceptTokenizer

**File:** `src/Precept/Dsl/PreceptTokenizer.cs`

**What it defines:** The Superpower tokenizer builder that converts source text into `TokenList<PreceptToken>`.

**What it consumes:**
- `PreceptTokenMeta.BuildKeywordDictionary()` — **attribute-driven derivation**, zero drift for keywords
- Hardcoded operator/punctuation patterns (multi-char `==`, `!=`, `>=`, `<=`, `->` before single-char `>`, `<`, `=`, etc.)
- Hardcoded string literal regex and number literal regex

**How it consumes:** Reflection-derived keyword dictionary at startup; hardcoded `Span.EqualTo()` / `Character.EqualTo()` for operators/punctuation.

**Alignment risk:** Operator/punctuation tokens are hardcoded — adding a new operator token to `PreceptToken` without adding a `builder.Match()` line creates a token that exists in the enum but is never produced. Keywords are zero-drift by construction.

---

#### 1.10 PreceptParser

**File:** `src/Precept/Dsl/PreceptParser.cs` (large — combinators + assembly)

**What it defines:** Grammar rules, AST production, `ConstructCatalog` registration.

**What it consumes:**
- `PreceptToken` enum members directly (combinator patterns like `Token.EqualTo(PreceptToken.Field)`)
- `DiagnosticCatalog` constraints for error messages (`C1`–`C25`, `C54`, `C70`, `C80`–`C82`)
- `ConstructCatalog.Register()` for construct documentation

**How it consumes:** Direct reference to `PreceptToken` members and `DiagnosticCatalog` static fields.

**Alignment risk:** Adding a new token kind without parser support means the token is recognized but never consumed. Adding a new grammar form without `ConstructCatalog.Register()` means the construct exists but is undocumented.

---

#### 1.11 PreceptTypeChecker (+ partials)

**Files:**
- `src/Precept/Dsl/PreceptTypeChecker.cs` — main: scope building, expression type inference dispatch
- `src/Precept/Dsl/PreceptTypeChecker.Helpers.cs` — copy helpers, utility methods
- `src/Precept/Dsl/PreceptTypeChecker.FieldConstraints.cs` — field constraint validation
- `src/Precept/Dsl/PreceptTypeChecker.Narrowing.cs` — null narrowing, proof narrowing
- `src/Precept/Dsl/PreceptTypeChecker.ProofChecks.cs` — proof engine: interval analysis, contradiction/tautology detection
- `src/Precept/Dsl/PreceptTypeChecker.TypeInference.cs` — (likely in main, expression type inference)

**What it defines:**
- `StaticValueKind` enum (see 1.3)
- `PreceptValidationDiagnostic` records
- `PreceptTypeContext` / `PreceptTypeScopeInfo` / `PreceptTypeExpressionInfo`
- `GlobalProofContext` for proof engine state

**What it consumes:**
- `FunctionRegistry` for function overload resolution and argument constraint checking
- `DiagnosticCatalog` constraints (C38–C98) for type error messages
- `PreceptExpression` subtypes in switch dispatch
- `StaticValueKind` flags throughout
- `FunctionArgConstraint` enum for proof obligations

**How it consumes:** Direct reference to all. Switch over `PreceptExpression` subtypes, switch over `StaticValueKind` combinations, direct `FunctionRegistry.TryGetFunction()` calls.

**Alignment risk:** New expression subtypes require new switch arms. New `StaticValueKind` flags require new inference paths. New functions in `FunctionRegistry` are automatically picked up for type checking but may need specific argument-constraint handling. New diagnostic codes need `// SYNC:CONSTRAINT:Cnn` comments.

---

#### 1.12 PreceptExpressionEvaluator

**File:** `src/Precept/Dsl/PreceptExpressionEvaluator.cs`

**What it defines:**
- `EvaluationResult` record (Success, Value, Error)
- Runtime expression evaluation logic

**What it consumes:**
- `PreceptExpression` subtypes in switch dispatch
- Hardcoded function names in `EvaluateFunction` switch
- Hardcoded operator dispatch in `EvaluateBinary` / `EvaluateUnary`
- Hardcoded member accessor names (`count`, `min`, `max`, `peek`, `length`)
- `CollectionValue` for collection operations
- `PreceptCollectionKind` for collection-kind-specific accessor validation

**How it consumes:** Direct pattern matching (`is long`, `is decimal`, `is string`); string-based function name switch; hardcoded operator dispatch.

**Alignment risk:** **This is the highest-drift component.** Every language addition must be mirrored here independently. There is no structural link between the evaluator's function switch and `FunctionRegistry`, between the evaluator's operator dispatch and the type checker's `TryInferBinaryKind`, or between the evaluator's accessor names and any registry. The planned DD24 (registry-driven functions) and DD25 (operator registry) address this.

---

#### 1.13 PreceptRuntime (Engine + Compiler)

**File:** `src/Precept/Dsl/PreceptRuntime.cs`

**What it defines:**
- `PreceptEngine` — runtime event processing, fire pipeline, validation, Update API
- `PreceptCompiler` — `Validate()`, `Compile()`, `CompileFromText()` pipeline
- `PreceptInstance` — runtime state holder
- Fire pipeline stages (event ensures → row selection → exit actions → row mutations → entry actions → derived recomputation → validation)

**What it consumes:**
- `DiagnosticCatalog` constraints (C26–C37) for compile/runtime errors
- `PreceptExpressionRuntimeEvaluator.Evaluate()` for guard/rule/ensure evaluation
- `PreceptTypeChecker.Check()` for compile-time validation
- `PreceptAnalysis.Analyze()` for reachability/dead-state analysis
- `PreceptModel` types throughout

**How it consumes:** Direct calls to evaluator, type checker, analysis. Direct reference to `DiagnosticCatalog` constraint fields.

**Alignment risk:** Fire pipeline stage changes require MCP LanguageTool `FirePipeline` array update. New violation categories require MCP ViolationDto/mapper updates.

---

### Tooling Consumers

#### 1.14 Language Server — PreceptAnalyzer (Completions)

**File:** `tools/Precept.LanguageServer/PreceptAnalyzer.cs`

**What it consumes:**
- `PreceptParser.ParseWithDiagnostics()` for diagnostics
- `PreceptCompiler.Validate()` for semantic diagnostics
- `DiagnosticCatalog.ToDiagnosticCode()` for diagnostic codes
- Hardcoded keyword completion items (must match `PreceptToken` vocabulary)
- Hardcoded regex patterns for context detection (field declarations, collection fields, event args, transition rows)
- `PreceptDocumentIntellisense.Analyze()` for document-level analysis

**How it consumes:** Mix of direct API calls and hardcoded string/regex patterns.

**Alignment risk:** Keyword completions are **hardcoded lists** — adding a new keyword to `PreceptToken` does NOT automatically add it to completions. Context-detection regexes must be updated when grammar forms change. Type keyword lists (`string|number|boolean|integer|decimal|choice`) are hardcoded in regexes.

---

#### 1.15 Language Server — PreceptDocumentIntellisense (Hover + Completions Support)

**File:** `tools/Precept.LanguageServer/PreceptDocumentIntellisense.cs`

**What it consumes:**
- `PreceptParser.ParseWithDiagnostics()` for model extraction
- `PreceptTypeChecker.Check()` for proof context
- Hardcoded regex patterns for fallback document analysis (when parser fails)
- Hardcoded `FunctionHoverContent` dictionary — **18 function entries maintained independently of `FunctionRegistry`**
- Type names hardcoded in regexes (`string|number|boolean|integer|decimal|choice`)
- Collection kinds hardcoded in regexes (`set|queue|stack`)

**How it consumes:** Direct API calls for model/type data; hardcoded strings and regexes for fallback paths and hover content.

**Alignment risk:** `FunctionHoverContent` is a **manually maintained parallel copy** of function documentation. Adding a function to `FunctionRegistry` without updating this dictionary means no hover for the new function. Type name and collection kind regexes are hardcoded copies.

---

#### 1.16 Language Server — PreceptSemanticTokensHandler

**File:** `tools/Precept.LanguageServer/PreceptSemanticTokensHandler.cs`

**What it consumes:**
- `PreceptTokenMeta.GetCategory()` via `BuildSemanticTypeMap()` — **attribute-driven derivation**, zero drift for token → semantic type mapping
- `PreceptTokenizerBuilder.Instance` for token stream
- Hardcoded context token sets (`StateContextTokens`, `EventContextTokens`, `FieldContextTokens`) — must match `PreceptToken` members that introduce state/event/field names
- `PreceptParser.ParseWithDiagnostics()` for constraint-set extraction

**How it consumes:** Reflection-driven `SemanticTypeMap` built at startup from `[TokenCategory]` attributes; hardcoded `HashSet<PreceptToken>` for identifier classification context.

**Alignment risk:** `SemanticTypeMap` is zero-drift by construction. Context token sets (`StateContextTokens` etc.) are hardcoded — adding a new token that introduces a state/event/field name requires manual update.

---

#### 1.17 Language Server — PreceptHoverHandler

**File:** `tools/Precept.LanguageServer/PreceptHoverHandler.cs`

**What it consumes:** `PreceptDocumentIntellisense.Analyze()` and `PreceptDocumentIntellisense.CreateHover()`.

**How it consumes:** Thin delegation — all intelligence is in `PreceptDocumentIntellisense`.

**Alignment risk:** Same as 1.15 — inherited from `PreceptDocumentIntellisense`.

---

#### 1.18 TextMate Grammar

**File:** `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`

**What it consumes (via hardcoded regex patterns):**
- Declaration keywords: `precept|state|event|field|edit|from|on|in|to|as|nullable|default|because|initial|with|any|all|of|into`
- Control keywords: `when|if|then|else`
- Grammar keywords: `rule|ensure`
- Action keywords: `set|add|remove|enqueue|dequeue|push|pop|clear`
- Outcome keywords: `transition|reject` + `no transition` compound pattern
- Type keywords: `string|number|boolean|integer|decimal|choice|set|queue|stack`
- Constraint keywords: `nonnegative|positive|notempty|min|max|minlength|maxlength|mincount|maxcount|maxplaces|ordered`
- Operators: `and|or|not|contains`, `==|!=|>=|<=`, `[+\-*/%]`, `>|<`, `=`
- Function names: `abs|floor|ceil|round|truncate|min|max|pow|sqrt|clamp|toLower|toUpper|startsWith|endsWith|trim|left|right|mid`
- Collection member accessors: `count|min|max|peek`
- String member accessors: `length`
- Literals: `true|false|null`

**How it consumes:** Entirely hardcoded regex alternations in JSON. No programmatic derivation. No build-time validation.

**Alignment risk:** **Every** keyword, operator, function, type, constraint, and accessor must be manually duplicated into regex patterns. This is the highest-maintenance consumer and the most likely to drift. No enforcement mechanism exists today except the copilot-instructions sync checklist and manual review.

---

#### 1.19 MCP LanguageTool

**File:** `tools/Precept.Mcp/Tools/LanguageTool.cs`

**What it consumes:**
- `PreceptTokenMeta` (all three attribute readers) — **attribute-driven derivation** for vocabulary
- `ConstructCatalog.Constructs` — **catalog-driven** for constructs
- `DiagnosticCatalog.Constraints` — **catalog-driven** for constraints
- `FunctionRegistry.AllFunctions` — **registry-driven** for function catalog
- Hardcoded `GetOperatorInfo()` switch — operator precedence/arity (must match evaluator dispatch)
- Hardcoded `ExpressionScopes` array — expression scope descriptions
- Hardcoded `FirePipeline` array — pipeline stage descriptions (must match runtime behavior)
- Hardcoded `OutcomeKinds` array — outcome kind descriptions
- Hardcoded `GetAssessmentModel()`, `GetRemediation()`, `GetProofDependency()` — proof enrichment for specific constraint IDs

**How it consumes:** Mix of attribute/catalog reflection (automatic) and hardcoded data (manual).

**Alignment risk:** Vocabulary, constructs, constraints, and functions are zero-drift by construction. `FirePipeline` stages must be updated when the runtime fire pipeline changes. `ExpressionScopes` must be updated when new expression contexts are added. `OutcomeKinds` must be updated when new outcome types are added. `GetOperatorInfo()` must match the evaluator's precedence table. Proof enrichment helpers must be updated when new proof-dependent constraints are added.

---

#### 1.20 MCP CompileTool

**File:** `tools/Precept.Mcp/Tools/CompileTool.cs`

**What it consumes:**
- `PreceptCompiler.CompileFromText()` — direct API call
- `PreceptModel` types → DTO projection (fields, states, events, transitions, rules, ensures, edit blocks)
- `FieldConstraint` subtypes in `FormatConstraint()` switch
- `PreceptClauseOutcome` subtypes in `MapBranch()` switch
- `PreceptCollectionMutationVerb` in verb formatting

**How it consumes:** Direct API calls with DTO projection.

**Alignment risk:** New model properties require new DTO properties. New `FieldConstraint` subtypes require new `FormatConstraint()` arms. New outcome types require new `MapBranch()` arms.

---

#### 1.21 MCP Fire/Inspect/Update Tools

**Files:** `tools/Precept.Mcp/Tools/FireTool.cs`, `InspectTool.cs`, `UpdateTool.cs`

**What they consume:**
- `PreceptCompiler.CompileFromText()` for compilation
- `PreceptEngine` API (`Fire`, `Inspect`, `Update`, `CreateInstance`)
- `JsonConvert.ToNativeDict()` for JSON → native type coercion
- `ViolationDtoMapper.Map()` for violation serialization

**How they consume:** Thin wrappers over core APIs.

**Alignment risk:** New API surface on `PreceptEngine` may need MCP exposure. New violation types need `ViolationDtoMapper` updates. `JsonConvert` type coercion must handle all `PreceptScalarType` values.

---

#### 1.22 MCP Shared DTOs

**Files:** `tools/Precept.Mcp/Tools/ViolationDto.cs`, `JsonConvert.cs`

**What they define:**
- `ViolationDto` record, `ViolationDtoMapper.Map()` — mirrors `ConstraintViolation` hierarchy
- `JsonConvert.ToNativeDict()` — JSON element → CLR type coercion (handles long/decimal/double/string/bool)

**Alignment risk:** `ViolationDtoMapper` switches on `ConstraintTarget` and `ConstraintSource` subtypes — new subtypes require new arms. `JsonConvert` must handle every `PreceptScalarType` lane.

---

## Part 2: Alignment Edge Map

| # | Source | Consumer | What Must Agree | Current Enforcement | Planned Enforcement | Risk if Drifted |
|---|--------|----------|----------------|--------------------|--------------------|----------------|
| E1 | `PreceptToken` `[TokenSymbol]` | Tokenizer keyword dict | Keyword text → token kind mapping | **Attribute-driven derivation** (zero drift) | — | N/A — eliminated by construction |
| E2 | `PreceptToken` `[TokenCategory]` | Semantic tokens `SemanticTypeMap` | Token → semantic type mapping | **Attribute-driven derivation** (zero drift) | — | N/A — eliminated by construction |
| E3 | `PreceptToken` `[TokenCategory]` | MCP LanguageTool vocabulary grouping | Keywords grouped by category | **Attribute-driven derivation** (zero drift) | — | N/A — eliminated by construction |
| E4 | `PreceptToken` `[TokenDescription]` | MCP LanguageTool vocabulary descriptions | Description text per keyword | **Attribute-driven derivation** (zero drift) | — | N/A — eliminated by construction |
| E5 | `PreceptToken` members | Token attribute completeness | Every member has `[TokenCategory]` + `[TokenDescription]` | Reflection test `AllTokens_HaveCategoryAndDescription` | Roslyn PREC003 (replaces test) | Missing attributes → broken derivation chain |
| E6 | `PreceptToken` keyword members | `[TokenSymbol]` presence | All keyword/operator tokens have `[TokenSymbol]` | Reflection test `KeywordAndOperatorTokens_HaveSymbol` | Roslyn PREC004 (replaces test) | Missing symbol → keyword not recognized by tokenizer |
| E7 | `PreceptToken` operators/punct | Tokenizer `builder.Match()` | Operator/punct tokens have tokenizer patterns | **None** — manual convention | — | Token exists in enum but never produced |
| E8 | `PreceptToken` vocabulary | TextMate grammar keyword alternations | All keywords appear in grammar regex patterns | **None** — copilot-instructions checklist | — | Missing keyword → no syntax highlighting |
| E9 | `PreceptToken` types | TextMate grammar `typeKeywords` | All type keywords in grammar | **None** — copilot-instructions checklist | — | New type → no highlighting |
| E10 | `PreceptToken` constraints | TextMate grammar `constraintKeywords` | All constraint keywords in grammar | **None** — copilot-instructions checklist | — | New constraint → no highlighting |
| E11 | `PreceptToken` operators | TextMate grammar `operators` | All operators in grammar | **None** — copilot-instructions checklist | — | New operator → no highlighting |
| E12 | `PreceptToken` vocabulary | LS completions keyword lists | All keywords offered as completions | **None** — manual convention | — | Missing keyword → no completion offered |
| E13 | `FunctionRegistry` function names | TextMate grammar `functionCall` pattern | All function names in grammar alternation | **None** — copilot-instructions checklist | Planned drift test `FunctionRegistry_GrammarSync` | New function → no syntax highlighting |
| E14 | `FunctionRegistry` function names | LS completions function list | All functions offered in expression context | **None** — manual convention | — | Missing function → no completion |
| E15 | `FunctionRegistry` function docs | LS hover `FunctionHoverContent` | Hover text matches registry descriptions | **None** — manual maintenance | — | Missing/stale hover for functions |
| E16 | `FunctionRegistry` signatures | Evaluator `EvaluateFunction` switch | Every registered function has eval implementation | **None** — manual convention | DD24: registry-driven evaluation | Type-checks but fails at runtime |
| E17 | `FunctionRegistry` signatures | Type checker overload resolution | Type checker reads registry directly | **Structural** (direct reference) | — | N/A — same data structure |
| E18 | Type checker operator rules | Evaluator operator dispatch | Legal operator × type combinations agree | **None** — independent implementations | DD25: operator registry | Type checker allows what evaluator rejects (or vice versa) |
| E19 | `DiagnosticCatalog` constraints | Parser `// SYNC:CONSTRAINT:Cnn` | Every parse constraint has SYNC comment | Reflection test `SyncComments_MatchDiagnosticCatalog` | Roslyn PREC010 (replaces test) | Orphaned/missing SYNC comments |
| E20 | `DiagnosticCatalog` constraints | Type checker `// SYNC:CONSTRAINT:Cnn` | Every compile constraint has SYNC comment | Reflection test `SyncComments_MatchDiagnosticCatalog` | Roslyn PREC010 (replaces test) | Orphaned/missing SYNC comments |
| E21 | `DiagnosticCatalog` constraints | Runtime `// SYNC:CONSTRAINT:Cnn` | Every runtime constraint has SYNC comment | Reflection test `SyncComments_MatchDiagnosticCatalog` | Roslyn PREC010 (replaces test) | Orphaned/missing SYNC comments |
| E22 | `DiagnosticCatalog` constraints | MCP LanguageTool constraints | Constraints serialized for AI consumers | **Catalog-driven** (zero drift) | — | N/A — eliminated by construction |
| E23 | `DiagnosticCatalog` constraint IDs | LS diagnostic codes | `C{nn}` → `PRECEPT{NNN}` mapping | **Structural** (`ToDiagnosticCode()` call) | — | N/A — single function |
| E24 | `ConstructCatalog` constructs | MCP LanguageTool constructs | Constructs serialized for AI consumers | **Catalog-driven** (zero drift) | — | N/A — eliminated by construction |
| E25 | `ConstructCatalog` examples | Parser correctness | Every example parses successfully | Drift test `AllConstructExamples_ParseSuccessfully` | — | Stale example → misleading docs |
| E26 | `ConstructCatalog` constructs | Sample files | At least one sample uses each construct | Drift test `SampleFiles_CoverAllConstructs` | — | Uncovered construct → no real-world validation |
| E27 | `PreceptExpression` subtypes | Type checker switch | All 7 subtypes handled | **None** — `default` arm fallthrough | Roslyn PREC002 | New subtype → silent type-check skip |
| E28 | `PreceptExpression` subtypes | Evaluator switch | All 7 subtypes handled | **None** — `default` arm returns Fail("unsupported") | Roslyn PREC002 | New subtype → runtime "unsupported" error |
| E29 | `StaticValueKind` flags | Type checker inference | All flag values handled in switch sites | **None** — convention | Roslyn PREC008 | New type kind → not type-checked |
| E30 | `StaticValueKind` flags | Evaluator dispatch | All type families handled in operator/function dispatch | **None** — convention | Roslyn PREC008 + DD24/DD25 | New type kind → runtime failure |
| E31 | `FunctionArgConstraint` members | Type checker constraint handling | All constraint kinds enforced | **None** — convention | Roslyn PREC007 | New constraint kind → not enforced |
| E32 | `PreceptModel` types | MCP CompileTool DTOs | DTO properties mirror model properties | **None** — manual maintenance | — | New model property → missing from MCP output |
| E33 | `FieldConstraint` subtypes | MCP CompileTool `FormatConstraint()` | All constraint types serialized | **None** — switch with potential gap | — | New constraint type → not shown in MCP |
| E34 | `PreceptClauseOutcome` subtypes | MCP CompileTool `MapBranch()` | All outcome types serialized | **None** — switch with potential gap | — | New outcome type → not shown in MCP |
| E35 | `ConstraintViolation` types | MCP `ViolationDtoMapper` | All violation types mapped | **None** — switch with potential gap | — | New violation type → not mapped in MCP |
| E36 | `PreceptScalarType` values | MCP `JsonConvert.ToNativeDict()` | All scalar types coerced correctly from JSON | **None** — manual maintenance | — | New scalar type → JSON coercion failure |
| E37 | Runtime fire pipeline stages | MCP LanguageTool `FirePipeline` | Stage descriptions match runtime behavior | **None** — hardcoded array | — | Pipeline change → stale MCP docs |
| E38 | Runtime outcome kinds | MCP LanguageTool `OutcomeKinds` | Outcome descriptions match runtime | **None** — hardcoded array | — | New outcome → missing from MCP |
| E39 | Evaluator `Fail()` sites | `EvalFailCode` classification | Every Fail call has enum identity | **None** — bare string calls today | DD23 + Roslyn PREC001 | Unclassified failure → invisible to conformance tests |
| E40 | `EvalFailCode` `[StaticallyPreventable]` | `DiagnosticCatalog` entries | Every statically preventable failure has a compiler rule | **None** — planned | DD23 + Roslyn PREC009 | Evaluator catches what compiler should prevent |
| E41 | Model record types | `SourceLine` property | All declaration records carry source position | **None** — convention | Roslyn PREC011 | Missing source line → no diagnostic squiggle position |
| E42 | `PreceptToken` `[TokenSymbol]` | No duplicate symbols | Each symbol text maps to exactly one token | **None** — manual review | Roslyn PREC005 | Ambiguous tokenization |
| E43 | Collection member accessors | TextMate grammar `collectionMemberAccess` | `count|min|max|peek` in grammar | **None** — manual maintenance | — | New accessor → no highlighting |
| E44 | String member accessors | TextMate grammar `stringMemberAccess` | `length` in grammar | **None** — manual maintenance | — | New accessor → no highlighting |
| E45 | Semantic tokens context sets | `PreceptToken` members | `StateContextTokens`, `EventContextTokens`, `FieldContextTokens` match token semantics | **None** — hardcoded `HashSet<PreceptToken>` | — | New context-setting token → wrong identifier coloring |
| E46 | LS completions type names | `PreceptScalarType` + collection kinds | Type names in regex patterns match type system | **None** — hardcoded regex patterns | — | New type → not offered in completions |
| E47 | Proof enrichment helpers | `DiagnosticCatalog` constraint IDs | `GetAssessmentModel()`, `GetRemediation()`, `GetProofDependency()` cover proof constraints | **None** — hardcoded switch | — | New proof constraint → missing enrichment |
| E48 | Expression scope descriptions | Type checker scope rules | `ExpressionScopes` array matches actual scoping | **None** — hardcoded array | — | Scope change → stale MCP docs |
| E49 | `DiagnosticCatalog` constraints | Constraint trigger tests | Every constraint can be triggered | Reflection test `EveryConstraint_CanBeTriggered` | — | Untriggerable constraint → dead code or wrong ID |
| E50 | Sample `.precept` files | Parser + compiler correctness | All samples parse and compile | Drift test `SampleFile_ParsesWithoutErrors` / `CompilesWithoutErrors` | — | Broken sample → misleading reference |

---

## Part 3: Enforcement Mechanism Inventory

### 3.1 Attribute-Driven Derivation

**Type:** Structural — eliminated by construction
**Feedback timing:** Runtime initialization (startup)
**What it covers:** E1 (tokenizer keywords), E2 (semantic token types), E3 (MCP vocabulary grouping), E4 (MCP vocabulary descriptions)
**What it misses:** Cannot enforce that the attribute values are correct — only that they exist and are consumed.
**Status:** Existing, operational

### 3.2 Catalog-Driven Serialization

**Type:** Structural — eliminated by construction
**Feedback timing:** Runtime API call
**What it covers:** E22 (MCP constraints), E24 (MCP constructs), E17 (type checker reads FunctionRegistry)
**What it misses:** Cannot enforce that the catalog entries are complete or that the serialized format matches consumer expectations.
**Status:** Existing, operational

### 3.3 Reflection Drift Tests (CatalogDriftTests)

**Type:** Reflection test
**Feedback timing:** Test-time (CI)
**What it covers:**
- E5 (`AllTokens_HaveCategoryAndDescription`)
- E6 (`KeywordAndOperatorTokens_HaveSymbol`)
- E19/E20/E21 (`SyncComments_MatchDiagnosticCatalog`)
- E25 (`AllConstructExamples_ParseSuccessfully`)
- E26 (`SampleFiles_CoverAllConstructs`)
- E49 (`EveryConstraint_CanBeTriggered`)
- Dual-role token test (`CollectionTypeTokens_HaveTypeCategoryAmongCategories`)
- Diagnostic code format test (`DiagnosticCodes_FollowPreceptNNNFormat`)

**What it misses:** Test-time only — no IDE feedback. Cannot detect gaps in consumers (TextMate grammar, completions, hover). Does not verify that enforcement code actually references the correct constraint.
**Status:** Existing, 4 tests planned for retirement (replaced by Roslyn PREC003, PREC004, PREC010, PREC011)

### 3.4 Diagnostic Sample Drift Tests (DiagnosticSampleDriftTests)

**Type:** Integration test
**Feedback timing:** Test-time (CI)
**What it covers:** E50 (sample correctness), plus strict `# EXPECT:` contract validation for diagnostic samples
**What it misses:** Only covers samples that exist — cannot detect missing samples for new constructs.
**Status:** Existing, operational

### 3.5 Copilot Instructions Sync Checklists

**Type:** Convention / review-time
**Feedback timing:** Review-time (Copilot reads `.github/copilot-instructions.md`)
**What it covers:** E8–E11 (TextMate grammar), E12 (completions), E13 (function grammar), E14 (function completions), E15 (function hover). The "Non-Negotiable" sections list explicit checklists.
**What it misses:** Copilot may not follow checklists consistently. No CI enforcement. No IDE feedback. Human reviewer must verify compliance.
**Status:** Existing, operational but unreliable

### 3.6 Language Surface Sync Instructions

**Type:** Convention / review-time
**Feedback timing:** Review-time (Copilot reads `.github/instructions/language-surface-sync.instructions.md`)
**What it covers:** Three-category impact analysis (Runtime, Tooling, MCP) for every language surface change.
**What it misses:** Same as 3.5 — convention-only, no CI enforcement.
**Status:** Existing, operational but unreliable

### 3.7 SYNC Comments

**Type:** Source-code marker
**Feedback timing:** Review-time (visible in diffs) + test-time (validated by `SyncComments_MatchDiagnosticCatalog`)
**What it covers:** E19/E20/E21 — bidirectional link between enforcement code and DiagnosticCatalog.
**What it misses:** Does not verify that the enforcement code at the SYNC comment actually implements the constraint correctly. Does not cover non-constraint alignment edges.
**Status:** Existing, planned for Roslyn PREC010 upgrade (edit-time enforcement)

### 3.8 Roslyn Analyzer (Planned — DD26, Issue #115)

**Type:** Roslyn analyzer — build-time / edit-time
**Feedback timing:** Edit-time (IDE squiggles) + build-time (`dotnet build`)
**What it covers:**
- E5 → PREC003 (token category + description)
- E6 → PREC004 (token symbol)
- E19/E20/E21 → PREC010 (SYNC comment validation)
- E27/E28 → PREC002 (exhaustive PreceptExpression switch)
- E29/E30 → PREC008 (exhaustive StaticValueKind switch)
- E31 → PREC007 (exhaustive FunctionArgConstraint switch)
- E39 → PREC001 (EvalFailCode on Fail calls)
- E40 → PREC009 (StaticallyPreventable ↔ DiagnosticCatalog)
- E41 → PREC011 (SourceLine on model records)
- E42 → PREC005 (no duplicate TokenSymbol)
- PREC006 (exhaustive EvalFailCode switch)

**What it misses:** Cannot reach cross-boundary edges (C# ↔ JSON grammar). Cannot validate TextMate grammar regex patterns. Cannot validate MCP DTO completeness. Cannot validate hardcoded string arrays (FirePipeline, ExpressionScopes, OutcomeKinds).
**Status:** Planned for issue #115

### 3.9 Registry-Driven Function Evaluation (Planned — DD24, Issue #115)

**Type:** Structural — eliminated by construction
**Feedback timing:** Runtime initialization
**What it covers:** E16 (FunctionRegistry ↔ evaluator function dispatch)
**What it misses:** Cannot verify evaluation correctness — only that a delegate exists.
**Status:** Planned for issue #115

### 3.10 Operator Registry (Planned — DD25, Issue #115)

**Type:** Structural — eliminated by construction
**Feedback timing:** Runtime initialization
**What it covers:** E18 (type checker operator rules ↔ evaluator operator dispatch)
**What it misses:** Cannot verify evaluation correctness — only that an entry exists.
**Status:** Planned for issue #115

### 3.11 EvalFailCode Enum (Planned — DD23, Issue #115)

**Type:** Enum-based classification
**Feedback timing:** Build-time (via Roslyn PREC001) + test-time (sentinel tests)
**What it covers:** E39 (Fail site identity), E40 (StaticallyPreventable ↔ DiagnosticCatalog)
**What it misses:** Cannot verify that the EvalFailCode classification is semantically correct — only structurally complete.
**Status:** Planned for issue #115

### 3.12 FunctionRegistry ↔ Grammar Sync Test (Planned)

**Type:** Reflection test (cross-boundary)
**Feedback timing:** Test-time (CI)
**What it covers:** E13 (function names in TextMate grammar)
**What it misses:** Cannot run at edit-time. Roslyn cannot reach JSON files.
**Status:** Planned for issue #115

---

## Part 4: Gap Analysis

### Undefended Edges (No Enforcement)

| Edge | Source → Consumer | Current State | Severity |
|------|------------------|---------------|----------|
| **E7** | Token operators/punct → Tokenizer patterns | Manual convention | **High** — new operator token silently not produced |
| **E8** | Token vocabulary → TextMate grammar keywords | Copilot checklist only | **High** — no syntax highlighting for new keywords |
| **E9** | Token types → TextMate grammar typeKeywords | Copilot checklist only | **High** — no highlighting for new types |
| **E10** | Token constraints → TextMate grammar constraintKeywords | Copilot checklist only | **Medium** — no highlighting for new constraints |
| **E11** | Token operators → TextMate grammar operators | Copilot checklist only | **High** — no highlighting for new operators |
| **E12** | Token vocabulary → LS completions | Manual convention | **Medium** — no completion for new keywords |
| **E14** | FunctionRegistry → LS completions | Manual convention | **Medium** — no completion for new functions |
| **E15** | FunctionRegistry → LS hover content | Manual maintenance | **Medium** — stale/missing hover for functions |
| **E32** | PreceptModel → MCP CompileTool DTOs | Manual maintenance | **Low** — new property missing from MCP |
| **E33** | FieldConstraint subtypes → MCP `FormatConstraint()` | Switch with gap potential | **Low** — new constraint type not serialized |
| **E34** | PreceptClauseOutcome → MCP `MapBranch()` | Switch with gap potential | **Low** — new outcome type not serialized |
| **E35** | ConstraintViolation → MCP ViolationDtoMapper | Switch with gap potential | **Low** — new violation type not mapped |
| **E36** | PreceptScalarType → MCP JsonConvert | Manual maintenance | **Low** — new type → coercion failure |
| **E37** | Runtime fire pipeline → MCP FirePipeline | Hardcoded array | **Low** — stale pipeline docs |
| **E38** | Runtime outcomes → MCP OutcomeKinds | Hardcoded array | **Low** — missing outcome description |
| **E43** | Collection accessors → TextMate grammar | Manual maintenance | **Medium** — no highlighting for new accessors |
| **E44** | String accessors → TextMate grammar | Manual maintenance | **Low** — no highlighting for new accessors |
| **E45** | Semantic token context sets → PreceptToken | Hardcoded HashSet | **Medium** — wrong identifier coloring |
| **E46** | LS completions type names → PreceptScalarType | Hardcoded regex | **Medium** — new type not in completions |
| **E47** | Proof enrichment → DiagnosticCatalog | Hardcoded switch | **Low** — missing enrichment |
| **E48** | Expression scopes → Type checker | Hardcoded array | **Low** — stale scope descriptions |

### Edges With Only Convention/Review Enforcement

All TextMate grammar edges (E8–E11, E13, E43, E44) rely solely on copilot-instructions checklists. All LS completions/hover edges (E12, E14, E15, E45, E46) rely solely on manual convention. All MCP hardcoded-array edges (E37, E38, E47, E48) rely solely on manual convention.

### Edges Addressed by Planned Mechanisms (#115)

| Edge | Mechanism | Status |
|------|-----------|--------|
| E16 | DD24: Registry-driven function evaluation | Planned |
| E18 | DD25: Operator registry | Planned |
| E27, E28 | Roslyn PREC002: Exhaustive expression switch | Planned |
| E29, E30 | Roslyn PREC008: Exhaustive StaticValueKind switch | Planned |
| E31 | Roslyn PREC007: Exhaustive FunctionArgConstraint switch | Planned |
| E39 | DD23 + Roslyn PREC001: EvalFailCode | Planned |
| E40 | DD23 + Roslyn PREC009: StaticallyPreventable ↔ DiagnosticCatalog | Planned |
| E13 | FunctionRegistry ↔ Grammar sync test | Planned |

---

## Part 5: Design Doc Scope Recommendation

### Recommended Enforcement Hierarchy

1. **Eliminated by construction** (attribute/catalog/registry-driven derivation) — the gold standard. No enforcement needed because the alignment is structural.
2. **Roslyn analyzer** — build-time + edit-time enforcement. Red squiggles in IDE, build failures in CI.
3. **Reflection drift test** — test-time enforcement in CI. For cross-boundary edges Roslyn cannot reach (C# ↔ JSON, C# ↔ markdown).
4. **SYNC comments** — review-time markers validated by Roslyn/tests. Not standalone enforcement.
5. **Convention / copilot-instructions** — behavioral guidance. Necessary for TextMate grammar and other non-code artifacts, but not sufficient alone.

### What the New Design Doc Should Cover

#### Edges to Eliminate by Construction (Registry Pattern)

- **E16** — Already designed (DD24): function evaluation via registry delegates.
- **E18** — Already designed (DD25): operator dispatch via registry.
- **E15** — Function hover content should derive from `FunctionRegistry.AllFunctions` descriptions instead of maintaining a parallel `FunctionHoverContent` dictionary. The registry already has `Description` fields.
- **E12, E14** — LS completions keyword/function lists should derive from `PreceptTokenMeta.GetByCategory()` and `FunctionRegistry.FunctionNames` instead of hardcoded lists.

#### Edges to Enforce with Roslyn (Already Designed in Appendix F)

PREC001–PREC011 cover E5, E6, E19–E21, E27–E31, E39–E42. These are already specified.

#### Edges to Enforce with New Drift Tests (Cross-Boundary)

- **E13** (FunctionRegistry ↔ TextMate grammar) — already planned: `FunctionRegistry_GrammarSync` test.
- **E8–E11** (Token vocabulary ↔ TextMate grammar) — **new**: reflection test that reads `precept.tmLanguage.json`, extracts keyword alternation patterns, and asserts they match `PreceptTokenMeta.GetByCategory()` results. This replaces copilot-instructions-only enforcement with CI enforcement.
- **E43, E44** (Collection/string accessors ↔ TextMate grammar) — **new**: reflection test that asserts accessor names in grammar match evaluator-supported accessors.
- **E33, E34, E35** (Model subtypes ↔ MCP switch arms) — consider Roslyn exhaustive-switch enforcement if feasible, otherwise reflection tests.
- **E45** (Semantic token context sets ↔ PreceptToken members) — **new**: reflection test that asserts context sets are complete relative to token category.

#### Edges to Remain Convention-Only

- **E7** (Tokenizer operator patterns) — Low change frequency. Document the obligation.
- **E32, E36–E38, E46–E48** (MCP DTOs, hardcoded arrays, regex patterns) — Low change frequency, low risk. Document as convention obligations.

#### Document Structure Recommendation

The new design doc should:

1. **Supersede** `docs/CatalogInfrastructureDesign.md` — incorporate its three-tier catalog content as a section, not as a separate document.
2. **Incorporate** the Appendix F content from `docs/EvaluatorDesign.md` by reference — the Roslyn analyzer and registry designs live there.
3. **Add** the full alignment edge map (Part 2 of this inventory) as a living reference.
4. **Specify** new drift tests for cross-boundary edges (TextMate grammar sync, accessor sync, MCP switch completeness).
5. **Specify** LS completions/hover derivation from catalogs/registries (eliminating hardcoded lists).
6. **Define** the enforcement hierarchy and classify every edge by its enforcement tier.
7. **Track** per-edge status (implemented, planned, convention-only) as a maintenance table.