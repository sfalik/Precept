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
    /// write-site collection is empty.
    /// </summary>
    private static void AnalyzeFieldWriteSites(
        SemanticIndex semantics,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        foreach (var field in semantics.Fields)
        {
            if (field.ResolvedType == TypeKind.Error)
                continue;

            if (HasAnyWriteSite(field, semantics))
                continue;

            diagnostics.Add(Diagnostics.Create(
                DiagnosticCode.FieldNeverSet,
                field.NameSpan,
                field.Name));
        }
    }

    private static bool HasAnyWriteSite(TypedField field, SemanticIndex semantics)
    {
        // Computed-field declarations: the `<-` expression is the value-establishing
        // site. A computed field can never trip FieldNeverSet by design.
        if (field.IsComputed || field.ComputedExpression is not null)
            return true;

        // Field-level access modifier `editable` — grants caller-side write
        // capability via the runtime API.
        if (field.Modifiers.Contains(ModifierKind.Write))
            return true;

        // Per-state access mode rows: `in <State> modify <Field> editable`
        // grants caller-side write capability while the entity is in that state.
        if (semantics.AccessModes.Any(am =>
                am.Mode == ModifierKind.Write
                && string.Equals(am.FieldName, field.Name, System.StringComparison.Ordinal)))
            return true;

        // Transition-row action chains — establishes-value semantics suppresses;
        // clears-contents does NOT suppress (the action removes a value, it
        // doesn't establish one).
        if (semantics.TransitionRows
            .OfType<TypedTransitionRowSuccess>()
            .Any(row => row.Actions.Any(action => IsEstablishingWriteTo(action, field.Name))))
            return true;

        // Construction / event-handler rows — same establishes-value rule. The
        // construction-row arg-to-field assignment surfaces here as `-> set X = Create.X`.
        if (semantics.EventHandlers
            .OfType<TypedEventRowSuccess>()
            .Any(row => row.Actions.Any(action => IsEstablishingWriteTo(action, field.Name))))
            return true;

        // State entry / exit hooks — same rule. `to <State> -> set F = ...` and
        // `from <State> -> set F = ...` both land here.
        if (semantics.StateHooks
            .Any(hook => hook.Actions.Any(action => IsEstablishingWriteTo(action, field.Name))))
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
