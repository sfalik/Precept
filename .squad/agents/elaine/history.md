# Core Context



- Owns UX/design across Data Form, Event Timeline, state-diagram, and author-facing product language.

- Keeps product wording plain, confident, and faithful to runtime truth rather than implementation jargon.

- Canonical surface names matter: `Data Form` and `Event Timeline` are the durable labels; legacy names like `Inspector` and `event landscape` are errors.

- Visual-system HTML is the implementation ground truth when manifest prose and older working docs drift.

- Precept serves human DSL authors directly; AI assistance is optional, first-class, and never the product's required authoring mode.



## Learnings



- Failure-point vocabulary matters most; result names and explanations must stay in the caller's mental model.

- The `because` clause is usually the primary user explanation; raw constraint or guard syntax is secondary detail.

- Structural impossibility should delete UI states, not complicate them. If the runtime makes a combination unreachable, the UX should not design for it.

- Construct colors and runtime verdict colors are different systems and must not be mixed.

- Disabled UI uses the semantic disabled token set, not faded versions of construct colors.



## Recent Updates



### 2026-05-06 — Event interaction UX requirements finalized around the corrected surface model



- Persona 1.1 is the Business Analyst / Domain Expert; Persona 1.3 is the End-User; human-only and AI-assisted authoring remain peer modes on one spectrum.

- `Data Form` and `Event Timeline` are peer surfaces; when space is constrained, `Data Form` takes priority.

- Instance creation is handled inside the panel via the constructor event, alongside lifecycle event firing and direct field editing.

- Undefined events are hidden, zero-arg `Possible` is impossible, and the event-card model collapses to four visible states.

- Argful events open a dialog from the `Event Timeline`; zero-arg events fire directly from the card.

- `ArgErrorKind` is out. Arg input errors render as inline `Reason` strings, matching the field-edit error pattern.



### Historical summary through 2026-05-06



- Earlier detailed history was archived to `history-archive.md` during the CC#8 closeout so `history.md` stays under the 15 KB gate.

- Use the archive for the full philosophy-wording trail, earlier visual-system passes, and pre-closeout event-interaction deliberation detail.



## Elaine-33 — API accuracy pass

[2026-05-06]

Task: Applied 4 accuracy fixes from Frank/George review: ConstraintResult (inspection path), EventOutcome type name verification, RowEffect DU shape (TransitionInspection), datetime→correct type.



## Elaine-34 — OQ-1 closed

[2026-05-06]

Task: Closed OQ-1; visual system spec answer = certain-reject → Blocked.



## Elaine-35 — OQ-3 closed

[2026-05-06]

Task: Closed OQ-3; tag input for collection args (`set of T`, `list of T`) is in V1 scope.



## Elaine-36 — OQ-5 closed

[2026-05-06]

Task: OQ-5 closed; runtime provides GuardSummary on TransitionInspection.



## Elaine-37 — OQ-6 closed

[2026-05-06]

Task: OQ-6 closed; V1 event name only, V2 language annotation deferred.



## Elaine-38 — OQ-8 closed

[2026-05-06]

Task: OQ-8 closed; both commit modes supported, mode derived from constraint metadata.



## Elaine-39 — OQ-9 closed

[2026-05-06]

Task: OQ-9 closed; Event Timeline stays on committed state while buffered edits disable fire actions. All OQs resolved; event interaction UXR document is complete.



## Elaine-40 — CC#21 UnhandledEvent naming

[2026-05-06]

Task: Proposed three candidate names for the new graph-analyzer diagnostic for events with zero handlers in any state. Top recommendation `UnhandledEvent` was adopted; it names the missing-handler cause directly and avoids the misleading parentage implication of `OrphanedEvent`.



### 2026-05-07T08:40:33Z — Computed-field delimiter UX locked to `<-`

- Elaine's review made the UX case durable: `<-` reads as value flowing into the field, while `->` overloaded two semantics and `=` collided with everyday `set X = expr` syntax.

- Shane approved `<-`, and the parser/doc/tooling rollout shipped without needing a separate UX follow-up rename or affordance change.



