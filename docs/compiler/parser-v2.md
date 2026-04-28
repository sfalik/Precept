# Parser (v2)

---

## Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Stub — `Parse()` throws `NotImplementedException`. Implementation per v8 plan (`docs/working/catalog-parser-design-v8.md`) |
| Source files | `src/Precept/Pipeline/Parser.cs`, `src/Precept/Pipeline/SyntaxNodes/` (directory) |
| Upstream | Lexer (`TokenStream`) |
| Downstream | TypeChecker, LS syntax features, MCP `precept_compile` |

**Note:** This document describes the parser as it exists after the v8 implementation plan is complete. The PR sequence and implementation slices live in `docs/working/catalog-parser-design-v8.md`.

---

## Overview

The parser is the second stage of the Precept compilation pipeline. It transforms the flat `TokenStream` produced by the lexer into a `SyntaxTree` — an abstract syntax tree representing the semantic structure of the precept definition. The parser is a hand-written recursive descent parser with a Pratt expression parser for operator precedence. It produces an AST (not a CST) — comments and whitespace are consumed silently. No information is discarded that downstream stages need; no information is preserved that only a formatter would need.

```csharp
public static SyntaxTree Parse(TokenStream tokens)
```

The parser is **catalog-driven**. `Constructs.ByLeadingToken` and `Constructs.LeadingTokens` are the primary dispatch indexes — not a hardcoded switch. For each declaration, the parser looks up the current token in `ByLeadingToken` to determine the set of candidate constructs. When the leading token maps to a single construct with no disambiguation tokens, the parser dispatches directly. When multiple constructs share a leading token, the parser enters the disambiguation path. `InvokeSlotParser()` is the generic slot dispatch mechanism — an exhaustive switch on `ConstructSlotKind` with CS8509 enforcement at build time. `BuildNode()` constructs the typed AST node from a filled slot array.

The parser always runs to end-of-source. On malformed input it emits diagnostics and inserts `IsMissing` nodes or skips to sync points, ensuring downstream stages receive a structurally coherent tree. The type checker, graph analyzer, proof engine, language server, and MCP tools all receive a complete tree — they never need to defensively handle "no tree" as a result.

The public surface:

| Member | Purpose |
|--------|---------|
| `Parser.Parse(TokenStream)` | Static pure function — sole entry point |
| `SyntaxTree` | Output record — declaration list + diagnostics |
| AST node hierarchy | Sealed records for each declaration and expression shape |
| `SourceSpan` | Location tracking — every node carries offset, length, line, column |

This matches the pipeline pattern used by all five stages: `Lexer.Lex`, `Parser.Parse`, `TypeChecker.Check`, `GraphAnalyzer.Analyze`, `ProofEngine.Prove`. Tests call the method directly and assert on the output. No instance, no DI, no configuration.

The parser handles three families of input:

- **Declarations** — precept header, field, state, event, rule, transition row, state ensure, event ensure, state action, access mode, omit declaration, event handler
- **Expressions** — arithmetic, comparison, logical, member access, function calls, conditionals, literals, interpolated strings and typed constants
- **Error recovery** — missing-node insertion for expected tokens, sync-point resync for structurally lost positions

The `SyntaxTree` is the `Compilation.SyntaxTree` field — it is part of the tooling surface and queryable by the language server for span-based operations (hover, go-to-definition, completions). `SyntaxTree.Declarations` is `ImmutableArray<Declaration>`.

---

## Responsibilities and Boundaries

**OWNS:** Source-structural representation of authored programs; error recovery shape (`IsMissing` nodes, `SkippedTokens`); `SourceSpan` ownership for every node; disambiguation of dual-use tokens (`set`, `min`, `max`) by syntactic position; parsing `OmitDeclaration` as a structurally separate construct from `AccessMode`.

**Does NOT OWN:** Name resolution, type compatibility, overload selection, semantic legality (`AllowedIn` constraint enforcement) — these belong to the TypeChecker. Token classification — the Lexer already classifies all tokens before the parser sees them. Operator precedence is parser-internal (not catalog metadata).

---

## Right-Sizing

The parser is scoped to structural reconstruction of a flat, line-oriented, keyword-anchored grammar. It does not attempt semantic validation (that belongs to the TypeChecker) and does not produce a CST (that would serve only a formatter, which Precept does not have). The flat declaration list as the primary output shape — not a nested block tree — is a right-sizing decision: Precept's grammar has no deeply-nested block structure, so the complexity budget of a block-scope parser is unnecessary. The three-family split (declarations, expressions, statements) is the minimum needed to give downstream consumers a useful type-level partition without over-specifying the tree shape.

---

## Inputs and Outputs

**Input:** `TokenStream` — `ImmutableArray<Token>` produced by the Lexer. Each token carries `Kind`, `Text`, and `SourceSpan`.

**Output:** `SyntaxTree` — `PreceptHeaderNode? Header`, `ImmutableArray<Declaration> Declarations`, `ImmutableArray<Diagnostic> Diagnostics`. Always produced, even from broken input.

| Member | Type | Notes |
|---|---|---|
| `Header` | `PreceptHeaderNode?` | null if `precept` keyword is missing or source is empty |
| `Declarations` | `ImmutableArray<Declaration>` | flat declaration list in source order |
| `Diagnostics` | `ImmutableArray<Diagnostic>` | parse-phase diagnostics only; merged into `Compilation.Diagnostics` |

---

## Architecture

### Static Class + ParseSession

The parser follows the same structural pattern as the lexer: a static class with a single public method, and a private mutable struct that holds all parsing state for a single invocation.

```csharp
public static class Parser
{
    public static SyntaxTree Parse(TokenStream tokens)
    {
        var session = new ParseSession(tokens);
        session.ParseAll();
        return session.Build();
    }

    private struct ParseSession
    {
        private readonly TokenStream _tokens;
        private int _position;
        private ImmutableArray<SyntaxNode>.Builder _declarations;
        private ImmutableArray<Diagnostic>.Builder _diagnostics;
        // ...
    }
}
```

`ParseSession` is stack-allocated. The only heap allocations per parse are the `ImmutableArray.Builder` instances, the AST node records, and the final `SyntaxTree` output. The struct is instantiated inside `Parse()` and discarded after — no instance leaks, no reuse, no thread-safety concerns.

#### Token Navigation

`ParseSession` maintains a `_position` index into the `TokenStream`. Helper methods provide the navigation vocabulary:

| Method | Behavior |
|--------|----------|
| `Current()` | Returns `_tokens[_position]` without advancing |
| `Peek(int offset)` | Returns `_tokens[_position + offset]` for bounded lookahead |
| `Advance()` | Returns `Current()` and increments `_position` |
| `Expect(TokenKind)` | If `Current()` matches, advances and returns the token. Otherwise emits `ExpectedToken` diagnostic and returns a synthetic `IsMissing` token at the current span |
| `Match(TokenKind)` | If `Current()` matches, advances and returns `true`. Otherwise returns `false` without advancing |
| `SkipTrivia()` | Advances past `NewLine` and `Comment` tokens. Called before each dispatch decision |

`Expect()` is the primary error-recovery primitive. It always returns a token — real or synthetic — so the calling production can always construct a complete node. The synthetic token carries `IsMissing = true` and a zero-length `SourceSpan` at the current position, preserving the location for downstream diagnostics.

### 5-Layer Architecture

The parser is organized into five layers:

| Layer | Name | Responsibility |
|-------|------|----------------|
| **A** | Vocabulary FrozenDictionaries | Operator precedence, type keywords, modifier sets, action recognition — all derived from catalog metadata at startup. No hardcoded vocabulary in the parser. |
| **B** | Top-Level Dispatch | Keyword-dispatched loop. Leading token → dispatch to construct parser or disambiguator. `Constructs.ByLeadingToken` index provides the mapping. |
| **C** | Generic Slot Iteration | `ParseConstructSlots()` iterates `ConstructMeta.Slots`, calling `InvokeSlotParser()` per slot. CS8509 exhaustive switch on `ConstructSlotKind`. |
| **D** | Disambiguation | Four scoped prepositions (`in`, `to`, `from`, `on`) share a generic disambiguator. Consumes the anchor target, peeks at the disambiguation token, routes to the matched construct. |
| **E** | Error Sync | `SyncToNextDeclaration()` recovers from parse errors by advancing to the next known leading token, derived from `Constructs.LeadingTokens`. |

### Top-Level Dispatch Loop

`ParseAll()` consumes the `precept` header, then enters a loop that skips trivia (newlines, comments) and dispatches on the current token. The dispatch is catalog-driven:

```
Current token → Constructs.ByLeadingToken lookup → candidates
  If 1 candidate, no disambiguation tokens → direct ParseConstruct()
  If 1+ candidates with disambiguation tokens → DisambiguateAndParse()
  Not found → EmitDiagnostic + SyncToNextDeclaration()
```

The resulting dispatch table:

| Current token | Candidates | Dispatch |
|---------------|------------|----------|
| `Precept` | 1 (PreceptHeader) | direct |
| `Field` | 1 (FieldDeclaration) | direct |
| `State` | 1 (StateDeclaration) | direct |
| `Event` | 1 (EventDeclaration) | direct |
| `Rule` | 1 (RuleDeclaration) | direct |
| `In` | 3 (StateEnsure, AccessMode, OmitDeclaration) | disambiguate |
| `To` | 2 (StateEnsure, StateAction) | disambiguate |
| `From` | 3 (TransitionRow, StateEnsure, StateAction) | disambiguate |
| `On` | 2 (EventEnsure, EventHandler) | disambiguate |
| `EndOfSource` | — | exit loop |
| *anything else* | — | `EmitDiagnostic` + `SyncToNextDeclaration()` |

The dispatch table is built from `Constructs.ByLeadingToken` — a `FrozenDictionary<TokenKind, ImmutableArray<(ConstructKind, DisambiguationEntry)>>` derived at startup from all `ConstructMeta.Entries`. The parser does not maintain a parallel keyword list.

### Preposition Disambiguation

The four preposition-scoped methods parse a state or event target, optionally pre-consume a `when` guard (stashing it for later injection), then look ahead to select the specific production.

**`ParseInScoped()` / `DisambiguateAndParse` for `In`** (3 constructs):

| Lookahead after state target | Production | `ConstructKind` |
|------------------------------|-----------|-----------------|
| `Ensure` | `ParseStateEnsure(In)` | `StateEnsure` |
| `Modify` | `ParseAccessMode(stateTarget)` | `AccessMode` |
| `Omit` | `ParseOmitDeclaration(stateTarget)` | `OmitDeclaration` |
| `When` *(then re-check)* | stash guard, re-dispatch | *(same as above)* |

`OmitDeclaration` is a distinct construct — NOT a sub-case of `AccessMode`. The disambiguation splits cleanly on the verb token: `modify` routes to `AccessMode`, `omit` routes to `OmitDeclaration`. They share no parent node type and have different slot sequences (4 slots vs. 2 slots), different guard eligibility (optional vs. never), and different semantic categories (mutability constraint vs. structural exclusion).

When the disambiguator pre-consumes a `when` guard and then routes to `OmitDeclaration`, it emits `DiagnosticCode.OmitDoesNotSupportGuard`. The guard is discarded — there is no slot to inject it into. The resulting `OmitDeclarationNode` has no guard.

**`ParseToScoped()`** — Leading token `to` (2 constructs):

| Lookahead after state target | Production | `ConstructKind` |
|------------------------------|-----------|-----------------|
| `Ensure` | `ParseStateEnsure(To)` | `StateEnsure` |
| `Arrow` | `ParseStateAction(To)` | `StateAction` |
| `When` *(then re-check)* | consume guard, re-dispatch | *(same as above)* |

**`ParseFromScoped()`** — Leading token `from` (3 constructs):

| Lookahead after state target | Production | `ConstructKind` |
|------------------------------|-----------|-----------------|
| `On` | `ParseTransitionRow()` | `TransitionRow` |
| `Ensure` | `ParseStateEnsure(From)` | `StateEnsure` |
| `Arrow` | `ParseStateAction(From)` | `StateAction` |
| `When` *(then re-check)* | consume guard, re-dispatch | *(same as above)* |

For `from`-scoped transition rows: when a guard was pre-consumed (stashedGuard is not null) and the disambiguation routes to `TransitionRow` (token is `On`), the parser emits `DiagnosticCode.PreEventGuardNotAllowed`. Error recovery injects the guard at the post-event `GuardClause` slot.

**`ParseOnScoped()`** — Leading token `on` (2 constructs):

| Lookahead after event target | Production | `ConstructKind` |
|------------------------------|-----------|-----------------|
| `Ensure` | `ParseEventEnsure()` | `EventEnsure` |
| `Arrow` | `ParseEventHandler()` | `EventHandler` |
| `When` *(then re-check)* | consume guard, re-dispatch | *(same as above)* |

The `When` case is the only two-step lookahead: the preposition method consumes the `when` guard expression, then re-inspects the next token to select the production. This is still bounded — the guard is parsed as a normal expression (Pratt parser stops at `ensure`, `->`, or a newline), and the next token is inspected exactly once.

An example from the insurance-claim sample illustrates the preposition flow:

```precept
from UnderReview on Approve when (not PoliceReportRequired or MissingDocuments.count == 0) and Approve.Amount <= ClaimAmount
    -> set ApprovedAmount = if FraudFlag then min(Approve.Amount, ClaimAmount / 2) else Approve.Amount
    -> set DecisionNote = Approve.Note
    -> transition Approved
```

The parser sees `from` → calls `ParseFromScoped()` → parses state target `UnderReview` → sees `on` → calls `ParseTransitionRow()` → parses event target `Approve` → sees `when` → calls `ParseExpression(0)` for the guard → sees `->` → enters action chain loop → parses two `set` actions → sees `-> transition` → parses outcome → emits `TransitionRow` node. Each `->` is consumed as an action separator; the final `-> transition` terminates the loop.

### SourceSpan Contract

Every AST node carries a `SourceSpan` that covers its full extent in the original source. The `SourceSpan` combines two coordinate systems:

```csharp
public readonly record struct SourceSpan(
    int Offset, int Length,
    int StartLine, int StartColumn,
    int EndLine, int EndColumn);
```

- **Offset/Length** — for slicing the source string (used by the evaluator for error messages, by MCP tools for snippet extraction)
- **Line/Column** — for LSP diagnostics, hover, and go-to-definition (1-based lines, 1-based start column, exclusive end column per LSP convention)

`SourceSpan.Covering(first, last)` computes the minimal span that encloses two child spans — used to build declaration-level spans from their component tokens. `SourceSpan.Missing` (all zeros) marks synthetic `IsMissing` nodes.

Downstream stages never need the raw source text to emit located diagnostics — the `SourceSpan` carries both coordinate systems on every node.

---

## AST Node Hierarchy

### Base Types

```csharp
public abstract record SyntaxNode(SourceSpan Span);
public abstract record Declaration(SourceSpan Span) : SyntaxNode(Span);
public abstract record Statement(SourceSpan Span) : SyntaxNode(Span);
public abstract record Expression(SourceSpan Span) : SyntaxNode(Span);
```

`Declaration` is the base for all declaration nodes (the 12 construct types). `Statement` covers action statements inside arrow chains. `Expression` covers the Pratt-parsed expression tree. The type checker, graph analyzer, and language server all switch on these three families.

### SyntaxTree

```csharp
public sealed record SyntaxTree(
    PreceptHeaderNode? Header,
    ImmutableArray<Declaration> Declarations,
    ImmutableArray<Diagnostic> Diagnostics);
```

`Header` is nullable for the case where the source is empty or the `precept` keyword is missing — the parser emits an `ExpectedToken` diagnostic and produces a tree with `Header = null`. `Declarations` is the flat list in source order. `Diagnostics` accumulates every parse-stage diagnostic. This is the `Compilation.SyntaxTree` field.

### FieldTargetNode — Discriminated Union

```csharp
public abstract record FieldTargetNode(SourceSpan Span) : SyntaxNode(Span);

public sealed record SingularFieldTarget(SourceSpan Span, Token Name) : FieldTargetNode(Span);
public sealed record ListFieldTarget(SourceSpan Span, ImmutableArray<Token> Names) : FieldTargetNode(Span);
public sealed record AllFieldTarget(SourceSpan Span, Token AllToken) : FieldTargetNode(Span);
```

**Why a discriminated union:** The three shapes carry structurally different data — `Token` vs. `ImmutableArray<Token>` vs. keyword token. A flat record with nullable fields would have `Token? Name`, `ImmutableArray<Token>? Names`, `Token? AllToken` — three mutually exclusive nullable fields where exactly one must be non-null. This is the exact anti-pattern that `catalog-system.md` § Architectural Identity prohibits: flat records with inapplicable nullable fields. The DU ensures compile-time exhaustiveness in every consumer via pattern matching — downstream consumers (type checker, graph analyzer, MCP) switch on the subtype and access only the fields that exist for that shape. Invalid states are structurally impossible.

### Declaration Nodes

#### PreceptHeader

```
precept Identifier
```

```csharp
public sealed record PreceptHeaderNode(
    SourceSpan Span, Token Name)
    : Declaration(Span);
```

```precept
precept InsuranceClaim
```

#### FieldDeclaration

```
field Identifier ("," Identifier)* as TypeRef FieldModifier* ("->" Expr)?
```

```csharp
public sealed record FieldDeclarationNode(
    SourceSpan Span,
    ImmutableArray<Token> Names,
    TypeRefNode Type,
    ImmutableArray<FieldModifierNode> Modifiers,
    Expression? ComputedExpression)
    : Declaration(Span);
```

```precept
field ClaimAmount as decimal default 0 nonnegative maxplaces 2
```

#### StateDeclaration

```
state StateEntry ("," StateEntry)*
StateEntry := Identifier StateModifier*
StateModifier := initial | terminal | required | irreversible | success | warning | error
```

```csharp
public sealed record StateDeclarationNode(
    SourceSpan Span,
    ImmutableArray<StateEntryNode> Entries)
    : Declaration(Span);

public sealed record StateEntryNode(
    SourceSpan Span, Token Name,
    ImmutableArray<Token> Modifiers)
    : SyntaxNode(Span);
```

```precept
state Draft initial, Submitted, UnderReview, Approved, Denied, Paid
```

#### EventDeclaration

```
event Identifier ("," Identifier)* ("with" ArgList)? ("initial")?
ArgList := ArgDecl ("," ArgDecl)*
ArgDecl := Identifier as TypeRef FieldModifier*
```

```csharp
public sealed record EventDeclarationNode(
    SourceSpan Span,
    ImmutableArray<Token> Names,
    ImmutableArray<ArgumentNode> Arguments,
    bool IsInitial)
    : Declaration(Span);

public sealed record ArgumentNode(
    SourceSpan Span, Token Name,
    TypeRefNode Type,
    ImmutableArray<FieldModifierNode> Modifiers)
    : SyntaxNode(Span);
```

```precept
event Approve with Amount as decimal, Note as string nullable default null notempty
```

#### RuleDeclaration

```
rule BoolExpr ("when" BoolExpr)? because StringExpr
```

```csharp
public sealed record RuleDeclarationNode(
    SourceSpan Span,
    Expression Condition,
    Expression? Guard,
    Expression Message)
    : Declaration(Span);
```

```precept
rule ApprovedAmount <= ClaimAmount because "Approved amounts cannot exceed the claim"
```

#### TransitionRow

```
from StateTarget on Identifier ("when" BoolExpr)?
  ("->" ActionStatement)* "->" Outcome
```

```csharp
public sealed record TransitionRowNode(
    SourceSpan Span,
    StateTargetNode FromState,
    Token EventName,
    Expression? Guard,
    ImmutableArray<Statement> Actions,
    OutcomeNode Outcome)
    : Declaration(Span);
```

```precept
from UnderReview on Approve when (not PoliceReportRequired or MissingDocuments.count == 0) and Approve.Amount <= ClaimAmount
    -> set ApprovedAmount = if FraudFlag then min(Approve.Amount, ClaimAmount / 2) else Approve.Amount
    -> set DecisionNote = Approve.Note
    -> transition Approved
```

#### StateEnsure

```
(in|to|from) StateTarget ("when" BoolExpr)? ensure BoolExpr because StringExpr
```

```csharp
public sealed record StateEnsureNode(
    SourceSpan Span,
    Token Preposition,
    StateTargetNode State,
    Expression? Guard,
    Expression Condition,
    Expression Message)
    : Declaration(Span);
```

```precept
in Approved ensure ApprovedAmount > 0 because "Approved claims must specify a payout amount"
```

#### AccessMode

```
in StateTarget modify FieldTarget readonly|editable ("when" BoolExpr)?
```

`AccessMode` is a separate construct from `OmitDeclaration`. It declares the mutability constraint of a field within a state — the field is present, and its access level is constrained. The `modify` keyword is the disambiguation token consumed by the disambiguator; it is not stored as a slot value.

```csharp
public sealed record AccessModeNode(
    SourceSpan Span,
    StateTargetNode State,
    FieldTargetNode Fields,
    Token Mode,          // TokenKind.Readonly or TokenKind.Editable
    Expression? Guard)   // optional when-clause
    : Declaration(Span);
```

```precept
in UnderReview modify FraudFlag editable
```

#### OmitDeclaration

```
in StateTarget omit FieldTarget
```

`OmitDeclaration` is a separate construct from `AccessMode`. It declares structural exclusion — the field is absent from the state entirely. It has no `Mode` slot, no `GuardClause` slot — exclusion is unconditional. The `omit` keyword is the disambiguation token consumed by the disambiguator; it is not stored as a slot value.

The structural separation is not incidental. `OmitDeclaration` and `AccessMode` differ in: (a) slot sequence (omit has 2 slots, access mode has 4), (b) guard eligibility (omit: never, access mode: optional), (c) semantic category (structural exclusion vs. mutability constraint). A shared node would require internal branching and nullable fields for the mode and guard that are structurally impossible for omit. Per `catalog-system.md` § Architectural Identity: "Do not use flat records with inapplicable nullable fields — use a DU instead."

NEVER has a guard clause — the slot sequence `[StateTarget, FieldTarget]` enforces this structurally. Attempting `in State omit Field when Guard` is a parse error (`DiagnosticCode.OmitDoesNotSupportGuard`).

```csharp
public sealed record OmitDeclarationNode(
    SourceSpan Span,
    StateTargetNode State,
    FieldTargetNode Fields)
    : Declaration(Span);
```

```precept
in Draft omit InternalNotes
```

#### StateAction

```
(to|from) StateTarget ("when" BoolExpr)? ("->" ActionStatement)*
```

```csharp
public sealed record StateActionNode(
    SourceSpan Span,
    Token Preposition,
    StateTargetNode State,
    Expression? Guard,
    ImmutableArray<Statement> Actions)
    : Declaration(Span);
```

```precept
to Submitted -> set submittedAt = now()
```

#### EventEnsure

```
on Identifier ("when" BoolExpr)? ensure BoolExpr because StringExpr
```

```csharp
public sealed record EventEnsureNode(
    SourceSpan Span,
    Token EventName,
    Expression? Guard,
    Expression Condition,
    Expression Message)
    : Declaration(Span);
```

```precept
on Submit ensure Amount > 0 because "Claim amounts must be positive"
```

#### EventHandler

```
on Identifier ("->" ActionStatement)*
```

```csharp
public sealed record EventHandlerNode(
    SourceSpan Span,
    Token EventName,
    ImmutableArray<Statement> Actions)
    : Declaration(Span);
```

```precept
on UpdateName -> set name = newName
```

### Complete Declaration Node Summary

| # | ConstructKind | Node Type | Slots |
|---|---------------|-----------|-------|
| 1 | PreceptHeader | PreceptHeaderNode | [IdentifierList] |
| 2 | FieldDeclaration | FieldDeclarationNode | [IdentifierList, TypeExpression, ModifierList?, ComputeExpression?] |
| 3 | StateDeclaration | StateDeclarationNode | [IdentifierList, StateModifierList?] |
| 4 | EventDeclaration | EventDeclarationNode | [IdentifierList, ArgumentList?] |
| 5 | RuleDeclaration | RuleDeclarationNode | [RuleExpression, GuardClause?, BecauseClause] |
| 6 | TransitionRow | TransitionRowNode | [StateTarget, EventTarget, GuardClause?, ActionChain?, Outcome] |
| 7 | StateEnsure | StateEnsureNode | [StateTarget, EnsureClause] |
| 8 | AccessMode | AccessModeNode | [StateTarget, FieldTarget, AccessModeKeyword, GuardClause?] |
| 9 | OmitDeclaration | OmitDeclarationNode | [StateTarget, FieldTarget] |
| 10 | StateAction | StateActionNode | [StateTarget, ActionChain] |
| 11 | EventEnsure | EventEnsureNode | [EventTarget, EnsureClause] |
| 12 | EventHandler | EventHandlerNode | [EventTarget, ActionChain] |

### Action Statement Nodes

Action statements appear inside arrow chains (`-> action -> action -> ...`). Each action keyword maps to a sealed record:

```csharp
public sealed record SetStatement(
    SourceSpan Span, Token Field, Expression Value) : Statement(Span);

public sealed record AddStatement(
    SourceSpan Span, Token Field, Expression Value) : Statement(Span);

public sealed record RemoveStatement(
    SourceSpan Span, Token Field, Expression Value) : Statement(Span);

public sealed record EnqueueStatement(
    SourceSpan Span, Token Field, Expression Value) : Statement(Span);

public sealed record DequeueStatement(
    SourceSpan Span, Token Field, Token? IntoField) : Statement(Span);

public sealed record PushStatement(
    SourceSpan Span, Token Field, Expression Value) : Statement(Span);

public sealed record PopStatement(
    SourceSpan Span, Token Field, Token? IntoField) : Statement(Span);

public sealed record ClearStatement(
    SourceSpan Span, Token Field) : Statement(Span);
```

`set` takes `Field = Value`. `add`, `remove`, `enqueue`, and `push` take `Field Value`. `dequeue` and `pop` take `Field` with an optional `into Field` for destructuring. `clear` takes only `Field`.

### Outcome Nodes

Outcomes terminate a transition row's arrow chain. Three shapes:

```csharp
public abstract record OutcomeNode(SourceSpan Span) : SyntaxNode(Span);

public sealed record TransitionOutcomeNode(
    SourceSpan Span, Token TargetState) : OutcomeNode(Span);

public sealed record NoTransitionOutcomeNode(
    SourceSpan Span) : OutcomeNode(Span);

public sealed record RejectOutcomeNode(
    SourceSpan Span, Expression Message) : OutcomeNode(Span);
```

```precept
-> transition Approved
-> no transition
-> reject "Required documents must be complete before a claim can be approved"
```

### Supporting Types

#### StateTarget

```csharp
public sealed record StateTargetNode(
    SourceSpan Span, Token Name, bool IsQuantifier) : SyntaxNode(Span);
```

`Name` is either a state identifier or the `any` quantifier keyword. `IsQuantifier` is `true` when the target is `any` rather than a specific state name.

#### TypeRef Hierarchy

```csharp
public abstract record TypeRefNode(SourceSpan Span) : SyntaxNode(Span);

public sealed record ScalarTypeRefNode(
    SourceSpan Span, Token TypeName, TypeQualifierNode? Qualifier) : TypeRefNode(Span);

public sealed record CollectionTypeRefNode(
    SourceSpan Span, Token CollectionKind, Token ElementType,
    TypeQualifierNode? Qualifier) : TypeRefNode(Span);

public sealed record ChoiceTypeRefNode(
    SourceSpan Span, ImmutableArray<Expression> Options) : TypeRefNode(Span);
```

`ScalarTypeRefNode` covers `string`, `decimal`, `boolean`, `money`, etc. `CollectionTypeRefNode` covers `set of T`, `queue of T`, `stack of T` — `CollectionKind` is the `Set`/`Queue`/`Stack` token. `ChoiceTypeRefNode` covers `choice("A", "B", "C")`.

#### TypeQualifier

```csharp
public sealed record TypeQualifierNode(
    SourceSpan Span, Token Keyword, Expression Value) : SyntaxNode(Span);
```

`Keyword` is `In` or `Of` — narrowing the type domain (e.g., `money in 'USD'`, `quantity of 'weight'`).

#### FieldModifier Hierarchy

```csharp
public abstract record FieldModifierNode(SourceSpan Span) : SyntaxNode(Span);

public sealed record FlagModifierNode(
    SourceSpan Span, Token Keyword) : FieldModifierNode(Span);

public sealed record ValueModifierNode(
    SourceSpan Span, Token Keyword, Expression Value) : FieldModifierNode(Span);
```

Flag modifiers (`optional`, `writable`, `nonnegative`, `positive`, `nonzero`, `notempty`, `ordered`) carry only the keyword token. Value modifiers (`default`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`) carry a keyword and an expression value. The DU prevents consumers from accessing a `Value` property on flag-only modifiers.

---

## Grammar Reference

### Access Mode Forms (6 — guarded)

```
in State modify Field readonly [when Guard]            ← singular
in State modify Field editable [when Guard]            ← singular
in State modify F1, F2, ... readonly [when Guard]      ← list (comma-separated shorthand)
in State modify F1, F2, ... editable [when Guard]      ← list
in State modify all readonly [when Guard]              ← all
in State modify all editable [when Guard]              ← all
```

### Omit Forms (3 — never guarded)

```
in State omit Field                                    ← singular (never has guard)
in State omit F1, F2, ...                             ← list (never has guard)
in State omit all                                      ← all (never has guard)
```

### Grammar Production Rules

```
AccessModeDeclaration := "in" StateTarget "modify" FieldTarget AccessModeKeyword GuardClause?
OmitDeclaration       := "in" StateTarget "omit" FieldTarget

FieldTarget           := Identifier
                       | Identifier ("," Identifier)+
                       | "all"

AccessModeKeyword     := "readonly" | "editable"
GuardClause           := "when" Expression
StateTarget           := Identifier
```

### Guard Restriction

`omit` never accepts a `when` guard clause. Structural field presence must never be data-dependent — this is a permanently locked invariant, not a current-sprint decision.

- **Post-field position:** Attempting `in State omit Field when Guard` is a parse error. The parser emits `DiagnosticCode.OmitDoesNotSupportGuard` and recovers by consuming/discarding the guard, producing an `OmitDeclarationNode` with no guard.
- **Pre-stashed position:** When the generic disambiguator pre-consumes a `when` before routing (e.g., `in State when Guard omit Field`), the parser detects that the routed construct is `OmitDeclaration` and emits the same `DiagnosticCode.OmitDoesNotSupportGuard`. The guard is discarded — there is no slot to inject it into.

Both positions produce the same diagnostic code and the same recovery: `OmitDeclarationNode` with no guard.

---

## Slot Dispatch

### `InvokeSlotParser()` Mechanism

`ParseConstructSlots()` iterates `ConstructMeta.Slots` for the routed construct, calling `InvokeSlotParser()` for each slot. `InvokeSlotParser()` is an exhaustive switch on `ConstructSlotKind` — CS8509 enforced at build time. Adding a new `ConstructSlotKind` member without a corresponding arm in this switch is a compilation error. Each arm calls the corresponding named slot parser method.

| `ConstructSlotKind` | Parser Method |
|---------------------|---------------|
| `IdentifierList` | `ParseIdentifierList()` |
| `TypeExpression` | `ParseTypeExpression()` |
| `ModifierList` | `ParseModifierList()` |
| `StateModifierList` | `ParseStateModifierList()` |
| `ArgumentList` | `ParseArgumentList()` |
| `ComputeExpression` | `ParseComputeExpression()` |
| `RuleExpression` | `ParseRuleExpression()` |
| `GuardClause` | `ParseGuardClause()` |
| `BecauseClause` | `ParseBecauseClause()` |
| `ActionChain` | `ParseActionChain()` |
| `Outcome` | `ParseOutcome()` |
| `StateTarget` | `ParseStateTarget()` |
| `EventTarget` | `ParseEventTarget()` |
| `EnsureClause` | `ParseEnsureClause()` |
| `AccessModeKeyword` | `ParseAccessModeKeyword()` |
| `FieldTarget` | `ParseFieldTarget()` |

Optional slots (those with `IsRequired = false`) are skipped when the current token does not match the slot's expected leading token.

### `BuildNode()` Mechanism

After all slots are filled, `BuildNode()` constructs the typed AST node from the slot array. This is an exhaustive switch on `ConstructKind` — CS8509 enforced at build time. Adding a new `ConstructKind` without a `BuildNode` arm is a compilation error. Each arm casts the slot values to their expected types and constructs the corresponding sealed record.

---

## `set` Disambiguation

The lexer always emits `TokenKind.Set`. The parser disambiguates by position:

| Context | Interpretation | How detected |
|---------|---------------|-------------- |
| After `as` or `of` in `ParseTypeRef()` | Collection type (`set of T`) | Next token is `Of` |
| After `->` in action chain | Assignment action (`set X = V`) | Inside `ParseActionChain()` |
| After `is` / `is not` in expression | Presence test (`X is set`) | Inside Pratt left-denotation for `Is` |

The parser never synthesizes a `TokenKind.SetType` token. It treats `Set` in a type position as a collection type constructor and produces a `CollectionTypeNode` with the set kind. The AST encodes the semantic meaning; the token kind stays as-is.

---

## `min` / `max` Disambiguation

`min` and `max` serve dual roles: constraint keyword in field modifier position, built-in function in expression position.

| Context | Interpretation | How detected |
|---------|---------------|-------------- |
| Inside `ParseFieldModifiers()` | Constraint — consumes a following expression as the bound value | Current position is the modifier zone after a type reference |
| Inside `ParseExpression()` nud | Function call — followed by `(` | Next token is `LeftParen` |

The disambiguation is trivial: constraint keywords are never followed by `(`, and function calls always are. The Pratt parser's null-denotation handler checks: if the token is `Min` or `Max` and the next token is `LeftParen`, it parses a function call expression. Otherwise, it falls through to identifier handling (which would be an error in expression position — the type checker catches it).

An example from the insurance-claim sample shows both uses in a single transition:

```precept
field ClaimAmount as decimal default 0 nonnegative maxplaces 2

from UnderReview on Approve when ...
    -> set ApprovedAmount = if FraudFlag then min(Approve.Amount, ClaimAmount / 2) else Approve.Amount
```

In the field declaration, `nonnegative` and `maxplaces 2` are constraint modifiers parsed by `ParseFieldModifiers()`. In the action expression, `min(Approve.Amount, ClaimAmount / 2)` is a function call parsed by the Pratt expression parser's null-denotation for `Min` + `LeftParen`.

---

## Expression Parsing Detail

The Pratt parser is the shared expression engine for all production methods. Any slot that expects an expression — guard clauses, ensure clauses, action RHS values, default values, constraint bounds, computed expressions — calls `ParseExpression(0)` (or `ParseExpression(minBp)` for sub-expressions at a specific precedence floor).

The binding power table:

| Token(s) | Left BP | Right BP | Associativity |
|----------|---------|----------|:-------------:|
| `or` | 10 | 10 | left |
| `and` | 20 | 20 | left |
| `not` (prefix) | — | 25 | right |
| `==` `!=` `~=` `!~` `<` `>` `<=` `>=` | 30 | 31 | non-associative |
| `contains` | 40 | 40 | left |
| `is` | 40 | 40 | left |
| `+` `-` (infix) | 50 | 50 | left |
| `*` `/` `%` | 60 | 60 | left |
| `-` (prefix) | — | 65 | right |
| `.` | 80 | 80 | left |
| `(` (postfix) | 80 | 0 | left |

Non-associative comparisons use right-binding power 31 (one above the left-binding power of 30) to prevent right-associativity. The explicit left-operand check in the comparison handler catches left-associative chaining and emits `NonAssociativeComparison`.

### Null-Denotation (Atoms and Prefix)

The null-denotation is the entry point for expressions. It handles atoms (identifiers, literals, parenthesized expressions) and prefix operators (`not`, unary `-`). When the current token has no null-denotation entry, the parser emits `ExpectedToken("expression")` and returns a missing `IdentifierExpression`.

### Left-Denotation (Infix and Postfix)

The left-denotation handles infix operators, member access (`.`), function calls (`(`), and the `is`/`is not`/`contains` keyword operators. The `is` handler is multi-token: it consumes an optional `Not`, then expects `Set`. The `contains` handler parses the right operand at the same binding power.

### Conditional Expressions

`if`/`then`/`else` is parsed as a null-denotation: consume `if`, parse condition at BP 0, expect `then`, parse consequent at BP 0, expect `else`, parse alternative at BP 0. The `else` branch is required — there is no short-form `if`/`then` without `else`.

An example from the insurance-claim sample:

```precept
-> set ApprovedAmount = if FraudFlag then min(Approve.Amount, ClaimAmount / 2) else Approve.Amount
```

The Pratt parser sees `if` as a null-denotation entry, parses `FraudFlag` as the condition (stops at `then`), parses `min(Approve.Amount, ClaimAmount / 2)` as the consequent (stops at `else`), and parses `Approve.Amount` as the alternative (stops at the next newline or `->`, which has no binding power).

### Interpolation Reassembly

The parser reassembles interpolated literals from the segmented token stream the lexer produced. Both `ParseInterpolatedString()` and `ParseInterpolatedTypedConstant()` use the same loop:

1. Consume `Start` token → `TextSegment`
2. `ParseExpression(0)` → `ExpressionSegment`
3. If `Middle` → `TextSegment`, go to step 2
4. If `End` → `TextSegment`, done

`ParseExpression(0)` terminates naturally at `StringMiddle`/`StringEnd`/`TypedConstantMiddle`/`TypedConstantEnd` because these token kinds have no binding power in the expression parser. This is the depth-unaware reassembly property: because `}` always ends an interpolation hole and has no meaning in the expression grammar, the parser stops naturally without tracking nesting depth.

### Action Chain Parsing

The action chain is a loop that consumes `->` followed by an action keyword. Each action is a self-contained statement:

| Action keyword | Syntax | Slot |
|----------------|--------|------|
| `set` | `set Identifier = Expr` | scalar assignment |
| `add` | `add Identifier Expr` | set add |
| `remove` | `remove Identifier Expr` | set remove |
| `enqueue` | `enqueue Identifier Expr` | queue enqueue |
| `dequeue` | `dequeue Identifier (into Identifier)?` | queue dequeue |
| `push` | `push Identifier Expr` | stack push |
| `pop` | `pop Identifier (into Identifier)?` | stack pop |
| `clear` | `clear Identifier` | collection clear |

The loop breaks when the token after `->` is an outcome keyword (`transition`, `no`, `reject`). For event handlers and state actions, the loop breaks at newline or `EndOfSource` — there is no outcome.

---

## Sync-Point Recovery

When the dispatch loop encounters an unrecognized token, it emits an `UnexpectedKeyword` diagnostic and scans forward for a sync token — a keyword that unambiguously starts a new declaration. The sync token set is derived from `Constructs.LeadingTokens` — a `FrozenSet<TokenKind>` built from catalog metadata at startup.

The sync set contains exactly 9 tokens:

```
precept  field  state  event  rule  from  to  in  on
```

These are the `LeadingToken` values from all `DisambiguationEntry` records across all constructs. `EndOfSource` also terminates the scan. Continuation tokens (`when`, `->`, `set`, `transition`, `ensure`, `because`) are never sync points — they appear mid-production and would cause the parser to skip valid content.

Within `in`-scoped parse failures, `modify` and `omit` serve as in-scope recovery anchors — they signal the start of a new access mode or omit declaration disambiguation after a state target. However, they are NOT in `Constructs.LeadingTokens` (they are post-anchor disambiguation tokens, not construct-initiating leading tokens). If parsing fails inside an `in`-scoped construct, the error sync advances until it finds the next top-level leading token (including `in` itself), at which point the outer dispatch loop re-enters disambiguation cleanly.

Recovery preserves all tokens between the error point and the sync point as a `SkippedTokens` span in diagnostics, so the language server can report the full extent of the unrecognized region.

---

## Validation Layer

The parser enforces correctness through four tiers, ordered from earliest detection to latest:

### Tier 1 — Build Time (CS8509): `InvokeSlotParser()` Switch

The `InvokeSlotParser()` method uses an exhaustive switch on `ConstructSlotKind`. Adding a new `ConstructSlotKind` member without a corresponding arm fails the C# build with CS8509. This guarantees that every slot kind recognized by the catalog has a parser implementation.

### Tier 2 — Build Time (CS8509): `BuildNode()` Switch

The `BuildNode()` method uses an exhaustive switch on `ConstructKind`. Adding a new `ConstructKind` without a corresponding arm fails the build. This guarantees that every construct recognized by the catalog can produce a typed AST node.

### Tier 3 — Test Time: Catalog Invariant Tests

A suite of tests validates structural invariants that cannot be expressed by CS8509:

| Test | What it validates |
|------|-------------------|
| `AllConstructsHaveAtLeastOneEntry` | Every `ConstructMeta` in `Constructs.All` has a non-empty `Entries` array |
| `DisambiguatedConstructs_HaveCorrectEntryCount` | Constructs with shared leading tokens have the expected number of entries |
| `EveryConstructSlotKindIsUsedByAtLeastOneConstruct` | No dead slot kinds in the enum |
| Slot-ordering drift tests | Anchor slots are at index 0, guard slots are at expected positions, `OmitDeclaration` never has a guard slot |

### Tier 4 — Design Time: Catalog-First Workflow

The catalog is the single source of truth. New constructs are added to `Constructs.cs` first — never to switch arms first. The workflow:

1. Add the `ConstructKind` enum member
2. Add the `GetMeta()` arm with entries and slots
3. Build fails (Tier 1 and Tier 2 catch the missing switch arms)
4. Add the `InvokeSlotParser()` arm and `BuildNode()` arm
5. Build succeeds; Tier 3 tests validate the structural invariants

This ordering ensures the catalog is always ahead of the parser implementation — never behind it.

---

## Dependencies and Integration Points

### Constructs Catalog

The Constructs catalog (`Constructs.All`) is the exhaustive registry of every declaration shape the parser can produce. Each `ConstructMeta` entry carries:

| Field | Parser use |
|-------|-----------|
| `Kind` | `ConstructKind` enum value — the identity of the production |
| `Entries` | `ImmutableArray<DisambiguationEntry>` — the leading token and optional disambiguation tokens per form |
| `Slots` | Ordered sequence of `ConstructSlot` values — the structural skeleton of the declaration |
| `AllowedIn` | Semantic scoping — which parent construct kinds this construct is valid after. The parser does not enforce this; the type checker does |
| `Name`, `Description`, `UsageExample` | Used in diagnostic messages and MCP vocabulary |

Each `DisambiguationEntry` carries:

| Field | Purpose |
|-------|---------|
| `LeadingToken` | The `TokenKind` that triggers this form in the dispatch loop |
| `DisambiguationTokens` | For shared leading tokens, the tokens that distinguish this construct from siblings. Null for unique leading tokens |
| `LeadingTokenSlot` | When the leading token is also slot content, identifies which slot receives the consumed token value. No current consumer |

The parser's relationship with the Constructs catalog is read-only. It reads `Entries` to build the `ByLeadingToken` dispatch index. It reads `Slots` to validate that each production method covers the correct sequence of slot kinds. It reads `Name` and `UsageExample` to produce diagnostic messages that match the domain language.

The two derived indexes:

- **`ByLeadingToken`** — `FrozenDictionary<TokenKind, ImmutableArray<(ConstructKind, DisambiguationEntry)>>` — groups all entries by their leading token, enabling O(1) dispatch lookup.
- **`LeadingTokens`** — `FrozenSet<TokenKind>` — the flat set of all leading tokens, used as the sync-point recovery set.

### Tokens Catalog

The parser reads token metadata indirectly through `TokenKind` values. It does not query `Tokens.All` at runtime — the lexer has already classified every token. The parser's contract with the Tokens catalog is:

- **Keyword token kinds are stable identifiers.** The parser switches on `TokenKind.Field`, `TokenKind.From`, `TokenKind.Set`, etc. These values come from the `TokenKind` enum, which is defined by the Tokens catalog. Adding a new keyword to the catalog adds a new `TokenKind` value; the parser must handle it or the exhaustive `ConstructKind` switch catches the gap. The parser never compares `Token.Text` against string literals to identify keywords — `TokenKind` is the sole classification mechanism.
- **Trivia token kinds control the skip loop.** `TokenKind.NewLine` and `TokenKind.Comment` are the two trivia kinds. `SkipTrivia()` consumes them between declaration-level dispatches. Within a single declaration, newlines are consumed as part of multi-line continuations (transition rows with `->` chains span multiple lines). The parser treats `NewLine` as a soft statement boundary — it terminates expressions but not arrow chains.
- **Operator vocabulary vs. binding powers.** `Operators.All` is the catalog source of truth for which tokens are operators — the parser's expression dispatch derives operator recognition sets from it. Binding powers (precedence numbers, associativity direction) are parser-internal mechanics: the catalog says "`+` is an arithmetic operator"; the parser says "`+` has left-binding power 50." Binding powers change when the expression grammar changes, not when the operator inventory changes. The precedence table is parser-internal; the operator set is catalog-derived.
- **`set` / `min` / `max` dual-use is documented in both catalogs.** The Tokens catalog documents the lexer's strategy (always emit one kind); the Constructs catalog documents where each interpretation appears in declaration shapes. The parser bridges the two — it is the only stage that knows both the token kind and the syntactic position.

### Diagnostics Catalog

The parser emits diagnostics via `Diagnostics.Create(DiagnosticCode, SourceSpan, params object[])`. The Diagnostics catalog owns the message templates — the parser passes codes, not strings. All parse-stage codes:

| Code | Condition |
|------|-----------|
| `ExpectedToken` | A required token was not found at the current position |
| `UnexpectedKeyword` | A keyword appeared in a position where it cannot start a production |
| `NonAssociativeComparison` | Chained comparison expression (`A == B == C`) |
| `InvalidCallTarget` | Parenthesized call on a non-callable expression |
| `OmitDoesNotSupportGuard` | A `when` guard appeared on an `omit` declaration (post-field or pre-stashed) |
| `PreEventGuardNotAllowed` | A `when` guard appeared before the event target in a `from`-scoped transition row |

Diagnostic messages are written for the **domain author** — the same audience as the lexer's diagnostics. "Expected a field name here, but found 'transition'" rather than "Expected Identifier, got TokenKind.Transition." The Diagnostics catalog holds the templates; the parser passes the contextual values (expected token description, found token text, construct name).

All codes are in the `DiagnosticCode` enum under the `// ── Parse ──` section, adjacent to the lexer codes. The diagnostic catalog owns severity (all are `Error`) and message templates — the parser never constructs message strings directly.

### Downstream Consumer Impact

#### Language Server

The language server calls `Parser.Parse` on every keystroke (debounced). It uses `SyntaxTree.Declarations` for:

- **Hover** — locates the node whose `SourceSpan` contains the cursor, returns the `ConstructKind` name and slot descriptions from the catalog.
- **Go-to-definition** — locates state/event/field reference nodes, resolves to the declaration node with the matching name.
- **Diagnostics** — forwards `SyntaxTree.Diagnostics` as LSP diagnostic objects, mapping `SourceSpan` line/column to LSP positions.
- **Document symbols** — walks the declaration list, emits LSP `DocumentSymbol` entries keyed on `ConstructKind`.

The resilient-by-construction guarantee means the language server never needs to handle "no tree" — every parse produces a traversable `SyntaxTree`.

#### Grammar Generation

The TextMate grammar is generated from catalog metadata, not from the parser. However, the parser's dispatch table and the grammar's pattern table must agree on which keywords start which productions. The `DisambiguationEntry.LeadingToken` field is the shared source of truth — both the parser's dispatch and the grammar generator read from it.

#### Completions

Context-aware completions use the parser's position to determine which `ConstructSlotKind` the cursor is in. The language server calls `Parser.Parse` on the partial text, finds the `IsMissing` node nearest the cursor, reads its slot kind, and offers completions from the catalog metadata for that slot. For example, an `IsMissing` node in the `Outcome` slot position triggers `transition`, `no transition`, and `reject` as completion candidates.

#### MCP Compile Tool

`precept_compile(text)` calls `Parser.Parse` and serializes the `SyntaxTree` as part of its response. The flat declaration list serializes naturally as a JSON array. Each node's `ConstructKind` becomes the `"kind"` field. `IsMissing` nodes are included with an `"isMissing": true` flag so MCP consumers can distinguish real declarations from error-recovery placeholders.

---

## Failure Modes and Recovery

### Missing-Node Insertion

`Expect(TokenKind)` is the primary recovery primitive. When the current token does not match, the parser:

1. Emits `ExpectedToken` diagnostic with the expected description and the found token.
2. Returns a synthetic `Token` with `IsMissing = true` and `SourceSpan.Missing`.
3. Does **not** advance — the unexpected token remains current for the calling production to handle.

The calling production constructs a complete node using the synthetic token. Downstream stages see a structurally coherent tree; they can inspect `IsMissing` to skip validation on phantom nodes.

### Sync-Point Resync

When the top-level dispatch loop encounters a token that cannot start any declaration, it scans forward to the next sync token from `Constructs.LeadingTokens`:

| Sync token | Keyword |
|------------|---------|
| `Precept` | `precept` |
| `Field` | `field` |
| `State` | `state` |
| `Event` | `event` |
| `Rule` | `rule` |
| `From` | `from` |
| `To` | `to` |
| `In` | `in` |
| `On` | `on` |
| `EndOfSource` | — |

Continuation tokens (`when`, `->`, `set`, `transition`, `ensure`, `because`) are never sync points — they appear mid-production and would cause the parser to skip valid content.

### Error Conditions

| Condition | Diagnostic code | Recovery action |
|-----------|-----------------|-----------------|
| Expected token not found | `ExpectedToken` | Insert `IsMissing` node, do not advance |
| Unrecognized token at declaration position | `UnexpectedKeyword` | Scan to next sync point, skip intervening tokens |
| Chained comparison (`A == B == C`) | `NonAssociativeComparison` | Emit diagnostic, return left operand as the expression |
| Non-callable expression followed by `(` | `InvalidCallTarget` | Emit diagnostic, parse arguments but mark node as missing |
| Guard on omit declaration | `OmitDoesNotSupportGuard` | Emit diagnostic, discard guard, parse OmitDeclarationNode without it |
| Pre-event guard on transition row | `PreEventGuardNotAllowed` | Emit diagnostic, inject guard at post-event GuardClause slot |

---

## Contracts and Guarantees

- **Always produces a tree.** `Parser.Parse` never returns null, never throws, and always produces a `SyntaxTree`. Downstream stages do not need to handle "no tree" as a result.
- **Every character is accounted for.** `IsMissing` nodes have zero-length spans at the expected position; `SkippedTokens` spans capture everything between an error and the next sync point. No source character is silently discarded.
- **`SourceSpan` coverage.** Every node's `Span` covers its full source extent. Declaration-level spans cover from the leading token to the last token in the declaration.
- **Flat list in source order.** `Declarations` is in the order the declarations appear in the source. No reordering.

### Bounded Work Guarantee

The parser consumes at least one token per loop iteration. Every `Advance()` call increments `_position`; every `Expect()` call on a mismatch does not advance but the calling production still progresses via its own `Advance()` or `Match()` calls. The sync-point scanner always advances at least one token. Combined with the lexer's 64KB source size limit (which bounds token count), this guarantees that `Parse()` terminates in O(n) time where n is the token count. No unbounded recursion is possible — expression nesting depth is bounded by token count, and the Pratt parser's `minBp` parameter ensures each token is visited at most twice (once as null-denotation, once as left-denotation).

---

## Design Rationale and Decisions

### The Parser Is Catalog-Driven Dispatch

The parser dispatches on `ConstructKind` values, not on ad-hoc token sequences. Each declaration production corresponds to exactly one `ConstructKind`, and the Constructs catalog (`Constructs.All`) is the exhaustive inventory of every declaration shape the parser can produce. When a new construct is added to the catalog, the parser gains a new production — the dispatch table, the slot sequence, and the MCP vocabulary all derive from the same metadata.

The Constructs catalog carries `Entries`, `Slots`, `AllowedIn`, and `UsageExample` per construct. These are the parser's dispatch key, shape skeleton, semantic scoping rule, and diagnostic example text — read from metadata, not duplicated in parser code.

### Dispatch Table: Grammar Structure vs. Vocabulary

The parser's hand-written dispatch table is not a catalog violation. The catalog-system design doc draws the line explicitly for the Parser row: *"Grammar productions stay hand-written."* The metadata-driven principle applies to **vocabulary**, not grammar structure.

**What the catalog-driven principle governs.** Vocabulary is the enumerable set of keywords, operators, types, and modifiers that constitute the language surface — domain knowledge that is independently useful to grammar generators, completions, hover text, and MCP consumers. Operator recognition sets, type keyword sets, modifier keyword sets, action keyword sets: these are domain knowledge and must derive from catalog frozen dictionaries (`Operators.All`, `Types.All`, `Modifiers.All`, `Actions.All`). Hand-coding these lists inside parse methods is a violation.

**Why grammar structure is different.** Grammar structure is the shape of productions and the logic that selects between them — control flow, not domain knowledge. It changes when the grammar changes, not when the language surface changes. Putting a `ParseHandler` delegate or production-selection logic in `ConstructCatalog` would:

- Not eliminate disambiguation — the 1:N `LeadingToken` cases require lookahead that cannot live in metadata
- Couple the catalog to the parser's internal method signatures
- Move code without removing complexity
- Violate the catalog's role: declarative metadata, not imperative behavior

**The 1:N problem.** Four leading tokens map to multiple `ConstructKind`s:

| Leading token | ConstructKinds | Disambiguation |
|---------------|----------------|----------------|
| `In` | `StateEnsure`, `AccessMode`, `OmitDeclaration` | verb after state target (`ensure` vs. `modify` vs. `omit`) |
| `To` | `StateEnsure`, `StateAction` | verb after state target (`ensure` vs. `->`) |
| `From` | `TransitionRow`, `StateEnsure`, `StateAction` | verb after state target (`on` vs. `ensure` vs. `->`) |
| `On` | `EventEnsure`, `EventHandler` | verb after event target (`ensure` vs. `->`) |

A catalog lookup on `LeadingToken` cannot select a production. The parser must read past the state/event target to the following verb. That disambiguation is grammar structure; metadata cannot hold it.

**The vocabulary gap to watch.** Vocabulary tables inside parse methods derive from catalog frozen dictionaries — not hand-coded:

| Vocabulary | Derives from |
|-----------|-----------------|
| Operator recognition and precedence | `Operators.All` |
| Type keyword sets | `Types.All` |
| Modifier recognition sets | `Modifiers.All` |
| Action keyword sets | `Actions.All` |

### Preposition-First Grammar

The four preposition keywords (`in`, `to`, `from`, `on`) are the parser's primary structural signal for scoped declarations. After the precept header and the declaration keywords (`field`, `state`, `event`, `rule`), every remaining production begins with a preposition. The parser reads the preposition, parses the state or event target, then looks ahead one token to select the specific production: ensure, access mode, omit, action, transition, or event handler.

A flat, line-oriented grammar with no block delimiters needs an unambiguous structural signal at the start of each line. Prepositions are the natural English equivalent of a block opener — they scope the line to a state or event context without requiring braces or indentation.

### Single-Pass Recursive Descent

The parser makes a single forward pass through the token stream. It never backtracks. Each production consumes the tokens it needs, emits diagnostics for tokens it expected but didn't find, and returns a node. The top-level dispatch loop peeks at the current token, selects a production, and calls it. The production consumes its tokens and returns.

Precept's grammar is LL(1) with a small number of two-token lookaheads (preposition + verb). No production requires unbounded lookahead. A single-pass design bounds memory to the depth of the expression tree and guarantees linear-time parsing. The token stream is immutable and indexable — `Peek(n)` is O(1), so bounded lookahead adds no allocation cost.

### Pratt Expression Parsing

Expressions use a Pratt parser (top-down operator precedence) rather than recursive descent with one function per precedence level. `ParseExpression(int minBp)` parses a complete expression, stopping when it encounters a token whose left-binding power is ≤ `minBp`. This handles unary prefix operators, binary infix operators, member access, function calls, and the `if`/`then`/`else` conditional — all in a single loop with a precedence table.

Precept has 10 precedence levels, prefix operators (`not`, unary `-`), postfix operators (`.`, `(`), and non-associative comparisons. A Pratt parser expresses all of this in ~80 lines of dispatch logic plus a binding-power table. A recursive-descent expression parser would need 10+ mutually recursive methods with identical structure.

### Resilient by Construction

The parser never throws, never returns null, and never produces a partial tree. Every production has a well-defined recovery path: missing tokens produce `IsMissing` nodes with zero-length spans; structurally lost positions scan forward to sync points (declaration-starting keywords). The resulting tree is always traversable by downstream stages.

The language server calls `Parser.Parse` on every keystroke. A parser that fails on incomplete input forces the language server to special-case "no tree" scenarios throughout completions, hover, go-to-definition, and diagnostics. A resilient parser eliminates that entire class of defensive code. The MCP tools (`precept_compile`, `precept_inspect`) also consume the tree directly — they need a structurally coherent tree even when the author is mid-edit.

### Flat Declaration List

The AST is a flat list of declaration nodes. There are no nested block nodes, no scope-introducing braces, and no parent-child relationships in the tree structure. Semantic scoping (`AllowedIn` — e.g., a state ensure is only valid after a state declaration) is a type-checker concern, not a parser concern. The parser produces the flat list; the type checker validates semantic ordering.

Precept is a line-oriented policy language. The author writes declarations top-to-bottom; the runtime reads them the same way. A flat list matches the mental model: each line is a self-contained declaration with its own preposition scope. Nested AST blocks would impose a hierarchical structure that the language surface does not express.

### What Stays Hand-Written

Not everything should be catalog-driven. The following remain hand-written because they are parser-internal mechanics, not domain knowledge:

- **Pratt expression parser** — binding power table, null-denotation dispatch, left-denotation dispatch. Precedence is parsing mechanics, not language surface vocabulary.
- **Preposition disambiguation logic** — the one-token lookahead after parsing a state/event target. This is control flow, not metadata.
- **Action chain loop** — the `->` consumption loop that breaks on outcome keywords. This is iteration structure.
- **Interpolation reassembly loop** — the Start/Middle/End token consumption pattern. This is structural scanning.
- **Error recovery decisions** — which tokens are sync points and how `Expect()` synthesizes missing nodes. Recovery strategy is implementation, not vocabulary.

These change when the parsing algorithm changes, not when the language surface changes. The catalog drives *what* the parser recognizes; the hand-written code drives *how* it recognizes it.

---

## Innovation

- **Preposition-first structural grammar.** Four preposition keywords (`in`, `to`, `from`, `on`) carry all semantic scope in the grammar without block delimiters. The parser dispatches on these with a one-token lookahead after the target — no brace matching, no indentation tracking, no ambiguous continuation.
- **Catalog-derived dispatch with derived indexes.** `Constructs.ByLeadingToken` and `Constructs.LeadingTokens` are built from `DisambiguationEntry` metadata — the same catalog that drives grammar generation, MCP vocabulary, and completions. The parser does not maintain a parallel keyword list.
- **Pratt expression parsing for a constraint DSL.** The bounded expression grammar (10 precedence levels, non-associative comparisons, `if`/`then`/`else`) is handled by a single ~80-line Pratt loop with a binding-power table. No mutually recursive per-precedence methods.
- **Depth-unaware interpolation reassembly.** Interpolated string and typed-constant segments are reassembled without tracking nesting depth — the token stream terminates expression holes naturally because `StringMiddle`/`StringEnd` have no binding power in the expression grammar.
- **4-tier validation pyramid.** CS8509 at build time catches missing switch arms; test-time invariant tests catch structural drift; design-time workflow ensures catalog-first evolution. No runtime surprises.

---

## Deliberate Exclusions

- **No CST (Concrete Syntax Tree).** Comments and whitespace are discarded. A CST is needed only by a formatter; Precept has no formatter.
- **No incremental reparsing.** Precept's 64KB ceiling makes full reparse on every edit fast enough. No red-green tree infrastructure.
- **No semantic validation.** `AllowedIn` enforcement, name resolution, and type checking belong to the TypeChecker. The parser validates structural form only.
- **No two-pass parsing.** The grammar is LL(1) with bounded lookahead — no first/follow set computation, no earley or GLR machinery.

---

## Open Questions / Implementation Notes

- The v8 implementation plan lives in `docs/working/catalog-parser-design-v8.md`. The PR sequence (5 PRs, 23 slices) is defined there.
- `AllowedIn` enforcement: currently a TypeChecker concern. If a future language feature needs parser-level scope enforcement (e.g., block-scoped declarations), revisit.
- Proposal C (`when` as `StateAction` disambiguation token) is deferred — not incorporated in this design. See v8 §8 for the "how to add it later" path.

---

## Cross-References

- `docs/compiler/lexer.md` — produces the `TokenStream` this stage consumes
- `docs/compiler/type-checker.md` — consumes `SyntaxTree`; owns `AllowedIn` enforcement and semantic validation
- `docs/language/catalog-system.md` — Constructs catalog design, metadata-driven architecture
- `docs/working/catalog-parser-design-v8.md` — implementation plan (PR sequence, test specs per slice)

---

## Source Files

- `src/Precept/Pipeline/Parser.cs` — static class + `ParseSession` struct
- `src/Precept/Pipeline/SyntaxNodes/` — AST node records (directory of per-node files)
- `src/Precept/Pipeline/SourceSpan.cs` — dual-coordinate location record
- `src/Precept/Language/Constructs.cs` — construct catalog (dispatch metadata)
- `src/Precept/Language/Construct.cs` — `ConstructMeta` record definition
- `src/Precept/Language/ConstructSlot.cs` — `ConstructSlot` / `ConstructSlotKind`
- `src/Precept/Language/DisambiguationEntry.cs` — `DisambiguationEntry` record (created in PR 1)
- `test/Precept.Tests/PreceptParserTests.cs` — parser unit tests
