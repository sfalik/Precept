# Core Context



- Owns UX/design across Data Form, Event Timeline, state-diagram, and author-facing product language.

- Keeps product wording plain, confident, and faithful to runtime truth rather than implementation jargon.

- Canonical surface names matter: `Data Form` and `Event Timeline` are the durable labels; legacy names like `Inspector` and `event landscape` are errors.

- Visual-system HTML is the implementation ground truth when manifest prose and older working docs drift.

- Precept serves human DSL authors directly; AI assistance is optional, first-class, and never the product's required authoring mode.



## Learnings

- The five-construct color model (Structure, State, Event, Data, Rule) has an implicit two-axis substructure: structural identity (indigo–violet) vs. behavioural identity (cyan). Fields and args are the named companions that complete each axis.

- Fields sitting under Data-slate (#B0BEC5) buries structural declarations in the same tone as types and values. Promoting fields to the hero identifier color (#A5B4FC) is semantically correct: field names define entity shape, not incidental data.

- The tonal relationship "anchor → lifted companion" (e.g., #6366F1 → #A5B4FC for structure) can be mirrored on other axes. For events, #30B8E8 → #9AD8E8 gives args the same subordinate-but-related read.

- Moving field and arg names out of the Data construct narrows Data to types and values only. This has cross-surface implications (Data Form, Event Timeline) that need explicit scoping.

- The 1–3 shade paradigm governs intra-construct tonal gradients. Companion tokens (field, arg) are inter-construct axis relationships — orthogonal to the shade model. No paradigm change needed; just name the pattern.

- When a family is defined by hue coherence (all tones in the same hue band), adding tokens from a different hue breaks the family's visual identity — even if the semantic grouping makes sense. Organizational home must not contradict visual signal.

- When the hue proximity between two tokens already communicates their relationship, naming the grouping adds overhead without new information. Let the color do the work; document the intent with a brief note, not a structural layer.

- Shane's override: families are semantic categories, not hue bands. When tokens share a semantic category (fields and args ARE data), they belong in that family even if their hues differ. The family name defines the grouping; distinct hues within a family signal relationships to neighboring families' concepts (field→structure, arg→event). The spec's family definition was the bottleneck, not the organizational placement.

- Failure-point vocabulary matters most; result names and explanations must stay in the caller's mental model.

- The `because` clause is usually the primary user explanation; raw constraint or guard syntax is secondary detail.

- Structural impossibility should delete UI states, not complicate them. If the runtime makes a combination unreachable, the UX should not design for it.

- Construct colors and runtime verdict colors are different systems and must not be mixed.

- Disabled UI uses the semantic disabled token set, not faded versions of construct colors.

- Completion UX must stay semantically honest: if a qualifier axis is invalid for the selected type, suppress value completions and guide the user back to the correct preposition instead.

- For closed-set qualifier vocabularies, omission hurts trust faster than list length; a filtered ~150-item UOM list is acceptable if it means expected real-world units are present.



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



## Elaine-41 — Field and Arg Semantic Color Proposal

[2026-05-09]

Task: Proposed field color (`#A5B4FC`, hero identifier reused as `--field`) and arg color (`#9AD8E8`, new lifted cyan as `--arg`) based on Shane's structure/behaviour axis model. Full proposal at `docs/working/elaine-field-arg-colors.md`. Pending Shane sign-off.


## Elaine-42 — Color Family Paradigm Recommendation

[2026-05-09]

Task: Answered Shane's question on whether the 1–3 shade paradigm needs to change for field/arg companion tokens. Recommendation: no paradigm change — companion tokens are an inter-construct axis relationship, orthogonal to the intra-construct shade model. Formalize a thin axis layer (Structure Axis, Behaviour Axis) naming cross-family groupings. Arg is the only net new tone (8→9). Appended to `docs/working/elaine-field-arg-colors.md`.


## Elaine-43 — Paradigm Recommendation Revised (Standalone Companions)

[2026-05-09]

Task: Revised the paradigm recommendation after Shane's feedback that the axis layer was circular (states are already structural). Evaluated Shane's alternative (add field/arg to Data family) — rejected because Data is hue-coherent at ~215° slate and absorbing 239° indigo + 195° cyan would break that coherence. Landed on a third option: field and arg as standalone companion tokens, documented alongside families but not inside any family card. Data narrows to 2 tones (type + value), #B0BEC5 drops. Five families stay clean. Colors unchanged. Updated `docs/working/elaine-field-arg-colors.md`.

### 2026-05-09T14:47:06Z — Companion tokens recommendation revised
- Elaine-43 replaced the rejected axis-layer recommendation with standalone companion tokens for `--field` and `--arg`.
- The fallback idea of absorbing field/arg into the Data family was rejected because Data's hue-coherent slate family cannot stretch to indigo/cyan without breaking the visual rule.
- Durable direction: keep the five family cards intact, narrow Data to type + value, and document companion tokens in a short sub-section after the family cards.

### 2026-05-09T14:56:26Z — Shane final color-family ruling
- A late decision-inbox directive superseded the standalone companion-token recommendation.
- Final durable direction: the Data family expands to include field (`#A5B4FC`) and arg (`#9AD8E8`), and family definitions may group semantically related hues rather than only tonal variants.
- Treat the companion-token write-up as a rejected intermediate recommendation; the Shane ruling is now the canonical color-system direction.

