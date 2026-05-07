using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record class FieldReference(TypedField Field, SourceSpan Site);
public sealed record class StateReference(TypedState State, SourceSpan Site);
public sealed record class EventReference(TypedEvent Event, SourceSpan Site);

public sealed record class SemanticIndex(
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableArray<FieldReference> FieldReferences,
    ImmutableArray<StateReference> StateReferences,
    ImmutableArray<EventReference> EventReferences
);
