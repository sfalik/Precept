# Initial Event / Initial State — Design Critique

**Author:** Frank (Lead/Architect)
**Date:** 2026-05-15T19:48:03-04:00
**Requested by:** Shane
**Disposition:** Pre-ship critical design review

---

## Executive Summary

Shane is right — this design has real problems. Not fatal ones, but more than cosmetic. I'm categorizing each issue as **(A) fix before ship**, **(B) smooth with docs/errors**, or **(C) actually fine**.

The keyword overload is the worst offender. The hollow-then-hydrate pattern is defensible. The construction-can-transition subtlety is a feature, not a bug, but needs better visibility. The stateless/stateful mutual exclusion is too rigid. The `no transition` in stateless context is an abstraction leak.

---

## 1. The `initial` Keyword Overload — VERDICT: (A) Fix Before Ship

### The Problem

`initial` means two completely unrelated things:

- On a state: "this is the lifecycle starting position" (a topological marker)
- On an event: "this is the construction mechanism" (a behavioral trigger)

These aren't even the same *category* of concept. One is a graph annotation. The other is an execution semantic. The shared keyword creates a false implication of conceptual unity where none exists.

### What the User Sees

```precept
state Draft initial          # "initial" = where you start
event Create(...) initial    # "initial" = how you get created
```

A domain author (philosophy says: business analyst, not primarily a developer) reads this and reasonably concludes these are the same concept. They're not. The state is already set BEFORE the event fires. The event doesn't "initialize to" the state — it acts on an entity already in that state. The word `initial` papers over a two-step process that the user cannot see from the surface.

### Measuring Against P5 (Keyword-Anchored Readability)

P5 says "Statement kind is identified by its opening keyword sequence." The principle is that keywords should make intent legible. When a single keyword means two different things on two different constructs, it actively harms readability. You have to know which construct you're looking at to know what the keyword means. That's the OPPOSITE of keyword-anchored clarity.

### Proposal

Split into two distinct keywords:

- **`initial`** stays on states. It means "starting position." This is intuitive and correct.
- **`constructor`** (or `creation` or `intake`) goes on events. It means "this is the construction event."

```precept
state Draft initial
event Create(...) constructor
```

Now a reader can immediately see: one marks position, one marks behavior. The false unity is broken. The mental model is honest.

### Cost of Change

Low. Precept hasn't shipped. The keyword appears on events in exactly one sample file (`Test.precept`). The canonical samples (loan-application, insurance-claim, etc.) don't use initial events at all — they rely on defaults and first-transition hydration. This is a trivial grammar change, a new token, one catalog entry, and a migration of zero real user definitions.

### Tradeoff

We lose the symmetry of "both marked `initial`." That symmetry was always false — it implied a relationship that doesn't exist at the semantic level. Losing false symmetry is a feature.

---

## 2. The Hollow-Then-Hydrate Pattern — VERDICT: (C) Actually Fine

### The Concern

The entity exists in an intermediate invalid state (hollow, required fields unassigned) between step 1 (state set, defaults applied) and step 2 (initial event fires).

### Why It's Not a Problem

The atomicity guarantee (§3A.4) is not "papering over" this — it IS the resolution. The hollow version never escapes the construction boundary. It exists on a working copy that either promotes to committed state (if construction succeeds) or is discarded entirely (if it fails). No external observer can ever see the hollow intermediate.

This is the same pattern as every other mutation: during a `Fire` call, the entity is on a working copy with potentially invalid intermediate states between action steps. The constraint check happens AFTER all mutations complete. Construction is not special here — it's the same contract.

The hollow version is an implementation detail of construction, not an observable state. The spec is explicit: "An invalid configuration never exists, even transiently." (§3A.4). The working copy is not "existence" in the relevant sense.

### Measuring Against P1 (Prevention, Not Detection)

P1 says invalid configurations cannot exist. They can't. The hollow version is not a configuration that persists — it's a working copy in mid-construction. If anything, this is *more* preventive than alternatives (like requiring the user to construct-then-initialize in two visible steps).

**No change needed.**

---

## 3. Construction Can Transition Away From Initial State — VERDICT: (C) Actually Fine, but (B) Needs Better Docs

### The Concern

If `from Draft on Create -> ... -> transition Active` fires at construction, the entity was in `Draft` for zero observable duration. What was the point of marking `Draft` as `initial`?

### Why It's Sound

The initial state serves THREE purposes even when construction transitions away:

1. **Row routing.** The initial event's transition rows are selected by `from Draft on Create`. Without an initial state, there's no `from` to route against. The initial state is the dispatch context for construction.
2. **Omit semantics.** `in Draft omit X` means X is structurally absent at construction time. This shapes what D94 requires — fields omitted in the initial state don't need assignment. The initial state determines the construction shape.
3. **Entry ensure composition.** `to Draft ensure ...` fires as a construction intake invariant. Even if the entity immediately leaves, those entry ensures validated the intake data.

The entity "passing through" the initial state is analogous to a function parameter being in scope for the function body even if it's immediately destructured. The initial state isn't "where the entity lives forever" — it's "the entry point of the lifecycle graph." You have to enter somewhere to start traversing.

### The Documentation Gap

The confusion here is real but pedagogical, not structural. The fix:

- The language server hover for `initial` on a state should say: "The construction entry point. The entity may transition away from this state during construction if the initial event's row specifies a transition."
- Samples should demonstrate construction-time routing explicitly (none currently do).
- The quickstart/docs should explain initial state as "dispatch context for construction" not "where the entity starts its life."

---

## 4. The Stateless/Stateful Mutual Exclusion — VERDICT: (A) Fix Before Ship

### The Problem

In a stateful precept, `on Event -> actions` is a compile error. You MUST use `from State on Event -> ...`. The spec says mixing creates "ambiguity about execution order."

But this restriction is too coarse. There's a legitimate use case:

```precept
state Draft initial
state Active
state Closed terminal

event UpdateNotes(Text as string)   # This doesn't care about state
event Submit
event Close

# Why can't I write this?
on UpdateNotes -> set Notes = UpdateNotes.Text

# Instead I'm forced to write:
from any on UpdateNotes -> set Notes = UpdateNotes.Text -> no transition
```

The forced `from any on UpdateNotes -> ... -> no transition` is BOILERPLATE. It says nothing the runtime couldn't infer. The author knows the event doesn't care about state. They're forced to pretend it does.

### Measuring Against the Philosophy

The spec justifies the restriction: "event handlers are redundant with `from any on Event -> no transition` followed by rules." But that's backwards — the fact that you CAN express it as `from any -> no transition` doesn't mean the shorter form should be banned. The question is whether the shorter form adds ambiguity, and the answer is: it doesn't. A stateless handler in a stateful precept would mean exactly what `from any on Event -> actions -> no transition` means. There's no execution order ambiguity because the handler has no `from` clause — it can't conflict with state-specific rows for the same event (same event can't appear in both a handler AND a from-row).

### Proposal

Allow `on Event -> actions` in stateful precepts AS LONG AS no `from ... on <same Event>` row exists. The semantics: state-agnostic handler, always fires, no transition. If someone writes both `on Foo -> ...` and `from Active on Foo -> ...`, THAT is the compile error (ambiguous dispatch). But the handler alone is unambiguous.

### Tradeoff

This adds a new interaction rule (can't have both handler and from-row for same event). But it removes boilerplate and makes the language more expressive for state-agnostic operations, which are common in real precepts (look at how many `from any on X -> ... -> no transition` patterns appear in the samples).

---

## 5. D93/D94 Complexity — VERDICT: (B) Smooth With Better Diagnostics

### The Concern

The per-row construction guarantee is non-trivial to reason about. Every guarded path must independently assign all required fields. Could the language prevent incomplete paths structurally?

### Why Structural Prevention Is Worse

The alternative — "make incomplete construction paths structurally impossible" — would mean one of:
- Banning guards on initial event rows (too restrictive — kills intake discrimination)
- Requiring all rows to assign the same field set (kills conditional construction)
- Introducing a special "construction block" syntax that enforces field coverage (adding language surface for a problem diagnostics already solve)

All of these are worse than the current approach. Guarded construction routing is a legitimate feature (`Unmatched` for rejected intake is explicitly by design). The compiler proves each path complete — this is exactly P1 (prevention, not detection) applied correctly.

### What Should Improve

The diagnostics could be clearer:
- D94 should enumerate WHICH row is incomplete and WHICH fields it's missing, not just "initial event doesn't assign X"
- The error should show the guard condition of the failing row so the author can see which construction path is broken
- A diagnostic note (not error) on the initial event declaration could summarize: "3 construction paths detected: rows at lines 12, 15, 18"

**This is a tooling quality issue, not a language design issue.**

---

## 6. Omitted Fields and Construction Guarantee — VERDICT: (B) Needs Explicit Documentation

### The Concern

Fields omitted in the initial state are exempt from D94. But construction can transition away from the initial state. If the entity leaves `Draft` (where `X` is omitted) and enters `Active` (where `X` is NOT omitted), is `X` guaranteed to have a value?

### The Answer

YES — and it's already enforced, but by D132, not D94. The spec at §3A.3 rule 5 says: "D132 — RequiredFieldUnassignedOnEntry: When a transition moves a required field from `omit` in the source state to non-omit in the target state, the transition action chain must include a `set` for that field."

So the guarantee IS as strong as it appears, but it's split across two diagnostics:
- D94 covers fields that are non-omit at construction time
- D132 covers fields that materialize from omit → present during any transition (including construction-time transitions)

### The Documentation Gap

This split is invisible to the user. They see D94 and think "that's the construction guarantee." It's not — it's HALF of it. D132 is the other half, and it kicks in precisely when point 3 applies (construction transitions away from initial state).

The fix: document the construction guarantee as the composition of D94 + D132, not as D94 alone. The initial-event semantics doc I wrote earlier should have made this explicit. It didn't. That's a gap in my own analysis.

---

## 7. `no transition` in Stateless Context — VERDICT: (A) Fix Before Ship

### The Problem

In the stateless form, event handlers use `on Event -> actions` with no outcome keyword. There IS no `no transition` because there's no state to not-transition. The spec explicitly says handlers "do not support an outcome keyword."

BUT — my earlier analysis claimed a stateless initial event can use `no transition`. If the spec says handlers don't support outcomes, and the stateless initial event IS a handler, then `no transition` should be a parse error in that context. If it ISN'T a parse error, the grammar is inconsistent with the spec's own description.

### Investigating the Actual Grammar

The grammar distinguishes two forms:
- `EventHandlerDeclaration`: `on EventTarget ("->" ActionStatement)*` — NO outcome in the grammar
- `TransitionRowDeclaration`: `FromClause ... Outcome` — requires outcome

Stateless precepts use `EventHandlerDeclaration`. This form has no `Outcome` production. So `no transition` should be a SYNTAX ERROR in a stateless handler.

If it ISN'T currently an error, we have a grammar enforcement bug. If it IS already an error, then my earlier semantics document was wrong to discuss it in that context.

### The Real Issue

The confusion arose because I described construction semantics without clearly distinguishing which grammar form the initial event uses in stateless vs. stateful contexts. In a stateful context, the initial event fires through a `from InitialState on Event -> ... -> no transition` row. In a stateless context, it fires through an `on Event -> actions` handler with NO outcome keyword.

The `no transition` keyword is not applicable to stateless precepts AT ALL. If it appears, it's either a bug or an abstraction leak. Either way: verify parser behavior, and if `no transition` is accepted in a stateless handler, add a diagnostic to reject it.

---

## Summary Table

| # | Issue | Verdict | Action |
|---|-------|---------|--------|
| 1 | `initial` keyword overload | **(A) Fix** | Split: `initial` for state, `constructor` for event |
| 2 | Hollow-then-hydrate | **(C) Fine** | No change needed |
| 3 | Construction transitions away | **(C) Fine**, **(B) Docs** | Better hover, samples, docs |
| 4 | Stateless/stateful exclusion | **(A) Fix** | Allow `on Event ->` handlers in stateful precepts |
| 5 | D93/D94 complexity | **(B) Diagnostics** | Improve error messages, enumerate paths |
| 6 | Omitted fields + D132 | **(B) Docs** | Document D94+D132 as composite construction guarantee |
| 7 | `no transition` in stateless | **(A) Fix** | Verify/enforce grammar prohibition; fix parser if accepting |

---

## Recommendations

Three language changes before ship:

1. **Rename `initial` on events to `constructor`.** One keyword, one meaning. No breaking changes in the wild (Precept hasn't shipped; only Test.precept uses it in samples).

2. **Allow `on Event -> actions` in stateful precepts** when no `from ... on <same event>` row exists. Semantics: fires from any state, no transition. Compile error if both forms exist for the same event. Removes a common boilerplate pattern.

3. **Verify `no transition` cannot appear in event handlers.** If the parser currently accepts it, add a diagnostic. The grammar says it shouldn't be there; enforcement should match.

These are small, targeted changes with high readability payoff and zero migration cost.
