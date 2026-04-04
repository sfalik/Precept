# Squad Decisions

---

# Team Knowledge Refresh — 2026-04-04 Findings
*Filed by Scribe, 2026-04-04T06:08:06Z. Consolidated from 6 domain reviews.*

---

## CRITICAL SYNC RULE: Grammar-Completions Drift
**Priority:** HIGH — NON-NEGOTIABLE  
**Owner:** Kramer (Tooling), Frank (Architecture)  
**From:** kramer-tooling-review.md

The DSL parser and VS Code tooling are loosely coupled via regex patterns in `PreceptAnalyzer.cs` and `syntaxes/precept.tmLanguage.json`. No automated drift detection exists.

**Finding:** When the parser syntax changes, both files MUST be updated in the same PR or the tooling drifts silently. Example: `NewFieldDeclRegex`, `NewCollectionFieldRegex`, `NewEventWithArgsRegex` are hand-written and not derived from the parser grammar.

**Action:** 
1. Add a documented checklist comment to `PreceptAnalyzer.cs` header:
   ```csharp
   // ⚠️ GRAMMAR SYNC REQUIRED: If DSL syntax changes, update these regexes:
   //   - field syntax: NewFieldDeclRegex, NewCollectionFieldRegex
   //   - event syntax: NewEventWithArgsRegex
   //   - transition syntax: SetAssignmentExpressionRegex, CollectionMutationExpressionRegex
   // Test by running samples/*.precept through the parser and verifying regex matches.
   ```
2. Establish review rule: Kramer must review tooling whenever parser syntax lands.

---

## Medium-Priority Architectural Concerns
**From:** frank-arch-review.md

### 1. Thin-Wrapper Violation Risk in MCP Tools
**Risk:** Some MCP tools independently versioned. Over time, tools accumulate business logic (validation, transformation) that should live in `src/Precept/`.

**Recommendation:** Establish "tool hygiene rule": if tool method exceeds ~50 lines of non-serialization code, logic belongs in core. Audit all 5 tools quarterly against this rule. **Action:** Audit before GA.

### 2. Expression Evaluator Isolation
**Risk:** `PreceptExpressionEvaluator` tested end-to-end through runtime/inspect tests, but has no dedicated unit suite. Null handling, operator precedence, arithmetic overflow tested indirectly.

**Recommendation:** Add `ExpressionEvaluatorTests.cs` with 20–30 test cases covering:
- All operator combinations with null operands
- Operator precedence (e.g., `1 + 2 * 3 == 7`, not `9`)
- Division by zero handling
- Numeric overflow/underflow
- String/boolean operator mismatches

**Action:** Add when expression-related bugs surface or before 1.0 GA.

### 3. Naming Density in Violation Model
**Risk:** `ConstraintViolation`, `ConstraintSource`, `ConstraintTarget`, `ConstraintSourceKind`, `ConstraintTargetKind`, `AssertAnchor`, `StateTarget`, `FieldTarget`, `EventTarget`, `EventArgTarget`, `DefinitionTarget` — 11 types, 4 enums, lots of discriminated unions. Maintainers and AI reading the code can get confused.

**Recommendation:** Add **Violation Model Guide** (`docs/ViolationModelGuide.md`) explaining type hierarchy, when each type is used, with worked examples. Keep under 2 pages (visual diagrams preferred).

**Action:** Create before public distribution.

### 4. Edit Mode Protocol Complexity
**Risk:** `PreceptPreviewProtocol` carries typed field data, edit metadata, and bidirectional graphs collapsed to index arrays. Well-designed but brittle; future changes could require careful migration.

**Recommendation:** 
- Document protocol version in `PreceptPreviewProtocol.cs` file header: `// Protocol version: 2 (2026-04-06, adds EditableFields)`
- Establish stability pledge: "Breaking changes require major version bump and migration guide."
- Add protocol changelog as comments in the file.

**Action:** Document and lock before Marketplace submission.

### 5. Graph Analysis Completeness vs. Data Constraint Detection
**Risk:** `PreceptAnalysis.cs` is deliberately incomplete. Does not detect impossible entry conditions, provably impossible guards, or deadlock states.

**Rationale:** Out of scope for MVP; inspector catches runtime impossibilities.

**Recommendation:** Document explicitly in `docs/PreceptLanguageDesign.md` § Compile-time checks: "The checker detects reachability, orphaned events, and reject-only pairs. It does not detect impossible data constraints (e.g., `X > 0 && X < 0`) — these are left to runtime inspection."

**Action:** Already handled by documentation. Confirm C48 warning is being emitted.

### 6. No Cross-Precept Composition
**Finding:** Precepts are isolated. No import, inheritance, reference, or composition across preceptsinstances.

**Recommendation:** Document as a deliberate scoping decision (not a bug) in `docs/RoadmapFuture.md` or README. If composition becomes a requirement, design a separate feature.

**Action:** Confirm with shane that this is intentional; document as known limitation if needed.

---

## Runtime Edge Case Review
**From:** george-runtime-review.md

Eight edge cases reviewed; 7 working-as-designed. **One medium-risk finding:**

### Dotted Name Resolution in Constraints ⚠️ MEDIUM RISK
**Location:** `ConstraintViolation.cs` lines 116–124 (`ExpressionSubjects.Walk`)

**Scenario:** Invariant references field with dotted property:
```precept
invariant Items.count > 0
```

**Issue:** Walk behavior identifies `Items.count` as `("Items", "count")` — treats it as EventArg reference instead of field property. Violation targets would be `EventArgTarget("Items", "count")` instead of `FieldTarget("Items")`.

**Impact:** Affects violation attribution in UI/API, not engine correctness.

**Mitigation:** Type checker validates at compile time — dotted refs in non-event-assert scopes are flagged if prefix isn't an event name.

**Recommendation:** Add defensive check in constraint extraction; if Walk produces arg-targets for field expressions, log warning.

---

## Language Server & Extension Gaps
**From:** kramer-tooling-review.md

### Syntax Highlighting Implementation (Phases 0-7)
**Status:** Design docs exist; implementation not started.

**Current state:**
- Phase 0 (Grammar refactor) — not started
- Phase 1-2 (Custom semantic tokens) — not started
- Phase 3-7 (Color binding + modifiers) — not started

**Impact:** 8-shade palette defined but not implemented; users see generic theme colors.

**Recommendation:** Lane assignment: **George** (Phases 0-1), **Kramer** (Phases 2-7). Multi-week project, not urgent, but blocks "design locked" claims in marketing.

### Completions Type-Awareness Gaps
**Finding:** Completions lack type-aware filtering in three scenarios:
1. Set assignment expressions — suggests all fields, not just same-type values
2. Collection mutations — suggests all expressions, not just inner-type values
3. Dequeue/pop "into" targets — partially implemented; doesn't exclude captured fields

**Status:** Nice-to-have validations; parser already catches errors. Queue as Phase 2 enhancement. Low priority.

### Semantic Token Modifiers Not Emitted
**Finding:** `preceptConstrained` modifier registered but never emitted. Design calls for italic text on constrained fields/states/events.

**Status:** Phase 7 of implementation plan. Queue after colors bound (Phase 5).

### Hover & Definition Limitations
1. **Built-in collection members** — hovering over `Floors.count` shows no tooltip
2. **Precept name** — top-level machine name not clickable for "go to definition"

**Status:** Design limitations. Built-in members should have dedicated hover tooltip in Phase 2. Top-level name requires workspace scanning.

### Document Intelligence Fallback Parsing
**Finding:** `PreceptDocumentIntellisense.cs` uses regex to extract declarations when main parser returns null (incomplete/invalid syntax). Patterns separate from canonical parser patterns.

**Recommendation:** Already documented as "fallback-path code." No change needed, but make clear that regex drift is a known risk. Mitigate via PR review + testing.

---

## Rule Analyzer Diagnostic Gaps
**From:** soup-nazi-rule-analyzer-gaps.md  
**Priority:** MEDIUM (UX/DX, not correctness)

### Problem
PreceptAnalyzerRuleWarningTests.cs has only **1 test**, covering 1 warning scenario. **Seven critical diagnostic cases are untested:**

1. **From-state asserts never checked** — no incoming transitions to state
2. **Field rule scope violations** — references field other than its own
3. **Event rule scope violations** — references instance data (only args visible)
4. **Top-level rule forward reference** — references field before declaration
5. **Null expression failure in rule** — may fail if nullable field is null
6. **Rule violated by field defaults** — default value violates invariant
7. **Initial state rule violated by defaults** — boot failure scenario

### Why This Matters
These diagnostics are **compile-time checks already** (parser + compiler validate them). The analyzer should expose them via Diagnostic objects for real-time IDE highlighting. Without them:
- User edits rule, hits save → no red squiggles
- User publishes → compile fails at deploy time
- Or: code compiles but rule is silently never checked

### Action
Write 7 additional test methods in `test/Precept.LanguageServer.Tests/PreceptAnalyzerRuleWarningTests.cs`:

```csharp
[Fact] public void Diagnostics_FromStateAssertWithoutExitingTransitions_ProducesWarning() { ... }
[Fact] public void Diagnostics_FieldRuleReferencesAnotherField_ProducesError() { ... }
[Fact] public void Diagnostics_EventRuleReferencesInstanceData_ProducesError() { ... }
[Fact] public void Diagnostics_RuleForwardReferencesField_ProducesError() { ... }
[Fact] public void Diagnostics_NullableFieldInNonNullExpression_ProducesWarning() { ... }
[Fact] public void Diagnostics_FieldDefaultViolatesRule_ProducesError() { ... }
[Fact] public void Diagnostics_InitialStateRuleViolatedByDefaults_ProducesError() { ... }
```

---

## Code Quality Concerns
**From:** uncle-leo-code-review.md

### 1. Unsafe Null-Forgiveness Pattern (LOW SEVERITY)
**Location:** `tools/Precept.LanguageServer/PreceptAnalyzer.cs:35`

```csharp
public bool TryGetDocumentText(DocumentUri uri, out string text)
    => _documents.TryGetValue(uri, out text!);
```

**Issue:** `text!` bypasses type system. While technically safe, it's a code smell that breaks nullable flow analysis. If logic changes, suppressed compiler warnings would catch the bug.

**Fix (ranked):**
1. **Explicit assignment** (Option 1) — most explicit, zero surprise
2. **Assertion** with comment explaining unreachability
3. **Accept pattern** — document inline if performance-critical

**Recommendation:** Option 1 (explicit assignment).

### 2. Hydrate/Dehydrate Dual-Format Complexity (MEDIUM SEVERITY)
**Location:** `src/Precept/Dsl/PreceptRuntime.cs:162–253`

**Issue:** Instance data lives in two formats:
- **Public:** Field names → values (no prefix). Collections are `List<object>`.
- **Internal:** `__collection__<fieldName>` → `CollectionValue` objects.

Three methods (`Hydrate`, `Dehydrate`, `CloneCollections`) invoked at three mutation sites (Fire, Inspect, Update). If any site forgets one step, **silent data corruption** occurs.

**Fix (ranked by effort):**
1. **Highest confidence (Medium effort):** Extract `DataMutation` record encapsulating triple: `(Clean, Internal, Collections)`. Pass through mutation methods instead of juggling three variables.
2. **Good practice (Low effort, high ROI):** Add invariant checks before returning:
   ```csharp
   foreach (var kvp in resultData) {
       if (kvp.Key.StartsWith("__collection__")) {
           throw new InvalidOperationException("Dehydrate forgot to strip collection prefix");
       }
   }
   ```
3. **Documentation (Immediate):** Add comment explaining three-step protocol.

**Recommendation:** Implement option 2 immediately (catches mistakes in testing). Schedule option 1 for next refactor.

---

### 2026-04-04T05:55: User directive — voice & hero tone update
**By:** shane (via Copilot)
**What:** Brand voice updated to allow occasional jokes. The hero sample may use a fun/pop-culture domain. Back to the Future (TimeMachine) is explicitly approved as a hero candidate. Jokes in `because` reason messages are appropriate for the hero snippet.
**Why:** User updated brand-decisions.md and confirmed this direction directly.
**Supersedes:** Any prior "Serious. No jokes." guidance from Steinbrenner's hero spec.

---

# Brand Research — Team Observations
*Filed by J. Peterman, 2026-04-05. For team awareness.*

---

## 1. Reference files are causing a navigation problem

The `brand/references/` files were written *before* brand decisions were locked. They contain "here are four options" framing for things that have already been resolved. A future contributor reading `color-systems.md` or `brand-positioning.md` will encounter open-question language for closed questions.

**Recommendation:** Add a status header to each reference file pointing to `brand-decisions.md` as the locked resolution. Example:

```
> STATUS: Research archive. Decisions resolved in brand/brand-decisions.md.
> Do not treat options in this file as open.
```

This is a one-line edit per file. Low cost, prevents re-litigation.

---

## 2. The AI-native frame is undersold

The secondary positioning — "the contract AI can reason about" — is treated almost as a footnote in current materials. But the actual implementation (five MCP tools, deterministic engine, structured APIs) is genuinely first-class and differentiated. No other tool in the state machine / domain integrity space has this story.

**Recommendation:** The AI-native frame deserves a dedicated paragraph in the README, not just a parenthetical in positioning docs. Not as the opening — the primary frame is correct — but as a named capability section. Something like:

> **AI-native by design.** Precept ships a complete MCP server. An AI can create, inspect, fire, and validate a `.precept` definition without human feedback. The deterministic engine guarantees the AI's changes produce the same outcomes a human would verify manually.

Worth discussing as a team: is this the right moment to elevate this, or does it wait for explicit AI-workflow documentation?

---

## 3. Hero snippet priority

The hero snippet is the most consequential unresolved brand decision — more so than the icon. It will appear in every screenshot, every VS Code listing, every blog post. The icon appears once in NuGet search. The hero snippet is seen by every developer who evaluates the product.

Active spec: `.squad/decisions/inbox/steinbrenner-hero-example-spec.md`

This should be treated as a blocking item for any launch-facing work.

---

## 4. Wordmark rationale should be public

"Small caps is the typographic convention for defined terms, legal codes, and axioms — exactly what a precept is." This is a strong line that builds brand credibility with developers who notice intentionality. It currently lives only in internal brand files.

**Recommendation:** Surface this reasoning in the README or a brief "About the design" section. Not a manifesto — two sentences. Developers who care about craft will notice it. Developers who don't will skip it.

---

# Decision: TimeMachine Hero Concept (Candidate I)

**Proposed by:** J. Peterman  
**Date:** 2026-04-04  
**Status:** Proposed — awaiting team review

---

## What changed

Candidate I in `brand/explorations/visual-language-exploration.html` has been upgraded from a minimal 2-state BTTF toy to a full-featured 18-line hero example.

## The improved snippet

```
precept TimeMachine

field Speed as number default 0
field FluxLevel as number default 0
invariant Speed >= 0 because "Even DeLoreans cannot drive in reverse through time"

state Parked initial, Accelerating, TimeTraveling

event FloorIt with TargetMph as number, Gigawatts as number
on FloorIt assert TargetMph > 0 because "The car has to be moving, Doc"
on FloorIt assert Gigawatts > 0 because "The flux capacitor cannot run on vibes"

event Arrive

from Parked on FloorIt -> set Speed = FloorIt.TargetMph -> set FluxLevel = FloorIt.Gigawatts -> transition Accelerating
from Accelerating on FloorIt when FloorIt.TargetMph >= 88 && FloorIt.Gigawatts >= 1.21 -> set Speed = FloorIt.TargetMph -> set FluxLevel = FloorIt.Gigawatts -> transition TimeTraveling
from Accelerating on FloorIt -> reject "Roads? Where we're going, we still need 88 mph and 1.21 gigawatts."
from TimeTraveling on Arrive -> set Speed = 0 -> set FluxLevel = 0 -> transition Parked
```

**Compiles clean — zero diagnostics.**

## Why this concept

### Part 1 — What makes a great hero

A hero example must do three things simultaneously: teach the DSL surface, demonstrate the brand voice, and make the reader smile. The `because` messages are the brand's one permitted moment of wit — they're the human voice inside the machine, and that voice earns trust from developers.

The original TimeMachine (candidate I) failed on all three counts: no invariant (the most important constraint mechanism, invisible), only 2 states (no state machine shape), no `when` guard (key conditional logic, missing), no `reject` (the enforcement story untold), and flat `because` messages ("1.21 gigawatts required" — informational but not memorable).

Tone: the brand voice is authoritative and matter-of-fact with warmth. Serious but not humorless. The `because` messages are the exception — they may carry personality because they are authored by the developer, not the framework.

### Part 2 — Two concepts considered

**Option A: Improved TimeMachine (winner)**
- Domain: Back to the Future DeLorean time machine
- Why it's funny: Everyone knows the 88mph/1.21 gigawatt conditions. The DSL asserts map perfectly to the film's exact physics contract. The reject message subverts the film's most famous line.
- DSL features: invariant, 3 states, event with 2 args, dual event asserts, when guard on event args, reject, dotted access, clean 3-state cycle
- Why it wins: universal cultural legibility, physics constraints map naturally to precept constraints, the `because` messages are immediately memorable

**Option B: EspressoMachine (runner-up)**
- Domain: Specialty espresso shot workflow (Cold → Preheating → Ready → Pulling)
- Why it's funny: treating coffee with engineering seriousness; `because` messages like "Anything under 7 grams is technically a beverage, not an espresso" and "The boiler is not at temperature. This is espresso, not hot brown water."
- DSL features: 4 states, invariant on BoilerTemp, event with domain args (DoseGrams, GrindSize), when guard on boiler temperature, reject on cold-pull attempt
- Why it doesn't win: requires more domain explanation; TimeMachine's conditions are already universally known, zero cognitive overhead

### Part 3 — Feature checklist

| Feature | Present |
|---------|---------|
| field(s) with defaults | ✅ Speed, FluxLevel default 0 |
| invariant with `because` | ✅ Speed >= 0 because "Even DeLoreans..." |
| 3+ states | ✅ Parked, Accelerating, TimeTraveling |
| event with args | ✅ FloorIt with TargetMph, Gigawatts |
| event assert with `because` | ✅ Two on FloorIt asserts |
| `when` guard | ✅ when FloorIt.TargetMph >= 88 && FloorIt.Gigawatts >= 1.21 |
| `reject` | ✅ from Accelerating on FloorIt -> reject "Roads?..." |
| set with dotted access | ✅ FloorIt.TargetMph, FloorIt.Gigawatts |
| clean transitions | ✅ Parked → Accelerating → TimeTraveling → Parked |

### The `because` messages — brand voice rationale

- `"Even DeLoreans cannot drive in reverse through time"` — dry, matter-of-fact, earns a smile
- `"The car has to be moving, Doc"` — addresses the reader directly; the "Doc" makes it feel authored, not generated
- `"The flux capacitor cannot run on vibes"` — this is the best line in the snippet; "vibes" is the exact wrong energy for a precision instrument
- `"Roads? Where we're going, we still need 88 mph and 1.21 gigawatts."` — perfect subversion of the film's most famous line; makes the constraint feel inevitable

## Decision requested

Should candidate I be promoted as the third shortlisted hero candidate alongside B′ (ParkingMeter) and H′ (TrafficLight)? If promoted, the shortlist note should be updated to reflect the three candidates and their distinct tradeoffs.

---

### 2026-04-04: User directive — model upgrade policy
**By:** Shane (via Copilot)
**What:** Always use latest 4.6 Claude models (claude-opus-4.6 / claude-sonnet-4.6). Never use haiku. Uncle Leo uses gpt-5.4 for large context window code reviews. Steinbrenner (PM) upgraded from haiku to claude-sonnet-4.6.
**Why:** User request — captured for team memory

---

# Hero Domain Verdict — Subscription

**Decision:** The Precept hero snippet domain is **Subscription**.

**Requested by:** shane  
**PM:** Steinbrenner  
**Status:** Final — execute against `steinbrenner-hero-sample-brief.md`

---

## The Winning Domain

**Subscription** (`Trial → Active → Suspended → Cancelled`)

## Three Reasons

1. **Maximum recognizability.** The subscription lifecycle is universally legible to any backend .NET developer in under three seconds — no industry context required. Every engineer has built or integrated a billing/subscription system. The projection is immediate and frictionless.

2. **Natural three-construct proof.** The domain generates the three hero constructs without forcing: `invariant MonthlyPrice >= 0` (obvious business fact), `reject "Cancelled subscriptions cannot be reactivated"` (obviously correct blocked path), `when PlanName == null` or similar guard (conditional engine reasoning). No contrived rules needed.

3. **Line budget fits cleanly.** State names are short (Trial, Active, Suspended, Cancelled). Field names are short (MonthlyPrice, PlanName). Events are short (Activate, Suspend, Cancel). The hero fits 15 lines with room for structural blank lines — no cramming.

## Ruled Out

- **TimeMachine:** Scored 1/5. Fantasy domain. No invariant, no when guard, no reject. Pop culture because messages violate brand voice. Disqualified.
- **Loan:** Canonical 35-line sample already exists in `samples/loan-application.precept`. A 15-line version would be a worse imitation of existing work.
- **Shipment:** Too many bootstrap fields (weight, carrier, address) to generate natural rules within the line budget.
- **ServiceTicket:** Strong (5/5) but narrower projection target than Subscription — requires knowing your team uses a ticketing system.

---

# Hero Example Spec — TimeMachine Replacement

**Status:** Spec — ready for J. Peterman to execute  
**Requested by:** shane  
**PM:** Steinbrenner

---

## 1. What the Hero Must Demonstrate (Non-Negotiables)

The hero has ONE job: make a .NET developer read it and think "this is a real business rule engine, and I understand it immediately." Six DSL features earn that reaction — every one of them must appear:

| Feature | Why it's non-negotiable |
|---------|------------------------|
| `invariant … because` | THE headline claim: "invalid states structurally impossible" — if this is missing, the hero doesn't prove the product |
| `when` guard | Shows the engine makes conditional decisions, not just routes transitions |
| `reject … because` | Shows blocked paths — the contract refuses bad requests, it doesn't just log them |
| Event `with` args + `assert … because` | Shows input validation at the event boundary, not in user code |
| `set … = Event.Arg` (dotted access) | Shows the transition body is an atomic, auditable pipeline |
| Named states that mean something | States must read like a real lifecycle (not Parked/TimeTraveling) |

### Also show (structural density signals)
- `from` / `on` transition syntax — the core dispatch pattern
- `no transition` — in-place update without state change
- Comma source list (`from A, B on Event`) if possible without cost to readability

---

## 2. Why the Current TimeMachine Is Weak

### Feature gaps (15 lines, but only ~7 DSL features)
- **No `invariant`** — the product's marquee claim is entirely absent
- **No `when` guard** — no conditional branching, no domain logic
- **No `reject`** — no demonstration that invalid states are impossible; nothing is ever refused
- **No `no transition`** — no in-place update pattern

### Domain problems
- **Fantasy domain** — a DeLorean is not a workflow a .NET developer will map to their codebase. The abstract distance is too high.
- **Pop culture asserts** — `TargetSpeed == 88` and `Gigawatts >= 1.21` are jokes, not business rules. They show the syntax but communicate nothing about why the engine matters.
- **Brand voice violation** — brand says "Serious. No jokes." The TimeMachine is a joke. It also wastes the gold `because` messages on punchlines, which is the only warm hue in the palette.

### Mechanical problems
- The `Accelerate` transition is crammed onto one line: `-> set Speed … -> set FluxLevel … -> transition` — hard to scan; defeats the top-to-bottom readability argument
- `Arrive` just zeros everything — a reset with no rule. No guard, no assertion, no business meaning.

---

## 3. Line-Count Target

**15 lines, hard cap.** Reasoning:
- LoanApplication (the full product demo) is 35 lines. A hero is not a tutorial.
- Both shortlisted candidates (B′ ParkingMeter, H′ TrafficLight) hit 15 clean.
- 15 lines ≈ a code block that fits in a README without a scroll affordance on most screens.
- Below 13 lines: too sparse to show enough features. Above 17: starts to look like documentation, not a hero shot.

Structural budget at 15 lines (±1):
```
1  precept Name
2  (blank)
3  field … default
4  invariant … because
5  (blank)
6  state … initial, …
7  (blank)
8  event … with … as …
9  on … assert … because
10 (blank)
11 from A, B on Event
12   -> set … = Event.Arg
13   -> transition C
14 from C on Event when …  -> reject "…"
15 from C on Event          -> no transition
```

That structure shows every non-negotiable in 15 lines with one blank separator budget.

---

## 4. What "Cute/Funny" Actually Buys

Nothing. Less than nothing.

The brand voice is "authoritative with warmth." Warmth means plain-language `because` messages that sound like a real product owner wrote them — e.g., "Approved loans must have verified documents." Not punchlines.

The TimeMachine's humor signals: "this is a toy demonstration." The product needs to signal: "this is a production runtime." The developer reading the README needs to see themselves using this in their actual codebase. A DeLorean blocks that transfer.

**Tone rule for J. Peterman:** the `because` messages are the only copy that breathes. Write them in the voice of a domain expert who cares about correctness, not a comedian. One wry edge is acceptable if the domain earns it (ParkingMeter's "Negative time is a mathematical luxury we cannot afford" lands because it's true and precise, not because it's a pop culture reference). Zero references to movies, TV, or internet culture.

---

## 5. Winning Example Checklist

J. Peterman executes against this list. All items must be checked.

**Domain**
- [ ] Real-world domain — something a .NET developer recognizes as a workflow they might own
- [ ] 2–4 states with meaningful names (a lifecycle, not arbitrary labels)
- [ ] Domain makes the `reject` condition self-evidently correct (reader should think "of course that's rejected")

**Features**
- [ ] `invariant … because` present — data integrity, not just event validation
- [ ] `when` guard on at least one transition
- [ ] `reject "…"` on at least one blocked path
- [ ] `on Event assert … because` — event-level input validation with a message
- [ ] `set Field = Event.Arg` — dotted access in transition body
- [ ] `no transition` — in-place update (or comma sources if budget allows both)
- [ ] `from … on … -> … -> transition` — multi-step transition body (must be multi-line, not crammed)

**Craft**
- [ ] 15 lines ±1
- [ ] `because` messages are domain-appropriate, not jokes or pop culture references
- [ ] All 4 semantic color families are represented: indigo structure, violet states, cyan events, slate data, gold messages
- [ ] The snippet compiles clean against `precept_compile`
- [ ] Multi-step transition bodies are line-broken (one `->` per line)

**Brand**
- [ ] No jokes; no movie/TV references
- [ ] No hedging ("kind of", "basically") — state facts
- [ ] Domain maps directly to what a .NET backend developer builds

---

## Candidate Domain Suggestions (for J. Peterman)

These are starting points, not mandates:

- **Subscription** — `Trial → Active → Suspended → Cancelled`: natural lifecycle, `reject` on reactivating a cancelled subscription, `invariant` on billing amount
- **ServiceTicket** — `Open → InProgress → Resolved → Closed`: multi-state, `reject` on resolving without a resolution note, `when` guard on SLA breach
- **ShipmentOrder** — `Pending → Dispatched → Delivered`: `invariant` on weight, `reject` on dispatching with no carrier, `when` based on weight threshold
- **MediaUpload** — `Queued → Processing → Published → Archived`: `reject` on publishing zero-byte file, `when` on file size, `no transition` on metadata update

Avoid: anything that requires 3+ fields to make the domain legible (kills the line budget). Avoid: domains that require long state names.

---

# Steinbrenner — Lang Spec Review Decisions

**Date:** 2026-04-04
**Author:** Steinbrenner (PM)
**Source:** language-spec-brief.md deep-dive

---

## Decision 1 — Syntax Highlighting is a Release Gate

**Decision:** The 8-shade semantic palette (SyntaxHighlightingImplementationPlan.md) must ship before v1 release. It is not a backlog item.

**Rationale:** The entire "color encodes compiler-known meaning" value proposition is undelivered. The extension currently renders `.precept` files with whatever the active theme provides. The brand spec, design doc, and 8-phase implementation plan are all locked and waiting. Phase 0 (TokenCategory.Grammar refactor) is a standalone 1-day change. Phases 1-6 are mechanical. Phases 7-8 are explicitly deferred and do not need to ship in v1.

**Owner needed:** George (or any engineer). Assign Phase 0 immediately.

---

## Decision 2 — RulesDesign.md Must Be Archived or Rewritten

**Decision:** RulesDesign.md must be updated before any new contributor or AI agent reads it.

**Rationale:** The doc says "Status: Implemented" and describes a `rule` keyword with indented syntax and type-prefix field declarations (`number Balance = 0`) that do not exist in the current language. No sample file uses `rule`. The current language uses `invariant` and `assert`. This is a documentation liability that actively misleads anyone trying to understand or author the language.

**Action:** Either (a) archive to `docs/archive/` and note the supersession, or (b) rewrite to accurately describe how invariants and asserts map to what `rule` was designed to do.

---

## Decision 3 — CLI-or-Kill: Decision Required

**Decision:** A decision must be made on whether to implement or permanently defer the CLI.

**Rationale:** The CLI design (CliDesign.md) has been pending since the old CLI host was removed. The MCP server now covers the same workflows. The @ToDo.md explicitly asks whether the CLI is still needed. Every roadmap review re-encounters this unresolved question. The design doc also uses stale `Dsl*` naming (pre Phase 0-3 renames) — it needs an audit pass before it could be implemented.

**Options:**
- **Kill:** Archive CliDesign.md, remove CLI from "Later" items. Rely on MCP for all machine interactions.
- **Implement:** Assign to an engineer, complete naming audit, implement in a dedicated milestone.
- **Defer with a hard date:** Set a specific milestone where this decision is revisited.

**Recommendation:** Kill. The MCP surface is live, the 5-tool surface covers inspect/fire/update, and the AI-first workflow is better served by MCP than a terminal REPL. Sample integration tests can be written directly against the C# API or MCP tools.

---

## Decision 4 — Contradiction/Deadlock Detection: Downgrade in Spec or Implement

**Decision:** The spec must be honest. Checks #4 and #5 in PreceptLanguageDesign.md are labeled as compile-time **errors** but are not implemented. This is a false promise.

**Options:**
- **Implement:** Requires interval/set analysis on expression ASTs. Non-trivial work. Estimate: 1-2 sprints.
- **Downgrade in spec:** Change their severity from "Error" to "Future Work" or "Planned" in the design doc. Makes the spec honest without implementation commitment.

**Recommendation:** Downgrade in spec now; implement later if interval analysis is ever prioritized.

---

## Decision 5 — Preview Protocol: Structured Violations Roadmap

**Decision:** Structured violations in the preview protocol should be scheduled for the release after syntax highlighting ships.

**Rationale:** The runtime returns full `ConstraintViolation` objects. The webview still receives flat strings. Field-level inline highlighting in the inspector panel is blocked by this gap. Not urgent, but it's a visible UX deficit in the flagship developer surface.

**Action:** Schedule as the next "Later" item to graduate to an active milestone after syntax highlighting and plugin distribution are done.

---

# Spec Review Findings — Inbox

**Date:** 2026-03-27  
**From:** Steinbrenner (PM)  
**Artifacts:** `.squad/agents/steinbrenner/spec-brief.md` (27KB comprehensive inventory)

---

## Summary for the Team

I've read the entire language spec, design docs, and implementation plan. Here's what you need to know:

### What We Have

**Language:** Feature-complete. Flat keyword-anchored syntax, four assert kinds, state actions, first-match transitions, editable fields, 50+ compile-time checks, graph analysis. All working.

**Tooling:** Complete. Parser (Superpower), compiler (type-checker + graph analysis), runtime (Fire + Inspect + Update), language server, VS Code extension, preview webview, Copilot plugin (agent + 2 skills), MCP server (5 tools).

**Quality:** High. Type safety locked in (Phases D–H). Constraint violations now structured (Phase CV). Graph analysis warnings (Phase I). 666 tests passing.

**Samples:** 20 `.precept` files demonstrating major features.

### What's NOT Done (But Planned)

| Item | Status | Why Deferred |
|---|---|---|
| CLI | Design exists; code not written | Lower priority; MCP already covers programmatic access |
| Same-preposition contradiction detection | Designed | Requires sophisticated interval/set analysis |
| Cross-preposition deadlock detection | Designed | Same analysis as above |
| Fluent interface for runtime | Nice-to-have | Ergonomic improvement; API works as-is |

**Blocking item:** None. Language is production-ready.

### For the Hero Example

**Pick a concrete domain** (loan application, work ticket, shipment — not fantasy). Demonstrate:
1. Fields + invariants (constraints)
2. States + events (workflow)
3. Transition with guard (routing)
4. One or two asserts showing "invalid states impossible"

**10–15 lines max.** The language can do it. Just need a domain that lets viewers see themselves in it.

---

## Key Design Wins

1. **Prepositions (in/to/from/on) carry consistent meaning everywhere.** Reduces keyword bloat; makes syntax learnable.

2. **First-match transitions instead of exclusive clauses.** Simpler semantics, common pattern (guarded special case + unguarded fallback).

3. **Four assert kinds instead of one generic "rule."** Each temporal scope (entry, exit, residing, event-args) deserves its own keyword. No confusion about when checks run.

4. **Type checking at compile time.** Workflow bugs caught before preview, not discovered on fire. This is a product feature, not implementation detail.

5. **Constraint violations with Source + Targets.** Enables precise error attribution (inline for field targets, banner for scope). No more string-guessing in the UI.

---

## Three Decisions the Team Should Make

### 1. Hero Example Domain & Timeline

**Decision needed:** Which workflow should we use to introduce Precept to the world? (Loan application? Service ticket? Shipment tracking?) And when?

The language is ready. Hero examples are ready. Timing is now a business/marketing decision, not a technical one.

### 2. CLI Implementation Priority

**Decision needed:** The design exists. Do we implement it now, or defer to post-launch? 

Trade-off: CLI gives standalone access (good for automation, testing, CI/CD). But MCP already covers programmatic use and the extension covers interactive use. CLI is nice-to-have, not blocking.

### 3. Same/Cross-Preposition Analysis (Contradiction + Deadlock Detection)

**Decision needed:** Implement the interval/set analysis to catch "state is provably unsatisfiable" at compile time, or leave that discovery to inspect/fire?

Trade-off: Catching contradictions at compile time is powerful and differentiating. But it's non-trivial algorithm work and the runtime already detects these via simulation. Both are valid choices.

---

## What the Spec Brief Contains

- **Language Surface** — full inventory of keywords, constructs, types, expressions
- **Implementation Status** — what's done vs. planned with rationale
- **Key Design Decisions** — the "why" behind syntax and semantics choices
- **API Surface** — complete public C# API (Parse, Compile, Engine, Fire, Inspect, Update)
- **Constraint System** — end-to-end: four kinds, compile checks, fire pipeline order
- **Open Design Questions** — things still being figured out
- **For Hero Work** — which features are stable/showable vs. avoid
- **PM Assessment** — three strategic priorities for the product

**Location:** `.squad/agents/steinbrenner/spec-brief.md` (27KB; comprehensive; reference-grade)

---

## Recommendations

1. **Land the hero example in the next sprint.** Language is done. Choose a domain, write the precept, put it in the README, use it for all marketing. This is the single best way to communicate what Precept does.

2. **Keep type safety + constraint violations on the radar.** These are what differentiate Precept from other state machine DSLs. Document them. Show them off. 

3. **Mark CLI as "next phase" not "blocking."** MCP + extension cover the current use cases. Revisit after launch if demand justifies.

4. **Table the interval/set analysis work.** Designed but non-trivial. Ship without it, add it later if teams report "I want this detected at compile time" feedback.


