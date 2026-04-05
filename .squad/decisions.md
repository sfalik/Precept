# Squad Decisions

---

## ACTIVE DECISIONS — Current Sprint

### Decision: Gold Brand Mark Exception

**Date:** 2026-04-04
**By:** Elaine (UX Designer)
**Status:** Implemented — pending Shane sign-off

#### Decision
Gold (`#FBBF24`) is permitted as a single sparse accent in the **combined brand mark only** — the third icon (tablet + state machine). It does not appear in the standalone diagram icon or the standalone tablet icon.

#### Placement
The Gold stroke represents the `because "…"` line — the human-readable rule message baked into the running system. In the SVG, it is the short horizontal line at `y=33` inside the tablet/code area: shorter than the body lines above it (stopping at x=34 vs x=40), stroke-width 1, opacity 0.65. Deliberately dim and singular so it reads as an accent, not a second brand color.

#### Why
Gold already carries this meaning in syntax highlighting: every `because` and `reject` string is Gold in the editor. The combined mark unifies the state machine and the written rule — it is the only icon that shows both halves. Adding one Gold line there extends an existing meaning rather than inventing a new one. It's a philosophical nod, not a decorative choice.

#### Constraints
- **One mark only.** The diagram icon and the tablet icon remain unchanged.
- **One line only.** Gold must not appear on structural elements (rect outlines, arrowheads, circles).
- **Not a new UI color.** This exception does not permit Gold in badges, borders, button states, status chips, or any other UI surface.
- **Not a new accent lane.** Gold remains syntax-primary. This is a narrow named exception, not a policy relaxation.
- Amber (`#FCD34D`) continues to own warning/caution semantics. The visual distance between Gold and Amber is maintained; Gold in the mark is dimmer (`opacity: 0.65`) and lives in a non-signal context (brand icon, not status UI), so no semantic collision occurs.

#### Files Changed
- `brand/brand-spec.html` — SVG updated, color key updated, prose updated in §1.3, §1.4 intro, §1.4.1, and the Rules · Gold surface section
- `.squad/skills/color-roles/SKILL.md` — Rule 2 and the Gold row updated to reflect this exception


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

### Decision: README Syntax Highlighting for Precept DSL (2026-04-05)
**Filed by:** Kramer (Tooling Dev)  
**Status:** No Change Required

README already uses the correct approach: ` ```precept ` fence for DSL code samples.

**Research findings:**
- GitHub Linguist does NOT recognize `precept` as a language identifier
- Unknown language fences render as plain monospace text without syntax highlighting
- Industry practice (Terraform, etc.) uses custom language names before Linguist support exists

**Options considered:**
- **Option A: Keep ` ```precept `** (SELECTED) — Truthful, future-proof, standard practice. Auto-highlights if/when Precept joins Linguist.
- **Option B: Use similar language tag** (e.g., `yaml`, `text`) — Rejected: misleading, provides false/inappropriate highlighting.
- **Option C: No language tag** (empty ` ``` `) — Rejected: same rendering as Option A but loses documentation value.

**Rationale:** No real improvement exists—GitHub cannot highlight unknown languages. Mislabeling would mislead readers. Current approach is already optimal and future-compatible.

**Future path:** To enable syntax highlighting, submit a Linguist PR with the TextMate grammar from `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`. All existing fences will automatically gain highlighting once merged.

---

### Decision: How We Got Here API Evolution Clarification (2026-04-05)
**Filed by:** J. Peterman  
**Status:** COMPLETE

`docs/HowWeGotHere.md` now states the product's authoring progression explicitly inside the chronology:

1. early fluent-interface experiments,
2. later public builder-pattern API,
3. current DSL-centric direction.

**Why:**
- Prevents the repo history from reading like a direct jump from an older state-machine library to the current DSL.
- Clarifies that the major shift was a change in authoring model, not just an implementation refresh.

**Scope:** Documentation correction only. No runtime or API behavior changed.

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

---

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

---

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

### 2026-04-04T21:23:19Z: User directive
**By:** shane (via Copilot)
**What:** Gold is the hero color and should have a prominent role in hero samples and syntax coloring so rule-oriented content stands out.
**Why:** User request — captured for team memory

---

### 2026-04-04T21:31:26Z: User directive
**By:** shane (via Copilot)
**What:** Relax the Gold syntax-only rule so Gold can be used sparingly as a hero color to highlight what is truly valuable and important, where appropriate. Exact wording still needs refinement.
**Why:** User request — captured for team memory

---

### 2026-04-04T21:40:08Z: User directive
**By:** shane (via Copilot)
**What:** Use the word "judicious" to describe Gold usage; Gold should be present in the tablet-only mark as well, and Emerald should own the transition arrow in both brand-mark contexts.
**Why:** User request — captured for team memory

---

### 2026-04-04T21:55:44Z: User directive
**By:** shane (via Copilot)
**What:** README restructure should retain some of the 'why this approach' in the README, keep contributor-focused developer loop content at the bottom, and work back in the phrase about treating business constraints as unbreakable precepts while keeping the message tight.
**Why:** User request — captured for team memory

---

### 2026-04-04T22:30:43Z: User directive
**By:** shane (via Copilot)
**What:** For README pass 1, use the already-settled temporary sample, tweak the tagline, and use the current brand mark for now; all three can be revisited later.
**Why:** User request — captured for team memory

---

### 2026-04-04T22:49:59Z: User directive
**By:** shane (via Copilot)
**What:** Stop using Haiku. Existing no-Haiku preference must be enforced consistently for squad routing and agent launches.
**Why:** User request — captured for team memory

---

---

# Brand Mark Icons: Spec Alignment

**Author:** Elaine (UX Designer)
**Date:** 2026-04-04
**Scope:** §1.3 Brand Mark — three forms + lockup combined icon

## Decision

Brand mark icons now follow the same semantic color and shape vocabulary as §2.2 State Diagram.

### Color changes
- **Transition arrows** use Grammar Indigo `#6366F1`, not Emerald `#34D399`. Emerald is reserved for verdict overlay per §1.4/§2.2.
- **Node borders** use Semantic Indigo `#4338CA` (initial: 2.5px, intermediate: 1.5px) per the §2.2 diagram color mapping.
- **Combined mark code page border** uses `#6366f1` (matching standalone tablet icon) instead of `#27272a` (was nearly invisible against `#1e1b4b` ground).

### Shape changes
- **Destination state node** changed from circle (Initial shape) to rounded rectangle (Intermediate shape) — circles are reserved for Initial nodes per §2.2 lifecycle roles.

### Color key
Updated to reflect actual icon palette: Semantic `#4338CA`, Grammar `#6366F1`, Accent `#818CF8`, Ground `#1E1B4B`.

## Rationale

Icons are abstractions, but they still speak the spec's locked visual language. Using Emerald for transitions and circles for non-initial states contradicted §2.2's explicit rules. The combined mark's code page border at `#27272a` created an undesirable fade-out effect that undermined the icon's legibility.

## Pattern established

**Brand mark icons inherit their parent surface's semantic rules.** If a brand mark depicts a state diagram, it uses §2.2 diagram colors and shapes. If it depicts a code page, it uses §2.1 syntax colors. The icons don't get to invent their own palette — they are small-scale renderings of the locked system.

---

---

# Decision: Gold Brand Mark Exception

**Date:** 2026-04-04  
**By:** Elaine (UX Designer)  
**Status:** Implemented — pending Shane sign-off  

## Decision

Gold (`#FBBF24`) is permitted as a single sparse accent in the **combined brand mark only** — the third icon (tablet + state machine). It does not appear in the standalone diagram icon or the standalone tablet icon.

## Placement

The Gold stroke represents the `because "…"` line — the human-readable rule message baked into the running system. In the SVG, it is the short horizontal line at `y=33` inside the tablet/code area: shorter than the body lines above it (stopping at x=34 vs x=40), stroke-width 1, opacity 0.65. Deliberately dim and singular so it reads as an accent, not a second brand color.

## Why

Gold already carries this meaning in syntax highlighting: every `because` and `reject` string is Gold in the editor. The combined mark unifies the state machine and the written rule — it is the only icon that shows both halves. Adding one Gold line there extends an existing meaning rather than inventing a new one. It's a philosophical nod, not a decorative choice.

## Constraints

- **One mark only.** The diagram icon and the tablet icon remain unchanged.
- **One line only.** Gold must not appear on structural elements (rect outlines, arrowheads, circles).
- **Not a new UI color.** This exception does not permit Gold in badges, borders, button states, status chips, or any other UI surface.
- **Not a new accent lane.** Gold remains syntax-primary. This is a narrow named exception, not a policy relaxation.
- Amber (`#FCD34D`) continues to own warning/caution semantics. The visual distance between Gold and Amber is maintained; Gold in the mark is dimmer (`opacity: 0.65`) and lives in a non-signal context (brand icon, not status UI), so no semantic collision occurs.

## Files Changed

- `brand/brand-spec.html` — SVG updated, color key updated, prose updated in §1.3, §1.4 intro, §1.4.1, and the Rules · Gold surface section
- `.squad/skills/color-roles/SKILL.md` — Rule 2 and the Gold row updated to reflect this exception

---

---

# Decision: Brand Mark Color Revision — Gold Judicious + Emerald Arrows

**Date:** 2026-04-05  
**Author:** Elaine (UX Designer)  
**Requested by:** Shane

## What Changed

### 1. Transition Arrow → Emerald (#34D399)
All three brand mark SVGs that carry a transition arrow now use Emerald for the arrow line and arrowhead. Previously both were Indigo (`#6366F1`). Emerald is already the "allowed flow / enabled transition" color across the product — the mark should say the same thing.

**Affected SVGs:**
- State + transition mark (standalone): arrow line + arrowhead
- Combined mark (tablet + state machine): arrow line + arrowhead
- Wordmark lockup icon (combined form, header context): arrow line + arrowhead

### 2. Gold — Less Muted, Now in Tablet Mark Too
**Combined mark:** Gold `because` line raised from `stroke-width="1" opacity="0.65"` to `stroke-width="1.5" opacity="0.9"`. It was too easy to overlook; the remembered rule should register.

**Tablet-only mark:** The shortest bottom line (the `because`-text position) is now Gold `#FBBF24` at `opacity="0.85"`, up from a muted zinc line at `opacity="0.4"`. This gives the tablet mark a family-consistent Gold signal without adding structural noise.

**Design rationale:** The word "judicious" was Shane's framing — Gold earns its place by being sparse and meaningful. Giving it presence in the tablet mark improves family consistency. The restraint is still intact: it's one line, at the bottom, in a role that encodes human language.

### 3. Language: "Judicious" replaces "Narrow exception"
All copy and skill rules that said "narrow exception" or "combined mark only" for Gold now say "judicious exception" or "tablet and combined marks." The tone shift from "barely allowed" to "deliberately placed" aligns with how the mark system actually works.

## Color Key Updated
The brand mark color key now shows Emerald as a fifth entry (Transition #34D399) alongside the existing four, and Gold's annotation reads "(judicious — tablet & combined)".

## Files Changed
- `brand/brand-spec.html` — three SVG marks, wordmark lockup, color key, §1.4 Gold description, §1.4 usage rules table, §2.1 Gold copy
- `.squad/skills/color-roles/SKILL.md` — Emerald row, Gold Syntax Accent row, Rule #2

## Guardrails Maintained
- Emerald is not a second structural lane — it only appears on the transition arrow (one element per mark)
- Gold does not appear on the state-only mark (no `because` line there; nothing to represent)
- State shapes and Indigo/Violet structure unchanged

---

---

# Decision: Elaine's Direct Contribution Role in README Rewrite

**Author:** Elaine (UX Designer)  
**Date:** 2026-04-07  
**Status:** Guidance — not yet implemented  
**Context:** Peterman holds primary ownership of the README rewrite. This document defines where Elaine contributes directly vs. where she reviews.

---

## Decision

Elaine co-authors six specific areas. Peterman authors everything else. No section gets final sign-off without Elaine's explicit approval on the areas listed below.

---

## Elaine's Direct Contribution Areas

### 1. Title Block — Above-the-Fold Composition

Elaine owns the layout spec, not the copy:
- Logo at **48px** (not 64px SVG full) to clear the 550px mobile viewport with room for one badge row, one blockquote, and the two-sentence hook
- **Badge count: 3 maximum** — NuGet version, MIT license, build status, in that order
- Badge alt text template: each must include the actual value (e.g. `NuGet version 1.2.3`, `Build: passing`)
- The complete title block must be tested at **400px, 550px, and 800px** viewport widths before finalizing

Peterman writes the copy. Elaine validates the layout passes the mobile test.

---

### 2. Hero Format Template — Quick Example Section

Elaine owns the **form**, not the code:
- Two labeled blocks under `## Quick Example`: `**The Contract** (filename.precept):` and `**The Execution** (C#):`
- DSL block: 18-20 lines, **≤60 characters per line** — this is a hard layout constraint, not a style preference
- C# block: 5 lines maximum, no comments, no imports
- Both blocks tagged with language identifiers (` ```precept `, ` ```csharp `)

The 60-character line constraint shapes what Peterman can write in the DSL hero. He needs Elaine's template before authoring the hero code. **This is a dependency, not a review.**

---

### 3. Getting Started — CTA Structure

Elaine directly authors the structural template for Getting Started:
- Numbered 1/2/3 sequence is Elaine's format (not Peterman's preference)
- Step 1 must install VS Code extension — not NuGet, not Copilot plugin
- Step 3 must carry the "when you're ready" qualifier to signal it is a progression, not a simultaneous decision
- Copilot plugin is **removed from Getting Started entirely** — it surfaces only in Section 4 (AI-Native Tooling)

The words inside each step are Peterman's. The order, the hierarchy signal, and the single-primary-CTA enforcement are Elaine's.

---

### 4. All Section Headings

Elaine directly approves every H2 and H3 heading before the rewrite ships:
- No emoji at heading start (screen reader and AI parseability requirement)
- Headings are descriptive, not clever — "Live Editor Experience" not "World-Class Tooling"
- H1 → H2 → H3 with no level skips — verified by Elaine after the full draft exists

This is a two-pass process: Peterman drafts headings; Elaine audits and edits them. Peterman does not unilaterally finalize headings.

---

### 5. Visual Separators and Prose Scannability

Elaine owns the formatting rules applied throughout:
- `---` horizontal rule between every major H2 section — Elaine places these, not Peterman
- Prose paragraphs: **maximum 2-3 sentences** — Elaine edits any paragraph that exceeds this
- Feature lists: **bullets, not prose** — Elaine converts any 3+ item run into a bulleted list
- Blockquote callouts (`> **Note:** ...`) for important asides — Elaine decides which asides qualify

Peterman writes the content. Elaine applies and enforces the scannability format throughout the full document.

---

### 6. Contributing Section — Formatting Pass

Elaine owns the formatting of the Contributing section:
- All build commands in properly tagged ` ```bash ` blocks
- Section placed **after Learn More, before License** — Elaine enforces this position
- No contributor content bleeds into the primary user flow above it
- Section opens with a single-sentence scope statement ("Precept is built with .NET 10.0 and TypeScript. The VS Code extension, language server, MCP server, and runtime are all in this repository.")

---

## What Peterman Owns Exclusively

- Hook copy (brand voice: the definition blockquote, positioning sentence, clarifier sentence)
- Section 4 differentiation copy (AI-Native Tooling, Unified Domain Integrity, Live Editor Experience — body content)
- Learn More link list (link text and ordering)
- License section
- Overall section order (already locked in the restructure proposal)
- Hero domain selection (Order Fulfillment vs. Subscription Billing — deferred to Shane)

---

## Collaboration Protocol

- Peterman drafts full sections → Elaine does a formatting pass → no copy changes, only structural/formatting edits
- Any conflict between Elaine's formatting pass and Peterman's intended copy hierarchy gets escalated to Shane
- Heading audit happens on the full draft, not section by section
- The 60-char line constraint in the hero is a **hard block** — Peterman cannot finalize hero code without Elaine's line-length validation

---

---

# Decision: Elaine's Gate Role in README Rewrite Flow

**Date:** 2026-04-07  
**By:** Elaine (UX Designer)  
**Status:** Recommendation — pending Shane acknowledgment  

---

## Decision

Elaine enters the README rewrite at **two specific gates**, not throughout the writing process.

### Gate 1 — Pre-Draft Hero Domain Confirmation (now)

Before Peterman writes a line, the hero domain must be locked. Elaine's role: confirm the selected domain satisfies the "universally relatable" requirement (no SaaS jargon, immediately recognizable by any .NET developer). This is a 5-minute validation, not a design pass. Per the `proposal-gate-analysis` skill: hero domain is always a Gate-Before-Start item. Changing it after the draft undoes the draft.

### Gate 2 — Post-Draft UX Compliance Audit

After Peterman delivers the first draft, Elaine runs a formal pass against the 16 hard constraints already embedded in the proposal. The audit is pass/fail per constraint — not a copy edit, not a rewrite. Peterman resolves any failures. Shane signs off after both Peterman and Elaine clear.

---

## Elaine Owns (Structural UX Layer)

| # | What | Why |
|---|------|-----|
| 1 | Above-the-fold audit (550px viewport) | Constraint #1 |
| 2 | Viewport resilience (no horizontal scroll at 400px) | Constraint #2 |
| 3 | CTA hierarchy enforcement | Constraint #3 |
| 4 | Heading hierarchy audit (H1→H2→H3, no skips) | Constraints #4, #5 |
| 5 | Scannability mechanics (prose length, bullets, separators) | Constraints #6, #7 |
| 6 | Hero line length audit (≤60 chars/line) | Constraint #8 |
| 7 | Progressive disclosure order | Constraint #10 |
| 8 | AI/human readability compliance (code block tags, link text, alt text) | Constraints #11–#14 |

---

## Peterman Owns (Brand/Copy Layer)

- Copy quality, brand voice, tone
- Positioning language (exact form of category claim)
- Hero sample content (DSL lines, within Elaine's line-length constraints)
- Section content decisions
- Badge selection and ordering
- Link anchor text (Elaine ensures form, Peterman ensures content)

---

## The Clean Split

**Peterman writes. Elaine audits structure.** These are separate passes, not concurrent collaboration. Peterman does not need Elaine present while writing. Elaine does not write copy. If there is a heading skip, Elaine flags it — Peterman corrects it. If the CTA hierarchy is correct but the positioning language is weak, Peterman owns that.

---

## Why This Matters

Elaine's constraints were treated as hard non-negotiables in the proposal precisely because they are prerequisites, not suggestions. But constraints on paper are not enforcement. The only way they hold is if the constraint-holder reviews the final artifact. A proposal that satisfies all 16 constraints structurally, delivered as a README that satisfies none of them, is a failure. Elaine's post-draft gate is what closes that loop.

---

## Gate Sequence (Full)

1. Shane — hero domain lock (Gate-Before-Start)
2. Peterman — writes draft
3. Elaine — UX compliance audit (post-draft)
4. Peterman — resolves Elaine's flagged issues
5. Frank — architectural accuracy check (contributor section, commands)
6. Shane — final sign-off

---

---

# Decision: README Form/Shape Pass

**Author:** Elaine (UX)  
**Date:** 2026-04-07  
**Status:** Applied

## What Changed

Form/shape improvements to README.md for improved scannability and viewport efficiency:

1. **Title block** — Removed emoji from H1, shortened badge labels ("NuGet version" → "NuGet"), tightened definition blockquote (removed invisible Unicode spacing)

2. **Quick Example** — Shortened C# variable names (`definition` → `def`, `engine` → `eng`, `instance` → `inst`) for visual compactness; kept vertical layout (side-by-side tables don't render code blocks reliably on GitHub)

3. **Getting Started** — Moved prerequisite to a single bold line (was a blockquote callout), cut redundant intro sentence, tightened step prose to single sentences where possible

4. **What Makes Precept Different** — Collapsed three H3 subsections into bold inline headers with em-dash lead-ins; preserved the bullet list for Unified Domain Integrity; cut granular bullet details from AI-Native and Live Editor (now single-sentence summaries)

5. **Learn More** — Changed from bullet list to table format for better scanability and alignment

6. **Contributing** — Collapsed language server and VS Code extension build details into a simpler two-command code block; trimmed quick-reference table to essential rows

## Rationale

The previous draft had good content but felt dense. Each section competed for attention. These changes improve heading rhythm, reduce vertical scroll, and make the README feel "shaped" rather than "stacked."

## What I Did Not Change

- Core messaging and word choices (Peterman's domain)
- Section order
- The brand mark (kept current state per task brief)
- Hero domain (Order) — marked as temporary but acceptable

---

---

# Decision: Shape-First README Pass — Elaine's Position

**Filed by:** Elaine  
**Date:** 2026-04-08  
**Status:** Recommendation — Pending Shane sign-off  
**Context:** Shane asked whether Elaine should produce a form/shape skeleton before Peterman writes the README copy.

---

## Recommendation

**Targeted scaffold — not a full skeleton pass.**

The restructure proposal already IS the structural spec at high fidelity. A full skeleton pass reproduces that work in a different format. The targeted intervention that adds genuine value is a **hero code block scaffold**: an annotated Markdown stub showing Peterman exactly what viewport-safe DSL looks like in 18-20 lines at ≤60 chars per line — with constraint callouts embedded as HTML comments.

---

## What Elaine Would Produce in Shape-First Mode

A `README-scaffold.md` file (not README.md itself) containing:

- All headings in place at correct H1/H2/H3 levels
- Badge row slot with alt-text annotation
- Blockquote definition slot
- Two-sentence hook slot with word-count annotation
- Hero DSL block as a commented stub:
  ```
  <!-- ≤20 lines | ≤60 chars per line | viewport constraint #8 -->
  <!-- Required: precept decl, 2-3 fields, 3 states (initial marked), 2 events, 1 guard, 1 invariant/assert -->
  ```
- 5-line C# execution block slot
- All section headings and visual separators in place
- Constraint callout comments above each constrained content slot

**What it is not:** copy. No body prose. No positioning language. Peterman owns all of that.

---

## Does This Reduce or Increase Churn?

**Reduces churn marginally for the full file; meaningfully for the hero block.**

The proposal is specific enough that section-level violations are unlikely. The real risk is the hero — Peterman writes a sample, we find at constraint audit that line 7 is 78 characters, and that means a content rewrite after copywriting is done. An annotated hero slot catches that class of issue before pen touches paper.

For everything outside the hero, the scaffold is a convenience, not a necessity.

---

## Peterman's Counter-Position

Peterman filed a recommendation (`.squad/decisions/inbox/j-peterman-shape-first-readme.md`) that the skeleton pass is redundant and adds an extra round trip. His argument is sound for the full file. The disagreement is narrow: Elaine's position is that the hero block specifically benefits from a pre-draft structural artifact. Peterman's position is that this is a "single targeted question, not a skeleton pass."

**They're describing the same intervention at different scopes.** Shane can resolve this by scope-limiting: no full skeleton, but a specific hero consultation before Peterman writes that section.

---

## What Must Be Locked Before Copywriting Starts

| Item | Status | Action Required |
|------|--------|----------------|
| Section order | Locked in proposal | None |
| 16 hard constraints | Locked | None |
| Badge count (3 max) | Specified, not signed off | Shane confirms |
| **Hero domain** (Order Fulfillment vs. Subscription Billing) | **Unresolved — deferred to Shane** | **Shane decides** |
| Time Machine hero — retire it | Proposed, not explicitly signed off | **Shane signs off** |
| Above-the-fold test baseline | Specified at 550px | Confirm before final |

**Hero domain selection is the hard gate.** Everything else can proceed without Elaine's scaffold. The hero cannot.

---

## Correct Gate Sequence If Shane Approves Scaffold

1. **Shane** — selects hero domain; signs off on Time Machine retirement
2. **Elaine** — produces `README-scaffold.md` (scope: hero stub + heading skeleton only; ~30 minutes)
3. **Peterman** — writes README copy into the scaffold
4. **Elaine** — constraint audit: 16-row pass/fail table
5. **Peterman** — resolves any failures
6. **Shane** — final sign-off

---

## Correct Gate Sequence If Shane Skips the Scaffold

1. **Shane** — selects hero domain; signs off on Time Machine retirement
2. **Elaine** — single hero consultation: what domain fits cleanest in ≤60 chars/line?
3. **Peterman** — writes README against the proposal directly
4. **Elaine** — constraint audit: 16-row pass/fail table
5. **Peterman** — resolves any failures
6. **Shane** — final sign-off

Both paths are valid. The scaffold path prevents one class of hero-related rework. The no-scaffold path is one pass shorter. The hero domain decision is required in both cases.

---

---

# Decision: Gold — Judicious Use, Tablet Mark Inclusion, Emerald Transition Arrows

**Date:** 2026-04-07  
**By:** J. Peterman (Brand/DevRel)  
**Status:** Implemented — Shane sign-off requested  

## Summary

Three related changes to the brand color system, directed by Shane:

1. **Gold policy language** — "narrow exception" replaced with "judicious exception" everywhere Gold's limited use is described. The word governs the policy; it is not decorative.

2. **Tablet mark carries Gold** — The standalone tablet icon now includes a single Gold `because` line (same signal as the combined mark). The family of marks reads consistently: where a tablet form appears, the remembered rule is present. The combined mark's Gold opacity was also boosted from 0.65 → 0.9 (was visually muted).

3. **Transition arrows are Emerald** — The §2.2 state diagram SVG (marker, edge lines, legend) and the §2.2 diagram color mapping swatches now use Emerald `#34D399`. The "State + transition" brand mark icon was already Emerald; the combined mark icon has also been updated. Emerald owns transition directionality across the system — "allowed flow," not "grammar structure."

## Rationale

Gold's prior scope ("combined brand mark only") was a Elaine-era constraint based on a philosophy of maximum restraint. Shane's direction expands the scope minimally: the tablet form is the vessel for written rules across all three marks, so Gold belongs there wherever the tablet appears. The combined mark is primary, but the standalone tablet is the same conceptual object — it would be peculiar for it to be silent about the rule it represents.

Emerald on transition arrows is semantically correct: Emerald means "allowed, do, go." A transition arrow is the diagram's way of saying the system may move. Indigo on arrows was a structural reading (grammar connective tissue); Emerald is a semantic reading (the allowed path forward). The latter is more honest.

## Gold Policy As It Stands

- **Primary:** `because` / `reject` string content in syntax highlighting. Always.
- **Judicious exception:** A single `because` line in the tablet mark and combined mark. One line. No other uses.
- **Never:** badges, borders, button states, status chips, general UI emphasis, signal overlay.

## Files Changed

- `brand/brand-spec.html` — tablet SVG, combined mark, §2.2 SVG diagram, §2.2 color mapping, §1.4 palette card, §1.4.1 cross-surface table, §2.1 Rules · Gold description, brand paragraph, color key
- `.squad/skills/color-roles/SKILL.md` — (already updated in prior session; confirmed current)

---

---

# Decision: Canonical hero snippet now lives in brand-spec §2.6

**Date:** 2026-04-08
**By:** J. Peterman (Brand/DevRel)
**Status:** Complete — Ready for review & merge to decisions.md

---

## Decision

Reworked `brand/brand-spec.html` §2.6 into the canonical reusable hero-snippet artifact for the current README example. The section now mirrors the live README sample verbatim, keeps its **TEMPORARY** status explicit, and provides cross-surface reuse rules for README, VS Code Marketplace, NuGet, Claude Marketplace, and AI prompt contexts.

## Why this was necessary

The previous §2.6 had already established the right intent, but it had drifted from reality in four important ways:

1. The DSL rendering no longer matched the live README exactly (`>= 0` had drifted to `= 0`).
2. The C# execution block omitted the closing result comment that exists in README.
3. The guidance treated color rendering as canonical, even though README/NuGet/AI contexts depend on plain text first.
4. It referenced a nonexistent `samples/Order.precept` file as though the temporary hero already had a raw sample file.

For a reusable brand artifact, copy fidelity matters more than decorative rendering. The source of truth must match the live public surface exactly.

## What §2.6 now contains

- **Status framing:** approved for reuse now, still explicitly temporary
- **Exact canonical text:** verbatim DSL block (20 lines, 60-character max) and verbatim C# execution block
- **Cross-surface usage table:** README, VS Code Marketplace, NuGet, Claude Marketplace, AI prompt context
- **Portable constraints:** preserve exact wording, preserve blank lines, keep plaintext legibility primary, update README + brand spec together
- **Refresh triggers:** usability review, language/API drift, or deliberate promotion of a dedicated hero sample file
- **Source references:** README as live surface, §2.6 as reuse reference, explicit note that there is no standalone `samples/` file yet

## Team-relevant decision

Until the usability review says otherwise, the current README Order example is the temporary canonical hero sample across brand surfaces. Treat the exact text plus its reuse rules as locked; treat the domain itself as provisional.

## Impact

- **README:** No content change required; the live README already contains the approved temporary hero text.
- **Marketplace copy:** Teams can now copy from §2.6 without inventing surface-specific variations.
- **AI-facing docs/prompts:** Plaintext reuse is now explicitly blessed, which matches how agents actually consume the sample.

## Next steps

1. Merge this into `decisions.md` after review.
2. Use §2.6 as the source when updating README-adjacent marketplace copy.
3. Revisit only if usability testing or language/API changes force a new hero.

---

---

# Decision: README Rewrite Execution Path

**Author:** J. Peterman  
**Date:** 2026-07-14  
**Status:** Awaiting Shane sign-off on one gating decision

---

## Situation

The restructure proposal has been through three review passes (Frank, George, Uncle Leo — all approved with required changes). The revised proposal (2026-04-07) already incorporates every required change from all three reviewers. The proposal is ready to execute. No further review pass is needed.

## Gating Decision for Shane

**The only decision that blocks drafting §2 (Quick Example / hero) is: what is the hero domain?**

Three options the proposal has already pre-analyzed:
- **Order Fulfillment** — universally relatable; Elaine's preference
- **Subscription Billing** — 18 DSL statements; Peterman's original recommendation
- **Loan Application** — already exists in samples; complex but proven

The rest of the README (§0 title block, §1 hook, §3 getting started, §4 differentiation, §5 learn more, §6 contributing, §7 license) can be fully drafted before this decision is made. Hero domain only gates §2.

**Recommended path:** Shane names the hero domain, then the agent drafts §2 in the same pass as everything else and delivers a complete README ready for a single review pass.

## What Must Be Preserved

- Dictionary definition blockquote — `> **pre·​cept** *(noun)*: A general rule intended to regulate behavior or thought.` — confirmed in proposal §Hook
- C# API examples (trimmed: current is 45+ lines, target is 5 lines flat)
- Dev loop contributor table (the `What you changed / What to do / Window reload?` table in Current README § Local Development Loop) — good content, fits directly into the new Contributing section
- .NET 10 SDK prerequisite — exists in current README; must appear as a callout before Getting Started step 1
- License badge + MIT statement
- The `CreateInstance` overload pattern (current README shows it correctly; George confirmed both overloads are real)

## What Gets Cut

| Current section | Fate |
|---|---|
| `📚 Sample Catalog` (21-row table + feature matrix) | Remove from README; link to external docs in Learn More |
| `🛠️ World-Class Tooling` (150-word prose block) | Cut prose; distill to 4 bullets under §4c Live Editor Experience |
| `🧠 The Problem It Solves` | Absorbed into §1 Hook second sentence; the diagnosis lives there, not as a standalone section |
| `🏗️ The Pillars of Precept` (4 subsections) | Content folded into §4b Unified Domain Integrity bullets; no standalone section |
| `🤖 Designed for AI` (5 prose bullets) | Absorbed into §4a AI-Native Tooling; prose format replaced by bullets |
| `🤖 MCP Server` section (current standalone) | Folded into §4a AI-Native Tooling |
| Emoji in all heading prefixes (🚀 💡 📚 🛠️ 🤖 🧠 🏗️) | All removed — Elaine constraint #5: no emoji at heading start |
| Installation table + Copilot plugin steps in Getting Started | Plugin steps moved to §4a; Installation simplified to NuGet in Getting Started step 3 |
| `💡 The "Aha!" Moment` heading | Replaced by `## Quick Example` — descriptive, not clever |

## Draft Sequencing (Minimal Churn Path)

**Round 1 — No dependencies, draft immediately:**
1. §0 Title Block + Badges (logo placeholder, 3 badges, badge alt text)
2. §1 Hook (definition blockquote + 2-sentence positioning)
3. §3 Getting Started (template is fully spec'd in proposal)
4. §4 What Makes Precept Different — all three subsections (copy spec is detailed)
5. §5 Learn More (link list, external URLs TBD)
6. §6 Contributing (dev loop table + build commands, adapted from current README)
7. §7 License

**Round 2 — After Shane names the hero domain:**
1. §2 Quick Example — draft the 18-20 line DSL hero + 5-line C# execution block

**Round 3 — Shane single review pass:**
- Full README in proposed structure
- Check viewport compliance (≤60 char lines, above-the-fold test at 550px)
- Confirm hero domain/sample reads as real business logic, not toy demo
- Confirm badge values (NuGet version, build status URL)
- Confirm external doc links (placeholder during draft, real URLs before merge)

**Commit after Shane approves Round 3.**

## Required Changes from Reviews — Status Check

All required changes from Frank, George, and Uncle Leo are incorporated into the revised proposal (2026-04-07). No pre-rewrite patch pass needed.

| RC | Status |
|---|---|
| Frank RC-1 (C# API chain) | Fixed by George G1: CreateInstance overloads documented correctly, RestoreInstance removed |
| Frank RC-2 (Step 3 title) | Fixed: "Add the NuGet Package" throughout |
| Frank RC-3 (Section 4c name) | Fixed: "Live Editor Experience" |
| Frank RC-4 (AI claims) | Fixed: "AI agents can validate, inspect, and iterate..." |
| George G1 (RestoreInstance) | Fixed |
| George G2 (Prerequisites) | Fixed: .NET 10 SDK prerequisite in Getting Started template |
| George G3 ("replacing") | Fixed: "eliminating the fragmentation" |
| Uncle Leo RC-1 (H3 vs bold labels) | Fixed: Section map now shows `[bold lead-in: The Contract / The Execution]` |
| Uncle Leo RC-2 (context reminder) | Fixed: Explicit one-sentence context reminder in Getting Started template |

---

---

# Decision: README Restructure Proposal — Shane Feedback Revision

**Author:** J. Peterman  
**Date:** 2026-04-07  
**Source:** Shane's feedback pass on `brand/references/readme-restructure-proposal.md`  
**Status:** Inbox — awaiting Shane sign-off  

---

## Decisions Established by This Revision

### 1. "By treating your business constraints as unbreakable precepts" — Retained and Integrated

**Decision:** The phrase is not cut. It is folded as the mechanism half of the positioning sentence.

**Approved form:**
> **Precept is a domain integrity engine for .NET.** By treating your business constraints as unbreakable precepts, it binds state, data, and rules into a single executable contract where invalid states are structurally impossible.

**Rationale:** The phrase earns the brand name — it connects the dictionary definition to the mechanism in one beat. As a standalone third sentence it was redundant; as a participial lead-in to the positioning sentence it carries weight without bloating.

**Constraint:** The mechanism phrase must always appear as a participial fold into the positioning sentence. It is not a standalone tagline. It is not a separate sentence.

---

### 2. "Why This Approach" Content Stays Partially in the README

**Decision:** Not all philosophical/rationale content is deferred to docs. The core reasoning belongs in the README.

**What stays in README:**
- 1–2 sentences in `§ Unified Domain Integrity` explaining *why* co-location prevents disagreement between separately-managed rules
- The "prevention, not detection" framing

**What defers to docs:**
- Full pipeline mechanics (6-stage fire, working copy behavior)
- Construct catalog with full syntax reference
- Extended design rationale and comparison to alternative approaches

**Rule:** If a developer needs it to understand why Precept exists, it stays. If they need it to use Precept correctly, it goes to docs.

---

### 3. Contributing / Developer Loop — Retained at Bottom

**Decision:** The developer build loop (language server → extension auto-detect → edit → diagnostics) is explicitly retained in the README as a `## Contributing` section, positioned after `## Learn More` and before `## License`.

**This is not user onboarding.** The section is scoped to contributors. Users of Precept do not need to build the language server. Contributors do.

**Content minimum:** `dotnet build`, language server build command, `npm run compile` / `npm run loop:local`, `dotnet test`, link to CONTRIBUTING.md.

---

### 4. Three-CTA Problem — Definition Locked

**Decision:** "Three-CTA problem" is now a defined term in this project's README vocabulary. It means: three equal-weight next steps presented simultaneously at the same decision point, with no hierarchy. The problem is not the count — three CTAs remain in the restructured README. The problem is the simultaneous presentation.

**Canonical description (for use in any related document):**
> The current README's Getting Started presents VS Code extension, NuGet package, and Copilot plugin as three simultaneously equal next steps with no hierarchy. The problem is not having three CTAs — it is having three primary CTAs at the same decision point.

---

*Written to inbox by J. Peterman, 2026-04-07.*

---

### README Pass 1 — Structural Decisions Made During Rewrite

**Date:** 2026-04-08
**By:** J. Peterman
**Status:** COMPLETE — Shane awareness

---

#### Decision 1: Hero Domain — Order (Temporary)

**What:** Used a simplified Order domain (`order.precept`) as the pass-1 hero. Four states (New, Paid, Shipped, Delivered), one invariant, two event args, one guard, one reject, 20 lines, all lines ≤60 characters.

**Why:** Neither Time Machine nor LoanApplication fit the 60-char line constraint without either stripping personality (Time Machine) or structural complexity (LoanApplication). The Order domain is the cleanest compliant fit in accessible context. Shane's instruction "already-settled temporary sample" confirmed this is explicitly a placeholder.

**Status:** Temporary. The Phase 3 hero creative brief process (8 domain ideas, Duel at Dawn / Heist Safe ranked highest) remains open and should replace this in a future pass.

---

#### Decision 2: Hook Tagline — One Sentence

**What:** Reduced the proposal's two-sentence hook to one: "It binds state machines, validation, and business rules into a single executable contract — eliminating the fragmentation that comes from managing them separately."

**Why:** Shane's pass-1 tightening instruction + George's G3 (remove "replacing") + Uncle Leo's WC redundancy note. The "unbreakable precepts" phrase was cut for pass-1 compression; it can be reintroduced in pass 2 as a participial phrase if the team wants it back.

**Open:** Whether "unbreakable precepts" reconnects the brand name to the mechanism in the hook is a pass-2 copy decision. Not blocking.

---

#### Decision 3: Copilot Plugin CTA in AI-Native Tooling

**What:** Added one-sentence Copilot plugin install call to action in "AI-Native Tooling": "Install the plugin via `Chat: Install Plugin From Source` or enable it from the plugin marketplace."

**Why:** The plugin CTA needed to appear somewhere in the README (it was removed from Getting Started per the proposal). "AI-Native Tooling" is the correct adoption-stage location per the CTA strategy.

**Status:** Placeholder instruction. The exact install path may need updating when the plugin is marketplace-published.

---

#### Decision 4: Learn More links use relative doc paths

**What:** `Learn More` links point to `docs/PreceptLanguageDesign.md`, `samples/`, `docs/McpServerDesign.md`. `CONTRIBUTING.md` is referenced but does not exist yet.

**Why:** No published docs site URLs exist. Relative paths to existing repo docs are the best available option.

**Open:** Replace with absolute docs site URLs when the site ships. Create `CONTRIBUTING.md` to resolve the dangling reference.

---

---
date: 2026-04-08T00:00:00Z
author: j-peterman
status: ready_for_shane
phase: readme_copy_refinement
---

---

# README Copy Polish Pass — Decision Summary

**Decision:** Complete focused copy polish on README.md post-Elaine's structural design pass. Preserve all structural improvements. Tighten prose only for clarity, cadence, and brand voice.

## What Changed

| Line | Original | Tightened | Rationale |
|------|----------|-----------|-----------|
| 8 | "It binds state machines, validation, and business rules into a single executable contract — eliminating the fragmentation that comes from managing them separately." | "By treating business constraints as unbreakable precepts, it binds state machines, validation, and business rules into a single executable contract where invalid states are structurally impossible." | Integrates "unbreakable precepts" mechanism phrase (from design review) into main positioning sentence. Connects brand name to core idea. Removes vague "fragmentation" → uses concrete "structurally impossible" outcome. |
| 79 | "MCP server (5 tools), GitHub Copilot plugin, and a language server that gives AI agents structured access…" | "MCP server with 5 core tools, GitHub Copilot plugin, and language server give AI agents structured access…" | Removes parenthetical restatement. Fixes parallel structure ("give" → plural subject). Preserves all three components and tool count. |
| 81 | "Precept co-locates them: constraints live next to the state they govern." | "Precept unifies them into one definition." | Same meaning, 9 fewer words. "Unify" is more direct than "co-locate/constraints live next to." Clearer antecedent. |
| 84 | "One file, complete rules…" | "One file, all rules…" | "All rules" matches "together" grammatically. Slightly more precise. |
| 85 | "…without mutation" | "…without executing it" | Avoids misreading "mutation" as data structure mutation. Actual meaning: avoiding execution. More precise verb. |
| 88 | "Context-aware completions, semantic highlighting, inline diagnostics, and a live state diagram preview." | "Completions, semantic highlighting, inline diagnostics, and a live state diagram preview in VS Code." | Removes redundant "context-aware" (implicit in completions). Adds "in VS Code" (concrete location, serves mobile navigation and AI reader). |

## Design Decisions Preserved

- ✅ Elaine's structural hierarchy (Title → Hook → Quick Example → Getting Started → Differentiation → Learn More → License)
- ✅ Hero code sample (Order fulfillment business domain, 18-20 lines DSL, 5-line C# execution)
- ✅ CTA hierarchy (VS Code extension as primary, NuGet as secondary)
- ✅ All technical claims, links, and prerequisite language
- ✅ Brand positioning: "domain integrity engine" + mechanism phrase

## Key Phrase Decision

The phrase "treating business constraints as unbreakable precepts" was flagged in earlier drafts as potentially redundant. On review, it does distinct work: it connects brand name to mechanism in one moment, making the name feel earned rather than invented. Rather than cutting, integrated it as a participial phrase in the main positioning sentence.

## Files Modified

- `README.md` — 5 text edits, all prose tightening (no structural changes)
- `.squad/agents/j-peterman/history.md` — appended learning entry

## Status

Ready for Shane sign-off. README is near-signoff state: structure locked by Elaine, prose tightened for clarity and cadence, all approved messaging preserved.

## Validation

- ✅ Approved message intact ("domain integrity engine" + "unbreakable precepts" mechanism)
- ✅ Key phrase about business constraints preserved and integrated naturally
- ✅ Copy factual, aligned with prior reviewer corrections
- ✅ Brand voice consistent (authoritative, matter-of-fact, no hedging)
- ✅ No structural changes introduced
- ✅ No new hero domain introduced (Order sample retained)

---

---

# Decision: README Proposal Review Gap Pass — All Required Changes Applied

**Author:** J. Peterman  
**Date:** 2026-04-07  
**Status:** Applied — ready for Shane sign-off  
**File modified:** `brand/references/readme-restructure-proposal.md`

---

## Summary

Seven required changes from the Frank/George/Uncle Leo review round were documented in the proposal's trim summary but had not been applied to the proposal body. All seven are now corrected.

---

## Changes Applied

| Reviewer | Item | Old | New |
|----------|------|-----|-----|
| Frank RC-4 | AI capability claim | "AI agents can author, validate, and debug `.precept` files without human intervention." | "AI agents can validate, inspect, and iterate on `.precept` files through structured tool APIs." |
| George G1 | Fabricated API name | `engine.CreateInstance(...)` (or `RestoreInstance`) | `engine.CreateInstance(savedState, savedData)` (or `engine.CreateInstance()` for new entity) |
| George G2 | Missing prerequisite | Getting Started steps with no .NET prereq | Explicit `.NET 10 SDK` prerequisite blockquote before step 1 |
| Uncle Leo RC-2 | Missing context reminder | "Here it's implicit in step 1's description..." (not actually present) | Explicit one-sentence context reminder as opening line of Getting Started template |
| Uncle Leo WC-1 | Badge-wall phrasing | "badge walls signal maintenance anxiety, not quality" | "additional badges add visual noise without adding signal at the awareness stage" |
| George G3 | Overclaiming hook | "It replaces three separate libraries..." | "It eliminates the fragmentation that comes from managing state, validation, and business rules separately." |
| Uncle Leo WC-3 | Unlabeled tagline | "One file. Every rule. Prove it in 30 seconds." (ambiguous: proposal copy or README copy?) | *(Proposed README tagline — confirm or substitute during rewrite):* One file. Every rule. Prove it in 30 seconds. |

---

## Decision for Shane

The proposal now accurately reflects the review feedback. No structural changes were required — all corrections were precision edits to content and copy specifications within the existing structure.

The proposal is ready for Shane's sign-off. The rewrite can begin.

---

## What Was Not Changed

- Frank RC-1/RC-2/RC-3 — already addressed in the prior Shane feedback pass
- Uncle Leo RC-1 (H3 vs bold lead-ins in section map) — already addressed in the prior pass
- Frank/George advisory notes (AN-1, AN-2, AN-3, Inspect args note, TransitionOutcome enum note) — non-blocking; addressed by the rewriter
- Uncle Leo WC-2 (Section 4a rationale sentence structure) — wording concern; non-blocking; addressable during rewrite

---

---

# Decision: README Restructure Trim Summary Added to Proposal

**Filed by:** J. Peterman  
**Date:** 2026-04-06  
**Status:** Proposed — pending Shane review  

---

## Decision

A "What the Restructure Trims, Compresses, Defers, or Removes" section has been appended to `brand/references/readme-restructure-proposal.md`. This section is a permanent part of the proposal document — not a separate artifact.

## Rationale

Restructure proposals communicate gains well. They communicate costs poorly. Before a rewrite begins, Shane should be able to evaluate the restructure against what it removes, not just what it adds. The trim summary makes that cost explicit and scannable.

## What the Summary Covers

The trim summary is organized into four categories:

1. **Removed entirely** — content that does not survive in any form (third hook sentence, full C# boilerplate, badge walls)
2. **Compressed** — content that stays but shrinks (49-line hero → 18-20 lines + 5-line C#; 150-word "World-Class Tooling" → 4 bullets; three front-loaded differentiation sections → one post-trial section with bullets)
3. **Repositioned** — same content, lower prominence or different section (AI tooling CTA, Time Machine hero, differentiation-before-installation → differentiation-after-installation)
4. **Deferred to external docs** — content moved from README to linked references (sample catalog table, full quickstart walkthrough, language reference, MCP docs, philosophy)

A fifth category — **Net-New Content** — captures additions the restructure requires (prerequisite note, context reminder, language tags). This ensures Shane evaluates the full delta, not just what's cut.

## Files Changed

- `brand/references/readme-restructure-proposal.md` — trim summary section appended

## Team Impact

The trim summary is a reference artifact for whoever executes the rewrite. It functions as a checklist: if any item in the "Removed Entirely" list appears in the rewritten README, something went wrong. If any item in "Deferred to External Docs" appears as embedded content (not a link), that is also a regression.

---

---

# Decision: README Collaboration Order — Shape-First vs. Draft-First

**Filed by:** J. Peterman  
**Date:** 2026-04-08  
**Status:** Recommendation — Pending Shane sign-off  
**Context:** Shane asked whether Elaine should produce a form/shape skeleton before Peterman writes the README copy.

---

## Recommendation

**Skip the skeleton pass. Peterman drafts first. Elaine audits after.**

The restructure proposal (`brand/references/readme-restructure-proposal.md`) already IS the skeleton. It defines:
- Section order and heading levels
- 16 hard constraints with explicit source attribution
- Per-section content guidance with placeholder examples
- Viewport requirements, CTA hierarchy, progressive disclosure order

An Elaine skeleton pass would translate that proposal into a blank document structure — a mechanical step that reproduces work already done.

---

## The Shape Is Already Fixed

The proposal documents form at the same precision Elaine would produce in a skeleton:

| Form element | Already in proposal |
|---|---|
| Section order | Yes — explicit recommended order with rationale |
| H1/H2/H3 hierarchy | Yes — Constraint #4, per-section heading levels specified |
| Mobile-first above-the-fold | Yes — Constraint #1, 550px requirement, explicit test instruction |
| CTA structure | Yes — Constraint #3, primary vs. secondary subordination |
| Code block format | Yes — Constraints #8/#9, 60-char limit, 5-line C# block |
| Visual separators | Yes — Constraint #7, `---` between major sections |

---

## Why Skeleton-First Adds an Extra Pass Without Adding Value

Collaboration order if Elaine goes first:
1. Peterman proposal (done)
2. Elaine skeleton (translates proposal to blank doc)
3. Peterman draft (fills skeleton with copy)
4. Elaine constraint audit (checks 16 hard constraints)

Total: 4 passes.

Collaboration order if Peterman goes first:
1. Peterman proposal (done)
2. Peterman draft (writes against the proposal directly)
3. Elaine constraint audit (checks 16 hard constraints)

Total: 3 passes. Same quality gate. One less round trip.

---

## The Risk of Skeleton-First

The skeleton pass creates a new artifact that Peterman must interpret. Any ambiguity in placeholder structure — heading text, container depth, code block labels — becomes a negotiation surface that didn't need to exist. The proposal already has this precision. Two sources of structural truth is one too many.

---

## The One Exception

**Hero code block.** The 60-character line constraint (#8) shapes what DSL sample Peterman can write — short field names, abbreviated rule copy. Before Peterman finalizes the hero DSL, Elaine should confirm which sample domain (Subscription Billing, Order Fulfillment, Loan Application) fits cleanest in a narrow viewport. This is a single targeted question, not a skeleton pass.

---

## Correct Gate Sequence for README Rewrite

1. **Peterman** — drafts full README against the restructure proposal
2. **Elaine** — constraint audit pass: 16-row pass/fail table, no copy edits
3. **Peterman** — resolves any constraint failures
4. **Frank** — optional architecture review (contributing section accuracy)
5. **Shane** — final sign-off

This matches the existing `constraint-holder-review-gate` skill pattern documented in `.squad/skills/`.

---

## What This Decision Is Not

This is not a ruling against Elaine's involvement. Elaine's structural requirements already govern this rewrite. The question is sequencing. Her form authority is exercised through the post-draft constraint audit, not through a pre-draft skeleton — because the skeleton already exists in the proposal.

---

---

# Decision: Signal Color Family Names

**Date:** 2026-04-07
**Agent:** j-peterman
**Status:** Applied — no new decision required

## Summary

The three semantic signal colors in the Precept color system have proper family names. They were already present in the spec in isolated sections but had not been applied consistently to the signal color definitions.

| Plain name | Family name | Hex |
|---|---|---|
| Green | Emerald | `#34D399` |
| Yellow | Amber | `#FCD34D` |
| Red | Rose | `#FB7185` |

## What was done

Updated `brand/brand-spec.html` to use the family names (Emerald, Amber, Rose) consistently everywhere the signal colors are named — swatch labels, intro paragraphs, cross-surface application table, syntax editor notes, state diagram intro, and README surface table.

## Implication for team

All future references to these colors — in docs, copy, surface specs, and team communications — should use **Emerald**, **Amber**, and **Rose**. Plain "green/yellow/red" is now retired from brand vocabulary.

---

---

# Decision: spm-* layout blocks must not add horizontal padding when parent spm-group already provides it

**Date:** 2026-04-04  
**Author:** J. Peterman  
**Status:** Resolved

## Context

The `spm-surface` component system (used in §2.1 and §2.2) has a three-level container hierarchy:

```
spm-surface
  spm-group        ← owns horizontal indent: padding: 16px 24px
    spm-header     ← section sub-header (e.g. "Structure · Indigo")
    spm-grid       ← swatch row container
      spm-row      ← individual token rows
        spm-swatch ← the colored block
```

The `sf-group` system (used in §1.4 and Reserved Verdict Colors) has a simpler hierarchy:

```
sf-group           ← owns horizontal indent: padding: 20px 24px
  sf-row           ← padding: 10px 0 (no horizontal)
    sf-swatch      ← the colored block
```

## Problem

Both `spm-header` (CSS class, `padding: 18px 24px`) and `spm-grid` (inline `style="padding: 16px 24px;"`) were adding their own 24px horizontal padding *in addition to* the 24px from the parent `spm-group`. This caused swatch content to render at 48px from the surface edge instead of 24px — a 24px misalignment vs. sf-swatch in the same card.

## Decision

**Rule:** In the `spm-*` component system, horizontal padding is the exclusive responsibility of the parent container (`spm-group`). Child layout blocks (`spm-header`, `spm-grid`) must use `padding: Npx 0` — vertical spacing only.

## Change Made

- `.spm-header { padding: 18px 0 }` (was `18px 24px`)
- All 6 `spm-grid` inline styles: `style="padding: 16px 0;"` (was `16px 24px`)

## Applies To

Any future `spm-*` sections (§2.3 Inspector, §2.4 Docs Site, §2.5 CLI/Terminal) must follow this rule when adding new `spm-header` or `spm-grid` blocks inside `spm-group`.

---

---

# Decision: README Ship Plan — Shortest Safe Path to Published README

**Author:** Steinbrenner (PM)
**Date:** 2026-05-01
**Status:** Recommendation — awaiting Shane sign-off
**Input artifacts:** readme-restructure-proposal.md (revised 2026-04-07), reviews from Frank, George, Uncle Leo, and j-peterman-readme-review-gap-pass.md

---

## Current State

The proposal is ready. All 7 required changes from the Frank/George/Uncle Leo review round have been applied to the proposal body (verified via `j-peterman-readme-review-gap-pass.md`). The reviewed issues that remain advisory/non-blocking are explicitly tracked and addressable by the rewriter without Shane involvement.

**The single remaining blocker is Shane's sign-off.**

---

## Gate 1 — Shane Decisions (Required Before Rewrite Begins)

These are the only four things that require Shane before the rewriter can start. None of them require a doc; they can be resolved in a single conversation.

| # | Decision | Options | Default if Shane passes |
|---|----------|---------|------------------------|
| G1 | **Approve the proposal** — structural, CTA hierarchy, section order | Approve / Approve with changes / Reject | Blocked |
| G2 | **Hero domain** — which sample domain goes in Quick Example | Subscription (team recommendation), TimeMachine (Shane prior approval), other | Blocked — no default |
| G3 | **Tagline** — confirm or substitute the proposed closing tagline | "One file. Every rule. Prove it in 30 seconds." or substitute | Rewriter picks during draft |
| G4 | **Logo/SVG** — is a brand mark SVG ready, or should title block use text wordmark only? | SVG at 48px / text wordmark placeholder | Text wordmark — rewriter proceeds, logo swaps in later without structural change |

G1 is the formal gate. G2 is the highest-churn risk — if the hero domain is not locked before the draft, the rewriter will make a call and may have to redo it. G3 and G4 can slip into the draft without blocking.

---

## In-Flight Decisions (Rewriter Resolves Without Shane)

These items are non-blocking per the reviews but must not be dropped silently:

| Item | Decision owner | Guidance |
|------|---------------|---------|
| Section 4 heading: "What Makes Precept Different" vs "What Precept Does" | J. Peterman | Leo's AN-2 flagged this as a copywriting call. Either is acceptable. "What Precept Does" is more category-creation-consistent; "What Makes Precept Different" is what developers scan for. |
| Section 4a rationale sentence structure (AI tooling "lead with / don't bury" conflation) | J. Peterman | Leo WC-2: split into two sentences per Leo's suggested rewrite. No Shane input needed. |
| `engine.Inspect` args footnote | George | If hero uses an event with required args, add a comment clarifying partial inspection behavior. George advises. |
| Context reminder wording (Getting Started opening line) | J. Peterman + Uncle Leo | The structure is locked ("Precept is a domain integrity engine for .NET. Install..."). The exact prose is Peterman's call; Leo reviews. |
| Badge alt text values (version/status) | J. Peterman | Pull current NuGet version and build status values at draft time. Constraint #14. |

---

## Explicitly Out of Scope — First Rewrite Pass

These items must not block or delay the first README rewrite. If they come up during drafting, defer them.

| Out of scope | Why |
|---|---|
| Color/palette application within the README document | Elaine's palette/usage pass for the README surface has not landed. Use shields.io defaults for badges; no custom README styling in this pass. |
| New DSL language features | Named guards, ternary in `set`, `string.length` — all in the research proposal pipeline, none implemented. The README must describe what exists. |
| CLI section | CLI design exists, implementation deferred. Do not add CLI content to the README until implemented. |
| Comparison table ("Precept vs. Stateless vs. FluentValidation") | Implicit differentiation strategy is locked. No comparison table. |
| Full pipeline mechanics | 6-stage fire, working copy behavior, TransitionOutcome enum values → deferred to docs. The README links out. |
| Sample catalog table | Per Elaine constraint #15: removed from main README, linked externally. |
| New screenshots or state diagram images | If no current screenshot is available, use a descriptive alt text placeholder. Do not delay the draft for new assets. |

---

## Execution Path After Gate 1 Clears

This is a one-shot draft → parallel review → ship sequence. No iteration loops.

```
1. J. Peterman writes README draft (one pass, targeting all 16 Elaine constraints)
2. Frank + George co-review in parallel (Frank: structure/narrative; George: API accuracy + runtime claims)
3. Uncle Leo editorial pass (copy, clarity, redundancy)
4. Scribe validates constraint checklist (16 Elaine constraints, explicit check)
5. Shane final read — no surprises expected if gates cleared upfront
6. Ship
```

**Estimated elapsed time after Gate 1 clears:** 1–2 sessions (draft + review). The proposal is highly specified — the rewriter does not need to invent structure, section order, CTA hierarchy, or C# API calls. Those are all locked.

**Churn risk:** Hero domain is the highest risk factor. A domain selected without conviction tends to get revised. Lock it in Gate 1.

---

## Appendix: What "Approved" Means for the Proposal

The proposal (revised 2026-04-07) includes:
- Recommended section order (finalized)
- All 16 Elaine hard constraints (listed)
- Section-by-section rationale with copy templates
- Hero treatment (DSL block spec + C# block spec with correct API surface)
- CTA hierarchy (primary/secondary/tertiary)
- Trim summary (what is removed, compressed, repositioned, deferred)
- Dual-audience (human/AI) structural validation table

A rewriter working from this proposal needs one thing: the hero sample. Everything else is already specified.

---

### 2026-04-04T23:00:50Z: User directive
**By:** shane (via Copilot)
**What:** Use the top-rated hero snippet from the visual-language exploration as the temporary live hero until the final hero question is settled later.
**Why:** User request — captured for team memory


---

---

# Decision Inbox Merge — 2026-04-05T02:00:30Z

**Merged by:** Scribe  
**Source:** .squad/decisions/inbox/
---

### 2026-04-04T23:22:00Z: User directive
**By:** shane (via Copilot)
**What:** For the README hero snippet, visual impact matters more than copyability; it is a hero artifact, not a practical code sample.
**Why:** User request — captured for team memory


---

### 2026-04-04T23:28:11Z: User directive
**By:** shane (via Copilot)
**What:** Do not show a raw Precept code block in the README hero; use the SVG route for the hero treatment instead.
**Why:** User request — captured for team memory


---

### 2026-04-05T01:10:02Z: User directive
**By:** shane (via Copilot)
**What:** Also include Frank as the architect on the SVG hero effort.
**Why:** User request — captured for team memory


---

### 2026-04-05T01:13:13Z: User directive
**By:** shane (via Copilot)
**What:** Frank should lead on architecture for the SVG hero effort.
**Why:** User request — captured for team memory


---

### 2026-04-05T01:15:32Z: User directive
**By:** shane (via Copilot)
**What:** Do not use Haiku. Use non-Haiku models for squad work.
**Why:** User request — captured for team memory


---

### 2026-04-05T01:25:49Z: User directive
**By:** shane (via Copilot)
**What:** Always use the latest version of models; do not route work to older model versions like gpt-4.1 by default.
**Why:** User request — captured for team memory


---

---

# Design Spec: README Hero SVG — "The Contract That Says No"

**Filed by:** Elaine (UX Designer)
**Date:** 2026-07-17
**Status:** DRAFT — awaiting Shane review
**Issue:** #4 — Replace README hero code block with branded SVG visual
**Asset:** `brand/readme-hero.svg`

---

## Concept

The hero SVG stages the Subscription Billing lifecycle as a visual narrative. The reader's eye follows a happy-path state flow left to right — Trial → Active → Cancelled — then drops below Cancelled to find the punchline: a rejected reactivation attempt, stopped by a Rose X mark, with the Gold rejection message as the final line.

**One sentence:** The hero shows a system that works perfectly — including the moment it refuses.

The comedy (and the product thesis) lives in the structural irony: a precise, orderly flow ends in a blunt refusal that the system treats as unremarkable. The reader sees that Precept doesn't check whether reactivation is allowed — it structurally can't express it.

---

## Composition

Four horizontal zones on a dark canvas:

| Zone | Content | Purpose |
|------|---------|---------|
| **Title** (top) | `precept Subscription` in syntax-like coloring | Establishes this is a precept definition |
| **Flow** (middle) | Trial → Active → Cancelled with event labels | The happy-path lifecycle |
| **Rejection** (below Cancelled) | Dashed Rose line → circled X → Gold message | The punchline — structural impossibility |
| **Tagline** (bottom) | "Invalid states are structurally impossible." | Product thesis, quiet and factual |

The Activate event appears three times — Trial→Active transition, Active self-loop (price update), and the rejected Cancelled path. One event, three states, three outcomes. That IS the product thesis in one image.

### State Node Shapes (per §2.2)

- **Trial** — Circle with thick Indigo border (#4338CA, 2.5px), small filled dot (#6366F1) for initial state indicator
- **Active** — Rounded rect, Indigo border (#4338CA, 1.5px)
- **Cancelled** — Double-border rect, Indigo (#6366F1, inner 2px + outer 1px at 0.3 opacity)

### Edges

- **Happy-path transitions** — Emerald (#34D399) solid lines with arrow markers
- **Self-loop on Active** — Emerald arc above node (price update without state change)
- **Rejected path** — Rose (#FB7185) dashed line descending from Cancelled to a circled X

### Text

- **State names** — Violet (#A898F5), monospace
- **Event labels** — Cyan (#30B8E8), monospace
- **Rejection event label** — Cyan at reduced opacity (0.6) — dimmer to signal it's the failed attempt
- **Rejection message** — Gold (#FBBF24) at 0.85 opacity — the human-readable rule text
- **Tagline** — Muted (#52525b) — present but not competing

---

## Color Mapping

Every color traces to brand-spec §1.4 semantic families:

| Element | Color | Hex | Semantic lane |
|---------|-------|-----|---------------|
| Canvas | Near-black | #0c0c0f | Background (brand standard) |
| Canvas border | Deep Indigo | #1e1b4b | Ground tone (brand mark family) |
| State node borders | Semantic Indigo | #4338CA | Structure |
| Terminal outer glow | Indigo | #6366F1 | Grammar-level structure |
| State name text | Violet | #A898F5 | State identity |
| Event label text | Cyan | #30B8E8 | Transition verbs |
| Flow arrows | Emerald | #34D399 | Allowed/success signal |
| Rejected path | Rose | #FB7185 | Blocked/error signal |
| Rejection message | Gold | #FBBF24 | Human-readable rule text |
| Title keyword | Indigo Accent | #818cf8 | Syntax keyword |
| Tagline | Muted | #52525b | Support text |

---

## Typography

Font stack: `'Cascadia Code', 'Fira Code', 'JetBrains Mono', 'Consolas', monospace`

GitHub renders SVGs server-side with limited font availability. The design degrades gracefully to any monospace font — colors and shapes carry the brand, not exact typeface rendering. If font fidelity becomes critical, key text can be converted to paths in a future polish pass.

---

## GitHub SVG Constraints

- Static SVG, no `<script>`, no `<foreignObject>`, no external resources
- Inline attributes only (no `<style>` block for maximum compatibility)
- All text as `<text>` elements (not paths — keeps the file editable and small)
- `viewBox` with fixed dimensions for consistent aspect ratio
- Tested target: renders correctly as `<img>` tag in GitHub Markdown

---

## README Structure Change

**Current:**
```

---

# Precept
badges + definition + tagline
---
## Quick Example  ← hero area (raw code blocks)
```

**Proposed:**
```

---

# Precept
badges + definition + tagline
---
![Precept — Subscription lifecycle](brand/readme-hero.svg)
---
## Quick Example  ← raw code blocks preserved below hero
```

The SVG becomes the visual-first hero treatment. The raw Precept + C# code blocks remain in "Quick Example" for teaching. The "Temporary hero sample" note is removed since the SVG is now the intentional hero, not a placeholder.

---

## Open Questions for Shane

### 1. Rose in the README

Brand-spec §1.4.1 says: "Do not use Rose in README marketing surfaces." The hero SVG IS in the README, but it's depicting a product diagram — the blocked path is the same visual language as §2.2 State Diagram, not a marketing decoration.

**My recommendation:** Rose is appropriate here. The hero is a product diagram embedded in a marketing context, not marketing styled with error colors. The rejection is the punchline — removing Rose would gut the visual narrative.

**If Shane disagrees:** The rejection could use a muted treatment (dimmed Indigo + text-only message) instead of Rose. It loses punch but stays within strict README color rules.

### 2. Tagline text

Current draft: "Invalid states are structurally impossible." — the product thesis verbatim.

**Alternative:** No tagline (let the image speak). Or a softer line like "A domain integrity engine for .NET" (repeats the existing subtitle).

**My recommendation:** Keep the thesis. It's the one sentence that makes the image click. Muted color (#52525b) keeps it subordinate.

### 3. Supporting copy in README Markdown

The issue suggests "one or two lines reinforcing the product thesis" between the image and Quick Example. Do we need this, or does the existing tagline in the README header + the image's embedded tagline cover it?

**My recommendation:** No additional copy between the image and Quick Example. The README header already has the definition and tagline. Adding more text between hero and code breaks the visual rhythm.

---

## Alternatives Considered

### A. Split-panel layout (code + diagram side by side)
Rejected. Two focal points compete for attention. Doesn't work on narrow viewports. The hero needs ONE story, not two panels.

### B. Styled code block rendering (syntax-colored SVG of the DSL text)
Rejected. This is just a fancier code block — it doesn't change the hero from "code listing" to "visual showcase." It also creates a maintenance burden (SVG must match DSL text exactly).

### C. Abstract brand mark blown up to hero scale
Rejected. The brand mark works at icon scale because it's symbolic. At hero scale, an abstract mark feels empty — there's no content, no story, no comprehension anchor.

### D. Full state diagram with all DSL features annotated
Rejected. Too dense. A hero is one idea, one glance, one feeling. The teaching happens in the code block below.

---

## What Shane Is Approving

Not pixels. Not final SVG polish. This is a concept lock:

1. **Message:** The hero shows governed state flow plus a blocked invalid path — the product thesis made visual
2. **Hierarchy:** SVG hero → raw code example → Getting Started (visual first, teaching second)
3. **Narrative device:** One event (Activate), three states, three outcomes — the third is structurally impossible
4. **Visual language:** Brand-spec §2.2 diagram vocabulary applied to a README hero context
5. **Concept:** Subscription Billing remains the temporary live domain

Once these are settled, the SVG can be refined without reopening narrative strategy.

---

## Next Steps After Approval

1. Peterman reviews for brand compliance
2. Refinement pass: tighten spacing, test font rendering on GitHub, verify mobile viewport behavior
3. Frank reviews for architectural fit (should be trivial — it's a static asset)
4. README wiring: insert SVG, remove "temporary" note, verify Quick Example still works
5. Brand-spec §2.6 update to reflect the new hero treatment
6. Shane final sign-off on the implemented README


---

---

# SVG Hero Proposal — Design Spec (Issue #4)

**Filed by:** Elaine (UX Designer)
**Date:** 2026-07-17
**Status:** PROPOSAL — awaiting Shane review
**Relates to:** GitHub issue #4

---

## 1. Recommended Concept: "The Contract That Says No"

A single dark-surface SVG panel (~800×360px) that stages the Subscription lifecycle as a **visual micro-narrative** — the happy path flows elegantly left-to-right, then the attempted reactivation from Cancelled gets definitively bounced. The rejection moment IS the punchline and IS the product thesis.

### Why this works as a hero

- **Two-second comprehension.** A developer sees three nodes, two green arrows, one red dashed arrow that doesn't connect. They understand "this system prevents bad transitions" before reading a single word.
- **The product thesis is the visual focal point.** "Invalid states are structurally impossible" isn't a tagline underneath — it's the thing your eye lands on. The Rose dashed arrow terminating at a ✕ is the most visually distinct element in the composition.
- **Cute without being corny.** The humor is structural, not illustrated. There's no mascot, no cartoon, no winking emoji. The comedy is the _contrast_ — three states flowing beautifully, and then one hilariously definitive "nah." The system has a voice (via the Gold rejection callout), and that voice is dry and matter-of-fact. Developer humor lives in the gap between elegant machinery and blunt refusal.
- **Brand-native.** Every color, shape, and label uses the locked brand vocabulary exactly as specified in `brand-spec.html` §1.3 and §2.2. No new visual concepts. The hero IS the brand.
- **GitHub-safe.** Static SVG, no scripts, no animation, no external fonts required (fallback to system monospace). Renders identically on light and dark GitHub themes against the self-contained dark canvas.

---

## 2. Composition / Layout

### Overall structure

```
┌─────────────────────────────────────────────────────────┐
│  (dark canvas #0c0c0f, 1px #1e1b4b border, 8px radius) │
│                                                          │
│  ┌─ top-left: context label ─────────────────────────┐  │
│  │  "precept Subscription"  (brand-light #818CF8)    │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
│  ┌─ center stage: state flow ────────────────────────┐  │
│  │                                                    │  │
│  │  (Trial)  ──Activate──▶  [Active]  ──Cancel──▶ ║Cancelled║ │
│  │   ○ initial    emerald    □ intermediate  emerald  ╬ terminal │
│  │                                                    │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
│  ┌─ focal moment: the rejection ─────────────────────┐  │
│  │                                                    │  │
│  │  ║Cancelled║ ╌╌╌ Activate ╌╌╌✕                    │  │
│  │    rose dashed arrow, terminates at ✕              │  │
│  │                                                    │  │
│  │    ┌ speech callout (gold accent) ──────────┐     │  │
│  │    │ "Cancelled subscriptions cannot         │     │  │
│  │    │  be reactivated"                        │     │  │
│  │    └─────────────────────────────────────────┘     │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
│  ┌─ bottom: tagline ─────────────────────────────────┐  │
│  │  Invalid states are structurally impossible.       │  │
│  │  (text-secondary #A1A1AA, small, centered)         │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### Spatial budget

| Zone | Vertical % | Purpose |
|------|-----------|---------|
| Context label | ~10% | Establishes this is a precept, not generic diagram |
| State flow row | ~35% | The happy path — three nodes, two Emerald arrows |
| Rejection moment | ~40% | The punchline — Rose dashed arrow + Gold callout |
| Tagline | ~15% | Reinforces thesis without competing with diagram |

### Key layout principles

- **Left-to-right narrative.** Trial sits at left, Cancelled at right. The happy path reads naturally. The rejected arrow curves back rightward, making the "going backward" attempt visible.
- **Rejection gets the most space.** The callout/bubble is the largest text element. It earns this space because it IS the product message.
- **No symmetry.** The happy path is tidy; the rejection moment is deliberately more expressive. This asymmetry creates visual interest and hierarchy.
- **Vertical breathing room.** No element should feel cramped. The dark canvas is a feature, not wasted space.

---

## 3. Visual Language

### State nodes (from §2.2 shape vocabulary)

| State | Shape | Border | Label color |
|-------|-------|--------|-------------|
| Trial | Circle, r≈26 | #4338CA 2.5px (initial) | #A898F5 (Violet) |
| Active | Rounded rect, rx=6 | #4338CA 1.5px (intermediate) | #A898F5 |
| Cancelled | Double-border rect | #6366F1 inner 2px + outer 1px @30% opacity | #A898F5 |

### Arrows and edges

| Path | Style | Color | Label color |
|------|-------|-------|-------------|
| Trial → Active | Solid, 1.5px, filled arrowhead | #34D399 (Emerald) | #30B8E8 (Cyan) "Activate" |
| Active → Cancelled | Solid, 1.5px, filled arrowhead | #34D399 (Emerald) | #30B8E8 (Cyan) "Cancel" |
| Cancelled → ✕ (rejected) | Dashed 3,2 pattern, 1.5px, no arrowhead — terminates at ✕ | #FB7185 (Rose) | #30B8E8 (Cyan) "Activate" (dimmed) |

### The rejection callout

- **Shape:** Rounded rectangle with a small triangular notch pointing toward the ✕ termination point. Like a speech bubble, but geometric and restrained.
- **Border:** #FB7185 (Rose) at 40% opacity, 1px — present but not loud.
- **Background:** #0c0c0f (same as canvas) or very subtle Rose tint at ~5% opacity.
- **Text:** #FBBF24 (Gold), the locked `because`/`reject` accent color. This is its correct semantic use — a human-readable rule message.
- **Content:** `"Cancelled subscriptions cannot be reactivated"`
- **Font:** Cascadia Cove monospace (with system mono fallback), ~11px, normal weight. Italic optional for the quoted string.

### Labels and tone

The hero uses **exactly two text tones**:
1. **Diagram labels** — state names in Violet, event names in Cyan, following §2.2 exactly.
2. **Narrative text** — the context label ("precept Subscription") in brand-light, the rejection message in Gold, the tagline in text-secondary.

No body copy. No explanatory paragraphs inside the SVG. The diagram tells the story; the callout delivers the punchline; the tagline names the principle.

### How "cute/funny" enters without becoming corny

The humor is **structural irony**, not decoration:

1. **Contrast comedy.** The happy path is elegant — clean Emerald arrows flowing left to right. The rejected attempt is visually messy — dashed Rose line, a ✕, a speech bubble. The system's _perfection_ in refusing is funny because it contrasts with the messy attempt.
2. **The system has a voice.** The Gold callout reads like the system is speaking directly: "Cancelled subscriptions cannot be reactivated." It's not a tooltip or annotation — it's the contract talking. Dry, definitive, slightly smug. Developer humor.
3. **The ✕ is the punchline.** A small Rose ✕ where the arrow terminates. Not a big red X, not a stop sign. Just a small, precise mark that says "this path does not exist." The understated delivery IS the joke.
4. **No mascots, no emojis, no winks.** The warmth comes from the rejection message being in Gold — the warmest color in the palette. The system is firm but not hostile. "I'm not angry, I'm just right."

---

## 4. Relationship to Supporting Copy and README Structure

### Hero → Copy bridge

The SVG hero replaces the current `## Quick Example` hero block. Below the hero image, the README should have:

```markdown
> **precept** *(noun)*: A general rule intended to regulate behavior or thought.

**Precept is a domain integrity engine for .NET.** By treating business constraints
as unbreakable precepts, it binds state machines, validation, and business rules
into a single executable contract where invalid states are structurally impossible.
```

The tagline inside the SVG ("Invalid states are structurally impossible") echoes directly into this copy. The reader sees the principle visually, then reads it in prose. Reinforcement without repetition.

### Hero → Practical example relationship

The raw Precept code block and C# execution example move to a **later section** (e.g., `## Quick Example` or `## How It Works`), not adjacent to the hero. The structural separation is:

| Section | Role | Contains raw code? |
|---------|------|--------------------|
| Hero (top) | Visual recognition, emotional hook | No — SVG only |
| Product copy | Thesis reinforcement | No |
| Quick Example (later) | Practical teaching | Yes — full DSL + C# |

The hero **earns the scroll** to the example. The example **proves the promise** the hero made visually. They're partners, not redundant.

### Section ordering (proposed)

1. Badges
2. SVG Hero image
3. One-liner definition + tagline paragraph
4. `---`
5. Quick Example (relocated DSL + C# blocks)
6. Getting Started
7. What Makes Precept Different
8. Learn More
9. Contributing
10. License

---

## 5. Alternatives Considered

### Alt A: "The Rule Tablet" — enlarging the brand mark to hero scale

Concept: Blow up the combined brand mark (tablet + state machine icon from §1.3) to a wide hero format. Left half shows stylized code lines (not readable DSL, just branded line fragments suggesting structure). Right half shows the state diagram emerging from the tablet, as if the rules generate the machine.

**Why it loses:**
- Too abstract. A developer sees colored lines and shapes but can't immediately answer "what does this product do?" The concept requires interpretation — the hero should require none.
- The brand mark works at icon scale because it's symbolic. At hero scale, the same abstraction feels empty. You need content at hero scale.
- The cute/funny dimension has nowhere to land. Abstract shapes aren't funny.

### Alt B: "Side-by-Side Panels" — what you write vs. what happens

Concept: Split the hero into two panels. Left panel: a stylized rendering of the DSL (not raw code, but visually distinguished fragments — field declarations, state list, a transition line). Right panel: the state diagram. Visual connection between them (dotted lines from code → diagram elements, or a shared background gradient).

**Why it loses:**
- Still feels like documentation, not a hero. Side-by-side explanation is a teaching layout, not a showcase layout.
- The left panel is uncomfortably close to "raw code block" even if it's styled. The issue explicitly says no raw Precept block in the hero.
- Splits attention. A hero needs one focal point, not two competing panels.
- The cute/funny moment doesn't have a natural home — you'd have to force it into a label.

### Alt C: "Three-Panel Story" — comic-inspired triptych

Concept: Three panels reading left to right like a comic strip. Panel 1: "The Contract" — a small tablet icon with "precept Subscription" label. Panel 2: "The Flow" — Trial→Active→Cancelled happy path. Panel 3: "The Refusal" — the Cancelled→Active rejection with callout.

**Why it's close but still loses:**
- The triptych framing adds visual structure that competes with the content. Borders between panels, panel numbering, visual gutters — it's a lot of chrome for a simple story.
- The three-panel layout implies sequence/time, which over-formalizes what should feel like a single glance.
- At ~800px width, each panel gets ~250px — tight for readable diagram content.
- The recommended concept tells the same story without the panel overhead. The spatial zones (flow row → rejection row) create the narrative sequence without explicit framing.

### Why "The Contract That Says No" wins

It has the highest signal-to-chrome ratio. Three nodes, two arrows, one blocked arrow, one callout. Every element earns its space. The story reads in one glance. The punchline (the rejection) gets the most visual weight. And the cute/funny dimension emerges naturally from the composition itself — the contrast between elegant flow and definitive refusal — without any added illustration or decoration.

---

## Implementation Notes (for handoff to Frank/Kramer)

- **Canvas:** `<svg viewBox="0 0 800 360">` with `<rect>` fill #0c0c0f, 1px #1e1b4b stroke, rx=8
- **Fonts:** Embed subset of Cascadia Cove if GitHub allows, or specify `font-family="Cascadia Code,Cascadia Mono,Consolas,monospace"` — GitHub SVG renders system fonts
- **GitHub SVG constraints:** No `<foreignObject>`, no `<script>`, no external stylesheets, no `<use>` referencing external files. All styles must be inline or in `<defs><style>`.
- **Dark/light mode:** The self-contained dark canvas means the SVG looks identical regardless of GitHub's page theme. This is intentional — the brand surface IS dark.
- **File location:** `brand/hero.svg` (source of truth) with a copy or symlink wherever README references it.
- **README wiring:** `<p align="center"><img src="brand/hero.svg" alt="Precept — a domain integrity engine for .NET. State diagram showing Trial → Active → Cancelled subscription lifecycle with a rejected reactivation attempt." width="800"></p>`

---

## Open Questions for Shane

1. **Tagline inside SVG vs. outside?** Proposal includes "Invalid states are structurally impossible." inside the SVG. Alternative: leave it out of the SVG and let the README copy carry it. Inside the SVG makes the image self-contained; outside keeps the SVG purely diagrammatic.
2. **Rejection message wording.** The current DSL uses `"Cancelled subscriptions cannot be reactivated"`. This is good — clear, direct, slightly formal. If we want more warmth for the hero moment, options include keeping it exactly as-is (recommended — the formality IS the humor) or adjusting slightly.
3. **"precept Subscription" label.** Should the top-left context label be present? It hints at the DSL without showing raw code. Alternative: omit it and let the diagram stand alone.


---

### SVG Hero Architecture Proposal — Issue #4

**Date:** 2026-04-08
**By:** Frank (Lead/Architect)
**Status:** PROPOSAL — awaiting Shane review + Elaine/Peterman design pass
**Issue:** [#4 — Replace README hero code block with branded SVG visual](https://github.com/sfalik/Precept/issues/4)

---

## 1. Recommended Asset/Workflow Architecture

### Asset path and format

| Item | Decision |
|------|----------|
| **Format** | Static inline SVG, no `<image>`, `<foreignObject>`, `<script>`, or external references |
| **Repo path** | `brand/readme-hero.svg` — single source-of-truth file, version-controlled alongside brand-spec |
| **README integration** | `<picture>` with `<source media="(prefers-color-scheme: dark)">` + `<img>` fallback referencing the same SVG via relative path |
| **Viewport** | Fixed `width`/`height` attributes (recommended 800×280) plus `viewBox` — GitHub strips `width`/`height` expressed as percentages |
| **Content** | Subscription Billing lifecycle flow: `Trial → Active → Cancelled`, with reject moment, using brand-spec §1.3 visual language (state nodes, transition arrows, rule text line) |

### Why a standalone `.svg` file (not inline in README)

- GitHub sanitizes inline SVG in markdown, stripping most attributes and all `<style>` blocks.
- A referenced `.svg` file is rendered through GitHub's SVG proxy (`camo`), which preserves the full SVG spec minus scripts and external resources.
- A standalone file allows Elaine and Peterman to iterate on the design without touching README markdown.
- `git diff` on a single `.svg` file gives clean changesets vs. diffing an embedded SVG inside a markdown file.

### README wiring pattern

```markdown
<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="brand/readme-hero.svg">
    <img alt="Precept — Subscription Billing lifecycle: Trial → Active → Cancelled" src="brand/readme-hero.svg" width="800">
  </picture>
</p>
```

A single dark-mode SVG is sufficient initially (the brand surface is dark-ground). If a light variant is needed later, add a second `<source>` and `brand/readme-hero-light.svg`. This is additive — no restructuring needed.

---

## 2. GitHub Rendering & Maintenance Constraints

### GitHub SVG sandbox rules (non-negotiable)

| Constraint | Impact |
|------------|--------|
| **No `<script>`** | All content must be static geometry + text |
| **No `<foreignObject>`** | No embedded HTML, no `<div>`, no markdown-in-SVG |
| **No external resources** | No `<image href="...">`, no `xlink:href` to remote assets, no `@import` fonts |
| **No `<style>` blocks in referenced SVGs** | GitHub's camo proxy strips `<style>`. All styling must be **inline attributes** (`fill`, `stroke`, `font-family`, etc.) |
| **No CSS classes** | Stripped by sanitizer; use inline style or direct SVG attributes |
| **`font-family` fallback** | Custom fonts (Cascadia Cove) will NOT render — GitHub serves SVGs as `<img>`, so system fonts only. Use `font-family="monospace"` and accept platform-default monospace |
| **Fixed dimensions** | Set explicit `width` and `height` in px on the root `<svg>` element; percentage-based sizing is stripped |
| **Max file size** | Keep under 100KB for fast rendering and reasonable diff size |

### Font reality check

The brand-spec locks Cascadia Cove as the brand font. **GitHub cannot render it** in SVGs served via camo proxy (no font loading). Two viable options:

1. **Use `font-family="monospace"`** — accepts platform default. Pragmatic, zero maintenance.
2. **Convert text to `<path>` outlines** — pixel-perfect but non-editable, increases file size, complicates maintenance.

**Recommendation:** Use `font-family="monospace"` for all text elements. Reserve `<path>` conversion only if Peterman's brand review determines that text rendering variance across platforms is unacceptable. This is a **Shane decision point** — see §5.

### Maintenance model

- The SVG lives in `brand/` alongside brand-spec. Brand-aware contributors (Elaine, Peterman) own its visual correctness.
- README changes that affect the hero area (copy above/below the SVG) do not require touching the SVG file.
- SVG color values must match the locked 8+3 palette. Any palette drift is caught during Peterman's brand compliance gate.
- If the hero domain changes away from Subscription Billing (a separate future decision), the SVG is replaced wholesale — not patched.

---

## 3. Source-of-Truth & Handoff Plan

### Design → Implementation handoff sequence

```
1. Frank         → Architecture proposal (this document)        ← YOU ARE HERE
2. Shane         → Approves architecture + resolves open decisions (§5)
3. Elaine        → SVG design spec: layout, composition, visual hierarchy
                   Deliverable: annotated mockup OR draft SVG in brand/
4. Peterman      → Brand compliance review of Elaine's design
                   Checks: palette compliance, shape vocabulary (§1.3 brand marks),
                   signal color rules, typography constraints
5. Frank         → Architecture review of final SVG
                   Checks: GitHub constraint compliance (§2 above),
                   file size, no external deps, inline-only styling
6. Kramer        → Implementation: produce final brand/readme-hero.svg,
                   wire into README, relocate raw code block below hero
7. Elaine        → Post-draft visual audit
8. Peterman      → Post-draft brand compliance audit
9. Shane         → Final sign-off
```

### Source-of-truth files

| Artifact | Path | Owner |
|----------|------|-------|
| Hero SVG asset | `brand/readme-hero.svg` | Elaine (design), Peterman (brand gate) |
| README wiring | `README.md` lines 1–20 (approx) | Kramer (implementation) |
| Brand palette reference | `brand/brand-spec.html` §1.4 | Peterman |
| Shape vocabulary reference | `brand/brand-spec.html` §1.3 | Peterman |
| Architecture constraints | This proposal | Frank |

### Documentation sync (per project rules)

When the SVG is merged, the following must update in the same PR:
- `README.md` — hero section restructured (raw block removed, SVG wired in, code example relocated)
- `brand/brand-spec.html` — if a new surface section is warranted for "README hero," add it or note it in §2
- Any `.squad/decisions/` entries reflecting the locked choices

---

## 4. Rejected Alternatives

| Alternative | Why rejected |
|-------------|-------------|
| **Inline SVG in README markdown** | GitHub's markdown sanitizer strips `<style>`, most attributes, and many elements. The SVG would render broken or severely degraded. A referenced file goes through the camo proxy which preserves far more of the SVG spec. |
| **PNG/JPEG hero image** | Loses scalability, crisp rendering on retina, and version-control friendliness. Binary diffs are opaque. SVG is the correct format for geometric/diagrammatic content with text. |
| **Animated SVG (CSS keyframes / SMIL)** | GitHub strips `<style>` blocks and SMIL is deprecated in some browsers. Animation is also explicitly out of scope per issue #4. |
| **`<foreignObject>` with styled HTML** | Stripped by GitHub sanitizer. Non-starter. |
| **Raster screenshot of a styled code block** | Combines the worst of both: non-scalable, non-diffable, requires regeneration on any change. Also violates the "no raw Precept block in hero" directive since it would visually be a code block. |
| **SVG with embedded web fonts (`@font-face`)** | GitHub's camo proxy does not load external fonts. The font would silently fall back to default, making the embedded font declaration dead weight. |
| **Text-to-path for all SVG text** | Technically GitHub-safe but creates a maintenance nightmare: every text change requires re-outlining. Only justified if platform font variance is deemed unacceptable (Shane decision — §5). |
| **Dark + light SVG pair from day one** | Premature — the brand surface is dark-ground, GitHub dark mode is the primary context, and the `<picture>` pattern allows adding a light variant later without restructuring. |

---

## 5. Open Decisions for Shane

These are **Gate-Before-Start** items. Elaine's design work cannot begin until these are resolved.

### Decision A: Font rendering strategy

**Question:** Accept platform-default monospace in the hero SVG, or require path-outlined text for pixel-perfect brand font rendering?

| Option | Tradeoff |
|--------|----------|
| **A1: `font-family="monospace"` (recommended)** | Text is editable, file stays small, maintenance is trivial. Visual will vary slightly across OS (Consolas on Windows, SF Mono on macOS, DejaVu Sans Mono on Linux). All are monospace — the structural feel is preserved. |
| **A2: Path outlines** | Pixel-perfect Cascadia Cove rendering. But every text edit requires font tooling to re-outline. File size increases. `git diff` on path data is meaningless. |

**Default if not decided:** A1 (platform monospace).

### Decision B: SVG composition scope

**Question:** Should the hero SVG contain only the visual diagram (state flow + reject moment), or also include text elements like the product name / tagline?

| Option | Tradeoff |
|--------|----------|
| **B1: Diagram only — text stays in README markdown (recommended)** | Clean separation: SVG owns the visual, markdown owns the copy. Text is searchable, editable, translatable. SVG stays focused and small. |
| **B2: Diagram + wordmark + tagline inside SVG** | Self-contained visual unit — looks exactly the same everywhere. But text is not searchable, not easily editable, and couples copy changes to SVG file changes. |

**Default if not decided:** B1 (diagram only in SVG, text in markdown).

### Decision C: Hero concept lock

**Question:** Confirm Subscription Billing as the hero concept for the SVG pass, understanding that changing it later means replacing the SVG wholesale.

This is already stated in issue #4 ("current live concept can remain Subscription Billing"), but per the `proposal-gate-analysis` skill, hero domain is always a Gate-Before-Start item. Explicit confirmation avoids mid-flight domain churn.

**Default if not decided:** Subscription Billing (per issue #4).

---

## Risks

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Font rendering variance across platforms looks unprofessional | Medium | Peterman reviews on macOS + Windows + Linux screenshots before merge. If unacceptable, escalate to Decision A (path outlines). |
| SVG file grows too complex / large for clean diffs | Low | Set 100KB ceiling. Kramer flags if approaching. Simplify geometry before adding detail. |
| GitHub changes camo proxy sanitization rules | Very low | The constraints listed in §2 have been stable for years. If they change, SVG is still the most resilient static format. |
| Design iteration takes multiple rounds | Medium | Expected. The handoff sequence (§3) has clear gates. Each round is scoped: Elaine owns visual, Peterman owns brand, Frank owns GitHub constraints. |
| Subscription Billing domain changes before SVG ships | Low | Issue #4 explicitly locks it. If it changes, the SVG is replaced — the architecture (file path, wiring, `<picture>` pattern) is domain-agnostic. |


---

---

# J. Peterman — Brand/DevRel proposal pass for issue #4

## Proposed section for amendment

### Brand / DevRel recommendation

**1. One-glance message**

The hero should communicate this instantly: **Precept is the contract that governs the lifecycle.** The visual should show a clean state flow for Subscription Billing, then make the blocked reactivation path unmistakable so GitHub readers understand, in one glance, that Precept does not merely describe rules — it structurally prevents invalid transitions.

**2. Supporting copy**

Recommended supporting copy beneath the SVG:

> Define the contract once. Precept binds state, data, and business rules into one executable surface where invalid states are structurally blocked.

If a second line is wanted:

> The README hero should sell the category first; the raw example can teach the syntax just below.

**3. Brand constraints the SVG must honor**

- **SVG-first, GitHub-safe, static.** No scripts, no fragile CSS dependence, no animation.
- **Dark-surface friendly.** It should feel native on GitHub without fighting GitHub's chrome.
- **Semantic color discipline.** Indigo for structure, Violet for states, Cyan for events, Bright Slate for data, Gold only as a sparse rule/message accent.
- **No Rose-led marketing surface.** Blocked status can be communicated by closure, crossed flow, or wording; the README hero should not feel error-branded.
- **Use the Precept visual language, not generic SaaS diagramming.** The piece should feel like a branded contract surface, not a flowchart template.
- **Tasteful restraint.** One focal story: `Trial → Active → Cancelled`, with the non-reactivation rule made memorable. No faux-dashboard clutter, no ornamental gradients that add heat but not meaning.
- **Typography should stay in-family.** If labels or wordmark treatment appear inside the SVG, they should align with the Cascadia Cove / editor-native brand posture.

**4. What should move lower in the README**

Once the hero becomes visual-first, the current raw DSL block and paired C# execution snippet should move down into the practical teaching section as the real **Quick Example**. The temporary status note should attach to that example, not sit in the hero area. In other words: top of README becomes product thesis; lower section becomes hands-on proof.

**5. Approval framing**

Shane is not really being asked to approve an SVG drawing. He is being asked to approve five things:

1. that the README's first screen becomes **visual-first**
2. that **Subscription Billing** remains the temporary live concept
3. that the hero's job is to communicate **executable contract + governed state flow + blocked invalid path**
4. that the raw Precept/C# sample moves lower as proof instead of serving as the hero itself
5. that brand discipline means **stronger than utilitarian**, but still truthful, restrained, and GitHub-safe

That is the real decision gate. Once those are approved, SVG execution becomes craft, not strategy.

---

---

# Decision Inbox Merge — 2026-04-05T02:32:19Z

**Merged by:** Scribe  
**Source:** .squad/decisions/inbox/

---

---

# README inline DSL for preview surfaces

- **Date:** 2026-04-05
- **Owner:** J. Peterman
- **Decision:** The README hero's DSL now renders inline in `README.md` with README-safe HTML (`<pre><code>` and inline `span` styles) instead of asking readers to open `brand/readme-hero-dsl.html`.
- **Why:** VS Code markdown preview needs the contract visible on the public surface itself. The standalone HTML and raw `.precept` artifacts remain as companion references, not substitutes for the README display.
- **Implication:** README-safe inline HTML is the preferred pattern when a branded code sample must remain visible inside markdown preview without inventing new product claims or replacing the source artifacts.

---

---

# README link removal

- Removed the contract artifact links from `README.md` and kept the inline DSL block as the single quick-example reading path.
- Left the standalone files in place, but stopped advertising them in the README so the hero section reads cleanly without sending readers away.


---

---

# Decision Inbox Merge — 2026-04-05T02:54:36Z

**Merged by:** Scribe
**Source:** .squad/decisions/inbox/
**Summary:** Team safe recommendation = freeze-and-curate cutover. Frank's force-promote recommendation remains logged as a dissenting architectural view and is currently blocked by reviewer rejection.
**Merged:** 4
**Skipped as duplicates:** 0

---

---

# Decision: Trunk Consolidation Strategy — Force-Promote Feature to Main

**Date:** 2026-04-09
**Owner:** Frank (Lead/Architect)
**Requested by:** Shane
**Status:** Awaiting Shane sign-off
**Supersedes:** `steinbrenner-consolidation-plan.md` (recommending rejection)

---

## Situation Analysis

### Branch Topology

| Branch | Commits | Root | Unique content vs `feature/language-redesign` |
|--------|---------|------|-----------------------------------------------|
| `main` | 2 | `a2b867f` (GitHub auto-init) | **Zero.** Placeholder README, superseded. |
| `feature/language-redesign` | 276 | `550b708` (real project init) | **This IS the project.** Runtime, tooling, 666 tests, 20+ samples, docs, brand. |
| `origin/master` | ancestor of feature | `550b708` | Zero. Fully contained. |
| `diagram-layout-option-b` | ancestor of feature | `550b708` | Zero. Fully contained. |
| `copilot/worktree-*` | ancestor of feature | `550b708` | Zero. Fully contained. |
| `origin/diagram-layout-option-a` | 1 unique commit | `550b708` | 1 abandoned exploration commit. |
| `origin/diagram-layout-option-c` | 1 unique commit | `550b708` | 1 abandoned exploration commit. |
| All other `origin/*` branches | ancestors of feature | `550b708` | Zero. All fully contained. |

### The Core Problem

`main` and `feature/language-redesign` have **no merge base** — they descend from different root commits. `main` was created via GitHub's "Initialize repository" UI (`a2b867f`), while all real development started from a separate local init (`550b708`). The two histories never intersected.

`main` contains exactly 2 commits: an auto-generated initial commit and a "concept readme" with placeholder badges and aspirational content. Every byte of that content has been superseded by the feature branch.

### Current State

- Working tree is **clean** (previously uncommitted doc changes were committed as `9436065`).
- 17 commits on `feature/language-redesign` are not yet pushed to origin.
- One sibling worktree exists at `Precept.worktrees/copilot-worktree-*` on branch `copilot/worktree-2026-03-28T05-06-33`. Its branch is fully contained in the feature branch.
- No open PRs on the repository.

---

## Decision

**Force-promote `feature/language-redesign` to `main`.** Do not merge, rebase, cherry-pick, or curate.

### Rationale

The "unrelated histories" framing makes this sound dangerous. It is not. Here is what is actually happening:

1. `main` is a **dead placeholder** — 2 commits, zero unique content, zero test coverage, zero runtime code.
2. `feature/language-redesign` **is the project** — 276 commits of coherent development history including the real initial commit, all implementation, all tests, all tooling.
3. There is nothing to merge. There is nothing to transplant. There is nothing to curate. The placeholder must be replaced with the reality.

A `--allow-unrelated-histories` merge would create a nonsensical merge commit joining an empty placeholder to 276 commits of real work. That is worse than force-push — it leaves a lie in the history.

Steinbrenner's "curated re-landing" plan proposes creating a fresh branch off `main`, transplanting content in buckets, and committing curated chunks. This is an **outrageous** amount of work to preserve two placeholder commits that have zero value. It also introduces transplant risk (missed files, broken references, test failures from incomplete copies) for absolutely no gain.

### What We Lose

`main`'s 2-commit history (`a2b867f Initial commit` → `31a9f9e concept readme`). This is a GitHub auto-init plus a concept README. There is nothing here worth preserving. If sentimentality demands it, tag it first.

### What We Gain

- `main` becomes the single authoritative line with the project's real history.
- Zero risk of transplant errors.
- No multi-day curated landing operation.
- Clean `git log main` showing the actual development story from `550b708 Initial commit` forward.

---

## Execution Sequence

### Pre-flight (before any push)

1. **Build passes.** `dotnet build` on `feature/language-redesign` must succeed.
2. **All 666+ tests pass.** `dotnet test` must be green.
3. **Push feature branch.** Push the 17 unpushed commits to `origin/feature/language-redesign` so they are backed up remotely before any destructive operation.

### Execute

4. **Tag the old main for provenance** (optional but cheap):
   ```
   git tag archive/old-main main
   git push origin archive/old-main
   ```

5. **Force-update main locally:**
   ```
   git checkout main
   git reset --hard feature/language-redesign
   ```

6. **Force-push main to origin:**
   ```
   git push origin main --force-with-lease
   ```

7. **Delete the feature branch** (it IS main now):
   ```
   git branch -d feature/language-redesign
   git push origin --delete feature/language-redesign
   ```

### Cleanup

8. **Remove sibling worktree:**
   ```
   git worktree remove <path-to-copilot-worktree> --force
   git branch -D copilot/worktree-2026-03-28T05-06-33
   ```

9. **Archive and prune stale remote branches.** Tag any branch with unique commits first:
   ```
   git tag archive/diagram-option-a origin/diagram-layout-option-a
   git tag archive/diagram-option-c origin/diagram-layout-option-c
   git push origin archive/diagram-option-a archive/diagram-option-c
   ```
   Then delete all stale remote branches: `origin/master`, `origin/diagram-layout-option-*`, `origin/shane/*`, `origin/upgrade-to-NET10`, `origin/copilot/*`.

10. **Set default branch on GitHub** to `main` if not already (it is — `origin/HEAD` → `origin/main`).

11. **Verify final state:** `main` is the sole authoritative branch. `git log main` shows 276 commits from the real root. All tests pass. No dangling worktrees or ghost branches.

---

## What Must Happen Before Any Push to Main

This is non-negotiable:

1. ✅ Working tree clean (confirmed — `9436065` committed the last dirty files)
2. ⬜ `dotnet build` passes on `feature/language-redesign`
3. ⬜ `dotnet test` passes — all 666+ tests green
4. ⬜ 17 unpushed commits pushed to `origin/feature/language-redesign` as backup
5. ⬜ Shane explicitly approves this strategy

---

## Side Branch and Worktree Treatment

| Asset | Action | Reason |
|-------|--------|--------|
| `diagram-layout-option-b` (local) | Tag as `archive/diagram-option-b`, then delete | Fully contained in feature; historical reference only |
| `copilot/worktree-*` (local + worktree) | Remove worktree, delete branch | Fully contained; worktree is stale |
| `origin/diagram-layout-option-a` | Tag as `archive/diagram-option-a`, then delete | 1 unique commit — abandoned exploration |
| `origin/diagram-layout-option-c` | Tag as `archive/diagram-option-c`, then delete | 1 unique commit — abandoned exploration |
| `origin/master` | Delete | Fully contained in feature; legacy default branch name |
| `origin/shane/*` (3 branches) | Delete | All fully contained in feature |
| `origin/upgrade-to-NET10` | Delete | Fully contained in feature |

---

## Rejection of Steinbrenner's "Curated Re-Landing" Plan

Steinbrenner proposed transplanting content from `feature/language-redesign` to a fresh branch off `main` in curated buckets. I am rejecting this approach for the following reasons:

1. **It preserves nothing of value.** `main`'s 2-commit history is a placeholder. Preserving it as the root of the curated branch adds no information.
2. **It creates transplant risk.** Manually copying a 276-commit project across branch boundaries is error-prone. Files get missed, paths break, test configurations diverge.
3. **It destroys real history.** The curated commits would replace 276 real commits with a synthetic reconstruction. The actual development story — who wrote what, when, and why — would be lost.
4. **It costs days of work for zero architectural benefit.** The only beneficiary is a 2-commit placeholder that should never have existed.

The only scenario where curated re-landing makes sense is when the feature branch contains work that should NOT go to trunk. I audited the full tree — there is no such content. The feature branch *is* the project.

---

## Definition of Done

1. `main` points to the current tip of `feature/language-redesign` (commit `9436065` or later).
2. `origin/main` is force-updated to match.
3. `feature/language-redesign` branch is deleted (local and remote).
4. All stale branches are archived (tagged) or deleted.
5. Sibling worktree is removed.
6. Build and tests pass on the new `main`.
7. Shane can point to `main` as the sole authoritative line for all ongoing work.

---

---

# Decision: Safe Return to Trunk from `feature/language-redesign`

**Date:** 2026-04-05
**Owner:** Steinbrenner
**Requested by:** Shane

## Situation

- `main` and `feature/language-redesign` have no merge base. This is an unrelated-history problem, not a normal long-lived feature branch.
- `main` is still the minimal trunk (`Initial commit` + `concept readme`).
- `feature/language-redesign` is the live superset branch and already contains the work from:
  - `diagram-layout-option-b`
  - `copilot/worktree-2026-03-28T05-06-33`
- The working tree is dirty before landing starts:
  - `docs/PreceptLanguageDesign.md`
  - `docs/RuntimeApiDesign.md`

## Decision

**Return to trunk by curated re-landing from a fresh branch created off `main`. Do not preserve the current branch history as the trunk history.**

This branch line is too mixed for direct promotion:

- unrelated history versus trunk
- exploratory README / brand / squad / language-doc work layered together
- uncommitted docs on top of the branch tip

The safe path is to treat `feature/language-redesign` as a source tree, not as merge-ready history.

## Recommended Landing Sequence

1. **Freeze the source branch.** No new work lands on `feature/language-redesign` until the trunk-return sequence is complete.
2. **Quarantine current uncommitted docs.** Stash `docs/PreceptLanguageDesign.md` and `docs/RuntimeApiDesign.md` together as a named WIP stash. Do not let those edits ride along invisibly into trunk work.
3. **Inventory what actually deserves trunk.** Split the source tree into landing buckets before copying anything:
   - product/runtime/tooling code and tests
   - core docs that describe shipped behavior
   - README / brand / squad process material
4. **Create a fresh integration branch from `main`.** This is the only branch that should target trunk.
5. **Transplant bucket 1 first: product/runtime/tooling.** Copy the implementation tree from `feature/language-redesign` into the integration branch and commit it in curated chunks that produce a coherent product baseline.
6. **Validate baseline.** Build and run the existing test suite on the integration branch before any README/brand/process payload is added.
7. **Transplant bucket 2 second: essential docs only.** Land docs that are required to describe the implementation now on trunk. Keep this separate from process/brand material.
8. **Re-evaluate the stashed local docs.** Only reapply them if they still describe implemented behavior after the product baseline is on trunk. If they are speculative or partially aligned, split or discard them.
9. **Transplant bucket 3 last and selectively: README / brand / squad.** Land only the pieces that support the current strategy and are intended to live on trunk. Do not bulk-copy every orchestration artifact by default.
10. **Review trunk shape against strategy.** Confirm the resulting trunk contains the intended product baseline plus the current README/hero direction, with no leftover exploratory baggage.
11. **Open one reviewed PR to trunk.** The PR should present curated commits, not the old branch graph.
12. **After merge, clean up retired branches/worktrees.** Do cleanup only after trunk is green and the new trunk branch is authoritative.

## What to Commit, Stash, Discard, or Split Before Trunk Work Begins

### Stash immediately

- `docs/PreceptLanguageDesign.md`
- `docs/RuntimeApiDesign.md`

Reason: these are local, unreviewed, and currently mixed into a planning problem. They must be evaluated after the integration branch exists.

### Split during landing

- **Implementation vs. docs** must be separate curated commits.
- **README / brand / squad** must not be combined with runtime/tooling commits.
- Any commit that mixes product behavior with process-history files should be broken apart.

### Commit into trunk only if still justified

- Product/runtime/tooling files that represent the real current strategy.
- Documentation that matches implemented behavior.
- README/brand assets that support the chosen hero and positioning and are intended to ship with trunk.

### Discard or leave behind unless specifically needed

- Pure orchestration exhaust, duplicated session logs, and temporary planning artifacts that do not add lasting trunk value.
- Any local doc text from the stash that describes behavior not yet implemented or no longer strategically chosen.

## Side Branch and Worktree Treatment

### `diagram-layout-option-b`

- Treat as **historical reference only** during trunk return.
- It is already an ancestor of `feature/language-redesign`; nothing from it needs to be merged separately.
- Keep it until trunk landing is complete, then retire it if no one still needs branch-local breadcrumbs.

### `copilot/worktree-2026-03-28T05-06-33`

- Treat the sibling worktree branch the same way: **freeze and preserve until trunk landing completes**, but do not merge it independently.
- It is also already contained in `feature/language-redesign`.
- After trunk is green, remove the worktree and delete the branch if no active task still depends on it.

## History Strategy

**Land as curated commits. Do not preserve the existing branch history on trunk.**

Why:

- There is no merge base with `main`.
- The branch history mixes product work, brand exploration, squad process setup, and follow-on cleanups.
- A curated sequence gives Shane a reviewable story: product baseline first, docs second, strategy-facing assets last.

If historical provenance matters, preserve the old branch by tag or by leaving the branch in remote temporarily. Trunk should still receive the curated series.

## Definition of Done

We are back on trunk and finalized on the current strategy when all of the following are true:

1. `main` contains the selected implementation baseline from `feature/language-redesign` through a reviewed PR from a fresh integration branch.
2. The trunk history is a curated commit series, not an unrelated-history merge.
3. The test/build baseline passes on the integration branch and again after merge.
4. Every local pre-landing edit was resolved deliberately: committed as scoped work, left in a clearly named follow-up branch, or discarded.
5. No side branch or sibling worktree remains as a hidden source of truth; either trunk supersedes them or they are explicitly retained as archived references.
6. README / brand / docs on trunk reflect the current strategy actually chosen to ship now, not exploratory alternates.
7. Shane can point to `main` as the sole authoritative line for ongoing work.

---

---

# Uncle Leo - Consolidation Safety Review

**Date:** 2026-04-05
**Reviewer:** Uncle Leo
**Subject:** return-to-main risk review for `feature/language-redesign`

## What I reviewed

- `main` at `31a9f9ed534c0290ac1340830b210b091ca37a35`
- `feature/language-redesign` at `9436065c678aff1d93538129bf72b1e4d9d244eb`
- branch topology, worktree state, recent history shape, `README.md`, `docs/PreceptLanguageDesign.md`, `.squad/decisions.md`
- repository health checks: IDE diagnostics clean; `dotnet build` succeeded; `dotnet test --no-build` succeeded (703/703)

## Observed facts that drive the review

1. There is **no merge base** between `HEAD` and `main`.
2. `main` is effectively a 2-commit concept branch; `feature/language-redesign` is a 275-commit product branch.
3. `main` vs current `HEAD` is not a normal PR-sized change. It is roughly **600 files / 106k insertions** and introduces the actual repo structure (`src`, `tools`, `test`, `samples`, `.github`, `.copilot`, `.squad`, brand assets, etc.).
4. The candidate branch **moved during review**: the snapshot started at `f302417...` and later advanced to `9436065...`. In a shared worktree, that means any trunk decision against the branch name alone is unsafe.
5. Local `feature/language-redesign` is **17 commits ahead of `origin/feature/language-redesign`**.

## 1. Biggest technical and process risks

### Technical risks

- **Topology risk:** unrelated histories mean a normal merge does not preserve a meaningful review trail from `main`.
- **Blast-radius risk:** landing this branch touches product code, tests, tooling, docs, automation, squad/process files, and public brand assets in one shot.
- **Contract drift risk:** `README.md`, `docs/PreceptLanguageDesign.md`, runtime API docs, language server behavior, and MCP-facing outputs are all trunk-visible contracts. A bad landing here breaks both humans and AI consumers.
- **Automation risk:** `.github`, `.copilot`, and `.squad` are not passive content. Landing them changes workflow behavior, agent behavior, and repo operations.

### Process risks

- **Unstable approval surface:** the branch advanced while being reviewed.
- **Mixed intent history:** code, docs, brand work, automation, and squad state are interleaved. That is terrible landing hygiene for trunk.
- **Reviewability failure:** treating this as a merge instead of a repository cutover will hide the real decision: which parts of this branch are actually authorized to become trunk.

## 2. What makes a direct merge or push unsafe

### Reject: **Direct merge**
**Why rejected:** no merge base, unrelated histories, and the resulting merge commit would pretend this was an incremental integration when it is actually a full replacement/import.

### Reject: **Direct push / force-push `main` to the floating branch**
**Why rejected:** the candidate moved during review, is ahead of origin by 17 local commits, and contains mixed product + process + documentation history. Replacing trunk from a mutable branch name is not a controlled landing.

### Reject: **Blind squash of current HEAD onto `main`**
**Why rejected:** it hides provenance for a 600-file import and prevents targeted rollback/review of code vs docs vs automation.

### Reject: **Cherry-pick only the newest docs/README commits**
**Why rejected:** those commits assume the rest of the product branch exists. Moving docs/public claims without the underlying product surface is guaranteed drift.

## 3. Artifacts and branch states that need explicit review before trunk is touched

1. **Frozen candidate SHA** - trunk work must target an exact commit, not `feature/language-redesign` as a moving ref.
2. **Product tree import** - `src\`, `tools\`, `test\`, `samples\` need explicit sign-off as the real software payload.
3. **Public contract docs** - `README.md`, `docs\PreceptLanguageDesign.md`, `docs\RuntimeApiDesign.md`, and MCP/language-server-facing docs must be checked against implementation.
4. **Operational surfaces** - `.github\`, `.copilot\`, `.squad\`, `.gitattributes`, `.gitignore`, and any workflow/config additions need explicit authorization, not incidental landing.
5. **Brand/public collateral** - `brand\` and related README hero assets should be reviewed as publication changes, not hidden inside a code cutover.
6. **Branch/worktree state** - confirm no sibling worktree or local-only state is about to be invalidated by history surgery, and confirm the reviewed SHA still matches what is being promoted.

## 4. Preserve history as-is, or curate it?

**Verdict:** preserve the current branch history for archaeology, but **do not land trunk with this history as-is**.

What should happen instead:

- keep `feature/language-redesign` (and optionally tag the reviewed SHA) as the archival record
- create a **curated integration branch** from `main`
- transplant the approved tree onto that branch in deliberate, reviewable commits (for example: product/code, docs/contracts, automation/process)
- only then update `main`

That keeps the evidence without making trunk absorb every exploratory, orchestration, and checkpoint-era commit.

## 5. Reviewer verdict on strategy patterns

### Approve: **Freeze-and-curate cutover**
Approved pattern:
1. freeze the exact SHA
2. create a fresh integration branch from `main`
3. import only the explicitly approved tree/content
4. review by artifact class (product, contracts, automation, brand/process)
5. rerun build/tests on the curated branch
6. update `main` from that curated branch

### Reject: **Merge-as-if-normal PR**
Rejected because the histories are unrelated and the scope is repository replacement, not incremental change.

### Reject: **Force-repoint `main` to feature head**
Rejected because the branch is mutable, locally ahead of origin, and not separated from process/automation payload.

### Reject: **Ship current history intact to trunk**
Rejected because the trunk history would inherit exploratory/squad/checkpoint noise and destroy review clarity.

## Bottom line

This branch can be the source of truth for a landing, but **not by direct merge and not by floating-ref force push**. Freeze the SHA, curate the landing, and treat trunk touch as a repository cutover with explicit artifact review gates.

---

---

# Recommendation: Treat trunk consolidation as curation, not merge mechanics

**Date:** 2026-04-05
**By:** J. Peterman
**Status:** Recommendation for review

## Summary

`feature/language-redesign` carries the actual product direction, while `main` remains a separate concept-readme lineage with no merge base to the current branch.

That means trunk consolidation should be treated as a curation decision, not a routine merge operation.

## Recommendation

Before finalizing onto trunk, explicitly decide:

1. whether trunk will be rebased around `feature/language-redesign` as the new root, or
2. whether a selected subset of this branch will be re-landed onto `main`.

## Why

- The current branch contains the real language redesign, runtime/tooling work, MCP/AI surface, and the current public narrative.
- `main` does not represent that work.
- A normal merge frame would imply continuity that the repository history does not have.

## Practical effect

The team should create a keep/defer/archive list before trunk consolidation, then land the chosen line deliberately with source-of-truth docs updated in the same pass.

---

# UX Decision: In-Diagram Transitions Exploration

**Filed by:** Elaine (UX Designer)  
**Date:** 2026-04-07  
**Status:** Exploration — pending Shane review  
**Artifact:** `tools/Precept.VsCode/mockups/preview-inspector-in-diagram-transitions-mockup.html`  
**Comparison baseline:** `tools/Precept.VsCode/mockups/preview-inspector-redesign-mockup.html`

---

## What This Explores

Moving the primary transition affordances (event buttons, inline args, fire action, outcomes, reject reasons) out of the bottom event dock and into edge-anchored panels on the diagram surface itself. The bottom dock is removed entirely; vertical space is reclaimed for the diagram.

---

## What Changed From The Dock Model

| Aspect | Dock model (redesign mockup) | In-diagram model (this exploration) |
|--------|------------------------------|-------------------------------------|
| Event interaction surface | Bottom dock panel with flat list of event rows | Floating panels anchored to their SVG edges |
| Vertical layout | Header + diagram/data + dock (three-part) | Header + diagram/data (two-part) |
| Spatial context | Events listed by name; user must map to edges mentally | Events live where their edges are; spatial context is immediate |
| Keyboard flow | Tab through flat list in dock | Tab through edge-anchored panels in diagram z-order |
| Scalability at 5+ events | Dock scrolls vertically; stays usable | Panels start overlapping; requires collapse/expand management |
| Screen reader story | Clean HTML list semantics | `role="region"` panels over SVG; harder to linearize |

---

## Tradeoffs

### Gains

1. **Spatial coherence.** Each event is visually connected to the edge it represents — source state, destination state, and transition direction are immediately visible without cross-referencing between the dock and the diagram.
2. **Vertical real estate.** The dock typically consumes 25-30% of panel height. Removing it gives the diagram room to breathe, especially in short viewports.
3. **Fewer cognitive zones.** The user scans one surface (diagram + panels) instead of two (diagram, then dock below).
4. **Debugging context.** When tracing "why did this transition fire?" the args, outcome, and edge are all in one place.

### Losses

1. **Keyboard navigation regression.** The dock's flat list is inherently keyboard-friendly — a simple `aria-role="list"` with tab stops. Edge-anchored panels require spatial focus management that is harder to implement and test accessibly.
2. **Overlap at scale.** With 5+ events from one state (common in real precepts like hiring pipelines or insurance claims), floating panels will overlap each other. The dock's vertical scroll handles this gracefully.
3. **Arg entry ergonomics.** In the dock, arg fields are always visible and inline. In the diagram model, expanded panels compete for space with the diagram itself, and expanding one may obscure adjacent edges or state nodes.
4. **Visual noise.** Always-expanded panels create a "dashboard" feeling rather than a focused diagram. Collapse-to-reveal adds interaction cost.

### Open Questions

- **Auto-expand vs click-to-reveal?** If panels auto-expand for all current-state events, dense diagrams become cluttered. If they require click, the user can't scan all events at a glance the way the dock allows.
- **Panel positioning strategy.** For self-loops, panels can anchor above the node. For horizontal edges, they can anchor below. But complex topologies (multiple edges between the same pair, backward edges) create positioning conflicts.
- **How does this interact with edit mode?** The dock cleanly separates event execution from field editing. In the diagram model, the data lane is the only non-diagram surface — does edit mode feel orphaned?

---

## Recommendation

**This is worth exploring further but is not ready to replace the dock model.**

The spatial-context gain is real and meaningful for debugging. But the keyboard/accessibility regression and the scalability concern at 5+ events are significant. A hybrid approach may be worth considering:

- Keep the dock as the primary keyboard-accessible event list
- Add edge-hover or edge-click highlighting that scrolls the dock to the relevant event
- Or: use diagram edge labels as a *secondary* fire affordance (click edge → fires event) while keeping the dock as the canonical interaction surface

I'd want Shane to look at both mockups side by side and judge whether the spatial gain outweighs the interaction density loss for real debugging work.

---

## Review Needed

- **Shane:** Does this direction feel worth pursuing, or does the dock model serve debugging better?
- **Peterman:** Brand compliance is identical (same palette, same font, same semantic colors). No brand review needed unless the layout direction changes.
- **Frank:** If this direction moves forward, the SVG overlay / `foreignObject` positioning strategy would need architectural review.

---

# Inspector review refresh decision

**Date:** 2026-04-05  
**Author:** Elaine

## Decision

For PRD and redesign work, treat the inspector as a **combined preview surface**, not a standalone side panel. The baseline UX is a three-part shell:

1. header shell
2. diagram canvas with in-canvas data lane
3. bottom event dock

## Why

- That is the shape implemented in `tools/Precept.VsCode/webview/inspector-preview.html`.
- It matches the archived interaction contract in `docs/archive/InteractiveInspectorMockup.md` closely enough to count as the lived UX baseline.
- The old `brand/inspector-panel-review.md` had become misleading because it audited mostly the data list and under-described the rest of the surface.

## Consequences for the redesign

- Do **not** write the PRD as if the task is only "reskin the inspector list."
- Preserve the diagram/data/event-dock relationship unless Shane signs off on a structural change.
- Evaluate accessibility improvements against the overlay model first; do not assume a table-first replacement without evidence.
- Define AI-first requirements at the preview host contract level, not only as DOM helper functions.

---

# UX Decision: Preview/Inspector Redesign Mockup v1

**Author:** Elaine (UX Designer)  
**Date:** 2026-04-07  
**Issue:** #7 — Create UX mockups for preview/inspector redesign  
**Artifact:** `tools/Precept.VsCode/mockups/preview-inspector-redesign-mockup.html`

---

## What this decides

The first concrete mockup for the preview/inspector redesign establishes the following UX patterns. These are design proposals — not locked until Shane signs off — but they should guide Peterman's brand review and Frank's architecture review.

### 1. Current-state label in header chrome

The current state now appears as an explicit **violet pill badge** in the header bar, alongside the file name and follow/lock mode. This satisfies PRD § 6.2: the current state is unambiguous even when the diagram is dense, and it's trivially extractable by AI agents.

**Rationale:** The diagram already shows current state visually (violet stroke + fill). But for quick scanning, screenshots, and AI extraction, a textual label in the chrome is faster. The violet pill treatment matches the state's semantic color and reads as a status indicator, not navigation.

### 2. Field type as secondary metadata

Each field row now shows type information (`string · nullable`, `number · default 0`) as a second line below the field name, in `--slate-type: #9AA8B5` at 10px. This satisfies PRD § 6.5.

**Rationale:** Field type is useful for debugging and AI comprehension but should not compete with the field name and value for visual weight. A muted secondary line gives the information without cluttering the scan path.

### 3. Constraint message treatment: Gold, not Red

Constraint explanation text (the `because` messages from invariants and assertions) uses **Gold `#FBBF24`** at 11px, per brand-spec § 2.3. This distinguishes constraint explanations from blocked/error signals (Rose).

**Rationale:** The current implementation shares the same red for "this event is blocked" and "here's why the rule exists." Those are different cognitive tasks — one is a status signal, the other is an explanation. Gold separates them.

### 4. State-rules badge

An `⚡ 1 rule` badge appears in the data lane header when invariants are active. This replaces the prior orange badge with a gold-bordered badge that matches the constraint-message color family.

### 5. Event outcome visibility

Each event row now shows the outcome inline: `→ transition Cancelled` or `→ no transition · stays Active`. This makes the destination explicit without requiring diagram reading.

### 6. Title change: "Precept Preview" not "State Diagram"

The header title is now "Precept Preview" — this surface is not just a diagram, it's the integrated preview/inspector. The title should reflect the full product surface.

### 7. Section labels for data lane and event dock

Both zones have small uppercase labels ("INSTANCE DATA", "EVENTS · Active") that clarify the panel structure. The event dock label includes the current state name so the scope is explicit.

### 8. Scenario switcher (mockup-only)

The mockup includes a tab switcher to show both the Active state (two enabled events with args) and the Cancelled state (one blocked, one undefined event with reasons). This is a **mockup-only** affordance — the real implementation switches states through event execution, not tabs.

---

## What this does NOT decide

- Edit mode layout (Save/Cancel flow, draft validation banners) — future mockup pass
- Diagram visual restyling beyond structural color alignment — future mockup pass
- AI-first contract format — needs Frank's architecture review
- Responsive breakpoint behavior — needs testing in real webview

---

## Review needed

- **Peterman:** Brand compliance review against § 1.4 + § 2.3. Especially: violet pill in header, gold constraint messages, emerald/rose event buttons.
- **Frank:** Architecture fit for the current-state label pattern and event outcome display.
- **Shane:** Design gate sign-off before Kramer implements anything.

---

# Preview/Inspector Panel Audit

**Author:** Kramer (Tooling Dev)  
**Date:** 2026-04-05  
**For:** PRD authoring — Shane / coordinator

---

## 1. Current-State Implementation Summary

The inspector/preview panel is a **fully functional, production-quality interactive surface** implemented as a VS Code webview in `tools/Precept.VsCode/webview/inspector-preview.html` (3,464 lines), driven by `tools/Precept.VsCode/src/extension.ts`.

### What it does today:

**Layout:**
- Header: "State Diagram" title + source file name + preview mode indicator (Following/Locked) + Edit/Save/Cancel/Reset buttons
- Main body: SVG state diagram (left, in a scrollable container) + in-canvas data lane (right, fixed-width column)
- Bottom event dock: vertical event list for current-state events

**State diagram:**
- SVG drawn with smooth rounded-corner polyline paths (computed via layout payload from extension host)
- Animated transition: runner dot travels at constant speed along the accepted path, with source collapse / destination arrival handoff
- Edge/node emphasis model: hover highlights matching transitions, non-hovered transitions mute
- Destination node colors reflect evaluated outcome (green/red)
- Toast overlay in diagram on fire result (transient pill chip)

**Event dock:**
- Parallelogram-skewed event buttons (design evolution from mockup's round pills)
- Microstatus glyphs: ✔ (enabled), ✖ (blocked), ∅ (undefined/disabled)
- Inline arg inputs beside each event button (text, number, boolean toggle, nullable toggle)
- Inline reason text for blocked/undefined events, dimmed when not selected, full-bright on hover/focus
- Row-anchored result chip on fire (transient, green/red)
- Keyboard nav: ArrowUp/Down cycles events, Enter fires selected event

**Data panel (in-canvas right lane):**
- `<ul>` list of field name / value pairs
- Rule violation banner (orange, shown when active rule violations exist on the current state)
- State-rules indicator badge (shows count of rule definitions scoped to the current state, with tooltip)
- Draft validation banner (shown during edit mode when form-level errors exist)
- Field-level rule icons (⚠ with tooltip for fields with rule definitions)
- Edit mode: Enter/Save/Cancel workflow, per-field inline inputs (text/number/boolean/null toggle)
- Live draft validation via `inspectUpdate` round-trip on debounced input change
- Data toasts: `before → after` inline animation on successful fire
- Null toggle for nullable fields

**Extension host integration (extension.ts):**
- Single webview panel (`preceptPreview`), singleton lifecycle
- Follow-active-editor mode + preview lock toggle
- postMessage protocol: `previewRequest/previewResponse` (snapshot, fire, reset, inspect, inspectUpdate, update, replay actions)
- Layout computed server-side, delivered in snapshot payload

---

## 2. Biggest Gaps vs. Mockup + Brand Review

### Gap 1: Color System — Still Not Migrated (HIGH)

The `inspector-panel-review.md` identified this and it remains 100% unaddressed:

| Element | Current | Brand Target | Status |
|---|---|---|---|
| State label (`.status`) | `--state: #6D7F9B` | Violet `#A898F5` | ❌ Not done |
| Event names | `--event: #8573A8` | Cyan `#30B8E8` | ❌ Not done |
| Enabled indicator | `--ok: #1FFF7A` (neon green) | `#34D399` (emerald) | ❌ Not done |
| Blocked indicator | `--err: #FF2A57` | `#F87171` (rose) | ❌ Not done |
| Constraint violation messages | `--err` red | Gold `#FBBF24` | ❌ Not done |
| Field names | inherits white | Slate `#B0BEC5` | ❌ Not done |
| Field values (read-only) | `--muted: #59657A` | Slate `#84929F` | ❌ Not done |

**Brand spec reference:** `brand/visual-surfaces-draft.html § Inspector Panel`

### Gap 2: Typography — Still Segoe UI (MEDIUM)

Body font is still `"Segoe UI", Arial, sans-serif`. The brand spec calls for `"Cascadia Cove", "Cascadia Code", "Consolas", monospace` for field names/values (identifiers from `.precept` source). Review's Priority 2.

### Gap 3: Header State Display — Removed (MEDIUM, NEW)

The mockup showed `Current: Red` in the header as a `.status` labeled element. The current implementation removed this — the current state is only visible as the filled/highlighted node inside the SVG diagram. This is not a direct regression (the implementation is richer overall), but the PRD should make a deliberate call: is the current state surfaced in a text header label, in the diagram only, or both?

### Gap 4: Field Types Not Displayed (LOW)

The brand review's Priority 3 — field type info (e.g. `string`, `number`) is not shown next to field names. Not a blocker but useful for debugging type mismatches.

### Gap 5: Inspector Review is Partially Stale (NEW)

The `inspector-panel-review.md` was written at an earlier implementation snapshot. It **misses entirely**:
- Rule violations banner (orange)
- State-rules indicator badge
- Draft validation banner
- Edit mode (Edit/Save/Cancel with live draft inspection)
- Null toggle for nullable fields
- Field-level rule icons

The color mismatch section of the review is still accurate. The "what was found" structural description is significantly incomplete. The PRD should use the implementation file itself as source of truth, not the review doc.

### Gap 6: No JSON Export / `getInspectorState()` (LOW, AI-FIRST)

The review recommended exposing a `getInspectorState()` function for AI consumption (structured `{ currentState, fields, violations }` JSON). Not implemented. The current surface is parseable via DOM but not programmatically exposed.

### Gap 7: `SaveInstance` / `ReplayInstance` Commands (LOW, FUTURE)

The archived design spec's command contract included `SaveInstance(path?)` and `ReplayInstance`. The extension has no save-instance flow. The `replay` action appears in the TypeScript `PreviewAction` type but its full behavior is unconfirmed. These were explicitly marked "future build" in the archived spec.

---

## 3. Source-of-Truth File Paths for PRD Author

| Purpose | File |
|---|---|
| Full webview implementation | `tools/Precept.VsCode/webview/inspector-preview.html` |
| Extension host (commands, panel lifecycle, message protocol) | `tools/Precept.VsCode/src/extension.ts` |
| Mockup (original UX contract — still useful for interaction spec) | `tools/Precept.VsCode/mockups/interactive-inspector-mockup.html` |
| Archived behavior spec | `docs/archive/InteractiveInspectorMockup.md` |
| Brand review (color/typography gaps; partial feature coverage) | `brand/inspector-panel-review.md` |
| Brand color/visual system reference | `brand/visual-surfaces-draft.html` |

---

## 4. Recommendation on `inspector-panel-review.md`

**Refresh it.** The color/typography gap analysis is still accurate and should be kept. The structural "what was found" section needs a full update to reflect the current feature set (edit mode, rule violation banners, state-rules indicator, field icons, null toggles). The AI-first JSON export recommendation should be elevated to a clearer requirement. The review doc is valuable input but must not be used as a complete feature inventory — it predates the current feature set by a significant margin.

---

## Decision Needed

Before PRD authoring proceeds, the following open questions should be resolved:

1. **Color migration priority** — Is the brand color migration (all 7 gaps in Gap 1) a PRD requirement for the redesign, or a separate polish pass?
2. **Current state in header** — Should the current state label be restored to the header, or is diagram-only sufficient?
3. **JSON export** — Is `getInspectorState()` a PRD requirement (AI-first contract) or nice-to-have?
4. **SaveInstance scope** — Is instance save/load in scope for this PRD or explicitly deferred?

---

# Preview panel redesign board blocked on GitHub project scopes

- Requested outcome: create a GitHub project board for the preview panel redesign in the `sfalik` owner context for `sfalik/Precept`.
- What I verified: the active `gh` auth can access the repo but only carries `gist`, `read:org`, `repo`, and `workflow` scopes.
- Blocking fact: GitHub Projects v2 listing fails without `read:project`, and creation requires `project`, so the board cannot be created or verified visible from this session as-is.
- Fallback check: the legacy repo-project REST endpoint for `sfalik/Precept` is not available here (`404`), so there is no classic-project escape hatch.
- PM decision: treat the preview-panel board as blocked pending auth refresh, not as deferred product scope.
- Unblock: refresh `gh` auth with `project,read:project`, then create `Preview Panel Redesign`, add a short description/readme, link `sfalik/Precept`, and confirm it appears in the owner's project list.

---

# UX Decision: Preview Reimagined — Phase 2 (Five More Directions)

**Author:** Elaine (UX Designer)  
**Date:** 2026-04-07  
**Status:** EXPLORATION — awaiting Shane review  
**Artifacts:** `tools/Precept.VsCode/mockups/preview-reimagined-index.html` (updated index with all ten)

---

## Context

Shane loved the first five reimagined preview concepts and asked for five more to push diversity further — bringing the total to ten. These five explore interaction models the first batch didn't cover: comparison, governance, spatial canvas, scenario testing, and dense monitoring.

---

## The Five New Concepts

### 06 — Dual-Pane Diff
**File:** `preview-reimagined-06-dual-pane-diff.html`  
**Metaphor:** Compare any two states side by side.

Pick two states (or two history moments) and see what differs: events, data, rules. Like a code diff but for state-machine snapshots.

**Strengths:**
- Immediately answers "what changes between A and B?" with visual precision
- Developers already think in diffs — this is a natural mental model
- Useful for both debugging (compare before/after a transition) and design review
- Could be a feature within any primary shape, not just standalone

**Weaknesses:**
- Only shows two points at a time — less useful for understanding the whole machine
- With only 3 states and 2 fields, the Subscription sample underplays its value
- More of a utility than a primary product shape
- Selector UX needs careful thought for precepts with 10+ states

---

### 07 — Rule Pressure Map
**File:** `preview-reimagined-07-rule-pressure-map.html`  
**Metaphor:** Constraints as the organizing principle.

Every rule/invariant/assertion is a tile with health status, pressure indicators, driving fields, and "what would violate this?" scenarios.

**Strengths:**
- Only view that inverts the usual state-first or event-first model
- Uniquely answers "is my machine safe?" before asking "where am I?"
- Pressure bars and violation scenarios give proactive governance
- Scales beautifully with complex precepts that have many business rules

**Weaknesses:**
- Not useful for simple precepts with 1-2 rules (feels sparse)
- Doesn't show state transitions or event outcomes directly
- Novel concept — users may not immediately understand the pressure metaphor
- Requires static analysis to detect "at risk" vs "passing" automatically

---

### 08 — Graph Canvas
**File:** `preview-reimagined-08-graph-canvas.html`  
**Metaphor:** The diagram IS the interface.

A full-bleed, zoomable, pannable 2D canvas with interactive state nodes and event edge labels. Direct manipulation: click to enter, click to fire, drag to rearrange.

**Strengths:**
- Spatial understanding is immediate — you see the whole topology
- Direct manipulation (click node to enter, click edge to fire) is deeply intuitive
- Zoom/pan handles any size of precept, from 3 states to 30
- Data overlay keeps field values accessible without cluttering the spatial view

**Weaknesses:**
- Auto-layout for complex precepts is a hard technical problem
- Data and rules are secondary — float as overlays rather than being primary citizens
- VS Code panel real estate is limited for a full canvas experience
- Requires significant rendering infrastructure (SVG/Canvas library)

---

### 09 — Storyboard / Scenario Builder
**File:** `preview-reimagined-09-storyboard-scenarios.html`  
**Metaphor:** Build and replay named event sequences.

A vertical storyboard where each step is a card showing event, args, outcome, and data snapshot. Save/name/replay scenarios. Coverage bar shows what paths you haven't tested yet.

**Strengths:**
- Only concept that treats the preview as a test harness
- Coverage tracking ("2 of 4 transitions covered") is immediately useful for QA
- Scenario library enables saving, sharing, and comparing paths
- Natural complement to Timeline (01): Timeline shows what happened, Storyboard plans what to test
- Saved scenarios could seed automated test suites

**Weaknesses:**
- Building scenarios step-by-step is slower than live fire-and-inspect
- Scenario library needs persistence infrastructure
- The storyboard view is vertical — long scenarios scroll extensively
- Less useful for exploratory "just playing around" usage

---

### 10 — Dashboard / Control Room
**File:** `preview-reimagined-10-dashboard-control-room.html`  
**Metaphor:** All signals at once.

Multiple independently useful widgets: state summary, event heatmap, field value sparklines, constraint health, and an activity feed. An instrument panel for complex precepts.

**Strengths:**
- Maximum information density — power users can see everything without drilling
- Each widget is independently useful and could be mixed into other shapes
- Heatmap is a compact version of the Decision Matrix (03) with less overhead
- Sparklines show field value history — a visual dimension no other concept offers
- Activity feed doubles as a history log

**Weaknesses:**
- Dense UIs can overwhelm new users — steep learning curve
- Six widgets in a VS Code panel is ambitious for screen real estate
- Subscription sample (3 states, 2 fields) is too small to fully demonstrate the value
- Widget layout may need to be configurable — one size won't fit all precepts

---

## Updated Recommendation

The original top picks hold: **01 (Timeline)** and **05 (Notebook)** remain the strongest primary shapes for their combination of debugger power and comprehensive coverage.

Phase 2 adds one compelling new primary contender:

**09 (Storyboard / Scenarios)** is the standout. It's the only concept that frames the preview as a testing and verification surface — not just observation. A **Timeline + Storyboard hybrid** (history for debugging, named scenarios for verification, coverage tracking for completeness) would be uniquely powerful and differentiated from any other VS Code extension.

**07 (Rule Pressure Map)** introduces a genuinely novel lens worth pursuing as a secondary mode. For precepts with complex business rules, seeing constraints as the organizing principle rather than states is the fastest path to "is my machine correct?"

**10 (Dashboard)** is the power-user play — strong for complex precepts, potentially overwhelming for simple ones. Worth prototyping as a "command center" mode.

**06 (Dual-Pane Diff)** and **08 (Graph Canvas)** are strong utility concepts that could be features within whatever primary shape is chosen, rather than standalone product shapes.

---

## Design Space Summary (All 10)

| # | Concept | Primary Lens | Unique Strength |
|---|---------|-------------|-----------------|
| 01 | Timeline Debugger | Time/history | "How did I get here?" |
| 02 | Conversational REPL | Text/commands | AI-native, greppable |
| 03 | Decision Matrix | Completeness | Full truth table at a glance |
| 04 | Focus / Spotlight | Present moment | Maximum clarity, minimal UI |
| 05 | Notebook / Report | Document/sections | Complete coverage, shareable |
| 06 | Dual-Pane Diff | Comparison | Visual diff between any two states |
| 07 | Rule Pressure Map | Governance/rules | Constraints-first, proactive safety |
| 08 | Graph Canvas | Spatial/topology | The diagram IS the interface |
| 09 | Storyboard / Scenarios | Testing/workflows | Build, save, replay, coverage |
| 10 | Dashboard / Control Room | Multi-signal | Everything at once, data-dense |

---

## Review Needed

- **Shane:** Across all ten — which resonated? Phase 2 standouts?
- **Peterman:** Brand compliance check on 06–10. All use the locked palette, but new interaction patterns (diff markers, pressure bars, canvas, sparklines) should be reviewed.
- **Frank:** Architecture implications — 09 needs scenario persistence, 07 needs static analysis for pressure scoring, 08 needs canvas rendering. Which are cheapest to prototype?

---

## Files Created / Updated

| File | Description |
|------|-------------|
| `tools/Precept.VsCode/mockups/preview-reimagined-06-dual-pane-diff.html` | Concept 06 |
| `tools/Precept.VsCode/mockups/preview-reimagined-07-rule-pressure-map.html` | Concept 07 |
| `tools/Precept.VsCode/mockups/preview-reimagined-08-graph-canvas.html` | Concept 08 |
| `tools/Precept.VsCode/mockups/preview-reimagined-09-storyboard-scenarios.html` | Concept 09 |
| `tools/Precept.VsCode/mockups/preview-reimagined-10-dashboard-control-room.html` | Concept 10 |
| `tools/Precept.VsCode/mockups/preview-reimagined-index.html` | Updated to include all ten + revised recommendation |

All first-five mockups are preserved. Shared CSS unchanged.

---

# UX Decision: Preview Reimagined — Five Alternative Directions

**Author:** Elaine (UX Designer)  
**Date:** 2026-04-07  
**Status:** EXPLORATION — awaiting Shane review  
**Artifacts:** `tools/Precept.VsCode/mockups/preview-reimagined-index.html` (index linking all five)

---

## Context

Shane asked me to think outside the box and reimagine what the preview could look like. The current preview surface (header + diagram + data lane + event dock) is functionally strong but architecturally fixed. This exploration asks: what if the core interaction model were fundamentally different?

All five concepts serve the same core jobs:
1. Understand the current state and what transitions are available
2. Inspect current data and active rule pressure
3. Try events and immediately understand outcomes
4. Support real debugging work inside VS Code

---

## The Five Concepts

### 01 — Timeline Debugger
**File:** `preview-reimagined-01-timeline-debugger.html`  
**Metaphor:** Time is the primary axis.

A horizontal timeline of fired events dominates the top. Click any point to see state + data at that moment. Below: a split view of data diffs (what changed) and available next actions.

**Strengths:**
- Uniquely answers "how did I get here?" — no other concept does this
- Data diffs at each step are immediately useful for debugging
- Scrubbing through history is natural for complex multi-step workflows
- Timeline is a visual debugger pattern developers already know

**Weaknesses:**
- History requires runtime tracking infrastructure the extension doesn't have yet
- Timeline gets unwieldy for 20+ step sessions
- Diagram is absent — spatial mental model is lost
- Initial view (no history yet) is sparse

---

### 02 — Conversational REPL
**File:** `preview-reimagined-02-conversational-repl.html`  
**Metaphor:** Type events, read results.

A scrolling command log replaces the diagram. State + data live in a compact sidebar. You type event commands and the system responds with structured outcome blocks.

**Strengths:**
- AI agents would consume this format natively — structured input/output
- Text-first means everything is greppable, copiable, shareable
- Builds on terminal/REPL familiarity developers already have
- Conversation log is a natural audit trail

**Weaknesses:**
- No spatial overview — you lose the diagram's "where am I in the machine?" view
- Scrolling log gets long; hard to see current state at a glance after 10+ actions
- Typing event names + args is slower than clicking
- Novel for VS Code panels — users expect visual content, not a terminal

---

### 03 — Decision Matrix
**File:** `preview-reimagined-03-decision-matrix.html`  
**Metaphor:** Every outcome in one table.

State × Event truth table. Rows = states, columns = events, cells = outcomes. Click a cell to inspect detail in a side panel.

**Strengths:**
- Only view that shows the FULL contract at once — completeness at a glance
- Immediately reveals undefined transitions, blocked paths, dead ends
- Great for design review ("is this machine correct?")
- Table structure is inherently accessible and AI-parseable

**Weaknesses:**
- Scales poorly for precepts with many states/events (10×10+ gets cramped)
- Doesn't show "current state" as strongly — it's just a row highlight
- Static feel — less useful for live debugging, more for contract review
- Data and fields are secondary; not a data-first view

---

### 04 — Focus / Spotlight
**File:** `preview-reimagined-04-focus-spotlight.html`  
**Metaphor:** One thing at a time, large and clear.

Current state name is massive, center-screen. Available transitions radiate outward as interactive cards. Data orbits beneath. Minimal chrome.

**Strengths:**
- Absolute clarity about current state — no ambiguity
- Cards for each path provide rich context without clutter
- Context-adaptive: the view can shift based on last action
- Beautiful, zen-like — great for demos, presentations, first impressions

**Weaknesses:**
- Scales poorly: 5+ events would overflow the horizontal cards
- No diagram, no history — only shows the present moment
- Feels light on information density for power users
- Data section is too compact for precepts with 10+ fields

---

### 05 — Notebook / Report
**File:** `preview-reimagined-05-notebook-report.html`  
**Metaphor:** A live, scrollable document.

Vertical card-based sections: contract overview, current state, data, events, rules, mini diagram. Progressive disclosure via expand/collapse.

**Strengths:**
- Complete coverage of every aspect in one scrollable view
- Progressive disclosure handles complexity well (collapse what you don't need)
- Readable, shareable, potentially printable for review sessions
- Card structure maps naturally to AI agent consumption (section by section)
- Accommodates precepts of any size — just adds more cards

**Weaknesses:**
- Vertical scrolling means the diagram is "below the fold"
- Less immediate than the current surface for quick fire/inspect loops
- Can feel long and document-heavy for simple precepts
- Card-based layout is common but not distinctive

---

## Recommendation

**Deeper iteration:** Concepts **01 (Timeline Debugger)** and **05 (Notebook / Report)**.

The Timeline gives debugger-grade "how did I get here?" power that no current design offers. The Notebook gives complete, readable coverage that works for understanding, sharing, and AI consumption. A hybrid — notebook structure with an embedded timeline and inline event execution — could be the strongest future direction.

**Strong secondary:** Concept **03 (Decision Matrix)** as a mode/tab alongside whatever primary shape is chosen. The "show me everything" view is uniquely valuable for contract review and catches problems the other views miss.

---

## Review Needed

- **Shane:** Which concepts resonate? Which feel right for the product?
- **Peterman:** Brand compliance check on all five — all use the locked palette, but card layouts, typography hierarchy, and visual density vary.
- **Frank:** Architecture implications — especially 01 (needs history tracking) and 02 (needs text input parsing). Which are cheapest to prototype?

---

## Files Created

| File | Description |
|------|-------------|
| `tools/Precept.VsCode/mockups/preview-reimagined-index.html` | Index linking all five concepts |
| `tools/Precept.VsCode/mockups/preview-reimagined-01-timeline-debugger.html` | Concept 01 |
| `tools/Precept.VsCode/mockups/preview-reimagined-02-conversational-repl.html` | Concept 02 |
| `tools/Precept.VsCode/mockups/preview-reimagined-03-decision-matrix.html` | Concept 03 |
| `tools/Precept.VsCode/mockups/preview-reimagined-04-focus-spotlight.html` | Concept 04 |
| `tools/Precept.VsCode/mockups/preview-reimagined-05-notebook-report.html` | Concept 05 |
| `tools/Precept.VsCode/mockups/preview-reimagined-shared.css` | Shared brand-aligned styles |

All existing mockups are preserved.

---

# Frank — language proposal review

Date: 2026-04-05

## Decision summary

Reviewed issues `#8` through `#13` and added architectural comments directly on GitHub.

## Recommended sequencing

1. **First wave**
   - `#10` String length accessor
   - `#8` Named guard declarations
2. **Second wave**
   - `#9` Ternary expressions in `set` mutations
   - `#11` Event argument absorb shorthand
3. **Last wave / explicit architectural review required**
   - `#12` Inline guarded fallback (`else reject`)
   - `#13` Field-level range/basic constraints

## Architectural conclusions

- `#10` is the safest proposal: it extends the existing expression/member-access model and should be treated as the string analogue of collection `.count`.
- `#8` is a good early declaration-form addition if scoped as reusable boolean symbols, not macros and not new control flow.
- `#9` is acceptable if it remains strictly about value selection in `set` RHS positions; it must not become a disguised outcome-branching feature.
- `#11` should be an explicit action keyword that desugars to ordered `set` operations. No hidden header inference; ambiguous mappings must fail closed.
- `#12` must stay syntax sugar for an existing guarded-row-plus-reject-row pair. Do not let it widen into general inline branching.
- `#13` is the highest-risk proposal because it pressures the DSL's keyword-anchored grammar. If no syntax preserves that discipline cleanly, the correct answer is to reject the feature.

## Guardrails for later design work

- Preserve **keyword-anchored flat statements** as a first-class design constraint.
- Preserve **top-to-bottom first-match routing**; do not trade concision for a muddier control-flow model.
- Favor **desugaring to existing semantic forms** where possible so runtime, MCP, diagnostics, and tooling stay aligned.
- Keep proposals narrowly scoped. None of these should be allowed to smuggle in regex validation, hierarchical states, or generalized inline branching.

---

# Steinbrenner — Language Proposal Intake

**Date:** 2026-04-05  
**Requested by:** Shane

## Framing

Created GitHub Project v2 **Precept Language Improvements** and loaded the first six proposal issues there so the language roadmap has a single queue:

- Project: https://github.com/users/sfalik/projects/2

## Proposal set

This six-issue bundle preserves the strongest remembered set from the DSL expressiveness research plus the hero-sample condensation pass:

1. Direct expressiveness proposals already ranked in research:
   - #8 — Proposal: Named guard declarations
   - #9 — Proposal: Ternary expressions in set mutations
   - #10 — Proposal: String length accessor
2. Hero-condensation / verbosity reducers that repeatedly surfaced in corpus review:
   - #11 — Proposal: Event argument absorb shorthand
   - #12 — Proposal: Inline guarded fallback (`else reject`)
   - #13 — Proposal: Field-level range/basic constraints

## Caveat to carry forward

Issue #13 is intentionally included even though it is the weakest of the six from a design-fit standpoint. The research explicitly flags field-inline constraints as being in tension with Precept's keyword-anchored statement model, so that issue should be treated as a proposal to evaluate carefully, not as a presumed roadmap commitment.

## Sequencing note

If we want a fast first pass on language value vs. implementation cost, the clean review order is:

1. #8 Named guards
2. #9 Ternary in `set`
3. #10 String `.length`
4. #11 Event absorb shorthand
5. #12 Inline `else reject`
6. #13 Field-level basic constraints (caveated)
