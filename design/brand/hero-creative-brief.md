# Hero Creative Brief — Phase 3
### Precept · Brand/DevRel · J. Peterman
### Purpose: Guide writers developing 8–12 hero sample candidates

---

## 1. Strategic Context

The hero sample is the most consequential brand asset Precept has. It lives at the top of the README, it is the first — and sometimes only — thing a developer examines before deciding whether to read further. Two audiences matter equally: a developer skimming GitHub at 11pm, and an AI agent interpreting tool documentation to decide whether Precept is appropriate for a given task. Both need the same thing: a domain they recognize immediately, a moment of structural drama they didn't expect, and `because` messages they might quote to a colleague. The sample must prove Precept is a new kind of thing — not a nicer validation library, not a state machine wrapper — by showing invalid states made structurally impossible in a way that makes the reader feel it.

---

## 2. Domain Direction

Six to eight recommended domains. Fictional and fun preferred — real-world domains that fit the line budget tend toward the trivial. Fictional domains can be conceptually crisp without the burden of representing a full business system.

---

### 2.1 Duel at Dawn

**Premise:** A dueling protocol. You have been called out. The sequence is fixed by the code of honor: challenge issued, seconds appointed, position taken, pistol drawn, fire. You cannot skip to fire. You cannot fire twice. Once fallen — there is no recovery event.

**Emotional hook:** Instantly legible to any English speaker with a passing familiarity with history or Hamilton. The absurd formality of "cannot fire before the count" maps perfectly to "transition not permitted" — readers laugh, then nod.

**Naturally scores well on:**
- Emotional Hook (20): the drama is real, the stakes are ridiculous, the combination is irresistible
- Voice/Wit (25): "The count was three" is funnier than any enterprise error message
- Domain Legibility (20): five words, fully understood

**Risks:** The terminal state must feel earned, not bleak. "Fallen" should read as inevitability, not violence. If `because` messages reach for gallows humor, they land; if they miss, they alienate.

---

### 2.2 Spell Casting (RPG)

**Premise:** A spell with mana cost, cast time, and forbidden targets. The spell cannot be cast while cooling down. A higher-level version can be cast only while empowered. Once broken, the incantation cannot be re-attempted without first resetting.

**Emotional hook:** Every developer who has played an RPG has been rejected by "insufficient mana" or "spell not ready." This converts that memory into structural truth. The rule on mana has a `because` that can be genuinely funny.

**Naturally scores well on:**
- Voice/Wit (25): "The arcane laws do not bend for convenience" earns its line
- DSL Coverage (20): typed event args (TargetId, SpellTier), when guard on empowerment state, reject for cooldown, rule on mana
- Precept Differentiation (15): "you cannot cast this spell from this state" is identical to "this transition is structurally forbidden"

**Risks:** Fantasy jargon can cloud legibility. Spell names need to be self-explanatory (`Cast`, `Recharge`, not `InvokeArcaneChannel`). State names must be immediate: `Ready`, `Casting`, `Cooling`, `Broken` — not `ArcanePreparation`.

---

### 2.3 Mission Abort Sequence

**Premise:** A rocket launch sequence with abort authority. Systems must be armed before commit. Once committed, abort authority expires at T-minus zero. After liftoff, there is no abort event — only telemetry.

**Emotional hook:** "T-minus" is universally understood as "no turning back." The structural impossibility of aborting post-liftoff is not a limitation — it's the rule. The moment where the reader realizes the engine enforces what engineers take on faith is the product thesis.

**Naturally scores well on:**
- Precept Differentiation (15): "cannot abort after commit" is cleaner as a structural fact than as a conditional check
- DSL Coverage (20): multi-state machine, typed event args for abort authority, rule on fuel level or system readiness, when guard on commitment flag
- Emotional Hook (20): the stakes are enormous; the language can be calm and authoritative

**Risks:** "Rocket" as a domain can feel startup-y or clichéd. The tone must be NASA-dry, not SpaceX-marketing. State names: `StandbyClear`, `Armed`, `Committed`, `Launched`. No exclamation points.

---

### 2.4 Interrogation Room

**Premise:** An interrogation protocol. The suspect is in custody. A deal can be offered only once. Once offered, it cannot be rescinded — even if the detective wants to. A confession makes the deal binding. If the suspect lawyers up, all events are rejected.

**Emotional hook:** Every developer has seen a crime procedural. The deal-offer window, the lawyering-up cutoff — these are instantly legible. The moment a developer sees `reject "The deal has been offered. It cannot be unsaid."` they know exactly what Precept does.

**Naturally scores well on:**
- Voice/Wit (25): `because` messages for cop procedurals write themselves — terse, declarative, slightly cinematic
- Emotional Hook (20): high situational drama, low technical overhead
- Precept Differentiation (15): "deal offer irrevocable" is the canonical structural impossibility framing

**Risks:** Multiple terminal states (LawyeredUp, Confessed, DealAccepted) can push statement count over budget. The "offer deal" path should be the one that shows the most DSL surface. The "lawyer up" path should be compressed to a single terminal transition.

---

### 2.5 Game Show Contestant

**Premise:** A game show contestant enters, competes, and is eliminated. Once eliminated, there is no re-entry. A lifeline can be used once. There is no reinstatement event — not even for a sponsor's special round.

**Emotional hook:** Game show elimination is a universal cultural reference. The "no coming back" rule is felt before it is read. The one-time lifeline maps perfectly to a guarded transition that consumes a boolean field.

**Naturally scores well on:**
- Domain Legibility (20): "Eliminated" is understood in milliseconds
- Emotional Hook (20): empathy for the contestant who uses their lifeline too early
- Line Economy: the domain is naturally compact — enter, play, win or lose, done

**Risks:** Low Precept Differentiation if not carefully constructed. An enum could model "Eliminated = no re-entry" just as easily. The hero moment must be the lifeline mechanic: a single-use, state-consuming, irreversible guard. That's the structural proof.

---

### 2.6 Heist Safe

**Premise:** A safe that tracks failed attempts. Correct combination opens it. Too many wrong attempts trigger lockdown — permanently. Once locked, no combination works. Not even the right one.

**Emotional hook:** "Even the right combination doesn't work once locked" is the sentence that makes a developer stop. It's not an error message. It's a structural fact. The `reject` in `Locked` state is the product thesis in four words.

**Naturally scores well on:**
- Precept Differentiation (15): the impossible-even-if-correct scenario is uniquely expressible with structural constraints
- Voice/Wit (25): "The combination is correct. The safe does not care." is a brand line
- DSL Coverage (20): rule on attempt count, when guard for max attempts, terminal state with reject

**Risks:** The "correct combination" must come in as a typed event arg, not a magic field, or the when-guard elegance is lost. State count: `Locked`, `Closed`, `Open` — exactly three. Don't add `Disarmed` or `Tampered`.

---

### 2.7 First Contact Protocol

**Premise:** A first contact sequence with an alien civilization. Phases are fixed: Signal → Acknowledged → Negotiating → Ratified. Hostile mode can be triggered at any point but is terminal. The ratified treaty cannot be un-ratified.

**Emotional hook:** Absurd stakes, instantly understood. The "hostile mode is terminal" rule feels both absurd and correct — exactly the frame Precept benefits from.

**Naturally scores well on:**
- Emotional Hook (20): the highest possible stakes + the most ridiculous application of business logic = delight
- Precept Differentiation (15): "ratified treaty cannot be undone" is identical to "Ratified is a terminal state with no outgoing transitions"
- Voice/Wit (25): diplomatic language applied to structural rejection is inherently funny

**Risks:** The scenario can feel too clever — humor crowding out proof. `because` messages must pull double duty: funny AND structurally clear. "The treaty is ratified. There is no undo." is better than "Even in space, promises are binding."

---

### 2.8 Chess Piece Promotion

**Premise:** A pawn that can only be promoted to queen, rook, bishop, or knight — never back to pawn. Promotion requires reaching the eighth rank. Once promoted, the piece type is structurally fixed. The promotion event has a typed arg (`PieceTo as string`) with an ensure that rejects invalid piece names.

**Emotional hook:** Chess is universal. Promotion is one of the few rules most people know. The irreversibility of promotion maps perfectly to a terminal-state-flavored transition — once you promote, you don't un-promote.

**Naturally scores well on:**
- Domain Legibility (20): zero domain education required
- DSL Coverage (20): typed event arg with string ensure, when guard on rank, reject for invalid piece type, terminal post-promotion state
- Line Economy: Chess piece state machines are naturally minimal

**Risks:** The domain may feel too abstract — chess as a domain has low stakes. The `because` messages must do emotional work ("A queen does not become a pawn again") that the scenario itself doesn't provide.

---

## 3. DSL Coverage Targets

The rubric requires a raw DSL Coverage score of ≥ 8/10 to pass (hard disqualifier). That means demonstrating at minimum: `rule`, `when` guard, `reject`, dotted field set (e.g. `set Speed = FloorIt.TargetMph`), `transition`, `no transition`, three or more states, a typed `event` with args, and an `on Event ensure`.

### Constructs by strategic value

**Most differentiating (show if possible):**
- `rule` — this is what "structural impossibility" looks like at the data layer. One rule with a memorable `because` message is proof.
- `reject` in a non-terminal state — a `reject` after a failed `when` guard is the product thesis in action. The invalid state never exists. Don't bury it in a terminal state (where it's compiler-redundant).
- `when` guard on event args — more legible than guarding on field values because the contract is visible at the moment of the transition. `when FloorIt.Gigawatts >= 1.21` reads as intention; `when PowerLevel >= 1.21` reads as implementation.

**Easy to show (structural baseline, always include):**
- Three distinct states with meaningful names — the state machine is the hero's skeleton
- `transition` — the most basic Precept operation; include at least two
- `on Event ensure` with a typed event arg — proves Precept validates inputs, not just states

**Adds depth without statement cost:**
- `from any on X` — one rule that applies in multiple states is more expressive than two identical rules. Use it at least once.
- `no transition` — actions that update data without changing state prove Precept isn't just a state machine
- `in State ensure` — state-entry constraints show the engine guards against structural contradictions

**What to omit at budget pressure:**
- More than one `rule` — one with a great `because` message is worth five with generic ones
- `edit` declarations — useful but not hero-critical; save for documentation examples
- Collection field types (`set<T>`, `queue<T>`, `stack<T>`) — powerful but consume budget fast; leave for extended library

---

## 4. Statement Budget Guidance

The Line Economy gate is **6–8 meaningful statements** (pass/fail; outside range = disqualified).

**What counts as a statement:**

| Type | Examples | Count |
|------|----------|-------|
| `precept` declaration | `precept TimeMachine` | 1 |
| `field` declaration | `field Speed as number default 0` | 1 each |
| `rule` declaration | `rule Speed >= 0 because "..."` | 1 each |
| `state` declaration | `state Parked initial` | 1 each |
| `event` declaration | `event FloorIt with Mph as number` | 1 each |
| `from X on Y` rule header | `from Parked on FloorIt when Mph >= 88` | 1 each |
| `set` action | `-> set Speed = FloorIt.Mph` | 1 each |
| `transition` action | `-> transition TimeTraveling` | 1 each |
| `no transition` action | `-> no transition` | 1 each |
| `reject` action | `-> reject "The flux capacitor..."` | 1 each |
| `on X ensure` | `on FloorIt ensure Mph > 0 because "..."` | **0** (part of event) |
| Blank lines, `{`, `}`, `#` comments | — | **0** |

**This is a tight budget.** Count before you write. A minimal viable hero with full coverage requires:

| Allocation | Count |
|-----------|-------|
| `precept` | 1 |
| Fields (1–2 max) | 1–2 |
| Rule (1 max) | 1 |
| States (3 min for legible machine) | 3 |
| Events (2 min for a meaningful interaction) | 2 |
| Rule headers (2–3) | 2–3 |
| Actions (set + transition + reject, minimum) | 3–4 |
| **Realistic minimum** | **13–16** |

**The tension is real.** The 6–8 target drawn from external benchmark research uses a different counting granularity — tools like xstate and FluentValidation treat a block of declarations as one "statement." By Precept's own counting (each declaration individually), 6–8 is below the threshold needed for full DSL coverage. Writers should treat 6–8 as an aspirational compression target and a pressure-testing discipline — not a hard ceiling that supersedes the Coverage floor.

**In practice:** Target ≤ 16 statements. Every statement above 12 must justify itself. Ask: does this action reveal something new about Precept, or does it repeat something already shown?

**Editorial rules for staying compact:**

1. **One field is enough.** Use it for the rule. A second field can appear in a dotted set (`set Score = Level.Points`) without needing its own dedicated rule.

2. **`from any on X` saves one statement per state.** If the same event is valid in multiple states, collapse it.

3. **The `reject` earns its line only when guarded.** A `reject` as the only transition in a terminal state is structurally redundant — the engine already forbids unknown events. Move the `reject` to where it has work to do: after a `when` guard that can succeed or fail.

4. **State names do the work of state comments.** `Committed`, not `AwaitingCommitment`. `Fallen`, not `TerminalStatePostDuel`.

5. **One `on Event ensure` is enough.** If you have a typed event, ensure its most important constraint. One, with a memorable `because`. Not three.

---

## 5. Voice and Tone Per Domain

**Duel at Dawn:** Clipped, formal, Georgian. No contractions. The code of honor speaks in declarative sentences. `"The count was not reached"` beats `"You fired too early"`.

**Spell Casting:** Wry and slightly archaic. The spell's rules predate the caster. Passive constructions work: `"The incantation cannot be repeated"`, not `"You can't cast again"`.

**Mission Abort Sequence:** NASA-dry. Control room language. Short. No warmth, no humor — the authority is the register. `"Commit is irrevocable at T-minus zero"`.

**Interrogation Room:** Cop-procedural terse. Two syllables or fewer when possible. `"The deal is on the table"`. `"Counsel has been invoked"`.

**Game Show Contestant:** Upbeat, then abrupt. The contrast is the joke. Warm before elimination; flat after. `"The lifeline has been used"` — no exclamation point.

**Heist Safe:** Cold. The safe doesn't explain itself. `"Lockdown is permanent"`. Do not let the safe apologize.

**First Contact Protocol:** Diplomatic, slightly pompous. The humor comes from applying interplanetary treaty language to a `reject`. `"The ratified terms are not subject to amendment"`.

**Chess Piece Promotion:** Matter-of-fact. Chess doesn't editorialize. `"A promoted piece does not revert"` — flat, certain, final.

---

## 6. Anti-Patterns

**The CRUD trap.** If your domain is really just "a record that goes through stages" — Draft → Review → Approved → Archived — you have a workflow, not a domain integrity engine. Precept's differentiation lives in the structural constraints, not the state sequence. If a plain enum with a switch statement could do the same thing, the domain is wrong.

**The trivial real-world domain.** A `CoffeeOrder` with states `Ordered → Brewing → Ready → Collected` is legible but unmemorable. Any developer has seen this pattern in onboarding docs for six other tools. Fictional domains avoid the "oh, another coffee example" dismissal.

**The kitchen-sink hero.** Four fields, three rules, five events, eight transition rows. Technically covers every DSL construct. Reads like a tutorial, not a hook. A developer who has to work to understand the domain cannot evaluate the product.

**The clever-but-opaque domain.** "Quantum entanglement state machine." Sounds interesting; requires domain education. The test is: can a developer who has never heard of Precept understand the domain in under three seconds? If not, the domain is working against the hero sample, not with it.

**The philosophical `reject`.** "A cancelled subscription cannot be reactivated" sounds like the product thesis. But if `Cancelled` is a terminal state with no outgoing transitions, the `reject` there is compiler-redundant — the engine would already reject the event. The `reject` must guard a path where success is structurally possible. Move it to where the guard can fail.

**Weak `because` messages.** "Invalid state" is not a `because` message — it is an error code with punctuation. `because` messages are the only prose in the snippet. They are the brand's voice. They must be memorable. The rubric weights Voice/Wit at 25 points — the highest single criterion. A `because` message that a developer might quote is worth five technically correct ones they would forget.

**Event names as gerunds.** `Submitting`, `Processing`, `Completing` — these sound like states, not events. Events are imperatives or nouns that describe what happened: `Submit`, `Complete`, `Fire`, `FloorIt`, `OfferDeal`. The name is part of the brand voice.

**Over-specifying the happy path.** If every state has two or more `set` actions before the `transition`, the snippet is documenting a workflow instead of demonstrating structural integrity. One `set` per rule row is often enough. The `when` guard and the `reject` are more valuable than the data mutation.

---

## 7. Scoring Heuristics

Quick gut-check questions before submitting a candidate.

**Line Economy gate:**
- Count every statement (see Section 4 table). Is the total ≤ 16? If not, identify the two statements you would cut first.
- Did you collapse multi-statement rows? Unpack them. `-> set X = Y -> transition Z` is two statements.

**DSL Coverage (≥ 8/10 required):**
- Does the snippet show a `rule` or `ensure`? (If not, Coverage < 8 automatically.)
- Is there a `when` guard on a rule header?
- Is there a `reject` that guards a path where a `transition` could succeed?
- Is there a dotted field set — `set Field = Event.Arg` — not just `set Field = 0`?
- Does at least one event have typed args?
- Are there three or more distinct states?

**Domain Legibility (≥ 6/10 required):**
- Say the precept name aloud. Does someone who has never heard of Precept know what it is?
- Are the state names obvious without reading the rules?
- Can you explain the domain in one sentence to a non-developer?

**Emotional Hook:**
- Is there a specific line — a `because` message, a state name, a `reject` reason — that you would want to share with a colleague?
- Does the domain involve a genuine constraint — something the domain truly cannot allow — or just a workflow sequence?
- Would a developer who has never used Precept feel something after reading it? (Curiosity, delight, recognition — any of the three counts.)

**Precept Differentiation:**
- Could an enum and a switch statement reproduce this snippet's constraints? If yes: the domain is wrong, or the constraints are too shallow.
- Is there a moment in the snippet where the reader realizes the invalid state never exists — not gets detected, never exists? That moment is Precept's thesis. Find it before submitting.

**Voice/Wit:**
- Read every `because` message aloud. Are any of them memorable? Would any of them survive being quoted in a conference talk?
- Are the event names verbs or imperatives? Are they fun to say?
- Is the `reject` reason — the single most important line in the snippet — worth reading twice?

---

*This brief is opinionated by design. In Phase 3, the brief is not a menu — it is a constraint. Writers who follow it will submit candidates that are genuinely comparable on the rubric. Writers who ignore it will submit candidates that are technically correct and strategically inert.*

*The `because` messages are the hero. Everything else is scaffolding.*
