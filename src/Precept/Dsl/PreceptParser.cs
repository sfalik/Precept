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
            return AssembleModel(result.Value.Header.Name, result.Value.Header.SourceLine, result.Value.Statements);

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
            var errPos = ex.ErrorPosition;
            diagnostics.Add(new ParseDiagnostic(
                errPos.HasValue ? errPos.Line : 1,
                errPos.HasValue ? errPos.Column : 0,
                ex.Message,
                DiagnosticCatalog.ToDiagnosticCode(DiagnosticCatalog.C2.Id)));
            return (null, diagnostics);
        }

        Superpower.Model.TokenListParserResult<PreceptToken, ((string Name, int SourceLine) Header, StatementResult[] Statements)> result;
        try
        {
            result = RawFileParser.TryParse(tokens);
        }
        catch (ConstraintViolationException cve)
        {
            // ConstraintViolationException may be thrown from modifier extraction during parsing
            // (e.g. C70 duplicate modifier detection in BuildFieldResult/BuildEventArgResult).
            var cveLine = cve.SourceLine > 0 ? cve.SourceLine : 1;
            diagnostics.Add(new ParseDiagnostic(cveLine, 0, cve.Message, DiagnosticCatalog.ToDiagnosticCode(cve.Constraint.Id)));
            return (null, diagnostics);
        }

        if (result.HasValue && result.Remainder.IsAtEnd)
        {
            try
            {
                var model = AssembleModel(result.Value.Header.Name, result.Value.Header.SourceLine, result.Value.Statements);
                return (model, diagnostics);
            }
            catch (InvalidOperationException ex)
            {
                string? code = null;
                int sourceLine = 1;
                if (ex is ConstraintViolationException cve)
                {
                    code = DiagnosticCatalog.ToDiagnosticCode(cve.Constraint.Id);
                    if (cve.SourceLine > 0) sourceLine = cve.SourceLine;
                }
                diagnostics.Add(new ParseDiagnostic(sourceLine, 0, ex.Message, code));
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
            else if (next.Kind == PreceptToken.If)
            {
                msg = "'if' is a value expression, not a statement. To conditionally apply a transition row, use 'when' as a guard: from <State> on <Event> when <Condition> -> ...";
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

    /// <summary>
    /// Returns either a <see cref="long"/> (for whole-number literals like <c>42</c>) or
    /// a <see cref="double"/> (for decimal/scientific literals like <c>3.14</c>, <c>1e5</c>).
    /// The caller is responsible for updating <see cref="PreceptLiteralExpression"/> accordingly.
    /// </summary>
    private static object ToNumericLiteralValue(this Token<PreceptToken> token)
    {
        var text = token.ToStringValue();
        // Whole-number literal → long (integer type in the DSL)
        if (!text.Contains('.') && !text.Contains('e', StringComparison.OrdinalIgnoreCase))
        {
            if (long.TryParse(text, out var longVal))
                return longVal;
        }
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            return d;
        // SYNC:CONSTRAINT:C5
        throw DiagnosticCatalog.C5.ToException(("value", text));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Expression Combinators
    // ═══════════════════════════════════════════════════════════════════

    // Level 5: Atoms
    private static readonly TokenListParser<PreceptToken, PreceptExpression> NumberAtom =
        Token.EqualTo(PreceptToken.NumberLiteral)
            .Select(t => (PreceptExpression)new PreceptLiteralExpression(t.ToNumericLiteralValue()));

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

    // Member tokens: identifiers plus 'min'/'max' which are keywords but also valid
    // dotted member names (e.g. Tags.min, Tags.max on set fields).
    private static readonly TokenListParser<PreceptToken, Token<PreceptToken>> AnyMemberToken =
        Token.EqualTo(PreceptToken.Identifier)
            .Try().Or(Token.EqualTo(PreceptToken.Min))
            .Try().Or(Token.EqualTo(PreceptToken.Max));

    private static readonly TokenListParser<PreceptToken, PreceptExpression> DottedIdentifier =
        Token.EqualTo(PreceptToken.Identifier)
            .Then(id =>
                Token.EqualTo(PreceptToken.Dot)
                    .IgnoreThen(AnyMemberToken)
                    .Then(member =>
                        Token.EqualTo(PreceptToken.Dot)
                            .IgnoreThen(Token.EqualTo(PreceptToken.Identifier))
                            .Select(subMember => (PreceptExpression)new PreceptIdentifierExpression(id.ToText(), member.ToText(), subMember.ToText()))
                        .Try()
                        .Or(Superpower.Parse.Return<PreceptToken, PreceptExpression>(
                            new PreceptIdentifierExpression(id.ToText(), member.ToText()))))
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

    // Conditional expression: if <condition> then <value> else <value>
    private static readonly TokenListParser<PreceptToken, PreceptExpression> ConditionalExpr =
        (from _if in Token.EqualTo(PreceptToken.If)
         from condition in Superpower.Parse.Ref(BoolExprRef)
         from _then in Token.EqualTo(PreceptToken.Then)
         from thenBranch in Superpower.Parse.Ref(BoolExprRef)
         from _else in Token.EqualTo(PreceptToken.Else)
         from elseBranch in Superpower.Parse.Ref(BoolExprRef)
         select (PreceptExpression)new PreceptConditionalExpression(condition, thenBranch, elseBranch))
        .Register(new ConstructInfo(
            "conditional-expression",
            "if <condition> then <value> else <value>",
            "expression",
            "Selects between two values based on a boolean condition. Valid in set RHS, invariant, assert, and guard expressions. Nesting via parentheses: if A then (if B then 1 else 2) else 3.",
            "from Idle on Apply -> set Label = if Priority > 5 then \"urgent\" else \"normal\" -> no transition"));

    // Built-in function call: FunctionName(expr, expr, ...)
    // Function names are identifiers validated against the FunctionRegistry,
    // plus keyword tokens (min/max) that also serve as function names.
    // This generalizes the AnyMemberToken pattern for function-call position.
    private static readonly TokenListParser<PreceptToken, string> AnyFunctionName =
        Token.EqualTo(PreceptToken.Identifier).Where(t => FunctionRegistry.IsFunction(t.ToStringValue())).Select(t => t.ToStringValue())
            .Try().Or(Token.EqualTo(PreceptToken.Min).Value("min"))
            .Try().Or(Token.EqualTo(PreceptToken.Max).Value("max"));

    private static readonly TokenListParser<PreceptToken, PreceptExpression> FunctionCallAtom =
        (from name in AnyFunctionName
         from _lp in Token.EqualTo(PreceptToken.LeftParen)
         from firstArg in Superpower.Parse.Ref(BoolExprRef)
         from restArgs in (
             from _c in Token.EqualTo(PreceptToken.Comma)
             from arg in Superpower.Parse.Ref(BoolExprRef)
             select arg
         ).Many()
         from _rp in Token.EqualTo(PreceptToken.RightParen)
         select (PreceptExpression)new PreceptFunctionCallExpression(
             name,
             new[] { firstArg }.Concat(restArgs).ToArray()))
        .Try()
        .Register(new ConstructInfo(
            "function-call",
            "round (<Expr>, ...)",
            "expression",
            "Calls a built-in function. 18 functions available: abs, ceil, clamp, endsWith, floor, left, max, mid, min, pow, right, round, sqrt, startsWith, toLower, toUpper, trim, truncate. Valid in set RHS and invariant/assert expressions.",
            "from Idle on Apply -> set Rate = round(Apply.Amount, 2) -> no transition"));

    private static readonly TokenListParser<PreceptToken, PreceptExpression> Atom =
        NumberAtom
            .Try().Or(StringAtom)
            .Try().Or(TrueAtom)
            .Try().Or(FalseAtom)
            .Try().Or(NullAtom)
            .Try().Or(ParenExpr)
            .Try().Or(ConditionalExpr)
            .Try().Or(FunctionCallAtom)
            .Or(DottedIdentifier);

    // Level 4: Unary (! and unary -)
    private static readonly TokenListParser<PreceptToken, PreceptExpression> Unary =
        Token.EqualTo(PreceptToken.Not)
            .IgnoreThen(Superpower.Parse.Ref(UnaryRef))
            .Select(expr => (PreceptExpression)new PreceptUnaryExpression("not", expr))
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

    // Level 1.5: Logical AND (and)
    private static readonly TokenListParser<PreceptToken, PreceptExpression> AndExpr =
        Superpower.Parse.Chain(
            Token.EqualTo(PreceptToken.And).Value("and"),
            Comparison,
            (op, left, right) => (PreceptExpression)new PreceptBinaryExpression(op, left, right));

    // Level 1: Logical OR (or)
    private static readonly TokenListParser<PreceptToken, PreceptExpression> OrExpr =
        Superpower.Parse.Chain(
            Token.EqualTo(PreceptToken.Or).Value("or"),
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
            .Or(Token.EqualTo(PreceptToken.BooleanType).Value(PreceptScalarType.Boolean))
            .Or(Token.EqualTo(PreceptToken.IntegerType).Value(PreceptScalarType.Integer))
            .Or(Token.EqualTo(PreceptToken.DecimalType).Value(PreceptScalarType.Decimal));

    /// <summary>
    /// Parses a scalar type or choice("A","B","C") reference.
    /// Returns the scalar type and (for choice) the list of allowed values.
    /// </summary>
    private static readonly TokenListParser<PreceptToken, (PreceptScalarType Type, IReadOnlyList<string>? ChoiceValues)> ScalarTypeOrChoice =
        (from _ in Token.EqualTo(PreceptToken.ChoiceType)
         from _lp in Token.EqualTo(PreceptToken.LeftParen)
         from values in Token.EqualTo(PreceptToken.StringLiteral)
                            .Select(t => t.ToStringLiteralValue())
                            .AtLeastOnceDelimitedBy(Token.EqualTo(PreceptToken.Comma))
         from _rp in Token.EqualTo(PreceptToken.RightParen)
         select (PreceptScalarType.Choice, (IReadOnlyList<string>?)(IReadOnlyList<string>)values.ToList()))
        .Try()
        .Or(ScalarType.Select(st => (st, (IReadOnlyList<string>?)null)));

    /// <summary>
    /// Parses a type reference: scalar type, choice type, or collection type.
    /// Collection types use "set of scalar", "queue of scalar", "stack of scalar".
    /// Dual-use 'set' keyword: after 'as' → if followed by 'of', it's a collection type.
    /// </summary>
    private static readonly TokenListParser<PreceptToken, (bool IsCollection, PreceptScalarType ScalarType, PreceptCollectionKind? CollectionKind, IReadOnlyList<string>? ChoiceValues)> TypeRef =
        // Collection types: set/queue/stack of <scalar-or-choice>
        (from kw in Token.EqualTo(PreceptToken.Set).Value(PreceptCollectionKind.Set)
             .Or(Token.EqualTo(PreceptToken.Queue).Value(PreceptCollectionKind.Queue))
             .Or(Token.EqualTo(PreceptToken.Stack).Value(PreceptCollectionKind.Stack))
         from _ in Token.EqualTo(PreceptToken.Of)
         from inner in ScalarTypeOrChoice
         select (IsCollection: true, ScalarType: inner.Type, CollectionKind: (PreceptCollectionKind?)kw, ChoiceValues: inner.ChoiceValues))
        .Try()
        .Or(ScalarTypeOrChoice.Select(st => (IsCollection: false, ScalarType: st.Type, CollectionKind: (PreceptCollectionKind?)null, ChoiceValues: st.ChoiceValues)));

    // ═══════════════════════════════════════════════════════════════════
    // Literal Parsers (for default values)
    // ═══════════════════════════════════════════════════════════════════

    private static readonly TokenListParser<PreceptToken, object?> ScalarLiteral =
        (from _ in Token.EqualTo(PreceptToken.Minus)
         from n in Token.EqualTo(PreceptToken.NumberLiteral)
         let raw = n.ToNumericLiteralValue()
         select (object?)(raw is long l ? (object?)-l : -(double)(raw))).Try()
        .Or(Token.EqualTo(PreceptToken.NumberLiteral).Select(t => (object?)t.ToNumericLiteralValue()))
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

    /// <summary>
    /// Parses a single field/arg constraint suffix keyword (with optional numeric argument).
    /// Used as <c>ConstraintSuffix.Many()</c> after the default clause.
    /// </summary>
    private static readonly TokenListParser<PreceptToken, FieldConstraint> ConstraintSuffix =
        Token.EqualTo(PreceptToken.Nonnegative).Value((FieldConstraint)new FieldConstraint.Nonnegative()).Try()
        .Or(Token.EqualTo(PreceptToken.Positive).Value((FieldConstraint)new FieldConstraint.Positive()).Try())
        .Or(Token.EqualTo(PreceptToken.Notempty).Value((FieldConstraint)new FieldConstraint.Notempty()).Try())
        .Or((from _ in Token.EqualTo(PreceptToken.Min)
             from n in Token.EqualTo(PreceptToken.NumberLiteral)
             select (FieldConstraint)new FieldConstraint.Min(n.ToNumberValue())).Try())
        .Or((from _ in Token.EqualTo(PreceptToken.Max)
             from n in Token.EqualTo(PreceptToken.NumberLiteral)
             select (FieldConstraint)new FieldConstraint.Max(n.ToNumberValue())).Try())
        .Or((from _ in Token.EqualTo(PreceptToken.Minlength)
             from n in Token.EqualTo(PreceptToken.NumberLiteral)
             select (FieldConstraint)new FieldConstraint.Minlength((int)n.ToNumberValue())).Try())
        .Or((from _ in Token.EqualTo(PreceptToken.Maxlength)
             from n in Token.EqualTo(PreceptToken.NumberLiteral)
             select (FieldConstraint)new FieldConstraint.Maxlength((int)n.ToNumberValue())).Try())
        .Or((from _ in Token.EqualTo(PreceptToken.Mincount)
             from n in Token.EqualTo(PreceptToken.NumberLiteral)
             select (FieldConstraint)new FieldConstraint.Mincount((int)n.ToNumberValue())).Try())
        .Or((from _ in Token.EqualTo(PreceptToken.Maxcount)
             from n in Token.EqualTo(PreceptToken.NumberLiteral)
             select (FieldConstraint)new FieldConstraint.Maxcount((int)n.ToNumberValue())).Try())
        .Or(from _ in Token.EqualTo(PreceptToken.Maxplaces)
            from n in Token.EqualTo(PreceptToken.NumberLiteral)
            select (FieldConstraint)new FieldConstraint.Maxplaces((int)n.ToNumberValue()));

    // ═══════════════════════════════════════════════════════════════════
    // Unified Field Modifier Parser (any-order modifiers)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Discriminated union for any modifier that can follow a type reference.
    /// Parsed via <c>.Many()</c> to allow any-order specification.
    /// </summary>
    private abstract record FieldModifier;
    private sealed record NullableModifier : FieldModifier;
    private sealed record DefaultModifier(object? Value) : FieldModifier;
    private sealed record ConstraintModifier(FieldConstraint Constraint) : FieldModifier;
    private sealed record OrderedModifier : FieldModifier;
    private sealed record DerivedModifier(PreceptExpression Expression) : FieldModifier;

    /// <summary>
    /// Parses a single field modifier (nullable, default, constraint, or ordered) for field declarations.
    /// Uses <see cref="DefaultValue"/> which supports both scalar and list literals.
    /// </summary>
    private static readonly TokenListParser<PreceptToken, FieldModifier> AnyFieldModifier =
        Token.EqualTo(PreceptToken.Nullable).Value((FieldModifier)new NullableModifier()).Try()
        .Or(Token.EqualTo(PreceptToken.Default)
            .IgnoreThen(DefaultValue)
            .Select(v => (FieldModifier)new DefaultModifier(v)).Try())
        .Or(Token.EqualTo(PreceptToken.Arrow)
            .IgnoreThen(BoolExpr)
            .Select(expr => (FieldModifier)new DerivedModifier(expr)).Try())
        .Or(ConstraintSuffix.Select(c => (FieldModifier)new ConstraintModifier(c)).Try())
        .Or(Token.EqualTo(PreceptToken.Ordered).Value((FieldModifier)new OrderedModifier()));

    /// <summary>
    /// Parses a single field modifier for event argument declarations.
    /// Uses <see cref="ScalarLiteral"/> (no list literals for event args).
    /// </summary>
    private static readonly TokenListParser<PreceptToken, FieldModifier> AnyEventArgModifier =
        Token.EqualTo(PreceptToken.Nullable).Value((FieldModifier)new NullableModifier()).Try()
        .Or(Token.EqualTo(PreceptToken.Default)
            .IgnoreThen(ScalarLiteral)
            .Select(v => (FieldModifier)new DefaultModifier(v)).Try())
        .Or(ConstraintSuffix.Select(c => (FieldModifier)new ConstraintModifier(c)).Try())
        .Or(Token.EqualTo(PreceptToken.Ordered).Value((FieldModifier)new OrderedModifier()));

    /// <summary>
    /// Extracts typed modifier properties from a flat array of <see cref="FieldModifier"/> values.
    /// Detects duplicate modifiers and throws <see cref="ConstraintViolationException"/> (C70).
    /// </summary>
    private static (bool IsNullable, bool HasDefault, object? DefaultValue, FieldConstraint[] Constraints, bool IsOrdered, PreceptExpression? DerivedExpr)
        ExtractModifiers(FieldModifier[] modifiers, string firstName, int sourceLine)
    {
        // SYNC:CONSTRAINT:C70
        if (modifiers.OfType<NullableModifier>().Count() > 1)
            throw DiagnosticCatalog.C70.ToException(sourceLine, ("modifier", "nullable"), ("name", firstName));
        if (modifiers.OfType<DefaultModifier>().Count() > 1)
            throw DiagnosticCatalog.C70.ToException(sourceLine, ("modifier", "default"), ("name", firstName));
        if (modifiers.OfType<OrderedModifier>().Count() > 1)
            throw DiagnosticCatalog.C70.ToException(sourceLine, ("modifier", "ordered"), ("name", firstName));
        if (modifiers.OfType<DerivedModifier>().Count() > 1)
            throw DiagnosticCatalog.C70.ToException(sourceLine, ("modifier", "->"), ("name", firstName));

        var isNullable = modifiers.OfType<NullableModifier>().Any();
        var dflt = modifiers.OfType<DefaultModifier>().FirstOrDefault();
        var constraints = modifiers.OfType<ConstraintModifier>().Select(m => m.Constraint).ToArray();
        var isOrdered = modifiers.OfType<OrderedModifier>().Any();
        var derived = modifiers.OfType<DerivedModifier>().FirstOrDefault();
        return (isNullable, dflt is not null, dflt?.Value, constraints, isOrdered, derived?.Expression);
    }

    /// <summary>
    /// Builds a <see cref="StatementResult"/> from parsed field declaration components.
    /// Called from the <see cref="FieldDecl"/> combinator's select clause.
    /// </summary>
    private static StatementResult BuildFieldResult(
        Token<PreceptToken> kw,
        Token<PreceptToken>[] names,
        (bool IsCollection, PreceptScalarType ScalarType, PreceptCollectionKind? CollectionKind, IReadOnlyList<string>? ChoiceValues) typeRef,
        FieldModifier[] modifiers)
    {
        int sourceLine = kw.Span.Position.Line;
        var (nullable, hasDefault, defaultValue, constraints, ordered, derivedExpr) = ExtractModifiers(modifiers, names[0].ToText(), sourceLine);

        if (derivedExpr is not null)
        {
            // SYNC:CONSTRAINT:C82 — multi-name + derived
            if (names.Length > 1)
                throw DiagnosticCatalog.C82.ToException(sourceLine);
            // SYNC:CONSTRAINT:C80 — default + derived mutual exclusion
            if (hasDefault)
                throw DiagnosticCatalog.C80.ToException(sourceLine, ("fieldName", names[0].ToText()));
            // SYNC:CONSTRAINT:C81 — nullable + derived mutual exclusion
            if (nullable)
                throw DiagnosticCatalog.C81.ToException(sourceLine, ("fieldName", names[0].ToText()));
        }

        if (typeRef.IsCollection)
            return new CollectionFieldResult(
                names.Select(n => new PreceptCollectionField(
                    n.ToText(), typeRef.CollectionKind!.Value, typeRef.ScalarType,
                    constraints.Length > 0 ? constraints : null,
                    typeRef.ChoiceValues,
                    SourceLine: sourceLine)).ToArray());

        return new FieldResult(
            names.Select(n => new PreceptField(
                n.ToText(), typeRef.ScalarType, nullable,
                hasDefault || nullable,
                hasDefault ? defaultValue : null,
                constraints.Length > 0 ? constraints : null,
                typeRef.ChoiceValues, ordered,
                SourceLine: sourceLine,
                DerivedExpression: derivedExpr,
                DerivedExpressionText: derivedExpr is not null ? ReconstituteExpr(derivedExpr) : null)).ToArray());
    }

    /// <summary>
    /// Builds a <see cref="PreceptEventArg"/> from parsed event argument components.
    /// Called from the <see cref="EventArg"/> combinator's select clause.
    /// </summary>
    private static PreceptEventArg BuildEventArgResult(
        Token<PreceptToken> name,
        (PreceptScalarType Type, IReadOnlyList<string>? ChoiceValues) typeRef,
        FieldModifier[] modifiers)
    {
        var (nullable, hasDefault, defaultValue, constraints, ordered, _) = ExtractModifiers(modifiers, name.ToText(), name.Span.Position.Line);
        return new PreceptEventArg(name.ToText(), typeRef.Type, nullable,
            hasDefault,
            hasDefault ? defaultValue : null,
            constraints.Length > 0 ? constraints : null,
            typeRef.ChoiceValues, ordered,
            SourceLine: name.Span.Position.Line);
    }

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

    /// <summary>
    /// Parses a field target: 'all' | Name (',' Name)*
    /// Returns the field names. 'all' is represented as ["all"].
    /// </summary>
    private static readonly TokenListParser<PreceptToken, string[]> FieldTarget =
        Token.EqualTo(PreceptToken.All).Value(new[] { "all" })
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
                long l => l.ToString(CultureInfo.InvariantCulture),
                double d => d.ToString(CultureInfo.InvariantCulture),
                string s => $"\"{s}\"",
                _ => lit.Value.ToString() ?? "null"
            },
            PreceptIdentifierExpression id => id.SubMember is not null
                ? $"{id.Name}.{id.Member}.{id.SubMember}"
                : id.Member is not null ? $"{id.Name}.{id.Member}" : id.Name,
            PreceptUnaryExpression un => $"{un.Operator}{ReconstituteExpr(un.Operand)}",
            PreceptBinaryExpression bin => $"{ReconstituteExpr(bin.Left)} {bin.Operator} {ReconstituteExpr(bin.Right)}",
            PreceptParenthesizedExpression paren => $"({ReconstituteExpr(paren.Inner)})",
            PreceptFunctionCallExpression fn => $"{fn.Name}({string.Join(", ", fn.Arguments.Select(ReconstituteExpr))})",
            PreceptConditionalExpression cond => $"if {ReconstituteExpr(cond.Condition)} then {ReconstituteExpr(cond.ThenBranch)} else {ReconstituteExpr(cond.ElseBranch)}",
            _ => expr.ToString() ?? ""
        };

    // ═══════════════════════════════════════════════════════════════════
    // Statement Declarations
    // ═══════════════════════════════════════════════════════════════════

    // precept <Name>
    private static readonly TokenListParser<PreceptToken, (string Name, int SourceLine)> PreceptHeader =
        (from kw in Token.EqualTo(PreceptToken.Precept)
         from name in Token.EqualTo(PreceptToken.Identifier)
         select (name.ToText(), kw.Span.Position.Line))
            .Named("precept declaration")
            .Register(new ConstructInfo(
                "precept-header",
                "precept <Name>",
                "top-level",
                "Names the workflow",
                "precept BugTracker"));

    // field <Name>[, <Name>, ...] as <Type> [modifier...] (nullable, default, -> expr, constraints, ordered — any order)
    private static readonly TokenListParser<PreceptToken, StatementResult> FieldDecl =
        (from kw in Token.EqualTo(PreceptToken.Field)
         from names in Token.EqualTo(PreceptToken.Identifier)
             .AtLeastOnceDelimitedBy(Token.EqualTo(PreceptToken.Comma))
         from _ in Token.EqualTo(PreceptToken.As)
         from typeRef in TypeRef
         from modifiers in AnyFieldModifier.Many()
         select BuildFieldResult(kw, names, typeRef, modifiers))
        .Named("field declaration")
            .Register(new ConstructInfo(
                "field-declaration",
                "field <Name>[, <Name>, ...] as <Type> [<modifier>...] | field <Name> as <Type> -> <Expr> [<constraint>...]",
                "top-level",
                "Declares a scalar, collection, or computed data field",
                "field Priority as number default 3"));

    // invariant <BoolExpr> because "reason"
    private static readonly TokenListParser<PreceptToken, StatementResult> InvariantDecl =
        (from kw in Token.EqualTo(PreceptToken.Invariant)
         from expr in BoolExpr
         from whenGuard in OptionalWhenGuardParser
         from _ in Token.EqualTo(PreceptToken.Because)
         from reason in Token.EqualTo(PreceptToken.StringLiteral)
         select (StatementResult)new InvariantResult(new PreceptInvariant(
             ReconstituteExpr(expr), expr, reason.ToStringLiteralValue(),
             SourceLine: kw.Span.Position.Line,
             WhenText: whenGuard is not null ? ReconstituteExpr(whenGuard) : null,
             WhenGuard: whenGuard)))
        .Named("invariant declaration")
            .Register(new ConstructInfo(
                "invariant",
                "invariant <Expr> [when <Guard>] because \"<Reason>\"",
                "top-level",
                "Global data constraint checked after every mutation",
                "invariant Priority >= 1 because \"Priority must be positive\""));

    // state <Name> [initial] (, <Name> [initial])*
    private sealed record StateNameEntry(string Name, bool IsInitial, int Line, int Column);

    private static readonly TokenListParser<PreceptToken, StateNameEntry> StateNameEntryParser =
        from name in Token.EqualTo(PreceptToken.Identifier)
        from initial in Token.EqualTo(PreceptToken.Initial).Value(true).OptionalOrDefault(false)
        select new StateNameEntry(name.ToText(), initial, name.Span.Position.Line, name.Span.Position.Column);

    private static readonly TokenListParser<PreceptToken, StatementResult> StateDecl =
        (from kw in Token.EqualTo(PreceptToken.State)
         from entries in StateNameEntryParser.AtLeastOnceDelimitedBy(Token.EqualTo(PreceptToken.Comma))
         select (StatementResult)new StateResult(
             entries.Select(e => new PreceptState(e.Name, kw.Span.Position.Line, e.Column)).ToArray(),
             entries.Select(e => e.IsInitial).ToArray()))
        .Named("state declaration")
            .Register(new ConstructInfo(
                "state-declaration",
                "state <Name> [initial][, <Name> [initial], ...]",
                "top-level",
                "Declares a workflow state",
                "state Idle initial"));

    // event <Name> [with <ArgList>]
    // where ArgList = Name as Type [modifier...] (nullable, default, constraints, ordered — any order) separated by commas
    private static readonly TokenListParser<PreceptToken, PreceptEventArg> EventArg =
        from name in Token.EqualTo(PreceptToken.Identifier)
        from _ in Token.EqualTo(PreceptToken.As)
        from typeRef in ScalarTypeOrChoice
        from modifiers in AnyEventArgModifier.Many()
        select BuildEventArgResult(name, typeRef, modifiers);

    private static readonly TokenListParser<PreceptToken, StatementResult> EventDecl =
        (from kw in Token.EqualTo(PreceptToken.Event)
         from names in Token.EqualTo(PreceptToken.Identifier)
             .AtLeastOnceDelimitedBy(Token.EqualTo(PreceptToken.Comma))
         from args in Token.EqualTo(PreceptToken.With)
             .IgnoreThen(EventArg.AtLeastOnceDelimitedBy(Token.EqualTo(PreceptToken.Comma)))
             .OptionalOrDefault(Array.Empty<PreceptEventArg>())
         select (StatementResult)new EventResult(
             names.Select(n => new PreceptEvent(
                 n.ToText(), args.ToList(), kw.Span.Position.Line, n.Span.Position.Column)).ToArray()))
        .Named("event declaration")
            .Register(new ConstructInfo(
                "event-declaration",
                "event <Name>[, <Name>, ...] [with <Arg> as <Type> [<modifier>...], ...]",
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
         from whenGuard in OptionalWhenGuardParser
         from __ in Token.EqualTo(PreceptToken.Because)
         from reason in Token.EqualTo(PreceptToken.StringLiteral)
         select (StatementResult)new StateAssertResult(
             kw.Kind == PreceptToken.In ? AssertAnchor.In :
             kw.Kind == PreceptToken.To ? AssertAnchor.To :
             AssertAnchor.From,
             states,
             ReconstituteExpr(expr), expr, reason.ToStringLiteralValue(),
             SourceLine: kw.Span.Position.Line,
             WhenText: whenGuard is not null ? ReconstituteExpr(whenGuard) : null,
             WhenGuard: whenGuard))
        .Named("state assert")
            .Register(new ConstructInfo(
                "state-assert",
                "in|to|from <State> assert <Expr> [when <Guard>] because \"<Reason>\"",
                "top-level",
                "State-scoped data constraint checked on entry, exit, or residency",
                "in Open assert Assignee != null because \"Must have an assignee\""));

    // on <Event> assert <BoolExpr> because "reason"
    private static readonly TokenListParser<PreceptToken, StatementResult> EventAssertDecl =
        (from kwOn in Token.EqualTo(PreceptToken.On)
         from eventName in Token.EqualTo(PreceptToken.Identifier)
         from _ in Token.EqualTo(PreceptToken.Assert)
         from expr in BoolExpr
         from whenGuard in OptionalWhenGuardParser
         from __ in Token.EqualTo(PreceptToken.Because)
         from reason in Token.EqualTo(PreceptToken.StringLiteral)
         select (StatementResult)new EventAssertResult(new EventAssertion(
             eventName.ToText(), ReconstituteExpr(expr), expr, reason.ToStringLiteralValue(),
             SourceLine: kwOn.Span.Position.Line,
             WhenText: whenGuard is not null ? ReconstituteExpr(whenGuard) : null,
             WhenGuard: whenGuard)))
        .Named("event assert")
            .Register(new ConstructInfo(
                "event-assert",
                "on <Event> assert <Expr> [when <Guard>] because \"<Reason>\"",
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

    // in <StateTarget> edit <FieldTarget>
    private static readonly TokenListParser<PreceptToken, StatementResult> EditDecl =
        (from kw in Token.EqualTo(PreceptToken.In)
         from states in StateTarget
         from whenGuard in OptionalWhenGuardParser
         from __ in Token.EqualTo(PreceptToken.Edit)
         from fields in FieldTarget
         select (StatementResult)new EditResult(states, fields,
             WhenText: whenGuard is not null ? ReconstituteExpr(whenGuard) : null,
             WhenGuard: whenGuard,
             SourceLine: kw.Span.Position.Line))
        .Named("edit declaration")
            .Register(new ConstructInfo(
                "edit-declaration",
                "in <State> [when <Guard>] edit <Field>, ...",
                "top-level",
                "Declares which fields are editable in a state",
                "in Open edit Priority"));

    // ═══════════════════════════════════════════════════════════════════
    // Root Edit Declarations (stateless precepts)
    // ═══════════════════════════════════════════════════════════════════

    // edit <FieldTarget> [when <Guard>]  (root-level; valid only when no states declared)
    private static readonly TokenListParser<PreceptToken, StatementResult> RootEditDecl =
        (from kw in Token.EqualTo(PreceptToken.Edit)
         from fields in FieldTarget
         from whenGuard in OptionalWhenGuardParser
         select (StatementResult)new RootEditResult(fields,
             WhenText: whenGuard is not null ? ReconstituteExpr(whenGuard) : null,
             WhenGuard: whenGuard,
             SourceLine: kw.Span.Position.Line))
        .Named("root edit declaration")
            .Register(new ConstructInfo(
                "root-edit-declaration",
                "edit <Field>, ... [when <Guard>] | edit all [when <Guard>]",
                "top-level",
                "Declares which fields are editable (stateless precepts)",
                "edit Priority when Active"));

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
    private sealed record FieldResult(PreceptField[] Fields) : StatementResult;
    private sealed record CollectionFieldResult(PreceptCollectionField[] Fields) : StatementResult;
    private sealed record InvariantResult(PreceptInvariant Invariant) : StatementResult;
    private sealed record StateResult(PreceptState[] States, bool[] InitialFlags) : StatementResult;
    private sealed record EventResult(PreceptEvent[] Events) : StatementResult;
    private sealed record StateAssertResult(AssertAnchor Prep, string[] States,
        string ExprText, PreceptExpression Expr, string Reason, int SourceLine = 0,
        string? WhenText = null, PreceptExpression? WhenGuard = null) : StatementResult;
    private sealed record EventAssertResult(EventAssertion Assert) : StatementResult;
    private sealed record StateActionResult(AssertAnchor Prep, string[] States,
        ParsedAction[] Actions) : StatementResult;
    private sealed record EditResult(string[] States, string[] Fields,
        string? WhenText = null, PreceptExpression? WhenGuard = null, int SourceLine = 0) : StatementResult;
    private sealed record RootEditResult(string[] Fields,
        string? WhenText = null, PreceptExpression? WhenGuard = null, int SourceLine = 0) : StatementResult;
    private sealed record TransitionRowResult(string[] States, string EventName,
        PreceptExpression? WhenGuard, ParsedAction[] ActionsAndOutcome, int SourceLine = 0) : StatementResult;

    /// <summary>Union parser: tries each statement kind in priority order.</summary>
    private static readonly TokenListParser<PreceptToken, StatementResult> Statement =
        // Order matters: more specific patterns before general ones
        // 'in ... edit' before 'in ... assert' (both start with In)
        EditDecl.Try()
        .Or(RootEditDecl.Try())
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
    /// AssembleModel is called separately after confirming IsAtEnd.
    /// </summary>
    private static readonly TokenListParser<PreceptToken, ((string Name, int SourceLine) Header, StatementResult[] Statements)> RawFileParser =
        from header in PreceptHeader
        from statements in Statement.Many()
        select (header, statements);

    /// <summary>
    /// Assembles individual parsed statements into a <see cref="PreceptDefinition"/>.
    /// Validates structural constraints (one initial state, no duplicates, etc.).
    /// </summary>
    private static PreceptDefinition AssembleModel(string name, int sourceLine, StatementResult[] statements)
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
                    foreach (var field in fr.Fields)
                    {
                        if (fields.Any(f => f.Name == field.Name) || collectionFields.Any(f => f.Name == field.Name))
                            // SYNC:CONSTRAINT:C6
                            throw DiagnosticCatalog.C6.ToException(field.SourceLine, ("fieldName", field.Name));
                        fields.Add(field);
                    }
                    break;

                case CollectionFieldResult cfr:
                    foreach (var collField in cfr.Fields)
                    {
                        if (fields.Any(f => f.Name == collField.Name) || collectionFields.Any(f => f.Name == collField.Name))
                            // SYNC:CONSTRAINT:C6
                            throw DiagnosticCatalog.C6.ToException(collField.SourceLine, ("fieldName", collField.Name));
                        collectionFields.Add(collField);
                    }
                    break;

                case InvariantResult ir:
                    invariants.Add(ir.Invariant);
                    break;

                case StateResult sr:
                    for (int i = 0; i < sr.States.Length; i++)
                    {
                        var state = sr.States[i];
                        if (states.Any(s => s.Name == state.Name))
                            // SYNC:CONSTRAINT:C7
                            throw DiagnosticCatalog.C7.ToException(state.SourceLine, ("stateName", state.Name));
                        states.Add(state);
                        if (sr.InitialFlags[i])
                        {
                            if (initialState is not null)
                                // SYNC:CONSTRAINT:C8
                                throw DiagnosticCatalog.C8.ToException(state.SourceLine, ("stateName", initialState.Name));
                            initialState = state;
                        }
                    }
                    break;

                case EventResult er:
                    foreach (var evt in er.Events)
                    {
                        if (events.Any(e => e.Name == evt.Name))
                            // SYNC:CONSTRAINT:C9
                            throw DiagnosticCatalog.C9.ToException(evt.SourceLine, ("eventName", evt.Name));
                        events.Add(evt);
                    }
                    break;

                case StateAssertResult sar:
                    ExpandStateTargets(sar.States, states).ForEach(stateName =>
                        stateAsserts.Add(new StateAssertion(
                            sar.Prep, stateName, sar.ExprText, sar.Expr, sar.Reason, sar.SourceLine,
                            WhenText: sar.WhenText, WhenGuard: sar.WhenGuard)));
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
                        editBlocks.Add(new PreceptEditBlock(stateName, edr.Fields.ToList(),
                            SourceLine: edr.SourceLine,
                            WhenText: edr.WhenText, WhenGuard: edr.WhenGuard)));
                    break;

                case RootEditResult redr:
                    editBlocks.Add(new PreceptEditBlock(null, redr.Fields.ToList(), SourceLine: redr.SourceLine,
                        WhenText: redr.WhenText, WhenGuard: redr.WhenGuard));
                    break;

                case TransitionRowResult trr:
                    var outcomes = trr.ActionsAndOutcome.OfType<OutcomeAction>().ToList();
                    if (outcomes.Count == 0)
                        // SYNC:CONSTRAINT:C10
                        throw DiagnosticCatalog.C10.ToException(trr.SourceLine, ("eventName", trr.EventName));

                    // Design: "exactly one outcome, at the end; no statements after it"
                    if (outcomes.Count > 1)
                        // SYNC:CONSTRAINT:C11
                        throw DiagnosticCatalog.C11.ToException(trr.SourceLine, ("eventName", trr.EventName));
                    var firstOutcomeIdx = Array.IndexOf(trr.ActionsAndOutcome, outcomes[0]);
                    if (firstOutcomeIdx < trr.ActionsAndOutcome.Length - 1)
                        // SYNC:CONSTRAINT:C11
                        throw DiagnosticCatalog.C11.ToException(trr.SourceLine, ("eventName", trr.EventName));

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

        // SYNC:CONSTRAINT:C54 — validate transition rows reference declared states
        var stateNames = new HashSet<string>(states.Select(s => s.Name), StringComparer.Ordinal);
        foreach (var row in transitionRows)
        {
            if (!stateNames.Contains(row.FromState))
                throw DiagnosticCatalog.C54.ToException(row.SourceLine, ("stateName", row.FromState));
            if (row.Outcome is StateTransition st && !stateNames.Contains(st.TargetState))
                throw DiagnosticCatalog.C54.ToException(row.SourceLine, ("stateName", st.TargetState));
        }

        if (states.Count == 0 && fields.Count == 0 && collectionFields.Count == 0)
            // SYNC:CONSTRAINT:C12
            throw DiagnosticCatalog.C12.ToException();
        if (states.Count > 0 && initialState is null)
            // SYNC:CONSTRAINT:C13
            throw DiagnosticCatalog.C13.ToException(states[0].SourceLine);

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
                        throw DiagnosticCatalog.C14.ToException(ea.SourceLine, ("eventName", ea.EventName), ("prefix", id.Name), ("member", id.Member));
                    if (!argNames.Contains(id.Member))
                        // SYNC:CONSTRAINT:C15
                        throw DiagnosticCatalog.C15.ToException(ea.SourceLine, ("eventName", ea.EventName), ("member", id.Member));
                }
                else if (!argNames.Contains(id.Name))
                {
                    // SYNC:CONSTRAINT:C16
                    throw DiagnosticCatalog.C16.ToException(ea.SourceLine, ("eventName", ea.EventName), ("identifier", id.Name));
                }
            }
        }

        // Validate field defaults: non-nullable fields must have a default; default type must match
        foreach (var f in fields)
        {
            if (!f.IsNullable && !f.HasDefaultValue && !f.IsComputed)
                // SYNC:CONSTRAINT:C17
                throw DiagnosticCatalog.C17.ToException(f.SourceLine, ("fieldName", f.Name));
            if (f.HasDefaultValue && f.DefaultValue is not null)
            {
                var ok = f.Type switch
                {
                    PreceptScalarType.Number => f.DefaultValue is double || f.DefaultValue is long,
                    PreceptScalarType.Integer => f.DefaultValue is long,
                    PreceptScalarType.String => f.DefaultValue is string,
                    PreceptScalarType.Boolean => f.DefaultValue is bool,
                    _ => true
                };
                if (!ok)
                    // SYNC:CONSTRAINT:C18
                    throw DiagnosticCatalog.C18.ToException(f.SourceLine, ("fieldName", f.Name), ("fieldType", f.Type));
            }
            if (f.HasDefaultValue && f.DefaultValue is null && !f.IsNullable)
                // SYNC:CONSTRAINT:C19
                throw DiagnosticCatalog.C19.ToException(f.SourceLine, ("fieldName", f.Name), ("fieldType", f.Type));
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
                        PreceptScalarType.Number => arg.DefaultValue is double || arg.DefaultValue is long,
                        PreceptScalarType.Integer => arg.DefaultValue is long,
                        PreceptScalarType.String => arg.DefaultValue is string,
                        PreceptScalarType.Boolean => arg.DefaultValue is bool,
                        _ => true
                    };
                    if (!ok)
                        // SYNC:CONSTRAINT:C20
                        throw DiagnosticCatalog.C20.ToException(arg.SourceLine, ("argName", arg.Name), ("argType", arg.Type));
                }
                if (arg.HasDefaultValue && arg.DefaultValue is null && !arg.IsNullable)
                    // SYNC:CONSTRAINT:C21
                    throw DiagnosticCatalog.C21.ToException(arg.SourceLine, ("argName", arg.Name), ("argType", arg.Type));
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
                        throw DiagnosticCatalog.C22.ToException(row.SourceLine, ("verb", mut.Verb.ToString().ToLowerInvariant()), ("fieldName", mut.TargetField));
                    // SYNC:CONSTRAINT:C23
                    throw DiagnosticCatalog.C23.ToException(row.SourceLine, ("verb", mut.Verb.ToString().ToLowerInvariant()), ("fieldName", mut.TargetField));
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
                    throw DiagnosticCatalog.C24.ToException(row.SourceLine, ("verb", mut.Verb.ToString().ToLowerInvariant()), ("collectionKind", kind.ToString().ToLowerInvariant()), ("fieldName", mut.TargetField));
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
                throw DiagnosticCatalog.C25.ToException(row.SourceLine, ("fromState", row.FromState), ("eventName", row.EventName));
            if (row.WhenGuard is null)
                seenUnguarded.Add(key);
        }

        // Desugar field-level constraints into synthetic invariants and event asserts.
        DesugarFieldConstraints(fields, collectionFields, events, invariants, eventAsserts);

        return new PreceptDefinition(
            name, states, initialState, events,
            fields, collectionFields,
            invariants.Count > 0 ? invariants : null,
            stateAsserts.Count > 0 ? stateAsserts : null,
            stateActions.Count > 0 ? stateActions : null,
            eventAsserts.Count > 0 ? eventAsserts : null,
            transitionRows.Count > 0 ? transitionRows : null,
            editBlocks.Count > 0 ? editBlocks : null,
            sourceLine);
    }

    /// <summary>
    /// Desugars field/arg-level constraint suffixes into synthetic <see cref="PreceptInvariant"/>
    /// and <see cref="EventAssertion"/> nodes, appending them to the existing lists.
    /// Constraint validation (C57/C58/C59) runs later in <see cref="PreceptTypeChecker"/>.
    /// </summary>
    private static void DesugarFieldConstraints(
        List<PreceptField> fields,
        List<PreceptCollectionField> collectionFields,
        List<PreceptEvent> events,
        List<PreceptInvariant> invariants,
        List<EventAssertion> eventAsserts)
    {
        foreach (var field in fields)
        {
            if (field.Constraints is not { Count: > 0 }) continue;
            foreach (var constraint in field.Constraints)
            {
                var inv = BuildFieldInvariant(field.Name, field.Type, field.IsNullable, constraint);
                if (inv is not null) invariants.Add(inv);
            }
        }

        foreach (var col in collectionFields)
        {
            if (col.Constraints is not { Count: > 0 }) continue;
            foreach (var constraint in col.Constraints)
            {
                var inv = BuildCollectionFieldInvariant(col.Name, constraint);
                if (inv is not null) invariants.Add(inv);
            }
        }

        foreach (var evt in events)
        {
            foreach (var arg in evt.Args)
            {
                if (arg.Constraints is not { Count: > 0 }) continue;
                foreach (var constraint in arg.Constraints)
                {
                    var assert = BuildEventArgAssert(evt.Name, arg.Name, arg.Type, arg.IsNullable, constraint);
                    if (assert is not null) eventAsserts.Add(assert);
                }
            }
        }
    }

    private static PreceptInvariant? BuildFieldInvariant(
        string name, PreceptScalarType type, bool isNullable, FieldConstraint constraint)
    {
        var (exprText, expr, reason) = BuildScalarConstraintExpr(name, type, isNullable, constraint);
        if (expr is null) return null;
        return new PreceptInvariant(exprText, expr, reason, IsSynthetic: true);
    }

    private static PreceptInvariant? BuildCollectionFieldInvariant(string name, FieldConstraint constraint)
    {
        var countExpr = new PreceptIdentifierExpression(name, "count");
        var (exprText, expr, reason) = constraint switch
        {
            FieldConstraint.Notempty =>
                ($"{name}.count > 0",
                 (PreceptExpression)new PreceptBinaryExpression(">", countExpr, new PreceptLiteralExpression(0.0)),
                 $"{name} must not be empty (notempty constraint)"),
            FieldConstraint.Mincount mc =>
                ($"{name}.count >= {mc.Value}",
                 (PreceptExpression)new PreceptBinaryExpression(">=", countExpr, new PreceptLiteralExpression((double)mc.Value)),
                 $"{name} count must be at least {mc.Value} (mincount constraint)"),
            FieldConstraint.Maxcount mc =>
                ($"{name}.count <= {mc.Value}",
                 (PreceptExpression)new PreceptBinaryExpression("<=", countExpr, new PreceptLiteralExpression((double)mc.Value)),
                 $"{name} count must be at most {mc.Value} (maxcount constraint)"),
            _ => (string.Empty, null, string.Empty)
        };
        if (expr is null) return null;
        return new PreceptInvariant(exprText, expr, reason, IsSynthetic: true);
    }

    private static EventAssertion? BuildEventArgAssert(
        string eventName, string argName,
        PreceptScalarType type, bool isNullable, FieldConstraint constraint)
    {
        var (exprText, expr, reason) = BuildScalarConstraintExpr(argName, type, isNullable, constraint);
        if (expr is null) return null;
        return new EventAssertion(eventName, exprText, expr, reason);
    }

    /// <summary>
    /// Builds a constraint expression (text + AST) and reason string for a scalar field/arg.
    /// Returns (string.Empty, null, string.Empty) when the constraint is inapplicable to the type.
    /// </summary>
    private static (string ExprText, PreceptExpression? Expr, string Reason) BuildScalarConstraintExpr(
        string name, PreceptScalarType type, bool isNullable, FieldConstraint constraint)
    {
        if (type == PreceptScalarType.Number)
        {
            switch (constraint)
            {
                case FieldConstraint.Nonnegative:
                    return MaybeNullGuard(name, isNullable, ">=", new PreceptLiteralExpression(0.0),
                        $"{name} must be non-negative (nonnegative constraint)");
                case FieldConstraint.Positive:
                    return MaybeNullGuard(name, isNullable, ">", new PreceptLiteralExpression(0.0),
                        $"{name} must be positive (positive constraint)");
                case FieldConstraint.Min mn:
                    return MaybeNullGuard(name, isNullable, ">=", new PreceptLiteralExpression(mn.Value),
                        $"{name} minimum value is {mn.Value.ToString(CultureInfo.InvariantCulture)} (min constraint)");
                case FieldConstraint.Max mx:
                    return MaybeNullGuard(name, isNullable, "<=", new PreceptLiteralExpression(mx.Value),
                        $"{name} maximum value is {mx.Value.ToString(CultureInfo.InvariantCulture)} (max constraint)");
            }
        }

        if (type is PreceptScalarType.Integer or PreceptScalarType.Decimal)
        {
            switch (constraint)
            {
                case FieldConstraint.Nonnegative:
                    return MaybeNullGuard(name, isNullable, ">=", new PreceptLiteralExpression(0.0),
                        $"{name} must be non-negative (nonnegative constraint)");
                case FieldConstraint.Positive:
                    return MaybeNullGuard(name, isNullable, ">", new PreceptLiteralExpression(0.0),
                        $"{name} must be positive (positive constraint)");
                case FieldConstraint.Min mn:
                    return MaybeNullGuard(name, isNullable, ">=", new PreceptLiteralExpression(mn.Value),
                        $"{name} minimum value is {mn.Value.ToString(CultureInfo.InvariantCulture)} (min constraint)");
                case FieldConstraint.Max mx:
                    return MaybeNullGuard(name, isNullable, "<=", new PreceptLiteralExpression(mx.Value),
                        $"{name} maximum value is {mx.Value.ToString(CultureInfo.InvariantCulture)} (max constraint)");
            }
        }

        if (type == PreceptScalarType.String)
        {
            switch (constraint)
            {
                case FieldConstraint.Notempty:
                    return MaybeNullGuard(name, isNullable, "!=", new PreceptLiteralExpression(""),
                        $"{name} must not be empty (notempty constraint)");
                case FieldConstraint.Minlength ml:
                {
                    var lenExpr = new PreceptIdentifierExpression(name, "length");
                    var coreExpr = new PreceptBinaryExpression(">=", lenExpr, new PreceptLiteralExpression((double)ml.Value));
                    var coreText = $"{name}.length >= {ml.Value}";
                    if (!isNullable)
                        return (coreText, coreExpr, $"{name} length must be at least {ml.Value} (minlength constraint)");
                    var nullCheck = new PreceptBinaryExpression("==",
                        new PreceptIdentifierExpression(name), new PreceptLiteralExpression(null));
                    return ($"{name} == null or {coreText}",
                            new PreceptBinaryExpression("or", nullCheck, coreExpr),
                            $"{name} length must be at least {ml.Value} (minlength constraint)");
                }
                case FieldConstraint.Maxlength ml:
                {
                    var lenExpr = new PreceptIdentifierExpression(name, "length");
                    var coreExpr = new PreceptBinaryExpression("<=", lenExpr, new PreceptLiteralExpression((double)ml.Value));
                    var coreText = $"{name}.length <= {ml.Value}";
                    if (!isNullable)
                        return (coreText, coreExpr, $"{name} length must be at most {ml.Value} (maxlength constraint)");
                    var nullCheck = new PreceptBinaryExpression("==",
                        new PreceptIdentifierExpression(name), new PreceptLiteralExpression(null));
                    return ($"{name} == null or {coreText}",
                            new PreceptBinaryExpression("or", nullCheck, coreExpr),
                            $"{name} length must be at most {ml.Value} (maxlength constraint)");
                }
            }
        }

        return (string.Empty, null, string.Empty);
    }

    /// <summary>
    /// Builds a comparison expression with an optional <c>null or ...</c> wrapper for nullable fields.
    /// </summary>
    private static (string ExprText, PreceptExpression Expr, string Reason) MaybeNullGuard(
        string name, bool isNullable, string op, PreceptExpression rhs, string reason)
    {
        var fieldRef = new PreceptIdentifierExpression(name);
        var coreExpr = new PreceptBinaryExpression(op, fieldRef, rhs);
        var coreText = $"{name} {op} {LiteralText(rhs)}";
        if (!isNullable)
            return (coreText, coreExpr, reason);
        var nullCheck = new PreceptBinaryExpression("==", fieldRef, new PreceptLiteralExpression(null));
        return ($"{name} == null or {coreText}",
                new PreceptBinaryExpression("or", nullCheck, coreExpr),
                reason);
    }

    private static string LiteralText(PreceptExpression expr) => expr switch
    {
        PreceptLiteralExpression { Value: double d } => d.ToString(CultureInfo.InvariantCulture),
        PreceptLiteralExpression { Value: string s } => $"\"{s}\"",
        PreceptLiteralExpression { Value: null } => "null",
        _ => ReconstituteExpr(expr)
    };
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
