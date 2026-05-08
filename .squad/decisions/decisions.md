# Message-position catalog metadata closed

**Date:** 2026-05-08
**Sources:** `.squad/decisions/inbox/george-is-message-position.md`, `.squad/decisions/inbox/kramer-grammar-gen-message-position.md`
**Status:** Implemented and validated

## Summary

`IsMessagePosition` is now first-class catalog metadata on both `TokenMeta` and `FunctionMeta`, and the grammar generator now derives message-string gold patterns from that metadata instead of hardcoding `because` / `reject`.

## Decisions

1. Message-position awareness belongs in catalog metadata, not parser or grammar-generator keyword lists.
2. `TokenKind.Because` and `TokenKind.Reject` are the only current token entries that opt into `IsMessagePosition`.
3. `FunctionMeta` carries the same flag now so future built-ins with trailing user-facing message strings can participate without new generator hardcoding.
4. The grammar generator must read `Tokens.All.Where(m => m.IsMessagePosition)` and `Functions.All.Where(f => f.IsMessagePosition)` when building `messageStrings` patterns.

## Validation

- George added the metadata fields plus token flags; build and tests passed; commits `105a42a7` and `315b00c9`.
- Kramer wired the generator, removed the stale TODO, regenerated `precept.tmLanguage.json`, and verified a zero-diff output; commit `7f3842fd`.

# ProofEngine Design Decisions — PE-G1, PE-G2, PE-G3

**Date:** 2026-05-08
**Author:** Frank
**Resolves:** PE-G1 (three unhandled obligation kinds), PE-G2 (ProofDischarges catalog prereq), PE-G3 (ProofLedger divergence)
**Status:** DECISIONS MADE — pending Shane sign-off before spec update or implementation

## Summary

Deep source analysis of the five `ProofRequirementKind` values, the Operations catalog's actual usage, the TypeChecker's resolution pipeline, and the existing SemanticIndex contract reveals that all three blocking gaps are resolvable without new proof strategies. The three "unhandled" requirement kinds (Dimension, Modifier, QualifierCompatibility) are all field-declaration-attribute checks — they belong in an expanded Strategy 2 that reads qualifier bindings alongside modifiers. The `ProofDischarge` catalog prerequisite is well-scoped: 6 of 15 `FieldModifierMeta` entries carry concrete discharges. The `ProofLedger` output type needs ~6 new record types but the spec's shape is sound — the only revisions are `ConstraintIdentity` field-name corrections to match the source-of-truth `SemanticIndex.cs` definitions.

---

## PE-G1a: DimensionProofRequirement

**Obligation:** "The period operand must have the required time dimension (Date or Time) for the arithmetic operation to be semantically valid."

**Source:** `ProofRequirement.cs` lines 81–85. `DimensionProofRequirement(ProofSubject Subject, PeriodDimension RequiredDimension, string Description)`. The `PeriodDimension` enum has three values: `Any`, `Date`, `Time`.

**Catalog usage:** `Operations.cs` lines 248, 257, 275, 284 — four temporal arithmetic entries:
- `DatePlusPeriod` / `DateMinusPeriod` → require `PeriodDimension.Date`
- `TimePlusPeriod` / `TimeMinusPeriod` → require `PeriodDimension.Time`

**TypeChecker analysis:** The TypeChecker resolves qualifier bindings on field declarations (`TypedField.Qualifier`) and operation results (`TypedBinaryOp.ResultQualifier`). Period fields accept qualifiers on the `TemporalDimension` axis (`period of 'date'`, `period of 'time'`) and the `TemporalUnit` axis (`period in 'days'`). The qualifier binding is resolved at type-checking time and available in `TypedField.Qualifier`. The TypeChecker does NOT validate the dimension constraint itself — it stamps the `DimensionProofRequirement` from the `BinaryOperationMeta` catalog entry and defers to the proof engine. Grep for `Dimension`, `PeriodDimension`, `QualifierAxis` in `TypeChecker.cs` and `TypeChecker.Expressions.cs` returned no validation logic for this constraint. **Confirmed: the TypeChecker does not pre-discharge this.**

**Decision: B) Discharged by Strategy 2 (Declaration Attribute Proof), extended to read qualifier bindings.**

**Rationale:** The period field's qualifier binding on the `TemporalDimension` axis is a compile-time-known declaration attribute, structurally identical to a modifier. When the proof subject resolves to a field with `TypeKind.Period`, Strategy 2 reads `TypedField.Qualifier` and checks whether a qualifier on `QualifierAxis.TemporalDimension` maps to the required `PeriodDimension`:
- Qualifier value `"date"` → satisfies `PeriodDimension.Date`
- Qualifier value `"time"` → satisfies `PeriodDimension.Time`
- `PeriodDimension.Any` → always satisfied (any temporal dimension)
- No qualifier on `TemporalDimension` axis → obligation **unresolved** (period without dimension is ambiguous)

**Alternatives rejected:**
- _New Strategy 5_: Unnecessary — this is a field-declaration attribute check, exactly what Strategy 2 does. Adding a strategy for one requirement kind when the existing strategy can be extended is overengineering.
- _Pre-discharge by TypeChecker_: Would violate the catalog-driven architecture. The type checker stamps requirements, the proof engine discharges them. The type checker's job is operation selection and requirement attachment, not requirement evaluation.

**Tradeoff accepted:** Strategy 2 becomes slightly more complex — it dispatches on requirement kind (Numeric → ProofDischarge lookup, Dimension → qualifier binding check). This is a single `switch` arm, not a separate strategy.

**Spec update required:** `proof-engine.md` §7 Strategy 2 pseudocode: add a `DimensionProofRequirement` branch to `TryModifierProof` that reads the subject field's qualifier binding on `QualifierAxis.TemporalDimension` and compares against `RequiredDimension`. Add to the Strategy 2 coverage table.

---

## PE-G1b: ModifierRequirement

**Obligation:** "The field operand must declare the required modifier (e.g., `ordered`) for the operation to be valid."

**Source:** `ProofRequirement.cs` lines 112–116. `ModifierRequirement(ProofSubject Subject, ModifierKind Required, string Description)`.

**Catalog usage:** `Operations.cs` lines 760, 768, 776, 784 — four choice ordinal comparison entries (`ChoiceLessThan`, `ChoiceGreaterThan`, `ChoiceLessThanOrEqual`, `ChoiceGreaterThanOrEqual`) all declare `ModifierRequirement(PChoice, ModifierKind.Ordered, ...)`. Both operands share the same `PChoice` parameter reference, so the requirement applies to all matching operand positions.

**TypeChecker analysis:** The TypeChecker resolves choice operations via the Operations catalog and stamps the `ModifierRequirement` on the `TypedBinaryOp`. It does NOT check whether the field has the `ordered` modifier itself — that's deferred to the proof engine. Grep for `ModifierRequirement`, `CheckModifier`, `modifier.*check` in `TypeChecker.cs` returned no hits. **Confirmed: the TypeChecker does not pre-discharge this.**

**Decision: B) Discharged by Strategy 2 (Declaration Attribute Proof), via direct modifier presence check.**

**Rationale:** This is the simplest possible Strategy 2 case. The proof subject resolves to a field. Strategy 2 checks `field.Modifiers.Contains(requirement.Required)`. If the field has the `ordered` modifier, the obligation is discharged. If not, unresolved — emit diagnostic.

This is distinct from the `ProofDischarge` lookup path. `ProofDischarge` entries map modifiers → numeric/presence requirements they discharge (e.g., `positive` discharges `> 0`). `ModifierRequirement` is the inverse: it asserts that a specific modifier must be present on the field. Strategy 2 handles both paths:

1. **ProofDischarge path** (for `NumericProofRequirement`, `PresenceProofRequirement`): "Does any modifier on this field carry a `ProofDischarge` that covers this requirement?"
2. **Modifier presence path** (for `ModifierRequirement`): "Does this field have the required modifier?"

**Alternatives rejected:**
- _Pre-discharge by TypeChecker_: Same rationale as PE-G1a — type checker stamps requirements, proof engine discharges them.
- _Always a type error (Option C)_: Wrong — `ordered` is an optional modifier on choice fields. Not having it isn't a type error; it's a proof failure for ordinal operations specifically.

**Tradeoff accepted:** None significant. This is a trivial addition to Strategy 2.

**Spec update required:** `proof-engine.md` §7 Strategy 2 pseudocode: add a `ModifierRequirement` branch that checks `field.Modifiers.Contains(requirement.Required)`. Add to the Strategy 2 coverage table.

---

## PE-G1c: QualifierCompatibilityProofRequirement

**Obligation:** "Two operands in a binary operation must have matching qualifier values on the specified axis (e.g., both `quantity in 'kg'` or both `money in 'USD'`)."

**Source:** `ProofRequirement.cs` lines 96–101. `QualifierCompatibilityProofRequirement(ProofSubject LeftSubject, ProofSubject RightSubject, QualifierAxis Axis, string Description)`. This is the only dual-subject requirement kind.

**Catalog usage:** Extensively used in `Operations.cs`:
- **Quantity arithmetic** (lines 475, 484, 921–966): `QualifierAxis.Unit` — operands must have the same unit qualifier
- **Price arithmetic** (lines 557–570, 977–1023): Both `QualifierAxis.Unit` AND `QualifierAxis.Currency` — operands must match on both axes
- **Money arithmetic**: `QualifierAxis.Currency` (via `QualifierMatch.Same` entries)

**TypeChecker analysis:** The TypeChecker handles qualifier disambiguation at operation resolution time (`TypeChecker.Expressions.cs` lines 560–591). For multi-candidate operations, it defaults to `QualifierMatch.Same` — the structurally safe assumption. It maps this to `SameQualifierRequired` on `TypedBinaryOp.ResultQualifier` and explicitly comments: "ProofEngine will verify qualifier compatibility at deeper analysis" (line 573). **Confirmed: the TypeChecker defers qualifier verification to the proof engine.**

**Decision: B) Discharged by Strategy 2 (Declaration Attribute Proof), extended to read qualifier bindings on both operand fields.**

**Rationale:** Both operands' qualifier bindings are compile-time-known declaration attributes. The proof engine:
1. Resolves both subjects (`LeftSubject`, `RightSubject`) to their respective fields
2. Reads the qualifier binding on the specified `QualifierAxis` from each `TypedField.Qualifier`
3. If both fields have explicit qualifiers on that axis AND the values match → discharged
4. If either field lacks a qualifier on that axis → **unresolved** (cannot prove compatibility without declared qualifiers)
5. If both have qualifiers but they differ → **unresolved** (type-incompatible operation)

**Alternatives rejected:**
- _New Strategy 5 (Qualifier Strategy)_: Unnecessary — this is a field-declaration attribute comparison. Strategy 2 already reads field declarations. Adding the qualifier binding read is architecturally consistent with its existing responsibility.
- _Always a type error_: Wrong — the type checker intentionally defers this to the proof engine. Making it a type error would duplicate logic and violate the catalog-driven obligation model.
- _Runtime-only check_: Wrong — qualifier values are declaration-time constants (string literals in `in 'USD'`, `in 'kg'`). They're always statically knowable. Deferring to runtime would miss a guaranteed-provable obligation.

**Tradeoff accepted:** Strategy 2 now handles two structural patterns — single-subject (modifiers, qualifier, dimension) and dual-subject (qualifier compatibility). The implementation must check for `QualifierCompatibilityProofRequirement` specifically and resolve both subjects. This is a single additional branch, not a general multi-subject framework.

**Spec update required:** `proof-engine.md` §7 Strategy 2 pseudocode: add a `QualifierCompatibilityProofRequirement` branch that resolves both subjects, reads their qualifier bindings on the specified axis, and compares values. Add to the Strategy 2 coverage table. Update Strategy 2's name from "Modifier Proof" to "Declaration Attribute Proof" to reflect its expanded scope.

---

## PE-G2: ProofDischarge Catalog Design

### 1. ProofDischarge Record Type

```csharp
/// <summary>
/// Declares a proof obligation that a field modifier statically discharges.
/// Read by Strategy 2 of the proof engine — no per-modifier switch needed.
/// </summary>
public sealed record ProofDischarge(
    ProofRequirementKind RequirementKind,  // which obligation kind this discharges
    OperatorKind? Comparison,              // for Numeric: the comparison operator
    decimal? Threshold                     // for Numeric: the threshold value
                                           //   null = read from modifier's HasValue parameter
);
```

**Design rationale:** The `Threshold` field is nullable. For fixed-value modifiers (`positive`, `nonnegative`, `nonzero`, `notempty`), the threshold is a literal. For parameterized modifiers (`min(N)`, `max(N)`, `mincount(N)`, `maxcount(N)`), the threshold is `null`, signaling the proof engine to read the value from the field declaration's modifier parameter at proof time. This keeps the catalog entry declarative while supporting parameterized constraints.

### 2. FieldModifierMeta Update

Add `ProofDischarges` property to the existing `FieldModifierMeta` record in `Modifier.cs`:

```csharp
public sealed record FieldModifierMeta(
    ModifierKind Kind,
    TokenMeta Token,
    string Description,
    ModifierCategory Category,
    TypeTarget[] ApplicableTo,
    bool HasValue = false,
    ModifierKind[] Subsumes = default!,
    ProofDischarge[] ProofDischarges = default!,  // ← NEW
    string? HoverDescription = null,
    string? UsageExample = null,
    string? SnippetTemplate = null,
    ModifierKind[]? MutuallyExclusiveWith = null)
    : ModifierMeta(Kind, Token, Description, Category, MutuallyExclusiveWith)
{
    public ModifierKind[] Subsumes { get; init; } = Subsumes ?? [];
    public ProofDischarge[] ProofDischarges { get; init; } = ProofDischarges ?? [];
}
```

### 3. Modifier Entries Requiring ProofDischarges

| Modifier | `ProofDischarges` value | Rationale |
|---|---|---|
| `positive` | `[ProofDischarge(Numeric, GreaterThan, 0)]` | Field > 0 — subsumes `!= 0` and `>= 0` via `DischargeCovers` subsumption logic |
| `nonnegative` | `[ProofDischarge(Numeric, GreaterThanOrEqual, 0)]` | Field ≥ 0 |
| `nonzero` | `[ProofDischarge(Numeric, NotEquals, 0)]` | Field ≠ 0 |
| `notempty` | `[ProofDischarge(Numeric, GreaterThan, 0)]` | Collection count > 0 or string length > 0 |
| `min(N)` | `[ProofDischarge(Numeric, GreaterThanOrEqual, null)]` | Field ≥ N where N is modifier parameter |
| `max(N)` | `[ProofDischarge(Numeric, LessThanOrEqual, null)]` | Field ≤ N where N is modifier parameter |
| `minlength(N)` | `[ProofDischarge(Numeric, GreaterThanOrEqual, null)]` | String length ≥ N |
| `maxlength(N)` | `[ProofDischarge(Numeric, LessThanOrEqual, null)]` | String length ≤ N |
| `mincount(N)` | `[ProofDischarge(Numeric, GreaterThanOrEqual, null)]` | Collection count ≥ N |
| `maxcount(N)` | `[ProofDischarge(Numeric, LessThanOrEqual, null)]` | Collection count ≤ N |

**Modifiers with NO ProofDischarges (empty array):**

| Modifier | Why empty |
|---|---|
| `optional` | Does not *discharge* a proof obligation — its absence is what guarantees presence. Strategy 2 handles presence via the non-optional check, not via ProofDischarge. |
| `ordered` | Handled by the modifier-presence path of Strategy 2 (for `ModifierRequirement`), not via ProofDischarge entries. |
| `default(expr)` | Provides initial value — does not establish a runtime bound. |
| `maxplaces(N)` | No current proof obligation targets decimal-place constraints. |
| `writable` | Access control, not a value constraint. |

### 4. File Location

**New file: `src/Precept/Language/ProofDischarge.cs`.**

**Rationale:** `ProofDischarge` is a first-class catalog type shared between the modifier catalog (`Modifiers.cs`) and the proof engine (`ProofEngine.cs`). It belongs in `Language/` because it's catalog metadata, not pipeline logic. It gets its own file because it's a distinct record type with its own semantic purpose — nesting it inside `Modifier.cs` would bury it among the modifier DU hierarchy. This mirrors the pattern of `ProofRequirement.cs` (catalog metadata type) having its own file.

### 5. Catalog Architecture Compliance

Verified against `docs/language/catalog-system.md`:

- **ProofDischarges is catalog metadata.** It declares what a modifier *means* for the proof system. The proof engine reads it — it does not compute it. This is exactly the metadata-driven architecture: domain knowledge lives in the catalog, pipeline stages are generic readers.
- **No per-modifier switch in the proof engine.** Strategy 2 iterates `field.Modifiers`, reads `Modifiers.GetMeta(kind).ProofDischarges`, and calls `DischargeCovers`. No `ModifierKind.Positive => ...` switches anywhere in `ProofEngine.cs`.
- **Subsumption is a generic algorithm.** `DischargeCovers` performs comparison-operator subsumption (e.g., `> 0` covers `!= 0`). This logic is proof-engine-internal, not per-modifier — it works for any `ProofDischarge` entry regardless of which modifier declares it.

---

## PE-G3: ProofLedger Output Type

### New Record Types Needed

The spec's §5 Output defines 8 types. Current source has only `ProofLedger(ImmutableArray<Diagnostic> Diagnostics)`. The following types must be added:

#### 1. `ProofObligation` — `Pipeline/ProofLedger.cs`

```csharp
public sealed record ProofObligation(
    ProofRequirement Requirement,
    TypedExpression Site,
    ProofDisposition Disposition,
    ProofStrategy? Strategy,
    DiagnosticCode? EmittedDiagnostic
);
```

Dependencies: `ProofRequirement` (Language), `TypedExpression` (Pipeline/SemanticIndex.cs), `DiagnosticCode` (Language)

#### 2. `ProofDisposition` enum — `Pipeline/ProofLedger.cs`

```csharp
public enum ProofDisposition { Proved, Unresolved }
```

#### 3. `ProofStrategy` enum — `Pipeline/ProofLedger.cs`

```csharp
public enum ProofStrategy
{
    Literal,
    DeclarationAttribute,  // renamed from "Modifier" — covers modifiers, qualifiers, dimensions
    GuardInPath,
    FlowNarrowing
}
```

**Note:** Renamed from `Modifier` to `DeclarationAttribute` per PE-G1 decisions. The spec should be updated accordingly.

#### 4. `FaultSiteLink` — `Pipeline/ProofLedger.cs`

```csharp
public sealed record FaultSiteLink(
    ProofObligation Obligation,
    FaultCode FaultCode,
    DiagnosticCode DiagnosticCode,
    SourceSpan Site
);
```

Dependencies: `FaultCode` (Language)

#### 5. `ConstraintInfluenceEntry` — `Pipeline/ProofLedger.cs`

```csharp
public sealed record ConstraintInfluenceEntry(
    ConstraintIdentity Constraint,
    ImmutableArray<string> ReferencedFields,
    ImmutableArray<EventArgReference> ReferencedArgs
);

public sealed record EventArgReference(string EventName, string ArgName);
```

Dependencies: `ConstraintIdentity` (Pipeline/SemanticIndex.cs — shared type, already exists)

#### 6. `InitialStateSatisfiabilityResult` — `Pipeline/ProofLedger.cs`

```csharp
public sealed record InitialStateSatisfiabilityResult(
    string StateName,
    bool IsSatisfiable,
    ImmutableArray<UnsatisfiedConstraint> Violations
);

public sealed record UnsatisfiedConstraint(
    ConstraintIdentity Constraint,
    string Reason
);
```

#### 7. Updated `ProofLedger` — `Pipeline/ProofLedger.cs`

```csharp
public sealed record ProofLedger(
    ImmutableArray<ProofObligation> Obligations,
    ImmutableArray<FaultSiteLink> FaultSiteLinks,
    ImmutableArray<ConstraintInfluenceEntry> ConstraintInfluence,
    ImmutableArray<InitialStateSatisfiabilityResult> InitialStateResults,
    ImmutableArray<Diagnostic> Diagnostics
);
```

### Decision: Match the spec — the shape is sound

**Rationale:** The spec was written after the catalog architecture was established and correctly reflects what the Precept Builder needs from the proof engine:

- `Obligations` — complete audit trail (which obligations exist and how they were resolved)
- `FaultSiteLinks` — consumed by Precept Builder Pass 4 for `FaultSiteAnnotation` planting
- `ConstraintInfluence` — consumed by Precept Builder for `ConstraintInfluenceMap`
- `InitialStateResults` — consumed by diagnostics (unsatisfiable initial state is a compile-time error)
- `Diagnostics` — merged into the final diagnostic stream

None of these fields are overengineered. Each has a concrete downstream consumer documented in the spec.

**One revision:** The `ConstraintIdentity` subtypes in the spec differ from the source. The **source is correct** (it's the implemented, tested shape). The spec must be updated:

| Spec shape | Source shape | Verdict |
|---|---|---|
| `RuleIdentity(string RuleName, int Index)` | `RuleIdentity(int RuleIndex)` | **Source wins** — Precept rules are anonymous (no `RuleName`). The spec's `RuleName` field doesn't exist in the DSL surface. |
| `EnsureIdentity(ConstraintKind, string? AnchorState, string? AnchorEvent, int Index)` | `EnsureIdentity(ConstraintKind, string? AnchorName, int EnsureIndex)` | **Source wins** — `AnchorName` collapses state/event discrimination. The `ConstraintKind` already indicates whether the anchor is a state or event. |

### File organization

All new types go in `Pipeline/ProofLedger.cs` alongside the `ProofLedger` record. This follows the existing pattern: `SemanticIndex.cs` contains both the index record and all its constituent types (`TypedField`, `TypedState`, `TypedTransitionRow`, etc.). Putting `ProofObligation`, `FaultSiteLink`, etc. in `ProofLedger.cs` keeps the proof engine's output contract in one file.

Exception: `ProofDischarge` goes in `Language/ProofDischarge.cs` (catalog metadata, not pipeline output).

---

## Significant Gaps — Terse Verdicts

### SIG-1: Missing `AllTypedExpressions` API on `SemanticIndex`

**Verdict: SPEC UPDATE NEEDED**

The spec's Pass 1 pseudocode (line 967) iterates `semantics.AllTypedExpressions` — this property does not exist on `SemanticIndex`. The implementer must define a traversal method that walks all expression-bearing records (`TransitionRows` → actions/guards, `Rules` → conditions, `Ensures` → conditions, `ComputedDeps` → computed expressions, `StateHooks` → actions). This is an **implementer responsibility** — the traversal is mechanical and the implementer knows the SemanticIndex shape. The spec should note this as a "to be implemented" API rather than assuming it exists.

### SIG-2: `ConstraintIdentity` shape mismatch

**Verdict: SPEC UPDATE NEEDED**

Covered in PE-G3 above. The spec's `ConstraintIdentity` subtypes have fields that don't exist in the source (`RuleName`, separate `AnchorState`/`AnchorEvent`). The spec must be updated to match the source shapes: `RuleIdentity(int RuleIndex)` and `EnsureIdentity(ConstraintKind Kind, string? AnchorName, int EnsureIndex)`.

### SIG-3: Unspecified `FindEnclosingTransitionRow` helper

**Verdict: ACCEPT AS-IS**

This is a straightforward lookup: given a `TypedExpression`, find which `TypedTransitionRow` contains it. The implementer walks `SemanticIndex.TransitionRows` and checks whether any row's guard or action chain contains the expression (by reference identity or span containment). No design decision needed — it's a utility function, not an architectural concern. The spec correctly identifies it as a helper without over-specifying implementation.

### SIG-4: Unspecified `ResolveSubject` helper

**Verdict: ACCEPT AS-IS**

`ResolveSubject` maps a `ProofSubject` to a concrete `TypedExpression` node. For `ParamSubject(ParameterMeta)`, it matches the parameter by object identity against the expression's operands. For `SelfSubject`, it returns the receiver expression. Implementation is mechanical — the spec correctly leaves it to the implementer.

### SIG-5: Underspecified initial-state satisfiability

**Verdict: DESIGN DECISION REQUIRED — deferred**

The spec says to check whether initial-state constraints are satisfiable given default field values. This requires evaluating default expressions against constraint expressions — essentially a mini-evaluator at compile time. The spec's description (lines 866–883) is correct in intent but implementation is blocked pending the type checker's expression resolution engine being fully operational (as the spec itself notes on line 883). **Owner: spec author + implementer, post-TypeChecker completion.**

### SIG-6: Collection-empty obligation ownership ambiguity

**Verdict: ACCEPT AS-IS**

Collection non-empty obligations are declared in catalog metadata (`TypeAccessor.ProofRequirements`, `ActionMeta.ProofRequirements`). The type checker stamps them on `TypedMemberAccess` and `TypedAction` nodes. The proof engine discharges them via Strategy 1 (literal), Strategy 2 (`notempty` modifier), or Strategy 3 (`count > 0` guard). There is no ownership ambiguity — the catalog declares, the type checker stamps, the proof engine discharges. The spec's §7 "Collection Non-Empty Proof" section (lines 886–899) correctly describes the flow. No change needed.

### SIG-7: Guard decomposition rules

**Verdict: SPEC UPDATE NEEDED**

The spec's Strategy 3 pseudocode references `ExtractGuardConstraints(row.Guard)` but does not define the decomposition rules for complex guard expressions. The spec should specify:

1. **Supported connectives:** `and` decomposes into individual constraints (each arm of `A and B` is a separate constraint). `or` does NOT decompose (cannot prove either arm independently).
2. **Supported atomic forms:** `field OP literal`, `count(collection) > 0`, `collection.count > 0`, `field is set`, `field is not set`.
3. **Unsupported forms:** Function calls (other than `count`), nested expressions, field-vs-field comparisons (those are Strategy 4).

**Owner: spec author.** These rules define the proof engine's guard recognition language. They should be specified in the spec before implementation.

---

## Required Spec Updates (in order)

1. **§7 Strategy 2 — Rename and expand scope.** Rename from "Modifier Proof" to "Declaration Attribute Proof." Add three new branches to `TryModifierProof` (renamed to `TryDeclarationAttributeProof`):
   - `ModifierRequirement` → direct `field.Modifiers.Contains(requirement.Required)` check
   - `DimensionProofRequirement` → read `TypedField.Qualifier` on `QualifierAxis.TemporalDimension`, compare to `RequiredDimension`
   - `QualifierCompatibilityProofRequirement` → resolve both subjects, read qualifier bindings on specified axis, compare values

2. **§7 Strategy 2 — Update coverage table.** Add rows for Dimension, Modifier, and QualifierCompatibility requirement kinds.

3. **§7 Strategy 2 — Update `ProofDischarge` pseudocode.** Show `DischargeCovers` handling nullable `Threshold` (reads from modifier parameter for `HasValue` modifiers).

4. **§5 Output — Fix `ConstraintIdentity` shapes.** Replace `RuleIdentity(string RuleName, int Index)` with `RuleIdentity(int RuleIndex)`. Replace `EnsureIdentity(ConstraintKind, string? AnchorState, string? AnchorEvent, int Index)` with `EnsureIdentity(ConstraintKind, string? AnchorName, int EnsureIndex)`.

5. **§5 Output — Update `ProofStrategy` enum.** Rename `Modifier` to `DeclarationAttribute`.

6. **§7 Strategy 3 — Add guard decomposition rules.** Specify `and` connective decomposition, `or` non-decomposition, supported atomic guard forms.

7. **§9 — Add `AllTypedExpressions` note.** Document that `SemanticIndex` requires a traversal method/property to enumerate all typed expressions across all declaration kinds.

8. **§7 initial-state satisfiability — Add blocking dependency note.** Explicitly state that implementation is blocked pending TypeChecker expression evaluation capability.

---

## Required Catalog Changes (in order)

1. **Add `ProofDischarge.cs`** — new file in `src/Precept/Language/` containing the `ProofDischarge` record type.

2. **Update `FieldModifierMeta` in `Modifier.cs`** — add `ProofDischarge[] ProofDischarges = default!` parameter after `Subsumes`, with `ProofDischarges` property initialization `= ProofDischarges ?? []`.

3. **Update `Modifiers.cs` entries** — populate `ProofDischarges` on 10 modifier entries:
   - `Nonnegative`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.GreaterThanOrEqual, 0)]`
   - `Positive`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.GreaterThan, 0)]`
   - `Nonzero`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.NotEquals, 0)]`
   - `Notempty`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.GreaterThan, 0)]`
   - `Min`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.GreaterThanOrEqual, null)]`
   - `Max`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.LessThanOrEqual, null)]`
   - `Minlength`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.GreaterThanOrEqual, null)]`
   - `Maxlength`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.LessThanOrEqual, null)]`
   - `Mincount`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.GreaterThanOrEqual, null)]`
   - `Maxcount`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.LessThanOrEqual, null)]`

4. **Update `ProofLedger.cs`** — replace stub with full output contract (ProofObligation, ProofDisposition, ProofStrategy, FaultSiteLink, ConstraintInfluenceEntry, EventArgReference, InitialStateSatisfiabilityResult, UnsatisfiedConstraint).

---

## Shane Sign-Off Required On

- **Strategy 2 rename to "Declaration Attribute Proof"**: This broadens Strategy 2's scope from modifier-only to all field declaration attributes (modifiers, qualifiers, dimensions). The alternative is keeping the name "Modifier Proof" and adding separate subroutines for qualifier/dimension checks under the same strategy. The rename is more honest but changes the spec vocabulary. Shane should confirm the rename is acceptable.

- **`PeriodDimension.Any` behavior**: When a period field has no `TemporalDimension` qualifier, should the Dimension obligation be unresolved (forcing authors to always qualify their period fields for temporal arithmetic), or should unqualified periods be treated as `PeriodDimension.Any` (accepting any dimension)? Current decision: **unresolved** — the author must declare `period of 'date'` or `period of 'time'` for temporal arithmetic to be proven safe. This is the conservative choice but may be annoying for simple precepts.

- **SIG-5 initial-state satisfiability deferral**: This is marked as blocked pending TypeChecker expression evaluation. Should it be deferred entirely from the proof engine's initial implementation scope, or should a minimal version (literals-only default values against simple comparison constraints) be included in the first implementation?

# Shane Sign-Off — ProofEngine Design Decisions

**Date:** 2026-05-08
**Source:** Direct conversation with Shane

## Decision 1 — Strategy 2 Rename: APPROVED ✅

**Approved:** Rename Strategy 2 from "Modifier Proof" to "Declaration Attribute Proof."

Strategy 2's expanded scope (modifiers, qualifier bindings, and temporal dimension qualifiers) makes the rename accurate. The old name "Modifier Proof" was too narrow given the PE-G1 expansion.

## Decision 2 — Unqualified Period Behavior: Permissive ✅

**Approved:** Treat unqualified periods as `PeriodDimension.Any` — accept any dimension.

When a `period` field has no `TemporalDimension` qualifier (no `period of 'date'` or `period of 'time'`), the `DimensionProofRequirement` is considered **satisfied** rather than unresolved. This is the permissive choice — authors are not forced to qualify period fields for temporal arithmetic to be proven safe.

## Decision 3 — Initial-State Satisfiability: PENDING FRANK DEEP DIVE ⏸

Shane raised the question: "why not just use the evaluator?" instead of a mini-evaluator at compile time.

Frank has been tasked with a deep dive on this architectural question. Key questions:
1. Does the evaluator depend on compiled Compiler-stage output, or can it operate on SemanticIndex?
2. Can evaluation logic be shared between ProofEngine (compile-time) and Evaluator (runtime)?
3. What are the architectural implications of using the evaluator for initial-state satisfiability?
4. What is the recommended design?

**Status:** Blocked on Frank deep dive. No implementation decision authorized yet.

# Readability Review: combined-design-v2.md (2026-07-17)



**Reviewer:** Elaine (UX Designer)



**Doc:** `docs/working/combined-design-v2.md`



**Verdict:** APPROVED-WITH-CONCERNS



## Top 3 Findings



1. **Parser section needs shape specificity.** The section explains the parser's *philosophy* (source-faithful, recovery-aware) but doesn't give an implementer enough to know what SyntaxTree nodes to define. Missing: error recovery node shape, concrete node inventory or shape sketch, and explicit contract for how malformed input is represented. A parser design doc author would need to invent these from scratch.



2. **Missing navigation guide.** A 486-line doc serving two audiences (human implementers and AI agents) needs a "How to read this document" paragraph after the status block. Three sentences: what §1–§3 cover (commitments and pipeline overview), what §4–§8 cover (per-stage contracts), what §9–§12 cover (runtime and integration). This is the single highest-ROI addition for both audiences.



3. **"How it serves the guarantee" paragraphs become formulaic.** Useful for Lexer through Graph Analyzer. By Proof Engine and Lowering, the pattern is predictable and the content is restating the opening sentence. Recommendation: fold the guarantee connection into the stage's opening paragraph for §8–§10 and drop the separate labeled paragraph.



## Genre Assessment



The rewrite succeeds. §1 opens with a problem statement and architectural commitment, not an inventory. Per-stage sections lead with design decisions. The philosophy-first framing is consistent throughout. This is a design document, not a reference manual.



## Decision



This doc is ready to serve as the architectural foundation for per-stage design docs (starting with the parser). The concerns above are improvements, not blockers — the parser concern is the most urgent because that's the immediate next use case.



---



---



---

# Design Review: combined-design-v2.md — Soundness, Completeness, Innovation



**Reviewer:** Frank (Lead Architect)



**Date:** 2026-06-03



**Document:** `docs/working/combined-design-v2.md`



**Context:** Only the Lexer is implemented. All other pipeline stages are stubs.



---



## VERDICT: APPROVED-WITH-CONCERNS



The document is architecturally sound and well-structured. It reads as a unified design explanation rooted in philosophy, not a defense of two separate systems. The pipeline is coherent, the artifact boundaries are clean, and the lowered executable model is concrete enough to implement against. The concerns below are real design gaps that will cost us if we hit them mid-implementation rather than addressing them now.



---



## Soundness Issues



1. **The proof strategy set is closed but its coverage boundary is unstated.** The doc lists four strategies (literal, modifier, guard-in-path, flow narrowing) and says "any obligation outside this set is unresolvable." But it never states what percentage of real-world proof obligations these four strategies can discharge. If most `ProofRequirement` instances in practice require cross-field reasoning (e.g., `ApprovedAmount <= RequestedAmount`), then the four strategies are a beautiful design that rejects most real programs. The doc should include a coverage analysis against the sample corpus — even an informal one — so implementers know whether the strategy set is right-sized or whether a fifth strategy (e.g., relational pair narrowing) is needed before v1.



2. **`Restore` bypasses access-mode but evaluates constraints — the interaction with computed fields is unspecified.** If persisted data includes stale computed-field values, does Restore recompute before constraint evaluation? The recomputation index is listed as a Restore input, but the evaluation order (recompute → validate vs. validate → recompute) is not specified. Getting this wrong means Restore either rejects valid persisted data or accepts invalid computed values.



3. **The `Create` without initial event path evaluates `always` + `in <initial>` — but default values may not satisfy `in <initial>` constraints.** The doc doesn't specify whether this is a compile-time guarantee (the proof engine should catch it) or a runtime domain outcome. The static-reasoning research (C3) says this is a known check, but the combined design doesn't thread it through the proof/fault chain. An author who writes `field X as number default 0` and `in Draft ensure X > 5` gets no compile-time warning in the current design — only a runtime `EventConstraintsFailed` on create. That violates the prevention promise.



4. **`ConstraintActivation` discriminant is described but not typed.** The doc says it "distinguishes whether a constraint binds to the current state, the source state, or the target state." But it doesn't specify whether this is an enum, a DU, or a tag on the descriptor. Given the catalog-driven architecture, this should be cataloged — it's language-surface knowledge that consumers need, not an implementation detail.



---



## Completeness Gaps



1. **No error recovery strategy for the parser.** The doc says `SyntaxTree` preserves "recovery shape for broken programs" and mentions "missing-node representation," but there is no recovery algorithm specified. Panic-mode? Synchronization tokens? The parser recovery strategy directly affects LS quality — a bad recovery model means completions and diagnostics degrade on every keystroke. This is a design decision, not an implementation detail, and it should be locked before the parser is built.



2. **No incremental compilation model.** The doc treats the pipeline as a single-shot transformation: source → tokens → tree → model → graph → proof → CompilationResult. But the language server needs incremental re-analysis on every keystroke. The doc should specify the invalidation boundary — does a keystroke re-lex the whole file? Re-parse? Re-typecheck? For a single-file DSL this may be "just re-run everything" and that's fine — but say so explicitly, with a size-ceiling argument for why that's acceptable (the 64KB source limit helps here).



3. **No serialization contract for `Version`.** The doc specifies `Restore` as the reconstitution path, but never specifies what the caller provides. What is the serialization shape of a `Version`? Is it `(stateName, fieldValues)`? `(stateDescriptor, slotArray)`? The host application needs a defined contract for what to persist and what to hand back to `Restore`. Without it, every host will invent its own serialization and we'll get impedance mismatches.



4. **No definition versioning or migration story.** When a `.precept` file changes (field added, state renamed, constraint tightened), what happens to persisted `Version` instances compiled against the old definition? `Restore` will reject them if they don't satisfy the new constraints. The doc should at least name this as a known gap and specify whether migration is in-scope or explicitly deferred.



5. **No observability hooks.** The doc specifies structured outcomes and inspections, but no tracing, logging, or metric emission points. For a production runtime, host applications need to observe: which events fired, which constraints failed, how long evaluation took, which proof strategies were used. These hooks shape the evaluator's internal architecture — bolting them on later means refactoring the evaluator.



---



## Innovation Opportunities



1. **The proof engine should guarantee initial-state satisfiability at compile time.** The research base (static-reasoning-expansion.md, C3/C4/C5) already describes per-field interval analysis. The combined design should commit to a concrete compile-time guarantee: *if default field values and initial-state constraints are both statically known, the proof engine verifies satisfiability and emits a diagnostic if no valid initial configuration exists.* This is unique among DSL runtimes — no validator, state machine library, or rules engine provides this. It's the proof engine's signature contribution and it's achievable with the bounded strategy set already designed.



2. **Precompute a "constraint influence map" during lowering for AI-native inspection.** Currently the inspection API tells you *what* constraints are active and *whether* they passed. It doesn't tell you *which fields drive which constraints* — the dependency graph exists in the `TypedModel` but is not lowered into an inspectable form. If lowering also produces a `ConstraintInfluenceMap` (constraint → contributing fields, with expression-text excerpts), then an AI agent can answer "why did this constraint fail?" and "which field change would fix it?" without reverse-engineering expressions. This is a structural differentiator for the MCP surface.



3. **The executable model should be a compiled decision table, not a tree walk.** The doc says lowering produces "lowered expression nodes and action plans" but doesn't specify the execution model. For Precept's small, closed expression language, the optimal model is a flat evaluation plan — precomputed slot references, operation opcodes, and result slots — not a recursive tree interpreter. Think of it as a register-based bytecode where "registers" are field slots. This makes evaluation predictable-time, cache-friendly, and trivially serializable for inspection. The doc should commit to "flat evaluation plan" as the executable model shape and explicitly reject tree-walking.



4. **Emit a machine-readable "contract digest" alongside `CompilationResult`.** A deterministic hash of the compiled definition's semantic content (fields, types, constraints, states, transitions — excluding whitespace and comments) would let host applications detect definition changes without diffing source text. Pair it with a structural diff API (`ContractDiff(old, new)` → added/removed/changed fields, states, constraints) and you have the foundation for the migration story (gap #4 above) and a production deployment safety net.



5. **The constraint evaluation matrix should surface "why not" explanations as structured data.** When `Fire` returns `Rejected` or `EventConstraintsFailed`, the outcome carries `ConstraintViolation` objects. But the doc doesn't specify whether violations carry *explanation depth* — just the failing expression text, or also the evaluated field values, the guard that scoped the constraint, and the specific sub-expression that failed. For AI legibility, violations should carry structured explanation: `{ constraint, expression, evaluatedValues: { field: value }, guardContext?, failingSubExpression? }`. This is cheap to compute during evaluation and transforms MCP from "it failed" to "it failed because X was 3 and the constraint requires X > 5."



---



## Right-Sizing Issues



1. **The `SyntaxTree` vs `TypedModel` anti-mirroring rules are over-specified for a doc at this level.** Four numbered rules about what `TypedModel` must not do, plus a seven-item "required inventory" — this is component-level design specification embedded in an architecture document. The architectural decision (they are separate artifacts with separate jobs) is correct and should stay. The implementation contract should move to a parser/type-checker-specific design doc that the implementer reads when building those stages.



2. **The five constraint-plan families and four activation indexes are correctly designed but could be simplified in the implementation.** The `from` and `to` families only activate during `Fire`. The `on` family only activates during `Fire`. The `in` family activates during `Update`, `Create`-without-event, and `Restore`. The `always` family activates everywhere. This means the evaluator really has two modes: "fire mode" (all five families) and "edit mode" (always + in). The doc could name these modes to simplify the mental model without losing the family distinction.



---



## Top 3 Recommended Changes Before This Doc Drives Per-Component Design

### 1. Add a proof coverage analysis against the sample corpus.



Run the four proof strategies against every `ProofRequirement` that would arise from the 20 sample files. Report how many obligations each strategy discharges and how many remain unresolvable. If coverage is below ~90%, design a fifth strategy before implementation begins. This is the highest-risk unknown in the document — the proof engine's value proposition depends on it.

### 2. Specify the parser error recovery strategy.



Lock one of: (a) panic-mode with synchronization at declaration keywords, (b) token-deletion/insertion with cost model, (c) "re-lex everything, re-parse everything" with the 64KB ceiling as the performance argument. The LS team cannot build completion/diagnostic features without knowing what the tree looks like on broken input.

### 3. Commit to a flat evaluation plan as the executable model.



Replace "lowered expression nodes and action plans" with a concrete specification: slot-addressed evaluation plans with operation opcodes, field-slot references, literal constants, and result slots. This prevents the implementation from defaulting to a recursive AST interpreter — which would be correct but would sacrifice the performance and inspectability properties that make Precept's runtime distinctive.



---



*This review is direct because the timing demands it. Addressing these three items now — before the parser, type checker, and evaluator are built — is nearly free. Addressing them after implementation begins is expensive. The architecture is sound. These are the gaps that would bite us.*



---



---



---

# Decision: Combined Design v2 Comprehensive Revision Pass



**By:** Frank



**Date:** 2026-07-17



**Status:** Applied



## Summary



Applied all team review feedback (Frank design review, George technical accuracy, Elaine readability) to `docs/working/combined-design-v2.md` in a single revision pass. Added Precept Innovations callouts to every major section. Added two new sections: §12 TextMate Grammar Generation and §13 MCP Integration.



## What Changed

### Review feedback applied (all three reviewers)



- Navigation guide ("How to read this document") after status block



- Parser: error recovery shape, node inventory, catalog-to-grammar mapping, anti-Roslyn guidance, ActionKind dual-use, parser/TypeChecker contract boundary



- TypeChecker: anti-pattern for per-construct check methods



- Proof engine: coverage boundary, flow narrowing clarification, initial-state satisfiability



- Compilation snapshot: no-incremental-compilation model, contract digest hash, definition versioning gap



- Lowering: fixed "catalogs not re-read" claim, descriptor shapes, flat evaluation plan, anti-pattern warnings, ConstraintActivation cataloging, Version serialization contract



- Runtime: Restore recomputation order, structured "why not" violations

### New content



- **Precept Innovations callouts** in every major section (§2–§14), 2–4 bullets each



- **§12 TextMate grammar generation** — catalog contributions table, anti-pattern, zero-drift guarantee



- **§13 MCP integration** — tool inventory, thin-wrapper principle, AI-first design, catalog-derived vocabulary

### Structural changes



- Former §12 (LS integration) renumbered to §14



- Doc grew from 486 to 694 lines



- Formulaic guarantee paragraphs folded into stage openings for §8–§10



## Decisions Locked



- Parser error recovery: construct-level panic mode with `MissingNode` + `SkippedTokens`



- Expression evaluation: flat slot-addressed evaluation plans, tree-walk explicitly rejected



- Incremental compilation: "re-run everything" is the intended model (64KB ceiling)



- Definition versioning: known gap, deferred beyond v1



- `ConstraintActivation`: should be cataloged (language-surface knowledge)



---



## Proposal Summary



Invert D3: make `write` the universal default for (field, state) pairs. Add a `readonly` modifier on field declarations to permanently lock fields from ever being written in any state. Eliminate root-level `write` declarations entirely.



---



## Question 1: Does inverting D3 weaken the conservative guarantee?



**Yes. Fundamentally.**



D3 as specified (§2.2 Access Mode, composition rule 1) states: "D3 is the universal per-pair baseline — undeclared (field, state) pairs default to `read`." The design principle behind this is explicit: "Authors declare only exceptions to readonly — `write` opens a field for editing in that state."



This is a **closed-world access model**. Nothing is writable unless explicitly opened. The omission failure mode is safe: if an author forgets to declare a `write`, the field is locked in that state. The author must take a deliberate action — writing the `write` keyword — to open the attack surface.



The proposal inverts this to an **open-world access model**. Everything is writable unless explicitly restricted. The omission failure mode is unsafe: if an author forgets to mark a field `readonly`, it is exposed in every state to direct mutation via `Update`.



This is the firewall-rule principle. Good security defaults to DENY; you add ALLOW exceptions. D3 defaults to DENY (read-only) and authors add ALLOW exceptions (`write`). The proposal defaults to ALLOW (writable) and authors add DENY exceptions (`readonly`). In a **governance** language — one whose entire identity is built on "invalid configurations are structurally impossible" (Principle 1: Prevention, not detection) — the conservative default is non-negotiable.

### Corpus evidence



The sample set confirms that the conservative default reflects real domain proportions:



- **Stateful precepts with zero write declarations:** `hiring-pipeline`, `loan-application` (except one guarded write), `apartment-rental-application`, `restaurant-waitlist`, `library-hold-request`, `travel-reimbursement`, `warranty-repair-request`. These precepts rely entirely on event-driven mutation. The D3 default silently protects all fields from direct editing. Under the proposal, all those fields would be writable by default — an enormous, invisible expansion of the attack surface.



- **Stateful precepts with 1–2 write declarations:** `crosswalk-signal` (1), `clinic-appointment-scheduling` (1), `building-access-badge-request` (1), `insurance-claim` (2), `maintenance-work-order` (1), `refund-request` (1), `subscription-cancellation-retention` (1), `event-registration` (2), `it-helpdesk-ticket` (1), `utility-outage-report` (1), `vehicle-service-appointment` (1). The typical pattern is opening 1–3 fields in 1–2 states. The remaining (field, state) pairs — the overwhelming majority — stay protected by D3.



- **Stateless precepts:** `fee-schedule` (3 of 5 writable), `payment-method` (2 of 6 writable), `computed-tax-net` (2 of 4 writable), `invoice-line-item` (4 of N writable). Even in stateless precepts, the typical pattern is that some fields are intentionally locked. `customer-profile` is the only sample using `write all`.



The verbosity cost of the current model is 1–2 lines per precept. The safety cost of the proposed model is an invisible, unbounded expansion of the mutation surface whenever an author omits a `readonly` marker.

### Principle citations



- **Principle 1 (Prevention, not detection):** The proposal turns field-level access control from structurally prevented to structurally permitted. An author who omits `readonly` on a field that should be locked has created a governance gap. Under D3, the same omission creates no gap.



- **Principle 4 (Full inspectability):** Auditability is stronger when the declared surface is the exception set (small, explicit) rather than the restriction set (requiring mental subtraction from a universal default). "What can a user directly edit here?" is answered by scanning for `write` keywords under D3. Under the proposal, the answer is "everything, minus what's marked `readonly`" — which requires reading every field declaration to check for the absence of a modifier.



---



## Question 2: Does `readonly` on a field cleanly complement or conflict with computed fields?



**It creates a semantic inconsistency.**



Computed fields (`field Tax as number -> Subtotal * TaxRate`) are already implicitly readonly. The spec enforces this structurally — `ComputedFieldNotWritable` is a type-checker diagnostic (§3.8). A computed field's readonly nature arises from its derivation: it has an expression, so it cannot be directly assigned. This is not a modifier; it is a structural consequence of the field's kind.



Under the proposal, the access defaults would be:



| Field kind | Proposed default | Actual access |



|---|---|---|



| Stored field (no `readonly`) | write | write |



| Stored field (with `readonly`) | write → overridden to read | read |



| Computed field | write (in theory) | read (structurally) |



The computed field's access mode would be inconsistent with the declared default. A stored field and a computed field would have different effective defaults despite the language claiming "write is the default." The author would need to understand that computed fields are a hidden exception to the stated default — undermining Principle 4 (inspectability) and Principle 5 (keyword-anchored readability).



Under D3, the picture is consistent:



| Field kind | D3 default | Actual access |



|---|---|---|



| Stored field (no `write`) | read | read |



| Stored field (with `write`) | read → overridden to write | write |



| Computed field | read | read |



All fields default to read. Computed fields are naturally aligned with the default. Stored fields that need to be writable are explicitly opened. There is no inconsistency to explain.



Adding `readonly` as a modifier also creates a redundancy question: should `readonly` on a computed field be a warning (redundant modifier), an error (modifier conflicts with structural readonly), or silently accepted? Each answer has downsides. Under D3, the question never arises — there is no `readonly` keyword, and computed fields simply match the default.



---



## Question 3: Does "write default, restrict per state" change the auditability story?



**Yes. It weakens it materially.**



In a stateful precept under D3, the audit question "which fields can a user directly edit in state S?" is answered by reading the `in S write` declarations. If there are none, the answer is "nothing — all mutation happens through events." This is a **closed-world audit**: the write declarations ARE the complete answer.



Under the proposal, the same question is answered by: "every field, minus those marked `readonly` on the field declaration, minus those restricted by `in S read` or `in S omit` declarations." This is an **open-world audit** requiring cross-referencing the field declarations (for `readonly` markers), the state-scoped access declarations (for per-state restrictions), and computing the difference. The mental model is subtraction from a universal set rather than enumeration of an explicit set.



For a governance language — one where the point is to make the access contract **explicit and visible** — the open-world model is the wrong posture. The current model's strength is that the write declarations positively assert what is open. The proposed model requires the reader to infer what is open from what is not restricted.



This matters especially for AI consumers. Precept's Principle 3 (deterministic semantics) and Principle 5 (keyword-anchored readability) are designed partly for AI legibility. A closed-world access model is easier for AI agents to reason about: "find all `write` declarations" is a simple, complete query. "Find all fields, subtract `readonly` fields, subtract per-state restrictions" is a compositional query with a higher error surface.



---



## Additional Concerns

### The `readonly` keyword itself is misaligned



`readonly` is a **programming-language concept** from C#, Java, Rust, TypeScript. It carries connotations of compile-time immutability, final binding, memory-model guarantees. Precept's access model is about **editability** — which fields can the host application directly mutate via the `Update` operation. These are different concepts. A field that is `read` in a given state is not immutable — events can still `set` it during transitions. It is merely not directly editable by the external caller. Introducing `readonly` would import programming-language semantics into a domain-configuration language, violating the philosophy's positioning of Precept as a language for domain experts and business analysts, not software developers (§ Who authors a precept in philosophy.md).

### Root-level `write` elimination is a false economy



The proposal motivates itself partly by eliminating root-level `write` declarations. But the current model already makes these declarations do useful work:



- `write BaseFee, DiscountPercent, MinimumCharge` in `fee-schedule` — the `write` keyword positively documents the author's intent. Reading it, you know immediately which fields are editable. The comment above it ("Only pricing levers are editable; TaxRate and CurrencyCode are locked") is restating what the `write` declaration already says.



- `write all` in `customer-profile` — a deliberate, visible assertion that everything is open. Under the proposal, this becomes the invisible default, and the author's deliberate intent vanishes from the surface.



The `write` keyword carries semantic weight as a positive assertion. Replacing it with the absence of `readonly` loses that signal.



---



## Verdict: **Reject**



The proposal inverts Precept's conservative access posture from closed-world (safe by default, explicitly opened) to open-world (exposed by default, explicitly restricted). This:



1. **Weakens the omission failure mode** from safe (field locked) to unsafe (field exposed).



2. **Creates an access-default inconsistency** between stored and computed fields.



3. **Degrades auditability** from positive enumeration to negative subtraction.



4. **Imports programming-language semantics** (`readonly`) into a domain-configuration language.



5. **Eliminates the positive-assertion value** of `write` declarations for marginal verbosity savings (1–2 lines per precept).



D3 is philosophically correct, empirically well-calibrated to real domain proportions, and consistent with the governance identity. It should not be inverted.

### What would need to change for reconsideration



If the underlying concern is verbosity in stateless precepts that happen to have mostly-writable fields, there are narrower solutions that preserve D3:



- A `write all` shorthand already exists and handles the fully-open case.



- If a `write all except F1, F2` syntax were needed, it could be evaluated without inverting the default. The exception list would still be a positive declaration against a positively-declared baseline.



Neither of these requires abandoning the conservative default. The proposal conflates "reduce boilerplate" with "invert the safety model." Only the former is a real problem; the latter is the wrong solution.



---



---



---

# Full Architecture Review — spike/Precept-V2



**Reviewer:** Frank (Lead Architect)



**Branch:** `spike/Precept-V2`



**Commits reviewed:** 36ccec4..4831cb3 (full branch vs main)



**Build:** ✅ Clean (1 pre-existing RS1030 warning in PRECEPT0013)



**Tests:** ✅ 2678 passing (2424 Precept.Tests + 254 Precept.Analyzers.Tests), 0 failures



---



## 1. Annotation Bridge Architecture (PRECEPT0019)

### Files Reviewed



- `src/Precept/HandlesCatalogExhaustivelyAttribute.cs`



- `src/Precept/Language/HandlesCatalogMemberAttribute.cs`



- `src/Precept.Analyzers/Precept0019PipelineCoverageExhaustiveness.cs`



- `src/Precept/Pipeline/Parser.cs` (class marker on `ParseSession`)



- `src/Precept/Pipeline/TypeChecker.cs` (class marker + 11 member annotations)



- `src/Precept/Pipeline/GraphAnalyzer.cs` (class marker + 11 member annotations)

### Assessment



The annotation bridge is clean and catalog-agnostic as specified. The class marker accepts `Type catalogEnum` — any enum can opt in. Method markers use `object kind` for call-site type safety without analyzer rewrites.



PRECEPT0019 correctly:



- Extracts `typeof(T)` from the class marker



- Collects all enum fields with constant values



- Resolves method marker arguments by matching `arg.Type` against the catalog enum



- Reports missing members with clear diagnostic formatting



- Is registered as `DiagnosticSeverity.Error` (was previously Warning, promoted per Slice 26)



Parser coverage: `ParseSession` (ref partial struct) has both `ParseExpression` and `ParseAtom` annotated, covering all 11 `ExpressionFormKind` members across the two methods. TypeChecker and GraphAnalyzer have placeholder methods with all 11 annotations each — correct forward-declarations for Phase 3.



---



## 2. Catalog Integrity Analyzers (PRECEPT0020–0023)

### PRECEPT0020 — Operators Token Collision



Two sub-rules (0020a: `(Token.Kind, Arity)` key collision; 0020b: binary `Token.Kind` collision). Both correctly:



- Scope to `OperatorKind` switches via `TryGetCatalogSwitchKind`



- Skip `MultiTokenOp` arms (correct — those are PRECEPT0023's domain)



- Extract token kind via `Tokens.GetMeta(TokenKind.X)` invocation walking



- Report against the creation syntax location (not the arm)

### PRECEPT0021 — Tokens Duplicate Text



- Correctly skips null `Text` (synthetic tokens like `SetType`, `Identifier`)



- Uses `ResolveStringConstant` which handles nameof, const fields, and string literals



- Only fires for `TokenKind` switches

### PRECEPT0022 — Operators Inline Token Reference



- Detects `new TokenMeta(...)` construction where `Tokens.GetMeta(TokenKind.X)` is required



- Clean single-purpose analyzer — no false-positive risk from DU subtype checks

### PRECEPT0023 — OperatorMeta DU Shape Invariants



Three sub-rules:



- **0023a:** MultiTokenOp < 2 tokens → Error. Correct.



- **0023b:** SingleTokenOp vs MultiTokenOp lead-token collision. Cross-checks single/multi dictionaries post-loop. Correct.



- **0023c:** Duplicate full token sequences. Uses `BuildFullSequenceKey` joining all tokens. Correctly checks the full sequence (e.g., "Is,Set" vs "Is,Not,Set"), not just the lead token. The diagnostic name says "MultiLeadCollision" but the invariant checks the **full sequence** — naming is slightly misleading but functionally correct.

### CatalogAnalysisHelpers



Shared infrastructure is well-factored:



- `TryGetCatalogSwitchKind` correctly guards scope (method named "GetMeta", in `Precept.Language`, known enum type)



- `EnumerateCollectionElements` handles both collection expressions and array initializers



- `UnwrapConversions` handles implicit conversion chains



- `FlagsEnumContains` supports single-ref, bitwise-OR-tree, and constant-folded forms



---



## 3. Parser Fixes

### GAP-A: `when` guard on StateEnsure/EventEnsure



`ParseStateEnsure` and `ParseEventEnsure` both implement post-condition `when` guards correctly:



- Check if `stashedGuard` exists (pre-ensure guard from outer dispatch)



- Only consume `when` if no stashed guard — prevents double-guard ambiguity



- Guard comes **after** the condition expression, before `because` — matches spec §2.2

### GAP-B: Modifiers after computed field expressions



Verified via `ExpressionBoundaryTokens` and the Pratt loop's natural termination on boundary tokens. The parser correctly stops expression parsing when it encounters modifier keywords because they're in `ExpressionBoundaryTokens` via `Constructs.LeadingTokens`. No explicit handling needed — clean by construction.

### GAP-C: Keyword-as-member-name and keyword-as-function-call



Two complementary fixes:



1. `ExpectIdentifierOrKeywordAsMemberName()` — accepts tokens in `KeywordsValidAsMemberName` after `.`



2. `ParseAtom` — `case TokenKind.Min: case TokenKind.Max:` falls through to identifier/function-call handling



Both correct. The keyword-as-function-call case handles `min(a, b)` / `max(a, b)` in expression position.

### is/is-not-set, method call, list literal, TypedConstant



- `is set` / `is not set`: Correctly uses separate `IsSetExpression`/`IsNotSetExpression` nodes. Precedence 60 matches `Operators.GetMeta(OperatorKind.IsSet).Precedence`. Non-associative by break-on-entry (`minPrecedence > 60`).



- Method call: Detects `LeftParen` following `MemberAccessExpression` at binding power 90. Correct.



- List literal: Dispatches from `ParseAtom` via `TokenKind.LeftBracket`. Correct.



- TypedConstant/InterpolatedTypedConstant: Both handled in `ParseAtom` correctly.



---



## 4. ExpressionFormKind Catalog

### Members (11 total — correct)



1. Literal, 2. Identifier, 3. Grouped, 4. BinaryOperation, 5. UnaryOperation,



6. MemberAccess, 7. Conditional, 8. FunctionCall, 9. MethodCall, 10. ListLiteral,



11. PostfixOperation

### Metadata Shape



`ExpressionFormMeta` record carries: Kind, Category, IsLeftDenotation, LeadTokens, HoverDocs. All fields populated. LeadTokens empty for led forms, non-empty for nud forms — structurally enforced by the Layer 2 test.

### Coverage Tests



Two test classes provide layered enforcement:



- `Tests.Language.ExpressionFormCoverageTests` — Layer 2: count, GetMeta completeness, HoverDocs, IsLeftDenotation, LeadTokens contract



- `Tests.ExpressionFormCoverageTests` — Layer 3: catalog completeness, annotation bridge xUnit mirror, parse round-trips



---



## 5. OperatorMeta DU Shape



Clean discriminated union:



- `OperatorMeta` (abstract base) → `SingleTokenOp` / `MultiTokenOp`



- `MultiTokenOp` carries `IReadOnlyList<TokenMeta> Tokens` with `LeadToken => Tokens[0]`



- `ByToken` FrozenDictionary indexed by `(TokenKind, Arity)` — excludes MultiTokenOp



- `ByTokenSequence` FrozenDictionary indexed by `(TokenKind, TokenKind?, TokenKind?)` — covers MultiTokenOp



- `BuildSequenceKey` correctly handles 2-token and 3-token sequences



Precedence values consistent: IsSet/IsNotSet at 60, matching arithmetic multiplication level. This is correct per spec §2.1 — presence checks bind tighter than comparisons but at the same level as multiplicative arithmetic.



---



## 6. TokenMeta.IsValidAsMemberName



- Property added to `TokenMeta` record with `bool IsValidAsMemberName = false` default



- Set to `true` on `TokenKind.Min` and `TokenKind.Max` only



- `Parser.KeywordsValidAsMemberName` derived from `Tokens.All.Where(t => t.IsValidAsMemberName).Select(t => t.Kind).ToFrozenSet()`



- No hardcoded `{ Min, Max }` array remains — pure catalog derivation



- Tests: `TokenMetaMemberNameTests` covers true/false/theory cases



- `SetType` handled correctly: `Text: null`, `TextMateScope: null`, `SemanticTokenType: null` — parser-synthesized token with no tooling metadata. Excluded from `Keywords` FrozenDictionary via explicit `m.Kind != TokenKind.SetType` filter. This prevents the `Text: null` duplicate-text false positive that would otherwise fire.



---



## 7. Parser Split



Three partial files with clean responsibility separation:



- `Parser.cs` — vocabulary FrozenDictionaries, boundary sets, `Parse()` entry point, `ParseSession` struct definition, token navigation



- `Parser.Declarations.cs` — construct parsers (state ensure, event ensure, access mode, omit, transition row, outcomes, action statements)



- `Parser.Expressions.cs` — Pratt expression parser (ParseExpression led loop, ParseAtom nud switch, interpolation parsers, list literal)



No duplication detected. The `HandlesCatalogExhaustively` attribute lives on `ParseSession` in `Parser.cs`; the `HandlesCatalogMember` annotations are distributed across `Parser.Expressions.cs` methods. This is correct — the ref partial struct spans files.



---



## 8. Documentation Accuracy



`docs/language/catalog-system.md` § Exhaustiveness Enforcement Strategies:



- Correctly describes both strategies (CS8509 vs annotation bridge)



- Decision rule table is clear and actionable



- Phase 3 note correctly defers TypeChecker/ProofEngine dispatch decision



- Consumer table for current CS8509 sites is accurate (`ConstructKind`, `ActionKind`, etc.)



---



## Findings

### Blockers



None.

### Guidance



- **G1:** [`src/Precept.Analyzers/Precept0023OperatorsDUShapeInvariants.cs:30`] The constant `DiagnosticId_MultiLeadCollision = "PRECEPT0023c"` and field name `MultiLeadCollisionRule` use "lead" in their identifiers, but the invariant actually checks the **full token sequence** (not just the lead). Consider renaming to `DiagnosticId_MultiSequenceCollision` / `MultiSequenceCollisionRule` for clarity. The diagnostic message is correct — only the code-level naming is misleading.



- **G2:** [`src/Precept.Analyzers/CatalogAnalysisHelpers.cs:57-62`] `CatalogEnumNames` is missing `ConstraintKind` and `ProofRequirementKind`. Both have `GetMeta` switches in `Precept.Language`. Currently their switches use discard arms (`_ =>`), so PRECEPT0007 would flag them anyway if they were included. When those catalogs drop the discard arm (expected in Phase 3), they should be added to `CatalogEnumNames` to enable PRECEPT0007 coverage. Track this as a Phase 3 prerequisite.



- **G3:** [`src/Precept.Analyzers/Precept0013ActionsCrossRef.cs:136`] Pre-existing RS1030 warning (`Compilation.GetSemanticModel()` inside analyzer). Not introduced on this branch, but should be addressed eventually — Roslyn best practice violation.

### Observations



- **O1:** TypeChecker and GraphAnalyzer currently throw `NotImplementedException` — the `[HandlesCatalogMember]` annotations are forward declarations. This is correct by design (Phase 3 work); PRECEPT0019 validates the annotation set at compile time regardless of implementation status.



- **O2:** The `contains` chaining test (Slice 18) correctly validates `NonAssociativeComparison` diagnostic for `a contains b contains c` via the Pratt loop's non-associativity detection in lines 113-126 of `Parser.Expressions.cs`. Binding power 40 is correct per catalog.



- **O3:** The test count increased from ~2000 (pre-spike) to 2678 — a ~34% test growth proportional to the implementation surface. Healthy ratio.



- **O4:** `ExpressionFormKind` is enumerated 1–11 (no zero slot). This is consistent with the other catalog enums that use `PRECEPT0018SemanticEnumZeroSlot` to enforce meaningful zero absence.



---



## VERDICT: APPROVED — 0 blockers, 3 guidance items



The annotation bridge architecture is sound, catalog-agnostic, and correctly enforced at `DiagnosticSeverity.Error`. The four new analyzers (PRECEPT0020–0023) cover real invariants that would otherwise manifest as startup crashes. Parser fixes are correct and well-tested. The ExpressionFormKind catalog and OperatorMeta DU are structurally complete. Documentation is accurate. The 3 guidance items are naming clarity and forward-looking hygiene — none block merge.



This branch is ready to merge to main.



---



---

# **CRITICAL GAPS**

The parser suite is green, but it is **not** comprehensive enough to support type-checker development safely. The biggest holes are the full type-reference surface, full action syntax surface, wildcard/shorthand routing (`from any`, `modify all`, `omit all`), event-arg richness, interpolation, and specific parser diagnostic-code assertions. Right now, too many tests stop at “a slot exists” or “the parser did not crash.” That is not enough. No soup for unanchored parser behavior.

# TypeChecker B1/B2/B3 Blockers — Fixed

**By:** George (Runtime Dev)
**Date:** 2026-05-08T07:00:00-04:00
**Status:** Complete — all three R3 blockers resolved, tests green
**Context:** Frank's R3 final gate review (`.squad/decisions/inbox/frank-r3-final-review.md`) identified three blockers preventing GraphAnalyzer from proceeding.

---

## Changes

### B3: MissingExpression D26 gap (5 LOC)

`ResolveMissing()` now emits a lightweight `DiagnosticCode.TypeMismatch` diagnostic with args `("expression", "missing")` before returning `TypedErrorExpression`. This closes the D26 self-containment invariant — every error path through Resolve() now records a TC-level diagnostic.

No new DiagnosticCode was added (per Frank's approval gate). TypeMismatch is the closest existing Error-severity TC code.

### B1: Field expression resolution (~100 LOC)

`ResolveFieldExpressions()` resolves default and computed expressions on `TypedField` entries:
- Default expressions from `ParsedModifier` with `Kind == ModifierKind.Default`
- Computed expressions from `ComputeExpressionSlot` on the field's `Syntax`
- `ComputedFieldDep` extraction via recursive `CollectFieldRefs()` tree walker
- `FieldScopeMode.PriorFieldsOnly` enforces forward-reference prohibition
- Qualifier binding left as null (no parser-level qualifier slot on field constructs yet)
- Event arg defaults left as null (DeclaredArg carries only ModifierKind, not values)

### B2: Construct normalization (~200 LOC)

Four new normalization methods following the established `manifest.ByKind` + Resolve + accumulate pattern:
- `PopulateEnsures()` — StateEnsure (in/to/from → ConstraintKind) and EventEnsure (on → EventPrecondition)
- `PopulateAccessModes()` — state/field reference resolution, Editable→Write / Readonly→Read mapping, optional guard
- `PopulateStateHooks()` — state reference, leading token → AnchorScope, action chain via ResolveAction()
- `PopulateEditDeclarations()` — D24 placeholder using ConstructKind.OmitDeclaration, field targets recorded

### Supporting changes

- `ParsedConstruct.LeadingTokenKind` — added `TokenKind?` to the positional record (2 parser sites updated) for anchor scope determination
- Doc updates W3 (§1 status), W4 (§4 LOC estimate → ~2700), W5 (§13 preamble → COMPLETED)
- 17 tests updated to match new diagnostic emission and populated accumulators

---

## Validation

- Build: 0 errors, 0 warnings
- Tests: 3342 Precept.Tests + 263 Precept.Analyzers.Tests — all passing
- D26 assert: no fires on any test or sample file

## Open Items

- **Qualifier binding** on TypedField — needs parser-level qualifier slot on field constructs (future work)
- **Event arg default expressions** — DeclaredArg only carries ModifierKind array, not values (future work)
- **DiagnosticCode.TypeMismatch reuse** for MissingExpression — Frank may want a dedicated code in the future

# Precept TextMate Grammar — Authoritative Specification

**Date:** 2026-05-08
**Author:** Frank
**Status:** DRAFT — pending review

**Source material reviewed:**
- `design/system/semantic-visual-system-manifest.md` — primary visual system design
- `design/system/semantic-visual-system-notes.md` — supplementary notes
- `design/system/README.md` — design system ownership
- `design/brand/brand-decisions.md` — brand palette and typography locked direction
- `design/brand/philosophy.md` — redirects to `docs/philosophy.md`
- `src/Precept/Language/Tokens.cs` (515 lines) — complete token catalog with TextMateScope assignments
- `src/Precept/Language/TokenKind.cs` (205 lines) — 139 token kinds
- `src/Precept/Language/Types.cs` — type catalog (37.4 KB)
- `src/Precept/Language/TypeKind.cs` — 32 type kinds
- `src/Precept/Language/Modifiers.cs` (260 lines) — 29 modifier kinds across 5 DU subtypes
- `src/Precept/Language/ModifierKind.cs` — modifier enum
- `src/Precept/Language/Actions.cs` (222 lines) — 15 action kinds
- `src/Precept/Language/ActionKind.cs` — action enum
- `src/Precept/Language/Operators.cs` (206 lines) — 21 operator kinds
- `src/Precept/Language/OperatorKind.cs` — operator enum
- `src/Precept/Language/Constructs.cs` (199 lines) — 12 construct kinds
- `src/Precept/Language/Functions.cs` — 21 built-in function kinds
- `src/Precept/Language/FunctionKind.cs` — function enum
- `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` (457 lines) — hand-authored grammar
- `tools/Precept.GrammarGen/Program.cs` (537 lines) — grammar generator scaffold
- `docs/tooling/extension.md` — extension architecture
- All 28 `.precept` sample files in `samples/`

---

## Executive Summary

The hand-authored `precept.tmLanguage.json` is **severely incomplete and stale**. It covers roughly 40% of the current language surface, uses at least 3 retired keywords (`nullable`, `invariant`, `assert`), a retired syntax form (`event Name with Arg` instead of parenthesized args), and classifies tokens into only 4 flat keyword groups that collapse the 14 semantic categories the catalog defines. The grammar generator (`GrammarGen/Program.cs`) correctly derives keyword alternation patterns from catalog metadata but carries the same 2 stale structural patterns (`with`-syntax events, `assert` keyword) and omits 8 construct-level patterns and the gold-colored message-string pattern that the visual system design requires. This spec defines the complete grammar that the generator must produce to replace the hand-authored file at parity-or-better.

---

## 1. Design System → TextMate Scope Mapping

The brand decisions (`brand-decisions.md`) lock 8 authoring-time color families plus comments. TextMate scopes must enable theme rules to target each family independently. The catalog (`Tokens.cs`) already assigns a `TextMateScope` to every token. This table maps visual system roles to catalog scopes and notes misalignments.

| # | Design Role | Brand Color | Typography | Catalog TextMateScope(s) | Notes |
|---|-------------|------------|------------|--------------------------|-------|
| 1 | Structure · Semantic | `#4338CA` | **bold** | `keyword.declaration.precept` | Declaration/behavioral keywords: `precept`, `field`, `state`, `event`, `rule`, `ensure`, `as`, `default`, `optional`, `writable`, `because`, `initial`, `ascending`, `descending` |
| 2 | Structure · Grammar | `#6366F1` | normal | `keyword.control.precept` | Prepositions and control flow: `in`, `to`, `from`, `on`, `of`, `into`, `when`, `if`, `then`, `else`, `by`, `at`, `for` |
| 3 | Structure · Grammar (actions) | `#6366F1` | normal | `keyword.other.action.precept` | Action verbs: `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `append`, `insert`, `put` |
| 4 | Structure · Grammar (outcomes) | `#6366F1` | normal | `keyword.other.outcome.precept` | Outcome keywords: `transition`, `no`, `reject` |
| 5 | Structure · Grammar (access) | `#6366F1` | normal | `keyword.other.access-mode.precept` | Access mode: `modify`, `readonly`, `editable`, `omit` |
| 6 | Structure · Grammar (quantifiers) | `#6366F1` | normal | `keyword.other.quantifier.precept` | Quantifiers: `all`, `any`, `each` |
| 7 | Structure · Grammar (constraints) | `#6366F1` | normal | `keyword.other.constraint.precept` | Field constraints: `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`, `ordered` |
| 8 | Structure · Grammar (operators) | `#6366F1` | normal | `keyword.operator.precept`, `keyword.operator.arrow.precept` | Symbol operators (`==`, `!=`, `~=`, `!~`, `>=`, `<=`, `>`, `<`, `=`, `+`, `-`, `*`, `/`, `%`) and arrows (`->`, `<-`) |
| 9 | Structure · Grammar (logical) | `#6366F1` | normal | `keyword.operator.logical.precept` | Keyword operators: `and`, `or`, `not` |
| 10 | Structure · Grammar (membership) | `#6366F1` | normal | `keyword.operator.membership.precept` | Membership: `contains`, `is` |
| 11 | States | `#A898F5` | normal (italic if constrained — semantic tokens only) | `entity.name.type.state.precept` | State names in declarations, `from`/`in`/`to` targets, `transition` targets |
| 12 | Events | `#30B8E8` | normal (italic if constrained — semantic tokens only) | `entity.name.function.event.precept` | Event names in declarations, `on` targets, dot-access prefix |
| 13 | Data · Names | `#B0BEC5` | normal (italic if guarded — semantic tokens only) | `variable.other.field.precept`, `variable.parameter.precept`, `variable.other.property.precept` | Field names, event argument names, property accessors after dot |
| 14 | Data · Types | `#9AA8B5` | normal | `storage.type.precept` | All type keywords. Also `storage.modifier.state.precept` for state modifiers (separate from types but same visual family in brand) |
| 15 | Data · Values | `#84929F` | normal | `constant.numeric.precept`, `constant.language.boolean.precept`, `string.quoted.double.precept`, `string.quoted.single.precept` | Literals: numbers, booleans, strings, typed constants |
| 16 | Rules · Messages | `#FBBF24` | normal | `string.quoted.double.message.precept` | **ONLY** in `because "msg"` and `reject "msg"` positions. Must be distinguished from regular `string.quoted.double.precept` |
| 17 | Comments | `#9096A6` | *italic* | `comment.line.number-sign.precept` | `#` line comments |
| 18 | State modifiers | (brand: same as types) | normal | `storage.modifier.state.precept` | `terminal`, `required`, `irreversible`, `success`, `warning`, `error` |
| 19 | Precept name | (brand: identity) | normal | `entity.name.precept.message.precept` | The precept name after `precept` keyword |
| 20 | Punctuation | `#6366F1` | normal | `punctuation.precept`, `punctuation.separator.comma.precept`, `punctuation.accessor.precept` | `.`, `,`, `(`, `)`, `[`, `]` |
| 21 | Member names | (brand: data names) | normal | `keyword.other.precept` | Special member accessors: `countof`, `peekby` |

### Brand-to-Catalog Misalignment Notes

The brand decisions doc (`brand-decisions.md`) lists specific keywords under "Structure · Semantic" that the catalog assigns to different scope categories:

| Keyword | Brand says | Catalog scope | Resolution |
|---------|-----------|--------------|------------|
| `from`, `on`, `in`, `to` | Structure · Semantic (bold) | `keyword.control.precept` | **Catalog wins.** These are prepositions/control flow. Theme can still bold them if desired. |
| `set` | Structure · Semantic (bold) | `storage.type.precept` (dual-use: action AND type) | **Catalog wins.** `set` is context-dependent — TextMate can't distinguish action vs type usage. Semantic tokens handle this. |
| `transition`, `reject`, `no` | Structure · Semantic (bold) | `keyword.other.outcome.precept` | **Catalog wins.** Dedicated outcome scope enables finer theme control. |
| `when` | Structure · Semantic (bold) | `keyword.control.precept` | **Catalog wins.** Control flow keyword. |
| `write` | Structure · Semantic (bold) | RETIRED (B4 2026-04-28) | **Remove from brand doc.** Replaced by `writable` field modifier. |
| `nullable` | Structure · Grammar | RETIRED | **Remove from brand doc.** Replaced by `optional`. |

**Action:** Brand doc keyword lists need a sync pass to match catalog reality. This is a brand-doc defect, not a grammar defect.

---

## 2. Language Surface Inventory

Complete enumeration of every token/construct type from the catalog, with canonical TextMateScope.

### 2.1 Keywords — Declaration (`keyword.declaration.precept`)

| Token | Text | Source |
|-------|------|--------|
| Precept | `precept` | TokenKind.Precept (=1) |
| Field | `field` | TokenKind.Field (=2) |
| State | `state` | TokenKind.State (=3) |
| Event | `event` | TokenKind.Event (=4) |
| Rule | `rule` | TokenKind.Rule (=5) |
| Ensure | `ensure` | TokenKind.Ensure (=6) |
| As | `as` | TokenKind.As (=7) |
| Default | `default` | TokenKind.Default (=8) |
| Optional | `optional` | TokenKind.Optional (=9) |
| Writable | `writable` | TokenKind.Writable (=10) |
| Because | `because` | TokenKind.Because (=11) |
| Initial | `initial` | TokenKind.Initial (=12) |
| Ascending | `ascending` | TokenKind.Ascending (=130) |
| Descending | `descending` | TokenKind.Descending (=131) |

### 2.2 Keywords — Prepositions/Control (`keyword.control.precept`)

| Token | Text | Source |
|-------|------|--------|
| In | `in` | TokenKind.In (=13) |
| To | `to` | TokenKind.To (=14) |
| From | `from` | TokenKind.From (=15) |
| On | `on` | TokenKind.On (=16) |
| Of | `of` | TokenKind.Of (=17) |
| Into | `into` | TokenKind.Into (=18) |
| When | `when` | TokenKind.When (=19) |
| If | `if` | TokenKind.If (=20) |
| Then | `then` | TokenKind.Then (=21) |
| Else | `else` | TokenKind.Else (=22) |
| By | `by` | TokenKind.By (=128) |
| At | `at` | TokenKind.At (=129) |
| For | `for` | TokenKind.For (=136) |

### 2.3 Keywords — Actions (`keyword.other.action.precept`)

| Token | Text | Source |
|-------|------|--------|
| Add | `add` | TokenKind.Add (=24) |
| Remove | `remove` | TokenKind.Remove (=25) |
| Enqueue | `enqueue` | TokenKind.Enqueue (=26) |
| Dequeue | `dequeue` | TokenKind.Dequeue (=27) |
| Push | `push` | TokenKind.Push (=28) |
| Pop | `pop` | TokenKind.Pop (=29) |
| Clear | `clear` | TokenKind.Clear (=30) |
| Append | `append` | TokenKind.Append (=132) |
| Insert | `insert` | TokenKind.Insert (=133) |
| Put | `put` | TokenKind.Put (=134) |

**Note:** `set` (TokenKind.Set =23) is dual-use (action AND collection type). Catalog assigns `storage.type.precept`. Appears in both action chains and type positions.

### 2.4 Keywords — Outcomes (`keyword.other.outcome.precept`)

| Token | Text | Source |
|-------|------|--------|
| Transition | `transition` | TokenKind.Transition (=31) |
| No | `no` | TokenKind.No (=32) |
| Reject | `reject` | TokenKind.Reject (=33) |

### 2.5 Keywords — Access Modes (`keyword.other.access-mode.precept`)

| Token | Text | Source |
|-------|------|--------|
| Modify | `modify` | TokenKind.Modify (=34) |
| Readonly | `readonly` | TokenKind.Readonly (=35) |
| Editable | `editable` | TokenKind.Editable (=36) |
| Omit | `omit` | TokenKind.Omit (=37) |

### 2.6 Keywords — Logical Operators (`keyword.operator.logical.precept`)

| Token | Text | Source |
|-------|------|--------|
| And | `and` | TokenKind.And (=38) |
| Or | `or` | TokenKind.Or (=39) |
| Not | `not` | TokenKind.Not (=40) |

### 2.7 Keywords — Membership (`keyword.operator.membership.precept`)

| Token | Text | Source |
|-------|------|--------|
| Contains | `contains` | TokenKind.Contains (=41) |
| Is | `is` | TokenKind.Is (=42) |

### 2.8 Keywords — Quantifiers (`keyword.other.quantifier.precept`)

| Token | Text | Source |
|-------|------|--------|
| All | `all` | TokenKind.All (=43) |
| Any | `any` | TokenKind.Any (=44) |
| Each | `each` | TokenKind.Each (=135) |

### 2.9 Keywords — State Modifiers (`storage.modifier.state.precept`)

| Token | Text | Source |
|-------|------|--------|
| Terminal | `terminal` | TokenKind.Terminal (=45) |
| Required | `required` | TokenKind.Required (=46) |
| Irreversible | `irreversible` | TokenKind.Irreversible (=47) |
| Success | `success` | TokenKind.Success (=48) |
| Warning | `warning` | TokenKind.Warning (=49) |
| Error | `error` | TokenKind.Error (=50) |

### 2.10 Keywords — Constraints (`keyword.other.constraint.precept`)

| Token | Text | Source |
|-------|------|--------|
| Nonnegative | `nonnegative` | TokenKind.Nonnegative (=51) |
| Positive | `positive` | TokenKind.Positive (=52) |
| Nonzero | `nonzero` | TokenKind.Nonzero (=53) |
| Notempty | `notempty` | TokenKind.Notempty (=54) |
| Min | `min` | TokenKind.Min (=55) |
| Max | `max` | TokenKind.Max (=56) |
| Minlength | `minlength` | TokenKind.Minlength (=57) |
| Maxlength | `maxlength` | TokenKind.Maxlength (=58) |
| Mincount | `mincount` | TokenKind.Mincount (=59) |
| Maxcount | `maxcount` | TokenKind.Maxcount (=60) |
| Maxplaces | `maxplaces` | TokenKind.Maxplaces (=61) |
| Ordered | `ordered` | TokenKind.Ordered (=62) |

### 2.11 Keywords — Type Names (`storage.type.precept`)

| Token | Text | Family | Source |
|-------|------|--------|--------|
| StringType | `string` | Scalar | TokenKind.StringType (=63) |
| BooleanType | `boolean` | Scalar | TokenKind.BooleanType (=64) |
| IntegerType | `integer` | Scalar | TokenKind.IntegerType (=65) |
| DecimalType | `decimal` | Scalar | TokenKind.DecimalType (=66) |
| NumberType | `number` | Scalar | TokenKind.NumberType (=67) |
| ChoiceType | `choice` | Scalar | TokenKind.ChoiceType (=68) |
| Set | `set` | Collection | TokenKind.Set (=23) — dual-use |
| QueueType | `queue` | Collection | TokenKind.QueueType (=70) |
| StackType | `stack` | Collection | TokenKind.StackType (=71) |
| BagType | `bag` | Collection | TokenKind.BagType (=124) |
| ListType | `list` | Collection | TokenKind.ListType (=125) |
| LogType | `log` | Collection | TokenKind.LogType (=126) |
| LookupType | `lookup` | Collection | TokenKind.LookupType (=127) |
| DateType | `date` | Temporal | TokenKind.DateType (=72) |
| TimeType | `time` | Temporal | TokenKind.TimeType (=73) |
| InstantType | `instant` | Temporal | TokenKind.InstantType (=74) |
| DurationType | `duration` | Temporal | TokenKind.DurationType (=75) |
| PeriodType | `period` | Temporal | TokenKind.PeriodType (=76) |
| TimezoneType | `timezone` | Temporal | TokenKind.TimezoneType (=77) |
| ZonedDateTimeType | `zoneddatetime` | Temporal | TokenKind.ZonedDateTimeType (=78) |
| DateTimeType | `datetime` | Temporal | TokenKind.DateTimeType (=79) |
| MoneyType | `money` | Business | TokenKind.MoneyType (=80) |
| CurrencyType | `currency` | Business | TokenKind.CurrencyType (=81) |
| QuantityType | `quantity` | Business | TokenKind.QuantityType (=82) |
| UnitOfMeasureType | `unitofmeasure` | Business | TokenKind.UnitOfMeasureType (=83) |
| DimensionType | `dimension` | Business | TokenKind.DimensionType (=84) |
| PriceType | `price` | Business | TokenKind.PriceType (=85) |
| ExchangeRateType | `exchangerate` | Business | TokenKind.ExchangeRateType (=86) |

### 2.12 Literals

| Token | Scope | Description |
|-------|-------|-------------|
| True (`true`) | `constant.language.boolean.precept` | Boolean literal |
| False (`false`) | `constant.language.boolean.precept` | Boolean literal |
| NumberLiteral | `constant.numeric.precept` | Integer and decimal numbers |
| StringLiteral | `string.quoted.double.precept` | Double-quoted strings |
| TypedConstant | `string.quoted.single.precept` | Single-quoted typed constants (`'USD'`, `'kg'`) |

**Note:** `null` is NOT a keyword in the token catalog. The hand-authored grammar includes it in `booleanNull` — this is stale. Precept uses `is set`/`is not set` for presence, not `null`.

### 2.13 Symbol Operators (`keyword.operator.precept`)

| Token | Text | Description |
|-------|------|-------------|
| DoubleEquals | `==` | Equality |
| NotEquals | `!=` | Inequality |
| CaseInsensitiveEquals | `~=` | Case-insensitive equals |
| CaseInsensitiveNotEquals | `!~` | Case-insensitive not-equals |
| Tilde | `~` | CI collection inner-type prefix |
| GreaterThanOrEqual | `>=` | Comparison |
| LessThanOrEqual | `<=` | Comparison |
| GreaterThan | `>` | Comparison |
| LessThan | `<` | Comparison |
| Assign | `=` | Assignment |
| Plus | `+` | Addition |
| Minus | `-` | Subtraction/negation |
| Star | `*` | Multiplication |
| Slash | `/` | Division |
| Percent | `%` | Modulo |

### 2.14 Arrow Operators (`keyword.operator.arrow.precept`)

| Token | Text | Description |
|-------|------|-------------|
| Arrow | `->` | Action chain / outcome separator |
| BackArrow | `<-` | Computed field derivation |

### 2.15 Punctuation (`punctuation.precept`)

| Token | Text | Description |
|-------|------|-------------|
| Dot | `.` | Member access |
| Comma | `,` | List separator |
| LeftParen | `(` | Open paren |
| RightParen | `)` | Close paren |
| LeftBracket | `[` | Open bracket |
| RightBracket | `]` | Close bracket |

### 2.16 Member-Name Tokens (`keyword.other.precept`)

| Token | Text | Description |
|-------|------|-------------|
| Countof | `countof` | Bag element count accessor |
| Peekby | `peekby` | Priority queue ordering-key peek |

### 2.17 Built-in Functions (21 total — not keywords, scoped as identifiers)

Functions are parsed as identifier + `(` + arguments + `)`. They are NOT lexer keywords. In TextMate, they match as generic identifiers unless a function-call pattern highlights them. The grammar SHOULD have a pattern for known function names followed by `(`.

| Function | Name |
|----------|------|
| Min | `min` |
| Max | `max` |
| Abs | `abs` |
| Clamp | `clamp` |
| Floor | `floor` |
| Ceil | `ceil` |
| Truncate | `truncate` |
| Round | `round` |
| Approximate | `approximate` |
| Pow | `pow` |
| Sqrt | `sqrt` |
| Trim | `trim` |
| StartsWith | `startsWith` |
| EndsWith | `endsWith` |
| ToLower | `toLower` |
| ToUpper | `toUpper` |
| Left | `left` |
| Right | `right` |
| Mid | `mid` |
| Now | `now` |
| ~startsWith | `~startsWith` (CI variant) |
| ~endsWith | `~endsWith` (CI variant) |

### 2.18 Constructs (12 — from `Constructs.cs`)

| # | ConstructKind | Leading Token(s) | Disambiguation | Example |
|---|---------------|-------------------|----------------|---------|
| 1 | PreceptHeader | `precept` | — | `precept LoanApplication` |
| 2 | FieldDeclaration | `field` | — | `field amount as money nonnegative` |
| 3 | StateDeclaration | `state` | — | `state Draft initial, Submitted, Approved terminal success` |
| 4 | EventDeclaration | `event` | — | `event Submit(approver as string)` |
| 5 | RuleDeclaration | `rule` | — | `rule amount > 0 because "..."` |
| 6 | TransitionRow | `from` + `on` | Disambiguated by `on` | `from Draft on Submit -> ... -> transition Submitted` |
| 7 | StateEnsure | `in`/`to`/`from` + `ensure` | Disambiguated by `ensure` | `in Approved ensure amount > 0 because "..."` |
| 8 | AccessMode | `in` + `modify` | Disambiguated by `modify` | `in Draft modify Amount editable` |
| 9 | OmitDeclaration | `in` + `omit` | Disambiguated by `omit` | `in Draft omit InternalNotes` |
| 10 | StateAction | `to`/`from` + `->` | Disambiguated by `->` | `to Confirmed -> set PaymentReceived = true` |
| 11 | EventEnsure | `on` + `ensure` | Disambiguated by `ensure` | `on Submit ensure Amount > 0 because "..."` |
| 12 | EventHandler | `on` + `->` | Disambiguated by `->` | `on UpdateName -> set name = newName` |

---

## 3. Hand-Authored Grammar Audit

### 3.1 Coverage Gap Table

| # | Language Construct / Token | In Hand Grammar? | Scope Assignment | Gap / Issue |
|---|---------------------------|:---:|------------------|-------------|
| G1 | `rule` keyword | ❌ NO | — | Missing. `invariant` exists at L276 but `rule` replaced it. |
| G2 | `ensure` keyword | ❌ NO | — | Missing entirely. Used in StateEnsure, EventEnsure constructs. |
| G3 | `optional` keyword | ❌ NO | — | Missing. L366 has stale `nullable` instead. |
| G4 | `writable` keyword | ❌ NO | — | Missing. New field modifier (B4). |
| G5 | `modify` keyword | ❌ NO | — | Missing. Access mode construct (B4). |
| G6 | `readonly` keyword | ❌ NO | — | Missing. Access mode adjective (B4). |
| G7 | `editable` keyword | ❌ NO | — | Missing. Access mode adjective (B4). |
| G8 | `omit` keyword (construct) | ❌ NO | — | Missing. Omit declaration construct. |
| G9 | State modifiers: `terminal`, `required`, `irreversible`, `success`, `warning`, `error` | ❌ NO | — | Only `initial` handled in state declaration (L101). 6 modifiers missing. |
| G10 | Type keywords: `integer`, `decimal`, `choice` | ❌ NO | — | L373-377 only has `string\|number\|boolean\|set\|queue\|stack`. |
| G11 | Temporal types (8): `date` through `datetime` | ❌ NO | — | All 8 temporal types missing. |
| G12 | Business-domain types (7): `money` through `exchangerate` | ❌ NO | — | All 7 business types missing. |
| G13 | Collection types: `bag`, `list`, `log`, `lookup` | ❌ NO | — | Missing from type keywords and collection field pattern. |
| G14 | Constraint keywords (12): `nonnegative` through `ordered` | ❌ NO | — | None present in grammar. |
| G15 | Access mode keywords | ❌ NO | — | `modify`, `readonly`, `editable` missing. |
| G16 | Quantifier `each` | ❌ NO | — | Missing. |
| G17 | Prepositions `by`, `at`, `for` | ❌ NO | — | Missing. |
| G18 | Control `then` | ❌ NO | — | Missing from L358 (has `if`/`else` but not `then`). |
| G19 | Action keywords: `append`, `insert`, `put` | ❌ NO | — | Missing from L381-385. |
| G20 | Operators: `~=`, `!~`, `~` | ❌ NO | — | Case-insensitive operators missing from L413-429. |
| G21 | Typed constants (`'...'`) | ❌ NO | — | No single-quoted string pattern. |
| G22 | Parenthesized event args | ❌ NO | — | `event Name(Arg as type)` syntax not matched. Grammar uses retired `with` syntax (L148-188). |
| G23 | RuleDeclaration construct | ❌ NO | — | No pattern for `rule Expr because "msg"`. |
| G24 | StateEnsure construct | ❌ NO | — | No pattern for `in/to/from State ensure Expr because "msg"`. |
| G25 | EventEnsure construct | ❌ NO | — | No pattern for `on Event ensure Expr because "msg"`. |
| G26 | AccessMode construct | ❌ NO | — | No pattern for `in State modify Field editable`. |
| G27 | OmitDeclaration construct | ❌ NO | — | No pattern for `in State omit Field`. |
| G28 | StateAction construct | ❌ NO | — | No pattern for `to/from State -> action chain`. |
| G29 | EventHandler construct | ❌ NO | — | No pattern for `on Event -> action chain`. |
| G30 | Computed field syntax | ❌ NO | — | `field X as type <- expr` not specifically highlighted. `<-` is in `arrowOperator` but no construct pattern. |
| G31 | Function calls | ❌ NO | — | `min(...)`, `round(...)` etc. — no function-name highlighting. |
| G32 | Parentheses/brackets | Partial | `punctuation.precept` | Parentheses exist in code but no explicit grammar pattern matches `(` or `)`. |
| G33 | Choice type with options | ❌ NO | — | `choice of string("a","b","c")` not matched. |
| G34 | Ascending/descending | ❌ NO | — | Sort order modifiers missing. |
| G35 | `is set` / `is not set` operators | ❌ NO | — | Multi-token presence operators not highlighted. |

### 3.2 Stale / Incorrect Patterns

| # | Pattern | Line | Issue |
|---|---------|------|-------|
| S1 | `declarationKeywords` → `nullable` | L366 | STALE. Should be `optional`. `nullable` is not in TokenKind. |
| S2 | `declarationKeywords` → `invariant` | L366 | STALE. Should be `rule`. `invariant` is not in TokenKind. |
| S3 | `declarationKeywords` → `with` | L366 | STALE. `with` is not in TokenKind. Retired event-arg syntax. |
| S4 | `declarationKeywords` → `assert` | L366 | STALE. Should be `ensure`. `assert` is not in TokenKind. |
| S5 | `booleanNull` → `null` | L436 | STALE. `null` is not a keyword in the token catalog. Precept uses `is set`/`is not set`. |
| S6 | `eventWithArgsDeclaration` | L146-188 | STALE. Uses `event Name with Arg as type` syntax. Current syntax is `event Name(Arg as type)`. |
| S7 | `invariantStatement` | L276-286 | STALE. Uses `invariant` keyword. Should be `rule`. |
| S8 | `assertStatement` | L288-300 | STALE. Uses `on EventName assert`. Should be `on EventName ensure`. |
| S9 | `controlKeywords` mix | L356-361 | INCORRECT. Mixes declaration keywords (`precept`, `state`, `event`) with control flow (`if`, `when`). Should use catalog-derived scope groups. |
| S10 | `actionKeywords` mix | L380-385 | INCORRECT. Mixes actions (`set`, `add`), prepositions (`into`), membership (`contains`), and logical operators (`and`, `or`, `not`) into one scope. |

### 3.3 Scope Assignment Errors

| # | Token | Grammar Scope | Catalog Scope | Visual System Role |
|---|-------|--------------|---------------|--------------------|
| E1 | `precept` | `keyword.control.precept` (L359) | `keyword.declaration.precept` | Structure · Semantic |
| E2 | `state` | `keyword.control.precept` (L359) | `keyword.declaration.precept` | Structure · Semantic |
| E3 | `event` | `keyword.control.precept` (L359) | `keyword.declaration.precept` | Structure · Semantic |
| E4 | `field` | `keyword.other.precept` (L366) | `keyword.declaration.precept` | Structure · Semantic |
| E5 | `as` | `keyword.other.precept` (L366) | `keyword.declaration.precept` | Structure · Semantic |
| E6 | `because` | `keyword.other.precept` (L366) | `keyword.declaration.precept` | Structure · Semantic |
| E7 | `default` | `keyword.other.precept` (L366) | `keyword.declaration.precept` | Structure · Semantic |
| E8 | `and`, `or`, `not` | `keyword.other.precept` (L383) | `keyword.operator.logical.precept` | Logical operators |
| E9 | `contains` | `keyword.other.precept` (L383) | `keyword.operator.membership.precept` | Membership |
| E10 | `into` | `keyword.other.precept` (L383) | `keyword.control.precept` | Preposition |
| E11 | `all`, `any` | `keyword.control.precept` (L359) | `keyword.other.quantifier.precept` | Quantifier |
| E12 | `of` | `keyword.control.precept` (L359) | `keyword.control.precept` | ✓ Correct |
| E13 | `set` (action) | `keyword.other.precept` (L383) | `storage.type.precept` | Dual-use |
| E14 | `transition` | `keyword.other.precept` (L394) | `keyword.other.outcome.precept` | Outcome |
| E15 | `reject` | `keyword.other.precept` (L394/L68) | `keyword.other.outcome.precept` | Outcome |
| E16 | `edit` | `keyword.other.precept` (L367) | Not a TokenKind! | `edit` appears in root-edit pattern but is not in the token catalog. The construct is `RuleDeclaration`, not `edit`. Actually, `edit` is used for `rootEditDeclaration` — but the TokenKind enum doesn't have an Edit token. Checking... `edit` may be a stale surface concept. The Constructs catalog does not have a root-level `edit` construct. This needs verification. |

**Note on E16:** Looking at the Constructs catalog, there is no `edit` construct. The `OmitDeclaration` and `AccessMode` constructs handle field access. The `rootEditDeclaration` pattern in the hand-authored grammar (`edit all | edit Field1, Field2`) may be stale — I need to verify whether root-level `edit` still exists. Looking at the sample files: `customer-profile.precept` uses `writable` modifier on fields, not `edit`. `fee-schedule.precept` uses `writable`. No sample uses `edit all`. This pattern appears stale. **However**, the `rootEditDeclaration` pattern is in both the hand-authored grammar AND the generator, so it may still be valid for backward compatibility. Needs owner clarification.

---

## 4. Generator Audit

### 4.1 Generator Strengths

1. **Catalog-driven keyword emission** (L38-77): Reads `Tokens.All`, groups by `TextMateScope`, emits one alternation pattern per scope. This correctly picks up all 139 tokens. ✓
2. **Typed constants** (L134-146): Handles single-quoted `'...'` strings. Hand-authored grammar doesn't. ✓
3. **Collection member access** (L453-470): Includes `countof` and `peekby`. ✓

### 4.2 Generator Gap Table

| # | Language Construct / Feature | Generator Pattern? | Correct Scope? | Gap |
|---|-----------------------------|----|----|----|
| GG1 | Message strings (`because "msg"`, `reject "msg"`) | ❌ NO | — | **Critical.** Visual system reserves gold for message payloads. Without this, all strings get `string.quoted.double.precept` — no visual interrupt for rules. |
| GG2 | Parenthesized event args `event Name(Arg as type)` | ❌ NO | — | Generator's `eventWithArgsDeclaration` (L218-258) uses stale `with` syntax. |
| GG3 | State modifiers beyond `initial` | ❌ NO | — | `stateDeclaration` (L180-215) only matches `initial`. Missing: `terminal`, `required`, `irreversible`, `success`, `warning`, `error`. These ARE emitted as catalog keywords under `storage.modifier.state.precept`, but the structural pattern doesn't recognize them in state declaration context. |
| GG4 | RuleDeclaration construct | ❌ NO | — | No `rule Expr because "msg"` pattern. |
| GG5 | StateEnsure constructs | ❌ NO | — | No `in/to/from State ensure Expr because "msg"` pattern. |
| GG6 | EventEnsure construct | ❌ NO | — | No `on Event ensure Expr because "msg"` pattern. |
| GG7 | AccessMode construct | ❌ NO | — | No `in State modify Field editable/readonly` pattern. |
| GG8 | OmitDeclaration construct | ❌ NO | — | No `in State omit Field` pattern. |
| GG9 | StateAction construct | ❌ NO | — | No `to/from State -> action chain` pattern. |
| GG10 | EventHandler construct | ❌ NO | — | No `on Event -> action chain` (stateless). |
| GG11 | Computed field declaration | ❌ NO | — | No `field X as type <- expr` structural pattern. |
| GG12 | Function call highlighting | ❌ NO | — | `min(...)`, `round(...)` etc. not highlighted as function names. |
| GG13 | Choice type with options | ❌ NO | — | `choice of string("a","b","c")` not matched. |
| GG14 | `assertStatement` uses stale `assert` | ✅ Present | ❌ Wrong keyword | L416-432: Uses `assert` instead of `ensure`. |
| GG15 | `no transition` compound keyword | ❌ NO | — | Two-word outcome not specially highlighted. The individual words are catalog-derived, but the compound meaning is lost. |
| GG16 | `is set` / `is not set` operators | ❌ NO | — | Multi-token presence operators. |
| GG17 | `ScopeToRepositoryKey` naming | — | — | Appends "Keywords" to scope, producing confusing repo keys like `keyword.declaration.preceptKeywords`. Should use descriptive names (e.g., `declarationKeywords`). |
| GG18 | `eventWithArgsDeclaration` broken `$ref` | ❌ Broken | — | L244: Uses `["$ref"] = "#/repository/storage.type.precept"` — TextMate doesn't support `$ref`. Should be `["include"] = "#storage.type.preceptKeywords"`. |
| GG19 | `fieldCollectionDeclaration` scope error | — | ❌ Wrong | L296: Uses `keyword.declaration.precept` for `field` keyword but the repo key in catalog patterns uses the same scope. Creates conflict with `keyword.declaration.preceptKeywords` repo entry — both claim `keyword.declaration.precept`. |
| GG20 | Missing punctuation patterns | ❌ NO | — | No explicit patterns for `(`, `)`, `[`, `]`. Catalog assigns them `punctuation.precept`. |

---

## 5. Authoritative Grammar Specification

### Spec Section 1: Scope Vocabulary

Every TextMate scope used in the Precept grammar, with semantic meaning and visual system role.

| # | TextMate Scope | Semantic Meaning | Visual System Role | Brand Color |
|---|---------------|------------------|-------------------|-------------|
| S1 | `comment.line.number-sign.precept` | Line comment starting with `#` | Comments | `#9096A6` italic |
| S2 | `keyword.declaration.precept` | Declaration and behavioral keywords | Structure · Semantic | `#4338CA` **bold** |
| S3 | `keyword.control.precept` | Prepositions and control flow | Structure · Grammar | `#6366F1` normal |
| S4 | `keyword.other.action.precept` | Action verbs in action chains | Structure · Grammar | `#6366F1` normal |
| S5 | `keyword.other.outcome.precept` | Transition, rejection, no-transition outcomes | Structure · Grammar | `#6366F1` normal |
| S6 | `keyword.other.access-mode.precept` | Access mode declarations | Structure · Grammar | `#6366F1` normal |
| S7 | `keyword.other.quantifier.precept` | Universal/existential quantifiers | Structure · Grammar | `#6366F1` normal |
| S8 | `keyword.other.constraint.precept` | Field constraint modifiers | Structure · Grammar | `#6366F1` normal |
| S9 | `keyword.operator.logical.precept` | `and`, `or`, `not` | Structure · Grammar | `#6366F1` normal |
| S10 | `keyword.operator.membership.precept` | `contains`, `is` | Structure · Grammar | `#6366F1` normal |
| S11 | `keyword.operator.precept` | Symbol operators (`==`, `!=`, `+`, `-`, etc.) | Structure · Grammar | `#6366F1` normal |
| S12 | `keyword.operator.arrow.precept` | `->` and `<-` arrows | Structure · Grammar | `#6366F1` normal |
| S13 | `storage.type.precept` | Type keywords (all scalar, temporal, business, collection types) | Data · Types | `#9AA8B5` normal |
| S14 | `storage.modifier.state.precept` | State lifecycle modifiers | Data · Types | `#9AA8B5` normal |
| S15 | `entity.name.type.state.precept` | State names | States | `#A898F5` normal |
| S16 | `entity.name.function.event.precept` | Event names | Events | `#30B8E8` normal |
| S17 | `entity.name.precept.message.precept` | Precept name (in header) | Identity | `#A898F5` normal |
| S18 | `variable.other.field.precept` | Field names | Data · Names | `#B0BEC5` normal |
| S19 | `variable.parameter.precept` | Event argument names (in declarations) | Data · Names | `#B0BEC5` normal |
| S20 | `variable.other.property.precept` | Property accessor after dot | Data · Names | `#B0BEC5` normal |
| S21 | `variable.other.precept` | Catch-all identifier reference | Data · Names | `#B0BEC5` normal |
| S22 | `constant.numeric.precept` | Number literals | Data · Values | `#84929F` normal |
| S23 | `constant.language.boolean.precept` | `true`, `false` | Data · Values | `#84929F` normal |
| S24 | `string.quoted.double.precept` | Double-quoted strings (non-message) | Data · Values | `#84929F` normal |
| S25 | `string.quoted.double.message.precept` | Message strings in `because`/`reject` | Rules · Messages | `#FBBF24` normal |
| S26 | `string.quoted.single.precept` | Single-quoted typed constants | Data · Values | `#84929F` normal |
| S27 | `constant.character.escape.precept` | Escape sequences in strings | Data · Values | `#84929F` normal |
| S28 | `punctuation.precept` | `.`, `,`, `(`, `)`, `[`, `]` | Structure · Grammar | `#6366F1` normal |
| S29 | `punctuation.separator.comma.precept` | Comma separator (in lists) | Structure · Grammar | `#6366F1` normal |
| S30 | `punctuation.accessor.precept` | Dot accessor (in member access) | Structure · Grammar | `#6366F1` normal |
| S31 | `keyword.other.precept` | Special member names (`countof`, `peekby`) | Data · Names | `#B0BEC5` normal |
| S32 | `support.function.precept` | Built-in function names | Data · Names | `#B0BEC5` normal |
| S33 | `meta.declaration.precept.precept` | Precept header construct (meta) | — | — |
| S34 | `meta.declaration.state.precept` | State declaration construct (meta) | — | — |
| S35 | `meta.declaration.event.precept` | Event declaration construct (meta) | — | — |
| S36 | `meta.field-declaration.precept` | Field declaration construct (meta) | — | — |
| S37 | `meta.transition.header.precept` | Transition row header (meta) | — | — |
| S38 | `meta.ensure.state.precept` | State ensure construct (meta) | — | — |
| S39 | `meta.ensure.event.precept` | Event ensure construct (meta) | — | — |
| S40 | `meta.access-mode.precept` | Access mode construct (meta) | — | — |
| S41 | `meta.omit.precept` | Omit declaration construct (meta) | — | — |
| S42 | `meta.action.state.precept` | State action construct (meta) | — | — |
| S43 | `meta.handler.event.precept` | Event handler construct (meta) | — | — |
| S44 | `meta.rule.precept` | Rule declaration construct (meta) | — | — |
| S45 | `meta.message.precept` | Message string context (meta) | — | — |
| S46 | `meta.computed-field.precept` | Computed field declaration (meta) | — | — |
| S47 | `meta.transition.target.precept` | Transition target (meta) | — | — |
| S48 | `meta.event-arg-ref.precept` | Event.arg dot access (meta) | — | — |
| S49 | `meta.collection-member.precept` | Collection.property access (meta) | — | — |

### Spec Section 2: Repository Patterns (Complete Enumeration)

#### 2.1 Comment

- **Key:** `comment`
- **Type:** `match`
- **Scope:** `comment.line.number-sign.precept`
- **Regex:** `#.*$`
- **Covers:** Line comments

#### 2.2 Message Strings

- **Key:** `messageStrings`
- **Type:** `match` (two patterns)
- **Scope:** captures `keyword.declaration.precept` for keyword, `string.quoted.double.message.precept` for message
- **Regex pattern 1:** `\b(because)(\s+)("(?:\\.|[^"\\])*")`
- **Regex pattern 2:** `\b(reject)(\s+)("(?:\\.|[^"\\])*")`
- **Covers:** Gold message payload in `because "..."` and `reject "..."` positions
- **Priority:** MUST precede generic `strings` pattern to prevent message strings from being consumed as regular strings
- **Visual system:** This is the **only** pattern that produces `string.quoted.double.message.precept` — the gold visual interrupt

#### 2.3 Strings

- **Key:** `strings`
- **Type:** `begin/end`
- **Scope:** `string.quoted.double.precept`
- **Begin:** `"`   End: `"`
- **Inner pattern:** `constant.character.escape.precept` for `\\.`
- **Covers:** All non-message double-quoted strings

#### 2.4 Typed Constants

- **Key:** `typedConstants`
- **Type:** `begin/end`
- **Scope:** `string.quoted.single.precept`
- **Begin:** `'`   End: `'`
- **Covers:** Single-quoted typed constants (`'USD'`, `'kg'`, `'2026-01-15'`)

#### 2.5 Precept Header

- **Key:** `preceptHeader`
- **Type:** `match`
- **Scope:** `meta.declaration.precept.precept`
- **Regex:** `^(\s*)(precept)(\s+)([A-Za-z_][A-Za-z0-9_]*)`
- **Captures:** `2` → `keyword.declaration.precept`, `4` → `entity.name.precept.message.precept`
- **Covers:** `precept LoanApplication`

#### 2.6 State Declaration

- **Key:** `stateDeclaration`
- **Type:** `match`
- **Scope:** `meta.declaration.state.precept`
- **Regex:** `^(\s*)(state)(\s+)(.*)`
- **Captures:** `2` → `keyword.declaration.precept`, `4` → sub-patterns:
  - State modifiers from catalog: `\b(initial|terminal|required|irreversible|success|warning|error)\b` → `storage.modifier.state.precept` (for `terminal`/`required`/`irreversible`/`success`/`warning`/`error`) and `keyword.declaration.precept` (for `initial`)
  - State names: `\b[A-Za-z_][A-Za-z0-9_]*\b` → `entity.name.type.state.precept`
  - Comma: `,` → `punctuation.separator.comma.precept`
- **Covers:** `state Draft initial, Submitted, Approved terminal success`
- **Critical change from current:** Must recognize ALL 7 state modifiers, not just `initial`

#### 2.7 Event Declaration (Parenthesized Args)

- **Key:** `eventDeclaration`
- **Type:** `match`
- **Scope:** `meta.declaration.event.precept`
- **Regex:** `^(\s*)(event)(\s+)((?:[A-Za-z_][A-Za-z0-9_]*\s*,\s*)*[A-Za-z_][A-Za-z0-9_]*)(\s*\(.*)?`
- **Captures:**
  - `2` → `keyword.declaration.precept`
  - `4` → sub-patterns for event names (`entity.name.function.event.precept`) and commas
  - `5` → sub-patterns for parenthesized args:
    - `initial` keyword → `keyword.declaration.precept`
    - Argument name before `as`: `\b([A-Za-z_][A-Za-z0-9_]*)(?=\s+as\b)` → `variable.parameter.precept`
    - `as` keyword → `keyword.declaration.precept`
    - Type keywords → include `#typeKeywords`
    - Constraint keywords → include `#constraintKeywords`
    - Default values → include `#numbers`, `#strings`, `#booleanLiterals`
    - Commas → `punctuation.separator.comma.precept`
    - Parentheses → `punctuation.precept`
- **Covers:** `event Submit(Applicant as string notempty, Amount as number)`
- **Critical change:** Replaces stale `eventWithArgsDeclaration` (used `with` syntax)

#### 2.8 Field Declaration (Scalar)

- **Key:** `fieldScalarDeclaration`
- **Type:** `match`
- **Scope:** `meta.field-declaration.precept`
- **Regex:** `^(\s*)(field)(\s+)((?:[A-Za-z_][A-Za-z0-9_]*\s*,\s*)*[A-Za-z_][A-Za-z0-9_]*)(\s+)(as)(\s+)(string|number|integer|decimal|boolean|choice|date|time|instant|duration|period|timezone|zoneddatetime|datetime|money|currency|quantity|unitofmeasure|dimension|price|exchangerate)(.*)`
- **Captures:**
  - `2` → `keyword.declaration.precept`
  - `4` → field names (`variable.other.field.precept`) + commas
  - `6` → `keyword.declaration.precept`
  - `8` → `storage.type.precept`
  - `9` → sub-patterns: constraint keywords, `optional`, `writable`, `default`, numbers, strings, typed constants, `<-` for computed
- **Covers:** All scalar field declarations including temporal and business-domain types
- **Note:** Type name list MUST be derived from the catalog (`Tokens.All` where category is `Type` and text is not null)

#### 2.9 Field Declaration (Collection)

- **Key:** `fieldCollectionDeclaration`
- **Type:** `match`
- **Scope:** `meta.field-declaration.precept`
- **Regex:** `^(\s*)(field)(\s+)((?:[A-Za-z_][A-Za-z0-9_]*\s*,\s*)*[A-Za-z_][A-Za-z0-9_]*)(\s+)(as)(\s+)(set|queue|stack|bag|list|log|lookup)(\s+)(of)(\s+)(~?(?:string|number|integer|decimal|boolean))(.*)`
- **Captures:**
  - `2` → `keyword.declaration.precept`
  - `4` → field names + commas
  - `6` → `keyword.declaration.precept`
  - `8` → `storage.type.precept` (collection type)
  - `10` → `keyword.control.precept` (`of`)
  - `12` → `storage.type.precept` (inner type, with optional `~` prefix)
  - `13` → sub-patterns for constraint keywords, modifiers
- **Covers:** `field Tags as set of string`, `field Items as bag of ~string`

#### 2.10 Computed Field Declaration

- **Key:** `computedFieldDeclaration`
- **Type:** `match`
- **Scope:** `meta.computed-field.precept`
- **Regex:** `^(\s*)(field)(\s+)([A-Za-z_][A-Za-z0-9_]*)(\s+)(as)(\s+)(string|number|integer|decimal|boolean|..types..)(\s+.*)?(<-)(\s+.*)`
- **Note:** This is hard to capture in a single regex because the `<-` can appear after optional modifiers. Recommend a separate pattern that matches `<-` preceded by field context, or handle via the existing field declaration patterns plus the arrow operator pattern.
- **Alternative approach:** The `<-` operator is already in the catalog. The constraint keywords and type keywords are already catalog-derived. A computed field declaration is just a field declaration that happens to contain `<-`. The structural pattern can be the same as `fieldScalarDeclaration` if the tail sub-patterns include the `<-` operator and expression patterns.

#### 2.11 Root Edit Declaration

- **Key:** `rootEditDeclaration`
- **Type:** `match`
- **Scope:** `meta.declaration.edit.root.precept`
- **Status:** **NEEDS VERIFICATION.** The `edit` keyword is not in the `TokenKind` enum. No sample file uses root-level `edit`. This pattern may be stale. If confirmed stale, remove. If still valid, add `edit` to TokenKind.

#### 2.12 Transition Row Header

- **Key:** `fromOnHeader`
- **Type:** `match`
- **Scope:** `meta.transition.header.precept`
- **Regex:** `^(\s*)(from)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*(?:\s*,\s*[A-Za-z_][A-Za-z0-9_]*)*)(\s+)(on)(\s+)([A-Za-z_][A-Za-z0-9_]*)`
- **Captures:**
  - `2` → `keyword.control.precept` (`from`)
  - `4` → `entity.name.type.state.precept` (source state(s)) — `any` should get `keyword.other.quantifier.precept`
  - `6` → `keyword.control.precept` (`on`)
  - `8` → `entity.name.function.event.precept` (event name)
- **Covers:** `from Draft on Submit`, `from any on Cancel`
- **Note:** `any` in state position should get quantifier scope, not state scope. Needs sub-pattern.

#### 2.13 State Ensure

- **Key:** `stateEnsure`
- **Type:** `match`
- **Scope:** `meta.ensure.state.precept`
- **Regex:** `^(\s*)(in|to|from)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\s+)(ensure)\b`
- **Captures:**
  - `2` → `keyword.control.precept` (anchor preposition)
  - `4` → `entity.name.type.state.precept` (state name) — `any` → `keyword.other.quantifier.precept`
  - `6` → `keyword.declaration.precept` (`ensure`)
- **Covers:** `in Approved ensure amount > 0 because "..."`

#### 2.14 Event Ensure

- **Key:** `eventEnsure`
- **Type:** `match`
- **Scope:** `meta.ensure.event.precept`
- **Regex:** `^(\s*)(on)(\s+)([A-Za-z_][A-Za-z0-9_]*)(\s+)(ensure)\b`
- **Captures:**
  - `2` → `keyword.control.precept` (`on`)
  - `4` → `entity.name.function.event.precept` (event name)
  - `6` → `keyword.declaration.precept` (`ensure`)
- **Covers:** `on Submit ensure Amount > 0 because "..."`

#### 2.15 Access Mode

- **Key:** `accessMode`
- **Type:** `match`
- **Scope:** `meta.access-mode.precept`
- **Regex:** `^(\s*)(in)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\s+)(modify)(\s+)((?:[A-Za-z_][A-Za-z0-9_]*\s*,\s*)*[A-Za-z_][A-Za-z0-9_]*|all)(\s+)(editable|readonly)`
- **Captures:**
  - `2` → `keyword.control.precept`
  - `4` → `entity.name.type.state.precept` (state name)
  - `6` → `keyword.other.access-mode.precept` (`modify`)
  - `8` → `variable.other.field.precept` (field names) or `keyword.other.quantifier.precept` (`all`)
  - `10` → `keyword.other.access-mode.precept` (`editable`/`readonly`)
- **Covers:** `in Draft modify Amount editable`, `in UnderReview modify AdjusterName editable when not FraudFlag`

#### 2.16 Omit Declaration

- **Key:** `omitDeclaration`
- **Type:** `match`
- **Scope:** `meta.omit.precept`
- **Regex:** `^(\s*)(in)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\s+)(omit)(\s+)([A-Za-z_][A-Za-z0-9_]*)`
- **Captures:**
  - `2` → `keyword.control.precept`
  - `4` → `entity.name.type.state.precept`
  - `6` → `keyword.other.access-mode.precept` (`omit`)
  - `8` → `variable.other.field.precept`
- **Covers:** `in Draft omit InternalNotes`

#### 2.17 State Action

- **Key:** `stateAction`
- **Type:** `match`
- **Scope:** `meta.action.state.precept`
- **Regex:** `^(\s*)(to|from)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\s+)(->)`
- **Captures:**
  - `2` → `keyword.control.precept` (anchor preposition)
  - `4` → `entity.name.type.state.precept` (state name)
  - `6` → `keyword.operator.arrow.precept` (`->`)
- **Covers:** `to Confirmed -> set PaymentReceived = true`
- **Note:** Must precede `stateEnsure` in pattern order since both start with `to`/`from`. Disambiguated by `->` vs `ensure`.

#### 2.18 Event Handler

- **Key:** `eventHandler`
- **Type:** `match`
- **Scope:** `meta.handler.event.precept`
- **Regex:** `^(\s*)(on)(\s+)([A-Za-z_][A-Za-z0-9_]*)(\s+)(->)`
- **Captures:**
  - `2` → `keyword.control.precept` (`on`)
  - `4` → `entity.name.function.event.precept` (event name)
  - `6` → `keyword.operator.arrow.precept` (`->`)
- **Covers:** `on UpdateName -> set name = newName` (stateless precepts)
- **Note:** Must precede `eventEnsure` in pattern order since both start with `on`.

#### 2.19 Rule Declaration

- **Key:** `ruleDeclaration`
- **Type:** `match`
- **Scope:** `meta.rule.precept`
- **Regex:** `^(\s*)(rule)\b`
- **Captures:** `2` → `keyword.declaration.precept`
- **Covers:** `rule amount > 0 because "..."`
- **Note:** Only needs to capture the `rule` keyword. The rest of the line is handled by included patterns (operators, identifiers, message strings, etc.)

#### 2.20 Transition Target

- **Key:** `transitionTarget`
- **Type:** `match`
- **Scope:** `meta.transition.target.precept`
- **Regex:** `\b(transition)(\s+)([A-Za-z_][A-Za-z0-9_]*)`
- **Captures:**
  - `1` → `keyword.other.outcome.precept`
  - `3` → `entity.name.type.state.precept`
- **Covers:** `transition Approved`

#### 2.21 No Transition

- **Key:** `noTransition`
- **Type:** `match`
- **Scope:** (captures only)
- **Regex:** `\b(no)(\s+)(transition)\b`
- **Captures:**
  - `1` → `keyword.other.outcome.precept`
  - `3` → `keyword.other.outcome.precept`
- **Covers:** `no transition`

#### 2.22 Event Arg Reference (Dot Access)

- **Key:** `eventArgReference`
- **Type:** `match`
- **Scope:** `meta.event-arg-ref.precept`
- **Regex:** `\b([A-Za-z_][A-Za-z0-9_]*)(\.)([A-Za-z_][A-Za-z0-9_]*)`
- **Captures:**
  - `1` → `entity.name.function.event.precept` (event name)
  - `2` → `punctuation.accessor.precept`
  - `3` → `variable.other.property.precept` (arg/property name)
- **Covers:** `Submit.Amount`, `Approve.Note`
- **Note:** This pattern is ambiguous — it also matches `Collection.count`. The `collectionMemberAccess` pattern must precede this one.

#### 2.23 Collection Member Access

- **Key:** `collectionMemberAccess`
- **Type:** `match`
- **Regex:** `\b([A-Za-z_][A-Za-z0-9_]*)(\.)(\bcount|countof|min|max|peek|peekby\b)`
- **Captures:**
  - `1` → `variable.other.field.precept` (collection field name)
  - `2` → `punctuation.accessor.precept`
  - `3` → `variable.other.property.precept` (member name)
- **Covers:** `MissingDocuments.count`, `Queue.peek`
- **Priority:** Must precede `eventArgReference` to prevent `Collection.count` from being highlighted as event.arg.

#### 2.24 Function Calls

- **Key:** `functionCalls`
- **Type:** `match`
- **Scope:** (captures only)
- **Regex:** `\b(min|max|abs|clamp|floor|ceil|truncate|round|approximate|pow|sqrt|trim|startsWith|endsWith|toLower|toUpper|left|right|mid|now)(\s*\()`
- **Captures:**
  - `1` → `support.function.precept`
  - `2` → `punctuation.precept`
- **Covers:** `min(x, y)`, `round(amount, 2)`, `trim(name)`, `now()`
- **Note:** Function name list MUST be derived from `Functions.All` (via the function catalog). CI variants `~startsWith` and `~endsWith` need a separate pattern: `(~)(startsWith|endsWith)(\s*\()`.

#### 2.25 Catalog-Derived Keyword Groups

These are generated automatically by reading `Tokens.All`, grouping by `TextMateScope`, and emitting one alternation pattern per scope. The generator already does this (L38-77 of `Program.cs`).

**Repository keys** (use descriptive names, not scope-suffixed):

| Key | Scope | Tokens |
|-----|-------|--------|
| `declarationKeywords` | `keyword.declaration.precept` | `as`, `ascending`, `because`, `default`, `descending`, `ensure`, `event`, `field`, `initial`, `optional`, `precept`, `rule`, `state`, `writable` |
| `controlKeywords` | `keyword.control.precept` | `at`, `by`, `else`, `for`, `from`, `if`, `in`, `into`, `of`, `on`, `then`, `to`, `when` |
| `actionKeywords` | `keyword.other.action.precept` | `add`, `append`, `clear`, `dequeue`, `enqueue`, `insert`, `pop`, `push`, `put`, `remove` |
| `outcomeKeywords` | `keyword.other.outcome.precept` | `no`, `reject`, `transition` |
| `accessModeKeywords` | `keyword.other.access-mode.precept` | `editable`, `modify`, `omit`, `readonly` |
| `logicalOperators` | `keyword.operator.logical.precept` | `and`, `not`, `or` |
| `membershipOperators` | `keyword.operator.membership.precept` | `contains`, `is` |
| `quantifierKeywords` | `keyword.other.quantifier.precept` | `all`, `any`, `each` |
| `stateModifiers` | `storage.modifier.state.precept` | `error`, `irreversible`, `required`, `success`, `terminal`, `warning` |
| `constraintKeywords` | `keyword.other.constraint.precept` | `max`, `maxcount`, `maxlength`, `maxplaces`, `min`, `mincount`, `minlength`, `nonnegative`, `nonzero`, `notempty`, `ordered`, `positive` |
| `typeKeywords` | `storage.type.precept` | `bag`, `boolean`, `choice`, `currency`, `date`, `datetime`, `decimal`, `dimension`, `duration`, `exchangerate`, `instant`, `integer`, `list`, `log`, `lookup`, `money`, `number`, `period`, `price`, `quantity`, `queue`, `set`, `stack`, `string`, `time`, `timezone`, `unitofmeasure`, `zoneddatetime` |
| `booleanLiterals` | `constant.language.boolean.precept` | `false`, `true` |
| `symbolOperators` | `keyword.operator.precept` | `!=`, `!~`, `%`, `*`, `+`, `-`, `/`, `<`, `<=`, `==`, `>`, `>=`, `=`, `~`, `~=` |
| `arrowOperators` | `keyword.operator.arrow.precept` | `->`, `<-` |
| `memberNameKeywords` | `keyword.other.precept` | `countof`, `peekby` |

#### 2.26 Numbers

- **Key:** `numbers`
- **Type:** `match`
- **Scope:** `constant.numeric.precept`
- **Regex:** `\b\d+(?:\.\d+)?\b`

#### 2.27 Punctuation

- **Key:** `punctuation`
- **Type:** `match`
- **Scope:** `punctuation.precept`
- **Regex:** `[()[\].,]` (individual captures for finer scoping optional)

#### 2.28 Identifier Reference (Catch-All)

- **Key:** `identifierReference`
- **Type:** `match`
- **Scope:** `variable.other.precept`
- **Regex:** `\b[A-Za-z_][A-Za-z0-9_]*\b`
- **Priority:** LAST in pattern order. This is the catch-all.

### Spec Section 3: Top-Level Pattern Ordering

Ordered from most-specific to least-specific to prevent false matches.

```json
{
  "patterns": [
    { "include": "#comment" },
    { "include": "#messageStrings" },
    { "include": "#strings" },
    { "include": "#typedConstants" },
    { "include": "#preceptHeader" },
    { "include": "#stateDeclaration" },
    { "include": "#eventDeclaration" },
    { "include": "#fieldCollectionDeclaration" },
    { "include": "#fieldScalarDeclaration" },
    { "include": "#ruleDeclaration" },
    { "include": "#stateAction" },
    { "include": "#stateEnsure" },
    { "include": "#eventHandler" },
    { "include": "#eventEnsure" },
    { "include": "#accessMode" },
    { "include": "#omitDeclaration" },
    { "include": "#fromOnHeader" },
    { "include": "#noTransition" },
    { "include": "#transitionTarget" },
    { "include": "#functionCalls" },
    { "include": "#collectionMemberAccess" },
    { "include": "#eventArgReference" },
    { "include": "#arrowOperators" },
    { "include": "#symbolOperators" },
    { "include": "#logicalOperators" },
    { "include": "#membershipOperators" },
    { "include": "#stateModifiers" },
    { "include": "#constraintKeywords" },
    { "include": "#typeKeywords" },
    { "include": "#declarationKeywords" },
    { "include": "#controlKeywords" },
    { "include": "#actionKeywords" },
    { "include": "#outcomeKeywords" },
    { "include": "#accessModeKeywords" },
    { "include": "#quantifierKeywords" },
    { "include": "#memberNameKeywords" },
    { "include": "#booleanLiterals" },
    { "include": "#numbers" },
    { "include": "#punctuation" },
    { "include": "#identifierReference" }
  ]
}
```

**Ordering rationale:**
1. Comments first — `#` to end of line must be captured before anything else
2. Message strings before regular strings — `because "msg"` must get gold scope before `"msg"` gets consumed as a regular string
3. Typed constants — `'USD'` before identifiers
4. Construct-level patterns (most-specific) — declaration headers capture entire lines with contextual scoping
5. `stateAction` before `stateEnsure` — both start with `to`/`from`, disambiguated by `->` vs `ensure`
6. `eventHandler` before `eventEnsure` — both start with `on`, disambiguated by `->` vs `ensure`
7. `noTransition` before `transitionTarget` — `no transition` is a compound keyword
8. Dot-access patterns — `collectionMemberAccess` before `eventArgReference` to prevent `F.count` → event scope
9. `functionCalls` — before identifierReference catch-all
10. Operator patterns — arrows first (longest match), then symbol, then keyword operators
11. Keyword groups from catalog (most-specific scope to least-specific)
12. Literals and numbers
13. Catch-all identifier last

---

## 6. Coverage Gaps (Current Grammar)

### Gaps in the hand-authored grammar (35 items from audit section 3.1, G1–G35)

See Section 3.1 above for the complete gap table. Summary of critical gaps:

1. **35 missing language constructs/tokens** (G1–G35)
2. **10 stale/incorrect patterns** (S1–S10) — 3 retired keywords, 2 retired syntax forms, 5 scope misassignments
3. **16 scope assignment errors** (E1–E16) — tokens assigned to wrong semantic category

### Gaps in the grammar generator (20 items from audit section 4.2, GG1–GG20)

See Section 4.2 above. Summary of critical gaps:

1. **GG1: Missing message strings** — most critical for visual system compliance
2. **GG2: Stale event arg syntax** — uses `with` instead of parenthesized args
3. **GG3-GG13: 11 missing construct patterns** — rules, ensures, access modes, state actions, handlers, computed fields, function calls
4. **GG14: Stale `assert` keyword** — should be `ensure`
5. **GG17: Bad repo key naming** — confusing scope-suffixed names
6. **GG18: Broken `$ref`** — TextMate doesn't support JSON `$ref`

---

## 7. Generator Completion Requirements

Numbered list keyed to spec entries above.

### Must-Fix (blocks parity with hand-authored grammar + visual system compliance)

1. **Add `messageStrings` pattern (Spec §2.2).** This is the single most important pattern for visual system compliance. Without it, message payloads are indistinguishable from regular strings — destroying the gold visual interrupt that the brand mandates. Emit TWO match patterns: one for `because "..."`, one for `reject "..."`. Captures must assign `keyword.declaration.precept` to the keyword and `string.quoted.double.message.precept` to the string.

2. **Replace `eventWithArgsDeclaration` with parenthesized-arg syntax (Spec §2.7).** Current pattern (L218-258) matches `event Name with Arg as type`. Replace with pattern matching `event Name(Arg as type, ...)`. Remove the `with` keyword from the structural pattern.

3. **Expand `stateDeclaration` to recognize all 7 state modifiers (Spec §2.6).** Current pattern (L180-215) only matches `initial`. Add sub-patterns for `terminal`, `required`, `irreversible`, `success`, `warning`, `error` with scope `storage.modifier.state.precept`.

4. **Replace `assertStatement` with `eventEnsure` and `stateEnsure` (Spec §2.13-2.14).** Current pattern (L416-432) uses stale `assert`. Replace with two patterns: `on Event ensure Expr` and `in/to/from State ensure Expr`.

5. **Add `ruleDeclaration` pattern (Spec §2.19).** Match `rule` keyword at line start.

6. **Add `accessMode` pattern (Spec §2.15).** Match `in State modify Field editable/readonly`.

7. **Add `omitDeclaration` pattern (Spec §2.16).** Match `in State omit Field`.

8. **Add `stateAction` pattern (Spec §2.17).** Match `to/from State -> action chain`.

9. **Add `eventHandler` pattern (Spec §2.18).** Match `on Event -> action chain`.

10. **Add `noTransition` pattern (Spec §2.21).** Match `no transition` as a compound keyword.

11. **Add `functionCalls` pattern (Spec §2.24).** Match known function names followed by `(`. Derive function name list from `Functions.All` catalog.

12. **Fix `ScopeToRepositoryKey` naming (Spec §2.25).** Replace scope-suffixed keys with descriptive names. Current: `keyword.declaration.preceptKeywords`. Proposed: `declarationKeywords`.

13. **Fix broken `$ref` in `eventWithArgsDeclaration` (GG18).** L244 uses `["$ref"]` which TextMate doesn't support. Replace with `["include"]`.

14. **Expand `fieldScalarDeclaration` type list (Spec §2.8).** Current pattern (L322) lists `string|number|integer|decimal|boolean|choice`. Must include all 27 type keywords from catalog. Derive from `Tokens.All` where category is `Type`.

15. **Expand `fieldCollectionDeclaration` to include all collection types (Spec §2.9).** Current pattern (L293) includes `set|queue|stack|bag|list|log|lookup` ✓. Inner type list needs expansion to include `integer`, `decimal`.

16. **Update top-level pattern ordering (Spec §3).** Current ordering (L491-524) must be restructured per spec. `messageStrings` must come before `strings`. New construct patterns must be inserted at correct priority.

### Should-Fix (improves correctness and visual system alignment)

17. **Add `any` quantifier sub-pattern in state position.** In `fromOnHeader`, `stateEnsure`, `accessMode`, etc., `any` should get `keyword.other.quantifier.precept`, not `entity.name.type.state.precept`.

18. **Add `punctuation` patterns for parentheses and brackets (Spec §2.27).** Explicit patterns for `(`, `)`, `[`, `]` with `punctuation.precept`.

19. **Verify `rootEditDeclaration` validity (Spec §2.11).** If `edit` is not in `TokenKind` and no sample uses it, remove. If still valid, add to catalog first.

20. **Add `computedFieldDeclaration` context (Spec §2.10).** At minimum, the `<-` operator pattern is sufficient. Consider whether a dedicated structural pattern is needed.

21. **Handle `choice of string("a","b","c")` syntax.** The parenthesized choice options need string highlighting within the type declaration. Currently the strings would be captured by the generic `strings` pattern, which is acceptable.

### Won't-Fix in Grammar (semantic tokens only)

22. **Italic for constrained states/events.** TextMate cannot apply `fontStyle: italic` based on semantic context (whether a state participates in `ensure` rules). This requires the semantic token provider, which already exists in the language server.

23. **Context-dependent `set` scoping.** `set` as action verb vs collection type. TextMate can't disambiguate. Semantic tokens handle this.

24. **`null` removal.** `null` is not a keyword. The hand-authored grammar has it but the generator doesn't. No action needed — the generator is correct.

---

## Appendix A: Brand Doc Sync Items

The following items in `design/brand/brand-decisions.md` need updating to match catalog reality:

1. Replace `nullable` with `optional` in the Structure · Grammar keyword list
2. Remove `write` from the Structure · Semantic keyword list (retired B4)
3. Add `rule` to Structure · Semantic keyword list
4. Add `ensure` to Structure · Semantic keyword list
5. Add `writable` to Structure · Semantic keyword list (or Grammar — decision needed)
6. Add `optional` to Structure · Semantic keyword list (or Grammar — decision needed)
7. The brand doc's 2-tier keyword split (Semantic vs Grammar) doesn't map 1:1 to the catalog's 14-category scope model. Consider updating the brand doc to reference catalog categories or accept that the theme mediates between the two.

## Appendix B: Theme Configuration Requirements

For the visual system to work as designed, the VS Code theme must include rules mapping scopes to colors and styles:

```json
{
  "editor.tokenColorCustomizations": {
    "textMateRules": [
      { "scope": "keyword.declaration.precept", "settings": { "foreground": "#4338CA", "fontStyle": "bold" } },
      { "scope": "keyword.control.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.other.action.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.other.outcome.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.other.access-mode.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.other.quantifier.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.other.constraint.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.operator.logical.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.operator.membership.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.operator.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.operator.arrow.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.other.precept", "settings": { "foreground": "#B0BEC5" } },
      { "scope": "storage.type.precept", "settings": { "foreground": "#9AA8B5" } },
      { "scope": "storage.modifier.state.precept", "settings": { "foreground": "#9AA8B5" } },
      { "scope": "entity.name.type.state.precept", "settings": { "foreground": "#A898F5" } },
      { "scope": "entity.name.function.event.precept", "settings": { "foreground": "#30B8E8" } },
      { "scope": "entity.name.precept.message.precept", "settings": { "foreground": "#A898F5" } },
      { "scope": "variable.other.field.precept", "settings": { "foreground": "#B0BEC5" } },
      { "scope": "variable.parameter.precept", "settings": { "foreground": "#B0BEC5" } },
      { "scope": "variable.other.property.precept", "settings": { "foreground": "#B0BEC5" } },
      { "scope": "variable.other.precept", "settings": { "foreground": "#B0BEC5" } },
      { "scope": "support.function.precept", "settings": { "foreground": "#B0BEC5" } },
      { "scope": "constant.numeric.precept", "settings": { "foreground": "#84929F" } },
      { "scope": "constant.language.boolean.precept", "settings": { "foreground": "#84929F" } },
      { "scope": "string.quoted.double.precept", "settings": { "foreground": "#84929F" } },
      { "scope": "string.quoted.double.message.precept", "settings": { "foreground": "#FBBF24" } },
      { "scope": "string.quoted.single.precept", "settings": { "foreground": "#84929F" } },
      { "scope": "comment.line.number-sign.precept", "settings": { "foreground": "#9096A6", "fontStyle": "italic" } },
      { "scope": "punctuation.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "punctuation.separator.comma.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "punctuation.accessor.precept", "settings": { "foreground": "#6366F1" } }
    ]
  }
}
```

## Appendix C: Catalog-Driven Generation Principle

The grammar generator MUST derive all keyword lists, type names, function names, operator symbols, and constraint keywords from the catalog source of truth (`Tokens.All`, `Functions.All`, etc.). No hardcoded token sets in the generator. If a new keyword is added to the catalog, the generator's output must automatically include it without manual changes.

The generator's current approach (L38-77) is architecturally correct for catalog-derived keyword patterns. The structural patterns (construct-level) are hand-written in the generator but MUST reference catalog-derived keyword lists where they enumerate token alternatives (e.g., type names in field declarations, state modifier names in state declarations).

---

*End of specification.*

# ProofEngine Spec — Pre-Implementation Gap Analysis

**Date:** 2026-05-08
**Author:** Frank
**Commit reviewed:** `79c340357aee4e54520a539dca8208bc734e3606`
**Verdict:** NOT READY

**Spec files reviewed:**
- `docs/compiler/proof-engine.md` (983 lines — primary spec)
- `docs/compiler/graph-analyzer.md`
- `docs/compiler/type-checker.md`
- `docs/compiler/diagnostic-system.md`

**Source files reviewed:**
- `src/Precept/Pipeline/ProofEngine.cs` (stub)
- `src/Precept/Pipeline/ProofLedger.cs` (stub)
- `src/Precept/Pipeline/StateGraph.cs`
- `src/Precept/Pipeline/GraphAnalyzer.cs`
- `src/Precept/Pipeline/SemanticIndex.cs`
- `src/Precept/Pipeline/Compilation.cs`
- `src/Precept/Compiler.cs`
- `src/Precept/Language/ProofRequirement.cs`
- `src/Precept/Language/ProofRequirementKind.cs`
- `src/Precept/Language/ProofRequirements.cs`
- `src/Precept/Language/DiagnosticCode.cs`
- `src/Precept/Language/Diagnostics.cs`
- `src/Precept/Language/FaultCode.cs`
- `src/Precept/Language/Faults.cs`
- `src/Precept/Language/Modifier.cs`
- `src/Precept/Language/Modifiers.cs`
- `src/Precept/Runtime/Descriptors.cs`

---

## Executive Summary

The ProofEngine spec is architecturally strong — the two-pass design, four-strategy discharge model, proof/fault chain, and catalog-driven obligation instantiation are well-conceived. However, the spec has **three blocking gaps** and **seven significant gaps** that prevent implementation from starting cleanly. The most critical issue: the spec defines five `ProofRequirementKind` values but only describes discharge strategies for two of them (Numeric and Presence). `DimensionProofRequirement`, `ModifierRequirement`, and `QualifierCompatibilityProofRequirement` are defined in the DU but have zero strategy coverage — an implementer would have to invent discharge logic from scratch. Additionally, the `FieldModifierMeta.ProofDischarges` property the spec declares as "canonical" (CC#5 resolved) does not exist in the source code, and the output type `ProofLedger` described in the spec is materially different from the stub in source.

---

## Gap Inventory

### [BLOCKING] Gaps

---

**PE-G1: Three of five ProofRequirementKind values have no discharge strategy**
- **Severity:** BLOCKING
- **Location:** `proof-engine.md` §7 "Four Proof Strategies" (lines 412–766)
- **Description:** The spec defines five `ProofRequirementKind` subtypes in §6 (lines 348–389): `Numeric`, `Presence`, `Dimension`, `Modifier`, and `QualifierCompatibility`. The four strategies (Literal, Modifier, GuardInPath, FlowNarrowing) only describe discharge predicates for `NumericProofRequirement` and `PresenceProofRequirement`. The remaining three kinds are completely absent:
  - **`DimensionProofRequirement`** — "period operand must have required time dimension." No strategy says how this is proven. Is it a static type check (always provable by the type checker)? Does it need a new strategy?
  - **`ModifierRequirement`** — "field must declare required modifier (e.g. `ordered`)." No strategy covers this. Logically Strategy 2 (Modifier Proof) should handle it, but the Strategy 2 pseudocode (lines 536–569) only reads `FieldModifierMeta.ProofDischarges`, not `ModifierRequirement.Required` directly. The mapping is unspecified.
  - **`QualifierCompatibilityProofRequirement`** — "two operands must share a qualifier value on the specified axis." This is a dual-subject requirement. None of the four strategies handle dual-subject obligations. The spec provides no guidance on how to discharge this — is it always resolvable from type-checker qualifier propagation? Does it require a fifth strategy?
- **Why it matters:** An implementer would have to guess how to handle 3 of 5 obligation kinds. Two implementers would write different code. This is the definition of a blocking ambiguity.
- **Suggested resolution:** For each of the three unhandled kinds, the spec must state:
  1. Which strategy discharges it (existing or new), OR
  2. That it is always discharged by the type checker and never reaches the proof engine as an unresolved obligation, OR
  3. That it is always `Unresolved` and produces a diagnostic (defensive backstop).

  Likely answers based on code analysis:
  - `DimensionProofRequirement`: Likely always resolvable by type-checker period-dimension inference. If so, state that it reaches the proof engine pre-discharged, or that it is a type error (not a proof obligation) and should never appear.
  - `ModifierRequirement`: Likely checked by seeing if the field has `ModifierKind.Required` in its `Modifiers` array. Add this to Strategy 2 pseudocode.
  - `QualifierCompatibilityProofRequirement`: Likely checked by the type checker's `QualifierBinding` propagation. If so, state the handoff.

---

**PE-G2: `FieldModifierMeta.ProofDischarges` does not exist in source code**
- **Severity:** BLOCKING
- **Location:** `proof-engine.md` §7 Strategy 2, lines 505–572, especially the CC#5 resolution box at line 571
- **Description:** The spec declares at line 571: "✅ Resolved (CC#5) — `FieldModifierMeta.ProofDischarges` is now canonical" and references `ProofDischarge[]` on `FieldModifierMeta`. The actual `FieldModifierMeta` record in `src/Precept/Language/Modifier.cs` (lines 105–121) has **no** `ProofDischarges` property. The `ProofDischarge` record type does not exist anywhere in the source code. `grep` for `ProofDischarge` across all of `src/Precept/Language/` returns zero matches.

  The Strategy 2 pseudocode depends entirely on `meta.ProofDischarges` for its discharge logic (line 551: `foreach (var discharge in meta.ProofDischarges)`). Without this property, Strategy 2 cannot be implemented as specified.
- **Why it matters:** Strategy 2 is the second most common discharge strategy. It covers `positive`, `nonnegative`, `nonzero`, `notempty`, `min(N)`, `max(N)`. Without the catalog property, the implementer must either:
  (a) Add the property to the catalog first (design + implementation work), or
  (b) Hardcode per-modifier logic in the proof engine (violating catalog-driven architecture).
  Both are design decisions that must be made before coding starts.
- **Suggested resolution:** Add the `ProofDischarges` property to `FieldModifierMeta` and the `ProofDischarge` record type before implementation begins. This is a catalog prerequisite, not part of the proof engine implementation itself.

---

**PE-G3: Output type `ProofLedger` in spec diverges materially from source stub**
- **Severity:** BLOCKING
- **Location:** `proof-engine.md` §5 "Output", lines 172–287
- **Description:** The spec defines `ProofLedger` with five fields:
  ```csharp
  ProofLedger(
      ImmutableArray<ProofObligation> Obligations,
      ImmutableArray<FaultSiteLink> FaultSiteLinks,
      ImmutableArray<ConstraintInfluenceEntry> ConstraintInfluence,
      ImmutableArray<InitialStateSatisfiabilityResult> InitialStateResults,
      ImmutableArray<Diagnostic> Diagnostics
  )
  ```
  The source stub at `src/Precept/Pipeline/ProofLedger.cs` defines:
  ```csharp
  ProofLedger(ImmutableArray<Diagnostic> Diagnostics)
  ```
  The following types referenced by the spec's `ProofLedger` do **not exist** anywhere in the source:
  - `ProofObligation`
  - `ProofDisposition` (enum)
  - `ProofStrategy` (enum)
  - `FaultSiteLink`
  - `ConstraintInfluenceEntry`
  - `EventArgReference`
  - `InitialStateSatisfiabilityResult`
  - `UnsatisfiedConstraint`
  - `FaultSiteAnnotation`

  The `Compilation` record in `Compilation.cs` consumes `ProofLedger` but only reads `Diagnostics` — it has no field for `FaultSiteLinks` or `ConstraintInfluence`.
- **Why it matters:** The implementer must create ~10 new record types and expand the ProofLedger shape before any meaningful work begins. The spec needs to be explicit about whether these types are created as part of the ProofEngine implementation or as a prerequisite.
- **Suggested resolution:** State that Slice 0 of the implementation plan is "shape declarations" — creating all the output types in `ProofLedger.cs` and `SemanticIndex.cs` with empty-default construction, updating the `Compilation` record, and verifying the build stays green. This matches the pattern from TypeChecker (Slice 0 shape) and GraphAnalyzer.

---

### [SIGNIFICANT] Gaps

---

**PE-G4: `SemanticIndex.AllTypedExpressions` does not exist**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §9 "Failure Modes", line 968
- **Description:** The spec's Pass 1 pseudocode (line 968) references `semantics.AllTypedExpressions`:
  ```csharp
  foreach (var expr in semantics.AllTypedExpressions)
  ```
  No such property exists on `SemanticIndex`. The `SemanticIndex` record exposes `TransitionRows`, `Rules`, `Ensures`, `StateHooks`, `EventHandlers` — but no aggregated expression enumeration surface.
- **Why it matters:** The implementer needs to know exactly which `SemanticIndex` members to walk to collect all proof-relevant expressions. Walking `TransitionRows[].Actions[].ProofRequirements` is obvious, but what about guard expressions? Constraint conditions? Computed field expressions? State hook actions? The spec doesn't enumerate the walk targets.
- **Suggested resolution:** Replace `AllTypedExpressions` with an explicit list of walk targets:
  - `TransitionRows` → `Actions[].ProofRequirements` and `Guard` expressions
  - `Rules` → `Condition` expressions
  - `Ensures` → `Condition` expressions
  - `StateHooks` → `Actions[].ProofRequirements`
  - `EventHandlers` → `Actions[].ProofRequirements`
  - Computed fields → `ComputedExpression` (if proof-relevant)

  Or add the `AllTypedExpressions` helper to `SemanticIndex` as a prerequisite.

---

**PE-G5: `ConstraintIdentity` shapes in spec differ from implementation**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §5, lines 263–267
- **Description:** The spec defines:
  ```csharp
  public sealed record RuleIdentity(string RuleName, int Index) : ConstraintIdentity;
  public sealed record EnsureIdentity(ConstraintKind Kind, string? AnchorState, string? AnchorEvent, int Index) : ConstraintIdentity;
  ```
  The actual implementation in `SemanticIndex.cs` (lines 401–404) defines:
  ```csharp
  public sealed record RuleIdentity(int RuleIndex) : ConstraintIdentity;
  public sealed record EnsureIdentity(ConstraintKind Kind, string? AnchorName, int EnsureIndex) : ConstraintIdentity;
  ```
  Differences:
  1. `RuleIdentity`: spec has `(string RuleName, int Index)`, source has `(int RuleIndex)` — no `RuleName` field.
  2. `EnsureIdentity`: spec has `(ConstraintKind, string? AnchorState, string? AnchorEvent, int Index)`, source has `(ConstraintKind, string? AnchorName, int EnsureIndex)` — spec separates state/event anchors into two nullable fields; source uses a single `AnchorName`.
- **Why it matters:** The `ConstraintInfluenceEntry` output uses `ConstraintIdentity`. If the implementer follows the spec shapes, they'll create types that conflict with existing ones. If they follow the source shapes, the spec's `EventArgReference` resolution logic may not work as described.
- **Suggested resolution:** Update the spec to match the existing source shapes. The implementation is canonical — it was created during TypeChecker implementation and has tests.

---

**PE-G6: `FindEnclosingTransitionRow` is not specified**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §7 Strategy 3, line 625; Strategy 4, line 722
- **Description:** Both Strategy 3 and Strategy 4 call `FindEnclosingTransitionRow(obligation.Site, semantics)` to find the transition row that encloses the proof obligation's expression site. The spec never defines this function. The proof engine must know: given a `TypedExpression`, how do you find which `TypedTransitionRow` contains it?
  
  This is non-trivial because:
  1. `TypedExpression` nodes don't carry parent pointers or transition-row back-references.
  2. The proof engine would need to either build an expression→row index in Pass 1, or walk `TransitionRows[].Actions` looking for expression identity matches.
  3. Obligations on expressions in `TypedRule`, `TypedEnsure`, `TypedStateHook`, or `TypedEventHandler` have no enclosing transition row — what do Strategies 3/4 return for those?
- **Why it matters:** This is critical path logic for the two guard-based strategies. The spec's pseudocode uses it as a black box, but its implementation drives the data structure design of Pass 1.
- **Suggested resolution:** Specify that Pass 1 builds an `obligation → enclosing context` index. Define the context as a discriminated union: `TransitionRowContext(TypedTransitionRow)`, `ConstraintContext(TypedRule | TypedEnsure)`, `HookContext(TypedStateHook)`, `HandlerContext(TypedEventHandler)`. Strategies 3/4 only fire for `TransitionRowContext`. All other contexts return `false`.

---

**PE-G7: `ResolveSubject` is not specified**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §7 Strategy 1, line 445; Strategy 2, line 539
- **Description:** Strategy 1 calls `ResolveSubject(numeric.Subject, obligation.Site)` and Strategy 2 calls `GetFieldName(obligation.Requirement.Subject, obligation.Site)`. Neither is defined. Given the `ProofSubject` DU:
  - `ParamSubject(ParameterMeta Parameter)` — how do you resolve a parameter to a concrete expression node from the obligation site? The `ParameterMeta` has object identity, but how does one locate the corresponding argument expression in a `TypedFunctionCall` or operand in a `TypedBinaryOp`?
  - `SelfSubject(TypeAccessor? Accessor)` — how does one resolve "self" to the receiver expression in a `TypedMemberAccess`?
  
  The spec says `ParamSubject` "must be reference-equal to one of the `ParameterMeta` instances in the containing overload's `Parameters` list" (ProofRequirement.cs, line 16), which gives identity, but the resolution logic from identity to expression is missing.
- **Why it matters:** Subject resolution is the first step in every strategy. Without it being specified, the implementer must infer the mapping from `ParameterMeta` identity to `TypedExpression` arguments — a non-trivial piece of logic.
- **Suggested resolution:** Add a `ResolveSubject` pseudocode section that handles both `ParamSubject` and `SelfSubject`:
  - `ParamSubject`: For `TypedFunctionCall`, match `Parameter` identity against `ResolvedFunction`'s overload `Parameters` list to find the positional index, then return `Arguments[index]`. For `TypedBinaryOp`, match against `ResolvedOp`'s operation metadata parameters.
  - `SelfSubject`: For `TypedMemberAccess`, return `Object`. For `TypedAction`, return the field reference expression (requires knowing the field from `FieldName`).

---

**PE-G8: Initial-state satisfiability check is underspecified**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §7 "Initial-State Satisfiability", lines 863–883
- **Description:** The spec says: "For each constraint condition, check whether default field values satisfy it." This is vague. Specifically:
  1. What does "check" mean? Evaluate the constraint expression with default values? Symbolically analyze it? The spec doesn't say.
  2. How are "default field values" determined? Fields with `default` expressions have typed defaults in `TypedField.DefaultExpression`. Fields without defaults — what is their default? `0` for numeric? `""` for string? `null` for optional? The spec doesn't define the default value model.
  3. What about fields that are set by the initial event? The initial event's `set` actions provide values at instantiation. Does satisfiability account for initial event args, or only declared defaults?
  4. The spec says to check `ensure in Draft: ...`. But the `ConstraintKind.StateResident` anchor means "while in state", not "at entry". Is entry a special case of residency? Does entry use `ConstraintKind.StateEntry` anchors instead?
  5. Computed fields (`IsComputed = true`) have `ComputedExpression` not `DefaultExpression`. Are computed field values available for satisfiability?
- **Why it matters:** This check is one of the three output surfaces of the proof engine (alongside obligation discharge and constraint influence). Without clear semantics, the implementer must make design decisions that should be in the spec.
- **Suggested resolution:** Define the satisfiability algorithm explicitly:
  - State which fields are relevant (all fields? only fields referenced by initial-scope constraints?)
  - Define the "default value" for each type kind when no `default` is declared
  - State whether initial event arguments are considered (probably not — they're runtime values)
  - Define which constraint scopes are checked (`in`, `to`, both?)

---

**PE-G9: No diagnostic code for collection-empty proof failures**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §7 "Collection Non-Empty Proof", lines 885–899; `DiagnosticCode.cs`
- **Description:** The spec describes collection non-empty obligations (first, last, peek, dequeue, pop) but the only proof-stage diagnostic codes are:
  - 82: `UnsatisfiableGuard` (Warning)
  - 83: `DivisionByZero` (Error)
  - 84: `SqrtOfNegative` (Error)
  
  There is no proof-stage diagnostic for "collection may be empty when `first()` is called." The type-checker stage has `UnguardedCollectionAccess` (63) and `UnguardedCollectionMutation` (64), but these are `DiagnosticStage.Type` — they fire during type checking, not proof. If the proof engine is supposed to handle collection non-empty proof discharge, it needs its own diagnostic code for the "unresolved" case. Or alternatively, collection safety is fully handled by the type checker and the proof engine should NOT create obligations for them.
  
  The `FaultCode` enum has `CollectionEmptyOnAccess = 9` with `[StaticallyPreventable(DiagnosticCode.UnguardedCollectionAccess)]` — linking to the type-checker code, not a proof code.
- **Why it matters:** The spec says the proof engine handles collection non-empty obligations, but there's no diagnostic to emit if the obligation is unresolved. Either the spec is wrong (collection safety is the type checker's job entirely) or diagnostic codes are missing.
- **Suggested resolution:** Clarify which pipeline stage owns collection non-empty safety:
  - If the type checker already emits `UnguardedCollectionAccess`/`UnguardedCollectionMutation` for all cases, the proof engine should NOT create duplicate obligations. Remove collection non-empty from the proof engine spec.
  - If the proof engine handles the richer case (modifier proof + guard proof), add a proof-stage diagnostic code for unresolved collection obligations.

---

**PE-G10: `ExtractGuardConstraints` is not specified**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §7 Strategy 3, line 631
- **Description:** Strategy 3 calls `ExtractGuardConstraints(row.Guard)` to decompose a `TypedExpression` guard into simple constraint forms. The spec lists supported patterns (line 599–608) but doesn't specify:
  1. What happens with compound guards? `when A > 0 and B > 0` — are both constraints extracted? What about `or`?
  2. What happens with negation? `when not (A == 0)` — is this recognized as `A != 0`?
  3. What about nested function calls in guards? `when count(Items) > 0 and len(Name) > 3` — is `len(Name) > 3` a valid constraint form?
  4. Does the proof engine look inside `TypedConditional` (if/then/else) for guard constraints?
- **Why it matters:** The guard pattern language directly determines Strategy 3's power. Without clarity on compound/negated guards, the implementer must choose a scope that may be too narrow or too broad.
- **Suggested resolution:** Specify that `ExtractGuardConstraints`:
  - Decomposes `and` conjunctions recursively — each leaf becomes a separate constraint
  - Does NOT decompose `or` disjunctions — the proof engine cannot use a disjunct because either branch might be false
  - Handles simple negation by inverting the comparison operator
  - Ignores complex expressions (nested conditionals, quantifiers) — they are not constraint forms

---

### [ADVISORY] Gaps

---

**PE-G11: Spec references `Compilation` but doesn't address the Precept Builder gap**
- **Severity:** ADVISORY
- **Location:** `proof-engine.md` §8 "Downstream Consumers", lines 930–937
- **Description:** The spec references "Precept Builder" as a consumer of `FaultSiteLinks` and `ConstraintInfluence`, and references `precept-builder.md §Pass 4` (line 218, 236, 250). No `precept-builder.md` file exists in `docs/compiler/`. The consumer contract for `ProofLedger` is described in the proof engine spec but has no counterpart in any builder spec.
- **Why it matters:** The proof engine's output shape is driven by what the builder consumes. Without a builder spec, the output shape is hypothetical — it could change when the builder is designed. Implementation risk is moderate: the proof engine can be built to the spec, but the builder may require changes.
- **Suggested resolution:** Accept this gap for now — the builder is a future stage. Add a note in the proof engine spec: "Builder contract is forward-looking; output shape may evolve when `precept-builder.md` is authored."

---

**PE-G12: No specification of diagnostic message formatting for proof obligations**
- **Severity:** ADVISORY
- **Location:** `proof-engine.md` §9 "Failure Modes", line 981
- **Description:** The pseudocode calls `CreateDiagnostic(obligation)` but doesn't specify how the diagnostic message template parameters `{0}`, `{1}` are populated. The existing diagnostic entries in `Diagnostics.cs` have:
  - `DivisionByZero`: `"Division by zero: '{0}' can be zero when {1}"` — what is `{0}` (field name? expression text?) and `{1}` (state name? guard absence?)?
  - `SqrtOfNegative`: `"sqrt() requires a non-negative value, but '{0}' can be negative when {1}"` — same question.
  - `UnsatisfiableGuard`: `"The condition '{0}' on event '{1}' can never be true when {2}"` — three params.
- **Why it matters:** Without knowing what fills the template parameters, test authors can't assert diagnostic messages. This is a testability gap.
- **Suggested resolution:** Add a message-formatting table: for each diagnostic code, specify what each `{N}` parameter is (field name, expression text, state name, constraint description).

---

**PE-G13: Error propagation from upstream stages is unspecified**
- **Severity:** ADVISORY
- **Location:** `proof-engine.md` §3 "Responsibilities and Boundaries"
- **Description:** The spec doesn't say whether the proof engine should short-circuit if the `SemanticIndex` or `StateGraph` already contain errors. Looking at the existing pipeline in `Compiler.cs`, every stage runs unconditionally — the proof engine receives its inputs regardless of upstream errors. But:
  1. If the `SemanticIndex` contains `TypedErrorExpression` nodes, can the proof engine encounter them during obligation instantiation? If so, what does it do?
  2. If the `StateGraph` has structural violation diagnostics (unreachable states), does the proof engine suppress obligations for those states? (The spec addresses this via `ReachabilityFact`, but doesn't address the case where the _graph analyzer itself_ emitted errors.)
- **Why it matters:** Without clarity, the implementer might crash on `TypedErrorExpression` nodes.
- **Suggested resolution:** Add: "Proof obligations are not instantiated for expression trees containing `TypedErrorExpression` — those trees already have type-checker diagnostics and no valid proof subject."

---

**PE-G14: `GuardRelationImpliesObligation` in Strategy 4 is a pattern-match black box**
- **Severity:** ADVISORY
- **Location:** `proof-engine.md` §7 Strategy 4, lines 758–766
- **Description:** The function `GuardRelationImpliesObligation` is described as "a simple pattern match on (guard.Op, expression.Op, requirement.Comparison) triples — not a solver" and provides three example triples. But the complete triple set is not enumerated. The spec gives examples but not an exhaustive table.
- **Why it matters:** An implementer would need to enumerate all valid triples. Given the bounded operator set, this is a finite list — but it's work the spec should contain.
- **Suggested resolution:** Add an exhaustive table of (guard.Op, expr.Op, requirement) → discharge triples. Given Precept's bounded operator set, this is likely ~10-15 entries.

---

**PE-G15: No specification of whether proof engine runs for stateless precepts**
- **Severity:** ADVISORY
- **Location:** `proof-engine.md` — absent from §3 and §9
- **Description:** The graph analyzer has explicit stateless-precept handling (emitting vacuous `TerminalCompletenessFact` and `DeadEndStateFact`). The proof engine spec doesn't address stateless precepts. Stateless precepts have `EventHandlers` instead of `TransitionRows` and no state machine. Questions:
  1. Do event handlers in stateless precepts carry proof requirements? (Yes — their `TypedAction` nodes can have `ProofRequirements`.)
  2. Do Strategies 3/4 (guard-based) apply to event handlers? (Event handlers don't have guards — `TypedEventHandler` has no `Guard` field.)
  3. Are there any proof obligations specific to stateless precepts?
- **Why it matters:** If the implementer ignores stateless precepts, proof obligations on event handler actions would be silently missed.
- **Suggested resolution:** Add a subsection: "For stateless precepts, the proof engine walks `EventHandlers[].Actions[]` for obligations. Strategies 1 (Literal) and 2 (Modifier) apply. Strategies 3/4 do not apply (event handlers have no guards). All unresolved obligations produce diagnostics as normal."

---

**PE-G16: Spec's `ProofObligation.Site` identity matching is underspecified**
- **Severity:** ADVISORY
- **Location:** `proof-engine.md` §5, line 217 (CC#6 resolved box)
- **Description:** CC#6 says the builder "matches against `ProofLedger.FaultSiteLinks` by `ProofObligation.Site` identity." But `TypedExpression` is a record — C# record equality is structural, not referential. The spec doesn't say whether `Site` matching uses reference equality or structural equality. For records, structural equality means two independently-created `TypedBinaryOp` nodes with identical fields would match — which could cause false positives.
- **Why it matters:** If the builder or proof engine relies on reference identity, the implementer must ensure the same `TypedExpression` object instance is used in both the `ProofObligation` and the builder's walk. If structural equality is fine, no action needed.
- **Suggested resolution:** Clarify that `ProofObligation.Site` uses the same object reference passed through from `SemanticIndex` — no copies. Reference identity is preserved because the proof engine reads the same `TypedExpression` nodes the builder later visits.

---

### [DOC-ONLY] Gaps

---

**PE-G17: Spec shows `OperatorKind` in code samples but source uses different names**
- **Severity:** DOC-ONLY
- **Location:** `proof-engine.md` §7, line 454
- **Description:** The Strategy 1 pseudocode uses `OperatorKind.NotEquals`, `OperatorKind.GreaterThan`, etc. Need to verify these match the actual `OperatorKind` enum values in source. Minor naming discrepancies between spec pseudocode and source enum members would cause confusion during implementation.
- **Suggested resolution:** Cross-reference with `src/Precept/Language/OperatorKind.cs` and update spec pseudocode to use actual enum member names.

---

**PE-G18: Spec says "accumulate diagnostics without abandoning" but doesn't cite the principle by name**
- **Severity:** DOC-ONLY
- **Location:** `proof-engine.md` §9, line 945
- **Description:** The spec references Precept's error accumulation principle but doesn't cite the canonical name or doc location. Other pipeline stage docs reference `diagnostic-system.md §Error Accumulation`.
- **Suggested resolution:** Add cross-reference to `diagnostic-system.md`.

---

## Cross-Stage Seam Issues

### GraphAnalyzer → ProofEngine

1. **ReachabilityFact emission is per-state.** The GraphAnalyzer emits one `ReachabilityFact` per state (line 186 of GraphAnalyzer.cs). The spec's consumption table (line 907) says "suppress proof obligations on transitions originating from unreachable states." This is correct — the proof engine can look up `ReachabilityFact.IsReachable` for a transition's `FromState`. **No gap.**

2. **EventCoverageFact consumption is vague.** The spec says the proof engine "uses coverage gaps to reason about guard completeness: in states where an event is handled, are the guards sufficient?" (line 909). This is hand-wavy. What does "guard completeness" mean for the proof engine? Is the proof engine checking that guards on transition rows cover all possible field value ranges? That's a significantly harder problem than the spec's other strategies suggest. **Overlaps with PE-G1 (underspecified algorithm).** The EventCoverageFact consumption should be clarified — likely it's just a structural record, not an active proof check.

3. **DominancePathFact:** The spec says "if `DominatedTerminals` is empty, records a structural violation in the proof ledger." But the GraphAnalyzer already emits `RequiredStateDoesNotDominateTerminal` (111) for this case. The proof engine recording it again is redundant. **Clarify whether the proof engine adds to the structural record or merely records the fact for downstream consumption without additional diagnostics.**

### ProofEngine → Runtime (via Precept Builder)

4. **No `precept-builder.md` exists.** The spec references it in three CC#6 resolution boxes (lines 218, 236, 250). The downstream contract is hypothetical. **Covered by PE-G11.**

5. **`FaultSiteAnnotation` is described in the spec but does not exist in source.** The source has `FaultSiteDescriptor` in `Runtime/Descriptors.cs` with a different shape: `FaultSiteDescriptor(FaultCode, DiagnosticCode PreventedBy, int SourceLine)`. The spec's `FaultSiteAnnotation` has `(FaultCode Code, DiagnosticCode PreventedBy, SourceSpan Site)` — `SourceSpan` vs `int SourceLine`. These may be different types (builder-time vs runtime), but the relationship is unspecified.

---

## Catalog Compliance Issues

1. **PE-G2 is the primary catalog violation.** `FieldModifierMeta.ProofDischarges` is described as catalog metadata but doesn't exist. The spec correctly identifies this as catalog-driven (Strategy 2 reads `meta.ProofDischarges` from the catalog), but the catalog hasn't been updated. **BLOCKING.**

2. **The four strategies themselves are generic machinery, not catalog-driven.** The strategies are predicate functions that pattern-match on requirement types and expression types. This is correct — strategies are algorithms, not per-member metadata. The obligation _source_ is catalog-driven (ProofRequirements on catalog entries), the _discharge_ is algorithmic. **No violation.**

3. **`ProofRequirementMeta` catalog is correctly implemented.** The `ProofRequirements.cs` catalog with `GetMeta()` switch and `All` enumeration matches the catalog pattern. **No issue.**

---

## Diagnostic Catalog Status

| Code | Name | Stage | Severity | Registered in `DiagnosticCode.cs` | Registered in `Diagnostics.cs` | `PreventsFault` | Status |
|------|------|-------|----------|------------------------------------|-------------------------------|-----------------|--------|
| 82 | `UnsatisfiableGuard` | Proof | Warning | ✅ | ✅ | — | Complete |
| 83 | `DivisionByZero` | Proof | Error | ✅ | ✅ | `FaultCode.DivisionByZero` | Complete |
| 84 | `SqrtOfNegative` | Proof | Error | ✅ | ✅ | `FaultCode.SqrtOfNegative` | Complete |

**Three proof-stage diagnostics exist and are fully registered.** `RelatedCodes` cross-link all three. `FixHint` values are present.

**Missing diagnostic gap:** Collection non-empty proof failures have no proof-stage diagnostic code (PE-G9). Depending on resolution of PE-G9, additional codes may be needed.

**Missing diagnostic gap:** `DimensionProofRequirement`, `ModifierRequirement`, and `QualifierCompatibilityProofRequirement` failures have no diagnostic codes. Depending on resolution of PE-G1, additional codes may be needed.

---

## Spec Readiness Verdict

**NOT READY** — three BLOCKING gaps prevent implementation from starting.

### Blockers (must resolve before any implementation work):

1. **PE-G1:** Three of five `ProofRequirementKind` values have no discharge strategy. The implementer cannot write discharge logic for `Dimension`, `Modifier`, or `QualifierCompatibility` obligations without spec guidance.
2. **PE-G2:** `FieldModifierMeta.ProofDischarges` does not exist in source. Strategy 2 cannot be implemented as specified.
3. **PE-G3:** Output type `ProofLedger` and ~10 supporting record types don't exist. Shape declarations must be created before coding begins.

### Conditions (must resolve before implementation is complete, but won't block starting if blockers are cleared):

4. **PE-G4:** `AllTypedExpressions` doesn't exist — Pass 1 walk targets must be enumerated.
5. **PE-G5:** `ConstraintIdentity` shapes must match source, not spec.
6. **PE-G6:** `FindEnclosingTransitionRow` must be specified.
7. **PE-G7:** `ResolveSubject` must be specified.
8. **PE-G8:** Initial-state satisfiability needs a concrete algorithm.
9. **PE-G9:** Collection non-empty proof ownership must be decided (type checker vs proof engine).
10. **PE-G10:** Guard decomposition rules must be specified.

---

## Recommended Pre-Implementation Actions

1. **Resolve PE-G1** — For each of `Dimension`, `Modifier`, `QualifierCompatibility`: state which strategy handles it, or state that the type checker resolves it before proof. This is a design decision, not an implementation detail.

2. **Implement PE-G2** — Add `ProofDischarge` record and `ProofDischarges` property to `FieldModifierMeta`. Populate entries for `positive`, `nonnegative`, `nonzero`, `notempty`, `min(N)`, `max(N)`. This is a catalog prerequisite.

3. **Update spec for PE-G3** — Add a "Slice 0: Shape declarations" section listing all new types to create. The implementer should create these in a build-green commit before any logic.

4. **Resolve PE-G9** — Decide collection-empty ownership. This affects diagnostic code allocation and obligation walk scope.

5. **Update spec for PE-G5** — Align `ConstraintIdentity` shapes with source implementation.

6. **Add `FindEnclosingTransitionRow` spec (PE-G6)** and **`ResolveSubject` spec (PE-G7)** — These are the two most complex helper functions. Providing pseudocode prevents design divergence during implementation.

7. **Specify initial-state satisfiability algorithm (PE-G8)** — Define the default value model and which constraint scopes are checked.

8. **Add compound guard decomposition rules (PE-G10)** — Specify `and`/`or`/`not` handling.

9. **Add stateless precept handling section (PE-G15)** — Small but prevents a class of missed-obligation bugs.
