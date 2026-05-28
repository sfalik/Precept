using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Precept.Language;

namespace Precept.Pipeline;

// FieldNeverSet analyzer.
//
// Whole-program write-site aggregation for every field declaration. A field
// without a write site can only ever hold its declared default (or remain
// unset for `optional`) — any rule, ensure, or consumer-side read sees a
// constant value. This sub-pass walks the type-checked program and emits a
// Warning.
//
// Suppression is catalog-driven: `ActionMeta.WriteSemantics == EstablishesValue`
// is the criterion. `Clear` / `Remove` / `Pop` / `Dequeue` (all classified
// `ClearsContents`) do NOT suppress — they don't establish a value.
//
// Caller-side write capability also suppresses: a field declared with
// `editable` (ModifierKind.Write) at the field-declaration site OR via a
// per-state `modify F editable` row grants the runtime API permission to
// write the field; both are write sites.

public static partial class GraphAnalyzer
{
    /// <summary>
    /// Collects every write site for every field and emits
    /// <see cref="DiagnosticCode.FieldNeverSet"/> for any field whose
    /// reachable write-site collection is empty. Write sites tied to states
    /// outside <paramref name="reachableStates"/> are excluded — the entity
    /// can never enter those states, so the writes can never execute.
    /// Construction-event rows are unconditional (a precept that can be
    /// constructed is by definition reachable at construction time);
    /// computed fields and field-level `editable` are also unconditional
    /// (no state association). Wildcard transition rows (<c>FromState == null</c>)
    /// are admitted as reachable — they fire from every state, and at least
    /// one of those states is reachable. See
    /// <c>docs/compiler/graph-analyzer.md § 6.7 Field-Write-Site Analysis</c>
    /// for the canonical description of the reachability semantics.
    /// </summary>
    private static void AnalyzeFieldWriteSites(
        SemanticIndex semantics,
        HashSet<string> reachableStates,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        foreach (var field in semantics.Fields)
        {
            if (field.ResolvedType == TypeKind.Error)
                continue;

            if (HasAnyWriteSite(field, semantics, reachableStates))
                continue;

            diagnostics.Add(Diagnostics.Create(
                DiagnosticCode.FieldNeverSet,
                field.NameSpan,
                field.Name));
        }
    }

    private static bool HasAnyWriteSite(TypedField field, SemanticIndex semantics, HashSet<string> reachableStates)
    {
        // Computed-field declarations: the `<-` expression is the value-establishing
        // site. A computed field can never trip FieldNeverSet by design.
        if (field.IsComputed || field.ComputedExpression is not null)
            return true;

        // Field-level access modifier `editable` — grants caller-side write
        // capability via the runtime API. No state association.
        if (field.Modifiers.Contains(ModifierKind.Write))
            return true;

        // Per-state access mode rows: `in <State> modify <Field> editable`
        // grants caller-side write capability while the entity is in that state.
        // Filter to reachable states only.
        if (semantics.AccessModes.Any(am =>
                am.Mode == ModifierKind.Write
                && string.Equals(am.FieldName, field.Name, System.StringComparison.Ordinal)
                && reachableStates.Contains(am.StateName)))
            return true;

        // Transition-row action chains — establishes-value semantics suppresses;
        // clears-contents does NOT suppress. Wildcard rows (FromState == null)
        // fire in any state and must be admitted unconditionally.
        if (semantics.TransitionRows
            .OfType<TypedTransitionRowSuccess>()
            .Any(row => (row.FromState is null || reachableStates.Contains(row.FromState))
                     && row.Actions.Any(action => IsEstablishingWriteTo(action, field.Name))))
            return true;

        // Construction / event-handler rows — unconditional. The construction-
        // row arg-to-field assignment surfaces here as `-> set X = Create.X`.
        // A precept that can be constructed at all is reachable at construction
        // time, regardless of post-construction state reachability.
        if (semantics.EventHandlers
            .OfType<TypedEventRowSuccess>()
            .Any(row => row.Actions.Any(action => IsEstablishingWriteTo(action, field.Name))))
            return true;

        // State entry / exit hooks — `to <State> -> set F = ...` and
        // `from <State> -> set F = ...` only fire when the state is entered/left.
        // Filter to reachable states only.
        if (semantics.StateHooks
            .Any(hook => reachableStates.Contains(hook.StateName)
                      && hook.Actions.Any(action => IsEstablishingWriteTo(action, field.Name))))
            return true;

        return false;
    }

    private static bool IsEstablishingWriteTo(TypedAction action, string fieldName)
    {
        // Catalog-driven: ask Actions.GetMeta which actions establish a value.
        // Hardcoding the set inside this analyzer would violate CLAUDE.md
        // § Catalog System — the analyzer derives from the catalog, never the
        // other way around.
        if (!string.Equals(action.FieldName, fieldName, System.StringComparison.Ordinal))
            return false;

        return Actions.GetMeta(action.Kind).WriteSemantics == ActionWriteSemantics.EstablishesValue;
    }
}
