namespace Precept.Language;

/// <summary>
/// A named multi-construct pattern example showing how Precept language features combine
/// in typical real-world definitions.
/// </summary>
/// <param name="IsFragment">
/// True when <see cref="DslSnippet"/> is a documentation fragment (single construct or
/// row, no enclosing <c>precept Name</c> header). Fragments cannot be compiled
/// standalone — they reference identifiers not declared in the snippet. Default false.
/// </param>
public sealed record CommonPattern(
    string Name,
    string Description,
    string DslSnippet,
    bool   IsFragment = false);

/// <summary>
/// A named anti-pattern showing a common mistake in Precept definitions,
/// paired with a correct alternative and explanation.
/// </summary>
/// <param name="IsFragment">
/// True when both <see cref="BadSnippet"/> and <see cref="GoodSnippet"/> are
/// documentation fragments (rows / declarations without an enclosing
/// <c>precept Name</c> header). Default false.
/// </param>
/// <param name="BadCompilesClean">
/// True when <see cref="BadSnippet"/> is a *design* anti-pattern that compiles
/// without errors but represents bad design (the compiler cannot catch it).
/// False when <see cref="BadSnippet"/> illustrates a violation the compiler IS
/// expected to catch — the anti-pattern's badness is expressed as a diagnostic.
/// Default true (most anti-patterns are design quality issues). Ignored when
/// <see cref="IsFragment"/> is true.
/// </param>
public sealed record AntiPattern(
    string Name,
    string Description,
    string BadSnippet,
    string GoodSnippet,
    string WhyItFails,
    bool   IsFragment        = false,
    bool   BadCompilesClean  = true);

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
            "Required string field with bounds",
            "Declare a required identifier or name field using the 'notempty maxlength N' modifier idiom. The modifiers are structural — non-emptiness and length bound become proof obligations satisfied at compile time, not runtime rules. Prefer this over declaring an empty default plus a separate non-empty rule, which compiles but weakens the contract.",
            """
            field PatientId as string notempty maxlength 50
            field ProcedureCode as string notempty maxlength 20
            field ReferringProviderNpi as string notempty maxlength 10
            """,
            IsFragment: true),

        new(
            "Numeric field with structural constraints",
            "Constrain numeric, money, quantity, and decimal fields with structural modifiers ('nonnegative', 'positive', 'min N', 'max N', 'maxplaces N') rather than separate 'rule' declarations. Modifiers participate in proof obligations and constrain the type itself; rules can only check after the fact. Use 'default' only when the business value at creation time is the default — not as a workaround for required fields.",
            """
            field CreditScore as integer default 0 nonnegative max 850
            field Premium as money in 'USD' default '0 USD' positive
            field DiscountPercent as decimal default 0 nonnegative max 100 maxplaces 2
            field RemainingDays as integer nonnegative
            """,
            IsFragment: true),

        new(
            "Guarded transition",
            "A transition that only fires when a runtime condition is true. The 'when' clause is evaluated against current field values and event arguments.",
            """
            from UnderReview on Approve when CreditScore >= 680
                -> set ApprovedAmount = Approve.Amount
                -> transition Approved
            from UnderReview on Approve
                -> reject "Approval requires sufficient credit score"
            """,
            IsFragment: true),

        new(
            "Computed field",
            "A field whose value is always derived from other fields. The '<-' syntax declares the formula. Computed fields cannot be assigned directly.",
            """
            field Subtotal as number <- UnitPrice * Quantity
            field DiscountAmount as number <- Subtotal * DiscountPercent / 100
            field LineTotal as number nonnegative <- Subtotal - DiscountAmount
            """,
            IsFragment: true),

        new(
            "Conditional action",
            "An action that produces different values based on a runtime condition. Uses if/then/else in the value expression.",
            """
            from UnderReview on Approve when CreditScore >= 680
                -> set DecisionNote = if CreditScore >= 750 then "Prime tier — auto-approved" else "Standard tier — approved"
                -> transition Approved
            """,
            IsFragment: true),

        new(
            "Collection state gate",
            "A transition that branches on collection state such as element count before deciding whether to advance or remain in place.",
            """
            from InterviewLoop on RecordFeedback when PendingInterviewers.count == 1 and CurrentInterviewer is set
                -> remove PendingInterviewers CurrentInterviewer
                -> set FeedbackCount = FeedbackCount + 1
                -> transition Decision
            from InterviewLoop on RecordFeedback when PendingInterviewers.count > 1 and CurrentInterviewer is set
                -> remove PendingInterviewers CurrentInterviewer
                -> set FeedbackCount = FeedbackCount + 1
                -> no transition
            from InterviewLoop on RecordFeedback
                -> reject "At least one interviewer must be pending"
            """,
            IsFragment: true),

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
            "Constructor Pattern (Atomic Creation)",
            "Use the Constructor Pattern only when the entity's data is available *atomically* at creation — from a system event, automated process, batch import, single-API-call record, snapshot, or scheduled job. Declare 'event Create(...) initial', validate inputs with event ensures, and populate required fields in 'on Create' rows. **This pattern is NOT appropriate for user-filled forms.** For applications, claims, tickets, work orders, registrations, requests — anywhere data accumulates over time as a user fills in a form — use Free-Construction Pattern (Governed Draft). Free-Construction is the default for user-facing workflow entities; the Constructor Pattern is the exception, reserved for genuinely atomic-creation scenarios.",
            """
            precept ScheduledMaintenanceTask

            field EquipmentId as string notempty maxlength 50
            field MaintenanceType as choice of string("Inspection", "Lubrication", "PartReplacement", "Calibration")
            field ScheduledFor as date
            field AssignedTechnician as string notempty maxlength 100
            field Priority as choice of string("Routine", "High", "Critical")
            field CompletionNote as string optional maxlength 500

            state Scheduled initial
            state InProgress
            state Completed terminal
            state Cancelled terminal

            # Created atomically by the maintenance scheduler when it generates the next task
            # from the equipment's preventive-maintenance plan. All fields are derivable at
            # creation time from the plan + equipment record — no user form-filling involved.
            event Schedule(
                EquipmentId as string notempty,
                Type as choice of string("Inspection", "Lubrication", "PartReplacement", "Calibration"),
                ScheduledFor as date,
                Technician as string notempty,
                Priority as choice of string("Routine", "High", "Critical")
            ) initial

            on Schedule
                -> set EquipmentId = Schedule.EquipmentId
                -> set MaintenanceType = Schedule.Type
                -> set ScheduledFor = Schedule.ScheduledFor
                -> set AssignedTechnician = Schedule.Technician
                -> set Priority = Schedule.Priority

            event Start
            event Complete(Note as string optional)
            event Cancel(Reason as string notempty)

            from Scheduled on Start
                -> transition InProgress
            from InProgress on Complete
                -> set CompletionNote = Complete.Note
                -> transition Completed
            from Scheduled, InProgress on Cancel
                -> transition Cancelled
            """),

        new(
            "Free-Construction Pattern (Governed Draft)",
            "**This is the default pattern for user-filled workflow entities** — loan applications, insurance claims, IT tickets, work orders, hiring requests, prior authorizations, referrals, registrations. The entity is governed from birth but its data accumulates over time as the user fills in a form. Leave the initial event undeclared so Create() is parameterless and always succeeds, start in an initial draft state, open the form fields with 'in Draft modify Fields editable', and gate progression to a real state with state ensures or event ensures on the submit event. The draft IS the form; the editing window IS the workflow. Use the Constructor Pattern only when data arrives atomically (system events, batch imports, automated creation) — not for user forms.",
            """
            precept InventoryItem

            field Sku as string optional
            field Description as string optional
            field ListPrice as money in 'USD' optional

            state Unlisted initial
            state Listed
            state Delisted terminal

            # No initial event is declared, so Create() is parameterless and always succeeds.
            in Unlisted modify Sku, Description, ListPrice editable
            in Listed ensure Sku is set because "A listed item must have a SKU"
            in Listed ensure ListPrice is set because "A listed item must have a list price"

            event Publish(Sku as string notempty, Desc as string optional, Price as money in 'USD')
            on Publish ensure Publish.Price > '0.00 USD' because "Published items must have a positive list price"

            event Delist

            from Unlisted on Publish
                -> set Sku = Publish.Sku
                -> set Description = if Publish.Desc is set then Publish.Desc else Publish.Sku
                -> set ListPrice = Publish.Price
                -> transition Listed
            from Listed on Delist
                -> transition Delisted
            """),

        new(
            "Lifecycle-absent fields (`omit`)",
            "Use `in State omit Field` when a field is meaningful through most of the lifecycle but should not exist in one or more specific states. Omitted fields cannot be read or written there, so pair the omission with a state-exit clear before the lifecycle enters the omitted state.",
            """
            precept SupportTicket

            field Title as string optional
            field CurrentHandler as string optional

            state Draft initial
            state Investigating
            state WaitingOnCustomer
            state Closed terminal

            in Closed omit CurrentHandler
            from Investigating, WaitingOnCustomer -> clear CurrentHandler

            event Submit(Title as string notempty, Handler as string notempty)
            event RequestCustomerInfo
            event Resume(Handler as string notempty)
            event Close

            from Draft on Submit
                -> set Title = Submit.Title
                -> set CurrentHandler = Submit.Handler
                -> transition Investigating
            from Investigating on RequestCustomerInfo
                -> transition WaitingOnCustomer
            from WaitingOnCustomer on Resume
                -> set CurrentHandler = Resume.Handler
                -> transition Investigating
            from Investigating, WaitingOnCustomer on Close
                -> transition Closed
            """),

        new(
            "Entry ensures as construction gate",
            "Use `to State ensure ... because ...` to declare the invariant that must already hold when a transition enters that state. This gates progression at the state boundary and, when paired with constructor-style intake, becomes the first real-state admission check.",
            """
            precept TenantApplication

            field MonthlyIncome as money in 'USD' default '0.00 USD'
            field RequestedRent as money in 'USD' default '0.00 USD'
            field CreditScore as integer default 0
            field DocumentsVerified as boolean default false

            state Draft initial
            state Approved terminal
            state Denied terminal

            to Approved ensure DocumentsVerified and MonthlyIncome >= RequestedRent * 3.0 and CreditScore >= 680 because "Approved tenants need verified documents, credit score 680+, and income at least 3x rent"

            event Submit(Income as money in 'USD', Rent as money in 'USD', Score as integer, Verified as boolean)
            on Submit ensure Submit.Rent > '0.00 USD' because "Requested rent must be positive"
            event Deny

            from Draft on Submit
                -> set MonthlyIncome = Submit.Income
                -> set RequestedRent = Submit.Rent
                -> set CreditScore = Submit.Score
                -> set DocumentsVerified = Submit.Verified
                -> transition Approved
            from Draft on Deny
                -> transition Denied
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
            """,
            IsFragment: true),

        new(
            "Cross-cutting event (from any or multi-state source list)",
            "An event that must fire across multiple states. Two forms: 'from any on Event' for events that apply everywhere (system-level signals like CancelOrder, ClosedForHoliday). 'from State1, State2, State3 on Event' for events that apply to a known finite subset — useful when the event is valid in some non-terminal states but NOT in terminals (since 'from any' would include them). Prefer the explicit multi-state list when the source set is finite and the exclusion matters; use 'from any' only when the event truly applies regardless of state.",
            """
            # Form 1 — from any: applies in every state including terminals.
            from any on PedestrianRequest
                -> set RequestPending = true
                -> no transition

            # Form 2 — multi-state source list: applies to a known finite subset only.
            # Excludes Closed (terminal) and Cancelled (terminal); 'from any' would
            # incorrectly allow Close from those states too.
            from Monitoring, InControl, Warning, OutOfControl, CorrectiveAction on RetireChart
                -> set RetiredAt = now()
                -> transition Retired

            # Multi-state source list with terminal source: closing from two non-terminal states.
            from Passed, Failed on Close
                -> set DispositionNote = Close.Note
                -> transition Closed
            """,
            IsFragment: true),

        new(
            "Host-driven event dispatch by calendar window",
            "When a state transition depends on which side of a calendar deadline 'today' falls on (cancellation free window, lease grace period, course add/drop window, renewal grace), Precept does NOT expose 'today()' — calendar-vs-now arithmetic is a host concern. The idiomatic shape: declare distinct events for the different windows ('CancelWithFullRefund' / 'CancelWithPartialRefund' / 'CancelNoRefund', or 'RenewOnTime' / 'RenewInGracePeriod') AND a host-fired window-closing event ('CloseFreeWindow', 'CloseGracePeriod') that flips a boolean flag. Subsequent transitions guard on the flag, not on the calendar. The host picks which event to fire by consulting the clock; the precept governs the structural consequence.",
            """
            field FreeCancellationWindowOpen as boolean default true
            field PartialRefundWindowOpen as boolean default true

            state Booked initial
            state Cancelled terminal

            # Host fires these as the calendar crosses each boundary.
            event CloseFreeWindow
            event ClosePartialRefundWindow

            from Booked on CloseFreeWindow
                -> set FreeCancellationWindowOpen = false
                -> no transition

            from Booked on ClosePartialRefundWindow
                -> set PartialRefundWindowOpen = false
                -> no transition

            # The host picks the right cancel event based on the current date.
            # Each row guards on flags, not on calendar arithmetic.
            event CancelWithFullRefund
            event CancelWithPartialRefund(Reason as string notempty)
            event CancelNoRefund(Reason as string notempty)

            from Booked on CancelWithFullRefund when FreeCancellationWindowOpen
                -> transition Cancelled
            from Booked on CancelWithFullRefund
                -> reject "Free cancellation window has closed"

            from Booked on CancelWithPartialRefund when PartialRefundWindowOpen and not FreeCancellationWindowOpen
                -> transition Cancelled
            from Booked on CancelWithPartialRefund
                -> reject "Partial-refund window does not apply at this time"

            from Booked on CancelNoRefund when not PartialRefundWindowOpen
                -> transition Cancelled
            from Booked on CancelNoRefund
                -> reject "Cancel with full or partial refund is still available"
            """),

        new(
            "Lookup with membership (parallel set + lookup)",
            "When you need a key→value mapping AND a way to test/iterate membership, pair a 'lookup of K to V' with a 'set of K' that carries the membership truth. The set supports 'contains', '.count', and 'remove'; the lookup supports 'put Key = Value' and '(lookup for Key)' for retrieval. Both fields stay in sync via the same transition rows. Why this idiom: 'contains' is not currently defined on lookup directly, and 'remove' on a lookup has a sharp edge (see docs/Working/bugs.md BUG-002), so the parallel set is the canonical workaround AND a clearer model of intent — membership and quantity are separately readable. Use this for any 'collection of named items each with a quantity / weight / price' situation.",
            """
            # Two fields that move together: ComponentPartNumbers is the membership set,
            # Components is the per-component quantity.
            field ComponentPartNumbers as set of string
            field Components as lookup of string to integer

            event AddComponent(PartNumber as string notempty, Quantity as integer positive)
            event RemoveComponent(PartNumber as string notempty)

            from Draft on AddComponent when ComponentPartNumbers contains AddComponent.PartNumber
                -> reject "Component {AddComponent.PartNumber} already present — use UpdateComponentQuantity to change the quantity"
            from Draft on AddComponent
                -> add ComponentPartNumbers AddComponent.PartNumber
                -> put Components AddComponent.PartNumber = AddComponent.Quantity
                -> no transition

            from Draft on RemoveComponent when ComponentPartNumbers contains RemoveComponent.PartNumber
                # BUG-002: 'remove Components Key' fails type check on lookups; workaround is zero + drop membership.
                -> put Components RemoveComponent.PartNumber = 0
                -> remove ComponentPartNumbers RemoveComponent.PartNumber
                -> no transition
            from Draft on RemoveComponent
                -> reject "Component {RemoveComponent.PartNumber} is not in the BOM"
            """,
            IsFragment: true),

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
            """,
            IsFragment: true),

        new(
            "Optional-with-fallback assignment",
            "When an event parameter is 'optional', use 'if Param is set then Param else fallback' in a 'set' action to provide a safe default without a separate guard row. The entire expression stays inline — no extra transition row is needed. Multiple fallback tiers can be chained with additional 'else if' clauses.",
            """
            event Approve(Amount as money in 'USD', Note as string optional)

            from UnderReview on Approve when DocumentsVerified and CreditScore >= 680
                -> set ApprovedAmount = Approve.Amount
                -> set DecisionNote = if Approve.Note is set then Approve.Note else if CreditScore >= 750 then "Prime tier — auto-approved" else "Standard tier — approved"
                -> transition Approved
            """,
            IsFragment: true),

        new(
            "Conditional rule (rule when)",
            "A global invariant that only applies when a guard condition is true. Use 'rule Expression when Condition because ...' when a constraint is only meaningful once the entity reaches a certain state. The runtime skips the rule entirely while the guard is false, preventing spurious violations during early lifecycle stages.",
            """
            # Skipped until DocumentsVerified = true; enforced on every operation thereafter.
            rule ExistingDebt <= AnnualIncome * 3.0 when DocumentsVerified because "Debt {ExistingDebt} exceeds the 3x income ceiling — maximum is {AnnualIncome * 3.0}"
            """,
            IsFragment: true),

        new(
            "State-scoped editing window",
            "Declare a window of mutability for specific fields only while the entity is in a given state. 'in State modify Fields editable' is lifecycle-aware: the editing window closes the moment the state changes. An optional 'when Condition' narrows the window further to a runtime guard within the state. Distinct from 'writable', which is a stateless per-field flag with no lifecycle awareness.",
            """
            # All five fields are editable while in Draft; window closes on Submit.
            in Draft modify ApplicantName, MonthlyIncome, RequestedRent, CreditScore, HouseholdSize editable

            # Conditional editing window — only open once DocumentsVerified is true.
            in UnderReview when DocumentsVerified modify DecisionNote editable
            """,
            IsFragment: true),

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
            """,
            IsFragment: true),
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
            "'->' is the transition outcome arrow and is not valid in a field declaration. Use '<-' to declare a computed field's derivation formula.",
            BadCompilesClean: false),

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
            "Today the parser accepts the first comparison and produces a boolean (`0 <= Amount`), then the second comparison type-errors as `boolean <= 1000`, so precept_compile currently emits PRE0018 rather than a parser diagnostic. PRE0010 (NonAssociativeComparison) is the intended diagnostic and should fire once the parser detects chained comparisons directly. Use 'and' to combine two separate comparison conditions.",
            BadCompilesClean: false),

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
            "Computed fields (declared with '<-') are read-only by definition. Their value is recalculated from the formula whenever A changes. Attempting to 'set' a computed field produces a ComputedFieldNotWritable error.",
            BadCompilesClean: false),

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
            "Approve from Draft and from Approved adds rows for events with no meaning in those states — no UI should offer an Approve button there, and no row is the correct way to say so. The only reject that belongs here is the fallback from Submitted when the applicant fails the income and credit check — that is a condition the applicant could potentially remedy. Structurally inapplicable events need no row.",
            IsFragment: true),

        new(
            "Hollow draft state",
            "Declaring a `state X initial` when the entity has no `editable` fields in that state and the first event provides all field values atomically as parameters. The initial state adds no governance — the entity does not meaningfully exist there.",
            """
            precept LoanApplication

            field ApplicantName as string optional
            field RequestedAmount as money in 'USD' optional
            field CreditScore as integer optional

            state Draft initial
            state UnderReview
            state Approved terminal

            event Submit(Applicant as string notempty, Amount as money in 'USD', Score as integer)
            on Submit ensure Submit.Amount > '0.00 USD' because "Loan amount must be positive"

            from Draft on Submit
                -> set ApplicantName = Submit.Applicant
                -> set RequestedAmount = Submit.Amount
                -> set CreditScore = Submit.Score
                -> transition UnderReview
            """,
            """
            precept LoanApplication

            field ApplicantName as string
            field RequestedAmount as money in 'USD'
            field CreditScore as integer

            state UnderReview initial
            state Approved terminal

            event Submit(Applicant as string notempty, Amount as money in 'USD', Score as integer) initial
            on Submit ensure Submit.Amount > '0.00 USD' because "Loan amount must be positive"

            on Submit
                -> set ApplicantName = Submit.Applicant
                -> set RequestedAmount = Submit.Amount
                -> set CreditScore = Submit.Score
            """,
            "The hollow draft state contributes no governance. A draft implies the entity exists with progressive enrichment — fields being set over time, rules applying, editability windows governing what is allowed. When the initial state has zero `editable` declarations and the first event fills everything atomically, the \"draft\" is structurally identical to not existing at all. Use the constructor pattern (Pattern A): mark the initial event with `initial`, write a construction row with `on EventName { }`, and let the entity arrive in its first real state fully formed. The tell: if your initial state has zero `editable` declarations and the first event provides all field values as parameters, it is a hollow draft. Either add `editable` fields with real governance to the initial state (Pattern B), or drop the hollow state and mark the event `initial` (Pattern A)."),

        new(
            "Sub-state encoded as a field-presence guard",
            "Using a self-looping 'commit' event in a draft state that records a timestamp (or sets some flag), then guarding every subsequent state-changing transition with 'when XxxAt is set' to gate 'has the draft been committed yet.' The guard is doing the work that a state should be doing. If 'uncommitted draft' and 'committed draft' are distinct lifecycle positions (different editability, different available transitions), they are distinct states — make the sub-state real with an explicit transition.",
            """
            precept InspectionPlan

            field ProductId as string optional maxlength 50
            field InspectorName as string optional maxlength 100
            field ScheduledAt as instant optional

            state Scheduled initial
            state InProgress
            state Passed terminal

            in Scheduled modify ProductId, InspectorName, ScheduledAt editable

            event ScheduleInspection
            on ScheduleInspection ensure ProductId is set because "Product ID required before scheduling"
            on ScheduleInspection ensure InspectorName is set because "Inspector required before scheduling"

            event BeginInspection

            # Self-loop commits the draft by setting a timestamp.
            from Scheduled on ScheduleInspection
                -> set ScheduledAt = now()
                -> no transition

            # Guard now has to gate "commit happened yet?" — but the state machine is the wrong place for this.
            from Scheduled on BeginInspection when ScheduledAt is set
                -> transition InProgress
            from Scheduled on BeginInspection
                -> reject "Inspection not yet scheduled — call ScheduleInspection first"
            """,
            """
            precept InspectionPlan

            field ProductId as string optional maxlength 50
            field InspectorName as string optional maxlength 100
            field ScheduledAt as instant optional

            state Draft initial
            state Scheduled
            state InProgress
            state Passed terminal

            in Draft modify ProductId, InspectorName editable

            event ScheduleInspection
            on ScheduleInspection ensure ProductId is set because "Product ID required before scheduling"
            on ScheduleInspection ensure InspectorName is set because "Inspector required before scheduling"

            event BeginInspection

            # Commit IS a transition. The state machine carries the sub-flow.
            from Draft on ScheduleInspection
                -> set ScheduledAt = now()
                -> transition Scheduled

            # No guard needed — the state machine guarantees we only fire BeginInspection
            # from Scheduled, which by construction means ScheduleInspection already ran.
            from Scheduled on BeginInspection
                -> transition InProgress
            """,
            "When you find yourself writing `when XxxField is set` as the guard on a state-changing transition, and the only purpose of that guard is to gate 'has the user completed this sub-flow,' you are using field presence to encode a sub-state. That is the wrong mechanism. States are Precept's structural device for capturing distinct lifecycle positions with distinct rules and editability windows. If 'uncommitted' and 'committed' are distinct in any meaningful way (different events available, different fields editable, different ensures applicable), they are distinct states — make the sub-state real. Bonus payoff: the `in Draft modify` editing window automatically closes on the transition out of Draft, which the self-loop pattern cannot achieve without an additional conditional editability clause that further compounds the workaround."),

        new(
            "Reading uninitialized field in construction row",
            "Using a field's current value on the right-hand side of its first construction assignment, as though construction were incrementally updating an already-existing entity. Construction is the first write — there is no prior business value to read.",
            """
            precept CounterExample

            field Counter as integer

            event Create(InitialCount as integer) initial

            on Create
                -> set Counter = Counter + 1
            """,
            """
            precept CounterExample

            field Counter as integer

            event Create(InitialCount as integer) initial

            on Create
                -> set Counter = Create.InitialCount
            """,
            "`Counter` on the right-hand side is not reading a meaningful prior business value — construction is the first write. In today's precept_compile behavior, this exact integer example is accepted with no PRE-code and reads the field's pre-write default instead of reporting a dedicated undefined-read diagnostic, so the bug is semantic rather than compiler-blocked. Use the event payload (or a value established earlier in the same action chain) for the field's first assignment.",
            BadCompilesClean: false),
    ];
}
