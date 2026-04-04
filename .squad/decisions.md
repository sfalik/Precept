# Squad Decisions

---

## ACTIVE DECISIONS — Current Sprint

### Design Gate: Peterman Brand Compliance Review (2026-04-04)
**Filed by:** Coordinator (via Shane)  
**Status:** LOCKED  

The design gate for technical surfaces now requires Peterman's brand compliance review as a formal step between Elaine's UX spec and Frank's architecture review.

**Gate sequence:**
1. Elaine — UX design spec
2. Peterman — brand compliance review
3. Frank — architectural fit
4. Shane — final sign-off

**Applies to:** VS Code extension, preview webview, state diagram, inspector panel, and any future product surface.

**Why:** Brand should be applied consistently to all surfaces. Peterman's involvement ensures brand decisions made in `brand/brand-spec.html` are honored in technical implementation, not just noted in documentation.

---

### Brand-Spec Restructure: Surface-First Organization (2026-04-04)
**Filed by:** J. Peterman  
**Status:** COMPLETE  

`brand/brand-spec.html` restructured from 10-section (color-category-first) to 3-section (visual-surfaces-first) structure.

**New structure:**
- **Section 1: Brand Identity** — positioning, narrative, voice, wordmark, brand mark, color system, typography
- **Section 2: Visual Surfaces** — Syntax Editor (locked), State Diagram (locked), Inspector Panel (draft), Docs Site (draft), CLI / Terminal (draft)
- **Section 3: Research & Explorations** — living section with research links and exploration index

**Deferred surfaces** (marked as DRAFT awaiting contribution):
- Inspector Panel — pending Elaine's design review
- Docs Site — scope clarification needed
- CLI / Terminal — color audit pending

**Research foundation:** `brand/references/brand-spec-structure-research.md` (validated surface-first pattern across 13 systems: VS Code, Vercel, GitHub, IBM, Material, etc.)

---

### Visual Surfaces Draft: Five UX Specifications (2026-04-04)
**Filed by:** Elaine  
**Status:** DRAFT FOR REVIEW  
**File:** `brand/visual-surfaces-draft.html`

Five surfaces drafted with UX descriptions covering purpose, visual concerns, color application, typography, accessibility, and AI-first notes:
1. **Syntax Editor** — `.precept` file authoring in VS Code
2. **State Diagram** — visual graph in preview webview for logic verification
3. **Inspector Panel** — live instance state and field editing (functional, brand drift found)
4. **Docs Site** — future public documentation surface (scope clarification needed)
5. **CLI / Terminal** — command-line output (color audit pending)

**Key principles locked:**
- Semantic unity: all surfaces use the same visual language
- Color + shape/symbol redundancy for accessibility
- Verdict colors (green/yellow/red) runtime-only
- AI-first design: every surface works for humans AND AI agents

**Critical issues flagged:**
- Inspector panel uses custom palette (#6D7F9B, #8573A8, #1FFF7A, #FF2A57) instead of brand system (violet, cyan, emerald, rose)
- Inspector uses Segoe UI instead of Cascadia Cove monospace
- CLI color system audit needed (unknown if existing tools apply colors)
- Light theme support clarification needed

**Next steps:** Shane review → Elaine/Peterman integration into brand-spec → Kramer inspector color fix

---

### Inspector Panel Design Review: Brand Color Drift Found (2026-04-04)
**Filed by:** Elaine  
**Status:** PENDING FIX  
**File:** `brand/inspector-panel-review.md`

Kramer's inspector panel is functionally complete but diverges from brand system:

**What works:** ✅ List-based layout, constraint violation workflow, edit mode UX, accessibility redundancy

**Brand mismatches (CSS-only fixes):**
| Element | Current | Brand Target |
|---------|---------|--------------|
| State colors | `#6D7F9B` | Violet `#A898F5` |
| Event colors | `#8573A8` | Cyan `#30B8E8` |
| Success | `#1FFF7A` | Emerald `#34D399` |
| Error | `#FF2A57` | Rose `#F87171` |
| Font | Segoe UI | Cascadia Cove monospace |

**Owner:** Kramer (implementation)  
**Gate:** Elaine review → Peterman brand compliance → Frank → Shane sign-off

---

### Charter Updates: Peterman + Elaine Design Governance (2026-04-04)
**Filed by:** Coordinator  
**Status:** LOCKED  

Two charter amendments formalize design gate participation:

**Peterman (Brand/DevRel) — Design Review Participation (Brand Compliance)**
- Participates in design reviews for any technical surface where brand is applied
- Flags brand violations, approves final surface designs before implementation
- Approval gate: brand identity correctly expressed, color palette applied per locked rules, typography follows conventions, voice consistent
- Surfaces: syntax highlighting, diagrams, inspector panels, documentation sites, CLI output

**Elaine (UX/Design) — Brand Compliance in Technical Surface Design**
- Leads design work on technical surfaces: Inspector Panel, Docs Site, CLI output, future surfaces
- All designs pass through gate: (1) Peterman reviews brand compliance → (2) Frank reviews architecture → (3) Shane signs off
- Peterman is the brand gate; her approval is prerequisite for architecture review
- Surface contracts defined in `brand/brand-spec.html §2`; designs must conform before review

---

### Color System Audit: Open Decisions (2026-04-04)
**Filed by:** J. Peterman  
**Status:** FINDINGS FOR REVIEW  

Four open brand decisions pending Shane approval:

**Decision #4: Color Card Treatment**
- **Current state:** `brand/explorations/color-exploration.html` has palette-card format but was never updated after Indigo was chosen
- **Options:**
  - (a) Add SUPERSEDED banner to color-exploration.html (like semantic-color-exploration already has)
  - (b) Create new "brand color card" HTML showing locked Indigo + Gold pair with same format
  - (c) Both
- **Recommendation:** Archive color-exploration as reference; no blocking impact

**Decision #5: Outcome Color Scope**
- **Current framing:** "Runtime outcomes in inspector and diagrams"
- **Shane's description:** Broader — diagnostics, inspector, UI states, any success/warning/error surface
- **Question:** Does outcome color layer apply everywhere except syntax highlighting, or is it scope-limited?

**Other open decisions documented in inbox files; recommend Shane review before locking charter updates and brand-spec final section numbering.**


### Diagram Color Mapping — Section Placement and Scope (2026-04-04)
**Filed by:** Frank (Lead/Architect)
**Status:** RECOMMENDATION — awaiting Shane sign-off

Add a **"Diagram color mapping"** h3 subsection to **§2.2 State Diagram** in brand-spec.html. This is the authoritative reference for every color decision in the state diagram surface.

**Placement:** Within §2.2, between the "No lifecycle tints" callout and the shape tiles. Reader flow: Purpose → Color intro → **Color mapping table** → Shape tiles → SVG example → Lifecycle tables.

**Scope boundaries:**
| Concern | Section | Owns |
|---------|---------|------|
| "Violet means states" | §1.4 | Semantic family identity |
| "State names in syntax: #A898F5, italic when constrained" | §2.1 | Syntax-level application |
| "State names in diagram: #A898F5, italic when constrained" | **§2.2** | Diagram-level application |
| "Blocked transitions: #FB7185 dashed" | **§2.2** | Diagram-specific element |
| "Active state: #4338CA fill overlay" | **§2.2** | Diagram-specific interactive state |
| "Error rose used in diagrams and inspector" | §1.4.1 | Cross-cutting usage note |

**Minimum mapping (4 categories):**
1. **Static elements** — canvas bg, node borders (per lifecycle role), node fill, state names, transition edges + arrowheads, event labels, guard annotations, legend
2. **Interactive elements** — active state highlight, enabled/blocked/warning transition edges + labels
3. **Semantic signals** — constrained state/event italic, orphaned node opacity, dead-end shape
4. **Exclusions** — data fields (inspector), rule messages (syntax-only), comments (editorial)

**Hex discrepancies to fix:**
| Element | Current | Correct (palette card source of truth) |
|---------|---------|---------------------------------------|
| Blocked legend SVG | #f43f5e | #FB7185 |
| brand-decisions.md Blocked | #F87171 | #FB7185 |
| brand-decisions.md Warning | #FDE047 | #FCD34D |

**Full analysis:** rand/references/brand-spec-diagram-color-mapping-review-frank.md

---

### Diagram Color Mapping Section (2026-04-04)
**Filed by:** Elaine (UX/Design)
**Status:** PROPOSED — needs Shane sign-off

Add a dedicated diagram color mapping subsection to §2.2 State Diagram in rand/brand-spec.html.

**What:**
Two new <h3> blocks within the existing §2.2 card:
1. **"Diagram color mapping"** — a complete element-to-color reference table covering every visible diagram component (canvas, node borders, node fills, state name text, event label text, transition arrows, arrow markers, guard annotations, legend text).
2. **"Runtime verdict overlay"** — how diagram colors change when paired with an active inspector instance. Covers: current state highlighting, enabled/blocked/warning edge coloring, muted non-current-state edges, transition glow effects, hover interaction colors.

**Why:**
- Scattered specification — diagram colors mentioned inline in §2.2 prose, partially in §1.4.1, and in brand-decisions.md; never collected into one reference
- Implementation drift — webview uses #1FFF7A / #FF2A57 / #6D7F9B; locked system specifies #34D399 / #F87171 / TBD
- Runtime overlay unspecified — verdict-colored edges exist in implementation but have no brand-spec backing
- Current state indicator undefined — not specified anywhere

**Open sub-decisions for Shane:**
1. **Current state indicator style:** Fill tint (#1e1b4b at low opacity) vs. border glow vs. badge dot. Elaine recommends fill tint.
2. **Muted edge color:** #71717A (text-muted, in system) vs. #52525b (zinc-600, off-system). Elaine recommends #71717A.
3. **Guard annotation text color:** Slate #B0BEC5 (data family). Elaine recommends this.

**Full analysis:** rand/references/brand-spec-diagram-color-mapping-review-elaine.md

---
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

---

## Hero Domain Selection: Subscription Billing

**Date:** 2026-05-01  
**Author:** J. Peterman  
**Status:** Decided

### Decision

The rank-1 domain chosen as the new hero in `brand/explorations/visual-language-exploration.html` is **Subscription Billing**.

### Rationale

Subscription Billing scored 29/30 in the hero deliberation (tied with SaaS Trial and Coffee Order), winning the tiebreak on Precept Differentiation (5/5). It was selected as rank #1 for:

- **Universal recognition**: The SaaS trial → active → cancelled lifecycle is immediately understood by any developer, regardless of stack or industry.
- **Quintessential structural impossibility**: `reject "Cancelled subscriptions cannot be reactivated"` is the clearest possible expression of the product thesis — "invalid states are structurally impossible."
- **Multi-line hero format**: The `from Trial on Activate when PlanName == null` block reads like a product spec, teaching five DSL concepts in four lines.
- **Full DSL coverage** (5/5): invariant, when guard, reject, dotted set, transition, no transition, 3 states, typed event, event assert — all present.

### HTML Changes Made

1. **Hero card**: Replaced loan-application.precept with subscription.precept (rank #1, score 29/30).
2. **Rubric section**: New section inserted after hero, showing the 6-criterion scoring table with max scores and descriptions.
3. **30-Candidate Gallery**: All 30 deliberation candidates displayed in rank order, each with badge (gold/silver/bronze), score breakdown, DSL snippet (inline-styled), and reasoning sentence.
4. **Final Ranking Table**: Compact table showing all 30 candidates with per-criterion scores and notes.

### Candidates Tied at 29/30

| Rank | Domain | Why ranked below |
|------|--------|-----------------|
| 1 | Subscription Billing | Winner — strongest differentiation |
| 2 | SaaS Trial | Slightly lower narrative immediacy |
| 3 | Coffee Order | Lower Diff score (4 vs 5) |
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



---

# Phase 1 Research Findings — Hero Research Sprint (2026-04-04T13:39:08Z)

*Filed by Scribe; consolidated from 4 agents (J. Peterman, Steinbrenner, Uncle Leo, George)*

---

## Finding 1: Named Guard Declarations (Priority: HIGH)

**From:** Steinbrenner (DSL Expressiveness Research)

**Decision Inbox Merged**

This section consolidates inbox items from Phase 1 research agents:
- steinbrenner-dsl-research.md
- uncle-leo-verbosity-analysis.md
- george-language-research.md
- j-peterman-hero-creative-brief.md



### 2026-04-04T19:45:58Z: User directive
**By:** Shane (via Copilot)
**What:** Reframe `brand/brand-spec.html` so §1.4 presents only the locked 5 semantic families plus 3 outcome colors; remove `brand-light` and `brand-muted` from the semantic color story entirely unless a specific surface truly needs a local tonal variant. §2.1 may use shading variants within a family as needed, but the general color story should remain 5+3.
**Why:** User request — captured for team memory

---

### 2026-04-04T19:55:35Z: User directive
**By:** Shane (via Copilot)
**What:** In the generic color spec, do not call green/yellow/red "outcome colors." They are only outcome colors in §2.2 when tied to transition/runtime outcomes. In the general spec they need a different name.
**Why:** User request — captured for team memory

---

### 2026-04-04T20:10:30Z: User directive
**By:** Shane (via Copilot)
**What:** Stop using Haiku for agent spawns going forward.
**Why:** User request — captured for team memory

---

### 2026-04-04: User directive — docs terminology + ownership
**By:** Shane (via Copilot)
**What:** 
- Do NOT call it "docs site". It is just "docs".
- Docs are internal team artifacts owned by the team.
- README is the only public-facing exception right now.
- In the future, when public docs exist, Peterman owns them.
- Future public docs site = planned, not current scope.
**Why:** User request — captured for team memory

---

### 2026-04-04T14-21-55: File organization directive
**By:** shane (via Copilot)
**What:** Research files should be stored according to domain ownership: Peterman's research (brand/copy) → rand/references/; Steinbrenner's research (PM/product/docs) → docs/references/. Each agent's research artifacts live under their domain root, not a shared location.
**Why:** User request — captured for team memory

---

# Brand-Spec Final Review Verdict

**Author:** Elaine (UX/Design)
**Date:** 2025-07-16
**Context:** Final review of brand-spec.html after Peterman's information architecture restructure and drift-normalization pass
**Status:** APPROVED

---

## Verdict: APPROVED

All known feedback from the four source reviews (Elaine palette structure, Elaine diagram color mapping, Frank palette structure, Frank diagram color mapping) and the consolidated follow-up checklist has been addressed. The one remaining open item — current state indicator style — is explicitly called out in §2.2 as awaiting Shane sign-off, which is the correct disposition.

---

## Checklist Results

### §1.4 Color System — ✅ All criteria met
- 8+3 brand palette card: present, unchanged
- Compact Semantic Family Reference table: present (6 rows, identity-level only)
- Verdicts framed as general semantic colors (success/error/warning): correct
- Background spec, "Semantic color as brand" callout, "Locked design" callout: all present
- Forward references to §2.1 and §2.2: present in both hue map and semantic family reference
- Syntax family cards removed from §1.4: confirmed — all 6 cards now live in §2.1
- Intro paragraph no longer says "8 authoring-time shades": rewritten to describe two-layer system (brand palette + syntax palette). Ambiguous "8" count resolved.

### §1.4.1 Cross-Surface Color Application — ✅ All criteria met
- Renamed from "Color Usage" to "Cross-Surface Color Application"
- Color Roles table focuses on cross-surface application guidance
- Diagram mentions are brief with §2.2 cross-references
- brand-light row corrected: now explicitly states "State names in syntax and diagrams use Violet #A898F5, not brand-light — see § 2.1, § 2.2"
- README & Markdown Application table: retained (unique content)
- Color Usage Q&A: retained

### §2.1 Syntax Editor — ✅ All criteria met
- All 6 syntax family cards absorbed from §1.4
- Hue map absorbed
- Constraint signaling table consolidated (single table, no duplicate in §1.4)
- Opening cross-reference: "applies semantic family identities from Brand Identity § 1.4 through an 8-shade token system"
- Self-contained: implementer reads §2.1 alone for complete spec

### §2.2 State Diagram — ✅ All criteria met
- New "Diagram color mapping" h3: present, positioned after Event lifecycle types
- Static elements table: 11 rows covering canvas, borders, fills, text, edges, arrowheads, labels, guards, legend
- "Runtime verdict overlay" h3: present as separate subsection
- Verdict hexes locked: #34D399, #FB7185, #FCD34D — all match §1.4 palette card
- Muted edge color resolved: #71717A (text-muted)
- Guard annotation color resolved: #B0BEC5 (Data · Names)
- Semantic signals section: constrained state/event, terminal, orphaned, self-loop
- Cross-references to §1.4 and §2.3: present

### Hex Discrepancies — ✅ All resolved
- Error/Blocked: locked to #FB7185 throughout (no trace of #F87171 or #f43f5e)
- Warning: locked to #FCD34D throughout (no trace of #FDE047)
- Success: #34D399 consistent throughout
- SVG legend blocked line: corrected from #f43f5e to #FB7185
- No off-system colors (#1FFF7A, #FF2A57, #6D7F9B) appear anywhere in brand-spec

### Duplication — ✅ Acceptable
- Constraint signaling: single occurrence in §2.1 only
- State names #A898F5: clean three-level split (§1.4 identity → §2.1 syntax → §2.2 diagram)
- Gold restriction: 5 mentions across 3 sections — more than the ideal 2, but each serves a distinct abstraction level (palette card, semantic family reference, cross-surface roles, Q&A, implementation card). Not the same structural confusion as the original 4-in-300-lines problem.

### Cross-references — ✅ All resolve
- §2.2, §2.3, §2.4, §2.5 all reference §1.4 correctly
- §2.2 references both §1.4 (identity) and §2.1 (syntax contrast)
- §2.2 verdict overlay references §2.3 for inspector usage

---

## Remaining Open Items (Not Blockers)

1. **Current state indicator style** — explicitly flagged in §2.2 as awaiting Shane sign-off. Two options documented (Elaine: #1e1b4b at 20-30% opacity; Frank: #4338CA at 10-15% opacity). This is correct — it's a design decision, not a spec gap.

2. **Hue map in both §1.4 and §2.1** — present in both sections. Defensible as an "orientation bridge" (Peterman's rationale) in §1.4, and as implementer context in §2.1. Not a duplication concern given its compact size (3 lines).

3. **No explicit diagram "Exclusions" subsection** — my original recommendation included listing what's NOT in the diagram (data fields, rule messages, comments). This is implicit from the mapping tables' scope. Minor; not a restructure concern.

---

## Summary

The brand-spec information architecture is now clean. §1.4 owns identity, §1.4.1 owns cross-surface application, §2.1 owns syntax, §2.2 owns diagrams. Each section stays within its abstraction level, cross-references resolve correctly, hex values are consistent, and the diagram has a comprehensive color mapping that didn't exist before. The restructure accomplished exactly what the four reviews converged on.

Ready for Shane's final sign-off.

---

# Brand-Spec Restructure: UX Review Gate and Remaining Risk Points

**Filed by:** Elaine (UX/Design)
**Date:** 2025-07-16
**Status:** ACTIVE — review criteria locked; awaiting Peterman's restructure delivery

---

## Decision Context

Peterman is implementing the brand-spec information architecture cleanup (§1.4 scope split, duplication removal, §2.1 absorption, §2.2 diagram color mapping). Four review documents from Elaine and Frank converge on the same structural recommendations. This decision record establishes what the team should track during and after that work.

## Team-Relevant Recommendations

### 1. Review gate after Peterman's restructure

Before the restructured brand-spec is considered complete, it should pass through:

1. **Elaine** — UX/IA review against the checklist in `brand/references/brand-spec-followup-feedback-elaine.md` (section scope, duplication, cross-references, diagram completeness, hex consistency, reader flow)
2. **Frank** — Architecture review confirming the two-level split (identity vs. surface application) holds
3. **Shane** — Final sign-off, including resolution of three open diagram decisions

This follows the established design gate sequence. The restructure is structural, not creative — but structural errors cascade.

### 2. Three diagram decisions block implementation

The following remain open and block Kramer's webview CSS alignment:

| Decision | Elaine's Recommendation |
|----------|------------------------|
| Current state indicator | Fill tint (#1e1b4b at 10–15% opacity) |
| Muted edge color | #71717A (text-muted, in system) |
| Guard annotation color | Slate #B0BEC5 (data family) |

**Recommendation to Shane:** Resolve these before or concurrent with Peterman's restructure so the diagram section can ship without TBD gaps.

### 3. Hex discrepancies must resolve during restructure

Three known mismatches (Error: #F87171 vs #FB7185 vs #f43f5e; Warning: #FDE047 vs #FCD34D; brand-light #818CF8 misattributed as diagram state name color). The palette card in §1.4 is the source of truth. These should be fixed atomically during the restructure — not deferred.

### 4. Gold restriction consolidation

Gold's "syntax only" rule currently appears 4 times across 300 lines. Consolidate to exactly 2: one in the 8+3 card (§1.4, brand-level) and one in the Rules category card (§2.1, surface-level). Four restatements isn't emphasis — it's a signal that the information doesn't have a clear home.

---

## Applies To

- `brand/brand-spec.html` (primary artifact being restructured)
- All `§2.x` visual surface sections (cross-reference integrity)
- Kramer's inspector/diagram webview CSS (implementation alignment post-spec)

## Full Checklist

See: `brand/references/brand-spec-followup-feedback-elaine.md`

---

# Brand-Spec §1.4 Palette Restructuring

**Filed by:** Elaine
**Date:** 2025-07-15
**Status:** PROPOSED — awaiting Shane decision
**Reference:** `brand/references/brand-spec-palette-structure-review-elaine.md`

---

## Decision Needed

Split §1.4 Color System along the brand-vs-surface seam:

- **§1.4** keeps: 8+3 palette card, a compact semantic hue assignment table, verdict colors, hue map. Scope: "what colors exist and what they mean."
- **§1.4.1** keeps: cross-surface Color Roles table (trimmed to remove palette restatements), README application table, Q&A. Scope: "what color goes where."
- **§2.1** absorbs: the six syntax-highlighting category cards (Structure, States, Events, Data, Rules, Comments) and the constraint signaling table. Scope: "how the editor applies the palette."

## Why

Shane flagged that §1.4 has two palettes stacked back-to-back, and §1.4.1 overlaps with both. The root cause: §1.4 mixes brand-level color identity with surface-level editor implementation. The category cards are syntax-editor-specific content living in a brand-identity section.

Moving them to §2.1:
1. Eliminates the "two palettes in a row" problem
2. Makes §2.1 self-contained for implementers (no jumping back to §1.4 for token-level detail)
3. Removes the duplicated constraint signaling table (currently in both §1.4 and §2.1)
4. Reduces Gold's "syntax only" restriction from 4 occurrences to 2

## Impact

- Brand-spec.html: §1.4, §1.4.1, §2.1 restructured
- No locked color values change — this is information architecture only
- Cross-references in §2.2–2.5 remain valid (they reference §1.4 for the hue system, which still lives there)
- Full edit plan in the reference doc above

## Recommendation

Approve this restructure. It's a readability fix with no impact on locked design decisions.

---

# Decision: Color Usage Roles for 8+3 System

**Date:** 2026-07-12  
**Filed by:** Elaine (UX Designer)  
**Status:** LOCKED  
**Affects:** brand-spec.html §1.4, §1.4.1; README revamp; all product surfaces

## Summary

Defined concrete color usage roles for every color in the locked 8+3 system. Updated brand-spec.html §1.4 palette card to correctly display all 8+3 colors. Added §1.4.1 Color Usage as the definitive reference for how each color maps to product surfaces and README context.

## What Changed

### §1.4 Palette Card — Fixed

**Problem:** The palette card showed an indigo gradient with 8 shades from color-exploration.html, including off-system colors (`#a5b4fc`, `#1e1b4b`, `#312e81`, `#3730a3`). It was an "Indigo overview" card, not the 8+3 brand system.

**Fix:** Replaced with a full-width palette card showing all 8 core colors + 3 semantic colors + gold accent note, organized as:
- Brand Family (indigo trio): `#6366F1` brand, `#818CF8` brand-light, `#C7D2FE` brand-muted
- Text Family: `#E5E5E5` text, `#A1A1AA` text-secondary, `#71717A` text-muted
- Structural: `#27272A` border, `#09090B` bg
- Semantic: `#34D399` success, `#FB7185` error, `#FCD34D` warning
- Note: `#F59E0B` gold (syntax accent only)

### Verdict Colors — Fixed

**Problem:** Verdicts section used wrong hex values inherited from earlier exploration:
- Error: `#F87171` → corrected to `#FB7185`
- Warning: `#FDE047` → corrected to `#FCD34D`

**Why:** The locked semantic colors are emerald `#34D399`, rose `#FB7185`, amber `#FCD34D`. The old values (`#F87171` = Tailwind red-400, `#FDE047` = Tailwind yellow-300) were never part of the locked system.

### §1.4.1 Color Usage — New Section

Added three subsections:

1. **Color Roles table** — All 12 colors with role name and specific usage examples across product surfaces.

2. **README & Markdown Application table** — How brand color maps to GitHub Markdown constraints (wordmark SVG, shields.io badge parameters, hero code block, emoji alignment).

3. **Color Usage Q&A** — Answers five common questions:
   - Secondary highlight → brand-light `#818CF8`
   - Success green in README → CI badges, feature checkmarks
   - Warning amber in README → beta/preview callouts
   - Error rose in README → No (product UI only)
   - Gold outside syntax → No (syntax-only, never UI)

4. **README Color Contract callout** — Defines the three channels for brand identity in plain Markdown: SVG wordmark, shields.io badge row, and DSL keyword rhythm in hero code block.

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| Error rose `#FB7185` never appears in README | Marketing copy should never communicate failure. Error states are product UI feedback, not public messaging. |
| Gold `#F59E0B` is syntax-only, not UI | Gold exists to distinguish human-readable rule messages from machine code. Using it in badges or UI would dilute that semantic precision. |
| Brand identity in GitHub Markdown comes through SVG + badges, not CSS | GitHub strips custom styles. Instead of fighting the platform, the brand speaks through assets (wordmark) and parameters (shields.io colors) that survive rendering. |
| Warning amber is valid in README for beta/preview | Unlike error (which implies failure), warning signals "attention needed" — appropriate for preview features and cautionary notes. |
| Secondary highlight is always brand-light, not a new color | The 8+3 system is closed. Brand-light `#818CF8` serves the "accent" role that might otherwise tempt someone to introduce a new color. |

## Downstream Impact

This decision record is the direct input for:
1. **README revamp** — Peterman can use the README Application table and Q&A without follow-up questions about color.
2. **Brand-spec §1.4 LOCKED status** — The palette card + usage guidance are now definitive enough to lock.
3. **Badge catalog** — Kramer's badge work should use the shields.io color values documented here.

## Off-System Colors Removed from §1.4

| Hex | What It Was | Why Removed |
|-----|-------------|-------------|
| `#a5b4fc` | Indigo gradient swatch (Tailwind indigo-300) | Not in locked 8+3. Shane explicitly called out as off-system. |
| `#1e1b4b` | Deep indigo gradient swatch | Part of indigo exploration ramp, not the brand system. Still used in callout styling as page chrome. |
| `#312e81` | Dark indigo gradient swatch | Same as above. |
| `#3730a3` | Medium-dark indigo gradient swatch | Same as above. |
| `#F87171` | Verdict "Blocked" color (Tailwind red-400) | Wrong. Locked error color is `#FB7185`. |
| `#FDE047` | Verdict "Warning" color (Tailwind yellow-300) | Wrong. Locked warning color is `#FCD34D`. |

---

# Palette Mapping Visual Unification

**Filed by:** Elaine  
**Date:** 2026-04-04  
**Status:** COMPLETE  
**Scope:** `brand/brand-spec.html` — Sections 2.1 and 2.2

## Summary

Unified the palette mapping visual treatment in §2.1 (Syntax Editor) and §2.2 (State Diagram) to match the polished §1.4 (Color System) design language. Created a reusable `.spm-*` CSS component system for surface palette mappings.

## What Changed

### New CSS Component System (`.spm-*`)

Added ~70 lines of scoped CSS creating a "Surface Palette Mapping" design system:

| Component | Purpose |
|-----------|---------|
| `.spm-surface` | Container card with dark background and indigo border |
| `.spm-header` | Color-tinted section headers with gradient backgrounds |
| `.spm-row` / `.spm-grid` | Grid-based layout for swatches and info |
| `.spm-swatch` | Gradient swatches with subtle shadows (56×38px) |
| `.spm-title` / `.spm-hex` / `.spm-weight` / `.spm-tokens` | Consistent info typography |
| `.spm-table-section` / `.spm-table` | Polished table treatment for diagram mappings |
| `.spm-shapes` / `.spm-shape-tile` | Unified shape legend tiles |

### §2.1 Syntax Editor

- Consolidated 7 separate `.card` elements into one `.spm-surface` container
- Each color family (Structure, States, Events, Data, Rules) gets a tinted header
- Grouped as: Core Semantic Tokens → Support Tokens → Reserved Verdict Colors
- Gradient swatches matching §1.4 visual treatment

### §2.2 State Diagram

- Shape tiles now use `.spm-shape-tile` — unified 4-column grid
- Static elements table wrapped in `.spm-table-section` with colored header dot
- Runtime verdict overlay table uses same refined treatment
- Mini-swatches (20×20px) inline with color names in tables

## Why

§2.1 and §2.2 contained the same semantic information as §1.4, but presented with inconsistent visual treatments. The brand spec is itself a brand artifact — visual coherence matters. The unified treatment:

1. Makes the document scannable — color-coded headers provide instant orientation
2. Reinforces the 5+3 semantic model — grouping makes structure obvious
3. Demonstrates brand quality — polished documentation signals polished product
4. Enables reuse — future surface sections (Inspector, Docs, CLI) can use `.spm-*` components

## Semantic Content Preserved

- All locked color values unchanged (5 semantic families + 3 signal colors)
- All element-to-color mappings unchanged
- All hex codes, weight/style notes, and token lists intact
- Runtime verdict semantics remain scoped to §2.2 only
- No new semantic colors introduced

## Applicability

The `.spm-*` system is intentionally general. When §2.3 (Inspector), §2.4 (Docs), or §2.5 (CLI) need detailed element-to-color mappings, the same components apply.

---

# README UX Requirements (Elaine)

**Date:** 2026-04-04  
**Source:** UX/IA review of Peterman and Steinbrenner's README research  
**Status:** Inbox — awaiting incorporation into README restructure proposal

---

## Non-Negotiable Requirements

The final README restructure proposal **must** satisfy all of these constraints:

### 1. Mobile-First "Above the Fold"

**Requirement:** Logo, hook, and primary CTA must be visible in a **550px vertical viewport** without scrolling.

**Rationale:** GitHub mobile web traffic is significant. Developers on phones decide "Is this relevant?" in the first screenful. If they have to scroll to see the value prop, they bounce.

**Validation:** Test rendered README on:
- 400px width (phone portrait)
- 600px width (phone landscape / small tablet)
- 800px width (tablet / narrow desktop)

**Current violation:** Current README's logo + hook + hero code = ~1200px vertical. Hero doesn't appear above fold on mobile.

---

### 2. Single Primary CTA

**Requirement:** The Getting Started section must present **one clear next action**, with secondary actions labeled as "next steps."

**Rationale:** Multiple equally-weighted CTAs = decision paralysis. Developers presented with three options (package, extension, plugin) choose none.

**Implementation:** Numbered sequence:
1. Install VS Code extension (primary CTA)
2. Create first file (follow hero)
3. Add NuGet package (integration step)

Secondary CTA (Copilot plugin) goes in a separate "Advanced" section.

**Current violation:** Three CTAs with equal weight in Quick Start section.

---

### 3. Semantic Heading Hierarchy

**Requirement:**
- H1 for title (project name)
- H2 for major sections (Getting Started, What Makes Precept Different)
- H3 for subsections (1. Install Extension, 2. Create File)
- **No heading level skips** (H2 → H4 is invalid)
- **Headings must be descriptive, not clever**

**Rationale:**
- Screen reader users navigate by heading level
- AI agents parse heading structure to build document outline
- Skipping levels breaks both use cases

**Current violations:**
- Emoji in H2 headings (`🚀 Quick Start`) — screen reader announces "rocket emoji Quick Start"
- Clever headings (`💡 The "Aha!" Moment`) instead of descriptive (`Quick Example`)

**Fix:** Move emoji to end of heading or remove. Replace clever headings with descriptive labels.

---

### 4. Progressive Disclosure

**Requirement:** Section order must follow the evaluation journey:
1. **What is this?** (Hook — 1 sentence)
2. **Can I read this?** (Hero DSL — 18 lines)
3. **Can I use this?** (Quickstart — 5 lines C#)
4. **What makes this different?** (AI tooling, features)
5. **Where do I learn more?** (Docs, samples)

**Rationale:** Each section deepens commitment. Never front-load complexity before proving basic usage works.

**Current violations:**
- Sample catalog appears before quickstart (reference material before onboarding)
- Tooling features described before C# usage is shown
- Philosophical "Pillars" content interrupts onboarding flow

**Fix:** Defer differentiation (AI tools, MCP server) until after Getting Started. Move sample catalog to "Learn More" section at bottom.

---

### 5. Scannable Formatting

**Requirements:**
- Prose paragraphs: **max 2-3 sentences**
- Feature lists: **bullets, not prose**
- Visual separators: **horizontal rules (`---`) between major sections**
- Callout boxes: **blockquotes (`>`) for important asides**

**Rationale:** F-pattern scanning research shows developers scan headings (left edge) and sweep horizontally for interesting content. Wall-of-text paragraphs are skipped.

**Current violations:**
- "The Problem It Solves" section: 150-word prose paragraph
- "World-Class Tooling" section: 6 features in paragraph form
- No horizontal rules between sections

**Fix:** Break prose into chunks, convert feature descriptions to bulleted lists, add visual separators.

---

### 6. Viewport Resilience

**Requirements:**
- Hero code block: **no horizontal scrolling at 600px width**
- Tables: **≤3 columns** (wider tables moved to external docs)
- Images: **responsive scaling** (width="100%" or similar)

**Rationale:** GitHub renders READMEs in a narrow column on desktop and mobile. Long code lines and wide tables break the layout.

**Current violations:**
- Hero code block: 49 lines with some long lines (requires horizontal scroll at narrow widths)
- Sample catalog table: 21 rows × 3 columns (collapses to single-column stack on mobile, unreadable)

**Fix:** Reduce hero to 18-20 lines, move catalog table to external docs page.

---

### 7. Screen Reader Compatibility

**Requirements:**
- Emoji: **after heading text or removed** (not at start)
- Badge alt text: **includes version/status** (not just "NuGet Badge")
- Code blocks: **preceded by descriptive labels** ("Example: Defining an order workflow")
- Heading hierarchy: **valid for navigation** (no H2 → H4 skips)

**Rationale:** Screen reader users navigate by heading level and rely on alt text for image context.

**Current violations:**
- All H2 headings start with emoji (noisy for screen readers)
- Badge alt text: generic ("NuGet Badge" instead of "NuGet version 1.0.0")
- Some code blocks lack descriptive labels

**Fix:** Move emoji to heading end, update badge alt text, add labels before code blocks.

---

### 8. AI Parseability

**Requirements:**

#### Code Blocks
- **All code blocks tagged with language:**
  - ` ```precept ` for DSL samples
  - ` ```csharp ` for C# runtime code
  - ` ```bash ` for shell commands
- **Preceded by descriptive labels:**
  - "**The Contract** (`time-machine.precept`):" before DSL
  - "**The Execution** (C#):" before runtime sample

#### Links
- **Descriptive link text** (no "click here")
  - ✅ "[Language Reference](link)"
  - ❌ "[here](link)"
- **Absolute URLs** for external links (AI agents don't always resolve relative paths)

#### Images
- **Descriptive alt text:**
  - ✅ `![State diagram showing Parked → Accelerating → TimeTraveling transitions](path)`
  - ❌ `![diagram](path)`
- **Text description in surrounding prose** (for AI agents that can't parse images)

#### Structure
- **Semantic HTML via Markdown:**
  - H1 → H2 → H3 (no level skips)
  - Horizontal rules (`---`) between major sections
- **Feature lists use bullets** (not prose)

**Rationale:** When a developer asks Claude "What is Precept?", Claude reads `README.md`. If the structure isn't AI-parseable, Claude gives shallow or hallucinated answers.

**Current violations:**
- Some code blocks lack language tags
- Image alt text: generic ("Interactive Inspector")
- Links use "here" pattern in some places

**Fix:** Tag all code blocks, update image alt text, use descriptive link text.

---

## Recommended Section Hierarchy

The proposal should use this structure as a starting point:

```
# Precept
[Logo + Badges]

> **Definition:** A general rule intended to regulate behavior or thought.

**Hook:** Precept is a domain integrity engine for .NET...

---

## Quick Example

**The Contract** (`time-machine.precept`):
[18-20 line DSL hero]

**The Execution** (C#):
[5-line runtime usage]

---

## Getting Started

### 1. Install the VS Code Extension
[Instructions + marketplace link]

### 2. Create Your First Precept File
[Follow hero example]

### 3. Integrate with Your C# Project
[`dotnet add package` + quickstart guide link]

---

## What Makes Precept Different

### AI-Native Tooling
[MCP + Copilot + LSP description]

### Unified Domain Integrity
[Replaces state machines + validation + rules]

### World-Class Developer Experience
[Bulleted list of features]

---

## Learn More

- [Documentation](link)
- [Language Reference](link)
- [Sample Catalog](link)
- [Contributing](link)

---

## License
[MIT badge + copyright]
```

**Key decisions:**
- Hero before installation (proves readability first)
- Getting Started as numbered sequence (single primary CTA)
- Differentiation after quickstart (progressive disclosure)
- Sample catalog in "Learn More" (reference, not onboarding)

---

## AI Parseability Checklist

The proposal author must verify:

- [ ] All code blocks tagged with language (` ```precept `, ` ```csharp `, ` ```bash `)
- [ ] Code blocks preceded by descriptive labels
- [ ] Descriptive link text (no "click here")
- [ ] Image alt text includes content description
- [ ] Heading hierarchy valid (H1 → H2 → H3, no skips)
- [ ] Emoji after heading text or removed
- [ ] Badge alt text includes version/status
- [ ] Feature lists use bullets, not prose
- [ ] Tables ≤3 columns (wider tables linked externally)
- [ ] Horizontal rules between major sections

**Validation:** An AI agent should be able to:
1. Answer "What is Precept?" from hook + definition
2. Extract hero code with language tag preserved
3. Navigate to installation using heading hierarchy
4. Identify primary CTA from Getting Started sequence
5. Understand AI tooling from structured list
6. Find links to docs without parsing complex prose

If any task fails, the structure needs revision.

---

## Viewport Testing Plan

Before approving the proposal, test at:

- **400px width** (phone portrait): Logo + hook + CTA visible above fold?
- **600px width** (phone landscape): Hero code readable without horizontal scroll?
- **800px width** (tablet): Full Getting Started section visible?

Current README fails all three tests.

---

## Next Steps

1. **Proposal author** uses these requirements to draft new README structure
2. **Shane + Peterman** review for brand compliance
3. **Steinbrenner** reviews for adoption journey mapping
4. **Elaine** validates against UX requirements checklist
5. **Implementation** once all four approve

These are **constraints**, not suggestions. The proposal can refine the execution, but it cannot violate these requirements.

---

# Elaine — Reviewer Corrections Applied to Brand Spec

**Date:** 2026-04-04  
**Status:** All corrections applied, ready for integration

---

## Summary

Three reviewers (George, Peterman, Frank) provided corrections to the 5-surface UX spec incorporated into `brand/brand-spec.html`. All corrections have been applied successfully.

---

## Corrections Applied

### From George (Technical)

1. **Diagnostic code range (line 632)**
   - **Issue:** Cited `PRECEPT001–PRECEPT047` instead of actual range `PRECEPT001–PRECEPT054` (54 constraints, not 47).
   - **Fix:** Updated example diagnostic code from `PRECEPT047` → `PRECEPT054`.
   - **Location:** `brand/brand-spec.html` §2.1 Diagnostics section.

2. **Inspector yellow NotApplicable state (line 788)**
   - **Issue:** Described "Warning (#FDE047) for unmatched guards" as one of the inspector's verdict states. The inspector does NOT show a yellow NotApplicable warning state — that outcome is filtered out entirely.
   - **Fix:** Removed reference to yellow warning for unmatched guards. Updated constraint violation description to "Blocked `#F87171` for rejected events or invariant violations."
   - **Location:** `brand/brand-spec.html` §2.3 Inspector Panel, Color application list.

3. **CLI surface describes non-existent tooling (§2.5)**
   - **Issue:** CLI surface spec described a `precept` CLI tool that does not exist. PRECEPT diagnostic codes are surfaced in VS Code Problems panel, not terminal output.
   - **Fix:** Changed section status from "LOCKED" to "ASPIRATIONAL". Added prominent callout at section start: "⚠️ CLI surface is planned; not yet implemented." Updated all prose to future tense and clarified this is a design contract for a future tool.
   - **Location:** `brand/brand-spec.html` §2.5 CLI / Terminal.

4. **Read-only fields mischaracterized (line 786)**
   - **Issue:** "Read-only" described as "fields that cannot be modified directly (computed, state-derived)." The DSL has no computed or state-derived field types. Read-only means editability-per-state.
   - **Fix:** Updated to "fields not declared editable in the current state via `in State edit Field`".
   - **Location:** `brand/brand-spec.html` §2.3 Inspector Panel, Color application list.

### From Peterman (Brand)

5. **State Diagram — off-system colors (lines 658, 666, 674, 739–750)**
   - **Issue:** State lifecycle roles (initial/intermediate/terminal) used three off-system hex values: `#A5B4FC`, `#94A3B8`, `#C4B5FD`. Event subtypes used off-system cyan shades: `#38BDF8`, `#7DD3FC`, `#0EA5E9`. This directly contradicted the "no lifecycle tints" principle stated in the same section.
   - **Fix:** Removed ALL off-system hex values. Replaced with locked system colors:
     - **States:** All use `#6366F1` (brand indigo) for borders, `#A898F5` (violet) for state names. Shape (circle, rounded rect, double-border) encodes lifecycle role.
     - **Events:** All use `#30B8E8` (locked cyan). Edge styling (solid, dashed, self-loop) differentiates event subtypes.
     - Added explicit callout: "No lifecycle tints: States are not tinted by role. Shape carries that signal."
   - **Location:** `brand/brand-spec.html` §2.2 State Diagram, visual samples and lifecycle tables.

### From Frank (Architectural)

6. **CLI surface — aspirational (same as George #3)**
   - **Issue:** Same as George's correction #3.
   - **Fix:** Applied (see George #3 above).

7. **Docs surface terminology**
   - **Issue:** Flagged potential confusion between "docs site" (public website) vs. internal `docs/` artifacts.
   - **Verification:** Reviewed §2.4 — already correctly described as internal team artifacts, not a public site. No changes needed.
   - **Location:** `brand/brand-spec.html` §2.4 Docs Surface.

8. **State Diagram — InitialState gap (§2.2)**
   - **Issue:** `PreceptPreviewSnapshot` does not currently expose `InitialState`, so the state diagram renderer cannot identify the initial state from the snapshot alone.
   - **Fix:** Added TODO callout in §2.2: "Protocol gap — TODO: `PreceptPreviewSnapshot` does not currently expose `InitialState`. Initial state highlighting in diagrams depends on adding this field to the protocol."
   - **Location:** `brand/brand-spec.html` §2.2 State Diagram, after state lifecycle roles table.

---

## Sections Affected

All corrections applied to:
- **§2.1 Syntax Editor** — Diagnostic code range updated
- **§2.2 State Diagram** — Off-system colors removed, locked colors enforced, InitialState gap documented
- **§2.3 Inspector Panel** — Yellow NotApplicable state removed, read-only field semantics corrected
- **§2.4 Docs Surface** — Verified correct (no changes needed)
- **§2.5 CLI / Terminal** — Marked as aspirational, added non-existent tool callout

---

## Remaining Open Items

**None.** All reviewer feedback addressed.

---

## Next Steps

1. Shane reviews corrected brand-spec.html §2.1–2.5
2. If approved, sections are formally locked
3. Implementation can proceed on editor (§2.1), diagram (§2.2), and inspector (§2.3)
4. CLI surface (§2.5) remains aspirational until tool is scoped

---

**Signed:** Elaine  
**Date:** 2026-04-04

---

# Semantic Reframe Review — Elaine's Verdict

**Author:** Elaine (UX/Design)
**Date:** 2025-07-17
**Requested by:** Shane
**Status:** APPROVED WITH REMAINING CHANGES

---

## Verdict: APPROVED WITH REMAINING CHANGES

The brand-spec has made major structural progress. The family cards are in §2.1, §2.2 has a complete diagram color mapping, cross-references are clean, and the semantic family reference table in §1.4 is exactly the 5+3 story Shane wants. But the section's **framing** still wraps that table inside a "brand palette" narrative that hasn't been rewritten to match the clarified 5+3 model, and `brand-light`/`brand-muted` remain in the general story.

---

## Focus Area Assessment

### 1. §1.4 — General and non-repetitive?

**Partially.** The semantic family reference table (lines 497–545) is correct — 5 families (Indigo, Violet, Cyan, Bright Slate, Gold) + Verdicts (Green, Red, Yellow). This IS the 5+3 system.

**Remaining issue:** The intro paragraph (line 353) still frames §1.4 as "the brand palette — 12 colors across brand, text, structural, semantic, and syntax-accent families." Shane's clarification says §1.4 should describe **only the general semantic color system**. The 8+3 palette card (brand/brand-light/brand-muted + text tiers + structural tokens) appears before the semantic family reference table and dominates the section's narrative. The conceptual order should lead with the 5+3 semantic system, with the brand palette tokens positioned as infrastructure rather than the headline story.

**Also:** The semantic family reference table leaks surface-level shade detail. Structure shows two hexes (#4338CA + #6366F1); Data shows three (#B0BEC5 + #9AA8B5 + #84929F). At the identity level, each family should have a single representative hex or a family range, with the shade breakdown deferred to §2.1. This is minor but makes the "general, not syntax-specific" claim less clean.

### 2. `brand-light` / `brand-muted` — Gone from the general story?

**No.** Both still appear:
- In the 8+3 palette card (lines 390–403) as equal members of the Brand Indigo group
- In §1.4.1 Color Roles table (lines 564–571) with general cross-surface usage descriptions
- In §1.4.1 Q&A (line 673) — actively recommended as general tools ("I need a secondary palette highlight")

Shane's clarification: these "should disappear unless a specific surface truly needs a local tonal variant." They haven't disappeared. They haven't even been demoted or flagged as local variants.

**What to do:** Remove brand-light and brand-muted from the §1.4 narrative and §1.4.1 general guidance. If a specific surface (e.g., docs, hover states) genuinely needs them, declare them there as a local tonal variant of the Indigo family, not as general system colors.

### 3. §1.4.1 — Still too repetitive?

**Somewhat.** The Color Roles table (lines 555–617) re-lists all 12 colors from the 8+3 card with role name, hex, and swatch — then adds a "Specific Uses" column. The only new information per row is that column. A reader who just scrolled past the 8+3 card sees the same swatches again.

**Improvement from prior state:** The intro now properly positions §1.4.1 as cross-surface guidance, and the brand-light row correctly notes that state names use Violet #A898F5 (line 565). Cross-references to §2.1 and §2.2 are clean.

**Remaining fix:** Trim the table to a "Specific Uses" reference — the reader already knows the swatches and hexes from the card above. Or collapse to a compact list that says "brand: wordmark, section headers, CTA buttons, diagram borders" without re-presenting the swatch.

### 4. §2.1 — Extra shades as local surface variants?

**Yes — this is correct.** ✅

The family cards in §2.1 (lines 729–830) use shading variants within families for syntax clarity: two indigos for Structure (Semantic vs Grammar), three slates for Data (Names, Types, Values). These are properly framed as syntax-surface implementation decisions. The opening paragraph (line 724) credits §1.4 for the identity-level families. This matches Shane's "§2.1 may use shading variants within a family as needed for syntax clarity."

### 5. §2.2 — Follows 5+3 logic, no extraneous colors?

**Yes — this is correct.** ✅

The diagram color mapping table (lines 1012–1030) and runtime verdict overlay (lines 1032–1086) use only colors from the 5 semantic families (Indigo, Violet, Cyan, Slate) + 3 outcome colors (Green, Red, Yellow) + neutral UI tokens (bg, text-muted) for canvas and legend. No extraneous colors. The SVG legend blocked line now uses #FB7185 (line 985), resolving the prior hex discrepancy.

The open decisions (current state indicator, lines 1088–1096) are properly flagged as awaiting Shane sign-off.

---

## Summary of Remaining Changes

| # | Issue | Severity | Section |
|---|-------|----------|---------|
| 1 | §1.4 intro still frames section as "brand palette" story, not "5+3 semantic system" story | Medium | §1.4 intro |
| 2 | `brand-light` and `brand-muted` still appear as general system colors in the palette card and §1.4.1 | Medium | §1.4, §1.4.1 |
| 3 | Semantic family reference table exposes surface-level shade counts (2 hexes for Structure, 3 for Data) | Low | §1.4 table |
| 4 | §1.4.1 Color Roles table re-lists all 12 swatches from the card above | Low | §1.4.1 |
| 5 | §1.4.1 Q&A still actively recommends brand-light/brand-muted as general tools | Medium | §1.4.1 Q&A |

Items 1, 2, and 5 directly contradict Shane's clarified feedback. Items 3 and 4 are polish but reinforce the wrong framing.

---

## What's Already Right

- ✅ Family cards properly moved to §2.1
- ✅ §2.1 treats shade variants as local surface decisions
- ✅ §2.2 diagram color mapping is complete and 5+3-compliant
- ✅ §2.2 SVG hex discrepancy fixed (#FB7185)
- ✅ Semantic family reference table exists with correct 5+3 content
- ✅ Cross-references between §1.4, §2.1, and §2.2 are clean
- ✅ Constraint signaling consolidated in §2.1 (no §1.4 duplicate)
- ✅ Open diagram decisions properly called out as awaiting Shane

---

# Visual Surfaces UX Specifications — Incorporated into Brand Spec

**Date:** 2026-04-04  
**Author:** Elaine (UX Designer)  
**Status:** Locked  
**Affects:** `brand/brand-spec.html` §2.3, §2.4, §2.5; `brand/explorations/visual-language-exploration.html`

## Decision

The 5-surface visual UX spec draft (previously in `brand/visual-surfaces-draft.html`) has been incorporated into `brand/brand-spec.html` as three fully specified, locked sections:

- **§2.3 Inspector Panel** — Runtime verdict surface with complete color, typography, accessibility, and AI-first specifications
- **§2.4 Docs** — Internal documentation artifacts (clarified scope: team-facing, not a public docs site)
- **§2.5 CLI / Terminal** — Command-line output with verdict color usage, symbol redundancy, and terminal compatibility constraints

All three sections are marked **LOCKED** and serve as implementation contracts for current and future work.

## Open Questions Resolved

1. **Inspector panel status:** Confirmed as fully implemented. Spec describes current behavior with brand color alignment recommendations.
2. **"Docs site" scope:** Clarified as internal team documentation artifacts (`docs/` folder), not a public-facing website. If Peterman designs a public docs site in the future, that's a separate surface.
3. **Light theme:** Not planned. Marked as backlog item across all surfaces. Current system is dark-mode-only.
4. **Accessibility audit:** Formal color-blind simulation and screen reader testing is a backlog item. Contrast ratios are documented; redundancy principles (color + shape/symbol) are locked.
5. **CLI color audit:** Current CLI tools need an audit pass to ensure compliance with locked spec. Marked as backlog item.

## Color Compliance Fixes

All instances of `#475569` (Tailwind slate-600, NOT in the locked 8+3 color system) were replaced with correct system colors in brand mark SVGs:

| Element | Old Color | New Color | System Role |
|---------|-----------|-----------|-------------|
| Document outline/border (combined icon) | `#475569` | `#27272A` | border |
| Document content lines (combined icon) | `#475569` | `#27272A` | border |
| Document content lines (tablet icon) | `#475569` | `#71717A` | text-muted |
| Inactive/destination state circle | `#475569` | `#27272A` | border |
| Color key label | "Slate #475569" | "Border #27272A" | — |
| Badge backgrounds (visual-language-exploration.html) | `#475569` | `#27272A` | border |

**Files affected:**
- `brand/brand-spec.html` §1.3 (brand marks), §1.4 (indigo overview card)
- `brand/explorations/visual-language-exploration.html` (brand marks, badges)

## Rationale

### Why these color replacements?

`#475569` is Tailwind's slate-600 — a pre-built utility shade that was never part of Precept's locked 8+3 system. The locked system includes:

**8 authoring shades:**
- Indigo structure: `#4338CA` (semantic), `#6366F1` (grammar)
- Violet states: `#A898F5`
- Cyan events: `#30B8E8`
- Slate data: `#B0BEC5` (names), `#9AA8B5` (types), `#84929F` (values)
- Gold messages: `#FBBF24`

**3 runtime verdict colors:**
- Enabled: `#34D399` (emerald)
- Blocked: `#F87171` (coral)
- Warning: `#FDE047` (yellow)

**Plus structural/UI neutrals:**
- Background: `#09090B` (bg)
- Text: `#E5E5E5` (text), `#A1A1AA` (text-secondary), `#71717A` (text-muted)
- Border: `#27272A` (border)

The brand marks were using a color outside this system. The replacements bring the marks into compliance:

- **`#27272A` (border)** is the correct choice for structural elements like document outlines, inactive state circles, and borders. It's the locked neutral for "this is a container, not content."
- **`#71717A` (text-muted)** is appropriate for secondary visual elements like content lines in the tablet icon — they're not primary structure, but they're not invisible either.

### Why lock the surface specs now?

1. **Implementation exists.** The syntax editor, state diagram, and inspector panel are all implemented. The specs describe current behavior and establish the contract for future changes.
2. **No guesswork left.** Every color hex, every typography rule, every accessibility requirement is now explicit. Kramer doesn't have to infer intent; Peterman doesn't have to reverse-engineer decisions.
3. **Brand compliance gate.** With locked specs, any UI artifact can be reviewed against its corresponding section. If a surface exists, it has a locked spec. If it doesn't have a spec, it shouldn't be implemented yet.
4. **AI-first by design.** Each spec includes an "AI-first note" explaining how the design serves both human and AI consumers. This is fundamental to Precept's positioning — the surfaces aren't just for developers, they're for AI agents authoring, inspecting, and reasoning about precepts.

## Impact

- **For Kramer:** Implementation contract. Every surface has explicit color/typography/accessibility rules.
- **For Peterman:** Brand compliance reference. Surface-specific application of brand decisions.
- **For Shane:** Decision record. Future changes require spec updates, not just code changes.
- **For AI agents:** Structured design knowledge. Each surface spec includes AI consumption notes.

## Follow-Up Work

1. **CLI color audit** — Verify current CLI tools (dotnet build, language server diagnostics, precept CLI) align with §2.5 spec
2. **Accessibility audit** — Formal color-blind simulation and screen reader testing on all surfaces
3. **Light theme exploration** — If/when light theme support is planned, color recalibration (especially verdict colors) will be required
4. **Inspector panel brand alignment** — Review Kramer's implementation against §2.3 spec; recommend CSS-only color remapping if drift exists

## Related Files

- `brand/brand-spec.html` — Canonical source of truth, §2.3–2.5 now locked
- `brand/visual-surfaces-draft.html` — Draft file now superseded; can be archived or deleted
- `.squad/agents/elaine/history.md` — Session 3 entry documents this work

---

**Locked by:** Shane (via Elaine)  
**Locked on:** 2026-04-04

---

# Decision: brand-spec §1.4 Palette Structure Refactor

**Filed by:** Frank (Lead/Architect)
**Date:** 2026-04-07
**Type:** Information Architecture
**Affects:** brand-spec.html §1.4, §1.4.1, §2.1
**Full review:** `brand/references/brand-spec-palette-structure-review-frank.md`

---

## Problem

§1.4 Color System contains two distinct palettes at different abstraction levels:

1. **Brand palette card** ("Precept Color System · 8 + 3") — foundational UI tokens: brand, text, structural, semantic. General-purpose.
2. **Syntax family cards** (Structure · Indigo through Constraint Signaling) — editor-specific token-to-color mappings with typography rules and keyword lists.

The section intro says "8 authoring-time shades" but the brand palette card has a different set of 8 (UI tokens). The actual 8 syntax shades are in the family cards. This "two different eights" collision confuses readers. The constraint signaling table is duplicated between §1.4 and §2.1.

## Recommendation

**Move syntax family cards from §1.4 to §2.1.** Replace them in §1.4 with a brief Semantic Family Reference table (one row per family, conceptual identity only — no hex/typography/keyword detail). Update the §1.4 intro to describe brand tokens + semantic families, not syntax shades.

**Result:**
- §1.4 = brand token definitions + semantic family identities (what colors *mean*)
- §1.4.1 = cross-cutting usage roles matrix (unchanged, minor trim of duplicate rationale)
- §2.1 = complete syntax color reference (what colors *do* in the editor)

No content is lost. No locked values change. The restructure separates identity from implementation, matching the rest of the document's §1 (identity) → §2 (surfaces) architecture.

## Owner

J. Peterman (brand-spec maintainer) with Elaine review.

## Status

PENDING — awaiting Shane sign-off.

---

# Decision: README Restructure Proposal — Architectural Approval

**Filed by:** Frank (Lead/Architect)  
**Date:** 2026-04-06  
**Status:** APPROVED WITH REQUIRED CHANGES — Pending Shane sign-off  
**Proposal:** `brand/references/readme-restructure-proposal.md`  
**Full review:** `brand/references/readme-restructure-review-frank.md`

---

## Decision

The README restructure proposal (Peterman, 2026-04-05) is architecturally approved with four required changes. The narrative architecture — prove → trial → differentiate → reference — is correct for a category-creation README. No implementation begins until the required changes are addressed and Shane signs off.

## Required Changes Before Rewrite

1. **RC-1: Fix C# API call chain** — The hero C# block spec lists the wrong API sequence. Must use `PreceptParser.Parse → PreceptCompiler.Compile → engine.CreateInstance → engine.Fire` per `docs/RuntimeApiDesign.md`.
2. **RC-2: Getting Started step 3 title inconsistency** — Two different titles used in the same proposal. Pick one.
3. **RC-3: Rename "Language Server and Preview"** — Implementation label, not user-facing. Use a capability-describing heading (e.g., "Live Editor Experience").
4. **RC-4: Remove "without human intervention" claim** — Overstates current AI tooling capability. Replace with factual statement about structured tool APIs.

## Gate Enforcement

Per the design gate protocol: no README rewrite implementation starts until Shane explicitly approves the revised proposal. Frank's architectural approval alone is not sufficient.

## Impact

- **No code changes.** This is a documentation restructure.
- **Downstream:** Once the rewrite ships, the README becomes the new source-of-truth narrative for the project. All brand, tooling, and feature claims must be verifiable from the codebase.
- **MCP/AI surface:** The restructured README improves AI agent comprehension via semantic headings, language-tagged code blocks, and structured bullet lists. This aligns with the AI-first design principle.

---

## Frank — Architecture Review: Elaine's 5-Surface UX Spec
**Date:** 2026-04-04
**Status:** Needs revision before implementation

---

### Surface-by-surface notes

#### Surface 1: Syntax Editor

Architecturally sound. The spec accurately describes what the language server delivers: semantic tokens with `preceptConstrained` modifier for constrained states/events/fields, 8-shade authoring palette, hover, diagnostics. The `preceptConstrained` modifier is implemented and emitted in `PreceptSemanticTokensHandler.cs` — the italic rendering Elaine describes is real, not aspirational.

One precision gap: "inline constraint indicators" is ambiguous. The language server produces diagnostic underlines and semantic token modifiers, not a separate inline indicator layer. If the spec means those two mechanisms, it's correct. If it implies a third display channel, that doesn't exist. Clarify in the final spec.

The AI-first note (syntax highlighting as machine-readable metadata) is architecturally correct and reflects how MCP tools consume semantic token data.

**Verdict: Approved with notation.**

---

#### Surface 2: State Diagram

**Protocol gap — blocker.**

`PreceptPreviewSnapshot` (the protocol record the webview receives) exposes `States` as `IReadOnlyList<string>` — a flat list with no role metadata. `PreceptEngine.InitialState` exists but is NOT included in the snapshot sent to the webview. The spec calls for initial states to be distinguished by shape and color. The diagram renderer cannot determine which state is initial from the snapshot alone. It would have to guess from the state name or run a secondary query — neither is acceptable.

What works: `PreceptPreviewTransition` includes `From`, `To`, `Event`, `GuardExpression`, and `Kind` — everything the spec needs for edge labeling, guard display, and event sub-shading is there. Terminal states can be derived by finding states with no outgoing transitions in the `Transitions` list.

Fix required before implementation: add `InitialState` to `PreceptPreviewSnapshot`. This is a one-field protocol addition, not a runtime change.

**Verdict: Needs one protocol fix before implementation can start.**

---

#### Surface 3: Inspector Panel

Mostly aligned. The protocol is implemented and the data model covers the core of Elaine's spec:
- Field names, types, values, current state → covered by `PreceptPreviewSnapshot.Data` + `PreceptPreviewEditableField`
- Constraint violations on fields → `PreceptPreviewEditableField.Violation`
- Event argument contracts → `PreceptPreviewEventArg` (name, type, nullable, default)
- Enabled/blocked event status → `PreceptPreviewEventStatus.Outcome`

Two architectural gaps worth flagging:

**Gap 1 — Field change delta.** The spec says "before and after state, with visual indication of which fields changed." The protocol delivers a complete replacement snapshot — no delta, no `PreviousData` field. To show which fields changed, the webview must cache the pre-fire snapshot and diff it client-side. This works but it's client logic, not protocol-supported. It's a warning, not a blocker, because client-side diffing is reasonable here.

**Gap 2 — Violation structure loss.** `PreceptPreviewEditableField.Violation` is a `string?` — a single flattened message. The underlying `ConstraintViolation` type carries structured targets (which fields/args/events are implicated). That structure is lost at the protocol boundary. For simple coloring, a string is enough. If the UI ever needs to cross-highlight multiple implicated fields from a single constraint, the protocol can't support it without a breaking change. Flag for future consideration.

**Gap 3 — Read-only field rendering.** The spec describes "read-only field indicators" as a separate visual affordance. In the current model, editability is implied by presence in `EditableFields` — non-editable fields appear only in `Data`. The webview must infer read-only status by set subtraction. This is workable but should be explicit in the webview implementation spec.

**Verdict: Approved with warnings. Note gaps before implementation.**

---

#### Surface 4: Docs Surface

**Terminology error — must be corrected.**

Elaine has designed a public-facing documentation *website* — "responsive layout," "mobile (stacked layout)," "docs site" — complete with a future delivery timeline. That is not what this surface is. In this project, "docs" means `docs/*.md` — internal design and architecture documents. There is no documentation website planned, no docs pipeline, no docs tooling.

Elaine herself flags this with "scope clarification needed," so she knows something is off. The answer is: this is not a product surface. There is no docs site architecture to review. The spec section should be retitled "Internal Docs" or struck from the visual surfaces document entirely. If Shane ever greenlights a public docs site, that gets its own design gate from scratch.

The AI-first note in this section (structured HTML, code blocks in `<pre>`, heading hierarchy for agent parsing) is well-reasoned for a docs site that *did* exist. File it away for when that surface is actually in scope.

**Verdict: Not a current product surface. Retitle or remove from this spec.**

---

#### Surface 5: CLI / Terminal

**Scope error — no such surface exists.**

The spec describes a "precept CLI" with `--json` output flags, custom colored diagnostic output, and build logging with brand verdict colors. None of this exists. There is no standalone `precept` CLI tool. The runtime is a library. The language server is an in-process VS Code extension host — it sends diagnostics to VS Code's Problems panel, not to a terminal stream. `dotnet build` output uses MSBuild formatting, not custom brand colors.

The fictional `--json` flag in the AI-first note is a concrete error — it implies an interface that doesn't exist and could mislead implementation agents.

What *is* real and terminal-visible: `dotnet build` output (standard MSBuild, not brand-colored), `dotnet test` output (standard xUnit), and MCP tool responses (JSON over stdio, invisible to human terminal users).

If a `precept` CLI tool is ever designed, that requires a full architectural spec and a new runtime interface — it cannot be assumed from the current engine API.

**Verdict: Reject this surface as currently specified. Either (a) remove it, or (b) retitle it "Diagnostic Output / VS Code Problems Panel" and redesign around what actually exists.**

---

### Blockers

1. **`PreceptPreviewSnapshot` missing `InitialState`** — State diagram cannot distinguish the initial state without this field. Protocol change required before any diagram rendering implementation begins. This is a one-field addition to `PreceptPreviewProtocol.cs` and `PreceptPreviewHandler.cs`.

2. **CLI surface describes a non-existent product surface** — The "precept CLI" tool does not exist. Any agent implementing against this spec would be building dead code or making false assumptions about the runtime API. Must be resolved (remove or replace) before implementation.

3. **Docs surface is misclassified** — Describing an aspirational public docs site as a current visual surface conflates product aspiration with design contract. Must be corrected before this spec is integrated into `brand-spec.html`.

---

### Warnings

1. **Inspector: no field change delta in protocol** — "Visual indication of which fields changed" requires client-side diffing. Acceptable, but the webview implementation spec needs to make this explicit so Kramer doesn't expect a protocol delta.

2. **Inspector: `Violation` is string, not structured** — `PreceptPreviewEditableField.Violation` stringifies what the engine produces as a structured `ConstraintViolation`. This loses multi-target attribution. Fine for now, but any future "cross-highlight implicated fields" feature requires a protocol change.

3. **Inspector: read-only field rendering requires set subtraction** — Non-editable fields live in `Data`, editable fields live in `EditableFields`. The webview computes read-only status by exclusion. Not a protocol gap, but the UI implementation must understand this model explicitly.

4. **Editor: "inline constraint indicators" is ambiguous** — Could mean diagnostics (squiggles), semantic token modifiers (color/italic), or a third thing. Clarify before implementation so Kramer builds the right affordance.

5. **Docs AI-first note is orphaned but valuable** — The structured content notes (heading hierarchy, `<pre>` blocks, definition lists for AI parsing) are worth keeping somewhere. When a docs site is ever scoped, this belongs in that spec.

---

### Overall verdict

The inspector panel and editor surface specs are solid and grounded in the real runtime; they need minor clarifications, not redesigns. The state diagram spec has a concrete protocol gap (missing `InitialState` in the snapshot) that must be fixed before implementation starts. The CLI and docs surfaces are both misaligned with what the product actually ships — one is fictional, one is mislabeled — and must be corrected before this spec is merged into `brand-spec.html`. Fix the blockers, then implementation can proceed on editor, inspector, and diagram.

---

## George — InitialState Protocol Fix
**Date:** 2026-04-04
**Status:** Complete

---

### Problem

Frank's architecture review of Elaine's 5-surface UX spec identified a **blocking protocol gap** preventing state diagram implementation:

> `PreceptPreviewSnapshot` (the protocol record the webview receives) exposes `States` as `IReadOnlyList<string>` — a flat list with no role metadata. `PreceptEngine.InitialState` exists but is NOT included in the snapshot sent to the webview. The spec calls for initial states to be distinguished by shape and color. The diagram renderer cannot determine which state is initial from the snapshot alone. It would have to guess from the state name or run a secondary query — neither is acceptable.

(Source: `.squad/decisions/inbox/frank-surfaces-review.md` § Surface 2)

### Solution

Added `InitialState` property to `PreceptPreviewSnapshot` and populated it from the existing `PreceptEngine.InitialState` field.

**Files changed:**
1. `tools\Precept.LanguageServer\PreceptPreviewProtocol.cs` (line 33) — added `string InitialState` positional parameter after `CurrentState`
2. `tools\Precept.LanguageServer\PreceptPreviewHandler.cs` (line 296) — populated `session.Engine.InitialState` in `BuildSnapshot()`

**Build/test status:**
- Language server build: ✓ succeeded (6.6s)
- Language server tests: ✓ 84/84 passed (1.6s)
- No regressions

### Downstream Impact

**Immediate consumers:**
- **Inspector webview** (`tools\Precept.VsCode\webview\inspector-preview.html`): Already reads `snapshot.CurrentState` at line 1387. `InitialState` now available but not yet consumed. No changes needed to existing UI — field is additive.

**Blocked work now unblocked:**
- **Kramer (Language Server / Webview Dev)**: State diagram renderer can now be implemented per Elaine's visual spec. `InitialState` provides the metadata needed to distinguish the initial state with a double-circle border and contrasting color as the spec requires.

**Not affected:**
- **Newman's MCP tools**: MCP tools in `tools\Precept.Mcp\Tools\` do not reference `PreviewSnapshot` — they use core engine types directly. No changes needed.

### Design Gate

This change was approved as part of Frank's architectural review and Shane's approval of the fix plan. It is a **one-field additive protocol change** with no impact on:
- DSL syntax or semantics
- Parser, tokenizer, type checker
- Runtime execution engine
- Constraint evaluation

The underlying `PreceptEngine.InitialState` property already existed and was already populated during compilation from `model.InitialState.Name`. This fix exposes existing data to consumers — no new logic, no new semantics.

Per charter, this qualifies as a non-design-gated fix because it's a small protocol addition explicitly called out in an approved architectural review.

### Notes

- Protocol is positional record type — downstream consumers using named construction will not break (C# record semantics).
- JSON serialization is automatic (OmniSharp LSP infrastructure) — `InitialState` will appear in webview responses immediately.
- No documentation update needed — preview protocol types in `PreceptPreviewProtocol.cs` serve as implementation documentation. (Design docs like `EditableFieldsDesign.md` and `RulesDesign.md` reference the protocol in context but don't document the full structure.)

---

# Runtime Review: README Restructure Proposal — Three Required Changes

**Filed by:** George (Runtime Dev)  
**Date:** 2026-04-06  
**Type:** Required changes — gates on rewrite begin  
**Full review:** `brand/references/readme-restructure-review-george.md`  
**Proposal:** `brand/references/readme-restructure-proposal.md`  
**Frank's review:** `brand/references/readme-restructure-review-frank.md`

---

## Status

CONDITIONALLY APPROVED — same verdict as Frank's review, plus three required changes from the runtime domain.

---

## Decision Summary

Frank's RC-1 fix direction is correct but contains a factual error that must be resolved before the copy brief is issued. Two additional required changes are new findings from runtime and documentation review.

---

## G1: Remove `RestoreInstance` — It Doesn't Exist

**Corrects:** Frank's RC-1 addendum  
**Severity:** Must fix before copy brief is issued  

Frank's RC-1 fix reads: "Include `RestoreInstance` as an alternative to `CreateInstance`." This is wrong. `RestoreInstance` is not a real method. It does not exist in `PreceptEngine` or anywhere in the public API.

Verified against `src/Precept/Dsl/PreceptRuntime.cs`: the only relevant methods are two overloads of `CreateInstance`:
- `engine.CreateInstance()` — new entity at InitialState
- `engine.CreateInstance(string state, IDictionary<string, object?> data)` — restore from database

The correct restore pattern is `engine.CreateInstance(savedState, savedData)`. There is no `RestoreInstance`.

**Required action:** Remove `(or RestoreInstance)` from the proposal's C# Block Specification. Update Frank's RC-1 fix guidance to reflect that the second overload of `CreateInstance` is the restore pattern. Any copy brief that propagates `RestoreInstance` will produce a README with a nonexistent API call.

---

## G2: Add .NET SDK Prerequisite to Getting Started

**New finding**  
**Severity:** Must fix before rewrite — first-run correctness gap  

The VS Code language server is a .NET 10 process. A developer who installs the VS Code extension without .NET installed gets no language features — no diagnostics, no completions, no hover. No error message. Silent failure.

The proposal's Getting Started section drops the Prerequisites section that exists in the current README ("Prerequisites: .NET 10 SDK — required for both the language server and MCP tools").

Steinbrenner's research requires a working first-run path. A developer who follows Getting Started Step 1 without .NET installed cannot get to a working state. The prerequisite must be restored.

**Required action:** Add a .NET 10 SDK prerequisite before or within Getting Started Step 1. One sentence is sufficient: "The language server requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) — install it first if you don't have it."

---

## G3: Soften "Replacing Three Separate Libraries" in the Hook

**New finding**  
**Severity:** Required before rewrite — adoption expectation risk  

The proposed hook reads: "Precept unifies state machines, validation, and business rules into a single DSL — **replacing three separate libraries with one contract**."

"Replacing" implies a drop-in substitution path. Precept requires adopting a new execution model — entities are governed entirely by the DSL contract. Developers who read "replacing" will attempt to swap out Stateless/FluentValidation/NRules piecemeal and find it doesn't work that way. Churn follows.

The brand voice requirement is "declarative, no hedging, no overclaiming." "Replacing" overclaims substitutability.

**Required action:** Change "replacing three separate libraries with one contract" to "eliminating the fragmentation that comes from managing them separately" or similar. Retains category-creation force without implying a drop-in path.

---

## Endorsements

Frank's RC-2 (title inconsistency in Step 3), RC-3 (rename Section 4c heading), and RC-4 (soften AI autonomous authoring claim) are all correct. No changes from me on those.

Frank's RC-4 fix suggestion — "AI agents can validate, inspect, and iterate on `.precept` files through structured tool APIs" — is accurate and should be adopted.

---

*George*

---

## George — Technical Review: Elaine's 5-Surface UX Spec
**Date:** 2026-04-04
**Status:** Accurate with notes

---

### Surface-by-surface notes

#### Surface 1: Syntax Editor
Mostly accurate. The 8-shade palette description matches the locked system. Bold/italic typography signal (bold = structure semantic, italic = constrained actors) is correctly described and is what the semantic tokens handler implements.

**One factual error:** She cites the diagnostic code range as **PRECEPT001–PRECEPT047**. The actual range is **PRECEPT001–PRECEPT054** (C1–C54 — 54 constraints, not 47). This appears in her `history.md` surfaces inventory section (under the VS Code Extension heading) and should be corrected.

Everything else under this surface — semantic tokens, hover, completions, go-to-definition, italic constraint signaling — is consistent with what the language server and runtime actually provide.

---

#### Surface 2: State Diagram
The lifecycle model (initial / intermediate / terminal) is technically accurate as a conceptual description. **Important nuance:** "terminal" is not a first-class DSL keyword. Terminal states are inferred at analysis time by the analysis pass (`PreceptAnalysis.cs`) — states with no outgoing transitions to other states. The DSL grammar only has `state <Name> initial` for the initial marker; "terminal" is a computed property. Elaine's diagram description won't mislead a designer, but the implementation team should know "terminal" isn't declared — it's detected.

The event sub-categories (transition / conditional / stationary) used in the diagram sub-shading are accurate:
- Transition = always moves state
- Conditional = guarded row, uncertain
- Stationary = no state change (AcceptedInPlace)

Color and shape redundancy claims are accurate to the locked brand decisions.

---

#### Surface 3: Inspector Panel
**Answer to open question:** YES, the inspector is fully implemented. `tools/Precept.VsCode/webview/inspector-preview.html` is a complete, working webview. Elaine's brand drift finding is correct (Segoe UI, custom colors — see her `inspector-panel-review.md`).

**What the inspector actually shows:**
- State diagram (SVG) with current state highlighted
- Event list for current state with fire forms and event argument inputs
- Field data list with values
- Inline field edit mode (not a modal — fields are editable directly in the data list with constraint violation messages inline)
- Nullable argument toggle (null button per arg, already implemented)

**Technical inaccuracy — event outcomes:** Elaine describes "Warning (#FDE047) for unmatched guards" as one of the inspector's three verdict states. This is wrong for the inspector surface specifically. The inspector event bar has **four** states:
1. `enabled` → green (#1FFF7A) — event will fire and change state
2. `noTransition` → green/dimmed (#1FFF7A at 72% opacity) — event fires in-place, no state change (AcceptedInPlace outcome)
3. `blocked` → red (#FF2A57) — rejected
4. `undefined` → red/dimmed (#FF2A57 at 72% opacity) — no row exists for this event from current state

The `notApplicable` outcome (all guards fail) is **filtered out** of the event bar entirely — it does not render at all, let alone as yellow. There is currently no yellow/warning state in the inspector event list. If the brand spec requires a yellow "NotApplicable" state in the inspector, that's a new implementation requirement — not current behavior.

**"Before and after" framing:** Her spec says the inspector shows "before and after state, with visual indication of which fields changed." This is slightly aspirational. The inspector shows the current state (after firing), a toast confirmation message, and updated field values — but there's no explicit side-by-side before/after comparison or field diff highlighting. Fields update in place.

**"Computed, state-derived" read-only fields:** The DSL doesn't have computed or state-derived field types. All fields are declared scalar or collection fields. What controls editability is `in State edit Field` — fields not declared editable in the current state are locked in the inspector. The "read-only" concept is real, but the framing as "computed/state-derived" is inaccurate — it's editability-per-state, not field type.

---

#### Surface 4: Docs Site
**Naming issue:** This surface is called "Docs Site" throughout, implying a public documentation website. We don't have one. The `docs/` folder is internal design documents and architecture records — not a public-facing site. Whether a docs site is planned is a Shane-level question, but the current naming treats an aspirational future artifact as a live surface. If this goes into `brand-spec.html`, it should be marked clearly as a future/planned surface, not a present one.

The visual guidelines Elaine wrote for this surface (syntax highlighting consistency in embedded code samples, indigo navigation accents, responsive layout) are sound if/when a site is built — no technical objections to the design principles.

---

#### Surface 5: CLI / Terminal
**Significant issue:** There is no standalone `precept` command-line tool. Elaine writes: "Developers see this surface when running `dotnet build`, invoking the language server, or checking `precept` command-line tools." That last phrase implies a dedicated `precept` CLI exists — it doesn't. The diagnostic toolchain is:

- `dotnet build` → standard MSBuild output (no PRECEPT-coded messages in the build output)
- Language server → surfaces PRECEPT diagnostics in VS Code's Problems panel (not the terminal)
- MCP server (`precept_compile`, etc.) → JSON API, not terminal output

PRECEPT diagnostic codes (PRECEPT001–PRECEPT054) are currently only surfaced in the VS Code Problems panel via the language server. They don't appear in `dotnet build` output. The CLI surface as Elaine describes it — with structured error formatting, `✗ ERROR: PRECEPT042` lines, and verdict colors — describes a tool that doesn't exist yet.

The color principles she defines (verdict colors + symbol redundancy, no structural indigo in terminals, default terminal color for file paths) are all technically sound as a design contract for a future CLI tool. But the spec should be honest that this surface is aspirational, not current.

---

### Corrections needed

1. **Diagnostic code range:** `PRECEPT001–PRECEPT047` → `PRECEPT001–PRECEPT054`. 54 constraints registered (C1–C54).

2. **Inspector — event outcome states:** The spec says three verdict states: enabled / blocked / warning (for unmatched guards). In the inspector, `notApplicable` is filtered out of the UI, not shown as yellow warning. The actual four states are: `enabled` (green), `noTransition` (green dimmed), `blocked` (red), `undefined` (red dimmed). Yellow/warning is not currently used in the event list. If yellow for NotApplicable is a brand requirement, it needs to be added as a new implementation ticket for Kramer.

3. **Inspector — "before and after" / field diff:** No explicit before/after comparison view exists. The inspector fires and shows the resulting state with updated values. "Visual indication of which fields changed" is aspirational.

4. **Inspector — "computed, state-derived" fields:** Wrong framing. Read-only fields in the inspector are fields not declared editable in the current state via `in State edit Field`. There are no computed or state-derived field types in the DSL.

5. **CLI — no `precept` command-line tool exists.** The surface as described is a future design artifact, not current implementation.

6. **Docs Site — naming:** "Docs Site" implies a public website. `docs/` is internal team artifacts. This surface should be explicitly labeled as future/planned if it stays in the spec.

---

### Open questions for Elaine

1. **Inspector yellow/warning:** Do you want to add a yellow "NotApplicable" state to the inspector event list? Right now that outcome is hidden. This would require an implementation change (Kramer's domain) — want to add it as a requirement?

2. **Docs Site scope:** Shane was already asked about this. Once he answers — is this a future public site, or internal docs only? — the surface section should be updated or labeled accordingly.

3. **CLI surface intent:** Is this a design contract for a future CLI tool, or is it meant to describe existing behavior? If future, flag it as aspirational. If someone is planning to build a `precept` CLI, that needs to be on the architecture roadmap.

4. **State diagram — current inspector vs. standalone surface:** The state diagram renders inside the inspector webview (same panel). Are you intending these as one surface or two? The current implementation is a unified preview panel with both diagram and inspector side-by-side. If they become separate surfaces, that's an architectural change.

---

### Overall verdict

The brand and UX design principles in Elaine's spec are sound — the color system, typography, accessibility thinking, and AI-first framing are all grounded correctly. The technical inaccuracies are concentrated in the inspector's event outcome model (no yellow NotApplicable state), the diagnostic code range (47 vs 54), and the CLI surface (which describes a tool that doesn't exist yet). None of these are blocking for brand spec integration, but the CLI section especially needs a disclaimer so it doesn't get mistaken for a description of current behavior.

---

# Brand-Spec Color Information Architecture — Implementation Complete

**Author:** J. Peterman
**Date:** 2026-04-07
**Status:** Implemented — pending Shane visual review
**Scope:** `brand/brand-spec.html` — §1.4, §1.4.1, §2.1, §2.2

---

## What Was Done

Implemented the approved color information architecture restructure. All changes are information architecture — no locked color values were changed.

### §1.4 Color System — Identity Palette Only

- **Rewrote intro paragraph** to clarify the two-layer system (brand palette here + syntax highlighting palette in §2.1).
- **Removed** all 7 per-category syntax token cards (Structure, States, Events, Data, Rules, Comments, Verdicts) — moved to §2.1.
- **Removed** the constraint signaling table — consolidated into §2.1 (where a functionally identical table already existed).
- **Kept** the 8+3 brand palette card, both callouts (Locked design, Semantic color as brand), and the hue map.
- **Updated** the hue map footer text to add a forward reference to §2.1.
- **Added** a new "Semantic family reference" table — 6 rows (Structure, States, Events, Data, Rules, Verdicts), identity-level only (hue assignment, meaning, surfaces). Forward references to §2.1 and §2.2 for implementation detail.

### §1.4.1 Cross-Surface Color Application — Renamed and Narrowed

- **Renamed** from "Color Usage" to "Cross-Surface Color Application."
- **Updated intro** to add forward references to §2.1 and §2.2.
- **Fixed** the `brand-light` row: removed the incorrect claim that brand-light is used for "state names in diagrams and syntax." State names use Violet `#A898F5`. Brand-light `#818CF8` is an accent color. (This was an existing discrepancy flagged by both Frank and Elaine.)
- Kept: Color Roles table, README & Markdown Application table, Color Usage Q&A, README color contract callout.

### §2.1 Syntax Editor — Self-Contained Syntax Color Reference

- **Updated Color application text** — now references §1.4 as the source of semantic family identities, with forward reference language rather than generic palette pointer.
- **Split the main card** into two: (1) Purpose + Color application; (2) Live example + Constraint signaling + Diagnostics.
- **Moved in** all 7 syntax token cards from §1.4, placed between the two cards.
- **Moved in** the hue map callout from §1.4 (now in §2.1 for syntax implementers).
- **Renamed** "Constraint-aware highlighting" → "Constraint signaling" for consistency with Frank's terminology.
- **Updated** the Constraint signaling intro text (minor rewrite for clarity).
- **Added** "Live example" h3 heading before the syntax block.
- **Updated** the Rules card to note explicitly that gold is syntax-only (deduplication: consolidating the gold restriction to the 8+3 card note and the §2.1 Rules card).

### §2.2 State Diagram — Diagram Color Mapping Added

- **Updated Color application paragraph** to reference §1.4 semantic families and distinguish the diagram surface from §2.1 (shape/edge styling vs. typography).
- **Fixed SVG legend** blocked line color: `#f43f5e` → `#FB7185` (aligning to brand palette source of truth, per Frank's hex discrepancy finding).
- **Added "Diagram color mapping" h3 subsection** with:
  - Static elements table (12 rows): canvas, borders by node type, fills, state name text, transition edges, event labels, guard annotations, legend text.
  - Runtime verdict overlay section with verdict overlay table (7 rows): current state node, enabled/blocked/warning edges, muted edges, enabled/blocked labels.
  - Open decision callout for current state indicator (Option A vs Option B — pending Shane sign-off).
  - Semantic signals callout (compile-time static analysis treatments: constrained italic, orphaned opacity, self-loop edges).

---

## Open Decisions Documented (Not Resolved)

Per the instruction to document open decisions rather than invent values:

**Current state indicator visual treatment** — documented as an open decision in §2.2 with two proposals:
- Option A (Elaine): `#1e1b4b` fill tint at 20–30% opacity
- Option B (Frank): `#4338CA` fill tint at 10–15% opacity

Both use a fill tint. The specific shade needs Shane sign-off before implementation.

---

## Hex Discrepancy Note

The SVG legend blocked line was fixed from `#f43f5e` to `#FB7185` (brand palette source of truth). The §2.3 Inspector Panel still uses `#F87171` for "Constraint violation indicators" — this pre-existing discrepancy is outside the scope of this restructure but should be addressed when §2.3 is next updated.

---

## What Was NOT Changed

- No locked color values changed
- No brand decisions altered
- §2.3, §2.4, §2.5 untouched (their §1.4 cross-references still resolve correctly)
- All existing content preserved — only reorganized

---

## Decision Required

**Shane visual review:** The restructure is functionally complete. Shane's eye is the final gate before this is considered locked.

Key things to review:
1. §1.4 — does the semantic family reference table give enough orientation without the full token cards?
2. §2.1 — does the layout (purpose card → 7 token cards → hue map → live example card) read well?
3. §2.2 — does the diagram color mapping section work as a reference? Open decision on current state indicator.

---

# Decision: Brand-Spec § 1.4 Palette Structure Reorganization
**Filed by:** J. Peterman  
**Date:** 2026-04-06  
**Status:** RECOMMENDATION — pending Shane sign-off  
**Reference:** `brand/references/brand-spec-palette-structure-review-peterman.md`

---

## Summary

Section 1.4 of `brand-spec.html` currently contains two distinct, unrelated color systems with no structural separation. Section 1.4.1 then re-lists the same brand palette a second time, creating redundancy. A constraint signaling table appears verbatim in both § 1.4 and § 2.1.

Three issues:
1. **Two palettes in one section** — brand palette (`pc-palette` card) and syntax-highlighting palette (per-category token cards) are stacked together in § 1.4 with no seam
2. **Conflicting "8+3" nomenclature** — the intro paragraph and the palette card both say "8+3" but count different colors (syntax tokens vs. brand/UI colors)
3. **§ 1.4.1 re-lists the same colors** — the Color Roles table duplicates the palette card's role names; the constraint signaling table appears verbatim in both § 1.4 and § 2.1

---

## Recommended Action

**Move:** Per-category syntax cards (Structure, States, Events, Data, Rules, Comments, Verdicts) and constraint signaling table from § 1.4 → § 2.1 (Syntax Editor), where they belong conceptually.

**Trim:** The "Color Roles" table in § 1.4.1 — eliminate the color identity columns already in the `pc-palette` card above. Keep only the "Specific Uses" column, or fold usage notes into the palette card itself.

**Rename:** § 1.4.1 "Color Usage" → "Cross-Surface Color Application" (more precise).

**Clarify:** § 1.4 intro paragraph — rewrite to explicitly acknowledge the two-layer system (brand palette here, syntax palette in § 2.1).

**No color values change. No locked decisions change.** This is reorganization only.

---

## Who Needs to Review

- **Shane** — final sign-off before edits
- **George / Kramer** — FYI that syntax token palette docs will move to § 2.1 (closer to their implementation reference)

---

## Files Affected (when approved)

- `brand/brand-spec.html` — structural reorganization of § 1.4, § 1.4.1, § 2.1

---

# J. Peterman — Final Brand-Spec Cleanup Pass

**Date:** 2026-04-04  
**Scope:** `brand/brand-spec.html` final palette consistency cleanup

## Decision

Treat the §1.4 palette card as the color source of truth and normalize all remaining downstream references to it.

## Applied in this pass

- Legacy verdict refs normalized to locked values:
  - Blocked/Error → `#FB7185`
  - Warning → `#FCD34D`
- Related palette drift tied to the same feedback thread also normalized:
  - Background → `#0c0c0f`
  - Gold syntax accent → `#FBBF24`
- Later-surface prose now uses the locked verdict language consistently (`emerald / rose / amber` where naming matters)

## Not changed

- No redesign
- No new color decisions
- No resolution invented for the §2.2 current-state indicator

## Remaining blocker

One item still requires Shane sign-off before the brand spec can be considered fully closed: the current-state indicator treatment in `brand/brand-spec.html` §2.2.

---

# Decision: README Restructure Proposal Filed

**Filed by:** J. Peterman  
**Date:** 2026-04-05  
**Status:** PROPOSED — awaiting Shane review  
**Artifact:** `brand/references/readme-restructure-proposal.md`

---

## Summary

A README restructure proposal has been filed synthesizing three research passes:

- **Peterman** — brand/copy conventions from 13 comparable library READMEs
- **Steinbrenner** — developer evaluation journey and adoption patterns
- **Elaine** — UX/IA review with explicit hard constraints

## Key Structural Decisions

1. **Section order locked:** Title → Hook → Quick Example → Getting Started → What Makes Precept Different → Learn More → License
2. **Hero treatment:** 18-20 line DSL block (≤60 chars/line) + separate 5-line C# execution block; both with language tags; business domain only
3. **CTA hierarchy:** Primary = VS Code extension; Secondary = NuGet; Tertiary = Copilot plugin (deferred to differentiation section — removes 3-way decision paralysis from current README)
4. **Sample catalog removed** from main README; linked externally
5. **Time Machine sample** moved to sample catalog; hero uses real business logic domain

## Hard Constraints (Elaine — 16 total)

All 16 constraints from Elaine's UX/IA review are documented as non-negotiables in the proposal. They include: mobile-first viewport, single primary CTA, semantic heading hierarchy, progressive disclosure order, viewport resilience, screen reader compatibility, and AI parseability requirements.

## Open Items Requiring Shane Decision

- **Hero sample domain:** Order vs. Subscription Billing vs. LoanApplication (structural spec is locked; domain is Shane's call)
- **Brand mark form** in title block (SVG at 48px for mobile-first)
- **Docs site links** — all "link" placeholders require real URLs before README ships
- **Palette usage roles** — Elaine's palette/usage pass still in flight; no color decisions in README rewrite should anticipate that pass

## What This Does NOT Change

- No edits to `README.md` have been made
- Brand decisions remain locked (`brand/brand-decisions.md`)
- Positioning language is locked: "domain integrity engine for .NET"

---

# README Research Recommendations — J. Peterman

**Date:** 2025-01-18  
**Status:** Pending Shane review  
**Research file:** `brand/references/readme-research-peterman.md`

---

## Summary

Studied 13 READMEs (8 comparable libraries, 5 exemplar projects) with real measurements. Three README models identified: Content-Rich, Gateway, Hybrid. **Recommendation: Hybrid model for Precept** — hook + hero + AI-first section + features + links.

---

## Key Recommendations

### 1. Structure: Hybrid README Model

**Pattern:**
- Opening hook (23 words): "Precept is a domain integrity engine for .NET that binds an entity's state, data, and business rules into a single executable DSL contract."
- Hero code (18 lines): Subscription Billing DSL sample (DSL only, no runtime code)
- One-line clarifier: "Precept unifies state machines, validation, and business rules into a single DSL — replacing three separate libraries with one executable contract."
- **AI-First Tooling section** (new, unique to Precept)
- Installation (dotnet add, VS Code extension)
- Quick links (Docs, Samples, Language Reference, MCP Server, Copilot Plugin)
- Features list (bullet overview of 9 DSL constructs)

**Why Hybrid?**
- **Not Content-Rich** (like XState, Stateless) — Precept has docs site; README shouldn't duplicate construct reference
- **Not Gateway** (like Vue, FastEndpoints) — DSL syntax must be shown immediately to prove readability
- **Hybrid works** (like Zod, FluentValidation) — hero sample proves "you can read this," docs site handles depth

**Evidence:** Zod (13-line hero, 8-word hook), FluentValidation (24-line hero, 18-word hook), React (12-line hero, 9-word hook) all use Hybrid model successfully.

---

### 2. Hero Code: 18 Lines, DSL Only

**Recommendation:** Use Subscription Billing sample (18 DSL statements, validated in line-economy research)

**What to show:**
- `precept SubscriptionBilling`
- 2-3 fields (Status, BillingCycleDay, ActiveSince)
- 3 states (Trial, Active, Cancelled)
- 2 events (Activate, Cancel)
- 3 event handlers with guards and transitions
- 1 constraint or invariant

**What NOT to show:**
- C# runtime invocation code (`var engine = new PreceptEngine(...)`)
- JSON state snapshots
- Fire results or verdicts

**Why?** Hero's job is to prove **the DSL is readable**. Runtime integration belongs in docs. Reader should see DSL and think "I understand this without reading the manual" — that's Precept's value prop.

**Evidence:** Hero code in studied READMEs ranges 6–26 lines (median 13). Precept's 18 lines aligns with FluentValidation (24), Stateless (18), XState (26) — all show complete round-trip within constraints.

---

### 3. Positioning: Category-Creating Language

**Recommendation:** "Precept is a domain integrity engine for .NET that binds an entity's state, data, and business rules into a single executable DSL contract." (23 words)

**Follow with one-line clarifier:** "Precept unifies state machines, validation, and business rules into a single DSL — replacing three separate libraries with one executable contract."

**Why?**
- **"Domain integrity engine"** is a new category claim (like Bun's "all-in-one toolkit," XState's "actor-based orchestration")
- **Not comparative** — doesn't say "better than X," says "different abstraction layer"
- **Concrete outcome** — "executable DSL contract" is the differentiator

**Evidence:** Category-creating tools use "[X] is a [new category] for [platform]" structure. Bun: "all-in-one toolkit for JavaScript and TypeScript apps." Polly: "resilience and transient-fault-handling library." NRules: "production rules engine for .NET, based on the Rete matching algorithm."

---

### 4. AI-First Tooling Section (NEW — Unique Opportunity)

**Recommendation:** Add dedicated section immediately after hero code:

```markdown
### AI-First Tooling

Precept ships with native MCP server integration and a GitHub Copilot plugin:
- **MCP server:** 5 tools (`precept_compile`, `precept_fire`, `precept_inspect`, `precept_update`, `precept_language`)
- **Copilot plugin:** Agent definition + 2 skills for DSL authoring and debugging
- **Language server:** Full LSP support with diagnostics, completions, hover, semantic tokens, and live preview

AI agents can author, validate, and debug `.precept` files without human intervention.
```

**Why?**
- **Unique to Precept:** No comparable library studied leads with MCP + Copilot + LSP as unified tooling story
- **Factual, concrete:** Lists specific tools, not marketing claims
- **Category differentiation:** "AI-first" is Precept's secondary positioning (per brand decisions)

**Evidence:** Only 1 of 13 projects studied mentions AI tooling (NRules links to "GPT Rules Writer" in passing). Precept's integrated MCP + Copilot + LSP story is a **competitive advantage** — should be front and center in README.

---

### 5. Copy Tone: Concrete, Confident, Technical

**Guidelines:**
- **Declarative, present tense** — "Precept is," "Precept compiles," "Precept ships with" (not "tries to," "hopes to")
- **Concrete metrics** — "18-line hero," "9 DSL constructs," "5 MCP tools" (not "many," "various," "multiple")
- **Technical precision** — "DSL runtime," "interpreter," "contract," "LSP" (not "tool," "system," "framework")
- **No hedging** — "unifies" (not "aims to unify"), "replaces" (not "can replace")
- **J. Peterman voice** — evocative but precise, authoritative with warmth (not marketing fluff)

**Evidence:** Successful READMEs use declarative tone (Polly: "Polly is," Bun: "Bun is"), concrete claims (Biome: "97% compatibility," "450+ rules"), technical precision (NRules: "Rete matching algorithm," Bun: "written in Zig").

---

## Anti-Patterns to Avoid

Based on research findings:

1. **Don't bury the hero code** — Polly's hero appears at line 51 (too late)
2. **Don't show runtime invocation in hero** — MediatR's README is all registration code (confusing)
3. **Don't redirect to docs before showing code** — Vue's README is pure links (wrong for unknown category)
4. **Don't write 32-word opening sentence** — Polly's hook lists six strategies (too dense)
5. **Don't skip "What is X?" framing** — Bun and Zod both ask "What is [X]?" before answering
6. **Don't use generic examples** — No "foo/bar" — real domains only (Subscription Billing, Coffee Order)

---

## Decision Points for Shane

1. **README model:** Approve Hybrid model (hook + hero + AI-first + features + links)?
2. **Hero sample:** Approve Subscription Billing (18 DSL statements) as hero code?
3. **Positioning:** Approve "domain integrity engine" as category claim?
4. **AI-first section:** Approve dedicated MCP + Copilot + LSP section after hero?
5. **Copy tone:** Approve J. Peterman voice guidelines (concrete, confident, technical)?

**Next step:** Draft new README.md using approved structure.

---

## Research File

Full research with measurements, quotes, and synthesis: `brand/references/readme-research-peterman.md`

---

## J. Peterman — Brand Review: Elaine's 5-Surface UX Spec
**Date:** 2026-04-04
**Status:** Approved with notes

---

### Surface-by-surface notes

#### Surface 1: Syntax Editor — ✅ Compliant
The spec is precise and correct. The full 8-shade authoring palette is applied with exact hex values. The "bold = semantic drivers, italic = constrained tokens, normal = everything else" typography signal is stated cleanly. The explicit rule — "No runtime verdicts in syntax highlighting" — is written as a hard constraint, which is exactly right. The AI-first note is accurate and earns its place: semantic tokens as a machine-readable metadata channel is a real insight, not decoration.

One note: `#9096A6` for comments is correctly identified as "dusk indigo, editorial, outside the semantic palette." That framing is consistent with brand-decisions.md. No change needed.

#### Surface 2: State Diagram — ⚠️ Needs revision (see Priority Issues)
The prose and callout box are excellent. "Lifecycle role is shown by shape, not tint" is the correct principle, stated with conviction. The AI-first note — diagrams as machine-readable knowledge graphs — is strong brand positioning.

The problem is the color table contradicts the callout. The spec lists three separate node colors:
- `#A5B4FC` for initial states
- `#94A3B8` for intermediate states  
- `#C4B5FD` for terminal states

None of these are in the locked 8+3 system. And their existence directly violates the "no lifecycle tints" principle stated two lines above. The spec cannot simultaneously claim shape carries lifecycle structure and then assign three different tint values to three lifecycle roles. One of them has to win. Per brand-decisions.md, shape wins.

Additionally: the event sub-shading entry references `#38BDF8`, `#7DD3FC`, and `#0EA5E9` as cyan sub-shades for event subtypes. These are off-system (Tailwind sky family, not the locked `#30B8E8` cyan). The locked system has one event hue. If subtype differentiation is needed, the mechanism is Elaine's — but the colors must come from the locked palette.

#### Surface 3: Inspector Panel — ✅ Compliant with one minor note
Solid. Verdict colors (`#34D399`, `#F87171`, `#FDE047`) are applied correctly and exclusively to runtime outcomes. The data color mapping (slate names / types / values, violet for current state label) is consistent with the editor. The "field names italic if guarded by invariant" rule correctly mirrors the syntax highlighting convention.

One point worth flagging: the spec uses `#FBBF24` gold for constraint message text beneath violated fields. This is defensible — the `because`/`reject` message payloads earn the warm interrupt whether they appear in the editor or the inspector, and it maintains cross-surface consistency. It's not a violation. But it means gold appears in two distinct contexts (authoring-time rule strings AND runtime violation messages), and those contexts must remain visually separate. They are, because violation messages appear below a field in inspector layout, not inline with syntax. I'm noting it, not flagging it as a problem.

Minor: `#9096A6` (the comments/editorial shade) is specified for read-only field indicators. This color sits intentionally outside the semantic palette. Using it for "cannot be modified" is a legitimate secondary role. Worth documenting in the integrated brand-spec as "editorial shade — second use: read-only field indicator."

Brand positioning for inspector is correct. "The inspector is both a human debugging tool and an API contract for AI tools" is exactly the right frame.

#### Surface 4: Docs Site — ✅ Approved as aspirational, with one flag
The color system is sound — indigo accents, dark neutral chrome, syntax palette in embedded code blocks. The callout box treatment maps cleanly to brand-spec.html patterns.

One flag: the typography section suggests "System font stack or Cascadia Cove 400" for prose. If Precept's brand is Cascadia Cove throughout — and it is — then "system font stack" is not a locked option; it's an escape hatch. The spec should read: Cascadia Cove for all surfaces, with monospace system font (Consolas, Courier New) as the fallback chain, not as an equivalent alternative.

H2/H3 at "600–700 weight" is also loosely specified. Brand-spec uses 700 for headings. A 600-weight sub-heading tier may be intentional and is reasonable — just needs to be pinned to a specific value when the docs site moves from aspirational to locked.

The PETERMAN REVIEW comment in the HTML (line 168) asks whether docs site branding is my responsibility. It is. When the docs site moves to active design, I own the brand compliance review. This draft correctly holds the aspirational specs here rather than locking them prematurely. That's the right call.

#### Surface 5: CLI / Terminal — ✅ Compliant
The verdict color mapping is correct and consistent. The explicit decision — "No structural indigo on CLI. Deep indigo (#6366F1) does not render well on light terminal themes" — is sound. Brand indigo belongs to UI chrome and syntax highlighting, not terminal escape sequences. This is the right trade.

The typography guidance (bold + verdict color for error headers, default color for file paths and diagnostic text) is sensible and light terminal-safe. The AI-first note — "color should never be the only signal — symbol and text structure matter more" — is right and reinforces the color + symbol redundancy principle across all surfaces.

---

### Priority issues

**1. State Diagram: Node color table must be corrected (blocks integration into brand-spec.html)**

The three-color lifecycle tinting (`#A5B4FC`, `#94A3B8`, `#C4B5FD`) directly contradicts the spec's own "no lifecycle tints" principle and introduces three off-system hex values. The resolution is clear: state nodes use the indigo family for borders/structure (`#4338CA`/`#6366F1`) and state names use violet (`#A898F5`). Shape (circle, rounded rect, double-border) encodes initial/intermediate/terminal. The color table entry should be revised to a single line: "State nodes: indigo border (`#6366F1`), dark background, state name text in violet (`#A898F5`)."

**2. State Diagram: Off-system event sub-shade colors**

`#38BDF8`, `#7DD3FC`, `#0EA5E9` are not in the locked system. If transition/conditional/stationary event subtypes need visual differentiation in diagrams, the mechanism must either use the single locked cyan (`#30B8E8`) with other signals (dashed vs. solid edges, label suffixes), or the introduction of additional shades must go through a formal palette extension decision — not appear in a spec draft as fait accompli. Remove these specific values until a decision is made.

---

### Minor notes

1. **Docs site: prose typography** — "System font stack" should read "Cascadia Cove, with monospace system fallback." Not equivalent alternatives.

2. **Docs site: H2/H3 weights** — Pin "600–700" to a specific value when the docs site moves to locked. 600 is not currently a specified brand weight.

3. **Inspector: `#9096A6` editorial reuse** — Document explicitly in the integrated spec that this shade serves two roles: syntax comments (authoring-time) and read-only field indicators (inspector). Clarifies intent for any implementer.

4. **Inspector: gold constraint messages** — Note in the integrated spec that `#FBBF24` serves both authoring-time rule message strings (in syntax highlighting) and runtime constraint message text (in inspector). The contexts are spatially separate, so there's no confusion — but the dual role should be documented.

---

### Overall verdict

The draft is well-grounded in the locked system — Elaine clearly read brand-decisions.md, not just skimmed it. One internal contradiction in the state diagram section (lifecycle tints specified while the principle bans them, using off-system hex values) must be resolved before this spec goes into brand-spec.html. Everything else is either compliant or correctly flagged as aspirational.

---

# Decision: README Badge Cleanup + Sample Catalog Count

**Author:** Kramer  
**Date:** 2026-04-05  
**Requested by:** Shane

---

## Decisions Made

### 1. Remove Build Status badge
**Decision:** Removed the Build Status badge entirely.  
**Rationale:** No `build.yml` (or any CI build workflow) exists in `.github/workflows/`. The badge used placeholder owner `OwnerName` and would have permanently shown "unknown" status. No CI build pipeline to link to — the badge was misleading noise.

### 2. Remove VS Code Extension marketplace badge
**Decision:** Removed the VS Code Marketplace badge entirely.  
**Rationale:** `tools/Precept.VsCode/package.json` has `"publisher": "local"` — a local development placeholder. The extension has not been published to the VS Code Marketplace. The badge would permanently fail to resolve against the marketplace API.

### 3. Fix `AuthorName.precept-vscode` placeholder in Quick Start
**Decision:** Updated to `sfalik.precept-vscode`.  
**Rationale:** GitHub remote is `https://github.com/sfalik/Precept.git`, so the owner/publisher is `sfalik`. This is consistent but still provisional until the extension is actually published with a confirmed publisher ID.

### 4. Update sample count 20 → 21 and add `crosswalk-signal.precept`
**Decision:** Updated README sample catalog to include `crosswalk-signal.precept` and corrected count to 21.  
**Rationale:** `crosswalk-signal.precept` existed in `samples/` but was absent from the README sample catalog table and all feature coverage matrix rows. The count claim ("20 workflows") was factually wrong.

---

## NuGet Badge — No Change Needed
The NuGet badge (`https://img.shields.io/nuget/v/Precept` → `https://www.nuget.org/packages/Precept`) is correctly structured. The package name `Precept` matches the project name in `src/Precept/Precept.csproj` (no explicit `<PackageId>`, defaults to project name).

---

## No Numeric Catalog/Constructs Count Claim Found
Searched README for claims about number of DSL constructs, catalog items, or language features. No such claim exists.

---

# Decision: Restore "Two Surfaces + 3 Brand Marks" to brand-spec.html § 1.3

**Date:** 2026-04-05  
**Owner:** J. Peterman (Brand/DevRel)  
**Status:** COMPLETED  
**File:** `brand/brand-spec.html`

---

## Problem Statement

The brand-spec lost content from explorations: the "two surfaces" (DSL Code + State Diagram side-by-side) and the brand mark size variants (64px, 32px, 16px) were not integrated into § 1.3 (Wordmark & Brand Mark). Shane requested restoration.

## Solution

### Part 1: Brand Mark Size Variants

Expanded the "Brand mark form" card to display three size variants in a horizontal flex layout:

- **Full (64px)** — NuGet, GitHub, VS Code extension icon  
  - Existing SVG maintained at full scale (80px display, 64px viewBox)
  - Use case: "NuGet, GitHub, VS Code"

- **Badge (32px)** — sidebar, compact contexts  
  - Same SVG scaled to 40px display (64px viewBox)
  - Use case: "Sidebar, compact"

- **Micro (16px)** — favicon, status bar  
  - Simplified SVG (32px viewBox) showing only indigo circle + emerald arrow
  - Destination circle removed for legibility at micro scale
  - Use case: "Favicon, status bar"

All three shown with label and use-case note beneath. Color key simplified to four swatches (indigo, emerald, slate, ground) without verbose role descriptions.

### Part 2: "Brand in System" Card

Added a new card immediately after the brand mark form card, showing the two locked DSL surfaces side-by-side:

**Title:** "Brand in system: the two primary surfaces"  
**Subtitle:** *"DSL code and state diagram. One palette. Every precept file becomes a brand moment."*

**Layout:** Two-column grid (`grid-template-columns: 1fr 1fr`)

- **Left column:** Surface 1: DSL Code
  - Syntax-colored Precept example (LoanApplication)
  - Uses locked palette: keywords #4338CA, states #A898F5, events #30B8E8, operators #6366F1

- **Right column:** Surface 2: State Diagram
  - SVG state machine diagram (Draft → Submit → UnderReview)
  - Same palette; transition arrow #34D399 emerald, ground #1e1b4b indigo

**Footer text:** Explains the locked palette and how DSL semantics appear in two visual forms.

## Design Rationale

1. **Size variants clarify product placement:**
   - No ambiguity about icon sizing across NuGet badges, GitHub repos, VS Code extensions, and system UI
   - Micro variant's simplified form (no destination circle) is a practical concession to legibility at 16px scale

2. **Two surfaces card reinforces visual language principle:**
   - The same DSL semantics appear in two visual forms
   - One locked palette across both surfaces (keywords, states, events, operators, transitions)
   - Placement after brand mark (not in § 2: Visual Surfaces) keeps § 1 as a complete identity system

3. **Grid layout matches brand-spec style:**
   - Inline CSS grid (not `.side-by-side` class)
   - Consistent with existing card styling: dark background #0c0c0f, border #1e1b4b
   - Each surface panel has its own visual container

## Content Source

The two-surfaces card adapted from `brand/explorations/visual-language-exploration.html` § 3 (lines 2132–2174), originally showing the "combined visual identity system" with code and diagram surfaces locked together.

## Files Changed

- `brand/brand-spec.html` — § 1.3 expanded with size variants and two-surfaces card

## Decision

**Approved:** Both the size variants expansion and the two-surfaces card restore the intended brand-spec narrative: the brand mark is a locked form with three deployment contexts (Full, Badge, Micro), and it lives within a larger visual language where DSL code and diagrams speak the same semantic palette.

---

**Next Steps:** (None — this completes Shane's request.)

---

# Decision: Brand mark corrected to three forms

**Date:** 2025-07-17  
**Author:** J. Peterman  
**Files changed:** `brand/brand-spec.html`, `brand/explorations/visual-language-exploration.html`

## What was wrong

Two previous agent passes corrupted the brand mark section in `brand/brand-spec.html` section 1.3:

1. **Size-variant display** — The "Brand mark form" card was replaced with Full (64px) / Badge (32px) / Micro (16px) size rows, all showing the same single state-machine mark. This was never requested and incorrectly implied a size-variant system was locked.

2. **"Brand in system" card** — A "Brand in system: the two primary surfaces" card (DSL code + state diagram) was incorrectly appended inside section 1.3. This content belongs elsewhere; section 1.3 covers the brand mark form itself.

3. **TBD placeholder** — `visual-language-exploration.html` Surface 3 still showed two dashed TBD boxes with "Autonomous prototyping loop running" text, never updated with actual marks.

## What was fixed

The correct three marks — sourced exactly from `brand/explorations/semantic-color-exploration.html` section 5, lines 637–675 — are now displayed in both files:

| Mark | Label | Concept |
|------|-------|---------|
| Mark 1 | State + transition | The state-machine atomic unit |
| Mark 2 | Tablet / precept | The written rule / code tablet |
| Mark 3 | Tablet + state machine | Combined form — **primary mark** |

All three SVGs are 64×64, use the locked palette (Indigo #6366F1, Emerald #34D399, Slate #475569, Ground #1E1B4B), and share one conceptual language.

### Changes made

**`brand/brand-spec.html` — section 1.3:**
- Replaced "Brand mark form" card (Full/Badge/Micro) with "Brand mark — three forms" card
- Removed incorrectly-added "Brand in system: the two primary surfaces" card
- Color key and prototyping-loop callout retained

**`brand/explorations/visual-language-exploration.html` — Surface 3:**
- Replaced TBD placeholder boxes with the same three-mark flex display
- Now in sync with brand-spec

---

# Decision: Replace README Hero with TimeMachine

**Date:** 2026-07-11  
**Author:** J. Peterman (Brand/DevRel)  
**Requested by:** Shane  
**Status:** Implemented

---

## What Changed

The hero code block in `README.md` (the `.precept` example under "The Aha! Moment") has been replaced.

**Before:** `LoanApplication` — 40+ lines, five fields, multiple states, compound guards on income ratios and credit scores, commented sections. Technically complete. Tonally: a mortgage application.

**After:** `TimeMachine` — 15 lines. One field (`Speed`), one invariant, three states (`Parked → Accelerating ↔ TimeTraveling`), two events, dotted arg access, an 88 mph && 1.21 gigawatts `when` guard, a `reject` with a line that earns its place, and a clean arrival loop back to `Parked`. No comments. No language tag on the fence.

---

## Rationale

The hero example is not documentation. It is an argument. It must answer — in seconds, without narration — *why Precept exists* and *what it feels like to use it*.

`LoanApplication` answered the first question competently. It did not answer the second.

`TimeMachine` answers both. The invariant is self-evident. The guard condition is famous. The reject message — *"Roads? Where we're going, we still need 88 mph and 1.21 gigawatts."* — demonstrates that constraint messages can carry personality without losing precision. The state loop is satisfying. The whole thing reads in under thirty seconds and leaves a developer thinking about what they would build.

This is the correct register for a hero example.

---

## Authoring Choices

- **No comments** — the code block reads cleanly without annotation. The structure is legible.
- **No language tag** on the fence — GitHub renders plain monospace, which is correct. A syntax tag would apply GitHub's generic highlighter, which does not know Precept.
- **15 lines exactly** — within the 10–15 line constraint. Nothing trimmed from the canonical source.

---

## Files Affected

- `README.md` — hero code block and section heading updated

---

# Decision: Indigo Overview Card Format in § 1.4

**Date:** 2026-05-01  
**Requested by:** Shane  
**Agent:** J. Peterman  
**Status:** Implemented

## Decision

The indigo color system overview card in `brand/brand-spec.html` § 1.4 now uses the `palette-card` format from `brand/explorations/color-exploration.html`.

## What Changed

The old card used a bespoke `.card` layout with:
- A 48px swatch bar bleeding into the card via negative margins
- An 8-shade ramp below it (also negative-margin bleeding)
- A prose title (`h3`) and description paragraph
- A full color role table (8 rows × 4 columns: swatch, hex, role, usage)
- A two-column grid (NuGet badge left / syntax snippet right)
- A separate state diagram block with its own background and border

The new card mirrors the `palette-card` format exactly:
- `pc-swatch-bar` — 64px tall, solid #6366F1, full bleed at the top
- `pc-swatch-gradient` — 32px flex row, 8 shade divs (#1e1b4b → #c7d2fe)
- `h2` title: "Indigo · 239°"
- `pc-hex`: `#6366F1   rgb(99, 102, 241)` in monospace
- Four `pc-context-block` sections, each with an uppercase `h3` header:
  1. **NuGet Badge** — v1.0.0 (indigo #4338ca), .NET 8.0 (slate), license MIT (slate)
  2. **Icon Mock (64px / 32px)** — combined tablet + state machine SVG at both sizes
  3. **Syntax Highlighting** — keywords #4338ca bold, states #818cf8, event #30b8e8, operators #6366f1
  4. **State Diagram Accent** — Draft → Submit → Review, indigo stroke, emerald arrow

## Why

The exploration card format (width: 320px, dark #141414 background, context-block sections) is the canonical visual audit format for the Precept palette. Keeping the brand-spec's overview card in a bespoke format meant maintaining two divergent representations of the same information. The exploration format is more compact, better organized for at-a-glance review, and visually consistent with the exploration artifacts that informed the brand decision.

## Implementation Notes

- Added a `pc-` CSS class prefix family to brand-spec.html's `<style>` block. The existing `.palette-card` class in brand-spec serves the palette swatch grid (a different context) and could not be repurposed.
- The `pc-context-block h3` override resets brand-spec.html's global `h3` styles (uppercase + border-bottom) to match the exploration card's style.
- All surrounding § 1.4 content preserved: heading, both callouts, and the structural/state/event/data/rules/comments/verdicts cards below.

---

# Decision: Indigo Color System Overview Card

**Author:** J. Peterman (Brand/DevRel)
**Date:** 2026-04-05
**Section:** brand/brand-spec.html § 1.4 Color System
**Status:** Implemented

---

## What was decided

A self-contained **"Indigo Color System — Overview"** card was added at the top of section 1.4, before the per-role "Structure · Indigo" card. It provides a single-card reference for the complete 8-shade indigo ramp.

## Structure

| Element | Detail |
|---------|--------|
| Swatch bar | 48px solid `#6366F1` bleed strip |
| Gradient ramp | 8 equal segments: `#1e1b4b` → `#312e81` → `#3730a3` → `#4338ca` → `#6366f1` → `#818cf8` → `#a5b4fc` → `#c7d2fe` |
| Title | "Indigo · 239°" in `#6366F1` |
| Role table | Swatch · hex · role · usage; 8 rows |
| Badge | NuGet `#6366f1` |
| Snippet | 5-line LoanApplication DSL |
| Diagram | Draft → Submit → UnderReview, 280×80 SVG |
| Bottom note | Rationale for indigo selection |

## Shade roles

| Hex | Role | Usage |
|-----|------|-------|
| `#1e1b4b` | Ground | Dark indigo surface; diagram and editor backgrounds |
| `#312e81` | Deep | Border emphasis; icon background layer |
| `#3730a3` | Rich | Reserved; deep structural accent |
| `#4338ca` | Semantic | DSL control keywords (bold): precept, state, event, from, on… |
| `#6366f1` | Grammar | DSL grammar connectives (normal): as, ->, =, operators |
| `#818cf8` | Brand-Light | Secondary brand; code block borders; diagram stroke |
| `#a5b4fc` | Brand-Muted | Diagnostic code prefix (PRECEPT001); subtle highlights |
| `#c7d2fe` | Pale | Light muted; barely-there tint for callout backgrounds |

## Rationale

The existing section 1.4 jumped straight to per-role breakdown without ever showing the family as a whole. Designers and engineers reading the spec had no single reference for the complete ramp or the logic of the progression. The overview card fills that gap — it is a map before the street-level directions.

The in-context examples (badge, syntax, diagram) demonstrate the palette in the three surfaces where it actually appears, not in abstraction. Every shade appears at least once.

## Implementation notes

- No new CSS classes — all styling is inline, per brief
- Negative top/side margins on swatch bar and ramp achieve full-bleed effect inside `.card` padding box
- Arrow marker ID: `indigo-arrow-overview` (scoped to avoid `<defs>` collisions)
- Card inserted before `<!-- Structure -->` comment, line 383 in original file

## Closing line

*"Depth and authority. Selected over teal, amber, and steel. The only brand color that reads as both technical precision and earned trust."*

---

# Decision: Voice & Tone — Wit Integration into Brand Spec

**Date:** 2026-XX-XX  
**Owner:** J. Peterman, Brand & DevRel  
**Status:** Implemented  
**Files:** `brand/brand-spec.html` (§1.2)  

## Problem

Section 1.2 Voice & Tone stated: *"Serious. No jokes."* This was wrong. It didn't capture Precept's actual voice—one that contains dry wit, precision humor, the kind of humor that lands because it's understated and earned.

Shane's feedback: "Doesn't represent the wit element we want to incorporate."

## Decision

Revised section 1.2 to acknowledge and celebrate dry wit as a core voice element. Wit that flows from confidence and precision, not performance. Like Stripe docs. Like the best GitHub changelogs.

### What Changed

**1. Table Row (Serious ↔ Funny)**
- **Old:** "Serious. No jokes."
- **New:** "Dry wit welcome. Never forced. Precision finds the humor in the truth."

**2. Prose Paragraph (Brand Description)**
- **Old:** "The voice states facts. It doesn't hedge. It doesn't oversell. But it occasionally explains *when* something matters."
- **New:** "The voice states facts. It doesn't hedge. It doesn't oversell. It finds the wit in precision. When something matters, it says why — and the clarity itself can be the humor."

**3. Do Examples (Wit in Action)**
Added two examples showcasing precision humor without mockery:
- *"If you've been writing the same validation in four services, Precept has questions."*
- *"Turns out business rules don't change just because you moved them to a different service."*

**4. Status**
- Changed header status chip from `LOCKED` to `REVISED` (blue background, light text).

## Reasoning

Precept's wit is **earned from specificity**:
- The tool knows exactly what it does.
- The alternatives are slightly absurd.
- The humor doesn't mock the user—it states the truth in a way that makes the truth funnier than any joke.

This wit is **not performance**:
- It doesn't wink at the camera.
- It doesn't try too hard.
- It's confident enough to be understated.

Compare: "Say goodbye to bugs forever!" (performative) vs. "If you've been writing the same validation in four services, Precept has questions." (precision humor). The second works because it's true. The truth is the joke.

## Impact

- **Voice brand now reflects reality** — Precept's communications will include this wit, examples will model it, and new writers will understand it's intentional and encouraged.
- **Alignment with design systems** — Stripe, GitHub, and similar high-confidence brands all use wit this way. We're in good company.
- **Developer experience** — Developers recognize and appreciate this tone. It signals confidence and taste.

## Next Steps

- Monitor copy across docs, changelogs, and communication for consistency with this updated voice.
- Use the two new Do examples as models for future content.
- Consider highlighting this distinction in internal brand guidelines or contributor docs.

---

# Decision: README Restructure Proposal — Editorial Review Complete

**Date:** 2026-04-06  
**Author:** Uncle Leo (Copy/Editorial)  
**Verdict:** Approve with required changes  
**Input artifact:** `brand/references/readme-restructure-proposal.md`  
**Full review:** `brand/references/readme-restructure-review-uncle-leo.md`

---

## Required Changes Before Rewrite

**RC-1 — Section map / hero label conflict**  
The Recommended Section Order block uses H3 markers (`### The Contract`, `### The Execution`) but the Hero Treatment section specifies bold inline labels, not H3 headings. The rewrite will get conflicting signals. The section map must be corrected to match the Hero Treatment spec (bold inline labels, not subheadings).

**RC-2 — Getting Started context reminder is missing, not implicit**  
The proposal claims Elaine's required one-sentence context reminder is "implicit" in step 1's benefit copy. It is not. The context reminder (re-anchoring non-linear readers to what Precept is) must be written explicitly before or within step 1. This was a hard requirement in Elaine's research.

---

## Wording Concerns (addressable during rewrite)

- WC-1: "Badge walls signal maintenance anxiety" — editorializing; suggest "add visual noise without adding signal at the awareness stage"  
- WC-2: AI tooling "lead with / don't bury" argument conflates two separate points; suggest splitting  
- WC-3: Closing tagline "One file. Every rule. Prove it in 30 seconds." is ambiguous — proposal flourish or proposed README copy? Label it.

---

## What Is Approved

Overall structure, section order, CTA hierarchy (Primary/Secondary/Tertiary), dual-audience table format, constraint table, hero spec (18-20 lines DSL + 5-line C# block), and the closing summary framing ("proves before it teaches") are all approved without changes.

---

## Status

Blocked on RC-1 and RC-2. Frank's four required changes are separate and not duplicated here. George's technical accuracy review is ongoing.

---

# README Revamp — Scope & Priority Recommendation

**From:** Steinbrenner (PM)  
**Date:** 2026-04-04  
**Context:** Parallel research to Peterman's brand/copy work. This is the product/adoption strategy for the README revamp.  
**Research Document:** `brand/references/readme-research-steinbrenner.md`

---

## Executive Summary

The current Precept README is structured like API documentation, not a product landing page. Based on analysis of 9 category-defining tools (xstate, Polly, Temporal, Terraform, Bun, Deno, etc.), the README must:

1. **Define the category** ("domain integrity") before describing the tool
2. **Lead with tooling** (MCP, VS Code, AI-native) as the primary differentiator
3. **Provide a quickstart** that gets from "I saw this" to "I have it running" in <3 minutes
4. **Teach the mental model** before showing syntax

This recommendation outlines the minimum viable README revamp scope from a product adoption perspective.

---

## Critical Gaps in Current README

### 1. **No Problem Statement**
**Current:** README opens with "Precept is a DSL for defining domain integrity constraints..."  
**Issue:** Developers don't know what "domain integrity" is or why they need it.  
**Fix:** Lead with the problem: "Your validation is scattered across controllers, your state machine is split from your business rules, and your constraints are duplicated in code and tests. Precept unifies all three into one executable contract."

### 2. **No Quickstart Path**
**Current:** README has installation instructions but no "first working example" flow.  
**Issue:** Developers can't evaluate time-to-value without seeing the quickstart friction.  
**Fix:** 3-step quickstart: Install extension → Create Order.precept → Get real-time diagnostics. Or: `dotnet add package Precept` → 10-line example → 3-line C# usage.

### 3. **No Category Education**
**Current:** README assumes you know what "domain integrity" means.  
**Issue:** We're establishing a new category, not implementing a known pattern.  
**Fix:** Add "What is Domain Integrity?" section that explains the unified state+data+rules model.

### 4. **Tooling Story Buried**
**Current:** VS Code extension, MCP server, language server mentioned in passing.  
**Issue:** This is Precept's strongest differentiator vs. xstate/FluentValidation — and it's hidden.  
**Fix:** Lead with tooling: "Precept is a domain integrity DSL with AI-native tooling. Write .precept files, get real-time diagnostics in VS Code, and let Claude reason about your domain model through MCP."

### 5. **No Developer Journey Mapping**
**Current:** Sections appear in implementation order (what we built), not evaluation order (what developers need to decide).  
**Issue:** Developers evaluate tools through a 4-stage journey (Awareness → Evaluation → Trial → Adoption). Section order must match this journey.  
**Fix:** Restructure to optimal adoption order (see Section Order proposal below).

---

## Minimum Viable Revamp Scope

### Phase 1: Above-the-Fold (Must-Have)

**Goal:** Hook the developer in 5 seconds, convince them to scroll in 30 seconds.

**Changes:**
1. Add **problem statement** (1-2 sentences): "Precept unifies your entity's state, data, and business rules into a single executable contract."
2. Add **differentiation tagline** (1 sentence): "Write .precept files, get real-time diagnostics in VS Code, and let AI agents reason about your domain model."
3. Move **badges** (build, license, version) immediately below logo
4. Add **quickstart** (3 steps max): Install → Example → Run

**Effort:** 2 hours (copy) + 1 hour (structure)

---

### Phase 2: Educational Section (Should-Have)

**Goal:** Teach "domain integrity" as a concept so developers understand the category.

**Changes:**
1. Add **"What is Domain Integrity?"** section
   - Explain the problem: scattered validation, split state machines, fragmented rules
   - Show how Precept unifies all three
   - Position as "single source of truth for domain behavior"
2. Add **simple before/after** example (optional but strong):
   - Before: C# with separate FluentValidation rules, Stateless state machine, scattered business logic
   - After: 15-line .precept file

**Effort:** 3 hours (research + copy)

---

### Phase 3: Feature Positioning (Should-Have)

**Goal:** Communicate capabilities without becoming a feature list.

**Changes:**
1. Add **"Key Features"** section with 3-5 bullets:
   - Unified state machines + validation + business rules
   - Real-time diagnostics in VS Code
   - AI-native tooling (MCP server, Claude integration)
   - Type-safe DSL with compile-time checking
   - Zero runtime dependencies
2. Keep bullets **benefit-focused**, not feature-focused:
   - Wrong: "Supports invariant, assert, and reject keywords"
   - Right: "Invalid states become structurally impossible"

**Effort:** 2 hours (copy)

---

### Phase 4: Section Reordering (Must-Have)

**Goal:** Match section order to developer evaluation journey.

**Current order:** Installation → Building → Testing → Documentation → Contributing  
**Optimal order:** Problem → Differentiation → Quickstart → "What is DI?" → Features → Docs → Community

**Changes:**
1. Move **Installation** and **Quickstart** to top (after problem statement)
2. Move **"What is Domain Integrity?"** before feature deep-dive
3. Move **Building/Testing** to Contributing section (developer-focused, not user-focused)
4. Keep **Documentation** link prominent (adoption phase)

**Effort:** 1 hour (restructure)

---

### Phase 5: Comparison Strategy (Nice-to-Have)

**Goal:** Handle "why not xstate/FluentValidation/Stateless?" without being defensive.

**Recommendation:** Use **implicit differentiation** — never name competitors, but position Precept as solving the problems they create:

- "Unlike separate state machine and validation libraries, Precept unifies state, data, and rules in one file."
- "Precept isn't a state machine library or a validation library — it's both, plus business rules enforcement."

**Optional:** If comparison table is needed, place it **below** quickstart with title "How Precept Compares" and focus on architectural differences (unified vs. split), not feature counts.

**Effort:** 2 hours (copy) — **Only if Shane requests explicit comparison**

---

### Phase 6: Visual Assets (Nice-to-Have)

**Goal:** Show, don't tell.

**Recommendations:**
1. **Screenshot:** VS Code extension with hover/diagnostics on a .precept file
2. **GIF/Video:** Live preview pane showing state machine visualization
3. **Diagram:** "Before Precept" (scattered validation/state/rules) vs. "After Precept" (unified .precept file)

**Effort:** 3-5 hours (design + screenshot + hosting)

---

## Section Order Proposal

Based on developer evaluation journey research:

```markdown
# Precept

[Logo]

**One-sentence hook:** A domain integrity engine for .NET that unifies state, data, and business rules.

**Problem statement:** Your validation is scattered across controllers, your state machine is split from your business rules, and your constraints are duplicated in code and tests. Precept binds all three into a single executable contract.

**Badges:** [Build] [Version] [License]

## Quick Start

1. Install VS Code extension: Search "Precept" in Extensions
2. Create `Order.precept` with 10-line example
3. Start typing — you'll get completions and diagnostics

Or use NuGet: `dotnet add package Precept`

[Link to Getting Started docs]

## What is Domain Integrity?

[Educational section explaining the unified state+data+rules model]

## Key Features

- Invalid states become structurally impossible
- Real-time diagnostics in VS Code
- AI-native tooling (MCP server, Claude integration)
- Type-safe DSL with compile-time checking
- Zero runtime dependencies

[Link to full feature docs]

## Documentation

- [Language Guide](docs/language-guide.md)
- [Runtime API](docs/runtime-api.md)
- [MCP Tools](docs/mcp-tools.md)
- [Samples](samples/)

## Community & Contributing

[Discord/GitHub/Contributing links]
```

---

## Social Proof Strategy

**Challenge:** Precept is new — can't compete on download counts or stars yet.

**Alternatives:**
1. **Test count:** "666 tests across 3 projects" (shows maturity)
2. **Sample count:** "20+ canonical domain models included" (shows real usage)
3. **Feature badges:** "Featured in Claude Marketplace" (once available)
4. **Build status:** CI passing badge (shows active maintenance)
5. **VS Code rating:** Extension rating (once published)

**Recommendation:** Use build status + test count + sample count now. Add download badges once >1k installs.

---

## Comparison to Studied Tools

| Tool       | Category Strategy        | Quickstart Position | Tooling Story         |
|------------|--------------------------|---------------------|-----------------------|
| xstate     | Category definition      | Section 2           | Stately Studio (lead) |
| Polly      | Category definition      | Section 3           | Buried                |
| Temporal   | Category definition      | Section 2 + video   | CLI (prominent)       |
| Terraform  | Category definition      | External docs       | Buried                |
| FastEndpoints | Direct positioning    | External docs       | Buried                |
| Bun        | Direct positioning       | Section 2           | Built-in (implied)    |
| Deno       | Problem-correction       | Section 2           | Built-in (implied)    |
| **Precept** | **Category definition** | **Section 1 (new)** | **Lead differentiator** |

**Insight:** No comparable tool leads with tooling as primary differentiator. This is Precept's positioning opportunity.

---

## Risks & Mitigations

### Risk 1: "Domain integrity" is too abstract
**Mitigation:** Lead with concrete problem (scattered validation/state/rules) before introducing the term. Use "What is Domain Integrity?" section to define it.

### Risk 2: Developers don't recognize they have this problem
**Mitigation:** Show before/after example — make the pain visible. "You're already writing validation, state machines, and business rules. Precept just puts them in one place."

### Risk 3: Quickstart feels too complex
**Mitigation:** Offer two paths: VS Code extension (zero code) OR NuGet package (3 lines of C#). Let developer choose their entry point.

### Risk 4: Tooling story overshadows DSL quality
**Mitigation:** Position tooling as *enabler*, not *replacement* for good DSL design. "The DSL is concise. The tooling makes it fast."

---

## Success Metrics

README revamp is successful if:

1. **Time-to-quickstart** < 60 seconds from opening README
2. **Mental model clarity** — developer understands "domain integrity" without reading docs
3. **Differentiation signal** — developer sees how Precept differs from xstate/FluentValidation within first scroll
4. **CTA completion** — developer clicks through to docs or installs extension

---

## Next Steps

1. **Shane review** — approve/reject scope recommendations
2. **Peterman handoff** — share research for brand/copy integration
3. **Content creation** — write problem statement, quickstart, "What is DI?" section
4. **Structure implementation** — reorder sections, add badges, create quickstart flow
5. **Visual assets** (if approved) — screenshot extension, create before/after diagram

---

## Appendix: Research Sources

- xstate: https://github.com/statelyai/xstate
- Polly: https://github.com/App-vNext/Polly
- Temporal: https://github.com/temporalio/temporal
- Terraform: https://github.com/hashicorp/terraform
- FastEndpoints: https://github.com/FastEndpoints/FastEndpoints
- Bun: https://github.com/oven-sh/bun
- Deno: https://github.com/denoland/deno
- TypeScript: https://github.com/microsoft/TypeScript
- Axios: https://github.com/axios/axios

Full analysis: `brand/references/readme-research-steinbrenner.md`

---

## Model Policy: Use Latest Available Versions

**Filed by:** User (via Copilot)  
**Date:** 2026-04-04T20:30:36Z  
**Status:** ACTIVE  

Always use the latest version of an available model rather than older pinned model versions. Global `defaultModel` constraint removed from `.squad/config.json` to enable automatic routing. Agent-specific overrides (Frank, Uncle Leo) remain intact.

---

## Agent Model Override: Elaine → claude-sonnet-4.6

**Filed by:** User (via Copilot)  
**Date:** 2026-04-04T20:38:09Z  
**Status:** ACTIVE  

For design and polish work, pin Elaine to Claude Sonnet 4.6 (latest available Sonnet). Applied to `.squad/config.json` via `agentModelOverrides.elaine`.

**Rationale:** Design and UI work benefits from Sonnet's balanced speed/reasoning profile over Opus. User directive captured as team memory.

---

## Decision: Mapping Table Visual Unification

**Author:** Elaine  
**Date:** 2026-04-05  
**Status:** Proposed  
**Category:** UX Design

Convert all three mapping tables in `brand/brand-spec.html` (§2.1 Reserved Verdict Colors, §2.2 Static Elements Compile-Time, §2.2 Runtime Verdict Overlay) to use identical `.sf-palette` component structure from §1.4:
- Card container with rounded corners, dark background, gradient header
- Title + subtitle describing purpose
- Grouped sections with semantic labels
- Row structure with 56px gradient swatches and info grid

Visual consistency builds trust in the specification. The row-with-swatch pattern is more scannable than tabular data for color reference. All three tables now share exact visual DNA with §1.4.

---



## Model Policy: Opus Escalation Acceptable When Needed

**Filed by:** User (via Copilot)
**Date:** 2026-04-04T20:38:55Z
**Status:** ACTIVE

Claude Sonnet 4.6 remains the default model for design and polish work. However, aggressive escalation to Claude Opus 4.6 is acceptable when Sonnet's context or reasoning capability proves insufficient for complex design decisions.

**Applied to:** Elaine agent (baseline Sonnet 4.6) + team-wide escalation policy.

**Rationale:** User directive clarifying nuanced model guidance — Sonnet handles most design polish, but Opus available for premium reasoning tasks.

---

## Decision: spm-row Horizontal Padding in Single-Row Groups

**Author:** Elaine  
**Date:** 2025-07-16  
**Section:** §2.1 Syntax Editor — Core Semantic Tokens table  
**Status:** Resolved

### Problem

In the `.spm-*` surface palette mapping layout, the CSS rule for `.spm-row` sets `padding: 14px 24px`. For multi-row groups (Structure · Indigo, Data · Bright Slate) the rows are inside an `spm-grid` wrapper, and `.spm-grid > .spm-row { padding: 12px 0; }` correctly removes the horizontal padding so only the container's `padding: 16px 24px` applies.

Single-row groups (State, Event, Messages, Comment) used bare `<div style="padding: 16px 24px;">` wrappers — no `spm-grid` class. The row's own 24px horizontal padding stacked on the container's 24px, visually indenting those rows ~48px from the section edge vs. ~24px for multi-row groups.

### Decision

Add `class="spm-grid"` to the single-row wrapper `div`s. This activates the `.spm-grid > .spm-row` override without requiring any new CSS rules or content changes.

### Pattern

**Rule:** All `.spm-row` elements must be direct children of a `.spm-grid` wrapper. Never place a `.spm-row` inside a bare div with explicit horizontal padding — the double-indent is unavoidable without the `spm-grid` class.

