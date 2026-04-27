# Parser

---

## Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Implemented |
| Source | `src/Precept/Pipeline/Parser.cs`, `src/Precept/Pipeline/SyntaxNodes.cs` |
| Upstream | Lexer (`TokenStream`) |
| Downstream | TypeChecker, LS syntax features, MCP `precept_compile` |

---

## Overview

The parser is the second stage of the Precept compilation pipeline. It transforms the flat `TokenStream` produced by the lexer into a `SyntaxTree` — an abstract syntax tree representing the semantic structure of the precept definition. The parser is a hand-written recursive descent parser with a Pratt expression parser for operator precedence. It produces an AST (not a CST) — comments and whitespace are consumed silently. No information is discarded that downstream stages need; no information is preserved that only a formatter would need.

```csharp
public static SyntaxTree Parse(TokenStream tokens)
```

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

- **Declarations** — precept header, field, state, event, rule, transition row, state ensure, event ensure, state action, access mode, event handler
- **Expressions** — arithmetic, comparison, logical, member access, function calls, conditionals, literals, interpolated strings and typed constants
- **Error recovery** — missing-node insertion for expected tokens, sync-point resync for structurally lost positions

The `SyntaxTree` is the `Compilation.SyntaxTree` field — it is part of the tooling surface and queryable by the language server for span-based operations (hover, go-to-definition, completions).

---

## Responsibilities and Boundaries

**OWNS:** Source-structural representation of authored programs; error recovery shape (`IsMissing` nodes, `SkippedTokens`); `SourceSpan` ownership for every node; disambiguation of dual-use tokens (`set`, `min`, `max`) by syntactic position.

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

### Top-Level Dispatch Loop

`ParseAll()` consumes the `precept` header, then enters a loop that skips trivia (newlines, comments) and dispatches on the current token to select a declaration production:

| Current token | Production | `ConstructKind` |
|---------------|-----------|-----------------|
| `Field` | `ParseFieldDeclaration()` | `FieldDeclaration` |
| `State` | `ParseStateDeclaration()` | `StateDeclaration` |
| `Event` | `ParseEventDeclaration()` | `EventDeclaration` |
| `Rule` | `ParseRuleDeclaration()` | `RuleDeclaration` |
| `Write`, `Read`, `Omit` | `ParseAccessMode()` | `AccessMode` |
| `In` | `ParseInScoped()` | *(disambiguates below)* |
| `To` | `ParseToScoped()` | *(disambiguates below)* |
| `From` | `ParseFromScoped()` | *(disambiguates below)* |
| `On` | `ParseOnScoped()` | *(disambiguates below)* |
| `EndOfSource` | exit loop | — |
| *anything else* | `EmitDiagnostic` + `SyncToNextDeclaration()` | — |

The dispatch table is a direct map from `ConstructMeta.LeadingToken` to parse methods. Each construct in the catalog declares its leading token; the parser uses this to route without maintaining a parallel keyword list.

### Preposition Disambiguation

The four preposition-scoped methods parse a state or event target, then look ahead to select the specific production:

**`ParseInScoped()`** — Leading token `in`:

| Lookahead after state target | Production | `ConstructKind` |
|------------------------------|-----------|-----------------|
| `Ensure` | `ParseStateEnsure(In)` | `StateEnsure` |
| `Write`, `Read`, `Omit` | `ParseAccessMode(stateTarget)` | `AccessMode` |
| `When` *(then re-check)* | consume guard, re-dispatch | *(same as above)* |

**`ParseToScoped()`** — Leading token `to`:

| Lookahead after state target | Production | `ConstructKind` |
|------------------------------|-----------|-----------------|
| `Ensure` | `ParseStateEnsure(To)` | `StateEnsure` |
| `Arrow` | `ParseStateAction(To)` | `StateAction` |
| `When` *(then re-check)* | consume guard, re-dispatch | *(same as above)* |

**`ParseFromScoped()`** — Leading token `from`:

| Lookahead after state target | Production | `ConstructKind` |
|------------------------------|-----------|-----------------|
| `On` | `ParseTransitionRow()` | `TransitionRow` |
| `Ensure` | `ParseStateEnsure(From)` | `StateEnsure` |
| `Arrow` | `ParseStateAction(From)` | `StateAction` |
| `When` *(then re-check)* | consume guard, re-dispatch | *(same as above)* |

**`ParseOnScoped()`** — Leading token `on`:

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

### Sync-Point Recovery

When the dispatch loop encounters an unrecognized token, it emits an `UnexpectedKeyword` diagnostic and scans forward for a sync token — a keyword that unambiguously starts a new declaration:

```
precept  field  state  event  rule  from  to  in  on  write  read  omit
```

These are exactly the `LeadingToken` values from the Constructs catalog plus `EndOfSource`. Continuation tokens (`when`, `->`, `set`, `transition`, `ensure`, `because`) are never sync points — they appear mid-production and would cause the parser to skip valid content. The sync scanner also stops at `EndOfSource`.

Recovery preserves all tokens between the error point and the sync point as a `SkippedTokens` span in diagnostics, so the language server can report the full extent of the unrecognized region.

### `set` Disambiguation

The lexer always emits `TokenKind.Set`. The parser disambiguates by position:

| Context | Interpretation | How detected |
|---------|---------------|-------------- |
| After `as` or `of` in `ParseTypeRef()` | Collection type (`set of T`) | Next token is `Of` |
| After `->` in action chain | Assignment action (`set X = V`) | Inside `ParseActionChain()` |
| After `is` / `is not` in expression | Presence test (`X is set`) | Inside Pratt left-denotation for `Is` |

The parser never synthesizes a `TokenKind.SetType` token. It treats `Set` in a type position as a collection type constructor and produces a `CollectionTypeNode` with the set kind. The AST encodes the semantic meaning; the token kind stays as-is.

### `min` / `max` Disambiguation

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

### Expression Parsing Detail

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

#### Null-Denotation (Atoms and Prefix)

The null-denotation is the entry point for expressions. It handles atoms (identifiers, literals, parenthesized expressions) and prefix operators (`not`, unary `-`). When the current token has no null-denotation entry, the parser emits `ExpectedToken("expression")` and returns a missing `IdentifierExpression`.

#### Left-Denotation (Infix and Postfix)

The left-denotation handles infix operators, member access (`.`), function calls (`(`), and the `is`/`is not`/`contains` keyword operators. The `is` handler is multi-token: it consumes an optional `Not`, then expects `Set`. The `contains` handler parses the right operand at the same binding power.

#### Conditional Expressions

`if`/`then`/`else` is parsed as a null-denotation: consume `if`, parse condition at BP 0, expect `then`, parse consequent at BP 0, expect `else`, parse alternative at BP 0. The `else` branch is required — there is no short-form `if`/`then` without `else`.

An example from the insurance-claim sample:

```precept
-> set ApprovedAmount = if FraudFlag then min(Approve.Amount, ClaimAmount / 2) else Approve.Amount
```

The Pratt parser sees `if` as a null-denotation entry, parses `FraudFlag` as the condition (stops at `then`), parses `min(Approve.Amount, ClaimAmount / 2)` as the consequent (stops at `else`), and parses `Approve.Amount` as the alternative (stops at the next newline or `->`, which has no binding power).

#### Interpolation Reassembly

The parser reassembles interpolated literals from the segmented token stream the lexer produced. Both `ParseInterpolatedString()` and `ParseInterpolatedTypedConstant()` use the same loop:

1. Consume `Start` token → `TextSegment`
2. `ParseExpression(0)` → `ExpressionSegment`
3. If `Middle` → `TextSegment`, go to step 2
4. If `End` → `TextSegment`, done

`ParseExpression(0)` terminates naturally at `StringMiddle`/`StringEnd`/`TypedConstantMiddle`/`TypedConstantEnd` because these token kinds have no binding power in the expression parser. This is the depth-unaware reassembly property: because `}` always ends an interpolation hole and has no meaning in the expression grammar, the parser stops naturally without tracking nesting depth.

#### Action Chain Parsing

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

## Component Mechanics

### SourceSpan

Every AST node carries a `SourceSpan` — the dual-coordinate location record defined in [src/Precept/Pipeline/SourceSpan.cs](../../src/Precept/Pipeline/SourceSpan.cs). The implementation is documented in [§ SourceSpan Contract](#sourcespan-contract) above. No changes needed — the parser reads `Token.Span` directly for leaf nodes and creates compound spans via `SourceSpan.Covering(first, last)` for declaration-level nodes. The parser never reconstructs a `SourceSpan` from separate token fields — the lexer provides it.

### SyntaxNode Base and Intermediate Types

```csharp
public abstract record SyntaxNode(SourceSpan Span, bool IsMissing = false);
```

All AST nodes inherit from `SyntaxNode`. `Span` covers the full source extent of the node. `IsMissing` is `true` when the parser synthesized the node to fill an expected-but-absent position — downstream stages can inspect this flag to suppress cascading diagnostics on phantom nodes. Records are immutable by default; no `with` copies are expected after construction.

Three abstract intermediates partition the node space for downstream pattern matching:

```csharp
public abstract record Declaration(SourceSpan Span, ConstructKind Kind, bool IsMissing = false)
    : SyntaxNode(Span, IsMissing);

public abstract record Statement(SourceSpan Span, bool IsMissing = false)
    : SyntaxNode(Span, IsMissing);

public abstract record Expression(SourceSpan Span, bool IsMissing = false)
    : SyntaxNode(Span, IsMissing);
```

`Declaration` carries a `ConstructKind` so consumers can switch on the catalog identity without downcasting. `Statement` covers action statements inside arrow chains. `Expression` covers the Pratt-parsed expression tree. The type checker, graph analyzer, and language server all switch on these three families.

### SyntaxTree

```csharp
public sealed record SyntaxTree(
    PreceptHeaderNode? Header,
    ImmutableArray<Declaration> Declarations,
    ImmutableArray<Diagnostic> Diagnostics);
```

`Header` is nullable for the case where the source is empty or the `precept` keyword is missing — the parser emits an `ExpectedToken` diagnostic and produces a tree with `Header = null`. `Declarations` is the flat list in source order. `Diagnostics` accumulates every parse-stage diagnostic. This is the `Compilation.SyntaxTree` field.

### Declaration Nodes

#### PreceptHeader

```
precept Identifier
```

```csharp
public sealed record PreceptHeaderNode(
    SourceSpan Span, Token Name, bool IsMissing = false)
    : Declaration(Span, ConstructKind.PreceptHeader, IsMissing);
```

```precept
precept InsuranceClaim
```

#### FieldDeclaration

```
field Identifier ("," Identifier)* as TypeRef FieldModifier* ("=" Expr)?
```

```csharp
public sealed record FieldDeclarationNode(
    SourceSpan Span,
    ImmutableArray<Token> Names,
    TypeRefNode Type,
    ImmutableArray<FieldModifierNode> Modifiers,
    Expression? ComputedExpression,
    bool IsMissing = false)
    : Declaration(Span, ConstructKind.FieldDeclaration, IsMissing);
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
    ImmutableArray<StateEntryNode> Entries,
    bool IsMissing = false)
    : Declaration(Span, ConstructKind.StateDeclaration, IsMissing);

public sealed record StateEntryNode(
    SourceSpan Span, Token Name,
    ImmutableArray<Token> Modifiers,
    bool IsMissing = false)
    : SyntaxNode(Span, IsMissing);
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
    bool IsInitial,
    bool IsMissing = false)
    : Declaration(Span, ConstructKind.EventDeclaration, IsMissing);

public sealed record ArgumentNode(
    SourceSpan Span, Token Name,
    TypeRefNode Type,
    ImmutableArray<FieldModifierNode> Modifiers,
    bool IsMissing = false)
    : SyntaxNode(Span, IsMissing);
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
    Expression Message,
    bool IsMissing = false)
    : Declaration(Span, ConstructKind.RuleDeclaration, IsMissing);
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
    OutcomeNode Outcome,
    bool IsMissing = false)
    : Declaration(Span, ConstructKind.TransitionRow, IsMissing);
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
    Expression Message,
    bool IsMissing = false)
    : Declaration(Span, ConstructKind.StateEnsure, IsMissing);
```

```precept
in Approved ensure ApprovedAmount > 0 because "Approved claims must specify a payout amount"
```

#### AccessMode

```
(in StateTarget ("when" BoolExpr)?)? AccessKeyword FieldTarget
AccessKeyword := write | read | omit
```

```csharp
public sealed record AccessModeNode(
    SourceSpan Span,
    StateTargetNode? State,
    Expression? Guard,
    Token Mode,
    FieldTargetNode Field,
    bool IsMissing = false)
    : Declaration(Span, ConstructKind.AccessMode, IsMissing);
```

```precept
in UnderReview write FraudFlag
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
    ImmutableArray<Statement> Actions,
    bool IsMissing = false)
    : Declaration(Span, ConstructKind.StateAction, IsMissing);
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
    Expression Message,
    bool IsMissing = false)
    : Declaration(Span, ConstructKind.EventEnsure, IsMissing);
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
    ImmutableArray<Statement> Actions,
    bool IsMissing = false)
    : Declaration(Span, ConstructKind.EventHandler, IsMissing);
```

```precept
on UpdateName -> set name = newName
```

### Action Statement Nodes

Action statements appear inside arrow chains (`-> action -> action -> ...`). Each action keyword maps to a sealed record:

```csharp
public sealed record SetStatement(
    SourceSpan Span, Token Field, Expression Value,
    bool IsMissing = false) : Statement(Span, IsMissing);

public sealed record AddStatement(
    SourceSpan Span, Token Field, Expression Value,
    bool IsMissing = false) : Statement(Span, IsMissing);

public sealed record RemoveStatement(
    SourceSpan Span, Token Field, Expression Value,
    bool IsMissing = false) : Statement(Span, IsMissing);

public sealed record EnqueueStatement(
    SourceSpan Span, Token Field, Expression Value,
    bool IsMissing = false) : Statement(Span, IsMissing);

public sealed record DequeueStatement(
    SourceSpan Span, Token Field, Token? IntoField,
    bool IsMissing = false) : Statement(Span, IsMissing);

public sealed record PushStatement(
    SourceSpan Span, Token Field, Expression Value,
    bool IsMissing = false) : Statement(Span, IsMissing);

public sealed record PopStatement(
    SourceSpan Span, Token Field, Token? IntoField,
    bool IsMissing = false) : Statement(Span, IsMissing);

public sealed record ClearStatement(
    SourceSpan Span, Token Field,
    bool IsMissing = false) : Statement(Span, IsMissing);
```

`set` takes `Field = Value`. `add`, `remove`, `enqueue`, and `push` take `Field Value`. `dequeue` and `pop` take `Field` with an optional `into Field` for destructuring. `clear` takes only `Field`. Examples from the insurance-claim sample:

```precept
-> set ClaimantName = Submit.Claimant
-> add MissingDocuments RequestDocument.Name
-> remove MissingDocuments ReceiveDocument.Name
```

### Outcome Nodes

Outcomes terminate a transition row's arrow chain. Three shapes:

```csharp
public abstract record OutcomeNode(SourceSpan Span, bool IsMissing = false)
    : SyntaxNode(Span, IsMissing);

public sealed record TransitionOutcomeNode(
    SourceSpan Span, Token TargetState,
    bool IsMissing = false) : OutcomeNode(Span, IsMissing);

public sealed record NoTransitionOutcomeNode(
    SourceSpan Span,
    bool IsMissing = false) : OutcomeNode(Span, IsMissing);

public sealed record RejectOutcomeNode(
    SourceSpan Span, Expression Message,
    bool IsMissing = false) : OutcomeNode(Span, IsMissing);
```

```precept
-> transition Approved
-> no transition
-> reject "Required documents must be complete before a claim can be approved"
```

The parser recognizes `transition` after `->` to produce `TransitionOutcomeNode`, `no transition` (two tokens) for `NoTransitionOutcomeNode`, and `reject` followed by a string expression for `RejectOutcomeNode`.

### Supporting Types

#### StateTarget

```csharp
public sealed record StateTargetNode(
    SourceSpan Span, Token Name, bool IsQuantifier,
    bool IsMissing = false) : SyntaxNode(Span, IsMissing);
```

`Name` is either a state identifier or the `any` quantifier keyword. `IsQuantifier` is `true` when the target is `any` rather than a specific state name.

#### FieldTarget

```csharp
public sealed record FieldTargetNode(
    SourceSpan Span, ImmutableArray<Token> Names, bool IsAll,
    bool IsMissing = false) : SyntaxNode(Span, IsMissing);
```

`IsAll` is `true` when the target is the `all` keyword. Otherwise `Names` contains one or more comma-separated field identifiers.

#### TypeRef Hierarchy

```csharp
public abstract record TypeRefNode(SourceSpan Span, bool IsMissing = false)
    : SyntaxNode(Span, IsMissing);

public sealed record ScalarTypeRefNode(
    SourceSpan Span, Token TypeName, TypeQualifierNode? Qualifier,
    bool IsMissing = false) : TypeRefNode(Span, IsMissing);

public sealed record CollectionTypeRefNode(
    SourceSpan Span, Token CollectionKind, Token ElementType,
    TypeQualifierNode? Qualifier,
    bool IsMissing = false) : TypeRefNode(Span, IsMissing);

public sealed record ChoiceTypeRefNode(
    SourceSpan Span, ImmutableArray<Expression> Options,
    bool IsMissing = false) : TypeRefNode(Span, IsMissing);
```

`ScalarTypeRefNode` covers `string`, `decimal`, `boolean`, `money`, etc. `CollectionTypeRefNode` covers `set of T`, `queue of T`, `stack of T` — `CollectionKind` is the `Set`/`Queue`/`Stack` token. `ChoiceTypeRefNode` covers `choice("A", "B", "C")`.

#### TypeQualifier

```csharp
public sealed record TypeQualifierNode(
    SourceSpan Span, Token Keyword, Expression Value,
    bool IsMissing = false) : SyntaxNode(Span, IsMissing);
```

`Keyword` is `In` or `Of` — narrowing the type domain (e.g., `money in 'USD'`, `quantity of 'weight'`).

#### FieldModifier Hierarchy

```csharp
public abstract record FieldModifierNode(SourceSpan Span, bool IsMissing = false)
    : SyntaxNode(Span, IsMissing);

public sealed record FlagModifierNode(
    SourceSpan Span, Token Keyword,
    bool IsMissing = false) : FieldModifierNode(Span, IsMissing);

public sealed record ValueModifierNode(
    SourceSpan Span, Token Keyword, Expression Value,
    bool IsMissing = false) : FieldModifierNode(Span, IsMissing);
```

Flag modifiers (`optional`, `nonnegative`, `positive`, `nonzero`, `notempty`, `ordered`) carry only the keyword token. Value modifiers (`default`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`) carry a keyword and an expression value. The DU prevents consumers from accessing a `Value` property on flag-only modifiers.

---

## Dependencies and Integration Points

### Constructs Catalog

The Constructs catalog (`Constructs.All`) is the exhaustive registry of every declaration shape the parser can produce. Each `ConstructMeta` entry carries:

| Field | Parser use |
|-------|-----------|
| `Kind` | `ConstructKind` enum value — the identity of the production |
| `LeadingToken` | The `TokenKind` that triggers this production in the dispatch loop |
| `Slots` | Ordered sequence of `ConstructSlot` values — the structural skeleton of the declaration. Each slot has a `ConstructSlotKind` (e.g., `IdentifierList`, `TypeExpression`, `GuardClause`) and an `IsRequired` flag |
| `AllowedIn` | Semantic scoping — which parent construct kinds this construct is valid after. Empty means top-level. The parser does not enforce this (flat list); the type checker does |
| `Name`, `Description`, `UsageExample` | Used in diagnostic messages and MCP vocabulary — the parser references these for error context |

The parser's relationship with the Constructs catalog is read-only. The parser does not modify catalog entries. It reads `LeadingToken` to build the dispatch table. It reads `Slots` to validate that each production method covers the correct sequence of slot kinds. It reads `Name` and `UsageExample` to produce diagnostic messages that match the domain language.

The slot sequence is the construct's structural skeleton. For example, `FieldDeclaration` has slots `[IdentifierList, TypeExpression, ModifierList, ComputeExpression]` — the parser's `ParseFieldDeclaration()` method parses exactly those four slot kinds in that order. `ModifierList` and `ComputeExpression` have `IsRequired = false`, so the parser checks for their presence before attempting to parse them. The slot sequence is not a parser generator input — it is a structural contract that the hand-written production method must satisfy.

The 11 `ConstructKind` values map to exactly 11 parse productions:

| `ConstructKind` | Parse method | Leading token |
|-----------------|-------------|---------------|
| `PreceptHeader` | `ParsePreceptHeader()` | `Precept` |
| `FieldDeclaration` | `ParseFieldDeclaration()` | `Field` |
| `StateDeclaration` | `ParseStateDeclaration()` | `State` |
| `EventDeclaration` | `ParseEventDeclaration()` | `Event` |
| `RuleDeclaration` | `ParseRuleDeclaration()` | `Rule` |
| `TransitionRow` | `ParseTransitionRow()` | `From` |
| `StateEnsure` | `ParseStateEnsure()` | `In`, `To`, or `From` |
| `AccessMode` | `ParseAccessMode()` | `In` or `Write`/`Read`/`Omit` |
| `StateAction` | `ParseStateAction()` | `To` or `From` |
| `EventEnsure` | `ParseEventEnsure()` | `On` |
| `EventHandler` | `ParseEventHandler()` | `On` |

Constructs with shared leading tokens (`In` → `StateEnsure` or `AccessMode`; `On` → `EventEnsure` or `EventHandler`) are resolved by the preposition disambiguation logic described above. The parser knows which production to select after parsing the target and one lookahead token — no ambiguity reaches the production method itself.

Note that `From` leads to three possible productions (`TransitionRow`, `StateEnsure`, `StateAction`), and `To` leads to two (`StateEnsure`, `StateAction`). The preposition disambiguation tables above capture the full resolution logic.

### Tokens Catalog

The parser reads token metadata indirectly through `TokenKind` values. It does not query `Tokens.All` at runtime — the lexer has already classified every token. The parser's contract with the Tokens catalog is:

- **Keyword token kinds are stable identifiers.** The parser switches on `TokenKind.Field`, `TokenKind.From`, `TokenKind.Set`, etc. These values come from the `TokenKind` enum, which is defined by the Tokens catalog. Adding a new keyword to the catalog adds a new `TokenKind` value; the parser must handle it or the exhaustive `ConstructKind` switch catches the gap. The parser never compares `Token.Text` against string literals to identify keywords — `TokenKind` is the sole classification mechanism.
- **Trivia token kinds control the skip loop.** `TokenKind.NewLine` and `TokenKind.Comment` are the two trivia kinds. `SkipTrivia()` consumes them between declaration-level dispatches. Within a single declaration, newlines are consumed as part of multi-line continuations (transition rows with `->` chains span multiple lines). The parser treats `NewLine` as a soft statement boundary — it terminates expressions but not arrow chains.

- **Keyword token kinds are stable identifiers.** The parser switches on `TokenKind.Field`, `TokenKind.From`, `TokenKind.Set`, etc. These values come from the `TokenKind` enum, which is defined by the Tokens catalog. Adding a new keyword to the catalog adds a new `TokenKind` value; the parser must handle it or the exhaustive `ConstructKind` switch catches the gap.
- **Operator binding powers are a parser concern.** The Tokens catalog classifies operators by category (`TokenCategory.Operator`), but binding powers (precedence, associativity) are parser-internal. The catalog says "`+` is an operator"; the parser says "`+` has left-binding power 50." This is intentional — operator precedence is parsing mechanics, not domain knowledge. It changes when the expression grammar changes, not when the keyword inventory changes. The precedence table lives in the parser, not in the catalog.
- **`set` / `min` / `max` dual-use is documented in both catalogs.** The Tokens catalog documents the lexer's strategy (always emit one kind); the Constructs catalog documents where each interpretation appears in declaration shapes. The parser bridges the two — it is the only stage that knows both the token kind and the syntactic position.

### Diagnostics Catalog

The parser emits diagnostics via `Diagnostics.Create(DiagnosticCode, SourceSpan, params object[])`. The Diagnostics catalog owns the message templates — the parser passes codes, not strings. All parse-stage codes:

| Code | Condition |
|------|-----------|
| `ExpectedToken` | A required token was not found at the current position |
| `UnexpectedKeyword` | A keyword appeared in a position where it cannot start a production |
| `NonAssociativeComparison` | Chained comparison expression (`A == B == C`) |
| `InvalidCallTarget` | Parenthesized call on a non-callable expression |

Diagnostic messages are written for the **domain author** — the same audience as the lexer's diagnostics. "Expected a field name here, but found 'transition'" rather than "Expected Identifier, got TokenKind.Transition." The Diagnostics catalog holds the templates; the parser passes the contextual values (expected token description, found token text, construct name).

The parser's diagnostic count is deliberately small — four codes for an entire grammar. This reflects the resilient-by-construction principle: most malformed input is handled by `Expect()` returning `IsMissing` nodes (counted under `ExpectedToken`), not by specialized error productions. Additional codes will be added only when the domain author needs a distinct message that `ExpectedToken` cannot convey.

All four codes are in the `DiagnosticCode` enum under the `// ── Parse ──` section, adjacent to the lexer codes. The diagnostic catalog owns severity (all four are `Error`) and message templates — the parser never constructs message strings directly.

### Downstream Consumer Impact

#### Language Server

The language server calls `Parser.Parse` on every keystroke (debounced). It uses `SyntaxTree.Declarations` for:

- **Hover** — locates the node whose `SourceSpan` contains the cursor, returns the `ConstructKind` name and slot descriptions from the catalog.
- **Go-to-definition** — locates state/event/field reference nodes, resolves to the declaration node with the matching name.
- **Diagnostics** — forwards `SyntaxTree.Diagnostics` as LSP diagnostic objects, mapping `SourceSpan` line/column to LSP positions.
- **Document symbols** — walks the declaration list, emits LSP `DocumentSymbol` entries keyed on `ConstructKind`.

The resilient-by-construction guarantee means the language server never needs to handle "no tree" — every parse produces a traversable `SyntaxTree`.

#### Grammar Generation

The TextMate grammar is generated from catalog metadata, not from the parser. However, the parser's dispatch table and the grammar's pattern table must agree on which keywords start which productions. The Constructs catalog's `LeadingToken` field is the shared source of truth — both the parser's dispatch and the grammar generator read from it.

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

When the top-level dispatch loop encounters a token that cannot start any declaration, it scans forward to the next sync token:

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
| `Write` | `write` |
| `Read` | `read` |
| `Omit` | `omit` |
| `EndOfSource` | — |

These are the `LeadingToken` values from the Constructs catalog. Continuation tokens (`when`, `->`, `set`, `transition`, `ensure`, `because`) are never sync points — they appear mid-production and would cause the parser to skip valid content.

### Error Conditions

| Condition | Diagnostic code | Recovery action |
|-----------|-----------------|-----------------|
| Expected token not found | `ExpectedToken` | Insert `IsMissing` node, do not advance |
| Unrecognized token at declaration position | `UnexpectedKeyword` | Scan to next sync point, skip intervening tokens |
| Chained comparison (`A == B == C`) | `NonAssociativeComparison` | Emit diagnostic, return left operand as the expression |
| Non-callable expression followed by `(` | `InvalidCallTarget` | Emit diagnostic, parse arguments but mark node as missing |

### Diagnostic Catalog Integration

All four parse-stage codes live in the `DiagnosticCode` enum under `// ── Parse ──`. The Diagnostics catalog owns severity (all `Error`) and message templates. The parser passes contextual values — expected token description, found token text, construct name — via `Diagnostics.Create(DiagnosticCode, SourceSpan, params object[])`. Messages target the domain author: "Expected a state name here, but found 'ensure'" not "Expected Identifier, got TokenKind.Ensure."

---

## Contracts and Guarantees

- **Always produces a tree.** `Parser.Parse` never returns null, never throws, and always produces a `SyntaxTree`. Downstream stages do not need to handle "no tree" as a result.
- **Every character is accounted for.** `IsMissing` nodes have zero-length spans at the expected position; `SkippedTokens` spans capture everything between an error and the next sync point. No source character is silently discarded.
- **`SourceSpan` coverage.** Every node's `Span` covers its full source extent. Declaration-level spans cover from the leading token to the last token in the declaration.
- **`ConstructKind` identity.** Every `Declaration` node carries a `ConstructKind` from the Constructs catalog — the parser never leaves this field unset.
- **Flat list in source order.** `Declarations` is in the order the declarations appear in the source. No reordering.

### Bounded Work Guarantee

The parser consumes at least one token per loop iteration. Every `Advance()` call increments `_position`; every `Expect()` call on a mismatch does not advance but the calling production still progresses via its own `Advance()` or `Match()` calls. The sync-point scanner always advances at least one token. Combined with the lexer's 64KB source size limit (which bounds token count), this guarantees that `Parse()` terminates in O(n) time where n is the token count. No unbounded recursion is possible — expression nesting depth is bounded by token count, and the Pratt parser's `minBp` parameter ensures each token is visited at most twice (once as null-denotation, once as left-denotation).

---

## Design Rationale and Decisions

### The Parser Is Catalog-Driven Dispatch

The parser dispatches on `ConstructKind` values, not on ad-hoc token sequences. Each declaration production corresponds to exactly one `ConstructKind`, and the Constructs catalog (`Constructs.All`) is the exhaustive inventory of every declaration shape the parser can produce. When a new construct is added to the catalog, the parser gains a new production — the dispatch table, the slot sequence, and the MCP vocabulary all derive from the same metadata.

The Constructs catalog carries `LeadingToken`, `Slots`, `AllowedIn`, and `UsageExample` per construct. These are the parser's dispatch key, shape skeleton, semantic scoping rule, and diagnostic example text — read from metadata, not duplicated in parser code.

Consider the `TransitionRow` construct. Its catalog entry declares `LeadingToken = TokenKind.From`, `Slots = [StateTarget, EventTarget, GuardClause, ActionChain, Outcome]`, and `AllowedIn = []` (top-level). The parser reads this shape: dispatch on `From`, parse a state target, parse an event target, optionally parse a guard, loop through an action chain, parse an outcome. If a new slot is added to the construct (say, a `BecauseClause`), the parser gains a new step in the production — but the catalog is the authority that says the step exists.

### Preposition-First Grammar

The four preposition keywords (`in`, `to`, `from`, `on`) are the parser's primary structural signal for scoped declarations. After the precept header and the declaration keywords (`field`, `state`, `event`, `rule`), every remaining production begins with a preposition. The parser reads the preposition, parses the state or event target, then looks ahead one token to select the specific production: ensure, access mode, action, transition, or event handler.

A flat, line-oriented grammar with no block delimiters needs an unambiguous structural signal at the start of each line. Prepositions are the natural English equivalent of a block opener — they scope the line to a state or event context without requiring braces or indentation.

The insurance-claim sample illustrates the pattern. Every line after the header declarations begins with `from`, `in`, or `on`:

```precept
in UnderReview write FraudFlag
in Approved ensure ApprovedAmount > 0 because "Approved claims must specify a payout amount"
on Submit ensure Amount > 0 because "Claim amounts must be positive"
from Draft on Submit -> set ClaimantName = Submit.Claimant -> transition Submitted
from Submitted on AssignAdjuster -> set AdjusterName = trim(AssignAdjuster.Name) -> transition UnderReview
```

Each line's preposition immediately establishes the scope. The parser needs zero lookahead to know which family of production it is entering — it just reads the preposition and proceeds.

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

The `AllowedIn` field on `ConstructMeta` captures the semantic scoping that a nesting parser would express structurally. For example, `StateEnsure` has `AllowedIn = [StateDeclaration]` — meaning it is only valid after a state declaration exists. But this is a type-checker validation, not a parsing constraint. The parser emits the flat node; the type checker checks `AllowedIn` against the declared state names.

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
- **Catalog-derived dispatch table.** The top-level dispatch loop is built from `ConstructMeta.LeadingToken` entries — the same catalog metadata that drives grammar generation, MCP vocabulary, and completions. The parser does not maintain a parallel keyword list.
- **Pratt expression parsing for a constraint DSL.** The bounded expression grammar (10 precedence levels, non-associative comparisons, `if`/`then`/`else`) is handled by a single ~80-line Pratt loop with a binding-power table. No mutually recursive per-precedence methods.
- **Depth-unaware interpolation reassembly.** Interpolated string and typed-constant segments are reassembled without tracking nesting depth — the token stream terminates expression holes naturally because `StringMiddle`/`StringEnd` have no binding power in the expression grammar.

---

## Open Questions / Implementation Notes

- The test coverage below documents current test coverage. No known structural gaps.
- `AllowedIn` enforcement: currently a TypeChecker concern. If a future language feature needs parser-level scope enforcement (e.g., block-scoped declarations), revisit.

---

## Deliberate Exclusions

- **No CST (Concrete Syntax Tree).** Comments and whitespace are discarded. A CST is needed only by a formatter; Precept has no formatter.
- **No incremental reparsing.** Precept's 64KB ceiling makes full reparse on every edit fast enough. No red-green tree infrastructure.
- **No semantic validation.** `AllowedIn` enforcement, name resolution, and type checking belong to the TypeChecker. The parser validates structural form only.
- **No two-pass parsing.** The grammar is LL(1) with bounded lookahead — no first/follow set computation, no earley or GLR machinery.

---

## Cross-References

- `docs/compiler/lexer.md` — produces the `TokenStream` this stage consumes
- `docs/compiler/type-checker.md` — consumes `SyntaxTree`; owns `AllowedIn` enforcement and semantic validation
- `docs/compiler-and-runtime-design.md §5` — Parser section in the main design doc
- `docs/language/catalog-system.md` — Constructs catalog design

---

## Source Files

- `src/Precept/Pipeline/Parser.cs` — static class + `ParseSession` struct
- `src/Precept/Pipeline/SyntaxNodes.cs` — all AST node records
- `src/Precept/Pipeline/SourceSpan.cs` — dual-coordinate location record
- `test/Precept.Tests/PreceptParserTests.cs` — parser unit tests
