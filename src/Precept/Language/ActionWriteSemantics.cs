namespace Precept.Language;

/// <summary>
/// Catalog-declared write-semantics classification for a state-machine action.
/// Consumed by graph-stage analyzers that distinguish value-establishing actions
/// from clearing actions and from non-write actions (e.g., FieldNeverSet).
/// Catalog-driven so analyzers never restate the classification in their own logic.
/// </summary>
public enum ActionWriteSemantics
{
    /// <summary>Does not write to a field at all (e.g., reject, transition, no transition).</summary>
    None             = 0,
    /// <summary>Establishes a value at the target — suppresses FieldNeverSet (set, put, add, enqueue, push, append, insert, and their *-by variants).</summary>
    EstablishesValue = 1,
    /// <summary>Mutates contents in place without necessarily establishing a value. Reserved for future structural mutations; no current actions classify here.</summary>
    MutatesContents  = 2,
    /// <summary>Removes content (clear, remove, removeAt, dequeue, pop) — does NOT establish a value; does NOT suppress FieldNeverSet.</summary>
    ClearsContents   = 3,
}
