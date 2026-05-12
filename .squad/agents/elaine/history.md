# Core Context







- Owns UX/design across Data Form, Event Timeline, state-diagram, and author-facing product language.



- Keeps product wording plain, confident, and faithful to runtime truth rather than implementation jargon.



- Canonical surface names matter: `Data Form` and `Event Timeline` are the durable labels; legacy names like `Inspector` and `event landscape` are errors.



- Visual-system HTML is the implementation ground truth when manifest prose and older working docs drift.



- 2026-05-07/05-09 visual-system work locked the computed-field delimiter `<-` and the Data-family `--field`/`--arg` expansion; the retired `--data` anchor and the standalone companion-token detour are historical only.



- Precept serves human DSL authors directly; AI assistance is optional, first-class, and never the product's required authoring mode.







## Learnings



- `<unknown>` in diagnostics is a UX failure mode, not a fallback. When the proof engine can't resolve an operand name, the diagnostic should fall back to source-text excerpts or honest "cannot identify" language — never identical placeholders that give zero signal.

- Diagnostic messages serve two audiences (human developers and AI agents) with different needs: humans need natural-language fix direction appended to the message; AI agents need structured `Args` arrays with individually addressable values, not sentence fragments they have to regex-parse.

- Qualifier compatibility diagnostics should show the *actual conflicting values* (e.g., `kg` vs `m`), not just say "incompatible." The delta between "what's wrong" and "what to fix" is often just including the concrete values.



- The five-construct color model(Structure, State, Event, Data, Rule) has an implicit two-axis substructure: structural identity (indigo–violet) vs. behavioural identity (cyan). Fields and args are the named companions that complete each axis.



- Fields sitting under Data-slate (#B0BEC5) buries structural declarations in the same tone as types and values. Promoting fields to the hero identifier color (#A5B4FC) is semantically correct: field names define entity shape, not incidental data.



- The tonal relationship "anchor → lifted companion" (e.g., #6366F1 → #A5B4FC for structure) can be mirrored on other axes. For events, #30B8E8 → #9AD8E8 gives args the same subordinate-but-related read.



- Moving field and arg names out of the Data construct narrows Data to types and values only. This has cross-surface implications (Data Form, Event Timeline) that need explicit scoping.



- The 1–3 shade paradigm governs intra-construct tonal gradients. Companion tokens (field, arg) are inter-construct axis relationships — orthogonal to the shade model. No paradigm change needed; just name the pattern.



- When a family is defined by hue coherence (all tones in the same hue band), adding tokens from a different hue breaks the family's visual identity — even if the semantic grouping makes sense. Organizational home must not contradict visual signal.



- When the hue proximity between two tokens already communicates their relationship, naming the grouping adds overhead without new information. Let the color do the work; document the intent with a brief note, not a structural layer.



- Shane's override: families are semantic categories, not hue bands. When tokens share a semantic category (fields and args ARE data), they belong in that family even if their hues differ. The family name defines the grouping; distinct hues within a family signal relationships to neighboring families' concepts (field→structure, arg→event). The spec's family definition was the bottleneck, not the organizational placement.



- Slate hue audit (2026-05-09): all 3 Data-family slates are wired into the VS Code TextMate theme. But `--data` (#B0BEC5) only colors field-name and arg-param scopes — both of which should migrate to `--field`/`--arg`. After migration, the anchor has zero consumers. `--data-t` (types, 2 scopes) and `--data-v` (values, 6 scopes) are genuinely active. Recommendation: drop the anchor, simplify to 2 slates + field + arg = 4-token family.



- Literal safety check (2026-05-09): confirmed that all 6 literal/value TextMate scopes are hardcoded to `#84929F` (`--data-v`). No literal scope references the anchor `--data` (#B0BEC5). The `--data-v` CSS custom property is defined as a standalone hex value, not derived from `--data` — no CSS fallback chain exists. Dropping the anchor is safe; literals will not lose color. The ~25 `var(--data)` references in the spec HTML are all for field names and UI elements, not for literals — they need a follow-on cleanup pass to `var(--field)`.



- Failure-point vocabulary matters most; result names and explanations must stay in the caller's mental model.



- The `because` clause is usually the primary user explanation; raw constraint or guard syntax is secondary detail.



- Structural impossibility should delete UI states, not complicate them. If the runtime makes a combination unreachable, the UX should not design for it.



- Construct colors and runtime verdict colors are different systems and must not be mixed.



- Disabled UI uses the semantic disabled token set, not faded versions of construct colors.



- Completion UX must stay semantically honest: if a qualifier axis is invalid for the selected type, suppress value completions and guide the user back to the correct preposition instead.



- For closed-set qualifier vocabularies, omission hurts trust faster than list length; a filtered ~150-item UOM list is acceptable if it means expected real-world units are present.

- Sample accuracy matters at the grammar-slot level: a single stale `when` position teaches the wrong mental model even when the surrounding workflow is otherwise sound.

- The 2026-05-10 sample audit found stale post-verb guard examples in `samples/insurance-claim.precept` and `samples/loan-application.precept`; I moved StateEnsure, EventEnsure, and AccessMode examples to pre-verb `when`, and added a minimal guarded StateAction example in `samples/event-registration.precept` so all four guard-bearing construct families now have at least one positive sample.

- The current compile/diagnostic path on this branch still rejects the corrected pre-verb sample syntax, so the user-facing samples now match the design decision while parser/tooling parity still needs follow-through elsewhere.

- Typed literal completion should behave like a type-owned mini-mode: once the caret is inside `'...'`, the menu belongs to the literal slot, never to outer grammar constructs.

- Wrong completions are worse than empty state inside a typed literal; if expected type or slot cannot be resolved, suppress the list rather than leaking `field`, `state`, functions, or general expression items.

- Structured literals need phase-specific help: numeric-entry phases stay quiet, but the first separator space should pivot instantly into suffix vocabularies such as temporal units or currency codes.

- Free-form literal types should stay lightweight. Text and plain numeric literals benefit more from reused local values and format examples on demand than from aggressive auto-popup behavior.

- Empty typed literals and partial typed literals should feel different: empty state teaches the format with full examples, while partial state narrows to the exact segment vocabulary the user is finishing.



- Qualifier-aware completion is a reusable design pattern: whenever a quoted scalar slot carries `in` or `of`, candidate lists hard-filter to the declared value(s) before any type-specific ranking.

- Compound temporal is a V1 requirement, not a follow-on polish pass: temporal completion must preserve a visible continuation path for `+ <number> <temporal unit>` instead of treating the first unit as terminal.



- Proof hover is currently a routing problem as much as a content problem: generic operator hover and generic transition hover often win before the user ever sees proof context, so flagship proof UX requires precedence changes, not just better copy.
- Qualifier UX needs four separate data points, not one blended sentence: declared syntax, resolved value, source of resolution, and current proof status. If any one of those is missing, users cannot tell whether they authored the wrong thing or the engine simply failed to prove it.
- Proof hover really breaks into three different UX jobs: declaration contract hover, expression proof hover, and diagnostic squiggle hover. Trying to make one generic card do all three produces muddy blame and weak repair guidance.
- A working design doc for Shane sign-off has to carry routing rules and data-shape requirements, not just example copy. Hover UX lives or dies on precedence and available proof evidence.

## Recent Updates

### 2026-05-12T13:52:04Z — Proof diagnostic UX audit is now a shipped team baseline
- Elaine's audit locked the user-facing bar: proof diagnostics must show real operand / qualifier values, keep structured payloads for AI consumers, and pair human repair guidance with honest uncertainty rather than `<unknown>` placeholders.
- George's Section A implementation pass closed the first message batch in commit `1d8962f7`, so the audit is now both a design rationale and a shipped wording baseline.

### 2026-05-12T13:52:04Z — Proof hover design is ready for Shane sign-off
- `docs/working/proof-hover-design.md` is now the canonical hover design surface; it covers precedence/routing, scenario-specific content, and the proof-evidence data shape Kramer will need before implementation.
- Durable rule: declaration-contract hover, expression-proof hover, and diagnostic-squiggle hover are separate UX jobs and should not be collapsed into one generic card.



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

