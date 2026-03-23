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
    /// <summary>
    /// Forces static field initializers to run, populating <see cref="ConstructCatalog"/>
    /// with construct registrations. Safe to call multiple times.
    /// </summary>
    public static void EnsureInitialized()
        => System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(PreceptParser).TypeHandle);

    // ═══════════════════════════════════════════════════════════════════
    // Public API (signature unchanged from old parser)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Parses a <c>.precept</c> DSL text string into a <see cref="PreceptDefinition"/> record tree.
    /// Throws <see cref="InvalidOperationException"/> on syntax errors.
    /// </summary>
    public static PreceptDefinition Parse(string text)
    {
        // SYNC:CONSTRAINT:C1
        if (string.IsNullOrWhiteSpace(text))
            throw DiagnosticCatalog.C1.ToException();

        TokenList<PreceptToken> tokens;
        try
        {
            tokens = PreceptTokenizerBuilder.Instance.Tokenize(text);
        }
        catch (Superpower.ParseException ex)
        {
            // SYNC:CONSTRAINT:C2
            throw new ConstraintViolationException(DiagnosticCatalog.C2, DiagnosticCatalog.C2.FormatMessage(("message", ex.Message)));
        }
        var result = RawFileParser.TryParse(tokens);
        if (result.HasValue && result.Remainder.IsAtEnd)
            return AssembleModel(result.Value.Name, result.Value.Statements);

        // SYNC:CONSTRAINT:C3
        throw DiagnosticCatalog.C3.ToException();
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
            // SYNC:CONSTRAINT:C1
            diagnostics.Add(new ParseDiagnostic(1, 0, DiagnosticCatalog.C1.FormatMessage(), DiagnosticCatalog.ToDiagnosticCode(DiagnosticCatalog.C1.Id)));
            return (null, diagnostics);
        }

        TokenList<PreceptToken> tokens;
        try
        {
            tokens = PreceptTokenizerBuilder.Instance.Tokenize(text);
        }
        catch (Superpower.ParseException ex)
        {
            // SYNC:CONSTRAINT:C2
            diagnostics.Add(new ParseDiagnostic(1, 0, ex.Message, DiagnosticCatalog.ToDiagnosticCode(DiagnosticCatalog.C2.Id)));
            return (null, diagnostics);
        }

        var result = RawFileParser.TryParse(tokens);
        if (result.HasValue && result.Remainder.IsAtEnd)
        {
            try
            {
                var model = AssembleModel(result.Value.Name, result.Value.Statements);
                return (model, diagnostics);
            }
            catch (InvalidOperationException ex)
            {
                var code = ex is ConstraintViolationException cve
                    ? DiagnosticCatalog.ToDiagnosticCode(cve.Constraint.Id)
                    : null;
                diagnostics.Add(new ParseDiagnostic(1, 0, ex.Message, code));
                return (null, diagnostics);
            }
        }

        // Report parse failure with position info from Superpower
        var pos = result.ErrorPosition;
        var line = pos.HasValue ? pos.Line : 1;
        var col = pos.HasValue ? pos.Column : 0;
        var msg = result.ErrorMessage ?? "Unexpected input";
        var expectations = result.Expectations ?? [];

        // When the parser partially succeeded (HasValue=true but tokens remain),
        // Superpower's ErrorPosition points to position 0 — not useful.
        // Use the first unconsumed token's position instead for an accurate squiggle,
        // and build a more descriptive error message.
        if (result.HasValue && !result.Remainder.IsAtEnd)
        {
            var next = result.Remainder.First();
            line = next.Position.Line;
            col = next.Position.Column;
            var tokenText = next.ToStringValue();
            var display = PreceptTokenMeta.GetSymbol(next.Kind) ?? tokenText;
            msg = $"Unexpected '{display}' — could not parse as a valid statement";

            // Look up matching construct forms for the leading keyword
            var keyword = display;
            var matchingForms = ConstructCatalog.Constructs
                .Where(c => ConstructFormStartsWithKeyword(c.Form, keyword))
                .Select(c => c.Form)
                .ToList();

            // Peek ahead for additional context (e.g. "from Draft on SendForReview" missing "->")
            var remaining = result.Remainder.ToArray();
            if (next.Kind == PreceptToken.From && remaining.Length >= 4
                && remaining[1].Kind == PreceptToken.Identifier
                && remaining[2].Kind == PreceptToken.On
                && remaining[3].Kind == PreceptToken.Identifier)
            {
                var transitionForm = ConstructCatalog.Constructs
                    .FirstOrDefault(c => c.Name == "transition-row")?.Form
                    ?? "from <State> on <Event> [when <Guard>] -> <Action>* -> <Outcome>";
                // Looks like a transition row — check for missing arrow
                var hasArrow = remaining.Skip(4).Any(t => t.Kind == PreceptToken.Arrow);
                if (!hasArrow)
                    msg = $"Transition row starting with 'from {remaining[1].ToStringValue()} on {remaining[3].ToStringValue()}' is missing '->' action chain";
                else
                    msg = $"Could not parse transition row 'from {remaining[1].ToStringValue()} on {remaining[3].ToStringValue()}' — expected '{transitionForm}'";
            }
            else if (matchingForms.Count > 0)
            {
                msg += ". Expected: " + string.Join(" or ", matchingForms.Select(f => $"'{f}'"));
            }
        }

        if (expectations.Length > 0)
            msg += $" (expected {string.Join(" or ", expectations)})";
        diagnostics.Add(new ParseDiagnostic(line, col, msg));
        return (null, diagnostics);
    }

    /// <summary>
    /// Parses an expression string into a <see cref="PreceptExpression"/> tree.
    /// Used by the language server for expression analysis (null narrowing, type inference).
    /// Throws <see cref="InvalidOperationException"/> on parse failure.
    /// </summary>
    public static PreceptExpression ParseExpression(string expression)
    {
        var tokens = PreceptTokenizerBuilder.Instance.Tokenize(expression);
        var result = BoolExpr.TryParse(tokens);
        if (result.HasValue && result.Remainder.IsAtEnd)
            return result.Value;
        // SYNC:CONSTRAINT:C4
        throw DiagnosticCatalog.C4.ToException(("expression", expression));
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
        // SYNC:CONSTRAINT:C5
        throw DiagnosticCatalog.C5.ToException(("value", token.ToStringValue()));
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

    #pragma warning disable CS8603, CS8620

    private static readonly TokenListParser<PreceptToken, PreceptExpression> DottedIdentifier =
        Token.EqualTo(PreceptToken.Identifier)
            .Then(id =>
                Token.EqualTo(PreceptToken.Dot)
                    .IgnoreThen(Token.EqualTo(PreceptToken.Identifier))
                    .Select(member => (PreceptExpression)new PreceptIdentifierExpression(id.ToText(), member.ToText()))
                .Try()
                .Or(Superpower.Parse.Return<PreceptToken, PreceptExpression>(
                    new PreceptIdentifierExpression(id.ToText()))));

    private static TokenListParser<PreceptToken, PreceptExpression> BoolExprRef()
        => BoolExpr;

    private static TokenListParser<PreceptToken, PreceptExpression> UnaryRef()
        => Unary;

    private static readonly TokenListParser<PreceptToken, string?> OptionalIntoFieldParser =
        (from _into in Token.EqualTo(PreceptToken.Into)
         from identifier in Token.EqualTo(PreceptToken.Identifier)
         select (string?)identifier.ToText())
        .Try()
        .Or(Superpower.Parse.Return<PreceptToken, string?>(null));

    private static readonly TokenListParser<PreceptToken, PreceptExpression> ParenExpr =
        from _lp in Token.EqualTo(PreceptToken.LeftParen)
        from inner in Superpower.Parse.Ref(BoolExprRef)
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
            .IgnoreThen(Superpower.Parse.Ref(UnaryRef))
            .Select(expr => (PreceptExpression)new PreceptUnaryExpression("!", expr))
        .Try()
        .Or(
            Token.EqualTo(PreceptToken.Minus)
                .IgnoreThen(Superpower.Parse.Ref(UnaryRef))
                .Select(expr => (PreceptExpression)new PreceptUnaryExpression("-", expr))
            .Try()
            .Or(Atom));

    // Level 3.5: Multiplicative (* / %)
    private static readonly TokenListParser<PreceptToken, PreceptExpression> Factor =
        Superpower.Parse.Chain(
            Token.EqualTo(PreceptToken.Star).Value("*")
                .Or(Token.EqualTo(PreceptToken.Slash).Value("/"))
                .Or(Token.EqualTo(PreceptToken.Percent).Value("%")),
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

    /// <summary>
    /// Parses an optional 'default &lt;value&gt;' clause, returning a sentinel tuple
    /// to distinguish "not specified" from "default null".
    /// </summary>
    private static readonly TokenListParser<PreceptToken, (bool Specified, object? Value)> OptionalDefault =
        Token.EqualTo(PreceptToken.Default)
            .IgnoreThen(DefaultValue)
            .Select(v => (Specified: true, Value: v))
            .OptionalOrDefault((Specified: false, Value: (object?)null));

    private static readonly TokenListParser<PreceptToken, (bool Specified, object? Value)> OptionalScalarDefault =
        Token.EqualTo(PreceptToken.Default)
            .IgnoreThen(ScalarLiteral)
            .Select(v => (Specified: true, Value: v))
            .OptionalOrDefault((Specified: false, Value: (object?)null));

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
            .Select(t => (PreceptClauseOutcome)new StateTransition(t.ToText()));

    private static readonly TokenListParser<PreceptToken, PreceptClauseOutcome> NoTransitionOutcome =
        Token.EqualTo(PreceptToken.No)
            .IgnoreThen(Token.EqualTo(PreceptToken.Transition))
            .Value((PreceptClauseOutcome)new NoTransition());

    private static readonly TokenListParser<PreceptToken, PreceptClauseOutcome> RejectOutcome =
        Token.EqualTo(PreceptToken.Reject)
            .IgnoreThen(Token.EqualTo(PreceptToken.StringLiteral))
            .Select(t => (PreceptClauseOutcome)new Rejection(t.ToStringLiteralValue()));

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
            expr,
            SourceLine: kw.Span.Position.Line));

    private static readonly TokenListParser<PreceptToken, ParsedAction> AddActionParser =
        from kw in Token.EqualTo(PreceptToken.Add)
        from field in Token.EqualTo(PreceptToken.Identifier)
        from expr in BoolExpr
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Add, field.ToText(), ReconstituteExpr(expr), expr,
            SourceLine: kw.Span.Position.Line));

    private static readonly TokenListParser<PreceptToken, ParsedAction> RemoveActionParser =
        from kw in Token.EqualTo(PreceptToken.Remove)
        from field in Token.EqualTo(PreceptToken.Identifier)
        from expr in BoolExpr
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Remove, field.ToText(), ReconstituteExpr(expr), expr,
            SourceLine: kw.Span.Position.Line));

    private static readonly TokenListParser<PreceptToken, ParsedAction> EnqueueActionParser =
        from kw in Token.EqualTo(PreceptToken.Enqueue)
        from field in Token.EqualTo(PreceptToken.Identifier)
        from expr in BoolExpr
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Enqueue, field.ToText(), ReconstituteExpr(expr), expr,
            SourceLine: kw.Span.Position.Line));

    private static readonly TokenListParser<PreceptToken, ParsedAction> DequeueActionParser =
        from kw in Token.EqualTo(PreceptToken.Dequeue)
        from field in Token.EqualTo(PreceptToken.Identifier)
        from intoField in OptionalIntoFieldParser
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Dequeue, field.ToText(), null, null, intoField,
            SourceLine: kw.Span.Position.Line));

    private static readonly TokenListParser<PreceptToken, ParsedAction> PushActionParser =
        from kw in Token.EqualTo(PreceptToken.Push)
        from field in Token.EqualTo(PreceptToken.Identifier)
        from expr in BoolExpr
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Push, field.ToText(), ReconstituteExpr(expr), expr,
            SourceLine: kw.Span.Position.Line));

    private static readonly TokenListParser<PreceptToken, ParsedAction> PopActionParser =
        from kw in Token.EqualTo(PreceptToken.Pop)
        from field in Token.EqualTo(PreceptToken.Identifier)
        from intoField in OptionalIntoFieldParser
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Pop, field.ToText(), null, null, intoField,
            SourceLine: kw.Span.Position.Line));

    private static readonly TokenListParser<PreceptToken, ParsedAction> ClearActionParser =
        from kw in Token.EqualTo(PreceptToken.Clear)
        from field in Token.EqualTo(PreceptToken.Identifier)
        select (ParsedAction)new CollectionAction(new PreceptCollectionMutation(
            PreceptCollectionMutationVerb.Clear, field.ToText(), null, null,
            SourceLine: kw.Span.Position.Line));

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
            .Named("precept declaration")
            .Register(new ConstructInfo(
                "precept-header",
                "precept <Name>",
                "top-level",
                "Names the workflow",
                "precept BugTracker"));

    // field <Name> as <Type> [nullable] [default <Value>]
    private static readonly TokenListParser<PreceptToken, StatementResult> FieldDecl =
        (from kw in Token.EqualTo(PreceptToken.Field)
         from name in Token.EqualTo(PreceptToken.Identifier)
         from _ in Token.EqualTo(PreceptToken.As)
         from typeRef in TypeRef
         from nullable in Token.EqualTo(PreceptToken.Nullable).Value(true).OptionalOrDefault(false)
         from dflt in OptionalDefault
         select typeRef.IsCollection
            ? (StatementResult)new CollectionFieldResult(new PreceptCollectionField(
                name.ToText(), typeRef.CollectionKind!.Value, typeRef.ScalarType))
            : new FieldResult(new PreceptField(
                name.ToText(), typeRef.ScalarType, nullable,
                dflt.Specified || nullable,
                dflt.Specified ? dflt.Value : null)))
        .Named("field declaration")
            .Register(new ConstructInfo(
                "field-declaration",
                "field <Name> as <Type> [nullable] [default <Value>]",
                "top-level",
                "Declares a scalar or collection data field",
                "field Priority as number default 3"));

    // invariant <BoolExpr> because "reason"
    private static readonly TokenListParser<PreceptToken, StatementResult> InvariantDecl =
        (from kw in Token.EqualTo(PreceptToken.Invariant)
         from expr in BoolExpr
         from _ in Token.EqualTo(PreceptToken.Because)
         from reason in Token.EqualTo(PreceptToken.StringLiteral)
         select (StatementResult)new InvariantResult(new PreceptInvariant(
             ReconstituteExpr(expr), expr, reason.ToStringLiteralValue(),
             SourceLine: kw.Span.Position.Line)))
        .Named("invariant declaration")
            .Register(new ConstructInfo(
                "invariant",
                "invariant <Expr> because \"<Reason>\"",
                "top-level",
                "Global data constraint checked after every mutation",
                "invariant Priority >= 1 because \"Priority must be positive\""));

    // state <Name> [initial]
    private static readonly TokenListParser<PreceptToken, StatementResult> StateDecl =
        (from kw in Token.EqualTo(PreceptToken.State)
         from name in Token.EqualTo(PreceptToken.Identifier)
         from initial in Token.EqualTo(PreceptToken.Initial).Value(true).OptionalOrDefault(false)
         select (StatementResult)new StateResult(new PreceptState(name.ToText()), initial))
        .Named("state declaration")
            .Register(new ConstructInfo(
                "state-declaration",
                "state <Name> [initial]",
                "top-level",
                "Declares a workflow state",
                "state Idle initial"));

    // event <Name> [with <ArgList>]
    // where ArgList = Name as Type [nullable] [default val] separated by commas
    private static readonly TokenListParser<PreceptToken, PreceptEventArg> EventArg =
        from name in Token.EqualTo(PreceptToken.Identifier)
        from _ in Token.EqualTo(PreceptToken.As)
        from type in ScalarType
        from nullable in Token.EqualTo(PreceptToken.Nullable).Value(true).OptionalOrDefault(false)
        from dflt in OptionalScalarDefault
        select new PreceptEventArg(name.ToText(), type, nullable,
            dflt.Specified,
            dflt.Specified ? dflt.Value : null);

    private static readonly TokenListParser<PreceptToken, StatementResult> EventDecl =
        (from kw in Token.EqualTo(PreceptToken.Event)
         from name in Token.EqualTo(PreceptToken.Identifier)
         from args in Token.EqualTo(PreceptToken.With)
             .IgnoreThen(EventArg.AtLeastOnceDelimitedBy(Token.EqualTo(PreceptToken.Comma)))
             .OptionalOrDefault(Array.Empty<PreceptEventArg>())
         select (StatementResult)new EventResult(new PreceptEvent(
             name.ToText(), args.ToList())))
        .Named("event declaration")
            .Register(new ConstructInfo(
                "event-declaration",
                "event <Name> [with <Arg> as <Type> [nullable] [default <Val>], ...]",
                "top-level",
                "Declares an external trigger with optional typed arguments",
                "event Submit with Comment as string"));

    // ═══════════════════════════════════════════════════════════════════
    // Assert Statements
    // ═══════════════════════════════════════════════════════════════════

    // in/to/from <StateTarget> assert <BoolExpr> because "reason"
    private static readonly TokenListParser<PreceptToken, StatementResult> StateAssertDecl =
        (from kw in Token.EqualTo(PreceptToken.In)
                         .Or(Token.EqualTo(PreceptToken.To))
                         .Or(Token.EqualTo(PreceptToken.From))
         from states in StateTarget
         from _ in Token.EqualTo(PreceptToken.Assert)
         from expr in BoolExpr
         from __ in Token.EqualTo(PreceptToken.Because)
         from reason in Token.EqualTo(PreceptToken.StringLiteral)
         select (StatementResult)new StateAssertResult(
             kw.Kind == PreceptToken.In ? AssertAnchor.In :
             kw.Kind == PreceptToken.To ? AssertAnchor.To :
             AssertAnchor.From,
             states,
             ReconstituteExpr(expr), expr, reason.ToStringLiteralValue(),
             SourceLine: kw.Span.Position.Line))
        .Named("state assert")
            .Register(new ConstructInfo(
                "state-assert",
                "in|to|from <State> assert <Expr> because \"<Reason>\"",
                "top-level",
                "State-scoped data constraint checked on entry, exit, or residency",
                "in Open assert Assignee != null because \"Must have an assignee\""));

    // on <Event> assert <BoolExpr> because "reason"
    private static readonly TokenListParser<PreceptToken, StatementResult> EventAssertDecl =
        (from kwOn in Token.EqualTo(PreceptToken.On)
         from eventName in Token.EqualTo(PreceptToken.Identifier)
         from _ in Token.EqualTo(PreceptToken.Assert)
         from expr in BoolExpr
         from __ in Token.EqualTo(PreceptToken.Because)
         from reason in Token.EqualTo(PreceptToken.StringLiteral)
         select (StatementResult)new EventAssertResult(new EventAssertion(
             eventName.ToText(), ReconstituteExpr(expr), expr, reason.ToStringLiteralValue(),
             SourceLine: kwOn.Span.Position.Line)))
        .Named("event assert")
            .Register(new ConstructInfo(
                "event-assert",
                "on <Event> assert <Expr> because \"<Reason>\"",
                "top-level",
                "Event-scoped argument constraint checked before firing",
                "on Submit assert Comment != \"\" because \"Comment required\""));

    // ═══════════════════════════════════════════════════════════════════
    // State Entry/Exit Actions
    // ═══════════════════════════════════════════════════════════════════

    // to/from <StateTarget> -> <ActionChain>
    private static readonly TokenListParser<PreceptToken, (AssertAnchor Prep, string[] States)> StateActionPrefix =
        Token.EqualTo(PreceptToken.To).Value(AssertAnchor.To)
            .Or(Token.EqualTo(PreceptToken.From).Value(AssertAnchor.From))
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
        .Named("state entry/exit action")
            .Register(new ConstructInfo(
                "state-action",
                "to|from <State> -> <Action> [-> <Action>]*",
                "top-level",
                "Automatic data mutations on state entry or exit",
                "to Closed -> set Resolution = null"));

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
        .Named("edit declaration")
            .Register(new ConstructInfo(
                "edit-declaration",
                "in <State> edit <Field>, ...",
                "top-level",
                "Declares which fields are editable in a state",
                "in Open edit Priority"));

    // ═══════════════════════════════════════════════════════════════════
    // Transition Rows
    // ═══════════════════════════════════════════════════════════════════

    private static readonly TokenListParser<PreceptToken, PreceptExpression?> OptionalWhenGuardParser =
        (from _when in Token.EqualTo(PreceptToken.When)
         from expr in BoolExpr
         select (PreceptExpression?)expr)
        .Try()
        .Or(Superpower.Parse.Return<PreceptToken, PreceptExpression?>(null));

    // from <StateTarget> on <Event> [when <BoolExpr>] [-> <actions>]* -> <outcome>
    private static readonly TokenListParser<PreceptToken, StatementResult> TransitionRowParser =
        (from kwFrom in Token.EqualTo(PreceptToken.From)
         from states in StateTarget
         from __ in Token.EqualTo(PreceptToken.On)
         from eventName in Token.EqualTo(PreceptToken.Identifier)
         from whenGuard in OptionalWhenGuardParser
         from actionsAndOutcome in Token.EqualTo(PreceptToken.Arrow)
             .IgnoreThen(AnyAction.Try().Or(Outcome.Select(o => (ParsedAction)new OutcomeAction(o))))
             .AtLeastOnce()
         select (StatementResult)new TransitionRowResult(
             states, eventName.ToText(), whenGuard, actionsAndOutcome,
             SourceLine: kwFrom.Span.Position.Line))
        .Named("transition row")
            .Register(new ConstructInfo(
                "transition-row",
                "from <State> on <Event> [when <Guard>] -> <Action>* -> <Outcome>",
                "top-level",
                "Maps a (state, event) pair to actions and an outcome",
                "from Open on Submit -> transition Closed"));

    #pragma warning restore CS8603, CS8620

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
    private sealed record StateAssertResult(AssertAnchor Prep, string[] States,
        string ExprText, PreceptExpression Expr, string Reason, int SourceLine = 0) : StatementResult;
    private sealed record EventAssertResult(EventAssertion Assert) : StatementResult;
    private sealed record StateActionResult(AssertAnchor Prep, string[] States,
        ParsedAction[] Actions) : StatementResult;
    private sealed record EditResult(string[] States, string[] Fields) : StatementResult;
    private sealed record TransitionRowResult(string[] States, string EventName,
        PreceptExpression? WhenGuard, ParsedAction[] ActionsAndOutcome, int SourceLine = 0) : StatementResult;

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

    /// <summary>
    /// Raw file parser: captures header + statements without validation.
    /// AssembleModel is called separately after confirming IsAtEnd, so partial
    /// parses of old-syntax files can fall through to the legacy parser.
    /// </summary>
    private static readonly TokenListParser<PreceptToken, (string Name, StatementResult[] Statements)> RawFileParser =
        from header in PreceptHeader
        from statements in Statement.Many()
        select (header, statements);

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
        var stateAsserts = new List<StateAssertion>();
        var stateActions = new List<PreceptStateAction>();
        var eventAsserts = new List<EventAssertion>();
        var transitionRows = new List<PreceptTransitionRow>();
        var editBlocks = new List<PreceptEditBlock>();

        foreach (var stmt in statements)
        {
            switch (stmt)
            {
                case FieldResult fr:
                    if (fields.Any(f => f.Name == fr.Field.Name) || collectionFields.Any(f => f.Name == fr.Field.Name))
                        // SYNC:CONSTRAINT:C6
                        throw DiagnosticCatalog.C6.ToException(("fieldName", fr.Field.Name));
                    fields.Add(fr.Field);
                    break;

                case CollectionFieldResult cfr:
                    if (fields.Any(f => f.Name == cfr.Field.Name) || collectionFields.Any(f => f.Name == cfr.Field.Name))
                        // SYNC:CONSTRAINT:C6
                        throw DiagnosticCatalog.C6.ToException(("fieldName", cfr.Field.Name));
                    collectionFields.Add(cfr.Field);
                    break;

                case InvariantResult ir:
                    invariants.Add(ir.Invariant);
                    break;

                case StateResult sr:
                    if (states.Any(s => s.Name == sr.State.Name))
                        // SYNC:CONSTRAINT:C7
                        throw DiagnosticCatalog.C7.ToException(("stateName", sr.State.Name));
                    states.Add(sr.State);
                    if (sr.IsInitial)
                    {
                        if (initialState is not null)
                            // SYNC:CONSTRAINT:C8
                            throw DiagnosticCatalog.C8.ToException(("stateName", initialState.Name));
                        initialState = sr.State;
                    }
                    break;

                case EventResult er:
                    if (events.Any(e => e.Name == er.Event.Name))
                        // SYNC:CONSTRAINT:C9
                        throw DiagnosticCatalog.C9.ToException(("eventName", er.Event.Name));
                    events.Add(er.Event);
                    break;

                case StateAssertResult sar:
                    ExpandStateTargets(sar.States, states).ForEach(stateName =>
                        stateAsserts.Add(new StateAssertion(
                            sar.Prep, stateName, sar.ExprText, sar.Expr, sar.Reason, sar.SourceLine)));
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
                    var outcomes = trr.ActionsAndOutcome.OfType<OutcomeAction>().ToList();
                    if (outcomes.Count == 0)
                        // SYNC:CONSTRAINT:C10
                        throw DiagnosticCatalog.C10.ToException(("eventName", trr.EventName));

                    // Design: "exactly one outcome, at the end; no statements after it"
                    if (outcomes.Count > 1)
                        // SYNC:CONSTRAINT:C11
                        throw DiagnosticCatalog.C11.ToException(("eventName", trr.EventName));
                    var firstOutcomeIdx = Array.IndexOf(trr.ActionsAndOutcome, outcomes[0]);
                    if (firstOutcomeIdx < trr.ActionsAndOutcome.Length - 1)
                        // SYNC:CONSTRAINT:C11
                        throw DiagnosticCatalog.C11.ToException(("eventName", trr.EventName));

                    var outcome = outcomes[0].Outcome;
                    var rowActions = trr.ActionsAndOutcome.Where(a => a is not OutcomeAction).ToArray();
                    var (rowSets, rowMutations) = SplitActions(rowActions);
                    var whenText = trr.WhenGuard is not null ? ReconstituteExpr(trr.WhenGuard) : null;

                    ExpandStateTargets(trr.States, states).ForEach(stateName =>
                    {
                        transitionRows.Add(new PreceptTransitionRow(
                            stateName, trr.EventName, outcome, rowSets, 
                            rowMutations.Count > 0 ? rowMutations : null,
                            whenText, trr.WhenGuard, trr.SourceLine));
                    });
                    break;
            }
        }

        if (states.Count == 0)
            // SYNC:CONSTRAINT:C12
            throw DiagnosticCatalog.C12.ToException();
        if (initialState is null)
            // SYNC:CONSTRAINT:C13
            throw DiagnosticCatalog.C13.ToException();

        // Validate event assert scope: expressions may only reference event argument identifiers
        foreach (var ea in eventAsserts)
        {
            var evt = events.FirstOrDefault(e => e.Name == ea.EventName);
            if (evt is null) continue;
            var argNames = evt.Args.Select(a => a.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var id in CollectIdentifiers(ea.Expression))
            {
                if (id.Member is not null)
                {
                    // EventName.ArgName form: prefix must be the event name, member must be an arg
                    if (!StringComparer.Ordinal.Equals(id.Name, ea.EventName))
                        // SYNC:CONSTRAINT:C14
                        throw DiagnosticCatalog.C14.ToException(("eventName", ea.EventName), ("prefix", id.Name), ("member", id.Member));
                    if (!argNames.Contains(id.Member))
                        // SYNC:CONSTRAINT:C15
                        throw DiagnosticCatalog.C15.ToException(("eventName", ea.EventName), ("member", id.Member));
                }
                else if (!argNames.Contains(id.Name))
                {
                    // SYNC:CONSTRAINT:C16
                    throw DiagnosticCatalog.C16.ToException(("eventName", ea.EventName), ("identifier", id.Name));
                }
            }
        }

        // Validate field defaults: non-nullable fields must have a default; default type must match
        foreach (var f in fields)
        {
            if (!f.IsNullable && !f.HasDefaultValue)
                // SYNC:CONSTRAINT:C17
                throw DiagnosticCatalog.C17.ToException(("fieldName", f.Name));
            if (f.HasDefaultValue && f.DefaultValue is not null)
            {
                var ok = f.Type switch
                {
                    PreceptScalarType.Number => f.DefaultValue is double,
                    PreceptScalarType.String => f.DefaultValue is string,
                    PreceptScalarType.Boolean => f.DefaultValue is bool,
                    _ => true
                };
                if (!ok)
                    // SYNC:CONSTRAINT:C18
                    throw DiagnosticCatalog.C18.ToException(("fieldName", f.Name), ("fieldType", f.Type));
            }
            if (f.HasDefaultValue && f.DefaultValue is null && !f.IsNullable)
                // SYNC:CONSTRAINT:C19
                throw DiagnosticCatalog.C19.ToException(("fieldName", f.Name), ("fieldType", f.Type));
        }

        // Validate event arg defaults: type mismatch / null on non-nullable
        foreach (var evt in events)
        {
            foreach (var arg in evt.Args)
            {
                if (arg.HasDefaultValue && arg.DefaultValue is not null)
                {
                    var ok = arg.Type switch
                    {
                        PreceptScalarType.Number => arg.DefaultValue is double,
                        PreceptScalarType.String => arg.DefaultValue is string,
                        PreceptScalarType.Boolean => arg.DefaultValue is bool,
                        _ => true
                    };
                    if (!ok)
                        // SYNC:CONSTRAINT:C20
                        throw DiagnosticCatalog.C20.ToException(("argName", arg.Name), ("argType", arg.Type));
                }
                if (arg.HasDefaultValue && arg.DefaultValue is null && !arg.IsNullable)
                    // SYNC:CONSTRAINT:C21
                    throw DiagnosticCatalog.C21.ToException(("argName", arg.Name), ("argType", arg.Type));
            }
        }

        // Validate collection mutation verb-vs-kind and unknown collections
        var collectionMap = collectionFields.ToDictionary(c => c.Name, c => c.CollectionKind, StringComparer.Ordinal);
        foreach (var row in transitionRows)
        {
            if (row.CollectionMutations is null) continue;
            foreach (var mut in row.CollectionMutations)
            {
                if (!collectionMap.TryGetValue(mut.TargetField, out var kind))
                {
                    if (fields.Any(f => f.Name == mut.TargetField))
                        // SYNC:CONSTRAINT:C22
                        throw DiagnosticCatalog.C22.ToException(("verb", mut.Verb.ToString().ToLowerInvariant()), ("fieldName", mut.TargetField));
                    // SYNC:CONSTRAINT:C23
                    throw DiagnosticCatalog.C23.ToException(("verb", mut.Verb.ToString().ToLowerInvariant()), ("fieldName", mut.TargetField));
                }
                var verbValid = (mut.Verb, kind) switch
                {
                    (PreceptCollectionMutationVerb.Add, PreceptCollectionKind.Set) => true,
                    (PreceptCollectionMutationVerb.Remove, PreceptCollectionKind.Set) => true,
                    (PreceptCollectionMutationVerb.Enqueue, PreceptCollectionKind.Queue) => true,
                    (PreceptCollectionMutationVerb.Dequeue, PreceptCollectionKind.Queue) => true,
                    (PreceptCollectionMutationVerb.Push, PreceptCollectionKind.Stack) => true,
                    (PreceptCollectionMutationVerb.Pop, PreceptCollectionKind.Stack) => true,
                    (PreceptCollectionMutationVerb.Clear, _) => true,
                    _ => false
                };
                if (!verbValid)
                    // SYNC:CONSTRAINT:C24
                    throw DiagnosticCatalog.C24.ToException(("verb", mut.Verb.ToString().ToLowerInvariant()), ("collectionKind", kind.ToString().ToLowerInvariant()), ("fieldName", mut.TargetField));
            }
        }

        // Validate unreachable rows: an unguarded row for (state, event) followed by another row
        var seenUnguarded = new HashSet<(string State, string Event)>(
            EqualityComparer<(string, string)>.Create(
                (a, b) => StringComparer.Ordinal.Equals(a.Item1, b.Item1) && StringComparer.Ordinal.Equals(a.Item2, b.Item2),
                h => HashCode.Combine(StringComparer.Ordinal.GetHashCode(h.Item1), StringComparer.Ordinal.GetHashCode(h.Item2))));
        foreach (var row in transitionRows)
        {
            var key = (row.FromState, row.EventName);
            if (seenUnguarded.Contains(key))
                // SYNC:CONSTRAINT:C25
                throw DiagnosticCatalog.C25.ToException(("fromState", row.FromState), ("eventName", row.EventName));
            if (row.WhenGuard is null)
                seenUnguarded.Add(key);
        }

        return new PreceptDefinition(
            name, states, initialState, events,
            fields, collectionFields,
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

    /// <summary>Collects all identifier references from an expression tree.</summary>
    private static IEnumerable<PreceptIdentifierExpression> CollectIdentifiers(PreceptExpression expr) =>
        expr switch
        {
            PreceptIdentifierExpression id => [id],
            PreceptUnaryExpression un => CollectIdentifiers(un.Operand),
            PreceptBinaryExpression bin => CollectIdentifiers(bin.Left).Concat(CollectIdentifiers(bin.Right)),
            PreceptParenthesizedExpression par => CollectIdentifiers(par.Inner),
            _ => []
        };

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

    /// <summary>
    /// Checks if a construct form starts with the given keyword.
    /// Handles forms like "in|to|from ..." where any alternative matches.
    /// </summary>
    private static bool ConstructFormStartsWithKeyword(string form, string keyword)
    {
        var spaceIdx = form.IndexOf(' ');
        var firstGroup = spaceIdx >= 0 ? form[..spaceIdx] : form;
        return firstGroup.Split('|').Any(k =>
            string.Equals(k, keyword, StringComparison.OrdinalIgnoreCase));
    }

}

/// <summary>A parse diagnostic with position information.</summary>
public sealed record ParseDiagnostic(int Line, int Column, string Message, string? Code = null);
