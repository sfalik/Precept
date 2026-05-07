# Elaine: UX Review — EventInspection Proposal (CC#8)

**Reviewer:** Elaine (UX Designer)  
**Date:** 2026-05-07  
**Reviewing:** `docs/working/event-inspection-proposal.md` (Frank + George)  
**Context:** Pre-approval UX review requested by Shane

---

## 1. Summary

Frank and George have produced a solid contract. The core design decisions — `Prospect` as the primary discriminator, `CurrentFields` + per-row `PostFields` for before/after diff, `ArgErrors` for invalid-arg feedback, and `EventName` for self-description — are all correct from a UX standpoint, and the shift to thin LS/MCP wrappers is exactly what's needed. My concerns are specific and actionable, not architectural. Two of the four open questions have clear UX answers (OQ-1, OQ-3). One is a strong lean (OQ-4). One is a judgment call Shane should make with the tradeoff named (OQ-2). The proposal is ready to approve with those four positions resolved and one structural gap addressed (the "winning row" identification problem). I'm not asking for iteration — I'm giving Shane what he needs to close it.

---

## 2. Positions on OQ-1 through OQ-4

### OQ-1: `DeclaredArgs` — this is the right name. Do not use `RequiredArgs`.

**Recommendation: `DeclaredArgs`.**

`Version.RequiredArgs(eventName)` already exists on the API surface and returns only required args (filtered by `IsOptional = false`). `EventInspection.DeclaredArgs` returns all args including optional ones. These are different data with different shapes. Using the same word `Required` for a collection that includes optional args is lying to the consumer.

A developer building an arg input form will render every declared arg — required and optional — with appropriate UI treatment (required fields marked, optional fields shown with softer treatment). If the field is named `RequiredArgs`, the natural reading is "only required args are here, I can ignore the optional ones." This produces incomplete forms. This is not a theoretical footgun — it is the most common mistake in form rendering: not knowing what optional inputs exist.

`DeclaredArgs` is unambiguous: it carries the complete arg contract for this event. The slight inconsistency with `Version.RequiredArgs()` is the price of not misleading every consumer who builds a form against this type.

---

### OQ-2: Add a minimal `ArgErrorKind` enum. Not `DiagnosticCode` richness — just 3 values.

**Recommendation: add `ArgErrorKind` now; defer rich code hierarchy.**

```csharp
public enum ArgErrorKind { TypeMismatch, UnknownArg, ValueInvalid }

public sealed record ArgError(
    string ArgName,
    ArgErrorKind Kind,
    string Reason);
```

The case for adding a code: once you ship `string Reason` as the only signal, every consumer that wants to style errors differently — show a type hint for `TypeMismatch`, show a value constraint for `ValueInvalid` — must parse the string. String parsing is brittle and breaks on localization or wording changes. The VS Code webview will want different iconography or tooltip behavior for "wrong type" vs "value out of range." An AI agent wants to dispatch on the kind, not parse the message.

The case against: over-engineering. The proposal's "start with string and add later" is pragmatic. The counter: adding a code field later is a breaking change to the public type. Adding it now costs two lines of code.

**My lean:** Add `ArgErrorKind` now. It is small, cheap, and addresses a real rendering need. It doesn't need to be as rich as `DiagnosticCode` — 3 values cover the scenarios the proposal describes. If Shane wants to defer, he should know the cost: string-parsing consumers proliferate and the field becomes load-bearing prose.

---

### OQ-3: Sealed DU. The nullable-string encoding is a rendering footgun and I've seen it burn developers.

**Recommendation: sealed DU (`RowEffect`) — Frank's preference.**

```csharp
public abstract record RowEffect
{
    public sealed record Transition(string TargetState) : RowEffect;
    public sealed record Apply() : RowEffect;
    public sealed record Reject(string Reason) : RowEffect;
}
```

Here is what happens when Kramer builds the preview panel with the enum approach: he writes code that accesses `row.TargetState` to show the state badge. On the `Apply` case, `TargetState` is null — the badge renders nothing, or worse, renders "null" in the UI. On the `Reject` case, he forgets to check `Kind == Reject` before showing the rejection reason panel. These are silent rendering bugs, not compiler errors.

With a sealed DU, neither mistake is possible. The pattern match is exhaustive. `TargetState` only exists in scope inside the `Transition` branch. `Reason` only exists inside `Reject`. The compiler enforces the contract that Frank wants to enforce in the type system.

For AI legibility: the sealed DU serializes to JSON as `{ "type": "Transition", "targetState": "Approved" }`. The `type` discriminant makes the payload self-describing. An AI agent parsing this knows immediately what shape it has without reading `Kind` and cross-referencing nullability rules. The enum approach produces `{ "kind": "Transition", "targetState": "Approved", "rejectReason": null }` — two nullable fields that are conditionally meaningful. The DU is cleaner for both human and AI consumers.

George's concern is that "consumer surface is simpler" with enum. I disagree: the pattern match on a sealed DU with 3 branches is straightforward, and its constraint enforcement is the simplicity — you cannot accidentally misuse the type. A simpler-looking API that silently misbehaves is worse than a slightly more verbose one that is correct by construction.

---

### OQ-4: `EventEnsures` per-row (Frank's preference), with one condition.

**Recommendation: move `EventEnsures` inside `TransitionInspection`, with the condition that `EventInspection` retains an `OverallConstraintStatus` computed field.**

Frank's core argument is right: `on<event>` constraints evaluate against a post-mutation working copy, and that working copy is row-specific. Placing constraint results on `EventInspection` (event-level) creates a semantic problem: which row's state did these constraints evaluate against? For `Certain` outcome, it's unambiguous — there is one winning row. For `Possible` outcome (multiple potentially matching rows), event-level constraint results are orphaned from their causal row.

From a rendering perspective: the developer building a preview panel naturally thinks in terms of rows. "For row 2 (which fires if Amount > 1000), what would happen and would any constraints fail?" That's one unit of display — the row and its consequences together. Separating the constraints to a higher level breaks that mental model. The consumer has to correlate event-level constraints back to row-level outcomes, which is the kind of reconstruction work the proposal correctly removes from the LS/MCP layers.

**The condition:** Rendering an event-level constraint summary is a common use case — an event picker panel might show a green/yellow/red indicator per event. With constraints only per-row, computing that indicator requires iterating `Transitions` to find the worst constraint status. This is minor computation, but it is the kind of thing every consumer will re-implement. Consider adding a computed read-only `ConstraintStatus WorstConstraintStatus` on `EventInspection` that aggregates the per-row constraint results. This gives the event-level summary back without duplicating the detailed data. It can also be omitted if Shane considers it over-engineering — the per-row move is the right base call regardless.

---

## 3. Broader UX Observations

**1. `Prospect` works cleanly as the primary discriminator. No fourth value needed.**

The three values map directly to UI copy without ambiguity: `Certain` → "Will fire", `Possible` → "Might fire", `Impossible` → "Cannot fire". Consumers do not need to combine `Prospect` with other fields to determine the primary rendering tier — `OverallProspect` on `EventInspection` is sufficient for the event-list-level indicator, and `TransitionInspection.Prospect` is sufficient for the row-level indicator. The Kleene propagation rules documented in `result-types.md` are internally consistent with this. Three values are right.

**2. The before/after diff structure is sufficient — but the "winning row" is implicit.**

`CurrentFields` (pre-mutation) + per-row `PostFields` (projected post-state) is the right structure. A consumer can render a field diff table by zipping `CurrentFields` and the relevant row's `PostFields` by `FieldName`. This works cleanly.

The gap: when `OverallProspect = Possible`, there is no canonical winning row — multiple rows may be `Possible`. The proposal correctly notes this and says "the runtime does not need to pick a canonical winner." That is a correct runtime decision but it pushes work to every consumer: each consumer must implement "find first non-Impossible row" to pick the best-guess row for their diff display. This is simple logic but it is logic every consumer will write. **Consider adding a `BestGuessRowIndex` (nullable int, null when `Certain`) to `EventInspection`**, naming the runtime's definition of "best guess" once rather than having each consumer re-implement it. This is optional — I am not blocking on it — but it is the kind of small API addition that prevents subtle divergence between consumers.

**3. `ArgDescriptor` is sufficient for a functional form but missing one field for a polished form.**

`ArgDescriptor(Name, Type, IsOptional, SlotIndex)` carries enough to render: input label, input type, required/optional marker, and display ordering (`SlotIndex`). A basic arg input form can be built from this.

What it does not carry: a description or help text for the arg. An arg named `OrderQuantity` is self-explanatory; an arg named `ThresholdOverride` is not. The consumer has no surface for a tooltip or inline description on the arg input. This is an acceptable gap for v1, but it should be a known TODO. The fix is trivial: add `string? Description` to `ArgDescriptor`. Shane should decide whether to add it now or carry the gap forward.

**4. MCP AI-legibility is strong overall, with one naming confusion risk.**

The structure is AI-friendly. `EventName` on every `EventInspection` makes the array self-describing — an AI agent iterating `UpdateInspection.Events` can identify each entry without an index lookup. `Prospect` values are plain English. `ArgErrors` with `ArgName` + `Reason` is natural language that an agent can act on.

The one naming risk: **`TransitionInspection.Prospect` vs `EventInspection.OverallProspect`** — two different fields both named `Prospect` at different levels of the hierarchy. An AI agent (or a human developer) parsing the JSON might confuse row-level certainty with event-level certainty. Using `OverallProspect` at the event level (as the proposal does) is correct but worth making explicit in the field names — `RowProspect` for `TransitionInspection.Prospect` would eliminate the ambiguity. I am not requiring this rename, but it should be considered as a clarification.

Secondary risk: with the enum approach for `TransitionKind` (if OQ-3 goes that way), an AI agent seeing `{ "kind": "Transition", "targetState": "Approved", "rejectReason": null }` must infer that `rejectReason = null` is non-meaningful here. With the sealed DU, the JSON shape makes the type explicit and null-free in the active branch. This is another argument for the DU.

**5. `UpdateInspection.Events` landscape is sufficient for an event-landscape panel — no additional calls needed.**

`EventName` + `DeclaredArgs` + `OverallProspect` + `CurrentFields` on each `EventInspection` in `UpdateInspection.Events` is sufficient to render a full event-landscape panel: event name as the row header, `OverallProspect` as the status indicator, `DeclaredArgs.IsEmpty` to flag "requires input" vs "fires immediately", `CurrentFields` for a field state reference column.

The redundancy of `CurrentFields` appearing on every embedded `EventInspection` when `UpdateInspection.Fields` already carries the same data (per OQ-5) is a real but acceptable cost. The self-describing design wins for consumers that receive an `EventInspection` out of context (e.g., from a direct `InspectFire` call). Consistency is the right call here. George's concern about surprise is a documentation problem, not a design problem — document that `CurrentFields` is always the same across embedded `EventInspection` items when they come from `InspectUpdate(null)`.

**6. The `Explanation` field was correctly removed from the LS shape.**

The LS-side `Explanation: string?` field (prose generation) was a violation of the thin-wrapper contract and a semantic mistake: prose describing a structured result belongs in the display layer, not the data contract. Its removal is correct. The LS webview and MCP tools should synthesize prose from structured fields at render time, not expect it in the data.

---

## 4. Items for Shane

**S-1: `ArgDescriptor.Description` — add now or carry as known gap?**  
The cost of adding `string? Description` now is one nullable string field on a type that doesn't exist yet in implementation. The cost of adding it later is a breaking change to a public contract. My lean: add it now with null value being valid (most args are self-explanatory from their names; description is optional). But this is your call on completeness vs minimalism.

**S-2: `BestGuessRowIndex` (or equivalent) — should the runtime name its winning-row heuristic?**  
For `Possible` outcomes, every consumer picks the "best guess" row to show in their before/after diff. If they all implement "first non-Impossible row" independently, that's consistent by convention. If one consumer decides "highest-weighted row" is better, you get divergent behavior across surfaces. A `BestGuessRowIndex: int?` on `EventInspection` (null when `Certain`, since the `Certain` row is unambiguous) encodes the runtime's intent once. This is a small API addition with meaningful consistency value. It is also potentially over-engineering at this stage. Your call — but name the heuristic somewhere if you don't put it in the type.

**S-3: `RowProspect` rename — worth the clarification or noise?**  
Renaming `TransitionInspection.Prospect` to `TransitionInspection.RowProspect` eliminates the ambiguity between row-level and event-level prospect fields. Both human and AI consumers benefit. But it is a minor rename with cosmetic value. You decide.

---

*Disposition: Ready to approve once OQ-1 through OQ-4 are resolved per the positions above, and Shane has called S-1 and S-2. No structural revision required.*
