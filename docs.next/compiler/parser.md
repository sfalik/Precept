# Parser

> **Status:** Draft
> **Decisions answered:** P1 (AST not CST), P2 (hybrid error recovery — missing nodes + sync-point resync), P3 (production-end detection for statement boundaries), P4 (parser/type-checker boundary at the grammar line), P5 (typed constant interior opaque), P6 (depth-unaware interpolation reassembly), P7 (AST node representation: abstract record base + SourceSpan, grouped intermediate types, IsMissing flag)
> **Survey references:** compiler-pipeline-architecture-survey
> **Grounding:** `docs.next/architecture-planning.md` § 2.2, `docs.next/compiler/pipeline-artifacts-and-consumer-contracts.md` § Pipeline Stages

## Overview

The parser is the second stage of the Precept compiler pipeline. It transforms the flat `TokenStream` produced by the lexer into a `SyntaxTree` — an abstract syntax tree representing the semantic structure of the precept definition.

```
TokenStream  →  Parser.Parse  →  SyntaxTree
```

The parser's job is pure structural assembly. It has no knowledge of types, scope, or graph properties. Its output is consumed by `TypeChecker.Check`.

The parser produces an AST regardless of input quality. On malformed input it emits parse diagnostics and inserts `IsMissing` nodes or skips to the next sync point, ensuring the type checker and language server receive a structurally coherent (if incomplete) tree. This is the pipeline's Model A (resilient) contract.

The public surface is a static class `Parser` with a single method:

```csharp
public static SyntaxTree Parse(TokenStream tokens)
```

No instance, no DI, no configuration. Tests call the method directly and assert on the output.

---

## Design Principles

### AST, not CST

The parser produces semantic structure — not a full-fidelity representation of the source. Comments and whitespace tokens are consumed silently. The tree contains no trivia nodes, no comment attachments, and no whitespace spans. Every AST node carries a `SourceSpan` (offset + length) pointing into the original source for diagnostics, hover, go-to-definition, and semantic tokens.

Precept files are 50–200 lines. LSP is a text-edit protocol — no LS feature requires tree-to-source round-tripping. A full-fidelity CST (Roslyn's red-green tree model) solves a formatting and refactoring problem that does not exist in Precept's scope. The v1 language server already works on an AST and all its features work correctly. The v2 parser carries this forward.

Rejected alternative: _Full-fidelity CST_ — disproportionate complexity for Precept's scale, requires trivia tokens, whitespace attachment, and a second "red" tree layer. Gains nothing against the actual LS feature set.

Rejected alternative: _AST with comment attachment_ — no LS or type-checker consumer reads comments on AST nodes today. An opt-in comment-attachment pass can be added later without changing the core tree model.

### Structural statement boundaries, not newline-terminated

Each `ParseX` method consumes tokens until its grammar production is satisfied. When the next meaningful token is not a valid continuation, the production is done. The top-level dispatch loop advances on that terminal token to the next declaration. NewLines and Comments are consumed silently by `SkipTrivia()` calls at every consumption point.

Precept routinely spans productions across lines: multi-line `when` guards, action chains, transition rows with trailing `->`, and `event ... with` arg lists all span more than one line. Treating NewLine as a statement terminator would require special-casing every multi-line form and would add significant complexity for zero gain — the grammar is keyword-disjoint and terminates productions naturally.

Rejected alternative: _NewLine as soft separator_ — creates an ambiguity problem where there is none. The grammar already terminates cleanly from keyword structure alone.

### Parser owns grammar; type checker owns semantics

The bright line: if the grammar rule defines it, the parser enforces it. If enforcement requires knowing what an identifier means, the type checker enforces it.

- **Parser:** missing required clauses, unrecognized keywords in syntactic positions, malformed expressions, unclosed blocks, invalid production sequences.
- **Type checker:** duplicate field names, unknown state/event references, type mismatches, unreachable states, constraint satisfaction, scope violations.

This boundary means the parser never builds a symbol table and never performs name resolution. `IdentifierExpression` nodes are bare name tokens — the type checker resolves them.

### Typed constant interior is opaque

The parser assembles typed constant tokens (`TypedConstant`, `TypedConstantStart`/`Middle`/`End`) into `TypedConstantExpression` or `InterpolatedTypedConstantExpression` AST nodes but does NOT parse or validate the interior content. The `Text` field on the token is stored as-is. Shape matching, type-family resolution, and context narrowing are type-checker responsibilities.

This is a direct consequence of the parser/type-checker boundary: what `'30 days'` means is semantic, not syntactic. The grammar is `'` content `'` with optional interpolation. Whether "30 days" is a `duration`, `period`, or something else is determined by context that only the type checker has.

Rejected alternative: _Parser pre-validates typed constant shape_ — redundant 40–80 lines that the type checker must re-run anyway. Any duplicate between parser and type checker would become a maintenance burden.

### Depth-unaware interpolation reassembly

`ParseInterpolatedString()` and `ParseInterpolatedTypedConstant()` are simple loops: consume `Start` → (parse expression → consume `Middle`)* → parse expression → consume `End`. `ParseExpression()` terminates naturally at `StringMiddle`, `StringEnd`, `TypedConstantMiddle`, or `TypedConstantEnd` — these token kinds have no null-denotation or left-denotation in the expression parser.

The lexer's mode stack already segmented the stream correctly. `StringMiddle`/`StringEnd`/`TypedConstantMiddle`/`TypedConstantEnd` are structurally disjoint from all expression tokens. There is no depth-tracking work left for the parser to do — it just reads the interleaved token sequence the lexer produced.

Rejected alternative: _Depth-tracked parsing_ — duplicates lexer knowledge for no benefit.

### Never stop parsing

The parser always runs to end-of-source, even after encountering errors. It produces `IsMissing` nodes for expected-but-absent tokens and resyncs to the next sync point when structurally lost. The pipeline receives a `SyntaxTree` with diagnostics — not an exception or a null. Every consumer (LS, type checker, MCP tools) requires a complete tree structure to operate.

---

## Architecture

### Static class + private ParseSession struct

The public surface is a static class `Parser` with a single method `Parse`. All mutable parsing state lives in a private `ParseSession` struct instantiated inside `Parse()` and discarded after:

```csharp
public static class Parser
{
    public static SyntaxTree Parse(TokenStream tokens)
    {
        var session = new ParseSession(tokens);
        var root = session.ParsePrecept();
        return new SyntaxTree(root, session.Diagnostics.ToImmutable());
    }
}
```

`ParseSession` is the struct that holds the token stream cursor, the diagnostic accumulator, and all `ParseX` helper methods. It is not visible to callers.

Rejected alternative: _Instance class `Parser`_ — adds a constructor, instance fields, and a lifecycle to manage with no benefit. No caller configures or reuses a parser instance.

### Top-level dispatch loop

After the `precept <Name>` header, the parser enters a loop that dispatches on the current non-trivia token:

| Leading token(s) | Production |
|-----------------|-----------|
| `field` | `ParseFieldDeclaration()` |
| `state` | `ParseStateDeclaration()` |
| `event` | `ParseEventDeclaration()` |
| `rule` | `ParseRuleDeclaration()` |
| `write` | `ParseRootWriteDeclaration()` |
| `in` | `ParseInStatement()` → StateEnsure or AccessModeDeclaration |
| `to` | `ParseToStatement()` → StateEnsure or StateAction |
| `from` | `ParseFromStatement()` → StateEnsure, StateAction, or TransitionRow |
| `on` | `ParseOnStatement()` → EventEnsureDeclaration or StatelessEventHookDeclaration |
| `EndOfSource` | exit loop |
| _anything else_ | emit diagnostic, resync |

The `in`/`to`/`from` keywords dispatch to multi-production parsers that look ahead one token to select the correct production.

### `set` disambiguation

The lexer always emits `TokenKind.Set` for the word `set`. The parser reinterprets it as `TokenKind.SetType` (synthetic — never emitted by the lexer) when inside `ParseTypeRef()`, specifically when the preceding token is `As` or `Of`. This is a parser-side disambiguation: inside `ParseTypeRef()`, if the current token is `Set`, the parser treats it as the collection type keyword and proceeds to consume `of <ElementType>`. Outside `ParseTypeRef()`, `Set` is the action keyword introducing a `SetAction`.

This distinction cannot be made in the lexer because it requires knowing whether `set` appears in a field declaration context or an action body — information the lexer does not have.

---

## AST Node Hierarchy

### SourceSpan struct

```csharp
/// <summary>
/// A span in the source text. Offset is 0-based; Length is in characters.
/// Used on every AST node for diagnostics, hover, go-to-definition, and semantic tokens.
/// </summary>
public readonly record struct SourceSpan(int Offset, int Length)
{
    public static readonly SourceSpan Missing = new(0, 0);

    public int End => Offset + Length;

    public static SourceSpan Covering(SourceSpan first, SourceSpan last) =>
        new(first.Offset, last.End - first.Offset);
}
```

### SyntaxNode base

```csharp
/// <summary>
/// Base type for all AST nodes. Every node carries a source span and an IsMissing flag.
/// </summary>
public abstract record SyntaxNode(SourceSpan Span)
{
    /// <summary>
    /// True when this node was synthesized by error recovery to fill a missing token
    /// or production. IsMissing nodes have zero-length spans pointing to the position
    /// where the absent construct was expected.
    /// </summary>
    public bool IsMissing { get; init; }
}
```

### Intermediate abstract types

Three abstract records group the concrete types and provide precise return types for `ParseX` methods:

```csharp
/// <summary>Top-level entries in the precept body.</summary>
public abstract record Declaration(SourceSpan Span) : SyntaxNode(Span);

/// <summary>Sub-statement units within declarations (action steps in transition rows and state actions).</summary>
public abstract record Statement(SourceSpan Span) : SyntaxNode(Span);

/// <summary>Expression nodes — leaf values, operators, literals, calls.</summary>
public abstract record Expression(SourceSpan Span) : SyntaxNode(Span);
```

### SyntaxTree (pipeline artifact)

```csharp
public sealed record class SyntaxTree(
    PreceptNode?               Root,        // null only when EndOfSource precedes 'precept' keyword
    ImmutableArray<Diagnostic> Diagnostics
);
```

`Root` is null only when the source is entirely empty or the parser cannot find a `precept` keyword before end-of-source. In all other cases, `Root` is non-null (potentially with `IsMissing` on the `Name` token if the identifier is absent).

---

### Complete Node Catalog

#### Root

```csharp
/// <summary>The precept file root. Contains the precept name and all top-level declarations.</summary>
public sealed record PreceptNode(
    SourceSpan                   Span,
    Token                        Name,       // the precept identifier; IsMissing if absent
    ImmutableArray<Declaration>  Body
) : SyntaxNode(Span);
```

---

#### Declarations

```csharp
/// <summary>
/// field Identifier ("," Identifier)* as TypeRef FieldModifier*
/// field Identifier as TypeRef "->" Expr ConstraintSuffix*
/// </summary>
public sealed record FieldDeclaration(
    SourceSpan                    Span,
    ImmutableArray<Token>         Names,      // one or more field identifiers
    TypeRef                       Type,
    ImmutableArray<FieldModifier> Modifiers,
    Expression?                   ComputedExpression   // set when "->" follows the type
) : Declaration(Span);

/// <summary>
/// state StateNameEntry ("," StateNameEntry)*
/// </summary>
public sealed record StateDeclaration(
    SourceSpan                Span,
    ImmutableArray<StateEntry> Entries
) : Declaration(Span);

/// <summary>
/// event Identifier ("," Identifier)* ("with" ArgList)? ("initial")?
/// </summary>
public sealed record EventDeclaration(
    SourceSpan                    Span,
    ImmutableArray<Token>         Names,
    ImmutableArray<ArgDeclaration> Args,     // empty when no "with" clause
    bool                           IsInitial  // true when "initial" keyword present
) : Declaration(Span);

/// <summary>
/// rule BoolExpr ("when" BoolExpr)? "because" StringExpr
/// </summary>
public sealed record RuleDeclaration(
    SourceSpan  Span,
    Expression  Condition,
    Expression? Guard,
    Expression  Message
) : Declaration(Span);

/// <summary>
/// "in" StateTarget AccessMode FieldTarget ("when" Guard)?   (state-scoped)
/// "write" FieldTarget                                        (root-level stateless)
/// </summary>
public sealed record AccessModeDeclaration(
    SourceSpan   Span,
    StateTarget? StateScope,    // null for root-level write
    AccessMode   Mode,
    FieldTarget  Fields,
    Expression?  Guard          // only valid when Mode == Write and StateScope != null
) : Declaration(Span);

public enum AccessMode { Write, Read, Omit }

/// <summary>"on" Identifier ActionChain — stateless event-driven mutation hook.</summary>
public sealed record StatelessEventHookDeclaration(
    SourceSpan                Span,
    Token                     EventName,
    ImmutableArray<Statement> Actions
) : Declaration(Span);

/// <summary>
/// "from" StateTarget "on" Identifier ("when" BoolExpr)? ActionChain? "->" Outcome
/// </summary>
public sealed record TransitionRowDeclaration(
    SourceSpan                   Span,
    StateTarget                  FromStates,
    Token                        EventName,
    Expression?                  Guard,
    ImmutableArray<Statement>    Actions,
    OutcomeNode                  Outcome
) : Declaration(Span);

/// <summary>
/// "in"|"to"|"from" StateTarget "ensure" BoolExpr ("when" BoolExpr)? "because" StringExpr
/// </summary>
public sealed record StateEnsureDeclaration(
    SourceSpan      Span,
    EnsureAnchor    Anchor,     // In, To, or From
    StateTarget     States,
    Expression      Condition,
    Expression?     Guard,
    Expression      Message
) : Declaration(Span);

/// <summary>
/// "on" Identifier "ensure" BoolExpr ("when" BoolExpr)? "because" StringExpr
/// </summary>
public sealed record EventEnsureDeclaration(
    SourceSpan  Span,
    Token       EventName,
    Expression  Condition,
    Expression? Guard,
    Expression  Message
) : Declaration(Span);

/// <summary>
/// "to"|"from" StateTarget ActionChain
/// </summary>
public sealed record StateActionDeclaration(
    SourceSpan                Span,
    StateActionAnchor         Anchor,    // To or From
    StateTarget               States,
    ImmutableArray<Statement> Actions
) : Declaration(Span);

// ── Supporting enums ───────────────────────────────────────────────────────────

public enum EnsureAnchor  { In, To, From }
public enum StateActionAnchor { To, From }
```

---

#### Statements (action steps)

```csharp
/// <summary>"set" Identifier "=" Expr</summary>
public sealed record SetActionStatement(
    SourceSpan Span, Token FieldName, Expression Value
) : Statement(Span);

/// <summary>"add" Identifier Expr</summary>
public sealed record AddActionStatement(
    SourceSpan Span, Token FieldName, Expression Value
) : Statement(Span);

/// <summary>"remove" Identifier Expr</summary>
public sealed record RemoveActionStatement(
    SourceSpan Span, Token FieldName, Expression Value
) : Statement(Span);

/// <summary>"enqueue" Identifier Expr</summary>
public sealed record EnqueueActionStatement(
    SourceSpan Span, Token FieldName, Expression Value
) : Statement(Span);

/// <summary>"dequeue" Identifier ("into" Identifier)?</summary>
public sealed record DequeueActionStatement(
    SourceSpan Span, Token FieldName, Token? IntoField
) : Statement(Span);

/// <summary>"push" Identifier Expr</summary>
public sealed record PushActionStatement(
    SourceSpan Span, Token FieldName, Expression Value
) : Statement(Span);

/// <summary>"pop" Identifier ("into" Identifier)?</summary>
public sealed record PopActionStatement(
    SourceSpan Span, Token FieldName, Token? IntoField
) : Statement(Span);

/// <summary>"clear" Identifier</summary>
public sealed record ClearActionStatement(
    SourceSpan Span, Token FieldName
) : Statement(Span);
```

---

#### Outcomes

Outcomes are not `Declaration` or `Statement` — they appear only inside `TransitionRowDeclaration`.

```csharp
public abstract record OutcomeNode(SourceSpan Span) : SyntaxNode(Span);

/// <summary>"transition" Identifier</summary>
public sealed record TransitionOutcomeNode(
    SourceSpan Span, Token StateName
) : OutcomeNode(Span);

/// <summary>"no" "transition"</summary>
public sealed record NoTransitionOutcomeNode(SourceSpan Span) : OutcomeNode(Span);

/// <summary>"reject" StringExpr</summary>
public sealed record RejectOutcomeNode(
    SourceSpan Span, Expression Message
) : OutcomeNode(Span);
```

---

#### Type references

```csharp
public abstract record TypeRef(SourceSpan Span) : SyntaxNode(Span);

/// <summary>string | number | integer | decimal | boolean + v2 temporal/domain types</summary>
public sealed record ScalarTypeRef(
    SourceSpan Span, ScalarTypeKind Kind, TypeQualifier? Qualifier
) : TypeRef(Span);

/// <summary>set of T | queue of T | stack of T</summary>
public sealed record CollectionTypeRef(
    SourceSpan      Span,
    CollectionKind  Kind,
    ScalarTypeRef   ElementType
) : TypeRef(Span);

/// <summary>choice("A", "B", ...)</summary>
public sealed record ChoiceTypeRef(
    SourceSpan             Span,
    ImmutableArray<Expression> Choices   // parser accepts only StringLiteralExpression here
) : TypeRef(Span);

public enum ScalarTypeKind
{
    String, Number, Integer, Decimal, Boolean,
    // v2 temporal
    Date, Time, Instant, Duration, Period, Timezone, ZonedDateTime, DateTime,
    // v2 business-domain
    Money, Currency, Quantity, UnitOfMeasure, Dimension, Price, ExchangeRate
}

public enum CollectionKind { Set, Queue, Stack }

/// <summary>Type qualifier: "in" TypedConstant | "of" TypedConstant — narrows value domain.</summary>
public sealed record TypeQualifier(
    SourceSpan        Span,
    TypeQualifierKind Kind,
    Expression        Value    // the typed constant: 'USD', 'kg', 'length', etc.
) : SyntaxNode(Span);

public enum TypeQualifierKind { In, Of }
```

---

#### Field modifiers

```csharp
public abstract record FieldModifier(SourceSpan Span) : SyntaxNode(Span);

public sealed record OptionalModifier(SourceSpan Span)   : FieldModifier(Span);  // "optional"
public sealed record OrderedModifier(SourceSpan Span)    : FieldModifier(Span);
public sealed record NonnegativeModifier(SourceSpan Span): FieldModifier(Span);
public sealed record PositiveModifier(SourceSpan Span)   : FieldModifier(Span);
public sealed record NonzeroModifier(SourceSpan Span)    : FieldModifier(Span);  // v2
public sealed record NotemptyModifier(SourceSpan Span)   : FieldModifier(Span);
public sealed record DefaultModifier(SourceSpan Span, Expression Value) : FieldModifier(Span);
public sealed record MinModifier(SourceSpan Span, Expression Value)     : FieldModifier(Span);
public sealed record MaxModifier(SourceSpan Span, Expression Value)     : FieldModifier(Span);
public sealed record MinLengthModifier(SourceSpan Span, Expression Value): FieldModifier(Span);
public sealed record MaxLengthModifier(SourceSpan Span, Expression Value): FieldModifier(Span);
public sealed record MinCountModifier(SourceSpan Span, Expression Value) : FieldModifier(Span);
public sealed record MaxCountModifier(SourceSpan Span, Expression Value) : FieldModifier(Span);
public sealed record MaxPlacesModifier(SourceSpan Span, Expression Value): FieldModifier(Span);
```

---

#### Expressions

```csharp
/// <summary>A op B — all infix binary operators.</summary>
public sealed record BinaryExpression(
    SourceSpan Span, Expression Left, BinaryOp Op, Expression Right
) : Expression(Span);

/// <summary>not Expr | -Expr</summary>
public sealed record UnaryExpression(
    SourceSpan Span, UnaryOp Op, Expression Operand
) : Expression(Span);

/// <summary>Expr contains Expr</summary>
public sealed record ContainsExpression(
    SourceSpan Span, Expression Collection, Expression Value
) : Expression(Span);

/// <summary>Expr is set | Expr is not set — presence test for optional fields.</summary>
public sealed record IsSetExpression(
    SourceSpan Span, Expression Operand, bool IsNot
) : Expression(Span);

/// <summary>if Cond then Consequence else Alternative</summary>
public sealed record ConditionalExpression(
    SourceSpan Span, Expression Condition, Expression Consequence, Expression Alternative
) : Expression(Span);

/// <summary>Expr.Member — dotted access (event arg, collection member, field member, chained).</summary>
public sealed record MemberAccessExpression(
    SourceSpan Span, Expression Object, Token Member
) : Expression(Span);

/// <summary>FunctionName(Arg, ...) — min, max, round, clamp</summary>
public sealed record CallExpression(
    SourceSpan                 Span,
    Token                      FunctionName,
    ImmutableArray<Expression> Args
) : Expression(Span);

/// <summary>A bare identifier (field name, state name, event name — resolved by type checker).</summary>
public sealed record IdentifierExpression(
    SourceSpan Span, Token Name
) : Expression(Span);

/// <summary>A numeric literal (integer or decimal as written in source).</summary>
public sealed record NumberLiteralExpression(
    SourceSpan Span, Token Value
) : Expression(Span);

/// <summary>true | false</summary>
public sealed record BooleanLiteralExpression(
    SourceSpan Span, bool Value
) : Expression(Span);

/// <summary>A non-interpolated string: single StringLiteral token.</summary>
public sealed record StringLiteralExpression(
    SourceSpan Span, Token Value
) : Expression(Span);

/// <summary>An interpolated string: StringStart + (expr + StringMiddle)* + expr + StringEnd.</summary>
public sealed record InterpolatedStringExpression(
    SourceSpan                        Span,
    ImmutableArray<InterpolationSegment> Segments
) : Expression(Span);

/// <summary>A non-interpolated typed constant: single TypedConstant token.</summary>
public sealed record TypedConstantExpression(
    SourceSpan Span, Token Value
) : Expression(Span);

/// <summary>An interpolated typed constant: same reassembly loop as string, different token kinds.</summary>
public sealed record InterpolatedTypedConstantExpression(
    SourceSpan                        Span,
    ImmutableArray<InterpolationSegment> Segments
) : Expression(Span);

/// <summary>[ Expr, Expr, ... ] — list literal for default values on collection fields.</summary>
public sealed record ListLiteralExpression(
    SourceSpan                 Span,
    ImmutableArray<Expression> Elements
) : Expression(Span);

/// <summary>(Expr) — parenthesized grouping.</summary>
public sealed record ParenthesizedExpression(
    SourceSpan Span, Expression Inner
) : Expression(Span);

// ── Operators ──────────────────────────────────────────────────────────────────

public enum BinaryOp
{
    Or, And,
    Equal, NotEqual,
    Less, Greater, LessOrEqual, GreaterOrEqual,
    Plus, Minus, Star, Slash, Percent
}

public enum UnaryOp { Not, Negate }
```

---

#### Interpolation segments

```csharp
/// <summary>One segment of an interpolated string or typed constant.</summary>
public abstract record InterpolationSegment(SourceSpan Span) : SyntaxNode(Span);

/// <summary>The literal text portion of an interpolated literal (text of Start/Middle/End token).</summary>
public sealed record TextSegment(SourceSpan Span, Token Token) : InterpolationSegment(Span);

/// <summary>An embedded expression within { ... }.</summary>
public sealed record ExpressionSegment(SourceSpan Span, Expression Inner) : InterpolationSegment(Span);
```

---

#### Auxiliary nodes

```csharp
/// <summary>The state target for from/to/in/state-action clauses.</summary>
public sealed record StateTarget(
    SourceSpan            Span,
    bool                  IsAny,        // true when "any" keyword
    ImmutableArray<Token> Names         // empty when IsAny
) : SyntaxNode(Span);

/// <summary>The field target for access mode declarations.</summary>
public sealed record FieldTarget(
    SourceSpan            Span,
    bool                  IsAll,        // true when "all" keyword
    ImmutableArray<Token> Names         // empty when IsAll
) : SyntaxNode(Span);

/// <summary>A single entry in a state declaration: name + modifiers.</summary>
public sealed record StateEntry(
    SourceSpan                       Span,
    Token                            Name,
    bool                             IsInitial,
    ImmutableArray<StateModifierKind> Modifiers   // v2: terminal, required, irreversible, success, warning, error
) : SyntaxNode(Span);

/// <summary>An argument declaration in an event "with" clause.</summary>
public sealed record ArgDeclaration(
    SourceSpan                    Span,
    Token                         Name,
    TypeRef                       Type,
    ImmutableArray<FieldModifier> Modifiers
) : SyntaxNode(Span);

public enum StateModifierKind { Terminal, Required, Irreversible, Success, Warning, Error }
```

---

## Grammar-to-Parser Mapping

Each production describes the entry point method, its dispatch token(s), its recovery behavior, and what it returns.

### `precept` header

```
ParsePrecept() → PreceptNode
  Consume: precept
  Expect:  Identifier (IsMissing if absent)
  Loop:    SkipTrivia(), dispatch on Current.Kind → ParseDeclaration()
  Recover: emit missing node for Name if not Identifier
```

### `field` declaration

```
ParseFieldDeclaration() → FieldDeclaration
  Consume:  field
  Expect:   Identifier (IsMissing if absent)
  Loop:     while Current.Kind == Comma → Consume Comma, Expect Identifier
  Expect:   As
  Call:     ParseTypeRef()
  Loop:     while Current.Kind ∈ FieldModifierStarters → ParseFieldModifier()
  Optional: if Current.Kind == Arrow → Consume Arrow, ParseExpression(0)
  Recover:  missing-node insertion at each Expect; does not resync — modifiers are
            optional so the next sync keyword naturally ends the production
```

### `state` declaration

```
ParseStateDeclaration() → StateDeclaration
  Consume: state
  Expect:  Identifier for first StateEntry
  Loop:    while Current.Kind == Comma → Consume Comma, ParseStateEntry()
  ParseStateEntry():
    Expect Identifier
    Optional: if Current.Kind == Initial → Consume
    v2: while Current.Kind ∈ StateModifierKinds → Consume and record
```

### `event` declaration

```
ParseEventDeclaration() → EventDeclaration
  Consume: event
  Expect:  Identifier
  Loop:    while Current.Kind == Comma → Consume Comma, Expect Identifier
  Optional: if Current.Kind == With →
    Consume With
    ParseArgDeclaration()
    while Current.Kind == Comma → Consume Comma, ParseArgDeclaration()
```

### `rule` declaration

```
ParseRuleDeclaration() → RuleDeclaration
  Consume: rule
  Call:    ParseExpression(0) → Condition
  Optional: if Current.Kind == When → Consume When, ParseExpression(0) → Guard
  Expect:  Because
  Call:    ParseStringExpression() → Message
```

### `write` / access-mode declaration

```
ParseRootWriteDeclaration() → AccessModeDeclaration (StateScope = null, Mode = Write)
  Consume: write
  Call:    ParseFieldTarget()
  (Guard not valid at root level)

ParseInStatement() branches:
  if peek-ahead finds "write"|"read"|"omit":
    StateScope = ParseStateTarget()
    Consume AccessMode keyword → Mode
    ParseFieldTarget()
    Optional: if Mode == Write and Current.Kind == When → Consume When, ParseExpression(0) → Guard
    → AccessModeDeclaration (StateScope set)
  if "ensure":     → StateEnsureDeclaration (Anchor = In)
  else:            → emit diagnostic, resync (no other production begins with in)
```

### `in` / `to` / `from` statements

`in`, `to`, and `from` are each shared-dispatch methods. After consuming the preposition keyword, the parser peeks at the sequence to determine which production follows:

| After preposition | Token that disambiguates | Production |
|-------------------|--------------------------|-----------|
| `in <target>` | `ensure` after target | `StateEnsureDeclaration` (Anchor=In) |
| `in <target>` | `write`/`read`/`omit` after target | `AccessModeDeclaration` (state-scoped) |
| `to <target>` | `ensure` after target | `StateEnsureDeclaration` (Anchor=To) |
| `to <target>` | `->` after target | `StateActionDeclaration` (Anchor=To) |
| `from <target>` | `ensure` after target | `StateEnsureDeclaration` (Anchor=From) |
| `from <target>` | `->` after target | `StateActionDeclaration` (Anchor=From) |
| `from <target>` | `on` after target | `TransitionRowDeclaration` |

### Transition row

```
ParseTransitionRowDeclaration() → TransitionRowDeclaration
  (from and StateTarget already consumed by dispatch)
  Expect:   On
  Expect:   Identifier → EventName
  Optional: if Current.Kind == When → Consume When, ParseExpression(0) → Guard
  Loop:     while Current.Kind == Arrow → Consume Arrow, ParseActionStatement()
  Expect:   Arrow (final → before outcome)
  Call:     ParseOutcome()

ParseOutcome():
  if Transition → Consume Transition, Expect Identifier → TransitionOutcomeNode
  if No        → Consume No, Expect Transition (keyword) → NoTransitionOutcomeNode
  if Reject    → Consume Reject, ParseStringExpression() → RejectOutcomeNode
  else         → IsMissing TransitionOutcomeNode, emit diagnostic
```

### `on` statement

```
ParseOnStatement()
  Consume:  On
  Expect:   Identifier → EventName
  if Current.Kind == Ensure → ParseEventEnsureDeclaration(eventName)
  else                      → ParseStatelessEventHookDeclaration(eventName)

ParseStatelessEventHookDeclaration(eventName) → StatelessEventHookDeclaration
  (On and EventName already consumed by ParseOnStatement)
  Expect:  Arrow (→)
  Loop:    while Current.Kind ∈ ActionKeywords → ParseActionStatement()
  Recover: if no Arrow, emit diagnostic, resync
```

### State/event ensure

```
ParseStateEnsureDeclaration(anchor) → StateEnsureDeclaration
  (preposition already consumed; StateTarget already parsed)
  Expect:   Ensure
  Call:     ParseExpression(0) → Condition
  Optional: When guard
  Expect:   Because
  Call:     ParseStringExpression() → Message

ParseEventEnsureDeclaration(eventName) → EventEnsureDeclaration
  (On and EventName already consumed by ParseOnStatement)
  Expect:  Ensure
  (same as above for condition/guard/because)
```

### Action statement

```
ParseActionStatement() → Statement (SetAction | CollectionAction)
  Dispatch on Current.Kind:
    Set      → Consume, Expect Identifier, Expect Assign, ParseExpression(0)
    Add      → Consume, Expect Identifier, ParseExpression(0)
    Remove   → Consume, Expect Identifier, ParseExpression(0)
    Enqueue  → Consume, Expect Identifier, ParseExpression(0)
    Dequeue  → Consume, Expect Identifier; if Into → Consume Into, Expect Identifier
    Push     → Consume, Expect Identifier, ParseExpression(0)
    Pop      → Consume, Expect Identifier; if Into → Consume Into, Expect Identifier
    Clear    → Consume, Expect Identifier
    else     → IsMissing SetActionStatement, emit diagnostic
```

### Type reference

```
ParseTypeRef() → TypeRef
  Dispatch on Current.Kind:
    StringType, NumberType, IntegerType, DecimalType, BooleanType,
    DateType, TimeType, InstantType, DurationType, PeriodType,
    TimezoneType, ZonedDateTimeType, DateTimeType,
    MoneyType, CurrencyType, QuantityType, UnitOfMeasureType,
    DimensionType, PriceType, ExchangeRateType  → ScalarTypeRef
    Set (followed by Of context) → CollectionTypeRef (CollectionKind.Set)
    QueueType  → CollectionTypeRef (CollectionKind.Queue)
    StackType  → CollectionTypeRef (CollectionKind.Stack)
    ChoiceType → Consume, Expect LeftParen, parse comma-separated StringLiterals, Expect RightParen

  CollectionTypeRef:
    Consume type keyword (Set/QueueType/StackType)
    Expect: Of
    Call:   ParseScalarTypeRef() → ElementType

  ScalarTypeRef (optional qualifier after type keyword):
    Optional: if Current.Kind ∈ {In, Of} → ParseTypeQualifier() → Qualifier
    → ScalarTypeRef(Kind, Qualifier?)

ParseTypeQualifier() → TypeQualifier
  Consume: In or Of → Kind (TypeQualifierKind)
  Consume: TypedConstant (non-interpolated) or TypedConstantStart (interpolated)
  Call:    ParseTypedConstantExpression() → Value
  → TypeQualifier(Kind, Value)
```

---

## Expression Parser

The expression parser uses **Pratt parsing** (top-down operator precedence). `ParseExpression(int minBp)` parses a complete expression, stopping when it encounters a token whose left-binding power is ≤ `minBp`. This terminates naturally at statement boundaries, because no statement keyword (`field`, `state`, `from`, etc.), no `StringMiddle`/`StringEnd`/`TypedConstantMiddle`/`TypedConstantEnd`, and no `Comma` in argument lists have expression left-binding power.

### Binding power table

| Token(s) | Role | Left BP (infix) | Right BP (prefix) | Associativity |
|----------|------|:-:|:-:|---|
| `or` | binary infix | 10 | — | left |
| `and` | binary infix | 20 | — | left |
| `not` | prefix | — | 25 | right (prefix) |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | binary infix | 30 | — | non-associative |
| `contains` | binary infix | 40 | — | left |
| `is` | binary infix (presence: `is set` / `is not set`) | 40 | — | left |
| `+`, `-` (infix) | binary infix | 50 | — | left |
| `*`, `/`, `%` | binary infix | 60 | — | left |
| `-` (prefix unary) | prefix | — | 65 | right (prefix) |
| `.` (member access) | binary infix | 80 | — | left |
| `(` (function call postfix) | postfix | 80 | — | left |
| atoms | n-denotation | — | — | — |

Non-associative comparisons: `A == B == C` is a parse error (the parser emits a diagnostic for the second `==`).

### Null-denotation (atoms and prefix)

| Token | Action |
|-------|--------|
| `Identifier` | If peek is `(` → `ParseCallExpression()`; if peek is `.` → `ParseMemberAccessExpression()`; else → `IdentifierExpression` |
| `NumberLiteral` | `NumberLiteralExpression` |
| `True` / `False` | `BooleanLiteralExpression` |
| `StringLiteral` | `StringLiteralExpression` |
| `StringStart` | `ParseInterpolatedString()` |
| `TypedConstant` | `TypedConstantExpression` |
| `TypedConstantStart` | `ParseInterpolatedTypedConstant()` |
| `LeftBracket` | `ParseListLiteral()` |
| `LeftParen` | Consume, `ParseExpression(0)`, Expect RightParen → `ParenthesizedExpression` |
| `Not` | Consume, `ParseExpression(25)` → `UnaryExpression(Not, ...)` |
| `-` | Consume, `ParseExpression(65)` → `UnaryExpression(Negate, ...)` |
| `If` | `ParseConditionalExpression()` |
| _other_ | IsMissing `IdentifierExpression`, emit diagnostic |

### Left-denotation (infix and postfix)

```csharp
private Expression Led(Expression left, Token op)
{
    return op.Kind switch
    {
        Or               => new BinaryExpression(left, BinaryOp.Or,           ParseExpression(10), ...),
        And              => new BinaryExpression(left, BinaryOp.And,          ParseExpression(20), ...),
        DoubleEquals     => new BinaryExpression(left, BinaryOp.Equal,        ParseExpression(31), ...),  // non-assoc: rbp > lbp
        NotEquals        => new BinaryExpression(left, BinaryOp.NotEqual,     ParseExpression(31), ...),
        LessThan         => new BinaryExpression(left, BinaryOp.Less,         ParseExpression(31), ...),
        GreaterThan      => new BinaryExpression(left, BinaryOp.Greater,      ParseExpression(31), ...),
        LessThanOrEqual  => new BinaryExpression(left, BinaryOp.LessOrEqual,  ParseExpression(31), ...),
        GreaterThanOrEqual=>new BinaryExpression(left, BinaryOp.GreaterOrEqual,ParseExpression(31),...),
        Contains         => new ContainsExpression(left,                       ParseExpression(40), ...),
        Is               => ParseIsSetExpression(left, ...),
        Plus             => new BinaryExpression(left, BinaryOp.Plus,         ParseExpression(50), ...),
        Minus            => new BinaryExpression(left, BinaryOp.Minus,        ParseExpression(50), ...),
        Star             => new BinaryExpression(left, BinaryOp.Star,         ParseExpression(60), ...),
        Slash            => new BinaryExpression(left, BinaryOp.Slash,        ParseExpression(60), ...),
        Percent          => new BinaryExpression(left, BinaryOp.Percent,      ParseExpression(60), ...),
        Dot              => ParseMemberAccessContinuation(left, ...),
        _                => /* should not reach — caller checked lbp > minBp */
    };
}
```

`ParseIsSetExpression`: after `is`, expect optional `Not`, then `Set` keyword. Returns `IsSetExpression`.

`ParseConditionalExpression`: Consume `If`, `ParseExpression(0)` → Condition, Expect `Then`, `ParseExpression(0)` → Consequence, Expect `Else`, `ParseExpression(0)` → Alternative.

### Function calls

Built-in functions: `min`, `max`, `round`, `clamp`. The parser recognizes these by their identifier text (lowercase), not by token kind — they are identifiers whose name is in the known function set. Unknown identifiers followed by `(` are parsed as `CallExpression` with an unknown function name; the type checker emits an error on unrecognized function names.

```
ParseCallExpression(name: Token) → CallExpression
  Consume: LeftParen
  if Current.Kind != RightParen:
    ParseExpression(0)
    while Current.Kind == Comma → Consume Comma, ParseExpression(0)
  Expect: RightParen
```

---

## Interpolation Reassembly

`ParseInterpolatedString()` and `ParseInterpolatedTypedConstant()` both use the same loop structure. The difference is only the token kinds consumed.

```csharp
private InterpolatedStringExpression ParseInterpolatedString()
{
    var segments = ImmutableArray.CreateBuilder<InterpolationSegment>();
    var startToken = Consume(TokenKind.StringStart);
    var startSpan  = Span(startToken);

    segments.Add(new TextSegment(startSpan, startToken));

    while (true)
    {
        // Expression between Start and Middle/End
        var expr = ParseExpression(0);
        segments.Add(new ExpressionSegment(Span(expr), expr));

        if (Current.Kind == TokenKind.StringMiddle)
        {
            var mid = Consume();
            segments.Add(new TextSegment(Span(mid), mid));
            // continue loop for next expression
        }
        else
        {
            // StringEnd — or missing End (error recovery)
            var endToken = Expect(TokenKind.StringEnd, DiagnosticCode.UnterminatedStringLiteral);
            segments.Add(new TextSegment(Span(endToken), endToken));
            break;
        }
    }

    return new InterpolatedStringExpression(
        SourceSpan.Covering(startSpan, segments[^1].Span),
        segments.ToImmutable()
    );
}
```

`ParseExpression(0)` terminates at `StringMiddle`/`StringEnd` because those token kinds have no binding power in the expression parser (they are not in the `lbp` table). This is the depth-unawareness property: the expression parser cannot consume into the surrounding literal's segment boundary.

The same loop with `TypedConstantStart`/`TypedConstantMiddle`/`TypedConstantEnd` produces `InterpolatedTypedConstantExpression`.

When `StringEnd` / `TypedConstantEnd` is missing (the lexer emitted `StringStart`/`StringMiddle` without a matching `End` due to unterminated input), the `Expect` call inserts a zero-length `IsMissing` end token and adds a diagnostic. The tree is structurally complete.

---

## Error Recovery

The parser uses two complementary mechanisms, as decided by P2. Mechanism selection is automatic: within a production, use missing-node insertion; when the production itself cannot start or is structurally incoherent, use sync-point resync.

### Mechanism 1: Missing-node insertion

When an expected token is absent, the parser emits a diagnostic and creates a synthetic token with `IsMissing = true` and a zero-length span at the current position:

```csharp
private Token Expect(TokenKind kind, DiagnosticCode code)
{
    if (Current.Kind == kind)
        return Advance();

    AddDiagnostic(code, Current.Span);
    return new Token(kind, string.Empty, Current.Line, Current.Column, Current.Offset, 0)
        { IsMissing = true };
}
```

The resulting AST node carries a structurally coherent shape with the missing child marked. The language server can present partial completions and hover even at the gap — the rest of the tree is intact.

Use this mechanism for: absent identifiers after declaration keywords, missing `as`/`because`/`ensure`/`on`, missing expression atoms, missing `->` or outcome keywords.

### Mechanism 2: Sync-point resync

When the parser is structurally lost at the top level (the current token is not a valid declaration starter), it scans forward for a NewLine followed by a sync token:

```csharp
private void SkipToNextSyncPoint()
{
    while (Current.Kind != TokenKind.EndOfSource)
    {
        SkipTrivia(); // advances past NewLine and Comment
        if (IsSyncToken(Current.Kind))
            return;
        Advance();
    }
}
```

### Sync token table

| Token kind | Precept keyword |
|------------|----------------|
| `Precept`  | `precept` |
| `Field`    | `field` |
| `State`    | `state` |
| `Event`    | `event` |
| `Rule`     | `rule` |
| `From`     | `from` |
| `To`       | `to` |
| `In`       | `in` |
| `On`       | `on` |

These are the unambiguous top-level starters — every one of them can only appear at the beginning of a new declaration. Continuation tokens (`when`, `->`, `set`, `transition`, `ensure`, `because`, constraint keywords) are never sync points: they always appear mid-production and would cause the parser to skip valid content if used as resync anchors.

### Recovery behavior per production

| Production | Primary mechanism | Resync condition |
|-----------|-------------------|-----------------|
| `ParsePrecept()` | Missing Name → missing-node | Token before `precept` sync |
| `ParseFieldDeclaration()` | Missing identifiers/`as`/type → missing-node | Next sync token after field body ends |
| `ParseStateDeclaration()` | Missing identifiers → missing-node | Next sync token |
| `ParseEventDeclaration()` | Missing identifiers/arg names → missing-node | Next sync token |
| `ParseTransitionRowDeclaration()` | Missing `on`/event/`->`/outcome → missing-node | If no valid outcome can be assembled |
| `ParseActionStatement()` | Unknown action keyword → missing SetAction + resync | Next `->` or sync token |
| `ParseExpression(0)` | Unknown atom → missing IdentifierExpression | None (caller resumes after returning) |
| Top-level loop | Unknown declaration keyword | Always resyncs |

The ~35 lines of resync logic are in `SkipToNextSyncPoint()` and `IsSyncToken()`. Missing-node insertion is inline at each `Expect` call.

---

## Consumer Contracts

### Language server

The language server reads `CompilationResult.SyntaxTree`. Its contracts:

| LS feature | What it reads |
|------------|--------------|
| Diagnostics | `SyntaxTree.Diagnostics` (parse-stage codes only) |
| Completions | `SyntaxTree.Root` — walks the tree to find the node containing the cursor; `IsMissing` nodes indicate gaps where completions are most useful |
| Hover | `SyntaxTree.Root` — finds the deepest node whose `Span` contains the cursor position; `IdentifierExpression` and `TypeRef` nodes are the primary hover targets |
| Go-to-definition | `IdentifierExpression.Name.Offset` → used to find the declaration whose name token matches |
| Semantic tokens | Traverses the tree to classify each `Token` by its syntactic role (declaration keyword, type keyword, identifier, operator, literal) |

`IsMissing` nodes are safe to traverse — they have valid spans (zero-length at the expected position) and are structurally present in the tree. The LS must check `IsMissing` before offering completions that depend on the node's content.

### Type checker

`TypeChecker.Check(SyntaxTree)` receives the tree as input. Its contracts:

- Walks `SyntaxTree.Root.Body` as a flat list of `Declaration` nodes.
- `IsMissing` nodes are skipped: a declaration with a missing name or type cannot be bound, so the type checker suppresses redundant name-resolution errors for it.
- All `Expression` subtrees are walked for type inference. Missing expression nodes (`IsMissing = true`) propagate an error type, suppressing cascading type errors.
- The type checker builds its symbol table from the AST — it does not feed back into the parser.

### MCP server

MCP tools (`precept_compile`) receive `CompilationResult`, which contains `SyntaxTree`. Parse diagnostics surface in the `Diagnostics` array with `DiagnosticStage.Parse`. MCP callers can distinguish parse errors from type errors via the stage field.

---

## Deliberate Exclusions

The parser intentionally does NOT:

- **Resolve names.** `IdentifierExpression` nodes carry raw name tokens. Name resolution is the type checker's job.
- **Validate typed constant interiors.** The `Text` of `TypedConstant`/`TypedConstantStart`/`Middle`/`End` tokens is passed through as-is to the AST. Shape matching is the type checker's job.
- **Synthesize `SetType` as a token kind.** The parser reads `TokenKind.Set` and contextually uses it as a collection type keyword within `ParseTypeRef()`. The `TokenKind.SetType` enum value exists as the parser's canonical kind annotation, but the parser never emits a `SetType` token — it reads `Set` and produces a `CollectionTypeRef(CollectionKind.Set, ...)` node.
- **Validate state/event name uniqueness.** Duplicate names are a semantic error. The parser would need a symbol table to catch them — that is the type checker's job.
- **Validate `initial` uniqueness.** Multiple `initial` modifiers on state entries is a semantic error — parser cannot count across declarations without symbol analysis.
- **Short-circuit on first error.** The parser always runs to end-of-source. Partial trees are valid `SyntaxTree` values.
- **Preserve whitespace or comments.** Comments are `TokenKind.Comment` tokens consumed and discarded by `SkipTrivia()`. Whitespace is never tokenized (the lexer discards it silently). No trivia is attached to any AST node.

---

## Cross-References

| Topic | Document |
|-------|----------|
| Grammar EBNF (v1 baseline) | [docs/PreceptLanguageDesign.md](../../docs/PreceptLanguageDesign.md) § Grammar |
| Expression precedence (locked) | [docs.next/language/precept-language-spec.md](../language/precept-language-spec.md) § 2.1 |
| Interpolation token kinds and segment origin | [docs.next/compiler/literal-system.md](../compiler/literal-system.md) § Parser |
| `TokenKind` vocabulary (all token kinds) | `src/Precept.Next/Pipeline/TokenKind.cs` |
| `Token` record struct (span fields) | `src/Precept.Next/Pipeline/Token.cs` |
| `SyntaxTree` as pipeline artifact, consumer table | [docs.next/compiler/pipeline-artifacts-and-consumer-contracts.md](../compiler/pipeline-artifacts-and-consumer-contracts.md) § Pipeline Stages |
| `set` disambiguation rationale | [docs.next/compiler/lexer.md](../compiler/lexer.md) § Deliberate Exclusions |
| Pipeline stage ordering | [docs.next/compiler/pipeline-artifacts-and-consumer-contracts.md](../compiler/pipeline-artifacts-and-consumer-contracts.md) § Pipeline Stages |
| Diagnostic system | [docs.next/compiler/diagnostic-system.md](../compiler/diagnostic-system.md) |
| Original architecture planning notes | [docs.next/architecture-planning.md](../architecture-planning.md) § 2.2 |

---

## Source Files

| File | Purpose |
|------|---------|
| `src/Precept.Next/Pipeline/Parser.cs` | Parser implementation — `Parser` static class, `ParseSession` struct (~600 lines estimated) |
| `src/Precept.Next/Pipeline/SyntaxTree.cs` | `SyntaxTree` pipeline artifact — root node + diagnostics |
| `src/Precept.Next/Pipeline/SyntaxNodes.cs` | All `SyntaxNode` concrete types — declarations, statements, expressions, outcomes, type refs, modifiers, auxiliary nodes |
| `src/Precept.Next/Pipeline/SourceSpan.cs` | `SourceSpan` struct |
| `src/Precept.Next/Pipeline/TokenKind.cs` | `TokenKind` enum — token vocabulary consumed by the parser |
| `src/Precept.Next/Pipeline/DiagnosticCode.cs` | Parse-stage diagnostic codes (to be added alongside lexer codes) |
