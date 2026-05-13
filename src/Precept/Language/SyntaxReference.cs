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
/// A named anti-pattern showing a common mistake in Precept definitions,
/// paired with a correct alternative and explanation.
/// </summary>
public sealed record AntiPattern(
    string Name,
    string Description,
    string BadSnippet,
    string GoodSnippet,
    string WhyItFails);

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
    public static string NumberLiteralRules => "Integers (42, -1), decimals (3.14, -3.14), exponent notation (1.5e2, -1e5, 1e-5); no hex/underscore separators; negative numbers are first-class literals (parser constant-folds unary minus on numeric literals)";
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
        - Unit qualifiers:       quantity in 'kg', price in 'USD' of 'mass'
        - Dimension assertions:  period of 'days', quantity of 'length'
        - Timezone identifiers:  timezone field default 'America/New_York'
        - Date literals:         field default '2024-01-15'
        - Time literals:         field default '14:30:00'
        - Instant literals:      field default '2024-01-15T14:30:00Z'
        - Period literals:       field default '1 year + 2 months'
        - Duration literals:     field default '4 hours + 30 minutes'

        Escape sequences in typed constants: \' (quote), \\ (backslash).

        Valid:   money in 'USD', quantity in 'kg', 'America/New_York'
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
            from UnderReview on Approve when CreditScore >= 680
                -> set ApprovedAmount = Approve.Amount
                -> transition Approved
            from UnderReview on Approve
                -> reject "Approval requires sufficient credit score"
            """),

        new(
            "Computed field",
            "A field whose value is always derived from other fields. The '<-' syntax declares the formula. Computed fields cannot be assigned directly.",
            """
            field Subtotal as number <- UnitPrice * Quantity
            field DiscountAmount as number <- Subtotal * DiscountPercent / 100
            field LineTotal as number nonnegative <- Subtotal - DiscountAmount
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
            "Collection state gate",
            "A transition that branches on collection state such as element count before deciding whether to advance or remain in place.",
            """
            from InterviewLoop on RecordFeedback when PendingInterviewers.count == 1
                -> remove PendingInterviewers CurrentInterviewer
                -> set FeedbackCount = FeedbackCount + 1
                -> transition Decision
            from InterviewLoop on RecordFeedback when PendingInterviewers.count > 1
                -> remove PendingInterviewers CurrentInterviewer
                -> set FeedbackCount = FeedbackCount + 1
                -> no transition
            from InterviewLoop on RecordFeedback
                -> reject "At least one interviewer must be pending"
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

        new(
            "Multi-state lifecycle precept",
            "A precept with initial, intermediate, and terminal states showing a complete entity lifecycle with multiple transitions and state-level constraints.",
            """
            precept SubscriptionOrder

            field CustomerName as string optional
            field PlanName as string optional
            field Amount as number default 0 nonnegative

            state Pending initial
            state Active
            state Cancelled terminal
            state Expired terminal

            in Active ensure Amount > 0 because "Active subscriptions must have a positive amount"

            event Activate(Name as string, Plan as string, Price as number)
            on Activate ensure Activate.Price > 0 because "Activation price must be positive"

            event Cancel
            event Expire

            from Pending on Activate
                -> set CustomerName = Activate.Name
                -> set PlanName = Activate.Plan
                -> set Amount = Activate.Price
                -> transition Active
            from Active on Cancel
                -> transition Cancelled
            from Active on Expire
                -> transition Expired
            """),

        new(
            "Ensures invariant",
            "Using 'ensures' at the field level (rule) and state level (in State ensure) to enforce invariants that the runtime checks before and after every operation.",
            """
            precept LoanBalance

            field Principal as number default 0 nonnegative
            field InterestRate as decimal default 0.05 nonnegative max 1 maxplaces 4
            field OutstandingBalance as number default 0 nonnegative

            rule InterestRate <= 1 because "Interest rate cannot exceed 100%"
            rule OutstandingBalance <= Principal because "Outstanding balance cannot exceed principal"

            state Active initial
            state PaidOff terminal

            in Active ensure OutstandingBalance >= 0 because "Balance cannot go negative while active"

            event MakePayment(PaymentAmount as number)
            on MakePayment ensure MakePayment.PaymentAmount > 0 because "Payment must be positive"

            event PayOff

            from Active on MakePayment when MakePayment.PaymentAmount < OutstandingBalance
                -> set OutstandingBalance = OutstandingBalance - MakePayment.PaymentAmount
                -> no transition
            from Active on MakePayment
                -> set OutstandingBalance = 0
                -> transition PaidOff
            from Active on PayOff
                -> set OutstandingBalance = 0
                -> transition PaidOff
            """),

        new(
            "Money and quantity typed fields",
            "Using currency-qualified money fields and dimension-qualified quantity fields with arithmetic constraints. These domain-typed fields prevent cross-currency arithmetic and dimension mismatches at compile time.",
            """
            precept ShipmentOrder

            field Weight as quantity of 'mass' default '0 kg'
            field UnitPrice as price in 'USD' of 'mass' default '0.00 USD/kg'
            field TotalCost as money in 'USD' <- Weight * UnitPrice
            field DiscountPercent as decimal default 0 nonnegative max 100 maxplaces 2
            field FinalCost as money in 'USD' <- TotalCost - (TotalCost * DiscountPercent / 100)

            rule DiscountPercent <= 100 because "Discount percent cannot exceed 100%"
            """),

        new(
            "Entry action hook",
            "Declare a single action that fires automatically whenever the entity transitions into a state, instead of repeating the same assignment in every inbound transition. The 'to State -> actions' clause runs on every inbound edge including back-edges that re-enter the state. Use this to initialize or reset a field on state entry.",
            """
            # Fires on every inbound edge to ReadyForPickup, regardless of source state.
            to ReadyForPickup -> set PickupContacted = true

            # Reset-on-re-entry: fires even when a re-approval transitions back into Approved.
            to Approved -> set BadgePrinted = false
            """),

        new(
            "Cross-cutting event (from any)",
            "An event that must fire regardless of current state — a system-level signal that cuts across all lifecycle stages. Declare it once with 'from any on Event' instead of repeating the row for every individual state. The usual outcome is 'no transition' to stay in place, or 'transition Target' to jump unconditionally.",
            """
            from any on PedestrianRequest
                -> set RequestPending = true
                -> no transition

            from any on CloseService
                -> set WalkInOpen = false
                -> transition Closed
            """),

        new(
            "Stack and queue operations",
            "Collection fields that maintain insertion order. A stack supports push, pop-into, and .count (LIFO). A queue supports enqueue, dequeue-into, .peek, and .count (FIFO). The .peek accessor reads the front element without consuming it — useful to capture it into a field before the dequeue action removes it.",
            """
            # Stack: push adds to top; pop into captures and removes the top element.
            field RepairSteps as stack of string

            from InRepair on LogRepairStep
                -> push RepairSteps LogRepairStep.StepName
                -> no transition
            from InRepair on UndoLastStep when RepairSteps.count > 0
                -> pop RepairSteps into LastReversedStep
                -> no transition

            # Queue: enqueue adds to back; peek reads front without removing; dequeue into removes.
            field PartyQueue as queue of string

            from Accepting on JoinWaitlist
                -> enqueue PartyQueue JoinWaitlist.PartyName
                -> no transition
            from Accepting on SeatNextParty when PartyQueue.count > 0
                -> set LastCalledParty = PartyQueue.peek
                -> dequeue PartyQueue into CurrentParty
                -> transition Seating
            """),

        new(
            "Optional-with-fallback assignment",
            "When an event parameter is 'optional', use 'if Param is set then Param else fallback' in a 'set' action to provide a safe default without a separate guard row. The entire expression stays inline — no extra transition row is needed. Multiple fallback tiers can be chained with additional 'else if' clauses.",
            """
            event Approve(Amount as money in 'USD', Note as string optional)

            from UnderReview on Approve when DocumentsVerified and CreditScore >= 680
                -> set ApprovedAmount = Approve.Amount
                -> set DecisionNote = if Approve.Note is set then Approve.Note else if CreditScore >= 750 then "Prime tier — auto-approved" else "Standard tier — approved"
                -> transition Approved
            """),

        new(
            "Conditional rule (rule when)",
            "A global invariant that only applies when a guard condition is true. Use 'rule Expression when Condition because ...' when a constraint is only meaningful once the entity reaches a certain state. The runtime skips the rule entirely while the guard is false, preventing spurious violations during early lifecycle stages.",
            """
            # Skipped until DocumentsVerified = true; enforced on every operation thereafter.
            rule ExistingDebt <= AnnualIncome * 3.0 when DocumentsVerified because "Debt {ExistingDebt} exceeds the 3x income ceiling — maximum is {AnnualIncome * 3.0}"
            """),

        new(
            "State-scoped editing window",
            "Declare a window of mutability for specific fields only while the entity is in a given state. 'in State modify Fields editable' is lifecycle-aware: the editing window closes the moment the state changes. An optional 'when Condition' narrows the window further to a runtime guard within the state. Distinct from 'writable', which is a stateless per-field flag with no lifecycle awareness.",
            """
            # All five fields are editable while in Draft; window closes on Submit.
            in Draft modify ApplicantName, MonthlyIncome, RequestedRent, CreditScore, HouseholdSize editable

            # Conditional editing window — only open once DocumentsVerified is true.
            in UnderReview when DocumentsVerified modify DecisionNote editable
            """),

        new(
            "Interpolation in diagnostic strings",
            "Embed field values and computed expressions directly into 'because' and 'reject' strings using {expr} interpolation. Any expression valid in a 'when' guard is also valid inside braces. Use this to make rejection messages self-explanatory — the actual values that caused the failure appear inline rather than requiring a separate lookup.",
            """
            # Field value inline in a rule violation message.
            rule ApprovedAmount <= RequestedAmount because "Approved amount {ApprovedAmount} exceeds the submitted request of {RequestedAmount}"

            # Computed expression inline — same arithmetic the guard uses, surfaced in the message.
            in Approved ensure MonthlyIncome >= RequestedRent * 3 because "Monthly income {MonthlyIncome} does not meet the 3x rent requirement — {RequestedRent * 3} needed"

            # Event argument and field value together in a reject string.
            from Submitted on Approve
                -> reject "Cannot approve {Approve.Amount} — the submitted request is only {RequestedAmount}"

            # Division in a reject message — the same computed value the guard checked.
            from Draft on Submit
                -> reject "Average lodging of {Submit.Lodging / Submit.Days} per day exceeds the $350 policy cap"
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

    /// <summary>
    /// Common anti-patterns: mistakes AI agents and new users frequently make,
    /// each paired with a correct alternative and an explanation of why the bad
    /// version fails.
    /// </summary>
    public static IReadOnlyList<AntiPattern> AntiPatterns { get; } =
    [
        new(
            "Arrow direction for computed fields",
            "Using '->' (transition arrow) instead of '<-' (derivation arrow) for computed field formulas. The '->' arrow is for transitions, '<-' is for derived field formulas.",
            """
            precept Example
            field A as number default 1
            field B as number -> A + 1
            """,
            """
            precept Example
            field A as number default 1
            field B as number <- A + 1
            """,
            "'->' is the transition outcome arrow and is not valid in a field declaration. Use '<-' to declare a computed field's derivation formula."),

        new(
            "Chaining comparisons",
            "Writing '0 <= Amount <= 1000' (a chained comparison) instead of 'Amount >= 0 and Amount <= 1000'. Comparison operators in Precept are non-associative and cannot be chained.",
            """
            precept Example
            field Amount as number default 0
            rule 0 <= Amount <= 1000 because "must be in range"
            """,
            """
            precept Example
            field Amount as number default 0
            rule Amount >= 0 because "must be nonnegative"
            rule Amount <= 1000 because "must be in range"
            """,
            "Precept comparison operators (==, !=, <, >, <=, >=) are non-associative. Chaining them produces a parse error (NonAssociativeComparison). Use 'and' to combine two separate comparison conditions."),

        new(
            "Assigning a computed field",
            "Using 'set Field = Value' in a transition for a field declared as computed (with '<-'). Computed fields derive their value automatically from the formula and cannot be directly assigned.",
            """
            precept Example
            field A as number default 1
            field B as number <- A * 2
            state Draft initial
            state Done terminal
            event Complete
            from Draft on Complete -> set B = 5 -> transition Done
            """,
            """
            precept Example
            field A as number default 1
            field B as number <- A * 2
            state Draft initial
            state Done terminal
            event Complete
            from Draft on Complete -> transition Done
            """,
            "Computed fields (declared with '<-') are read-only by definition. Their value is recalculated from the formula whenever A changes. Attempting to 'set' a computed field produces a ComputedFieldNotWritable error."),

        new(
            "Sentinel defaults for not-yet-meaningful fields",
            "Using `default 0`, `default false`, or `default \"\"` for a field that should be absent in earlier states. A sentinel default turns 'not meaningful yet' into a real value and hides the transition where the field must first be set.",
            """
            precept RefundReview

            field RequestedAmount as money in 'USD' optional
            field ApprovedAmount as money in 'USD' default '0.00 USD'

            state Draft initial
            state Reviewed
            state Approved terminal

            in Approved ensure ApprovedAmount > '0.00 USD' because "Approved refunds must have a positive approved amount"

            event Submit(Amount as money in 'USD')
            event Approve(Amount as money in 'USD')
            on Approve ensure Approve.Amount > '0.00 USD' because "Approved refunds must be positive"

            from Draft on Submit
                -> set RequestedAmount = Submit.Amount
                -> transition Reviewed
            from Reviewed on Approve
                -> set ApprovedAmount = Approve.Amount
                -> transition Approved
            """,
            """
            precept RefundReview

            field RequestedAmount as money in 'USD' optional
            field ApprovedAmount as money in 'USD'

            state Draft initial
            state Reviewed
            state Approved terminal

            in Draft omit ApprovedAmount
            in Reviewed omit ApprovedAmount
            in Approved ensure ApprovedAmount > '0.00 USD' because "Approved refunds must have a positive approved amount"

            event Submit(Amount as money in 'USD')
            event Approve(Amount as money in 'USD')
            on Approve ensure Approve.Amount > '0.00 USD' because "Approved refunds must be positive"

            from Draft on Submit
                -> set RequestedAmount = Submit.Amount
                -> transition Reviewed
            from Reviewed on Approve
                -> set ApprovedAmount = Approve.Amount
                -> transition Approved
            """,
            "Declare the field with `omit` in every state where it has no business meaning. Then, on the transition into a non-omitted state, include `set Field = ...` to initialize it; the compiler requires that assignment before the field can become present. Keep `default` only for real business defaults."),

        new(
            "Exhaustive rejection rows",
            "Adding 'reject' rows for state/event combinations that are simply not applicable in that state. No row is the correct way to say an event has no meaning here — it produces Unmatched and hides the button entirely. 'reject' is for business-rule violations the user could potentially resolve.",
            """
            from Submitted on Approve when MonthlyIncome >= RequestedRent * 3 and CreditScore >= 650
                -> transition Approved
            from Submitted on Approve
                -> reject "Approval requires strong income coverage and acceptable credit"
            from Draft on Approve
                -> reject "Cannot approve an application that has not been submitted"
            from Approved on Approve
                -> reject "Application is already approved"
            """,
            """
            from Submitted on Approve when MonthlyIncome >= RequestedRent * 3 and CreditScore >= 650
                -> transition Approved
            from Submitted on Approve
                -> reject "Approval requires strong income coverage and acceptable credit"
            """,
            "Approve from Draft and from Approved adds rows for events with no meaning in those states — no UI should offer an Approve button there, and no row is the correct way to say so. The only reject that belongs here is the fallback from Submitted when the applicant fails the income and credit check — that is a condition the applicant could potentially remedy. Structurally inapplicable events need no row."),
    ];
}
