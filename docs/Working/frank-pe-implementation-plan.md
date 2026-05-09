# ProofEngine Implementation Plan

**Author:** Frank (Lead/Architect)
**Date:** 2026-05-08
**Status:** Ready for Shane review
**Spec:** `docs/compiler/proof-engine.md` (canonical, all 18 gaps incorporated)
**Executor:** George

---

## Baseline

| Metric | Value |
|---|---|
| Build | ✅ Green (0 errors, 2 pre-existing LS warnings) |
| Tests | ✅ 2924 pass / 2 pre-existing TokensTests failures / 194 pre-existing LS test failures |
| ProofEngine.cs | Stub — returns empty ProofLedger |
| ProofLedger.cs | Full G3 shape landed (ProofObligation, FaultSiteLink, etc.) — missing ObligationContext on ProofObligation |

---

## ⚠️ Critical Finding: Diagnostic Code Conflict

The spec references new diagnostic codes 96–99 for proof-stage diagnostics. **These ordinals are already allocated:**

| Spec Code | Spec Name | Actual Occupant |
|---|---|---|
| 96 | `UnprovedModifierRequirement` | `CaseInsensitiveValueInCaseSensitiveContains` |
| 97 | `UnprovedDimensionRequirement` | `CaseInsensitiveFieldRequiresTildeStartsWith` |
| 98 | `UnprovedQualifierCompatibility` | `CaseInsensitiveFieldRequiresTildeEndsWith` |
| 99 | `UnsatisfiableInitialState` | `KeyPresenceSafety` |

**Resolution:** Allocate the four new proof-stage codes at 112–115 (next available after `RequiredStateDoesNotDominateTerminal = 111`). Update `docs/compiler/proof-engine.md` §9 Diagnostic Message Formatting table to reflect the correct ordinals. The names stay the same; only the numeric codes change.

| New Code | Name |
|---|---|
| 112 | `UnprovedModifierRequirement` |
| 113 | `UnprovedDimensionRequirement` |
| 114 | `UnprovedQualifierCompatibility` |
| 115 | `UnsatisfiableInitialState` |

---

## Phase 1: Prework — Structural Definitions & Catalog Metadata

All prework creates shapes, types, catalog rows, and metadata WITHOUT implementing engine logic. After prework completes: build green, all existing tests pass, no behavioral changes.

---

### Prework Slice P1 — ProofSatisfaction DU and Supporting Types

**Goal:** Create the ProofSatisfaction discriminated union (5 subtypes + 3 supporting DUs) per PE-G2 locked design.

**Create:**

- `src/Precept/Language/ProofRequirement.cs` — append after line 159 (end of file):

```csharp
// ProofSatisfaction DU — positive carrier fact that can satisfy a ProofRequirement
public abstract record ProofSatisfaction(ProofRequirementKind RequirementKind)
{
    public sealed record Numeric(
        SatisfactionProjection Projection,
        OperatorKind Comparison,
        NumericBoundSource Bound)
        : ProofSatisfaction(ProofRequirementKind.Numeric);

    public sealed record Presence()
        : ProofSatisfaction(ProofRequirementKind.Presence);

    public sealed record Dimension(DimensionSource Source)
        : ProofSatisfaction(ProofRequirementKind.Dimension);

    public sealed record Modifier(ModifierKind RequiredModifier)
        : ProofSatisfaction(ProofRequirementKind.Modifier);

    public sealed record QualifierCompatibility(QualifierAxis Axis)
        : ProofSatisfaction(ProofRequirementKind.QualifierCompatibility);
}

public abstract record SatisfactionProjection
{
    public sealed record SelfValue() : SatisfactionProjection;
    public sealed record Accessor(string Name) : SatisfactionProjection;
}

public abstract record NumericBoundSource
{
    public sealed record Constant(decimal Value) : NumericBoundSource;
    public sealed record DeclarationValue() : NumericBoundSource;
}

public abstract record DimensionSource
{
    public sealed record Constant(PeriodDimension Value) : DimensionSource;
    public sealed record DeclaredTemporalDimension() : DimensionSource;
}
```

**Tests (in `test/Precept.Tests/ProofRequirementTests.cs`):**

- `ProofSatisfaction_Numeric_HoldsProjectionComparisonBound` — `[Fact]` verifying Numeric subtype construction and property access
- `ProofSatisfaction_Presence_HasPresenceRequirementKind` — `[Fact]` verifying Presence subtype has correct RequirementKind
- `ProofSatisfaction_Dimension_HoldsSource` — `[Fact]` verifying Dimension subtype
- `ProofSatisfaction_Modifier_HoldsRequiredModifier` — `[Fact]`
- `ProofSatisfaction_QualifierCompatibility_HoldsAxis` — `[Fact]`
- `SatisfactionProjection_SelfValue_IsDistinctFromAccessor` — `[Fact]`
- `SatisfactionProjection_Accessor_HoldsName` — `[Fact]`
- `NumericBoundSource_Constant_HoldsValue` — `[Fact]`
- `NumericBoundSource_DeclarationValue_IsDistinct` — `[Fact]`
- `DimensionSource_Constant_HoldsValue` — `[Fact]`
- `DimensionSource_DeclaredTemporalDimension_IsDistinct` — `[Fact]`

**Regression anchors:**
- `ParamSubject_HoldsParameterReference` — adjacent code, must not break
- `SelfSubject_DefaultAccessorIsNull` — adjacent code
- All tests in `ProofRequirementCatalogTests` — catalog integrity

**Build gate:** ✅ Compiles. All existing tests pass.

**Files:** `src/Precept/Language/ProofRequirement.cs`, `test/Precept.Tests/ProofRequirementTests.cs`

---

### Prework Slice P2 — DeclaredPresenceMeta Carrier Type

**Goal:** Create the DeclaredPresenceMeta DU (2 subtypes) per PE-G2.

**Create:**

- `src/Precept/Language/DeclaredPresence.cs`:

```csharp
namespace Precept.Language;

public abstract record DeclaredPresenceMeta(
    string Description,
    ProofSatisfaction[]? ProofSatisfactions = null)
{
    public ProofSatisfaction[] ProofSatisfactions { get; } = ProofSatisfactions ?? [];

    public sealed record Guaranteed()
        : DeclaredPresenceMeta(
            "Value is structurally present on every instance",
            [new ProofSatisfaction.Presence()]);

    public sealed record Optional()
        : DeclaredPresenceMeta(
            "Value may be absent");
}
```

**Tests (in `test/Precept.Tests/ProofRequirementTests.cs`):**

- `DeclaredPresenceMeta_Guaranteed_CarriesPresenceSatisfaction` — `[Fact]` verifying Guaranteed.ProofSatisfactions contains exactly one Presence entry
- `DeclaredPresenceMeta_Optional_HasNoSatisfactions` — `[Fact]` verifying Optional.ProofSatisfactions is empty

**Build gate:** ✅ Compiles.

**Files:** `src/Precept/Language/DeclaredPresence.cs`, `test/Precept.Tests/ProofRequirementTests.cs`

---

### Prework Slice P3 — DeclaredQualifierMeta Carrier Type

**Goal:** Create the DeclaredQualifierMeta DU (8 subtypes + QualifierOrigin enum) per PE-G2.

**Create:**

- `src/Precept/Language/DeclaredQualifierMeta.cs`:

```csharp
namespace Precept.Language;

public enum QualifierOrigin
{
    Explicit = 1,
    Derived  = 2,
    Baseline = 3,
}

public abstract record DeclaredQualifierMeta(
    QualifierAxis Axis,
    QualifierOrigin Origin,
    TokenKind? Preposition,
    ProofSatisfaction[]? ProofSatisfactions = null)
{
    public ProofSatisfaction[] ProofSatisfactions { get; } = ProofSatisfactions ?? [];

    public sealed record Currency(string CurrencyCode,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.Currency, Origin, Preposition, ProofSatisfactions);

    public sealed record Unit(string UnitCode, string DimensionName,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.Unit, Origin, Preposition, ProofSatisfactions);

    public sealed record Dimension(string DimensionName,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.Of,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.Dimension, Origin, Preposition, ProofSatisfactions);

    public sealed record FromCurrency(string CurrencyCode,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.FromCurrency, Origin, Preposition, ProofSatisfactions);

    public sealed record ToCurrency(string CurrencyCode,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.To,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.ToCurrency, Origin, Preposition, ProofSatisfactions);

    public sealed record Timezone(string TimezoneId,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.Timezone, Origin, Preposition, ProofSatisfactions);

    public sealed record TemporalDimension(PeriodDimension Value,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.Of,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.TemporalDimension, Origin, Preposition, ProofSatisfactions);

    public sealed record TemporalUnit(string UnitName, PeriodDimension DerivedDimension,
        QualifierOrigin Origin = QualifierOrigin.Explicit,
        TokenKind? Preposition = TokenKind.In,
        ProofSatisfaction[]? ProofSatisfactions = null)
        : DeclaredQualifierMeta(QualifierAxis.TemporalUnit, Origin, Preposition, ProofSatisfactions);
}
```

**Tests (in `test/Precept.Tests/ProofRequirementTests.cs`):**

- `DeclaredQualifierMeta_Currency_HasCorrectAxis` — `[Fact]`
- `DeclaredQualifierMeta_TemporalDimension_Any_HasBaselineOrigin` — `[Fact]` verifying `TemporalDimension(PeriodDimension.Any, Origin: Baseline, Preposition: null)` construction
- `DeclaredQualifierMeta_TemporalUnit_CarriesDerivedDimension` — `[Fact]`
- `DeclaredQualifierMeta_AllSubtypes_HaveCorrectAxis` — `[Theory]` with 8 rows, one per subtype

**Build gate:** ✅ Compiles.

**Files:** `src/Precept/Language/DeclaredQualifierMeta.cs`, `test/Precept.Tests/ProofRequirementTests.cs`

---

### Prework Slice P4 — FieldModifierMeta.ProofSatisfactions Property

**Goal:** Add `ProofSatisfaction[]? ProofSatisfactions` property to `FieldModifierMeta` and populate 10 modifier catalog entries.

**Modify:**

- `src/Precept/Language/Modifier.cs` (~line 116–133): Add `ProofSatisfactions` parameter to `FieldModifierMeta` record, placed after `Subsumes`. Add backing property initializer.

```csharp
public sealed record FieldModifierMeta(
    ModifierKind Kind,
    TokenMeta Token,
    string Description,
    ModifierCategory Category,
    TypeTarget[] ApplicableTo,
    bool HasValue = false,
    ModifierKind[] Subsumes = default!,
    ProofSatisfaction[]? ProofSatisfactions = null,  // ← NEW
    string? HoverDescription = null,
    string? UsageExample = null,
    string? SnippetTemplate = null,
    bool DesugarsToRule = false,
    ModifierKind[]? MutuallyExclusiveWith = null)
    : ModifierMeta(Kind, Token, Description, Category, DesugarsToRule, MutuallyExclusiveWith)
{
    public ModifierKind[] Subsumes { get; init; } = Subsumes ?? [];
    public ProofSatisfaction[] ProofSatisfactions { get; init; } = ProofSatisfactions ?? [];
}
```

- `src/Precept/Language/Modifiers.cs` (~lines 61–145): Populate `ProofSatisfactions` on 10 modifier entries:

| Modifier | ProofSatisfactions Value |
|---|---|
| `Positive` | `[new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.GreaterThan, new NumericBoundSource.Constant(0m))]` |
| `Nonnegative` | `[new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.GreaterThanOrEqual, new NumericBoundSource.Constant(0m))]` |
| `Nonzero` | `[new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.NotEquals, new NumericBoundSource.Constant(0m))]` |
| `Notempty` | `[new ProofSatisfaction.Numeric(new SatisfactionProjection.Accessor("length"), OperatorKind.GreaterThan, new NumericBoundSource.Constant(0m)), new ProofSatisfaction.Numeric(new SatisfactionProjection.Accessor("count"), OperatorKind.GreaterThan, new NumericBoundSource.Constant(0m))]` |
| `Min` | `[new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.GreaterThanOrEqual, new NumericBoundSource.DeclarationValue())]` |
| `Max` | `[new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.LessThanOrEqual, new NumericBoundSource.DeclarationValue())]` |
| `Minlength` | `[new ProofSatisfaction.Numeric(new SatisfactionProjection.Accessor("length"), OperatorKind.GreaterThanOrEqual, new NumericBoundSource.DeclarationValue())]` |
| `Maxlength` | `[new ProofSatisfaction.Numeric(new SatisfactionProjection.Accessor("length"), OperatorKind.LessThanOrEqual, new NumericBoundSource.DeclarationValue())]` |
| `Mincount` | `[new ProofSatisfaction.Numeric(new SatisfactionProjection.Accessor("count"), OperatorKind.GreaterThanOrEqual, new NumericBoundSource.DeclarationValue())]` |
| `Maxcount` | `[new ProofSatisfaction.Numeric(new SatisfactionProjection.Accessor("count"), OperatorKind.LessThanOrEqual, new NumericBoundSource.DeclarationValue())]` |

**Tests (in `test/Precept.Tests/ModifiersTests.cs`):**

- `Positive_HasProofSatisfaction_Numeric_GreaterThan_Zero` — `[Fact]`
- `Nonnegative_HasProofSatisfaction_Numeric_GreaterThanOrEqual_Zero` — `[Fact]`
- `Nonzero_HasProofSatisfaction_Numeric_NotEquals_Zero` — `[Fact]`
- `Notempty_HasTwoProofSatisfactions_LengthAndCount` — `[Fact]`
- `Min_HasProofSatisfaction_Numeric_DeclarationValue` — `[Fact]`
- `Max_HasProofSatisfaction_Numeric_DeclarationValue` — `[Fact]`
- `ModifiersWithoutProofSatisfactions_HaveEmptyArray` — `[Theory]` with rows for `Optional`, `Ordered`, `Default`, `Writable`, `Maxplaces` verifying empty `ProofSatisfactions`
- `AllFieldModifiers_HaveValidProofSatisfactions` — `[Fact]` exhaustive check that every `FieldModifierMeta` either has populated entries or empty (no null)

**Regression anchors:**
- All existing `ModifiersTests` tests — modifier catalog integrity
- `GetMeta_ReturnsForEveryModifierKind` (if exists) — exhaustiveness

**Build gate:** ✅ Compiles. All existing modifier tests pass.

**Files:** `src/Precept/Language/Modifier.cs`, `src/Precept/Language/Modifiers.cs`, `test/Precept.Tests/ModifiersTests.cs`

---

### Prework Slice P5 — TypedField and TypedArg Carrier Properties

**Goal:** Add `Presence` and `DeclaredQualifiers` properties to `TypedField` and `TypedArg`. All call sites initially pass default values (no behavioral change).

**Modify:**

- `src/Precept/Pipeline/SemanticIndex.cs` — `TypedField` record (~line 239–253): Add two new properties:

```csharp
public sealed record TypedField(
    string Name,
    TypeKind ResolvedType,
    TypeKind? ElementType,
    TypeKind? KeyType,
    ImmutableArray<ModifierKind> Modifiers,
    ImmutableArray<ModifierKind> ImpliedModifiers,
    TypedExpression? DefaultExpression,
    TypedExpression? ComputedExpression,
    QualifierBinding? Qualifier,
    bool IsComputed,
    bool IsOptional,
    bool IsWritable,
    DeclaredPresenceMeta Presence,                           // ← NEW
    ImmutableArray<DeclaredQualifierMeta> DeclaredQualifiers, // ← NEW
    ParsedConstruct Syntax
);
```

- `src/Precept/Pipeline/SemanticIndex.cs` — `TypedArg` record (~line 276–285): Add two new properties:

```csharp
public sealed record TypedArg(
    string Name,
    string EventName,
    TypeKind ResolvedType,
    TypeKind? ElementType,
    ImmutableArray<ModifierKind> Modifiers,
    TypedExpression? DefaultExpression,
    bool IsOptional,
    DeclaredPresenceMeta Presence,                           // ← NEW
    ImmutableArray<DeclaredQualifierMeta> DeclaredQualifiers, // ← NEW
    SourceSpan Span
);
```

- `src/Precept/Pipeline/TypeChecker.cs` — All call sites constructing `TypedField` and `TypedArg` must pass default values:
  - `TypedField`: `Presence: field.IsOptional ? new DeclaredPresenceMeta.Optional() : new DeclaredPresenceMeta.Guaranteed()`, `DeclaredQualifiers: ImmutableArray<DeclaredQualifierMeta>.Empty`
  - `TypedArg`: `Presence: arg.IsOptional ? new DeclaredPresenceMeta.Optional() : new DeclaredPresenceMeta.Guaranteed()`, `DeclaredQualifiers: ImmutableArray<DeclaredQualifierMeta>.Empty`

- Any other files constructing `TypedField` or `TypedArg` (search for `new TypedField(` and `new TypedArg(` — likely also in test helpers). All must be updated.

**Important:** This slice will touch many files because `TypedField` and `TypedArg` are positional records. Every construction site must be updated. George must search exhaustively for all construction sites.

**Tests:**

- All existing TypeChecker tests must pass unchanged — the new properties are defaulted to correct values based on existing `IsOptional` flag.
- `TypedField_Presence_GuaranteedWhenNotOptional` — `[Fact]` in `test/Precept.Tests/TypeChecker/` verifying a non-optional field gets `Guaranteed` presence
- `TypedField_Presence_OptionalWhenOptional` — `[Fact]`
- `TypedField_DeclaredQualifiers_EmptyByDefault` — `[Fact]`
- `TypedArg_Presence_MatchesIsOptional` — `[Fact]`

**Regression anchors:**
- ALL existing TypeChecker tests (~hundreds) — this is a record shape change
- ALL existing GraphAnalyzer tests — they construct SemanticIndex which contains TypedFields
- ALL existing MCP tests — they consume Compilation which contains SemanticIndex

**Build gate:** ✅ Compiles. All existing tests pass (new properties are defaulted).

**Files:** `src/Precept/Pipeline/SemanticIndex.cs`, `src/Precept/Pipeline/TypeChecker.cs`, every file constructing `TypedField` or `TypedArg`, test helpers

---

### Prework Slice P6 — ObligationContext DU on ProofObligation

**Goal:** Add the `ObligationContext` DU and the `Context` property to `ProofObligation`.

**Modify:**

- `src/Precept/Pipeline/ProofLedger.cs` (~line 14–20): Add `ObligationContext Context` parameter to `ProofObligation`:

```csharp
public sealed record ProofObligation(
    ProofRequirement Requirement,
    TypedExpression Site,
    ObligationContext Context,           // ← NEW
    ProofDisposition Disposition,
    ProofStrategy? Strategy,
    DiagnosticCode? EmittedDiagnostic
);
```

- `src/Precept/Pipeline/ProofLedger.cs` — Add after `ProofObligation`:

```csharp
public abstract record ObligationContext;
public sealed record TransitionRowContext(TypedTransitionRow Row) : ObligationContext;
public sealed record ConstraintContext(ConstraintIdentity Constraint) : ObligationContext;
public sealed record StateHookContext(TypedStateHook Hook) : ObligationContext;
public sealed record EventHandlerContext(TypedEventHandler Handler) : ObligationContext;
public sealed record FieldExpressionContext(TypedField Field) : ObligationContext;
```

**Tests (in a new `test/Precept.Tests/ProofLedgerTests.cs`):**

- `ObligationContext_TransitionRowContext_HoldsRow` — `[Fact]`
- `ObligationContext_ConstraintContext_HoldsIdentity` — `[Fact]`
- `ObligationContext_AllFiveSubtypes_AreDistinct` — `[Fact]` pattern-match test
- `ProofObligation_IncludesContext` — `[Fact]`

**Regression anchors:**
- `ProofEngine.Prove` stub must still compile — update the stub if it constructs any ProofObligation (currently returns empty, so no change needed)

**Build gate:** ✅ Compiles.

**Files:** `src/Precept/Pipeline/ProofLedger.cs`, `test/Precept.Tests/ProofLedgerTests.cs`

---

### Prework Slice P7 — Diagnostic Codes 112–115

**Goal:** Add four new `DiagnosticCode` members and their `Diagnostics.GetMeta` entries.

**Modify:**

- `src/Precept/Language/DiagnosticCode.cs` — Add after `RequiredStateDoesNotDominateTerminal = 111`:

```csharp
// ── Proof (new codes) ───────────────────────────────────
/// <summary>A field referenced in a proof obligation does not declare the required modifier.</summary>
UnprovedModifierRequirement            = 112,
/// <summary>An operand requires a specific temporal dimension but the resolved dimension does not match.</summary>
UnprovedDimensionRequirement           = 113,
/// <summary>Two operands require matching qualifiers on an axis but their qualifier values differ or are unresolved.</summary>
UnprovedQualifierCompatibility         = 114,
/// <summary>An initial state's constraints cannot be satisfied with default field values.</summary>
UnsatisfiableInitialState              = 115,
```

- `src/Precept/Language/Diagnostics.cs` — Add four entries to the `GetMeta` switch before the `_ => throw`:

```csharp
DiagnosticCode.UnprovedModifierRequirement => new(
    nameof(DiagnosticCode.UnprovedModifierRequirement),
    DiagnosticStage.Proof, Severity.Error,
    "Field '{0}' must have modifier '{1}' but it is not declared{2}",
    DiagnosticCategory.Proof,
    FixHint: "Add the required modifier to the field declaration"),

DiagnosticCode.UnprovedDimensionRequirement => new(
    nameof(DiagnosticCode.UnprovedDimensionRequirement),
    DiagnosticStage.Proof, Severity.Error,
    "Operand '{0}' requires {1} dimension but has {2}{3}",
    DiagnosticCategory.Proof,
    FixHint: "Qualify the field with the correct temporal dimension, e.g., 'period of date'"),

DiagnosticCode.UnprovedQualifierCompatibility => new(
    nameof(DiagnosticCode.UnprovedQualifierCompatibility),
    DiagnosticStage.Proof, Severity.Error,
    "Operands '{0}' and '{1}' have incompatible {2} qualifiers{3}",
    DiagnosticCategory.Proof,
    FixHint: "Ensure both operands have matching qualifier values"),

DiagnosticCode.UnsatisfiableInitialState => new(
    nameof(DiagnosticCode.UnsatisfiableInitialState),
    DiagnosticStage.Proof, Severity.Error,
    "Initial state '{0}' cannot be satisfied: constraint '{1}' fails with default values",
    DiagnosticCategory.Proof,
    FixHint: "Adjust field defaults or constraint conditions so the initial state is satisfiable"),
```

**Tests (in `test/Precept.Tests/DiagnosticsTests.cs`):**

- Existing `GetMeta_ReturnsForEveryDiagnosticCode` (if exists) — must pass with new codes
- `UnprovedModifierRequirement_HasProofStage` — `[Fact]`
- `UnprovedDimensionRequirement_HasProofStage` — `[Fact]`
- `UnprovedQualifierCompatibility_HasProofStage` — `[Fact]`
- `UnsatisfiableInitialState_HasProofStage` — `[Fact]`

**Regression anchors:**
- `DiagnosticsTests.GetMeta_ReturnsForEveryDiagnosticCode` (exhaustiveness test)
- All drift tests in `SlotOrderingDriftTests.cs`

**Build gate:** ✅ Compiles.

**Files:** `src/Precept/Language/DiagnosticCode.cs`, `src/Precept/Language/Diagnostics.cs`, `test/Precept.Tests/DiagnosticsTests.cs`

---

### Prework Slice P8 — Spec Correction (Diagnostic Codes)

**Goal:** Update the canonical spec to use the correct diagnostic code ordinals.

**Modify:**

- `docs/compiler/proof-engine.md` §9 Diagnostic Message Formatting table: Replace `(96)` → `(112)`, `(97)` → `(113)`, `(98)` → `(114)`, `(99)` → `(115)` in the table and all references throughout the document.
- `docs/compiler/diagnostic-system.md`: If it references these codes, update accordingly.
- `docs/Working/frank-proof-engine-gap-analysis.md`: Update any references to codes 96–99.

**Build gate:** N/A (documentation only).

**Files:** `docs/compiler/proof-engine.md`, `docs/compiler/diagnostic-system.md`, `docs/Working/frank-proof-engine-gap-analysis.md`

---

### Phase 1 Summary

| Slice | Files Created | Files Modified | Tests Added |
|---|---|---|---|
| P1 — ProofSatisfaction DU | — | `ProofRequirement.cs`, `ProofRequirementTests.cs` | 11 |
| P2 — DeclaredPresenceMeta | `DeclaredPresence.cs` | `ProofRequirementTests.cs` | 2 |
| P3 — DeclaredQualifierMeta | `DeclaredQualifierMeta.cs` | `ProofRequirementTests.cs` | 4 |
| P4 — FieldModifierMeta.ProofSatisfactions | — | `Modifier.cs`, `Modifiers.cs`, `ModifiersTests.cs` | 8 |
| P5 — TypedField/TypedArg carriers | — | `SemanticIndex.cs`, `TypeChecker.cs`, test helpers, construction sites | 4 |
| P6 — ObligationContext DU | — | `ProofLedger.cs`; creates `ProofLedgerTests.cs` | 4 |
| P7 — Diagnostic codes 112–115 | — | `DiagnosticCode.cs`, `Diagnostics.cs`, `DiagnosticsTests.cs` | 4 |
| P8 — Spec correction | — | `proof-engine.md`, `diagnostic-system.md`, gap analysis | 0 |
| **Total** | **2 new** | **~15 modified** | **~37** |

**Phase 1 exit criteria:** `dotnet build` green, `dotnet test` passes at baseline (2924+ in Precept.Tests), all new shape tests green, no behavioral changes.

---

## Phase 2: Full Engine Implementation

---

### Slice 1 — Pass 1: Obligation Collection (Walk + Context Tagging)

**Goal:** Implement the `CollectObligations` private method that walks all SemanticIndex members and instantiates `ProofObligation` records with `ObligationContext`.

**Create (in `src/Precept/Pipeline/ProofEngine.cs`):**

- `CollectObligations(SemanticIndex semantics) → List<ProofObligation>` (~80–120 lines):
  - Walk `TransitionRows[].Actions[]` → recurse into `TypedInputAction.InputExpression` → for each node with non-empty `ProofRequirements`, create `ProofObligation` with `TransitionRowContext(row)`
  - Walk `EventHandlers[].Actions[]` → `EventHandlerContext(handler)`
  - Walk `StateHooks[].Actions[]` → `StateHookContext(hook)`
  - Walk `Rules[].Condition` → `ConstraintContext(RuleIdentity(i))`
  - Walk `Ensures[].Condition` → `ConstraintContext(EnsureIdentity(...))`
  - Walk `Fields[].ComputedExpression` → `FieldExpressionContext(field)`
- `WalkExpression(TypedExpression expr, ObligationContext ctx, List<ProofObligation> obligations)` (~30 lines): Recursive depth-first visitor that pattern-matches on `TypedBinaryOp`, `TypedFunctionCall`, `TypedMemberAccess`, `TypedUnaryOp`, `TypedConditional`, `TypedQuantifier` to recurse, and creates obligations for nodes with `ProofRequirements`.
- `WalkActions(ImmutableArray<TypedAction> actions, ObligationContext ctx, List<ProofObligation> obligations)` (~15 lines): Walks each action, creates obligations from `TypedAction.ProofRequirements`, recurses into `TypedInputAction.InputExpression` and `SecondaryExpression`.

**Modify:**

- `ProofEngine.Prove()` — Replace the empty-return stub: call `CollectObligations`, then return the collected obligations with `Disposition = ProofDisposition.Unresolved` for all. No discharge yet — that's Slice 2+. FaultSiteLinks, ConstraintInfluence, InitialStateResults remain empty for now.

**Tests (in a new `test/Precept.Tests/ProofEngineTests.cs`):**

- `CollectObligations_TransitionRowWithDivision_CreatesNumericObligation` — `[Fact]` compile a precept with `set X = Y / Z`, verify ProofLedger.Obligations contains a NumericProofRequirement obligation with TransitionRowContext
- `CollectObligations_EventHandlerWithAction_CreatesObligation` — `[Fact]` stateless precept with event handler action
- `CollectObligations_RuleCondition_CreatesObligation` — `[Fact]` rule with function call having proof requirements
- `CollectObligations_ComputedField_CreatesObligation` — `[Fact]` computed field with proof-bearing expression
- `CollectObligations_EmptySemanticIndex_ProducesNoObligations` — `[Fact]`
- `CollectObligations_NoProofRequirements_ProducesNoObligations` — `[Fact]` a transition with `set X = 1` (literal assignment, no proof requirements)
- `ObligationContext_IsCorrectPerWalkTarget` — `[Theory]` with rows for each walk target verifying correct context subtype

**Regression anchors:**
- All existing compilation tests — `ProofEngine.Prove` is called by `Compiler.Compile`, so any change to its return type or behavior must not break existing compilation workflows

**Build gate:** ✅ Compiles. All obligations are Unresolved (correct — no strategies yet).

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `test/Precept.Tests/ProofEngineTests.cs`

---

### Slice 2 — Subject Resolution Utilities

**Goal:** Implement `ResolveSubject`, `GetFieldName`, and parameter resolution helpers.

**Create (in `src/Precept/Pipeline/ProofEngine.cs`):**

- `ResolveSubject(ProofSubject subject, TypedExpression site) → TypedExpression?` (~25 lines): Pattern-match on ParamSubject/SelfSubject, then dispatch to site-specific resolution per spec §7.
- `ResolveParamInBinaryOp(ParameterMeta param, TypedBinaryOp bin) → TypedExpression?` (~8 lines): Reference-equality match against `Operations.GetMeta(bin.ResolvedOp).Left/Right`.
- `ResolveParamInFunctionCall(ParameterMeta param, TypedFunctionCall call) → TypedExpression?` (~12 lines): Iterate overload parameters, reference-equality match.
- `ResolveParamInMemberAccess(ParameterMeta param, TypedMemberAccess access) → TypedExpression?` (~5 lines)
- `ResolveSelfInAction(SelfSubject self, TypedAction action) → TypedExpression?` (~5 lines)
- `GetFieldName(ProofSubject subject, TypedExpression site) → string?` (~8 lines): Resolve, then extract field name from `TypedFieldRef` or `TypedMemberAccess { Object: TypedFieldRef }`.

**Tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `ResolveSubject_ParamSubject_BinaryOp_ResolvesToLeftOperand` — `[Fact]`
- `ResolveSubject_ParamSubject_BinaryOp_ResolvesToRightOperand` — `[Fact]`
- `ResolveSubject_ParamSubject_FunctionCall_ResolvesToArgument` — `[Fact]`
- `ResolveSubject_SelfSubject_MemberAccess_ResolvesToObject` — `[Fact]`
- `ResolveSubject_SelfSubject_Action_ResolvesToField` — `[Fact]`
- `ResolveSubject_NullWhenNotFound` — `[Fact]`
- `GetFieldName_TypedFieldRef_ReturnsFieldName` — `[Fact]`
- `GetFieldName_MemberAccessOnFieldRef_ReturnsFieldName` — `[Fact]`
- `GetFieldName_NonFieldRef_ReturnsNull` — `[Fact]`

**Build gate:** ✅ Compiles. Subject resolution is tested in isolation — no behavioral change to Prove().

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `test/Precept.Tests/ProofEngineTests.cs`

---

### Slice 3 — Strategy 1: Literal Proof

**Goal:** Implement `TryLiteralProof` and wire it into the discharge loop.

**Create (in `src/Precept/Pipeline/ProofEngine.cs`):**

- `TryLiteralProof(ProofObligation obligation) → bool` (~25 lines): Gate on NumericProofRequirement, resolve subject, check for TypedLiteral, extract numeric value, compare against threshold per spec §7.
- `TryDischarge(ProofObligation obligation, SemanticIndex semantics) → (ProofDisposition, ProofStrategy?)` (~10 lines): Strategy dispatch chain — currently only calls TryLiteralProof. Will grow in subsequent slices.

**Modify:**

- `ProofEngine.Prove()` — Add Pass 2 discharge loop: iterate obligations, call `TryDischarge`, update disposition. Unresolved obligations still produce no diagnostics yet (deferred to Slice 9).

**Tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `Strategy1_LiteralDivisor_DischargesNumericObligation` — `[Fact]` `set X = Y / 2` → obligation proved by Strategy 1
- `Strategy1_LiteralZeroDivisor_RemainsUnresolved` — `[Fact]` `set X = Y / 0` → obligation unresolved
- `Strategy1_LiteralSqrtNonNegative_Discharged` — `[Fact]` `sqrt(4)` → proved
- `Strategy1_LiteralSqrtNegative_Unresolved` — `[Fact]` `sqrt(-1)` → unresolved
- `Strategy1_NonNumericRequirement_SkipsStrategy` — `[Fact]` PresenceProofRequirement → Strategy 1 returns false
- `Strategy1_NonLiteralSubject_SkipsStrategy` — `[Fact]` `set X = Y / Z` where Z is a field → Strategy 1 returns false

**Build gate:** ✅ Compiles. Literal-provable obligations now show `Proved` disposition.

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `test/Precept.Tests/ProofEngineTests.cs`

---

### Slice 4 — Strategy 2: Declaration Attribute Proof

**Goal:** Implement `TryDeclarationAttributeProof` — four arms: Dimension, Modifier, Numeric (from modifier satisfactions), Presence (from DeclaredPresenceMeta).

**Create (in `src/Precept/Pipeline/ProofEngine.cs`):**

- `TryDeclarationAttributeProof(ProofObligation obligation, SemanticIndex semantics) → bool` (~50 lines): Per spec §7 pseudocode:
  1. Dimension arm: Resolve subject, look up `ResolvePeriodDimension`, check `PeriodDimension.Any` permissive rule
  2. Modifier arm: `GetFieldName`, look up field, `field.Modifiers.Contains(required)`
  3. Numeric/Presence arm: Walk field's effective modifiers (`Modifiers` + `ImpliedModifiers`), call `Modifiers.GetMeta()`, iterate `ProofSatisfactions`, call `SatisfactionCovers`
  4. Presence fallback: Check `field.Presence is DeclaredPresenceMeta.Guaranteed`
- `ResolvePeriodDimension(TypedExpression? subject, SemanticIndex semantics) → PeriodDimension?` (~15 lines): For `TypedFieldRef`, look up `field.DeclaredQualifiers` for `TemporalDimension` entry.
- `SatisfactionCovers(ProofSatisfaction satisfaction, ProofRequirement requirement) → bool` (~20 lines): Subsumption check — `positive (>, 0)` covers `(!=, 0)` and `(>=, 0)`.

**Modify:**

- `TryDischarge` — Add `TryDeclarationAttributeProof` as second strategy in chain.

**Tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `Strategy2_NonzeroDivisor_DischargedByNonzeroModifier` — `[Fact]` `field D as nonzero integer; set X = Y / D` → proved
- `Strategy2_PositiveDivisor_DischargesNotEqualsZero` — `[Fact]` positive subsumes nonzero
- `Strategy2_NonnegativeDivisor_DoesNotDischargeNotEqualsZero` — `[Fact]` nonnegative does NOT subsume nonzero (nonneg allows zero)
- `Strategy2_NotemptyCollection_DischargesCountGreaterThanZero` — `[Fact]`
- `Strategy2_ModifierRequirement_OrderedField_Discharged` — `[Fact]` field with `ordered` modifier satisfies ModifierRequirement
- `Strategy2_ModifierRequirement_UnorderedField_Unresolved` — `[Fact]`
- `Strategy2_DimensionRequirement_ExplicitDatePeriod_Discharged` — `[Fact]` `period of 'date'` with Date dimension requirement
- `Strategy2_DimensionRequirement_UnqualifiedPeriod_DischargedByAny` — `[Fact]` unqualified period → `PeriodDimension.Any` satisfies
- `Strategy2_Presence_GuaranteedField_Discharged` — `[Fact]` non-optional field → Presence obligation proved
- `Strategy2_Presence_OptionalField_Unresolved` — `[Fact]` optional field → not proved by Strategy 2
- `Strategy2_ImpliedModifiers_InheritedNotempty_Discharges` — `[Fact]` type-implied modifiers (e.g., timezone implies notempty)

**Regression anchors:**
- All Slice 3 literal proof tests — Strategy 1 still fires first

**Build gate:** ✅ Compiles. Declaration-attribute-provable obligations now show `Proved`.

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `test/Precept.Tests/ProofEngineTests.cs`

---

### Slice 5 — Strategy 3: Guard-in-Path Proof

**Goal:** Implement `TryGuardInPathProof`, `ExtractGuardConstraints`, and `GuardSubsumes`.

**Create (in `src/Precept/Pipeline/ProofEngine.cs`):**

- `TryGuardInPathProof(ProofObligation obligation, SemanticIndex semantics) → bool` (~20 lines): Read `ObligationContext`, extract guard, decompose, check subsumption per spec §7.
- `ExtractGuardConstraints(TypedExpression guard) → ImmutableArray<GuardConstraint>` (~50 lines): Pattern-match decomposition per PE-G10 table — AND decomposes, OR does not, recognize field-vs-literal comparisons, `count()` patterns, `is set` patterns.
- `GuardSubsumes(GuardConstraint guard, NumericProofRequirement requirement, TypedExpression site) → bool` (~20 lines): Subsumption table per spec §7.
- `InvertOp(OperatorKind op) → OperatorKind` (~10 lines): Operator inversion per spec table.
- `NegateOp(OperatorKind op) → OperatorKind` (~10 lines): Negation inversion per spec table.
- Internal record: `GuardConstraint(string Field, OperatorKind Comparison, decimal? Value, bool IsPresenceCheck)` (~1 line).

**Modify:**

- `TryDischarge` — Add `TryGuardInPathProof` as third strategy.

**Tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `Strategy3_GuardNotEqualsZero_DischargesDivisionByZero` — `[Fact]` `when D != 0; set X = Y / D`
- `Strategy3_GuardGreaterThanZero_DischargesNotEqualsAndGreaterOrEqual` — `[Fact]` `when D > 0` subsumes `!= 0` and `>= 0`
- `Strategy3_GuardLessThanZero_DischargesNotEqualsZero` — `[Fact]` `when D < 0` subsumes `!= 0`
- `Strategy3_CountGuard_DischargesCollectionNonEmpty` — `[Fact]` `when count(Items) > 0; first(Items)`
- `Strategy3_IsSetGuard_DischargesPresenceRequirement` — `[Fact]` `when F is set; ... F.accessor`
- `Strategy3_OrGuard_DoesNotDischarge` — `[Fact]` `when X > 0 or Y > 0` — neither disjunct is guaranteed
- `Strategy3_AndGuard_DecomposesConjuncts` — `[Fact]` `when X > 0 and Y > 0`
- `Strategy3_NegatedComparison_Inverts` — `[Fact]` `when not (X == 0)` → `X != 0`
- `Strategy3_LiteralOnLeft_InvertsOp` — `[Fact]` `when 0 < X` → `X > 0`
- `Strategy3_NoGuard_ReturnsFalse` — `[Fact]`
- `Strategy3_EventHandlerContext_ReturnsFalse` — `[Fact]` event handlers have no guards

**Build gate:** ✅ Compiles. Guard-provable obligations now show `Proved`.

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `test/Precept.Tests/ProofEngineTests.cs`

---

### Slice 6 — Strategy 4: Flow Narrowing

**Goal:** Implement `TryFlowNarrowingProof`, `ExtractFieldToFieldConstraints`, and `GuardRelationImpliesObligation`.

**Create (in `src/Precept/Pipeline/ProofEngine.cs`):**

- `TryFlowNarrowingProof(ProofObligation obligation, SemanticIndex semantics) → bool` (~20 lines): Per spec §7.
- `ExtractFieldToFieldConstraints(TypedExpression guard) → ImmutableArray<FieldToFieldConstraint>` (~20 lines): Like `ExtractGuardConstraints` but for field-vs-field comparisons (both sides `TypedFieldRef`).
- `GuardRelationImpliesObligation(FieldToFieldConstraint guard, TypedBinaryOp expr, NumericProofRequirement requirement) → bool` (~30 lines): Full 12-entry triple table per PE-G14.
- `IsSubtractionOp(OperationKind op) → bool` (~5 lines): Check if the operation is subtraction.
- `GetFieldName(TypedExpression expr) → string?` overload (~3 lines): For direct field name extraction from expression node.
- Internal record: `FieldToFieldConstraint(string LeftField, OperatorKind Comparison, string RightField)` (~1 line).

**Modify:**

- `TryDischarge` — Add `TryFlowNarrowingProof` as fourth strategy.

**Tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `Strategy4_AGreaterThanB_SubtractionResultGreaterThanZero` — `[Fact]` `when A > B; set X = A - B` where obligation requires `result > 0`
- `Strategy4_AGreaterThanB_SubtractionResultNotEqualsZero` — `[Fact]` `> B` implies `A - B != 0`
- `Strategy4_AGreaterOrEqualB_SubtractionResultGreaterOrEqualZero` — `[Fact]`
- `Strategy4_AGreaterOrEqualB_DoesNotDischargeNotEquals` — `[Fact]` `A >= B` allows `A == B`, so `A - B` can be 0
- `Strategy4_ALessThanB_ReversedSubtractionResultGreaterThanZero` — `[Fact]` `A < B; B - A`
- `Strategy4_DivisionNotCovered` — `[Fact]` `A > B; B / A` — Strategy 4 does not cover division
- `Strategy4_FieldVsLiteralGuard_NotStrategy4` — `[Fact]` `when A > 0` is Strategy 3, not 4

**Build gate:** ✅ Compiles.

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `test/Precept.Tests/ProofEngineTests.cs`

---

### Slice 7 — Strategy 5: Qualifier Compatibility Proof

**Goal:** Implement `TryQualifierCompatibilityProof`.

**Create (in `src/Precept/Pipeline/ProofEngine.cs`):**

- `TryQualifierCompatibilityProof(ProofObligation obligation, SemanticIndex semantics) → bool` (~20 lines): Gate on `QualifierCompatibilityProofRequirement`, resolve both subjects, look up `DeclaredQualifiers` on both fields for the requested axis, compare values.
- `ResolveQualifierOnAxis(ProofSubject subject, QualifierAxis axis, TypedExpression site, SemanticIndex semantics) → DeclaredQualifierMeta?` (~15 lines): Look up field, find matching `DeclaredQualifiers` entry on the axis.

**Modify:**

- `TryDischarge` — Add `TryQualifierCompatibilityProof` as fifth (final) strategy.

**Tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `Strategy5_MatchingCurrencyQualifiers_Discharged` — `[Fact]` two `money in 'USD'` fields added → proved (requires qualifier resolution in TC — test deferred if TC doesn't populate `DeclaredQualifiers` yet)
- `Strategy5_MismatchedQualifiers_Unresolved` — `[Fact]`
- `Strategy5_UnqualifiedFields_Unresolved` — `[Fact]` both fields unqualified → can't prove compatibility
- `Strategy5_TemporalDimensionAny_DoesNotSatisfyCompatibility` — `[Fact]` two `PeriodDimension.Any` fields → NOT compatible
- `Strategy5_NonQualifierRequirement_SkipsStrategy` — `[Fact]` NumericProofRequirement → returns false

**Note:** Until the TypeChecker populates `DeclaredQualifiers` on `TypedField`, Strategy 5 will conservatively return `false` for all obligations (correct per spec §7 Strategy 5: "Until qualifier resolution ships, all QualifierCompatibilityProofRequirement obligations produce Unresolved"). Tests should construct SemanticIndex manually with populated qualifiers for unit testing.

**Build gate:** ✅ Compiles.

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `test/Precept.Tests/ProofEngineTests.cs`

---

### Slice 8 — Error-Tainted Obligation Suppression (PE-G13)

**Goal:** Implement `ContainsErrorExpression` and suppress diagnostics for error-tainted obligations.

**Create (in `src/Precept/Pipeline/ProofEngine.cs`):**

- `ContainsErrorExpression(TypedExpression expr) → bool` (~15 lines): Recursive check per spec §9.

**Modify:**

- `ProofEngine.Prove()` Pass 2 loop: Before calling `TryDischarge`, check `ContainsErrorExpression(obligation.Site)`. If true, mark as `Unresolved` with no diagnostic, no fault link — skip to next obligation.

**Tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `ErrorTainted_BinaryOpWithErrorOperand_SuppressesDiagnostic` — `[Fact]`
- `ErrorTainted_NestedErrorExpression_SuppressesDiagnostic` — `[Fact]`
- `ErrorTainted_FunctionCallWithErrorArg_SuppressesDiagnostic` — `[Fact]`
- `NonErrorTainted_StillEmitsDiagnostic` — `[Fact]` (once diagnostic emission is wired in Slice 9)

**Build gate:** ✅ Compiles. Error-tainted obligations produce no diagnostics.

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `test/Precept.Tests/ProofEngineTests.cs`

---

### Slice 9 — Diagnostic Emission and FaultSiteLink Production

**Goal:** Wire diagnostic creation for unresolved obligations and produce FaultSiteLinks.

**Create (in `src/Precept/Pipeline/ProofEngine.cs`):**

- `CreateDiagnostic(ProofObligation obligation) → Diagnostic` (~30 lines): Dispatch on `obligation.Requirement` type to select `DiagnosticCode` and format template parameters per spec §9 formatting table:
  - `NumericProofRequirement` with Comparison `!=` and Threshold 0 → `DivisionByZero` (83)
  - `NumericProofRequirement` with `>=` and Threshold 0 → `SqrtOfNegative` (84)
  - `ModifierRequirement` → `UnprovedModifierRequirement` (112)
  - `DimensionProofRequirement` → `UnprovedDimensionRequirement` (113)
  - `QualifierCompatibilityProofRequirement` → `UnprovedQualifierCompatibility` (114)
  - Other `NumericProofRequirement` → `DivisionByZero` (83) as fallback for generic numeric failures
- `FormatContextDescription(ObligationContext context) → string` (~15 lines): `"event '{EventName}' in state '{FromState}'"` for TransitionRowContext, etc.
- `CreateFaultSiteLink(ProofObligation obligation) → FaultSiteLink` (~10 lines): Map `DiagnosticCode` → `FaultCode` using `[StaticallyPreventable]` attribute lookup on FaultCode enum.

**Modify:**

- `ProofEngine.Prove()` Pass 2: After `TryDischarge`, if `Unresolved` and not error-tainted, call `CreateDiagnostic` and `CreateFaultSiteLink`, add to lists.

**Tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `Diagnostic_DivisionByZero_EmittedForUnresolvedNumericNotEquals` — `[Fact]`
- `Diagnostic_SqrtOfNegative_EmittedForUnresolvedNumericGreaterOrEqual` — `[Fact]`
- `Diagnostic_UnprovedModifier_EmittedForUnresolvedModifierRequirement` — `[Fact]`
- `Diagnostic_UnprovedDimension_EmittedForUnresolvedDimensionRequirement` — `[Fact]`
- `Diagnostic_UnprovedQualifier_EmittedForUnresolvedQualifierCompatibility` — `[Fact]`
- `Diagnostic_ContextDescription_TransitionRow_FormatsCorrectly` — `[Fact]`
- `Diagnostic_ContextDescription_EventHandler_FormatsCorrectly` — `[Fact]`
- `FaultSiteLink_CreatedForUnresolvedObligation` — `[Fact]`
- `FaultSiteLink_NotCreatedForProvedObligation` — `[Fact]`
- `FaultSiteLink_NotCreatedForErrorTaintedObligation` — `[Fact]`
- `ProvedObligation_EmitsNoDiagnostic` — `[Fact]` `set X = Y / 2` — proved by literal → no diagnostic

**Build gate:** ✅ Compiles. Unresolved obligations now emit diagnostics and FaultSiteLinks.

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `test/Precept.Tests/ProofEngineTests.cs`

---

### Slice 10 — Constraint Influence Analysis

**Goal:** Implement `ProjectConstraintInfluence` — reads `SemanticIndex.ConstraintRefs`, enriches with event-qualified arg references.

**Create (in `src/Precept/Pipeline/ProofEngine.cs`):**

- `ProjectConstraintInfluence(SemanticIndex semantics) → ImmutableArray<ConstraintInfluenceEntry>` (~25 lines): Per spec §7 — iterate `semantics.ConstraintRefs`, resolve bare arg names to `EventArgReference` using `semantics.EventsByName`.
- `ResolveArgToEvent(string argName, ConstraintIdentity constraint, SemanticIndex semantics) → EventArgReference` (~15 lines): Find which event owns the arg.

**Modify:**

- `ProofEngine.Prove()` — Replace empty `ConstraintInfluence` with call to `ProjectConstraintInfluence`.

**Tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `ConstraintInfluence_RuleWithFieldRef_RecordsReferencedField` — `[Fact]`
- `ConstraintInfluence_EnsureWithArgRef_ResolvesToEventArgReference` — `[Fact]`
- `ConstraintInfluence_EmptyConstraintRefs_ProducesEmptyInfluence` — `[Fact]`
- `ConstraintInfluence_MultipleConstraints_AllProjected` — `[Fact]`

**Build gate:** ✅ Compiles. ConstraintInfluence populated.

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `test/Precept.Tests/ProofEngineTests.cs`

---

### Slice 11 — Initial-State Satisfiability

**Goal:** Implement `CheckInitialStateSatisfiability` with bounded constant folding.

**Create (in `src/Precept/Pipeline/ProofEngine.cs`):**

- `CheckInitialStateSatisfiability(SemanticIndex semantics) → ImmutableArray<InitialStateSatisfiabilityResult>` (~40 lines): Per spec §7:
  1. Find initial state
  2. Collect `StateResident` ensures anchored to initial state
  3. Build default value environment from fields
  4. Constant-fold each ensure condition
  5. Report violations for conditions that fold to `false`
- `GetTypeDefault(TypeKind type) → object?` (~20 lines): Returns 0m for numeric, "" for string, false for boolean, empty for collections, null for complex types (marking unfoldable).
- `ConstantFold(TypedExpression expr, Dictionary<string, object?> defaults, HashSet<string> unfoldable) → FoldResult` (~60 lines): Bounded constant folding per spec — TypedLiteral returns value, TypedFieldRef substitutes, TypedBinaryOp evaluates known operands, TypedConditional branches, function calls return Unknown.
- `EvaluateBinaryOp(OperatorKind op, object? left, object? right) → object?` (~30 lines): Simple comparison/arithmetic evaluator for known values.
- `FormatViolationReason(TypedEnsure ensure, Dictionary<string, object?> defaults) → string` (~10 lines)
- Internal enum: `FoldResult { True, False, Unknown }` (~1 line) — or use nullable bool.

**Modify:**

- `ProofEngine.Prove()` — Replace empty `InitialStateResults` with call to `CheckInitialStateSatisfiability`. Emit `UnsatisfiableInitialState` (115) diagnostic for violations.

**Tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `InitialStateSatisfiability_UnsatisfiableEnsure_ReportsViolation` — `[Fact]` `field X as integer default 0; state Draft initial; in Draft ensure X > 5` → violation
- `InitialStateSatisfiability_SatisfiableEnsure_NoViolation` — `[Fact]` `field X as integer default 10; in Draft ensure X > 5` → no violation
- `InitialStateSatisfiability_NonLiteralDefault_UnfoldableConservative` — `[Fact]` computed default → Unknown → no violation (conservative)
- `InitialStateSatisfiability_GuardedEnsure_Skipped` — `[Fact]` guarded ensure → vacuously satisfiable
- `InitialStateSatisfiability_NoInitialState_ReturnsEmpty` — `[Fact]` stateless precept
- `InitialStateSatisfiability_OptionalFieldDefault_Null` — `[Fact]`
- `InitialStateSatisfiability_BooleanDefaultFalse_FoldsCorrectly` — `[Fact]`
- `InitialStateSatisfiability_DiagnosticEmitted_Code115` — `[Fact]`

**Build gate:** ✅ Compiles. Initial-state violations now emit diagnostics.

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `test/Precept.Tests/ProofEngineTests.cs`

---

### Slice 12 — ProofForwardingFact Consumption

**Goal:** Implement `IncorporateForwardingFacts` — consume graph analyzer facts to suppress obligations on unreachable/dead-end paths.

**Create (in `src/Precept/Pipeline/ProofEngine.cs`):**

- `IncorporateForwardingFacts(ImmutableArray<ProofForwardingFact> facts, List<ProofObligation> obligations, SemanticIndex semantics)` (~30 lines): Per spec §7:
  - `ReachabilityFact`: Suppress obligations on transitions from unreachable states
  - `DeadEndStateFact`: Suppress obligations on transitions FROM dead-end states TO other dead-end states
  - `DominancePathFact`, `EventCoverageFact`, `TerminalCompletenessFact`: Record for structural completeness, no obligation suppression

**Modify:**

- `ProofEngine.Prove()` — `IncorporateForwardingFacts` is already called in Slice 1 skeleton; now implement the body.

**Tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `ForwardingFacts_UnreachableState_SuppressesObligations` — `[Fact]`
- `ForwardingFacts_DeadEndToDeadEnd_SuppressesObligations` — `[Fact]`
- `ForwardingFacts_DeadEndIncoming_RetainsObligations` — `[Fact]` transitions INTO dead-end states keep obligations
- `ForwardingFacts_ReachableState_RetainsObligations` — `[Fact]`
- `ForwardingFacts_EmptyFacts_NoEffect` — `[Fact]`

**Build gate:** ✅ Compiles.

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `test/Precept.Tests/ProofEngineTests.cs`

---

### Slice 13 — Stateless Precept Handling (PE-G15) + Final Integration

**Goal:** Verify stateless precept behavior, wire final ProofLedger output, and confirm end-to-end integration.

**Verify (no new code expected — existing walk handles this):**

- Stateless precepts walk `EventHandlers[].Actions[]` (no `TransitionRows`, no `StateHooks`)
- Strategies 3 and 4 correctly return `false` for `EventHandlerContext` (no guard)
- `CheckInitialStateSatisfiability` returns empty for `States.IsEmpty`

**Tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `StatelessPrecept_EventHandlerActions_CreateObligations` — `[Fact]`
- `StatelessPrecept_Strategy1And2_Apply` — `[Fact]` literal and modifier proof work
- `StatelessPrecept_Strategy3And4_DoNotApply` — `[Fact]` no guards → strategies skip
- `StatelessPrecept_NoInitialStateSatisfiability` — `[Fact]`

**Integration tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `Integration_DivisionByZeroGuarded_NoProofDiagnostic` — `[Fact]` `when D != 0; set X = Y / D` → no diagnostic
- `Integration_DivisionByZeroUnguarded_EmitsDiagnostic` — `[Fact]` `set X = Y / D` → DivisionByZero diagnostic
- `Integration_ProofLedger_HasAllComponents` — `[Fact]` verify Obligations, FaultSiteLinks, ConstraintInfluence, InitialStateResults, Diagnostics are all populated
- `Integration_AllProved_NoFaultSiteLinks` — `[Fact]` all obligations proved → empty FaultSiteLinks
- `Integration_MixedProvedAndUnresolved` — `[Fact]` some proved, some not → correct counts

**Regression anchors:**
- ALL existing compilation/diagnostic tests — full pipeline runs through ProofEngine now
- `Compiler.Compile` must still work for all sample files

**Build gate:** ✅ Full `dotnet test` green.

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `test/Precept.Tests/ProofEngineTests.cs`

---

## Ordering Constraints

```
Phase 1 (Prework):
P1 → P2 → P3 → P4 → P5 → P6 → P7 → P8
     (P2+P3 depend on P1's ProofSatisfaction type)
     (P4 depends on P1's ProofSatisfaction type)
     (P5 depends on P2's DeclaredPresenceMeta and P3's DeclaredQualifierMeta)
     (P6 and P7 are independent of each other, both after P5)

Phase 2 (Engine):
Slice 1 → Slice 2 → Slice 3 → Slice 4 → Slice 5 → Slice 6 → Slice 7
    (linear dependency: each slice adds to the discharge chain)
Slice 8 (error taint): after Slice 3 (needs discharge loop)
Slice 9 (diagnostics): after Slice 7 (all strategies must be in place)
Slice 10 (constraint influence): independent after Slice 1
Slice 11 (satisfiability): independent after Slice 1
Slice 12 (forwarding facts): after Slice 1
Slice 13 (integration): after ALL other slices
```

**Parallelizable:** Slices 10, 11, and 12 can be implemented in parallel with Slices 3–7 (they don't depend on strategy implementations).

---

## File Inventory

| File | Phase | Slices |
|---|---|---|
| `src/Precept/Language/ProofRequirement.cs` | P1 | P1 |
| `src/Precept/Language/DeclaredPresence.cs` | P2 | P2 (create) |
| `src/Precept/Language/DeclaredQualifierMeta.cs` | P3 | P3 (create) |
| `src/Precept/Language/Modifier.cs` | P4 | P4 |
| `src/Precept/Language/Modifiers.cs` | P4 | P4 |
| `src/Precept/Language/DiagnosticCode.cs` | P7 | P7 |
| `src/Precept/Language/Diagnostics.cs` | P7 | P7 |
| `src/Precept/Pipeline/SemanticIndex.cs` | P5 | P5 |
| `src/Precept/Pipeline/ProofLedger.cs` | P6 | P6 |
| `src/Precept/Pipeline/ProofEngine.cs` | 2 | S1–S13 |
| `src/Precept/Pipeline/TypeChecker.cs` | P5 | P5 (construction sites) |
| `test/Precept.Tests/ProofRequirementTests.cs` | P1–P3 | P1, P2, P3 |
| `test/Precept.Tests/ModifiersTests.cs` | P4 | P4 |
| `test/Precept.Tests/DiagnosticsTests.cs` | P7 | P7 |
| `test/Precept.Tests/ProofLedgerTests.cs` | P6 | P6 (create) |
| `test/Precept.Tests/ProofEngineTests.cs` | 2 | S1–S13 (create) |
| `docs/compiler/proof-engine.md` | P8 | P8 |
| `docs/compiler/diagnostic-system.md` | P8 | P8 |
| `docs/Working/frank-proof-engine-gap-analysis.md` | P8 | P8 |

---

## Tooling / MCP Sync Assessment

| Surface | Impact | Action |
|---|---|---|
| **MCP `precept_compile`** | ProofLedger gains populated Obligations, FaultSiteLinks, ConstraintInfluence, InitialStateResults | Check MCP DTOs in `tools/Precept.Mcp/Tools/CompileTool.cs` — if it serializes `ProofLedger`, verify new fields appear in output. Likely no changes needed if the DTO serializes the full `ProofLedger` record. |
| **MCP `precept_language`** | New diagnostic codes 112–115 appear in `Diagnostics.All` | No code change — `Diagnostics.All` iterates all enum values automatically. Verify the MCP output includes the new codes. |
| **Language Server** | Proof diagnostics now emitted by ProofEngine | No LS changes — diagnostics flow through `Compilation.Diagnostics` which the LS already consumes. |
| **Syntax highlighting / grammar** | No new keywords, operators, or syntax forms | No changes. |
| **Semantic tokens** | No new token types | No changes. |
| **Completions / hover** | No new completable items | No changes. |

---

## Test Count Estimate

| Phase | Slice | Tests |
|---|---|---|
| P1 | ProofSatisfaction DU | 11 |
| P2 | DeclaredPresenceMeta | 2 |
| P3 | DeclaredQualifierMeta | 4 |
| P4 | FieldModifierMeta.ProofSatisfactions | 8 |
| P5 | TypedField/TypedArg carriers | 4 |
| P6 | ObligationContext DU | 4 |
| P7 | Diagnostic codes | 4 |
| S1 | Obligation collection | 7 |
| S2 | Subject resolution | 9 |
| S3 | Strategy 1 (Literal) | 6 |
| S4 | Strategy 2 (Declaration Attribute) | 11 |
| S5 | Strategy 3 (Guard-in-Path) | 11 |
| S6 | Strategy 4 (Flow Narrowing) | 7 |
| S7 | Strategy 5 (Qualifier Compatibility) | 5 |
| S8 | Error-tainted suppression | 4 |
| S9 | Diagnostic emission + FaultSiteLinks | 11 |
| S10 | Constraint influence | 4 |
| S11 | Initial-state satisfiability | 8 |
| S12 | ProofForwardingFact consumption | 5 |
| S13 | Stateless + integration | 9 |
| **Total** | | **~134** |

---

## Documentation Sync

After all slices complete:

1. `docs/compiler/proof-engine.md` §1 Status table: change `Implementation state` from `Stub — not yet implemented` to `Implemented`
2. `docs/compiler/proof-engine.md` §13: Remove items 1 and 2 from Implementation Status (no longer stubs)
3. `README.md`: If it mentions ProofEngine as planned/future, update to reflect implemented status
4. `docs/language/catalog-system.md`: Update the carrier types section if needed to reference new DeclaredPresenceMeta and DeclaredQualifierMeta
