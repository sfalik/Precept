# Reasoning Ingress — LLM-Driven Decisions Inside a Deterministic Precept

> ## Status
>
> **Exploratory brainstorm — not designed, not implemented, has not gone through `/design` or `/plan`.**
>
> This document captures the output of a single extended brainstorm session. It is **not** a
> locked design, **not** a spec, and **not** a canonical doc. No `/design`, `/research`, or
> `/plan` ceremony has been triggered on any of this. Per Precept's **Pre-Design Owner
> Consultation** gate, nothing here has been surfaced to the owner as a settled proposal — this
> file exists only so the brainstorm survives across sessions and can seed a real design pass
> *if and when* the owner decides to open one.
>
> Everything below is provisional. Where the brainstorm reached a "settled" shape, that means
> "settled within the conversation," not "adopted by the project." Read § 5 for the honest
> real-vs-proposed accounting before treating any construct here as real Precept.

| Property | Value |
|---|---|
| Doc purpose | Forward-looking vision / brainstorm capture |
| Status | Exploratory brainstorm — pre-design |
| Grounded against | `docs/language/precept-grammar.md`, `docs/runtime/runtime-api.md` (read 2026-07-21) |
| Owner consultation | **Not yet held.** This is not a proposal to start work. |
| Net-new surface | `~>` token, `[...]` write-refs, reasoning-provider runtime — none exist today |

---

## Contents

- [§1. Original vision](#1-original-vision)
- [§2. The core problem](#2-the-core-problem)
- [§3. Walked-back ideas (compact)](#3-walked-back-ideas-compact)
- [§4. The settled design](#4-the-settled-design)
  - [4a. `~>` — the async sibling of `->`](#4a---the-async-sibling-of--)
  - [4b. Terminal placement — where `~>` can appear](#4b-terminal-placement--where--can-appear)
  - [4c. The prompt syntax: `{read}` / `[write]` interpolation](#4c-the-prompt-syntax-read--write-interpolation)
  - [4d. Two-call runtime mapping (Update, then Fire)](#4d-two-call-runtime-mapping-update-then-fire)
  - [4e. Structural rules summary](#4e-structural-rules-summary)
  - [4f. How this maps to real LLM provider mechanics](#4f-how-this-maps-to-real-llm-provider-mechanics)
  - [4g. The event-arg-direction tension (unresolved)](#4g-the-event-arg-direction-tension-unresolved)
- [§5. Real/grounded vs. proposed/net-new](#5-realgrounded-vs-proposednet-new)
- [§6. Open questions / explicitly deferred](#6-open-questions--explicitly-deferred)
- [Appendix: The content-moderation worked example](#appendix-the-content-moderation-worked-example)

---

## §1. Original vision

The starting point was infrastructural, not linguistic:

- **Server + persistence.** Precepts don't just compile — they run as long-lived governed
  entities backed by durable storage, restorable across restarts (the runtime already models
  this: `Precept.Restore(state, fields)` hydrates a `Version` from persisted data — see
  `runtime-api.md` § Restore).
- **Scale on Kubernetes via an actor framework.** Orleans or Dapr virtual actors, one actor
  per live entity, giving location-transparent addressing, single-threaded-per-entity
  consistency, and horizontal scale.
- **Message passing between precepts + saga-like workflows.** Extend Precept so one governed
  entity can trigger operations on another, and multi-entity business processes can be
  expressed as coordinated sagas rather than orchestrated by external glue code.
- **MCP-callable server.** Expose the running server through MCP so an agent can *author* a
  business process, *put it into action* (instantiate + drive live entities), *monitor* it, and
  *evolve* the definition — a full author→operate→observe→evolve loop from inside an agent.

The brainstorm then narrowed onto the single hardest sub-problem inside this vision: **how an
LLM makes a decision inside a Precept without breaking what makes a Precept a Precept.** Almost
everything settled in § 4 turns out to be the in-language substrate for the async /
message-passing / actor half of this vision — declared at the trigger point rather than bolted
on outside.

---

## §2. The core problem

Precept's core guarantee (`docs/philosophy.md`): **same definition + same data = same outcome,
nothing hidden.** Prevention over detection; governance over validation; determinism; full
inspectability and replay.

An LLM is, by construction, the opposite: non-deterministic, opaque, and un-replayable if you
re-call it. So the question is not "can we call an LLM from Precept" (trivially yes) but:

> How does an LLM reason and decide *within* Precept's deterministic state-machine model
> **without** violating determinism, replayability, or governance?

The whole of § 4 is an answer to that question. The short version: **the LLM never gets
mutation authority and its output is captured as recorded data.** The model is treated as a
*principal at the ingress boundary* — exactly like a human submitting a form. A human is also
non-deterministic; Precept already governs human input by validating it and recording the
resulting operation, never by trusting the human to mutate state directly. An LLM is governed
the same way. Determinism is preserved not by making the model deterministic, but by making its
influence **enter through a recorded, governed operation** and **replay from that record rather
than re-calling the model.**

---

## §3. Walked-back ideas (compact)

Constructs proposed and then rejected or superseded during the session, one line each. This is
context for *why* the final shape is as small as it is — not the main content.

- **Separate `prompt` / `reasoner` constructs** — superseded; the trigger arrow (`~>`) plus a
  bare prompt string carries everything, no dedicated construct needed.
- **`including` / `excluding` clauses** (to widen/narrow context) — dropped; `{read}`
  interpolation makes context explicit and local.
- **`version N` selector** (pin a reasoning definition version) — dropped as premature.
- **Capture slots** (`... into Rationale`, `... into Confidence`, `... into Provenance`) —
  dropped; provenance/confidence belong in the recorded operation's metadata, not author syntax.
- **A name for the `decide`** (labelling the decision) — dropped; the arrow site *is* the
  identity.
- **`among admissible [except X]`** (author-curated candidate set) — dropped; the admissible
  event set (`AvailableEvents`) is already structural and authoritative; authors shouldn't
  hand-maintain it.
- **`reason` as a `set`-RHS expression** — rejected once for embedding non-determinism inside
  the deterministic `->` chain; briefly reconsidered *only* under a separate arrow, which
  became `~>`.
- **`to <State> decide` as an auto-invoke spelling** — superseded by terminal `~>` placement.
- **Framing `->` vs `~>` as pre-commit / post-commit timing** — superseded; the real axis is
  **synchronous-deterministic vs. asynchronous-recorded** (see § 4a), not *when* it runs.
- **`infer` / `invoke` as two distinct verbs with an `among` marker** for event-selection —
  superseded; the verb dissolves (§ 4c).
- **`prompt` as a verb with a bare list / `any` for candidate sets** — superseded by `[...]`
  write-refs resolving candidates from the symbol table.
- **One-batched-LLM-call-does-everything** — superseded by the two-call model (§ 4d), which is
  *more correct*, not just tidier.

---

## §4. The settled design

> "Settled" = settled *within the brainstorm*. See the status banner.

### 4a. `~>` — the async sibling of `->`

Precept already has one arrow, `->`, and it is a lexed disambiguation token that routes action
chains and terminal outcomes (`grammar` § 4 — `->` disambiguates `StateAction`, `EventRow`, and
the `TransitionRow` action-chain/outcome tail). The proposal adds a **second arrow, `~>`**, as
its asynchronous sibling.

| | `->` (exists today) | `~>` (proposed) |
|---|---|---|
| Timing | Synchronous — runs inline in the triggering operation's pipeline | Asynchronous — the trigger *schedules* an out-of-band task; the operation returns immediately |
| Determinism | Deterministic, pure, atomic | Non-deterministic (LLM), out-of-band |
| Recompute vs. record | Recomputable from definition+data; never needs recording | Must be **captured as data** — replay reads the record, never re-calls the model |
| Effect | Mutations + transition, committed in one pipeline pass | On completion, performs **recorded** Update / Fire operations producing new `Version`s |

The justification for a *syntactically distinct* arrow is the **determinism/recordedness axis**,
not pre/post-commit timing (that framing was walked back — § 3). A `->` outcome is pure and
recomputable, so it never needs to be persisted as a distinct fact; you can always re-derive it.
A `~>` outcome is a non-deterministic external decision, so it **must** be persisted as data and
replayed from that record. Two fundamentally different substances → two arrows.

A `~>` trigger therefore creates an observable, durable, crash-resumable **"reasoning-pending"
intermediate `Version` state**: the entity has committed its synchronous outcome and is now
durably waiting for an out-of-band decision. For the deterministic `->` case such an
intermediate state would be a bug (nothing should be observably half-done); for the async `~>`
case it is *exactly correct* — it is the durable checkpoint the actor/message-passing substrate
needs. **This is the async substrate from § 1's original vision, declared in-language at the
trigger point** rather than bolted on as external orchestration.

### 4b. Terminal placement — where `~>` can appear

`~>` is a chain of **one-action-per-arrow** statements (mirroring `->`'s discipline — see
`grammar` § 5, `ActionChain` = a sequence of `-> action` steps) that comes **after** the
complete synchronous `->` outcome. It is **never interleaved** with `->` statements — the whole
deterministic outcome resolves first, then the async reasoning tail begins.

It is legal at all three real `->` outcome sites (using the grammar doc's family shapes):

```precept
to   <State>  [when G]  [-> ActionChain]                              ~> ReasoningChain
from <State> on <Event> [when G]  -> ActionChain -> transition <T>    ~> ReasoningChain
on   <Event>  [when G]  -> ActionChain                               ~> ReasoningChain
```

**Legal only after a *success* outcome** — `-> transition <T>` or `-> no transition`.
**ILLEGAL after `-> reject "…"`**: a reject commits nothing, so there is no post-commit fact to
reason *from*. This maps cleanly onto the runtime's real row-dispatch split: `runtime-api.md`
(§ Fire, "Row dispatch model") states transition rows come in two shapes — **mutation rows**
(action chain + success outcome) and **reject rows** (reject clause only) — and "a single row
cannot mix mutations with rejection." A `~>` tail attaches to the mutation-row shape only; the
grammar already forbids action-plus-reject hybrids (`grammar` § — `TransitionRowReject` has no
`ActionChain` slot), and `~>` inherits that exclusion for the same structural reason.

Two cases specifically require **row/event-level** placement — a state-entry hook (`to <State>
-> …`) structurally cannot express them:

1. **A field-mutating event that does *not* change state.** New evidence arrives, the entity
   stays in the same state, but it should be *re-reasoned*. The runtime already has an outcome
   for exactly this: `EventOutcome.Applied` — a mutation that commits without a transition
   (`runtime-api.md` § Fire). A state-entry hook never fires here (no state was entered), so the
   only place to hang the reasoning is the event row itself.
2. **Stateless precepts.** `runtime-api.md` is explicit: stateless precepts have `State = null`
   and "have events, hooks, rules, and fields — but no states" (§ Design Decisions,
   "`string? State` for stateless precepts"). With no states there are no state-entry hooks at
   all, so an event-/row-terminal `~>` is their **only** possible reasoning trigger. This keeps
   reasoning available to first-class stateless precepts, consistent with the philosophy that
   states are the mechanism, not the point.

**Explicitly OUT of scope: reactive / threshold triggers** ("re-reason whenever `RiskScore`
crosses 0.8"). Precept's model is **operation-triggered**, not continuous field-watching. A
threshold watcher would fire reasoning outside any recorded operation, breaking "every change
flows through a recorded operation." This is a *rejection*, not a deferral (see § 6).

### 4c. The prompt syntax: `{read}` / `[write]` interpolation

This is the final settled prompt form. It **supersedes** every verb-based iteration tried along
the way (`infer` / `invoke` / `prompt`-as-verb / `among` / `by` / `using` / `given` / `from`
markers — all § 3 walk-backs). The insight: the `~>` arrow *already* signals "this is
reasoning," and the prose *itself* — written in ordinary English — is what the model reads.
Precept only structurally cares about the **bracketed holes** in that prose.

Two interpolation forms, distinguished by bracket shape:

**`{Identifier}` — READ interpolation.** This is **already real Precept syntax**, reused from
the `because` / rule-message mechanism. It is live today in samples, e.g.
`academic-course-registration.precept:59`:

```precept
rule RegistrationCloses > RegistrationOpens
  when RegistrationOpens is set and RegistrationCloses is set
  because "Registration close date {RegistrationCloses} must be after open date {RegistrationOpens}"
```

`{RegistrationCloses}` injects a live field value into the message string. Reused for reasoning,
`{Field}` injects a live value into the prompt as **context**. **Zero new surface** on the read
side — it is the existing interpolation mechanism pointed at a prompt string.

**`[Identifier]` — WRITE reference (NEW, collision-free).** Square brackets are **EBNF
meta-notation** in `precept-grammar.md` — `[AnchorState[, ...] | any]`, `[when Guard]`,
`[because "..."]` — they denote *optionality in the grammar description* and are **never real
surface syntax**. That makes `[...]` free to claim for a new purpose. A `[...]` ref resolves via
the precept's **symbol table — by *what* the identifier is, not by shape-guessing**:

| Bracketed ref | Resolves to | Contributes to |
|---|---|---|
| `[FieldName]` | a field | a recorded **Update** (field write) |
| `[EventName]` (2+ present) | an event candidate | the choice set for a recorded **Fire** |
| `[EventName.ArgName]` | a dotted event-arg ref | an arg value, bound **only if** that event is chosen |

A **single** lone `[EventName]` with no sibling event candidates is degenerate and disallowed:
one candidate is no choice. If exactly one bracketed name resolves to an event and nothing else
does, it is treated as a field reference instead (or is a resolution error if it's neither).

With reads and writes both handled by interpolation, the `~>` chain collapses to just a prompt
string plus an optional fallback:

```precept
~> "<prompt with {reads} and [writes]>" [otherwise <Event>]
```

The "verb" keywords dissolve entirely. Example prose: *"Set `[Category]`… then invoke
`[KeepContent]` or `[RemoveContent]`…"* — the words "Set" and "invoke" are **English the model
reads**, not Precept keywords. Precept parses the brackets, not the sentence.

**Structural rules Precept enforces regardless of how the prose is worded:**

- **Effect order is FIXED and never inferred from sentence word-order:** all field Updates
  apply first, then the single Fire (§ 4d explains why). If the prose says "fire X after setting
  Y" or "set Y after firing X," it makes no difference — the runtime order is fixed.
- **Every event-ref must be a member of the current state's admissible event set**
  (`Version.AvailableEvents`) or it's a **compile error**. `AvailableEvents` is a structural,
  precomputed, per-`Version` read (`runtime-api.md` § Constraint Exposure / Structural tier —
  "Zero — precomputed").
- **`{...}` reads get `sensitive` / taint checking** — data leaving the process boundary to a
  third-party model — exactly as `because`-clause interpolation is checked today.
- **`[...]` writes are validated by their declared types** plus the normal event `ensure` /
  constraint sweep on commit. **The model never gets raw mutation authority** — it returns
  typed decision *data*; only Precept applies effects, through governed `Update` / `Fire`.

Finally, Precept can derive a compile-time **effect signature** from the `[...]` refs — e.g.
*"sets {Category, Severity}; fires one of {KeepContent, RemoveContent, RestrictContent}; may set
RestrictContent.Reason; falls back to EscalateToHumanReviewer."* Tooling / hover surfaces this to
a reviewer, so **legibility doesn't depend on trusting the prose** — the structural contract is
extractable and reviewable independent of the English.

### 4d. Two-call runtime mapping (Update, then Fire)

The runtime has exactly two mutation operations, `Fire` and `Update`, and they are **mutually
exclusive per commit** — `runtime-api.md` (§ InspectFire/InspectUpdate rationale) states plainly:
"Fire and Update are mutually exclusive commit operations — field patches and event args produce
a new `Version` through fundamentally different pipelines." (`Update` = access-mode check + type
validation + constraint sweep; `Fire` = row matching + transition.)

So a `~>` chain that both sets fields *and* fires an event becomes **two sequential recorded
commits:**

- **Call 1 — Update:** *all* field-refs batched into **one** LLM call and **one** `Update`
  commit (no N-calls-per-field cost).
- **Call 2 — Fire:** the event choice plus any conditional args, **one** LLM call and **one**
  `Fire` commit.

Rule: **number of LLM calls = number of runtime mutation ops present in the chain.** Field-only
`~>` → one call. Event-only `~>` → one call. Both → two.

**Why two calls is *more correct*, not just tidier.** `AvailableEvents` is structural and
**recomputed per-`Version`** (structural tier, zero-cost, derived from graph analysis). Setting a
field can change which events are subsequently admissible — e.g. a guard keyed on `Severity` may
gate `RestrictContent` out above some threshold.

- **Single-call model (rejected):** the model picks fields *and* event simultaneously against
  the **pre-mutation** admissible set. If the chosen fields would gate out the chosen event, the
  Update commits, admissibility recomputes, and the Fire then hits a **now-inadmissible** event
  → rejected. Broken by construction in exactly the interesting cases.
- **Two-call model:** Update commits first → admissibility recomputes → Fire chooses against the
  **fresh post-Update admissible set.** Correct by construction.

This mirrors how a human actually operates the entity: **fill the form fields, *then* see which
buttons are now live, *then* click.** The runtime already enforces this ordering for humans (a
Fire is dispatched against the current `Version`'s live `AvailableEvents`); the two-call model
just holds the model to the same discipline.

Other benefits:

- **1:1 provenance** — each commit maps to exactly one model response, not a shared blob.
- **Clean async checkpointing / crash-resumability** — if the provider is down on the Fire call,
  the Update is *already durably committed*; resume at Fire, not from scratch. (This is the same
  durable "reasoning-pending" checkpoint from § 4a, now with sub-steps.)
- **Simpler per-call JSON schemas** — no conditional-arg logic spanning both fields and events in
  a single blob (see § 4f / Appendix for the concrete schemas).

### 4e. Structural rules summary

Consolidated, independent of prose wording:

1. **Mutations precede the move.** Field writes (Update) come before the event fire (Fire) —
   mirrors `->`'s existing mutations-before-transition discipline (`grammar`: `ActionChain`
   precedes `Outcome`).
2. **At most one Fire per `~>` chain**, and if present it is **last**.
3. **No intra-batch forward references.** A statement can't interpolate a value produced *earlier
   in the same batched call* — the batch produces its values jointly, not sequentially; only
   pre-batch state is interpolable. Not a practical limitation: joint reasoning inside one call
   gives consistency *without* needing textual chaining.
4. **`otherwise <Event>` is the fallback/escalation clause** — for low-confidence output, a
   timeout, or a fault on *either* the Update or the Fire call. It routes to a normal event,
   exactly as a bad human submission would (e.g. `otherwise EscalateToHumanReviewer`).
5. **`sensitive` / taint checking applies to `{read}` interpolations** (context leaving the
   process boundary to a third-party provider) exactly as for existing `because` interpolation.
6. **Writes are never raw mutation authority.** The model returns typed, schema-constrained
   decision *data*; **Precept is the one and only actor that calls governed `Update` / `Fire`.**
   The model is a principal at the ingress boundary, like a human's form submission — never
   handed a bypass around governance.

### 4f. How this maps to real LLM provider mechanics

Grounded in real OpenAI-style tool-calling / structured-output behavior, not speculation.

**Real mechanics.** A request carries a `tools` array, each tool a JSON-Schema `parameters`
object. The model returns tool-call **intents** — `{name, arguments}` where `arguments` is a
JSON *string* — it **does not execute anything.** The *caller's own code* executes the function
and feeds the result back as a `role:"tool"` message, looping until the model stops emitting tool
calls. That **call → execute → feed-back → call-again loop is entirely the caller's code**; the
provider defines the protocol shapes, not the loop. Separately, there is a loop-free **Structured
Outputs** mode (`response_format: {type:"json_schema", …}` with `strict:true`) that forces the
*final message* to conform to a schema via constrained decoding — one call, one object, no tool
loop.

**Our write path needs NO agentic loop.** Each of the two calls (Update, Fire) is a **single
forced structured response** — either a forced single-tool `tool_choice` call, or pure
`response_format:json_schema` with no tools at all. Because the effect surface is **fully
compile-time-enumerated from the `[...]` refs**, there is nothing to iteratively discover, so
there is no loop to run. The model returns one object conforming to a schema Precept generated
from the effect signature.

**A thin harness IS needed only if reads/context tools are exposed during reasoning** — e.g.
letting the model consult an external classifier, a fraud-score API, or some OpenAPI endpoint as
**read-only** context before deciding. *That* is a genuine multi-turn tool-calling loop, and it
must be **built and OWNED by Precept — never delegated to a general agent framework** (LangChain,
hosted Assistants-style orchestrators, etc.), for three concrete reasons:

1. **Governance.** A black-box loop could be given — or drift into — *mutation*-tool access,
   breaching "all writes go through governed `Update` / `Fire`." In Precept's own harness the
   model can **only** emit *read*-tool intents, never write-tool intents.
2. **Replay.** "Replay reads the record, never re-calls the model" requires capturing the **full
   transcript** — every request, tool intent, tool result, and final decision — as provenance. An
   opaque third-party orchestrator we can't fully observe breaks deterministic replay.
3. **Budget / safety.** Max-iterations, timeouts, and cost ceilings must be enforced by Precept,
   not left to a framework's defaults.

Read tools (including an external OpenAPI endpoint as a *data source*) are welcome inside that
thin harness; **writes are always the returned decision that Precept itself executes afterward.**
Reads-as-tools is fully compatible with the two-call Update/Fire write model in § 4d.

**Provider-agnostic interface.** All of this sits behind a **reasoning-provider** abstraction
with per-endpoint adapters (OpenAI/Azure function-calling + structured outputs, Anthropic
tool-use, OpenAI-API-compatible local models). The `~>` runtime targets one interface, not a
specific vendor dialect.

### 4g. The event-arg-direction tension (unresolved)

Two mechanisms for directing *how an event's argument should be filled* were both reached in the
session and **were not reconciled.** Flagging honestly rather than papering over it.

**Mechanism A — declaration-level `infer "<hint>"` on the arg.** Put intrinsic per-arg guidance
directly on the event's argument *declaration*:

```precept
event RemoveContent(Reason as string notempty maxlength 280
                    infer "State the specific policy violated and the sentence(s) that violate it.")
```

Any reasoning site that chooses `RemoveContent` reuses the same guidance — **DRY, reusable across
every decision site**, because the arg's *meaning* is intrinsic to the event the same way its
*type* is. (The arg already carries `as string notempty maxlength 280` on its declaration; the
hint would ride alongside.)

**Mechanism B — inline `[EventName.Arg]` at the decision site.** Reached later in the same
session, the `[...]` mechanism (§ 4c) *also* solves arg-direction — situationally, right in the
prose:

```precept
~> "…if restricting, set [RestrictContent.Reason] accordingly."
```

This gives per-decision-site direction without any declaration-level hint at all.

**These two were NOT reconciled.** The open question:

> Does declaration-level `infer "<hint>"` still have a place for **truly intrinsic,
> always-true-regardless-of-site** guidance (the way a field's *type* lives on its declaration,
> not at each use), while inline `[EventName.Arg]` handles **situational per-decision-site**
> direction? Or does inline `[...]` **fully subsume** the declaration hint, making it redundant?

Not resolved in the brainstorm. Listed as open in § 6.

---

## §5. Real/grounded vs. proposed/net-new

Honest accounting, in the spirit the whole session held. Citations verified against the docs
this session.

**Real / grounded — exists in Precept today:**

- **The `->` token** as an existing lexed disambiguation token routing action chains and terminal
  outcomes — `precept-grammar.md` § 4 (`->` disambiguates `StateAction` / `EventRow` /
  `TransitionRow`).
- **`StateTarget`'s `any` / comma-list shape** — `grammar` § 5 (`StateTarget` = "State name(s)
  or `any` wildcard," `from Draft, Pending`). Cited as precedent for author-facing choice sets;
  the design ultimately used symbol-table resolution over admissible events instead of a hand
  list, but the shape precedent is real.
- **The unlabeled-string-operand `reject "<string>"` pattern** — `grammar` § 4 / `RejectClause`
  (`-> reject "reason"`). Precedent that a construct can take a bare string literal operand
  (the `~>` prompt string leans on this precedent).
- **`{Identifier}` read-interpolation** — live in `because` / rule messages, e.g.
  `academic-course-registration.precept:59`, `medical-prior-auth.precept`,
  `tax-rate-configuration.precept`. The read side of § 4c is *literally the existing mechanism*.
- **The four real runtime ops — `Create`, `Restore`, `Fire`, `Update`** — `runtime-api.md` § API
  Surface ("four operations"). `Fire` and `Update` are the two *mutation* ops and are **mutually
  exclusive per commit** (§ InspectFire/InspectUpdate rationale). `Create` fires the initial
  event atomically; `Restore` is trusted hydration.
- **`AvailableEvents` as structural / per-`Version`** — `runtime-api.md` § Constraint Exposure,
  Structural tier: "Zero — precomputed," "events with rows in current state."
- **`InspectFire` / `InspectUpdate` as the discard-instead-of-commit twins** — `runtime-api.md`
  (Inspection tier: "Same path as commit, working copy discarded"). The model relies on this for
  the "recompute admissibility, then decide" ordering being observable pre-commit.
- **Stateless precepts have `State = null` and no state-entry hooks** — `runtime-api.md` § Design
  Decisions ("`string? State` for stateless precepts … `State` is `null`").
- **`EventOutcome.Applied`** — a mutation committing without a transition (`runtime-api.md`
  § Fire) — the runtime fact behind § 4b case (1).
- **Row-dispatch split (mutation rows vs. reject rows), single row can't mix** — `runtime-api.md`
  § Fire, "Row dispatch model." The runtime fact behind § 4b's "illegal after reject."
- **`[...]` as EBNF meta-notation, never surface syntax** — `precept-grammar.md` throughout
  (`[when Guard]`, `[because "..."]`, `[AnchorState[, ...] | any]`). This is what makes `[...]`
  *free to claim* for write-refs.

**Proposed / net-new — does NOT exist in Precept today:**

- **The `~>` token itself** — no second arrow exists.
- **The `[...]` write-reference mechanism** and its symbol-table-driven resolution.
- **The reasoning-provider abstraction** and per-endpoint adapters (§ 4f).
- **The entire async / scheduling / checkpointing runtime** — durable "reasoning-pending"
  `Version` state, out-of-band task execution, crash-resume.
- **The compiled prompt-template / JSON-schema / effect-plan artifacts** derived from a `~>`
  chain.
- **The governed read-tool harness** (§ 4f) with its transcript capture and budget enforcement.
- **The declaration-level `infer "<hint>"` arg modifier** (§ 4g, Mechanism A) — proposed, then
  left unreconciled.

---

## §6. Open questions / explicitly deferred

Only genuinely unresolved items. (Note: event-/row-terminal `~>` for field-mutating-no-transition
and stateless precepts was **adopted** in § 4b — it is *not* open.)

- **Reactive / threshold triggers are RULED OUT, not deferred.** Continuous field-watching
  ("re-reason whenever `RiskScore` crosses 0.8") is explicitly *rejected* because it fires
  reasoning outside any recorded operation, breaking "every change flows through a recorded
  operation." This is a decision, not a postponement.
- **Does the entry-hook (`to <State> -> actions`) genuinely run pre-commit in the real
  implementation?** The brainstorm argued yes from *inference* — governance / constraint-sweep
  ordering, the single-working-copy / zero-copy-promotion model in `runtime-api.md` — but this
  was **never directly confirmed by a citable line.** `runtime-api.md` § Fire even flags entry-
  action ordering as unsettled ("whether entry actions fire at construction and on a
  self-transition … See § 3A.4 / Open Questions"). **Needs a source-code check at implementation
  time; not resolvable from docs alone.**
- **The event-arg-direction tension (§ 4g)** — declaration-level `infer "<hint>"` vs. inline
  `[EventName.Arg]`. Not reconciled.
- **No complete, gap-free worked `.precept` sample** exists anywhere reflecting this whole design
  end-to-end. Worked examples exist only as brainstorm fragments (see the Appendix). A complete
  worked sample is the natural next artifact **if** this moves toward `/design`.

---

## Appendix: The content-moderation worked example

Illustrative, not final — the running domain used throughout the brainstorm. (No such sample
exists in `samples/` today; this is a fragment, per § 6.)

```precept
from Triaging on CompleteIntake
    -> set IntakeCompletedAt = now()
    -> transition Reported
    ~> "Examine the reported {ContentType}: {ContentText}. Judge the content, not its author.
        Set [Category] and [Severity]. Then invoke [KeepContent], [RemoveContent], or
        [RestrictContent]; if restricting, set [RestrictContent.Reason] accordingly."
       otherwise EscalateToHumanReviewer
```

Reading this:

- The synchronous `->` outcome fully resolves first: set `IntakeCompletedAt`, transition to
  `Reported`. This commits normally — a plain `TransitionRow`. **Then** the async `~>` tail is
  scheduled; the `CompleteIntake` operation returns immediately with the entity in a durable
  "reasoning-pending" state (§ 4a).
- `{ContentType}` / `{ContentText}` are **read** interpolations (existing mechanism) — live
  field values injected as context. They are `sensitive`-checked as data crossing the boundary.
- `[Category]`, `[Severity]` resolve (symbol table) to **fields** → one batched **Update**.
- `[KeepContent]`, `[RemoveContent]`, `[RestrictContent]` resolve to **events** (2+ present →
  a genuine choice) → the candidate set for one **Fire**.
- `[RestrictContent.Reason]` is a dotted event-arg ref → bound **only if** `RestrictContent` is
  chosen.
- `otherwise EscalateToHumanReviewer` is the fallback for low confidence / timeout / fault on
  either call.

**Compile-time effect signature** (surfaced to reviewers via tooling/hover, § 4c):

> sets {Category, Severity}; fires one of {KeepContent, RemoveContent, RestrictContent}; may set
> RestrictContent.Reason; falls back to EscalateToHumanReviewer.

**Runtime: two sequential recorded commits (§ 4d).**

**Call 1 — Update** (all field-refs, one call). Structured-output schema (illustrative):

```json
{
  "type": "object",
  "additionalProperties": false,
  "required": ["Category", "Severity"],
  "properties": {
    "Category": {
      "type": "string",
      "enum": ["Spam", "Harassment", "Misinformation", "GraphicContent", "None"]
    },
    "Severity": {
      "type": "number",
      "minimum": 0,
      "maximum": 1
    }
  }
}
```

Precept applies this via governed `Update`. The constraint sweep runs; `AvailableEvents` is then
**recomputed** on the resulting `Version`. Suppose a guard gates `RestrictContent` out when
`Severity < 0.3`.

**Call 2 — Fire** (event choice + conditional arg, one call). The `chosen_event` enum is built
from admissibility **recomputed after Call 1 commits** — so if `Severity` came back `0.1`,
`RestrictContent` simply isn't in the enum, and the model cannot pick an inadmissible event:

```json
{
  "type": "object",
  "additionalProperties": false,
  "required": ["chosen_event"],
  "properties": {
    "chosen_event": {
      "type": "string",
      "enum": ["KeepContent", "RemoveContent", "RestrictContent"]
    },
    "Reason": {
      "type": "string",
      "maxLength": 280,
      "description": "Required only when chosen_event = RestrictContent."
    }
  },
  "allOf": [
    {
      "if": { "properties": { "chosen_event": { "const": "RestrictContent" } } },
      "then": { "required": ["Reason"] }
    }
  ]
}
```

Precept applies the choice via governed `Fire`, binding `Reason` to `RestrictContent.Reason`
only when `RestrictContent` is chosen. If the provider faults on either call, or confidence is
low, or a timeout trips, the entity routes to `EscalateToHumanReviewer` — exactly as a bad human
submission would. The Update, if already committed, stays durably committed; resume happens at
the Fire step (§ 4d).

---

*End of brainstorm capture. Nothing here is adopted. If this moves forward, it enters `/design`
with the § 6 open questions and § 5 net-new surface as its starting agenda.*
