# Unified Modifier Taxonomy — Comprehensive Candidate Inventory

**Date:** 2026-04-12  
**Author:** Frank (Lead/Architect & Language Designer)  
**Status:** Modifier candidate inventory for Shane's decision-making  
**Sources synthesized:**
- [structural-lifecycle-modifiers.md](./structural-lifecycle-modifiers.md) — #58 research, full modifier taxonomy, tier system, comparable system survey
- [verdict-semantic-reframing.md](./verdict-semantic-reframing.md) — semantic model endorsement, soft diagnostics, unification with #58
- [verdict-modifier-design-options.md](./verdict-modifier-design-options.md) — original 3-tier design options (partially superseded)
- [verdict-modifier-runtime-enforceability.md](./verdict-modifier-runtime-enforceability.md) — George's Model D, philosophy reframing, engine partitioning
- [verdict-modifier-ux-perspective.md](./verdict-modifier-ux-perspective.md) — Elaine's UX analysis, two-layer model
- [verdict-modifier-roadmap-positioning.md](./verdict-modifier-roadmap-positioning.md) — Steinbrenner's milestone options
- [verdict-modifiers.md](./verdict-modifiers.md) — background 7-system external survey

---

## Preamble

### What this document IS

A **comprehensive modifier candidate inventory** organized for decision-making. Every candidate from the #58 research and the verdict research is included. Candidates are grouped by mechanism category, then by entity type, and ranked within each group from most practical and well-grounded to most speculative and complex. The per-candidate entries present factual analysis — what each modifier does, how it's checked, what systems have it, what it depends on, and how complex it is to implement.

### What this document is NOT

A recommendation. This document does not endorse, reject, defer, bundle, or sequence any modifier candidate. It does not suggest shipping sets or wave order. Those decisions belong to Shane.

### Shane's decisions to date

These are locked. The inventory respects all of them:

1. **Proceed** — verdict modifiers are worth pursuing beyond research.
2. **All tiers** — events, rules, AND states. No arbitrary scope reduction.
3. **Non-blocking warnings** — if we do rule severity, warning-level rules should NOT block. Not negotiable.
4. **State verdict as differentiator** — the fact that no comparable system does this is an opportunity. Explore novel territory.
5. **Semantic reframing endorsed** — verdict modifiers on states and events are semantic annotations (meaning declarations), not outcome enforcement.
6. **Philosophy change accepted** — Shane is "very open to the philosophy change suggested in option D runtime analysis." Error-level rules PREVENTED; warning-level rules DETECTED and reported.

### Key insight driving this inventory

The modifier research converged on a **three-mechanism taxonomy**. All modifiers share the same grammatical position (after the declaration name) but differ in enforcement profile. This taxonomy organizes the entire design space.

---

## Section 1: The Unified Modifier Taxonomy

### Three mechanism types

Every modifier in Precept falls into exactly one of three categories:

| Mechanism | What it does | Enforcement | Primary value | Examples |
|-----------|-------------|-------------|---------------|----------|
| **Structural** | Compile-time provable constraint on graph topology, row structure, or mutation pattern | Hard — compiler error on violation | Prevention | `initial`, `terminal`, `entry`, `advancing`, `settling`, `writeonce`, `sealed after` |
| **Semantic** | Intent declaration + tooling directive on lifecycle significance | Soft — consistency warnings, no blocking | Readability + tooling | `success`, `error`, `warning` on states and events |
| **Severity** | Enforcement-tier marker on constraint rules | Behavioral — changes whether violation blocks | Graduated enforcement | `warning` on `invariant`/`assert` |

### How they relate to the five modifier roles

The [structural-lifecycle-modifiers research](./structural-lifecycle-modifiers.md) established five modifier roles:

| Role | Structural | Semantic | Severity |
|------|-----------|----------|----------|
| 1. Structural constraint | **Primary** | — | — |
| 2. Intent declaration | Secondary | **Primary** | Secondary |
| 3. Tooling directive | Secondary | **Primary** | Secondary |
| 4. Analysis enabler | Yes | Partial (soft checks) | Yes (new outcome values) |
| 5. Feature gate | Some (`derived`) | — | — |

**Structural modifiers** serve role #1 first and gain roles #2–#4 as consequences. A `terminal` state is constrained (no outgoing transitions), communicates intent, enables diagram rendering, and enables stronger C50 messaging.

**Semantic modifiers** serve roles #2 and #3 first. They don't constrain what the author can write — they label what the author *means*. Soft consistency checks (role #4) are a bonus, not the justification.

**Severity declarations** serve a unique role: they change the engine's *response* to constraint evaluation. They don't constrain structure or declare intent — they partition enforcement behavior.

### Shared grammar position, distinct mechanisms

All three types share the modifier position:

```precept
state Approved terminal          # structural: no outgoing transitions
state Approved success           # semantic: positive lifecycle endpoint
state Approved terminal success  # both: structural boundary + semantic significance

event Submit entry               # structural: fires only from initial state
event Submit success             # semantic: positive lifecycle trajectory

invariant X because "Y"          # severity: error (default — blocks)
invariant X because "Y" warning  # severity: warning (does not block)
```

The grammar position is unified. The mechanisms behind them are distinct and independent. This is by design — it gives authors a consistent syntax while maintaining clear enforcement boundaries.

### Why `warning` in two places is not a conflict

The word `warning` appears in both semantic and severity contexts:

- `state OnHold warning` — **semantic**: "being in OnHold means something needs attention"
- `invariant X because "Y" warning` — **severity**: "violating this rule flags but does not block"

These are different mechanisms despite sharing a word. The disambiguation is the **declaration context**: `warning` after a state or event name is semantic; `warning` after a `because` clause is severity. No syntactic ambiguity exists — the parser knows which is which from the production rule.

This is the same pattern as `in` — which means "scope to state" in `in Approved assert X` and "element membership" in `MissingDocuments contains X`. Same word, different production, no ambiguity.

---

## Section 2: The Philosophy Evolution

### Current philosophy

`docs/philosophy.md` states:

> **Prevention, not detection.** Invalid entity configurations — combinations of lifecycle position and field values that violate declared rules — cannot exist. They are structurally prevented before any change is committed, not caught after the fact.

### Proposed update

> **Prevention and detection.** Precept constraints divide into two enforcement tiers. **Error-level rules** (the default) are structurally prevented — invalid configurations involving error-rule violations cannot exist. **Warning-level rules** (author opt-in) are structurally detected — the engine evaluates them on every operation, reports violations in the result, and surfaces them through inspection. The author declares the boundary between what must be blocked and what must be flagged. The `.precept` file specifies both.

### Why this strengthens the contract

1. **One-file completeness improves.** Today, advisory business rules that shouldn't block live OUTSIDE the `.precept` file — in service code, logging, separate validators. With warning-level rules, those rules move INTO the file. The file becomes more complete.

2. **The engine still evaluates every rule on every operation.** Warning rules aren't skipped — they are evaluated and reported. Nothing is hidden.

3. **The author explicitly declares the severity boundary.** The default is `error` — every rule blocks unless deliberately opted to `warning`. The `.precept` file now declares not just WHAT rules exist, but WHICH are critical vs. advisory. Strictly more information.

4. **Determinism preserved.** Same definition, same data → same outcome, same warnings.

5. **Inspectability preserved.** MCP `inspect` reports warning constraints alongside error constraints.

### Precedent

- **BPMN:** Error events terminate; escalation events continue. Both evaluated. Consequence differs.
- **Kubernetes:** `Deny` blocks; `Warn` allows with header. Both evaluated by CEL. Consequence differs.
- **ESLint:** `error` exits 1; `warn` exits 0. Both evaluate the same rule. Consequence differs.

### Dependency note

The philosophy evolution is a prerequisite for the severity mechanism (Section 3.3) only. Structural and semantic modifiers (Sections 3.1 and 3.2) do not require any change to `docs/philosophy.md`.

**⚠️ Per copilot-instructions.md, `docs/philosophy.md` is not edited until Shane explicitly approves.**

---

## Section 3: Modifier Candidates by Category

Candidates are grouped first by mechanism (Structural → Semantic → Severity), then by entity type (States → Events → Fields → Rules), then ranked within each group from most practical to most speculative.

### Summary

| # | Modifier | Entity | Mechanism | What it means | Compile check | Feasibility &amp; dependencies |
|---|----------|--------|-----------|---------------|---------------|-------------------------------|
| | **Structural — States** | | | | | |
| 1 | [`terminal`](#terminal) | State | Structural | Marks a final resting state — the entity's lifecycle is over; no event may trigger an outgoing transition | Hard — error on outgoing `transition` | Simple — single diagnostic, row scan |
| 2 | [`guarded`](#guarded) | State | Structural | Every transition INTO this state must pass a `when` guard; prevents unconditional arrivals | Hard — error on unguarded incoming row | Simple — incoming-row scan for `when` |
| 3 | [`required` / `milestone`](#required--milestone) | State | Structural | Every possible path from initial to terminal MUST pass through this state; guarantees the entity always reaches this stage | Hard — dominator analysis | Moderate — dominator analysis ~90 LOC on existing graph+BFS; guard false positives are design question, not implementation barrier; enhanced by `terminal` |
| 4 | [`forward`](#forward) | State | Structural | One-way door — once the entity leaves, no transition may target this state again; prevents loops and backtracking | Hard — BFS from successors, check unreachability | Simple — one BFS per marked state on existing graph; ~15 LOC |
| 5 | [`gate`](#gate) | State | Structural | Section closure — entering permanently shuts the door on ALL prior states, not just this one; like crossing a phase boundary | Hard — ancestor BFS + transition target scan | Moderate — two BFS passes per gate state on existing graph; ~40–60 LOC; "before" ambiguity in branching graphs is a design question |
| 6 | [`transient`](#transient) | State | Structural | Pass-through state — the entity cannot stay here permanently; at least one event must produce a successful outgoing transition | Hard — error if no outgoing success | Simple — outgoing-row scan |
| 7 | [`absorbing`](#absorbing) | State | Structural | Events fire here but the entity stays put — like `terminal` but still responsive; can update fields without ever leaving | Hard — error on any `transition` outcome | Simple — outcome scan on outgoing rows |
| 8 | [`convergent`](#convergent) | State | Structural | Junction point — multiple different lifecycle paths converge here; reachable from two or more distinct source states | Hard — count incoming source states | Simple — incoming-edge count |
| 9 | [`ordered before <State>`](#ordered-before-state) | State | Structural | Enforces sequencing between two states — if both are visited on a path, this one must come first | Hard — BFS from named state, check unreachability | Simple — one BFS on existing graph; ~15 LOC; niche use case |
| 10 | [`oneshot`](#oneshot) | State | Structural | Must be visited AND can only be visited once — combines `required` (must pass through) with `forward` (can't return) | Hard — combined dominator + BFS | Moderate — reuses milestone + forward infrastructure; compositional |
| 11 | [`bottleneck`](#bottleneck) | State | Structural | Critical path node — if removed, some terminal states become unreachable from initial; weaker than `required` | Hard — dominator analysis per terminal | Moderate — same infrastructure as `milestone`; weaker guarantee |
| 12 | [`depth <N>`](#depth-n) | State | Structural | Fixed-depth positioning — this state sits exactly N transitions from initial on every path that reaches it | Hard — path enumeration | Moderate — practical only with `linear`; narrow satisfiability |
| 51 | [`exclusive`](#exclusive) | State | Structural | Mutual exclusion — for any given data in this state, at most one event's guards can evaluate to true; prevents branching ambiguity | Partial — guard satisfiability analysis | Low — very limited static provability; honest partial |
| 52 | [`dormant`](#dormant) | State | Structural | Quiet state — only one event advances the lifecycle; everything else just updates fields in place while the entity parks here | Hard — advancing event count from state | Simple — count non-settling, non-reject events from state |
| 53 | [`passthrough`](#passthrough) | State | Structural | No loitering — every event either moves the entity to another state or rejects; nothing just updates fields in place | Hard — error on `no transition` success outcome | Simple — outcome scan on state's rows |
| 54 | [`readonly` (state)](#readonly-state) | State | Structural | Frozen data — no event can modify any field while the entity is in this state; events still fire (transition/reject) but data stays untouched | Hard — error on any `set`/`add`/`remove` | Simple — mutation scan on state's outgoing rows |
| 55 | [`reactive`](#reactive) | State | Structural | State has side-effect hooks — code runs automatically when the entity enters (`to`) or exits (`from`) this state | Hard — error if no entry/exit actions | Simple — action declaration existence check |
| | **Structural — Events** | | | | | |
| 13 | [`entry`](#entry) | Event | Structural | Onboarding event — can only fire from the initial state; this is how entities enter the lifecycle (Submit, Create, Register) | Hard — error on non-initial source | Simple — source-state scan |
| 14 | [`advancing`](#advancing) | Event | Structural | Always moves forward — every successful firing changes the entity's state; no in-place updates, purely drives progression | Hard — error on `no transition` outcome | Simple — outcome scan on rows |
| 15 | [`settling`](#settling) | Event | Structural | In-place update only — fires without changing state; used for data collection and field updates while the entity stays put | Hard — error on `transition` outcome | Simple — outcome scan on rows |
| 16 | [`isolated`](#isolated) | Event | Structural | Single-source event — only valid in one specific lifecycle state; tightly coupled to a particular stage | Hard — error on 2+ source states | Simple — source-state count |
| 17 | [`completing`](#completing) | Event | Structural | End-game event — every transition targets a `terminal` state; firing this always ends the lifecycle | Hard — error on non-terminal target | Simple — target-state check; requires `terminal` |
| 18 | [`guarded` (event)](#guarded-event) | Event | Structural | No unconditional paths — every transition row for this event requires a `when` guard; behavior always depends on data | Hard — error on unguarded row | Simple — row scan for `when`; design question on reject-row exemption |
| 19 | [`total`](#total) | Event | Structural | Never purely rejected — wherever this event fires, at least one non-reject row exists; the event can succeed in every handling state | Hard — inversion of C51 | Simple — extends existing C51 analysis |
| 20 | [`universal`](#universal) | Event | Structural | Available everywhere — this event is handled in every non-terminal reachable state; think: Cancel, which should be possible at any point | Hard — error on missing state | Moderate — requires reachability set from BFS |
| 21 | [`irreversible`](#irreversible) | Event | Structural | Point of no return — after this event fires, no path exists from the target state back to the source; permanently one-way | Hard — graph reverse-reachability | Moderate — reverse BFS per transition target |
| 22 | [`symmetric`](#symmetric) | Event | Structural | Round-trip possible — for every state this event transitions to, a path back to the source exists somewhere in the graph | Hard — graph adjacency check | Moderate — reverse-edge existence check per target |
| 56 | [`fallback`](#fallback) | Event | Structural | Safety net — wherever this event fires, exactly one unguarded row catches anything the guards miss; guarantees a default outcome | Hard — unguarded row count per group | Simple — row scan per (state,event) group |
| 57 | [`disjoint`](#disjoint) | Event | Structural | No ambiguity — guard conditions on this event's rows are mutually exclusive; for any given data, at most one guard passes | Partial — expression analysis | Moderate — feasible for simple boolean/enum guards; hard for arithmetic |
| 58 | [`paired with <Event>`](#paired-with-event) | Event | Structural | Sibling events — fires from exactly the same set of states as the named event; they always appear as alternatives (Approve paired with Deny) | Hard — source state set comparison | Simple — compare source states of both events |
| 59 | [`after <Event>`](#after-event) | Event | Structural | Sequenced — can only fire from states that the named event transitions into; establishes structural ordering between events | Hard — BFS + source comparison | Moderate — compute reachable targets of predecessor event |
| 60 | [`covered`](#covered) | Event | Structural | Complete coverage — the combination of guards and fallback rows handles every possible data scenario; no input falls through unhandled | Partial — guard completeness analysis | Moderate — feasible with unguarded fallback check; full coverage is harder |
| 61 | [`resetting`](#resetting) | Event | Structural | Rewind — sets fields back to their declared `default` values; used for retry loops, error recovery, or cleanup patterns | Hard — compare set values to defaults | Simple — match each `set` RHS against declared field default |
| 62 | [`intake`](#intake) | Event | Structural | Data ingestion event — fires from initial and populates multiple fields from event arguments; the entity's onboarding moment | Hard — combined `entry` check + arg-sourced mutation count | Simple — compositional: `entry` + multi-field arg writes |
| 63 | [`idempotent`](#idempotent) | Event | Structural | Safe to retry — firing multiple times produces the same outcome as firing once; provable for constant assignments like `set X = true` | Partial — provable for constant/flag assignments | Moderate — `set X = true`, `set X = constant` provable; general case undecidable |
| 64 | [`global` / `broadcast`](#global--broadcast) | Event | Structural | State-agnostic — every row uses `from any`; not tied to a specific lifecycle position; fires regardless of current state | Hard — error on non-`from any` rows | Simple — source scan on all rows *(consolidated: Frank `broadcast` + George `global`)* |
| 65 | [`noop` / `transparent`](#noop--transparent) | Event | Structural | Side-effect free — fires without changing any field value; pure state-machine movement or a diagnostic no-op | Hard — error on any `set`/`add`/`remove` | Simple — mutation scan on all rows |
| 66 | [`stackable`](#stackable) | Event | Structural | Can accumulate — every handling state includes a `no transition` path; the event can fire repeatedly in place before advancing | Hard — per-state no-transition row check | Simple — outcome scan per handling state |
| 67 | [`preserves <Field>`](#preserves-field) | Event | Structural | Hands-off guarantee — this event never modifies the named field regardless of which row fires or which guard matches | Hard — error on any mutation targeting field | Simple — targeted mutation scan on all rows |
| 68 | [`focused`](#focused) | Event | Structural | Single destination — no matter which guard matches, every transition row targets the same state; outcome is uniform | Hard — target state uniqueness check | Simple — collect distinct transition targets |
| 69 | [`narrowing`](#narrowing) | Event | Structural | Funneling — transitions lead to states with fewer available actions than the source; the entity's options decrease as it progresses | Partial — outgoing row count comparison | Moderate — compare outgoing event counts; definition of "fewer options" needs design work |
| 70 | [`deterministic` (event)](#deterministic-event) | Event | Structural | Predictable — each handling state has exactly one unguarded row; outcome is entirely determined by current state, not data | Hard — per-state row count + guard check | Simple — count rows per handling state, verify single unguarded |
| 71 | [`unguarded`](#unguarded) | Event | Structural | Unconditional — no `when` clauses anywhere on this event; always fires the same way regardless of entity data | Hard — error on any `when` clause | Simple — row scan; complement of `guarded` (event) |
| 72 | [`pure`](#pure) | Event | Structural | Truly side-effect free — no field mutations AND no entry/exit state actions trigger; stronger than `noop` which ignores action hooks | Hard — mutation scan + action check | Moderate — extends `noop` with entry/exit action analysis |
| 73 | [`initializes <Field>`](#initializes-field) | Event | Structural | Sole source of truth for a field — the only event that writes a meaningful value to the named field; clear write ownership | Hard — mutation source uniqueness per field | Simple — count events writing to field with non-default values |
| | **Structural — Fields** | | | | | |
| 23 | [`writeonce`](#writeonce) | Field | Structural | Set-and-forget — the field can be written exactly once; any second mutation from any event at any lifecycle stage is a compile error | Partial — row-partition analysis | Moderate — mutation-site enumeration across all rows |
| 24 | [`sealed after <State>`](#sealed-after-state) | Field | Structural | Frozen after a milestone — writable until the named state is reached, then permanently locked for the rest of the lifecycle | Hard — reachability + row analysis | Moderate — reachability from named state + mutation scan; enhanced by `terminal` |
| 25 | [`immutable`](#immutable) | Field | Structural | Born with its value — never written to by any event; its `default` declaration is its permanent value; stronger than `writeonce` | Hard — total mutation ban | Simple — scan all rows for any `set` targeting field |
| 26 | [`monotonic`](#monotonic) | Field | Structural | One direction only — numbers can only go up, collections can only grow; prevents value regression or accidental shrinkage | Full for collections, partial for scalars | Moderate — collection verb check is simple; scalar expression analysis is hard |
| 27 | [`identity`](#identity) | Field | Structural | Entity identifier — written once, never changed, excluded from `edit` declarations; tooling treats it as the primary key | Same as `writeonce` + `edit` exclusion | Moderate — superset of `writeonce` plus `edit` interaction |
| 28 | [`derived`](#derived) | Field | Structural | Calculated value — the field's value is derived from other fields, not set directly; any `set` or `edit` targeting it is an error | Hard — error on any `set`/`edit` targeting | Moderate — mutation ban is simple, but computed-value evaluation is a larger feature; deferred to #17 |
| 74 | [`flag`](#flag) | Field | Structural | One-way boolean switch — starts false, can only flip to true, never reverts; captures irreversible facts (DocumentsVerified, TermsAccepted) | Hard — type + default + mutation value check | Simple — verify boolean, default false, all `set` assign `true` |
| 75 | [`localized to <State>`](#localized-to-state) | Field | Structural | State-scoped writes — the field is only modified by events firing from the named state; mutations are geographically contained | Hard — mutation source state check | Simple — scan all rows mutating field, verify source state |
| 76 | [`required in <State>`](#required-in-state) | Field | Structural | Data gate — the field must have a meaningful value whenever the entity is in the named state; sugar for a state `assert` | Hard — equivalent to state assert | Simple — syntactic sugar for `in <State> assert Field != null` |
| 77 | [`capped <N>`](#capped-n) | Collection | Structural | Size limit — the collection can never exceed N elements; sugar for `invariant Collection.count <= N` | Hard — equivalent to invariant on count | Simple — sugar for `invariant Collection.count <= N` |
| 78 | [`drains before <State>`](#drains-before-state) | Collection | Structural | Must be drained — the collection must reach zero elements before the entity enters the named state; enforces processing completion | Hard — equivalent to state assert | Simple — sugar for `in <State> assert Collection.count == 0` |
| 79 | [`ephemeral`](#ephemeral) | Collection | Structural | Temporary workspace — the collection accumulates elements and is fully cleared during the lifecycle; not meant to persist permanently | Partial — reachability analysis for empty state | Moderate — needs path analysis for emptiness; tooling-actionable as intent |
| 80 | [`argbound`](#argbound) | Field | Structural | External input only — the field's value always comes directly from event arguments, never from expressions or other fields | Hard — mutation RHS analysis | Simple — verify all `set` RHS are event arg references |
| 81 | [`singlewriter`](#singlewriter) | Field | Structural | One event owns this field — exactly one event type across the entire lifecycle can write to it; clear mutation ownership | Hard — mutation event count | Simple — count distinct events with `set` targeting field |
| 82 | [`projection of <Field>`](#projection-of-field) | Field | Structural | Shadow field — every mutation's RHS references the named source field; the value is always a function of that one field | Partial — expression analysis | Moderate — verify all `set` RHS reference the named field |
| 83 | [`accumulates via <Event>`](#accumulates-via-event) | Field | Structural | Running total — a single event progressively builds this field's value through additive writes (e.g., TotalAmount with each AddLineItem) | Partial — `singlewriter` + expression pattern | Moderate — verify `singlewriter` + additive expression pattern |
| 84 | [`snapshot`](#snapshot) | Field | Structural | Frozen record — written once, and the value captures existing field state rather than incoming event args | Hard — `writeonce` + RHS source analysis | Moderate — extends `writeonce` with field-reference check |
| 85 | [`append only`](#append-only) | Collection | Structural | Grow-only collection — elements can be added but never individually removed; the collection can only expand | Hard — error on `remove` targeting collection | Simple — verb check; related to `monotonic` for collections |
| 86 | [`pinned`](#pinned) | Field | Structural | Assertion anchor — this field appears in state-level `assert` expressions; signals it's load-bearing for lifecycle correctness | Hard — assertion reference scan | Simple — scan state assert expressions for field reference |
| | **Structural — Precept** | | | | | |
| 29 | [`linear`](#linear) | Precept | Structural | No going back — the entire state graph is acyclic; every transition moves strictly forward and no state can be revisited | Hard — cycle detection on graph | Simple — standard DFS/BFS cycle detection on existing adjacency list; ~15 LOC |
| 30 | [`strict`](#strict) | Precept | Structural | Zero tolerance — every analysis warning becomes a hard compile error; no soft diagnostics slip through; the strictest compilation mode | Hard — diagnostic severity reclassification | Simple — flag on definition; analysis checks the flag when emitting diagnostics |
| 31 | [`governed`](#governed) | Precept | Structural | No freebies — every non-initial state requires guarded incoming transitions; the entity must satisfy conditions before entering any state | Hard — error on unguarded incoming row to any non-initial state | Simple — extends per-state `guarded` check to all states; incoming-row scan |
| 32 | [`exhaustive`](#exhaustive) | Precept | Structural | No dead ends — every non-terminal state has at least one event that can advance the lifecycle; the entity is never permanently stuck | Hard — upgrade of C50 to error | Simple — reuses existing C50 analysis; narrower than `strict` |
| 33 | [`single-entry`](#single-entry) | Precept | Structural | One way in — only one event type can kick off the lifecycle from the initial state; a single onboarding path | Hard — count events from initial | Simple — event count from initial state; hyphenated keyword is a grammar question |
| 34 | [`single-exit`](#single-exit) | Precept | Structural | One way out — the lifecycle converges to exactly one final state; all paths end at the same place regardless of route taken | Hard — count terminal states | Simple — terminal state count; enhanced by `terminal` |
| 35 | [`sequential`](#sequential) | Precept | Structural | Strict pipeline — states can only transition to the next one in declaration order (A→B→C→D); the most rigid possible flow | Hard — target vs declaration order | Simple — declaration-order check; implies `linear`; very rigid |
| 36 | [`stateless`](#stateless) | Precept | Structural | Data-only entity — explicitly declares no state machine; just fields, constraints, and edits; no states, transitions, or events | Hard — error if states declared | Simple — currently inferred; explicit adds intent declaration |
| 37 | [`total` (precept)](#total-precept-level) | Precept | Structural | Complete matrix — the full event×state grid is covered; every event is handled in every non-terminal state | Hard — event×state matrix check | Simple — cross-product check; contradicts locality principle |
| 87 | [`guarded total`](#guarded-total) | Precept | Structural | Nothing unconditional — every transition row (except rejections) requires a `when` guard; the entity's behavior is always data-dependent | Hard — row scan for `when` presence | Simple — scan all non-reject transition rows |
| | **Semantic — States** | | | | | |
| 38 | [`success`](#success) | State | Semantic | Happy ending — marks a terminal state as a positive outcome for tooling color-coding, verdict reporting, and diagram rendering | Soft — warn if unreachable | Simple — extend existing C48 with verdict context |
| 39 | [`error`](#error) | State | Semantic | Unhappy ending — marks a terminal state as a negative outcome (Denied, Rejected, Failed); displayed with error-level styling | Soft — warn if unreachable | Simple — same as `success` |
| 40 | [`warning`](#warning) | State | Semantic | Caution flag — marks a non-terminal state that needs human attention; the entity isn't failed but is in a concerning condition | Soft — consistency checks | Simple — same mechanism as `success`/`error` |
| 41 | [`resting`](#resting) | State | Semantic | Parking lot — the entity lingers here while data accumulates; must support in-place (settling) events; think: the Review stage | Partial — must have `no transition` outcome | Simple — self-loop outcome scan |
| 42 | [`decision`](#decision) | State | Semantic | Fork in the road — this state is where the lifecycle branches based on business judgment; must have 2+ distinct outgoing event types | Full — must have 2+ outgoing event types | Simple — outgoing-event count |
| 43 | [`cyclic`](#cyclic) | State | Semantic | Designed for loops — this state intentionally participates in a cycle; suppresses cycle warnings and signals that re-entry is expected | Soft — warn if state is not in any cycle | Simple — BFS from successors on existing graph; complement of `forward` |
| 88 | [`phase <N>`](#phase-n) | State | Semantic | Swim lane assignment — groups this state into a numbered lifecycle phase (1, 2, 3…) for diagram lane layout and progress indicators | None — tooling only | Simple — parser + model flag; diagram lane assignment |
| 89 | [`cluster <Name>`](#cluster-name) | State | Semantic | Visual grouping — bundles this state with others under a shared label for diagram clustering and organizational context | None — tooling only | Simple — parser + model flag; diagram group rendering |
| 90 | [`waiting`](#waiting) | State | Semantic | On hold — the entity is blocked waiting for an external actor or system to respond (AwaitingApproval, PendingVerification) | Soft — warn if no outgoing transition exists | Simple — intent annotation + optional outgoing-check |
| 91 | [`deprecated`](#deprecated) | State/Event/Field | Semantic | Sunset notice — marks state, event, or field as deprecated; LSP renders with strikethrough and warns on any reference | Soft — warn on usage references | Simple — parser flag + LSP `deprecated` tag; multi-entity |
| 92 | [`display "<text>"`](#display-text) | State/Event | Semantic | Friendly name — overrides the identifier with a human-readable label in diagrams and the inspector panel | None — tooling only | Simple — parser + model string; diagram/inspector labeling |
| | **Semantic — Events** | | | | | |
| 44 | [`success` (event)](#success-event) | Event | Semantic | Good-news event — signals positive progress in the lifecycle; warns if every row rejects (contradicts the declared intent) | Soft — warn if every row rejects | Simple — total-contradiction scan |
| 45 | [`error` (event)](#error-event) | Event | Semantic | Bad-news event — signals a problem or failure; warns if every row transitions to a non-error state (contradicts the declared intent) | Soft — warn if every row transitions | Simple — total-contradiction scan |
| 46 | [`warning` (event)](#warning-event) | Event | Semantic | Caution event — signals concern without being definitively positive or negative; same mechanism as event-level `success`/`error` | Soft — consistency checks | Simple — same mechanism as event `success`/`error` |
| 93 | [`antonym of <Event>`](#antonym-of-event) | Event | Semantic | Semantic mirror — declares two events as opposites (Approve/Deny, Accept/Reject); warns if both target the same verdict state | Soft — warn if both target same verdict | Simple — cross-reference verdict consistency |
| 94 | [`primary`](#primary) | Event | Semantic | Main storyline — marks the primary happy-path event at each lifecycle stage; diagrams render it with bold or prominent arrows | None — tooling only | Simple — parser flag; diagram bold rendering |
| 95 | [`scaffold`](#scaffold) | Event | Semantic | Backstage work — marks setup or housekeeping events (AttachDocument, AddNote); diagrams render them with lighter, thinner arrows | None — tooling only | Simple — parser flag; diagram lighter rendering |
| | **Semantic — Fields** | | | | | |
| 47 | [`sensitive`](#sensitive) | Field | Semantic | Protected data — contains PII, PHI, or other sensitive information; tooling masks the value in inspector, preview, and MCP output | None — tooling only | Simple — parser + model flag; tooling reads it |
| 48 | [`audit`](#audit) | Field | Semantic | Compliance trail — signals that every mutation to this field should be tracked in the host application's audit log; pure intent marker | None — tooling only | Simple — parser + model flag; host-app integration TBD |
| 96 | [`volatile`](#volatile) | Field | Semantic | Noisy field — changes often during the lifecycle; tooling de-prioritizes it in diffs and inspector panels to reduce clutter | None — tooling only | Simple — parser flag; diff/inspector de-prioritization |
| 97 | [`temporal`](#temporal) | Field | Semantic | Timestamp-ish — represents a time-like value (epoch, sequence counter); enables timeline views and duration calculations in tooling | None — tooling only | Simple — parser flag; inspector timeline rendering |
| 98 | [`internal`](#internal) | Field | Semantic | Plumbing — a mechanical bookkeeping field, not business-meaningful (RetryCount, InternalVersion); diagrams and inspector de-emphasize it | None — tooling only | Simple — parser flag; diagram/inspector de-emphasis |
| 99 | [`summary`](#summary) | Field | Semantic | Title field — the inspector uses this field's value as the entity's display name (e.g., Claim #12345 instead of raw ID) | None — tooling only | Simple — parser flag; inspector title source |
| 100 | [`hidden`](#hidden) | Field/State | Semantic | Invisible — completely excluded from diagrams and inspector panels; the field or state exists in the model but isn't surfaced to users | None — tooling only | Simple — parser flag; diagram/inspector exclusion; multi-entity |
| | **Semantic — Precept** | | | | | |
| 49 | [`auditable`](#auditable) | Precept | Semantic | Full compliance mode — every field in this precept is automatically treated as `audit`; sugar for marking every field individually | None — tooling only | Simple — propagates `audit` to all fields; syntactic sugar for per-field `audit` |
| 101 | [`pattern <name>`](#pattern-name) | Precept | Semantic | Workflow tag — classifies the precept under a named pattern for metadata, search, and categorization | None — tooling only | Simple — parser + model string; metadata/search/classification |
| 102 | [`cyclic` (precept)](#cyclic-precept) | Precept | Semantic | Loops by design — declares that the lifecycle intentionally contains cycles; suppresses cycle warnings; semantic complement of `linear` | Soft — warn if graph is actually acyclic | Simple — cycle detection check; complement of `linear`; re-evaluated from §5 |
| | **Severity — Rules** | | | | | |
| 50 | [`warning` (rule)](#warning-on-invariantassert) | Rule | Severity | Soft enforcement — an invariant or assertion violation produces a warning instead of blocking the operation; flags the issue without rejecting | Hard — C60/C61/C62 | Moderate — parser + model + engine partitioning + new outcomes + API changes; requires philosophy evolution (§2) |

---

### 3.1 Structural Modifiers

Structural modifiers impose compile-time provable constraints on graph topology, transition row structure, or field mutation patterns. The compiler errors on violations.

---

#### 3.1.1 Structural — States

##### `terminal`

- **Syntax:** `state Approved terminal`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** The lifecycle ends here. No outgoing transitions allowed.
- **Compile-time check:** Error if a `terminal` state has outgoing `transition` outcomes. C50 (dead-end) suppressed for `terminal` states, strengthened for non-terminal states ("If intentional, mark with `terminal`"). C48 (unreachable) + `terminal` = definition smell (unreachable endpoint).
- **Tooling impact:** Diagram renders terminal states with distinctive border (double-line or filled). Inspector shows "Terminal state — no outgoing transitions." MCP `precept_compile` includes `terminal: true` in state metadata. Completions suggest `terminal` after state names that have no outgoing transitions.
- **Precedent:** Every surveyed system has terminal states — XState (`type: 'final'`), Step Functions (`Succeed`/`Fail`), BPMN (End Event), UML (`FinalState`), TLA+ (`<>Terminated`), Alloy (`no s.transitions`). Broadest precedent of any modifier candidate.
- **Dependencies:** None. Ships independently. Unblocks `completing` (event) and strengthens `sealed after` (field).
- **Concrete example — Insurance Claim:**
  ```precept
  precept InsuranceClaim

  state Draft initial
  state Submitted
  state UnderReview
  state Approved terminal      # lifecycle endpoint — no transitions out
  state Denied terminal        # lifecycle endpoint — no transitions out
  state Paid terminal          # lifecycle endpoint — no transitions out
  ```
- **Value/practicality assessment:** Lowest implementation cost of any candidate. Single new diagnostic (error on outgoing transitions from terminal). Strengthens existing C50 diagnostic. The `initial` parallel — fills the other bookend. Every real sample has states that would benefit from this annotation (e.g., Approved/Denied/Paid in Insurance Claim, Hired/Rejected in Hiring Pipeline, Funded/Declined in Loan Application).

---

##### `guarded`

- **Syntax:** `state Approved guarded`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** All incoming transitions must have `when` clauses. Prevents accidental unguarded entry to high-value states.
- **Compile-time check:** Error if any incoming transition row targeting this state lacks a `when` clause. Reject rows that don't transition into the guarded state are unaffected.
- **Tooling impact:** Inspector shows "Guarded state — all incoming transitions require conditions." Diagram could render a shield icon. Completions could flag when the author writes an unguarded transition to a guarded state.
- **Precedent:** No direct single-keyword precedent across surveyed systems. The concept exists in BPMN (conditional flows into activities) and in convention-driven patterns across XState and Stateless (guard functions on transitions). TLA+ expresses this as a safety invariant over incoming edges.
- **Dependencies:** None.
- **Concrete example — Loan Application:**
  ```precept
  state Approved guarded

  # ✓ This row has a guard — satisfies `guarded`
  from UnderReview on Approve
      when DocumentsVerified and CreditScore >= 680
      and AnnualIncome >= ExistingDebt * 2
      and RequestedAmount < AnnualIncome / 2
      -> set ApprovedAmount = Approve.Amount -> transition Approved

  # The reject row doesn't transition to Approved, so it doesn't violate `guarded`
  from UnderReview on Approve -> reject "Requirements not met"
  ```
- **Value/practicality assessment:** Low implementation cost — incoming-row scan for `when` clause presence. High safety value for states like Approved, Funded, Hired where unguarded entry is almost always a bug. Every loan/insurance/hiring sample has at least one state that should be guarded.

---

##### `required` / `milestone`

- **Syntax:** `state UnderReview required` or `state UnderReview milestone`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** All initial→terminal paths must visit this state. The entity cannot reach an endpoint without passing through this checkpoint.
- **Compile-time check:** Dominator analysis on the transition graph. Error if any initial→terminal path bypasses the required state. The analysis treats all edges as traversable (overapproximation — guards are ignored for structural checks).
- **Tooling impact:** Diagram could highlight required states with a distinctive marker. Inspector reports "Required checkpoint — all paths pass through here." Path analysis tools report which paths satisfy the requirement.
- **Precedent:** BPMN's Parallel Gateway (AND-join) enforces path completion but applies to convergence, not individual state visitation. TLA+ expresses this as `Spec => <>InReview` (liveness property). No workflow system has an explicit `required` keyword on states.
- **Dependencies:** Enhanced by `terminal` (dominator analysis is cleaner when lifecycle endpoints are explicitly declared). Does not strictly require `terminal` — the analyzer can infer terminal states from graph topology.
- **Concrete example — Insurance Claim:**
  ```precept
  state Draft initial
  state Submitted
  state UnderReview required   # every claim must be reviewed before resolution
  state Approved terminal
  state Denied terminal
  state Paid terminal
  ```
- **Value/practicality assessment:** High conceptual value — captures compliance obligations ("every claim must be reviewed"). Medium-high implementation complexity — requires dominator analysis or path enumeration on the transition graph. Guard-dependent false positives are a known risk: the overapproximation may flag paths that are infeasible due to guards. Diagnostic clarity for false positives needs design work. The keyword choice (`required` vs `milestone`) is a naming question — `required` is more precise but could clash with future field requirements; `milestone` is more domain-friendly but less precise. *(Note: feasibility upgraded to Moderate based on code-grounded analysis in [milestone-modifier-feasibility.md](./milestone-modifier-feasibility.md) — dominator analysis ~90 LOC on existing graph+BFS; guard false positives are a design question, not an implementation barrier.)*

---

##### `forward`

- **Syntax:** `state Submitted forward`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** Once the entity leaves this state, it cannot re-enter. No cycle in the transition graph passes through this state. The state is a one-way checkpoint — once past it, the lifecycle cannot return here.
- **Compile-time check:** For state S marked `forward`, compute S's successors from the adjacency graph. Run BFS from all successors (treating them as a collective start set). Error if S is reachable from any successor — a cycle exists that passes through S.
- **Tooling impact:** Diagram renders forward states with a one-way indicator (directional border or arrow badge). Inspector shows "Forward state — no re-entry." Completions could warn when the author writes a transition row whose target is a `forward` state reachable from a state downstream of it. AI consumers know this state is a one-directional checkpoint.
- **Precedent:** BPMN sequence flows are inherently forward (no back-edges in well-structured processes). Petri net workflow soundness includes acyclicity requirements for structured workflows. Process mining conformance checking detects re-entry as a deviation from normative models. TLA+ temporal operators (`[](Left(S) => []~InS)` after leaving) express the property formally. No workflow system has an explicit per-state `forward` keyword — the concept is structural in BPMN and temporal in TLA+.
- **Dependencies:** None. Enhanced by `terminal` — a terminal state is trivially forward (can't be re-entered because the lifecycle ends). Enhanced by `milestone` — a `milestone forward` state must be visited exactly once (mandatory + no re-entry).
- **Concrete example — Insurance Claim:**
  ```precept
  state Draft initial
  state Submitted forward        # once claim leaves Submitted, it can't go back
  state UnderReview              # review may cycle (request docs → receive docs → review)
  state Approved forward         # once approved, no returning to review
  state Denied forward           # once denied, final
  state Paid forward             # once paid, final
  ```
  Note that `UnderReview` is intentionally NOT marked `forward` — the document request/receive cycle means the entity may stay in UnderReview through multiple event rounds, and other designs might allow re-review from downstream states. `Submitted`, `Approved`, `Denied`, and `Paid` are naturally forward — once past them, the lifecycle doesn't return.
- **Value/practicality assessment:** High practical value. Maps directly to Shane's observation about flow direction — "once left, cannot be re-entered." The check is a single BFS per `forward` state, reusing the existing `graph` adjacency list (~15 LOC for the BFS + diagnostic emission). Every linear or mostly-linear workflow (insurance, loan, hiring, travel reimbursement) has states that are naturally forward; the modifier catches accidental back-transitions that create unintended cycles. The `forward` + `milestone` combination captures the "exactly once" checkpoint pattern common in compliance contexts (SOX, KYC/AML: step must happen AND must happen only once). The concept is the per-state complement of the existing `irreversible` event modifier (#21) — `irreversible` says "this event's transitions can't be reversed," `forward` says "this state can't be revisited." Both check the same graph property (no reverse path) but from different declaration surfaces.

---

##### `gate`

- **Syntax:** `state Approved gate`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** Once the entity enters this state, all predecessors of this state in the transition graph are closed off — no transition from any downstream state can target any predecessor. The "predecessor" (ancestor) relationship is defined by graph ancestry: ancestors are all states from which this gate state is reachable via forward edges. This is the stronger companion to `forward`: where `forward` prevents re-entry to one specific state, `gate` prevents return to the entire subgraph that preceded the gate.
- **Compile-time check:** For gate state G:
  1. Compute G's **ancestor set**: reverse-BFS from G — all states from which G is reachable via forward edges in the transition graph.
  2. Compute G's **downstream set**: forward-BFS from G — all states reachable from G.
  3. Scan all transition rows originating from G or any state in the downstream set. Error if any such transition targets a state in G's ancestor set.

  The analysis treats all edges as traversable (overapproximation — guards are ignored for structural checks, consistent with `forward`, `irreversible`, and `milestone`).
- **Tooling impact:** Diagram renders gate states with a barrier indicator (double vertical line or phase-gate icon). Inspector shows "Gate state — all predecessor states closed on entry." MCP `precept_compile` includes `gate: true` in state metadata. Completions could warn when the author writes a transition row from a downstream state targeting an ancestor of the gate. AI consumers know that this state represents a one-way phase boundary.
- **Precedent:** The "stage gate" or "phase gate" concept is well-established in project management (Cooper's Stage-Gate® process) and manufacturing (quality gates in automotive/aerospace). BPMN process models enforce phase progression through pool/lane structure and non-interrupting boundary events, though not with a single per-state keyword. Petri net soundness analysis includes properties about unreachability of prior markings after crossing synchronization barriers. TLA+ temporal operators can express the ancestor-closure property formally (`□(Entered(G) ⇒ □(∀ S ∈ Ancestors(G): ¬InS))`). No workflow DSL has an explicit per-state `gate` keyword — the concept is architectural in business process frameworks and temporal in formal methods.
- **Dependencies:** None required. Enhanced by `forward` — a `gate` state is necessarily `forward` (if all predecessors are closed, the gate state itself cannot be re-entered, since it is its own trivial ancestor). The compiler could detect `forward` on a `gate` state as subsumed (harmless redundancy or hint). Enhanced by `terminal` — a terminal gate state closes off the entire ancestor chain permanently (trivially satisfied since no downstream transitions exist). Enhanced by `milestone` — `milestone gate` means "every path visits this state, and once visited, all prior states are permanently closed." This is the strongest lifecycle checkpoint: mandatory passage through a one-way phase boundary.
- **Concrete example — Insurance Claim:**
  ```precept
  state Draft initial
  state Submitted forward
  state UnderReview
  state Approved gate              # once approved: Draft, Submitted, UnderReview all closed
  state Denied gate                # once denied: Draft, Submitted, UnderReview all closed
  state Paid terminal

  # Approved's ancestor set: {Draft, Submitted, UnderReview}
  #   (all states from which Approved is reachable)
  # Approved's downstream set: {Paid}
  # Check: no transition from Approved or Paid targets Draft, Submitted, or UnderReview
  #
  # Denied's ancestor set: {Draft, Submitted, UnderReview}
  # Denied is terminal-like (no outgoing transitions) so downstream set is empty
  # Check: no transition from Denied targets any ancestor — trivially satisfied
  ```
  Compare with per-state `forward`: marking `Submitted forward` only prevents re-entry to Submitted specifically — it says nothing about Draft. Marking `Approved gate` closes off ALL of Draft, Submitted, and UnderReview in one declaration.
- **Value/practicality assessment:** Strong conceptual value — captures the "phase gate" pattern common in regulated industries (insurance underwriting phases, manufacturing quality gates, pharmaceutical trial stages). The guarantee is broadly useful: once a decision is made, the entire preceding phase is sealed off, not just one state.

  **Design tensions — stated honestly:**

  1. **"Before" is ambiguous in branching graphs.** The ancestor set is defined by reverse-reachability: state X is an ancestor of gate G if any path from X to G exists. In graphs with multiple branches, this may include states that are not intuitively "before" the gate. Example: if state X reaches gate G via one branch but also participates in a completely unrelated branch of the lifecycle, X is still in G's ancestor set. The overapproximation treats all edges as traversable, so X is "before" G even if the branch through X to G is guard-infeasible. This can close off states the author didn't intend to close.

  2. **Cycles in the ancestor set.** If states in G's ancestor set participate in a cycle (e.g., UnderReview ↔ AwaitingDocuments), the entire cycle is in the ancestor set. This is semantically correct (the cycle is "before" the gate and should be closed), but it may surprise authors who think of the cycle as a separate concern from the gate's phase boundary.

  3. **Overapproximation amplifies false positives.** The same overapproximation that affects `forward` and `irreversible` applies here, but is amplified because the ancestor set is typically larger than a single state. More transitions are subject to the check, so more guard-infeasible paths may be flagged. Clear diagnostics ("transition from {Source} to {Target} violates gate on {G}: {Target} is an ancestor of {G}") are essential to help authors diagnose and address false positives.

  4. **`forward` vs `gate` — when to use which.** `forward` is surgical: it protects one state from re-entry. `gate` is sweeping: it closes off an entire ancestor subgraph. Use `forward` when you want to prevent re-entry to a specific checkpoint (e.g., "once past Submitted, don't go back to Submitted"). Use `gate` when you want to enforce phase boundaries (e.g., "once Approved, the entire review phase is closed"). In linear or mostly-linear lifecycles, `gate` on a late-stage state has the same effect as `forward` on every predecessor — but `gate` expresses it as a single declaration on the boundary rather than scattered annotations. In branching graphs, `forward` gives finer control because each state's forward constraint is independent.

  5. **Implementation complexity.** Moderate — requires reverse BFS from the gate state to compute ancestors (~15 LOC), then forward BFS from the gate state to compute the downstream set (~15 LOC), then a scan of all downstream transitions to check for ancestor targets (~10–15 LOC). Two BFS passes per gate state plus a transition scan. More expensive than `forward` (one BFS per state) but still tractable — precept graphs typically have 5–20 states. Estimated ~40–60 LOC on existing graph infrastructure. No new data structures needed beyond the existing adjacency list.

  Overall: `gate` is the natural "next step" after `forward` for authors who want phase-level protection rather than per-state protection. The design tensions around branching graphs and overapproximation are real but manageable with clear diagnostics. The keyword `gate` is the best fit among the candidates considered (`phase`, `barrier`, `checkpoint`): it's concise (4 characters), has strong business-process precedent ("stage gate" / "quality gate"), reads naturally in Precept syntax (`state Approved gate`), and the metaphor is precise — a gate lets you through but closes everything behind you. `phase` connotes a grouping concept rather than a constraint. `barrier` connotes blocking forward movement (opposite of intent). `checkpoint` overlaps with `milestone`/`required` in connotation.

---

##### `transient`

- **Syntax:** `state Processing transient`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** Must have at least one successful outgoing transition. The entity should not remain in this state permanently.
- **Compile-time check:** Error if the state has no outgoing transition that succeeds (all rows reject or there are no rows). This is an upgrade of C50 (dead-end) to a hard error for the declared state.
- **Tooling impact:** Inspector shows "Transient state — entity should not remain here." Diagram could render with a pass-through visual treatment.
- **Precedent:** Step Functions has a `Pass` state (explicit pass-through). Temporal has activity heartbeat timeouts (runtime residency constraint). TLA+ expresses this as `[](InProcessing => <>~InProcessing)` (liveness). No workflow system has a compile-time-only `transient` keyword.
- **Dependencies:** None.
- **Concrete example — Insurance Claim:**
  ```precept
  state Submitted transient    # claims shouldn't sit in Submitted — move to review
  ```
- **Value/practicality assessment:** The compile-time check ("has outgoing transitions") is weaker than the concept implies. The natural reading of "transient" suggests time-bounded residency, but Precept has no clock model. The structural check only verifies that escape routes *exist*, not that they'll be *taken*. This gap between the name's connotation and the actual guarantee is a usability risk.

---

##### `absorbing`

- **Syntax:** `state Closed absorbing`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** Has event handlers (rows exist) but none of them produce a `transition` outcome. Events are handled (data mutations, rejections) but the entity never leaves this state.
- **Compile-time check:** Error if any row from this state produces a `transition` outcome. Must have at least one row (otherwise it's just `terminal`).
- **Tooling impact:** Inspector shows "Absorbing state — handles events but never transitions out." Diagram renders as a terminal-like node with incoming event edges that loop back.
- **Precedent:** The concept exists in Markov chain theory (absorbing states). No workflow system has an explicit keyword for this.
- **Dependencies:** None, but expressible as `terminal` combined with event rows that use `no transition`.
- **Concrete example:**
  ```precept
  state Archived absorbing

  # Events are handled for audit purposes but entity stays in Archived
  from Archived on AddNote -> set ArchiveNote = AddNote.Text -> no transition
  from Archived on RequestReview -> reject "Archived items cannot be reviewed"
  ```
- **Value/practicality assessment:** Structurally valid concept. Marginal incremental value over `terminal` — a `terminal` state can already have `no transition` and `reject` rows. The distinction is whether the state has *any* rows at all. In practice, most terminal states that handle events are absorbing by definition. A separate keyword adds vocabulary without adding much practical differentiation.

---

##### `convergent`

- **Syntax:** `state UnderReview convergent`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** Reachable from 2+ distinct source states. Multiple paths converge at this point.
- **Compile-time check:** Count distinct source states that have transition rows targeting this state. Error if fewer than 2.
- **Tooling impact:** Diagram could highlight convergence points. Inspector reports "Convergent state — reachable from N source states."
- **Precedent:** BPMN's merge gateways represent convergence. No workflow system has an explicit convergence keyword on states.
- **Dependencies:** None.
- **Concrete example:**
  ```precept
  # UnderReview is reachable from both Submitted and Reopened
  state UnderReview convergent

  from Submitted on AssignReviewer -> transition UnderReview
  from Reopened on AssignReviewer -> transition UnderReview
  ```
- **Value/practicality assessment:** Low. The property is trivially derivable from the transition graph. The compiler already knows how many sources a state has. A keyword adds authorial intent but provides minimal structural leverage. Most convergent states are obvious from reading the rows. Limited real demand from samples.

---

##### `ordered before <State>`

- **Syntax:** `state Screening ordered before Decision`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** On any execution path that visits both this state and the named state, this state is visited first. The lifecycle cannot reach the named state and then return to this state — relative ordering between two specific states is enforced.
- **Compile-time check:** BFS from the named state (the "after" state). Error if THIS state (the "before" state) is reachable from the named state — a path from the "after" state back to the "before" state exists, violating the declared ordering.
- **Tooling impact:** Diagram could render ordering constraints as dashed directional markers between the two states. Inspector shows "Ordered before {State} — ordering enforced." MCP compile output includes declared ordering relationships.
- **Precedent:** BPMN sequence flows imply ordering through topology but have no explicit ordering annotation. TLA+ temporal operators can express ordering constraints via auxiliary variables. Process mining trace alignment detects ordering violations as conformance deviations. No workflow system has an explicit `ordered before` keyword on states.
- **Dependencies:** None. Partially subsumed by `forward` — a `forward` state that is topologically before another state inherently satisfies `ordered before`. The modifier adds value only when `forward` is too strong (the "before" state may participate in cycles elsewhere in the graph but must still precede a specific other state). Parameterized syntax follows the precedent of `sealed after <State>` on fields.
- **Concrete example — Hiring Pipeline:**
  ```precept
  state Screening ordered before Decision
  # Ensures no path from Decision back to Screening
  # Even if InterviewLoop has review cycles, Screening→...→Decision ordering holds

  state InterviewLoop
  state Decision

  from Screening on PassScreen -> transition InterviewLoop
  from InterviewLoop on RecordInterviewFeedback -> ... -> transition Decision
  # No transition from Decision or any state reachable from Decision leads back to Screening
  ```
- **Value/practicality assessment:** Low-medium. The check is simple (one BFS, ~15 LOC on existing `graph`), but use cases are niche. Most ordering in real workflows comes from the graph topology itself — if there's no transition from Decision back to Screening, the ordering is already implicit and the modifier adds nothing beyond documenting intent. The modifier's value appears only when the graph COULD have a path from the "after" state back to the "before" state, which the modifier then catches as an error. In practice, `forward` on the "before" state achieves the same protection more broadly (no cycles through that state, period). The parameterized syntax (like `sealed after <State>`) is well-precedented in the existing language but adds vocabulary for a narrow use case. Honest assessment: this is the most speculative of the three new flow candidates.

---

##### `oneshot`

- **Syntax:** `state Screening oneshot`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** Visited exactly once across the lifecycle — the conjunction of `milestone` (mandatory visitation) and `forward` (no re-entry). The entity must pass through this state AND cannot return to it. The strongest single-state checkpoint guarantee.
- **Compile-time check:** Combined `milestone` + `forward` analysis. Error if any initial→terminal path bypasses this state (milestone violation) OR if the state is reachable from any of its successors (forward violation). Both checks use existing BFS/dominator infrastructure.
- **Tooling impact:** Diagram renders oneshot states with a combined mandatory + one-way indicator. Inspector shows "Oneshot state — visited exactly once." MCP `precept_compile` includes `oneshot: true` in state metadata.
- **Precedent:** SOX mandatory review gates (must happen, must happen once). KYC/AML single-pass verification. No workflow system has an explicit keyword for this — it's the intersection of two properties that existing systems express separately.
- **Dependencies:** Compositional — equivalent to `milestone forward`. Requires the same analysis infrastructure as both (`milestone` #3 dominator analysis + `forward` #4 BFS reachability). If both `milestone` and `forward` ship independently, `oneshot` is syntactic sugar for the combination. If either is missing, `oneshot` provides the combined guarantee in one keyword.
- **Concrete example — Hiring Pipeline:**
  ```precept
  state Draft initial
  state Screening oneshot        # every candidate screened exactly once
  state InterviewLoop
  state Decision oneshot         # every candidate gets exactly one decision
  state OfferExtended
  state Hired terminal
  state Rejected terminal

  # Screening must be on every path (milestone) and can't be revisited (forward)
  # Decision must be on every path (milestone) and can't be revisited (forward)
  ```
- **Value/practicality assessment:** Medium. Clear concept with strong compliance domain demand. The "exactly once" pattern is common in regulated workflows. Being compositional (`milestone` + `forward`) is not disqualifying — many modifiers are compositional (`identity` is `writeonce` + tooling, `completing` depends on `terminal`). The question is whether the one-keyword convenience justifies a dedicated keyword when `milestone forward` achieves the same result. For authors who frequently need the combined guarantee, `oneshot` is more readable. Implementation reuses existing milestone and forward infrastructure — no new analysis needed.

---

##### `bottleneck`

- **Syntax:** `state UnderReview bottleneck`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** A state whose removal would disconnect the initial state from at least one terminal state. A weaker form of `milestone` — `milestone` dominates ALL terminals, `bottleneck` dominates at least one. The state is a structural chokepoint in the lifecycle graph.
- **Compile-time check:** Dominator analysis per terminal. For each terminal state T, compute dominators on all paths from initial to T. The state passes if it dominates at least one terminal. Error if the bottleneck state does not dominate any terminal. Alternative implementation: remove the state from the graph and check if any terminal becomes unreachable from initial.
- **Tooling impact:** Diagram highlights bottleneck states with a constriction visual. Inspector shows "Bottleneck — removal disconnects path to {TerminalList}." MCP compile output includes `bottleneck: true` in state metadata with dominated terminals listed.
- **Precedent:** Graph theory: cut vertices (articulation points). Network flow analysis uses bottleneck identification extensively. No workflow system uses this as a keyword.
- **Dependencies:** Enhanced by `terminal` (the check references terminal states for reachability analysis). Weaker than `milestone` (#3) — a milestone is always a bottleneck, but a bottleneck is not always a milestone.
- **Concrete example — Insurance Claim with multiple resolution paths:**
  ```precept
  state Draft initial
  state Submitted
  state UnderReview bottleneck   # removal would disconnect path to Approved/Paid
  state FastTrack                # alternative path bypasses UnderReview for Denied
  state Approved terminal
  state Denied terminal
  state Paid terminal

  # UnderReview dominates Approved and Paid, but FastTrack provides
  # a direct path from Submitted to Denied. UnderReview is a bottleneck
  # (dominates some terminals) but NOT a milestone (doesn't dominate all).
  ```
- **Value/practicality assessment:** Low-medium. The concept is structurally sound and compile-time provable (same dominator infrastructure as `milestone`). The practical question is whether "dominates some terminals but not all" is a useful constraint for authors. In most workflows, if a state is important enough to annotate, the author usually wants the full `milestone` guarantee (dominates all terminals). The weaker `bottleneck` guarantee is harder to reason about — which terminals does it dominate? The diagnostic would need to specify which paths are affected, adding diagnostic complexity. No additional analysis algorithms needed beyond `milestone`'s existing dominator infrastructure.

---

##### `depth <N>`

- **Syntax:** `state UnderReview depth 2`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** This state is exactly N transitions away from the initial state along ALL paths that reach it. Every path from initial to this state has the same length N.
- **Compile-time check:** Path enumeration on the transition graph. Compute all simple paths from initial to this state. Error if any path has a length other than N. For DAGs (when `linear` is also declared), this is standard BFS/DFS depth calculation. For cyclic graphs, "all simple paths" is well-defined but the set may be large — the analysis is still decidable but potentially expensive.
- **Tooling impact:** Diagram could render depth annotations as lane markers or layer numbers. Inspector shows "Depth 2 — exactly 2 transitions from initial." MCP compile output includes `depth: 2` in state metadata.
- **Precedent:** Layered graph layouts use depth (topological level) for positioning. No workflow system constrains states by transition depth. BPMN has an informal notion of process layers but doesn't enforce transition counts.
- **Dependencies:** Practically useful only with `linear` (#29) — in DAGs, depth is well-defined and path lengths may coincide. In cyclic graphs, states have multiple path lengths (including paths through cycles), making uniform depth extremely unlikely. The modifier could be restricted to `linear` precepts (error if used without `linear`), or it could apply generally but rarely be satisfiable in cyclic graphs. The parameterized syntax follows the precedent of `sealed after <State>` and `ordered before <State>`.
- **Concrete example — Loan Application (linear):**
  ```precept
  precept LoanApplication linear

  state Draft initial            # depth 0 (implicit)
  state UnderReview depth 1      # exactly 1 transition from Draft
  state Approved depth 2         # exactly 2 transitions from Draft
  state Funded depth 3           # exactly 3 transitions from Draft
  state Declined depth 2         # exactly 2 transitions from Draft
  ```
- **Value/practicality assessment:** Low. Even in DAGs, reconvergent paths (e.g., two different routes from Draft to UnderReview with different intermediate states) violate the uniform-depth constraint unless every route has the same length. The modifier is only satisfiable in strictly layered DAGs where every path to a state has the same length — a small subset of even linear workflows. Branching and reconvergence (common even in DAGs) break uniform depth. The constraint is compile-time provable but the set of satisfiable real-world lifecycles is narrow. Shane should be aware that few sample files would satisfy per-state depth constraints.

---

##### `exclusive`

- **Syntax:** `state Decision exclusive`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** No two events can both succeed from this state for the same data snapshot. At most one event produces a non-reject outcome for any given field configuration.
- **Compile-time check:** Guard satisfiability analysis across all (state, event) groups originating from the state. For each pair of events that can succeed, attempt to prove their success guards are mutually exclusive. Error if two events have overlapping satisfiable guard conditions. **Partial provability:** feasible for simple boolean/enum guard combinations; infeasible for general arithmetic expressions or data-dependent guards. The compiler can prove disjointness for `when X == true` vs `when X == false` but not for `when Amount > threshold` vs `when Score < limit` without symbolic reasoning.
- **Tooling impact:** Inspector shows "Exclusive state — at most one event succeeds per data snapshot." Diagram could render with a ⊕ marker (exclusive-or). AI consumers know this is a decision point with non-overlapping outcomes.
- **Precedent:** BPMN's Exclusive Gateway (XOR) enforces exactly-one-path semantics. UML's choice pseudostate with non-overlapping guards. The concept is well-established in workflow modeling but rarely enforced at compile time due to the satisfiability challenge.
- **Dependencies:** None. Partially overlaps with `disjoint` (#57) on events — `exclusive` operates at the state level across events; `disjoint` operates within a single event across guard rows.
- **Concrete example — Hiring Pipeline:**
  ```precept
  state Decision exclusive

  from Decision on ExtendOffer when FeedbackCount >= 2 -> ... -> transition OfferExtended
  from Decision on ExtendOffer -> reject "..."
  from Decision on RejectCandidate -> ... -> transition Rejected
  # At most one of ExtendOffer or RejectCandidate succeeds for any given data
  ```
- **Value/practicality assessment:** Low practical confidence due to very limited static provability. The general case is undecidable (guard satisfiability over arbitrary expressions). The compiler can prove exclusivity for trivially disjoint guards (boolean flags, enum equality) but not for the compound arithmetic guards that appear in real samples (see Loan Application's Approve guard). Including per Shane's inclusion criteria — partial provability exists — but the honest assessment is that this modifier would rarely be provably satisfiable in real precepts.
- **Origin:** Frank creative sweep

---

##### `dormant`

- **Syntax:** `state AwaitingDocuments dormant`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** Only one event can cause a state transition from this state; all other events are settling (produce `no transition` or reject). The state has a single "escape route."
- **Compile-time check:** Count distinct events from this state that have at least one `transition` outcome. Error if more than one event can advance. Settling events (`no transition` only) and reject-only events are permitted without limit.
- **Tooling impact:** Inspector shows "Dormant state — one exit event, rest settle." Diagram renders with a single prominent outgoing edge and lighter-weight in-place event markers. AI consumers know this is a dwell-and-accumulate state with one exit.
- **Precedent:** The concept maps to Temporal's activity-with-single-completion pattern. No workflow system has an explicit keyword for single-exit-event states.
- **Dependencies:** None. Enhanced by `settling` (#15) — settling events in a dormant state are the expected pattern.
- **Concrete example — Insurance Claim:**
  ```precept
  state Submitted dormant   # only AssignAdjuster transitions out; RequestDocument settles

  from Submitted on RequestDocument -> add MissingDocuments RequestDocument.Name -> no transition
  from Submitted on AssignAdjuster -> set AdjusterName = AssignAdjuster.Name -> transition UnderReview
  ```
- **Value/practicality assessment:** Medium. Clear concept with a simple compile-time check (count advancing events). Maps to real sample patterns — Submitted in Insurance Claim has one transition event and one settling event. The keyword makes the "accumulate then advance" pattern explicit. Low implementation cost.
- **Origin:** Frank creative sweep

---

##### `passthrough`

- **Syntax:** `state Routing passthrough`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** No settling events from this state. Every event either transitions to another state or rejects. The entity does not dwell here — it passes through.
- **Compile-time check:** Error if any row from this state produces a `no transition` success outcome. All rows must produce either `transition` or `reject`.
- **Tooling impact:** Inspector shows "Passthrough state — no dwelling." Diagram renders with a slim profile (transit node, not a dwell node). AI consumers know this state is a routing checkpoint.
- **Precedent:** Step Functions' `Choice` state (pure routing, no activity). BPMN gateways (routing nodes with no work). The concept is common in workflow modeling.
- **Dependencies:** None. Structural complement of `dormant` — dormant has one exit, passthrough has all exits. Overlaps with `transient` (#6) in spirit but is a stronger check: `transient` only requires at least one exit, `passthrough` requires ALL successes to be exits.
- **Concrete example — Hiring Pipeline:**
  ```precept
  state Decision passthrough  # every event transitions or rejects — no settling

  from Decision on ExtendOffer when FeedbackCount >= 2 -> set OfferAmount = ExtendOffer.Amount -> transition OfferExtended
  from Decision on ExtendOffer -> reject "At least two feedback entries required"
  from Decision on RejectCandidate -> set FinalNote = RejectCandidate.Note -> transition Rejected
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (outcome scan). Maps to real patterns — Decision in Hiring Pipeline is a passthrough. Captures routing/decision nodes vs. dwell/accumulation nodes. The `passthrough`/`dormant` axis is complementary to the `advancing`/`settling` axis on events, but applied at the state level.
- **Origin:** Frank creative sweep

---

##### `readonly` (state)

- **Syntax:** `state Archived readonly`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** No field mutations from any row originating in this state. Events can be handled (for routing/rejection) but cannot change data.
- **Compile-time check:** Error if any transition row from this state contains a `set`, `add`, `remove`, `enqueue`, or `dequeue` action.
- **Tooling impact:** Inspector shows "Read-only state — no mutations." Diagram renders with a lock icon. AI consumers know data is frozen while in this state.
- **Precedent:** Immutable state patterns in event-sourced systems. Read-only transaction isolation levels in databases. No workflow system has a per-state read-only keyword.
- **Dependencies:** None. Subsumes `absorbing` (#7) in the mutation dimension — a `readonly absorbing` state handles events, never transitions, and never mutates. Enhanced by `terminal` — a `terminal readonly` state is the strongest "lifecycle is finished" declaration.
- **Concrete example:**
  ```precept
  state Denied readonly         # once denied, data doesn't change

  from Denied on Appeal -> reject "Appeals must be submitted through a separate process"
  # No `set` commands — readonly enforces this
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (mutation scan). Strong conceptual model for post-decision states where data should be frozen but events are still handle-able for routing or rejection. Many terminal-like states in real samples are implicitly readonly (Denied, Rejected, Paid have no mutations in their outgoing rows). The modifier makes this explicit.
- **Origin:** Frank creative sweep

---

##### `reactive`

- **Syntax:** `state Submitted reactive`
- **Entity type:** State
- **Mechanism:** Structural
- **What it means:** This state has `to` or `from` entry/exit action declarations. It performs automatic actions when the entity enters or leaves.
- **Compile-time check:** Error if the state has no `to <State> -> ...` or `from <State> -> ...` action declarations.
- **Tooling impact:** Inspector shows "Reactive state — has entry/exit actions." Diagram could render with an action badge (⚡). AI consumers know this state has side effects on entry/exit.
- **Precedent:** XState's `entry`/`exit` actions on states. UML's do/entry/exit activities. BPMN's boundary events. State entry/exit actions are well-established in state machine theory.
- **Dependencies:** Requires entry/exit action syntax to exist in the language.
- **Concrete example:**
  ```precept
  state UnderReview reactive

  to UnderReview -> set ReviewStarted = true
  from UnderReview -> set ReviewCompleted = true
  ```
- **Value/practicality assessment:** Low-medium. The property is derivable from declared structure (presence of entry/exit action lines). The modifier adds authorial intent but the compiler already knows which states have actions. Value depends on whether the "this state has automatic effects" signal is worth a keyword. In real samples, entry/exit actions are visible from reading the definition — the modifier adds readability but limited structural leverage.
- **Origin:** George creative sweep

---

#### 3.1.2 Structural — Events

##### `entry`

- **Syntax:** `event Submit entry`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Fires only from the `initial` state. The intake event — how entities enter the lifecycle.
- **Compile-time check:** Error if any non-initial source state has transition rows for this event.
- **Tooling impact:** Diagram renders the entry event distinctively (bold edge from initial). Inspector shows "Entry event — fires only from Draft." Completions suppress this event when authoring rows for non-initial states.
- **Precedent:** BPMN has Start Events (explicit intake). XState workflows have an implicit initial transition. UML has InitialPseudostate transitions. The explicit `entry` keyword on events has no direct precedent — it's the event-side parallel of `initial` on states.
- **Dependencies:** None. Structurally requires `initial` to exist (which it always does in stateful precepts).
- **Concrete example — Insurance Claim:**
  ```precept
  event Submit entry

  from Draft on Submit
      -> set ClaimantName = Submit.Claimant
      -> set ClaimAmount = Submit.Amount
      -> transition Submitted

  # No rows for Submit from any other state — `entry` enforces this
  ```
- **Value/practicality assessment:** Low implementation cost — source-state scan. Clear value for every stateful precept (Submit, SubmitApplication, Create events are universal). Makes the intake pattern explicit. The structural corollary of `initial` on states.

---

##### `advancing`

- **Syntax:** `event Approve advancing`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Every successful outcome is a state transition. When this event succeeds, the entity always moves forward.
- **Compile-time check:** Error if any row for this event produces a `no transition` outcome. Reject rows are permitted (the event can fail, but when it succeeds, it always transitions).
- **Tooling impact:** Diagram renders advancing events as the primary routing edges. Completions suppress `no transition` as an outcome when authoring rows for advancing events. AI knows to generate `transition` outcomes.
- **Precedent:** No surveyed system has an explicit `advancing` keyword. The concept exists implicitly — BPMN sequence flows between activities always advance, and Step Functions transitions always move to the next state. But the explicit annotation on events is novel.
- **Dependencies:** None.
- **Concrete example — Insurance Claim:**
  ```precept
  event Submit advancing         # intake — always transitions on success
  event AssignAdjuster advancing # routing — always transitions on success
  event Approve advancing        # routing — transitions to Approved
  event Deny advancing           # routing — transitions to Denied
  event PayClaim advancing       # routing — transitions to Paid
  ```
- **Value/practicality assessment:** High leverage. The `advancing`/`settling` split is the single most valuable modifier discovery from the research. Every stateful workflow has a natural division between events that move the lifecycle forward and events that accumulate data. Making this visible at the declaration level gives authors, AI, and tooling an immediate structural signal. Low implementation cost — row outcome-shape check.

---

##### `settling`

- **Syntax:** `event VerifyDocuments settling`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Every successful outcome is `no transition`. The event accumulates data or performs mutations without changing state.
- **Compile-time check:** Error if any row for this event produces a `transition` outcome. Reject rows are permitted.
- **Tooling impact:** Diagram renders settling events with lighter weight (grouped near their host state, not as primary edges). Completions suppress `transition` as an outcome when authoring rows for settling events. AI knows to generate `no transition` outcomes.
- **Precedent:** Same as `advancing` — no direct precedent. The concept maps to BPMN's in-place activities and XState's `assign` (data mutations without state change), but no explicit keyword exists.
- **Dependencies:** None.
- **Concrete example — Insurance Claim:**
  ```precept
  event RequestDocument settling   # data accumulation — stays in state
  event ReceiveDocument settling   # data accumulation — stays in state
  ```
- **Value/practicality assessment:** Equal in leverage to `advancing` — they are complementary halves of the same insight. Low implementation cost — row outcome-shape check. Natural pairing with `advancing` (events are either one or the other in practice, with rare exceptions).

---

##### `isolated`

- **Syntax:** `event PayClaim isolated`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Fires from exactly one state. A phase-specific action tied to a single lifecycle position.
- **Compile-time check:** Error if 2+ distinct source states have transition rows for this event.
- **Tooling impact:** Inspector shows "Isolated event — fires only from {StateName}." Diagram groups isolated events visually with their host state. Completions suppress this event when authoring rows for non-host states.
- **Precedent:** No direct precedent. The concept is implicit in Step Functions (each Task state has its own Next) and BPMN (activities are typically scoped to one pool lane), but no system explicitly marks events as single-state.
- **Dependencies:** None.
- **Concrete example — Insurance Claim:**
  ```precept
  event AssignAdjuster isolated   # fires from Submitted only
  event PayClaim isolated         # fires from Approved only
  ```
- **Value/practicality assessment:** Low implementation cost — source-state count. Clear value for phase-specific events that would be a bug if they fired from the wrong state. Many real events are naturally isolated (PayClaim, FundLoan, AcceptOffer, MarkReturnReceived).

---

##### `completing`

- **Syntax:** `event Close completing`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Transitions only to `terminal` states. A lifecycle-ending event.
- **Compile-time check:** Error if any transition row for this event targets a non-terminal state.
- **Tooling impact:** Diagram renders completing events as final edges. Inspector shows "Completing event — targets only terminal states."
- **Precedent:** BPMN has Terminate End Events. Step Functions workflows have terminal transitions. The concept is common but not expressed as an event-level keyword.
- **Dependencies:** Requires `terminal` to exist (the check references terminal state status). Without `terminal`, the compiler would need to infer terminal states from graph topology.
- **Concrete example:**
  ```precept
  state Approved terminal
  state Denied terminal

  event Approve completing       # can only transition to terminal states
  event Deny completing          # can only transition to terminal states
  ```
- **Value/practicality assessment:** Clear concept. Medium implementation complexity — requires cross-referencing target states against terminal status. Value depends on `terminal` being available first. In many workflows, the same event (e.g., Approve) targets both terminal and non-terminal states depending on guards, which would conflict with `completing`. Best suited for single-target completion events (PayClaim → Paid, FundLoan → Funded, AcceptOffer → Hired).

---

##### `guarded` (event)

- **Syntax:** `event Approve guarded`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Every transition row for this event must have a `when` clause. No unguarded successes.
- **Compile-time check:** Error if any row for this event lacks a `when` clause.
- **Tooling impact:** Inspector shows "Guarded event — all rows require conditions." Completions always prompt for a guard when authoring rows for this event.
- **Precedent:** XState has named guard functions on transitions. Stateless has `PermitIf`/`PermitReentryIf`. The concept of guarded transitions is universal; the event-level declaration that ALL rows must be guarded is novel.
- **Dependencies:** None.
- **Concrete example:**
  ```precept
  event Approve guarded

  from UnderReview on Approve
      when DocumentsVerified and CreditScore >= 680
      -> set ApprovedAmount = Approve.Amount -> transition Approved
  from UnderReview on Approve -> reject "Requirements not met"
  ```
- **Value/practicality assessment:** Conceptually sound but conflicts with the idiomatic Precept pattern of a final reject-fallback row without a guard (e.g., `from UnderReview on Approve -> reject "..."`). The reject-fallback row serves as the `else` branch and is intentionally unguarded. A `guarded` event would require either (a) exempting reject rows from the check, or (b) requiring the author to add a redundant `when` to the fallback. This design tension needs resolution before implementation. More complex than `guarded` on states because of the reject-row interaction.

---

##### `total`

- **Syntax:** `event Approve total`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Every state that handles this event has at least one non-reject row. The event can succeed from every state where it appears. An inversion of the C51 (reject-only pair) diagnostic — upgraded to a hard error.
- **Compile-time check:** Error if any (State, Event) pair has only reject rows.
- **Tooling impact:** Inspector shows "Total event — succeeds from every handling state." Diagnostic is an upgrade of existing C51 warning to error.
- **Precedent:** No direct precedent. The concept maps to "totality" in functional programming pattern matching.
- **Dependencies:** None.
- **Concrete example:**
  ```precept
  event Submit total    # Submit should never be reject-only in any state

  from Draft on Submit -> transition Submitted
  # If any other state handled Submit and only rejected, `total` would error
  ```
- **Value/practicality assessment:** Incremental value over the existing C51 diagnostic, which already warns about reject-only pairs. The modifier upgrades the severity from warning to error for specific events. Low implementation cost (reuses C51 analysis). The question is whether the upgrade from warning to error justifies a new keyword — the compiler already catches this.

---

##### `universal`

- **Syntax:** `event Cancel universal`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Fires from every reachable non-terminal state. The event is available everywhere in the lifecycle.
- **Compile-time check:** Error if any reachable non-terminal state lacks transition rows for this event.
- **Tooling impact:** Diagram renders universal events distinctively. Inspector shows "Universal event — available from all states."
- **Precedent:** No direct precedent. XState's `always` transitions are state-level, not event-level. The concept maps to global interrupts in BPMN.
- **Dependencies:** Enhanced by `terminal` (the check excludes terminal states, which need to be identified).
- **Concrete example:**
  ```precept
  event Cancel universal         # cancellation available from any non-terminal state

  from Draft on Cancel -> transition Cancelled
  from Submitted on Cancel -> transition Cancelled
  from UnderReview on Cancel -> transition Cancelled
  # ... one row per non-terminal state
  ```
- **Value/practicality assessment:** The concept overlaps with the existing `from any on Event` mechanism, which already provides a shorthand for broad event handling. If `from any` covers the same ground, `universal` as a modifier adds authorial intent but limited structural value beyond what `from any` already provides. The interaction between `universal` and `from any` needs design work.

---

##### `irreversible`

- **Syntax:** `event Approve irreversible`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** No path from the target state back to the source state. Once this event fires, you can't go back.
- **Compile-time check:** Graph reverse-reachability analysis. Error if any target state has a path back to any source state (treating all edges as traversable).
- **Tooling impact:** Diagram renders irreversible transitions with a distinctive arrow style (one-way marker). Inspector shows "Irreversible — no return path."
- **Precedent:** TLA+ can express this as a path property. No workflow system has an explicit keyword for this.
- **Dependencies:** None, but guard-dependent paths make the analysis an overapproximation.
- **Concrete example:**
  ```precept
  event Approve irreversible     # once approved, no path back to UnderReview

  from UnderReview on Approve -> transition Approved
  # No transition from Approved or any reachable state back to UnderReview
  ```
- **Value/practicality assessment:** Graph reverse-reachability is computationally straightforward but guard-dependent. The overapproximation (treating all edges as traversable) means the check may flag paths that are infeasible due to guards. Most useful when combined with `terminal` (transitions to terminal states are trivially irreversible). For non-terminal targets, the guarantee is weaker and the false-positive risk is higher.

---

##### `symmetric`

- **Syntax:** `event Escalate symmetric`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** For every transition this event produces, a return path exists (via any event) from the target back to the source.
- **Compile-time check:** Graph adjacency check for reverse edges. Error if any target state has no path back to the source state.
- **Tooling impact:** Diagram renders symmetric transitions as bidirectional arrows.
- **Precedent:** No precedent in workflow systems. The concept exists in graph theory (symmetric relations).
- **Dependencies:** None.
- **Concrete example:**
  ```precept
  event Escalate symmetric       # can always de-escalate back

  from InProgress on Escalate -> transition Escalated
  from Escalated on DeEscalate -> transition InProgress
  ```
- **Value/practicality assessment:** Niche domain applicability. Most business workflows are directional — entities move forward through a lifecycle, not back and forth. The concept is structurally sound but demand is low. Applicable to bidirectional workflows like ticket escalation/de-escalation or hold/resume patterns, which are a small fraction of real-world use cases.

---

##### `fallback`

- **Syntax:** `event Approve fallback`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Every (state, event) group handling this event has exactly one unguarded row — the fallback/else branch. Ensures the event always has a defined outcome regardless of guard evaluation.
- **Compile-time check:** For each state that handles this event, count rows without a `when` clause. Error if zero (no fallback) or more than one (ambiguous fallback). Exactly one unguarded row per handling state required.
- **Tooling impact:** Inspector shows "Fallback event — every handling state has a defined else branch." Completions prompt for a fallback row when all existing rows have guards. AI consumers know this event is fully covered in every state.
- **Precedent:** Pattern matching exhaustiveness in functional languages (Rust, Haskell). Default case in switch statements. The Precept idiomatic pattern of guarded success + unguarded reject follows this pattern naturally.
- **Dependencies:** None. Strengthened by `guarded` (event, #18) — a `guarded fallback` event has guards on all non-fallback rows and exactly one unguarded fallback per state. Related to `covered` (#60), which is the broader coverage concept.
- **Concrete example — Loan Application:**
  ```precept
  event Approve fallback

  from UnderReview on Approve when DocumentsVerified and CreditScore >= 680
      -> set ApprovedAmount = Approve.Amount -> transition Approved
  from UnderReview on Approve -> reject "Requirements not met"   # ← the one fallback
  ```
- **Value/practicality assessment:** High. Maps directly to Precept's idiomatic guard-then-reject pattern. The check is simple (count unguarded rows per state group). Every event in the loan, insurance, and hiring samples follows this pattern. Makes the "always has a defined outcome" guarantee explicit. Low implementation cost.
- **Origin:** Frank creative sweep

---

##### `disjoint`

- **Syntax:** `event Approve disjoint`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Guard conditions across rows within each (state, event) group are provably non-overlapping. No two guarded rows can both match the same data snapshot.
- **Compile-time check:** For each pair of guarded rows within a (state, event) group, attempt to prove their `when` expressions are mutually exclusive. **Partial provability:** feasible for boolean flags (`when X` vs `when not X`), enum equality (`when Status == "A"` vs `when Status == "B"`), and simple boolean conjunctions. Infeasible for general arithmetic comparisons or cross-field expressions without symbolic reasoning.
- **Tooling impact:** Inspector shows "Disjoint event — guards don't overlap." Diagram could render guard branches with non-overlapping markers. AI consumers know that at most one guarded row matches.
- **Precedent:** Pattern matching non-overlapping checks in Rust/OCaml (compiler warns on overlapping patterns). BPMN exclusive gateways assume non-overlapping conditions (though not enforced).
- **Dependencies:** None. Complementary to `fallback` (#56) — a `disjoint fallback` event has non-overlapping guards AND exactly one unguarded fallback per state. Operates within a single event; `exclusive` (#51) operates across events within a state.
- **Concrete example:**
  ```precept
  event Route disjoint

  from Decision on Route when FeedbackCount >= 3 -> transition OfferExtended
  from Decision on Route when FeedbackCount < 3 and FeedbackCount >= 1 -> transition SecondRound
  from Decision on Route -> reject "No feedback recorded"
  ```
- **Value/practicality assessment:** Medium. Partial provability is the main constraint — the compiler can prove disjointness for simple boolean/enum guards but not for compound arithmetic. The concept is valuable for code review (guarantee no overlapping match) but limited by expression analysis capability. Including per Shane's criteria despite partial provability.
- **Origin:** Frank creative sweep

---

##### `paired with <Event>`

- **Syntax:** `event Approve paired with Deny`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** The two events share the same set of source states. Wherever one is available, the other is too.
- **Compile-time check:** Compute the set of source states for both events (states with transition rows for the event). Error if the sets differ.
- **Tooling impact:** Inspector shows "Paired with Deny — same source states." Diagram renders paired events as co-located edge groups. AI consumers know these events are always available together.
- **Precedent:** No direct precedent in workflow systems. The concept maps to complementary operations (approve/deny, accept/reject) that should be available in the same lifecycle positions.
- **Dependencies:** None. Parameterized syntax follows the precedent of `sealed after <State>` and `ordered before <State>`.
- **Concrete example — Insurance Claim:**
  ```precept
  event Approve paired with Deny

  from UnderReview on Approve when ... -> transition Approved
  from UnderReview on Approve -> reject "Requirements not met"
  from UnderReview on Deny -> set DecisionNote = Deny.Note -> transition Denied
  # Both Approve and Deny are available from UnderReview — pairing satisfied
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (source state set comparison). Maps to the common pattern where approval and denial events are always offered together. Low implementation cost. The parameterized syntax adds vocabulary but the concept is intuitive.
- **Origin:** Frank creative sweep

---

##### `after <Event>`

- **Syntax:** `event FundLoan after Approve`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** This event's source states are a subset of the states reachable via the predecessor event's transitions. The event fires only from states the predecessor can reach.
- **Compile-time check:** Compute the predecessor event's target states (all `transition` targets from the predecessor's rows). Compute this event's source states. Error if this event's source states are not a subset of the predecessor's targets.
- **Tooling impact:** Inspector shows "After Approve — fires from Approve's target states." Diagram renders a sequencing arrow between events. AI consumers know the lifecycle ordering between events.
- **Precedent:** BPMN sequence flows between tasks define ordering. No workflow system has an explicit event-ordering keyword.
- **Dependencies:** None. Parameterized syntax follows existing `sealed after <State>` precedent.
- **Concrete example — Loan Application:**
  ```precept
  event FundLoan after Approve

  # Approve transitions to Approved
  from UnderReview on Approve when ... -> transition Approved
  # FundLoan fires from Approved — a subset of Approve's targets
  from Approved on FundLoan -> transition Funded
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (target set of predecessor vs source set of this event). Captures lifecycle sequencing between events. Maps to real patterns — FundLoan naturally follows Approve, PayClaim follows Approve in insurance. Low implementation cost. The value is primarily authorial intent + documentation — the sequencing is usually visible from the transition rows.
- **Origin:** Frank creative sweep

---

##### `covered`

- **Syntax:** `event Approve covered`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Guards and the fallback row together cover all possible cases. The event has a defined outcome for every reachable data configuration.
- **Compile-time check:** **Partial provability.** The compiler can verify: (a) at least one unguarded fallback row exists per state group (simple), or (b) guards form a complete partition (requires expression analysis). Check (a) is straightforward; check (b) is feasible for simple guard patterns but undecidable in general.
- **Tooling impact:** Inspector shows "Covered event — all cases handled." AI consumers know this event cannot "fall through" without an outcome.
- **Precedent:** Exhaustive pattern matching in Rust/ML. Switch statement coverage analysis. The concept is well-established in PL theory.
- **Dependencies:** Enhanced by `fallback` (#56) — if an event has a fallback in every state, it is trivially covered. Enhanced by `disjoint` (#57) — disjoint guards with a fallback guarantee full coverage.
- **Concrete example:**
  ```precept
  event Approve covered

  from UnderReview on Approve when DocumentsVerified and CreditScore >= 680 -> transition Approved
  from UnderReview on Approve -> reject "Requirements not met"
  # Guarded success + unguarded reject = full coverage
  ```
- **Value/practicality assessment:** Medium. The simple form (has an unguarded fallback) is trivially checkable and maps to the standard Precept pattern. The stronger form (guard completeness analysis) requires expression analysis that's only partially feasible. Most practical value comes from the simple form, which overlaps significantly with `fallback` (#56). The modifier adds a coverage-semantics lens rather than the structural-fallback lens.
- **Origin:** Frank creative sweep

---

##### `resetting`

- **Syntax:** `event Reset resetting`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** All field mutations in this event's rows set fields to their declared default values. A cleanup/reset event.
- **Compile-time check:** For every `set` action in every row of this event, verify the assigned value matches the target field's declared `default`. Error if any `set` assigns a non-default value.
- **Tooling impact:** Inspector shows "Resetting event — sets fields to defaults." Diagram renders with a reset icon (↺). AI consumers know this event is a cleanup step.
- **Precedent:** Reset patterns in state machines. Initialization actions in BPMN sub-processes. No workflow system has an explicit `resetting` keyword.
- **Dependencies:** Requires fields to have `default` declarations (otherwise there's nothing to reset to).
- **Concrete example:**
  ```precept
  field FraudFlag as boolean default false
  field AdjusterName as string nullable   # nullable default is null

  event ResetReview resetting

  from UnderReview on ResetReview -> set FraudFlag = false -> set AdjusterName = null -> transition Submitted
  # Both mutations set to the declared defaults — resetting satisfied
  ```
- **Value/practicality assessment:** Low-medium. The check is straightforward (compare `set` values to declared defaults). The concept applies to cleanup/re-queue events in real workflows. Limited demand — most events set non-default values as their primary purpose. Resetting events are rare in samples.
- **Origin:** Frank creative sweep

---

##### `intake`

- **Syntax:** `event Submit intake`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Composite of `entry` (#13) semantics + sets multiple fields from event arguments. The lifecycle's initialization event — fires from initial state and populates the entity's data from args.
- **Compile-time check:** Combined checks: (a) fires only from initial state (same as `entry`), (b) at least 2 distinct fields are set from event argument references in the event's rows.
- **Tooling impact:** Inspector shows "Intake event — initializes entity from args." Diagram renders with a bold entry edge plus data-flow indicators. AI consumers know this is the lifecycle ignition point with data population.
- **Precedent:** Constructor pattern in OOP. GraphQL `create` mutation. No workflow system has an explicit `intake` keyword.
- **Dependencies:** Structurally includes `entry` semantics. If `entry` (#13) exists, `intake` subsumes it.
- **Concrete example — Insurance Claim:**
  ```precept
  event Submit intake

  from Draft on Submit
      -> set ClaimantName = Submit.Claimant
      -> set ClaimAmount = Submit.Amount
      -> set PoliceReportRequired = Submit.RequiresPoliceReport
      -> transition Submitted
  # Fires from initial (Draft) AND sets 3 fields from args — intake satisfied
  ```
- **Value/practicality assessment:** Medium. Maps to the universal pattern in every sample — the first event sets up the entity's data. Low implementation cost (compositional check). The question is whether the one-keyword convenience over `entry` + the multi-field pattern justifies a separate keyword. For AI authoring, `intake` signals both structural and data-flow intent in one word.
- **Origin:** Frank creative sweep

---

##### `idempotent`

- **Syntax:** `event MarkReturnReceived idempotent`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Repeated firing produces the same result as a single firing. The event's mutations are naturally idempotent.
- **Compile-time check:** **Partial provability.** For each `set` action, analyze whether the assignment is idempotent:
  - `set X = true` → idempotent (same value regardless of current X)
  - `set X = "constant"` → idempotent
  - `set X = EventArg.Value` → idempotent for fixed args (same args → same result)
  - `set X = X + 1` → NOT idempotent (value increases on each firing)
  - `add Collection Item` → NOT idempotent for sets (but IS idempotent if item already present)
  
  The compiler can prove idempotency for constant and flag assignments. General expression idempotency is undecidable. Collection operations have mixed provability.
- **Tooling impact:** Inspector shows "Idempotent event — safe to re-fire." AI consumers know this event can be retried safely. MCP metadata marks idempotent events for consumer use.
- **Precedent:** HTTP PUT/DELETE idempotency. `createOrUpdate` patterns in APIs. Idempotent message handlers in event-driven systems. The concept is critical in distributed systems.
- **Dependencies:** None. 
- **Note:** Previously excluded in §5 as "undecidable in the general case." Re-evaluated under Shane's inclusion criteria: partial provability exists for common patterns (constant assignments, flag sets). Moved to candidate list with honest partial-provability assessment.
- **Concrete example — Refund Request:**
  ```precept
  event MarkReturnReceived idempotent

  from AwaitingReturn on MarkReturnReceived -> set ReturnReceived = true -> transition Refunded
  # `set ReturnReceived = true` is idempotent — provable by compiler
  ```
- **Value/practicality assessment:** Medium. The concept has strong practical demand (retry safety is valuable in integrations). Partial provability limits the guarantee — the compiler can certify idempotency for flag/constant mutations but not for arithmetic or collection operations. Starting with the provable subset (flag/constant assignments) and being honest about the boundary is the right approach.
- **Origin:** Frank creative sweep; re-evaluated from §5 exclusion

---

##### `global` / `broadcast`

- **Syntax:** `event Cancel global`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** All transition rows for this event use `from any` as their source. The event is not scoped to specific states — it's handled system-wide.
- **Compile-time check:** Error if any row for this event specifies a named source state instead of `from any`.
- **Tooling impact:** Inspector shows "Global event — fires from any state via `from any`." Diagram renders with a broadcast indicator (all-states edge). AI consumers know this event is not state-scoped.
- **Precedent:** Global event handlers in BPMN (signal events). XState's `always` transitions. Broadcast patterns in actor systems.
- **Dependencies:** None. Related to `universal` (#20) — `universal` requires the event to be handled in every non-terminal state (but rows can be per-state); `global` requires all rows to use `from any` (but the event need not cover every state if `from any` rows are present).
- **Note:** *(Consolidated: Frank's `broadcast` (#28) and George's `global` (G3) — same semantics: all rows use `from any`. Keeping `global` as primary name with `broadcast` as alias.)*
- **Concrete example — IT Helpdesk Ticket:**
  ```precept
  event RegisterAgent global

  from any on RegisterAgent -> enqueue AgentQueue RegisterAgent.AgentName -> no transition
  # All rows use `from any` — global satisfied
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (source scan). Maps to real patterns — RegisterAgent in IT Helpdesk Ticket uses `from any` for all rows. The modifier makes the "system-wide event" intent explicit. Distinguishes events scoped to specific states from events available everywhere via `from any`.
- **Origin:** Frank creative sweep (`broadcast`) + George creative sweep (`global`) — consolidated

---

##### `noop` / `transparent`

- **Syntax:** `event Acknowledge noop`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** No field mutations in any row of this event. The event may transition or reject, but never changes data.
- **Compile-time check:** Error if any row for this event contains a `set`, `add`, `remove`, `enqueue`, or `dequeue` action.
- **Tooling impact:** Inspector shows "Noop event — no data changes." Diagram renders with a dotted edge (progression without mutation). AI consumers know this event is a pure routing/signaling action.
- **Precedent:** Pure functions in FP (no side effects). NOP instructions in assembly. Signal events in BPMN (routing without data transformation).
- **Dependencies:** None. Strictly weaker than `pure` (#72) — `noop` checks row-level mutations only; `pure` also verifies no entry/exit action effects.
- **Concrete example — Hiring Pipeline:**
  ```precept
  event PassScreen noop

  from Screening on PassScreen when PendingInterviewers.count > 0 -> transition InterviewLoop
  from Screening on PassScreen -> reject "At least one interviewer must be assigned"
  # No `set`/`add`/`remove` in any row — noop satisfied
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (mutation scan). Maps to real patterns — PassScreen, AcceptOffer, PayClaim, FundLoan are all mutation-free routing events in samples. The modifier makes the "pure routing" intent explicit. Low implementation cost.
- **Origin:** Frank creative sweep

---

##### `stackable`

- **Syntax:** `event AddInterviewer stackable`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Can fire repeatedly without transitioning. In every state that handles this event, at least one row produces a `no transition` outcome. The event is designed for repeated accumulation.
- **Compile-time check:** For every state that handles this event, verify at least one row has a `no transition` outcome. Error if any handling state has only `transition` or `reject` rows.
- **Tooling impact:** Inspector shows "Stackable event — repeatable without state change." Diagram renders with a loop arrow. AI consumers know this event supports repeated firing.
- **Precedent:** Accumulator operations in collection processing. `while` loop bodies. No workflow system has an explicit `stackable` keyword.
- **Dependencies:** None. Related to `settling` (#15) — a `settling stackable` event always produces `no transition` and can be repeated. `stackable` is weaker: it requires at least one `no transition` row per state, but other rows can transition.
- **Concrete example — Hiring Pipeline:**
  ```precept
  event AddInterviewer stackable

  from Screening on AddInterviewer -> add PendingInterviewers AddInterviewer.Name -> no transition
  # In Screening, AddInterviewer has a no-transition row — stackable satisfied
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (per-state outcome scan). Maps to real patterns — AddInterviewer, RequestDocument, RegisterAgent are all stackable in samples. The modifier captures the "fire this multiple times" intent. Low implementation cost.
- **Origin:** Frank creative sweep

---

##### `preserves <Field>`

- **Syntax:** `event UpdateNotes preserves ClaimAmount`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** This event never modifies the named field. An immutability guarantee scoped to one event.
- **Compile-time check:** Error if any row for this event contains a `set`, `add`, or `remove` targeting the named field.
- **Tooling impact:** Inspector shows "Preserves ClaimAmount." AI consumers know this event won't change the specified data. Completions suppress `set` suggestions for the preserved field when authoring rows for this event.
- **Precedent:** `const` parameters in C/C++. `readonly` references in C#. The concept of promising not to modify specific data is universal.
- **Dependencies:** None. Parameterized syntax follows `sealed after <State>` precedent.
- **Concrete example — Insurance Claim:**
  ```precept
  event RequestDocument preserves ClaimAmount

  from UnderReview on RequestDocument -> add MissingDocuments RequestDocument.Name -> no transition
  # No mutation of ClaimAmount — preserves satisfied
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (targeted mutation scan). Valuable for critical fields that should not be changed by certain events (e.g., ClaimAmount should never change after submission). Low implementation cost. The modifier is more surgical than `readonly` (state) — it's per-event per-field rather than per-state all-fields.
- **Origin:** Frank creative sweep

---

##### `focused`

- **Syntax:** `event FundLoan focused`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** All transitions produced by this event target the same state. The event routes to a single destination.
- **Compile-time check:** Collect all distinct `transition` target states from this event's rows (across all source states). Error if more than one distinct target exists. Reject and `no transition` rows are exempt.
- **Tooling impact:** Inspector shows "Focused event — all transitions target {State}." Diagram renders with a single-target edge. AI consumers know this event has one destination.
- **Precedent:** No direct precedent. The concept maps to unconditional routing in pipeline systems.
- **Dependencies:** None. Enhanced by `completing` (#17) — a `completing focused` event transitions to exactly one terminal state.
- **Concrete example — Loan Application:**
  ```precept
  event FundLoan focused

  from Approved on FundLoan -> transition Funded
  # Only one transition target (Funded) — focused satisfied
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (target set uniqueness). Maps to real patterns — FundLoan, PayClaim, AcceptOffer all target a single state. Many single-row events are trivially focused. Value is primarily for events with multiple guarded rows that all converge to the same target.
- **Origin:** George creative sweep

---

##### `narrowing`

- **Syntax:** `event Approve narrowing`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** For every transition this event produces, the target state has fewer outgoing event-handling options than the source state. The event moves the entity toward a more constrained lifecycle position.
- **Compile-time check:** **Partial provability.** For each transition row, count the number of distinct events handled by the source state and the target state. Error if any target has equal or more outgoing event types than its source. The "option count" metric is a structural approximation — guard-dependent reachability may differ.
- **Tooling impact:** Inspector shows "Narrowing event — targets have fewer options." Diagram could render with a funnel indicator. AI consumers know this event reduces the entity's available actions.
- **Precedent:** Funnel metrics in conversion analysis. Process narrowing in lifecycle management. No workflow system has an explicit narrowing keyword.
- **Dependencies:** None. Enhanced by `completing` (#17) — completing events are inherently narrowing (terminal states have zero outgoing options).
- **Concrete example — Hiring Pipeline:**
  ```precept
  event AcceptOffer narrowing

  # OfferExtended handles: AcceptOffer
  # Hired (terminal) handles: nothing
  from OfferExtended on AcceptOffer -> transition Hired
  # Hired has fewer options (0) than OfferExtended (1) — narrowing satisfied
  ```
- **Value/practicality assessment:** Low-medium. Partially provable — the "options" metric is approximative. The concept captures lifecycle progression toward endpoints, which is intuitive. Limited practical leverage beyond what `completing` and `advancing` already provide. The definition of "fewer options" needs design work (count events? count non-reject rows? count reachable states?).
- **Origin:** George creative sweep

---

##### `deterministic` (event)

- **Syntax:** `event PayClaim deterministic`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** In every state that handles this event, there is exactly one row and that row has no guard. The event's outcome is fully determined by position alone — no conditional logic.
- **Compile-time check:** For each state handling this event, verify: (a) exactly one row exists, and (b) that row has no `when` clause. Error on multiple rows or any guarded row.
- **Tooling impact:** Inspector shows "Deterministic event — single unguarded row per state." Diagram renders with a solid arrow (no guard branching). AI consumers know this event has no conditional logic.
- **Precedent:** Deterministic finite automata (DFA) — transitions with no branching ambiguity.
- **Dependencies:** None. Subsumes `unguarded` (#71) — a deterministic event is always unguarded, but also has exactly one row per state (unguarded permits multiple unguarded rows).
- **Note:** The word `deterministic` was previously excluded at the **precept** level in §5 because ALL precepts are deterministic by design (Principle #6). This candidate operates at the **event** level with a different meaning: not "the engine is deterministic" (which is always true) but "this specific event has trivially predictable outcomes" (single unguarded row per state).
- **Concrete example — Loan Application:**
  ```precept
  event FundLoan deterministic

  from Approved on FundLoan -> transition Funded
  # One row, no guard — deterministic satisfied
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (row count + guard check per state). Maps to real patterns — FundLoan, PayClaim, AcceptOffer, Pay are all single-row unguarded events. The modifier captures the "this event has no conditional logic" intent. Distinguishes simple routing events from complex guarded events.
- **Origin:** George creative sweep

---

##### `unguarded`

- **Syntax:** `event PayClaim unguarded`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** No rows for this event have `when` clauses. The event has no conditional logic — every row is unconditional.
- **Compile-time check:** Error if any row for this event has a `when` clause.
- **Tooling impact:** Inspector shows "Unguarded event — no conditions." Diagram renders without guard labels. AI consumers know this event always fires unconditionally.
- **Precedent:** Complement of guarded transitions. Unconditional transitions in simple state machines.
- **Dependencies:** None. Complement of `guarded` (event, #18) — a `guarded` event requires all rows to have guards; `unguarded` requires zero guards. Subsumed by `deterministic` (#70) — `deterministic` implies `unguarded` plus single-row-per-state.
- **Concrete example:**
  ```precept
  event AcceptOffer unguarded

  from OfferExtended on AcceptOffer -> transition Hired
  # Zero `when` clauses — unguarded satisfied
  ```
- **Value/practicality assessment:** Low-medium. Simple compile-time check (guard scan). The property is derivable from reading the rows. The keyword adds authorial intent but limited structural leverage. Moderate overlap with `deterministic` (#70). The `guarded`/`unguarded` pair provides completeness for the guard-presence vocabulary.
- **Origin:** George creative sweep

---

##### `pure`

- **Syntax:** `event Acknowledge pure`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** Zero mutations anywhere — no field mutations in any row AND no effects from entry/exit actions triggered by transitions. The strictest no-side-effect guarantee.
- **Compile-time check:** (a) Error if any row for this event contains `set`, `add`, `remove`, `enqueue`, or `dequeue` actions. (b) For rows that produce `transition` outcomes, check that the target state has no `to <State> -> ...` entry actions, and the source state has no `from <State> -> ...` exit actions, that would cause mutations. Error if entry/exit actions exist with mutations.
- **Tooling impact:** Inspector shows "Pure event — zero side effects." Diagram renders with a distinctive minimal style. AI consumers know this event has absolutely no data effects.
- **Precedent:** Pure functions in FP. Haskell's `IO`-free functions. The purity concept is fundamental in PL theory.
- **Dependencies:** Strictly stronger than `noop` (#65) — `noop` checks row-level mutations only; `pure` also verifies entry/exit action purity.
- **Concrete example:**
  ```precept
  event AcceptOffer pure

  from OfferExtended on AcceptOffer -> transition Hired
  # No row mutations, and assuming no entry/exit actions on Hired or OfferExtended
  # that mutate fields — pure satisfied
  ```
- **Value/practicality assessment:** Medium. Extends `noop` with entry/exit action analysis. The additional check is straightforward if entry/exit actions are in the model. In practice, most mutation-free routing events are also pure (few states have entry/exit actions). The `noop` vs `pure` distinction matters only when entry/exit actions exist — which is a minority of real precepts today.
- **Origin:** George creative sweep

---

##### `initializes <Field>`

- **Syntax:** `event Submit initializes ClaimantName`
- **Entity type:** Event
- **Mechanism:** Structural
- **What it means:** This is the only event that sets the named field to a non-default value. The field's meaningful value comes from this event alone.
- **Compile-time check:** Scan all events. Collect those that contain `set` actions targeting the named field with non-default values. Error if more than one event qualifies. The annotated event must be in the set.
- **Tooling impact:** Inspector shows "Initializes ClaimantName." AI consumers know this event is the data source for the field. Diagram could render with a data-flow indicator from event to field.
- **Precedent:** Constructor initialization in OOP. Required parameters in factory methods. The concept of single-point initialization is universal.
- **Dependencies:** None. Related to `singlewriter` (#81) on fields — `singlewriter` declares the property on the field; `initializes` declares it on the event. They are complementary perspectives of the same structural fact.
- **Concrete example — Insurance Claim:**
  ```precept
  event Submit initializes ClaimantName

  from Draft on Submit -> set ClaimantName = Submit.Claimant -> ... -> transition Submitted
  # No other event sets ClaimantName — initializes satisfied
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (mutation source scan per field). Maps to real patterns — Submit in every sample is the initializer for intake fields. The modifier declares the event→field relationship explicitly. Low implementation cost. Complements `singlewriter` from the event's perspective.
- **Origin:** Frank creative sweep

---

#### 3.1.3 Structural — Fields

##### `writeonce`

- **Syntax:** `field ClaimantName as string nullable writeonce`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** The field can be set at most once across the lifecycle. Once written, it's permanent.
- **Compile-time check:** Row-partition analysis. Error if 2+ transition rows can set the field AND reachability analysis shows both rows are on feasible paths (overapproximation — all edges traversable).
- **Tooling impact:** Inspector shows "Read-only: writeonce (already set)." Completions suppress `set` suggestions for this field after it has been written. Diagram could show write points.
- **Precedent:** The concept maps to `final` in Java, `readonly` in C#, and `val` in Kotlin — though those are lexical scope constraints, not lifecycle constraints. No workflow system has lifecycle-scoped write-once semantics at the language level.
- **Dependencies:** None.
- **Concrete example — Loan Application:**
  ```precept
  # Intake data locked after submission
  field ApplicantName as string nullable writeonce
  field RequestedAmount as number default 0 writeonce
  field CreditScore as number default 0 writeonce

  from Draft on Submit
      -> set ApplicantName = Submit.Applicant
      -> set RequestedAmount = Submit.Amount
      -> set CreditScore = Submit.Score
      -> transition UnderReview

  # No subsequent event can set ApplicantName, RequestedAmount, or CreditScore
  ```
- **Value/practicality assessment:** Strong demand from real samples — intake data (applicant names, claim amounts, order IDs) is naturally write-once. Medium implementation complexity — requires row mutation analysis to identify which rows set which fields, combined with reachability to determine if multiple write paths exist. The overapproximation may produce false positives for fields that are written in mutually exclusive branches. Manageable with clear diagnostic messaging.

---

##### `sealed after <State>`

- **Syntax:** `field ApprovedAmount as number default 0 sealed after Approved`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** No mutation of this field is permitted after the named state is entered. The field freezes at a specific lifecycle point.
- **Compile-time check:** Reachability + row analysis. Error if any transition row sets the field in a state that is reachable from the named state (including the named state itself, for rows that originate there).
- **Tooling impact:** Inspector shows "Sealed after Approved." Completions suppress `set` suggestions for this field in states reachable from the seal point. Diagram could show freeze points.
- **Precedent:** No direct precedent as a single keyword. The concept maps to lifecycle-phase immutability in enterprise systems (e.g., SAP document locking after posting). TLA+ can express this as a safety property.
- **Dependencies:** Enhanced by `terminal` — `sealed after` a terminal state is maximally clear (no further mutations possible by definition). Supports multiple states: `sealed after Approved, Denied` (freezes when either is entered).
- **Concrete example — Insurance Claim:**
  ```precept
  field ApprovedAmount as number default 0 sealed after Approved
  field DecisionNote as string nullable sealed after Approved, Denied

  from UnderReview on Approve
      -> set ApprovedAmount = Approve.Amount
      -> set DecisionNote = Approve.Note
      -> transition Approved

  # After reaching Approved, no event can set ApprovedAmount or DecisionNote
  ```
- **Value/practicality assessment:** Strong conceptual value — lifecycle-phase immutability is a common business requirement (approved amounts shouldn't change after approval). Medium implementation complexity — requires reachability analysis from the seal state combined with row mutation analysis. Multi-state `sealed after` (freeze at any of several states) adds parser and analysis complexity but follows existing multi-state syntax (`in S1, S2 assert`). Open question: can a `sealed after` field's seal state be the same as the state where it's last written? (Yes — the seal activates *after* the transition's mutations complete.)

---

##### `immutable`

- **Syntax:** `field TaxRate as number default 0.07 immutable`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** Never mutated after creation. Only the `default` value applies — no `set` or `edit` can target this field.
- **Compile-time check:** Error on any `set` or `edit` targeting this field, anywhere in the definition.
- **Tooling impact:** Inspector shows "Immutable — default value only." Completions never suggest this field as a `set` target.
- **Precedent:** `const` in many languages. `readonly` in TypeScript/C# (at construction time). The concept is universal.
- **Dependencies:** Requires `default` to be present (otherwise the field can never have a value).
- **Concrete example:**
  ```precept
  field TaxRate as number default 0.07 immutable
  field Currency as string default "USD" immutable
  ```
- **Value/practicality assessment:** Narrow use case. Most fields that never change are configuration constants that belong outside the precept definition. The overlap with `writeonce` and `default` makes this partially redundant — a field with `default` that's never targeted by `set` is effectively immutable already. A keyword adds authorial intent but limited structural leverage beyond what the absence of `set` targets already implies.

---

##### `monotonic`

- **Syntax:** `field ProcessedDocuments as set of string monotonic` or `field FeedbackCount as number default 0 monotonic`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** Value only increases (numbers) or only grows (collections). Reversals are prohibited.
- **Compile-time check:** For collections: verb check — error if `remove` is used on a monotonic collection. For numbers: expression analysis — error if any `set` assigns a value that could be less than the current value. The number check has split provability: some expressions are provably monotonic (`X + 1`), others are not (`X + Arg.Delta` where Delta could be negative).
- **Tooling impact:** Inspector shows "Monotonic — value only increases." Completions suppress `remove` for monotonic collections.
- **Precedent:** CRDTs (conflict-free replicated data types) use grow-only sets and counters. Alloy's `fact` constraints can express monotonicity. No workflow system has this as a keyword.
- **Dependencies:** None.
- **Concrete example — Insurance Claim:**
  ```precept
  field MissingDocuments as set of string            # can add and remove
  field ProcessedDocuments as set of string monotonic # can only add — never lose a processed doc

  from UnderReview on ProcessDocument
      -> add ProcessedDocuments ProcessDocument.Name
      -> no transition

  # remove ProcessedDocuments would be a compile error
  ```
- **Value/practicality assessment:** Split provability is the main challenge. Collection monotonicity (grow-only) is fully provable — just ban `remove`. Number monotonicity is partially provable — `set X = X + 1` is provably increasing, but `set X = EventArg.Value` is not provable without knowing the argument's value at runtime. Starting with collection-only monotonicity is practical. Number monotonicity would require either conservative rejection of unprovable expressions or runtime-only enforcement.

---

##### `identity`

- **Syntax:** `field OrderId as string nullable identity`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** Identifying data — combines `writeonce` semantics with additional tooling weight. This field identifies the entity and should be treated as primary in displays.
- **Compile-time check:** Same as `writeonce` (set at most once). Additionally, `edit` declarations cannot target identity fields.
- **Tooling impact:** Inspector shows identity fields prominently. MCP metadata marks identity fields for consumer prioritization. Diagram could render identity field values as state labels.
- **Precedent:** Primary keys in databases. `[Key]` attribute in Entity Framework. `id` fields in GraphQL schemas. The concept is universal but the lifecycle-constrained version (writeonce + tooling weight) is novel.
- **Dependencies:** Subsumes `writeonce` — an `identity` field is automatically writeonce. The subsumption relationship needs a design decision: does `identity` *imply* `writeonce`, or does the author write both?
- **Concrete example:**
  ```precept
  field ClaimNumber as string nullable identity
  field ClaimantName as string nullable writeonce

  # ClaimNumber is writeonce AND gets identity treatment in tooling
  ```
- **Value/practicality assessment:** The concept is clear, but the relationship with `writeonce` creates a subsumption question. If `identity` implies `writeonce`, then `identity` is a superset and the author doesn't need both. If they're independent, then `identity` without `writeonce` means "identifying but changeable," which is unusual. The tooling weight is the differentiator — without concrete tooling behavior beyond `writeonce`, `identity` is just `writeonce` with a different name. Value depends on whether the tooling story justifies a separate keyword.

---

##### `derived`

- **Syntax:** `field TotalCost as number derived`
- **Entity type:** Field
- **Mechanism:** Structural (feature gate)
- **What it means:** Computed from other fields. No direct `set` or `edit` can target this field — it's calculated.
- **Compile-time check:** Error on any `set` or `edit` targeting this field. The field must have a derivation expression (future syntax — depends on computed fields proposal #17).
- **Tooling impact:** Inspector shows "Derived — computed automatically." Completions never suggest this field as a `set` target. Diagram could show computation dependencies.
- **Precedent:** Computed properties in C#, derived attributes in UML, calculated fields in databases. Universal concept.
- **Dependencies:** Functionally depends on the computed fields proposal (#17). Without a derivation expression mechanism, `derived` only prevents writes — it doesn't specify what the computed value is.
- **Concrete example:**
  ```precept
  field LodgingTotal as number default 0
  field MealsTotal as number default 0
  field MileageTotal as number default 0
  field TotalCost as number derived    # = LodgingTotal + MealsTotal + MileageTotal
  # Derivation expression syntax TBD (proposal #17)
  ```
- **Value/practicality assessment:** The modifier itself (write-ban) is trivial to implement. The value proposition depends entirely on computed fields (#17) providing the derivation expression mechanism. Without #17, `derived` is just `immutable` with no `default` — a field that can never have a value, which is useless. Best treated as part of the computed fields proposal, not as an independent modifier.

---

##### `flag`

- **Syntax:** `field ReturnReceived as boolean default false flag`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** A boolean that starts `false` and can only be set to `true`. Boolean monotonic — once flipped, stays flipped.
- **Compile-time check:** (a) Field type must be `boolean`. (b) Default must be `false`. (c) Every `set` targeting this field must assign `true`. Error if any `set` assigns `false` or an expression that could evaluate to `false`.
- **Tooling impact:** Inspector shows "Flag — can only flip to true." Completions suppress `set X = false` suggestions for flag fields. AI consumers know this field is a one-way latch.
- **Precedent:** Boolean flags in every programming language. Feature flags. The concept is universal. The lifecycle-scoped monotonic boolean is a specific pattern with no direct precedent as a keyword.
- **Dependencies:** None. Special case of `monotonic` (#26) restricted to booleans. Could be expressed as `monotonic` + boolean type + default false, but `flag` is a more intuitive keyword for the pattern.
- **Concrete example — Refund Request:**
  ```precept
  field ReturnReceived as boolean default false flag

  from AwaitingReturn on MarkReturnReceived -> set ReturnReceived = true -> transition Refunded
  # Only sets to true — flag satisfied
  ```
- **Value/practicality assessment:** High. The boolean flag pattern is extremely common in samples — `ReturnReceived`, `DocumentsVerified`, `PoliceReportRequired`, `FraudFlag`, `MarketingOptIn`. Simple compile-time check (type + default + value analysis). Low implementation cost. Clear, intuitive keyword for a pervasive pattern.
- **Origin:** Frank creative sweep

---

##### `localized to <State>`

- **Syntax:** `field FraudFlag as boolean default false localized to UnderReview`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** All mutations to this field occur from transition rows originating in the named state only. The field's data is a product of one lifecycle phase.
- **Compile-time check:** Scan all transition rows that contain `set`, `add`, or `remove` targeting this field. Error if any such row originates from a state other than the named state. `edit` declarations are also checked — the field should only be editable in the named state.
- **Tooling impact:** Inspector shows "Localized to UnderReview — mutations only from this state." Diagram could associate the field visually with its host state. AI consumers know where this field's data comes from.
- **Precedent:** Scope locality in programming languages. The concept of phase-scoped data is common in business process management.
- **Dependencies:** None. Parameterized syntax follows `sealed after <State>` precedent.
- **Concrete example — Insurance Claim:**
  ```precept
  field FraudFlag as boolean default false localized to UnderReview

  in UnderReview edit FraudFlag
  # FraudFlag is only edited/mutated from UnderReview — localized satisfied
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (mutation source scan). Maps to real patterns — FraudFlag in Insurance Claim is only editable in UnderReview. The modifier captures scoped field ownership. Low implementation cost.
- **Origin:** Frank creative sweep

---

##### `required in <State>`

- **Syntax:** `field AdjusterName as string nullable required in UnderReview`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** The field must be non-null (or non-default for non-nullable fields) when the entity is in the named state. Syntactic sugar for `in <State> assert <Field> != null`.
- **Compile-time check:** Equivalent to injecting `in <State> assert <Field> != null because "<Field> is required in <State>"`. Same enforcement as a state assertion.
- **Tooling impact:** Inspector shows "Required in UnderReview." Completions could warn when the entity enters the named state without setting this field. Diagram shows field requirement badges per state.
- **Precedent:** Required fields in form validation. NOT NULL constraints in databases scoped to specific record states through application logic.
- **Dependencies:** None. Syntactic sugar for existing `in <State> assert` mechanism.
- **Concrete example — Insurance Claim:**
  ```precept
  field AdjusterName as string nullable required in UnderReview
  # Equivalent to: in UnderReview assert AdjusterName != null because "..."
  ```
- **Value/practicality assessment:** Medium. No new analysis infrastructure — desugars to existing state assert. Improves readability by keeping field requirements near the field declaration. The sugar question: does a modifier on the field declaration add clarity over a state assertion near the state? For large precepts with many fields and states, field-local requirements may be more scannable.
- **Origin:** Frank creative sweep

---

##### `capped <N>`

- **Syntax:** `field MissingDocuments as set of string capped 10`
- **Entity type:** Collection field
- **Mechanism:** Structural
- **What it means:** The collection's size is bounded by N. The collection may never contain more than N elements.
- **Compile-time check:** Equivalent to injecting `invariant <Collection>.count <= N because "<Collection> cannot exceed N items"`. Same enforcement as an invariant — checked on every operation.
- **Tooling impact:** Inspector shows "Capped at 10." MCP metadata includes size bound. AI consumers know the collection has a maximum size.
- **Precedent:** Array length limits. Buffer size bounds. `maxItems` in JSON Schema. The bounded-collection concept is common across systems.
- **Dependencies:** None. Syntactic sugar for existing invariant mechanism.
- **Concrete example — Insurance Claim:**
  ```precept
  field MissingDocuments as set of string capped 10
  # Equivalent to: invariant MissingDocuments.count <= 10 because "..."
  ```
- **Value/practicality assessment:** Medium. No new analysis — desugars to invariant. Improves readability by keeping size bounds near the collection declaration. Useful for collections that model real-world bounded sets (max interviewers, max documents, max queue depth).
- **Origin:** Frank creative sweep

---

##### `drains before <State>`

- **Syntax:** `field MissingDocuments as set of string drains before Approved`
- **Entity type:** Collection field
- **Mechanism:** Structural
- **What it means:** The collection must be empty when the entity enters the named state. Syntactic sugar for a state assertion.
- **Compile-time check:** Equivalent to injecting `in <State> assert <Collection>.count == 0 because "<Collection> must be empty in <State>"`. Same enforcement as a state assertion.
- **Tooling impact:** Inspector shows "Drains before Approved." Diagram could show a drain indicator on the collection-to-state edge. AI consumers know the collection must be emptied by a specific lifecycle point.
- **Precedent:** Precondition patterns in formal methods. Queue draining in message processing systems.
- **Dependencies:** None. Parameterized syntax follows `sealed after <State>` precedent. Syntactic sugar for state assert.
- **Concrete example — Insurance Claim:**
  ```precept
  field MissingDocuments as set of string drains before Approved
  # Equivalent to: in Approved assert MissingDocuments.count == 0 because "..."
  ```
- **Value/practicality assessment:** Medium. Desugars to state assert. Improves readability for the pattern where a working collection must be emptied before advancing. Maps to real patterns — MissingDocuments in Insurance Claim should be empty before approval.
- **Origin:** Frank creative sweep

---

##### `ephemeral`

- **Syntax:** `field MissingDocuments as set of string ephemeral`
- **Entity type:** Collection field
- **Mechanism:** Structural
- **What it means:** This collection is working storage — it is populated during the lifecycle and either emptied or abandoned before the lifecycle ends. The collection's contents are transient, not long-lived data.
- **Compile-time check:** **Partial provability.** The compiler can check: (a) the collection is targeted by both `add` and `remove` operations (bidirectional mutation — working storage signature), or (b) a state assertion ensures the collection is empty in at least one non-initial state. Full verification that the collection "is cleared at some point" requires path analysis across all reachable states, which is expensive. The simpler form (bidirectional mutation check) is a structural heuristic.
- **Tooling impact:** Inspector shows "Ephemeral collection — working storage." Diagram de-emphasizes the collection (it's operational, not core data). AI consumers know this collection is transient.
- **Precedent:** Temporary variables in programming. Working tables in ETL pipelines. The concept of transient storage is universal.
- **Dependencies:** None. Related to `drains before <State>` (#78) — `ephemeral` is the intent annotation; `drains before` is the structural guarantee.
- **Concrete example — Insurance Claim:**
  ```precept
  field MissingDocuments as set of string ephemeral

  from UnderReview on RequestDocument -> add MissingDocuments RequestDocument.Name -> no transition
  from UnderReview on ReceiveDocument when MissingDocuments contains ReceiveDocument.Name
      -> remove MissingDocuments ReceiveDocument.Name -> no transition
  # Both add and remove — ephemeral's bidirectional mutation heuristic is satisfied
  ```
- **Value/practicality assessment:** Medium. Partial provability — the bidirectional mutation check is a heuristic, not a proof that the collection drains. The semantic intent ("this is working storage") is the primary value. Maps to MissingDocuments and PendingInterviewers in samples.
- **Origin:** Frank creative sweep

---

##### `argbound`

- **Syntax:** `field ClaimantName as string nullable argbound`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** The field is only set from event argument references — never from computed expressions or other field values. The field's value comes directly from external input.
- **Compile-time check:** Scan all `set` actions targeting this field. Verify every RHS expression is a direct event argument reference (`EventName.ArgName`). Error if any `set` uses a computed expression, field reference, or literal value.
- **Tooling impact:** Inspector shows "Arg-bound — value from event args only." AI consumers know this field is a passthrough for external input, not computed data.
- **Precedent:** Bound parameters in APIs. Constructor injection (value comes from caller, not computed). No workflow system has this as a keyword.
- **Dependencies:** None.
- **Concrete example — Insurance Claim:**
  ```precept
  field ClaimantName as string nullable argbound

  from Draft on Submit -> set ClaimantName = Submit.Claimant -> ...
  # RHS is Submit.Claimant (event arg reference) — argbound satisfied
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (RHS expression analysis). Maps to real patterns — most intake fields (ClaimantName, ApplicantName, OrderId) are set directly from event args. The modifier distinguishes input-passthrough fields from computed fields. Low implementation cost.
- **Origin:** Frank creative sweep

---

##### `singlewriter`

- **Syntax:** `field ClaimantName as string nullable singlewriter`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** Exactly one event type mutates this field. The field has a single data source.
- **Compile-time check:** Count distinct events that contain `set`, `add`, or `remove` targeting this field. Error if more than one event modifies it.
- **Tooling impact:** Inspector shows "Single writer — Submit only." Diagram could draw a data-flow edge from the writing event to the field. AI consumers know the field's sole data source.
- **Precedent:** Single-writer patterns in concurrent systems. Ownership types in Rust (one mutable reference). The concept of exclusive write access is universal.
- **Dependencies:** None. Related to `initializes <Field>` (#73) on events — `singlewriter` declares the property on the field; `initializes` declares it on the event. Complementary perspectives.
- **Concrete example — Insurance Claim:**
  ```precept
  field ClaimantName as string nullable singlewriter

  # Only Submit sets ClaimantName
  from Draft on Submit -> set ClaimantName = Submit.Claimant -> ...
  ```
- **Value/practicality assessment:** High. Simple compile-time check (mutation event count). Maps to real patterns — most intake fields are single-writer (set by Submit and never again). The modifier captures a common structural invariant with low implementation cost. Stronger signal than `writeonce` for fields that are set once per lifecycle, and applicable even if the field could theoretically be re-set from the same event in multiple states.
- **Origin:** Frank creative sweep

---

##### `projection of <Field>`

- **Syntax:** `field MileageTotal as number default 0 projection of MileageRate`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** The field's value is always derived from (references) the named field in every `set` expression. The field tracks or depends on another field.
- **Compile-time check:** **Partial provability.** For every `set` targeting this field, check that the RHS expression contains a reference to the named field. Error if any `set` expression does not reference the named field. This doesn't prove the value is a pure function of the named field (the expression may reference other fields too), but it proves the dependency exists.
- **Tooling impact:** Inspector shows "Projection of MileageRate." Diagram could draw a dependency edge between fields. AI consumers know the field's data flow.
- **Precedent:** Dependent types. Database views (projections of base tables). Computed columns dependent on source columns.
- **Dependencies:** None. Related to `derived` (#28) — `derived` says "no direct writes, computed automatically"; `projection of` says "always references the named field" but allows direct writes. The concepts overlap for computed fields but are distinct.
- **Concrete example — Travel Reimbursement:**
  ```precept
  field MileageTotal as number default 0 projection of MileageRate

  from Draft on Submit -> set MileageTotal = Submit.Miles * Submit.Rate -> ...
  # Hmm — this references Submit.Rate (event arg), not MileageRate (field)
  # Better example:
  # from X on Recalculate -> set MileageTotal = TotalMiles * MileageRate -> no transition
  # RHS references MileageRate — projection satisfied
  ```
- **Value/practicality assessment:** Low-medium. Partial provability and the dependency-existence check (rather than full derivation proof) limits the guarantee. The concept is sound but the compile-time check only proves reference presence, not functional dependence. Most useful for documentation and data-flow tracing rather than hard guarantees.
- **Origin:** Frank creative sweep

---

##### `accumulates via <Event>`

- **Syntax:** `field FeedbackCount as number default 0 accumulates via RecordInterviewFeedback`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** A single event is the sole writer to this field, and the mutation pattern is additive (the new value includes the old value plus something). Combination of `singlewriter` + additive expression pattern.
- **Compile-time check:** (a) Count events mutating this field — error if more than one (same as `singlewriter`). (b) **Partial provability:** check that the `set` expression follows an additive pattern: `set X = X + <expr>`, `set X = X - <expr>`, or for collections `add Collection Item`. Simple self-referential addition is provable; arbitrary expressions are not.
- **Tooling impact:** Inspector shows "Accumulates via RecordInterviewFeedback." Diagram draws accumulation edges. AI consumers know this field is a running counter/total modified by one event.
- **Precedent:** Accumulators in reducers (Redux). Running totals in financial systems. Counter patterns.
- **Dependencies:** Subsumes `singlewriter` for the annotated field. Parameterized syntax follows `sealed after <State>` precedent.
- **Concrete example — Hiring Pipeline:**
  ```precept
  field FeedbackCount as number default 0 nonnegative accumulates via RecordInterviewFeedback

  from InterviewLoop on RecordInterviewFeedback when PendingInterviewers contains RecordInterviewFeedback.Interviewer
      -> set FeedbackCount = FeedbackCount + 1 -> ...
  # Single writer (RecordInterviewFeedback) + additive pattern (FeedbackCount + 1)
  ```
- **Value/practicality assessment:** Medium. Compositional check (singlewriter + expression pattern). Maps to real patterns — FeedbackCount in Hiring Pipeline, ReopenCount in IT Helpdesk. Partial provability for the additive pattern. Low implementation cost for the provable cases.
- **Origin:** Frank creative sweep

---

##### `snapshot`

- **Syntax:** `field RequestedTotal as number default 0 snapshot`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** Write-once with the value sourced from existing field references (not event arguments). The field captures a computed state at a point in the lifecycle.
- **Compile-time check:** (a) Same as `writeonce` — set at most once. (b) The `set` expression must reference only other declared fields or constants — no event argument references (`EventName.ArgName`). Error if the RHS references any event argument.
- **Tooling impact:** Inspector shows "Snapshot — computed from fields, written once." AI consumers know this field is a point-in-time capture of derived data.
- **Precedent:** Snapshot pattern in event sourcing. Materialized views. The concept of capturing computed state is common.
- **Dependencies:** Extends `writeonce` with an additional RHS constraint.
- **Concrete example — Travel Reimbursement:**
  ```precept
  field RequestedTotal as number default 0 snapshot

  from Draft on Submit -> set RequestedTotal = Submit.Lodging + Submit.Meals + (Submit.Miles * Submit.Rate) -> ...
  # Hmm — this uses event args. A true snapshot would be:
  # from X on Calculate -> set RequestedTotal = LodgingTotal + MealsTotal + MileageTotal -> ...
  # RHS references only fields — snapshot satisfied
  ```
- **Value/practicality assessment:** Low-medium. The concept is clear but the real-world applicability is limited. Most fields in samples are set from event args (not from other fields). Fields derived from other fields are uncommon in current samples — they'd become more relevant with computed fields (#17). The modifier is sound but under-demanded in current usage patterns.
- **Origin:** Frank creative sweep

---

##### `append only`

- **Syntax:** `field ProcessedDocuments as set of string append only`
- **Entity type:** Collection field
- **Mechanism:** Structural
- **What it means:** Only `add` (and bulk clear if it existed) are permitted — no individual `remove` operations. The collection grows or resets but doesn't shrink item by item.
- **Compile-time check:** Error if any `remove` targets this collection. `add` is permitted.
- **Tooling impact:** Inspector shows "Append-only — no individual removal." Completions suppress `remove` for this collection. AI consumers know the collection is additive.
- **Precedent:** Append-only logs. Event sourcing (immutable event stream). CRDTs (grow-only sets).
- **Dependencies:** None. Related to `monotonic` (#26) for collections — in current Precept (no `clear` verb), `append only` and collection `monotonic` are equivalent (both ban `remove`). They would diverge if a `clear` verb were added: `monotonic` means "only grows" (no clear), `append only` means "no individual remove" (clear OK).
- **Concrete example — Insurance Claim:**
  ```precept
  field ProcessedDocuments as set of string append only

  from UnderReview on ProcessDocument -> add ProcessedDocuments ProcessDocument.Name -> no transition
  # add is permitted; remove ProcessedDocuments would be a compile error
  ```
- **Value/practicality assessment:** Medium. Simple compile-time check (verb scan). Currently equivalent to `monotonic` for collections, but the semantic intent is different ("additive log" vs "only grows"). The keyword is more intuitive for log/audit-trail patterns. Would differentiate from `monotonic` if `clear` verb is added in the future.
- **Origin:** Frank creative sweep

---

##### `pinned`

- **Syntax:** `field ApprovedAmount as number default 0 pinned`
- **Entity type:** Field
- **Mechanism:** Structural
- **What it means:** This field is referenced in at least one state assertion (`in <State> assert` or `to <State> assert`). The field is structurally significant — it participates in state-entry constraints.
- **Compile-time check:** Scan all state assertion expressions. Error if this field is not referenced in any assertion.
- **Tooling impact:** Inspector shows "Pinned — referenced in state assertions." Diagram could highlight pinned fields with a constraint badge. AI consumers know this field is governance-relevant.
- **Precedent:** Referenced columns in database constraints (foreign keys, check constraints). The concept of constraint participation is universal.
- **Dependencies:** None.
- **Concrete example — Insurance Claim:**
  ```precept
  field ApprovedAmount as number default 0 pinned

  in Approved assert ApprovedAmount > 0 because "Approved claims must specify a payout amount"
  # ApprovedAmount referenced in state assertion — pinned satisfied
  ```
- **Value/practicality assessment:** Low-medium. The property is fully derivable from the definition (scan assertions for field references). The modifier adds authorial intent — "I'm aware this field is governance-critical" — but the compiler already knows this. Value is primarily for scanability in large precepts where the field declaration is far from the assertions.
- **Origin:** George creative sweep

---

#### 3.1.4 Structural — Precept

Precept-level modifiers declare structural constraints on the **entire lifecycle graph**. They attach to the `precept Name` declaration and constrain all states, events, and transitions globally. This is a new entity type for modifiers — previous candidates target states, events, fields, or rules individually.

**Grammar extension:** The current grammar is `PreceptHeader := "precept" Identifier`. Adding modifiers extends this to `PreceptHeader := "precept" Identifier PreceptModifier*`. This follows the exact pattern of `StateDecl := "state" Identifier InitialOpt` where `initial` is an optional modifier after the name. The parser (`PreceptParser.cs`, line 613) currently consumes `precept` keyword + identifier; extending it to accept optional modifier keywords after the identifier is straightforward in Superpower — `.Many()` on a modifier parser, no conflict with existing syntax since all proposed modifiers are new keywords that don't collide with statement-starting keywords.

**Fundamental design tension:** Precept-level modifiers are global constraints — they constrain the ENTIRE lifecycle. This is powerful but also potentially rigid. A modifier like `linear` forbids ALL cycles, even intentional ones. Many real workflows have mixed topology (mostly linear with one cycle for document collection). The per-state and per-event modifiers in §3.1.1–§3.1.2 provide finer-grained control. Precept-level modifiers are best suited for lifecycles that genuinely satisfy the global property throughout — not as approximations for "mostly X."

**Scope rule for precept-level modifiers:** Same as all modifiers — must be either compile-time provable from declared structure, or trigger concrete tooling behavior. Additionally, a precept-level modifier must provide value that per-state/per-event modifiers DON'T provide: if marking every state/event individually achieves exactly the same thing, the precept-level modifier is syntactic sugar. Syntactic sugar is not automatically disqualifying (it may improve readability and express intent), but the honesty tax applies — such cases are documented.

---

##### `linear`

- **Syntax:** `precept LoanApplication linear`
- **Entity type:** Precept
- **Mechanism:** Structural
- **What it means:** The entire transition graph is a directed acyclic graph (DAG). No cycles exist anywhere in the lifecycle. Once the entity leaves any state, it never returns to a previously visited state.
- **Compile-time check:** Standard cycle detection on the transition graph (DFS with back-edge detection, or topological sort). Error if any cycle is found, with diagnostic identifying the cycle path (e.g., "Cycle detected: UnderReview → AwaitingDocuments → UnderReview"). Treats all edges as traversable (overapproximation — guards are ignored for structural checks, consistent with `forward`, `irreversible`, `gate`).
- **Tooling impact:** Diagram renders with a DAG layout engine rather than a general graph layout (no cycle edges to accommodate). Inspector shows "Linear lifecycle — no cycles." MCP `precept_compile` includes `linear: true` in precept metadata. AI consumers know the graph is acyclic and can reason about progress monotonicity — the entity always moves "forward" in a topological ordering.
- **Precedent:** BPMN's well-structured process models are inherently acyclic (loops use explicit back-edges through gateways). Petri net soundness analysis includes acyclicity as a special case of structured workflows. Step Functions workflows are implicitly DAG-structured unless explicit loops are introduced. TLA+ can express `[]~(InS /\ InS')` for all state pairs. No workflow DSL has an explicit `linear` keyword on the definition — the concept is structural in BPMN and enforced by convention in most pipeline systems.
- **Dependencies:** None. Subsumes per-state `forward` on ALL states — a `linear` precept makes every state trivially `forward` (no cycle passes through any state). If both `linear` (precept-level) and `forward` (per-state) appear, the per-state annotation is redundant. The compiler could accept this silently or hint at subsumption.
- **Relationship to per-state `forward`:** `linear` is NOT merely syntactic sugar for marking every state `forward`. There is a semantic difference: `linear` declares a global lifecycle property ("this workflow is acyclic by design"), while per-state `forward` is surgical ("this specific state is a one-way checkpoint"). The compiler CHECK is equivalent (cycle detection vs. per-state BFS), but the authorial intent differs. `linear` says "I designed this lifecycle to have no cycles anywhere." Per-state `forward` says "I want to protect these specific states from re-entry, but cycles elsewhere are fine." The distinction matters for AI consumers, documentation, and diagram layout decisions.
- **Concrete example — Loan Application:**
  ```precept
  precept LoanApplication linear

  field ApplicantName as string nullable
  field RequestedAmount as number default 0
  # ... fields ...

  state Draft initial
  state UnderReview
  state Approved
  state Funded
  state Declined

  # All transitions move forward — no cycles in the graph:
  # Draft → UnderReview → Approved → Funded
  #                     → Declined
  ```
  The Loan Application lifecycle is naturally linear — every transition moves the entity closer to an endpoint. No document-collection cycle, no review-return loop. `linear` captures this global property in one declaration.

  **Counter-example — Insurance Claim (would NOT use `linear`):**
  ```precept
  precept InsuranceClaim
  # NOT linear — intentional document request/receive cycle in UnderReview:
  # from UnderReview on RequestDocument -> add MissingDocuments ... -> no transition
  # from UnderReview on ReceiveDocument -> remove MissingDocuments ... -> no transition
  # The `no transition` rows don't create cycles (entity stays in UnderReview),
  # but the INTENT is that UnderReview is a dwell-state with repeated events.
  # If the lifecycle had an explicit AwaitingDocuments state with a cycle back
  # to UnderReview, `linear` would be violated.
  ```

  **Samples that WOULD use `linear`:** `loan-application`, `travel-reimbursement`, `maintenance-work-order`, `refund-request`, `subscription-cancellation-retention`, `restaurant-waitlist`.

  **Samples that would NOT:** `insurance-claim` (document loop potential), `library-book-checkout` (Available → CheckedOut → Returned → Available cycle), `crosswalk-signal` (DontWalk → Walk → FlashingDontWalk → DontWalk cycle), `trafficlight` (Red → Green → Yellow → Red cycle), `hiring-pipeline` (interview loop).
- **Design tension — stated honestly:**

  1. **Rigidity.** `linear` forbids ALL cycles. A lifecycle that is "90% linear with one intentional loop" cannot use `linear`. The author must choose: use `linear` and restructure the loop (e.g., keep the dwell pattern with `no transition` instead of inter-state cycles), or skip `linear` and use per-state `forward` on the states that should be one-way. This is the fundamental tension of global modifiers.

  2. **`no transition` is not a cycle.** Events with `no transition` outcomes keep the entity in the same state — this is NOT a cycle by graph definition (no edge back to the same state through a different state). A `linear` precept CAN have settling events that use `no transition` freely. The `linear` constraint only prohibits edges that form a cycle in the state graph.

  3. **Overapproximation.** The cycle detection treats all edges as traversable. Guard-infeasible cycles (cycles that exist in the graph but can never be traversed because guards make them mutually exclusive) will be flagged. This is consistent with `forward`, `irreversible`, and `gate` — structural modifiers check declared structure, not runtime behavior.

- **Value/practicality assessment:** MEDIUM confidence. The concept is clean, well-defined, and broadly useful for the ~50% of sample files that have DAG-shaped lifecycles. Implementation cost is minimal — standard DFS cycle detection on the existing graph adjacency list (~15 LOC). The main risk is that authors with "almost linear" lifecycles will be frustrated that `linear` doesn't allow exceptions. Per-state `forward` remains the right tool for mixed-topology lifecycles. `linear` is for lifecycles that are acyclic by design, not by approximation.

---

##### `strict`

- **Syntax:** `precept InsuranceClaim strict`
- **Entity type:** Precept
- **Mechanism:** Structural (compilation mode)
- **What it means:** All analysis diagnostics that would normally be warnings are upgraded to hard errors. Zero-tolerance compilation — no unresolved warnings allowed. The definition must be completely clean.
- **Compile-time check:** After normal analysis completes, scan all diagnostics. Any diagnostic with Warning severity is upgraded to Error severity. The following existing diagnostics are affected:
  - C48 (unreachable state) — Warning → Error
  - C49 (orphaned event) — Warning → Error
  - C50 (dead-end state) — Warning → Error
  - C51 (reject-only state/event pair) — Warning → Error
  - C52 (event that never succeeds) — Warning → Error
  - C53 (empty precept) — Warning → Error
  - Any future analysis-phase warnings are automatically included.
- **Tooling impact:** Inspector shows "Strict mode — all warnings are errors." MCP `precept_compile` includes `strict: true` in precept metadata. Completions could pre-warn when authoring patterns that would trigger warnings. AI authors know to be more careful with strict precepts — every warning must be resolved.
- **Precedent:** `use strict` in JavaScript. `<TreatWarningsAsErrors>` in C#/.NET project files. `-Werror` in GCC/Clang. `strict_types` declaration in PHP. `--strict` flag in TypeScript. The concept of strict compilation mode is universal across programming languages. The novelty here is expressing it per-definition rather than per-project — each `.precept` file chooses its own strictness level.
- **Dependencies:** None. Independent of all other modifiers. Does not require the philosophy evolution (Section 2) — it operates on existing diagnostics. If the `warning` severity mechanism (Section 3.3) were also present, `strict` would NOT upgrade rule-level `warning` severity to error — those are author-chosen behavioral severities, not analysis-phase warnings. `strict` affects the compiler's analysis output, not the author's declared rule severities.
- **Concrete example — Loan Application (strict):**
  ```precept
  precept LoanApplication strict

  # With `strict`, any of these would be compile ERRORS, not warnings:
  # - An unreachable state
  # - An orphaned event (declared but never used in a transition row)
  # - A dead-end state (non-terminal state with only reject outcomes)
  # - An event that never succeeds from any reachable state
  ```

  **Samples that WOULD use `strict`:** `loan-application` (clean DAG, no warnings expected), `travel-reimbursement` (clean DAG), `maintenance-work-order` (clean structure). Any precept authored for a regulated domain where "clean compile" is a compliance requirement.

  **Samples that would NOT:** Precepts under active development where warnings are expected and informational. Precepts with intentional dead-end states (though those should use `terminal`).
- **Design tension — stated honestly:**

  1. **Not a lifecycle property.** Unlike `linear` and `governed`, `strict` doesn't describe a property of the lifecycle graph — it describes a compilation mode. It's a pragma, not a semantic declaration. This is a philosophical question: should precept-level modifiers be limited to lifecycle topology, or can they also be compilation directives? The precedent is strong (`use strict` in JavaScript IS a per-file pragma), but it's a different category from the other precept-level modifiers.

  2. **Interaction with future diagnostics.** `strict` automatically includes any future analysis-phase warnings. This is a strength (automatic escalation) but also a risk (a new warning added in a future runtime version could break existing `strict` precepts). Mitigation: only analysis-phase warnings are escalated, never hard constraint codes.

  3. **Interaction with `warning` rule severity.** If both `strict` and rule-level `warning` severity exist, the interaction needs clear semantics. Recommendation: `strict` affects analysis-phase diagnostics (C48–C53+), NOT author-declared rule severities. An author who writes `invariant X because "Y" warning` in a `strict` precept is making an intentional behavioral choice — `strict` respects that choice.

- **Value/practicality assessment:** MEDIUM-HIGH confidence. The concept is universally understood, trivially implementable (one flag check before emitting diagnostics), and directly useful for production-quality precepts. The main philosophical question is whether a compilation mode belongs as a language modifier. The per-file pragma model (JavaScript, PHP) supports this. Implementation: ~5–10 LOC in `PreceptAnalysis.Analyze()` to check the flag and upgrade warning-severity diagnostics to error.

---

##### `governed`

- **Syntax:** `precept InsuranceClaim governed`
- **Entity type:** Precept
- **Mechanism:** Structural
- **What it means:** All incoming transitions to non-initial states must have `when` clauses. A global guard policy — every state transition requires explicit justification. Equivalent to every non-initial state being implicitly `guarded`.
- **Compile-time check:** For every transition row whose outcome is `transition <TargetState>` where `TargetState` is NOT the initial state: error if the row lacks a `when` clause. Reject rows are exempt (their outcome is rejection, not a state entry). `no transition` rows are exempt (no state entry occurs). Rows targeting the initial state are exempt (the initial state represents the starting point, and some designs allow returning to initial without guards for "reset" semantics).
- **Tooling impact:** Inspector shows "Governed lifecycle — all state entries require guards." Completions always prompt for a `when` clause when authoring transition rows in governed precepts. AI consumers know to generate guards on every transition. Diagram could render a governance badge on the precept.
- **Precedent:** No direct precedent for a global guard requirement on all transitions. BPMN has conditional sequence flows (but they're optional, not mandatory). Regulatory compliance frameworks (SOX, KYC/AML, HIPAA) implicitly require justification for every state change, but this is enforced by audit processes, not by the modeling language. The closest analogue is mandatory code review policies in CI/CD — every merge requires an approving review, no exceptions.
- **Dependencies:** None. Subsumes per-state `guarded` on ALL non-initial states. If both `governed` (precept-level) and `guarded` (per-state) appear, the per-state annotation is redundant for non-initial states. A `guarded` modifier on the initial state itself applies independently (initial states are exempt from `governed`).
- **Relationship to per-state `guarded`:** `governed` IS syntactic sugar for marking every non-initial state `guarded`. The compiler check is identical (scan incoming rows for `when` clauses). The value added is authorial intent and brevity — a single declaration on `precept Name governed` replaces N declarations of `state X guarded` on every state. This is an honest assessment: if per-state `guarded` ships, `governed` is a convenience. But the convenience is significant for lifecycles with many states (10+ states where adding `guarded` to each is noisy).
- **Concrete example — Loan Application (governed):**
  ```precept
  precept LoanApplication governed

  state Draft initial
  state UnderReview
  state Approved
  state Funded
  state Declined

  # ✓ This row has a guard — satisfies `governed`
  from UnderReview on Approve
      when DocumentsVerified and CreditScore >= 680
      -> set ApprovedAmount = Approve.Amount
      -> transition Approved

  # ✓ Reject rows are exempt from `governed` (no state entry)
  from UnderReview on Approve -> reject "Requirements not met"

  # ✗ This row would FAIL under `governed` — unguarded transition to non-initial state:
  # from Draft on Submit -> set ... -> transition UnderReview
  # Fix: add a guard:
  from Draft on Submit
      when Submit.Applicant != "" and Submit.Amount > 0
      -> set ApplicantName = Submit.Applicant
      -> set RequestedAmount = Submit.Amount
      -> transition UnderReview
  ```

  **Samples that WOULD use `governed`:** `loan-application` (most transitions already have guards), `insurance-claim` (approval requires compound guards). Any precept for regulated industries where every state change must be justified.

  **Samples that would NOT:** `trafficlight` (Advance events don't always need guards — Green → Yellow is unconditional), `crosswalk-signal` (phase transitions are unconditional from some states), `maintenance-work-order` (some transitions like Open → Cancelled are intentionally unguarded).
- **Design tension — stated honestly:**

  1. **The reject-fallback pattern.** Precept's idiomatic pattern is a guarded success row followed by an unguarded reject fallback: `from S on E when Guard -> transition T` / `from S on E -> reject "..."`. Under `governed`, the reject fallback is exempt (it doesn't enter a new state). But the FIRST row must have a guard, which it already does in this pattern. So `governed` is compatible with the idiomatic pattern — but only because reject rows are exempt.

  2. **Unconditional intake transitions.** Many lifecycles have an unconditional intake event: `from Draft on Submit -> set ... -> transition Submitted`. The initial→second-state transition is often unguarded because event assertions (`on Submit assert`) already validate the data. Under `governed`, this row would need a `when` clause. The initial-state exemption mitigates this for self-transitions back to initial, but not for Draft→Submitted. Authors must either (a) add a `when` guard that duplicates the event assert logic, or (b) not use `governed`. This redundancy between event assertions and transition guards is the main friction point.

  3. **Honesty: syntactic sugar.** `governed` is syntactic sugar for per-state `guarded` on every non-initial state. If per-state `guarded` ships, `governed` provides brevity and intent declaration but no structural leverage that per-state `guarded` doesn't already provide. The value is proportional to the number of states — for 3-state precepts, per-state `guarded` is fine; for 10+ state precepts, `governed` is meaningfully more concise.

  4. **Guard presence vs. guard quality.** `governed` checks for the PRESENCE of a `when` clause, not its quality. A guard like `when true` technically satisfies `governed` but provides no actual protection. This is the same limitation as per-state `guarded` — structural modifiers verify structure, not semantics.

- **Value/practicality assessment:** MEDIUM-LOW confidence. The concept is valid and maps to real regulatory requirements. But per-state `guarded` is more precise, the unconditional intake friction is real, and the syntactic-sugar honesty applies. Best suited for highly regulated domains with many states where the per-state annotation would be noisy. Implementation cost is minimal — extends the existing incoming-row scan from per-state `guarded` to all non-initial states (~10 LOC in `PreceptAnalysis.Analyze()`).

---

##### `exhaustive`

- **Syntax:** `precept InsuranceClaim exhaustive`
- **Entity type:** Precept
- **Mechanism:** Structural
- **What it means:** Every non-terminal state has at least one outgoing event that can produce a successful transition. No dead-end states. The lifecycle always has somewhere to go from every non-endpoint state.
- **Compile-time check:** For every non-terminal state, verify at least one outgoing transition row produces a non-reject outcome (`transition` or `no transition`). Error if any non-terminal state has only reject outcomes or no outgoing rows. This is an upgrade of the existing C50 (dead-end state) diagnostic from warning to error, scoped to the specific property of outgoing-transition coverage.
- **Tooling impact:** Inspector shows "Exhaustive lifecycle — no dead-end states." MCP `precept_compile` includes `exhaustive: true` in precept metadata.
- **Precedent:** No direct precedent. The concept relates to Petri net "liveness" (every transition can fire). C50 already checks this property as a warning.
- **Dependencies:** Enhanced by `terminal` — the check naturally excludes terminal states (they are endpoints, not dead ends). Without `terminal`, the checker must infer which states are intentional endpoints.
- **Relationship to `strict`:** `strict` upgrades ALL analysis warnings to errors, including C50 (dead-end) but also C48 (unreachable), C49 (orphaned event), C51 (reject-only pair), C52 (event never succeeds), C53 (empty precept). `exhaustive` upgrades ONLY the dead-end property (C50). An author who wants strict dead-end enforcement but tolerates other warnings would use `exhaustive` instead of `strict`. The two are complementary — `strict` subsumes `exhaustive` for the C50 case, but `exhaustive` is narrower and independent.
- **Concrete example:**
  ```precept
  precept InsuranceClaim exhaustive

  state Draft initial
  state Submitted
  state UnderReview
  state Approved terminal
  state Denied terminal

  # Under `exhaustive`, if Submitted had no outgoing transitions
  # other than reject rows, it would be a compile ERROR, not a warning
  ```
- **Value/practicality assessment:** Medium. Clear concept, trivially implementable (reuses C50 analysis, adds an error-severity flag). The question is whether the single-property upgrade justifies a keyword when `strict` already covers C50 among others. For authors who want targeted dead-end prevention without full strict mode, `exhaustive` provides the precision. In practice, most authors who care about dead ends also care about unreachable states and orphaned events — so they'd use `strict` anyway. The standalone demand for `exhaustive` without `strict` is uncertain.

---

##### `single-entry`

- **Syntax:** `precept LoanApplication single-entry`
- **Entity type:** Precept
- **Mechanism:** Structural
- **What it means:** Exactly one event fires from the initial state. The lifecycle has a single intake point — one way in.
- **Compile-time check:** Count distinct events that have transition rows originating from the initial state. Error if more than one event has rows from the initial state. This counts event names, not row count — one event with multiple guarded rows from initial is fine; two different events from initial violates the constraint.
- **Tooling impact:** Inspector shows "Single-entry lifecycle — one intake event." Diagram renders the initial state with a single outgoing edge type. MCP `precept_compile` includes `singleEntry: true` in precept metadata. AI consumers know there's exactly one way to start the lifecycle.
- **Precedent:** No direct precedent as a DSL keyword. BPMN has a single Start Event convention (though multiple start events are allowed). Step Functions workflows have exactly one StartAt state. Many real workflow systems assume single intake by convention.
- **Dependencies:** Requires `initial` (always present in stateful precepts). Complementary to `single-exit` — together they describe a single-intake, single-endpoint lifecycle.
- **Grammar note:** The hyphenated keyword (`single-entry`) has no precedent in Precept's current grammar — all existing keywords are single words. This is a surface-syntax question for Shane. Alternatives: `singleentry` (one word), or a different keyword entirely (e.g., `funneled`). The hyphen would need tokenizer support.
- **Concrete example — Loan Application:**
  ```precept
  precept LoanApplication single-entry

  state Draft initial

  # Only Submit fires from Draft — the single intake event
  from Draft on Submit -> set ApplicantName = Submit.Applicant -> transition UnderReview
  # No other event has rows from Draft: ✓ single-entry satisfied
  ```
- **Value/practicality assessment:** Medium-low. The check is trivial (count events from initial state). The constraint captures a real pattern — many workflows have a single intake event (Submit, Create, Register). But many others legitimately have multiple initial events: data-setting events before submission (e.g., `SetApplicant`, `SetAmount` as separate settling events before `Submit`), or alternative creation paths (e.g., `CreateFromTemplate` vs `CreateManual`). The modifier is meaningful for workflows where "one way in" is an architectural guarantee.

---

##### `single-exit`

- **Syntax:** `precept SimpleWorkflow single-exit`
- **Entity type:** Precept
- **Mechanism:** Structural
- **What it means:** Exactly one terminal state (or, if `terminal` modifiers are not used, exactly one state with no outgoing transitions). The lifecycle converges to a single endpoint.
- **Compile-time check:** Count states marked `terminal`. Error if more than one. If no states are marked `terminal`, fall back to counting states with no outgoing transition rows — error if more than one.
- **Tooling impact:** Inspector shows "Single-exit lifecycle — one terminal endpoint." Diagram renders a single convergence point. MCP `precept_compile` includes `singleExit: true` in precept metadata.
- **Precedent:** BPMN best practices recommend a single End Event for clarity. Step Functions workflows converge to a single terminal state by convention. Petri net soundness analysis requires a single output place.
- **Dependencies:** Enhanced by `terminal` (the check is cleaner when terminal states are explicitly marked). Complementary to `single-entry`.
- **Concrete example:**
  ```precept
  precept SimpleWorkflow single-exit

  state Draft initial
  state InProgress
  state Complete terminal        # the only terminal state

  from Draft on Start -> transition InProgress
  from InProgress on Finish -> transition Complete
  ```
- **Value/practicality assessment:** Low. Multiple terminal states are nearly universal in real business workflows — Approved/Denied, Funded/Declined, Hired/Rejected. Forcing a single exit requires either merging semantically distinct endpoints (losing readability and governance value) or creating an artificial "Completed" umbrella state. The few workflows with genuinely single endpoints (simple pipelines, signal cycles) are so simple they rarely need the annotation. The modifier captures a clean structural property but applies to a narrow slice of real lifecycles. Shane should be aware that most sample files (20+ out of 24) have multiple terminal states.

---

##### `sequential`

- **Syntax:** `precept SimpleApproval sequential`
- **Entity type:** Precept
- **Mechanism:** Structural
- **What it means:** States have strict ordering based on declaration order. Each state can only transition to the immediately next state in declaration order. The lifecycle is a simple linear chain — no branching, no skipping.
- **Compile-time check:** Build the declared-order sequence from state declarations (the order states appear in the `.precept` file). For each transition row, verify the target state is the immediate successor of the source state in this sequence. Error if any transition targets a non-successor state. Terminal states (or the last in declaration order) have no required successor — the chain ends there.
- **Tooling impact:** Diagram renders as a strict pipeline (one lane, one direction). Inspector shows "Sequential lifecycle — strict state progression." MCP `precept_compile` includes `sequential: true` in precept metadata.
- **Precedent:** Conveyor belt / assembly line model. Jenkins pipeline stages are strictly sequential by default. GitHub Actions job dependencies can form DAGs but individual jobs are sequential. No workflow DSL has an explicit `sequential` keyword.
- **Dependencies:** Implies `linear` (a strictly sequential chain is a DAG). More restrictive than `linear` — `linear` allows branching DAGs, `sequential` requires a single chain.
- **Concrete example:**
  ```precept
  precept SimpleApproval sequential

  state Draft initial             # 1st in sequence
  state Submitted                 # 2nd — can only reach from Draft
  state Reviewed                  # 3rd — can only reach from Submitted
  state Complete terminal         # 4th — can only reach from Reviewed

  from Draft on Submit -> transition Submitted        # ✓ next in sequence
  from Submitted on Review -> transition Reviewed      # ✓ next in sequence
  from Reviewed on Complete -> transition Complete      # ✓ next in sequence
  # from Submitted on Reject -> transition Complete    # ✗ would ERROR — Complete is not next after Submitted
  ```
- **Value/practicality assessment:** Very low. Almost no real workflow is strictly sequential — even simple approval workflows branch (Approved vs Denied from the same state). The only real candidates are trivial signal cycles (`crosswalk-signal`, `trafficlight`), but those have CYCLES which violate the sequential chain model. Workflows simple enough to be sequential are so simple they don't benefit from the annotation. The constraint is compile-time provable and structurally sound, but applicability to real business lifecycles is minimal.

---

##### `stateless`

- **Syntax:** `precept CustomerProfile stateless`
- **Entity type:** Precept
- **Mechanism:** Structural
- **What it means:** Explicit declaration that this precept has no lifecycle states. The definition contains only fields, invariants, and edit declarations — no states, no events, no transitions.
- **Compile-time check:** Error if any `state`, `event`, or `from` declarations exist. The modifier enforces the absence of lifecycle machinery.
- **Tooling impact:** Inspector shows "Stateless precept — data validation only." MCP `precept_compile` includes `stateless: true` in precept metadata. Diagram rendering is skipped (no state graph to render). AI consumers know this precept is a validation schema, not a lifecycle.
- **Precedent:** TypeScript's `type` vs `class` distinction (pure type vs behavioral). Zod/Valibot schemas are inherently stateless. The concept of schema-only definitions is universal.
- **Dependencies:** Contradictory with all state-graph modifiers (`linear`, `governed`, `exhaustive`, `terminal`, etc.). The compiler should error if `stateless` appears alongside any modifier that references the state graph.
- **Concrete example:**
  ```precept
  precept CustomerProfile stateless

  field Name as string nullable
  field Email as string nullable
  field Phone as string nullable

  invariant Name != null because "Name is required"
  invariant Email != null or Phone != null because "Must have email or phone"
  ```
- **Value/practicality assessment:** Low-medium. The property is already structurally inferred — the runtime's `definition.IsStateless` flag is set automatically when no state declarations exist. The inference is unambiguous: zero states means stateless, period. Making it explicit adds documentation value ("I intended this to have no states") but challenges Precept's pattern where file structure IS the truth (you don't declare what can be structurally derived). Compare with `initial` — which IS needed because the compiler can't know which state the author intended as the starting point. `stateless` IS derivable — there's no ambiguity in zero states. The counter-argument: explicit declaration catches accidental omission (the author forgot to add states, and the explicit `stateless` modifier confirms the intent was no-lifecycle). Shane should weigh whether the documentation/intent value justifies a keyword for a structurally-inferred property.

---

##### `total` (precept-level)

- **Syntax:** `precept Workflow total`
- **Entity type:** Precept
- **Mechanism:** Structural
- **What it means:** Every declared event has transition rows from every non-terminal state. The event × state matrix is fully populated — no gaps. Every event is handled everywhere.
- **Compile-time check:** Build the cross-product of (non-terminal states) × (events). For each pair, verify at least one transition row exists. Error if any pair has no rows.
- **Tooling impact:** Inspector shows "Total lifecycle — all events handled in all states." MCP `precept_compile` includes `total: true` (precept-level) in precept metadata. Diagram renders all event edges from every non-terminal state.
- **Precedent:** Total function specification in formal methods — every input has a defined output. No workflow system enforces total event coverage.
- **Dependencies:** Enhanced by `terminal` (the check excludes terminal states). Note: precept-level `total` is distinct from event-level `total` (#19) — event-level `total` checks that one specific event has non-reject rows in every state where it appears; precept-level `total` checks that EVERY event is handled in EVERY non-terminal state.
- **Concrete example:**
  ```precept
  precept SimpleWorkflow total

  state Draft initial
  state Active
  state Cancelled terminal

  event Start
  event Update
  event Cancel

  # ALL events from ALL non-terminal states:
  from Draft on Start -> transition Active
  from Draft on Update -> reject "Not started yet"
  from Draft on Cancel -> transition Cancelled
  from Active on Start -> reject "Already started"
  from Active on Update -> no transition
  from Active on Cancel -> transition Cancelled
  ```
- **Value/practicality assessment:** Very low. Contradicts Precept's state-scoped event design — a core language principle (Design Principle #4: locality of reference) is that events are structurally relevant only in the states where they matter. Requiring `Approve` in `Draft`, `PayClaim` in `Draft`, etc. would produce definitions filled with reject rows for events that have no business meaning in early states. The modifier is compile-time provable (cross-product check), but applying it to real workflows would generate boilerplate and obscure the actual lifecycle logic. In practice, the value of Precept's model is that events DON'T need to be handled everywhere — `total` negates this advantage.

---

##### `guarded total`

- **Syntax:** `precept InsuranceClaim guarded total`
- **Entity type:** Precept
- **Mechanism:** Structural
- **What it means:** Every non-reject transition row in the entire precept has a `when` clause. Reject rows are exempt (they're fallbacks). This is the strictest guard policy — no unguarded successes anywhere.
- **Compile-time check:** Scan all transition rows. For every row whose outcome is `transition` or `no transition`, verify a `when` clause is present. Error on any unguarded success row. Reject rows are exempt.
- **Tooling impact:** Inspector shows "Guarded total — all successes require conditions." Completions always prompt for guards when authoring non-reject rows. AI consumers know every successful outcome is guarded.
- **Precedent:** No direct precedent. Extends `governed` (#31) which requires guards on incoming transitions to non-initial states. `guarded total` is broader — it requires guards on ALL non-reject rows, including settling events and self-transitions.
- **Dependencies:** Subsumes `governed` (#31) — `guarded total` implies `governed` because all incoming transitions to non-initial states are a subset of all non-reject rows. Independent of per-state `guarded` (#2) and per-event `guarded` (#18).
- **Design tension:** Similar to `governed` (#31), the reject-fallback pattern means the guarded row already has a `when` clause in the idiomatic pattern. The tension is with unconditional settling events like `from Submitted on RequestDocument -> add MissingDocuments ... -> no transition` — under `guarded total`, this row would need a guard even though it's an unconditional data accumulation step.
- **Concrete example:**
  ```precept
  precept LoanApplication guarded total

  # ✓ Guarded non-reject row
  from UnderReview on Approve when DocumentsVerified and CreditScore >= 680
      -> set ApprovedAmount = Approve.Amount -> transition Approved
  # ✓ Reject row — exempt
  from UnderReview on Approve -> reject "Requirements not met"
  # ✗ Unguarded no-transition row — WOULD FAIL under guarded total:
  # from UnderReview on VerifyDocuments -> set DocumentsVerified = true -> no transition
  # Fix: from UnderReview on VerifyDocuments when not DocumentsVerified -> ...
  ```
- **Value/practicality assessment:** Low. Very rigid — requires guards even on unconditional data-accumulation events, which creates friction for settling patterns. The exemption for reject rows helps but the settling-event tension is significant. More restrictive than `governed` with less clear incremental value. Best suited for extremely high-assurance precepts where every successful outcome must have an explicit precondition.
- **Origin:** Frank creative sweep

---

### 3.2 Semantic Modifiers

Semantic modifiers declare lifecycle significance — what the author *means* by a state or event — without constraining what can be written. They enable soft consistency diagnostics and drive tooling presentation.

---

#### 3.2.1 Semantic — States

##### `success`

- **Syntax:** `state Approved success`
- **Entity type:** State
- **Mechanism:** Semantic
- **What it means:** Positive lifecycle endpoint. Being in this state means the entity's lifecycle concluded favorably.
- **Compile-time check:** Soft diagnostic (C-cross): warning if this state is only reachable via `error`-annotated events. Enhanced C48: warning if a success state is unreachable from initial — a declared positive endpoint that no path reaches.
- **Tooling impact:** Diagram renders success states in emerald (`#34D399`). Inspector shows "Success endpoint." MCP `precept_compile` includes `verdict: "success"` in state metadata. Preview coverage stat: "Reachable from current state: 2 success endpoints, 1 error endpoint."
- **Precedent:** No comparable system annotates states with verdict severity. XState has `tags` for UI state queries (`state.hasTag('success')`), but these are unstructured strings, not first-class language constructs. This is genuinely novel territory — Shane identified it as a differentiator.
- **Dependencies:** None. Enhanced by `terminal` — `terminal success` combines structural boundary with semantic significance.
- **Concrete example — Hiring Pipeline:**
  ```precept
  state Draft initial
  state Screening
  state InterviewLoop
  state Decision
  state OfferExtended
  state Hired success            # the positive outcome

  event SubmitApplication success
  event PassScreen success
  event ExtendOffer success
  event AcceptOffer success
  ```
- **Value/practicality assessment:** Strong tooling story — the diagram becomes a governance map with happy paths visible at a glance. No structural enforcement means no philosophy tension. The main question is whether keywords without hard enforcement carry their weight. The semantic reframing (modifier roles #2 and #3) provides the justification: `success` communicates authorial intent and drives concrete tooling behavior. Novel territory (no comparable system does this) is the opportunity, not the risk.

---

##### `error`

- **Syntax:** `state Denied error`
- **Entity type:** State
- **Mechanism:** Semantic
- **What it means:** Negative lifecycle endpoint. Being in this state means the entity's lifecycle concluded unfavorably.
- **Compile-time check:** Soft diagnostic (C-cross): warning if this state is only reachable via `success`-annotated events. Enhanced C48: warning if an error state is unreachable.
- **Tooling impact:** Diagram renders error states in rose (`#FB7185`). Inspector shows "Error endpoint." MCP metadata includes `verdict: "error"`.
- **Precedent:** Same as `success` — novel territory. XState's `tags` (unstructured). Step Functions' `Fail` state is closest but is a structural construct, not a semantic annotation.
- **Dependencies:** None. Enhanced by `terminal` — `terminal error` is the natural pairing.
- **Concrete example — Hiring Pipeline:**
  ```precept
  state Rejected error           # the negative outcome

  event RejectCandidate error
  ```
- **Value/practicality assessment:** Complement to `success`. Same assessment — strong tooling story, novel territory, no enforcement tension. Together with `success`, creates an endpoint taxonomy that makes lifecycle narratives scannable.

---

##### `warning`

- **Syntax:** `state OnHold warning`
- **Entity type:** State
- **Mechanism:** Semantic
- **What it means:** Concern state. Being here means something needs attention — but it's not a terminal failure.
- **Compile-time check:** No hard checks. Soft hint: warning if the state has no outgoing transitions (a terminal warning state is unusual — concerns are typically transient, not endpoints).
- **Tooling impact:** Diagram renders warning states in amber (`#FCD34D`). Inspector shows "Warning state — needs attention." MCP metadata includes `verdict: "warning"`.
- **Precedent:** Same novel territory as `success`/`error`.
- **Dependencies:** None.
- **Concrete example — Hiring Pipeline:**
  ```precept
  state InterviewLoop warning    # interviews in progress — needs attention/tracking

  event AddInterviewer warning           # administrative — needs attention
  event RecordInterviewFeedback warning  # feedback pending — concern level
  ```
- **Value/practicality assessment:** Completes the three-value verdict vocabulary (`success`/`error`/`warning`). In practice, warning states are less common than success/error endpoints — most intermediate states are neutral, not concerning. The value depends on whether enough real workflows have "concern" states to justify the keyword. Applicable to states like OnHold, AwaitingReturn, EscalatedReview.

---

##### `resting`

- **Syntax:** `state AwaitingResponse resting`
- **Entity type:** State
- **Mechanism:** Semantic
- **What it means:** Expected steady state — the entity is expected to spend significant time here. The lifecycle's "plateau."
- **Compile-time check:** Soft validation: must have at least one `no transition` outcome (a resting state should handle events without always transitioning). Otherwise, no hard enforcement.
- **Tooling impact:** Inspector shows "Resting state — expected dwell point." Diagram could render with a distinctive fill to indicate the entity spends time here.
- **Precedent:** No direct precedent. The concept maps to wait states in workflow systems (Temporal activity wait, Step Functions Wait state) but those are runtime-enforced.
- **Dependencies:** None.
- **Concrete example:**
  ```precept
  state UnderReview resting      # entity stays here while documents are gathered

  from UnderReview on RequestDocument -> ... -> no transition
  from UnderReview on ReceiveDocument -> ... -> no transition
  from UnderReview on Approve -> ... -> transition Approved
  ```
- **Value/practicality assessment:** Primarily a tooling/intent annotation. The compile-time check is weak (has a `no transition` outcome). The concept is derivable from transition structure — states with settling events are implicitly resting. A keyword adds authorial intent but limited leverage. Lower demand than `success`/`error`/`warning` — most authors won't think to annotate dwell points.

---

##### `decision`

- **Syntax:** `state Decision decision`
- **Entity type:** State
- **Mechanism:** Semantic
- **What it means:** Branching point — the entity goes in different directions from here based on events or guards.
- **Compile-time check:** Soft validation: must have 2+ distinct outgoing event types. A decision point with only one outgoing path is not a real decision.
- **Tooling impact:** Diagram renders decision states as diamond shapes (BPMN convention). Inspector shows "Decision point — branches to N paths."
- **Precedent:** BPMN has Exclusive Gateways (XOR) and Inclusive Gateways (OR) as explicit decision nodes. UML has choice and junction pseudostates. The concept is well-established in workflow modeling.
- **Dependencies:** None.
- **Concrete example:**
  ```precept
  state Decision decision

  from Decision on ExtendOffer -> transition OfferExtended
  from Decision on RejectCandidate -> transition Rejected
  ```
- **Value/practicality assessment:** The property is derivable from the transition graph (states with 2+ distinct outgoing events are decision points). The compiler already knows this. A keyword adds authorial intent and enables BPMN-style diamond rendering, but the structural signal is already available without the annotation. Marginally useful for documentation and diagram styling.

---

##### `cyclic`

- **Syntax:** `state InterviewLoop cyclic`
- **Entity type:** State
- **Mechanism:** Semantic
- **What it means:** This state intentionally participates in a cycle — the entity is expected to revisit this state or remain through repeated event loops. Declares the cycle as intended, not accidental. The semantic complement of `forward`.
- **Compile-time check:** Soft diagnostic: warning if the state is NOT actually part of any cycle (declared cyclic but structurally acyclic — the state's successors cannot reach it via any path). This catches stale annotations after refactoring that removed the cycle.
- **Tooling impact:** Diagram renders cyclic states with a loop indicator (circular arrow badge). Inspector shows "Cyclic state — revisitable by design." The visual counterpart of `forward`: `forward` states guarantee no re-entry, `cyclic` states declare that re-entry is intentional. MCP compile output includes `cyclic: true` in state metadata.
- **Precedent:** Petri net theory distinguishes live (potentially re-fireable) from bounded (finite visits) properties. BPMN uses loop markers on activities and sub-processes. XState's `tags` could informally mark loop states but provide no structural validation. No workflow system has an explicit `cyclic` keyword — the concept is usually implicit from the graph structure.
- **Dependencies:** None. Semantic complement of `forward` — a state cannot be both `forward` and `cyclic`. If both appear on the same state, the compiler should error (contradictory modifiers). Does not interact with `milestone` (a cyclic state can be a milestone if all initial→terminal paths pass through it).
- **Concrete example — Insurance Claim with document loop:**
  ```precept
  state UnderReview cyclic       # expected: document request/receive loop
  state AwaitingDocuments cyclic  # entity cycles between review and awaiting docs

  from UnderReview on RequestDocument -> transition AwaitingDocuments
  from AwaitingDocuments on ReceiveDocument -> transition UnderReview
  from UnderReview on Approve -> transition Approved    # exit ramp from cycle
  ```
  The `cyclic` annotation tells reviewers and AI consumers that the UnderReview↔AwaitingDocuments loop is intentional. Without the annotation, a reviewer seeing the cycle might question whether it's a bug.
- **Value/practicality assessment:** Low. The property is derivable from the transition graph (states reachable from their own successors are in cycles). The semantic annotation adds authorial intent but provides minimal structural leverage. Most authors don't distinguish intentional from unintentional cycles at the annotation level — they know their state graph has cycles from reading the transitions. The strongest argument for inclusion: in complex lifecycles with many states, explicitly marking which states allow re-entry helps reviewers understand the intended flow patterns. But this benefit is marginal compared to the structural guarantee of `forward`. The `cyclic` modifier is the weakest of the three new flow candidates and exists primarily to complete the `forward`/`cyclic` intent vocabulary. If only one flow modifier ships, it should be `forward`, not `cyclic`.

---

##### `phase <N>`

- **Syntax:** `state UnderReview phase 2`
- **Entity type:** State
- **Mechanism:** Semantic
- **What it means:** Assigns a numeric lifecycle tier to the state. States with the same phase number are conceptually grouped in the same lifecycle stage. Phase ordering is authorial — the compiler does not enforce that transitions respect phase order.
- **Compile-time check:** None. Pure semantic annotation. A soft diagnostic could warn if a transition moves to a lower phase number (backward progress), but this is advisory only.
- **Tooling impact:** Diagram uses phase numbers for lane/layer assignment — states with the same phase number are rendered in the same horizontal band. Inspector groups states by phase. MCP metadata includes `phase: N`. AI consumers can reason about lifecycle progression by phase number.
- **Precedent:** BPMN swim lanes (phase grouping). Kanban column ordering. Stage numbering in pipeline systems. No workflow system uses a numeric phase annotation on states.
- **Dependencies:** None.
- **Concrete example — Insurance Claim:**
  ```precept
  state Draft initial phase 1           # intake
  state Submitted phase 1               # intake
  state UnderReview phase 2             # processing
  state Approved phase 3                # resolution
  state Denied phase 3                  # resolution
  state Paid phase 4                    # fulfillment
  ```
- **Value/practicality assessment:** Medium. Strong tooling value — diagram layout benefits significantly from explicit phase information. No structural enforcement, but the advisory backward-transition warning adds light guidance. The concept is intuitive and maps to how humans think about lifecycle stages.
- **Origin:** Frank creative sweep

---

##### `cluster <Name>`

- **Syntax:** `state UnderReview cluster "Processing"`
- **Entity type:** State
- **Mechanism:** Semantic
- **What it means:** Named grouping for related states. States with the same cluster name are conceptually part of the same lifecycle area. More expressive than `phase` — uses names instead of numbers.
- **Compile-time check:** None. Pure semantic annotation.
- **Tooling impact:** Diagram renders cluster groups as labeled boxes containing their member states (BPMN pool/lane style). Inspector groups states by cluster name. MCP metadata includes `cluster: "name"`. AI consumers can reason about state groupings.
- **Precedent:** BPMN pools and lanes. UML state machine regions. Mermaid `stateDiagram-v2` composite states. Grouping constructs are common in visual workflow tools.
- **Dependencies:** None. Complementary to `phase <N>` — `phase` is numeric ordering, `cluster` is named grouping. Both can be used together: `state X phase 2 cluster "Review"`.
- **Concrete example — Insurance Claim:**
  ```precept
  state Draft initial cluster "Intake"
  state Submitted cluster "Intake"
  state UnderReview cluster "Processing"
  state Approved cluster "Resolution"
  state Denied cluster "Resolution"
  state Paid cluster "Fulfillment"
  ```
- **Value/practicality assessment:** Medium. Strong diagram value — named grouping is the standard approach in visual workflow tools. No enforcement. The string parameter is a grammar question (quoted vs unquoted identifier). Complements `phase` for different use cases.
- **Origin:** Frank creative sweep

---

##### `waiting`

- **Syntax:** `state AwaitingReturn waiting`
- **Entity type:** State
- **Mechanism:** Semantic
- **What it means:** The entity is blocked on an external action — waiting for something outside the system (customer response, third-party approval, physical item return). The lifecycle cannot progress until the external condition is satisfied.
- **Compile-time check:** Soft — warn if the state has no outgoing transitions at all (a waiting state with no exit is likely a dead end, not a wait). Otherwise no enforcement.
- **Tooling impact:** Diagram renders waiting states with a clock/hourglass icon. Inspector shows "Waiting — blocked on external action." MCP metadata includes `waiting: true`. AI consumers know this state represents external dependency.
- **Precedent:** Wait states in Temporal workflows. BPMN's intermediate catch events (waiting for a signal). Step Functions' Wait state. The concept of external-dependency blocking is well-established.
- **Dependencies:** None. The semantic distinction from `resting` (#41) — `resting` implies expected dwell (data collection), `waiting` implies external blockage (out of the system's control). Related but different connotations.
- **Concrete example — Refund Request:**
  ```precept
  state AwaitingReturn waiting

  from AwaitingReturn on MarkReturnReceived -> set ReturnReceived = true -> transition Refunded
  # Waiting for the physical item to be returned — external action
  ```
- **Value/practicality assessment:** Medium. Strong tooling value — the waiting/blocked distinction is meaningful in dashboards, SLA tracking, and workflow monitoring. Maps to real patterns — AwaitingReturn, WaitingOnCustomer, AwaitingDocuments. No structural enforcement needed beyond the advisory dead-end check.
- **Origin:** Elaine creative sweep

---

##### `deprecated`

- **Syntax:** `state LegacyReview deprecated` / `event OldSubmit deprecated` / `field OldField as string nullable deprecated`
- **Entity type:** State, Event, or Field (multi-entity)
- **Mechanism:** Semantic
- **What it means:** The declaration is being phased out. Still functional but should not be used in new work. LSP renders with strikethrough.
- **Compile-time check:** Soft diagnostic — warn on every usage reference to a deprecated entity (e.g., transition rows referencing a deprecated state, `set` targeting a deprecated field). The warning says "X is deprecated" and suggests alternatives if provided.
- **Tooling impact:** LSP applies strikethrough text decoration to deprecated identifiers. Inspector shows "Deprecated — scheduled for removal." Diagram renders deprecated elements with reduced opacity. Completions list deprecated items last (or with a deprecation badge). MCP metadata includes `deprecated: true`.
- **Precedent:** `@deprecated` in Java/JSDoc. `[Obsolete]` in C#. `deprecated` CSS property markers. Deprecation annotations are universal across programming ecosystems. LSP's `CompletionItem.deprecated` flag is a standard protocol feature.
- **Dependencies:** None. Multi-entity applicability is novel for Precept modifiers — most modifiers are scoped to one entity type. `deprecated` applies to any declaration type.
- **Concrete example:**
  ```precept
  state LegacyReview deprecated
  event OldSubmit deprecated
  field ObsoleteField as string nullable deprecated

  # Usage of LegacyReview would trigger a deprecation warning
  from Submitted on Route -> transition LegacyReview   # ⚠ LegacyReview is deprecated
  ```
- **Value/practicality assessment:** High. Universal concept with well-established tooling semantics (LSP strikethrough is a one-line implementation). Multi-entity applicability means one keyword covers states, events, and fields. The phaseout signal is valuable for evolving precepts in production — authors can mark declarations for removal without immediately breaking consumers. Every programming ecosystem has this; its absence from Precept would be a gap.
- **Origin:** Elaine creative sweep

---

##### `display "<text>"`

- **Syntax:** `state UnderReview display "Under Review"` / `event SubmitApplication display "Submit Application"`
- **Entity type:** State or Event (multi-entity)
- **Mechanism:** Semantic
- **What it means:** Human-readable label for diagrams, inspector panels, and any visual surface. Overrides the identifier as the display name. Allows spaces, punctuation, and natural language in the rendered output while keeping identifiers valid and machine-friendly.
- **Compile-time check:** None. Pure tooling directive.
- **Tooling impact:** Diagram renders the display text instead of the identifier on state nodes and transition edges. Inspector shows the display text as the primary label. MCP metadata includes `display: "text"`. AI consumers can use display text for human-facing output.
- **Precedent:** `label` attributes in GraphViz/Mermaid. `displayName` in many UI frameworks. `@label` annotations. Display names are universal in tooling that renders identifiers.
- **Dependencies:** None. The string parameter grammar is a design question — quoted string following a keyword is already precedented by `because "..."` in Precept.
- **Concrete example — Insurance Claim:**
  ```precept
  state UnderReview display "Under Review"
  state Draft display "New Claim"
  event AssignAdjuster display "Assign an Adjuster"
  ```
- **Value/practicality assessment:** High. Universal need — identifiers are constrained by syntax rules (no spaces, no special characters), but visual surfaces need human-friendly labels. The `because` clause already establishes the quoted-string-after-keyword pattern. Trivial to implement (parser + model string + tooling reads it). Every visual tool has this capability.
- **Origin:** Elaine creative sweep

---

#### 3.2.2 Semantic — Events

##### `success` (event)

- **Syntax:** `event Approve success`
- **Entity type:** Event
- **Mechanism:** Semantic
- **What it means:** This event signifies a positive development in the entity's lifecycle. The event's *intent* is progress.
- **Compile-time check:** Soft diagnostic (C58-soft): warning if **every** row from **every** source state produces Rejected. If 100% of rows contradict the annotation, the compiler flags it. Partial contradiction (some rows reject) is fine — `event Approve success` with a guard-dependent reject is correct business logic.
- **Tooling impact:** Diagram colors success event edges in emerald. Inspector shows event verdict badge. Completions suggest verdict modifiers ranked by context. MCP `precept_inspect` includes `eventVerdict`. AI consumers compare declared verdict to actual outcome.
- **Precedent:** Same novel territory as state verdicts. No comparable system annotates events with verdict severity.
- **Dependencies:** None. Enhanced by structural event modifiers (e.g., `event Approve advancing success` — full event characterization).
- **Concrete example — Loan Application:**
  ```precept
  event Submit success           # starts the positive path
  event VerifyDocuments success  # verification is progress
  event Approve success          # approval intent (may fail on guards — that's fine)
  event FundLoan success         # completes the lifecycle
  ```
  Note: `event Approve success` coexists naturally with a guard-dependent reject row. Under the semantic model this is correct — Approve *signifies* positive intent even though it can fail when preconditions aren't met.
- **Value/practicality assessment:** Same as state `success` — strong tooling story, novel territory. The semantic model (meaning declaration, not outcome enforcement) resolves the tension that the earlier Option A (hard C58/C59) created. Near-zero false positives because soft checks fire only on total contradiction.

---

##### `error` (event)

- **Syntax:** `event Deny error`
- **Entity type:** Event
- **Mechanism:** Semantic
- **What it means:** This event signifies a negative development. The event's *intent* is decline, rejection, or failure.
- **Compile-time check:** Soft diagnostic (C59-soft): warning if **every** row from **every** source state produces Transition. If 100% of rows succeed, the `error` annotation is contradicted.
- **Tooling impact:** Diagram colors error event edges in rose. Inspector shows verdict badge. Same MCP and AI integration as success events.
- **Precedent:** Same novel territory.
- **Dependencies:** None.
- **Concrete example — Loan Application:**
  ```precept
  event Decline error            # the off-ramp
  ```
- **Value/practicality assessment:** Complement to event `success`. Same assessment.

---

##### `warning` (event)

- **Syntax:** `event Escalate warning`
- **Entity type:** Event
- **Mechanism:** Semantic
- **What it means:** This event signals a concern — something needs attention. Not positive progress, not failure, but a flag.
- **Compile-time check:** No specific soft diagnostic. A `warning` event has no expected outcome shape — it can transition, stay, or reject.
- **Tooling impact:** Diagram colors warning event edges in amber. Inspector shows verdict badge.
- **Precedent:** Same novel territory.
- **Dependencies:** None.
- **Concrete example — Hiring Pipeline:**
  ```precept
  event AddInterviewer warning           # administrative — needs attention
  event RecordInterviewFeedback warning  # feedback is concern-level
  ```
- **Value/practicality assessment:** Completes the event verdict vocabulary. Same caution as state `warning` — fewer events are naturally "concern-level" compared to success/error. Events like AddInterviewer and RequestDocument are operational but not always concerning. The label is more subjective than success/error.

---

##### `antonym of <Event>`

- **Syntax:** `event Deny antonym of Approve`
- **Entity type:** Event
- **Mechanism:** Semantic
- **What it means:** Declares that two events are semantic opposites — one is the positive path, the other is the negative. The annotation is informational and enables tooling pairing.
- **Compile-time check:** Soft diagnostic — warn if both events carry the same verdict modifier (e.g., both marked `success` would be contradictory for declared antonyms). Warn if one antonym is unreachable while the other is not (asymmetric lifecycle paths).
- **Tooling impact:** Diagram renders antonym pairs as visually balanced branches (e.g., green/red fork from a shared source state). Inspector groups antonym pairs together. MCP metadata includes `antonymOf: "EventName"`. AI consumers understand the semantic relationship.
- **Precedent:** No direct precedent in workflow systems. The concept maps to decision branches in BPMN (Yes/No gateways) and complementary operations in business rule engines.
- **Dependencies:** None. Parameterized syntax follows existing `sealed after <State>` precedent. Enhanced by verdict modifiers — `event Approve success antonym of Deny` + `event Deny error` gives a complete semantic picture.
- **Concrete example — Insurance Claim:**
  ```precept
  event Approve success antonym of Deny
  event Deny error antonym of Approve

  from UnderReview on Approve when ... -> transition Approved
  from UnderReview on Deny -> set DecisionNote = Deny.Note -> transition Denied
  ```
- **Value/practicality assessment:** Medium. Strong diagram value — the antonym relationship enables balanced visual rendering of decision forks. The soft diagnostic (consistent verdicts) provides light guidance. Maps to the universal approve/deny, accept/reject, fund/decline pattern present in nearly every business sample. Low implementation cost.
- **Origin:** Frank creative sweep

---

##### `primary`

- **Syntax:** `event Approve primary`
- **Entity type:** Event
- **Mechanism:** Semantic
- **What it means:** This is the happy-path event — the expected, desired progression. Diagram bold styling and inspector prioritization signal that this event represents the intended flow.
- **Compile-time check:** None. Pure tooling directive. A soft hint could warn if a `primary` event is marked `error` (contradictory intent), but this is advisory.
- **Tooling impact:** Diagram renders primary event edges with bold/thicker lines and prominent positioning. Inspector lists primary events first. MCP metadata includes `primary: true`. AI consumers prioritize primary events in suggestions and explanations.
- **Precedent:** UML's main success scenario in use cases. Feature-flagged "golden path" in distributed tracing. Critical path highlighting in project management tools.
- **Dependencies:** None. Complementary to `scaffold` (#95) — `primary` marks the main flow, `scaffold` marks the setup flow. Compatible with verdict modifiers — `event Approve primary success` gives full characterization.
- **Concrete example — Loan Application:**
  ```precept
  event Submit primary
  event Approve primary
  event FundLoan primary
  # The happy path: Submit → Approve → FundLoan
  ```
- **Value/practicality assessment:** Medium. Strong diagram value — happy-path highlighting makes complex lifecycle diagrams scannable at a glance. No enforcement concerns. Subjective (author decides what's "primary"), but that's inherent to semantic annotations. Maps to the natural human understanding of lifecycle narratives.
- **Origin:** Elaine creative sweep

---

##### `scaffold`

- **Syntax:** `event VerifyDocuments scaffold`
- **Entity type:** Event
- **Mechanism:** Semantic
- **What it means:** This is a setup/preparation event — necessary groundwork before the main flow progresses. Not the primary action but an enabler. Diagram lighter styling signals supporting role.
- **Compile-time check:** None. Pure tooling directive.
- **Tooling impact:** Diagram renders scaffold event edges with thinner lines and lighter colors (gray/muted). Inspector shows "Scaffold event — setup/preparation." MCP metadata includes `scaffold: true`. AI consumers understand this event is setup, not the main action.
- **Precedent:** Setup/teardown in testing frameworks. Prerequisite activities in project management. Scaffolding in construction/software development.
- **Dependencies:** None. Complementary to `primary` (#94) — together they create visual weight hierarchy: primary events are prominent, scaffold events are de-emphasized, unmarked events are default weight.
- **Concrete example — Loan Application:**
  ```precept
  event VerifyDocuments scaffold     # prerequisite for Approve, not the main action

  from UnderReview on VerifyDocuments -> set DocumentsVerified = true -> no transition
  ```
- **Value/practicality assessment:** Medium. Strong diagram value — de-emphasizing setup events reduces visual noise on lifecycle diagrams. Maps to real patterns — VerifyDocuments, AddInterviewer, RequestDocument are all setup/prep events. Subjective annotation, same as `primary`. The `primary`/`scaffold` pair creates a visual weight system for event importance.
- **Origin:** Elaine creative sweep

---

#### 3.2.3 Semantic — Fields

##### `sensitive`

- **Syntax:** `field SSN as string nullable sensitive`
- **Entity type:** Field
- **Mechanism:** Semantic (tooling directive)
- **What it means:** Contains PII, PHI, or other sensitive data. Tooling should mask the value.
- **Compile-time check:** None. This is a pure tooling directive.
- **Tooling impact:** MCP tools mask the field value in output (show `"***"` instead of actual value). Preview panel masks sensitive values. Inspector shows "Sensitive — value masked." Diagram omits sensitive field values from state labels.
- **Precedent:** `[SensitiveData]` attributes in .NET. `@sensitive` in Terraform. GDPR field-level annotations in data platforms. The concept is common across systems.
- **Dependencies:** None.
- **Concrete example:**
  ```precept
  field SSN as string nullable sensitive
  field CreditScore as number default 0 sensitive
  field ClaimantEmail as string nullable sensitive
  ```
- **Value/practicality assessment:** No compile-time check — this is the first "tooling-directive-only" modifier candidate (modifier role #3 without roles #1 or #2). Accepting `sensitive` requires a philosophical decision: does Precept accept modifiers with no structural enforcement and no soft diagnostics? The scope rule from the #58 research says "a modifier must be either structurally verifiable or tooling-actionable." `sensitive` qualifies under "tooling-actionable" — it has concrete, defined tooling behavior (masking). The question is whether the team considers tooling-only sufficient justification for a language keyword.

---

##### `audit`

- **Syntax:** `field ApprovedAmount as number default 0 audit`
- **Entity type:** Field
- **Mechanism:** Semantic (tooling directive)
- **What it means:** Mutations to this field should be tracked for compliance or auditing purposes.
- **Compile-time check:** None. Tooling metadata only.
- **Tooling impact:** MCP metadata marks the field for audit tracking. Host applications could use this to trigger audit logging. Inspector shows "Audited field — mutations tracked."
- **Precedent:** Audit trail columns in databases. `@audit` annotations in enterprise frameworks. SOX compliance field marking.
- **Dependencies:** None.
- **Concrete example:**
  ```precept
  field ApprovedAmount as number default 0 audit
  field DecisionNote as string nullable audit
  ```
- **Value/practicality assessment:** Weaker than `sensitive` as a tooling directive. `sensitive` has concrete, defined tooling behavior (masking is a specific visual treatment). `audit` is a metadata hint with no specific tooling behavior defined in Precept's own tools — the behavior depends on the host application consuming the audit flag. This makes `audit` a host-application concern rather than a language concern. The scope rule requires "tooling-actionable" — `audit` is only actionable if the host application acts on it, which is outside Precept's control.

---

##### `volatile`

- **Syntax:** `field ReopenCount as number default 0 volatile`
- **Entity type:** Field
- **Mechanism:** Semantic
- **What it means:** This field changes frequently during the lifecycle. Tooling should de-prioritize it in diff views and change summaries — it's expected to change, so changes are not noteworthy.
- **Compile-time check:** None. Pure tooling directive.
- **Tooling impact:** Inspector de-prioritizes volatile fields in change summaries (collapsed/grayed). MCP diff output sorts volatile fields last. Diagram omits volatile field values from state labels. AI consumers understand this field is noisy and changes often.
- **Precedent:** `volatile` in C/C++ (different meaning — memory model). "Noisy" fields in data quality tools. Change-frequency annotations in data governance.
- **Dependencies:** None.
- **Concrete example — IT Helpdesk Ticket:**
  ```precept
  field ReopenCount as number default 0 volatile
  # ReopenCount changes on every reopen — de-prioritize in diffs
  ```
- **Value/practicality assessment:** Low-medium. Tooling-actionable (diff de-prioritization). Subjective annotation — the author decides what's "volatile." Limited enforcement or diagnostic value. The primary benefit is reducing noise in tooling output for fields that change frequently.
- **Origin:** Frank creative sweep

---

##### `temporal`

- **Syntax:** `field ReviewStartDate as number default 0 temporal`
- **Entity type:** Field
- **Mechanism:** Semantic
- **What it means:** This field represents a time-like value — a timestamp, date, or time-ordered number. Monotonically increasing by convention (though not structurally enforced). Tooling should render it as a temporal value.
- **Compile-time check:** None. Pure tooling directive. A soft hint could warn if the field type is not `number` (temporal fields should be numeric for ordering).
- **Tooling impact:** Inspector renders temporal fields as timestamps (if the host provides a format). Diagram could show temporal fields on a timeline axis. MCP metadata includes `temporal: true`. AI consumers understand this field represents time progression.
- **Precedent:** `@timestamp` in Elasticsearch. `datetime` types in databases. Temporal decorators in data modeling tools.
- **Dependencies:** None. Related to `monotonic` (#26) — a temporal field is conceptually monotonic, but `temporal` is a semantic label while `monotonic` is a structural constraint. The author could combine both: `field X as number default 0 monotonic temporal`.
- **Concrete example:**
  ```precept
  field ReviewStartDate as number default 0 temporal
  field ApprovalDate as number default 0 temporal
  ```
- **Value/practicality assessment:** Low. Tooling-actionable but narrow. Precept has no native date type — temporal semantics are a host-application concern. The modifier's value depends on the host providing temporal formatting. Without that, it's just a metadata flag.
- **Origin:** Frank creative sweep

---

##### `internal`

- **Syntax:** `field DocumentsVerified as boolean default false internal`
- **Entity type:** Field
- **Mechanism:** Semantic
- **What it means:** This is a mechanical/operational field used for internal workflow logic rather than business-facing data. Visual surfaces should de-emphasize it.
- **Compile-time check:** None. Pure tooling directive.
- **Tooling impact:** Diagram omits or de-emphasizes internal fields (collapsed in field list, grayed out). Inspector groups internal fields separately (after business-facing fields). MCP metadata includes `internal: true`. AI consumers distinguish business data from workflow machinery.
- **Precedent:** `internal` access modifier in C#. Private/implementation fields in OOP. The concept of implementation-detail vs. public-interface data is universal.
- **Dependencies:** None. Related to `hidden` (#100) — `internal` de-emphasizes, `hidden` completely excludes. Different degrees of visibility reduction.
- **Concrete example — Loan Application:**
  ```precept
  field DocumentsVerified as boolean default false internal
  # Verification flag is operational — not the kind of data a customer sees
  ```
- **Value/practicality assessment:** Medium. Strong tooling value — the business/operational field distinction helps focus visual surfaces on what matters to stakeholders. Maps to real patterns — DocumentsVerified, FraudFlag, ReturnReceived are operational flags, not business data. Subjective but intuitive.
- **Origin:** Elaine creative sweep

---

##### `summary`

- **Syntax:** `field ClaimantName as string nullable summary`
- **Entity type:** Field
- **Mechanism:** Semantic
- **What it means:** This field is the entity's display label — used as the title in inspector panels and entity cards. When a precept instance needs a one-line identity, this field provides it.
- **Compile-time check:** Soft diagnostic — warn if more than one field is marked `summary` in the same precept (ambiguous display label). Warn if the field is numeric (numbers rarely make good display labels).
- **Tooling impact:** Inspector uses the summary field's value as the panel title (e.g., "Insurance Claim: John Doe" instead of "Insurance Claim instance"). Entity cards in visual surfaces show the summary value. MCP metadata includes `summary: true`.
- **Precedent:** `__str__` / `toString()` in OOP. `displayName` in React components. `<title>` in HTML. The concept of a human-readable display label is universal.
- **Dependencies:** None. Related to `identity` (#27) — `identity` marks identifying data (writeonce + immutable); `summary` marks display data (may change). An identity field could also be the summary field, but they serve different roles.
- **Concrete example — Insurance Claim:**
  ```precept
  field ClaimantName as string nullable summary
  # Inspector title: "Insurance Claim: Jane Smith"
  ```
- **Value/practicality assessment:** Medium. Strong tooling value — every inspector/dashboard needs an entity display label, and without `summary` the tooling must guess or use the precept name alone. Simple implementation (parser flag + single-summary soft check). Maps to real need.
- **Origin:** Elaine creative sweep

---

##### `hidden`

- **Syntax:** `field ImplementationDetail as string nullable hidden` / `state InternalRouting hidden`
- **Entity type:** Field or State (multi-entity)
- **Mechanism:** Semantic
- **What it means:** Completely excluded from visual surfaces — diagrams, inspector panels, and public-facing output skip this declaration. The entity still exists in the runtime model but is invisible to non-technical consumers.
- **Compile-time check:** None. Pure tooling directive.
- **Tooling impact:** Diagram omits hidden states from the state graph and hidden fields from field lists. Inspector skips hidden entities. MCP metadata includes `hidden: true` so consumers can filter. AI consumers know to exclude hidden entities from user-facing summaries.
- **Precedent:** `[EditorBrowsable(EditorBrowsableState.Never)]` in .NET. `hidden` HTML attribute. `@internal` in documentation generators. Visibility control is universal in tooling.
- **Dependencies:** None. Stronger than `internal` (#98) — `internal` de-emphasizes, `hidden` completely excludes. Multi-entity applicability (fields and states).
- **Concrete example:**
  ```precept
  field InternalTrackingId as string nullable hidden
  state SystemRouting hidden
  # Neither appears in diagrams or inspector
  ```
- **Value/practicality assessment:** Medium. Tooling-actionable with concrete behavior (exclusion from visual surfaces). Applicable to implementation-detail fields and system-level routing states that would clutter visual output. The risk is that hiding elements makes the precept harder to understand from visual surfaces alone — hidden elements still affect behavior but are invisible. A "show hidden" toggle in tooling mitigates this.
- **Origin:** Elaine creative sweep

---

#### 3.2.4 Semantic — Precept

##### `auditable`

- **Syntax:** `precept InsuranceClaim auditable`
- **Entity type:** Precept
- **Mechanism:** Semantic (tooling directive)
- **What it means:** All fields in this precept are implicitly `audit`-flagged. Every field mutation should be tracked for compliance purposes. Syntactic sugar for applying `audit` to every field individually.
- **Compile-time check:** None. Pure tooling directive — propagates the `audit` flag to all fields.
- **Tooling impact:** MCP metadata marks all fields for audit tracking. Inspector shows "Auditable precept — all mutations tracked." Host applications consuming the precept metadata can trigger audit logging for every field without per-field opt-in.
- **Precedent:** HIPAA and SOX compliance frameworks often require complete audit trails for regulated entities. Database-level audit triggers that capture all column changes. Django's `django-simple-history` applies to entire models, not individual fields.
- **Dependencies:** Requires per-field `audit` (#48) semantics to be defined. If per-field `audit` has no concrete tooling behavior in Precept's own tools, `auditable` inherits that limitation. Syntactic sugar for per-field `audit` — same relationship as `governed` (#31) to per-state `guarded` (#2).
- **Concrete example:**
  ```precept
  precept InsuranceClaim auditable

  field ClaimantName as string nullable
  field ClaimAmount as number default 0
  field ApprovedAmount as number default 0

  # All three fields are implicitly audit-tracked
  ```
- **Value/practicality assessment:** Low. Inherits the weakness of per-field `audit` — the actual audit behavior depends on the host application, not Precept's own tools. `audit` as a per-field modifier (#48) is already borderline (tooling-only, no specific Precept tooling behavior). The precept-level version is syntactic sugar on a borderline modifier. If per-field `audit` gains concrete Precept tooling behavior (e.g., MCP tools reporting mutation history), `auditable` becomes more valuable as a blanket opt-in. Until then, the value proposition is thin.

---

##### `pattern <name>`

- **Syntax:** `precept InsuranceClaim pattern "claims-processing"`
- **Entity type:** Precept
- **Mechanism:** Semantic
- **What it means:** Classifies the workflow by pattern type — a free-text label that identifies the workflow archetype (e.g., "approval-flow", "claims-processing", "hiring-pipeline", "reimbursement"). Enables search, categorization, and pattern-based recommendations.
- **Compile-time check:** None. Pure tooling directive.
- **Tooling impact:** MCP metadata includes `pattern: "name"`. Catalog/registry tools can group precepts by pattern. Inspector shows "Pattern: claims-processing." AI consumers can recommend template patterns or compare precepts within the same pattern family.
- **Precedent:** Workflow patterns (van der Aalst). Design pattern annotations in code. Tags/labels in content management. Pattern classification is standard in workflow theory.
- **Dependencies:** None. The string parameter grammar follows the `because "..."` and proposed `display "..."` patterns.
- **Concrete example:**
  ```precept
  precept InsuranceClaim pattern "claims-processing"
  precept LoanApplication pattern "approval-flow"
  precept HiringPipeline pattern "hiring-pipeline"
  ```
- **Value/practicality assessment:** Medium. Tooling-actionable for catalog/search/classification. The free-text string is flexible but uncontrolled — no enum of valid patterns. Value depends on whether a pattern vocabulary emerges from usage. Useful for large precept repositories where classification aids discovery.
- **Origin:** Elaine creative sweep

---

##### `cyclic` (precept)

- **Syntax:** `precept InsuranceClaim cyclic`
- **Entity type:** Precept
- **Mechanism:** Semantic
- **What it means:** The lifecycle intentionally contains cycles. The complement of `linear` — where `linear` declares "no cycles by design," `cyclic` declares "cycles are intentional, not accidental." A documentation/tooling annotation, not a structural constraint (the absence of `linear` already permits cycles).
- **Compile-time check:** Soft diagnostic — warn if the graph is actually acyclic (declared cyclic but no cycles exist — stale or incorrect annotation). This catches intent/reality mismatch.
- **Tooling impact:** Diagram renders with a cycle-aware layout strategy (accommodates back-edges rather than treating them as layout errors). Inspector shows "Cyclic lifecycle — cycles are intentional." MCP metadata includes `cyclic: true`. AI consumers know cycles are by design, not bugs.
- **Precedent:** Complement of DAG declarations in workflow systems. BPMN's loop markers on sub-processes. No workflow system has an explicit `cyclic` keyword on the definition level.
- **Dependencies:** Contradictory with `linear` (#29) — cannot be both acyclic and intentionally cyclic. The compiler should error if both appear. Enhanced by per-state `cyclic` (#43) — precept-level `cyclic` is a global intent declaration; per-state `cyclic` marks specific states.
- **Note:** Previously excluded in §5 as "vacuous — declares the default condition." Re-evaluated under Shane's inclusion criteria: the annotation IS tooling-actionable (diagram layout, AI signaling, acyclicity-mismatch diagnostic). The default condition objection is valid for structural enforcement, but the semantic annotation still carries value as intent declaration and tooling directive.
- **Concrete example — Insurance Claim:**
  ```precept
  precept InsuranceClaim cyclic

  # Declares that the UnderReview ↔ document loop is intentional
  # Graph layout engine accommodates cycles rather than fighting them
  ```
- **Value/practicality assessment:** Low. The annotation provides marginal value over the absence of `linear` — most authors and AI consumers can infer that cycles exist by reading the transition rows. The soft diagnostic (warn if actually acyclic) catches a specific error but is a narrow use case. Including per Shane's re-evaluation criteria; honest assessment is that the practical demand is low compared to `linear`'s clear structural guarantee.
- **Origin:** George creative sweep; re-evaluated from §5 exclusion

---

### 3.3 Severity Modifiers

Severity modifiers change how the engine responds to constraint violations — converting blocking errors to non-blocking warnings.

---

#### 3.3.1 Severity — Rules

##### `warning` (on invariant/assert)

- **Syntax:** `invariant X because "Y" warning` or `in State assert X because "Y" warning`
- **Entity type:** Rule (invariant, assert)
- **Mechanism:** Severity
- **What it means:** Violation of this rule flags the operation with a warning but does not block it. The default is `error` (blocks). The author opts in to `warning` explicitly.
- **Compile-time check:** New diagnostics:

  | Diagnostic | Trigger | Severity |
  |------------|---------|----------|
  | C60 | Warning rule always evaluates true (dead warning) | Warning |
  | C61 | Same expression used as both error and warning on overlapping scope | Error |
  | C62 | All constraints on an event path are warning-only | Warning |

- **Tooling impact:** MCP `precept_inspect` reports warning constraints alongside error constraints (with severity labels). Preview panel shows warning violations in amber. Inspector distinguishes error vs warning constraint status. MCP `precept_fire` result includes `Warnings` list alongside `Violations`.
- **Precedent:** Kubernetes (Deny vs Warn — same CEL expression, different consequence based on binding), ESLint (error vs warn — same rule, different exit code), BPMN (error events terminate, escalation events continue), FluentValidation (severity levels on validators).
- **Dependencies:** Requires the philosophy evolution (Section 2). Independent of all structural and semantic modifiers — `warning` on rules and `warning` on states/events are different mechanisms on different entity types.
- **Runtime behavior (George's Model D):**
  ```
  1. Evaluate ALL constraints (unchanged — collect-all semantics)
  2. Partition: errors vs warnings by declared severity
  3. If any error-level violation → ConstraintFailure (blocks, unchanged)
  4. If only warning-level violations → TransitionWithWarnings / NoTransitionWithWarnings
     (mutation commits, warnings attached to result)
  5. If neither → clean success (unchanged)
  ```
  Error precedence is strict: if ANY error-level rule fails, the entire operation blocks. Warning evaluation is moot on a blocked operation.

  New outcome values: `TransitionWithWarnings`, `NoTransitionWithWarnings`. `IsSuccess` returns `true` for both.
- **Concrete example — Insurance Claim with advisory rules:**
  ```precept
  precept InsuranceClaim

  # Hard rules (error — block operation)
  invariant ClaimAmount >= 0 because "Claim amounts cannot be negative"
  invariant ApprovedAmount <= ClaimAmount because "Approved amounts cannot exceed the claim"

  # Advisory rules (warning — flag but allow)
  invariant DecisionNote == null or DecisionNote.length <= 500 because "Decision notes should be concise" warning
  in UnderReview assert AdjusterName != null because "An adjuster should be assigned during review" warning
  ```
  The advisory rules capture business guidelines that aren't hard blockers. Today these live in service-layer code. With warning-level severity, they move into the `.precept` file — one-file completeness improves.
- **Value/practicality assessment:** Directly answers Shane's non-blocking question. George's Model D analysis confirms enforceability. Implementation touches four layers: parser (recognize `warning` keyword after `because`), model (add severity to constraints), engine (partition violations), result types (add `Warnings`, new outcomes). The engine change is clean because constraint evaluation is already isolated — partitioning happens after evaluation, not during. The philosophy reframing is the hardest part — not technically, but as a product identity decision. Runtime complexity: medium (touches the commit/rollback boundary and all consumers that switch on `TransitionOutcome`).

---

## Section 4: Cross-Tier Interactions

Modifier candidates interact across categories. These interactions are stated as facts — what combinations mean and what they enable.

### `terminal` + `success`/`error` = endpoint taxonomy

```precept
state Approved terminal success  # structural boundary + semantic significance
state Denied terminal error      # structural boundary + semantic significance
state OnHold warning             # semantic significance only (not terminal — has outgoing transitions)
```

`terminal` says "lifecycle ends here." `success`/`error` says "ending here is good/bad." Together they create an **endpoint taxonomy**: the compiler knows where the lifecycle ends AND what those endpoints mean. Each modifier contributes independent information.

### `advancing`/`settling` + `success`/`error`/`warning` = full event characterization

```precept
event Submit entry advancing success        # intake, always transitions, positive trajectory
event VerifyDocuments settling success       # data accumulation, stays in state, positive progress
event Approve advancing success             # routing, transitions on success, positive intent
event Deny advancing error                  # routing, transitions to failure, negative intent
event RequestDocument settling warning      # data accumulation, concern-level (something missing)
```

The structural modifier tells you the event's **mechanism** (does it route or accumulate?). The semantic modifier tells you its **significance** (positive, negative, or concern). Together they give a complete one-line characterization of every event.

### `sealed after` + `terminal` = freeze-point clarity

```precept
state Approved terminal success
field ApprovedAmount as number default 0 sealed after Approved
```

`sealed after` a `terminal` state is maximally clear: the field freezes when the entity reaches its endpoint. The compiler can verify this more precisely if `terminal` is declared (no outgoing transitions means no further mutation is structurally possible).

### Rule severity is independent of semantic modifiers

`warning` on `invariant` (severity) and `warning` on `state` (semantic) are different mechanisms on different entity types. They share a keyword but not a mechanism. There are no cross-tier diagnostic interactions.

### `entry` + `advancing` = intake event characterization

```precept
event Submit entry advancing     # fires only from initial state AND always transitions
```

An `entry advancing` event is the lifecycle's ignition — it fires only at the start and always moves the entity forward. Both checks are independently provable.

### `guarded` (state) + `terminal` = protected endpoints

```precept
state Approved terminal guarded success
```

A state that is terminal, guarded, and success-annotated is the most richly described endpoint: it's where the lifecycle ends, it can't be reached without explicit conditions, and it signifies positive completion. Each modifier is independently meaningful; combined they describe the full character of the state.

### `writeonce` + `identity` = subsumption

`identity` implies `writeonce` (an identifying field should not change). If both modifiers exist, `identity` subsumes `writeonce` for the fields it applies to. The subsumption relationship needs a design decision: does `identity` automatically carry `writeonce` semantics, or does the author declare both?

### `completing` + `terminal` = event-level endpoint targeting

`completing` requires `terminal` to define which states are eligible targets. Without `terminal`, the compiler would need to infer terminal status from graph topology. With `terminal`, the check is a simple lookup.

### `required` + `terminal` = path analysis clarity

`required` checks that all initial→terminal paths visit the required state. The analysis is cleaner when `terminal` explicitly marks endpoint states — otherwise the analyzer must infer them.

### `forward` + `milestone` = exactly-once checkpoint

```precept
state UnderReview milestone forward  # must be visited, and visited exactly once
```

`milestone` says "every path visits here." `forward` says "once left, never re-entered." Together they enforce **exactly-once visitation** — the strongest checkpoint guarantee. This combination maps directly to compliance requirements like SOX mandatory review (must happen) and KYC single-pass verification (must happen once, not twice). Each modifier contributes independent structural information.

### `forward` + `terminal` = trivial forward

```precept
state Paid terminal forward          # terminal is trivially forward
```

A `terminal` state has no outgoing transitions, so it can never be left and therefore can never be re-entered. `forward` on a terminal state is vacuously true. The compiler could accept it silently (harmless redundancy) or warn ("terminal states are always forward"). This is a design question, not a correctness issue.

### `forward` + `irreversible` = complementary flow guarantees

`forward` (state-level) and `irreversible` (event-level) check the same underlying graph property — absence of reverse paths — from different declaration surfaces. `forward` on state S means "no path from any successor of S back to S." `irreversible` on event E means "for each transition row of E, no path from target back to source."

In practice, if all events leaving a `forward` state are `irreversible`, the constraints are consistent. If an event leaving a `forward` state is NOT `irreversible`, it means the target has a path back to the source — but that path doesn't pass through the `forward` state (otherwise the `forward` check would fail). This combination gives fine-grained flow control: `forward` constrains at the state level, `irreversible` at the event level.

### `forward` vs `cyclic` = contradictory pair

A state cannot be both `forward` and `cyclic` — `forward` means no cycle through this state, while `cyclic` declares intentional cycle participation. The compiler should error if both appear on the same state. This is the structural/semantic complement pair: `forward` constrains, `cyclic` documents intent.

### `gate` + `forward` = subsumed forward

```precept
state Approved gate forward      # forward is redundant — gate implies forward
```

A `gate` state is necessarily `forward` — if all predecessors are closed off, the gate state itself (being in its own ancestor set) cannot be re-entered. The compiler could accept this combination silently (harmless redundancy) or hint that `forward` is subsumed by `gate`. This mirrors the `terminal` + `forward` redundancy pattern.

### `gate` + `milestone` = mandatory phase boundary

```precept
state UnderReview milestone gate  # every path visits here; once visited, all prior states close
```

`milestone` says "every initial→terminal path visits this state." `gate` says "once entered, all predecessor states are closed off." Together they enforce a **mandatory phase boundary** — the entity must pass through this gate (no skip), and once through, the entire prior phase is sealed. This is stronger than `milestone forward` (exactly-once visitation): `gate` doesn't just prevent re-entry to this one state — it prevents return to ANY predecessor. Applicable to compliance gates in regulated industries (every insurance claim must be reviewed, and once reviewed, the intake phase is permanently closed).

### `gate` + `terminal` = permanent ancestor closure

```precept
state Approved terminal gate     # lifecycle ends; all predecessors permanently closed
```

`terminal` means no outgoing transitions — the lifecycle ends. `gate` means all predecessors are closed. Together, the ancestor closure is permanent because there is no downstream state from which a violation could occur. The `gate` check on a terminal state is trivially satisfied (no downstream transitions exist to violate it). Like `terminal` + `forward`, this combination is vacuously true — the question is whether the compiler should accept it silently or hint at redundancy.

### `gate` vs `cyclic` = contradictory pair

A state cannot be both `gate` and `cyclic` — `gate` requires that all predecessors are closed (which implies the gate state itself cannot be re-entered), while `cyclic` declares intentional cycle participation. Since `gate` implies `forward`, and `forward` and `cyclic` are already contradictory, `gate` + `cyclic` is also contradictory. The compiler should error if both appear on the same state.

### `linear` (precept) + `forward` (state) = subsumption

```precept
precept LoanApplication linear

state Submitted forward          # redundant — `linear` implies all states are forward
```

A `linear` precept's graph is a DAG — no state can be part of a cycle. Per-state `forward` checks the same property (no cycle through this state). Every state in a linear precept is trivially forward. The compiler could accept this silently (harmless redundancy) or hint that `forward` is subsumed by `linear`. Same pattern as `gate` + `forward` subsumption.

### `linear` (precept) vs `cyclic` (state) = contradictory pair

```precept
precept Workflow linear
state UnderReview cyclic         # ✗ contradicts `linear`
```

A `linear` precept declares no cycles. A `cyclic` state declares intentional cycle participation. These are contradictory — the compiler should error. The author must choose: use `linear` (no cycles anywhere) or drop `linear` and use `cyclic` to mark the intentional loop.

### `linear` (precept) + `terminal` (state) = complementary bookends

```precept
precept LoanApplication linear

state Draft initial
state Funded terminal
state Declined terminal
```

`linear` guarantees acyclic progress. `terminal` marks endpoints. Together they describe a lifecycle that starts at one point, progresses through intermediate stages without revisiting any, and ends at declared terminal states. This is the cleanest lifecycle shape — a complete DAG with explicit start and endpoints.

### `governed` (precept) + `guarded` (state) = subsumption

```precept
precept InsuranceClaim governed

state Approved guarded           # redundant for non-initial states — `governed` implies guarded
```

`governed` makes every non-initial state implicitly `guarded`. A per-state `guarded` annotation on a non-initial state is redundant. A `guarded` annotation on the initial state is NOT subsumed — `governed` exempts the initial state. The compiler could accept redundant annotations silently or hint at subsumption.

### `strict` (precept) + `warning` (rule severity) = orthogonal dimensions

```precept
precept LoanApplication strict

invariant RequestedAmount >= 0 because "Amount cannot be negative"
invariant DecisionNote == null or DecisionNote.length <= 500 because "Notes should be concise" warning
```

`strict` upgrades analysis-phase warnings (C48–C53+) to errors. It does NOT upgrade author-declared rule severities. The `warning` on the invariant above is the author's behavioral choice — they want this rule to flag, not block. `strict` respects that choice. These are orthogonal dimensions: `strict` controls the compiler's tolerance for structural issues; rule `warning` controls the engine's response to constraint violations.

### `linear` (precept) + `strict` (precept) = combined rigor

```precept
precept LoanApplication linear strict
```

A precept can carry multiple precept-level modifiers. `linear strict` says "the lifecycle is acyclic AND the compiler must have zero warnings." This combination produces the tightest validation: no cycles, no unreachable states, no orphaned events, no dead ends — all enforced as hard errors. Natural for production-quality regulated lifecycles.

### `governed` (precept) + `strict` (precept) = maximum rigor

```precept
precept InsuranceClaim governed strict
```

All state entries require guards AND all analysis warnings are errors. This is the defensive maximum — every transition must justify itself, and every structural issue must be resolved. Best suited for lifecycles in heavily regulated domains (financial, healthcare, legal).

### Precept-level modifiers + stateless precepts = inapplicable

Precept-level structural modifiers (`linear`, `governed`, `exhaustive`, `sequential`, `total`) apply to the transition graph. Stateless precepts have no transition graph. The compiler should error if a stateless precept (no `state` declarations) carries any graph-dependent modifier. `strict` could apply to stateless precepts (it affects analysis diagnostics), but the only applicable diagnostic for stateless precepts is C49 (orphaned events), which would be unusual in a stateless context. The `stateless` modifier (#36) is contradictory with all state-graph modifiers — the compiler should error if `stateless` appears alongside `linear`, `governed`, `exhaustive`, `sequential`, `total`, `single-entry`, or `single-exit`.

### `oneshot` = `milestone forward` (compositional modifier)

```precept
state UnderReview oneshot        # equivalent to: state UnderReview milestone forward
```

`oneshot` is the composition of `milestone` (must visit) + `forward` (no re-entry) = exactly-once visitation. This is NOT a synonym — it's a convenience keyword for a two-modifier combination. If `milestone` and `forward` both ship, `oneshot` is syntactic sugar. The value is readability: `oneshot` is one word that captures a common compliance pattern. The compiler could accept `milestone forward oneshot` as harmless redundancy or warn about subsumption.

### `exhaustive` vs `strict` = subset relationship

`strict` upgrades ALL analysis warnings to errors. `exhaustive` upgrades only C50 (dead-end). `exhaustive` is a strict subset of `strict` for the C50 case. An `exhaustive strict` precept has `exhaustive` subsumed by `strict`. An `exhaustive` precept without `strict` gets targeted dead-end enforcement while tolerating other warnings.

### `sequential` implies `linear` = subsumption

A `sequential` precept (strict declaration-order progression) is always a DAG. `sequential` is strictly more restrictive than `linear` — a linear precept allows branching, sequential does not. A `sequential linear` precept has `linear` subsumed by `sequential`.

### `single-entry` + `single-exit` = pipeline lifecycle

```precept
precept SimpleWorkflow single-entry single-exit
```

Together, `single-entry` and `single-exit` describe a lifecycle with exactly one intake event and exactly one terminal endpoint — a classic pipeline. Combined with `linear`, this creates the simplest possible lifecycle shape: one start, one finish, no branches, no cycles. Combined with `sequential`, it creates a strict conveyor belt.

### `auditable` + `sensitive` = interaction question

If a precept is `auditable` (all fields implicitly `audit`) and a field is `sensitive`, the audit and masking behaviors interact. Should audit logging mask sensitive field values? This is a host-application concern, but the modifier combination surfaces the tension.

### `depth <N>` + `linear` = well-defined depth

`depth <N>` is practical only with `linear`. In a `linear` precept, the transition graph is a DAG and depth (path length from initial) is well-defined. In cyclic graphs, path lengths are ambiguous. The compiler could restrict `depth` to `linear` precepts or apply it generally with the understanding that non-linear satisfiability is rare.

---

## Section 5: Candidates Not Included in the Taxonomy

These concepts were evaluated during the #58 and verdict modifier research but excluded from the candidate inventory. Each is excluded for one of four legitimate grounds: requires runtime state that doesn't exist, is computationally undecidable, is a true semantic synonym of an existing candidate, or would change Precept's fundamental model.

| Concept | Entity | Why excluded | Legitimate ground |
|---------|--------|-------------|-------------------|
| `once` | Event | Requires a runtime counter — the compiler cannot verify "at most one firing" from declared structure. Achievable today via a boolean field + guard pattern (`field SubmitDone as boolean default false` + `when SubmitDone == false` + `set SubmitDone = true`). | Runtime state (firing history) |
| `idempotent` | Event | Mutation commutativity analysis is undecidable in the general case. Whether re-firing an event produces the same result depends on expression evaluation and guard conditions that can't be statically determined. | Undecidable |
| State-type system | State | Would turn states into typed entities with per-type behavioral contracts (e.g., `state Closed as terminal`). Architecturally invasive — breaks the current flat model where all states are structurally identical. Keyword modifiers add information without changing the fundamental model; types change the model. | Changes fundamental model |
| Event lifecycle annotations | Event | Pre/postcondition specification on events (e.g., `event Approve preconditions [R1, R2]`). Exact semantic duplicate of existing constructs: `on Event assert` is the precondition surface; state asserts and invariants are the postcondition surface. | True semantic duplicate of existing language surface |
| Temporal properties | State | Time-bounded residency (e.g., `state Processing timeout 30m`), deadlines, SLA modifiers. Requires a runtime clock, a polling mechanism for timeout detection, and system-generated timeout events. Precept is event-driven with no clock model. | Runtime state (clock) + changes fundamental model |
| Hierarchical state modifiers | State | Modifiers that reference other states (e.g., `state Review parent of TechnicalReview, LegalReview`). Precept's flat state model is a deliberate design choice. Modifiers that establish parent-child relationships between states introduce hierarchy through the back door. | Changes fundamental model |
| Configuration-level verdicts | — | Putting verdict declarations in a `.preceptrc` or similar configuration file rather than in the `.precept` file. Not a modifier — a configuration mechanism. | Not a modifier |
| `forward` (event-level) | Event | Every transition this event produces moves to a state from which the source is unreachable. Structurally identical to `irreversible` (#21 in the taxonomy) — same graph property, same compile check, different name. | True synonym of `irreversible` (#21) |
| `spanning` / `noskip` | Event | Events constrained to transition only to "adjacent" states in some ordering. Requires defining a canonical total ordering on states — no such ordering exists in Precept. Topological orderings are non-unique for DAGs and undefined for cyclic graphs. The concept is ill-defined without infrastructure that doesn't exist. | Ill-defined — no canonical state ordering exists in the language |
| `cyclic` (precept-level) | Precept | Declares the entire lifecycle intentionally contains cycles. The absence of `linear` already permits cycles — `cyclic` as a precept-level modifier adds no structural constraint and no soft diagnostic. Declaring the default condition (cycles permitted) is vacuous. Per-state `cyclic` (#43 in taxonomy) marks SPECIFIC states as intentionally participating in a cycle, which is more targeted and useful. | Vacuous — declares the default condition |
| `forward` (precept-level) | Precept | All states are implicitly `forward`. Semantically identical to `linear` (#29) — both declare the graph is a DAG. | True synonym of `linear` (#29) |
| `deterministic` | Precept | Every (state, event) pair has at most one non-reject outcome. Conflates the multi-row guard pattern — which is fundamental to Precept's design (guarded success + unguarded reject fallback) — with nondeterminism. Real nondeterminism doesn't exist in Precept — first-match evaluation is already deterministic by design (Principle #6). The property is universally true for every precept; declaring it is vacuous. | Universally true — the property holds for every precept by design |
| `bounded` | Precept | Maximum number of transitions (finite progress guarantee). Not compile-time provable in the general case — whether a lifecycle terminates depends on runtime event sequencing and data values, not declared structure. Cycles make termination undecidable. For acyclic graphs, termination is trivially guaranteed by `linear` (#29). No intermediate case exists where `bounded` adds value that `linear` doesn't already provide. | Undecidable in cyclic graphs; trivially true (adds nothing) for linear precepts |
| `preview` | Precept | Designed for preview rendering (tooling hint). Every precept should be previewable — this is a default capability, not a per-definition opt-in. Tooling configuration belongs in build settings or pragmas, not in lifecycle definition semantics. | Not a modifier — tooling configuration |

These exclusions are limited to four legitimate grounds: requires runtime state that doesn't exist in Precept, is computationally undecidable, is a true semantic synonym of another candidate already in the inventory, or would change Precept's fundamental model. Value judgments about practicality or demand do not justify exclusion — those assessments appear in the per-candidate detail entries for Shane's decision-making.

---

## Section 6: Modifier Dependencies

Dependencies between modifiers are stated as facts. Some modifiers require others to function; some are enhanced by others but don't require them.

| Modifier | Requires | Enhanced by |
|----------|----------|-------------|
| `terminal` | — | — |
| `guarded` (state) | — | — |
| `required` / `milestone` | — (but cleaner with `terminal`) | `terminal` |
| `transient` | — | — |
| `absorbing` | — | `terminal` (partially overlapping) |
| `convergent` | — | — |
| `forward` | — | `terminal` (trivially forward), `milestone` (exactly-once combination) |
| `gate` | — | `forward` (subsumed), `terminal` (trivially satisfied), `milestone` (mandatory phase boundary) |
| `ordered before <State>` | — | `forward` (subsumed when `forward` applies) |
| `oneshot` | — (equivalent to `milestone` + `forward`) | `terminal` (via both constituent modifiers) |
| `bottleneck` | — (but cleaner with `terminal`) | `terminal`, `milestone` (bottleneck is weaker) |
| `depth <N>` | — (but practical only with `linear`) | `linear` (well-defined depth in DAGs) |
| `entry` | `initial` (always present in stateful precepts) | — |
| `advancing` | — | — |
| `settling` | — | — |
| `isolated` | — | — |
| `completing` | `terminal` (target check references terminal status) | — |
| `guarded` (event) | — | — |
| `total` (event) | — | — |
| `universal` | — | `terminal` (excludes terminal states from check) |
| `irreversible` | — | `terminal` (trivially satisfied for terminal targets) |
| `symmetric` | — | — |
| `writeonce` | — | — |
| `sealed after` | — (but clearer with `terminal`) | `terminal` |
| `immutable` | `default` (otherwise field can never have a value) | — |
| `monotonic` | — | — |
| `identity` | — (subsumes `writeonce`) | — |
| `derived` | Computed fields mechanism (#17) | — |
| `linear` (precept) | — | `terminal` (complementary bookends — acyclic graph + explicit endpoints) |
| `strict` (precept) | — | — |
| `governed` (precept) | — | `guarded` (state) (subsumed for non-initial states) |
| `exhaustive` (precept) | — | `terminal` (excludes terminal states from dead-end check) |
| `single-entry` (precept) | `initial` (always present in stateful precepts) | — |
| `single-exit` (precept) | — | `terminal` (cleaner count when terminal states are explicit) |
| `sequential` (precept) | — (implies `linear`) | — |
| `stateless` (precept) | — (contradictory with all state-graph modifiers) | — |
| `total` (precept) | — | `terminal` (excludes terminal states from check) |
| `success` (state) | — | `terminal` |
| `error` (state) | — | `terminal` |
| `warning` (state) | — | — |
| `resting` | — | — |
| `decision` | — | — |
| `cyclic` | — | `forward` (contradictory pair — cannot both apply to same state), `gate` (contradictory — gate implies forward) |
| `success` (event) | — | structural event modifiers |
| `error` (event) | — | structural event modifiers |
| `warning` (event) | — | — |
| `sensitive` | — | — |
| `audit` | — | — |
| `auditable` (precept) | `audit` semantics defined | — |
| `warning` (rule severity) | Philosophy evolution (Section 2) | — |

---

## Key References

- [structural-lifecycle-modifiers.md](./structural-lifecycle-modifiers.md) — Full candidate inventory, comparable system survey, tier analysis
- [verdict-semantic-reframing.md](./verdict-semantic-reframing.md) — Semantic model analysis, soft diagnostics, mechanism unification
- [verdict-modifier-runtime-enforceability.md](./verdict-modifier-runtime-enforceability.md) — George's Model D, engine partitioning, philosophy reframing
- [verdict-modifier-ux-perspective.md](./verdict-modifier-ux-perspective.md) — Elaine's UX analysis
- [verdict-modifier-roadmap-positioning.md](./verdict-modifier-roadmap-positioning.md) — Steinbrenner's milestone analysis
- [verdict-modifiers.md](./verdict-modifiers.md) — 7-system external survey
- [verdict-modifier-design-options.md](./verdict-modifier-design-options.md) — Original 3-tier design options
- [PreceptLanguageDesign.md](../../docs/PreceptLanguageDesign.md) — Language design principles
- [philosophy.md](../../docs/philosophy.md) — Core guarantees
- [DiagnosticCatalog.cs](../../src/Precept/Dsl/DiagnosticCatalog.cs) — Existing diagnostics C1–C59
- [RuntimeApiDesign.md](../../docs/RuntimeApiDesign.md) — TransitionOutcome enum, API contract
