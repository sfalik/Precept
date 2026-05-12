# Elaine History Archive



Archived from `history.md` during the 2026-05-06 CC#8 closeout summarization pass.



---



## Elaine-32 — ArgErrorKind removal

[2026-05-06]

Task: Removed ArgErrorKind from UXR-9 and all arg error display specs. Aligned to field edit pattern: ArgError.Reason string inline below field. No kind-specific branching.







- Owns UX/design across README form, semantic visual system, preview surfaces, and author-facing product language.

- Keeps public prose in plain, confident language that matches runtime truth rather than implementation jargon.

- Durable philosophy baseline: the strongest copy names the reader's pain, lands the absolute guarantee without hedging, and only then explains the structural reason the claim holds.

- Durable grammar baseline: grammar/design references should be negative-first, flat/keyword-anchored/named-slot centered, and documented with ASCII-first artifacts when examples are too wide for graph tooling.



## Learnings



- **Vocabulary at failure points matters most.** Developers read outcome/error variant names when something goes wrong — that's the highest-friction moment. Names that import vocabulary from other domains (RBAC: `AccessDenied`) or internal vocabulary (Precept rows: `RowInspection`) create maximum confusion at maximum pain. Fix those first.

- **DU base type provides operation context; variant names shouldn't repeat it.** `EventOutcome.ConstraintsFailed` is cleaner than `EventOutcome.EventConstraintsFailed` because the union name already tells you which operation produced the result. Variant names should describe *what*, not *where*.

- **Naming register consistency within a type family matters.** When success variants across parallel DUs (`EventOutcome` vs `UpdateOutcome`) use different naming registers (entity-centric `Transitioned` vs operational `FieldWriteCommitted`), it signals the types weren't reviewed side-by-side. Check all variants in a DU family against each other, not just each variant in isolation.

- **DSL keyword properties should be consistent across types.** `ConstraintDescriptor.Because` and `ConstraintViolation.BecauseClause` refer to the same concept in the same consumption context. Where a property maps to a DSL keyword, use the keyword form consistently; don't invent an alternate noun form for the same concept on a companion type.

- **Operation parameter vocabulary should propagate to outcome vocabulary.** `Update(fields)` → bad-input outcome should be `InvalidFields`, not `InvalidInput`. The asymmetry (`InvalidArgs` for Fire vs `InvalidInput` for Update) is a missed parallel; the outcome name should mirror the parameter name the caller used.





- Audience framing is usually a POV problem, not a content problem: the same guarantee can read as domain-expert guidance or as a developer commitment depending on pronouns and beneficiary framing.

- Philosophy copy weakens the moment it falls back to spec-speak (`failure classes`, semicolon mechanism lists, scare-quoted hedges) or implementation nouns the reader never encounters.

- The prevention claim lands best as structural impossibility, not as validator-style interception; the compiled contract is the ground, not a safety net beneath code.

- Business-process guarantees and expression-safety guarantees solve different anxieties; both deserve explicit language when explaining compile-time structural checking.

- Use distinct visual classes when diagrams mix grouping markers and per-construct annotations; readers need to tell structure from badges at a glance.

- For fixed-width documentation, prefer ASCII-safe symbols over ambiguous-width glyphs when alignment is part of the contract.



- **Public API result vocabulary should stay in the caller's mental model.** Use state-machine terms like `TransitionInspection` and field-shape terms like `FieldNotEditable`; avoid internal implementation words (`RowInspection`) and adjacent-domain security language (`AccessDenied`).

- **Outcome families should be reviewed as a family, not variant-by-variant.** Let the DU base carry operation context, keep failure names aligned with operation inputs (`InvalidFields`) and structural facts (`ConstraintsFailed`), and keep success names in the same short past-tense register (`Transitioned`, `Applied`, `Updated`).



## Recent Updates



### Historical summary through 2026-05-03T15:37:24Z

- Built and refined the active grammar/design reference baseline: grammar hierarchy/reference artifacts, catalog-system diagram simplification, language-server diagnostic-enrichment relocation, and the related durable presentation rules for ASCII-first documentation and author-centered wording.



### 2026-05-04T12:31:05Z — Public API naming assessment



- Conducted comprehensive naming assessment of `docs/working/runtime-api-public-surface-spec.md` against philosophy vocabulary, API consistency, and developer clarity criteria.

- Six naming issues identified: `AccessDenied` (RBAC false connotation → `FieldNotEditable`); `RowInspection` (internal vocab → `TransitionInspection`); `BecauseClause` vs `Because` inconsistency; `InvalidInput` vs `InvalidArgs` asymmetry (→ `InvalidFields`); `FieldWriteCommitted` register mismatch (→ `Updated`); plus concurrence with Frank's `ConstraintsFailed` rename and DU nesting.

- Decision record written to `.squad/decisions/inbox/elaine-api-naming-assessment.md`.



- Elaine-23 produced philosophy v5 after applying Frank/Steinbrenner/Peterman reviewer notes from the v4 review pass.

- Elaine-24 then shifted the same content to the correct audience: a developer evaluating/adopting Precept, with domain-user pain retained as the beneficiary frame rather than the direct addressee.

- Elaine-25 applied the locked v6 wording to docs/philosophy.md, replacing the Prevention, not detection and Compile-time structural checking bullets with the final prevention-first / developer-commitment text.





### 2026-05-06 — Event Interaction UX Requirements: Visual System Reconciliation + Data Edit Workflows



**Task:** Deep research pass + two addendums on `docs/working/elaine-ux-requirements-event-interaction.md`.



**Research consumed:**

- `docs/philosophy.md`, `docs/language/precept-language-spec.md`, `docs/runtime/runtime-api.md`

- `design/system/semantic-visual-system-manifest.md`, `design/system/semantic-visual-system-notes.md`

- `research/design-system/business-app-inspectability-ux.md`, `business-app-inspectability-product-communication.md`

- Samples: `loan-application.precept`, `insurance-claim.precept`, `customer-profile.precept`, `fee-schedule.precept`, `payment-method.precept`



**Changes made to the doc:**



1. **§0.5 Mental Model** — New section grounding the event/state machinery in business analogies. Establishes: (a) the two-path model (event fire path vs. direct edit path), (b) the key insight that data edits reshape event availability via InspectUpdate, (c) the Version-as-immutable-snapshot model for undo, (d) stateless precepts as first-class citizens.



2. **Terminology reconciliation** — "Inspector" banned, "Data Form" locked. "Field value panel" → "Data Form." "Event landscape panel" / "event landscape" → "event actions region" (when referring to the component within the Data Form surface).



3. **Color token reconciliation** — All informal color names throughout the doc replaced with locked canonical CSS custom property tokens: `--valid: #34D399`, `--warn: #FDE047`, `--violated: #F87171`, `--event: #30B8E8`, `--rule: #FBBF24`. Dashed border = blocked/violated (exclusive signal). Italic = constraint pressure on identity label (exclusive use).



4. **§6 Data Edit Workflows** — Major new section covering the entire direct-edit path: field status signals, inline editing, buffered atomic commit, real-time InspectUpdate preview (5 scenarios), UpdateOutcome handling, state-dependent editability scenarios, the critical Edit→Save distinction, and undo/redo.



5. **UXR-31 through UXR-38** — Eight new derived UX requirements for data edit workflows.



6. **OQ updates** — OQ-2, OQ-4, OQ-7 closed with decisions. OQ-8, OQ-9, OQ-10 added.



7. **Design Conflicts for Shane** — New closing section flagging three conflicts: surface priority vs. peer-surfaces rule (A), event-landscape naming vs. Event Timeline surface (B), runtime gap for InspectUpdate + FieldAccess mode update (C, also OQ-10).



**Decision record:** `.squad/decisions/inbox/elaine-ux-research-pass.md`



**Key learnings from this session:**



- **Two-path structural enforcement is a design gift.** `Update()` never transitions state; `Fire()` never patches fields outside its action chain. This is a runtime invariant, not a convention. The UX never needs to warn "this edit might transition state" — structurally impossible.

- **InspectUpdate(pendingPatch) is the live preview engine.** The fact that it returns the full event landscape against the hypothetical patch state means the Data Form can show "Approve will unlock after saving." This is the most important workflow relationship in the product. Build the UX around surfacing this.

- **Conditional editability (guarded fields) needs runtime clarification.** `InspectUpdate({FraudFlag: false})` — does it return updated `FieldAccessInfo.Mode = Editable` for `AdjusterName`? Unclear from API doc. If not, conditional unlock UX requires a new API surface.

- **Stateless precepts are structurally different from lifecycle precepts.** No state selector, no event actions region (or empty), pure `writable`-field maintenance UX. The Data Form must handle this gracefully.

- **The Version snapshot is the undo primitive.** Every Update/Fire returns a new Version. Undo is a stack of previous Version instances — not mutation reversal. The webview maintains its own undo stack, not VS Code's.





### 2026-05-06T18:25:02.170-04:00 — Elaine-29 Targeted Correction Pass



Six Shane-directed rulings applied to `docs/working/elaine-ux-requirements-event-interaction.md`:



1. **Persona 1.1 replaced: Business Analyst / Domain Expert.** The prior "Developer: DSL Author / Runtime Tester" persona was wrong for who actually owns precept definitions. The BA is the domain authority — analyst-tier (SQL/Power BI), not a developer. Primary authoring mode is AI-assisted via MCP; the spectrum from full human authoring to fully AI-assisted is first-class. Language readability serves both modes. Grounded in philosophy.md "governed integrity" framing.



2. **Persona 1.3 replaced: End-User.** The prior "Domain Expert / Product Owner" conflated the domain rule owner (now persona 1.1) with someone using a governed product. End-User has zero DSL awareness, cares about actions and blocks in domain terms, and is never authoring or validating.



3. **§2.1 layout priority corrected.** "Events first" framing removed. New rule: Data Form and Event Timeline are peer surfaces; if layout forces a choice, Data Form > Event Timeline. Conflict A in Design Conflicts updated to ✅ Resolved.



4. **"event landscape" → "Event Timeline" everywhere.** 15 occurrences replaced; zero remain. This rename is complete and canonical.



5. **§0.5 Mental Model expanded to three interaction modes.** Previously only described the two-path change model (event firing / field editing). Added explicit "instance creation" as a third first-class mode: firing the constructor event to bring a new instance into existence. Instance creation is distinct from the other two — it applies before an instance exists; the others apply to an existing instance.



6. **OQ-10 closed.** Shane confirmed: `InspectUpdate` MUST return updated `FieldAccessInfo` modes reflecting the hypothetical field patch. §6.3 (real-time conditional field unlock) is unblocked. §6.3 updated with runtime confirmation. Conflict C updated to ✅ Resolved. Document status line updated.



**Key learning from this pass:**



- **Personas define the UX register.** The shift from "developer" to "business analyst" as persona 1.1 isn't just a label change — it reframes what the inspector surface must prioritize: legible, reviewable, validatable output rather than maximum technical detail. The `because` clause over the expression; the outcome in plain terms over the pipeline mechanics.

- **OQ-10 being confirmed is quietly significant.** `InspectUpdate` returning updated `FieldAccessInfo` modes means real-time conditional unlocking is structurally supported by the existing API — no new surface needed. This is a clean design; it means the Data Form can show field locks/unlocks in real time as edits happen, powered by a single existing call.

- **Instance creation deserves its own panel state.** Calling it out as distinct from "no data filled in yet" is important: the panel behaves differently before an instance exists (no event firing, no field editing — only instance creation available). The prior framing left this as an edge case; it's actually a primary entry state.





### 2026-05-06T18:31:57.635-04:00 — Elaine-30 Two Targeted Corrections



Two Shane-directed rulings applied to `docs/working/elaine-ux-requirements-event-interaction.md`:



1. **`undefined` event = hide entirely (not grayed out).** The prior State 1 (Unavailable) was triggered by EventOutcome.UndefinedEvent / empty Transitions — it showed a grayed-out placeholder card. This was wrong. Corrected: `undefined` events are not rendered at all in the Event Timeline. State 1 (Unavailable) is now scoped to events that ARE defined in the precept for this state but are currently non-interactive. A prominent blockquote added to State 1, UXR-1 updated, terminal state scenario updated. The key principle: hiding absent events entirely is correct UX; a grayed-out placeholder for a non-existent event is wrong.



2. **DeclaredArgs == 0 AND OverallProspect = Possible is structurally impossible.** State 5 (Ready — Uncertain), which described `DeclaredArgs.Length == 0 AND OverallProspect = Possible`, was removed entirely. The five-state model is now a four-state model. All references to this impossible combination corrected: §3.3d heading, §3.3e heading, §4.6, §5.1, §2.4 hover tooltip, UXR-1, UXR-12, UXR-21. OQ-2's prior closed decision superseded — the labeling distinction it addressed is moot because the case never occurs. A structural constraint note added to §3.3d: Possible requires at least one declared arg.



**Key learnings:**



- **"Unavailable" and "undefined" are categorically different.** Conflating them was the root error in State 1. An event that exists in the precept for this state but is non-interactive deserves a presence (grayed out); an event that doesn't exist here at all deserves none. The visual model is simpler and more honest when we hold this distinction firmly.

- **Structural impossibility cleans up UX states.** When the runtime's data model makes a combination impossible, the UX shouldn't design for it. Removing State 5 simplifies the four-state card model significantly — and the rationale ("OverallProspect = Possible requires at least one arg") is now documented in the spec, not just in memory.

- **Closed OQs can become moot.** OQ-2's heuristic decision was valid at the time but was rendered moot by a structural fact about the runtime. Updating it to "superseded by structural ruling" preserves the history and prevents confusion when someone reads it later.





### 2026-05-06 — Elaine-31 Visual System Alignment Pass



Full read of all `design/system/` artifacts (HTML canonical output, manifest, notes, canonical specimen) against `docs/working/elaine-ux-requirements-event-interaction.md`. Four categories of misalignment found and corrected.



**Changes made:**



1. **§3.1 + UXR-5 — Inline expansion → dialog model (structural).** The most significant correction. The prior spec described arg input as inline card expansion (card expands in-place, action bar below). The canonical HTML defines a **dialog** pattern: clicking a Needs Input event opens a modal overlay dialog with header (event name + target state), body (arg form + outcome preview), and footer (Cancel + OK). The OK button is inactive/pristine until required args are supplied, then takes event color when active. Correction applied to §3.1 (full rewrite), UXR-5 (full rewrite), and all "fire button" terminology in §3.3a–e, §3.5, UXR-9 through UXR-13, UXR-18, UXR-28, UXR-29, OQ-1. No-args Certain events still fire directly on card click (UXR-30 unchanged — already correct).



2. **§2.2 State 1 — Support-disabled tokens (not opacity tint).** "50% opacity of event color" → `--support-disabled-text: rgba(158,171,190,0.42)`, `--support-disabled-surface: #0d1014`, `--support-disabled-controls: #1f2a39`. The rule: disabled UI elements use the semantic disabled token set, not opacity tints of the construct color.



3. **§2.2 State 4 — "Fire button or chevron" removed.** The card itself IS the fire trigger for Ready–Certain events. No separate fire button lives inside the card. The ▶ icon signals readiness; the card affordance (pointer cursor, hover/active styling) signals clickability.



4. **§3.4 — Reject reason color: rule gold → warning amber.** "rendered in rule/message gold (`--rule: #FBBF24`)" → "rendered in warning amber (`--warn: #FDE047`)." The `--rule` token belongs to DSL rule construct identity (displaying rule keywords/names in code surfaces), not to verdict output. A reject reason is a verdict signal; `--warn: #FDE047` is the correct verdict color for a business rejection. This also aligns §3.4 prose with UXR-14, which already correctly used `--warn`.



**Decision record:** `.squad/decisions/inbox/elaine-31-corrections.md`



**Key learnings:**



- **The canonical HTML is the single ground truth.** The manifest and notes describe intent; the HTML implements the contract. When I find a discrepancy, the HTML wins. The manifest says "dialog"; the HTML shows what the dialog looks like. Always read the HTML before specifying implementation details.

- **Construct colors and verdict colors have non-overlapping domains.** `--rule` is for displaying Rule constructs in code. `--warn`/`--violated`/`--valid` are for runtime verdict output. Mixing these is a visible error — a gold reject reason text looks like a code annotation, not a verdict. Hold the domain boundaries firmly.

- **Disabled token sets are the canonical disabling treatment.** Using `opacity: 0.5` on a construct color is not equivalent to the `support-disabled` token set. The disabled tokens produce a neutral muted gray-blue, not a faded version of the construct color. The distinction matters: a faded cyan event name still reads as an "event" at low contrast; the disabled token reads as "system-disabled."

- **"Fire button" was a design fiction.** The canonical model has no separate fire button anywhere in the card or the inline expansion — because there is no inline expansion. The button that fires is the event card itself (no-args) or the dialog OK button (with-args). This simplifies the affordance model significantly and removes the question of "what color should the fire button be" — it's always the event color when active.



---



## Archive Batch — 2026-05-12T13:52:04Z



---



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



## Archive Batch — 2026-05-12T18:59:32Z



Archived from `history.md` during the 15 KB gate summarization pass.



### 2026-05-12T04:29:05Z — Diagnostic audit and location tags merged



- The diagnostic audit and location-tag revision were folded into the canonical decision ledger.



- George's implementation commit closed the message-fix loop that the audit identified.







### 2026-05-11T01:38:51Z — Terminal-state diagnostic UX recommendation shipped



- Elaine's Message A / Message B split is now the durable team decision: warn structural sinks independently, and only emit no-path-to-terminal warnings after at least one terminal state exists.



- George implemented the design as `StructuralSinkState` (C119) plus gated `DeadEndState` (C108), so future UX follow-ups should treat Elaine's wording and timing guidance as the canonical rationale.







### 2026-05-09T15:26:09Z — Data-family anchor removal recorded



- Scribe merged Elaine's slate audit, literal-safety check, and final anchor-drop implementation into the canonical ledger.



- Durable outcome: the Data family is now the four-token semantic grouping `--data-t` / `--data-v` / `--field` / `--arg`, with no surviving `--data` anchor dependency.



















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



### Historical summary through 2026-05-09



- Earlier 2026-05-06 through 2026-05-09 execution detail was compacted into `history-archive.md` during the 2026-05-12 proof-diagnostic UX closeout so live history stays under the 15 KB gate.

- Durable outcomes retained from that span: the runtime-API accuracy pass, OQ-1/OQ-3/OQ-5/OQ-6/OQ-8/OQ-9 closures for event interaction UX, `UnhandledEvent` naming, the `<-` computed-field delimiter ruling, and the field / arg companion-color design sequence through the standalone-companion recommendation.

