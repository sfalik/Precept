using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace Precept;

/// <summary>
/// Superpower-based parser for the Precept DSL.
/// Converts source text → <see cref="PreceptDefinition"/> via token stream.
/// </summary>
public static class PreceptParser
{
    // ═══════════════════════════════════════════════════════════════════
    // Public API (signature unchanged from old parser)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Parses a <c>.precept</c> DSL text string into a <see cref="PreceptDefinition"/> record tree.
    /// Throws <see cref="InvalidOperationException"/> on syntax errors.
    /// </summary>
    public static PreceptDefinition Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("DSL input is empty.");

        TokenList<PreceptToken> tokens;
        try
        {
            tokens = PreceptTokenizerBuilder.Instance.Tokenize(text);
        }
        catch (Superpower.ParseException ex)
        {
            throw new InvalidOperationException($"Tokenization error: {ex.Message}", ex);
        }

        var result = FileParser.TryParse(tokens);
        if (!result.HasValue)
        {
            var pos = result.ErrorPosition;
            var line = pos.HasValue ? pos.Line : 0;
            var expectations = result.Expectations ?? [];
            var expectStr = expectations.Length > 0
                ? $" Expected: {string.Join(", ", expectations)}."
                : "";
            throw new InvalidOperationException(
                $"Line {line}: parse error.{expectStr} {result.ErrorMessage ?? ""}".TrimEnd());
        }

        return result.Value;
    }

    /// <summary>
    /// Parses with diagnostics — returns a model (or null) and a list of parse diagnostics.
    /// For use by the language server.
    /// </summary>
    public static (PreceptDefinition? Model, IReadOnlyList<ParseDiagnostic> Diagnostics) ParseWithDiagnostics(string text)
    {
        var diagnostics = new List<ParseDiagnostic>();

        if (string.IsNullOrWhiteSpace(text))
        {
            diagnostics.Add(new ParseDiagnostic(1, 0, "DSL input is empty."));
            return (null, diagnostics);
        }

        TokenList<PreceptToken> tokens;
        try
        {
            tokens = PreceptTokenizerBuilder.Instance.Tokenize(text);
        }
        catch (Superpower.ParseException ex)
        {
            diagnostics.Add(new ParseDiagnostic(1, 0, $"Tokenization error: {ex.Message}"));
            return (null, diagnostics);
        }

        var result = FileParser.TryParse(tokens);
        if (!result.HasValue)
        {
            var pos = result.ErrorPosition;
            var line = pos.HasValue ? pos.Line : 1;
            var col = pos.HasValue ? pos.Column : 0;
            diagnostics.Add(new ParseDiagnostic(line, col, result.ErrorMessage ?? "Parse error."));
            return (null, diagnostics);
        }

        return (result.Value, diagnostics);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Extracts the text value (identifier name) from a token span.</summary>
    private static string ToText(this Token<PreceptToken> token) => token.ToStringValue();

    /// <summary>Extracts the string literal value (unquoting) from a StringLiteral token.</summary>
    private static string ToStringLiteralValue(this Token<PreceptToken> token)
    {
        var raw = token.ToStringValue();
        // Remove surrounding quotes and unescape
        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
        {
            raw = raw[1..^1];
            raw = raw.Replace("\\\"", "\"")
                     .Replace("\\\\", "\\")
                     .Replace("\\n", "\n")
                     .Replace("\\r", "\r")
                     .Replace("\\t", "\t");
        }
        return raw;
    }

    private static double ToNumberValue(this Token<PreceptToken> token)
    {
        if (double.TryParse(token.ToStringValue(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            return v;
        throw new InvalidOperationException($"Invalid number literal: {token.ToStringValue()}");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Expression Combinators
    // ═══════════════════════════════════════════════════════════════════

    // Level 5: Atoms
    private static readonly TokenListParser<PreceptToken, PreceptExpression> NumberAtom =
        Token.EqualTo(PreceptToken.NumberLiteral)
            .Select(t => (PreceptExpression)new PreceptLiteralExpression(t.ToNumberValue()));

    private static readonly TokenListParser<PreceptToken, PreceptExpression> StringAtom =
        Token.EqualTo(PreceptToken.StringLiteral)
            .Select(t => (PreceptExpression)new PreceptLiteralExpression(t.ToStringLiteralValue()));

    private static readonly TokenListParser<PreceptToken, PreceptExpression> TrueAtom =
        Token.EqualTo(PreceptToken.True)
            .Value((PreceptExpression)new PreceptLiteralExpression(true));

    private static readonly TokenListParser<PreceptToken, PreceptExpression> FalseAtom =
        Token.EqualTo(PreceptToken.False)
            .Value((PreceptExpression)new PreceptLiteralExpression(false));

    private static readonly TokenListParser<PreceptToken, PreceptExpression> NullAtom =
        Token.EqualTo(PreceptToken.Null)
            .Value((PreceptExpression)new PreceptLiteralExpression(null));

    private static readonly TokenListParser<PreceptToken, PreceptExpression> DottedIdentifier =
        Token.EqualTo(PreceptToken.Identifier)
            .Then(id =>
                Token.EqualTo(PreceptToken.Dot)
                    .IgnoreThen(Token.EqualTo(PreceptToken.Identifier))
                    .Select(member => (PreceptExpression)new PreceptIdentifierExpression(id.ToText(), member.ToText()))
                .Try()
                .Or(Superpower.Parse.Return<PreceptToken, PreceptExpression>(
                    new PreceptIdentifierExpression(id.ToText()))));

    private static readonly TokenListParser<PreceptToken, PreceptExpression> ParenExpr =
        from _lp in Token.EqualTo(PreceptToken.LeftParen)
        from inner in Superpower.Parse.Ref(() => BoolExpr)
        from _rp in Token.EqualTo(PreceptToken.RightParen)
        select (PreceptExpression)new PreceptParenthesizedExpression(inner);

    private static readonly TokenListParser<PreceptToken, PreceptExpression> Atom =
        NumberAtom
            .Try().Or(StringAtom)
            .Try().Or(TrueAtom)
            .Try().Or(FalseAtom)
            .Try().Or(NullAtom)
            .Try().Or(ParenExpr)
            .Or(DottedIdentifier);

    // Level 4: Unary (! and unary -)
    private static readonly TokenListParser<PreceptToken, PreceptExpression> Unary =
        Token.EqualTo(PreceptToken.Not)
            .IgnoreThen(Superpower.Parse.Ref(() => Unary))
            .Select(expr => (PreceptExpression)new PreceptUnaryExpression("!", expr))
        .Try()
        .Or(
            Token.EqualTo(PreceptToken.Minus)
                .IgnoreThen(Superpower.Parse.Ref(() => Unary))
                .Select(expr => (PreceptExpression)new PreceptUnaryExpression("-", expr))
            .Try()
            .Or(Atom));

    // Level 3.5: Multiplicative (* / %)
    private static readonly TokenListParser<PreceptToken, PreceptExpression> Factor =
        Superpower.Parse.Chain(
            Token.EqualTo(PreceptToken.Star).Value("*")
                .Or(Token.EqualTo(PreceptToken.Slash).Value("/")),
            Unary,
            (op, left, right) => (PreceptExpression)new PreceptBinaryExpression(op, left, right));

    // Level 3: Additive (+ -)
    private static readonly TokenListParser<PreceptToken, PreceptExpression> Term =
        Superpower.Parse.Chain(
            Token.EqualTo(PreceptToken.Plus).Value("+")
                .Or(Token.EqualTo(PreceptToken.Minus).Value("-")),
            Factor,
            (op, left, right) => (PreceptExpression)new PreceptBinaryExpression(op, left, right));

    // Level 2: Comparison (==, !=, >, >=, <, <=, contains)
    private static readonly TokenListParser<PreceptToken, PreceptExpression> Comparison =
        Superpower.Parse.Chain(
            Token.EqualTo(PreceptToken.DoubleEquals).Value("==")
                .Try().Or(Token.EqualTo(PreceptToken.NotEquals).Value("!="))
                .Try().Or(Token.EqualTo(PreceptToken.GreaterThanOrEqual).Value(">="))
                .Try().Or(Token.EqualTo(PreceptToken.LessThanOrEqual).Value("<="))
                .Try().Or(Token.EqualTo(PreceptToken.GreaterThan).Value(">"))
                .Try().Or(Token.EqualTo(PreceptToken.LessThan).Value("<"))
                .Or(Token.EqualTo(PreceptToken.Contains).Value("contains")),
            Term,
            (op, left, right) => (PreceptExpression)new PreceptBinaryExpression(op, left, right));

    // Level 1.5: Logical AND (&&)
    private static readonly TokenListParser<PreceptToken, PreceptExpression> AndExpr =
        Superpower.Parse.Chain(
            Token.EqualTo(PreceptToken.And).Value("&&"),
            Comparison,
            (op, left, right) => (PreceptExpression)new PreceptBinaryExpression(op, left, right));

    // Level 1: Logical OR (||)
    private static readonly TokenListParser<PreceptToken, PreceptExpression> OrExpr =
        Superpower.Parse.Chain(
            Token.EqualTo(PreceptToken.Or).Value("||"),
            AndExpr,
            (op, left, right) => (PreceptExpression)new PreceptBinaryExpression(op, left, right));

    /// <summary>Full boolean expression parser.</summary>
    internal static readonly TokenListParser<PreceptToken, PreceptExpression> BoolExpr = OrExpr;

    // ═══════════════════════════════════════════════════════════════════
    // Expression text reconstruction (for ExpressionText fields)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reconstructs expression source text from a span of tokens.
    /// Used for populating ExpressionText fields.
    /// </summary>
    private static string ExtractSpanText(Token<PreceptToken> startToken, Token<PreceptToken> endToken, string source)
    {
        var start = startToken.Span.Position.Absolute;
        var end = endToken.Span.Position.Absolute + endToken.Span.Length;
        return source[start..end].Trim();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Type Combinators
    // ═══════════════════════════════════════════════════════════════════

    private static readonly TokenListParser<PreceptToken, PreceptScalarType> ScalarType =
        Token.EqualTo(PreceptToken.StringType).Value(PreceptScalarType.String)
            .Or(Token.EqualTo(PreceptToken.NumberType).Value(PreceptScalarType.Number))
            .Or(Token.EqualTo(PreceptToken.BooleanType).Value(PreceptScalarType.Boolean));

    /// <summary>
    /// Parses a type reference: scalar type or collection type.
    /// Collection types use "set of scalar", "queue of scalar", "stack of scalar".
    /// Dual-use 'set' keyword: after 'as' → if followed by 'of', it's a collection type.
    /// </summary>
    private static readonly TokenListParser<PreceptToken, (bool IsCollection, PreceptScalarType ScalarType, PreceptCollectionKind? CollectionKind)> TypeRef =
        // Collection types: set/queue/stack of <scalar>
        (from kw in Token.EqualTo(PreceptToken.Set).Value(PreceptCollectionKind.Set)
             .Or(Token.EqualTo(PreceptToken.Queue).Value(PreceptCollectionKind.Queue))
             .Or(Token.EqualTo(PreceptToken.Stack).Value(PreceptCollectionKind.Stack))
         from _ in Token.EqualTo(PreceptToken.Of)
         from inner in ScalarType
         select (IsCollection: true, ScalarType: inner, CollectionKind: (PreceptCollectionKind?)kw))
        .Try()
        .Or(ScalarType.Select(st => (IsCollection: false, ScalarType: st, CollectionKind: (PreceptCollectionKind?)null)));

    // ═══════════════════════════════════════════════════════════════════
    // Literal Parsers (for default values)
    // ═══════════════════════════════════════════════════════════════════

    private static readonly TokenListParser<PreceptToken, object?> ScalarLiteral =
        Token.EqualTo(PreceptToken.NumberLiteral).Select(t => (object?)t.ToNumberValue())
            .Try().Or(Token.EqualTo(PreceptToken.StringLiteral).Select(t => (object?)t.ToStringLiteralValue()))
            .Try().Or(Token.EqualTo(PreceptToken.True).Value((object?)true))
            .Try().Or(Token.EqualTo(PreceptToken.False).Value((object?)false))
            .Or(Token.EqualTo(PreceptToken.Null).Value((object?)null));

    private static readonly TokenListParser<PreceptToken, object?> ListLiteral =
        from _ in Token.EqualTo(PreceptToken.LeftBracket)
        from items in ScalarLiteral.ManyDelimitedBy(Token.EqualTo(PreceptToken.Comma))
        from _2 in Token.EqualTo(PreceptToken.RightBracket)
        select (object?)items.ToList();

    private static readonly TokenListParser<PreceptToken, object?> DefaultValue =
        ListLiteral.Try().Or(ScalarLiteral);

    // ═══════════════════════════════════════════════════════════════════
    // State Target Parser
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Parses a state target: 'any' | Name (',' Name)*
    /// Returns the list of state names. 'any' is represented as ["any"].
    /// </summary>
    private static readonly TokenListParser<PreceptToken, string[]> StateTarget =
        Token.EqualTo(PreceptToken.Any).Value(new[] { "any" })
        .Or(Token.EqualTo(PreceptToken.Identifier)
            .Select(t => t.ToText())
            .AtLeastOnceDelimitedBy(Token.EqualTo(PreceptToken.Comma)));

    // ═══════════════════════════════════════════════════════════════════
    // Outcome Parser
    // ═══════════════════════════════════════════════════════════════════

    private static readonly TokenListParser<PreceptToken, PreceptClauseOutcome> TransitionOutcome =
        Token.EqualTo(PreceptToken.Transition)
            .IgnoreThen(Token.EqualTo(PreceptToken.Identifier))
            .Select(t => (PreceptClauseOutcome)new PreceptStateTransition(t.ToText()));

    private static readonly TokenListParser<PreceptToken, PreceptClauseOutcome> NoTransitionOutcome =
        Token.EqualTo(PreceptToken.No)
            .IgnoreThen(Token.EqualTo(PreceptToken.Transition))
            .Value((PreceptClauseOutcome)new PreceptNoTransition());

    private static readonly TokenListParser<PreceptToken, PreceptClauseOutcome> RejectOutcome =
        Token.EqualTo(PreceptToken.Reject)
            .IgnoreThen(Token.EqualTo(PreceptToken.StringLiteral))
            .Select(t => (PreceptClauseOutcome)new PreceptRejection(t.ToStringLiteralValue()));

    private static readonly TokenListParser<PreceptToken, PreceptClauseOutcome> Outcome =
        NoTransitionOutcome.Try()
            .Or(TransitionOutcome)
            .Or(RejectOutcome);

    // ═══════════════════════════════════════════════════════════════════
    // Action Parsers (for -> pipeline)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Represents a single parsed action (either set assignment or collection mutation).</summary>
    private abstract record ParsedAction;
    private sealed record SetAction(PreceptSetAssignment Assignment) : ParsedAction;
    private sealed record CollectionAction(PreceptCollectionMutation Mutation) : ParsedAction;

    private static readonly TokenListParser<PreceptToken, ParsedAction> SetActionParser =
        from kw in Token.EqualTo(PreceptToken.Set)
        from field in Token.EqualTo(PreceptToken.Identifier)
        from eq in Token.EqualTo(PreceptToken.Assign)
        from expr in BoolExpr
        select (ParsedAction)new SetAction(new PreceptSetAssignment(
            field.ToText(),
            ReconstituteExpr(expr),
            expr));

    private static readonly TokenListParser<PreceptToken, ParsedAction> AddActionParser =
        from kw in Token.EqualTo(PreceptToken.Add)
        from field in Token.EqualTo(PreceptToken.Identifier)
        from expr in BoolExpr
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Add, field.ToText(), ReconstituteExpr(expr), expr));

    private static readonly TokenListParser<PreceptToken, ParsedAction> RemoveActionParser =
        from kw in Token.EqualTo(PreceptToken.Remove)
        from field in Token.EqualTo(PreceptToken.Identifier)
        from expr in BoolExpr
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Remove, field.ToText(), ReconstituteExpr(expr), expr));

    private static readonly TokenListParser<PreceptToken, ParsedAction> EnqueueActionParser =
        from kw in Token.EqualTo(PreceptToken.Enqueue)
        from field in Token.EqualTo(PreceptToken.Identifier)
        from expr in BoolExpr
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Enqueue, field.ToText(), ReconstituteExpr(expr), expr));

    private static readonly TokenListParser<PreceptToken, ParsedAction> DequeueActionParser =
        from kw in Token.EqualTo(PreceptToken.Dequeue)
        from field in Token.EqualTo(PreceptToken.Identifier)
        from intoField in Token.EqualTo(PreceptToken.Into)
            .IgnoreThen(Token.EqualTo(PreceptToken.Identifier))
            .Select(t => t.ToText())
            .OptionalOrDefault()
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Dequeue, field.ToText(), null, null, intoField));

    private static readonly TokenListParser<PreceptToken, ParsedAction> PushActionParser =
        from kw in Token.EqualTo(PreceptToken.Push)
        from field in Token.EqualTo(PreceptToken.Identifier)
        from expr in BoolExpr
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Push, field.ToText(), ReconstituteExpr(expr), expr));

    private static readonly TokenListParser<PreceptToken, ParsedAction> PopActionParser =
        from kw in Token.EqualTo(PreceptToken.Pop)
        from field in Token.EqualTo(PreceptToken.Identifier)
        from intoField in Token.EqualTo(PreceptToken.Into)
            .IgnoreThen(Token.EqualTo(PreceptToken.Identifier))
            .Select(t => t.ToText())
            .OptionalOrDefault()
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Pop, field.ToText(), null, null, intoField));

    private static readonly TokenListParser<PreceptToken, ParsedAction> ClearActionParser =
        from kw in Token.EqualTo(PreceptToken.Clear)
        from field in Token.EqualTo(PreceptToken.Identifier)
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Clear, field.ToText(), null, null));

    private static readonly TokenListParser<PreceptToken, ParsedAction> AnyAction =
        SetActionParser
            .Try().Or(AddActionParser)
            .Try().Or(RemoveActionParser)
            .Try().Or(EnqueueActionParser)
            .Try().Or(DequeueActionParser)
            .Try().Or(PushActionParser)
            .Try().Or(PopActionParser)
            .Or(ClearActionParser);

    // ═══════════════════════════════════════════════════════════════════
    // Expression text reconstitution
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reconstitutes expression text from a parsed AST. Used for ExpressionText fields.
    /// </summary>
    private static string ReconstituteExpr(PreceptExpression expr)
        => expr switch
        {
            PreceptLiteralExpression lit => lit.Value switch
            {
                null => "null",
                bool b => b ? "true" : "false",
                double d => d.ToString(CultureInfo.InvariantCulture),
                string s => $"\"{s}\"",
                _ => lit.Value.ToString() ?? "null"
            },
            PreceptIdentifierExpression id => id.Member is not null ? $"{id.Name}.{id.Member}" : id.Name,
            PreceptUnaryExpression un => $"{un.Operator}{ReconstituteExpr(un.Operand)}",
            PreceptBinaryExpression bin => $"{ReconstituteExpr(bin.Left)} {bin.Operator} {ReconstituteExpr(bin.Right)}",
            PreceptParenthesizedExpression paren => $"({ReconstituteExpr(paren.Inner)})",
            _ => expr.ToString() ?? ""
        };

    // ═══════════════════════════════════════════════════════════════════
    // Statement Declarations
    // ═══════════════════════════════════════════════════════════════════

    // precept <Name>
    private static readonly TokenListParser<PreceptToken, string> PreceptHeader =
        Token.EqualTo(PreceptToken.Precept)
            .IgnoreThen(Token.EqualTo(PreceptToken.Identifier))
            .Select(t => t.ToText())
            .Named("precept declaration");

    // field <Name> as <Type> [nullable] [default <Value>]
    private static readonly TokenListParser<PreceptToken, StatementResult> FieldDecl =
        (from kw in Token.EqualTo(PreceptToken.Field)
         from name in Token.EqualTo(PreceptToken.Identifier)
         from _ in Token.EqualTo(PreceptToken.As)
         from typeRef in TypeRef
         from nullable in Token.EqualTo(PreceptToken.Nullable).Value(true).OptionalOrDefault(false)
         from defaultVal in Token.EqualTo(PreceptToken.Default).IgnoreThen(DefaultValue).OptionalOrDefault()
         select typeRef.IsCollection
            ? (StatementResult)new CollectionFieldResult(new PreceptCollectionField(
                name.ToText(), typeRef.CollectionKind!.Value, typeRef.ScalarType))
            : new FieldResult(new PreceptField(
                name.ToText(), typeRef.ScalarType, nullable,
                defaultVal is not null || (nullable && defaultVal is null),
                defaultVal)))
        .Named("field declaration");

    // invariant <BoolExpr> because "reason"
    private static readonly TokenListParser<PreceptToken, StatementResult> InvariantDecl =
        (from kw in Token.EqualTo(PreceptToken.Invariant)
         from expr in BoolExpr
         from _ in Token.EqualTo(PreceptToken.Because)
         from reason in Token.EqualTo(PreceptToken.StringLiteral)
         select (StatementResult)new InvariantResult(new PreceptInvariant(
             ReconstituteExpr(expr), expr, reason.ToStringLiteralValue())))
        .Named("invariant declaration");

    // state <Name> [initial]
    private static readonly TokenListParser<PreceptToken, StatementResult> StateDecl =
        (from kw in Token.EqualTo(PreceptToken.State)
         from name in Token.EqualTo(PreceptToken.Identifier)
         from initial in Token.EqualTo(PreceptToken.Initial).Value(true).OptionalOrDefault(false)
         select (StatementResult)new StateResult(new PreceptState(name.ToText()), initial))
        .Named("state declaration");

    // event <Name> [with <ArgList>]
    // where ArgList = Name as Type [nullable] [default val] separated by commas
    private static readonly TokenListParser<PreceptToken, PreceptEventArg> EventArg =
        from name in Token.EqualTo(PreceptToken.Identifier)
        from _ in Token.EqualTo(PreceptToken.As)
        from type in ScalarType
        from nullable in Token.EqualTo(PreceptToken.Nullable).Value(true).OptionalOrDefault(false)
        from dflt in Token.EqualTo(PreceptToken.Default).IgnoreThen(ScalarLiteral).OptionalOrDefault()
        select new PreceptEventArg(name.ToText(), type, nullable, dflt is not null, dflt);

    private static readonly TokenListParser<PreceptToken, StatementResult> EventDecl =
        (from kw in Token.EqualTo(PreceptToken.Event)
         from name in Token.EqualTo(PreceptToken.Identifier)
         from args in Token.EqualTo(PreceptToken.With)
             .IgnoreThen(EventArg.AtLeastOnceDelimitedBy(Token.EqualTo(PreceptToken.Comma)))
             .OptionalOrDefault(Array.Empty<PreceptEventArg>())
         select (StatementResult)new EventResult(new PreceptEvent(
             name.ToText(), args.ToList())))
        .Named("event declaration");

    // ═══════════════════════════════════════════════════════════════════
    // Assert Statements
    // ═══════════════════════════════════════════════════════════════════

    // in/to/from <StateTarget> assert <BoolExpr> because "reason"
    private static readonly TokenListParser<PreceptToken, (PreceptAssertPreposition Prep, string[] States)> StateAssertPrefix =
        Token.EqualTo(PreceptToken.In).Value(PreceptAssertPreposition.In)
            .Or(Token.EqualTo(PreceptToken.To).Value(PreceptAssertPreposition.To))
            .Or(Token.EqualTo(PreceptToken.From).Value(PreceptAssertPreposition.From))
            .Then(prep => StateTarget.Select(states => (prep, states)));

    private static readonly TokenListParser<PreceptToken, StatementResult> StateAssertDecl =
        (from prefix in StateAssertPrefix
         from _ in Token.EqualTo(PreceptToken.Assert)
         from expr in BoolExpr
         from __ in Token.EqualTo(PreceptToken.Because)
         from reason in Token.EqualTo(PreceptToken.StringLiteral)
         select (StatementResult)new StateAssertResult(prefix.Prep, prefix.States,
             ReconstituteExpr(expr), expr, reason.ToStringLiteralValue()))
        .Named("state assert");

    // on <Event> assert <BoolExpr> because "reason"
    private static readonly TokenListParser<PreceptToken, StatementResult> EventAssertDecl =
        (from _ in Token.EqualTo(PreceptToken.On)
         from eventName in Token.EqualTo(PreceptToken.Identifier)
         from __ in Token.EqualTo(PreceptToken.Assert)
         from expr in BoolExpr
         from ___ in Token.EqualTo(PreceptToken.Because)
         from reason in Token.EqualTo(PreceptToken.StringLiteral)
         select (StatementResult)new EventAssertResult(new PreceptEventAssert(
             eventName.ToText(), ReconstituteExpr(expr), expr, reason.ToStringLiteralValue())))
        .Named("event assert");

    // ═══════════════════════════════════════════════════════════════════
    // State Entry/Exit Actions
    // ═══════════════════════════════════════════════════════════════════

    // to/from <StateTarget> -> <ActionChain>
    private static readonly TokenListParser<PreceptToken, (PreceptAssertPreposition Prep, string[] States)> StateActionPrefix =
        Token.EqualTo(PreceptToken.To).Value(PreceptAssertPreposition.To)
            .Or(Token.EqualTo(PreceptToken.From).Value(PreceptAssertPreposition.From))
            .Then(prep => StateTarget.Select(states => (prep, states)));

    private static readonly TokenListParser<PreceptToken, ParsedAction[]> ActionChain =
        Token.EqualTo(PreceptToken.Arrow)
            .IgnoreThen(AnyAction)
            .AtLeastOnce();

    private static readonly TokenListParser<PreceptToken, StatementResult> StateActionDecl =
        (from prefix in StateActionPrefix
         from actions in ActionChain
         where !actions.Any(a => a is SetAction sa && false) // placeholder — allow all actions
         select (StatementResult)new StateActionResult(prefix.Prep, prefix.States, actions))
        .Named("state entry/exit action");

    // ═══════════════════════════════════════════════════════════════════
    // Edit Declarations
    // ═══════════════════════════════════════════════════════════════════

    // in <StateTarget> edit <FieldList>
    private static readonly TokenListParser<PreceptToken, StatementResult> EditDecl =
        (from _ in Token.EqualTo(PreceptToken.In)
         from states in StateTarget
         from __ in Token.EqualTo(PreceptToken.Edit)
         from fields in Token.EqualTo(PreceptToken.Identifier)
             .Select(t => t.ToText())
             .AtLeastOnceDelimitedBy(Token.EqualTo(PreceptToken.Comma))
         select (StatementResult)new EditResult(states, fields))
        .Named("edit declaration");

    // ═══════════════════════════════════════════════════════════════════
    // Transition Rows
    // ═══════════════════════════════════════════════════════════════════

    // from <StateTarget> on <Event> [when <BoolExpr>] [-> <actions>]* -> <outcome>
    private static readonly TokenListParser<PreceptToken, StatementResult> TransitionRowParser =
        (from _ in Token.EqualTo(PreceptToken.From)
         from states in StateTarget
         from __ in Token.EqualTo(PreceptToken.On)
         from eventName in Token.EqualTo(PreceptToken.Identifier)
         from whenGuard in Token.EqualTo(PreceptToken.When)
             .IgnoreThen(BoolExpr)
             .OptionalOrDefault()
         from actionsAndOutcome in Token.EqualTo(PreceptToken.Arrow)
             .IgnoreThen(AnyAction.Try().Or(Outcome.Select(o => (ParsedAction)new OutcomeAction(o))))
             .AtLeastOnce()
         select (StatementResult)new TransitionRowResult(
             states, eventName.ToText(), whenGuard, actionsAndOutcome))
        .Named("transition row");

    /// <summary>Wraps an outcome as a parsed action for the unified action pipeline.</summary>
    private sealed record OutcomeAction(PreceptClauseOutcome Outcome) : ParsedAction;

    // ═══════════════════════════════════════════════════════════════════
    // Statement Union (all statement kinds)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Base type for parsed statement results before assembly into the model.</summary>
    private abstract record StatementResult;
    private sealed record FieldResult(PreceptField Field) : StatementResult;
    private sealed record CollectionFieldResult(PreceptCollectionField Field) : StatementResult;
    private sealed record InvariantResult(PreceptInvariant Invariant) : StatementResult;
    private sealed record StateResult(PreceptState State, bool IsInitial) : StatementResult;
    private sealed record EventResult(PreceptEvent Event) : StatementResult;
    private sealed record StateAssertResult(PreceptAssertPreposition Prep, string[] States,
        string ExprText, PreceptExpression Expr, string Reason) : StatementResult;
    private sealed record EventAssertResult(PreceptEventAssert Assert) : StatementResult;
    private sealed record StateActionResult(PreceptAssertPreposition Prep, string[] States,
        ParsedAction[] Actions) : StatementResult;
    private sealed record EditResult(string[] States, string[] Fields) : StatementResult;
    private sealed record TransitionRowResult(string[] States, string EventName,
        PreceptExpression? WhenGuard, ParsedAction[] ActionsAndOutcome) : StatementResult;

    /// <summary>Union parser: tries each statement kind in priority order.</summary>
    private static readonly TokenListParser<PreceptToken, StatementResult> Statement =
        // Order matters: more specific patterns before general ones
        // 'in ... edit' before 'in ... assert' (both start with In)
        EditDecl.Try()
        // Event assert: 'on <Event> assert'
        .Or(EventAssertDecl.Try())
        // State assert: 'in/to/from <State> assert'
        // Must come before state action and transition row
        .Or(StateAssertDecl.Try())
        // Transition row: 'from <State> on <Event> ...'
        // Must come before state action (both can start with from)
        .Or(TransitionRowParser.Try())
        // State action: 'to/from <State> -> ...'
        .Or(StateActionDecl.Try())
        // Simple declarations
        .Or(FieldDecl.Try())
        .Or(InvariantDecl.Try())
        .Or(StateDecl.Try())
        .Or(EventDecl);

    // ═══════════════════════════════════════════════════════════════════
    // File Parser (top-level)
    // ═══════════════════════════════════════════════════════════════════

    private static readonly TokenListParser<PreceptToken, PreceptDefinition> FileParser =
        from header in PreceptHeader
        from statements in Statement.Many()
        select AssembleModel(header, statements);

    /// <summary>
    /// Assembles individual parsed statements into a <see cref="PreceptDefinition"/>.
    /// Validates structural constraints (one initial state, no duplicates, etc.).
    /// </summary>
    private static PreceptDefinition AssembleModel(string name, StatementResult[] statements)
    {
        var states = new List<PreceptState>();
        PreceptState? initialState = null;
        var events = new List<PreceptEvent>();
        var fields = new List<PreceptField>();
        var collectionFields = new List<PreceptCollectionField>();
        var invariants = new List<PreceptInvariant>();
        var stateAsserts = new List<PreceptStateAssert>();
        var stateActions = new List<PreceptStateAction>();
        var eventAsserts = new List<PreceptEventAssert>();
        var transitionRows = new List<PreceptTransitionRow>();
        var editBlocks = new List<PreceptEditBlock>();

        // Also build old-style transitions for backward compat during migration
        var oldTransitions = new List<PreceptTransition>();
        var oldTopLevelRules = new List<PreceptRule>();

        foreach (var stmt in statements)
        {
            switch (stmt)
            {
                case FieldResult fr:
                    if (fields.Any(f => f.Name == fr.Field.Name) || collectionFields.Any(f => f.Name == fr.Field.Name))
                        throw new InvalidOperationException($"Duplicate field '{fr.Field.Name}'.");
                    fields.Add(fr.Field);
                    break;

                case CollectionFieldResult cfr:
                    if (fields.Any(f => f.Name == cfr.Field.Name) || collectionFields.Any(f => f.Name == cfr.Field.Name))
                        throw new InvalidOperationException($"Duplicate field '{cfr.Field.Name}'.");
                    collectionFields.Add(cfr.Field);
                    break;

                case InvariantResult ir:
                    invariants.Add(ir.Invariant);
                    // Also add as old-style top-level rule for backward compat
                    oldTopLevelRules.Add(new PreceptRule(
                        ir.Invariant.ExpressionText, ir.Invariant.Expression, ir.Invariant.Reason,
                        ir.Invariant.SourceLine, 0, 0, 0, 0));
                    break;

                case StateResult sr:
                    if (states.Any(s => s.Name == sr.State.Name))
                        throw new InvalidOperationException($"Duplicate state '{sr.State.Name}'.");
                    states.Add(sr.State);
                    if (sr.IsInitial)
                    {
                        if (initialState is not null)
                            throw new InvalidOperationException($"Duplicate initial state. '{initialState.Name}' is already marked initial.");
                        initialState = sr.State;
                    }
                    break;

                case EventResult er:
                    if (events.Any(e => e.Name == er.Event.Name))
                        throw new InvalidOperationException($"Duplicate event '{er.Event.Name}'.");
                    events.Add(er.Event);
                    break;

                case StateAssertResult sar:
                    ExpandStateTargets(sar.States, states).ForEach(stateName =>
                        stateAsserts.Add(new PreceptStateAssert(
                            sar.Prep, stateName, sar.ExprText, sar.Expr, sar.Reason)));
                    break;

                case EventAssertResult ear:
                    eventAsserts.Add(ear.Assert);
                    break;

                case StateActionResult sact:
                    ExpandStateTargets(sact.States, states).ForEach(stateName =>
                    {
                        var (sets, mutations) = SplitActions(sact.Actions);
                        stateActions.Add(new PreceptStateAction(
                            sact.Prep, stateName, sets,
                            mutations.Count > 0 ? mutations : null));
                    });
                    break;

                case EditResult edr:
                    ExpandStateTargets(edr.States, states).ForEach(stateName =>
                        editBlocks.Add(new PreceptEditBlock(stateName, edr.Fields.ToList())));
                    break;

                case TransitionRowResult trr:
                    var outcome = trr.ActionsAndOutcome.OfType<OutcomeAction>().LastOrDefault()?.Outcome;
                    if (outcome is null)
                        throw new InvalidOperationException($"Transition row for event '{trr.EventName}' is missing an outcome (transition, no transition, or reject).");

                    var rowActions = trr.ActionsAndOutcome.Where(a => a is not OutcomeAction).ToArray();
                    var (rowSets, rowMutations) = SplitActions(rowActions);
                    var whenText = trr.WhenGuard is not null ? ReconstituteExpr(trr.WhenGuard) : null;

                    ExpandStateTargets(trr.States, states).ForEach(stateName =>
                    {
                        transitionRows.Add(new PreceptTransitionRow(
                            stateName, trr.EventName, outcome, rowSets, 
                            rowMutations.Count > 0 ? rowMutations : null,
                            whenText, trr.WhenGuard));

                        // Build old-style transition for backward compat
                        BuildOldTransition(oldTransitions, stateName, trr.EventName,
                            outcome, rowSets, rowMutations, whenText, trr.WhenGuard);
                    });
                    break;
            }
        }

        if (states.Count == 0)
            throw new InvalidOperationException("At least one state must be declared.");
        if (initialState is null)
            throw new InvalidOperationException("Exactly one state must be marked initial. Use 'state <Name> initial'.");

        return new PreceptDefinition(
            name, states, initialState, events,
            oldTransitions,
            fields, collectionFields,
            oldTopLevelRules.Count > 0 ? oldTopLevelRules : null,
            invariants.Count > 0 ? invariants : null,
            stateAsserts.Count > 0 ? stateAsserts : null,
            stateActions.Count > 0 ? stateActions : null,
            eventAsserts.Count > 0 ? eventAsserts : null,
            transitionRows.Count > 0 ? transitionRows : null,
            editBlocks.Count > 0 ? editBlocks : null);
    }

    /// <summary>Expands 'any' to all declared state names, or returns the list as-is.</summary>
    private static List<string> ExpandStateTargets(string[] targets, List<PreceptState> declaredStates)
    {
        if (targets.Length == 1 && targets[0] == "any")
            return declaredStates.Select(s => s.Name).ToList();
        return targets.ToList();
    }

    /// <summary>Splits parsed actions into set assignments and collection mutations.</summary>
    private static (List<PreceptSetAssignment> Sets, List<PreceptCollectionMutation> Mutations) SplitActions(ParsedAction[] actions)
    {
        var sets = new List<PreceptSetAssignment>();
        var mutations = new List<PreceptCollectionMutation>();
        foreach (var action in actions)
        {
            switch (action)
            {
                case SetAction sa: sets.Add(sa.Assignment); break;
                case CollectionAction ca: mutations.Add(ca.Mutation); break;
            }
        }
        return (sets, mutations);
    }

    /// <summary>Adds a new-style transition row to the old-style transition list for backward compat.</summary>
    private static void BuildOldTransition(
        List<PreceptTransition> transitions,
        string fromState, string eventName,
        PreceptClauseOutcome outcome,
        List<PreceptSetAssignment> sets,
        List<PreceptCollectionMutation> mutations,
        string? whenText,
        PreceptExpression? whenGuard)
    {
        // Find or create the transition for this (state, event) pair
        var existing = transitions.FirstOrDefault(t => t.FromState == fromState && t.EventName == eventName);
        var clause = new PreceptClause(
            outcome, sets,
            Predicate: whenText,
            PredicateAst: whenGuard,
            CollectionMutations: mutations.Count > 0 ? mutations : null);

        if (existing is not null)
        {
            var idx = transitions.IndexOf(existing);
            var newClauses = existing.Clauses.Append(clause).ToList();
            transitions[idx] = existing with { Clauses = newClauses };
        }
        else
        {
            transitions.Add(new PreceptTransition(fromState, eventName, [clause]));
        }
    }
}

/// <summary>A parse diagnostic with position information.</summary>
public sealed record ParseDiagnostic(int Line, int Column, string Message);
