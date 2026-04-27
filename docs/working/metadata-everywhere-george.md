# Metadata Everywhere: Runtime and Evaluator Design
## Full Execution Layer Vision

**Status:** Working design — pending cross-review with Frank's architectural design  
**Author:** George, Runtime Dev  
**Date:** 2026-04-26  
**Audience:** Implementers, runtime engineers  
**Scope:** Evaluator, proof engine, TypedModel node shapes, dispatch tables, fault bridge

---

## Preamble

The current state: we have kind enums everywhere (`ActionKind`, `OperationKind`, `FunctionKind`), we have comprehensive metadata in catalog static methods (`Actions.GetMeta()`, `Operations.GetMeta()`, `Functions.GetMeta()`), but the executable model layer is missing. When a statement node arrives at the evaluator, we don't yet carry the kind enum or a descriptor. The proof engine is a stub. The TypedModel is a stub.

**Correctness axioms (non-negotiable):**
- Compile-time obligations are exhaustive. If the proof engine emits zero diagnostics, the evaluator **must not fault** on any proof requirement. This is PRECEPT0001/0002 territory — a compile-time error corresponds 1:1 to a FaultCode that can't fire at runtime.
- Metadata is the source of truth. The evaluator is stateless machinery reading kind enums and dispatching through metadata — never hardcoding per-kind behavior.
- No runtime re-checking of statically proven obligations. If the proof engine already proved `divisor != 0`, the evaluator doesn't re-check the divisor before dividing.

---

## 1. Action Execution: Descriptor-Keyed Dispatch

### Current State (Problem)

Parse tree carries `SetStatement`, `AddStatement`, etc. — distinct sealed record types with no shared kind discriminant. To reach `ActionMeta`, every consumer type-switches:

```csharp
var meta = statement switch
{
    SetStatement s => Actions.GetMeta(ActionKind.Set),
    AddStatement a => Actions.GetMeta(ActionKind.Add),
    // ... duplicated everywhere
};
```

This is hostile to metadata-driven execution.

### Fix: ActionKind on Statement Base

```csharp
public abstract record Statement(ActionKind Kind, SourceSpan Span);

public sealed record SetStatement(
    ActionKind Kind,    // Always ActionKind.Set
    SourceSpan Span,
    Expression Target,
    Expression Value) : Statement(Kind, Span);

public sealed record AddStatement(
    ActionKind Kind,    // Always ActionKind.Add
    SourceSpan Span,
    Expression Target,
    Expression Value) : Statement(Kind, Span);

// ... similarly for Remove, Enqueue, Dequeue, Push, Pop, Clear
```

The parser already knows the kind when it constructs the node — it just wasn't recording it.

### Evaluator Dispatch Table

```csharp
public static class Evaluator
{
    private delegate void ActionExecutor(WorkingCopy copy, Statement stmt, ActionMeta meta);

    private static readonly FrozenDictionary<ActionKind, ActionExecutor> ActionDispatchTable =
        BuildActionDispatchTable();

    private static FrozenDictionary<ActionKind, ActionExecutor> BuildActionDispatchTable()
    {
        var table = new Dictionary<ActionKind, ActionExecutor>();
        foreach (var kind in Enum.GetValues<ActionKind>())
        {
            table[kind] = kind switch
            {
                ActionKind.Set     => ExecuteSet,
                ActionKind.Add     => ExecuteAdd,
                ActionKind.Remove  => ExecuteRemove,
                ActionKind.Enqueue => ExecuteEnqueue,
                ActionKind.Dequeue => ExecuteDequeue,
                ActionKind.Push    => ExecutePush,
                ActionKind.Pop     => ExecutePop,
                ActionKind.Clear   => ExecuteClear,
                _ => throw new InvalidOperationException($"Unknown ActionKind: {kind}"),
            };
        }
        return table.ToFrozenDictionary();
    }

    internal static void ExecuteActionChain(
        WorkingCopy copy,
        IReadOnlyList<Statement> actions,
        IReadOnlyDictionary<string, object?> context)
    {
        foreach (var stmt in actions)
        {
            var meta = Actions.GetMeta(stmt.Kind);
            ActionDispatchTable[stmt.Kind](copy, stmt, meta);
        }
    }
}
```

**Critical:** The dispatch table is compiled once at startup. At runtime: read `stmt.Kind`, look up in frozen table, call handler. Zero per-invocation branching. If a new `ActionKind` is added to the enum, the exhaustive switch in `BuildActionDispatchTable` fails to compile until handled.

### ActionMeta Integration

- **Compile-time:** Type checker reads `ActionMeta.ApplicableTo`, verifies target field type is compatible.
- **Runtime:** Evaluator does **not** re-verify `ApplicableTo` — that's a compiler guarantee. The evaluator carries `ActionMeta` for context-rich fault reporting only.
- **Proof requirements:** `Dequeue` and `Pop` have `ProofRequirements` in their `ActionMeta`. The proof engine proves them at compile time. The evaluator trusts they hold.

---

## 2. Expression Evaluation with OperationKind and FunctionKind

### Binary Expressions

The type checker produces `TypedModel` with `OperationKind` on binary expressions:

```csharp
public sealed record TypedBinaryExpression(
    TypeKind ResultType,
    SourceSpan Span,
    TypedExpression Lhs,
    OperatorKind Op,
    TypedExpression Rhs,
    OperationKind Kind) : TypedExpression(ResultType, Span);
```

Evaluator dispatch:

```csharp
private delegate object? BinaryOperationExecutor(object? lhs, object? rhs);

private static readonly FrozenDictionary<OperationKind, BinaryOperationExecutor> BinaryOperationTable =
    BuildBinaryOperationTable();

private static object? EvaluateTypedBinaryExpression(TypedBinaryExpression expr, WorkingCopy copy)
{
    var lhsValue = Evaluate(expr.Lhs, copy);
    var rhsValue = Evaluate(expr.Rhs, copy);
    return BinaryOperationTable[expr.Kind](lhsValue, rhsValue);
}

private static FrozenDictionary<OperationKind, BinaryOperationExecutor> BuildBinaryOperationTable()
{
    var table = new Dictionary<OperationKind, BinaryOperationExecutor>();

    table[OperationKind.IntegerPlusInteger]  = (l, r) => (long)(l!) + (long)(r!);
    table[OperationKind.IntegerMinusInteger] = (l, r) => (long)(l!) - (long)(r!);
    table[OperationKind.IntegerTimesInteger] = (l, r) => (long)(l!) * (long)(r!);
    table[OperationKind.IntegerDivideInteger] = (l, r) => {
        if ((long)(r!) == 0)
            Fail(FaultCode.DivisionByZero);  // should never fire — proof engine proved it
        return (long)(l!) / (long)(r!);
    };
    // ... 200+ entries

    return table.ToFrozenDictionary();
}
```

**Does the evaluator re-check proof requirements?** No. But defensive checks remain as bug-catchers — if one fires, it's a proof engine bug, not a user error.

### Function Calls

```csharp
public sealed record TypedFunctionCall(
    TypeKind ResultType,
    SourceSpan Span,
    string FunctionName,
    IReadOnlyList<TypedExpression> Args,
    FunctionKind Kind,
    int OverloadIndex) : TypedExpression(ResultType, Span);

private delegate object? FunctionExecutor(params object?[] args);

private static readonly FrozenDictionary<FunctionKind, FunctionExecutor> FunctionDispatchTable =
    BuildFunctionDispatchTable();

private static object? EvaluateTypedFunctionCall(TypedFunctionCall expr, WorkingCopy copy)
{
    var argValues = expr.Args.Select(arg => Evaluate(arg, copy)).ToArray();
    return FunctionDispatchTable[expr.Kind](argValues);
}
```

---

## 3. The Proof Engine Pipeline

### Contract

```csharp
public static class ProofEngine
{
    // Walks the typed tree, collects proof obligations from metadata,
    // attempts to prove each one using static analysis, emits diagnostics
    // for obligations that cannot be proven.
    public static ProofModel Prove(TypedModel model, GraphResult graph)
        => throw new NotImplementedException();
}
```

### Walk & Collect

```csharp
private static void CollectObligations(TypedModel model, List<ProofObligation> out_obligations)
{
    foreach (var expr in WalkExpressionsInModel(model))
    {
        switch (expr)
        {
            case TypedBinaryExpression binExpr:
            {
                var opMeta = Operations.GetMeta(binExpr.Kind);
                foreach (var req in opMeta.ProofRequirements ?? [])
                    out_obligations.Add(new ProofObligation(req, binExpr, "binary operation"));
                break;
            }
            case TypedUnaryExpression uniExpr:
            {
                var opMeta = Operations.GetMeta(uniExpr.Kind);
                foreach (var req in opMeta.ProofRequirements ?? [])
                    out_obligations.Add(new ProofObligation(req, uniExpr, "unary operation"));
                break;
            }
            case TypedFunctionCall funcCall:
            {
                var funcMeta = Functions.GetMeta(funcCall.Kind);
                var overload = funcMeta.Overloads[funcCall.OverloadIndex];
                foreach (var req in overload.ProofRequirements ?? [])
                    out_obligations.Add(new ProofObligation(req, funcCall, "function call"));
                break;
            }
        }
    }

    foreach (var stmt in WalkStatementsInModel(model))
    {
        var actionMeta = Actions.GetMeta(stmt.Kind);
        foreach (var req in actionMeta.ProofRequirements ?? [])
            out_obligations.Add(new ProofObligation(req, stmt, "action"));
    }
}
```

Zero hardcoded obligations. Every obligation comes from catalog metadata.

### Static Proof Attempts (Conservative)

For each obligation, attempt to prove it statically:

```csharp
private static bool TryProveNumericRequirement(
    NumericProofRequirement req,
    ProofContext ctx,
    out ProofCertificate? cert)
{
    // Simple case: literal constant
    if (GetSubjectExpression(req.Subject) is TypedLiteral lit && lit.Value is decimal d)
    {
        bool holds = req.Comparison switch
        {
            OperatorKind.NotEquals          => d != req.Threshold,
            OperatorKind.GreaterThan        => d > req.Threshold,
            OperatorKind.GreaterThanOrEqual => d >= req.Threshold,
            OperatorKind.LessThan           => d < req.Threshold,
            OperatorKind.LessThanOrEqual    => d <= req.Threshold,
            _ => false,
        };
        if (holds) { cert = new ProofCertificate(req, "Proven by literal constant"); return true; }
        return false;  // literal violates — emit diagnostic
    }

    // Conservative: field or expression — can't statically prove
    cert = null;
    return false;
}
```

If proof fails, emit a diagnostic suggesting a guard. The proof engine never executes expressions — fast, deterministic, bounded.

---

## 4. The Fault Bridge: Metadata-Rich Fault Context

### Enhanced Fault Context

```csharp
public sealed record FaultContext(
    FaultCode Code,
    object?[] MessageArgs,
    ActionKind? ActionKind = null,
    OperationKind? OperationKind = null,
    FunctionKind? FunctionKind = null,
    string? FieldName = null,
    string? EventName = null,
    Type? DeclaredFieldType = null,
    object? FailedValue = null);

public sealed record Fault(FaultCode Code, string Message, FaultContext Context);
```

Every `FaultCode` carries `[StaticallyPreventable(DiagnosticCode)]`. At runtime a fault can now be traced back to: the action executing, the operation that triggered the proof requirement, the field involved, and the value that failed.

### Fault Path Example

1. **Compile time:** Proof engine identifies `Divisor must be non-zero` for `OperationKind.IntegerDivideInteger`. If no guard establishes this, diagnostic is emitted.
2. **Runtime:** If somehow reached (compiler bug), evaluator fires `Fail(FaultCode.DivisionByZero, ...)` with `OperationKind: IntegerDivideInteger`.
3. **Consumer:** Examines fault, reads `Diagnostics.GetMeta(fault.DiagnosticCode)` — the compile-time message that should have blocked this.

---

## 5. TypedModel Node Shape

### Complete Node Definitions

```csharp
public sealed record TypedModel(
    ImmutableArray<TypedDeclaration> Declarations,
    ImmutableArray<Diagnostic> Diagnostics);

public abstract record TypedDeclaration(SourceSpan Span);
public abstract record TypedStatement(ActionKind Kind, SourceSpan Span);
public abstract record TypedExpression(TypeKind ResultType, SourceSpan Span);

// ── Declarations ──────────────────────────────────────────────────────────────

public sealed record TypedFieldDeclaration(
    SourceSpan Span,
    string Name,
    TypeKind Type,
    IReadOnlyList<ModifierKind> Modifiers,
    TypedExpression? DefaultValue,
    IReadOnlyList<TypedConstraint> Constraints) : TypedDeclaration(Span);

public sealed record TypedEventDeclaration(
    SourceSpan Span,
    string Name,
    IReadOnlyList<TypedEventArg> Args,
    IReadOnlyList<TypedStatement> Handler) : TypedDeclaration(Span);

public sealed record TypedTransitionRow(
    SourceSpan Span,
    string FromState,
    string EventName,
    TypedExpression? Guard,
    IReadOnlyList<TypedStatement> Actions,
    string ToState) : TypedDeclaration(Span);

// ── Statements ────────────────────────────────────────────────────────────────

public sealed record TypedSetStatement(
    ActionKind Kind, SourceSpan Span,
    FieldReference Target,
    TypedExpression Value) : TypedStatement(Kind, Span);

public sealed record TypedAddStatement(
    ActionKind Kind, SourceSpan Span,
    FieldReference Target,
    TypedExpression Value) : TypedStatement(Kind, Span);

// ... TypedRemoveStatement, TypedEnqueueStatement, TypedDequeueStatement,
//     TypedPushStatement, TypedPopStatement, TypedClearStatement

// ── Expressions ───────────────────────────────────────────────────────────────

public sealed record TypedLiteral(
    TypeKind ResultType, SourceSpan Span, object? Value) : TypedExpression(ResultType, Span);

public sealed record TypedFieldAccess(
    TypeKind ResultType, SourceSpan Span,
    FieldReference Field) : TypedExpression(ResultType, Span);

public sealed record TypedBinaryExpression(
    TypeKind ResultType, SourceSpan Span,
    TypedExpression Lhs,
    OperatorKind Op,
    TypedExpression Rhs,
    OperationKind Kind) : TypedExpression(ResultType, Span);

public sealed record TypedUnaryExpression(
    TypeKind ResultType, SourceSpan Span,
    OperatorKind Op,
    TypedExpression Operand,
    OperationKind Kind) : TypedExpression(ResultType, Span);

public sealed record TypedFunctionCall(
    TypeKind ResultType, SourceSpan Span,
    string FunctionName,
    IReadOnlyList<TypedExpression> Args,
    FunctionKind Kind,
    int OverloadIndex) : TypedExpression(ResultType, Span);

public sealed record TypedMemberAccess(
    TypeKind ResultType, SourceSpan Span,
    TypedExpression Receiver,
    TypeAccessor Accessor,
    TypedExpression? AccessorArg = null) : TypedExpression(ResultType, Span);

public sealed record TypedConditional(
    TypeKind ResultType, SourceSpan Span,
    TypedExpression Condition,
    TypedExpression Then,
    TypedExpression Else) : TypedExpression(ResultType, Span);

// ── Supporting types ──────────────────────────────────────────────────────────

public sealed record FieldReference(
    string FieldName,
    TypeKind Type,
    int SlotIndex,             // O(1) array index into working copy
    IReadOnlyList<ModifierKind> Modifiers);

public sealed record TypedEventArg(
    string Name,
    TypeKind Type,
    int SlotIndex,
    IReadOnlyList<ProofRequirement> Constraints);
```

### Key Design Decisions

1. **Parallel tree, not wrapped.** TypedModel is a fresh AST — parse tree is discarded after type checking. Evaluator never needs the parse tree.
2. **SlotIndex on FieldReference.** Working copy is a flat array. `workingCopy[fieldRef.SlotIndex] = value` — O(1) field access, no dictionary lookup at runtime.
3. **OperationKind + FunctionKind on expression nodes.** No runtime lookup needed; kind is already resolved.
4. **ModifierKind[] on FieldReference.** Evaluator reads modifiers for constraint checking without re-translating tokens.

---

## 6. Runtime Proof Certificates

### The Concept

The proof engine could emit `ProofCertificate` records for successfully proven obligations:

```csharp
public sealed record ProofCertificate(
    ProofRequirement Requirement,
    string ProofMethod,   // "LiteralConstant", "FieldModifier", "GuardInPath"
    object? EvidenceData = null);

public sealed record ProofModel(
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableArray<ProofCertificate> Certificates);
```

### Verdict: Emit but Don't Wire Yet

1. Emit certificates from the proof engine — the proof logic exists anyway, certificates are free.
2. Store them in `ProofModel`.
3. **Do NOT** wire them into the evaluator yet. Evaluators remain defensive.
4. Profile later: if checks are hot in tight loops, add optional fast-path certificate checks.

The guard structure already provides most of the benefit: if a guard prevents the operation when the requirement fails, the evaluator never reaches the risky path.

---

## 7. Evaluator Architecture: Static + Executable Model

```csharp
public static class Evaluator
{
    // Dispatch tables — compiled once at startup, frozen, thread-safe
    private static readonly FrozenDictionary<ActionKind, ActionExecutor>      ActionDispatchTable;
    private static readonly FrozenDictionary<OperationKind, BinaryOpExecutor> BinaryOpsTable;
    private static readonly FrozenDictionary<FunctionKind, FunctionExecutor>  FunctionsTable;

    // Core entry points
    internal static EventOutcome Fire(
        Precept model,
        Version version,
        string eventName,
        IReadOnlyDictionary<string, object?> args)
    {
        var eventDecl = model.ResolveEvent(eventName);
        var typedEventDecl = model.TypedModel.Events[eventDecl.Index];
        var workingCopy = WorkingCopy.From(version);

        if (typedEventDecl.Guard != null)
        {
            var guardResult = EvaluateExpression(typedEventDecl.Guard, workingCopy, args);
            if (!(bool)guardResult!)
                return new EventOutcome.Rejected("Guard failed");
        }

        ExecuteActionChain(workingCopy, typedEventDecl.Actions, args);

        var violations = EvaluateConstraints(model, workingCopy);
        if (violations.Any())
            return new EventOutcome.ConstraintsFailed(violations);

        return new EventOutcome.Applied(Version.From(workingCopy));
    }
}
```

### State Split: Precept (compile-time) + Version (runtime instance)

```csharp
public sealed class Precept
{
    public TypedModel TypedModel { get; private set; }
    public ProofModel ProofModel { get; private set; }
    public IReadOnlyDictionary<string, int> FieldSlotMap  { get; private set; }
    public IReadOnlyDictionary<string, int> EventSlotMap  { get; private set; }
    public IReadOnlyDictionary<string, int> StateSlotMap  { get; private set; }
}

public sealed record Version(
    Precept Precept,
    string? State,
    object?[] FieldValues);    // indexed by SlotIndex

internal sealed class WorkingCopy
{
    private readonly object?[] _fields;
    public object? GetField(int slotIndex) => _fields[slotIndex];
    public void SetField(int slotIndex, object? value) => _fields[slotIndex] = value;

    public static WorkingCopy From(Version version)
        => new WorkingCopy(version.Precept, (object?[])version.FieldValues.Clone());
}
```

One `Precept` instance serves all entity versions. Thread-safe, cached, never mutated.

---

## 8. End-to-End Concrete Example

**DSL:**
```precept
from Draft on Submit when Amount > 0 -> set Status = "Submitted" -> transition InReview
```

**Parse tree:**
```csharp
new TransitionRow(
    FromState: "Draft", EventName: "Submit",
    Guard: new BinaryExpression(
        Lhs: new FieldAccess("Amount"),
        Op: OperatorKind.GreaterThan,
        Rhs: new IntegerLiteral(0)),
    Actions: [new SetStatement(ActionKind.Set, ...)],
    ToState: "InReview");
```

**Typed tree (after type checker):**
```csharp
new TypedTransitionRow(
    FromState: "Draft", EventName: "Submit",
    Guard: new TypedBinaryExpression(
        Lhs: new TypedFieldAccess(TypeKind.Integer,
            new FieldReference("Amount", TypeKind.Integer, slotIndex: 0, modifiers: [])),
        Op: OperatorKind.GreaterThan,
        Rhs: new TypedLiteral(TypeKind.Integer, value: 0),
        Kind: OperationKind.GreaterThanIntegerInteger,  // resolved
        ResultType: TypeKind.Boolean),
    Actions: [
        new TypedSetStatement(ActionKind.Set,
            Target: new FieldReference("Status", TypeKind.String, slotIndex: 2, ...),
            Value: new TypedLiteral(TypeKind.String, value: "Submitted"))
    ],
    ToState: "InReview");
```

**Proof engine:** `GreaterThanIntegerInteger` has no proof requirements. `Set` on String has no proof requirements. Zero diagnostics.

**Runtime execution:**
```csharp
// 1. Evaluate guard
var guardPasses = BinaryOpsTable[OperationKind.GreaterThanIntegerInteger](
    workingCopy[0],  // Amount
    0);

if (!guardPasses) return Rejected("Guard failed");

// 2. Execute action
ActionDispatchTable[ActionKind.Set](workingCopy, typedSetStatement, setMeta);
// → workingCopy[2] = "Submitted"

// 3. Evaluate constraints
var violations = EvaluateConstraints(precept, workingCopy);

// 4. Return
return EventOutcome.Transitioned(
    new Version(precept, "InReview", workingCopy.Snapshot()));
```

All operations dispatch through kind-based tables. All metadata from catalogs. No string-based branching. No ad-hoc logic.

---

## 9. Known Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Proof engine too conservative; valid operations rejected | Usability — unnecessary guards | Start with literals + field modifiers; expand static analysis over time |
| Dispatch table compilation at startup is expensive | Startup latency | Dispatch tables are small (<1MB); profile before optimizing |
| Adding a new operation requires dispatch table entry | Maintenance burden | Exhaustive switch catches missing entries at compile time |
| TypedModel parallel tree: parse tree and typed tree both in memory during type checking | Memory overhead | Discard parse tree immediately after TypedModel is produced |
| Proof certificates wired too early | Unnecessary complexity | Don't wire until profiling justifies it |

---

## 10. Correctness Invariants

These must hold before any runtime ships:

1. **Compile-time safety → runtime safety:** If `ProofModel.Diagnostics` is empty, no `Fault` with a `StaticallyPreventable` code fires at runtime.
2. **Metadata is source of truth:** Every action, operation, and function behaves per its `*Meta` definition; no hardcoded special cases in the evaluator.
3. **Executor purity:** `Evaluator` static methods produce the same result for identical inputs; no hidden state, no side effects to shared structures.
4. **Kind enum coverage:** Every `ActionKind`, `OperationKind`, and `FunctionKind` has an exhaustive match in dispatch tables; missing entries are compile errors.

---

## 11. Precedent Survey

| System | Dispatch Approach | Kind Enums | Proof Model |
|--------|-------------------|-----------|-------------|
| **CEL** | Operator registry (`map[string]OperationFn`) | Ad-hoc; no exhaustive enum | None |
| **CUE** | Pattern dispatch on `Value` type | Yes | Constraint solver (optional) |
| **XState** | Event registry (`map[string]Transition`) | Yes (action type) | None |
| **Roslyn** | Visitor dispatch on node type | Yes (`SyntaxKind` enum) | Flow-sensitive type narrowing |

Precept's design: Roslyn-style dispatch (kind enums + frozen tables) + CUE-style metadata (structured requirements) + CEL-style simplicity (stateless evaluation).

---

**George's closing note:** This design is correct and complete. It is not the fastest possible runtime — a JIT-compiled evaluator would be faster — but it is the clearest, most maintainable, and most obviously correct. Correctness is non-negotiable in a domain integrity engine. The performance profile will be determined by profile-guided optimization; the architecture does not preclude it.

**End of George's Design Document**
