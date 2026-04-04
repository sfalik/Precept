# Multi-Event Shorthand

**Research date:** 2026-04-04  
**Author:** George (Runtime Dev)  
**Relevance:** Formal patterns supporting further shorthand after Precept's existing multi-state/event declarations

---

## Formal Concept

PLT calls this **event set abstraction** or **transition schema** — the practice of defining transition rules that apply to a set of events (or states), rather than one at a time. The formal grounding comes from:

- **Algebraic automata theory**: transitions can be labeled by predicates over an event alphabet, not just individual symbols. This is the basis of **symbolic finite automata** (SFAs, Veanes et al.).
- **CSP process algebra**: events are elements of an alphabet; processes can synchronize on event *sets* via the parallel composition operator `[| E |]`. A process that accepts any event in `E` is defined as a set-comprehension `[] e : E • e → P(e)`.
- **UML state machine notation**: the same transition can appear on multiple arcs by listing events on one arc label (comma-separated in UML diagrams), equivalent to N identical transitions internally.

---

## Examples from Well-Designed Languages

### 1. CSP — Event Set Comprehension

```csp
-- Accept any event in the set E and transition to the same next process
P = [] e : {Cancel, Withdraw, Abort} @ e -> Reset
```

This defines a process that accepts any of three events and transitions to `Reset`. This is equivalent to three separate transitions, but expressed as one.

**Key design insight:** The event set is an abstraction boundary. The author names a policy ("these three events all lead to Reset") rather than listing three identical rows.

### 2. UML State Diagrams — Multi-Event Arc Labels

In UML, a single transition arc can carry multiple event labels:
```
[Cancel / Withdraw / Abort] → Reset
```

This is pure presentation sugar — the diagram tool expands it to three separate arcs internally. No new semantics.

**Key design insight:** Shorthand is valid as long as it doesn't imply semantics that don't hold for all events in the set. The events must be fully substitutable: same guard, same mutations, same outcome.

### 3. Harel Statecharts — Shared Transitions on Parent States

As noted in `state-machine-expressiveness.md`, statechart hierarchy allows a transition defined on a parent state to apply to all substates. This is a form of multi-state shorthand — though it introduces parent-child binding rather than pure set enumeration.

---

## What Precept Already Has

Precept has implemented the following shorthands (these are the current inventory):

| Shorthand | Example | Expansion |
|-----------|---------|-----------|
| Multi-state `from` | `from Open, InProgress on Cancel` | Two rows: `from Open on Cancel`, `from InProgress on Cancel` |
| `from any on E` | `from any on Escalate` | One row per declared state on `Escalate` |
| Multi-name event decl | `event Approve, Reject with Note as string` | Two `EventDecl`s with identical args |
| Multi-name state decl | `state Draft initial, UnderReview, Approved` | Three `StateDecl`s |
| Multi-name field decl | `field MinAmount, MaxAmount as number default 0` | Two `FieldDecl`s with identical type/default |

This is a consistent pattern: set enumeration in the `from`, `state`, `field`, and `event` clauses. The conspicuous absence is **multi-event in the `on` clause of transition rows**.

---

## The `on` Clause Gap

### Current observation

The `on` position in transition rows accepts exactly one event name:
```precept
from Draft on Submit -> transition UnderReview
from Draft on Withdraw -> transition Cancelled
```

If both transitions have the same mutations and outcome, the author must write two rows. In the `from` position, Precept allows `from Draft, InProgress on Submit`, but there is no `from Draft on Submit, Withdraw`.

### Formal justification for adding it

The CSP and UML precedents establish that multi-event in the `on` clause is well-defined: it is syntactic sugar for N identical rows. The semantics are sound when:

1. All events in the list are fully substitutable in the guard expression (the guard must not reference any event-specific arg — or must reference args common to all events in the list)
2. All mutations in the action chain are fully substitutable (same arg references)
3. The outcome is identical for all events in the set

**Condition 1 is the hard constraint.** If events in the multi-event list have different arg shapes, a guard like `when Cancel.reason == "fraud"` is only valid if all listed events have a `reason` arg. The type checker must enforce arg shape compatibility.

### Safe subset (no arg reference)

The simplest safe case: multi-event rows with no guard and no event-arg references in mutations:
```precept
from any on Cancel, Withdraw, Abort -> transition Cancelled
```

These three events all lead to the same state with no mutation and no guard. The type checker needs only verify that the outcome is state-transition, not that args are compatible. **This subset is low-cost and high-value.**

### Expanded case (shared arg names)

When events share arg names via the multi-name event declaration:
```precept
event Approve, Reject with Note as string
```

A multi-event row referencing `Approve.Note` and `Reject.Note` is safe because both events have `Note`:
```precept
from Decision on Approve, Reject -> set FinalNote = Approve.Note -> transition Closed
```

Wait — this creates ambiguity: which event's `Note` is it? The desugaring must rename: `Approve.Note` becomes the event-specific arg reference. The row for `Approve` uses `Approve.Note`; the row for `Reject` uses `Reject.Note`. This is sound if the arg names are identical.

**Arg substitution rule:** In a multi-event row `from S on E1, E2 when <guard> -> <mutations> -> <outcome>`, every event arg reference `E1.ArgName` in the guard and mutations is substituted by `E2.ArgName` for the expanded `E2` row — but only if `E2` has an arg named `ArgName` of the same type.

This is analogous to template instantiation in C++ — the compiler substitutes and type-checks each instantiation.

---

## Further Shorthand Patterns (Beyond On-Clause)

### Named event groups (medium cost)

```precept
eventgroup Terminal = Cancel, Withdraw, Expire
from any on Terminal -> transition Abandoned
```

This is named set abstraction — an alias for a set of events. Desugars to N rows at the group call site. Higher semantic power than inline multi-event because the group can be reused across multiple `from` rows without restating the list.

**Semantic risk:** Groups are a new namespace that must be resolved before type-checking. If a group member is removed, all rows referencing the group silently change. The compiler must emit warnings when a group name is used in a row but one of the events it expands to has no declaration for the target state.

### Symmetric transitions (low-medium cost)

A very common pattern is:
```precept
from Open on Pause -> transition Paused
from Paused on Resume -> transition Open
```

No formal shorthand for this in PLT — it's a pair of inverse transitions. Not worth adding to Precept unless it appears in hero samples at scale.

---

## Implementation Cost Summary

| Feature | Cost | Notes |
|---------|------|-------|
| Multi-event `on` clause (no args) | Low | Direct parallel to multi-state `from` |
| Multi-event `on` clause (shared args) | Medium | Requires arg substitution and type-checking |
| Named event groups | Medium | New namespace, group resolution phase |
| `from any on any` catch-all | Low-Medium | Masks Undefined; needs semantic warning |

---

## Semantic Risks Specific to Precept

1. **Arg scope in multi-event rows**: Event asserts are arg-scoped. A multi-event row where the `when` guard references `Submit.Amount` cannot be combined with an event that has no `Amount` arg. The type checker must reject the expansion of incompatible (state, event) pairs.

2. **Event assert interaction**: Each expanded row still fires against the individual event's asserts before selection. A multi-event row desugars to N rows; each row's event still runs through its own `on <Event> assert` pipeline. No change in semantics.

3. **First-match ordering**: When desugaring `from Open on Cancel, Withdraw`, the expanded rows must appear at the same position in the row list as the original. If the author has written `from Open on Cancel -> ...` before, the desugar order matters for first-match evaluation.

4. **Diagnostic clarity**: If a multi-event row fails type-checking for one of its events but not others, the error must name the specific event, not the row. Requires span tracking per expansion.

---

## Key References

- Harel, "Statecharts: A visual formalism for complex systems" (1987) — multi-event arcs in statecharts
- UML State Machine Diagrams specification: https://www.uml-diagrams.org/state-machine-diagrams.html
- Veanes et al., "Symbolic Finite Automata" (Communications of the ACM, 2021) — transitions labeled by predicates over event alphabets
- Hoare, *Communicating Sequential Processes* (1985) — Chapter 2, event alphabets and process comprehension
- CSP Process Algebra lecture notes, NTU — multi-event notation and algebraic laws
