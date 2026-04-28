namespace Precept.Language;

/// <summary>
/// A named multi-construct pattern example showing how Precept language features combine
/// in typical real-world definitions.
/// </summary>
public sealed record CommonPattern(
    string Name,
    string Description,
    string DslSnippet);

/// <summary>
/// Grammar meta-rules — singular facts about how Precept source text is structured.
/// Not a catalog (no enum, no <c>GetMeta()</c>, no <c>All</c>). Structured metadata
/// about the grammar as a whole, consumed by MCP, LS hover, reference docs, and AI grounding.
/// </summary>
public static class SyntaxReference
{
    public static string GrammarModel       => "line-oriented";
    public static string CommentSyntax      => "# to end of line";
    public static string IdentifierRules    => "Starts with letter, alphanumeric + underscore, case-sensitive";
    public static string StringLiteralRules => "Double-quoted strings with {expr} interpolation and \\, \\n, \\t, \\\", {{, }} escapes; single-quoted typed constants ('value') for date, time, instant, duration, period, timezone, zoneddatetime, datetime literals";
    public static string NumberLiteralRules => "Integers (42), decimals (3.14), exponent notation (1.5e2, 1e-5); no hex/underscore separators";
    public static string WhitespaceRules    => "Not significant — indentation is cosmetic, line breaks separate declarations";
    public static string NullNarrowing      => "if Field is set narrows to guaranteed-present in the then branch";

    /// <summary>
    /// Rules for typed constant literals — single-quoted values that represent domain-constrained identifiers
    /// and temporal values. Used by the type checker and MCP tools.
    /// </summary>
    public static string TypedConstantRules =>
        """
        Typed constants use single quotes: 'value'. They represent domain-constrained literal values whose
        meaning is determined by the type context in which they appear.

        Contexts where typed constants appear:
        - Currency qualifiers:   money in 'USD', price in 'EUR' of 'kg'
        - Unit modifiers:        quantity of 'kg', price in 'USD' of 'miles'
        - Dimension assertions:  period of 'days', quantity of 'length'
        - Timezone identifiers:  timezone field default 'America/New_York'
        - Date literals:         field default '2024-01-15'
        - Time literals:         field default '14:30:00'
        - Instant literals:      field default '2024-01-15T14:30:00Z'
        - Period literals:       field default '1 year + 2 months'
        - Duration literals:     field default '4 hours + 30 minutes'

        Escape sequences in typed constants: \' (quote), \\ (backslash).

        Valid:   money in 'USD', quantity of 'kg', 'America/New_York'
        Invalid: money in "USD" (double quotes), money in USD (no quotes)

        The type context determines validity. 'USD' is valid for currency but not for unit.
        Using a typed constant where no type context is available produces UnresolvedTypedConstant.
        """;

    /// <summary>
    /// Rules for expression syntax — conditionals, function calls, member access, and operator composition.
    /// </summary>
    public static string ExpressionRules =>
        """
        Expressions appear in: guard conditions (when), ensure conditions, rule right-hand sides,
        default values, event ensures (on E ensure ...), and action value arguments (set Field = Expr).

        if/then/else:
          if Condition then ValueIfTrue else ValueIfFalse
          The condition must be boolean. Both branches must have compatible types.
          Nesting: if A then (if B then X else Y) else Z

        Function calls:
          FunctionName(arg1, arg2)
          Functions are lower-camelCase. Only built-in functions are available.
          Examples: min(a, b), max(score, 0), round(amount, 2), abs(balance), trim(name)

        Member access:
          Field.accessor   — e.g., StartDate.year, Amount.currency, Items.count
          Chained:         — e.g., Instant.inZone(tz).date.year

        Null guard narrowing:
          if Field is set then Field.accessor else default
          Inside the 'then' branch, Field is narrowed to guaranteed-present.

        Operators bind tighter than 'if/then/else'. Wrap in parentheses to override:
          if (a + b) > 10 then "high" else "low"
        """;

    /// <summary>
    /// Operator precedence table from highest to lowest binding power.
    /// Higher precedence operators bind more tightly and are evaluated first.
    /// </summary>
    public static IReadOnlyList<string> PrecedenceTable { get; } =
    [
        "80  . (               — member access, function call",
        "65  (unary -)         — arithmetic negation",
        "60  * / %             — multiplication, division, modulo",
        "50  + -               — addition, subtraction",
        "40  contains is       — collection membership, type/null test",
        "30  == != ~= !~ < > <= >=  — comparison (all non-associative: cannot be chained)",
        "25  not               — logical negation",
        "20  and               — logical conjunction",
        "10  or                — logical disjunction",
    ];

    /// <summary>
    /// Common multi-construct patterns. Named templates showing how language features combine
    /// in typical real-world precept definitions.
    /// </summary>
    public static IReadOnlyList<CommonPattern> CommonPatterns { get; } =
    [
        new(
            "Guarded transition",
            "A transition that only fires when a runtime condition is true. The 'when' clause is evaluated against current field values and event arguments.",
            """
            from UnderReview on Approve when DocumentsVerified and CreditScore >= 680
                -> set ApprovedAmount = Approve.Amount
                -> transition Approved
            from UnderReview on Approve
                -> reject "Approval requires verified documents and sufficient credit score"
            """),

        new(
            "Computed field",
            "A field whose value is always derived from other fields. The '->' syntax declares the formula. Computed fields cannot be assigned directly.",
            """
            field Subtotal as number -> UnitPrice * Quantity
            field DiscountAmount as number -> Subtotal * DiscountPercent / 100
            field LineTotal as number -> Subtotal - DiscountAmount nonnegative
            """),

        new(
            "Conditional action",
            "An action that produces different values based on a runtime condition. Uses if/then/else in the value expression.",
            """
            from UnderReview on Approve when CreditScore >= 680
                -> set DecisionNote = if CreditScore >= 750 then "Prime tier — auto-approved" else "Standard tier — approved"
                -> transition Approved
            """),

        new(
            "Collection membership gate",
            "A transition that fires only if a collection contains a specific element, or reads a collection's state to decide behavior.",
            """
            from InterviewLoop on RecordFeedback when PendingInterviewers contains RecordFeedback.Interviewer and PendingInterviewers.count == 1
                -> remove PendingInterviewers RecordFeedback.Interviewer
                -> set FeedbackCount = FeedbackCount + 1
                -> transition Decision
            from InterviewLoop on RecordFeedback when PendingInterviewers contains RecordFeedback.Interviewer
                -> remove PendingInterviewers RecordFeedback.Interviewer
                -> set FeedbackCount = FeedbackCount + 1
                -> no transition
            from InterviewLoop on RecordFeedback
                -> reject "Only assigned interviewers can submit feedback"
            """),

        new(
            "Stateless write-only precept",
            "A precept with no lifecycle states or events. Defines structural constraints on a data object and declares which fields the host application may write.",
            """
            precept FeeSchedule

            field BaseFee as decimal default 0 nonnegative maxplaces 2 writable
            field DiscountPercent as decimal default 0 nonnegative max 100 maxplaces 2 writable
            field TaxRate as decimal default 0.1 nonnegative maxplaces 4
            """),
    ];

    public static IReadOnlyList<string> ConventionalOrder { get; } =
    [
        "header",
        "fields",
        "rules",
        "states",
        "ensures",
        "accessModes",
        "events",
        "event ensures",
        "transitions",
        "state actions",
    ];
}
