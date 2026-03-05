# Precept Language Redesign — Implementation Plan

Date: 2026-03-05
Branch: `feature/language-redesign`
Spec: `docs/PreceptLanguageDesign.md`

This plan implements the full-stack language redesign: new Superpower-based parser, updated model, updated runtime, language server, grammar, tests, samples, and docs.

---

## Guiding Principle: Tokenize Once, Consume Everywhere

Superpower produces a `TokenList<PreceptToken>` with exact source spans (`Position` = line, column, absolute offset). This single token stream replaces all regex-based text analysis across the entire codebase:

| Consumer | Currently | After |
|---|---|---|
| Parser (`PreceptParser.cs`) | 20+ compiled Regex, 1308 lines imperative | Superpower combinators on `TokenList<PreceptToken>` |
| Expression parser (`PreceptExpressionParser.cs`) | Hand-written `Lexer` (224 lines) + recursive descent `Parser` | Superpower expression combinators on same token stream (no separate lexer) |
| Semantic tokens (`SmSemanticTokensHandler.cs`) | 12 Regex per line | Walk `TokenList<PreceptToken>` — token kind → semantic token type |
| Completions (`SmDslAnalyzer.cs`) | 8+ Regex for context detection | Token-at-cursor + previous-token lookup from `TokenList<PreceptToken>` |
| Diagnostics (`SmDslAnalyzer.cs`) | Exception message regex parsing | Superpower parse errors with position + expected tokens → LSP `Diagnostic` |
| Preview handler | Calls `PreceptParser.Parse(text)` | Same — just a different parser behind the same API |

### Token stream as shared infrastructure

The tokenizer is the **foundation layer** that every other component builds on. It must be robust to incomplete input (language server feeds it partial/in-progress files on every keystroke).

Key design decisions:
- Tokenization **always succeeds** — unknown characters become an `Error` or `Unknown` token, never an exception. This guarantees the language server always has a token list for coloring and completions, even when the file is mid-edit.
- The parser works on the token list and may fail — that's fine. Token-level features (semantic coloring, completions) work regardless.
- Statement-level error recovery: on parse failure, skip forward to the next statement-starting keyword token (`field`, `state`, `event`, `invariant`, `in`, `to`, `from`, `on`) and resume.

---

## Phase 0: Branch + Superpower Dependency

**Goal:** Clean branch, Superpower installed, builds green.

- [ ] Create branch `feature/language-redesign` from current main
- [ ] Add `Superpower` NuGet package to `src/Precept/Precept.csproj`
- [ ] Verify `dotnet build` passes (no code changes yet)

---

## Phase 1: Token Enum + Tokenizer

**Goal:** A `PreceptTokenizer` that converts raw source text into `TokenList<PreceptToken>`.

**New files:**
- `src/Precept/Dsl/PreceptToken.cs` — token enum
- `src/Precept/Dsl/PreceptTokenizer.cs` — Superpower `Tokenizer<PreceptToken>`

### Token enum

```csharp
public enum PreceptToken
{
    // === Keywords: declarations ===
    Precept, Field, As, Nullable, Default, Invariant, Because,
    State, Initial, Event, With, Assert,

    // === Keywords: prepositions + modifiers ===
    In, To, From, On, When, Any, Of,

    // === Keywords: actions ===
    Set, Add, Remove, Enqueue, Dequeue, Push, Pop, Clear, Into,

    // === Keywords: outcomes ===
    Transition, No, Reject,

    // === Type keywords ===
    StringType, NumberType, BooleanType,

    // === Literal keywords ===
    True, False, Null,

    // === Operators ===
    DoubleEquals,    // ==
    NotEquals,       // !=
    GreaterThanOrEqual, // >=
    LessThanOrEqual,    // <=
    GreaterThan,     // >
    LessThan,        // <
    And,             // &&
    Or,              // ||
    Not,             // !
    Assign,          // =
    Contains,        // contains (keyword-operator)

    // === Punctuation ===
    Arrow,           // ->
    Comma,           // ,
    Dot,             // .
    LeftParen,       // (
    RightParen,      // )
    LeftBracket,     // [
    RightBracket,    // ]

    // === Identifiers + literals ===
    Identifier,
    StringLiteral,
    NumberLiteral,

    // === Structure ===
    Comment,         // # ...
    NewLine
}
```

### Tokenizer rules (priority-ordered)

1. Multi-char operators first: `==`, `!=`, `>=`, `<=`, `&&`, `||`, `->`
2. Single-char operators: `>`, `<`, `=`, `!`
3. Single-char punctuation: `,`, `.`, `(`, `)`, `[`, `]`
4. String literals: `"..."` (with `\"` escape)
5. Number literals: `\d+(\.\d+)?`
6. Comments: `#` to end-of-line
7. Keywords vs identifiers: tokenize as `Identifier`, then use Superpower's keyword recognition to promote matching identifiers. This ensures `From` (uppercase) stays an `Identifier` while `from` (lowercase) becomes `From` token.
8. Newlines: `\r\n` or `\n` — significant only for comment termination and statement boundary heuristics, but tracked as tokens for position accuracy.

### Checkpoint

- `dotnet build` passes
- Unit test: tokenize a minimal `.precept` source and assert the token sequence

---

## Phase 2: Model Records

**Goal:** New model records that represent the design doc's semantics. Old model records stay until Phase 4.

**Modified file:** `src/Precept/Dsl/PreceptModel.cs`

### New/changed records

| Record | Change | Rationale |
|---|---|---|
| `DslWorkflowModel` | Add `Invariants`, `StateAsserts`, `StateActions`; change `Transitions` to `IReadOnlyList<DslTransitionRow>` | New concepts + flat first-match rows |
| `DslInvariant` | **New** | `invariant <expr> because "reason"` — global data truth |
| `DslStateAssert` | **New** | `in/to/from <State> assert <expr> because "reason"` — with `Preposition` enum |
| `DslStateAction` | **New** | `to/from <State> -> <actions>` — entry/exit actions |
| `DslAssertPreposition` | **New enum** | `In`, `To`, `From` |
| `DslTransitionRow` | Replaces `DslTransition` | Flat: `FromState`, `EventName`, `WhenGuard?`, `Actions`, `Outcome`. No nested `DslClause` array. |
| `DslTransition` | **Removed** | Replaced by `DslTransitionRow` |
| `DslClause` | **Removed** | Clauses (if/else if/else) are replaced by ordered first-match rows |
| `DslRule` | **Removed** | Replaced by `DslInvariant`, `DslStateAssert`, `DslEventAssert` |
| `DslEventAssert` | **New** | `on <Event> assert <expr> because "reason"` — event arg validation |
| `DslEvent` | Drop `Rules`; args now use `with` keyword (parser concern only) | |
| `DslState` | Drop `Rules` | State asserts/actions are top-level, not attached to states |
| `DslField` | Drop `Rules` | Invariants are top-level |

### Preserve unchanged

- `DslExpression` hierarchy (all 5 records) — unchanged
- `DslSetAssignment` — unchanged
- `DslCollectionMutation` / `DslCollectionMutationVerb` — unchanged
- `DslClauseOutcome` hierarchy (`DslStateTransition`, `DslRejection`, `DslNoTransition`) — unchanged
- `DslScalarType`, `DslCollectionKind` — unchanged
- `DslEventArg` — unchanged

### Checkpoint

- `dotnet build` passes (old parser and runtime may use `#pragma` or compatibility shims temporarily)

---

## Phase 3: Superpower Parser

**Goal:** `PreceptParser.Parse(string)` returns a `DslWorkflowModel` using the new token+combinator pipeline.

**Modified file:** `src/Precept/Dsl/PreceptParser.cs` — **full rewrite**

### Architecture

```
string source
  → PreceptTokenizer.Tokenize(source)      // TokenList<PreceptToken>
  → PreceptParser.FileParser.Parse(tokens)  // DslWorkflowModel
```

### Parser combinators (one per grammar rule)

Organized by statement kind:

```csharp
// Entry point
static TokenListParser<PreceptToken, DslWorkflowModel> FileParser = ...;

// Declarations
static TokenListParser<PreceptToken, ...> PreceptHeader = ...;  // precept <Name>
static TokenListParser<PreceptToken, DslField> FieldDecl = ...;  // field <Name> as <Type> [nullable] [default <val>]
static TokenListParser<PreceptToken, DslInvariant> InvariantDecl = ...;  // invariant <expr> because "reason"
static TokenListParser<PreceptToken, DslState> StateDecl = ...;  // state <Name> [initial]
static TokenListParser<PreceptToken, DslEvent> EventDecl = ...;  // event <Name> [with <args>]

// Asserts
static TokenListParser<PreceptToken, DslStateAssert> StateAssert = ...;  // in/to/from <Target> assert <expr> because "reason"
static TokenListParser<PreceptToken, DslEventAssert> EventAssert = ...;  // on <Event> assert <expr> because "reason"

// Actions
static TokenListParser<PreceptToken, DslStateAction> StateAction = ...;  // to/from <Target> -> <chain>

// Transitions
static TokenListParser<PreceptToken, DslTransitionRow> TransitionRow = ...;  // from <Target> on <Event> [when <expr>] -> ... -> <outcome>

// Expressions (ported from DslExpressionParser)
static TokenListParser<PreceptToken, DslExpression> BoolExpr = ...;
// Uses Parse.Chain for operator precedence:
//   Level 1: ||
//   Level 2: &&
//   Level 3: ==, !=, >, >=, <, <=, contains
//   Level 4: ! (unary)
//   Level 5: atom (literal, identifier, identifier.member, parenthesized)

// Shared pieces
static TokenListParser<PreceptToken, string[]> StateTarget = ...;  // any | Name(, Name)*
static TokenListParser<PreceptToken, DslClauseOutcome> Outcome = ...;  // transition <State> | no transition | reject "reason"
static TokenListParser<PreceptToken, ...> ActionChain = ...;  // (-> Action)+
```

### Expression parser unification

The current `DslExpressionParser` (with its own `Lexer` class) is **eliminated**. Expression parsing uses the same Superpower token stream via combinators. Benefits:
- No "extract substring, hand to separate parser" boundary
- Expression positions map back to original source positions automatically
- One fewer parser to maintain

### `because` as expression terminator

When parsing `invariant <expr> because "reason"` or `assert <expr> because "reason"`, the expression combinator parses tokens until it encounters `Because`. This is a natural terminator — expressions can never contain the keyword `because`.

### Error recovery strategy

- The `FileParser` combinator is: `PreceptHeader` then `Statement.Many()` where `Statement` tries each statement kind (field, invariant, state, etc.) via `TokenListParser.Or()`.
- On failure, skip to the next statement-starting keyword and resume. Collect errors along the way.
- Expose `ParseWithDiagnostics(string) → (DslWorkflowModel?, IReadOnlyList<ParseDiagnostic>)` for the language server.

### Checkpoint

- `dotnet build` passes
- Parser tests: parse a full `.precept` file → verify model
- Round-trip test: current samples parse without errors

---

## Phase 4: Runtime Adaptation

**Goal:** `PreceptEngine` works with the new model records.

**Modified files:**
- `src/Precept/Dsl/PreceptRuntime.cs` — moderate changes
- `PreceptCompiler` (inside `PreceptRuntime.cs`) — updated validation

### Key changes

| Area | Current | New |
|---|---|---|
| Transition lookup | `Dict<(State,Event), DslTransition>` (single entry, clauses inside) | `Dict<(State,Event), List<DslTransitionRow>>` (ordered list, first-match) |
| Transition resolution | Iterate `DslClause` in a `DslTransition` (if/else if/else guards) | Iterate `DslTransitionRow` list; `when` guard → skip if false, else match |
| Rule evaluation | `_topLevelRules` + `_allFieldRules` + `_stateRuleMap` + `_eventRules` (all `DslRule`) | `_invariants` + `_stateAsserts` (with preposition-aware evaluation) + `_eventAsserts` |
| State asserts | Single `DslRule` list per state | `DslStateAssert` with `In`/`To`/`From` preposition; evaluation depends on whether state is changing and direction |
| State actions | Not implemented | New: run exit actions → row mutations → entry actions |
| Compile-time checks | Field/collection/state rules against defaults | All 5 state assert checks (subsumption, duplication, initial-state, contradiction, deadlock) + transition checks (coverage, unreachable row, missing outcome) |

### Fire pipeline update

Current:
1. Event rules (arg-only)
2. Guard evaluation (resolve clause within single transition)
3. Set execution
4. Collection mutations
5. Field + top-level rules (post-mutation)
6. State rules (on transition)

New (per design doc):
1. **Event asserts** (`on <Event> assert`) — arg-only context, pre-transition. If false → `Rejected`.
2. **First-match row selection** — iterate rows for `(state, event)`, evaluate `when` guards. First match wins. No match → `NotApplicable`.
3. **Exit actions** (`from <SourceState> ->`) — run automatic exit mutations.
4. **Row mutations** (`-> set ...`, `-> add ...`, etc.) — the matched row's action chain.
5. **Entry actions** (`to <TargetState> ->`) — run automatic entry mutations.
6. **Validation** — invariants, state asserts (`in`/`to`/`from` with correct temporal scoping), collect-all. If any fail → full rollback, `Rejected`.

### State assert evaluation logic

```
Given: sourceState, targetState, proposedData

if sourceState == targetState (AcceptedInPlace or no transition):
    evaluate all `in <sourceState>` asserts
else (state transition):
    evaluate all `from <sourceState>` asserts
    evaluate all `to <targetState>` asserts
    evaluate all `in <targetState>` asserts
```

### Preserve unchanged

- `DslWorkflowInstance` record — unchanged
- `DslFireResult`, `DslInspectionResult`, `DslEventInspectionResult` — unchanged
- `DslOutcomeKind` enum — unchanged
- `CreateInstance()` — minor updates for new field defaults
- `Inspect()` — updated to use new model but same return types
- `CheckCompatibility()` — unchanged
- `CoerceEventArguments()` — unchanged
- All collection operations — unchanged
- `DslExpressionRuntimeEvaluator` — unchanged

### Checkpoint

- `dotnet build` passes
- Existing test logic adapted to new model and new syntax strings
- Fire/Inspect behavior matches design doc pipeline

---

## Phase 5: Tests

**Goal:** All tests pass with the new syntax and semantics.

**Modified files:** All 7 test files in `test/Precept.Tests/`

### Test migration strategy

1. **Expression parser tests** (`DslExpressionParserTests.cs`, `DslExpressionParserEdgeCaseTests.cs`) — likely minimal changes if the expression parser API stays `DslExpressionParser.Parse(string)`. If expressions are fully integrated into Superpower, these tests either test the expression combinator in isolation or remain as integration tests.

2. **Expression evaluator tests** (`DslExpressionRuntimeEvaluatorBehaviorTests.cs`) — unchanged (evaluator doesn't change).

3. **Workflow tests** (`DslWorkflowTests.cs`) — rewrite all embedded DSL strings to new syntax. Test behavior stays the same.

4. **Set/collection tests** (`DslSetParsingTests.cs`, `DslCollectionTests.cs`) — rewrite DSL strings. Collection semantics unchanged.

5. **Rules tests** (`DslRulesTests.cs`) — rewrite to use `invariant ... because` and `assert ... because` syntax. Add new tests for `in`/`to`/`from` preposition semantics.

### New tests to add

- [ ] Tokenizer tests: specific token sequences for each statement form
- [ ] Parser combinator unit tests: each combinator in isolation
- [ ] `invariant` evaluation (always, post-commit)
- [ ] `in <State> assert` — checked on entry + AcceptedInPlace
- [ ] `to <State> assert` — checked only on cross-state entry
- [ ] `from <State> assert` — checked only on cross-state exit
- [ ] State entry/exit actions — execution order verification
- [ ] First-match row evaluation (guarded + unguarded fallback)
- [ ] `when` guard → `NotApplicable` when no row matches
- [ ] Multi-state targets (`Open, InProgress`) and `any`
- [ ] `because` sentinel parsing (expressions with various operators before `because`)
- [ ] `event Name with args` syntax
- [ ] `field Name as type nullable default val` syntax
- [ ] Compile-time checks: subsumption, duplication, initial-state, contradiction, deadlock
- [ ] sectionless file parsing (statements in any order)

### Checkpoint

- `dotnet test` all green
- No skipped tests unless explicitly noted

---

## Phase 6: Samples

**Goal:** All 18 `.precept` sample files updated to new syntax.

**Modified files:** All files in `samples/`

### Migration pattern per file

```
Old:                              New:
machine Name                →     precept Name
string? Email               →     field Email as string nullable
number Balance = 0          →     field Balance as number default 0
set<string> Tags            →     field Tags as set of string
state Open initial          →     state Open initial          (unchanged)
event Submit(string token)  →     event Submit with token as string
rule expr "reason"          →     invariant expr because "reason"  (or assert variant)
from X on Y                 →     from X on Y                (mostly unchanged)
  if guard reason "msg"     →     from X on Y when guard -> ... -> outcome
  set K = V                 →     (inline in -> pipeline)
  transition Z              →     (inline as -> transition Z)
```

### Checkpoint

- All 18 files parse without errors
- Preview handler can snapshot/fire/inspect at least one sample

---

## Phase 7: Language Server — Token-Based Rewrite

**Goal:** Language server uses the Superpower token stream instead of regex for everything.

This is where the "tokenize once, consume everywhere" principle pays off most.

### 7a. Shared tokenization layer

Add a shared tokenization method in the analyzer or a dedicated utility:

```csharp
// In SmDslAnalyzer or a new PreceptLspUtils
internal static TokenList<PreceptToken>? TryTokenize(string text)
{
    try { return PreceptTokenizer.Instance.Tokenize(text); }
    catch { return null; }
}
```

The language server calls `TryTokenize()` once per document change and caches the result. Parser, semantic tokens, completions, and diagnostics all consume from this cached token list.

### 7b. Semantic tokens (`SmSemanticTokensHandler.cs`) — rewrite

**Current:** 12+ regex patterns matched per line, manually pushing semantic tokens.

**New:** Walk `TokenList<PreceptToken>`:

```csharp
foreach (var token in tokens)
{
    var semanticType = token.Kind switch
    {
        PreceptToken.Precept or PreceptToken.Field or ... => "keyword",
        PreceptToken.StringType or PreceptToken.NumberType => "type",
        PreceptToken.StringLiteral => "string",
        PreceptToken.NumberLiteral => "number",
        PreceptToken.Comment => "comment",
        PreceptToken.Identifier => ClassifyIdentifier(token, context),
        _ when IsOperator(token.Kind) => "operator",
        _ => null
    };
    if (semanticType != null)
        builder.Push(token.Span.Position.Line, token.Span.Position.Column, token.Span.Length, semanticType);
}
```

Identifier classification (state name, event name, field name, variable) uses the parsed model or declaration-tracking from earlier tokens.

### 7c. Completions (`SmDslAnalyzer.cs`) — rewrite

**Current:** 8+ regex patterns to detect cursor context, manual identifier collection.

**New:** Find the token at or before the cursor position in the token list:

| Previous token(s) | Suggest |
|---|---|
| `As` | Type keywords: `string`, `number`, `boolean`, `set`, `queue`, `stack` |
| `Arrow` (`->`) | Action keywords: `set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `transition`, `no`, `reject` |
| `From` + `Identifier` | `on`, `assert`, `->` |
| `From` + `Identifier` + `On` | Event names |
| `On` + `Identifier` | `assert` |
| `In`/`To` + `Identifier` | `assert`, `->` |
| `Of` | Scalar type keywords: `string`, `number`, `boolean` |
| `Identifier` + `Dot` | Member names: `count`, `min`, `max`, `peek`, event arg names |
| Start of line / no context | Statement-starting keywords, semantically ordered: `field`, `invariant`, `state`, `in`, `to`, `from`, `event`, `on` |

Statement-starting keyword ordering uses semantic context: what's already declared in the file determines priority (per design doc: fields first → states → events → transitions).

### 7d. Diagnostics (`SmDslAnalyzer.cs`)

**Current:** Try `PreceptParser.Parse()`, catch exception, regex-parse error message for line number.

**New:** Use `ParseWithDiagnostics()` from Phase 3. Each diagnostic has exact position from Superpower's token spans → map directly to LSP `Diagnostic` with precise range.

### 7e. Preview handler (`SmPreviewHandler.cs`)

Minimal changes — still calls `PreceptParser.Parse()` → `PreceptCompiler.Compile()` → engine methods. The parser API is the same; the implementation behind it changed.

### Checkpoint

- Extension loads, syntax coloring works on new-syntax files
- Completions suggest correct keywords in context
- Diagnostics show with accurate positions
- Preview panel works for at least one sample

---

## Phase 8: TextMate Grammar

**Goal:** `precept.tmLanguage.json` updated for new syntax.

**Modified file:** `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`

### Changes

- Remove: `rule` keyword, section header patterns (`\[fields\]` etc.), `if`/`else` patterns, type-before-name patterns (`string? Name`)
- Add: `invariant`, `assert`, `because`, `with`, `when`, `any`, `of`, `nullable`, `default`, `initial` keywords
- Update: `field` declaration pattern, `event ... with` pattern, transition row pattern
- Reorder: Specific patterns (declarations, dotted refs) before general ones (type keywords, identifiers)

### Checkpoint

- Syntax coloring matches semantic tokens for all sample files
- No regex conflicts or mismatched scopes

---

## Phase 9: Documentation

**Goal:** README and DesignNotes reflect the new language.

**Modified files:**
- `README.md` — syntax reference, cookbook, examples, API descriptions, current status
- `docs/DesignNotes.md` — mark old DSL Syntax Contract as superseded, or replace with new

### README updates

- DSL Syntax Reference → new keyword-anchored syntax
- DSL Cookbook → rewrite all examples
- API section → updated model types, fire pipeline description
- Current Status → "Language redesign implemented on `feature/language-redesign`"

### DesignNotes updates

- DSL Syntax Contract (Current) → replace with new grammar from `PreceptLanguageDesign.md`, or mark as superseded with pointer to new doc

### Checkpoint

- No aspirational claims presented as implemented
- All code examples in docs parse correctly with the new parser
- Design doc, README, and DesignNotes are consistent

---

## File Change Summary

| File | Action | Phase |
|---|---|---|
| `src/Precept/Precept.csproj` | Add Superpower dependency | 0 |
| `src/Precept/Dsl/PreceptToken.cs` | **New** | 1 |
| `src/Precept/Dsl/PreceptTokenizer.cs` | **New** | 1 |
| `src/Precept/Dsl/PreceptModel.cs` | **Major edit** — new records, remove `DslRule`/`DslClause`/`DslTransition` | 2 |
| `src/Precept/Dsl/PreceptParser.cs` | **Full rewrite** — Superpower combinators | 3 |
| `src/Precept/Dsl/PreceptExpressionParser.cs` | **Remove** — expressions integrated into parser | 3 |
| `src/Precept/Dsl/PreceptRuntime.cs` | **Moderate edit** — new pipeline, transition lookup, assert evaluation | 4 |
| `test/Precept.Tests/*.cs` | **Rewrite DSL strings** + add new tests | 5 |
| `samples/*.precept` | **Rewrite all 18 files** | 6 |
| `tools/Precept.LanguageServer/SmSemanticTokensHandler.cs` | **Full rewrite** — token-based | 7 |
| `tools/Precept.LanguageServer/SmDslAnalyzer.cs` | **Major edit** — token-based completions + diagnostics | 7 |
| `tools/Precept.LanguageServer/SmCompletionHandler.cs` | Minor (API unchanged) | 7 |
| `tools/Precept.LanguageServer/SmPreviewHandler.cs` | Minor (API unchanged) | 7 |
| `tools/Precept.LanguageServer/SmTextDocumentSyncHandler.cs` | Minor (add tokenization cache) | 7 |
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | **Major edit** — new patterns | 8 |
| `README.md` | **Major edit** — syntax, examples, status | 9 |
| `docs/DesignNotes.md` | **Moderate edit** — supersede or replace | 9 |

## Preserve List (Unchanged)

These files/components are not modified:

- `src/Precept/Dsl/PreceptExpressionEvaluator.cs` — tree-walk evaluator, no changes
- `DslExpression` record hierarchy — `DslLiteralExpression`, `DslIdentifierExpression`, `DslUnaryExpression`, `DslBinaryExpression`, `DslParenthesizedExpression`
- `DslSetAssignment`, `DslCollectionMutation`, `DslCollectionMutationVerb`
- `DslClauseOutcome` hierarchy — `DslStateTransition`, `DslRejection`, `DslNoTransition`
- `DslScalarType`, `DslCollectionKind`, `DslEventArg`
- `DslWorkflowInstance`, `DslFireResult`, `DslInspectionResult`, `DslEventInspectionResult`, `DslOutcomeKind`
- `CollectionValue` class
- `test/Precept.Tests/Infrastructure/` — test infrastructure
- `tools/Precept.VsCode/src/` — VS Code extension TypeScript (LSP client)
- `tools/Precept.LanguageServer/SmPreviewProtocol.cs` — protocol records
- `tools/Precept.LanguageServer/Program.cs` — server bootstrapping

## Risk Points

1. **Expression parser integration** — porting hand-written recursive descent to Superpower combinators requires careful precedence handling. `Parse.Chain` handles left-recursive binary operators naturally, but `contains` (a keyword-operator, not a symbol) needs special treatment.

2. **First-match transition model** — the runtime currently stores one `DslTransition` per `(State, Event)` with multiple clauses inside. The new model has multiple `DslTransitionRow` entries per key. The ordering must be preserved from source order for first-match semantics.

3. **State assert preposition scoping** — the runtime doesn't currently distinguish between entry-only, exit-only, and while-in-state validation. This is new logic in the fire pipeline.

4. **State entry/exit actions** — entirely new runtime concept. Must execute in the correct order (exit → row → entry) and interact correctly with rollback on validation failure.

5. **Language server resilience** — the LS must work on incomplete/malformed files. Tokenization must always succeed; parsing should degrade gracefully. This needs explicit testing with partial files.

6. **Compile-time static analysis** — the 5 state assert checks (subsumption, duplication, initial-state, contradiction, deadlock) are new compile-time logic. The domain analysis (per-field interval/set analysis) is the most complex new algorithm.

## Estimated Scope

| Phase | New/modified LOC (est.) | Risk |
|---|---|---|
| 0. Branch + dependency | ~5 | None |
| 1. Token + tokenizer | ~150 | Low |
| 2. Model records | ~100 | Low |
| 3. Parser (full rewrite) | ~400 | Medium (expression integration) |
| 4. Runtime adaptation | ~300 | Medium (new pipeline logic) |
| 5. Tests | ~800 | Low (mechanical + new behavior tests) |
| 6. Samples | ~600 | Low (mechanical rewrite) |
| 7. Language server | ~500 | Medium (resilience to partial files) |
| 8. TextMate grammar | ~100 | Low |
| 9. Documentation | ~300 | Low |
| **Total** | **~3,250** | |
