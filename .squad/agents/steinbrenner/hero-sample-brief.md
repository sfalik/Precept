# Hero Sample Brief — Definitive Reference for J. Peterman

**Status:** Final  
**Author:** Steinbrenner  
**Audience:** J. Peterman — executes against this when writing the hero snippet  

---

## What the Hero Has to Prove

The hero's job is to make a .NET developer read fifteen lines and think: *"this runtime refuses bad states — it doesn't just log them."* That is the product's core claim — "invalid states are structurally impossible" — and it requires three constructs firing in the reader's face at once: an `invariant` proving that data rules always hold regardless of how you got here, a `when` guard proving the engine reasons conditionally about domain state before acting, and a `reject` proving the engine structurally refuses bad requests rather than recording a violation after the fact. If any one of those three is absent, the hero has not proved the product. It has only described a state machine.

---

## The Domain Rules

A domain works for the hero when a .NET developer can project themselves into it in under three seconds. It fails when it requires explanation — any explanation at all.

### Scoring Criteria

| Criterion | Definition |
|-----------|-----------|
| **Recognizability** | Developer projects themselves into it in <3 seconds |
| **Seriousness** | Carries professional weight — this is software they would ship |
| **Richness** | Naturally generates invariants, guards, and rejections without forcing |
| **Compactness** | Entire entity fits in 15 lines; no cramming |
| **Transition density** | 3+ meaningful states with real transitions |

---

### TimeMachine (current)

| Criterion | Score | Notes |
|-----------|-------|-------|
| Recognizability | ❌ | A DeLorean is a movie prop. Zero .NET developers own one or build software for one. Transfer to real work blocked. |
| Seriousness | ❌ | Pop culture by definition. `mph >= 88` and `Gigawatts >= 1.21` are punchlines dressed as domain rules. |
| Richness | ❌ | Has no `invariant`, no `when` guard, no `reject`. The three hero constructs are entirely absent. |
| Compactness | ✅ | Fits in 15 lines |
| Transition density | ⚠️ | 2 meaningful transitions; sparse |

**Score: 1/5. Disqualified.**

---

### ServiceTicket

| Criterion | Score | Notes |
|-----------|-------|-------|
| Recognizability | ✅ | Every backend developer has built or consumed a ticketing system. Instant projection. |
| Seriousness | ✅ | Support workflows run production systems. Stakes are real. |
| Richness | ✅ | Natural invariant on severity range; natural reject on resolving without a note; natural when guard on priority or SLA breach. |
| Compactness | ✅ | Core lifecycle (Open → InProgress → Resolved → Closed) fits 15 lines cleanly. |
| Transition density | ✅ | 4 states, 3+ meaningful transitions |

**Score: 5/5. Strong contender.**

---

### Subscription

| Criterion | Score | Notes |
|-----------|-------|-------|
| Recognizability | ✅ | Every developer has built or lived a subscription lifecycle. Trial→Active→Suspended→Cancelled is universally legible. |
| Seriousness | ✅ | Billing is never a toy domain. Price invariants, payment rejections — immediately professional. |
| Richness | ✅ | Natural invariant on price (non-negative); natural reject on reactivating a cancelled subscription; natural when guard on payment method or plan presence. |
| Compactness | ✅ | Three core states plus an optional fourth fits 15 lines without pressure. Short field names and state names help. |
| Transition density | ✅ | 3–4 states, all with semantically distinct transitions |

**Score: 5/5. Winner. See verdict.**

---

### Shipment

| Criterion | Score | Notes |
|-----------|-------|-------|
| Recognizability | ✅ | Familiar lifecycle. |
| Seriousness | ✅ | Logistics domain carries professional weight. |
| Richness | ⚠️ | Needs weight, carrier, and address fields to generate natural rules — that's 3+ fields just to bootstrap the domain, burning the line budget before the interesting constructs appear. |
| Compactness | ❌ | Minimum viable domain requires too many fields. 15-line cap is uncomfortable. |
| Transition density | ✅ | 3 states |

**Score: 3.5/5. Ruled out on compactness.**

---

### Loan

| Criterion | Score | Notes |
|-----------|-------|-------|
| Recognizability | ✅ | Universally understood. |
| Seriousness | ✅ | Highest-stakes financial domain. |
| Richness | ✅ | The existing `loan-application.precept` sample is the canonical richness proof. |
| Compactness | ❌ | The canonical sample is 35 lines. A 15-line Loan snippet would look like a stripped-down imitation of a sample already in the repo. |
| Transition density | ✅ | 5 states |

**Score: 3.5/5. Ruled out — we already shipped the definitive Loan sample. Don't write a worse version of it as the hero.**

---

### Winner: **Subscription**

Subscription wins over ServiceTicket on one decisive point: **billing domains are maximally universal.** Every developer has implemented a subscription lifecycle or worked with one. The core lifecycle states (Trial, Active, Suspended, Cancelled) are known without explanation to any backend engineer regardless of industry. The natural domain rules are immediately obvious — price can't be negative, a cancelled subscription can't be reactivated without re-subscribing, you can't activate without a payment method. No context required. The reader sees the rule and nods. ServiceTicket is strong but requires knowing that "your company uses a ticketing system" — a slightly narrower projection target.

---

## The Required Constructs Checklist

Every one of these must appear in the hero. No exceptions.

### 1. `precept` declaration with state enum + `initial`
- **What it is:** The entity header — names the precept and establishes the initial state inline.
- **What it proves:** The engine knows where every instance starts. No ambiguous initial condition.
- **Exact form:**
  ```
  precept Subscription
  state Trial initial, Active, Suspended, Cancelled
  ```

### 2. Field with type + `default` value
- **What it is:** A typed field declaration with a compile-time default.
- **What it proves:** Fields are typed, have defined starting values, and are never silently null.
- **Exact form:**
  ```
  field MonthlyPrice as number default 0
  ```

### 3. `invariant` block with `because` message
- **What it is:** A data rule that holds in every state, checked after every mutation.
- **What it proves:** The engine enforces data integrity as a permanent structural contract, not as ad-hoc validation in user code. THIS IS THE HEADLINE CLAIM.
- **Exact form:**
  ```
  invariant MonthlyPrice >= 0 because "Monthly price cannot be negative"
  ```

### 4. Event with typed argument
- **What it is:** An event declaration that carries typed, named input.
- **What it proves:** Events are typed contracts, not stringly-typed command bags.
- **Exact form:**
  ```
  event Activate with Plan as string, Price as number
  ```

### 5. `when` guard expression
- **What it is:** A conditional expression on a transition row that gates execution.
- **What it proves:** The engine makes conditional decisions based on domain state — it is a rule engine, not just a router.
- **Exact form:**
  ```
  from Trial on Activate when Price > 0 -> ...
  ```

### 6. `set` action using dotted arg access
- **What it is:** A mutation action in a transition body that reads from an event argument via `Event.Arg`.
- **What it proves:** Transition bodies are auditable pipelines — inputs flow from typed event args into fields atomically.
- **Exact form:**
  ```
  -> set MonthlyPrice = Activate.Price
  ```

### 7. `transition` to new state
- **What it is:** The state-change outcome of a transition row.
- **What it proves:** The engine manages state change as an explicit, declared outcome — not an implicit side effect.
- **Exact form:**
  ```
  -> transition Active
  ```

### 8. `reject` with `because` message
- **What it is:** A structural refusal — the transition does not execute and the invalid state never exists.
- **What it proves:** The engine doesn't log bad requests — it structurally refuses them. Invalid states are impossible, not just detected.
- **Exact form:**
  ```
  from Cancelled on Activate -> reject "Cancelled subscriptions cannot be reactivated"
  ```

### 9. A no-transition event (in-place update without state change)
- **What it is:** A transition row whose outcome is `no transition` — fields change but state does not.
- **What it proves:** The engine handles in-place mutations with full constraint enforcement. Not every meaningful operation changes state.
- **Exact form:**
  ```
  from Active on UpdatePlan -> set MonthlyPrice = UpdatePlan.Price -> no transition
  ```

### 10. At least 3 states
- **What it is:** Three or more meaningfully distinct lifecycle positions.
- **What it proves:** The engine models real workflows, not just binary flags.
- **Exact form:** `state Trial initial, Active, Suspended, Cancelled` (four states is ideal; three is the minimum)

---

## The Line Budget

| Metric | Value |
|--------|-------|
| **Hard maximum** | **15 lines** (including blanks) |
| **Hard minimum** | **13 lines** |
| **Target** | **15 lines** |
| **Minimum constructs within budget** | All 10 from the checklist above |
| **Blank lines allowed** | 2–3 structural separators (after fields/invariants, before events, before transitions) |

**Why 15:** 15 lines is the largest code block that renders without a scroll affordance in a standard README viewport. Above 15 it becomes a tutorial, not a proof. Below 13 there is not enough surface to demonstrate all ten required constructs without cramming.

**Multi-step transition bodies must be line-broken.** One `->` per line when a transition body has two or more actions. Cramming `-> set A -> set B -> transition C` onto one line defeats the top-to-bottom scanability argument that is the product's authoring story.

**Reference structural budget (±1 line):**
```
1   precept Subscription
2
3   field PlanName as string nullable
4   field MonthlyPrice as number default 0
5   invariant MonthlyPrice >= 0 because "Monthly price cannot be negative"
6
7   state Trial initial, Active, Suspended, Cancelled
8
9   event Activate with Plan as string, Price as number
10  on Activate assert Price > 0 because "Plan price must be positive"
11
12  from Trial on Activate when PlanName == null
13    -> set PlanName = Activate.Plan
14    -> set MonthlyPrice = Activate.Price
15    -> transition Active
16  from Active on Activate -> set MonthlyPrice = Activate.Price -> no transition
17  from Cancelled on Activate -> reject "Cancelled subscriptions cannot be reactivated"
```

*(This reference runs 17 lines as shown — the actual snippet must collapse or trim to fit 15. Blank lines and the multi-line transition body are the pressure points.)*

---

## The Voice Rules

`because` messages are the only copy that breathes in the hero. Every other token is a keyword or an identifier. The messages are the only place the product's voice speaks directly to the developer. They must sound like a domain expert wrote them, not a programmer commenting code.

### ✅ RIGHT — for the Subscription domain

> `"Monthly price cannot be negative"`  
Domain-expert fact. Short. Precise. A product owner would write this sentence in a requirements doc.

> `"Cancelled subscriptions cannot be reactivated"`  
Specific to the lifecycle. States an operational business rule. The reader instantly thinks "of course — you'd need a new subscription."

### ❌ WRONG — for the Subscription domain

> `"Nice try, but that price doesn't fly here 😄"`  
Joke. Violates "Serious. No jokes." Gold messages are the only warmth in the palette — wasting them on humor is a brand failure.

> `"Invalid state"`  
Programmer error message, not a domain rule. Conveys nothing about what the rule is or why it exists. The developer reading this cannot improve their understanding.

**Rule summary:** Write `because` messages in the voice of the stakeholder who would file the bug if the rule were violated. Not the developer who implemented it.

---

## What the TimeMachine Gets Wrong

### Failure 1: The marquee claim is entirely absent

The product's positioning is "invalid states are structurally impossible." That claim requires `invariant`, `when`, and `reject`. The current TimeMachine has none of them — not one. It is a state machine with assertions. It proves routing; it proves nothing about integrity. A developer reading it would not understand that the engine enforces data rules as permanent contracts. The hero's one job is unachieved.

### Failure 2: `because "You need 1.21 gigawatts of power"` is a pop culture joke, not a domain rule

The brand directive is explicit: "Serious. No jokes." Gold `because` messages are the only warm hue in the entire syntax palette — they are the human-readable signal that the engine speaks in domain language. Using that slot for a *Back to the Future* reference communicates "this is a toy." The developer reading the README is evaluating whether to ship this in a production system. A punchline tells them the answer is no.

### Failure 3: The transition body `-> set Speed … -> set FluxLevel … -> transition TimeTraveling` is crammed on one line

One of Precept's core authoring stories is that the transition pipeline is readable top-to-bottom: event fires, guard checks, mutations run in order, state changes. Cramming three actions onto one line makes that pipeline invisible. It looks like a chained method call, not a declarative contract. The brand visual identity is built around "the code IS the hero image" — if the code isn't readable as a sequence, the visual argument collapses.

---

## The Verdict

**Subscription wins.** Three reasons it's right: (1) the lifecycle (Trial → Active → Suspended → Cancelled) is universally legible to any backend developer in under three seconds — no context required; (2) the natural domain rules (price non-negative, cancelled can't be reactivated, activation requires a plan) generate exactly the three proof constructs the hero must demonstrate without any forcing; (3) the state names are short, the field names are short, and the events are short — the line budget breathes. The single most important thing Peterman must not get wrong: **`invariant` must appear and it must be on a business rule that a non-technical stakeholder would recognize as obviously true.** `MonthlyPrice >= 0` is that rule. If the invariant is a technical constraint — a length check, a format validation — the headline claim ("invalid states structurally impossible") sounds like input validation, not domain integrity. The invariant must be a business fact.
