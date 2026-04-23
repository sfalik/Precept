using System.Collections.Immutable;

namespace Precept.Pipeline;

// ════════════════════════════════════════════════════════════════════════════════
//  AST Node Hierarchy — Precept v2 Parser
//
//  Design decisions: docs.next/compiler/parser.md (P1, P7a-c)
//
//  SyntaxNode (abstract)  ← SourceSpan + IsMissing
//  ├── Declaration        ← top-level precept body entries
//  ├── Statement          ← action steps inside transition rows / state actions
//  ├── Expression         ← leaf values, operators, literals, calls
//  ├── OutcomeNode        ← transition row outcomes
//  ├── TypeRef            ← field type annotations
//  ├── FieldModifier      ← field constraint/modifier suffixes
//  ├── InterpolationSegment ← text/expression segments in interpolated literals
//  └── (auxiliary nodes)  ← StateTarget, FieldTarget, StateEntry, ArgDeclaration
// ════════════════════════════════════════════════════════════════════════════════

// ── Base type ──────────────────────────────────────────────────────────────────

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

// ── Intermediate abstract types ────────────────────────────────────────────────

/// <summary>Top-level entries in the precept body.</summary>
public abstract record Declaration(SourceSpan Span) : SyntaxNode(Span);

/// <summary>Sub-statement units within declarations (action steps in transition rows and state actions).</summary>
public abstract record Statement(SourceSpan Span) : SyntaxNode(Span);

/// <summary>Expression nodes — leaf values, operators, literals, calls.</summary>
public abstract record Expression(SourceSpan Span) : SyntaxNode(Span);

// ── Root ───────────────────────────────────────────────────────────────────────

/// <summary>The precept file root. Contains the precept name and all top-level declarations.</summary>
public sealed record PreceptNode(
    SourceSpan                   Span,
    Token                        Name,
    ImmutableArray<Declaration>  Body
) : SyntaxNode(Span);

// ── Declarations ───────────────────────────────────────────────────────────────

/// <summary>
/// field Identifier ("," Identifier)* as TypeRef FieldModifier*
/// field Identifier as TypeRef "-&gt;" Expr ConstraintSuffix*
/// </summary>
public sealed record FieldDeclaration(
    SourceSpan                    Span,
    ImmutableArray<Token>         Names,
    TypeRef                       Type,
    ImmutableArray<FieldModifier> Modifiers,
    Expression?                   ComputedExpression
) : Declaration(Span);

/// <summary>state StateNameEntry ("," StateNameEntry)*</summary>
public sealed record StateDeclaration(
    SourceSpan                 Span,
    ImmutableArray<StateEntry> Entries
) : Declaration(Span);

/// <summary>event Identifier ("," Identifier)* ("with" ArgList)?</summary>
public sealed record EventDeclaration(
    SourceSpan                     Span,
    ImmutableArray<Token>          Names,
    ImmutableArray<ArgDeclaration> Args
) : Declaration(Span);

/// <summary>rule BoolExpr WhenOpt "because" StringExpr</summary>
public sealed record RuleDeclaration(
    SourceSpan  Span,
    Expression  Condition,
    Expression? Guard,
    Expression  Message
) : Declaration(Span);

/// <summary>
/// "edit" FieldTarget WhenOpt                              (root-level)
/// "in" StateTarget WhenOpt "edit" FieldTarget             (state-scoped)
/// </summary>
public sealed record EditDeclaration(
    SourceSpan   Span,
    StateTarget? StateScope,
    Expression?  Guard,
    FieldTarget  Fields
) : Declaration(Span);

/// <summary>"from" StateTarget "on" Identifier WhenOpt ActionChain? "-&gt;" Outcome</summary>
public sealed record TransitionRowDeclaration(
    SourceSpan                Span,
    StateTarget               FromStates,
    Token                     EventName,
    Expression?               Guard,
    ImmutableArray<Statement> Actions,
    OutcomeNode               Outcome
) : Declaration(Span);

/// <summary>"in"|"to"|"from" StateTarget "ensure" BoolExpr WhenOpt "because" StringExpr</summary>
public sealed record StateEnsureDeclaration(
    SourceSpan   Span,
    EnsureAnchor Anchor,
    StateTarget  States,
    Expression   Condition,
    Expression?  Guard,
    Expression   Message
) : Declaration(Span);

/// <summary>"on" Identifier "ensure" BoolExpr WhenOpt "because" StringExpr</summary>
public sealed record EventEnsureDeclaration(
    SourceSpan  Span,
    Token       EventName,
    Expression  Condition,
    Expression? Guard,
    Expression  Message
) : Declaration(Span);

/// <summary>"to"|"from" StateTarget ActionChain</summary>
public sealed record StateActionDeclaration(
    SourceSpan                Span,
    StateActionAnchor         Anchor,
    StateTarget               States,
    ImmutableArray<Statement> Actions
) : Declaration(Span);

public enum EnsureAnchor { In, To, From }
public enum StateActionAnchor { To, From }

// ── Statements (action steps) ──────────────────────────────────────────────────

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

// ── Outcomes ───────────────────────────────────────────────────────────────────

/// <summary>Outcome of a transition row — appears only inside TransitionRowDeclaration.</summary>
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

// ── Type references ────────────────────────────────────────────────────────────

/// <summary>Field type annotation.</summary>
public abstract record TypeRef(SourceSpan Span) : SyntaxNode(Span);

/// <summary>Primitive scalar type: string, number, integer, decimal, boolean, temporal, domain.</summary>
public sealed record ScalarTypeRef(
    SourceSpan Span, ScalarTypeKind Kind
) : TypeRef(Span);

/// <summary>Collection type: set of T, queue of T, stack of T.</summary>
public sealed record CollectionTypeRef(
    SourceSpan     Span,
    CollectionKind Kind,
    ScalarTypeRef  ElementType
) : TypeRef(Span);

/// <summary>choice("A", "B", ...)</summary>
public sealed record ChoiceTypeRef(
    SourceSpan                 Span,
    ImmutableArray<Expression> Choices
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

// ── Field modifiers ────────────────────────────────────────────────────────────

/// <summary>Modifier or constraint suffix on a field declaration.</summary>
public abstract record FieldModifier(SourceSpan Span) : SyntaxNode(Span);

public sealed record NullableModifier(SourceSpan Span)    : FieldModifier(Span);
public sealed record OrderedModifier(SourceSpan Span)     : FieldModifier(Span);
public sealed record NonnegativeModifier(SourceSpan Span) : FieldModifier(Span);
public sealed record PositiveModifier(SourceSpan Span)    : FieldModifier(Span);
public sealed record NonzeroModifier(SourceSpan Span)     : FieldModifier(Span);
public sealed record NotemptyModifier(SourceSpan Span)    : FieldModifier(Span);
public sealed record DefaultModifier(SourceSpan Span, Expression Value)    : FieldModifier(Span);
public sealed record MinModifier(SourceSpan Span, Expression Value)        : FieldModifier(Span);
public sealed record MaxModifier(SourceSpan Span, Expression Value)        : FieldModifier(Span);
public sealed record MinLengthModifier(SourceSpan Span, Expression Value)  : FieldModifier(Span);
public sealed record MaxLengthModifier(SourceSpan Span, Expression Value)  : FieldModifier(Span);
public sealed record MinCountModifier(SourceSpan Span, Expression Value)   : FieldModifier(Span);
public sealed record MaxCountModifier(SourceSpan Span, Expression Value)   : FieldModifier(Span);
public sealed record MaxPlacesModifier(SourceSpan Span, Expression Value)  : FieldModifier(Span);

// ── Expressions ────────────────────────────────────────────────────────────────

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

/// <summary>if Cond then Consequence else Alternative</summary>
public sealed record ConditionalExpression(
    SourceSpan Span, Expression Condition, Expression Consequence, Expression Alternative
) : Expression(Span);

/// <summary>Identifier.Member (event arg, collection member, field member).</summary>
public sealed record MemberAccessExpression(
    SourceSpan Span, Token Object, Token Member
) : Expression(Span);

/// <summary>FunctionName(Arg, ...) — min, max, round, clamp.</summary>
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
    SourceSpan                           Span,
    ImmutableArray<InterpolationSegment> Segments
) : Expression(Span);

/// <summary>A non-interpolated typed constant: single TypedConstant token.</summary>
public sealed record TypedConstantExpression(
    SourceSpan Span, Token Value
) : Expression(Span);

/// <summary>An interpolated typed constant: same reassembly pattern as string, different token kinds.</summary>
public sealed record InterpolatedTypedConstantExpression(
    SourceSpan                           Span,
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

public enum BinaryOp
{
    Or, And,
    Equal, NotEqual,
    Less, Greater, LessOrEqual, GreaterOrEqual,
    Plus, Minus, Star, Slash, Percent
}

public enum UnaryOp { Not, Negate }

// ── Interpolation segments ─────────────────────────────────────────────────────

/// <summary>One segment of an interpolated string or typed constant.</summary>
public abstract record InterpolationSegment(SourceSpan Span) : SyntaxNode(Span);

/// <summary>The literal text portion of an interpolated literal (Start/Middle/End token text).</summary>
public sealed record TextSegment(SourceSpan Span, Token Token) : InterpolationSegment(Span);

/// <summary>An embedded expression within { ... }.</summary>
public sealed record ExpressionSegment(SourceSpan Span, Expression Inner) : InterpolationSegment(Span);

// ── Auxiliary nodes ────────────────────────────────────────────────────────────

/// <summary>State target for from/to/in clauses: "any" or Identifier ("," Identifier)*.</summary>
public sealed record StateTarget(
    SourceSpan            Span,
    bool                  IsAny,
    ImmutableArray<Token> Names
) : SyntaxNode(Span);

/// <summary>Field target for edit declarations: "all" or Identifier ("," Identifier)*.</summary>
public sealed record FieldTarget(
    SourceSpan            Span,
    bool                  IsAll,
    ImmutableArray<Token> Names
) : SyntaxNode(Span);

/// <summary>A single entry in a state declaration: name + optional initial + v2 modifiers.</summary>
public sealed record StateEntry(
    SourceSpan                        Span,
    Token                             Name,
    bool                              IsInitial,
    ImmutableArray<StateModifierKind> Modifiers
) : SyntaxNode(Span);

/// <summary>An argument declaration in an event "with" clause.</summary>
public sealed record ArgDeclaration(
    SourceSpan                    Span,
    Token                         Name,
    TypeRef                       Type,
    ImmutableArray<FieldModifier> Modifiers
) : SyntaxNode(Span);

public enum StateModifierKind { Terminal, Required, Irreversible, Success, Warning, Error }
