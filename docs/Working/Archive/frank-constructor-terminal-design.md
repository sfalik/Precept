# Design Critique: Terminal Constructor (Initial Event as True Constructor)

**Author:** Frank (Lead/Architect)  
**Date:** 2026-05-15  
**Status:** Analysis — pending owner decision  
**Requested by:** Shane

---

## Proposal Summary

Make the initial event a **true constructor** with two structural guarantees:

1. **Terminal in initial state** — the action chain cannot include a `transition` outcome. Construction always terminates in the initial state.
2. **Fire-once enforcement** — the initial event cannot be fired after construction. It is structurally impossible to re-fire it on an existing entity.

---

## 1. What This Changes About the Construction Mental Model

The current model treats the initial event as a regular event that *happens to fire first*. It goes through the full pipeline and produces the full `EventOutcome` space — including `Transitioned`. The entity can land anywhere. Construction is "fire this event in a specific context" — semantically uniform with post-construction events.

The proposed model makes construction a **distinct semantic category**: an event whose sole purpose is to produce the entity in its declared starting position. It is not "an event that fires first" — it is "the act of bringing the entity into existence at its declared origin."

This is a sharper mental model. The current model's uniformity is elegant in theory ("construction is just an event!") but it creates a semantic gap: if construction can transition away from the initial state, then what does `state Draft initial` *mean*? It means "the state that construction starts from, but not necessarily where the entity will be after construction completes." That is confusing. The word "initial" on the state becomes a technical implementation detail about hollow-version bootstrapping rather than a user-facing guarantee about where entities begin their life.

Under the proposal: `state Draft initial` means "entities begin here." Full stop. The initial event is the mechanism that *produces* an entity in Draft. There is no ambiguity about what "initial" means.

**Verdict: The proposed model is semantically cleaner.** The current uniformity is engineer-facing elegance. The proposed model is domain-author-facing clarity.

---

## 2. What Is Gained

### 2.1 Keyword Overload Resolution

My prior critique identified `initial` as a P5 violation — same keyword meaning different things on states vs events. I proposed renaming the event keyword to `constructor`.

The proposal resolves this differently and, I'll concede, **more elegantly**. Under this design:

- `state Draft initial` = "this is the starting position"
- `event Create(...) initial` = "this is the starting event (the one that produces the entity at the starting position)"

The keyword now means the *same category of thing* in both contexts: "this is where things begin." The state is where the entity begins existing. The event is the act that begins existence. The semantic link between the two is **strengthened** rather than coincidental.

This is better than the `constructor` rename. A rename solves the syntactic ambiguity but creates a vocabulary expansion. This proposal solves the *semantic* ambiguity by making the two uses of `initial` genuinely mean the same thing: origin.

### 2.2 Construction-Time Transition Ambiguity Eliminated

Under the current design, a domain author writes `from Draft on Create -> set Amount = Create.Amount -> transition Active` and must understand that "initial state" is a bootstrapping detail, not a guarantee. Under the proposal, this pattern is a compile error. The meaning of `initial` becomes a guarantee about the entity's post-construction position.

### 2.3 Simpler Caller Mental Model

Currently the caller must handle `Transitioned` from `Create`. Under the proposal, `Create` can only produce `Applied`, `Rejected`, `ConstraintsFailed`, or `Unmatched`. `Transitioned` is structurally impossible at construction. The caller code simplifies:

```csharp
// Current — must handle arbitrary post-construction state
Version v = outcome switch {
    EventOutcome.Transitioned t      => t.Result,      // could be anywhere
    EventOutcome.Applied a           => a.Result,      // in initial state
    // ... error cases
};

// Proposed — construction always produces initial state
Version v = outcome switch {
    EventOutcome.Applied a           => a.Result,      // always in initial state
    // ... error cases
};
```

The caller *knows* what state the entity is in after successful construction. That is a meaningful simplification for consuming code.

### 2.4 D94 / Construction Guarantee Simplification

See §5 below.

---

## 3. What Is Lost

### 3.1 Construction-Time Routing

The current design allows patterns like:

```precept
from Draft on Create when Create.Priority = "urgent"
    -> set Amount = Create.Amount
    -> transition Expedited

from Draft on Create
    -> set Amount = Create.Amount
    -> transition Review
```

This lets the initial event *route* the entity to different starting states based on construction-time arguments. Under the proposal, this is forbidden. The entity always starts in Draft.

**How common is this pattern?** Looking at the 20+ sample files in `samples/`: exactly zero use construction-time transitions. Every sample either (a) has no initial event, or (b) has an initial event that stays in the initial state. The construction-time routing pattern is theoretically available but empirically unused.

**Is it replaceable?** Yes. The equivalent under the proposal is:

```precept
// 1. Construct (always lands in Draft)
// 2. Immediately fire a routing event

from Draft on Create
    -> set Amount = Create.Amount

from Draft on Triage when Priority = "urgent"
    -> transition Expedited

from Draft on Triage
    -> transition Review
```

This is *two* operations instead of one, but it is *clearer*: construction produces the entity, then a separate business event routes it. The construction and the routing are distinct concerns. Merging them into one operation saves a call but conflates "bring entity into existence" with "decide where it goes."

**My assessment:** The lost pattern is rare (zero observed usage), replaceable (two-step construction + routing), and conflates concerns (existence vs routing). The loss is acceptable.

### 3.2 The `Transitioned` Outcome Branch in `Create`

The runtime API currently documents `EventOutcome.Transitioned` as a valid construction outcome. Removing it is a breaking API change if any consumer handles it. However, since the compiler would now structurally prevent the case, the branch becomes dead code. This is a documentation and API surface cleanup, not a behavioral break for correct programs.

### 3.3 Atomic Construction-to-Active Patterns

Some domains might want entities that are "born active" — never exist in a draft state. Example: a system-generated audit record that is always "Active" from birth. Under the proposal, such an entity would need either (a) `state Active initial` (just make Active the initial state) or (b) two-step construction + immediate transition.

Option (a) is the clean answer: if entities are always born Active, make Active the initial state. The word "initial" means where things begin — if things begin Active, say so. This is actually *better* domain modeling than "the entity is born in Draft but immediately transitions to Active."

**This is not a real loss.** It is forcing the domain author to name the correct initial state.

---

## 4. Stateless Precepts

Under the current spec, stateless precepts have `State = null`. There is no initial state. The initial event fires through the pipeline with no state-entry semantics.

**Under the proposal:** "Construction always ends in the initial state" and "there is no initial state" are not contradictory — they are vacuously true. The constraint "no `transition` outcome" is already structurally guaranteed for stateless precepts (there are no states to transition to). The constraint "cannot be fired again" applies identically.

For stateless precepts, the proposal adds no new constraints. The initial event already cannot produce `Transitioned` (no states exist). The fire-once enforcement adds a new guarantee but doesn't change the construction flow.

**Compatibility verdict: Fully compatible.** The proposal's constraints are either vacuously satisfied or independently valuable for stateless precepts.

---

## 5. D94 and the Construction Guarantee

Currently, D94 (`InitialEventMissingAssignments`) must check every guarded path through the initial event to ensure all required fields are assigned. If construction can transition away from the initial state, the field-shape requirements depend on the *target* state — a field that is `omit` in the target state doesn't need assignment.

Under the proposal, construction always ends in the initial state. Therefore:

- **The target state is always known at compile time.** It is always the initial state.
- **The field shape is fixed.** The compiler knows exactly which fields are `omit` in the initial state and which require values.
- **Per-row target analysis is eliminated.** Every construction path produces the same target state. D94 becomes: "every guarded path through the initial event must assign all required fields that are non-omit in the initial state."

This is a meaningful simplification. The current design requires D94 to potentially consider different field-shape requirements per guarded row (if different rows transition to different states). The proposal makes D94 a single-target check.

**Additionally:** The proof engine no longer needs to reason about "what state does construction land in?" for downstream analysis. Construction outcome is deterministic with respect to state. Entry ensures (`to <InitialState> ensure`) always fire. Residency ensures (`in <InitialState> ensure`) always apply. The constraint surface at construction time is fully knowable at compile time.

**Verdict: Meaningful proof engine simplification.** Not transformative, but real and correct.

---

## 6. "Cannot Be Fired Again" Enforcement

### Compile-Time Enforcement

The type checker can enforce this structurally:

- If an event is marked `initial`, it is a compile error to reference it in any `from <State> on <Event>` row except those with the implicit `from <InitialState>` construction context.
- Wait — this needs more precision. In the current model, `from Draft on Create` is how you write a construction row. Under the proposal, the initial event cannot appear in ANY `from` row, because it cannot be fired on an existing entity.

Actually, there's a tension here. Currently, the initial event's construction rows ARE written as `from <InitialState> on <EventName>`. If the event can only be used for construction, do we keep that syntax? The hollow version starts in the initial state, so the `from <InitialState> on Create` row is the construction path. Post-construction, the entity is in the initial state, and the event cannot fire again — so even though the entity IS in the initial state and the event IS defined `from <InitialState>`, the runtime refuses re-fire.

**The enforcement splits into:**

1. **Compile-time:** The type checker emits a diagnostic if the initial event appears in any `from <NonInitialState> on <InitialEvent>` row. It can only be defined in the construction context (initial state).
2. **Runtime:** If the entity already exists (has been constructed), firing the initial event returns — what? `UndefinedEvent`? A new outcome? A specific rejection?

Actually, wait. If the entity is in the initial state post-construction and the event is defined `from <InitialState> on Create`, what prevents re-fire? The *runtime* must track "has this entity been constructed?" — which it knows implicitly (if a Version exists, it was constructed). The Fire operation on a Version should refuse the initial event entirely.

**Proposed enforcement model:**

- **Compile-time:** `PRE0XXX — InitialEventInNonConstructionContext` — the initial event appears in a `from <NonInitialState>` row. This is always an error.
- **Runtime:** `Fire(initialEventName, args)` on an existing Version returns `EventOutcome.UndefinedEvent` (or a new variant like `Forbidden`). The initial event is not in the set of fireable events for any existing entity, regardless of state.
- **Inspection:** `InspectFire` for the initial event on an existing version returns `Impossible` or equivalent.

**Is this enforceable at compile-time alone?** Partially. The compiler can prevent the author from writing non-construction rows for the initial event. But the *caller* attempting to `version.Fire("Create", args)` is a runtime check — the compiler cannot prevent a C# consumer from passing the wrong event name string.

**Diagnostic:** Something like `ConstructorEventFiredPostConstruction` — but this is a runtime boundary, not a compile-time diagnostic on the precept definition. In the definition itself, the compile-time check is: the initial event cannot appear as a fireable event from any post-construction state.

Actually — there's a cleaner framing. The initial event simply **does not appear in `precept.Events`** as a fireable event. It is exposed only through `precept.InitialEvent` for construction. The `Version.Fire` method would return `UndefinedEvent` because the event genuinely is not defined in the post-construction event space. This requires no new outcome type and no new runtime check beyond "is this event defined for this state?" — which already returns `UndefinedEvent` for unknown events.

**Verdict: Enforceable.** Compile-time prevents definition-level misuse. Runtime refuses re-fire via existing `UndefinedEvent` semantics by excluding the initial event from the post-construction event space. No new mechanism needed.

---

## 7. Transition Out of Initial State

If construction always lands in the initial state, first-transition semantics become the entity's first real lifecycle event:

```precept
state Draft initial
state Active
state Closed terminal

event Create(Name as string) initial
event Submit
event Close

from Draft on Create
    -> set Name = Create.Name

from Draft on Submit
    -> transition Active

from Active on Close
    -> transition Closed
```

The pattern becomes: **construct → then fire a real event to advance.** This is the standard two-step pattern that every OOP system uses: `var x = new Entity(args); x.Submit();`

**Is this worse, same, or better than construction-time transitions?**

It is **better** for three reasons:

1. **Separation of concerns:** "Bring into existence" and "advance through lifecycle" are distinct operations with distinct semantics. Construction initializes. Events advance. Conflating them hides a lifecycle decision inside what looks like a creation call.

2. **Inspectability:** Under the current model, `InspectCreate` must reveal that construction might land in different states. Under the proposal, `InspectCreate` always shows "entity will be in initial state." Lifecycle inspection starts at `InspectFire` from the initial state — where it belongs.

3. **Atomicity boundaries:** If construction + transition is one operation, failure of the transition also fails construction. The entity doesn't exist. Under the proposal, construction can succeed (entity exists in Draft) and the subsequent event can independently fail (entity stays in Draft). The caller has a created entity to work with even if the first advancement fails.

**Counter-argument:** Some domains want atomic intake-to-active. "A loan application should not exist unless it successfully enters UnderReview." Under the proposal, the entity would exist in Draft even if the subsequent Submit event fails.

**Response:** This is what `reject` is for. If the entity should not exist with invalid intake data, the construction action chain uses `reject` to refuse creation. The initial event can still reject. What it cannot do is *transition*. If the domain requires "entity must not exist in Draft ever" — then Draft is the wrong initial state. Make `UnderReview` the initial state and construct directly into it.

---

## 8. Overall Verdict

### Comparison to My Prior Recommendations

My prior critique recommended:
1. Rename `initial` → `constructor` on events (solve keyword overload)
2. Relax stateless/stateful mutual exclusion
3. Verify `no transition` grammar enforcement
4. Better docs for construction-time transitions

The proposal replaces recommendations #1 and #4 with a fundamentally different approach: instead of *renaming* the keyword and *documenting* construction-time transitions, it *eliminates the need for both* by making the semantics coherent.

- **#1 (keyword rename):** The proposal makes the rename unnecessary. `initial` genuinely means "origin" in both contexts when construction is terminal. The keyword overload is resolved semantically, not syntactically. This is superior.
- **#2 (stateless relaxation):** Orthogonal. The proposal doesn't address this and doesn't need to. Still a valid independent improvement.
- **#3 (`no transition` grammar):** The proposal makes this partially moot. If the initial event structurally cannot transition, there's no need for an explicit `no transition` in construction rows. The grammar question still applies to other contexts.
- **#4 (better docs):** Replaced entirely. No confusing behavior to document if the behavior doesn't exist.

### Does It Trade One Problem Set For Another?

The main cost is loss of construction-time routing. This is empirically unused in all samples, conceptually replaceable with two-step patterns, and architecturally dubious (conflates concerns). The cost is negligible.

The secondary cost is a slightly more restricted API (`Transitioned` removed from construction outcomes). This is a simplification, not a loss.

No new problems are introduced that I can identify. Stateless compatibility is clean. Proof engine simplifies. Enforcement is achievable with existing mechanisms. The keyword semantics become coherent.

### Final Assessment

**The terminal constructor design is superior to my prior recommendations.** It solves the same problems more elegantly, introduces no new problems, simplifies the proof engine, and produces a cleaner mental model for domain authors.

I am reversing my prior position that "construction-time transition away from initial state is sound." It was *technically* sound but *semantically* confusing. The proposal eliminates the confusion by eliminating the case. The resulting design is tighter, more predictable, and more aligned with Precept's philosophy of making invalid configurations structurally impossible — including the invalid *mental configuration* of "initial state doesn't mean initial state."

**Recommendation: Adopt.** Implement both constraints (terminal in initial state + fire-once enforcement). Do not rename the keyword. The semantic resolution is cleaner than the syntactic one.

---

## Implementation Implications (High-Level)

1. **Type checker:** New diagnostic when initial event's action chain contains `transition` outcome.
2. **Type checker:** New diagnostic when initial event appears in `from <NonInitialState>` rows.
3. **Runtime:** Exclude initial event from post-construction event space (Fire returns `UndefinedEvent`).
4. **Runtime API doc:** Remove `Transitioned` from construction outcome examples.
5. **D94:** Simplify to single-target analysis (always initial state).
6. **Spec §3A.5:** Rewrite construction outcomes paragraph to exclude `Transitioned`.
7. **Spec §3A.2:** Update outcome table for construction context.

---

## Open Question

Should the initial event's `from` rows be written differently syntactically? Currently: `from Draft on Create -> ...`. If the event can *only* fire in the construction context and *only* produces Draft, is the `from Draft` prefix still needed?

Options:
- **Keep `from <InitialState> on <InitialEvent>`** — consistent with all other transition syntax. The `from` clause is the initial state (which is where the hollow version starts). No grammar change needed.
- **Allow bare `on <InitialEvent>`** — since the from-state is always known (initial state), the `from` clause is redundant. But this creates a grammar variant that breaks the uniformity of transition declarations.

**My recommendation:** Keep `from <InitialState> on <InitialEvent>`. The syntactic uniformity is worth the minor redundancy. Domain authors can see at a glance that construction rows follow the same structural pattern as all other transitions. No grammar exceptions needed.
