using System.Collections.Generic;
using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Controls whether identifier resolution in the current scope is restricted to
/// fields declared before the current field index (for default/computed expressions)
/// or allows all fields (for guards, actions, rules).
/// </summary>
internal enum FieldScopeMode
{
    /// <summary>All declared fields are in scope.</summary>
    AllFields = 1,
    /// <summary>
    /// Only fields whose declaration order is strictly less than
    /// <see cref="CheckContext.CurrentFieldIndex"/> are in scope.
    /// Fires the ForwardReferenceProhibited diagnostic on violations.
    /// </summary>
    PriorFieldsOnly = 2,
}

/// <summary>
/// Mutable working state accumulated during the type-checker's two-pass check.
/// Not part of the public <see cref="SemanticIndex"/> contract.
/// </summary>
internal sealed class CheckContext
{
    // ── Typed symbol tables (Pass 1 output / Pass 2 input) ────────────────

    /// <summary>Typed fields in declaration order (primary collection).</summary>
    public List<TypedField> Fields { get; } = [];

    /// <summary>O(1) name lookup for typed fields.</summary>
    public Dictionary<string, TypedField> FieldLookup { get; } = new();

    /// <summary>Typed states in declaration order (primary collection).</summary>
    public List<TypedState> States { get; } = [];

    /// <summary>O(1) name lookup for typed states.</summary>
    public Dictionary<string, TypedState> StateLookup { get; } = new();

    /// <summary>Typed events in declaration order (primary collection).</summary>
    public List<TypedEvent> Events { get; } = [];

    /// <summary>O(1) name lookup for typed events.</summary>
    public Dictionary<string, TypedEvent> EventLookup { get; } = new();

    // ── Current scope (Pass 2) ────────────────────────────────────────────

    /// <summary>
    /// Event args visible in the current expression context — set when entering a
    /// transition row, event handler, or event-anchored ensure; cleared on exit.
    /// </summary>
    public IReadOnlyDictionary<string, TypedArg>? CurrentEventArgs { get; set; }

    /// <summary>
    /// Declaration index of the field currently being resolved (0-based).
    /// Set to -1 when not inside a field expression. Used by the
    /// <see cref="FieldScopeMode.PriorFieldsOnly"/> gate.
    /// </summary>
    public int CurrentFieldIndex { get; set; } = -1;

    /// <summary>
    /// Whether identifier resolution is restricted to prior-declared fields.
    /// Set to <see cref="FieldScopeMode.PriorFieldsOnly"/> when resolving default
    /// value or computed-field expressions; <see cref="FieldScopeMode.AllFields"/> otherwise.
    /// </summary>
    public FieldScopeMode CurrentScope { get; set; } = FieldScopeMode.AllFields;

    // ── Quantifier binding stack ──────────────────────────────────────────

    /// <summary>
    /// Stack of active quantifier bindings. Each frame records the binding name and its
    /// resolved type. Innermost binding is on top — shadows event args and fields.
    /// </summary>
    public Stack<(string Name, TypeKind Type)> QuantifierBindings { get; } = new();

    // ── Normalized declaration accumulators (Pass 2 output) ───────────────

    /// <summary>Normalized transition rows accumulated during Pass 2.</summary>
    public List<TypedTransitionRow> TransitionRows { get; } = [];

    /// <summary>Normalized rule declarations accumulated during Pass 2.</summary>
    public List<TypedRule> Rules { get; } = [];

    /// <summary>Normalized ensure declarations accumulated during Pass 2.</summary>
    public List<TypedEnsure> Ensures { get; } = [];

    /// <summary>Normalized access-mode declarations accumulated during Pass 2.</summary>
    public List<TypedAccessMode> AccessModes { get; } = [];

    /// <summary>Normalized state lifecycle hooks accumulated during Pass 2.</summary>
    public List<TypedStateHook> StateHooks { get; } = [];

    /// <summary>Normalized event handler declarations accumulated during Pass 2.</summary>
    public List<TypedEventHandler> EventHandlers { get; } = [];

    /// <summary>Normalized edit declarations accumulated during Pass 2.</summary>
    public List<TypedEditDeclaration> EditDeclarations { get; } = [];

    // ── Dependency facts ──────────────────────────────────────────────────

    /// <summary>Computed field dependency records accumulated during Pass 2.</summary>
    public List<ComputedFieldDep> ComputedDeps { get; } = [];

    /// <summary>Constraint field reference records accumulated during Pass 2.</summary>
    public List<ConstraintFieldRefs> ConstraintRefs { get; } = [];

    // ── Reference sites ───────────────────────────────────────────────────

    /// <summary>Field reference sites recorded at resolution time for LS semantic tokens.</summary>
    public List<FieldReference> FieldReferences { get; } = [];

    /// <summary>State reference sites recorded at resolution time for LS semantic tokens.</summary>
    public List<StateReference> StateReferences { get; } = [];

    /// <summary>Event reference sites recorded at resolution time for LS semantic tokens.</summary>
    public List<EventReference> EventReferences { get; } = [];

    /// <summary>Event-argument reference sites recorded at resolution time for LS semantic tokens.</summary>
    public List<ArgReference> ArgReferences { get; } = [];

    // ── CI tracking ──────────────────────────────────────────────────────

    /// <summary>
    /// Field names declared with <c>~string</c> (<see cref="CITypeReference"/>).
    /// Used by CI enforcement and identifier resolution to set
    /// <see cref="TypedFieldRef.IsCaseInsensitive"/>.
    /// </summary>
    public HashSet<string> CIFields { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Field names whose collection element type is <c>~string</c>.
    /// A <c>contains</c> on such a collection is CI-aware and does NOT trigger
    /// <see cref="DiagnosticCode.CaseInsensitiveValueInCaseSensitiveContains"/>.
    /// </summary>
    public HashSet<string> CIElementCollections { get; } = new(StringComparer.Ordinal);

    // ── Diagnostics accumulator ───────────────────────────────────────────

    /// <summary>All diagnostics emitted during the check pass.</summary>
    public List<Diagnostic> Diagnostics { get; } = [];
}
