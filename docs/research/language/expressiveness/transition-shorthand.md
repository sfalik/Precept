# Transition Shorthand

Research grounding for future transition compactness proposals. No open proposal exists today.

This file is durable research, not a proposal body. It captures why transition shorthand keeps surfacing, what adjacent systems do, which semantic contracts must be explicit before any implementation, and which directions look attractive but weaken Precept's model.

Transition shorthand applies exclusively to stateful precepts. Data-only precepts have no transition rows and are unaffected by this domain.

## Background and Problem

Precept's transition rows are self-contained: each `from <State> on <Event> [when <Guard>] -> <actions> -> <outcome>` is independently readable, independently routable, and independently auditable. That property is a design asset — it is why the language is AI-writable, flat-file portable, and first-match deterministic.

The cost is row count. The 21-sample corpus contains **196 transition rows**. Several recurring patterns inflate that count without adding semantic information:

### Pattern 1 — Multi-state from (already addressed)

The most common duplication pattern is the same event handled identically from multiple states. The corpus shows this clearly:

- **it-helpdesk-ticket.precept** — `RegisterAgent` from New, Assigned, WaitingOnCustomer, and Resolved (4 rows, identical actions: enqueue → no transition).
- **utility-outage-report.precept** — `RegisterCrew` from Reported, VerifiedState, Dispatched, and Restored (4 rows, identical actions).
- **library-book-checkout.precept** — `ReturnBook` from CheckedOut and Overdue (2 rows, identical actions), and `ReportLost` from the same pair (2 more).
- **hiring-pipeline.precept** — `RejectCandidate` from Screening, InterviewLoop, and Decision (3 rows, identical actions).
- **maintenance-work-order.precept** — `Cancel` from Open and Scheduled (2 rows, identical actions).

Precept already addresses this with multi-state `from` and `from any`. The IT helpdesk example could be written `from New, Assigned, WaitingOnCustomer, Resolved on RegisterAgent -> ...` (1 row instead of 4) or `from any on RegisterAgent -> ...` if all states apply. **This is the highest-value shorthand in the corpus and it already exists.**

The verbosity analysis ([internal-verbosity-analysis.md](./internal-verbosity-analysis.md)) confirms that these multi-state patterns are available in every sample but are written expanded in the reference corpus for clarity.

### Pattern 2 — Guard-pair fallthrough duplication

The second most impactful pattern: every conditional transition requires a matching unguarded reject row for the "else" case. The corpus contains approximately **39 reject rows** across 21 samples (~20% of all transition rows), and the verbosity analysis estimates that 20–35% of all header counts are fallthrough-only rows.

Examples:

```precept
from Submitted on Approve when MonthlyIncome >= RequestedRent * 3 && CreditScore >= 650
    -> set ReviewerNote = Approve.Note -> transition Approved
from Submitted on Approve
    -> reject "Approval requires strong income coverage and acceptable credit"
```

Two headers for one conditional decision. This is not a multi-event shorthand problem — it is a guard-pair problem. The relevant compactness mechanism would be something like `else reject "..."` as an inline suffix, which is a separate design domain from multi-event `on` clauses. It is noted here because it accounts for more row inflation than the `on`-clause gap, but it is out of scope for this document's primary focus.

### Pattern 3 — The multi-event `on` gap

Precept's shorthand inventory has one conspicuous absence. State prepositions (`in`, `to`, `from`) accept comma-separated names and `any`. Event prepositions (`on`) do **not**. The language design doc states this explicitly:

> **Multi-target shorthand:** State prepositions (`in`, `to`, `from`) accept comma-separated state names or `any` (expands to all declared states). Event prepositions (`on`) do **not** — event asserts are arg-scoped, and different events have different arg shapes.

The exclusion is deliberate: events carry typed arguments, and different events in a multi-event list may have incompatible argument shapes. A guard like `when Cancel.Reason == "fraud"` is only valid if every listed event has a `Reason` argument of compatible type.

In the current sample corpus, genuine multi-event `on` candidates — cases where the same source state handles different events with identical actions and outcomes — are **rare**. The most common duplication pattern (same event, different states) is already addressed by multi-state `from`. The rarity is structural: events tend to carry different argument shapes, so their handling naturally diverges.

The future relevance comes from business domains where multiple no-argument terminal events route to the same end state:

```precept
# Hypothetical: three ways to end an engagement, all argument-free
from Active on Cancel -> transition Terminated
from Active on Expire -> transition Terminated
from Active on Withdraw -> transition Terminated
```

With multi-event `on`, this would become:

```precept
from Active on Cancel, Expire, Withdraw -> transition Terminated
```

### Pattern 4 — Catch-all event routing

The current model distinguishes `Undefined` (no rows exist for a state-event pair) from `Rejected` (a row explicitly denies the event). There is no mechanism for a blanket default across all unrouted pairs.

The language design doc locks the position:

> First-match evaluation for multiple rows on same `(state, event)`; no catch-all required

The `trafficlight.precept` sample uses `from any on Emergency` and `from any on VehiclesArrive` to handle system-wide events — but these must name specific events. A hypothetical `from any on any -> reject "Not applicable"` would convert every `Undefined` outcome to `Rejected`, eliminating the need for defensive reject-only rows.

The risk: `Undefined` is a meaningful signal. It tells authors (and the analyzer's C49/C51/C52 warnings) which event-state pairs have no authored routing. A catch-all silences that signal.

## Precedent Survey

| Category | System | Transition compactness model | Precept implication |
|---|---|---|---|
| **State machines** | XState v5 ([docs](https://stately.ai/docs/transitions)) | Wildcard transitions: `on: { "*": "targetState" }` catches any event not otherwise handled. Parent-state transitions apply to all children without repetition. | XState's wildcard is the exact catch-all Precept lacks — but it relies on hierarchical state scoping, which Precept deliberately excludes. |
| | Statecharts (Harel, 1987) | Multi-event arcs: a single transition arc labeled with multiple events desugars to N internal arcs. History and hierarchy reduce repeated routing. | The multi-event arc is pure sugar — semantically identical to N separate arcs. No new behavioral semantics. |
| | SCXML ([W3C spec](https://www.w3.org/TR/scxml/#transition)) | `<transition event="e1 e2 e3">` — space-separated event list on one transition element. Wildcard `event="*"` catches all unhandled events. | SCXML normalizes multi-event transitions as a first-class syntax feature. The `*` wildcard is explicitly specified. |
| | Ragel ([docs](https://www.colm.net/open-source/ragel/)) | Character class unions and ranges on transitions: `[a-zA-Z]` matches any character in the set. Kleene star for repetition. | Ragel operates at the character-automaton level, not business-event level. Confirms that set-based transition labels are standard in automata but the abstraction level differs. |
| **Process algebra** | CSP (Hoare, 1985) | Event set comprehension: `[] e : {Cancel, Withdraw, Abort} @ e -> Reset`. A process accepts any event in a named set and transitions uniformly. | CSP's event-set abstraction is the formal justification for multi-event `on`. The set is an abstraction boundary — the author names a policy, not N identical rows. |
| | UML state machines ([spec](https://www.uml-diagrams.org/state-machine-diagrams.html)) | Comma-separated event labels on a single transition arc. Visual sugar only — expands to N arcs internally. | Confirms multi-event labeling is standard visual practice. No semantic weight. |
| **Workflow / orchestration** | BPMN ([Camunda docs](https://docs.camunda.io/docs/components/modeler/bpmn/)) | Exclusive gateways evaluate conditions; no multi-event concept on sequence flows. Each event trigger is a separate catching event. | BPMN does not model multi-event transitions. Its complexity budget goes to routing, not transition compactness. |
| | Temporal ([docs](https://docs.temporal.io/workflows)) | Signal handlers name individual signals. No multi-signal shorthand. Workflow code handles routing. | Temporal delegates transition routing to host-language code. No DSL-level compactness mechanism. |
| **Rule / policy engines** | Drools DRL ([docs](https://docs.drools.org/latest/drools-docs/docs-website/drools/language-reference/index.html)) | Rules match on fact patterns; no event-set or wildcard-event concept. Each rule is self-contained. | Drools' self-contained rule model parallels Precept's self-contained rows. No multi-event sugar. |
| | DMN decision tables ([OMG spec](https://www.omg.org/spec/DMN/)) | Input columns can use ranges, lists, and wildcards (`-`) for "any value." Collect-hit or first-hit policy. | DMN's `-` (any) in input columns is the closest analog to a catch-all event match. The model is cell-based, not row-based. |
| **Formal methods** | Alloy ([docs](https://alloytools.org/documentation.html)) | Relations and set comprehensions over events and states. No transition-row concept; everything is a constraint. | Alloy confirms that set-based event grouping is natural in formal models, but the representation is constraints, not rows. |
| | TLA+ | Actions are predicates that may be disjoined: `Next == Action1 \/ Action2 \/ Action3`. No dedicated transition syntax. | TLA+'s disjunctive action composition is the logical equivalent of multi-event routing — expressed as predicate algebra. |
| **Validator DSLs** | FluentValidation, Zod, JSON Schema | No transition concept. Rules operate on data snapshots. | Validators have no state-machine surface. No relevance to transition compactness. |
| **Enterprise platforms** | Salesforce Process Builder | Multi-criteria entry conditions but single-trigger process entries. Each process starts from one triggering event. | Enterprise workflow tools generally do not support multi-event triggers on a single transition. Salesforce Flow is more expressive but still event-specific. |
| | ServiceNow Flow Designer | Individual triggers per flow. No multi-event shorthand. | Confirms that enterprise platforms do not model multi-event transitions as a first-class concept. |

### Cross-category pattern

Systems that support multi-event transitions fall into two groups:

1. **Formal / automata-theoretic:** CSP, SCXML, UML, and Harel statecharts treat multi-event labeling as standard sugar — it desugars to N identical transitions with no new semantics. These systems accept the pattern because it is syntactically obvious and semantically flat.

2. **Hierarchical state machines:** XState and statecharts use parent-state transitions as an implicit catch-all for children. This is more powerful than multi-event labeling but requires hierarchical scoping, which Precept deliberately excludes.

No system in the survey combines multi-event transitions with typed event arguments in the way Precept would need to. The arg-substitution problem is unique to Precept because its events carry structured arguments that participate in guards and mutations. CSP events are unparameterized; SCXML events carry flat data objects; XState context is separate from event payloads.

This means multi-event `on` in Precept requires a **type-compatibility contract** that has no direct precedent — it must verify that all events in the list share the referenced argument names and types before desugaring.

## Philosophy Fit

The product's unifying principle is governed integrity — ensuring the entity's data satisfies its declared rules at every moment. Each shorthand candidate is evaluated against the 13 design principles in `docs/PreceptLanguageDesign.md`.

### Multi-event `on` clauses

| Principle | Assessment |
|---|---|
| 1. Deterministic, inspectable model | **Neutral.** Desugaring is deterministic — same expansion every time. Inspect output would show per-event results, same as today. |
| 2. English-ish but not English | **Positive.** `from Active on Cancel, Expire, Withdraw -> transition Terminated` reads as naturally as the multi-state form. |
| 3. Minimal ceremony | **Positive.** Eliminates N-1 rows when N events share identical handling. |
| 4. Locality of reference | **Neutral.** The policy ("all these events do the same thing") is local to the row. |
| 5. Data truth vs movement truth | **Neutral.** No change to the invariant/assert model. |
| 6. Collect-all for validation, first-match for routing | **Needs care.** Desugaring must preserve first-match position. If `from Open on Cancel, Withdraw` desugars to two rows, both must occupy the same position relative to other `from Open on Cancel` rows. |
| 7. Self-contained rows | **Slight tension.** A multi-event row is still independently readable, but the reader must understand that it expands. The substitution rule for event args adds cognitive load if args are referenced. |
| 8. Sound, compile-time-first static analysis | **Needs enforcement.** The type checker must verify arg-shape compatibility across all events in the list. A reference to `Cancel.Reason` in a multi-event row must fail at compile time if `Withdraw` has no `Reason` arg. |
| 9. Tooling drives syntax | **Moderate cost.** Language server must expand multi-event rows for per-event completions. Grammar must extend the `on` clause parser. Diagnostic spans must attribute errors to specific events within the list. |
| 10. Consistent prepositions | **Positive.** Extends an existing pattern: `from` already accepts lists and `any`; applying the same to `on` is consistent. |
| 11. `->` means "do something" | **Neutral.** No change to the arrow model. |
| 12. AI is a first-class consumer | **Positive.** AI agents can generate multi-event rows as easily as multi-state rows. The expanded form is also generatable as a fallback. |
| 13. Keywords for domain, symbols for math | **Neutral.** No new keywords or symbols needed. |

**Net assessment:** Multi-event `on` is philosophy-positive in the no-arg case and philosophy-neutral-to-cautious in the shared-arg case. The arg-substitution complexity is the main friction point.

### Catch-all event routing (`from any on any`)

| Principle | Assessment |
|---|---|
| 1. Deterministic, inspectable model | **Tension.** A catch-all converts `Undefined` to `Rejected` for all unrouted pairs. The inspect output changes from "not applicable" to "rejected." Both are deterministic, but they carry different semantic weight. |
| 2. English-ish | **Positive.** `from any on any -> reject "Not applicable"` reads clearly. |
| 3. Minimal ceremony | **Positive.** Eliminates defensive reject-only rows. |
| 5. Data truth vs movement truth | **Neutral.** |
| 6. Collect-all / first-match | **Needs care.** A catch-all must be guaranteed to be the lowest-priority row for every (state, event) pair. Desugaring order matters. |
| 7. Self-contained rows | **Tension.** The catch-all is readable in isolation, but its effect is global — it changes the outcome for every unrouted pair. This is unlike any existing row, which targets a specific (state, event) combination. |
| 8. Sound static analysis | **Significant tension.** The analyzer's C49 (unused event), C51 (all-reject state-event), and C52 (event never succeeds) warnings depend on distinguishing "no rows" from "rows that reject." A catch-all silences the `Undefined` signal that powers these diagnostics. |
| 9. Tooling drives syntax | **Medium cost.** Inspect output must distinguish "explicitly rejected by catch-all" from "explicitly rejected by authored row." Preview and completions must not suggest the catch-all hides a missing transition. |
| 12. AI consumer | **Risk.** An AI authoring a precept might add `from any on any -> reject "..."` as a safety net, masking real authoring omissions. The catch-all is precisely the kind of blanket defense that makes AI-generated precepts harder to audit. |

**Net assessment:** Catch-all routing is philosophy-negative on balance. It weakens the diagnostic surface and the `Undefined` / `Rejected` distinction that Precept deliberately maintains. If it were added, it would need to carry a mandatory compiler warning ("catch-all masks N unrouted pairs") to preserve the signal it suppresses.

## Semantic Contracts To Make Explicit

These are the contracts any future proposal must state directly.

### 1. Desugaring Model

Multi-event `on` is pure syntactic sugar. The expansion must produce exactly one row per event in the list, each with identical guards, actions, and outcomes. The desugared rows must occupy the same position in the row list as the original multi-event row — they are interleaved at the source position, not appended.

The expansion is analogous to multi-state `from`, which already desugars one row per state internally. The parser should use the same expansion mechanism.

### 2. Arg-Shape Compatibility

This is the contract that has no direct precedent in adjacent systems.

In a multi-event row `from S on E1, E2 [when <Guard>] -> <actions> -> <outcome>`:

- If the guard or any action references `E1.ArgName`, the compiler must verify that `E2` has an argument named `ArgName` of the same type.
- The desugared row for `E2` substitutes `E1.ArgName` with `E2.ArgName` — this is template-style instantiation.
- If `E2` lacks the referenced argument, or the argument has a different type, the compiler rejects the multi-event row with a diagnostic that names the specific incompatible event.

The safe subset is multi-event rows with **no event-arg references** — no guard references, no `set` RHS references. This subset requires only that all listed events are declared; no arg-shape checking is needed. It is the highest-value, lowest-cost slice.

### 3. Event Assert Independence

Each expanded row still fires through its own event's `on <Event> assert` pipeline independently. A multi-event row does not merge or union event asserts — event asserts are structurally tied to individual events and cannot be cross-applied.

### 4. First-Match Ordering Across Expansion

When a multi-event row is interleaved with other rows for the same (state, event) pair, the expansion position matters. Given:

```precept
from Open on Cancel when Cancel.Reason == "fraud" -> transition Flagged
from Open on Cancel, Withdraw -> transition Terminated
```

The desugared `from Open on Cancel` row from the multi-event line must appear **after** the guarded `from Open on Cancel` row, because that is its source-order position. First-match semantics evaluate in declaration order, and desugaring must not reorder.

### 5. Catch-All Scope and Priority

If `from any on any -> reject "..."` is ever added, it must desugar to one row per (state, event) pair that has no other rows. It is a gap-filler, not an override. Pairs that already have authored rows are unaffected.

The compiler must emit a diagnostic (warning, not error) listing how many (state, event) pairs the catch-all covers, so authors can verify the scope is intentional.

### 6. Diagnostic Attribution

If a multi-event row fails type-checking for one event but not others, the diagnostic must name the specific failing event. A span-tracked expansion model is required so that the error points to `Withdraw` in `from Open on Cancel, Withdraw` if `Withdraw` is the incompatible event.

## Dead Ends and Rejected Directions

### Named Event Groups

```precept
eventgroup Terminal = Cancel, Withdraw, Expire
from any on Terminal -> transition Abandoned
```

Named event groups introduce a new namespace and a new resolution phase. If a group member is removed, every row referencing the group silently changes. The compiler must track group membership and emit warnings for missing or changed events.

The cost is disproportionate to the benefit. Multi-event `on` clauses achieve the same row reduction without a new namespace. Named groups would only justify their overhead if event sets were reused across many rows — but in the corpus, the same event set rarely appears more than once.

**Rejected: namespace complexity without proportional value.**

### Implicit Guard Fallthrough

An alternative compactness mechanism: if a guarded row has no matching unguarded row for the same (state, event) pair, the engine implicitly treats the unmatched case as `Unmatched` rather than requiring an explicit fallthrough row.

This already happens. The engine produces `Unmatched` when all rows have guards and none matches. The guard-pair duplication exists because authors want to provide a **specific rejection reason** rather than a generic `Unmatched` signal. The compactness solution for guard-pair rows is an inline `else reject "..."` suffix, not a change to fallthrough semantics.

**Out of scope: guard-pair reduction is a separate design domain.**

### Symmetric Transition Sugar

```precept
# Hypothetical: express bidirectional transitions in one statement
between Open, Paused on Pause, Resume
```

This would declare that `Pause` transitions from Open to Paused and `Resume` transitions from Paused to Open. But the mutations, guards, and outcomes are typically different in each direction. A symmetric declaration that hides asymmetric behavior is worse than two explicit rows.

The pattern also violates Principle 7 (self-contained rows) — neither direction is independently readable without understanding the expansion rule.

**Rejected: hides asymmetry, violates self-containment.**

### `from any on any` as Default Reject

As analyzed in the philosophy fit section, a blanket catch-all weakens the `Undefined` / `Rejected` distinction and silences diagnostics C49/C51/C52 that catch authoring omissions. The `Undefined` outcome is a signal, not an error — it tells authors and tooling which paths have no authored routing.

If a catch-all is ever needed, a scoped form (`from State on any -> reject "..."`) that covers all unrouted events in a single state would be less damaging than a global `from any on any`. But even the scoped form masks `Undefined` within its state.

**Rejected for now: diagnostic cost exceeds compactness benefit.**

### Event Wildcards in Guards

```precept
from Open on * when *.Amount > 0 -> transition Processing
```

An event wildcard that binds in guard expressions is a step toward pattern matching — a fundamentally different language model. It would require runtime dispatch on event shape rather than declared name, breaking the static (state, event) lookup model.

**Rejected: category shift from declared routing to pattern matching.**

## Row-Count Reduction: Quantified Impact

The following table estimates the row savings each mechanism would deliver against the 196-row corpus, **preserving explicit state-machine reading**.

| Mechanism | Status | Estimated rows eliminable | Notes |
|---|---|---|---|
| Multi-state `from` | **Already exists** | 15–20 | IT helpdesk (3), utility outage (3), library checkout (2+2), hiring (2), maintenance (1), others |
| Guard-pair `else reject` | **Not yet proposed** | 8–14 | ~39 reject rows, ~20–35% are pure fallthrough pairs |
| Multi-event `on` (no args) | **Research phase** | 3–6 | Rare in current corpus; future value in terminal-event domains |
| Multi-event `on` (shared args) | **Research phase** | 1–3 | Very rare; arg-shape compatibility limits applicability |
| Catch-all `from any on any` | **Rejected for now** | 0 (corpus) | No sample uses defensive reject-only rows for unrouted pairs; `Undefined` handles them |
| Catch-all `from State on any` | **Rejected for now** | 0 (corpus) | Same reasoning at state level |

**Key finding:** The highest-value row-reduction mechanisms are multi-state `from` (already shipped) and guard-pair `else reject` (not yet proposed but well-understood). Multi-event `on` has clear formal justification and is the natural next step in the shorthand inventory, but its corpus pressure is modest today.

## Why This Stays Horizon-Facing

Three factors keep this in Batch 3 rather than the active proposal queue:

1. **Low sample pressure.** The current corpus shows modest multi-event `on` candidates. The pattern's value scales with the number of terminal/cancellation events in a precept, which is higher in enterprise domains than in the reference samples.

2. **Arg-compatibility is novel.** No adjacent system combines multi-event transitions with typed arguments. The arg-substitution contract is Precept-specific and needs careful design — the safe no-arg subset is straightforward, but the shared-arg case requires template-style instantiation and per-event diagnostic attribution.

3. **Higher-priority lanes exist.** The type system expansion (Batch 1), expression expansion (Batch 2), and entity modeling surface (Batch 2) all have stronger sample pressure, more open proposals, and larger blast radius. Transition shorthand should not compete for design bandwidth until those lanes are resolved.

When this domain moves to a proposal, the recommended approach is:

- **Phase 1:** Multi-event `on` with no event-arg references (safe subset). Low parser cost, high conceptual consistency with existing multi-state `from`.
- **Phase 2:** Multi-event `on` with shared-arg substitution. Requires the arg-compatibility checker and per-event diagnostic attribution.
- **Phase 3 (if ever):** Scoped catch-all `from State on any`. Only if the diagnostic masking problem is solved with mandatory warnings.

The guard-pair `else reject` mechanism is a separate design domain with higher impact and should be tracked independently.

## Key References

- Harel, D. "Statecharts: A visual formalism for complex systems." *Science of Computer Programming* 8, no. 3 (1987): 231–274. — Original statechart paper; multi-event arcs.
- Hoare, C.A.R. *Communicating Sequential Processes.* Prentice Hall, 1985. Chapter 2: event alphabets and process comprehension.
- Veanes, M. et al. "Symbolic Finite Automata." *Communications of the ACM* 64, no. 5 (2021): 95–103. — Transitions labeled by predicates over event alphabets.
- W3C. "State Chart XML (SCXML): State Machine Notation for Control Abstraction." W3C Recommendation, September 2015. [https://www.w3.org/TR/scxml/](https://www.w3.org/TR/scxml/)
- XState v5 Documentation: Transitions. [https://stately.ai/docs/transitions](https://stately.ai/docs/transitions)
- UML State Machine Diagrams specification. [https://www.uml-diagrams.org/state-machine-diagrams.html](https://www.uml-diagrams.org/state-machine-diagrams.html)
- Ragel State Machine Compiler. [https://www.colm.net/open-source/ragel/](https://www.colm.net/open-source/ragel/)
- OMG. "Decision Model and Notation (DMN)." Version 1.4, March 2021. [https://www.omg.org/spec/DMN/](https://www.omg.org/spec/DMN/)
- Alloy Analyzer documentation. [https://alloytools.org/documentation.html](https://alloytools.org/documentation.html)

## Internal Cross-References

- [multi-event-shorthand.md](../references/multi-event-shorthand.md) — George's formal concept research on event set abstraction, CSP precedent, and Precept's existing shorthand inventory.
- [state-machine-expressiveness.md](../references/state-machine-expressiveness.md) — George's analysis of what PLT and statechart theory offer beyond Precept's current `from/on/transition` model.
- [internal-verbosity-analysis.md](./internal-verbosity-analysis.md) — Uncle Leo's statement-count analysis identifying guard-pair duplication as the #2 verbosity smell across 21 samples.
- [computed-fields.md](./computed-fields.md) — Quality bar for domain research documents.
