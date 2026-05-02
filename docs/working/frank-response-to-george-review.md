# Frank's Response to George's Type Checker Design Review

**From:** Frank (Lead/Architect)
**Date:** 2026-05-02T16:05:34-04:00
**Re:** `docs/working/george-type-checker-review.md`

---

## 1. Overall Assessment

George's review is excellent. It's the kind of review that saves weeks of mid-implementation rework. He caught real problems — not style nitpicks, not hypothetical concerns, but concrete structural issues that would have manifested as compile errors, silent incorrect behavior, or architectural debt the moment real code met the design document.

The six concrete problems are all legitimate. Five are accepted outright or with minor modification. One (Gap 5) is a non-finding because the fix already shipped — which is itself useful to document. George's pre-requisite list is correct: I should have specified these as gating conditions, not left them implicit.

Quality: **A-tier review.** George read the source code, verified my claims against actual APIs, and identified the exact semantic gap (BinaryIndex returns `BinaryOperationMeta[]`, not a single entry). That's the critical-path finding. Everything else is important but that one could have produced silent incorrect qualification logic.

---

## 2. Per-Finding Response

### Finding 1: BinaryIndex returns `BinaryOperationMeta[]`, not single entry — **ACCEPT**

George is completely right. I designed against an API that doesn't exist.

**Source verification:** `Operations.cs` line 1111:
```csharp
public static FrozenDictionary<(OperatorKind, TypeKind, TypeKind), BinaryOperationMeta[]> BinaryIndex { get; }
```

And the convenience wrapper at line 1153:
```csharp
public static ReadOnlySpan<BinaryOperationMeta> FindCandidates(OperatorKind op, TypeKind lhs, TypeKind rhs)
```

My design proposed `BinaryBySignature` returning a single `BinaryOperationMeta` — this doesn't exist and *shouldn't* exist. The multi-entry case is real: money/money division has entries with `QualifierMatch.Same` and `QualifierMatch.Different` (Operations.cs lines 425/435, 504/514). Quantity operations have similar qualifier-disambiguated pairs.

**Design correction:** The type checker uses `Operations.FindCandidates()` directly. When the result span has length > 1, the checker applies qualifier disambiguation:

```csharp
// ~15 lines of structural logic after FindCandidates returns > 1 entry:
// 1. If any entry has QualifierMatch.Same, check that operand qualifiers match
// 2. If qualifiers match → select the QualifierMatch.Same entry
// 3. If qualifiers differ → select the QualifierMatch.Different entry (if present)
// 4. If no match → emit QualifierMismatch diagnostic
```

This is genuine checker logic — the catalog *can't* do it because qualifier identity requires knowing the actual field qualifiers at the expression site. George correctly identifies this as the one point where "pure catalog lookup" has a small structural-logic layer on top.

**No new catalog indexes needed.** `FindCandidates` and `FindUnary` are the right APIs. My proposal to add `BinaryBySignature` / `UnaryBySignature` is withdrawn.

---

### Finding 2: 7 AST nodes missing from Resolve pseudocode — **ACCEPT**

George is right. My pseudocode covered 10 expression forms; there are 13 `[HandlesCatalogMember]` annotations in `TypeChecker.cs` (line 18-31), and the glob of `SyntaxNodes/Expressions/` shows 16 files. The real `Resolve` function needs arms for:

| Missing Node | Verification | Slice Assignment |
|---|---|---|
| `IsSetExpression` | File exists: `SyntaxNodes/Expressions/IsSetExpression.cs` | Slice 6 (Structural Validation) — operand must be optional |
| `IsNotSetExpression` | File exists: `SyntaxNodes/Expressions/IsNotSetExpression.cs` | Slice 6 — same |
| `CIFunctionCallExpression` | Parser.Expressions.cs:269 creates it | Slice 8 (CI enforcement) |
| `MethodCallExpression` | File exists: `SyntaxNodes/Expressions/MethodCallExpression.cs` | Slice 3 (Functions + Accessors) |
| `InterpolatedStringExpression` | File exists | Slice 3 or new Slice 3a (see §3.5 below) |
| `InterpolatedTypedConstantExpression` | File exists | Slice 4 (Typed Constants) |
| `TypedConstantExpression` | File exists | Slice 4 |

**Line count revision:** George's estimate of 250-350 lines is realistic. My ~100-line claim was optimistic pseudocode, not an implementation estimate. The real Resolve function will be 250-350 lines with all 16+ arms.

**Stub strategy:** Every node type that won't be implemented in its slice must have an explicit stub arm that returns `TypedError` with a `NotYetImplemented` marker. No switch fallthrough, no crash. This is required from Slice 2 onward.

---

### Finding 3: SemanticIndex record types must be pre-Slice 0 — **ACCEPT**

George is absolutely right. My Slice 10 placement was wrong. You can't write a `TypedField` test in Slice 1 if `TypedField` doesn't exist as a type definition.

**Design correction:** Pre-Slice 0 commits the full record type hierarchy:
- All `TypedField`, `TypedState`, `TypedEvent`, `TypedArg` records
- The `TypedExpression` DU (abstract base + all subtypes)
- The `TypedAction` DU (3-shape hierarchy)
- `TypedTransitionRow`, `TypedEnsure`, `TypedRule`, `TypedAccess`
- `CheckContext` (see §4.1)

This is a pure shape commit — no logic, no resolution, no tests beyond "it compiles." Every subsequent slice compiles against these types.

My Slice 10 ("Final SemanticIndex construction and immutable sealing") becomes just the final assembly logic — taking the mutable working state and producing the immutable `SemanticIndex` record. That's still a real slice but it's much smaller than what I had there.

---

### Finding 4: `TypedInputAction.SecondaryExpression` needs a role discriminator — **ACCEPT WITH MODIFICATION**

George identifies a real problem. A single `TypedExpression? SecondaryExpression` is insufficient for the Evaluator because it can't dispatch without back-referencing `ActionKind`.

However, I **reject George's "named per-role fields" alternative** (separate `IndexExpression?`, `KeyExpression?`, `PriorityExpression?`). That's three nullable fields where at most one is populated — exactly the shape the metadata-driven architecture considers a smell. If we add a new action syntax shape that has a secondary expression, we'd need to add another nullable field to the record. That doesn't scale.

**I accept George's other proposal:** Keep a single `SecondaryExpression?` field but add an explicit role discriminator:

```csharp
public sealed record TypedInputAction(
    ActionKind Kind,
    TypedExpression InputExpression,
    TypedExpression? SecondaryExpression,
    ActionSecondaryRole? SecondaryRole,  // null iff SecondaryExpression is null
    SyntaxNode Syntax
) : TypedAction(Kind, Syntax);

public enum ActionSecondaryRole
{
    Index,     // insert ... at <index>
    Key,       // put ... key <key>, appendBy <key>, enqueueBy <key>
    Priority   // enqueueBy ... priority <priority> (future, if distinguished from key)
}
```

The enum is the correct metadata shape: it carries role semantics, the Evaluator switches on it, and it extends cleanly when new action shapes arrive. The invariant `SecondaryRole.HasValue == SecondaryExpression != null` is enforced at construction time.

**Note on Priority vs Key:** Currently `EnqueueBy` uses the secondary for priority ordering — I'm collapsing Key and Priority into one arm unless the Evaluator genuinely needs to distinguish them. If it does, the enum has the extension point. Start with `Index` and `Key` only.

---

### Finding 5: Gap 5 (pow ProofRequirement) already closed — **ACCEPT (non-finding)**

Correct. GAP-032 was fixed 2026-05-02 (my own fix, recorded in history.md). The `NumericProofRequirement(PPowIntExp, GreaterThanOrEqual, 0m, ...)` is already on the Integer^Integer overload in `Functions.cs`. 

**Design update:** Remove Gap 5 from the active blocker list. The type checker simply reads the existing `ProofRequirement` from `FunctionMeta.Overloads[n].ProofRequirements` during function resolution (Slice 3). No implementation gap remains.

---

### Finding 6: `[HandlesCatalogMember]` stub migration — **ACCEPT**

George catches a real oversight. The current `TypeChecker.cs` (lines 18-31) has 13 `[HandlesCatalogMember]` annotations on the dead-letter `CheckExpression()` stub. PRECEPT0019 enforces that each `ExpressionFormKind` member is covered by exactly one handler method. If Slice 2 adds a real `ResolveBinaryExpression()` with `[HandlesCatalogMember(ExpressionFormKind.BinaryOperation)]` but doesn't remove it from the stub, PRECEPT0019 fires a duplicate-coverage error.

**Design correction:** Each slice's first step is:
1. Remove the relevant `[HandlesCatalogMember(ExpressionFormKind.X)]` from the stub
2. Add it to the real handler method
3. Verify PRECEPT0019 doesn't fire

When the last annotation leaves the stub, delete the `CheckExpression()` method entirely.

This is mechanical but it must be documented per-slice to prevent CI failures.

---

## 3. Pre-Requisites Resolution

### Pre-req 1: Field ordering decision — **DECIDED**

George's recommendation is correct. Use the `Functions.ByName` pattern:

```csharp
// Primary: ordered array (preserves declaration order)
ImmutableArray<TypedField> Fields { get; }

// Secondary: derived frozen lookup (O(1) by name)
FrozenDictionary<string, TypedField> FieldsByName { get; }
```

The same pattern for states, events, args. `ImmutableDictionary` is **not used** as primary storage anywhere in the SemanticIndex.

**Rationale:**
- Declaration order matters for "prior fields only" scope (§3.5 default value forward-reference prohibition)
- Declaration order matters for LS hover and MCP compile output (users expect source order)
- `FrozenDictionary` gives O(1) lookup for resolution
- This is the exact pattern `Functions.ByName` uses (`FrozenDictionary<string, FunctionMeta[]>` derived from the ordered `All` array)

---

### Pre-req 2: Pre-Slice 0 commit record type definitions — **DECIDED**

Accepted. The pre-Slice 0 commit contains:
- `SemanticIndex.cs` expanded with the full record hierarchy (or split across `TypedExpressions.cs`, `TypedActions.cs`, `TypedDeclarations.cs` if the file exceeds 300 lines)
- `CheckContext` internal class definition (see §4.1)
- No logic. No tests beyond build verification.
- This commit unblocks all numbered slices.

---

### Pre-req 3: SecondaryExpression role clarity — **DECIDED**

See Finding 4 above. Single `SecondaryExpression?` + `ActionSecondaryRole?` enum. Two initial members: `Index`, `Key`. Invariant: `SecondaryRole.HasValue == (SecondaryExpression != null)`.

---

### Pre-req 4: MethodCallExpression dispatch strategy — **DECIDED**

`MethodCallExpression` resolves via **TypeMeta accessor lookup**, not as a standalone function dispatch. Confirmation:

The current Precept surface has method-call syntax only for collection accessors (`queue.peek()`, `stack.peek()`, `log.latest()`). These are `TypeAccessor` entries on the container's `TypeMeta`. The resolution path is:

1. Resolve receiver expression → get receiver TypeKind
2. Look up `Types.GetMeta(receiverKind).Accessors` (or equivalent accessor catalog — TBD in pre-Slice 0 shapes)
3. Match method name against accessor names
4. Return accessor's result type

If we ever add user-defined methods (unlikely given §0.4.1 "no general-purpose computation"), this extends. For now: accessor lookup.

**Slice assignment:** Slice 3 (Functions + Accessors). The method-call arm in Resolve is just the accessor path with the receiver expression resolved first.

---

### Pre-req 5: Interpolated string slice assignment — **DECIDED**

- `InterpolatedStringExpression` → **Slice 3** (alongside functions/accessors). It's simple: resolve each hole expression, verify each is scalar, result is `TypeKind.String`. ~20 lines.
- `InterpolatedTypedConstantExpression` → **Slice 4** (Typed Constants). Same mechanics but the result type is context-determined. Depends on Slice 4's typed-constant resolution infrastructure.

George's alternative of "Slice 8a" is overkill. Interpolated strings are not string-CI operations — they belong with the general expression resolution machinery.

---

## 4. Additional Items

### 4.1 CheckContext — mutable working state

George is right that this needs explicit design before Slice 1. Here's the contract:

```csharp
internal sealed class CheckContext
{
    // Symbol tables (populated in Pass 1)
    public List<TypedField> Fields { get; } = [];
    public Dictionary<string, TypedField> FieldLookup { get; } = new();
    public List<TypedState> States { get; } = [];
    public Dictionary<string, TypedState> StateLookup { get; } = new();
    public List<TypedEvent> Events { get; } = [];
    public Dictionary<string, TypedEvent> EventLookup { get; } = new();

    // Current scope (for Pass 2)
    public IReadOnlyDictionary<string, TypedArg>? CurrentEventArgs { get; set; }
    public int CurrentFieldIndex { get; set; } = -1;  // for "prior fields only" scope

    // Diagnostics accumulator
    public List<Diagnostic> Diagnostics { get; } = [];

    // Binding accumulator (for the SemanticIndex bindings section)
    public List<TypedBinding> Bindings { get; } = [];
}
```

This goes in the pre-Slice 0 commit. It's internal to the type checker, not part of the `SemanticIndex` public shape.

---

### 4.2 TypedTransitionRow.FromState — nullable string sentinel

George identifies a real anti-pattern concern. My decision:

**Option A: nullable string (`string? FromState` where null = "any").** This is correct and sufficient.

I reject Option B (the `StateTarget` DU). Rationale:
- The "any" wildcard is exactly one sentinel value with exactly one consumer (GraphAnalyzer's transition keying)
- A DU for a binary discriminator that will never gain a third case is over-abstraction
- The anti-mirroring rule says `SemanticIndex` shapes are driven by consumer needs — GraphAnalyzer needs to filter "any-state rows" vs "named-state rows," and `== null` is as clear as `is AnyState`
- The doc must explicitly state: **`null` = "any-state wildcard" (the row fires in any source state)**

Decision: `string? FromState` with a mandatory XML doc comment encoding the null-means-any convention.

---

### 4.3 Gap 1 (ContentValidation) — discriminated shape

George is right that the flat `ContentValidation(Pattern, Examples, FormatDescription)` is undertyped. The checker would still need a per-type switch to know *how* to validate.

**Accept George's DU proposal with one modification:**

```csharp
public abstract record ContentValidation(string FormatDescription, string[] Examples);
public sealed record RegexValidation(string Pattern, string FormatDescription, string[] Examples) : ContentValidation(FormatDescription, Examples);
public sealed record NodaTimeValidation(string NodaTimePattern, string FormatDescription, string[] Examples) : ContentValidation(FormatDescription, Examples);
public sealed record ClosedSetValidation(string SetName, string FormatDescription, string[] Examples) : ContentValidation(FormatDescription, Examples);
```

- `RegexValidation` — for freeform patterns (string-typed constants with format constraints)
- `NodaTimeValidation` — date, time, datetime, period types (delegates to NodaTime's pattern parser)
- `ClosedSetValidation` — currency (ISO 4217), unit (UCUM) (membership check against a known set)

The `DelegateName` in George's proposal is too stringly-typed. The DU subtypes *are* the dispatch mechanism — the checker pattern-matches on the subtype, not on a delegate name string. Each subtype carries exactly the metadata its validation strategy needs.

**Implementation note:** This catalog shape change is a separate PR from the type checker slices. Slice 4 depends on it. If it's not landed before Slice 4, Slice 4 uses a hardcoded per-TypeKind dispatch table with a TODO referencing this gap — exactly as George suggests. This is acceptable temporary scaffolding.

---

### 4.4 Qualifier propagation through expressions

George is correct that my design is silent on this. My position:

**Qualifier propagation is a type-checker concern but not a Slice 1-4 concern.** Here's why:

The type checker resolves `TypeKind` and validates type-correctness of operations. Qualifier identity (`"USD"`, `"EUR"`, `"kg"`) is a *runtime value* — the checker can't know which qualifier a field holds at type-check time. The checker's job is:

1. When `FindCandidates` returns multiple entries disambiguated by `QualifierMatch`, the checker validates that the *structure* is qualifier-compatible (same-qualifier required → operand qualifiers must match → emit diagnostic if provably incompatible)
2. The result type carries a `QualifierBinding?` — either `SameAsOperand(operandRef)` or `None`

The **ProofEngine** (not the type checker) handles the deeper obligation: "prove that these two money values have the same currency before allowing addition." The type checker emits a `ProofObligation` record that the ProofEngine must discharge.

**Design addition to SemanticIndex:** `TypedBinaryExpression` carries an optional `QualifierBinding?`:
```csharp
public sealed record TypedBinaryExpression(
    OperatorKind Op,
    TypedExpression Left,
    TypedExpression Right,
    TypeKind ResultType,
    QualifierBinding? ResultQualifier,  // propagated qualifier identity
    SyntaxNode Syntax
) : TypedExpression(ResultType, Syntax);

public abstract record QualifierBinding;
public sealed record InheritedQualifier(string FieldName) : QualifierBinding;  // result inherits qualifier from named field
public sealed record SameQualifierRequired : QualifierBinding;  // both operands must have same qualifier; result inherits
```

This goes in the pre-Slice 0 shapes. Implementation of qualifier checking goes in Slice 2 (binary operations) — it's the disambiguation logic from Finding 1.

---

### 4.5 Error recovery shape — partial results policy

George's question: does a resolution error in a guard produce a `TypedTransitionRow` with a `TypedError` guard, or skip the row?

**Decision: always produce the partial result.** The type checker accumulates diagnostics without abandoning. The rule is:

> Any sub-expression that fails resolution is replaced with `TypedErrorExpression` (carrying the diagnostic and the source span). The containing declaration is still emitted to the SemanticIndex. Downstream stages (GraphAnalyzer, ProofEngine) must handle `TypedErrorExpression` gracefully — typically by skipping proof obligations on that expression but still analyzing the structural topology of the transition.

This is consistent with the existing `type-checker.md` principle: "accumulate all diagnostics without abandoning the pass."

**Per-declaration behavior:**
- `TypedField` with a failed default expression → emitted, default = `TypedErrorExpression`
- `TypedTransitionRow` with a failed guard → emitted, guard = `TypedErrorExpression`
- `TypedTransitionRow` with a failed action expression → emitted, action carries `TypedErrorExpression`
- `TypedEnsure` with a failed constraint body → emitted, body = `TypedErrorExpression`

No declaration type is ever skipped due to sub-expression errors.

---

### 4.6 ImmutableDictionary loses declaration order

Resolved in Pre-req 1. Array-primary + frozen dict secondary. No `ImmutableDictionary` in the public SemanticIndex shape.

---

## 5. Updated Design Decisions

| Decision | Original | Revised |
|---|---|---|
| Catalog lookup API | New `BinaryBySignature` / `UnaryBySignature` | Use existing `FindCandidates()` / `FindUnary()` directly |
| BinaryIndex disambiguation | Not addressed | ~15 lines qualifier-match logic after multi-candidate return |
| SemanticIndex record placement | Slice 10 | Pre-Slice 0 commit |
| Field storage | `ImmutableDictionary<string, TypedField>` | `ImmutableArray<TypedField>` primary + `FrozenDictionary` secondary |
| TypedInputAction secondary | Single nullable, no discriminator | Single nullable + `ActionSecondaryRole?` enum |
| HandlesCatalogMember stubs | Not addressed | Per-slice migration: remove from stub, add to real handler |
| Resolve line count | ~100 lines | ~250-350 lines (16+ arms) |
| Gap 5 (pow) | Active blocker | Closed — already in `Functions.cs` |
| ContentValidation shape | Flat record | DU: Regex / NodaTime / ClosedSet subtypes |
| TypedTransitionRow.FromState | Unspecified | `string?` with null = "any" convention (documented) |
| Qualifier propagation | Not addressed | `QualifierBinding?` on `TypedBinaryExpression`; proof obligations for qualifier compatibility |
| Error recovery | Implicit | Always produce partial result; `TypedErrorExpression` replaces failed sub-expressions |
| Interpolated string | No slice | `InterpolatedStringExpression` in Slice 3, `InterpolatedTypedConstantExpression` in Slice 4 |
| MethodCallExpression | Not addressed | Accessor-style lookup via TypeMeta; Slice 3 |

---

## 6. Revised Vertical Slice Plan

### Pre-Slice 0: Shape Commit (unblocks everything)
- Full `TypedField`, `TypedState`, `TypedEvent`, `TypedArg` record definitions
- `TypedExpression` DU (all subtypes including `TypedErrorExpression`)
- `TypedAction` DU (3 shapes + `ActionSecondaryRole` enum)
- `TypedTransitionRow`, `TypedEnsure`, `TypedRule`, `TypedAccess`
- `QualifierBinding` DU
- `CheckContext` internal class
- `SemanticIndex` expanded to hold the typed inventories
- Build verification only — no logic, no behavioral tests

### Slice 1: Symbol Tables (Pass 1)
- Field/state/event/arg registration into `CheckContext`
- Duplicate-name detection
- Initial/terminal state counting
- `[HandlesCatalogMember]` stub migration: none needed (registration doesn't cover expression forms)

### Slice 2: Scalar Expression Resolution — Binary & Unary Ops
- `Resolve()` function with arms for: `LiteralExpression`, `IdentifierExpression`, `BinaryExpression`, `UnaryExpression`, `ParenthesizedExpression` (grouped)
- `Operations.FindCandidates()` + qualifier disambiguation logic
- `Operations.FindUnary()` integration
- ErrorType propagation
- Stub arms for all other expression forms → `TypedErrorExpression`
- **Stub migration:** Remove `Literal`, `Identifier`, `BinaryOperation`, `UnaryOperation`, `Grouped` from CheckExpression stub

### Slice 3: Functions, Accessors, Method Calls, Interpolated Strings
- `CallExpression` → `Functions.ByName` lookup + overload resolution
- `MemberAccessExpression` → field ref or TypeAccessor lookup
- `MethodCallExpression` → TypeMeta accessor dispatch
- `InterpolatedStringExpression` → hole resolution, scalar check, result = string
- `PostfixOperation` (`.count`, etc.) — if these map to member-access
- **Stub migration:** Remove `FunctionCall`, `MethodCall`, `MemberAccess`, `PostfixOperation` from stub

### Slice 4: Typed Constants + Context-Sensitive Resolution
- `TypedConstantExpression` → context type propagation + content validation (hardcoded dispatch table if ContentValidation DU not yet landed)
- `InterpolatedTypedConstantExpression` → same with interpolation holes
- Numeric literal context resolution (propagate expected type downward)
- **Stub migration:** Remove no new forms (these reuse Literal arm with context)

### Slice 5: Transition Row Normalization
- Guard expression resolution (boolean result required)
- Action chain resolution (per `ActionSyntaxShape` → TypedAction DU)
- SecondaryExpression + SecondaryRole assignment
- Transition target validation (state name lookup)
- Partial result policy: failed guard/action → TypedErrorExpression, row still emitted

### Slice 6: Structural Validation
- `IsSetExpression` / `IsNotSetExpression` → operand must be optional field, result = bool
- Cycle detection in computed field dependencies
- Choice field validation (values are valid for the type, no duplicates)
- Forward-reference prohibition (default exprs reference only prior fields)
- **Stub migration:** Remove remaining expression form stubs; delete `CheckExpression()` method

### Slice 7: Modifier Validation
- Per-field modifier applicability (e.g., `notempty` only for string + 8 collection types)
- Modifier conflicts (e.g., `required` + `optional` on same field)
- State modifier validation (exactly one initial, at least one terminal)
- Reads `Modifiers` catalog metadata directly

### Slice 8: CI Enforcement + CIFunctionCall
- `CIFunctionCallExpression` resolution → subject must be `~string` type, lookup `Functions.ByName` for CI variant
- CI operator validation (case-insensitive comparison on ~string operands)
- Diagnostic for CI function on non-~string operand
- **Stub migration:** Remove `CIFunctionCall` from stub (if not already removed)

### Slice 9: Quantifiers + List Literals
- `QuantifierExpression` → binding variable scoping, predicate must be boolean, collection operand validation
- `ListLiteralExpression` → element type unification, result = inferred collection type
- **Stub migration:** Remove `Quantifier`, `ListLiteral` from stub

### Slice 10: Final Assembly
- `CheckContext` → immutable `SemanticIndex` transformation
- Array-to-frozen-dict derivation for lookup indexes
- Dependency fact extraction
- Integration test: full precept file → complete SemanticIndex with all inventories populated

---

*End of response. George: proceed with Pre-Slice 0 shape definitions once this is acknowledged.*
