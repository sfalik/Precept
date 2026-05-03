# Catalog Updates

This document tracks catalog metadata additions required by design documents but not yet implemented.

---

## FieldModifierMeta.ProofDischarges

**Source:** `docs/compiler/proof-engine.md` §7 Strategy 2: Modifier Proof

**Purpose:** Enable the proof engine's modifier-proof strategy to discharge proof obligations based on field modifiers without hardcoding modifier-to-obligation mappings.

**Current state:** `FieldModifierMeta` does not declare which proof obligations each modifier discharges.

### Proposed Addition

Add `ProofDischarges` property to `FieldModifierMeta`:

```csharp
// src/Precept/Language/Modifier.cs

/// <summary>Describes a proof obligation that a modifier discharges.</summary>
public sealed record ProofDischarge(
    ProofRequirementKind RequirementKind,
    OperatorKind? Comparison,    // for Numeric requirements (null for non-Numeric)
    decimal? Threshold           // for Numeric requirements (null for non-Numeric)
);

/// <summary>Field constraint modifiers (14 members: optional, ordered, nonnegative, …, maxplaces).</summary>
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
    /// <summary>Modifiers this one makes redundant. Empty for most.</summary>
    public ModifierKind[] Subsumes { get; init; } = Subsumes ?? [];
    
    /// <summary>Proof obligations this modifier discharges. Empty for most.</summary>
    public ProofDischarge[] ProofDischarges { get; init; } = ProofDischarges ?? [];
}
```

### Catalog Entries

Update `src/Precept/Language/Modifiers.cs` with `ProofDischarges` for relevant modifiers:

```csharp
ModifierKind.Positive => new FieldModifierMeta(
    kind, Tokens.GetMeta(TokenKind.Positive),
    "Value > 0",
    ModifierCategory.Structural, NumericTypes,
    Subsumes: [ModifierKind.Nonnegative, ModifierKind.Nonzero],
    ProofDischarges: [
        new(ProofRequirementKind.Numeric, OperatorKind.GreaterThan, 0m),
        new(ProofRequirementKind.Numeric, OperatorKind.NotEquals, 0m),
    ],
    HoverDescription: "The field's value must be strictly greater than zero. Implies nonnegative and nonzero.",
    MutuallyExclusiveWith: [ModifierKind.Nonnegative]),

ModifierKind.Nonnegative => new FieldModifierMeta(
    kind, Tokens.GetMeta(TokenKind.Nonnegative),
    "Value ≥ 0",
    ModifierCategory.Structural, NumericTypes,
    ProofDischarges: [
        new(ProofRequirementKind.Numeric, OperatorKind.GreaterThanOrEqual, 0m),
    ],
    HoverDescription: "The field's value must be zero or greater. Enforced on every assignment.",
    MutuallyExclusiveWith: [ModifierKind.Positive]),

ModifierKind.Nonzero => new FieldModifierMeta(
    kind, Tokens.GetMeta(TokenKind.Nonzero),
    "Value ≠ 0",
    ModifierCategory.Structural, NumericTypes,
    ProofDischarges: [
        new(ProofRequirementKind.Numeric, OperatorKind.NotEquals, 0m),
    ],
    HoverDescription: "The field's value must not be zero. Allows negative values."),

ModifierKind.Notempty => new FieldModifierMeta(
    kind, Tokens.GetMeta(TokenKind.Notempty),
    "String or collection is non-empty",
    ModifierCategory.Structural, StringAndCollectionTypes,
    ProofDischarges: [
        new(ProofRequirementKind.Numeric, OperatorKind.GreaterThan, 0m),  // count > 0
    ],
    HoverDescription: "The field must not be empty..."),

ModifierKind.Min => new FieldModifierMeta(
    kind, Tokens.GetMeta(TokenKind.Min),
    "Minimum value",
    ModifierCategory.Structural, NumericTypes, HasValue: true,
    // ProofDischarges populated at instantiation time based on HasValue
    HoverDescription: "The field's value must be at least this minimum..."),

ModifierKind.Max => new FieldModifierMeta(
    kind, Tokens.GetMeta(TokenKind.Max),
    "Maximum value",
    ModifierCategory.Structural, NumericTypes, HasValue: true,
    // ProofDischarges populated at instantiation time based on HasValue
    HoverDescription: "The field's value must be at most this maximum..."),
```

### Proof Engine Consumption

The proof engine's modifier-proof strategy consumes `ProofDischarges`:

```csharp
bool TryModifierProof(ProofObligation obligation, TypedField field)
{
    if (obligation.Requirement is not NumericProofRequirement numeric)
        return false;
    
    foreach (var modifier in field.Modifiers)
    {
        var meta = Modifiers.GetMeta(modifier);
        if (meta is not FieldModifierMeta fieldMeta) continue;
        
        foreach (var discharge in fieldMeta.ProofDischarges)
        {
            if (discharge.RequirementKind != numeric.Kind) continue;
            if (DischargeSubsumes(discharge, numeric))
                return true;
        }
    }
    
    return false;
}

bool DischargeSubsumes(ProofDischarge discharge, NumericProofRequirement requirement)
{
    // > 0 discharge subsumes != 0 and >= 0 requirements
    if (discharge.Comparison == OperatorKind.GreaterThan && discharge.Threshold == 0m)
    {
        if (requirement.Comparison == OperatorKind.NotEquals && requirement.Threshold == 0m)
            return true;
        if (requirement.Comparison == OperatorKind.GreaterThanOrEqual && requirement.Threshold <= 0m)
            return true;
    }
    
    // Exact match
    return discharge.Comparison == requirement.Comparison 
        && discharge.Threshold == requirement.Threshold;
}
```

### Implementation Checklist

- [ ] Add `ProofDischarge` record to `src/Precept/Language/Modifier.cs`
- [ ] Add `ProofDischarges` property to `FieldModifierMeta`
- [ ] Update `Modifiers.GetMeta()` entries for `positive`, `nonnegative`, `nonzero`, `notempty`
- [ ] Consider `min`/`max` value-dependent discharge logic (may require type checker support)
- [ ] Update MCP vocabulary output if `ProofDischarges` should be exposed to AI tooling

---

## ConstructMeta.ModelContribution (Candidate)

**Source:** `docs/runtime/precept-builder.md` §13 Open Questions

**Purpose:** Make the Precept Builder's assembly loop fully generic by declaring what each construct contributes to the `Precept` model.

**Current state:** The builder "knows" that `FieldDeclaration` adds a field and `TransitionRow` adds a transition. This domain knowledge is implicit in builder code, not declared in catalog metadata.

**Status:** Candidate — value is marginal for ~12 constructs. Documented as open question pending owner ruling.

### Proposed Addition

Add `ModelContribution` property to `ConstructMeta`:

```csharp
// src/Precept/Language/Construct.cs

/// <summary>What this construct contributes to the Precept model.</summary>
public enum ModelContribution
{
    None,              // No model contribution (e.g., comments)
    DeclaresField,     // Adds a FieldDescriptor
    DeclaresState,     // Adds a StateDescriptor
    DeclaresEvent,     // Adds an EventDescriptor
    AddsTransition,    // Adds an ExecutionRow to TransitionDispatchIndex
    AddsConstraint,    // Adds a ConstraintDescriptor to ConstraintPlanIndex
    AddsAccessMode,    // Modifies FieldDescriptor access mode for a state
    AddsStateHook,     // Adds entry/exit actions to a state
}

public sealed record ConstructMeta(
    ConstructKind Kind,
    TokenMeta LeadingToken,
    string Description,
    ImmutableArray<SlotMeta> Slots,
    ModelContribution Contribution,  // ← NEW
    // ...existing properties...
);
```

### Builder Consumption

With `ModelContribution`, the builder's assembly loop becomes fully generic:

```csharp
foreach (var construct in semanticIndex.Constructs)
{
    var contribution = construct.Meta.ModelContribution;
    
    switch (contribution)
    {
        case ModelContribution.DeclaresField:
            builder.AddField(ExtractField(construct));
            break;
        case ModelContribution.DeclaresState:
            builder.AddState(ExtractState(construct));
            break;
        case ModelContribution.DeclaresEvent:
            builder.AddEvent(ExtractEvent(construct));
            break;
        case ModelContribution.AddsTransition:
            builder.AddTransition(ExtractTransition(construct));
            break;
        case ModelContribution.AddsConstraint:
            builder.AddConstraint(ExtractConstraint(construct));
            break;
        // ... remaining contribution kinds
    }
}
```

### Implementation Checklist

- [ ] Owner decision: Is the marginal value worth the catalog addition for ~12 constructs?
- [ ] If approved: Add `ModelContribution` enum to `src/Precept/Language/Construct.cs`
- [ ] If approved: Add `Contribution` property to `ConstructMeta`
- [ ] If approved: Update `Constructs.GetMeta()` entries with contribution values
- [ ] If approved: Refactor builder to dispatch on `ModelContribution` instead of `ConstructKind`

---

## FieldDescriptor.AccessModes

**Source:** `docs/runtime/evaluator.md` §7.5 Access Mode Enforcement

**Purpose:** Enable the evaluator to check field access modes without re-deriving from modifiers at runtime.

**Current state:** `FieldDescriptor` does not carry pre-resolved access modes per state. The evaluator would need to re-derive access from field modifiers and state-level `modify`/`omit` overrides.

### Proposed Addition

Add `AccessModes` property to `FieldDescriptor`:

```csharp
// src/Precept/Runtime/Descriptors.cs

/// <summary>
/// Pre-resolved per-state access mode map. Built by Precept Builder from:
/// - Field-level baseline: default is Writable unless 'readonly' modifier
/// - State-level override: 'in <State> modify field' or 'in <State> omit field'
/// </summary>
public enum AccessMode
{
    Writable = 1,   // Field can be patched via Update
    ReadOnly = 2,   // Field is visible but cannot be patched
    Omit     = 3,   // Field is hidden and cannot be patched
}

public sealed record FieldDescriptor(
    string Name,
    TypeKind Type,
    int SlotIndex,
    IReadOnlyList<ModifierKind> Modifiers,
    string? DefaultExpression,
    bool IsComputed,
    int SourceLine,
    IReadOnlyDictionary<StateDescriptor?, AccessMode> AccessModes  // ← NEW
);
```

### Evaluator Consumption

The evaluator reads `AccessModes` for Update operations:

```csharp
AccessMode GetAccessMode(FieldDescriptor field, StateDescriptor? currentState)
{
    // O(1) lookup — no re-derivation from modifiers
    if (field.AccessModes.TryGetValue(currentState, out var mode))
        return mode;
    
    // Fall back for states without explicit override
    return AccessMode.Writable;
}

UpdateOutcome Update(Precept precept, Version version, IReadOnlyDictionary<FieldDescriptor, object?> patch)
{
    foreach (var (field, _) in patch)
    {
        var mode = GetAccessMode(field, version.CurrentState);
        if (mode != AccessMode.Writable)
            return new AccessDenied(field.Name, mode == AccessMode.ReadOnly 
                ? FieldAccessMode.Read 
                : FieldAccessMode.Omit);
    }
    // ... continue with patch application
}
```

### Precept Builder Construction

The builder resolves access modes during the descriptor pass:

```csharp
IReadOnlyDictionary<StateDescriptor?, AccessMode> ResolveAccessModes(
    TypedField field,
    IReadOnlyList<TypedStateModification> stateModifications)
{
    var modes = new Dictionary<StateDescriptor?, AccessMode>();
    
    // Field-level baseline
    var baseline = field.Modifiers.Contains(ModifierKind.Readonly) 
        ? AccessMode.ReadOnly 
        : AccessMode.Writable;
    
    // Apply state-level overrides
    foreach (var mod in stateModifications.Where(m => m.FieldName == field.Name))
    {
        modes[mod.State] = mod.Kind switch
        {
            StateModificationKind.Modify => AccessMode.Writable,
            StateModificationKind.Omit => AccessMode.Omit,
            _ => baseline
        };
    }
    
    // Add baseline for states without explicit override
    modes[null] = baseline;  // null = all states / stateless
    
    return modes.ToImmutableDictionary();
}
```

### Open Question

> **Open Question (unresolved):** Should `AccessModes` be `IReadOnlyDictionary<StateDescriptor?, AccessMode>` or `ImmutableArray<(StateDescriptor?, AccessMode)>`? The dictionary is O(1) lookup but has overhead for small counts. The array is cache-friendly but O(n) lookup. For typical precepts with 3–10 states, the difference is negligible. Recommend dictionary for clarity.

### Implementation Checklist

- [ ] Add `AccessMode` enum to `src/Precept/Runtime/Descriptors.cs` or `SharedTypes.cs`
- [ ] Add `AccessModes` property to `FieldDescriptor`
- [ ] Update Precept Builder's descriptor pass to resolve access modes
- [ ] Update evaluator's `Update` operation to use `AccessModes`
- [ ] Update `FieldSnapshot` in inspection types to include access mode

---

## FaultCode.AmbiguousDispatch (Candidate)

**Source:** `docs/runtime/evaluator.md` §13 Open Questions

**Purpose:** Classify the impossible-path failure when multiple transition rows pass guard and constraint evaluation (first-match routing expects exactly one winner).

**Current state:** No `FaultCode` exists for ambiguous dispatch. The evaluator design mentions this failure mode but cannot produce a classified fault.

### Proposed Addition

```csharp
// src/Precept/Language/FaultCode.cs

public enum FaultCode
{
    // ... existing codes ...
    
    [StaticallyPreventable(DiagnosticCode.AmbiguousTransition)]
    AmbiguousDispatch = 14,
}

// src/Precept/Language/Faults.cs

FaultCode.AmbiguousDispatch => new(
    nameof(FaultCode.AmbiguousDispatch),
    "Multiple transition rows matched for event '{0}' in state '{1}' — first-match routing expected exactly one",
    RecoveryHint: "This indicates a bug in the proof engine's exclusivity analysis. The compiler should have emitted an error if overlapping guards exist."),
```

### Requires

- [ ] Add `DiagnosticCode.AmbiguousTransition` (may already exist)
- [ ] Add `FaultCode.AmbiguousDispatch` with `[StaticallyPreventable]` attribute
- [ ] Add `FaultMeta` entry in `Faults.GetMeta()`
- [ ] Update evaluator to call `Fail(FaultCode.AmbiguousDispatch, ...)` when multiple candidates pass

---

## HoverDescription / SnippetTemplate Catalog Additions (Candidate)

**Source:** `docs/tooling/language-server.md` §7.3 Completions, §7.4 Hover, §13 Open Questions

**Purpose:** Enable the language server's completion and hover features to show rich documentation for keywords, types, modifiers, and actions without hardcoding text in LS code.

**Current state:** Some catalog records may not carry `HoverDescription` (markdown documentation for hover tooltips) or `SnippetTemplate` (insert text with placeholders for completions).

### Proposed Addition

Add `HoverDescription` and optional `SnippetTemplate` to user-facing catalog entries:

```csharp
// src/Precept/Language/Type.cs

public sealed record TypeMeta(
    TypeKind Kind,
    TokenMeta Token,
    string Description,
    // ... existing properties ...
    string? HoverDescription = null,    // ← NEW: markdown for hover
    string? SnippetTemplate = null      // ← NEW: completion insert text with placeholders
);

// src/Precept/Language/Action.cs

public sealed record ActionMeta(
    ActionKind Kind,
    TokenMeta Token,
    string Description,
    ActionCategory Category,
    TypeTarget[] ApplicableTo,
    // ... existing properties ...
    string? HoverDescription = null,    // ← NEW
    string? SnippetTemplate = null      // ← NEW
);

// src/Precept/Language/Modifier.cs (base)

public abstract record ModifierMeta(
    ModifierKind Kind,
    TokenMeta Token,
    string Description,
    ModifierCategory Category,
    ModifierKind[]? MutuallyExclusiveWith = null,
    string? HoverDescription = null,    // ← NEW
    string? SnippetTemplate = null      // ← NEW
);

// src/Precept/Language/Operation.cs

public sealed record OperationMeta(
    OperatorKind Operator,
    TypeKind LeftType,
    TypeKind? RightType,
    TypeKind ResultType,
    // ... existing properties ...
    string? HoverDescription = null     // ← NEW (no snippet for operators)
);

// src/Precept/Language/Function.cs

public sealed record FunctionMeta(
    FunctionKind Kind,
    TokenMeta Token,
    string Description,
    // ... existing properties ...
    string? HoverDescription = null,    // ← NEW
    string? SnippetTemplate = null      // ← NEW
);
```

### Example Catalog Entries

```csharp
// Types.cs
TypeKind.Money => new TypeMeta(
    kind, Tokens.GetMeta(TokenKind.Money),
    "Qualified decimal for currency amounts",
    // ... existing properties ...
    HoverDescription: "**money** — Qualified decimal for currency amounts.\n\n" +
                      "Requires a qualifier (e.g., `USD`, `EUR`) for currency identity.\n\n" +
                      "Supports arithmetic, comparison, and mixed-currency prevention.",
    SnippetTemplate: "money(${1:qualifier})"),

// Actions.cs
ActionKind.Set => new ActionMeta(
    kind, Tokens.GetMeta(TokenKind.Set),
    "Assign a value to a field",
    ActionCategory.Assignment, AllFieldTypes,
    // ... existing properties ...
    HoverDescription: "**set** — Assign a value to a field.\n\n" +
                      "Replaces the current value with the specified expression.\n\n" +
                      "Example: `set balance = balance + amount`",
    SnippetTemplate: "set ${1:field} = ${2:value}"),

// Modifiers.cs
ModifierKind.Positive => new FieldModifierMeta(
    kind, Tokens.GetMeta(TokenKind.Positive),
    "Value > 0",
    ModifierCategory.Structural, NumericTypes,
    Subsumes: [ModifierKind.Nonnegative, ModifierKind.Nonzero],
    HoverDescription: "**positive** — The field's value must be strictly greater than zero.\n\n" +
                      "Implies `nonnegative` and `nonzero`. Enforced on every assignment.",
    MutuallyExclusiveWith: [ModifierKind.Nonnegative]),
```

### LS Consumption

The language server reads these properties directly:

```csharp
// Completions (§7.3)
IEnumerable<CompletionItem> GetTypeCompletions()
{
    return Types.All
        .Where(t => t.IsUserFacing)
        .Select(t => new CompletionItem
        {
            Label = t.Token.Text,
            Kind = CompletionItemKind.TypeParameter,
            Documentation = t.HoverDescription,  // ← uses catalog property
            InsertText = t.SnippetTemplate ?? t.Token.Text,
            InsertTextFormat = t.SnippetTemplate is not null 
                ? InsertTextFormat.Snippet 
                : InsertTextFormat.PlainText
        });
}

// Hover (§7.4)
Hover? GetKeywordHover(Token token)
{
    var meta = Tokens.GetMeta(token.Kind);
    if (meta.HoverDescription is null) return null;
    
    return new Hover
    {
        Contents = new MarkupContent
        {
            Kind = MarkupKind.Markdown,
            Value = meta.HoverDescription  // ← uses catalog property
        }
    };
}
```

### Status

**Candidate** — value is high (rich UX) but not blocking. Recommended for implementation with core LS features.

### Implementation Checklist

- [ ] Add `HoverDescription` property to `TypeMeta`, `ActionMeta`, `ModifierMeta` (abstract), `OperationMeta`, `FunctionMeta`
- [ ] Add `SnippetTemplate` property to `TypeMeta`, `ActionMeta`, `ModifierMeta` (abstract), `FunctionMeta`
- [ ] Update catalog entries with documentation strings (incremental — prioritize most-used first)
- [ ] Consider adding `HoverDescription` to `TokenMeta` for keyword hover (may duplicate `Types`/`Modifiers`/`Actions` hover)
- [ ] Update MCP `precept_language` output if `HoverDescription` should be exposed to AI tooling
