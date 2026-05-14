# Core Context

- Owns UX/design across Data Form, Event Timeline, state-diagram, and author-facing product language.
- Keeps product wording plain, confident, and faithful to runtime truth rather than implementation jargon.
- Canonical surface names matter: `Data Form` and `Event Timeline` are the durable labels; legacy labels are errors.
- Visual-system HTML is the implementation ground truth when manifest prose and older working docs drift.
- Precept serves human DSL authors directly; AI help is optional, first-class, and never the required authoring mode.

## Learnings

- Full diagnostic review (132 diagnostics) found five systemic naming families: (1) graph-theory jargon in names/messages (`dominate`, `back-edge`, `sink`), (2) `Unproved*` proof-engine prefix leaking into 5 names, (3) CI enforcement family encoding operators as name fragments (`TildeEquals`) instead of the failure condition, (4) collection-safety guard names describing the remedy (`IndexBoundsGuard`) instead of the failure (`UnguardedIndexAccess`), (5) terse business-domain messages with no catalog examples (PRE0075, PRE0077).
- Worst single diagnostic: PRE0111 `RequiredStateDoesNotDominateTerminal` — "dominate" is graph-analysis vocabulary that no DSL author will recognize; flagged 🔴 on both name and message.
- Naming convention bar established: diagnostic names must be readable by someone who has only read the Precept language reference — no compiler-theory vocabulary allowed.
- The `Unguarded*` style (PRE0063/PRE0064) is the correct template for collection-safety guard diagnostics; later additions (PRE0099–PRE0101) deviated and should be normalized.
- CI enforcement family (5 diagnostics) should describe failure conditions, not the fix; `CaseSensitive*On*Field` is the approved pattern going forward.
- Messages that omit the value's own qualifier when reporting a qualifier mismatch (PRE0068) are below bar — both sides of a mismatch must be named.
- "Cannot be empty here" and "not accessible here" are message anti-patterns — "here" contributes nothing without context about what scope or construct is involved.

- Interval display needs a notation that's compact AND unambiguous in markdown: `[lo .. hi]` with two dots avoids confusion with Precept's range syntax and renders cleanly in VS Code hover.
- When extending the hover badge vocabulary, check existing badges before proposing new ones — `⚖️` (comparison contract) already covers declared bounds, and `🔬` already covers arithmetic reasoning. No new icon was needed for intervals.
- The proof status and the runtime fallback are different things. Unbounded fields must show `⚠️ Gap` even when a runtime check exists — hover cards communicate static guarantees, not safety nets.
- Repair hints belong on line 3 of the compact card, not gated behind expansion. The most common hover use case is "what do I do?" — that answer must be visible without extra interaction.

- Hover docs need a quick-reference table first so implementers can find the construct they care about before reading prose.
- Meaning-first hover copy works best when the authored `because` text leads and proof detail stays compact.
- Field/arg coloring is a semantic split: fields read as structure identity, args as event-scoped behaviour; docs must not drift back to the retired unified data-name lane.
- Construct colors, verdict colors, and disabled-surface colors are separate systems and should never be blended.
- Field-state diagnostics need Problems-panel copy that names the field, the relevant state or state change, and the repair action in plain DSL terms; compiler shorthand like `omit→non-omit` is not shippable user text.
- Diagnostic IDs should use subject-first, plain-English condition names; `MustSetOmitToNonOmit` is a naming smell because it encodes compiler shorthand instead of the user-visible failure.

## Historical Summary

- 2026-05-07 through early 2026-05-12 locked the current visual-system posture: `<-` for computed fields, Data-family `--field` / `--arg` expansion, typed-value tone stability, and hover-doc organization centered on rendered examples first.
- The older unified `--data` anchor is retired history; current design truth lives in the field/arg split and in visual-system HTML when prose lags.

## Recent Updates


### 2026-05-14T03:16:39Z — Elaine's approved diagnostic naming rules were synced into the enforcement plan

- Frank's full Elaine sync pass updated roughly 40 references across eight `docs/Working/diagnostic-enforcement.md` sections so the enforcement plan now uses the adopted `*WithoutWhen`, `CaseMismatchOn*`, and condition-first proof-name families plus the related one-off renames.
- Test method names in the slice plans were updated to match the renamed diagnostics, and PRE0019 was intentionally left untouched because the rename question had already moved into the separate retirement decision.
### 2026-05-13T18:17:15Z — Interval hover display design authored

- Designed `docs/working/interval-hover-design.md`: 6 template variations (proven field, gap field, proven expression, overflow-risk expression, unbounded field, optional field), interval notation (`[lo .. hi]`), propagation chain display, compactness rules, and gap handling.
- Confirmed no new badge icons are needed: `🔬` covers interval arithmetic, `⚖️` extends naturally to declared bounds as a comparison contract.
- Key design ruling: unbounded fields show `⚠️ Gap` not `⚡ Enforced`, even when a runtime fallback exists — hover communicates proof status, not safety nets.
- Expanded propagation chain view deferred to V2 (requires solver to expose intermediate intervals).
- Filed decisions to `.squad/decisions/inbox/elaine-interval-hover.md`.



### 2026-05-13T00:46:00Z — Omit anti-pattern prose sharpened for SyntaxReference

- Elaine rewrote the omit anti-pattern guidance to reject sentinel defaults like `default 0`, `default false`, and `default ""` when a field is not meaningful yet.
- The durable guidance is to use `omit` in every state without business meaning, then add `set Field = ...` on the transition where the field becomes present; `default` stays reserved for real business defaults.

### 2026-05-13T00:45:00Z — Field-state diagnostic naming v2 was adopted

- Elaine tightened the field-state family to the adopted compact names `OmittedFieldReadInState`, `OmittedFieldSetInTargetState`, and `RequiredFieldUnassignedOnEntry`.
- The approved set preserves the existing catalog house style while removing the sentence-like feel of the earlier normalization pass.

### 2026-05-13T00:32:50Z — Field-state diagnostic UX review locked the user-facing naming bar

- Elaine reviewed the canonicalized `docs/Working/field-state-guarantees-v3.md` surface and flagged code drift: earlier team notes used provisional D131/D133/D135, but the v3 doc now canonically uses D130/D131/D132.
- She judged D130 and D131 shippable in concept, rejected `MustSetOmitToNonOmit` as compiler shorthand, and proposed direct Problems-panel copy that names the field first, the state(s) second, and the repair action explicitly.
- In a follow-up naming-normalization proposal, she argued the catalog family should move toward subject-first names like `FieldOmittedInStateCannotBeRead`, `FieldOmittedInTargetStateCannotBeSet`, and `RequiredFieldNeedsAssignmentWhenBecomingPresent`, while leaving D42/D43 alone.

### 2026-05-12T22:25:28Z — B4 as-built hover doc sync recorded

- Updated `docs/Working/hover-design.md` so the working hover spec now records the shipped B4 state-proof narrative as-built, including the `📍`, `✅ Proven`, and `⚠️ Gap` badge vocabulary.
- Locked the doc boundary that B4 appends to the rich state hover card and does not ship as a standalone hover kind.

### 2026-05-12T13:52:04Z — Hover color and docs alignment pass recorded

- Confirmed the current field/arg split is the intended design direction and that the remaining gap is documentation drift, not a request to revert implementation.
- Prioritized typed-literal semantic consistency and explicit builtin-function coloring over any rollback toward the retired unified slate model.

### 2026-05-12T18:01:17.648-04:00 — Hover Q1/Q2/Q3 resolved in V6

- Locked three implementation-facing hover decisions in `docs/Working/hover-design.md`: suppress qualifier/use counts in V1, wrap long guards instead of truncating them, and inline PRE codes on rule/ensure violation cards.
- Section 6 no longer carries open questions; Elaine’s hover-design revision is complete and ready for implementation consumption.

### 2026-05-13T22:33:16.752-04:00 — Diagnostic review revised for `when` vocabulary and Frank conditions

- Verified `docs/language/precept-language-spec.md` exposes `When` / `when` as the author-facing control keyword; the spec may describe it as a "Guard clause," but the DSL author writes `when`, so `when` is the right replacement for `guard` in naming.
- Revised `docs/Working/diagnostic-name-message-review.md` so the collection-safety family now uses `*WithoutWhen` proposals: `CollectionAccessWithoutWhen`, `CollectionMutationWithoutWhen`, `KeyAccessWithoutWhen`, `IndexAccessWithoutWhen`, and `DuplicateKeyAddWithoutWhen`.
- Incorporated Frank's conditions into the review: proof-family renames now use condition-first names (`ModifierNotGuaranteed`, `DimensionQualifierMissing`, `QualifiersMayBeIncompatible`, `InitialStateConstraintUnsatisfied`, `FieldMayBeAbsent`), the CI family now uses `CaseMismatchOn*`, PRE0019 is explicitly blocked pending an emission-site audit, and the Patterns section now carries Frank's AI-parseable-message and metadata-sync conventions plus a note about his tiered priority ordering.



