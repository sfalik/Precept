using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Option F output shape: a single parsed construct, identified by its catalog metadata
/// and carrying an ordered array of typed slot values. Generic — no per-construct subtypes.
/// The parser produces ImmutableArray&lt;ParsedConstruct&gt;; all downstream consumers
/// (type checker, graph analyzer, proof engine) read this type.
/// </summary>
public sealed record ParsedConstruct(
    ConstructMeta             Meta,
    ImmutableArray<SlotValue> Slots,
    SourceSpan                Span
);
